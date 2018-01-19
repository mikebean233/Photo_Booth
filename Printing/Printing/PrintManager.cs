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

namespace Printing
{
    public class PrintManager
    {
        private static PrintManager instance;
        private String printerNameString;
        private LocalPrintServer printServer;
        private PrintQueue thisPrinter;
        private PrintStatus status;
        private PrintTemplate template;

        private enum PrintStatus {
            NOT_READY,
            READY,
            PRINTING
        }

        private PrintManager(String name)
        {
            printServer = new LocalPrintServer();
            printerNameString = name ?? "";
            status = PrintStatus.NOT_READY;
            FindPrintQueue();
            template = PrintTemplate.ofType(PrintTemplateType.Wide, thisPrinter);
        }

        public static PrintManager GetInstance(String name)
        {
            if (instance == null)
                instance = new PrintManager(name);
            return instance;
        }

        public void SetPrinterSearchString(String value)
        {
            if(value != null)
                printerNameString = value.ToLower();
        }

        private void FindPrintQueue()
        {
            if (status == PrintStatus.PRINTING)
                throw new Exception("Cannot change printers during print!");

            foreach (PrintQueue printQueue in printServer.GetPrintQueues())
            {
                if (printQueue.Name.ToLower().Contains(printerNameString))
                {
                    thisPrinter = printQueue;
                    break;
                }
            }
            if(thisPrinter != null)
                status = PrintStatus.READY;
        }

        public int ImageCapacity()
        {
            return template.ImageCapacity();
        }

        public Boolean CanAddMoreImage()
        {
            return template.CanAddMoreImages();
        }

        public Boolean AddImage(ImageSource imageSource)
        {
            return template.AddImage(imageSource);
        }
        
        public Boolean print()
        {
            Boolean printStarted = false;
            if (!template.CanAddMoreImages() && status == PrintStatus.READY)
            {
                try
                {
                    FixedPage page = template.Render();
                    XpsDocumentWriter writer = PrintQueue.CreateXpsDocumentWriter(thisPrinter);
                    writer.Write(page);
                    thisPrinter.Commit();
                    printServer.Commit();
                    
                    printStarted = true;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }
            }

            if (printStarted)
                status = PrintStatus.PRINTING;

            return printStarted;
        }

    }
}

