using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using RustServerManager.Models;
using RustServerManager.Utility;
using RustServerManager.ViewModels;
using RustServerManager.Views;

namespace RustServerManager
{
    public partial class App : Application
    {
        internal static bool MemoryWasGenerated { get; set; } = false;

        internal static MainWindow MainWindowInstance { get; set; }

        internal static Memory Memory { get; set; }

        internal static Authentication Authentication { get; set; }

        internal static string Version = "1.0";

        internal static string ServersDirectory { get => Path.Combine(App.Memory.Configuration.WorkingDirectory, "rustservers"); }
        
        [STAThread]
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            Utility.API.Initialize();

            LoginWindow.ShowAndCheckAuthentication();

            if (Authentication == null)
            {
                Environment.Exit(0);
            }

            Memory.Load();
            
            if (string.IsNullOrEmpty(Memory.Configuration.WorkingDirectory))
            {
                MessageBox.Show("Rust Server Manager requires a working directory, try again when you're ready to select one.");
                Environment.Exit(0);
            }

            CreateDirectories();

            // Initialize MainWindow
            this.MainWindow = MainWindowInstance = new MainWindow
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            this.ShutdownMode = ShutdownMode.OnMainWindowClose;

            this.Exit += delegate { PreExit(); };

            this.Dispatcher.UnhandledException += OnDispatcherUnhandledException;

            MainWindowInstance.ShowDialog();
        }

        private void CreateDirectories()
        {
            Directory.CreateDirectory(App.Memory.Configuration.WorkingDirectory);
            Directory.CreateDirectory(Path.Combine(App.Memory.Configuration.WorkingDirectory, "steamcmd"));
            Directory.CreateDirectory(ServersDirectory);
        }

        private void PreExit()
        {
            Console.WriteLine("Begin Shutdown...");

            Console.WriteLine("Attempting to close ViewModel Tasks..");
            foreach (GameserverViewModel gameserverViewModel in MainWindowInstance.ViewModel.GamserversViewModel.Gameservers)
            {
                gameserverViewModel?.CancelTasks();
            }

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
                MessageBox.Show($"Application Error (please report!){Environment.NewLine}" +
                    $"Target: {exception.TargetSite.Name}{Environment.NewLine}" +
                    $"Trace: {exception.StackTrace}", "Application Error", MessageBoxButton.OK);
            }

            e.Handled = false;
        }
    }
}
