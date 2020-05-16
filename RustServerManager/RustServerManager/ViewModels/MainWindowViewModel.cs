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

        [JsonProperty]
        public GameserversViewModel GamserversViewModel { get; set; } = new GameserversViewModel(App.Memory.Gameservers);

        [JsonProperty]
        public ConfigurationViewModel ConfigurationViewModel { get; set; } = new ConfigurationViewModel(App.Memory.Configuration);

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
            //Task.Run(async () =>
            //{
            //    App.Memory.Gameservers[0].OpenPorts();

            //    await Task.Delay(5000);

            //    App.Memory.Gameservers[0].ClosePorts();
            //});


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
