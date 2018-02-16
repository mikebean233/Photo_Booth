using System;
using Printing;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace Testing
{
    internal class Tester
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                PrintManager printManager = PrintManager.GetInstance("hp");
                printManager.AddImage(GetImageSourceFromPath("pack://application:,,,/Kinect_Standing.bmp"));
                printManager.AddImage(GetImageSourceFromPath("pack://application:,,,/Kinect_Leaning.bmp"));
                Boolean result = printManager.print();
                while (true)
                {
                    System.Threading.Thread.Sleep(100);
                }
                //System.Diagnostics.Debug.WriteLine("Print result: " + (result ? "success" : "failure"));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("=========== Exception In Main ===========");
                System.Diagnostics.Debug.WriteLine("{0}", ex.Message);
                System.Diagnostics.Debug.WriteLine("=========================================");
            }
        }


        static ImageSource GetImageSourceFromPath(String path)
        {
            ImageSource returnValue = new BitmapImage(new Uri(path, UriKind.RelativeOrAbsolute));
            return returnValue;
        }
    }
}
