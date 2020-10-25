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
        public ContactEntry ContactEntry { get; set; }

        public NewContactPage()
        {
            InitializeComponent();

            ContactEntry = new ContactEntry
            {
                Contact = new Contact
                {
                    DisplayName = "",
                    PhoneNumber = "",
                    NewMessages = false
                },
                CircleName = ""
            };

            BindingContext = this;
        }

        async void Save_Clicked(object sender, EventArgs e)
        {
            MessagingCenter.Send(this, "AddContact", ContactEntry);
            await Navigation.PopModalAsync();
        }

        async void Cancel_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }
}