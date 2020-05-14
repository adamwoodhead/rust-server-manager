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

        [JsonProperty]
        public SetupViewModel SetupViewModel { get; set; } = new SetupViewModel();

        public ICommand CommandGameservers { get; set; }

        public ICommand CommandConfiguration { get; set; }

        public ICommand CommandSettings { get; set; }

        public ICommand CommandExit { get; set; }

        public ICommand CommandTest { get; set; }

        public MainWindowViewModel()
        {
            CommandGameservers      = new CommandImplementation(o => Gameservers());
            CommandConfiguration    = new CommandImplementation(o => Configuration());
            CommandSettings         = new CommandImplementation(o => Setup());
            CommandExit             = new CommandImplementation(o => Exit());
            CommandTest             = new CommandImplementation(o => Test());

            Gameservers();
        }

        private void Exit()
        {
            App.MainWindowInstance.Close();
        }

        private void Test()
        {
            Models.WebRcon.RconService rcon = new Models.WebRcon.RconService();

            rcon.Connect("51.68.204.234:28016", "testing7539");

            rcon.GetPlayers();
        }

        void Gameservers()
        {
            CurrentContent = new Views.GameserversUserControl(GamserversViewModel);
        }

        void Configuration()
        {
            CurrentContent = new Views.ConfigurationUserControl(ConfigurationViewModel);
        }

        void Setup()
        {
            CurrentContent = new Views.SetupUserControl(SetupViewModel);
        }
    }
}
