using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Security.Cryptography;

namespace Clipboard
{
    class ClipDataObject
    {
        #region Global Variables

        private string[] SupportedTypes = new string[]
    {
            DataFormats.Bitmap,
            DataFormats.CommaSeparatedValue,
            DataFormats.Dib,
            DataFormats.Dif,
            DataFormats.EnhancedMetafile,
            DataFormats.FileDrop,
            DataFormats.Html,
            DataFormats.Locale,
            DataFormats.MetafilePict,
            DataFormats.OemText,
            DataFormats.Palette,
            DataFormats.PenData,
            DataFormats.Riff,
            DataFormats.Rtf,
            DataFormats.Serializable,
            DataFormats.StringFormat,
            DataFormats.SymbolicLink,
            DataFormats.Text,
            DataFormats.Tiff,
            DataFormats.UnicodeText,
            DataFormats.WaveAudio
    };

        #endregion

        #region Properties

        /// <summary>
        /// A unique key for the item
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// The raw data object
        /// </summary>
        public object Data { get; }

        /// <summary>
        /// the format of the raw data object
        /// </summary>
        public string Type { get; }

        public ToolStripLabel Label { get; }
        #endregion

        #region Constructor

        /// <summary>
        /// Constructs a new Clip object in memory from the dataobject passed in from the clipboard
        /// </summary>
        /// <param name="item"></param>
        public ClipDataObject(IDataObject item)
        {
            Type = GetMainFormat(item.GetFormats());
            Data = item.GetData(Type);
            Key = GenerateKey(Data);
            Label = GenerateLabel((DataObject)item);
        }

        #endregion

        #region sub Functions

        private string GenerateKey(object o)
        {
            try
            {
                using (SHA1 sha1Hash = SHA1.Create())
                {
                    byte[] bytes = sha1Hash.ComputeHash(ObjectToByteArray(o));
                    System.Text.StringBuilder builder = new System.Text.StringBuilder();
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        builder.Append(bytes[i].ToString("x2"));
                    }
                    return builder.ToString();
                }
            }
            catch { }
            return o.GetHashCode().ToString();
        }

        private static byte[] ObjectToByteArray(Object obj)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        private string GetMainFormat(string[] types)
        {
            return (from string s in types where SupportedTypes.Contains(s) select s).First();
        }

        private ToolStripLabel GenerateLabel(DataObject obj)
        {

            if (obj.ContainsText())
            {
                string Val = obj.GetText().Replace(Environment.NewLine, string.Empty);
                if (Val.Length > 120) { Val = Val.Substring(0, 120); }
                return new ToolStripLabel(Val, null, false);
            }
            else if (obj.ContainsImage())
            {
                return new ToolStripLabel(Type + " " + DateTime.Now.ToString("hhMMss"), obj.GetImage().GetThumbnailImage(32, 32, () => false, IntPtr.Zero), false)
                {
                };
            }
            else
            {
                return new ToolStripLabel(Type + " " + DateTime.Now.ToString("hhMMss"), null, false);
            }
        }
        #endregion
    }
}
