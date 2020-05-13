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

        public GameserverViewModel(Gameserver gameserver)
        {
            _gameserver = gameserver;

            ViewModelLoopTask(() => { OnPropertyChanged(nameof(IsRunning)); }, 250);
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

        public bool IsRunning
        {
            get => _gameserver.IsRunning;
            set
            {
                if (_gameserver.IsRunning != value)
                {
                    _gameserver.IsRunning = value;
                    OnPropertyChanged(nameof(IsRunning));
                }
            }
        }

        public ICommand StartCommand { get; set; }

        public ICommand StopCommand { get; set; }

        public ICommand KillCommand { get; set; }

        public ICommand RestartCommand { get; set; }

        public ICommand InstallCommand { get; set; }

        public ICommand ReinstallCommand { get; set; }

        public ICommand UninstallCommand { get; set; }

        public ICommand DeleteCommand { get; set; }

        public ICommand WipeMapCommand { get; set; }

        public ICommand WipeBPCommand { get; set; }

        public GameserverViewModel()
        {
            StartCommand = new CommandImplementation(o => Start());
            StopCommand = new CommandImplementation(o => Stop());
            RestartCommand = new CommandImplementation(o => Restart());
            KillCommand = new CommandImplementation(o => Kill());

            InstallCommand = new CommandImplementation(o => Install());
            ReinstallCommand = new CommandImplementation(o => Reinstall());
            UninstallCommand = new CommandImplementation(o => Uninstall());
            DeleteCommand = new CommandImplementation(o => Delete());

            WipeMapCommand = new CommandImplementation(o => WipeMap());
            WipeBPCommand = new CommandImplementation(o => WipeMapAndBP());
        }

        public void Start()
        {
            _gameserver.Start();
        }

        public void Restart()
        {
            _gameserver.Restart();
        }

        public void Stop()
        {
            _gameserver.Stop();
        }

        public void Kill()
        {
            _gameserver.Kill();
        }

        public void Install()
        {
            _gameserver.Install();
        }

        public void Reinstall()
        {
            _gameserver.Reinstall();
        }

        public void Uninstall()
        {
            _gameserver.Uninstall();
        }

        public void Delete()
        {
            _gameserver.Delete();
        }

        public void WipeMap()
        {
            _gameserver.WipeMap();
        }

        public void WipeMapAndBP()
        {
            _gameserver.WipeMapAndBP();
        }
    }
}
