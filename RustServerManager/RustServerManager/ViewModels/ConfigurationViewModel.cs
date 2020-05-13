using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RustServerManager.Models;

namespace RustServerManager.ViewModels
{
    public class ConfigurationViewModel : ObservableViewModel
    {
        private Configuration configuration;

        public ConfigurationViewModel(Configuration configuration)
        {
            this.configuration = configuration;
        }
    }
}
