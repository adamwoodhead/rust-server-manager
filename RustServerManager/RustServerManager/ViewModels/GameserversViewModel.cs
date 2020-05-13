using RustServerManager.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RustServerManager.ViewModels
{
    public class GameserversViewModel : ObservableViewModel
    {
        public ObservableCollection<GameserverViewModel> Gameservers { get; set; } = new ObservableCollection<GameserverViewModel>();

        public ICommand CreateCommand { get; set; }

        public GameserversViewModel(List<Gameserver> gameservers)
        {
            foreach (Gameserver gameserver in gameservers)
            {
                Gameservers.Add(new GameserverViewModel(gameserver));
            }

            CreateCommand = new CommandImplementation(o => CreateGameserver());
        }

        private void CreateGameserver()
        {
            Gameserver gameserver = new Gameserver(true);
            App.Memory.Gameservers.Add(gameserver);

            Gameservers.Add(new GameserverViewModel(gameserver));

            App.Memory.Save();
        }
    }
}
