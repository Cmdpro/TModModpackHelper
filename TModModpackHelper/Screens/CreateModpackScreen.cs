using Gtk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TModModpackHelper.Screens
{
    public class CreateModpackScreen : AbstractScreen
    {
        protected override Fixed Create()
        {
            widget = new Fixed();

            (int x, int y) nameFieldPos = (0, 225);
            Label nameText = new Label("Modpack Name");
            nameText.WidthRequest = 300;
            nameText.Justify = Justification.Center;
            widget.Put(nameText, (300 - (nameText.WidthRequest / 2)) + nameFieldPos.x, nameFieldPos.y-25);
            Entry nameEntry = new Entry();
            nameEntry.WidthRequest = 200;
            widget.Put(nameEntry, (300 - (nameEntry.WidthRequest / 2)) + nameFieldPos.x, nameFieldPos.y);

            Button createButton = new Button("Create");
            createButton.WidthRequest = 100;
            createButton.Clicked += (sender, args) =>
            {
                if (nameEntry.Text.Length > 0)
                {
                    Modpack modpack = Program.AddModpack(nameEntry.Text).modpack;
                    nameEntry.Text = "";
                    Program.ReloadModpacks();
                    Program.modpackScreen.SetScreen();
                    Program.modpackScreen.BuildModpackSelection();
                    Program.modpackScreen.SetSelectedModpack(Program.modpacks[new DirectoryInfo(modpack.path).Name]);
                }
            };
            widget.Put(createButton, 300 - (createButton.WidthRequest / 2), 275);
            AddBackButton(widget);
            return widget;
        }
    }
}
