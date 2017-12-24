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
using System.Resources;
using System.IO;

using System.Drawing;
using Image = System.Windows.Controls.Image;
using System.Reflection;

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
        PageResolution resolution;
        Double XRes = 100, YRes = 100;
        Printing.Page1 page;

		public MainWindow()
		{
			InitializeComponent();

			dialog = new PrintDialog();
			printServer = new LocalPrintServer();
            printerCollection = printServer.GetPrintQueues();
            page = new Printing.Page1();
            page.BeginInit();
            page.InitializeComponent();
            page.EndInit();
        }

        private enum Axis {X,Y}

        private double convertDouble(Axis axis, double value) {
            double dpi = (axis == Axis.X) ? (double) resolution.X : (double )resolution.Y;
            return dpi * value;
        }

        private int convertInt(Axis axis, double value) { return (int)convertDouble(axis, value); }

        private Image setImageSource(Image image, String fileName)
        {
            image.Source = new BitmapImage(new Uri(fileName, UriKind.RelativeOrAbsolute)); ;
            return image;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
		{
            PrintQueue targetPrinter = null;

			foreach (PrintQueue thisPrinter in printerCollection )
			{
				if (thisPrinter.Name.ToLower().Contains("pdf"))
					targetPrinter = thisPrinter;
			}

			if (targetPrinter != null)
			{
				Console.WriteLine();
				Console.WriteLine("Printer:  {0}", targetPrinter.FullName);

                PrintTicket ticket = targetPrinter.DefaultPrintTicket;
                resolution = new PageResolution( 96, 96);

                PrintCapabilities capabilities = targetPrinter.GetPrintCapabilities();
				try
				{
                    setImageSource(page.ImageTop, "Kinect_Leaning.bmp");
                    setImageSource(page.ImageCenter, "Kinect_Standing.bmp");

                    XpsDocumentWriter writer = PrintQueue.CreateXpsDocumentWriter(targetPrinter); 
                    writer.Write(page.Page1_FixedPage);
                    targetPrinter.Commit();
					printServer.Commit();
				}
				catch(Exception ex)
				{
					Console.WriteLine(ex.Message);
				}
			}

		}
	}
}
