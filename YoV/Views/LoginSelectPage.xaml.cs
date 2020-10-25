using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using YoV.ViewModels;

namespace YoV.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LoginSelectPage : ContentPage
    {
        public LoginSelectPage()
        {
            var vm = new LoginSelectViewModel(Navigation);
            this.BindingContext = vm;
            InitializeComponent();
        }
    }
}