using Newtonsoft.Json;
using RustServerManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RustServerManager.Models
{
    [DataContract]
    public class Memory
    {
        [DataMember]
        public Configuration Configuration { get; set; } = new Configuration();

        [DataMember]
        public List<Gameserver> Gameservers { get; set; } = new List<Gameserver>();

        #region Serialization

        private static readonly string _appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        private static readonly string _saveFolder = Path.Combine(_appDataFolder, "RSSM");

        private static readonly string _memoryFile = "memory.json";

        public void Save()
        {
            Directory.CreateDirectory(_saveFolder);

            try
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(Path.Combine(_saveFolder, _memoryFile), json);
            }
            catch (Exception)
            {
                System.Windows.MessageBox.Show("There was an error saving the config file...");
                throw;
            }
        }

        public static Memory Load()
        {
            Directory.CreateDirectory(_saveFolder);

            if (File.Exists(Path.Combine(_saveFolder, _memoryFile)))
            {
                Console.WriteLine("Memory File Does Exist...");
                try
                {
                    string json = File.ReadAllText(Path.Combine(_saveFolder, _memoryFile));
                    return JsonConvert.DeserializeObject<Memory>(json);
                }
                catch (Exception)
                {
                    System.Windows.MessageBox.Show("There was an error loading the config file...");
                    throw;
                }
            }
            else
            {
                Console.WriteLine("Memory File Does Not Exist...");
                App.MemoryWasGenerated = true;
                return new Memory();
            }
        }

        #endregion
    }
}
