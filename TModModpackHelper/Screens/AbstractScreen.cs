using Gtk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TModModpackHelper.Screens
{
    public abstract class AbstractScreen
    {
        public bool HasCreated { 
            get {
                return widget != null;
            }
        }
        protected Fixed? widget;
        protected abstract Fixed Create();
        public Fixed GetWidget()
        {
            if (widget == null)
            {
                widget = Create();
            }
            return widget;
        }
        public void SetScreen(Window window)
        {
            Fixed widget = GetWidget();
            widget.ShowAll();
            if (window.Child != widget)
            {
                if (window.Children.Length > 0)
                {
                    window.Remove(window.Child);
                }
                window.Add(widget);
            }
        }
        public void SetScreen()
        {
            SetScreen(Program.window);
        }
        protected void AddBackButton(Fixed widget, int x = 10, int y = 10)
        {
            Button backButton = new Button();
            backButton.Clicked += (sender, args) =>
            {
                if (Program.modpacks.Count > 0)
                {
                    Program.modpackScreen.SetScreen();
                }
                else
                {
                    Program.noModpacksScreen.SetScreen();
                }
            };
            backButton.Label = "Back";
            widget.Put(backButton, x, y);
        }
        protected void AddSettingsButton(Fixed widget, int x = 10, int y = 10)
        {
            Button settingsButton = new Button();
            settingsButton.Clicked += (sender, args) =>
            {
                Program.settingsScreen.SetScreen();
            };
            settingsButton.Label = "Settings";
            widget.Put(settingsButton, x, y);
        }
    }
}
