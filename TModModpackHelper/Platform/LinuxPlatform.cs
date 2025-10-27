using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TModModpackHelper.Platform
{
    public class LinuxPlatform : Platform
    {
        public override string GetSteamcmdFile()
        {
            return "steamcmd_linux.tar";
        }

        public override string GetSteamcmdInstallLink()
        {
            return "https://steamcdn-a.akamaihd.net/client/installer/steamcmd_linux.tar.gz";
        }
        public override void OpenFolder(string path)
        {
            Process.Start("xdg-open", path);
        }
    }
}
