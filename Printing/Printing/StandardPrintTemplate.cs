using System;
using System.Windows.Documents;
using System.Printing;
using System.Windows.Threading;

namespace Printing
{
    internal class StandardPrintTemplate : PrintTemplate
    {
        public StandardPrintTemplate(DispatcherUnhandledExceptionEventHandler errorHandler) : base(2, errorHandler)
        {
        }
            
        public override FixedPage Render()
        {
            throw new NotImplementedException();
        }
    }
}
