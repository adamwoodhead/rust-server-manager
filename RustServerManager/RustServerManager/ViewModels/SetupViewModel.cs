using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RustServerManager.ViewModels
{
    public class SetupViewModel : ObservableViewModel
    {
        private string _steamCMDDirectory = @"C:\SteamCMD";
        private string _rustServerDirectory = @"C:\RustServer";

        public string SteamCMDDirectory { get => _steamCMDDirectory; set => _steamCMDDirectory = value; }

        public string RustServerDirectory { get => _rustServerDirectory; set => _rustServerDirectory = value; }
    }
}
