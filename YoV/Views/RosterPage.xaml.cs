using System;
using Xamarin.Forms;

using YoV.Models;
using YoV.ViewModels;
using YoV.Services;
using System.Diagnostics;

namespace YoV.Views
{
    public partial class RosterPage : ContentPage
    {
        RosterViewModel viewModel;

        public RosterPage()
        {
            InitializeComponent();

            BindingContext = viewModel = new RosterViewModel();
            viewModel.DisplayMaxContacts += () => DisplayAlert("Error",
                "Maximum contacts reached", "OK");

            INotificationManager notificationManager =
                DependencyService.Get<INotificationManager>();
            notificationManager.NotificationReceived += OnReceiveNotification;

            Device.StartTimer(TimeSpan.FromSeconds(1.0f), () =>
            {
                if (viewModel.RosterLoaded)
                {
                    return false;
                }
                else
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        viewModel.LoadRosterCommand.Execute(null);
                    });
                    return true;
                }
            });
        }

        private void OnReceiveNotification(object sender, EventArgs e)
        {
            NotificationEventArgs notification = (NotificationEventArgs)e;
            Device.BeginInvokeOnMainThread(() =>
               {
                   viewModel.UpdateContactState(notification);
               }
            );
        }

        async void OnItemSelected(object sender, EventArgs args)
        {
            var layout = (BindableObject)sender;
            var contact = (Contact)layout.BindingContext;
            await Navigation.PushAsync(new ChatPage(new ChatViewModel(contact)));
        }

        async void AddContact_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(new NavigationPage(new NewContactPage()));
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
        }
    }
}