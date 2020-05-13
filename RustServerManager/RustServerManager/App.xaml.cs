using System;
using System.Threading.Tasks;
using System.Windows;
using RustServerManager.Models;
using RustServerManager.Views;

namespace RustServerManager
{
    public partial class App : Application
    {
        internal static MainWindow MainWindowInstance { get; set; }

        internal static Memory Memory { get; set; }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            Memory = Memory.Load();

            // Initialize MainWindow
            this.MainWindow = MainWindowInstance = new MainWindow
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            this.MainWindow.DataContext = new ViewModels.MainWindowViewModel();

            this.ShutdownMode = ShutdownMode.OnMainWindowClose;

            this.Exit += delegate{ PreExit(); };

            this.Dispatcher.UnhandledException += OnDispatcherUnhandledException;

            MainWindowInstance.ShowDialog();
        }

        private void PreExit()
        {
            Console.WriteLine("Begin Shutdown...");

            Console.WriteLine("Stopping Servers.");
            foreach (Gameserver gameserver in App.Memory.Gameservers)
            {
                gameserver.Kill();
            }

            Console.WriteLine("Saving Memory.");
            App.Memory.Save();

            Console.WriteLine("Shutdown.");
        }

        private static void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Exception exception = e.Exception;

            if (exception.GetType() == typeof(NotImplementedException))
            {
                MessageBox.Show("This feature is currently in development.", "Not Implemented", MessageBoxButton.OK);
            }
            else
            {
                MessageBox.Show($"Application Error (please report this on the git!){Environment.NewLine}" +
                    $"Target: {exception.TargetSite.Name}{Environment.NewLine}" +
                    $"Trace: {exception.StackTrace}", "Application Error", MessageBoxButton.OK);
            }

            e.Handled = false;
        }
    }
}
