using System;
using System.Collections.Generic;
using Xamarin.Forms;
using YoV.ViewModels;

namespace YoV.Views.Partials
{
    public partial class ChatInputView : ContentView
    {
        public ChatInputView()
        {
            InitializeComponent();

            if (Device.RuntimePlatform == Device.iOS)
            {
                this.SetBinding(HeightRequestProperty,
                    new Binding("Height", BindingMode.OneWay,
                    null, null, null, chatTextInput));
            }
        }

        public void OnSendMessage(object sender, EventArgs e)
        {
            chatTextInput.Focus();
        }
    }
}
