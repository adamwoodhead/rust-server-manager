using RustServerManager.ViewModels;
using System;
using System.Windows;

namespace RustServerManager.Views
{
    public partial class MainWindow : Window
    {
        public MainWindowViewModel ViewModel { get; set; }

        public MainWindow()
        {
            ViewModel = new MainWindowViewModel();
            this.DataContext = ViewModel;
            InitializeComponent();
        }
    }
}
