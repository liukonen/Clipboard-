using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.IO.Compression;

namespace Clipboard
{
    class ClipDataObject : IDisposable
    {
        #region Global Variables

        private Dictionary<string, byte[]> extractedItems = new Dictionary<string, byte[]>();
        private Dictionary<string, string> ExtractedTypeLookupTable = new Dictionary<string, string>();
        private Dictionary<string, DataType> CompresionTypeLookup = new Dictionary<string, DataType>();
        
        #endregion

        #region Enums
        private enum DataType : byte { Text = 0, Image = 1, MemoryStream = 2, Other = 3 }
        #endregion

        #region Properties

        /// <summary>
        /// A unique key for the item
        /// </summary>
        public string Key { get; }

        public DataObject ClipboardObject()
        {
                DataObject data = new DataObject();
                foreach (string item in Formats)
                {
                    data.SetData(item, GetData(item));
                }
                return data;
        }

        public ToolStripLabel Label { get; }
        public long FileSize { get { try { return (from X in extractedItems.Values select X.Length).Sum(); } catch { } return int.MaxValue; } }
        public string[] Formats { get { return ExtractedTypeLookupTable.Keys.ToArray(); } }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructs a new Clip object in memory from the dataobject passed in from the clipboard
        /// </summary>
        /// <param name="item">Clipboard Data object</param>
        public ClipDataObject(IDataObject item) : this(item, new string[] { }) { }

        /// <summary>
        /// Constructs a new Clip object in memory from the dataobject passed in from the clipboard
        /// </summary>
        /// <param name="item">Clipboard Data object</param>
        /// <param name="FilteredKeys">Optional Filtered keys for distinct items. If there is a match, the key will be empty</param>
        public ClipDataObject(IDataObject item, string[] FilteredKeys)
        {
            string[] types = item.GetFormats();
            foreach (var strCurrentType in types)
            {
                if (item.GetDataPresent(strCurrentType, false) && item.GetData(strCurrentType, false) != null)
                {
                    object lookedupItem = item.GetData(strCurrentType, false);
                    using (MemoryStream ms = new MemoryStream())
                    {
                        KeyValuePair<string, byte[]> response;
                        DataType CompressionType;

                        if (lookedupItem is string)
                        {
                            response = Lookup((string)lookedupItem, FilteredKeys);
                            CompressionType = DataType.Text;
                        }

                        else if (lookedupItem is System.Drawing.Bitmap)
                        {
                            response = Lookup((System.Drawing.Bitmap)lookedupItem, FilteredKeys);
                            CompressionType = DataType.Image;
                        }
                        else if (lookedupItem is MemoryStream)
                        {
                            response = Lookup((MemoryStream)lookedupItem, FilteredKeys);
                            CompressionType = DataType.MemoryStream;
                        }
                        else
                        {
                            response = LookupObject(lookedupItem, FilteredKeys);
                            CompressionType = DataType.Other;
                        }
                        if (response.Key == string.Empty) { break; }
                        if (string.IsNullOrEmpty(Key)) { Key = response.Key; }
                        if (!extractedItems.ContainsKey(response.Key)) { extractedItems.Add(response.Key, response.Value); }
                        if (!CompresionTypeLookup.ContainsKey(response.Key)) { CompresionTypeLookup.Add(response.Key, CompressionType); }
                        ExtractedTypeLookupTable.Add(strCurrentType, response.Key);
                    }
                }
            }
            Label = GenerateLabel((DataObject)item);
        }
        #endregion

        #region Object Specific Transformers
        private static KeyValuePair<string, byte[]> Lookup(string item, string[] filterHash)
        {
            string hash = Shared.GenerateHash(System.Text.Encoding.Unicode.GetBytes(item)); 
            if (!filterHash.Contains(hash))            {                return new KeyValuePair<string, byte[]>(hash, CompressString(item));            }
            return new KeyValuePair<string, byte[]>(string.Empty, null);
        }

        private static KeyValuePair<string, byte[]> Lookup(System.Drawing.Bitmap item, string[] filterHash)
        {
            string hash = Shared.GenerateHash(Shared.imageToByte(item)); 
            if (!filterHash.Contains(hash))            {                return new KeyValuePair<string, byte[]>(hash, CompressImage(item));            }
            return new KeyValuePair<string, byte[]>(string.Empty, null);
        }

