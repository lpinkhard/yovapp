using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using YoV.Models;

namespace YoV.Views
{
    public partial class NewContactPage : ContentPage
    {
        public Contact Contact { get; set; }

        public NewContactPage()
        {
            InitializeComponent();

            Contact = new Contact
            {
                DisplayName = "",
                Username = "",
                NewMessages = false
            };

            BindingContext = this;
        }

        async void Save_Clicked(object sender, EventArgs e)
        {
            MessagingCenter.Send(this, "AddContact", Contact);
            await Navigation.PopModalAsync();
        }

        async void Cancel_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }
}