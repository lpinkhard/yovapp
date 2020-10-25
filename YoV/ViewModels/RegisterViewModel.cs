using System;
using System.ComponentModel;
using System.Threading;
using System.Windows.Input;
using Xamarin.Forms;
using YoV.Services;

namespace YoV.ViewModels
{
    public class RegisterViewModel : INotifyPropertyChanged
    {
        public Action DisplayInvalidDetailsPrompt;
        public Action DisplayPasswordMismatchPrompt;
        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        private string phone;
        private string password;
        private string confirm;
        private INavigation navigation;
        private XMPPService xmpp;

        private bool IsBusy { get; set; }

        public string PhoneNumber
        {
            get { return phone; }
            set
            {
                phone = value;
                PropertyChanged(this, new PropertyChangedEventArgs("PhoneNumber"));
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

        public string ConfirmPassword
        {
            get { return confirm; }
            set
            {
                confirm = value;
                PropertyChanged(this, new PropertyChangedEventArgs("ConfirmPassword"));
            }
        }

        public ICommand RegisterCommand { protected set; get; }

        public RegisterViewModel(INavigation navigation)
        {
            this.navigation = navigation;
            this.xmpp = DependencyService.Get<XMPPService>();

            IsBusy = false;
            RegisterCommand = new Command(OnRegister);
        }

        public void OnRegister()
        {
            if (password == confirm)
            {
                if (!IsBusy)
                {
                    IsBusy = true;
                    Thread registerThread = new Thread(() => RegisterThread(phone, password));
                    registerThread.Start();
                }
            }
            else
            {
                DisplayPasswordMismatchPrompt();
            }
        }

        private void RegisterThread(string username, string password)
        {
            xmpp.Register(username, password, OnRegisterResult);
        }

        public bool OnRegisterResult(bool success)
        {
            if (success)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    navigation.PopModalAsync();
                    navigation.PopModalAsync();
                });
            }
            else
            {
                DisplayInvalidDetailsPrompt();
            }

            IsBusy = false;

            return true;
        }
    }
}