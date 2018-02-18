using System;
using Printing;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace Testing
{
    internal class Tester
    {
        private PrintManager _printManager;
        private Tester()
        {
            _printManager = PrintManager.GetInstance("pdf", 2);
            _printManager.SetPrintErrorInformer(HandlePrintError);
        }

        private void HandlePrintError(String errorMessages)
        {
            System.Diagnostics.Debug.WriteLine(String.Format("================= PRINT ERRORS RECEIVED =================="));
            System.Diagnostics.Debug.WriteLine(errorMessages);

            // If there was a paper outage, simulate refilling the printerr
            if (errorMessages.Contains("out of paper"))
                _printManager.ResetRemainingPrintCount(200);

            _printManager.RetryPrintingAfterUserIntervention();
        }

        public void Run()
        {
            try
            {
                PrintManager.PrintBatchHandler batch1 = _printManager.startNewBatch(PrintTemplateType.Wide);
                batch1.AddImage(GetImageSourceFromPath("pack://application:,,,/Kinect_Standing.bmp"));
                batch1.AddImage(GetImageSourceFromPath("pack://application:,,,/Kinect_Standing.bmp"));
                batch1.CompleteBatch(1);

                PrintManager.PrintBatchHandler batch2 = _printManager.startNewBatch(PrintTemplateType.Wide);
                batch2.AddImage(GetImageSourceFromPath("pack://application:,,,/Kinect_Standing.bmp"));
                batch2.AddImage(GetImageSourceFromPath("pack://application:,,,/Kinect_Leaning.bmp"));
                batch2.CompleteBatch(1);

                PrintManager.PrintBatchHandler batch3 =_printManager.startNewBatch(PrintTemplateType.Wide);
                batch3.AddImage(GetImageSourceFromPath("pack://application:,,,/Kinect_Leaning.bmp"));
                batch3.AddImage(GetImageSourceFromPath("pack://application:,,,/Kinect_Standing.bmp"));
                batch3.CompleteBatch(1);

                PrintManager.PrintBatchHandler batch4 = _printManager.startNewBatch(PrintTemplateType.Wide);
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

        [STAThread]
        static void Main(string[] args)
        {
            (new Tester()).Run();
        }


        static ImageSource GetImageSourceFromPath(String path)
        {
            ImageSource returnValue = new BitmapImage(new Uri(path, UriKind.RelativeOrAbsolute));
            return returnValue;
        }
    }
}
