using System.ComponentModel;
using System.Windows.Input;
using Xamarin.Forms;
using YoV.Views;

namespace YoV.ViewModels
{
    public class LoginSelectViewModel : INotifyPropertyChanged
    {
        private INavigation navigation;

        public event PropertyChangedEventHandler PropertyChanged;

        private bool IsBusy { get; set; }

        public ICommand LoginCommand { protected set; get; }
        public ICommand RegisterCommand { protected set; get; }

        public LoginSelectViewModel(INavigation navigation)
        {
            this.navigation = navigation;

            IsBusy = false;
            LoginCommand = new Command(OnLogin);
            RegisterCommand = new Command(OnRegister);
        }

        public void OnLogin()
        {
            if (!IsBusy)
            {
                IsBusy = true;
                Device.BeginInvokeOnMainThread(() =>
                {
                    navigation.PushModalAsync(new LoginPage());
                });
            }
        }

        public void OnRegister()
        {
            if (!IsBusy)
            {
                IsBusy = true;
                Device.BeginInvokeOnMainThread(() =>
                {
                    navigation.PushModalAsync(new RegisterPage());
                });
            }
        }
    }
}