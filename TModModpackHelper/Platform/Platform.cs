using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TModModpackHelper.Platform
{
    public abstract class Platform
    {
        public abstract string GetSteamcmdInstallLink();
        public abstract string GetSteamcmdFile();
        public abstract void OpenFolder(string path);
    }
}
