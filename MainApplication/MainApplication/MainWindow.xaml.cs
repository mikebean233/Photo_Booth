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

            String testMessage = "This is a test print error message";




        }


        private void HandlePrintError(String errorMessages)
        {
            

        }
    }
}
