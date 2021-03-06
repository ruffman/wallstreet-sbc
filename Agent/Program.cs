using SharedFeatures.Model;
using System;
using System.Threading;
using System.Reactive.Linq;
using XcoSpaces;
using XcoSpaces.Collections;
using XcoSpaces.Exceptions;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace Agent
{
    class Program
    {
        static private IObservable<long> timer;
        static int counter = 0;
        static private XcoSpace space;
        static private IList<Tuple<XcoList<ShareInformation>, XcoQueue<string>, XcoList<Order>, XcoList<FundDepot>>> spaces;

        static bool exitSystem = false;

        #region Trap application termination
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        private delegate bool EventHandler(CtrlType sig);
        static EventHandler _handler;

        enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        private static bool Handler(CtrlType sig)
        {
            Console.WriteLine("Exiting system due to external CTRL-C, or process kill, or shutdown");

            space.Dispose();

            Console.WriteLine("Cleanup complete");

            //allow main to run off
            exitSystem = true;

            //shutdown right away so there are no lingering threads
            Environment.Exit(-1);

            return true;
        }
        #endregion

        static void Main(string[] args)
        {


            _handler += new EventHandler(Handler);
            SetConsoleCtrlHandler(_handler, true);

            IList<Uri> urls = new List<Uri>();
            spaces = new List<Tuple<XcoList<ShareInformation>, XcoQueue<string>, XcoList<Order>, XcoList<FundDepot>>>();

            string input = null;

            Console.WriteLine("Please enter the URIs of all space servers: ");

            do
            {

                input = null;
                Console.Write("URI (or press enter to continue): ");
                input = Console.ReadLine();
                if (input.Length > 0)
                {
                    urls.Add(new Uri(input));
                }

            } while (input.Length > 0);

            try
            {
                space = new XcoSpace(0);

                foreach (Uri spaceServer in urls)
                {

                    XcoList<ShareInformation> stockPrices = space.Get<XcoList<ShareInformation>>("StockInformation", spaceServer);
                    XcoQueue<string> stockPricesUpdates = space.Get<XcoQueue<string>>("StockInformationUpdates", spaceServer);
                    XcoList<Order> orders = space.Get<XcoList<Order>>("Orders", spaceServer);
                    XcoList<FundDepot> fundDepots = space.Get<XcoList<FundDepot>>("FundDepots", spaceServer);

                    Tuple<XcoList<ShareInformation>, XcoQueue<string>, XcoList<Order>, XcoList<FundDepot>> t = new Tuple<XcoList<ShareInformation>, XcoQueue<string>, XcoList<Order>, XcoList<FundDepot>>(stockPrices, stockPricesUpdates, orders, fundDepots);
                    spaces.Add(t);
                }

                if (args.Length > 0 && args[0].Equals("-Manual"))
                {
                    //Console.WriteLine("Type \"list\" to list all shares and set the price by typing <sharename> <price>");
                    //while (true)
                    //{
                    //    input = Console.ReadLine();
                    //    if (input.Equals("list"))
                    //    {
                    //        for (int i = 0; i < stockPrices.Count; i++)
                    //        {
                    //            ShareInformation s = stockPrices[i];
                    //            Console.WriteLine(s.FirmName + "\t" + s.PricePerShare);
                    //        }
                    //    }
                    //    else
                    //    {
                    //        var info = input.Split(' ');
                    //        var stock = Utils.FindElement(stockPrices, info[0], "FirmName");
                    //        ShareInformation s = new ShareInformation()
                    //        {
                    //            FirmName = info[0],
                    //            NoOfShares = stock.NoOfShares,
                    //            PricePerShare = Double.Parse(input.Split(' ')[1])
                    //        };
                    //        Utils.ReplaceElement(stockPrices, s, "FirmName");
                    //    }
                    //}
                }
                else
                {

                    timer = Observable.Interval(TimeSpan.FromSeconds(2));
                    timer.Subscribe(_ => UpdateStockPrices());
                    Thread.Sleep(1000);

                    while (true)
                    {
                        Thread.Sleep(1000);
                    }
                }
            }
            catch (XcoException)
            {
                Console.WriteLine("Unable to reach server.\nPress enter to exit.");
                Console.ReadLine();
                if (space != null && space.IsOpen) { space.Close(); }
            }
        }

        static void UpdateStockPrices()
        {
            using (XcoTransaction tx = space.BeginTransaction())
            {
                Console.WriteLine("UPDATE stock prices: " + DateTime.Now);

                try
                {

                    Dictionary<string, ShareInformation> shareStore = new Dictionary<string, ShareInformation>();

                    foreach (Tuple<XcoList<ShareInformation>, XcoQueue<string>, XcoList<Order>, XcoList<FundDepot>> t in spaces)
                    {

                        XcoList<ShareInformation> stockPrices = t.Item1;
                        XcoQueue<string> stockPricesUpdates = t.Item2;
                        XcoList<Order> orders = t.Item3;
                        XcoList<FundDepot> fundDepots = t.Item4;


                        for (int i = 0; i < stockPrices.Count; i++)
                        {
                            ShareInformation oldPrice = stockPrices[i, true];

                            if (!oldPrice.isFund)
                            {
                                long pendingBuyOrders = PendingOrders(orders, oldPrice.FirmName, Order.OrderType.BUY);
                                long pendingSellOrders = PendingOrders(orders, oldPrice.FirmName, Order.OrderType.SELL);
                                double x = ComputeNewPrice(oldPrice.PricePerShare, pendingBuyOrders, pendingSellOrders);
                                ShareInformation newPrice = new ShareInformation()
                                {
                                    FirmName = oldPrice.FirmName,
                                    NoOfShares = oldPrice.NoOfShares,
                                    PricePerShare = x,
                                    isFund = false
                                };
                                Console.WriteLine("Update {0} from {1} to {2}.", newPrice.FirmName, oldPrice.PricePerShare, newPrice.PricePerShare);

                                Utils.ReplaceElement(stockPrices, newPrice, "FirmName");
                                stockPricesUpdates.Enqueue(newPrice.FirmName, true);

                                shareStore.Add(newPrice.FirmName, newPrice);
                            }
                        }

                        RandomlyUpdateASingleStock(stockPrices, stockPricesUpdates);

                    }

                    foreach (Tuple<XcoList<ShareInformation>, XcoQueue<string>, XcoList<Order>, XcoList<FundDepot>> t in spaces)
                    {

                        XcoList<ShareInformation> stockPrices = t.Item1;
                        XcoQueue<string> stockPricesUpdates = t.Item2;
                        XcoList<Order> orders = t.Item3;
                        XcoList<FundDepot> fundDepots = t.Item4;


                        for (int i = 0; i < stockPrices.Count; i++)
                        {
                            ShareInformation oldPrice = stockPrices[i, true];

                            if (oldPrice.isFund)
                            {
                                FundDepot depot = Utils.FindElement(fundDepots, oldPrice.FirmName, "FundID");
                                double shareValue = ValueOfOwnedShares(shareStore, depot);
                                ShareInformation newPrice = new ShareInformation()
                                {
                                    FirmName = oldPrice.FirmName,
                                    NoOfShares = oldPrice.NoOfShares,
                                    PricePerShare = (shareValue + depot.FundBank) / depot.FundShares,
                                    isFund = true
                                };

                                if (!oldPrice.PricePerShare.Equals(newPrice.PricePerShare))
                                {
                                    Console.WriteLine("Update {0} from {1} to {2}.", newPrice.FirmName, oldPrice.PricePerShare, newPrice.PricePerShare);
                                    Utils.ReplaceElement(stockPrices, newPrice, "FirmName");
                                    stockPricesUpdates.Enqueue(newPrice.FirmName, true);
                                }

                            }
                        }
                    }

                    tx.Commit();

                }

                catch (XcoException e)
                {
                    Console.WriteLine("Could not update stock due to: " + e.StackTrace);
                    tx.Rollback();
                }
            }
        }

        private static double ValueOfOwnedShares(Dictionary<string, ShareInformation> shareInformation, FundDepot fundDepot)
        {
            double value = 0.0;

            foreach (string share in fundDepot.Shares.Keys)
            {
                if (shareInformation.ContainsKey(share))
                {
                    value += shareInformation[share].PricePerShare * fundDepot.Shares[share];
                }
            }

            return value;
        }

        private static void RandomlyUpdateASingleStock(XcoList<ShareInformation> stockPrices, XcoQueue<string> stockPricesUpdates)
        {
            counter++;
            if (stockPrices.Count > 0 && counter % 3 == 0)
            {
                counter = 0;
                Random rrd = new Random();
                ShareInformation oldPrice = stockPrices[rrd.Next(0, stockPrices.Count - 1), true];


                if (!oldPrice.isFund)
                {
                    Console.WriteLine("UPDATE stock price {0} randomly: {1}", oldPrice.FirmName, DateTime.Now);

                    double x = Math.Max(1, oldPrice.PricePerShare * (1 + (rrd.Next(-3, 3) / 100.0)));
                    ShareInformation newPrice = new ShareInformation()
                    {
                        FirmName = oldPrice.FirmName,
                        NoOfShares = oldPrice.NoOfShares,
                        PricePerShare = x
                    };
                    Console.WriteLine("Update {0} from {1} to {2}.", newPrice.FirmName, oldPrice.PricePerShare, newPrice.PricePerShare);

                    Utils.ReplaceElement(stockPrices, newPrice, "FirmName");
                    stockPricesUpdates.Enqueue(newPrice.FirmName);
                }
            }
        }

        static private long PendingOrders(XcoList<Order> orders, string stockName, Order.OrderType orderType)
        {
            long stocks = 0;

            for (int i = 0; i < orders.Count; i++)
            {
                Order order = orders[i];
                if (order.ShareName == stockName && (order.Status == Order.OrderStatus.OPEN || order.Status == Order.OrderStatus.PARTIAL) && order.Type == orderType)
                {
                    stocks += order.NoOfOpenShares;
                }
            }

            return stocks;
        }

        static double ComputeNewPrice(double oldPrice, long pendingBuyOrders, long pendingSellOrders)
        {
            double d = Math.Max(1, pendingBuyOrders + pendingSellOrders);
            double n = (double)(pendingBuyOrders - pendingSellOrders);
            double x = (1 + ((n / d) * (1.0 / 16.0)));

            return Math.Max(1, oldPrice * x);
        }
    }
}
