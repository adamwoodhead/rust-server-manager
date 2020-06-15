using ServerNode.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerNode.Models.Servers
{
    public partial class Server
    {
        public event EventHandler Starting;

        public event EventHandler Started;

        public event EventHandler StartFailed;


        public event EventHandler Stopping;

        public event EventHandler Stopped;

        public event EventHandler StopFailed;


        public event EventHandler Restarting;

        public event EventHandler Restarted;

        public event EventHandler RestartFailed;


        public event EventHandler Installing;

        public event EventHandler Installed;

        public event EventHandler InstallFailed;


        public event EventHandler Updating;

        public event EventHandler Updated;

        public event EventHandler UpdateFailed;


        public event EventHandler Uninstalling;

        public event EventHandler Uninstalled;

        public event EventHandler UninstallFailed;


        public event EventHandler Reinstalling;

        public event EventHandler Reinstalled;

        public event EventHandler ReinstallFailed;


        public event EventHandler Deleting;

        public event EventHandler Deleted;

        public event EventHandler DeleteFailed;


        public event EventHandler UnexpectedStop;

        internal void OnStarting()
        {
            Log.Informational($"Server {this.ID:00} Starting.");
            Starting.Invoke(this, null);
        }

        internal void OnStarted()
        {
            Log.Success($"Server {this.ID:00} Started.");
            Started.Invoke(this, null);
        }

        internal void OnStartFailed()
        {
            Log.Warning($"Server {this.ID:00} Start Failed.");
            StartFailed.Invoke(this, null);
        }

        internal void OnStopping()
        {
            Log.Informational($"Server {this.ID:00} Stopping.");
            Stopping.Invoke(this, null);
        }

        internal void OnStopped()
        {
            Log.Success($"Server {this.ID:00} Stopped.");
            Stopped.Invoke(this, null);
        }

        internal void OnStopFailed()
        {
            Log.Warning($"Server {this.ID:00} Stop Failed.");
            StopFailed.Invoke(this, null);
        }

        internal void OnRestarting()
        {
            Log.Informational($"Server {this.ID:00} Restarting.");
            Restarting.Invoke(this, null);
        }

        internal void OnRestarted()
        {
            Log.Success($"Server {this.ID:00} Restarted.");
            Restarted.Invoke(this, null);
        }

        internal void OnRestartFailed()
        {
            Log.Warning($"Server {this.ID:00} Restart Failed.");
            RestartFailed.Invoke(this, null);
        }

        internal void OnInstalling()
        {
            Log.Informational($"Server {this.ID:00} Installing.");
            Installing.Invoke(this, null);
        }

        internal void OnInstalled()
        {
            Log.Success($"Server {this.ID:00} Installed.");
            Installed.Invoke(this, null);
        }

        internal void OnInstallFailed()
        {
            Log.Warning($"Server {this.ID:00} Install Failed.");
            InstallFailed.Invoke(this, null);
        }

        internal void OnUpdating()
        {
            Log.Informational($"Server {this.ID:00} Updating.");
            Updating.Invoke(this, null);
        }

        internal void OnUpdated()
        {
            Log.Success($"Server {this.ID:00} Updated.");
            Updated.Invoke(this, null);
        }

        internal void OnUpdateFailed()
        {
            Log.Warning($"Server {this.ID:00} Updated Failed.");
            UpdateFailed.Invoke(this, null);
        }

        internal void OnUninstalling()
        {
            Log.Informational($"Server {this.ID:00} Uninstalling.");
            Uninstalling.Invoke(this, null);
        }

        internal void OnUninstalled()
        {
            Log.Success($"Server {this.ID:00} Uninstalled.");
            Uninstalled.Invoke(this, null);
        }

        internal void OnUninstallFailed()
        {
            Log.Warning($"Server {this.ID:00} Uninstall Failed.");
            UninstallFailed.Invoke(this, null);
        }

        internal void OnReinstalling()
        {
            Log.Informational($"Server {this.ID:00} Reinstalling.");
            Reinstalling.Invoke(this, null);
        }

        internal void OnReinstalled()
        {
            Log.Success($"Server {this.ID:00} Reinstalled.");
            Reinstalled.Invoke(this, null);
        }

        internal void OnReinstallFailed()
        {
            Log.Warning($"Server {this.ID:00} Reinstall Failed.");
            ReinstallFailed.Invoke(this, null);
        }

        internal void OnDeleting()
        {
            Log.Informational($"Server {this.ID:00} Deleting.");
            Deleting.Invoke(this, null);
        }

        internal void OnDeleted()
        {
            Log.Success($"Server {this.ID:00} Deleted.");
            Deleted.Invoke(this, null);
        }

        internal void OnDeleteFailed()
        {
            Log.Warning($"Server {this.ID:00} Delete Failed.");
            DeleteFailed.Invoke(this, null);
        }

        internal void OnUnexpectedStop()
        {
            Log.Success($"Server {this.ID:00} Unexpectedly Stopped.");
            UnexpectedStop.Invoke(this, null);
        }
    }
}
