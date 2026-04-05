using System;
using System.Collections.Generic;
using System.Printing;
using System.Windows.Media;
using System.Windows.Documents;
using System.Windows.Xps;
using System.Threading;
using System.Windows.Xps.Packaging;
using Libs;
using System.Drawing.Printing;
using System.Windows.Media.Imaging;
using System.Windows;

namespace Printing
{
    public class PrintManager
    {
        private static PrintManager _instance;
        private String _printerNameString;
        private PrintQueue _thisPrinter;
        private String _printErrors = "";
        private bool _inError = false;
        private Queue<IPrintBatch> _printQueue = new Queue<IPrintBatch>();
        private Object _queueLock = new Object();
        private IPrintBatch _thisPrintBatch;
        private Action<String> _printErrorInformer;
        private ManualResetEvent _doPrintSignal;
        private ManualResetEvent _killSignal;
        private Config _config;
        private RibbonMonitor _ribbonMonitor;

        #region Construction
        private PrintManager(String name, Config config)
        {
            _thisPrintBatch = NullPrintBatch.Instance;
            _doPrintSignal = new ManualResetEvent(false);
            _killSignal = new ManualResetEvent(false);
            _config = config;

            Thread printWorker = new Thread(() =>
            {
                SetPrinterSearchString(name);

                _thisPrinter = PrintFinder.FindPrinter(_config.PrinterName);
                
                if(_thisPrinter == null)
                {
                    String message = "Could not find printer with name containing: " + _printerNameString;
                    MessageBox.Show(message, "Printer Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                    throw new Exception(message);
                }

                _thisPrinter.Refresh();
                _ribbonMonitor = new RibbonMonitor(_thisPrinter.FullName);
                SyncRemainingPrintsFromPrinter();

                while (!_killSignal.WaitOne(0))
                {
                    if (_doPrintSignal.WaitOne(0))
                    {
                        _doPrintSignal.Reset();
                        Print();
                    }
                }
            });
            printWorker.SetApartmentState(ApartmentState.STA);
            printWorker.Start();
        }
        
        public static PrintManager GetInstance(String name, Config config)
        {
            if (_instance == null)
                _instance = new PrintManager(name, config);
            return _instance;
        }
        #endregion

        public void Cleanup()
        {
            _killSignal.Set();
            _ribbonMonitor?.Dispose();
        }


        #region Setup
        private void SetPrinterSearchString(String name)
        {
            name = name ?? "";
            _printerNameString = name.ToLower();
        }

        public void SetPrintErrorInformer(Action<String> informer)
        {
            _printErrorInformer = informer;
        }

        public RibbonMonitor RibbonMonitor { get { return _ribbonMonitor; } }

        #endregion


        #region QueueManagement

        private bool EnQueuePrintBatch(IPrintBatch batch)
        {
            bool doPrint = false;
            bool addedBatch = false;

            if (batch.Template.CanAddMoreImages())
                throw new Exception("Cannot print a print batch before it has all of the its images");
            
            lock (_queueLock)
            {
                if (!_printQueue.Contains(batch))
                {
                    _printQueue.Enqueue(batch);
                    addedBatch = true;

                    if (_printQueue.Count > 0)
                        doPrint = true;
                }
            }
            if (doPrint)
                _doPrintSignal.Set();

            return addedBatch;
        }

        private IPrintBatch DeQueuePrintBatch()
        {
            lock (_queueLock)
            {
                if (_printQueue.Count > 0)
                    return _printQueue.Dequeue();
                else
                    return NullPrintBatch.Instance;
            }
        }
        #endregion

        #region Printing
        public void RetryPrintingAfterUserIntervention()
        {
            _inError = false;
            Print();
        }

        // Call this before RetryPrinting() if we refilled the printer with paper
        public void ResetRemainingPrintCount(int remainingPrints)
        {
            _config.RemainingPrints = remainingPrints;
        }

        // TODO: Make this thread safe
        private void Print()
        {
            try
            {
                _thisPrinter.Refresh();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
            }

            // Get our current print Batch(if we don't already have it)
            if (_thisPrintBatch.BatchFinishedPrinting())
                _thisPrintBatch = DeQueuePrintBatch();

            if (!_thisPrintBatch.BatchFinishedPrinting())
            {
                try
                {
                    // See if our printer is in an error state
                    if (CheckPrinterForErrorState())
                    {
                        _inError = true;
                     }
                    else
                    {
                        FixedPage page = (_thisPrintBatch.Template.Clone() as PrintTemplate).Render();
                        if (_config.UseGDIPrinting)
                        {
                            var printDocument = new PrintDocument();
                            printDocument.PrinterSettings.PrinterName = _thisPrinter.Name;
                            printDocument.PrintPage += (s, e) =>
                            {
                                using (var bmp = page.ToBMP(_config))
                                {
                                    e.Graphics.PageUnit = System.Drawing.GraphicsUnit.Pixel;
                                    e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                                    e.Graphics.DrawImage(bmp, 0, 0, bmp.Width, bmp.Height);
                                }
                            };

                            printDocument.Print();
                        }
                        else {
                            // Original print implemenation (doesn't provide as much control over the print quality as GDI)
                            _thisPrinter.UserPrintTicket.PageBorderless = PageBorderless.Borderless;
                            _thisPrinter.UserPrintTicket.PhotoPrintingIntent = PhotoPrintingIntent.None;
                            _thisPrinter.UserPrintTicket.PageMediaSize = new PageMediaSize(4, 6);
                            _thisPrinter.Commit();
                            XpsDocumentWriter writer = PrintQueue.CreateXpsDocumentWriter(_thisPrinter);

                            writer.Write(page);
                        }
                        SaveRenderedTemplate(page);
                        _thisPrintBatch.RegisterSucessfullPrint();

                        SyncRemainingPrintsFromPrinter(decrementFallback: true);

                        // Print the next page (eventually this should run out of batches or a print error will happen...)
                        Print();
                    }
                }
                catch (Exception ex)
                {
                    _printErrors = ex.Message;
                    _inError = true;
                }
            }

            if(_inError)
            {
                InformClientOfPrintProblems(_printErrors);
            }
        }

        private void SaveRenderedTemplate(FixedPage page)
        {
            try
            {
                ImageFileService.WriteImageFile("Template", page, _config);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"{ex.Message}: {ex.StackTrace}");
            }
        }

