using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            Process process = new Process() {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "SteamCMDWrapper",
                    Arguments = @"D:\RSM\Test",
                    UseShellExecute = false,
                    //CreateNoWindow = true,
                    //WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                },
                EnableRaisingEvents = true
            };

            process.OutputDataReceived += (s, e) => { Console.WriteLine(e.Data); };

            process.Start();

            process.BeginOutputReadLine();
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
