using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using TShockAPI;

namespace CustomStarterItems
{
    class Config
    {
        private static string savepath = TShock.SavePath;
        public static Contents contents;

        public class Contents
        {
            public bool EnableStarterItems = true;

            public int[] StarterItems = new int[]
            #region StarterItems 
            {
                -13,    //Copper Pickaxe
                24,     //Wooden Sword
                -16,    //Copper Axe
                76,     //Copper Greaves
                80,     //Copper Chainmail
                89      //Copper Helmet
            };
            #endregion
        }

        public static void CreateConfig()
        {
            string filepath = Path.Combine(savepath, "CustomStarterItems.json");

            try
            {
                using (var stream = new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.Write))
                {
                    using (var sr = new StreamWriter(stream))
                    {
                        contents = new Contents();
                        var configString = JsonConvert.SerializeObject(contents, Formatting.Indented);
                        sr.Write(configString);
                    }
                    stream.Close();
                }
            }
            catch (Exception e)
            {
                Log.ConsoleError(e.Message);
                contents = new Contents();
            }
        }

        public static bool ReadConfig()
        {
            string filepath = Path.Combine(savepath, "CustomStarterItems.json");

            try
            {
                if (File.Exists(filepath))
                {
                    using (var stream = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        using (var sr = new StreamReader(stream))
                        {
                            var configString = sr.ReadToEnd();
                            contents = JsonConvert.DeserializeObject<Contents>(configString);
                        }
                        stream.Close();
                    }
                    return true;
                }
                else
                {
                    CreateConfig();
                    Log.ConsoleInfo("Created CustomStarterItems.json");
                    return true;
                }
            }
            catch (Exception e)
            {
                Log.ConsoleError(e.Message);
            }
            return false;
        }

        public static bool UpdateConfig()
        {
            string filepath = Path.Combine(savepath, "CustomStarterItems.json");

            try
            {
                if (!File.Exists(filepath))
                    return false;

                string query = JsonConvert.SerializeObject(contents, Formatting.Indented);
                using (var stream = new StreamWriter(filepath, false))
                {
                    stream.Write(query);
                }
                return true;
            }
            catch (Exception e)
            {
                Log.ConsoleError(e.Message);
                return false;
            }
        }
    }
}
