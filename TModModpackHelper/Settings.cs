using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace TModModpackHelper
{
    public static class Settings
    {
        public static readonly string dataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TModLoaderModpackHelper");
        public static readonly string modpacksPath = Path.Combine(dataPath, "Modpacks");
        public static readonly string appid = "1281930";
        public static string steamPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam");
        public static readonly string steamExePath = Path.Combine(steamPath, "steam.exe");
        public static readonly string settingsPath = Path.Combine(dataPath, "settings.json");
        
        public static void LoadSettings()
        {
            if (File.Exists(settingsPath))
            {
                JsonObject jsonObj = JsonObject.Parse(File.ReadAllText(settingsPath)) as JsonObject;
                if (jsonObj.ContainsKey("steamPath"))
                {
                    steamPath = jsonObj["steamPath"].ToString();
                }
            }
        }
        public static void SaveSettings()
        {
            JsonObject jsonObj = new JsonObject();
            jsonObj.Add("steamPath", steamPath);
            File.WriteAllText(settingsPath, jsonObj.ToJsonString());
        }

        //public static string TMODLOADER_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam/steamapps/common/tModLoader");
        public static void StartTModLoader(string args)
        {
            if (IsTModLoaderRunning())
            {
                return;
            }
            string launchArg = "-applaunch " + appid;
            Process.Start(steamExePath, launchArg + " " + args);
        }
        public static void StopTModLoader()
        {
            File.WriteAllLines(Path.Combine(dataPath, "processes.txt"), Process.GetProcesses().Select((i) => i.MainWindowTitle));
            Process? process = GetTModLoader();
            if (process != null)
            {
                process.CloseMainWindow();
            }
        }
        public static bool IsTModLoaderRunning()
        {
            return GetTModLoader() != null;
        }
        // I dont know a better way at the time so
        public static Process? GetTModLoader()
        {
            return Process.GetProcesses().FirstOrDefault((i) => i.MainWindowTitle.StartsWith("Terraria"));
        }
        public static void CopyConfigs(Modpack modpack)
        {
            DirectoryInfo tModDirectory = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Terraria", "tModLoader"));
            if (tModDirectory.Exists)
            {
                string[] filesToCopy = [
                    "config.json"
                ];
                foreach (string i in filesToCopy)
                {
                    string fromPath = Path.Combine(tModDirectory.FullName, i);
                    string targetPath = Path.Combine(modpack.path, "instance", i);
                    if (!File.Exists(targetPath))
                    {
                        string instanceDir = Path.Combine(modpack.path, "instance");
                        if (!Directory.Exists(instanceDir))
                        {
                            Directory.CreateDirectory(instanceDir);
                        }
                        File.Copy(fromPath, targetPath);
                    }
                }
            }
        }
    }
}
