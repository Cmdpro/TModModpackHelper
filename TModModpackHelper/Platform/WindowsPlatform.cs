using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TModModpackHelper.Platform
{
    public class WindowsPlatform : Platform
    {
        public override string GetSteamcmdFile()
        {
            return "steamcmd.exe";
        }

        public override string GetSteamcmdInstallLink()
        {
            return "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip";
        }

        public override void OpenFolder(string path)
        {
            Process.Start("explorer.exe", path);
        }
    }
}
