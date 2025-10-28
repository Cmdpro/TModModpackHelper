using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TModModpackHelper.Platform
{
    public abstract class Platform
    {
        public abstract void SteamCmdCheck();
        public abstract void OpenFolder(string path);
    }
}
