﻿using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Fondsmanager.Model;
using Fondsmanager.View;
using SharedFeatures.Model;
using System.Collections;
using System.Collections.Generic;

namespace Fondsmanager.ViewModel
{

    public class LoginViewModel : ViewModelBase
    {
        private IDataService data;
        private bool submitted;
        private int loginCounter;

        public LoginViewModel(IDataService data)
        {
            this.data = data;
            data.AddNewInvestorInformationAvailableCallback(OnRegistrationConfirmed);
            SubmitCommand = new RelayCommand(Submit, () => !FundID.Equals(string.Empty) && FundAssests >= 0 && FundShares >= 0 && !submitted);
            FundID = string.Empty;
            FundAssests = 0;
            FundShares = 0;
            ButtonText = "Submit";
            submitted = false;
        }

        private string fundid;
        public string FundID
        {
            get
            {
                return fundid;
            }
            set
            {
                fundid = value;
                RaisePropertyChanged(() => FundID);
                SubmitCommand.RaiseCanExecuteChanged();
            }
        }

        private double fundassets;
        public double FundAssests
        {
            get
            {
                return fundassets;
            }
            set
            {
                fundassets = value;
                RaisePropertyChanged(() => FundAssests);
                SubmitCommand.RaiseCanExecuteChanged();
            }
        }

        private int fundshares;

        public int FundShares
        {
            get
            {
                return fundshares;
            }
            set
            {
                fundshares = value;
                RaisePropertyChanged(() => FundShares);
                SubmitCommand.RaiseCanExecuteChanged();
            }
        }

        private string buttonText;
        public string ButtonText
        {
            get
            {
                return buttonText;
            }
            set
            {
                buttonText = value;
                RaisePropertyChanged(() => ButtonText);
                SubmitCommand.RaiseCanExecuteChanged();
            }
        }

        public RelayCommand SubmitCommand { get; private set; }

        public void Submit()
        {
            data.Login(new FundRegistration() { FundID = FundID, FundAssets = FundAssests, FundShares = FundShares });
            ButtonText = "Waiting for confirmation ...";
            submitted = true;
            SubmitCommand.RaiseCanExecuteChanged();
        }

        public void OnRegistrationConfirmed()
        {
            loginCounter += 1;

            if (loginCounter != (new List<string>(data.ListOfSpaces())).Count)
            {
                return;
            }

            Messenger.Default.Send<NotificationMessage>(new NotificationMessage(this, "Close"));
            var MainWindow = new MainWindow();
            MainWindow.Show();
            data.RemoveNewInvestorInformationAvailableCallback(OnRegistrationConfirmed);
            this.Cleanup();
        }
    }
}