using RustServerManager.Utility;
using RustServerManager.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace RustServerManager.ViewModels
{
    public class LoginViewModel : ObservableViewModel
    {
        private string _username;
        private string _password;

        internal LoginWindow LoginWindow { get; private set; }

        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged(nameof(Username));
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged(nameof(Password));
            }
        }

        public ICommand LoginCommand { get; set; }

        public ICommand CancelCommand { get; set; }

        public ICommand RegisterCommand { get; set; }

        public LoginViewModel(LoginWindow loginWindow)
        {
            LoginWindow = loginWindow;
            LoginCommand = new CommandImplementation(o => TryLogin());
            CancelCommand = new CommandImplementation(o => Cancel());
            RegisterCommand = new CommandImplementation(o => Register());
        }

        private async void Register()
        {
            APIResponse response = await Utility.API.CreateUserAsync(Username, LoginWindow.PasswordBox.Password.SHA256());

            if (!response.IsError)
            {
                App.Authentication = new Utility.Authentication()
                {
                    Username = Username,
                    Password = LoginWindow.PasswordBox.Password.SHA256()
                };
                LoginWindow.Close();
            }
            else
            {
                MessageBox.Show(response.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel()
        {
            App.Authentication = null;

            LoginWindow.Close();
        }

        private async void TryLogin()
        {
            APIResponse response = await Utility.API.AuthenticateUserAsync(Username, LoginWindow.PasswordBox.Password.SHA256());

            if (!response.IsError)
            {
                App.Authentication = new Utility.Authentication()
                {
                    Username = Username,
                    Password = LoginWindow.PasswordBox.Password.SHA256()
                };
                LoginWindow.Close();
            }
            else
            {
                MessageBox.Show(response.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
