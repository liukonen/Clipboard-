

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;

namespace Clipboard
{
    [DefaultEvent("ClipboardChanged")]
    public partial class ClipboardManager : Control
    {
        private Boolean onOffSwitch = true;

        public void Stop() { onOffSwitch = false; }
        public void Start() { onOffSwitch = true; }

        public ClipboardManager()
        {
            ClipboardViewer = (IntPtr)SetClipboardViewer((int)this.Handle);
        }

        public event EventHandler<ClipboardChangedEventArgs> ClipboardChanged;


        #region Low Level control Items

        IntPtr ClipboardViewer;

        [DllImport("User32.dll")]
        protected static extern int SetClipboardViewer(int hWndNewViewer);

        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern bool ChangeClipboard(IntPtr hWndRemove, IntPtr hWndNewNext);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hwnd, int wMsg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// The WndProc is the function you write to receive all input directed at your window. 
        /// You will have already told Windows to call this function with messages by supplying a pointer to 
        /// this function in the class structure (it is a callback function).
        /// </summary>
        /// <param name="m"></param>
        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            // defined in winuser.h
            const int cDrawClipboard = 0x308;
            const int cChangeCBChain = 0x030D;

            switch (m.Msg)
            {
                case cDrawClipboard:
                    if (onOffSwitch) { OnClipboardChanged(); }
                    SendMessage(ClipboardViewer, m.Msg, m.WParam, m.LParam);
                    break;

                case cChangeCBChain:
                    if (m.WParam == ClipboardViewer)
                        ClipboardViewer = m.LParam;
                    else
                        SendMessage(ClipboardViewer, m.Msg, m.WParam, m.LParam);
                    break;

                default:
                    base.WndProc(ref m);
                    break;
            }
        }
        #endregion

        /// <summary>
        /// On event close, dispose of remaining items
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            try
            {
                ChangeClipboard(this.Handle, ClipboardViewer);
            }
            catch { }
        }


        /// <summary>
        /// Called from WndProc, fires event handler call to ClipboardChangedEventArgs. On Error, displays popup of error
        /// </summary>
        void OnClipboardChanged()
        {
            try
            {
                IDataObject iData = System.Windows.Forms.Clipboard.GetDataObject();
                ClipboardChanged?.Invoke(this, new ClipboardChangedEventArgs(iData));

            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }
    }

    /// <summary>
    /// Custom Event Args object for Event handle on Clipboard change event.
    /// </summary>
    public class ClipboardChangedEventArgs : EventArgs
    {
        public readonly IDataObject _data;

        public ClipboardChangedEventArgs(IDataObject data)
        {
            _data = data;
        }
    }

}