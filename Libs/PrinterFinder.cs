using System.Printing;

namespace Libs
{
    public class PrintFinder
    {
        public static PrintQueue? FindPrinter(String printerName)
        {
            PrintServer printServer = new LocalPrintServer();
            
            foreach (PrintQueue printQueue in printServer.GetPrintQueues())
            {
                if (printQueue.Name.ToLower().Contains(printerName))
                {
                    return printQueue;
                }
            }
            return null;
        }
    }
}
