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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Printing;

namespace MainApplication
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private PrintManager _printManager;


        public MainWindow()
        {
            InitializeComponent();
            
            StartDialog startDialog = new StartDialog();
            startDialog.ShowDialog();
            _printManager = PrintManager.GetInstance(startDialog.Name, startDialog.PrintCount);
            _printManager.SetPrintErrorInformer(HandlePrintError);
            
            TestErrorDialog("Something \nwent \nwrong, \ngood luck with taht ;)");
            TestErrorDialog("Blah Blah Blah\n out of paper Blah\n Blah\n");
        }

        private void TestErrorDialog(String errorMessages)
        {
            PrinterErrorDialog dialog = new PrinterErrorDialog(errorMessages);
            dialog.ShowDialog();
            System.Diagnostics.Debug.WriteLine("PrintCount: {0}",dialog.PrintCount);
        }

        private void HandlePrintError(String errorMessages)
        {
                

        }
    }
}
