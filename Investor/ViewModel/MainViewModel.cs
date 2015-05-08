﻿using GalaSoft.MvvmLight;
using SharedFeatures.Model;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Investor.Model;
using System;
using GalaSoft.MvvmLight.Command;

namespace Investor.ViewModel
{

    public class MainViewModel : ViewModelBase
    {
        private IDataService data;
        private InvestorDepot depot;
                
        public MainViewModel(IDataService data)
        {
            this.data = data;
            depot = data.LoadInvestorInformation();
            MarketInformation = new ObservableCollection<ShareInformation>(data.LoadMarketInformation());
            OwnedShares = new ObservableCollection<OwningShareDTO>();
            UpdateOwnedShares();
            PendingOrders = new ObservableCollection<Order>(data.LoadPendingOrders());
            data.AddNewInvestorInformationAvailableCallback(UpdateInvestorInformation);
            data.AddNewMarketInformationAvailableCallback(UpdateShareInformation);
            data.AddNewPendingOrdersCallback(o => PendingOrders = new ObservableCollection<Order>(o));
            PlaceBuyingOrderCommand = new RelayCommand(PlaceBuyingOrder, () => SelectedBuyingShare != null);
            PlaceSellingOrderCommand = new RelayCommand(PlaceSellingOrder, () => SelectedSellingShare != null);
            CancelPendingOrderCommand = new RelayCommand(CancelPendingOrder, () => SelectedPendingOrder != null);
            LogoutCommand = new RelayCommand(Logout, () => true);
        }

        private void UpdateInvestorInformation(InvestorDepot d)
        {
            depot = d;
            RaisePropertyChanged(() => Budget);
            UpdateOwnedShares();
        }

        private void UpdateOwnedShares()
        {
            var collection = new ObservableCollection<OwningShareDTO>();

            foreach (String shareName in depot.Shares.Keys)
            {
                 var infos = MarketInformation.Where(x => x.FirmName == shareName).ToList();
                ShareInformation info = infos.First();
                if (info != null) {
                    OwningShareDTO s = new OwningShareDTO()
                    {
                        ShareName = shareName,
                        Amount = depot.Shares[shareName],
                        StockPrice = info.PricePerShare
                    };
                    collection.Add(s);
                }
            }

            OwnedShares = collection;

        }

        private void UpdateShareInformation(ShareInformation info)
        {
            MarketInformation = new ObservableCollection<ShareInformation>(MarketInformation.Where(x => x.FirmName != info.FirmName));
            MarketInformation.Add(info);
        }

        public string Email { get { return depot.Email; } }

        public double Budget { get { return depot.Budget; } }

        private ObservableCollection<ShareInformation> marketInformation;
        public ObservableCollection<ShareInformation> MarketInformation
        {
            get
            {
                return marketInformation;
            }
            set
            {
                marketInformation = value;
                RaisePropertyChanged(() => MarketInformation);
            }
        }

        private ObservableCollection<OwningShareDTO> ownedShares;
        public ObservableCollection<OwningShareDTO> OwnedShares
        {
            get
            {
                return ownedShares;
            }
            set
            {
                ownedShares = value;
                RaisePropertyChanged(() => OwnedShares);
            }
        }

        private ObservableCollection<Order> pendingOrders;
        public ObservableCollection<Order> PendingOrders
        {
            get
            {
                return pendingOrders;
            }
            set
            {
                pendingOrders = value;
                RaisePropertyChanged(() => PendingOrders);
            }
        }

        private ShareInformation selectedBuyingShare;
        public ShareInformation SelectedBuyingShare
        {
            get
            {
                return selectedBuyingShare;
            }
            set
            {
                selectedBuyingShare = value;
                RaisePropertyChanged(() => SelectedBuyingShare);
                PlaceBuyingOrderCommand.RaiseCanExecuteChanged();
            }
        }

        private OwningShareDTO selectedSellingShare;
        public OwningShareDTO SelectedSellingShare
        {
            get
            {
                return selectedSellingShare;
            }
            set
            {
                selectedSellingShare = value;
                RaisePropertyChanged(() => SelectedSellingShare);
                PlaceSellingOrderCommand.RaiseCanExecuteChanged();
            }
        }

        private Order selectedPendingOrder;
        public Order SelectedPendingOrder
        {
            get
            {
                return selectedPendingOrder;
            }
            set
            {
                selectedPendingOrder = value;
                RaisePropertyChanged(() => SelectedPendingOrder);
                CancelPendingOrderCommand.RaiseCanExecuteChanged();
            }
        }

        private int noOfSharesBuying;
        public int NoOfSharesBuying
        {
            get
            {
                return noOfSharesBuying;
            }
            set
            {
                noOfSharesBuying = value;
                RaisePropertyChanged(() => NoOfSharesBuying);
            }
        }

        private int noOfSharesSelling;
        public int NoOfSharesSelling
        {
            get
            {
                return noOfSharesSelling;
            }
            set
            {
                noOfSharesSelling = value;
                RaisePropertyChanged(() => NoOfSharesSelling);
            }
        }

        private double upperPriceLimit;
        public double UpperPriceLimit
        {
            get
            {
                return upperPriceLimit;
            }
            set
            {
                upperPriceLimit = value;
                RaisePropertyChanged(() => UpperPriceLimit);
            }
        }

        private double lowerPriceLimit;
        public double LowerPriceLimit
        {
            get
            {
                return lowerPriceLimit;
            }
            set
            {
                lowerPriceLimit = value;
                RaisePropertyChanged(() => LowerPriceLimit);
            }
        }

        public RelayCommand PlaceBuyingOrderCommand { get; private set; }

        public RelayCommand PlaceSellingOrderCommand { get; private set; }

        public RelayCommand CancelPendingOrderCommand { get; private set; }

        public RelayCommand LogoutCommand { get; private set; }

        private void OnNewMarketInformationAvailable(ShareInformation nu)
        {
            var tmp = MarketInformation.Where(x => x.FirmName.Equals(nu.FirmName));
            var old = tmp.Count() == 0 ? null : tmp.First();
            if (old != null)
            {
                MarketInformation.Insert(MarketInformation.IndexOf(old), nu);
                MarketInformation.Remove(old);
            }
            else
            {
                MarketInformation.Add(nu);
            }
        }

        private void PlaceBuyingOrder()
        {
            var id = Email + DateTime.Now.Ticks.ToString();
            var order = new Order() { Id = id, InvestorId = Email, Type = Order.OrderType.BUY, ShareName = SelectedBuyingShare.FirmName, Limit = UpperPriceLimit, TotalNoOfShares = NoOfSharesBuying, NoOfProcessedShares = 0 };
            data.PlaceOrder(order);
        }

        private void PlaceSellingOrder()
        {
            var id = Email + DateTime.Now.Ticks.ToString();
            var order = new Order() { Id = id, InvestorId = Email, Type = Order.OrderType.SELL, ShareName = SelectedSellingShare.ShareName, Limit = LowerPriceLimit, TotalNoOfShares = NoOfSharesSelling, NoOfProcessedShares = 0 };
            data.PlaceOrder(order);
        }

        private void CancelPendingOrder()
        {
            data.CancelOrder(SelectedPendingOrder);
        }

        private void Logout()
        {
            data.Dispose();
            App.Current.Shutdown();
        }
    }
}