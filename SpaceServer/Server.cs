﻿using SharedFeatures.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XcoSpaces;
using XcoSpaces.Collections;
using XcoSpaces.Exceptions;
using XcoSpaces.Kernel;
using XcoSpaces.Kernel.Selectors;

namespace SpaceServer
{
    class Server
    {
        static void Main(string[] args)
        {
            using (XcoKernel kernel = new XcoKernel())
            using (XcoSpace space = new XcoSpace(0))
            {
                Console.WriteLine("Use the following URL to connect to this space: " + space.Address);
                
                kernel.Start(space.Address.Port + 1);
                ContainerReference cref = kernel.CreateNamedContainer(null, "BrokerIdContainer", 1, new LindaSelector());
                kernel.Write(cref, null, 0, new Entry(new XcoSpaces.Kernel.Selectors.Tuple(new TupleValue<int>(0))));
                
                var qRequests = new XcoQueue<Request>();
                space.Add(qRequests, "RequestQ");
                qRequests.AddNotificationForEntryEnqueued((s, r) => Console.WriteLine("New request queued for {0}, publishing {1} shares for {2} Euros.", r.FirmName, r.Shares, r.PricePerShare));

                var firmDepots = new XcoDictionary<string, FirmDepot>();
                space.Add(firmDepots, "FirmDepots");
                firmDepots.AddNotificationForEntryAdd((s, k, r) => Console.WriteLine("Depot entry created/overwritten for {0}, publishing/adding {1} shares.", k, r.OwnedShares));

                var stockInformation = new XcoList<ShareInformation>();
                space.Add(stockInformation, "StockInformation");
                stockInformation.AddNotificationForEntryAdd((s, k, r) => Console.WriteLine("New info for {0}: price per share {1:C}, at a volume of {2} shares.", k.FirmName, k.PricePerShare, k.NoOfShares));

                var stockInformationUpdates = new XcoQueue<string>();
                space.Add(stockInformationUpdates, "StockInformationUpdates");
                stockInformationUpdates.AddNotificationForEntryEnqueued((s,r) => Console.WriteLine("New update for share {0}", r));

                var investorRegistrations = new XcoQueue<Registration>();
                space.Add(investorRegistrations, "InvestorRegistrations");
                investorRegistrations.AddNotificationForEntryEnqueued((s, r) => Console.WriteLine("New registration queued for Email address {0} and budget {1}.", r.Email, r.Budget));

                var fundRegistrations = new XcoQueue<FundRegistration>();
                space.Add(fundRegistrations, "FundRegistrations");
                fundRegistrations.AddNotificationForEntryEnqueued((s, r) => Console.WriteLine("New registration queue for fund id {0}, fund assets {1} and amount of shares {2}", r.FundID, r.FundAssets, r.FundShares));

                var investorDepots = new XcoList<InvestorDepot>();
                space.Add(investorDepots, "InvestorDepots");
                investorDepots.AddNotificationForEntryAdd((s, r, k) => Console.WriteLine("New investor depot entry for Email address {0} (Budget: {1}).", r.Email, r.Budget));

                var fundDepots = new XcoList<FundDepot>();
                space.Add(fundDepots, "FundDepots");
                fundDepots.AddNotificationForEntryAdd((s, r, k) => Console.WriteLine("New fund depot entry for fund id {0} (fund assets: {1}, fund shares: {2}", r.FundID, r.FundBank, r.FundShares));

                var fundDepotQueue = new XcoQueue<FundDepot>();
                space.Add(fundDepotQueue, "FundDepotQueue");
                fundDepotQueue.AddNotificationForEntryEnqueued((s, f) => Console.WriteLine("New fund depot entry for fund id {0} added to queue", f.FundID));

                var orders = new XcoList<Order>();
                space.Add(orders, "Orders");
                orders.AddNotificationForEntryAdd((s, v, k) => Console.WriteLine("New {0} order for Investor {1}, intending to buy {2} shares from {3}.", v.Type, v.InvestorId, v.ShareName, v.TotalNoOfShares));

                var orderUpdates = new XcoQueue<Order>();
                space.Add(orderUpdates, "OrderQueue");
                orderUpdates.AddNotificationForEntryEnqueued((s, v) => Console.WriteLine("Updated order of type {0} for Investor {1}, intending to buy {2} shares from {3}.", v.Type, v.InvestorId, v.ShareName, v.TotalNoOfShares));

                var transactions = new XcoList<Transaction>();
                space.Add(transactions, "Transactions");
                transactions.AddNotificationForEntryAdd((s, t, i) => Console.WriteLine("New transaction between {1} and {2}, transfering {2} shares for {3} Euros per share.", t.SellerId, t.BuyerId, t.NoOfSharesSold, t.PricePerShare));

                Console.WriteLine("Press enter to quit ...");
                Console.ReadLine();
                space.Remove(qRequests);
                space.Remove(firmDepots);
                space.Remove(stockInformation);
                space.Remove(investorRegistrations);
                space.Remove(investorDepots);
                space.Remove(orders);
                space.Remove(transactions);
                space.Close();
            }
        }
    }
}
