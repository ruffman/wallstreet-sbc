﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedFeatures;
using SharedFeatures.Model;
using GalaSoft.MvvmLight;

namespace Investor.Model
{
    public interface IDataService : IDisposable
    {
        void Login(Registration r);

        void PlaceOrder(Order order);

        void CancelOrder(Order order);

        void SetSpace(string space);

        IEnumerable<string> ListOfSpaces();

        InvestorDepot LoadInvestorInformation();

        IEnumerable<ShareInformation> LoadMarketInformation();

        IEnumerable<Order> LoadPendingOrders();

        void AddNewMarketInformationAvailableCallback(Action callback);

        void AddNewPendingOrdersCallback(Action callback);

        void AddNewInvestorInformationAvailableCallback(Action callback);

        void RemoveNewInvestorInformationAvailableCallback(Action callback);
    }
}
