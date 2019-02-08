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

        private Dictionary<int, byte[]> extractedItems = new Dictionary<int, byte[]>();
        private Dictionary<string, int> ExtractedTypeLookupTable = new Dictionary<string, int>();
        private Dictionary<int, DataType> CompresionTypeLookup = new Dictionary<int, DataType>();
        
        #endregion

        #region Enums
        private enum DataType : byte { Text = 0, Image = 1, MemoryStream = 2, Other = 3 }
        #endregion

        #region Properties

        /// <summary>
        /// A unique key for the item
        /// </summary>
        public string Key { get; }

        public DataObject ClipboardObject
        {
            get
            {
                DataObject data = new DataObject();
                foreach (string item in Formats)
                {
                    data.SetData(item, GetData(item));
                }
                return data;
            }
        }

        public ToolStripLabel Label { get; }

        public long FileSize { get { try { return (from X in extractedItems.Values select X.Length).Sum(); } catch { } return int.MaxValue; } }

        public string[] Formats { get { return ExtractedTypeLookupTable.Keys.ToArray(); } }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructs a new Clip object in memory from the dataobject passed in from the clipboard
        /// </summary>
        /// <param name="item"></param>
        public ClipDataObject(IDataObject item)
        {

            string[] types = item.GetFormats();
            foreach (var strCurrentType in types)
            {
                if (item.GetDataPresent(strCurrentType, false) && item.GetData(strCurrentType, false) != null)
                {
                    object lookedupItem = item.GetData(strCurrentType, false);
                    using (MemoryStream ms = new MemoryStream())
                    {
                        int hash = lookedupItem.GetHashCode();
                        if (string.IsNullOrEmpty(Key)) { Key = hash.ToString(); }

                        if (!extractedItems.ContainsKey(hash))
                        {

                            if (lookedupItem is string)
                            {
                                var x = CompressString((string)lookedupItem);
                                extractedItems.Add(hash, x);
                                CompresionTypeLookup.Add(hash, DataType.Text);
                            }

                            else if (lookedupItem is System.Drawing.Bitmap)
                            {
                                extractedItems.Add(hash, CompressImage((System.Drawing.Bitmap)lookedupItem));
                                CompresionTypeLookup.Add(hash, DataType.Image);

                            }
                            else if (lookedupItem is MemoryStream)
                            {
                                extractedItems.Add(hash, CompressStream((MemoryStream)lookedupItem));
                                CompresionTypeLookup.Add(hash, DataType.MemoryStream);
                            }
                            else
                            {
                                extractedItems.Add(hash, CompressObject(lookedupItem));
                                CompresionTypeLookup.Add(hash, DataType.Other);

                            }
                        }
                        ExtractedTypeLookupTable.Add(strCurrentType, hash);
                    }
                }
            }







            //_data = CompressObject(getMainObjectDict(item));
           // Key = GenerateKey(_data);
            Label = GenerateLabel((DataObject)item);
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
            int hash = ExtractedTypeLookupTable[format];
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