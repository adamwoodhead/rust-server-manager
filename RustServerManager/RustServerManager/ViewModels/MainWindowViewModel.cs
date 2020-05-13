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

        public ICommand CommandView { get; set; }

        public ICommand CommandConfiguration { get; set; }

        public ICommand CommandSettings { get; set; }

        public MainWindowViewModel()
        {
            CommandView             = new CommandImplementation(o => View());
            CommandConfiguration    = new CommandImplementation(o => Configuration());
            CommandSettings         = new CommandImplementation(o => Setup());

            View();
        }

        void View()
        {
            CurrentContent = new Views.ViewUserControl(GamserversViewModel);
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
