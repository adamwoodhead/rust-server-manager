using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RustServerManager.Models.Umod
{
    internal static class Umod
    {
        private static readonly string _UMOD_INFO_URL = @"https://umod.org/games/rust.json";
        
        internal static UmodInfoResponse GetInfo()
        {
            try
            {
                using (WebClient webClient = new WebClient())
                {
                    string info = webClient.DownloadString(_UMOD_INFO_URL);

                    UmodInfoResponse umodInfo = JsonConvert.DeserializeObject<UmodInfoResponse>(info);

                    return umodInfo;
                }
            }
            catch (WebException)
            {
                return null;
            }
        }

        internal static void DownloadUmod(Gameserver gameserver)
        {
            try
            {
                using (WebClient webClient = new WebClient())
                {
                    webClient.DownloadFile(_UMOD_INFO_URL, gameserver.WorkingDirectory);
                }
            }
            catch (WebException)
            {
                return;
            }
        }
    }
}
