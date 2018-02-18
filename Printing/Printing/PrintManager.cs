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
        private String printErrors = "";
        private bool inError = false;
        private Queue<IPrintBatch> printQueue = new Queue<IPrintBatch>();
        private Object queueLock = new Object();

        private IPrintBatch thisPrintBatch;
        //PrintTicket printTicket;

        private PrintManager(String name)
        {
            printServer = new LocalPrintServer();
            SetPrinterSearchString(name);
            FindPrinter();
            thisPrinter.Refresh();
            thisPrintBatch = NullPrintBatch.Instance;
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

        private void FindPrinter()
        {
            // Don't allow choosing another printer for this instance of Print Manager
            if (thisPrinter != null)
                return;

            foreach (PrintQueue printQueue in printServer.GetPrintQueues())
            {
                if (printQueue.Name.ToLower().Contains(printerNameString))
                {
                    thisPrinter = printQueue;
                    break;
                }
            }
            if (thisPrinter == null)
                throw new Exception(String.Format("Cannot find printer that matches name of \"{0}\"", printerNameString)); 

        }

        #region QueueManagement
        private void EnQueuePrintBatch(IPrintBatch batch)
        {
            if (batch.Template.CanAddMoreImages())
                throw new Exception("Cannot print a print batch before it has all of the its images");
            Boolean doPrint = false;
            lock (queueLock)
            { 
                printQueue.Enqueue(batch);

                if (printQueue.Count == 1)
                    doPrint = true;
            }
            if (doPrint)
                Print();
        }

        private IPrintBatch DeQueuePrintBatch()
        {
            lock (queueLock)
            {
                if (printQueue.Count > 0)
                    return printQueue.Dequeue();
                else
                    return NullPrintBatch.Instance;
            }
        }
        #endregion

        #region Printing
        public void ResolveNonPaperOutPrintError()
        {
            inError = false;
            Print();
        }

        public void ResolvePaperOutPrintError(int paperStockPrintCount)
        {
            // TODO: Implement paper paper out detection
        }

        private void Print()
        {
            thisPrinter.Refresh();

            // TODO: Try to detect paper outage with a print counter

            if (!inError)
                inError = CheckPrinterForErrorState();

            // TODO: Make this thread safe
            // Get our current print Batch(if we don't already have it)
            if (thisPrintBatch.BatchFinishedPrinting())
                thisPrintBatch = DeQueuePrintBatch();

            if (!inError && !thisPrintBatch.BatchFinishedPrinting())
            {
                try
                {
                    FixedPage page = thisPrintBatch.Template.Render();
                    thisPrinter.UserPrintTicket.PageBorderless = PageBorderless.Borderless;
                    thisPrinter.UserPrintTicket.PhotoPrintingIntent = PhotoPrintingIntent.PhotoBest;
                    thisPrinter.UserPrintTicket.PageMediaSize = new PageMediaSize(4, 6);
                    thisPrinter.Commit();
                    //printTicket = thisPrinter.UserPrintTicket;
                    XpsDocumentWriter writer = PrintQueue.CreateXpsDocumentWriter(thisPrinter);
                    
                    writer.Write(page);
                    
                    
                    // Pretend we know that this print worked and tell our current print batch that it did.
                    thisPrintBatch.RegisterSucessfullPrint();

                    // Print the next page (eventually this should run our of batches or a print error will happen...)
                    Print();
                }
                catch (Exception ex)
                {
                    // TODO: Specifically look for paper out exceptions
                    printErrors = ex.Message;
                    inError = true;
                    InformClientOfPrintProblems(printErrors);
                }
            }
        }

        private Boolean CheckPrinterForErrorState()
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

        #endregion

        private void InformClientOfPrintProblems(String problemDescription)
        {
            // TODO: Implement this
        }


        
        public PrintBatchHandler startNewBatch(PrintTemplateType printTemplateType)
        {
            return new PrintBatchHandler(printTemplateType, this);
        }
        
        #region PrintBatchHandler
        public class PrintBatchHandler
        {
            private PrintBatch _printBatch;
            
            public PrintBatchHandler(PrintTemplateType templateType, PrintManager printManager)
            {
                _printBatch = new PrintBatch(PrintTemplate.OfType(templateType), printManager);
            }

            public Boolean AddImage(ImageSource imageSource)
            {
                return _printBatch.Template.AddImage(imageSource);
            }

            public bool CompleteBatch(int copyCount)
            {
                return _printBatch.QueueBatchForPrinting(copyCount);
            }
            public bool BatchFinished()
            {
                return _printBatch.BatchFinishedPrinting();
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
            private int _remainingCopiesInBatch;

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
                    throw new ArgumentOutOfRangeException("PrintBatch must have at least one copy to print");

                if (_template.CanAddMoreImages())
                {
                    return false;
                }
                else
                {
                    _totalCopiesInbatch = _remainingCopiesInBatch = totalCopiesInBatch;
                    _printManager.EnQueuePrintBatch(this);
                    return true;
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
                return _remainingCopiesInBatch <= 0;
            }
        }
        #endregion
    }
}

