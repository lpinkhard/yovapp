using System;
using System.ComponentModel;
using System.Diagnostics;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using YoV.Models;
using YoV.ViewModels;

namespace YoV.Views
{
    public partial class ChatPage : ContentPage
    {
        ChatViewModel viewModel;

        public ChatPage(ChatViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = this.viewModel = viewModel;

            Device.StartTimer(TimeSpan.FromSeconds(1.0f), () =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    viewModel.LoadMessagesCommand.Execute(null);
                });
                return true;
            });
        }

        public ChatPage()
        {
            InitializeComponent();

            var contact = new Contact
            {
                DisplayName = "Display Name",
                Username = "Username"
            };

            viewModel = new ChatViewModel(contact);
            BindingContext = viewModel;
        }
    }
}