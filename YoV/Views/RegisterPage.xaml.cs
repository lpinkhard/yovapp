using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using YoV.ViewModels;

namespace YoV.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class RegisterPage : ContentPage
    {
        public RegisterPage()
        {
            var vm = new RegisterViewModel(Navigation);
            this.BindingContext = vm;

            vm.DisplayInvalidDetailsPrompt += () => DisplayAlert("Error", "Invalid details", "OK");
            vm.DisplayPasswordMismatchPrompt += () => DisplayAlert("Error", "Passwords do not match", "OK");

            InitializeComponent();

            PhoneNumber.Completed += (object sender, EventArgs e) =>
            {
                Password.Focus();
            };

            Password.Completed += (object sender, EventArgs e) =>
            {
                ConfirmPassword.Focus();
            };

            ConfirmPassword.Completed += (object sender, EventArgs e) =>
            {
                vm.RegisterCommand.Execute(null);
            };
        }
    }
}