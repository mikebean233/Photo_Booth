using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Printing;
using System.Windows.Media;
using System.Windows.Documents;
using System.Windows.Xps;
using static System.Net.Mime.MediaTypeNames;
using System.Windows.Documents.Serialization;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.Threading;

namespace Printing
{
    public class PrintManager
    {
        private static PrintManager instance;
        private String printerNameString;
        private LocalPrintServer printServer;
        private PrintQueue thisPrinter;
        private PrintStatus status;
        //private PrintTemplate template;
        private Object printingLock = new object();
        private String printErrors = "";

        PrintTicket printTicket;

        // TODO: Use Queue instead of one reference
        private PrintBatch thisPrintBatch;

        private enum PrintStatus {
            NOT_READY,
            READY,
            PRINTING
        }
        
        private PrintManager(String name)
        {
            printServer = new LocalPrintServer();
            SetPrinterSearchString(name);
            status = PrintStatus.NOT_READY;
            FindPrintQueue();
            thisPrinter.Refresh();
           
            thisPrintBatch = new PrintBatch(PrintTemplate.ofType(PrintTemplateType.Wide, this.Dispatcher_UnhandledException),2);
        }

        public static PrintManager GetInstance()
        {
            String printerName;
            #if DEBUG
                printerName = "pdf";
            #else
                printerName = "hiti";
            #endif
            return GetInstance(printerName);
        }

        public static PrintManager GetInstance(String name)
        {
            if (instance == null)
                instance = new PrintManager(name);
            return instance;
        }

        public void SetPrinterSearchString(String name)
        {
            name = name ?? "";
            printerNameString = name.ToLower();
        }

        private void FindPrintQueue()
        {
            if (status == PrintStatus.PRINTING)
                throw new Exception("Cannot change printers during print!");

            Object thing = printServer.GetPrintQueues();

            foreach (PrintQueue printQueue in printServer.GetPrintQueues())
            {
                if (printQueue.Name.ToLower().Contains(printerNameString))
                {
                    thisPrinter = printQueue;
                    break;
                }
            }
            if(thisPrinter != null && !havePrinterError())
                status = PrintStatus.READY;
        }

        public int ImageCapacity()
        {
            return thisPrintBatch.Template.ImageCapacity();
        }

        public Boolean CanAddMoreImage()
        {
            return thisPrintBatch.Template.CanAddMoreImages();
        }

        public Boolean AddImage(ImageSource imageSource)
        {
            return thisPrintBatch.Template.AddImage(imageSource);
        }
        
        public Boolean print()
        {
            Boolean printStarted = false;
            thisPrinter.Purge();
            if (!thisPrintBatch.Template.CanAddMoreImages() && status == PrintStatus.READY && !havePrinterError())
            {
                try
                {
                    
                    FixedPage    page = thisPrintBatch.Template.Render();
                    
                    thisPrinter.UserPrintTicket.PageBorderless = PageBorderless.Borderless;
                    thisPrinter.UserPrintTicket.PhotoPrintingIntent = PhotoPrintingIntent.PhotoBest;
                    thisPrinter.UserPrintTicket.PageMediaSize = new PageMediaSize(4, 6);
                    thisPrinter.Commit();
                    printTicket = thisPrinter.UserPrintTicket;
                    XpsDocumentWriter writer = PrintQueue.CreateXpsDocumentWriter(thisPrinter);

                    writer.WritingCancelled += Writer_WritingCancelled;
                    writer.WritingCompleted += Writer_WritingCompleted;
                    writer.WritingPrintTicketRequired += Writer_WritingPrintTicketRequired;
                    writer.WritingProgressChanged += Writer_WritingProgressChanged;

                    writer.Write(page);
                    
                    printStarted = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }

            if (printStarted)
                status = PrintStatus.PRINTING;
            else
            {
                System.Diagnostics.Debug.WriteLine("=========== Print Queue Errors ===========");
                System.Diagnostics.Debug.WriteLine(printErrors);
                System.Diagnostics.Debug.WriteLine("============================================");
            }

            return printStarted;
        }

        private void Dispatcher_UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("=========== Dispatcher Exception ===========");
            System.Diagnostics.Debug.WriteLine("{0}", e.Exception.StackTrace);
            System.Diagnostics.Debug.WriteLine("============================================");

        }

        private void Writer_WritingProgressChanged(object sender, WritingProgressChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("=========== Writing Progress Changed ===========");
            System.Diagnostics.Debug.WriteLine("     Progress:{0}", e.ProgressPercentage);
            System.Diagnostics.Debug.WriteLine("    Use State:{0}", e.UserState);
            System.Diagnostics.Debug.WriteLine("Writing Level:{0}", e.WritingLevel);
            System.Diagnostics.Debug.WriteLine("       Number:{0}", e.Number);
            System.Diagnostics.Debug.WriteLine("================================================");
        }

