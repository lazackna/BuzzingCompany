using BepInEx;
using BepInEx.Configuration;

namespace LethalMod
{
    internal class Config
    {
        private static ConfigFile ConfigFile;

        internal static ConfigEntry<string> ServerUri;

        static Config()
        {
            ConfigFile = new ConfigFile(Paths.ConfigPath + "\\BuzzingCompany.cfg", true);

            ServerUri = ConfigFile.Bind(
                "Devices",
                "Server Uri",
                "ws://localhost:12345",
                "Intiface server URI");
            
            
        }
    }
}