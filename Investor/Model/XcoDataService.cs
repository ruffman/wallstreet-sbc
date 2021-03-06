﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using XcoSpaces;
using XcoSpaces.Collections;
using XcoSpaces.Exceptions;
using SharedFeatures;
using SharedFeatures.Model;
using GalaSoft.MvvmLight;

namespace Investor.Model
{
    class XcoDataService : IDataService
    {
        private XcoSpace space;
        private XcoQueue<Registration> registrations;
        private XcoList<InvestorDepot> investorDepots;
        private XcoList<ShareInformation> stockInformation;
        private XcoList<Order> orders;
        private XcoQueue<Order> orderQueue;
        private IList<Action> marketCallbacks;
        private IList<Action> investorDepotCallbacks;
        private IList<Action> pendingOrdersCallback;
        private IList<ShareInformation> shareInformationCache;
        private IList<Order> orderCache;
        private Registration registration;
        private InvestorDepot depot;

        public XcoDataService(Uri spaceServerUri)
        {
            space = new XcoSpace(0);
            marketCallbacks = new List<Action>();
            investorDepotCallbacks = new List<Action>();
            pendingOrdersCallback = new List<Action>();
            shareInformationCache = new List<ShareInformation>();
            orderCache = new List<Order>();
            depot = null;
            registrations = space.Get<XcoQueue<Registration>>("InvestorRegistrations", spaceServerUri);
            investorDepots = space.Get<XcoList<InvestorDepot>>("InvestorDepots", spaceServerUri);
            investorDepots.AddNotificationForEntryAdd(OnInvestorDepotAdded);
            stockInformation = space.Get<XcoList<ShareInformation>>("StockInformation", spaceServerUri);
            stockInformation.AddNotificationForEntryAdd(OnShareInformationAdded);
            orders = space.Get<XcoList<Order>>("Orders", spaceServerUri);
            orders.AddNotificationForEntryAdd(OnNewOrderAdded);
            orderQueue = space.Get<XcoQueue<Order>>("OrderQueue", spaceServerUri);
        }

        public void Login(Registration r)
        {
            registration = r;
            using (XcoTransaction tx = space.BeginTransaction())
            {
                try
                {
                    this.registrations.Enqueue(r);
                    tx.Commit();
                }
                catch (XcoException e)
                {
                    Console.WriteLine("Investor: " + e.StackTrace);
                    tx.Rollback();
                }
            }
        }

        public void PlaceOrder(Order order)
        {
            orderQueue.Enqueue(order);
        }

        public void CancelOrder(Order order)
        {
            using (XcoTransaction tx = space.BeginTransaction())
            {

                try
                {
                    int index = -1;
                    for (int i = 0; i < orders.Count; i++)
                    {
                        
                        if (orders[i].Id.Equals(order.Id))
                        {

                            Console.WriteLine(orders[i]);
                            index = i;
                            break;
                        }
                    }

                    if (index >= 0)
                    {
                        orders.RemoveAt(index);
                        order.Status = Order.OrderStatus.DELETED;
                        orders.Add(order);
                    }

                    tx.Commit();
                }
                catch (XcoException e)
                {
                    tx.Rollback();
                }

            }
        }

        public void SetSpace(string space)
        {

        }

        public IEnumerable<string> ListOfSpaces()
        {
            return null;
        }

        public InvestorDepot LoadInvestorInformation()
        {
            return depot;
        }

        public IEnumerable<ShareInformation> LoadMarketInformation()
        {
            shareInformationCache = new List<ShareInformation>();
            orderCache = new List<Order>();

            for (int i = 0; i < orders.Count; i++)
            {
                orderCache.Add(orders[i]);
            }
            using (XcoTransaction tx = space.BeginTransaction())
            {
                try
                {

                        for (int i = 0; i < stockInformation.Count; i++) {
                            ShareInformation s = stockInformation[i];

                        shareInformationCache.Add(new ShareInformation()
                        {
                            FirmName = s.FirmName,
                            NoOfShares = s.NoOfShares,
                            PurchasingVolume = GetPurchasingVolume(orderCache, s.FirmName),
                            SalesVolume = GetSalesVolume(orderCache, s.FirmName),
                            PricePerShare = s.PricePerShare
                        });
                    }

                    tx.Commit();
                }
                catch (XcoException e)
                {
                    Console.WriteLine("Investor: " + e.StackTrace);
                    tx.Rollback();
                }
            }

            return shareInformationCache;
        }

