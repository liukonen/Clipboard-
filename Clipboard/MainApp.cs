using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Linq;
using System.Reflection;

namespace Clipboard
{

    public sealed class MainApp : IDisposable
    {
        #region "Globals"
        private NotifyIcon notifyIcon;
        private List<ClipDataObject> CacheHandler = new List<ClipDataObject>();
        private ClipboardManager Manager;
        private int _totalCount = 9;
        private Boolean _useEncryption = false;
        private Boolean _saveOnExit;
        #endregion

        #region Properties
        private int TotalCount { get { return _totalCount; } set { _totalCount = value; Shared.AddUpdateAppSettings(Shared.cNumberOfSupportedCaches, value.ToString()); } }
        private Boolean UseEncryption { get { return _useEncryption; } set { _useEncryption = value; Shared.AddUpdateAppSettings(Shared.cUseSaveEncryption, value.ToString()); } }
        private Boolean SaveOnExit { get { return _saveOnExit; } set { _saveOnExit = value; Shared.AddUpdateAppSettings(Shared.cExitCaching, value.ToString()); } }
        #endregion

        #region "Event Handles"

        /// <summary>
        /// Event Load
        /// </summary>
        public MainApp()
        {
            LoadSettings();
            LoadCacheItemsFromDisk();
            notifyIcon = new NotifyIcon { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath), Text = "Clipboard++", ContextMenuStrip = ConstructContextMenu() };
            Manager = new ClipboardManager();
            Manager.ClipboardChanged += OnClipboardChangeEvent;
        }

        /// <summary>
        /// Event Clipboard Change Event
        /// </summary>
        /// <param name="sender">ClipboardManager Manager</param>
        /// <param name="e">Clipboard Changed Event Args</param>
        private void OnClipboardChangeEvent(object sender, ClipboardChangedEventArgs e)
        {
            AddItemToCache(e._data);
            notifyIcon.ContextMenuStrip = ConstructContextMenu();
        }

        /// <summary>
        /// Handles the Pause / Resume Menu Button
        /// </summary>
        /// <param name="sender">Settings.ToolStripButton PauseResume</param>
        /// <param name="e"></param>
        private void PauseResume_ClickEvent(object sender, EventArgs e)
        {
            ToolStripButton current = ((ToolStripButton)sender);
            if (current.Text == "Pause")
            {
                Manager.Stop();
                current.Text = "Resume";
            }
            else { Manager.Start(); current.Text = "Pause"; }
        }
 
        /// <summary>
        /// Handles the Cached Item Save button event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ItemSaveClickEvent(object sender, EventArgs e)
        {
            var X = (from y in CacheHandler
                     where y.Key == ((ToolStripLabel)sender).Name.Substring(4)
                     select y).First();
            System.Windows.Forms.Clipboard.Clear();
            Shared.SaveAsObject(X.ClipboardObject());
        }

        /// <summary>
        /// Handles the Cached Item Copy Event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ItemClickEvent(object sender, EventArgs e)
        {
            Manager.Stop();

            var X = (from y in CacheHandler
                     where y.Key == ((ToolStripLabel)sender).Name
                     select y).First();
            System.Windows.Forms.Clipboard.Clear();
            System.Windows.Forms.Clipboard.SetDataObject(X.ClipboardObject());
            Manager.Start();
        }

        /// <summary>
        /// Handles the Settings.Save Event
        /// </summary>
        /// <param name="sender">ToolStripMenuItem save</param>
        /// <param name="e"></param>
        private void SaveOnExitClickEvent(object sender, EventArgs e)
        {
            SaveOnExit = !SaveOnExit;
            ((ToolStripMenuItem)sender).Checked = SaveOnExit;
        }

        /// <summary>
        /// Handles the Settings.Encrypt Event
        /// </summary>
        /// <param name="sender">ToolStripMenuItem Encrpyt</param>
        /// <param name="e"></param>
        private void EncryptClickEvent(object sender, EventArgs e)
        {
            UseEncryption = !UseEncryption;
            ((ToolStripMenuItem)sender).Checked = UseEncryption;
        }

