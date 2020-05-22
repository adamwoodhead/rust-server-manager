using Newtonsoft.Json;
using RustServerManager.Models;
using RustServerManager.Utility;
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
        private static readonly string _appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        private static readonly string _saveFolder = Path.Combine(_appDataFolder, "RSSM");

        private static readonly string _memoryFile = "memory.json";

        [DataMember]
        public Configuration Configuration { get; set; } = new Configuration(400, 600);

        [DataMember]
        public List<Gameserver> Gameservers { get; set; } = new List<Gameserver>();

        private static string QueryFolder()
        {
            string baseDir = string.Empty;

            using (System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "Please select, or create, a working directory where you would like Rust Server Manager to store the rust server(s) files.";
                dialog.RootFolder = Environment.SpecialFolder.MyComputer;
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    if (string.IsNullOrEmpty(dialog.SelectedPath) || !Directory.Exists(dialog.SelectedPath))
                    {
                        return QueryFolder();
                    }
                    else
                    {
                        baseDir = dialog.SelectedPath;
                        return baseDir;
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        #region Serialization

        public Memory(string basedir)
        {
            this.Configuration.WorkingDirectory = basedir;
        }

        public void Save()
        {
            Directory.CreateDirectory(_saveFolder);

            try
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                string encryptedData = Encryption.Encrypt(json, "asd");
                File.WriteAllText(Path.Combine(_saveFolder, _memoryFile), encryptedData);
            }
            catch (Exception)
            {
                System.Windows.MessageBox.Show("There was an error saving the config file...");
                throw;
            }
        }

        public static void Load()
        {
            Directory.CreateDirectory(_saveFolder);

            if (File.Exists(Path.Combine(_saveFolder, _memoryFile)))
            {
                Console.WriteLine("Memory File Does Exist...");
                try
                {
                    string encryptedData = File.ReadAllText(Path.Combine(_saveFolder, _memoryFile));
                    string json = Encryption.Decrypt(encryptedData, "asd");
                    App.Memory = JsonConvert.DeserializeObject<Memory>(json);
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
                string dir = QueryFolder();
                App.Memory = new Memory(dir);
            }
        }

        #endregion
    }
}
