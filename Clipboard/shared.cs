using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipboard
{
    class Shared
    {
        public static byte[] imageToByte(System.Drawing.Bitmap bm)
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


    }
}
