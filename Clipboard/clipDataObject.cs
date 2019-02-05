using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Security.Cryptography;
using System.IO.Compression;

namespace Clipboard
{



    class ClipDataObject
    {
        #region Global Variables

        private static readonly string[] SupportedTypes = { DataFormats.Bitmap, DataFormats.CommaSeparatedValue, DataFormats.Dib, DataFormats.EnhancedMetafile, DataFormats.Html, DataFormats.OemText, DataFormats.Palette, DataFormats.Rtf, DataFormats.Serializable, DataFormats.StringFormat, DataFormats.Text, DataFormats.UnicodeText };
        private static readonly string[] imageFormats = { DataFormats.Bitmap, DataFormats.Dib };
        private static readonly string[] stringFormats = { DataFormats.CommaSeparatedValue, DataFormats.EnhancedMetafile, DataFormats.Html, DataFormats.OemText, DataFormats.Rtf, DataFormats.Serializable, DataFormats.StringFormat, DataFormats.Text, DataFormats.UnicodeText };

        #endregion

        #region Properties

        /// <summary>
        /// A unique key for the item
        /// </summary>
        public string Key { get; }


        private readonly byte[] _data;
        /// <summary>
        /// The raw data object
        /// </summary>
        public object Data
        {
            get
            {
                DataObject dataObject = new DataObject();

                Dictionary<string, object> CachedItem = (Dictionary<string, object>)DecompressObject(_data);
                foreach (var item in CachedItem)
                {
                    dataObject.SetData(item.Key, item.Value);
                }
                return dataObject;
            }
        }

        /// <summary>
        /// the format of the raw data object
        /// </summary>
        private string Type { get; }

        public ToolStripLabel Label { get; }

        public long _fileSize;
        public long FileSize { get { return _fileSize; } }
        #endregion

        #region Constructor

        /// <summary>
        /// Constructs a new Clip object in memory from the dataobject passed in from the clipboard
        /// </summary>
        /// <param name="item"></param>
        public ClipDataObject(IDataObject item)
        {
            _data = CompressObject(getMainObjectDict(item));
            Key = GenerateKey(_data);
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

                    byte[] bytes = ObjectToByteArray(o);
                    _fileSize = bytes.Length;
                    bytes = sha1Hash.ComputeHash(bytes);
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

        private static object StreamToObject(Stream ar)
        {
            BinaryFormatter bf = new BinaryFormatter();
            return bf.Deserialize(ar);
        }


        private static byte[] CompressObject(object o)
        {
            using (MemoryStream compressedItem = new MemoryStream())
            {
                using (DeflateStream Compressor = new DeflateStream(compressedItem, CompressionMode.Compress))
                {
                    (new MemoryStream(ObjectToByteArray(o))).CopyTo(Compressor);
                }
                return compressedItem.ToArray();
            }
        }

        private static object DecompressObject(object o)
        {
            var item = new MemoryStream();
            using (DeflateStream Decompressor = new DeflateStream(new MemoryStream((byte[])o), CompressionMode.Decompress))
            {
                Decompressor.CopyTo(item);
            }
            item.Position = 0;
            return StreamToObject(item);

        }


        private static byte[] CompressImage(System.Drawing.Image o)
        {
            byte[] result = null;
            using (MemoryStream stream = new MemoryStream())
            {
                o.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                result = stream.ToArray();
            }
            return result;
        }

        private static System.Drawing.Image DecompressImage(object o) { return System.Drawing.Image.FromStream(new MemoryStream((byte[])o)); }




        private Dictionary<string, object> getMainObjectDict(IDataObject data)
        {

            Dictionary<string, object> response = new Dictionary<string, object>();
            foreach (string s in data.GetFormats())
            {
                if (data.GetDataPresent(s, false) && data.GetData(s, false) != null)
                {
                    response.Add(s, data.GetData(s));
                }
            }
            return response;
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
                System.Drawing.Image thumb = obj.GetImage().GetThumbnailImage(16, 16, () => false, IntPtr.Zero);
                return new ToolStripLabel(Type + " " + DateTime.Now.ToString("hhMMss"), thumb, false);
            }
            else
            {
                return new ToolStripLabel(Type + " " + DateTime.Now.ToString("hhMMss"), null, false);
            }
        }
        #endregion
    }
}