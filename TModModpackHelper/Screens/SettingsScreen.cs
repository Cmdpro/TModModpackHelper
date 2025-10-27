using Gtk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TModModpackHelper.Screens
{
    public class SettingsScreen : AbstractScreen
    {
        Button? steamPathButton;
        protected override Fixed Create()
        {
            widget = new Fixed();
            (Fixed group, Button button, Label label) steamPathData = CreateDirectorySelectButton("Steam Path", Settings.steamPath, 450, 25, (path) =>
            {
                Settings.steamPath = path;
                Settings.SaveSettings();
                steamPathButton.Label = path;
            });
            steamPathButton = steamPathData.button;
            widget.Put(steamPathData.group, 300 - (steamPathButton.WidthRequest / 2), 225);
            AddBackButton(widget);
            return widget;
        }
        private (Fixed group, Button button, Label label) CreateDirectorySelectButton(string name, string buttonText, int width, int height, Action<string> selected)
        {
            Fixed group = new Fixed();
            group.WidthRequest = width;
            group.HeightRequest = height + 25;
            Button button = new Button();
            button.Label = buttonText;
            button.WidthRequest = width;
            button.HeightRequest = height;
            button.Clicked += (sender, args) =>
            {
                SelectDirectory(selected);
            };
            group.Put(button, 0, 25);
            Label label = new Label();
            label.Text = name;
            label.WidthRequest = width;
            group.Put(label, 0, 0);
            return (group, button, label);
        }
        private void SelectDirectory(Action<string> selected)
        {
            FileChooserDialog fileChooser = new FileChooserDialog(
                "Select where the file will be put",
                Program.window,
                FileChooserAction.SelectFolder,
                "Cancel", ResponseType.Cancel,
                "Select", ResponseType.Accept
            );

            int response = fileChooser.Run();
            if (response == (int)ResponseType.Accept)
            {
                string file = fileChooser.Filename;
                fileChooser.Destroy();
                selected.Invoke(file);
            }
            else if (response == (int)ResponseType.Cancel)
            {
                fileChooser.Destroy();
            }
            else
            {
                fileChooser.Destroy();
            }
        }
    }
}
