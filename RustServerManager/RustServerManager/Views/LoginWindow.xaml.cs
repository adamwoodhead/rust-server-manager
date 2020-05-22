using RustServerManager.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RustServerManager.Views
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginViewModel ViewModel { get; set; }

        public LoginWindow()
        {
            ViewModel = new LoginViewModel(this);
            this.DataContext = ViewModel;
            InitializeComponent();
        }

        public static bool ShowAndCheckAuthentication()
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.ShowDialog();

            if (App.Authentication != null)
            {
                return true;
            }

            return false;
        }
    }
}
