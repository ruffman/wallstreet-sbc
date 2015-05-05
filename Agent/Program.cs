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
        static private IObservable<long> timer1;
        static private IObservable<long> timer2;

        static void Main(string[] args)
        {
            try
            {
                
                timer1 = Observable.Interval(TimeSpan.FromSeconds(2));
                timer1.Subscribe(_ => UpdateStockPrices());
                Thread.Sleep(1000);
                timer2 = Observable.Interval(TimeSpan.FromSeconds(6));
                timer2.Subscribe(_ => UpdateStockPriceRandomly());

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
            using (XcoSpace space = new XcoSpace(0))
            {
                XcoDictionary<string, Tuple<int, double>> stockPrices = space.Get<XcoDictionary<string, Tuple<int, double>>>("StockInformation", new Uri("xco://" + Environment.MachineName + ":" + 9000));
                XcoDictionary<string, FirmDepot> firmDepots = space.Get<XcoDictionary<string, FirmDepot>>("FirmDepots", new Uri("xco://" + Environment.MachineName + ":" + 9000));
                XcoDictionary<string, Order> orders = space.Get<XcoDictionary<string, Order>>("Orders", new Uri("xco://" + Environment.MachineName + ":" + 9000));
                
                using (XcoTransaction tx = space.BeginTransaction())
                {
                    Console.WriteLine("UPDATE stock prices: " + DateTime.Now);

                    foreach (string firmKey in firmDepots.Keys)
                    {

                        if (stockPrices.ContainsKey(firmKey))
                        {
                            Tuple<int, double> oldPrice = stockPrices[firmKey];
                            long pendingBuyOrders = PendingOrders(space, orders, firmKey, Order.OrderType.BUY);
                            long pendingSellOrders = PendingOrders(space, orders, firmKey, Order.OrderType.SELL);
                            double x = ComputeNewPrice(oldPrice.Item2, pendingBuyOrders, pendingSellOrders);
                            Tuple<int, double> newPrice = new Tuple<int, double>(oldPrice.Item1, x);
                            Console.WriteLine("Update {0} from {1} to {2}.", firmKey, oldPrice.Item2, newPrice.Item2);
                            stockPrices[firmKey] = newPrice;
                        }
                    }

                    tx.Commit();
                }
            }
        }

        static private long PendingOrders(XcoSpace space, XcoDictionary<string, Order> orders, string stockName, Order.OrderType orderType)
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

        static void UpdateStockPriceRandomly()
        {
            using (XcoSpace space = new XcoSpace(0))
            {
                XcoDictionary<string, Tuple<int, double>> stockPrices = space.Get<XcoDictionary<string, Tuple<int, double>>>("StockInformation", new Uri("xco://" + Environment.MachineName + ":" + 9000));
                XcoDictionary<string, FirmDepot> firmDepots = space.Get<XcoDictionary<string, FirmDepot>>("FirmDepots", new Uri("xco://" + Environment.MachineName + ":" + 9000));
                XcoDictionary<string, Order> orders = space.Get<XcoDictionary<string, Order>>("Orders", new Uri("xco://" + Environment.MachineName + ":" + 9000));
                
                using (XcoTransaction tx = space.BeginTransaction())
                {
                    Console.WriteLine("UPDATE stock price randomly: " + DateTime.Now);

                    if (firmDepots.Keys.Length > 0)
                    {
                        Random rrd = new Random();
                        string firmKey = firmDepots.Keys[rrd.Next(0, firmDepots.Count - 1)];

                        Tuple<int, double> oldPrice = stockPrices[firmKey];
                        double x = oldPrice.Item2 * (1 + (rrd.Next(-3, 3) / 100.0));
                        Tuple<int, double> newPrice = new Tuple<int, double>(oldPrice.Item1, x);
                        Console.WriteLine("Update {0} randomly from {1} to {2}.", firmKey, oldPrice.Item2, newPrice.Item2);
                        stockPrices[firmKey] = newPrice;


                    }

                    tx.Commit();
                }
            }
        }
    }
}
