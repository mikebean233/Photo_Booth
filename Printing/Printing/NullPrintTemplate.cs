using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Documents;
using System.Windows.Controls;

namespace Printing
{
    class NullPrintTemplate : PrintTemplate
    {
        private static NullPrintTemplate _instance = new NullPrintTemplate();
        public static NullPrintTemplate Instance { get { return _instance; } }

        private NullPrintTemplate() : base(0){}
        public new Boolean CanAddMoreImages(){return false;}
        public new Boolean AddImage(ImageSource imageSource) {throw new NotImplementedException(); }
        public override FixedPage Render(){throw new NotImplementedException();}
    }
}
