using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using YoV.Services;
using YoV.Views;

namespace YoV
{
    public partial class App : Application
    {

        public App()
        {
            InitializeComponent();

            DependencyService.Register<XMPPService>();
            MainPage = new MainPage();
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