        private void Writer_WritingPrintTicketRequired(object sender, WritingPrintTicketRequiredEventArgs e)
        {
            //e.CurrentPrintTicket = printTicket;
            //throw new NotImplementedException();
        }

        private void Writer_WritingCompleted(object sender, WritingCompletedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("=========== Writing Completed ===========");
            System.Diagnostics.Debug.WriteLine("============================================");
        }

        private void Writer_WritingCancelled(object sender, WritingCancelledEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("=========== Writing Canceled ===========");
            System.Diagnostics.Debug.WriteLine("============================================");
        }

        private Boolean havePrinterError()
        {
            PrintQueue pq = thisPrinter;

            String statusReport = "";
            if ((pq.QueueStatus & PrintQueueStatus.PaperProblem) == PrintQueueStatus.PaperProblem)
            {
                statusReport = statusReport + "Has a paper problem. ";
            }
            if ((pq.QueueStatus & PrintQueueStatus.NoToner) == PrintQueueStatus.NoToner)
            {
                statusReport = statusReport + "Is out of toner. ";
            }
            if ((pq.QueueStatus & PrintQueueStatus.DoorOpen) == PrintQueueStatus.DoorOpen)
            {
                statusReport = statusReport + "Has an open door. ";
            }
            if ((pq.QueueStatus & PrintQueueStatus.Error) == PrintQueueStatus.Error)
            {
                statusReport = statusReport + "Is in an error state. ";
            }
            if ((pq.QueueStatus & PrintQueueStatus.NotAvailable) == PrintQueueStatus.NotAvailable)
            {
                statusReport = statusReport + "Is not available. ";
            }
            if ((pq.QueueStatus & PrintQueueStatus.Offline) == PrintQueueStatus.Offline)
            {
                statusReport = statusReport + "Is off line. ";
            }
            if ((pq.QueueStatus & PrintQueueStatus.OutOfMemory) == PrintQueueStatus.OutOfMemory)
            {
                statusReport = statusReport + "Is out of memory. ";
            }
            if ((pq.QueueStatus & PrintQueueStatus.PaperOut) == PrintQueueStatus.PaperOut)
            {
                statusReport = statusReport + "Is out of paper. ";
            }
            if ((pq.QueueStatus & PrintQueueStatus.OutputBinFull) == PrintQueueStatus.OutputBinFull)
            {
                statusReport = statusReport + "Has a full output bin. ";
            }
            if ((pq.QueueStatus & PrintQueueStatus.PaperJam) == PrintQueueStatus.PaperJam)
            {
                statusReport = statusReport + "Has a paper jam. ";
            }
            if ((pq.QueueStatus & PrintQueueStatus.Paused) == PrintQueueStatus.Paused)
            {
                statusReport = statusReport + "Is paused. ";
            }
            if ((pq.QueueStatus & PrintQueueStatus.TonerLow) == PrintQueueStatus.TonerLow)
            {
                statusReport = statusReport + "Is low on toner. ";
            }
            if ((pq.QueueStatus & PrintQueueStatus.UserIntervention) == PrintQueueStatus.UserIntervention)
            {
                statusReport = statusReport + "Needs user intervention. ";
            }

            if ("".Equals(statusReport))
            {
                return false;
            }
            else
            {
                printErrors = statusReport;
                return true;
            }
        }

        private class PrintBatch
        {
            static int printBatchCount = 0;
            private int successfullPrintCount = 0;

            private int _totalCopiesInbatch;
            private int _remainingCopiesInBatch;

            private PrintTemplate _template;
            public PrintTemplate Template { get { return _template; } }

            private String _id;
            public String Id{get {return _id;}}

            IList<String> _printErrors = new List<String>();
            public IList<String> PrintErrors { get { return new List<String>(_printErrors); } }
            
            
            public PrintBatch(PrintTemplate template, int totalCopiesInBatch)
            {
                if (template == null)
                    throw new NullReferenceException("parameter template was null for PrintBatch constructor");

                if (totalCopiesInBatch < 1)
                    throw new ArgumentOutOfRangeException("PrintBatch must have at least one copy to print");

                _totalCopiesInbatch = _remainingCopiesInBatch =  totalCopiesInBatch;
                
                _template = template;
                buildId();
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            private void buildId()
            {
                _id = (printBatchCount++) + "_" + DateTime.Today.ToShortTimeString();
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns>
            ///     True: There are more prints left in this batch
            ///     False: There are no more prints left in this batch
            /// </returns>
            [MethodImpl(MethodImplOptions.Synchronized)]
            public Boolean registerSucessfullPrint()
            {
                if (_remainingCopiesInBatch > 0)
                    _remainingCopiesInBatch--;

                return batchFinished();
            }

            public Boolean batchFinished()
            {
                return _remainingCopiesInBatch > 0;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            public void registerPrintError(String error)
            {
                _printErrors.Add(error);
            }
        }
    }
}

