using System;
using Printing;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace MainApplication
{
    class PrintTester
    {
        private PrintManager _printManager;
        public PrintTester()
        {
            _printManager = PrintManager.GetInstance("hiti", 2);
            _printManager.SetPrintErrorInformer(HandlePrintError);
        }

        private void HandlePrintError(String errorMessages)
        {
            System.Diagnostics.Debug.WriteLine(String.Format("================= PRINT ERRORS RECEIVED =================="));
            System.Diagnostics.Debug.WriteLine(errorMessages);

            // If there was a paper outage, simulate refilling the printer
            if (errorMessages.Contains("out of paper"))
                _printManager.ResetRemainingPrintCount(200);

            _printManager.RetryPrintingAfterUserIntervention();
        }

        public void Run()
        {
            try 
            {
                ImageSource[] images = new ImageSource[]
                {
                    GetImageSourceFromPath("backgroundImages/beach2.bmp"),
                    GetImageSourceFromPath("backgroundImages/mario.bmp")

                };


                PrintManager.PrintBatchHandler batch1 = _printManager.startNewBatch(PrintTemplateType.Wide);
                batch1.AddImage(images[0]);
                batch1.AddImage(images[1]);
                batch1.CompleteBatch(1);
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
            returnValue.Freeze();
            return returnValue;
        }
    }
}