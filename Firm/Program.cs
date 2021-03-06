﻿using SharedFeatures.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XcoSpaces;
using XcoSpaces.Collections;
using XcoSpaces.Exceptions;

namespace Firm
{
    class Program
    {
        static void Main(string[] args)
        {

            Console.Write("Please enter a URI of a space server: ");
            Uri spaceServer = new Uri(Console.ReadLine());

            using (XcoSpace space = new XcoSpace(0))
            {
                string name;
                int shares;
                double pricePerShare;
                if (args.Count() == 3 && Int32.TryParse(args[1], out shares) && Double.TryParse(args[2], out pricePerShare))
                {
                    name = args[0];
                    try
                    {
                        XcoQueue<Request> q = space.Get<XcoQueue<Request>>("RequestQ", spaceServer);
                        q.Enqueue(new Request() { FirmName = name, Shares = shares, PricePerShare = pricePerShare });
                    }
                    catch (XcoException)
                    {
                        Console.WriteLine("Unable to reach server.\nPress enter to exit.");
                        Console.ReadLine();
                    }
                }
                else
                {
                    Console.Error.WriteLine("Enter a firmname, the number of shares and the price per share.\nPress enter to exit.");
                    name = Console.ReadLine();
                    Int32.TryParse(Console.ReadLine(), out shares);
                    Double.TryParse(Console.ReadLine(), out pricePerShare);
                    Console.ReadLine();

                    try
                    {
                        XcoQueue<Request> q = space.Get<XcoQueue<Request>>("RequestQ", spaceServer);
                        q.Enqueue(new Request() { FirmName = name, Shares = shares, PricePerShare = pricePerShare });
                    }
                    catch (XcoException)
                    {
                        Console.WriteLine("Unable to reach server.\nPress enter to exit.");
                        Console.ReadLine();
                    }
                }
            }
        }
    }
}
