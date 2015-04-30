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

namespace SpaceServer
{
    class Server
    {
        static void Main(string[] args)
        {
            using (XcoSpace space = new XcoSpace(9000))
            {
                XcoQueue<Request> qRequests = new XcoQueue<Request>();
                space.Add(qRequests, "RequestQ");
                qRequests.AddNotificationForEntryEnqueued((s, r) => Console.WriteLine("New request queued for {0}, publishing {1} shares for {2} Euros.", r.FirmName, r.Shares, r.PricePerShare));

                XcoDictionary<string, FirmDepot> firmDepots = new XcoDictionary<string, FirmDepot>();
                space.Add(firmDepots, "FirmDepots");
                firmDepots.AddNotificationForEntryAdd((s, k, r) => Console.WriteLine("Depot entry created/overwritten for {0}, publishing/adding {1} shares.", k, r.OwnedShares));

                XcoDictionary<string, double> stockPrices = new XcoDictionary<string, double>();
                space.Add(stockPrices, "StockPrices");
                stockPrices.AddNotificationForEntryAdd((s, k, r) => Console.WriteLine("New price for {0} is {1} Euros.", k, r));

                Console.WriteLine("Press enter to quit ...");
                Console.ReadLine();
                space.Remove(qRequests);
                space.Remove(firmDepots);
                space.Remove(stockPrices);
                space.Close();
            }
        }
    }
}
