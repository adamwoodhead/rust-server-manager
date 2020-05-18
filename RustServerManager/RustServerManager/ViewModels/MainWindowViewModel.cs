using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace RustServerManager.ViewModels
{
    [JsonObject]
    public class MainWindowViewModel : ObservableViewModel
    {
        private UserControl _currentContent;

        public UserControl CurrentContent
        {
            get => _currentContent;
            set
            {
                _currentContent = value;
                OnPropertyChanged(nameof(CurrentContent));
            }
        }
        
        public GameserversViewModel GamserversViewModel { get; set; } = new GameserversViewModel(App.Memory.Gameservers);

        public ConfigurationViewModel ConfigurationViewModel { get; set; } = new ConfigurationViewModel(App.Memory.Configuration);

        public int SavedHeight
        {
            get => App.Memory.Configuration.SavedHeight;
            set
            {
                if (App.Memory.Configuration.SavedHeight != value)
                {
                    App.Memory.Configuration.SavedHeight = value;
                    OnPropertyChanged(nameof(SavedHeight));
                }
            }
        }

        public int SavedWidth
        {
            get => App.Memory.Configuration.SavedWidth;
            set
            {
                if (App.Memory.Configuration.SavedWidth != value)
                {
                    App.Memory.Configuration.SavedWidth = value;
                    OnPropertyChanged(nameof(SavedWidth));
                }
            }
        }

        public ICommand CommandGameservers { get; set; }

        public ICommand CommandConfiguration { get; set; }

        public ICommand CommandSettings { get; set; }

        public ICommand CommandExit { get; set; }

        public ICommand CommandTest { get; set; }

        public MainWindowViewModel()
        {
            CommandGameservers = new CommandImplementation(o => Gameservers());
            CommandConfiguration = new CommandImplementation(o => Configuration());
            //CommandSettings         = new CommandImplementation(o => Setup());
            CommandExit = new CommandImplementation(o => Exit());
            CommandTest = new CommandImplementation(o => Test());

            Gameservers();
        }

        private void Exit()
        {
            App.MainWindowInstance.Close();
        }

        private void Test()
        {
            Task.Run(async () =>
            {
                Progress<double> p1 = new Progress<double>();

                p1.ProgressChanged += (s, e) => { Console.WriteLine("SteamCMD Progress: " + string.Format("{0:00.00%}", e / 100)); };

                await Models.SteamCMD.TestDownloadRust(App.Memory.Gameservers[0], p1);
            });
        }

        void Gameservers()
        {
            CurrentContent = new Views.GameserversUserControl(GamserversViewModel);
        }

        void Configuration()
        {
            CurrentContent = new Views.ConfigurationUserControl(ConfigurationViewModel);
        }
    }
}
