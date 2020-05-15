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
        public ObservableCollection<GameserverViewModel> Gameservers { get; set; } = new ObservableCollection<GameserverViewModel>();

        public ICommand CreateCommand { get; set; }

        public ICommand UpdateCommand { get; set; }

        public GameserversViewModel(List<Gameserver> gameservers)
        {
            foreach (Gameserver gameserver in gameservers)
            {
                Gameservers.Add(new GameserverViewModel(gameserver));
            }

            CreateCommand = new CommandImplementation(o => CreateGameserver());
            UpdateCommand = new CommandImplementation(o => UpdateRust());
        }

        private void CreateGameserver()
        {
            Gameserver gameserver = new Gameserver(true);
            App.Memory.Gameservers.Add(gameserver);

            Gameservers.Add(new GameserverViewModel(gameserver));

            App.Memory.Save();
        }

        private async void UpdateRust()
        {
            Views.SetupWindow setupWindow = new Views.SetupWindow(false)
            {
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen
            };

            TaskCompletionSource<bool?> completion = new TaskCompletionSource<bool?>();
            await setupWindow.Dispatcher.BeginInvoke(new Action(() => completion.SetResult(setupWindow.ShowDialog())));
            //bool? result = await completion.Task;
        }
    }
}
