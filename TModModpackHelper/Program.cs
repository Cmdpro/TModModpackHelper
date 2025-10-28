using Gtk;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using TModModpackHelper.Screens;

namespace TModModpackHelper
{
    internal class Program
    {
        public static Window window;
        public static Dictionary<string, Modpack> modpacks = new Dictionary<string, Modpack>();
        public static Platform.Platform platform;
        public static ModpackScreen modpackScreen = new ModpackScreen();
        public static NoModpacksScreen noModpacksScreen = new NoModpacksScreen();
        public static NewModpackScreen newModpackScreen = new NewModpackScreen();
        public static CreateModpackScreen createModpackScreen = new CreateModpackScreen();
        public static SettingsScreen settingsScreen = new SettingsScreen();
        static void Main(string[] args)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                platform = new Platform.WindowsPlatform();
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                platform = new Platform.LinuxPlatform();
            }
            Settings.LoadSettings();

            if (!Directory.Exists(Settings.dataPath))
            {
                Directory.CreateDirectory(Settings.dataPath);
            }
            if (!File.Exists(Settings.settingsPath))
            {
                Settings.SaveSettings();
            }
            if (!Directory.Exists(Settings.modpacksPath))
            {
                Directory.CreateDirectory(Settings.modpacksPath);
            }

            Application.Init();

            platform.SteamCmdCheck();

            //if (Directory.Exists(Path.Combine(Settings.dataPath, "Testing"))) Directory.Delete(Path.Combine(Settings.dataPath, "Testing"), true);
            //ImportModpack(Path.Combine(Settings.dataPath, "Testing.zip"));

            window = new Window("TModLoader Modpack Helper");
            window.SetDefaultSize(600, 500);
            window.Resizable = false;
            window.DeleteEvent += delegate { Application.Quit(); };

            noModpacksScreen.SetScreen();
            window.ShowAll();

            ReloadModpacks();

            Application.Run();

