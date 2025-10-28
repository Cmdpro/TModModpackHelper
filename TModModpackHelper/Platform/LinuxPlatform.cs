using Gtk;
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
        public override void OpenFolder(string path)
        {
            Process.Start("xdg-open", path);
        }

        public override void SteamCmdCheck()
        {
            if (!IsSteamCmdInstalled())
            {
                Dialog popup = new Dialog(
                    "",
                    Program.window,
                    DialogFlags.Modal,
                    "Check", ResponseType.Accept,
                    "Cancel", ResponseType.Cancel
                );
                popup.WidthRequest = 300;
                Box area = popup.ContentArea;
                Button confirmButton = ((area.Children[0] as Box).Children[0] as ButtonBox).Children.First((i) => i is Button) as Button;
                Label label = new Label($"You must install \"steamcmd\"");
                label.Justify = Justification.Center;
                label.MarginBottom = 10;
                label.MarginTop = 10;
                area.Add(label);
                area.ShowAll();
                int response = popup.Run();
                if (response == (int)ResponseType.Accept)
                {
                    if (IsSteamCmdInstalled())
                    {
                        popup.Destroy();
                    }
                } else
                {
                    popup.Destroy();
                    Environment.Exit(0);
                }
            }
            ModInstallationHelper.steamcmd = "steamcmd";
        }
        public bool IsSteamCmdInstalled()
        {
            Process process = Process.Start("which", "steamcmd");
            process.WaitForExit();
            return process.StandardOutput.ReadToEnd().EndsWith("/steamcmd");
        }
    }
}
