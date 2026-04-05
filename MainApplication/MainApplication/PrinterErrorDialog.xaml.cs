using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Printing;

namespace MainApplication
{
    /// <summary>
    /// Interaction logic for PrinterErrorDialog.xaml
    /// </summary>
    public partial class PrinterErrorDialog : Window
    {
        private String _printErrors;
        private bool _needPrintCount;
        public bool WasOutOfPaper { get { return _needPrintCount; } }

        private int _printCount;
        public int PrintCount {get{return _printCount;} }

        public void ValidateInput()
        {
            Button_Ok.IsEnabled = _printCount > 0;
        }

        public PrinterErrorDialog(String printErrors, RibbonMonitor ribbonMonitor = null)
        {
            InitializeComponent();
            Window.Topmost = true;
            _printErrors = printErrors ?? "";
            _needPrintCount = _printErrors.Contains("out of paper");
            TextBlock_errorMessages.Text = _needPrintCount ? "The printer is out of paper, please find the groom and ask him to refill the printer." : _printErrors;
            Button_Ok.IsEnabled = !_needPrintCount;
            TextBox_printCount.Visibility = _needPrintCount ? Visibility.Visible : Visibility.Collapsed;
            Label_printCount.Visibility = _needPrintCount ? Visibility.Visible : Visibility.Collapsed;
            Window.Title = _needPrintCount ? "OUT OF PAPER" : "Print Error";
            Window.Activate();

            // Auto-fill the remaining count from the printer if Bidi is available
            // (the operator may have already swapped in a fresh ribbon)
            if (_needPrintCount && ribbonMonitor != null)
            {
                int? count = ribbonMonitor.GetRemainingPrints();
                if (count.HasValue && count.Value > 0)
                {
                    _printCount = count.Value;
                    TextBox_printCount.Text = count.Value.ToString();
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TextBox_printCount_TextChanged(object sender, TextChangedEventArgs e)
        {
            String value = ((TextBox)sender).Text;
            bool parseSucceded = int.TryParse(value, out _printCount);
            ValidateInput();
        }
    }
}