        /// <summary>
        /// Handles the Menu Exit Event
        /// </summary>
        /// <param name="sender">ToolStripLabel "Exit"</param>
        /// <param name="e"></param>
        private void MenuExitClick(object sender, EventArgs e) { Application.Exit(); }

        /// <summary>
        /// Handles the Menu About Event
        /// </summary>
        /// <param name="sender">ToolStripLabel "About"</param>
        /// <param name="e"></param>
        private void MenuAboutClick(object sender, EventArgs e)
        {
            string Name = this.GetType().Assembly.GetName().Name;

            System.Text.StringBuilder message = new System.Text.StringBuilder();

            string name = ((AssemblyTitleAttribute)this.GetType().Assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false)[0]).Title;
            string Copyright = ((AssemblyCopyrightAttribute)this.GetType().Assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false)[0]).Copyright;
            string Version = this.GetType().Assembly.GetName().Version.ToString();

            message.Append(name).Append(Environment.NewLine).Append(Copyright).Append(Environment.NewLine).Append(Environment.NewLine).Append(Version);
            MessageBox.Show(message.ToString(), Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Handles the Settings Clear Event
        /// </summary>
        /// <param name="sender">ToolStripLabel Clear</param>
        /// <param name="e"></param>
        private void ClearClickEvent(object sender, EventArgs e) {
            CacheHandler.Clear();
            notifyIcon.ContextMenuStrip = ConstructContextMenu(); }
  
        /// <summary>
        /// Updates the Max File Size you can save in cache (max 50mb)
        /// </summary>
        /// <param name="sender">ToolStripMenuItem setFileSize </param>
        /// <param name="e"></param>
        private void UpdateMaxFileSize(object sender, EventArgs e)
        {
            var box = Microsoft.VisualBasic.Interaction.InputBox("Please enter max size of individual items in file storage to use. Use MB or KB at the end of the number for easy entry.", "Max File Size", "50MB");
            if (Shared.ValidateMaxFileSize(box))
            {
                MessageBox.Show("Settings will take place on restart of application", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Shared.AddUpdateAppSettings(Shared.cMaxFileSize, Shared.ConvertFileSize(box).ToString());
            }
            else { MessageBox.Show("Value was incorrect, please try again", "Invalid", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        /// <summary>
        /// Updates the Max Cache Size you can save (max 500mb)
        /// </summary>
        /// <param name="sender">ToolStripMenuItem setCacheSize </param>
        /// <param name="e"></param>
        private void UpdateMaxCacheSize(object sender, EventArgs e)
        {
            string box = Microsoft.VisualBasic.Interaction.InputBox("Please enter max size of total file size to use. Use MB or KB at the end of the number for easy entry.", "Max File Size", "500MB");
            if (Shared.ValidateMaxCacheSize(box))
            {
                MessageBox.Show("Settings will take place on restart of application", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Shared.AddUpdateAppSettings(Shared.cMaxCacheSize, box);
                ((ToolStripMenuItem)sender).Text = string.Format(Shared.cFormatCacheSizeMenu, box);

            }

            else { MessageBox.Show("Value was incorrect, please try again", "Invalid", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
        #endregion

        #region "Functions and Methods"

        /// <summary>
        /// On the Clipboard event, this function puts the clip object into the CacheHandler List
        /// </summary>
        /// <param name="obj"></param>
        private void AddItemToCache(IDataObject obj)
        {
            string[] ActiveCache = (from Ca in CacheHandler select Ca.Key).ToArray();
            ClipDataObject dataObject = new ClipDataObject(obj, ActiveCache);
            if (!string.IsNullOrEmpty(dataObject.Key))
            {
                CacheHandler.Insert(0, dataObject);
                if (CacheHandler.Count > TotalCount) { CacheHandler.Remove(CacheHandler.Last()); GC.Collect(); }
            }
        }

        /// <summary>
        /// On Application Load, this method refreshes the global app settings
        /// </summary>
        private void LoadSettings()
        {
            Boolean.TryParse(Shared.cExitCaching, out _saveOnExit);
            Boolean.TryParse(Shared.ReadAppSetting(Shared.cUseSaveEncryption), out _useEncryption);
            if (!int.TryParse(Shared.ReadAppSetting(Shared.cNumberOfSupportedCaches), out _totalCount) || _totalCount > 10 || _totalCount < 0) { _totalCount = 9; }
        }

        /// <summary>
        /// On Load, call to attempt to load from disk
        /// </summary>
        private void LoadCacheItemsFromDisk()
        {
            try
            {

                if (Shared.ExitCaching())
                {
                    using (System.IO.MemoryStream MS = new System.IO.MemoryStream(System.IO.File.ReadAllBytes("cache.mp")))
                    {
                        if (CacheHandler == null) { CacheHandler = new List<ClipDataObject>(); }
                        var msgPackSerializer = MsgPack.Serialization.MessagePackSerializer.Get<List<SerializableClipObject>>();

                        List<SerializableClipObject> DeSerializedObject;
                        if (UseEncryption) {
                            using (System.IO.MemoryStream UnEncrypted = new System.IO.MemoryStream(Shared.DecryptClipObject(MS.ToArray())))
                            {
                                DeSerializedObject = msgPackSerializer.Unpack(UnEncrypted);
                            }

                        }
                        else { DeSerializedObject = msgPackSerializer.Unpack(MS); }
                        foreach (var item in DeSerializedObject)
                        {
                            CacheHandler.Add(new ClipDataObject(item));
                        }
                    }
                }

            }
            catch { }
        }

        /// <summary>
        /// On Exit,attempt to save to cache file
        /// </summary>
        public void SaveCacheItemsToDisk()
        {
            try
            {
                if (Shared.ExitCaching())
                {
                    int MaxFileSize = Shared.MaxCacheSize();
                    List<SerializableClipObject> SaveObject = new List<SerializableClipObject>();
                    int counter = 0;
                    foreach (var item in CacheHandler)
                    {
                        if (MaxFileSize > item.FileSize + counter)
                        {
                            counter += (int)item.FileSize;
                            SaveObject.Add(item.ToOjbect());
                        }
                    }

                    using (System.IO.MemoryStream MS = new System.IO.MemoryStream())
                    {

                        var X = MsgPack.Serialization.MessagePackSerializer.Get<List<SerializableClipObject>>();
                        X.Pack(MS, SaveObject);
                        if (UseEncryption)
                        { System.IO.File.WriteAllBytes("Cache.mp", Shared.EncryptClipObject(MS.ToArray())); }
                        else { System.IO.File.WriteAllBytes("Cache.mp", MS.ToArray()); }                      
                    }
                }
            }
            catch { }

        }

        #endregion

        #region "Menu Construction"

        /// <summary>
        /// Build Base Menu
        /// </summary>
        /// <returns></returns>
        public ContextMenuStrip ConstructContextMenu()
        {
            ContextMenuStrip value = new ContextMenuStrip();
            ToolStripItem[] items = CachedItems();
            if (items != null) { value.Items.AddRange(items); }
            value.Items.Add(new ToolStripSeparator());

            if (items != null)
            {
                value.Items.Add(new ToolStripSeparator());
            }
            value.Items.Add(SettingsMenu());
            value.Items.Add(new ToolStripLabel("About", null, false, MenuAboutClick));
            value.Items.Add(new ToolStripLabel("Exit", null, false, MenuExitClick));
            return value;
        }

        /// <summary>
        /// Handles construction of the Settings Menu
        /// </summary>
        /// <returns></returns>
        private ToolStripMenuItem SettingsMenu()
        {
            ToolStripMenuItem SettingsMenuItem = new ToolStripMenuItem() { Text = "Settings" };

            ToolStripButton PauseResume = new ToolStripButton("Pause", null, PauseResume_ClickEvent);
            SettingsMenuItem.DropDownItems.Add(PauseResume);

            ToolStripLabel Clear = new ToolStripLabel("Clear Items");
            Clear.Click += ClearClickEvent;
            SettingsMenuItem.DropDownItems.Add(Clear);

            SettingsMenuItem.DropDownItems.Add(new ToolStripSeparator());

            ToolStripMenuItem save = new ToolStripMenuItem("Save Cache On Exit") { Checked = Shared.ExitCaching() };
            save.Click += SaveOnExitClickEvent;
            SettingsMenuItem.DropDownItems.Add(save);

            ToolStripMenuItem Encrpyt = new ToolStripMenuItem("Encrpyt Saved Items") { Checked = UseEncryption };
            Encrpyt.Click += EncryptClickEvent;
            SettingsMenuItem.DropDownItems.Add(Encrpyt);

            SettingsMenuItem.DropDownItems.Add(new ToolStripSeparator());

            ToolStripMenuItem setCacheSize = new ToolStripMenuItem(string.Format(Shared.cFormatCacheSizeMenu, Shared.MaxCacheSize().ToString()));
            setCacheSize.Click += UpdateMaxCacheSize;
            SettingsMenuItem.DropDownItems.Add(setCacheSize);

            ToolStripMenuItem setFileSize = new ToolStripMenuItem(string.Format(Shared.cFormatFileSizeMenu, Shared.MaxFileSize().ToString()));
            setFileSize.Click += UpdateMaxFileSize;
            SettingsMenuItem.DropDownItems.Add(setFileSize);

            return SettingsMenuItem;

        }

        /// <summary>
        /// Handles the construction of the Cached Items menu
        /// </summary>
        /// <returns></returns>
        private ToolStripItem[] CachedItems()
        {
            List<ToolStripMenuItem> output = new List<ToolStripMenuItem>();

            foreach (var item in CacheHandler)
            {
                //item.Label.Click += ItemClickEvent;
                if (item.Label.DropDownItems.Count > 0)
                {
                    item.Label.DropDownItems[0].Click -= ItemClickEvent;
                    item.Label.DropDownItems[0].Click += ItemClickEvent;
                    item.Label.DropDownItems[1].Click -= ItemSaveClickEvent;
                    item.Label.DropDownItems[1].Click += ItemSaveClickEvent;
                }
                //item.Label.Click -= ItemClickEvent;
                //item.Label.Click += ItemClickEvent;

                output.Add(item.Label);
            }
            if (output.Count > 0) { return output.ToArray(); }
            return null;
        }

        #endregion

        #region IDisposable Support
        private bool disposedValue = false;

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    disposedValue = true;
                }
            }
        }

        // This code added to correctly implement the disposable pattern.
        void IDisposable.Dispose() { Dispose(true); }
        #endregion

        #region Main - Program entry point
        /// <summary>Program entry point.</summary>
        /// <param name="args">Command Line Arguments</param>
        [STAThread]
        public static void Main()
        {

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool isFirstInstance = false;
            using (Mutex mtx = new Mutex(true, "Clipboard++", out isFirstInstance))
            {
                if (isFirstInstance)
                {
                    try
                    {
                        MainApp notificationIcon = new MainApp();
                        notificationIcon.notifyIcon.Visible = true;
                        GC.Collect();
                        Application.Run();
                        notificationIcon.SaveCacheItemsToDisk();
                        notificationIcon.notifyIcon.Dispose();
                    }
                    catch (Exception x)
                    {
                        MessageBox.Show("Error: " + x.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    mtx.ReleaseMutex();
                }
                else
                {
                    GC.Collect();
                    MessageBox.Show("App appears to be running. if not, you may have to restart your machine to get it to work.");
                }
            }



        }
        #endregion

    }
    
}
