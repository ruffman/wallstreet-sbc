using SharedFeatures.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using XcoSpaces;
using XcoSpaces.Collections;
using XcoSpaces.Exceptions;

namespace Agent
{
    class Program
    {
        static private IObservable<long> timer;
        static private XcoSpace space;
        static private XcoDictionary<string, Tuple<int, double>> stockPrices;
        static private XcoDictionary<string, FirmDepot> firmDepots;
        static private XcoDictionary<string, Order> orders;
        static private XcoDictionary<string, InvestorDepot> investorDepots;

        static void Main(string[] args)
        {
            try
            {
                space = new XcoSpace(0);
                stockPrices = space.Get<XcoDictionary<string, Tuple<int, double>>>("StockInformation", new Uri("xco://" + Environment.MachineName + ":" + 9000));
                firmDepots = space.Get<XcoDictionary<string, FirmDepot>>("FirmDepots", new Uri("xco://" + Environment.MachineName + ":" + 9000));
                orders = space.Get<XcoDictionary<string, Order>>("Orders", new Uri("xco://" + Environment.MachineName + ":" + 9000));
                investorDepots = space.Get<XcoDictionary<string, InvestorDepot>>("InvestorDepots", new Uri("xco://" + Environment.MachineName + ":" + 9000));

                timer = Observable.Interval(TimeSpan.FromSeconds(2));
                timer.Subscribe(_ => UpdateStockPrices());

                Random random = new Random();
                while (true)
                {
                    /*
                    Thread.Sleep(3000);
                    stockPrices["GOOG"] = 563.30 + random.Next(100);
                    Thread.Sleep(3000);
                    stockPrices["AMZN"] = 262.51 + random.Next(50);
                    Thread.Sleep(3000);
                    stockPrices["Alete"] = 12.00 + random.Next(10);
                     */
                    Thread.Sleep(1000);
                }
            }
            catch (XcoException)
            {
                Console.WriteLine("Unable to reach server.\nPress enter to exit.");
                Console.ReadLine();
            }
  
        }

        static void UpdateStockPrices()
        {
            using (XcoTransaction tx = space.BeginTransaction())
            {
                Console.WriteLine("UPDATE stock prices: " + DateTime.Now);

                foreach(string firmKey in firmDepots.Keys) {

                    if (stockPrices.ContainsKey(firmKey))
                    {
                        Tuple<int, double> oldPrice = stockPrices[firmKey];
                        long pendingBuyOrders = PendingOrders(firmKey, Order.OrderType.BUY);
                        long pendingSellOrders = PendingOrders(firmKey, Order.OrderType.SELL);
                        double x = ComputeNewPrice(oldPrice.Item2, pendingBuyOrders, pendingSellOrders);
                        Tuple<int, double> newPrice = new Tuple<int, double>(oldPrice.Item1, x);
                        Console.WriteLine("Update {0} from {1} to {2}.", firmKey, oldPrice.Item2, newPrice.Item2);
                        stockPrices[firmKey] = newPrice;
                    }
                }

                tx.Commit();
            }
        }

        static private long PendingOrders(string stockName, Order.OrderType orderType)
        {
            using (XcoTransaction tx = space.BeginTransaction())
            {
                long stocks = 0;
                foreach (string orderId in orders.Keys)
                {
                    Order order = orders[orderId];
                    if (order.ShareName == stockName && order.Status == Order.OrderStatus.OPEN && order.Type == orderType)
                    {
                        stocks += order.NoOfOpenShares;
                    }
                }

                tx.Commit();
                return stocks;
            }
        }

        static double ComputeNewPrice(double oldPrice, long pendingBuyOrders, long pendingSellOrders)
        {
            double d = Math.Max(1, pendingBuyOrders + pendingSellOrders);
            double n = (double)(pendingBuyOrders - pendingSellOrders);
            double x = (1 + ((n/d) * (1.0 / 16.0)));

            return Math.Max(1, oldPrice * x);
        }
    }
}
