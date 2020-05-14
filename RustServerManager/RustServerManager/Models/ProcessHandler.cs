//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace RustServerManager.Models
//{
//    public class ProcessHandler
//    {
//        private int? _processID = null;
//        private Process _appProcess = null;
//        private Watchdog _watchdog = null;
//        private Gameserver _relatedGameserver = null;
//        private bool _stopRequested = false;
//        private Wrapper _wrapper = null;

//        public int? ProcessID { get => _processID ?? 0; set => _processID = value; }

//        public Process AppProcess { get => _appProcess; set => _appProcess = value; }

//        public Watchdog Watchdog { get => _watchdog; set => _watchdog = value; }

//        public Gameserver RelatedGameserver { get => _relatedGameserver; set => _relatedGameserver = value; }

//        public Wrapper Wrapper { get => _wrapper; set => _wrapper = value; }

//        public bool StopRequested { get => _stopRequested; private set => _stopRequested = value; }

//        public bool IsRunning
//        {
//            get
//            {
//                if (AppProcess == null)
//                {
//                    return false;
//                }

//                if (AppProcess.HasExited)
//                {
//                    return false;
//                }

//                return true;
//            }
//        }

//        internal ProcessHandler(Gameserver gameserver)
//        {
//            RelatedGameserver = gameserver;
//            RelatedGameserver.Performance = new Statistics.Performance(this);
//        }

//        internal async Task Start(bool WatchdogRequested = false)
//        {
//            await Task.Run(() => {
//                StopRequested = false;
//                AssignProtocol();

//                AppProcess = new Process
//                {
//                    StartInfo = new ProcessStartInfo()
//                    {
//                        FileName = Path.Combine(RelatedGameserver.WorkingDirectory, RelatedGameserver.Profile.Executable),
//                        Arguments = RelatedGameserver.Commandline.ToString(),
//                        UseShellExecute = false,
//                        RedirectStandardError = false,
//                        RedirectStandardInput = false,
//                        RedirectStandardOutput = false
//                    },
//                };

//                AppProcess.Exited += HasExited;

//                RelatedGameserver.Protocol?.PreStart();
//                AppProcess?.Start();
//                RelatedGameserver.Protocol?.PostStart();

//#if RELEASE
//                RelatedGameserver.Performance.BeginMonitoring();
//#endif

//                Watchdog = new Watchdog(this);
//                Watchdog.BeginWatching(WatchdogRequested);

//                ProcessID = AppProcess.Id;

//                Wrapper = new Wrapper(this);
//            });
//        }

//        internal async Task Stop()
//        {
//            await Task.Run(() =>
//            {
//                try
//                {
//                    Wrapper.Stop();

//                    StopRequested = true;

//                    if (RelatedGameserver.Protocol != null)
//                    {
//                        RelatedGameserver.Protocol.SafeStop();
//                    }

//                    if (!AppProcess.HasExited)
//                    {
//                        AppProcess.Kill();
//                    }

//                    AppProcess = null;
//                }
//                catch (Exception e)
//                {
//                    Log.Exception(e);
//                }
//            });
//        }

//        internal async Task Restart()
//        {
//            await Stop();
//            await Start();
//        }

//        private void HasExited(object sender, EventArgs e)
//        {
//            RelatedGameserver.OnProcessExited();
//            Log.Warning($"Gameserver {RelatedGameserver.ID} has exited.");
//        }

//        private void AssignProtocol()
//        {
//            switch (RelatedGameserver.Profile.Protocol)
//            {
//                case ProtocolType.DIRECT_IN_OUT:
//                    RelatedGameserver.Protocol = new Console.DirectStInOut(RelatedGameserver);
//                    break;

//                case ProtocolType.SOURCE_RCON:
//                    RelatedGameserver.Protocol = new Console.SourceRconClient(RelatedGameserver);
//                    break;

//                default:
//                    RelatedGameserver.Protocol = null;
//                    break;
//            }
//        }
//    }

//}