        private Boolean CheckPrinterForErrorState()
        {
            PrintQueue pq = _thisPrinter;
            String statusReport = "";
            if ((pq.QueueStatus & PrintQueueStatus.PaperProblem) == PrintQueueStatus.PaperProblem)
            {
                statusReport = statusReport + "The printer has a paper problem. ";
            }
            if ((pq.QueueStatus & PrintQueueStatus.NoToner) == PrintQueueStatus.NoToner)
            {
                statusReport = statusReport + "The printer is out of toner. ";
            }
            if ((pq.QueueStatus & PrintQueueStatus.DoorOpen) == PrintQueueStatus.DoorOpen)
            {
                statusReport = statusReport + "The printer has an open door. ";
            }
            if ((pq.QueueStatus & PrintQueueStatus.Error) == PrintQueueStatus.Error)
            {
                statusReport = statusReport + "The printer is in an error state. ";
            }
            if ((pq.QueueStatus & PrintQueueStatus.NotAvailable) == PrintQueueStatus.NotAvailable)
            {
                statusReport = statusReport + "The printer is not available. ";
            }
            if ((pq.QueueStatus & PrintQueueStatus.Offline) == PrintQueueStatus.Offline)
            {
                statusReport = statusReport + "The printer is off line. ";
            }
            if ((pq.QueueStatus & PrintQueueStatus.OutOfMemory) == PrintQueueStatus.OutOfMemory)
            {
                statusReport = statusReport + "The printer is out of memory. ";
            }
            SyncRemainingPrintsFromPrinter();
            if ((pq.QueueStatus & PrintQueueStatus.PaperOut) == PrintQueueStatus.PaperOut || _config.RemainingPrints <= 0)
            {
                _config.RemainingPrints = 0;
                statusReport = statusReport + "The printer is out of paper. ";
            }
            else if (_ribbonMonitor != null && _ribbonMonitor.IsLowOnRibbon())
            {
                statusReport = statusReport + $"Warning: only {_config.RemainingPrints} prints remaining on ribbon. ";
            }
            if ((pq.QueueStatus & PrintQueueStatus.OutputBinFull) == PrintQueueStatus.OutputBinFull)
            {
                statusReport = statusReport + "The printer has a full output bin. ";
            }
            if ((pq.QueueStatus & PrintQueueStatus.PaperJam) == PrintQueueStatus.PaperJam)
            {
                statusReport = statusReport + "The printer has a paper jam. ";
            }
            if ((pq.QueueStatus & PrintQueueStatus.Paused) == PrintQueueStatus.Paused)
            {
                statusReport = statusReport + "The printer is paused. ";
            }
            if ((pq.QueueStatus & PrintQueueStatus.TonerLow) == PrintQueueStatus.TonerLow)
            {
                statusReport = statusReport + "The printer is low on toner. ";
            }
            if ((pq.QueueStatus & PrintQueueStatus.UserIntervention) == PrintQueueStatus.UserIntervention)
            {
                statusReport = statusReport + "The printer needs user intervention. ";
            }

            if ("".Equals(statusReport))
            {
                return false;
            }
            else
            {
                _printErrors = statusReport;
                return true;
            }
        }

