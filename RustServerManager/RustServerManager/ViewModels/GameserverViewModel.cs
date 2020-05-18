using RustServerManager.Interfaces;
using RustServerManager.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace RustServerManager.ViewModels
{
    public class GameserverViewModel : ObservableViewModel, IGameserver
    {
        private Gameserver _gameserver;
        private string _commandToSay;
        private string _commandToRcon;
        private bool _updating = false;

        public GameserverViewModel(Gameserver gameserver)
        {
            _gameserver = gameserver;

            ViewModelLoopTask(() =>
            {
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(IsRunning));
            }, 250);

            StartCommand = new CommandImplementation(o => Start());
            StopCommand = new CommandImplementation(o => Stop());
            RestartCommand = new CommandImplementation(o => Restart());
            KillCommand = new CommandImplementation(o => Kill());

            DeleteCommand = new CommandImplementation(o => Delete());

            InstallCommand = new CommandImplementation(o => Install());
            UninstallCommand = new CommandImplementation(o => Uninstall());
            ReinstallCommand = new CommandImplementation(o => Reinstall());

            WipeMapCommand = new CommandImplementation(o => WipeMap());
            WipeBPCommand = new CommandImplementation(o => WipeMapAndBP());
        }
         
        public GameserverViewModel()
        {
            _gameserver = new Gameserver();

            StartCommand = new CommandImplementation(o => Start());
            StopCommand = new CommandImplementation(o => Stop());
            RestartCommand = new CommandImplementation(o => Restart());
            KillCommand = new CommandImplementation(o => Kill());

            DeleteCommand = new CommandImplementation(o => Delete());

            InstallCommand = new CommandImplementation(o => Install());
            UninstallCommand = new CommandImplementation(o => Uninstall());
            ReinstallCommand = new CommandImplementation(o => Reinstall());

            WipeMapCommand = new CommandImplementation(o => WipeMap());
            WipeBPCommand = new CommandImplementation(o => WipeMapAndBP());
        }

        public bool Updating
        {
            get => _updating;
            set
            {
                if (_updating != value)
                {
                    _updating = value;
                    OnPropertyChanged(nameof(Updating));
                }
            }
        }

        public int ID
        {
            get => _gameserver.ID;
            set
            {
                if (_gameserver.ID != value)
                {
                    _gameserver.ID = value;
                    OnPropertyChanged(nameof(ID));
                }
            }
        }

        public string Name
        {
            get => _gameserver.Name;
            set
            {
                if (_gameserver.Name != value)
                {
                    _gameserver.Name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        // Advanced Config

        public string Server_Identity
        {
            get => _gameserver.Server_Identity;
            set
            {
                if (_gameserver.Server_Identity != value)
                {
                    _gameserver.Server_Identity = value;
                    OnPropertyChanged(nameof(Server_Identity));
                }
            }
        }

        public string Server_IP
        {
            get => _gameserver.Server_IP;
            set
            {
                if (_gameserver.Server_IP != value)
                {
                    _gameserver.Server_IP = value;
                    OnPropertyChanged(nameof(Server_IP));
                }
            }
        }

        public int Server_Port
        {
            get => _gameserver.Server_Port;
            set
            {
                if (_gameserver.Server_Port != value)
                {
                    _gameserver.Server_Port = value;
                    OnPropertyChanged(nameof(Server_Port));
                }
            }
        }

        public string RCON_IP
        {
            get => _gameserver.RCON_IP;
            set
            {
                if (_gameserver.RCON_IP != value)
                {
                    _gameserver.RCON_IP = value;
                    OnPropertyChanged(nameof(RCON_IP));
                }
            }
        }

        public string RCON_Password
        {
            get => _gameserver.RCON_Password;
            set
            {
                if (_gameserver.RCON_Password != value)
                {
                    _gameserver.RCON_Password = value;
                    OnPropertyChanged(nameof(RCON_Password));
                }
            }
        }

        public int RCON_Port
        {
            get => _gameserver.RCON_Port;
            set
            {
                if (_gameserver.RCON_Port != value)
                {
                    _gameserver.RCON_Port = value;
                    OnPropertyChanged(nameof(RCON_Port));
                }
            }
        }

        public bool RCON_Web
        {
            get => _gameserver.RCON_Web;
            set
            {
                if (_gameserver.RCON_Web != value)
                {
                    _gameserver.RCON_Web = value;
                    OnPropertyChanged(nameof(RCON_Web));
                }
            }
        }

        public int Server_Tickrate
        {
            get => _gameserver.Server_Tickrate;
            set
            {
                if (_gameserver.Server_Tickrate != value)
                {
                    _gameserver.Server_Tickrate = value;
                    OnPropertyChanged(nameof(Server_Tickrate));
                }
            }
        }

        // Simple Config

        public string Server_Hostname
        {
            get => _gameserver.Server_Hostname;
            set
            {
                if (_gameserver.Server_Hostname != value)
                {
                    _gameserver.Server_Hostname = value;
                    OnPropertyChanged(nameof(Server_Hostname));
                }
            }
        }

        public int Server_MaxPlayers
        {
            get => _gameserver.Server_MaxPlayers;
            set
            {
                if (_gameserver.Server_MaxPlayers != value)
                {
                    _gameserver.Server_MaxPlayers = value;
                    OnPropertyChanged(nameof(Server_MaxPlayers));
                }
            }
        }

        public int Server_WorldSize
        {
            get => _gameserver.Server_WorldSize;
            set
            {
                if (_gameserver.Server_WorldSize != value)
                {
                    _gameserver.Server_WorldSize = value;
                    OnPropertyChanged(nameof(Server_WorldSize));
                }
            }
        }

        public string Server_Seed
        {
            get => _gameserver.Server_Seed;
            set
            {
                if (_gameserver.Server_Seed != value)
                {
                    _gameserver.Server_Seed = value;
                    OnPropertyChanged(nameof(Server_Seed));
                }
            }
        }

        public int Server_SaveInterval
        {
            get => _gameserver.Server_SaveInterval;
            set
            {
                if (_gameserver.Server_SaveInterval != value)
                {
                    _gameserver.Server_SaveInterval = value;
                    OnPropertyChanged(nameof(Server_SaveInterval));
                }
            }
        }

        // Non Interactive

        public string WorkingDirectory
        {
            get => _gameserver.WorkingDirectory;
            set
            {
                if (_gameserver.WorkingDirectory != value)
                {
                    _gameserver.WorkingDirectory = value;
                    OnPropertyChanged(nameof(WorkingDirectory));
                }
            }
        }

        public bool IsInstalled
        {
            get => _gameserver.IsInstalled;
            set
            {
                if (_gameserver.IsInstalled != value)
                {
                    _gameserver.IsInstalled = value;
                    OnPropertyChanged(nameof(IsInstalled));
                }
            }
        }

        public bool UmodInstalled
        {
            get => _gameserver.UmodInstalled;
            set
            {
                if (_gameserver.UmodInstalled != value)
                {
                    _gameserver.UmodInstalled = value;
                    OnPropertyChanged(nameof(UmodInstalled));
                }
            }
        }

        public bool IsRunning
        {
            get => _gameserver.IsRunning;
        }

        public string Status
        {
            get => _gameserver.Status;
            set
            {
                if (_gameserver.Status != value)
                {
                    _gameserver.Status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        public string CommandToRcon
        {
            get => _commandToRcon;
            set
            {
                if (_commandToRcon != value)
                {
                    _commandToRcon = value;
                    OnPropertyChanged(nameof(CommandToRcon));
                }
            }
        }

        public string CommandToSay
        {
            get => _commandToSay;
            set
            {
                if (_commandToSay != value)
                {
                    _commandToSay = value;
                    OnPropertyChanged(nameof(CommandToSay));
                }
            }
        }

        public ICommand StartCommand { get; set; }

        public ICommand StopCommand { get; set; }

        public ICommand KillCommand { get; set; }

        public ICommand RestartCommand { get; set; }

        public ICommand DeleteCommand { get; set; }

        public ICommand InstallUmodCommand { get; set; }

        public ICommand InstallCommand { get; set; }

        public ICommand UninstallCommand { get; set; }

        public ICommand ReinstallCommand { get; set; }

        public ICommand WipeMapCommand { get; set; }

        public ICommand WipeBPCommand { get; set; }

        private void StatusUpdate(string status, bool active)
        {
            Status = status;
            Updating = active;
        }

        public async void Start()
        {
            await Task.Run(() => {
                StatusUpdate("Starting", true);

                _gameserver.Start();

                StatusUpdate("", false);
            });
        }

        public async void Restart()
        {
            await Task.Run(() => {
                StatusUpdate("Restarting", true);

                _gameserver.Restart();

                StatusUpdate("", false);
            });
        }

        public async void Stop()
        {
            await Task.Run(() => {
                StatusUpdate("Stopping", true);

                _gameserver.Stop();

                StatusUpdate("", false);
            });
        }

        public async void Kill()
        {
            StatusUpdate("Killing", true);

            await Task.Run(() => {
                _gameserver.Kill();
            });

            StatusUpdate("", false);
        }

        public async void Delete()
        {
            StatusUpdate("Deleting", true);

            await Task.Run(() => {
                _gameserver.Delete();
            });

            StatusUpdate("", false);
        }

        public async void InstallUmod()
        {
            StatusUpdate("Installing Umod", true);

            await _gameserver.InstallUmod();
            UmodInstalled = true;
            App.Memory.Save();

            StatusUpdate("", false);
        }

        public async void Install()
        {
            StatusUpdate("Installing", true);

            await _gameserver.Install();
            IsInstalled = true;
            App.Memory.Save();

            StatusUpdate("", false);
        }

        public async void Update()
        {
            StatusUpdate("Updating", true);

            await _gameserver.Install();
            IsInstalled = true;
            App.Memory.Save();

            StatusUpdate("", false);
        }

        public async void Uninstall()
        {
            StatusUpdate("Uninstalling", true);

            await _gameserver.Uninstall();
            IsInstalled = false;
            App.Memory.Save();

            StatusUpdate("", false);
        }

        public async void Reinstall()
        {
            StatusUpdate("Reinstalling", true);

            await Task.Run(() => {
                _gameserver.Reinstall();
            });

            StatusUpdate("", false);
        }

        public async void WipeMap()
        {
            await Task.Run(() => {
                _gameserver.WipeMap();
            });
        }

        public async void WipeMapAndBP()
        {
            await Task.Run(() => {
                _gameserver.WipeMapAndBP();
            });
        }
    }
}
