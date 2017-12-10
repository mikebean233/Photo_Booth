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
					FixedPage fixedPage = new FixedPage();

                    Image image = new Image();
                    //var imageSource = new BitmapImage();
                    //imageSource.BeginInit();
                    //imageSource.UriSource = new Uri("PhotoBoothPicture.bmp", UriKind.RelativeOrAbsolute);
                    //imageSource.EndInit();
                    //image.Source = imageSource;

                    //image.Height = 96 * 4;
                    //image.Width  = 96 * 6;

                    ImageSource imageSource;
                    Bitmap bitmap = ((Bitmap)Properties.Resources.ResourceManager.GetObject("PhotoBoothPicture"));
                    //var bitmapImage = new BitmapImage(new Uri("pack://application:,,,/PhotoBoothPicture.bmp"));
                    using (var stream = new MemoryStream())
                    {
                        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
                        stream.Position = 0;
                        imageSource = BitmapFrame.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                    }
                    image.Source = imageSource;

                    fixedPage.Children.Add(image);

                    TextBlock textBlock = new TextBlock();
                    textBlock.Text = "------------- TEST TEXT -----------------";
                    textBlock.TextAlignment = TextAlignment.Center;
                    textBlock.FontSize = 18;
                    textBlock.Width = 100;
                    textBlock.Height = 20;

                    //fixedPage.Children.Add(textBlock);

					//XpsDocument document = new XpsDocument("thing", FileAccess.ReadWrite);
                    
					XpsDocumentWriter writer = PrintQueue.CreateXpsDocumentWriter(targetPrinter); //XpsDocument.CreateXpsDocumentWriter(targetPrinter); //GetPrintXpsDocumentWriter(); //new XpsDocumentWriter(visual);
					writer.Write(fixedPage);
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
