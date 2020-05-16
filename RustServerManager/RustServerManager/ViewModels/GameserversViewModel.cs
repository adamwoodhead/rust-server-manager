using RustServerManager.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace RustServerManager.ViewModels
{
    public class GameserversViewModel : ObservableViewModel
    {
        private ObservableCollection<GameserverViewModel> _gameservers = new ObservableCollection<GameserverViewModel>();

        public ObservableCollection<GameserverViewModel> Gameservers
        {
            get => _gameservers;
            set
            {
                if (_gameservers != value)
                {
                    _gameservers = value;
                    OnPropertyChanged(nameof(Gameservers));
                }
            }
        }

        public ICommand CreateCommand { get; set; }

        public ICommand UpdateCommand { get; set; }

        public GameserversViewModel(List<Gameserver> gameservers)
        {
            foreach (Gameserver gameserver in gameservers)
            {
                Gameservers.Add(new GameserverViewModel(gameserver));
            }

            CreateCommand = new CommandImplementation(o => CreateGameserver());
            UpdateCommand = new CommandImplementation(o => UpdateAllGameservers());
        }

        private void CreateGameserver()
        {
            Gameserver gameserver = new Gameserver(true);
            App.Memory.Gameservers.Add(gameserver);

            Gameservers.Add(new GameserverViewModel(gameserver));

            App.Memory.Save();
        }

        private void UpdateAllGameservers()
        {
            throw new NotImplementedException();
        }
    }
}
