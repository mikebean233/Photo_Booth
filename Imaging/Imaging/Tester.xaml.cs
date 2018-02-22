using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
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
using System.Collections.Concurrent;

namespace Imaging
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Tester : Window
    {
        ImageProducer _imageProducer;
        ConcurrentQueue<ImageSource> _queue;
        public Tester()
        {
            InitializeComponent();

            _imageProducer = ImageProducerFactory.GetImageProducer();
            _queue = _imageProducer.GetImageQueue();
            System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 0, 0, 50);
            timer.Tick += (sender, e) =>
            {
                ImageSource thisImage = null;
                if (_queue.TryDequeue(out thisImage))
                    Image_preview.Source = thisImage;
                Label_count.Content = _queue.Count + "";
            };
            timer.Start();

        }
    }
}