        public IEnumerable<Order> LoadPendingOrders()
        {
            if (orderCache.Count == 0)
            {
                LoadMarketInformation();
            }
            return orderCache.Where(x => x.InvestorId == depot.Email && x.NoOfOpenShares > 0 && x.Status != Order.OrderStatus.DONE && x.Status != Order.OrderStatus.DELETED);
        }

        public void AddNewMarketInformationAvailableCallback(Action callback)
        {
            marketCallbacks.Add(callback);
        }

        public void AddNewInvestorInformationAvailableCallback(Action callback)
        {
            investorDepotCallbacks.Add(callback);
        }

        public void AddNewPendingOrdersCallback(Action callback)
        {
            pendingOrdersCallback.Add(callback);
        }

        public void RemoveNewInvestorInformationAvailableCallback(Action callback)
        {
            investorDepotCallbacks.Remove(callback);
        }

        private void OnInvestorDepotAdded(XcoList<InvestorDepot> source, InvestorDepot d, int k)
        {
            if (this.registration != null && this.registration.Email == d.Email)
            {
                depot = d;
                ExecuteOnGUIThread(investorDepotCallbacks);
            }
        }

        private void OnShareInformationAdded(XcoList<ShareInformation> source, ShareInformation share, int index)
        {
            share.PurchasingVolume = GetPurchasingVolume(orderCache, share.FirmName);
            share.SalesVolume = GetSalesVolume(orderCache, share.FirmName);
            shareInformationCache.Add(share);
            ExecuteOnGUIThread(marketCallbacks);
        }

        private void OnNewOrderAdded(XcoList<Order> source, Order order, int index)
        {
            orderCache = orderCache.Where(x => x.Id != order.Id).ToList();
            orderCache.Add(order);
            UpdateShareInformation(order);
        }

        private void UpdateShareInformation(Order order)
        {
            var match = shareInformationCache.Where(x => x.FirmName == order.ShareName);
            var share = match.Count() > 0 ? match.First() : null;
            if (share != null)
            {
                share.PurchasingVolume = GetPurchasingVolume(orderCache, order.ShareName);
                share.SalesVolume = GetSalesVolume(orderCache, order.ShareName);
                ExecuteOnGUIThread(marketCallbacks);

                if (depot != null)
                {
                    UpdatePendingOrders();
                }
            }
        }

        private void UpdatePendingOrders()
        {
            ExecuteOnGUIThread(pendingOrdersCallback);
        }

        private int GetPurchasingVolume(IEnumerable<Order> orders, string key)
        {
            return orders.Where(x => x.ShareName == key && x.Type == Order.OrderType.BUY && x.Status != Order.OrderStatus.DONE && x.Status != Order.OrderStatus.DELETED).Sum(x => x.NoOfOpenShares);
        }

        private int GetSalesVolume(IEnumerable<Order> orders, string key)
        {
            return orders.Where(x => x.ShareName == key && x.Type == Order.OrderType.SELL && x.Status != Order.OrderStatus.DONE && x.Status != Order.OrderStatus.DELETED).Sum(x => x.NoOfOpenShares);
        }

        private void ExecuteOnGUIThread<T>(IEnumerable<Action<T>> callbacks, T arg)
        {
            foreach (Action<T> callback in callbacks)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    callback(arg);
                }), null);
            }
        }

        private void ExecuteOnGUIThread(IEnumerable<Action> callbacks)
        {
            foreach (Action callback in callbacks)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    callback();
                }), null);
            }
        }

        public void Dispose()
        {
            space.Close();
        }
    }
}
