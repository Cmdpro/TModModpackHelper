using Gtk;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace TModModpackHelper
{
    public class Modpack
    {
        public string name;
        public string path;
        public Mod[] modlist;
        public Modpack(string name, string path, Mod[] modlist)
        {
            this.name = name;
            this.path = path;
            this.modlist = modlist;
        }

        public void Start()
        {
            Settings.CopyConfigs(this);
            DirectoryInfo modsDirectory = new DirectoryInfo(Path.Combine(path, "instance", "Mods"));
            if (modsDirectory.Exists)
            {
                foreach (FileInfo i in modsDirectory.EnumerateFiles())
                {
                    if (i.Extension == ".tmod_inactive")
                    {
                        i.MoveTo(Path.Combine(modsDirectory.FullName, Path.GetFileNameWithoutExtension(i.Name) + ".tmod"), true);
                    }
                }
            }
            Settings.StartTModLoader("-tmlsavedirectory " + Path.Combine(path, "instance"));
        }
        public void SetModEnabled(Mod mod, bool enabled)
        {
            DirectoryInfo instanceFolder = new DirectoryInfo(Path.Combine(path, "instance"));
            if (!instanceFolder.Exists)
            {
                instanceFolder.Create();
            }
            DirectoryInfo outputModsFolder = new DirectoryInfo(Path.Combine(path, "instance", "Mods"));
            if (!outputModsFolder.Exists)
            {
                outputModsFolder.Create();
            }
            string enabledJsonPath = Path.Combine(outputModsFolder.FullName, "enabled.json");
            JsonArray enabledObj = JsonObject.Parse(File.ReadAllText(enabledJsonPath)) as JsonArray ?? [];
            if (enabled && !enabledObj.Contains(mod.name))
            {
                enabledObj.Add(mod.name);
            }
            if (!enabled)
            {
                JsonNode? first = enabledObj.FirstOrDefault((i) => i != null && i.ToString() == mod.name, null);
                if (first != null)
                {
                    enabledObj.Remove(first);
                }
            }
            File.WriteAllText(enabledJsonPath, enabledObj.ToJsonString());
        }
        public bool IsModEnabled(Mod mod)
        {
            DirectoryInfo outputModsFolder = new DirectoryInfo(Path.Combine(path, "instance", "Mods"));
            if (!outputModsFolder.Exists)
            {
                return false;
            }
            string enabledJsonPath = Path.Combine(outputModsFolder.FullName, "enabled.json");
            JsonArray? enabledObj = JsonObject.Parse(File.ReadAllText(enabledJsonPath)) as JsonArray;
            if (enabledObj != null && enabledObj.Contains(mod.name))
            {
                return true;
            }
            return false;
        }
        public Dictionary<string, string> GetSteamIds()
        {
            Dictionary<string, string> steamIds = new Dictionary<string, string>();

            string steamidsPath = Path.Combine(path, "steamids.json");
            JsonObject? steamidsObj = JsonObject.Parse(File.ReadAllText(steamidsPath)) as JsonObject;
            if (steamidsObj != null)
            {
                foreach (KeyValuePair<string, JsonNode?> i in steamidsObj)
                {
                    if (i.Value != null)
                    {
                        steamIds.Add(i.Key, i.Value.ToString());
                    }
                }
            }
            return steamIds;
        }
        public void Export(string path)
        {
            DirectoryInfo midExportDirectory = new DirectoryInfo(Path.Combine(this.path, "Export"));
            if (midExportDirectory.Exists)
            {
                midExportDirectory.Delete(true);
            }
            midExportDirectory.Create();
            List<(string from, string to)> filesToCopy = [
                ("modpack.json", "modpack.json"),
                (Path.Combine("instance", "Mods", "enabled.json"), Path.Combine("Mods", "enabled.json")),
                (Path.Combine("instance", "ModConfigs"), "ModConfigs")
            ];
            Dictionary<string, string> steamids = GetSteamIds();
            foreach (FileInfo i in new DirectoryInfo(Path.Combine(this.path, "instance", "Mods")).EnumerateFiles())
            {
                if (i.Extension == ".tmod" || i.Extension == ".tmod_inactive")
                {
                    string modName = Path.GetFileNameWithoutExtension(i.Name);
                    if (!steamids.ContainsKey(modName))
                    {
                        filesToCopy.Add((Path.Combine("instance", "Mods", i.Name), Path.Combine("Mods", modName + ".tmod")));
                    }
                }
            }
            foreach ((string from, string to) i in filesToCopy)
            {
                string from = Path.Combine(this.path, i.from);
                string to = Path.Combine(midExportDirectory.FullName, i.to);
                if (!Directory.Exists(from) && !File.Exists(from))
                {
                    continue;
                }
                string[] parts = i.to.Split(Path.DirectorySeparatorChar);
                string folder = midExportDirectory.FullName;
                for (int j = 0; j < parts.Length - 1; j++)
                {
                    folder = Path.Combine(folder, parts[j]);
                    if (!Directory.Exists(folder))
                    {
                        Directory.CreateDirectory(folder);
                    }
                }
                if (Directory.Exists(from))
                {
                    MiscUtil.CopyDirectory(from, to);
                }
                if (File.Exists(from))
                {
                    File.Copy(from, to);
                }
            }
            JsonArray steamidsJson = new JsonArray();
            foreach (string i in steamids.Values)
            {
                steamidsJson.Add(i);
            }
            File.WriteAllText(Path.Combine(midExportDirectory.FullName, "Mods", "steamids.json"), steamidsJson.ToJsonString());

            if (File.Exists(path))
            {
                File.Delete(path);
            }
            ZipFile.CreateFromDirectory(midExportDirectory.FullName, path);
        }
    }
    public class Mod
    {
        public string name;
        public string? steamId;
        public bool enabled;
        public Mod(string name, string? steamId, bool enabled)
        {
            this.name = name;
            this.steamId = steamId;
            this.enabled = enabled;
        }
    }
}
