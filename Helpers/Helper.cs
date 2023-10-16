using DTCChecker.Items;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DTCChecker.Helpers
{
    public static class Helper
    {
        public static List<DTCval> ReadJsonConfiguration(string JsonValue)
        {
            List<DTCval> list = new List<DTCval>();
            try
            {
                list = JsonConvert.DeserializeObject<List<DTCval>>(JsonValue);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            //kufotalConf = JsonConvert.DeserializeObject<KufotalJsonConfiguration>(jsonString);
            return list;
        }

        public static string ReadResource(string name)
        {
            // Determine path
            var assembly = Assembly.GetExecutingAssembly();
            Console.WriteLine(assembly);
            string resourcePath = name;

            string execdic = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            resourcePath = Path.Combine(execdic, "Resources", "dtclist.json");

            //Stream stream = assembly.GetManifestResourceStream(resourcePath);

            //using (StreamReader reader = new StreamReader(stream))
            //{
            //    return reader.ReadToEnd();
            //}

            string jsontext = File.ReadAllText(resourcePath);

            return jsontext;           
        }
    }
}
