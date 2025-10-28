using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TModModpackHelper.Platform
{
    public class WindowsPlatform : Platform
    {
        public string GetSteamcmdFile()
        {
            return "steamcmd.exe";
        }

        public string GetSteamcmdInstallLink()
        {
            return "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip";
        }
        public override void SteamCmdCheck()
        {
            string steamcmdDirectory = Path.Combine(Settings.dataPath, "steamcmd");
            if (!Directory.Exists(steamcmdDirectory))
            {
                string steamcmdZip = Path.Combine(Settings.dataPath, "steamcmd.zip");
                Directory.CreateDirectory(steamcmdDirectory);
                using (HttpClient client = new HttpClient())
                {
                    Task<HttpResponseMessage> task = client.GetAsync(GetSteamcmdInstallLink(), HttpCompletionOption.ResponseHeadersRead);
                    task.Wait();
                    HttpResponseMessage response = task.Result;
                    response.EnsureSuccessStatusCode();
                    using (var streamToReadFrom = response.Content.ReadAsStream())
                    using (var streamToWriteTo = new FileStream(steamcmdZip, FileMode.Create))
                    {
                        streamToReadFrom.CopyTo(streamToWriteTo);
                    }
                }
                ZipFile.ExtractToDirectory(steamcmdZip, steamcmdDirectory);
                File.Delete(steamcmdZip);
            }
            ModInstallationHelper.steamcmd = Path.Combine(steamcmdDirectory, GetSteamcmdFile());
        }

        public override void OpenFolder(string path)
        {
            Process.Start("explorer.exe", path);
        }
    }
}
