using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using YoV.Models;
using YoV.Views;
using YoV.ViewModels;

namespace YoV.Views
{
    public partial class RosterPage : ContentPage
    {
        RosterViewModel viewModel;

        public RosterPage()
        {
            InitializeComponent();

            BindingContext = viewModel = new RosterViewModel();
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