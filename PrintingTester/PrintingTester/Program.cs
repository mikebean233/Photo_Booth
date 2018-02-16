using System;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using PrinterPlusPlusSDK;

using Printing;

namespace PrintingTester
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            PrintManager printManager = PrintManager.GetInstance("pdf");
            printManager.AddImage(GetImageSourceFromPath("pack://application:,,,/Kinect_Standing.bmp"));
            printManager.AddImage(GetImageSourceFromPath("pack://application:,,,/Kinect_Leaning.bmp"));
            Boolean result = printManager.print();

            Console.WriteLine("Print result: {0}", result ? "success" : "failure");
        }

        static ImageSource GetImageSourceFromPath(String path)
        {
            ImageSource returnValue = null;
            try
            {
                returnValue =  new BitmapImage(new Uri(path, UriKind.RelativeOrAbsolute));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }
            return returnValue;
        }

        public class PrintProcessor : PrinterPlusPlusSDK.IProcessor
        {
            public ProcessResult Process(string key, string psFilename)
            {
                Console.WriteLine("key: {0}    psFilename: {1}", key, psFilename);
                return new ProcessResult();
            }
        }

    }
}