        private static KeyValuePair<string, byte[]> Lookup(MemoryStream item, string[] filterHash)
        {
            string hash = Shared.GenerateHash((item).ToArray());
            if (!filterHash.Contains(hash)) { return new KeyValuePair<string, byte[]>(hash, CompressStream(item)); }
            return new KeyValuePair<string, byte[]>(string.Empty, null);
        }

        private static KeyValuePair<string, byte[]> LookupObject(Object item, string[] filterHash)
        {
            string hash = item.GetHashCode().ToString();
            if (!filterHash.Contains(hash)) { return new KeyValuePair<string, byte[]>(hash, CompressObject(item)); }
            return new KeyValuePair<string, byte[]>(string.Empty, null);
        }
        #endregion
 
        #region Compressions
        #region Compress
        private static byte[] CompressString(string stringToCompress)
        {

           using (MemoryStream streamToCompressTo = new MemoryStream())
            {
                using (DeflateStream compressor = new DeflateStream(streamToCompressTo, CompressionMode.Compress))
                {
                    using (System.IO.StreamWriter writer = new System.IO.StreamWriter(compressor))
                    {
                        writer.Write(stringToCompress);
                        writer.Flush();
                    }

                }
                return streamToCompressTo.ToArray();
            }
        }

        private static byte[] CompressStream(MemoryStream StreamToCompress)
        {
            using (MemoryStream streamToCompressTo = new MemoryStream())
            {
                using (DeflateStream compressor = new DeflateStream(streamToCompressTo, CompressionMode.Compress))
                {
                    StreamToCompress.CopyTo(compressor);
                }
                return streamToCompressTo.ToArray();
            }
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

        #endregion

        #region Decompress

        private static string DecompressString(byte[] CompressedString)
        {

            string s;
            using (StreamReader reader = new StreamReader(DecompressStream(CompressedString)))
            {
                s = reader.ReadToEnd();
            }
            return s;
        }

        private static MemoryStream DecompressStream(Byte[] CompressedStream)
        {
            var item = new MemoryStream();
            using (DeflateStream Decompressor = new DeflateStream(new MemoryStream(CompressedStream), CompressionMode.Decompress))
            {
                Decompressor.CopyTo(item);
            }
            item.Position = 0;
            return item;
        }

        private static object DecompressObject(object o)
        {
            var item = DecompressStream((byte[])o);
            return StreamToObject(item);
        }
        
        private static System.Drawing.Image DecompressImage(object o) { return System.Drawing.Image.FromStream(new MemoryStream((byte[])o)); }


        #endregion
        #endregion

        #region Unknown Object Serializations

        private static byte[] ObjectToByteArray(Object input)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, input);
                return ms.ToArray();
            }
        }

        private static object StreamToObject(Stream input)
        {
            BinaryFormatter bf = new BinaryFormatter();
            return bf.Deserialize(input);
        }

        #endregion

        #region Functions

        /// <summary>
        /// Extracts the raw data object based on format, from Extracted items
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        private object GetData(string format)
        {
            string hash = ExtractedTypeLookupTable[format];
            DataType CompressionType = CompresionTypeLookup[hash];
            switch (CompressionType)
            {
                case DataType.Text:
                    return DecompressString(extractedItems[hash]);
                case DataType.Image:
                    return DecompressImage(extractedItems[hash]);
                case DataType.MemoryStream:
                    return DecompressStream(extractedItems[hash]);
                default:
                    return DecompressObject(extractedItems[hash]);
            }
        }

        /// <summary>
            /// Generates the visable label to display on the screen
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
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
                return new ToolStripLabel("Image " + DateTime.Now.ToString("hhMMss"), thumb, false);
            }
            else
            {
                return new ToolStripLabel("Object " + DateTime.Now.ToString("hhMMss"), null, false);
            }
        }
        #endregion

        #region IDisposable Support
        private bool disposedValue = false;

        public virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Label.Dispose();
                }
                extractedItems.Clear();
                ExtractedTypeLookupTable.Clear();
                CompresionTypeLookup.Clear();
                disposedValue = true;
              
            }
        }

        void IDisposable.Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion

    }
}