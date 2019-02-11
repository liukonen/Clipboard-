using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Clipboard
{
    class SaveAsObject
    {


        enum SaveAsType: byte { png2 = 0, rtf =1, img = 2, csv = 3, html = 4, txt = 5 }

        enum SaveAsExtentions: Byte { png, jpg, csv, bmp, html, rtf, txt}

        private Dictionary<string, SaveAsType> lookupSaveAsType = new Dictionary<string, SaveAsType>()
        {
            { DataFormats.Bitmap, SaveAsType.img},
            //{ DataFormats.CommaSeparatedValue, SaveAsType.csv},
            {DataFormats.Html, SaveAsType.html  },
            {DataFormats.Rtf, SaveAsType.rtf },
            {DataFormats.Text, SaveAsType.txt },
            {DataFormats.UnicodeText, SaveAsType.txt },
            {"PNG", SaveAsType.png2 }
        };

        private Dictionary<SaveAsExtentions, string> extentionName = new Dictionary<SaveAsExtentions, string>()
        {
            { SaveAsExtentions.bmp, "Bitmap Image"},
            //{SaveAsExtentions.csv, "Comma Seperated Value" },
            {SaveAsExtentions.html, "webpage" },
            {SaveAsExtentions.jpg, "Jpeg Image" },
            {SaveAsExtentions.png, "Portable Network Graphic" },
            {SaveAsExtentions.rtf, "Rich Text File" },
            {SaveAsExtentions.txt, "Text File" }
        };


        List<Tuple<SaveAsType, SaveAsExtentions>> extentionMap = new List<Tuple<SaveAsType, SaveAsExtentions>>()
        {
            new Tuple<SaveAsType, SaveAsExtentions>(SaveAsType.png2, SaveAsExtentions.png),
            new Tuple<SaveAsType, SaveAsExtentions>(SaveAsType.img, SaveAsExtentions.png),
            new Tuple<SaveAsType, SaveAsExtentions>(SaveAsType.img, SaveAsExtentions.jpg),
            new Tuple<SaveAsType, SaveAsExtentions>(SaveAsType.img, SaveAsExtentions.bmp),
            new Tuple<SaveAsType, SaveAsExtentions>(SaveAsType.rtf, SaveAsExtentions.rtf),
            new Tuple<SaveAsType, SaveAsExtentions>(SaveAsType.txt, SaveAsExtentions.txt),
            new Tuple<SaveAsType, SaveAsExtentions>(SaveAsType.html, SaveAsExtentions.html)
        };


        private Dictionary<SaveAsType, string> typeFormats = new Dictionary<SaveAsType, string>()
        {
            {SaveAsType.img, "JPEG Image (*.jpg)|*.jpg|Portable Network Graphics Image (*.png)|*.png|Bitmap Image (*.bmp)|*.bmp" },
        //    {SaveAsType.csv, "Comma Seperated Value (*.csv)|*.csv" },
            {SaveAsType.html, "html page (*.html)|*.html" },
            {SaveAsType.png2, "Portable Network Graphics with Transparency (*.png)|*.png" },
            {SaveAsType.rtf, "RichText File (*.rtf)|*.rtf" },
            {SaveAsType.txt, "Text File (*.txt)|*.txt" }

        };




        public SaveAsObject(DataObject clip)
        {
            //lookup types 
            IEnumerable<SaveAsType> lookupItems = (from fi in clip.GetFormats() where lookupSaveAsType.ContainsKey(fi) orderby fi select lookupSaveAsType[fi]).Distinct();



            SaveAsExtentions[] x = extentionMap.Where(item => lookupItems.Contains(item.Item1)).OrderBy(item => item.Item2).Select(item => item.Item2).ToArray();

            Dictionary<int, SaveAsExtentions> lookupindex = new Dictionary<int, SaveAsExtentions>();
            string filter = string.Join("|", from y in x select string.Format("{0} (*.{1})|*.{1}", extentionName[y], y.ToString()));



            //string Filter = string.Join("|", from x in lookupItems select typeFormats[x]);
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = filter;
            saveFileDialog1.Title = "Save File";
            saveFileDialog1.ShowDialog();

            // If the file name is not an empty string open it for saving.  
            if (saveFileDialog1.FileName != string.Empty)
            {
                SaveAsExtentions ext;
                Enum.TryParse<SaveAsExtentions>(System.IO.Path.GetExtension(saveFileDialog1.FileName).Substring(1), out ext);


                //SaveAsExtentions ext = x[saveFileDialog1.FilterIndex];
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
                    //case SaveAsExtentions.csv:
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
    }
}
