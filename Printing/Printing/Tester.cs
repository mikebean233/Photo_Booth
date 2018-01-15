using System;
using Printing;

namespace Testing
{
    public class Tester
    {
        static void Main(string[] args)
        {
            PrintManager printManager = PrintManager.GetInstance("pdf");
        }
    }
}
