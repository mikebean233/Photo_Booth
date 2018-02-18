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
                PrintManager printManager = PrintManager.GetInstance("hiti");

                PrintManager.PrintBatchHandler batch1 = printManager.startNewBatch(PrintTemplateType.Wide);                
                batch1.AddImage(GetImageSourceFromPath("pack://application:,,,/Kinect_Standing.bmp"));
                batch1.AddImage(GetImageSourceFromPath("pack://application:,,,/Kinect_Standing.bmp"));
                batch1.CompleteBatch(1);

                PrintManager.PrintBatchHandler batch2 = printManager.startNewBatch(PrintTemplateType.Wide);
                batch2.AddImage(GetImageSourceFromPath("pack://application:,,,/Kinect_Standing.bmp"));
                batch2.AddImage(GetImageSourceFromPath("pack://application:,,,/Kinect_Leaning.bmp"));
                batch2.CompleteBatch(1);

                PrintManager.PrintBatchHandler batch3 = printManager.startNewBatch(PrintTemplateType.Wide);
                batch3.AddImage(GetImageSourceFromPath("pack://application:,,,/Kinect_Leaning.bmp"));
                batch3.AddImage(GetImageSourceFromPath("pack://application:,,,/Kinect_Standing.bmp"));
                batch3.CompleteBatch(1);

                PrintManager.PrintBatchHandler batch4 = printManager.startNewBatch(PrintTemplateType.Wide);
                batch4.AddImage(GetImageSourceFromPath("pack://application:,,,/Kinect_Leaning.bmp"));
                batch4.AddImage(GetImageSourceFromPath("pack://application:,,,/Kinect_Leaning.bmp"));
                batch4.CompleteBatch(2);

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