            Settings.StopTModLoader();
        }
        public static void PromptModpackImport(string? forcedName = null, bool updating = false)
        {
            FileChooserDialog fileChooser = new FileChooserDialog(
                "Select a modpack to import",
                window,
                FileChooserAction.Open,
                "Cancel", ResponseType.Cancel,
                "Open", ResponseType.Accept
            );
            FileFilter filter = new FileFilter();
            filter.Name = "Modpacks";
            filter.AddPattern("*.zip");
            fileChooser.AddFilter(filter);

            FileFilter allFilesFilter = new FileFilter();
            allFilesFilter.Name = "All Files";
            allFilesFilter.AddPattern("*.*");
            fileChooser.AddFilter(allFilesFilter);

            int response = fileChooser.Run();
            if (response == (int)ResponseType.Accept)
            {
                string file = fileChooser.Filename;
                fileChooser.Destroy();
                (DirectoryInfo directory, Modpack modpack) modpack = ImportModpack(file, forcedName, updating);
                ReloadModpacks();
                modpackScreen.BuildModpackSelection();
                modpackScreen.GetModpackSelect().Active = modpacks.Keys.ToList().IndexOf(modpack.directory.Name);
                modpackScreen.SetToModlist(modpackScreen.GetMods(), modpacks[modpack.directory.Name]);
            } else if (response == (int)ResponseType.Cancel)
            {
                fileChooser.Destroy();
            } else
            {
                fileChooser.Destroy();
            }
        }
        public static void ReloadModpacks()
        {
            ModInstallationHelper.FindSubscribedModData();
            modpacks.Clear();
            foreach (DirectoryInfo i in new DirectoryInfo(Settings.modpacksPath).EnumerateDirectories())
            {
                DirectoryInfo modsFolder = new DirectoryInfo(Path.Combine(i.FullName, "instance", "Mods"));
                Modpack? pack = null;
                FileInfo info = new FileInfo(Path.Combine(i.FullName, "modpack.json"));
                string[] modpackMods = [];
                if (info.Exists)
                {
                    JsonObject json = JsonObject.Parse(File.ReadAllText(info.FullName)) as JsonObject;
                    if (!json.ContainsKey("name"))
                    {
                        Console.WriteLine("Failed to load modpack at \"" + i.FullName + "\" because \"name\" in \"modpack.json\" doesnt exist");
                        continue;
                    }
                    if (!json.ContainsKey("modlist"))
                    {
                        Console.WriteLine("Failed to load modpack at \"" + i.FullName + "\" because \"modlist\" in \"modpack.json\" doesnt exist");
                        continue;
                    }
                    string modpackName = json["name"].ToString();
                    pack = new Modpack(modpackName, i.FullName, []);
                    modpackMods = json["modlist"].AsArray().Where((i) => i != null).Select((i) => i.ToString()).ToArray();
                    modpacks.Add(i.Name, pack);
                }
                if (pack == null)
                {
                    continue;
                }
                Dictionary<string, string> steamids = new Dictionary<string, string>();
                FileInfo info2 = new FileInfo(Path.Combine(i.FullName, "steamids.json"));
                if (info2.Exists)
                {
                    try
                    {
                        JsonObject json = JsonObject.Parse(File.ReadAllText(info2.FullName)) as JsonObject;
                        foreach (KeyValuePair<string, JsonNode?> j in json)
                        {
                            if (j.Value.GetValueKind() == JsonValueKind.String)
                            {
                                steamids.Add(j.Key, j.Value.ToString());
                            }
                        }

                    }
                    catch (Exception) { }
                }


                List<string> enabled = new List<string>();
                List<Mod> modlist = new List<Mod>();
                if (modsFolder.Exists)
                {
                    FileInfo info3 = new FileInfo(Path.Combine(modsFolder.FullName, "enabled.json"));
                    if (info3.Exists)
                    {
                        try
                        {
                            JsonArray json = JsonObject.Parse(File.ReadAllText(info3.FullName)) as JsonArray;
                            foreach (JsonNode? j in json)
                            {
                                if (j.GetValueKind() == JsonValueKind.String)
                                {
                                    enabled.Add(j.ToString());
                                }
                            }
                        }
                        catch (Exception) { }
                    }
                    foreach (FileInfo j in modsFolder.EnumerateFiles())
                    {
                        if (j.Extension == ".tmod" || j.Extension == ".tmod_inactive")
                        {
                            string modname = Path.GetFileNameWithoutExtension(j.Name);
                            Mod mod = new Mod(modname, steamids.GetValueOrDefault(modname), enabled.Contains(modname));
                            modlist.Add(mod);
                        }
                    }
                }
                Dictionary<string, string> newsteamids = new Dictionary<string, string>();
                foreach (string modName in modpackMods)
                {
                    if (modlist.Any((i) => i.name == modName))
                    {
                        continue;
                    }
                    if (!ModInstallationHelper.subscribedModData.ContainsKey(modName))
                    {
                        continue;
                    }
                    (string id, string path) subscribedData = ModInstallationHelper.subscribedModData[modName];
                    FileInfo modFile = new FileInfo(subscribedData.path);
                    modFile.CopyTo(Path.Combine(modsFolder.FullName, modFile.Name));
                    string modname = Path.GetFileNameWithoutExtension(modFile.Name);
                    Mod mod = new Mod(modname, subscribedData.id, enabled.Contains(modname));
                    modlist.Add(mod);
                    newsteamids.Add(modName, subscribedData.id);
                }
                if (newsteamids.Count > 0)
                {
                    foreach (KeyValuePair<string, string> j in newsteamids)
                    {
                        if (!steamids.TryAdd(j.Key, j.Value)) steamids[j.Key] = j.Value;
                    }
                    JsonObject obj = new JsonObject();
                    foreach (KeyValuePair<string, string> j in steamids)
                    {
                        obj.Add(j.Key, j.Value);
                    }
                    File.WriteAllText(info2.FullName, obj.ToJsonString());
                }
                pack.modlist = modlist.ToArray();
            }
            if (modpacks.Count > 0)
            {
                modpackScreen.SetScreen();
            } else if (modpacks.Count <= 0)
            {
                noModpacksScreen.SetScreen();
            }
        }
        public static Modpack GetCurrentModpack()
        {
            return modpackScreen.GetSelectedModpack();
        }

        public static (DirectoryInfo directory, Modpack modpack) ImportModpack(string path, string? forcedName = null, bool updating = false)
        {
            FileInfo zip = new FileInfo(path);
            string modpackFileName = forcedName ?? Path.GetFileNameWithoutExtension(zip.Name);
            List<string> dontInstallIds = new List<string>();
            Dictionary<string, string> originalSteamIds = new Dictionary<string, string>();
            if (updating)
            {
                string originalSteamIdsPath = Path.Combine(Settings.modpacksPath, modpackFileName, "steamids.json");
                if (File.Exists(originalSteamIdsPath))
                {
                    JsonObject idsJson = JsonObject.Parse(File.ReadAllText(originalSteamIdsPath)) as JsonObject;
                    foreach (var i in idsJson)
                    {
                        if (i.Value != null)
                        {
                            originalSteamIds.Add(i.Key, i.Value.ToString());
                            dontInstallIds.Add(i.Value.ToString());
                        }
                    }
                }
            }
            (DirectoryInfo directory, Modpack modpack) modpack = AddModpack(modpackFileName, updating);
            DirectoryInfo directory = modpack.directory;
            ZipFile.ExtractToDirectory(path, directory.FullName, true);
            DirectoryInfo modsFolder = new DirectoryInfo(Path.Combine(directory.FullName, "Mods"));
            if (modsFolder.Exists)
            {
                DirectoryInfo outputModsFolder = new DirectoryInfo(Path.Combine(directory.FullName, "instance", "Mods"));
                if (!outputModsFolder.Exists)
                {
                    Directory.CreateDirectory(outputModsFolder.FullName);
                }
                foreach (FileInfo i in modsFolder.GetFiles())
                {
                    if (i.Name == "steamids.json")
                    {
                        List<string> install = new List<string>();
                        JsonArray json = JsonObject.Parse(File.ReadAllText(i.FullName)) as JsonArray;
                        foreach (JsonNode? j in json)
                        {
                            if (j.GetValueKind() == JsonValueKind.String)
                            {
                                install.Add(j.ToString());
                            }
                        }
                        install.RemoveAll(dontInstallIds.Contains);
                        Dictionary<string, string> steamids = new Dictionary<string, string>();
                        if (install.Count > 0) steamids = ModInstallationHelper.InstallMods(modpack.modpack, install.ToArray());
                        foreach (var j in originalSteamIds)
                        {
                            steamids.TryAdd(j.Key, j.Value);
                        }
                        JsonObject steamidsJson = new JsonObject();
                        foreach (KeyValuePair<string, string> j in steamids)
                        {
                            steamidsJson.Add(j.Key, j.Value);
                        }
                        File.WriteAllText(Path.Combine(directory.FullName, i.Name), steamidsJson.ToJsonString());
                        continue;
                    }
                    i.CopyTo(Path.Combine(outputModsFolder.FullName, i.Name), true);
                }
                modsFolder.Delete(true);
            }
            DirectoryInfo modConfigsFolder = new DirectoryInfo(Path.Combine(directory.FullName, "ModConfigs"));
            if (modConfigsFolder.Exists)
            {
                DirectoryInfo instanceFolder = new DirectoryInfo(Path.Combine(directory.FullName, "instance"));
                DirectoryInfo outputModConfigsFolder = new DirectoryInfo(Path.Combine(instanceFolder.FullName, "ModConfigs"));
                if (outputModConfigsFolder.Exists)
                {
                    try
                    {
                        MiscUtil.DeleteDirectoryOnlyFiles(outputModConfigsFolder.FullName);
                    } catch (IOException e)
                    {
                        Console.WriteLine(e);
                    }
                }
                try
                {
                    MiscUtil.CopyDirectory(modConfigsFolder.FullName, Path.Combine(instanceFolder.FullName, modConfigsFolder.Name));
                    modConfigsFolder.Delete(true);
                }
                catch (IOException e)
                {
                    Console.WriteLine(e);
                }
            }
            return modpack;
        }
        public static (DirectoryInfo directory, Modpack modpack) AddModpack(string name, bool updating = false)
        {
            string folderName = "";
            foreach (char i in name)
            {
                if ("abcdefghijklmnopqrstuvwxyz1234567890 ".Contains(char.ToLower(i)))
                {
                    folderName += i;
                } else
                {
                    folderName += "_";
                }
            }
            if (!updating)
            {
                int tries = 0;
                while (true)
                {
                    string folderNameTry = folderName + (tries > 0 ? tries + 1 : "");
                    if (!Directory.Exists(Path.Combine(Settings.modpacksPath, folderNameTry)))
                    {
                        folderName = folderNameTry;
                        break;
                    }
                    tries++;
                }
            }
            string path = Path.Combine(Settings.modpacksPath, folderName);
            JsonObject json = new JsonObject();
            json.Add("name", name);
            json.Add("modlist", new JsonArray());
            JsonObject steamids = new JsonObject();
            DirectoryInfo directory = new DirectoryInfo(path);
            if (!directory.Exists) directory.Create();
            File.WriteAllText(Path.Combine(path, "modpack.json"), json.ToJsonString());
            if (!updating) File.WriteAllText(Path.Combine(path, "steamids.json"), steamids.ToJsonString());
            if (!Directory.Exists(Path.Combine(path, "instance"))) Directory.CreateDirectory(Path.Combine(path, "instance"));
            return (directory, new Modpack(name, directory.FullName, []));
        }
    }
}
