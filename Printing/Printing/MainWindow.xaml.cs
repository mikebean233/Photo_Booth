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
using System.Windows.Xps;
using System.Windows.Xps.Packaging;
using System.Printing;
using System.IO;

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
			PrintQueue targetPrinter = null;
			foreach (PrintQueue thisPrinter in printerCollection )
			{
				//Console.WriteLine("{0} {1} {2} {3} {4}",thisPrinter.FullName, thisPrinter.Description, thisPrinter.DefaultPriority, thisPrinter.HasPaperProblem, thisPrinter.IsDirect);
				if (thisPrinter.Name.ToLower().Contains("pdf"))
					targetPrinter = thisPrinter;
			}

			if (targetPrinter != null)
			{
				Console.WriteLine();
				Console.WriteLine("Printer:  {0}", targetPrinter.FullName);

				PrintTicket ticket = targetPrinter.DefaultPrintTicket;
				PrintCapabilities capabilities = targetPrinter.GetPrintCapabilities();
				try
				{
					Visual visual = this.GetVisualChild(0);
//					FixedPage fixedPage = new FixedPage();

					//XpsDocument document = new XpsDocument("thing", FileAccess.ReadWrite);

					XpsDocumentWriter writer = PrintQueue.CreateXpsDocumentWriter(targetPrinter); //XpsDocument.CreateXpsDocumentWriter(targetPrinter); //GetPrintXpsDocumentWriter(); //new XpsDocumentWriter(visual);
					writer.Write(visual);
					targetPrinter.Commit();
					printServer.Commit();
					
					//PrintSystemJobInfo PrintJob = targetPrinter.AddJob( AddJob("new Job", ticket);
				}
				catch(Exception ex)
				{
					Console.WriteLine(ex.Message);
				}
			}

		}
	}
}
