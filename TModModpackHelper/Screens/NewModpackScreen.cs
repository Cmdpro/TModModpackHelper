using Gtk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TModModpackHelper.Screens
{
    public class NewModpackScreen : AbstractScreen
    {
        protected override Fixed Create()
        {
            widget = new Fixed();
            Label noModpackText = new Label("Would you like to create or import a modpack?");
            noModpackText.WidthRequest = 300;
            noModpackText.Justify = Justification.Center;
            widget.Put(noModpackText, 300 - (noModpackText.WidthRequest / 2), 150);
            Button createButton = new Button("Create");
            createButton.WidthRequest = 100;
            createButton.Clicked += (sender, args) =>
            {
                Program.createModpackScreen.SetScreen();
            };
            widget.Put(createButton, 200 - (createButton.WidthRequest / 2), 300);
            Button importButton = new Button("Import");
            importButton.WidthRequest = 100;
            importButton.Clicked += (sender, args) =>
            {
                Program.PromptModpackImport();
            };
            widget.Put(importButton, 400 - (importButton.WidthRequest / 2), 300);
            AddBackButton(widget);
            return widget;
        }
    }
}
