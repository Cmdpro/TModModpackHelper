using Gdk;
using Gtk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace TModModpackHelper.Screens
{
    public class ModpackScreen : AbstractScreen
    {
        private ListBox? mods;
        private ComboBoxText? modpackSelect;
        static List<Modpack> modpackSelectModpacks = new List<Modpack>();
        ScrolledWindow modlistScroll;
        public ListBox GetMods()
        {
            if (widget == null)
            {
                Create();
            }
            return mods;
        }
        public ComboBoxText GetModpackSelect()
        {
            if (widget == null)
            {
                Create();
            }
            return modpackSelect;
        }
        public Modpack GetSelectedModpack()
        {
            return GetModpack(GetModpackSelect().Active);
        }
        public void SetSelectedModpack(Modpack modpack)
        {
            int index = modpackSelectModpacks.IndexOf(modpack);
            if (index == -1)
            {
                return;
            }
            modpackSelect.Active = index;
            Program.modpackScreen.SetToModlist(Program.modpackScreen.GetMods(), modpack);
        }
        public Modpack GetModpack(int index)
        {
            return modpackSelectModpacks[index];
        }
        protected override Fixed Create()
        {
            widget = new Fixed();

            modpackSelect = new ComboBoxText();
            BuildModpackSelection();
            modpackSelect.Changed += (sender, args) =>
            {
                if (mods == null || sender == null)
                {
                    return;
                }
                ComboBoxText box = (ComboBoxText)sender;
                int amount = box.Model.NColumns;
                if (amount > box.Active && box.Active >= 0)
                {
                    Modpack modpack = Program.GetCurrentModpack();
                    SetToModlist(mods, modpack);
                }
            };
            modpackSelect.Active = 0;
            modpackSelect.SetSizeRequest(150, 25);

            modlistScroll = new ScrolledWindow();
            mods = new ListBox();
            mods.SelectionMode = SelectionMode.None;
            modlistScroll.Add(mods);
            modlistScroll.SetSizeRequest(500, 300);
            SetToModlist(mods, Program.modpacks.Values.ToArray()[0]);
            widget.Put(modlistScroll, 300 - (modlistScroll.WidthRequest / 2), 150);

            Button playButton = new Button("Play");
            playButton.SetSizeRequest(150, 25);
            playButton.Clicked += (sender, args) =>
            {
                Program.GetCurrentModpack().Start();
            };

            widget.Put(playButton, 300 - (playButton.WidthRequest / 2), 75);
            widget.Put(modpackSelect, 300 - (modpackSelect.WidthRequest / 2), 20);

            Button importButton = new Button("New");
            importButton.WidthRequest = 100;
            importButton.Clicked += (sender, args) =>
            {
                Program.newModpackScreen.SetScreen();
            };
            widget.Put(importButton, 150 - (importButton.WidthRequest / 2), 20);

            MenuButton modpackActionButton = new MenuButton();
            modpackActionButton.Label = "Modpack";
            modpackActionButton.WidthRequest = 100;
            widget.Put(modpackActionButton, 450 - (modpackActionButton.WidthRequest / 2), 20);


            Dictionary<string, EventHandler> modpackActionButtons = new Dictionary<string, EventHandler>();

            modpackActionButtons.Add("Export", (sender, args) =>
            {
                PromptModpackExport();
            });
            modpackActionButtons.Add("Update Modpack", (sender, args) =>
            {
                Program.PromptModpackImport(new DirectoryInfo(Program.GetCurrentModpack().path).Name, true);
            });
            modpackActionButtons.Add("Update Mods", (sender, args) =>
            {
                Modpack modpack = Program.GetCurrentModpack();
                string steamIdsPath = Path.Combine(modpack.path, "steamids.json");
                if (File.Exists(steamIdsPath))
                {
                    List<string> install = new List<string>();
                    JsonObject idsJson = JsonObject.Parse(File.ReadAllText(steamIdsPath)) as JsonObject;
                    foreach (var i in idsJson)
                    {
                        if (i.Value != null) install.Add(i.Value.ToString());
                    }
                    Dictionary<string, string> steamids = ModInstallationHelper.InstallMods(modpack, install.ToArray());
                }
            });
            modpackActionButtons.Add("Add Enabled", (sender, args) =>
            {
                Modpack modpack = Program.GetCurrentModpack();
                string enabledPath = Path.Combine(modpack.path, "instance", "Mods", "enabled.json");
                if (File.Exists(enabledPath))
                {
                    JsonArray enabledJson = JsonArray.Parse(File.ReadAllText(enabledPath)) as JsonArray;
                    ModInstallationHelper.FindSubscribedModData();
                    JsonObject steamids = JsonObject.Parse(File.ReadAllText(Path.Combine(modpack.path, "steamids.json"))) as JsonObject;
                    JsonObject modpackJson = JsonObject.Parse(File.ReadAllText(Path.Combine(modpack.path, "modpack.json"))) as JsonObject;
                    JsonArray modlistJson = modpackJson["modlist"].AsArray();
                    foreach (JsonNode? i in enabledJson)
                    {
                        if (i == null)
                        {
                            continue;
                        }
                        string mod = i.ToString();
                        if (ModInstallationHelper.subscribedModData.ContainsKey(mod))
                        {
                            (string id, string path) data = ModInstallationHelper.subscribedModData[mod];
                            FileInfo file = new FileInfo(data.path);
                            string target = Path.Combine(modpack.path, "instance", "Mods", Path.GetFileNameWithoutExtension(file.Name) + ".tmod_inactive");
                            string tmodFile = Path.Combine(modpack.path, "instance", "Mods", file.Name);
                            if (!File.Exists(target) && !File.Exists(tmodFile)) File.Copy(file.FullName, target);
                            steamids.TryAdd(mod, data.id);
                            if (!modlistJson.Any((i) => i.ToString() == mod))
                            {
                                modlistJson.Add(mod);
                            }
                        }
                    }
                    File.WriteAllText(Path.Combine(modpack.path, "steamids.json"), steamids.ToJsonString());
                    File.WriteAllText(Path.Combine(modpack.path, "modpack.json"), modpackJson.ToJsonString());
                    Program.ReloadModpacks();
                    SetToModlist(GetMods(), Program.modpacks[new DirectoryInfo(modpack.path).Name]);
                }
            });
            modpackActionButtons.Add("Delete", (sender, args) =>
            {
                Modpack modpack = Program.GetCurrentModpack();
                Dialog popup = new Dialog(
                    "",
                    Program.window,
                    DialogFlags.Modal,
                    "Confirm", ResponseType.Accept,
                    "Cancel", ResponseType.Cancel
                );
                popup.WidthRequest = 300;
                Box area = popup.ContentArea;
                Button confirmButton = ((area.Children[0] as Box).Children[0] as ButtonBox).Children.First((i) => i is Button) as Button;
                string toType = modpack.name;
                Label label = new Label($"Please type \"{toType}\" to confirm deletion");
                label.Justify = Justification.Center;
                label.MarginBottom = 10;
                label.MarginTop = 10;
                area.Add(label);
                Entry entry = new Entry();
                entry.MarginBottom = 20;
                entry.MarginTop = 10;
                entry.Changed += (sender, args) =>
                {
                    confirmButton.Sensitive = entry.Text == toType;
                };
                confirmButton.Sensitive = false;
                area.Add(entry);
                area.ShowAll();
                int response = popup.Run();
                if (response == (int)ResponseType.Accept)
                {
                    if (entry.Text == toType)
                    {
                        try
                        {
                            MiscUtil.DeleteDirectoryOnlyFiles(modpack.path);
                            Directory.Delete(modpack.path, true);
                        } catch (IOException e)
                        {
                            Console.WriteLine(e);
                        }
                        Program.noModpacksScreen.SetScreen();
                        Program.ReloadModpacks();
                        BuildModpackSelection();
                        if (GetModpackSelect().Model.NColumns > 0) GetModpackSelect().Active = 0;
                    }
                }
                popup.Destroy();
            });
            modpackActionButtons.Add("Rename", (sender, args) =>
            {
                Modpack modpack = Program.GetCurrentModpack();
                Dialog popup = new Dialog(
                    "",
                    Program.window,
                    DialogFlags.Modal,
                    "Confirm", ResponseType.Accept,
                    "Cancel", ResponseType.Cancel
                );
                popup.WidthRequest = 300;
                Box area = popup.ContentArea;
                Button confirmButton = ((area.Children[0] as Box).Children[0] as ButtonBox).Children.First((i) => i is Button) as Button;
                Label label = new Label("What would you like to rename the modpack to?");
                label.Justify = Justification.Center;
                label.MarginBottom = 10;
                label.MarginTop = 10;
                area.Add(label);
                Entry entry = new Entry();
                entry.MarginBottom = 20;
                entry.MarginTop = 10;
                entry.Changed += (sender, args) =>
                {
                    confirmButton.Sensitive = entry.Text.Length > 0;
                };
                entry.Text = modpack.name;
                area.Add(entry);
                area.ShowAll();
                int response = popup.Run();
                if (response == (int)ResponseType.Accept)
                {
                    if (entry.Text.Length > 0)
                    {
                        string modpackName = entry.Text;
                        JsonObject modpackJson = JsonObject.Parse(File.ReadAllText(Path.Combine(modpack.path, "modpack.json"))) as JsonObject;
                        modpackJson["name"] = modpackName;
                        File.WriteAllText(Path.Combine(modpack.path, "modpack.json"), modpackJson.ToJsonString());
                        modpack.name = modpackName;
                        Program.ReloadModpacks();
                        BuildModpackSelection();
                        SetSelectedModpack(Program.modpacks[new DirectoryInfo(modpack.path).Name]);
                    }
                }
                popup.Destroy();
            });
            modpackActionButtons.Add("Open Folder", (sender, args) =>
            {
                Modpack modpack = Program.GetCurrentModpack();
                Program.platform.OpenFolder(modpack.path);
            });

            Menu modpackActions = new Menu();
            foreach (var i in modpackActionButtons)
            {
                MenuItem item = new MenuItem();
                item.Label = i.Key;
                item.Activated += i.Value;
                modpackActions.Add(item);
            }
            modpackActions.ShowAll();

            modpackActionButton.Popup = modpackActions;

            AddSettingsButton(widget);

            return widget;
        }
        public static void PromptModpackExport()
        {
            FileChooserDialog fileChooser = new FileChooserDialog(
                "Select where the file will be put",
                Program.window,
                FileChooserAction.Save,
                "Cancel", ResponseType.Cancel,
                "Save", ResponseType.Accept
            );
            FileFilter allFilesFilter = new FileFilter();
            allFilesFilter.Name = "Modpacks";
            allFilesFilter.AddPattern("*.zip");
            fileChooser.AddFilter(allFilesFilter);

            int response = fileChooser.Run();
            if (response == (int)ResponseType.Accept)
            {
                string file = fileChooser.Filename;
                fileChooser.Destroy();
                Program.GetCurrentModpack().Export(file);
            }
            else if (response == (int)ResponseType.Cancel)
            {
                fileChooser.Destroy();
            } else
            {
                fileChooser.Destroy();
            }
        }
        public void SetToModlist(ListBox box, Modpack modpack)
        {
            foreach (var i in box.Children)
            {
                box.Remove(i);
            }
            foreach (Mod i in modpack.modlist)
            {
                Fixed mod = new Fixed();
                mod.HeightRequest = 40;
                Label label = new Label(i.name + "  |  " + (i.steamId == null ? "Local" : i.steamId));
                label.HeightRequest = 25;
                label.Justify = Justification.Left;
                mod.Put(label, 30, (mod.HeightRequest/2)-(label.HeightRequest/2));
                CheckButton active = new CheckButton();
                active.WidthRequest = 20;
                active.HeightRequest = 20;
                active.Active = i.enabled;
                active.Clicked += (sender, args) =>
                {
                    modpack.SetModEnabled(i, active.Active);
                };
                mod.Put(active, 5, (mod.HeightRequest / 2) - (active.HeightRequest / 2));
                Button delete = new Button();
                delete.WidthRequest = 50;
                delete.HeightRequest = 35;
                delete.Clicked += (sender, args) =>
                {
                    string modsPath = Path.Combine(modpack.path, "instance", "Mods");
                    string enabledPath = Path.Combine(modsPath, "enabled.json");
                    JsonArray enabledJson = JsonArray.Parse(File.ReadAllText(enabledPath)) as JsonArray;
                    JsonObject steamids = JsonObject.Parse(File.ReadAllText(Path.Combine(modpack.path, "steamids.json"))) as JsonObject;
                    JsonObject modpackJson = JsonObject.Parse(File.ReadAllText(Path.Combine(modpack.path, "modpack.json"))) as JsonObject;
                    JsonArray modlistJson = modpackJson["modlist"].AsArray();

                    if (File.Exists(Path.Combine(modsPath, i.name + ".tmod")))
                    {
                        File.Delete(Path.Combine(modsPath, i.name + ".tmod"));
                    }
                    if (File.Exists(Path.Combine(modsPath, i.name + ".tmod_inactive")))
                    {
                        File.Delete(Path.Combine(modsPath, i.name + ".tmod_inactive"));
                    }
                    if (enabledJson.Any((j) => j.ToString() == i.name))
                    {
                        enabledJson.Remove(enabledJson.First((j) => j.ToString() == i.name));
                    }
                    if (modlistJson.Any((j) => j.ToString() == i.name))
                    {
                        modlistJson.Remove(modlistJson.First((j) => j.ToString() == i.name));
                    }
                    if (steamids.ContainsKey(i.name))
                    {
                        steamids.Remove(i.name);
                    }

                    File.WriteAllText(Path.Combine(modpack.path, "steamids.json"), steamids.ToJsonString());
                    File.WriteAllText(Path.Combine(modpack.path, "modpack.json"), modpackJson.ToJsonString());
                    File.WriteAllText(enabledPath, enabledJson.ToJsonString());
                    Program.ReloadModpacks();
                    SetToModlist(GetMods(), Program.modpacks[new DirectoryInfo(modpack.path).Name]);
                };
                delete.Label = "X";
                mod.Put(delete, (modlistScroll.WidthRequest-25)-(delete.WidthRequest), (mod.HeightRequest / 2) - (delete.HeightRequest / 2));
                box.Add(mod);
            }
            box.ShowAll();
        }
        public void BuildModpackSelection()
        {
            modpackSelect.RemoveAll();
            modpackSelectModpacks.Clear();
            foreach (Modpack i in Program.modpacks.Values)
            {
                modpackSelect.AppendText(i.name);
                modpackSelectModpacks.Add(i);
            }
        }
    }
}
