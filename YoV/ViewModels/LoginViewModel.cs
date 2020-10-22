﻿using System;
using System.ComponentModel;
using System.Threading;
using System.Windows.Input;
using Xamarin.Forms;
using YoV.Services;

namespace YoV.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        public Action DisplayInvalidLoginPrompt;
        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        private string username;
        private string password;
        private INavigation navigation;
        private XMPPService xmpp;

        private bool IsBusy { get; set; }

        public string Username
        {
            get { return username; }
            set
            {
                username = value;
                PropertyChanged(this, new PropertyChangedEventArgs("Username"));
            }
        }

        public string Password
        {
            get { return password; }
            set
            {
                password = value;
                PropertyChanged(this, new PropertyChangedEventArgs("Password"));
            }
        }

        public ICommand LoginCommand { protected set; get; }

        public LoginViewModel(INavigation navigation)
        {
            this.navigation = navigation;
            this.xmpp = DependencyService.Get<XMPPService>();

            IsBusy = false;
            LoginCommand = new Command(OnLogin);
        }

        public void OnLogin()
        {
            if (!IsBusy)
            {
                IsBusy = true;
                Thread loginThread = new Thread(() => LoginThread(username, password));
                loginThread.Start();
            }
        }

        private void LoginThread(string username, string password)
        {
            xmpp.Login(username, password, OnLoginResult);
        }

        public bool OnLoginResult(bool success)
        {
            if (success)
            {
                navigation.PopModalAsync();
            }
            else
            {
                DisplayInvalidLoginPrompt();
            }

            IsBusy = false;

            return true;
        }
    }
}