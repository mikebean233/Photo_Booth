using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;

using Printing;

namespace PrintingTester
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            PrintManager printManager = PrintManager.GetInstance("pdf");
            printManager.AddImage(GetImageSourceFromPath("Kinect_Standing.bmp"));
            printManager.AddImage(GetImageSourceFromPath("Kinect_Leaning.bmp"));
            Boolean result = printManager.print();

            Console.WriteLine("Print result: {0}", result ? "success" : "failure");
        }

        static ImageSource GetImageSourceFromPath(String path)
        {
            return new BitmapImage(new Uri(path, UriKind.RelativeOrAbsolute));
        }
    }
}
