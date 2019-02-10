using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Linq;
using System.Security.Cryptography;
using MsgPack;

namespace Clipboard
{

    public sealed class MainApp : IDisposable
    {
        #region "Globals"
        private NotifyIcon notifyIcon;

        private List<ClipDataObject> CacheHandler = new List<ClipDataObject>();
        private ClipboardManager Manager;
        private string ActiveKey = string.Empty;
        #endregion

        #region Properties
        private int TotalCount
        {
            get
            {
                string itemRead = Shared.ReadAppSetting("NumberOfSupportedCaches");
                //if we can't properly parse, its bigger then 10, or less then zero, fall back to 9
                if (!int.TryParse(itemRead, out int recordCount) || recordCount > 10 || recordCount < 0) { recordCount = 9; }
                return recordCount;
            }
        }
        #endregion

        #region "Event Handles"

        public MainApp()
        {
            LoadCacheItemsFromDisk();
            notifyIcon = new NotifyIcon
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath),
                Text = "Clipboard++",
                ContextMenuStrip = ConstructContextMenu()
            };

            Manager = new ClipboardManager();
            Manager.ClipboardChanged += OnTimedEvent;
        }

        private void OnTimedEvent(object sender, ClipboardChangedEventArgs e)
        {
            AddItemToCache(e._data);
            notifyIcon.ContextMenuStrip = ConstructContextMenu();
        }


        public ContextMenuStrip ConstructContextMenu()
        {
            ContextMenuStrip value = new ContextMenuStrip();
            ToolStripItem[] items = CachedItems();
            if (items != null) { value.Items.AddRange(items); }
            value.Items.Add(new ToolStripSeparator());

            if (items != null)
                //TODO: implement new save functionality//{ value.Items.Add(new ToolStripMenuItem("Save", null, SaveCachedItems())); }
                value.Items.Add(new ToolStripSeparator());
            //TODO: Implement Settings functionality

            value.Items.Add(SettingsMenu());
            value.Items.Add(new ToolStripLabel("About", null, false, MenuAboutClick));
            value.Items.Add(new ToolStripLabel("Exit", null, false, MenuExitClick));
            return value;

        }


        private ToolStripMenuItem SettingsMenu()
        {
            ToolStripMenuItem SettingsMenuItem = new ToolStripMenuItem() { Text = "Settings" };

            ToolStripButton PauseResume = new ToolStripButton("Pause", null, PauseResume_ClickEvent);
            SettingsMenuItem.DropDownItems.Add(PauseResume);

            return SettingsMenuItem;

        }

        private void PauseResume_ClickEvent(object sender, EventArgs e)
        {
            ToolStripButton current = ((ToolStripButton)sender);
            if (current.Text == "Pause")
            {
                Manager.Stop();
                current.Text = "Resume";
            }
            else { Manager.Start(); current.Text = "Pause"; ActiveKey = string.Empty; }
        }



        private void AddItemToCache(IDataObject obj)
        {
            ActiveKey = string.Empty;
            string[] ActiveCache = (from Ca in CacheHandler select Ca.Key).ToArray();
            ClipDataObject dataObject = new ClipDataObject(obj, ActiveCache);
            if (!string.IsNullOrEmpty(dataObject.Key))
            {
                CacheHandler.Insert(0, dataObject);
                if (CacheHandler.Count > TotalCount) { CacheHandler.Remove(CacheHandler.Last()); GC.Collect(); }
            }
        }

        private ToolStripItem[] CachedItems()
        {
            List<ToolStripItem> output = new List<ToolStripItem>();

            foreach (var item in CacheHandler)
            {
                item.Label.Click += (sender, e) => ItemClickEvent(sender, e, item.Key);
                output.Add(item.Label);
            }
            if (output.Count > 0) { return output.ToArray(); }
            return null;
        }

        private void ItemClickEvent(object sender, EventArgs e, string key)
        {
            if (ActiveKey != key)
            {
                ActiveKey = key;
                Manager.Stop();

                var X = (from y in CacheHandler
                         where y.Key == key
                         select y).First();
                System.Windows.Forms.Clipboard.Clear();
                System.Windows.Forms.Clipboard.SetDataObject(X.ClipboardObject());
                Manager.Start();
            }
        }

        private ToolStripItem[] SaveCachedItems() { return null; }


        public void MenuSettingsUpdateLocation(object sender, EventArgs e) { }

        public void MenuSettingsUpdateDayImages(object sender, EventArgs e) { }

        public void MenuSettingsUpdateNightImages(object sender, EventArgs e) { }


        private void MenuExitClick(object sender, EventArgs e) { Application.Exit(); }

        private void MenuAboutClick(object sender, EventArgs e)
        {
            string Name = this.GetType().Assembly.GetName().Name;

            System.Text.StringBuilder message = new System.Text.StringBuilder();
            var CustomDescriptionAttributes = this.GetType().Assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyDescriptionAttribute), false);
            if (CustomDescriptionAttributes.Length > 0) { message.Append(((System.Reflection.AssemblyDescriptionAttribute)CustomDescriptionAttributes[0]).Description).Append(Environment.NewLine); }
            message.Append(Environment.NewLine);
            message.Append("Version: ").Append(this.GetType().Assembly.GetName().Version.ToString()).Append(Environment.NewLine);
            var CustomInfoCopyrightCall = this.GetType().Assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyCopyrightAttribute), false);
            if (CustomInfoCopyrightCall.Length > 0) { message.Append("Copyright: ").Append(((System.Reflection.AssemblyCopyrightAttribute)CustomInfoCopyrightCall[0]).Copyright).Append(Environment.NewLine); }
            message.Append(Environment.NewLine);
            MessageBox.Show(message.ToString(), Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        #endregion

        #region "Functions and Methods"

        private void LoadCacheItemsFromDisk()
        {
            try {

                if (Shared.ExitCaching())
                {
                    using (System.IO.MemoryStream MS = new System.IO.MemoryStream(System.IO.File.ReadAllBytes("cache.mp")))
                    {
                        if (CacheHandler == null) { CacheHandler = new List<ClipDataObject>(); }
                        var X = MsgPack.Serialization.MessagePackSerializer.Get<List<SerializableClipObject>>();
                        var Y = X.Unpack(MS);

                        foreach (var item in Y)
                        {
                            CacheHandler.Add(new ClipDataObject(item));
                        }
                    }
                }

            }catch{ }
        }

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
                        System.IO.File.WriteAllBytes("Cache.mp", MS.ToArray());
                    }
                }
            }
            catch { }

        }



        #endregion

        #region "Menu Construction"




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
