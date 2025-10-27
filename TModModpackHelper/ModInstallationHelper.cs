using Gtk;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace TModModpackHelper
{
    public class ModInstallationHelper
    {
        public static Dictionary<string, (string id, string path)> subscribedModData = new Dictionary<string, (string id, string path)>();
        public static string steamcmd;
        public static void FindSubscribedModData()
        {
            Dictionary<string, (string id, string path, int year, int month)> mods = new Dictionary<string, (string id, string path, int year, int month)>();
            DirectoryInfo directory = new DirectoryInfo(Path.Combine(Settings.steamPath, "steamapps", "workshop", "content", Settings.appid));
            if (directory.Exists)
            {
                foreach (DirectoryInfo i in directory.EnumerateDirectories())
                {
                    string modId = i.Name;
                    foreach (DirectoryInfo j in i.EnumerateDirectories())
                    {
                        foreach (FileInfo k in j.EnumerateFiles())
                        {
                            if (k.Extension == ".tmod")
                            {
                                string modName = Path.GetFileNameWithoutExtension(k.Name);
                                string[] modCreation = j.Name.Split('.');
                                if (modCreation.Length < 2)
                                {
                                    continue;
                                }
                                int year = -1;
                                int.TryParse(modCreation[0], out year);
                                int month = -1;
                                int.TryParse(modCreation[1], out month);
                                if (year == -1 || month == -1)
                                {
                                    continue;
                                }
                                (string id, string path, int year, int month) data = (modId, k.FullName, year, month);
                                if (!mods.TryAdd(modName, data))
                                {
                                    var existing = mods[modName];
                                    if (existing.year < data.year)
                                    {
                                        mods[modName] = data;
                                    }
                                    else if (existing.year == data.year && existing.month < data.month)
                                    {
                                        mods[modName] = data;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            Dictionary<string, (string id, string path)> modData = mods.Select((i) => KeyValuePair.Create(i.Key, (i.Value.id, i.Value.path))).ToDictionary();
            ModInstallationHelper.subscribedModData = modData;
        }
        public static void InstallMod(Modpack modpack, string id)
        {
            InstallMods(modpack, [id]);
        }
        public static Dictionary<string, string> InstallMods(Modpack modpack, string[] ids)
        {
            Dictionary<string, string> modSteamIds = new Dictionary<string, string>();
            try
            {
                string downloadsPath = Path.Combine(modpack.path, "Downloads");
                if (Directory.Exists(downloadsPath))
                {
                    Directory.Delete(downloadsPath, true);
                }
                Directory.CreateDirectory(downloadsPath);
                string command = string.Join(' ', ids.Select(i => $"+workshop_download_item {Settings.appid} {i}"));
                var startInfo = new ProcessStartInfo
                {
                    FileName = steamcmd,
                    Arguments = $"+force_install_dir \"{downloadsPath}\" +login anonymous { command} +quit",
                    WorkingDirectory = modpack.path,
                    UseShellExecute = false,
                    RedirectStandardOutput = false,
                    CreateNoWindow = false
                };

                DirectoryInfo downloaded = new DirectoryInfo(Path.Combine(downloadsPath, "steamapps", "workshop", "content", Settings.appid));
                using (Process process = Process.Start(startInfo))
                {
                    process.WaitForExit();
                }

                foreach (DirectoryInfo i in downloaded.EnumerateDirectories())
                {
                    int year = 0;
                    int month = 0;
                    DirectoryInfo? finalDirectory = null;
                    foreach (DirectoryInfo j in i.EnumerateDirectories())
                    {
                        string[] split = j.Name.Split('.');
                        if (split.Length < 2)
                        {
                            continue;
                        }
                        int dirYear = -1;
                        int.TryParse(split[0], out dirYear);
                        int dirMonth = -1;
                        int.TryParse(split[1], out dirMonth);
                        if (dirYear > year)
                        {
                            year = dirYear;
                            month = dirMonth;
                            finalDirectory = j;
                        }
                        if (dirYear == year && dirMonth > month)
                        {
                            month = dirMonth;
                            finalDirectory = j;
                        }
                    }
                    if (finalDirectory != null)
                    {
                        string modsPath = Path.Combine(modpack.path, "instance", "Mods");
                        foreach (FileInfo j in finalDirectory.EnumerateFiles())
                        {
                            modSteamIds.TryAdd(Path.GetFileNameWithoutExtension(j.Name), i.Name);
                            j.CopyTo(Path.Combine(modsPath, j.Name), true);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to install mods: {e.Message}");
            }
            return modSteamIds;
        }
    }
}
