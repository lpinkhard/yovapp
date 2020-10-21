using System;
using System.ComponentModel;
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

            LoginCommand = new Command(OnLogin);
        }

        public async void OnLogin()
        {
            if (await xmpp.LoginAsync(username, password))
            {
                await navigation.PopModalAsync();
            }
            else
            {
                DisplayInvalidLoginPrompt();
            }
        }
    }
}