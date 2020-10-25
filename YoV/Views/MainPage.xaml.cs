using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

using YoV.Models;
using YoV.Services;

namespace YoV.Views
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : MasterDetailPage
    {
        Dictionary<int, NavigationPage> MenuPages = new Dictionary<int, NavigationPage>();
        public MainPage()
        {
            InitializeComponent();

            MasterBehavior = MasterBehavior.Popover;

            MenuPages.Add((int)MenuItemType.Chat, (NavigationPage)Detail);

            string phone = Preferences.Get("phone", "");
            string password = Preferences.Get("password", "");

            if (phone.Length > 0 && password.Length > 0)
            {
                XMPPService xmpp = DependencyService.Get<XMPPService>();
                xmpp.Login(phone, password, OnLoginOutput);
            }
            else
            {
                Navigation.PushModalAsync(new LoginSelectPage());
            }
        }

        private bool OnLoginOutput(bool success)
        {
            if (!success)
            {
                Navigation.PushModalAsync(new LoginSelectPage());
            }
            return true;
        }

        public async Task NavigateFromMenu(int id)
        {
            if (!MenuPages.ContainsKey(id))
            {
                switch (id)
                {
                    case (int)MenuItemType.Chat:
                        MenuPages.Add(id, new NavigationPage(new RosterPage()));
                        break;
                    case (int)MenuItemType.About:
                        MenuPages.Add(id, new NavigationPage(new AboutPage()));
                        break;
                }
            }

            var newPage = MenuPages[id];

            if (newPage != null && Detail != newPage)
            {
                Detail = newPage;

                if (Device.RuntimePlatform == Device.Android)
                    await Task.Delay(100);

                IsPresented = false;
            }
        }
    }
}