        #endregion

        /// <summary>
        /// Sync the remaining prints count from the printer via Bidi if available,
        /// otherwise fall back to decrementing the manual counter.
        /// </summary>
        private void SyncRemainingPrintsFromPrinter(bool decrementFallback = false)
        {
            int? actual = _ribbonMonitor?.GetRemainingPrints();
            if (actual.HasValue)
                _config.RemainingPrints = actual.Value;
            else if (decrementFallback)
                _config.RemainingPrints--;
        }

        private void InformClientOfPrintProblems(String problemDescription)
        {
            if (_printErrorInformer != null)
                _printErrorInformer(problemDescription);
        }
 
        public PrintBatchHandler startNewBatch(ImageSource templateImage)
        {
            return new PrintBatchHandler(templateImage, this);
        }

        #region PrintBatchHandler
        public class PrintBatchHandler
        {
            private PrintBatch _printBatch;
            private int _addedImageCount = 0;

            public int RemainingImageCount    { get { return _printBatch.Template.ImageCapacity - _addedImageCount; } }
            public bool TemplateFull          { get { return _printBatch.Template.ImageCapacity == _addedImageCount; } }
            public int TemplateImageCapacity  { get { return _printBatch.Template.ImageCapacity;}}
            public int AddedImageCount        { get { return _addedImageCount;}}
            public bool BatchFinishedPrinting { get { return _printBatch.BatchFinishedPrinting(); }}

            public PrintBatchHandler(ImageSource templateImage, PrintManager printManager)
            {
                var templateInstance = new WidePrintTemplate(templateImage);
                _printBatch = new PrintBatch(templateInstance, printManager);
            }

            public Boolean AddImage(ImageSource imageSource)
            {
                if (_printBatch.Template.AddImage(imageSource))
                {
                    ++_addedImageCount;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            public void CompleteBatch(int copyCount)
            {
                _printBatch.QueueBatchForPrinting(copyCount);
            }

        }
        #endregion

        private interface IPrintBatch
        {
            PrintTemplate Template { get; }
            bool BatchFinishedPrinting();
            void RegisterSucessfullPrint();
            bool QueueBatchForPrinting(int totalCopiesInBatch);
        }

        private class NullPrintBatch : IPrintBatch
        {
            private static NullPrintBatch _instance = new NullPrintBatch();
            public static NullPrintBatch Instance { get { return _instance; } }

            private NullPrintBatch() { }
            public PrintTemplate Template { get { return NullPrintTemplate.Instance; } }
            public bool BatchFinishedPrinting(){return true;}
            public bool QueueBatchForPrinting(int totalCopiesInBatch){throw new NotImplementedException();}
            public void RegisterSucessfullPrint(){throw new NotImplementedException();}
        }

        #region PrintBatch
        private class PrintBatch : IPrintBatch
        {
            static int printBatchCount = 0;
            private int _totalCopiesInbatch;
            private int _remainingCopiesInBatch = -1;

            private PrintTemplate _template;
            public PrintTemplate Template { get { return _template; } }

            private String _id;
            public String Id{get {return _id;}}

            private PrintManager _printManager;
            
            public PrintBatch(PrintTemplate template, PrintManager printManager)
            {
                _template = template;
                _printManager = printManager;
                BuildId();
            }

            // We won't know how many prints the user wants to make until they are finished taking pictures, so we're getting 
            // that upon completion of the photo session(hence totalCopiesInBatch not being passed into the constructor)
            public bool QueueBatchForPrinting(int totalCopiesInBatch)
            {
                if (totalCopiesInBatch < 1)
                    throw new ArgumentOutOfRangeException("Print batch must have at least one copy to print");

                if (_template.CanAddMoreImages())
                {
                    return false;
                }
                else
                {
                    _totalCopiesInbatch = _remainingCopiesInBatch = totalCopiesInBatch;
                    return _printManager.EnQueuePrintBatch(this);
                }
            }

            private void BuildId()
            {
                _id = (printBatchCount++) + "_" + DateTime.Today.ToShortTimeString();
            }

            public void RegisterSucessfullPrint() 
            {
                if (!BatchFinishedPrinting())
                    _remainingCopiesInBatch--;
            }

            public Boolean BatchFinishedPrinting()
            {
                return _remainingCopiesInBatch == 0;
            }

            public override bool Equals(object obj)
            {
                return obj != null && obj.GetType() == GetType() && ((PrintBatch)obj).Id == _id;
            }

            public override int GetHashCode()
            {
                return 2108858624 + EqualityComparer<string>.Default.GetHashCode(Id);
            }
        }
        #endregion
    }
}

