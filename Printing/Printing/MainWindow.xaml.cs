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
using System.Printing;

namespace Printing
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		PrintDialog dialog;
		LocalPrintServer printServer;
		PrintQueueCollection printerCollection;

		public MainWindow()
		{
			InitializeComponent();

			dialog = new PrintDialog();
			printServer = new LocalPrintServer();
			printerCollection = printServer.GetPrintQueues();


		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			foreach (var thisPrinter in printerCollection )
			{
				 Console.WriteLine("{0} {1} {2} {3} {4}",thisPrinter.FullName, thisPrinter.Description, thisPrinter.DefaultPriority, thisPrinter.HasPaperProblem, thisPrinter.IsDirect);
			}
			
		}
	}
}
