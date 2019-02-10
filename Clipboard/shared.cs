using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Windows.Forms;

namespace Clipboard
{
    class Shared
    {
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
                if (ConfigurationManager.AppSettings.AllKeys.Contains(key)) { ConfigurationManager.AppSettings[key] = value; }
                else { ConfigurationManager.AppSettings.Add(key, value); }
            }
            catch (ConfigurationErrorsException) { MessageBox.Show("Error writing app settings", "Error Editing Config", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        public static string ReadAppSetting(string key)
        {
            try { return System.Configuration.ConfigurationManager.AppSettings[key]; }
            catch{ return string.Empty; } 
        }

        public static Boolean ExitCaching()
        {
            string val = ReadAppSetting("ExitCaching");
            Boolean response = false;
            Boolean.TryParse(val, out response);
            return response;
        }

        public static int MaxFileSize()
        {
            const int MaxHardCodedSize = 52428800; //Max 50 Megs
            string val = ReadAppSetting("MaxFileSize");
            int response = ConvertFileSize(val);
            if (response == 0 || response > MaxHardCodedSize) { response = MaxHardCodedSize; }
            return response;

        }

        public static int MaxCacheSize()
        {
            const int MaxHardCodedSize = 528392000; //Max 500 Megs
            string val = ReadAppSetting("MaxCacheSize");
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
            int Parse = 0;
            int.TryParse(FileSize, out Parse);
            return Parse;
        }


    }
}
