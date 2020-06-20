using System;
using System.IO;
using System.Net;

namespace ApiConnector.Models
{
    internal class ApiBase
    {
        public static string RootURI { get => Connector.RootURI; }

        public static string Request(RequestType requestType, string url)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

                request.Method = requestType.ToString(); // POST, GET, PUT, PATCH, DELETE

                request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36";
                request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                request.ContentType = "application/json";

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                string content = string.Empty;
                using (Stream stream = response.GetResponseStream())
                {
                    using (StreamReader streamReader = new StreamReader(stream))
                    {
                        content = streamReader.ReadToEnd();
                    }
                }

                return content;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.ToString());
                return null;
            }
        }
    }
}