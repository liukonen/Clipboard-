using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Windows.Forms;

namespace Clipboard
{
    class Shared
    {
        public const string cMaxFileSize = "MaxFileSize";
        public const string cMaxCacheSize = "MaxCacheSize";
        public const string cUseSaveEncryption = "UseSaveEncryption";
        public const string cNumberOfSupportedCaches = "NumberOfSupportedCaches";
        public const string cExitCaching = "ExitCaching";
        public const string cFormatCacheSizeMenu = "Update Cache Size({0})";
        public const string cFormatFileSizeMenu = "Update File Size({0})";

        public enum DataType : byte { Text = 0, Image = 1, MemoryStream = 2, Other = 3 }


        public static byte[] ImageToByte(System.Drawing.Bitmap bm)
        {
            byte[]  response;
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                bm.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                response= ms.ToArray();
            }
            return response;
        }

        public static string GenerateHash(byte[] hashObject)
        {
            const string c_HashFormat = "x2";
            StringBuilder sb = new StringBuilder();
            using (var X = System.Security.Cryptography.SHA1.Create())
            {
                foreach (byte item in X.ComputeHash(hashObject))
                {
                    sb.Append(item.ToString(c_HashFormat));

                }
            }
                return sb.ToString();     
        }

        public static void AddUpdateAppSettings(string key, string value)
        {
                try
                {
                    var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    var settings = configFile.AppSettings.Settings;
                    if (settings[key] == null) { settings.Add(key, value); }
                    else { settings[key].Value = value; }
                    configFile.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
                }
                catch (ConfigurationErrorsException)
                {
                    MessageBox.Show("Error writing app settings", "Error Editing Config", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }


        public static string ReadAppSetting(string key)
        {
            try { return System.Configuration.ConfigurationManager.AppSettings[key]; }
            catch{ return string.Empty; } 
        }

        public static Boolean ExitCaching()
        {
            string val = ReadAppSetting(cExitCaching);
            Boolean.TryParse(val, out Boolean response);
            return response;
        }
        
        public static Boolean ValidateMaxFileSize(string test)
        {
            const int MaxHardCodedSize = 52428800; //Max 50 Megs
            int response = ConvertFileSize(test);
            return (!(response == 0 || response > MaxHardCodedSize));
            
        }

        public static Boolean ValidateMaxCacheSize(string text)
        {
            const int MaxHardCodedSize = 528392000; //Max 500 Megs
            int response = ConvertFileSize(text);
            return (!(response == 0 || response > MaxHardCodedSize));
        }

        public static int MaxFileSize()
        {
            const int MaxHardCodedSize = 52428800; //Max 50 Megs
            string val = ReadAppSetting(cMaxFileSize);
            int response = ConvertFileSize(val);
            if (response == 0 || response > MaxHardCodedSize) { response = MaxHardCodedSize; }
            return response;

        }

        public static int MaxCacheSize()
        {
            const int MaxHardCodedSize = 528392000; //Max 500 Megs
            string val = ReadAppSetting(cMaxCacheSize);
            int response = ConvertFileSize(val);
            if (response == 0 || response > MaxHardCodedSize) { response = MaxHardCodedSize; }
            return response;
        }

        public static int ConvertFileSize(string FileSize)
        {
            if (string.IsNullOrEmpty(FileSize)) { return 0; }
            FileSize = FileSize.TrimEnd().ToUpper();
            int Multiplier = 1;

            if (FileSize.EndsWith("KB")) { Multiplier = 1028; }
            else if (FileSize.EndsWith("MB")) { Multiplier = 1056784; }
            //Any Larger then 1 gig would be too much IO for a drive right now
            if (Multiplier > 1) { FileSize = FileSize.Substring(0, FileSize.Length - 2); }
            int.TryParse(FileSize, out int Parse);
            return Parse * Multiplier;
        }

        #region Encryption Items
        public static byte[] EncryptClipObject(byte[] clipObject)
        {
            return System.Security.Cryptography.ProtectedData.Protect(clipObject, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
        }

        public static byte[] DecryptClipObject(byte[] clipObject)
        {
            return System.Security.Cryptography.ProtectedData.Unprotect(clipObject, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
        }
        #endregion


        #region Save As Objects
        enum SaveAsType : byte { png2 = 0, rtf = 1, img = 2, csv = 3, html = 4, txt = 5 }
        enum SaveAsExtentions : Byte { png, jpg, csv, bmp, html, rtf, txt }
        private static Dictionary<string, SaveAsType> lookupSaveAsType = new Dictionary<string, SaveAsType>()
        {
            { DataFormats.Bitmap, SaveAsType.img},
            //{ DataFormats.CommaSeparatedValue, SaveAsType.csv},
            {DataFormats.Html, SaveAsType.html  },
            {DataFormats.Rtf, SaveAsType.rtf },
            {DataFormats.Text, SaveAsType.txt },
            {DataFormats.UnicodeText, SaveAsType.txt },
            {"PNG", SaveAsType.png2 }
        };
        private static readonly Dictionary<SaveAsExtentions, string> extentionName = new Dictionary<SaveAsExtentions, string>()
        {
            { SaveAsExtentions.bmp, "Bitmap Image"},
            //{SaveAsExtentions.csv, "Comma Seperated Value" },
            {SaveAsExtentions.html, "webpage" },
            {SaveAsExtentions.jpg, "Jpeg Image" },
            {SaveAsExtentions.png, "Portable Network Graphic" },
            {SaveAsExtentions.rtf, "Rich Text File" },
            {SaveAsExtentions.txt, "Text File" }
        };

        private static List<Tuple<SaveAsType, SaveAsExtentions>> extentionMap = new List<Tuple<SaveAsType, SaveAsExtentions>>()
        {
            new Tuple<SaveAsType, SaveAsExtentions>(SaveAsType.png2, SaveAsExtentions.png),
            new Tuple<SaveAsType, SaveAsExtentions>(SaveAsType.img, SaveAsExtentions.png),
            new Tuple<SaveAsType, SaveAsExtentions>(SaveAsType.img, SaveAsExtentions.jpg),
            new Tuple<SaveAsType, SaveAsExtentions>(SaveAsType.img, SaveAsExtentions.bmp),
            new Tuple<SaveAsType, SaveAsExtentions>(SaveAsType.rtf, SaveAsExtentions.rtf),
            new Tuple<SaveAsType, SaveAsExtentions>(SaveAsType.txt, SaveAsExtentions.txt),
            new Tuple<SaveAsType, SaveAsExtentions>(SaveAsType.html, SaveAsExtentions.html)
        };

        private static readonly Dictionary<SaveAsType, string> typeFormats = new Dictionary<SaveAsType, string>()
        {
            {SaveAsType.img, "JPEG Image (*.jpg)|*.jpg|Portable Network Graphics Image (*.png)|*.png|Bitmap Image (*.bmp)|*.bmp" },
            {SaveAsType.html, "html page (*.html)|*.html" },
            {SaveAsType.png2, "Portable Network Graphics with Transparency (*.png)|*.png" },
            {SaveAsType.rtf, "RichText File (*.rtf)|*.rtf" },
            {SaveAsType.txt, "Text File (*.txt)|*.txt" }
        };

        public static void SaveAsObject(DataObject clip)
        {
            //lookup types 
            IEnumerable<SaveAsType> lookupItems = (from fi in clip.GetFormats() where lookupSaveAsType.ContainsKey(fi) orderby fi select lookupSaveAsType[fi]).Distinct();
            SaveAsExtentions[] x = extentionMap.Where(item => lookupItems.Contains(item.Item1)).OrderBy(item => item.Item2).Select(item => item.Item2).ToArray();
            Dictionary<int, SaveAsExtentions> lookupindex = new Dictionary<int, SaveAsExtentions>();
            string filter = string.Join("|", from y in x select string.Format("{0} (*.{1})|*.{1}", extentionName[y], y.ToString()));
            SaveFileDialog saveFileDialog1 = new SaveFileDialog() { Filter = filter, Title = "Save File" };
            saveFileDialog1.ShowDialog();

            if (saveFileDialog1.FileName != string.Empty)
            {
                Enum.TryParse<SaveAsExtentions>(System.IO.Path.GetExtension(saveFileDialog1.FileName).Substring(1), out SaveAsExtentions ext);
                switch (ext)
                {
                    case SaveAsExtentions.bmp:
                        ((System.Drawing.Bitmap)clip.GetData(DataFormats.Bitmap)).Save(saveFileDialog1.FileName, System.Drawing.Imaging.ImageFormat.Bmp);
                        break;
                    case SaveAsExtentions.jpg:
                        ((System.Drawing.Bitmap)clip.GetData(DataFormats.Bitmap)).Save(saveFileDialog1.FileName, System.Drawing.Imaging.ImageFormat.Jpeg);
                        break;
                    case SaveAsExtentions.png:
                        if (clip.GetFormats().Contains("PNG"))
                        {
                            System.IO.File.WriteAllBytes(saveFileDialog1.FileName, ((System.IO.MemoryStream)clip.GetData("PNG")).ToArray());
                        }
                        else { ((System.Drawing.Bitmap)clip.GetData(DataFormats.Bitmap)).Save(saveFileDialog1.FileName, System.Drawing.Imaging.ImageFormat.Png); }
                        break;
                    case SaveAsExtentions.html:
                        System.IO.File.WriteAllText(saveFileDialog1.FileName, (string)clip.GetData(DataFormats.Html));
                        break;
                    case SaveAsExtentions.rtf:
                        System.IO.File.WriteAllText(saveFileDialog1.FileName, (string)clip.GetData(DataFormats.Rtf));
                        break;
                    case SaveAsExtentions.txt:
                        System.IO.File.WriteAllText(saveFileDialog1.FileName, clip.GetText());
                        break;
                    default:
                        break;
                }
            }
        }
        #endregion
    }
}
