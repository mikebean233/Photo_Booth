using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Imaging;
using System.Threading;
using System.Collections.Concurrent;
using System.IO;
using ButtonDriver;
using Printing;
using System.Windows.Controls;

namespace MainApplication
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private PrintManager _printManager;
        private ConcurrentQueue<ImageSource> _queue;
        private PrintManager.PrintBatchHandler _currentBatch;
        private ImageProducer _imageProducer;
        private bool _readyForCapture = false;
        private Thread _consumer;
        private Dictionary<Key, Action> _keyActions = new Dictionary<Key, Action>();
        private Dictionary<int, Action> _buttonActions = new Dictionary<int, Action>();
        private ButtonListener _buttonListener;
        private float _depthThresholdInMeters = 2.0f;
        private float _depthChangeStep = 0.25f;
        private float _minDepthThreshold = 0.5f;
        private float _maxDepthThreshold = 8.0f;
        private int _carouselSize = 7; // preferably an odd number
        private int _centerCarouselImageIndex;
        private int _carouselWidth = 250;  // in pixels
        private double _carouselItemHeight;



        private ContinousIterator<BackgroundImage> _backgroundIterator;
        public MainWindow()
        {
            InitializeComponent();

            _buttonListener = new ButtonListener(ButtonPressHandler);

            // Setup key actions
            _keyActions.Add(Key.Left , NextBackgroundImage);
            _keyActions.Add(Key.Right, PrevBackgroundImage);
            _keyActions.Add(Key.Up   , IncreaseDepthThreshold);
            _keyActions.Add(Key.Down , DecreaseDepthThreshold);
            _keyActions.Add(Key.Space, TakePicture);

            // Setup button actions
            _buttonActions.Add(0, TakePicture);            // pin 2
            _buttonActions.Add(1, PrevBackgroundImage);    // pin 3
            _buttonActions.Add(2, NextBackgroundImage);    // pin 4
            _buttonActions.Add(3, IncreaseDepthThreshold); // pin 5
            _buttonActions.Add(4, DecreaseDepthThreshold); // pin 6
             

            //StartDialog startDialog = new StartDialog();
            //startDialog.ShowDialog();
            //_printManager = PrintManager.GetInstance(startDialog.Name, startDialog.PrintCount);
            _printManager = PrintManager.GetInstance("hiti", 10);
            _printManager.SetPrintErrorInformer(HandlePrintError);
            _currentBatch = _printManager.startNewBatch(PrintTemplateType.Wide);

            _imageProducer = ImageProducerFactory.GetImageProducer();
            _imageProducer.Start();

            // Setup the background images
            _centerCarouselImageIndex = _carouselSize / 2;
            _carouselItemHeight = (double)_carouselWidth * (108.0f / 192.0f);

            LoadBackgroundImages();
            SetBackgroundImage();
            BuildCarousel();

            _queue = _imageProducer.GetImageQueue();

            _consumer = new Thread(new ThreadStart(Consume));
            _consumer.Start();
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            String statusText = "";
            if (_currentBatch.BatchFinishedPrinting)
                statusText = "session finished, images sent to printer";
            else
                statusText = String.Format("{0}/{1}", _currentBatch.AddedImageCount, _currentBatch.TemplateImageCapacity);

            Dispatcher.Invoke(new Action(() => Label_remCount.Content = statusText));
        }

        private void NextBackgroundImage()
        {
            _backgroundIterator.MoveNext();
            SetBackgroundImage();
            Dispatcher.Invoke(UpdateCarousel);
        }
        
        private void PrevBackgroundImage()
        {
            _backgroundIterator.MovePrev();
            SetBackgroundImage();
            Dispatcher.Invoke(UpdateCarousel);
        }

        private void SetBackgroundImage()
        {
            _imageProducer.SetConfiguration(ImageProducerConfiguration.Simple("selectBackgroundImage", _backgroundIterator.Current.Name));
        }

        private void Consume()
        {
            try
            {
                while (true)
                {
                    ImageSource thisImage = null;
                    if (_queue.TryDequeue(out thisImage))
                    {

                        Dispatcher.Invoke(new Action(() =>
                        {
                            Image_preview.Source = thisImage;
                            if (_readyForCapture)
                            {
                                _readyForCapture = false;
                                _currentBatch.AddImage(thisImage);
                                if (_currentBatch.RemainingImageCount == 0)
                                    _currentBatch.CompleteBatch(1);

                                UpdateStatus();
                            }
                        }));
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void UpdateCarousel()
        {
            // Set the carousel images
            int itemIndex = 0;
            foreach (ListViewItem  item in ListView_Carousal.Items)
            {
                int offset = itemIndex++ - _centerCarouselImageIndex;
                Image image = (Image)item.Content;
                image.Source = _backgroundIterator.PeekRelative(offset).Image;
            }
        }

        private void BuildCarousel()
        {
            Image image;
            ListViewItem listViewItem;
            ListView listView = ListView_Carousal;
            Thickness zeroThickness = new Thickness(0);

            for(int i = 0; i < _carouselSize; ++i)
            {
                image = new Image();
                image.HorizontalAlignment = HorizontalAlignment.Stretch;
                image.VerticalAlignment = VerticalAlignment.Stretch;
                image.Source = _backgroundIterator.PeekRelative(i - _centerCarouselImageIndex).Image;

                listViewItem = new ListViewItem();
                listViewItem.Width = _carouselWidth;
                listViewItem.BorderThickness = zeroThickness;
                listViewItem.Padding = zeroThickness;
                listViewItem.Content = image;
                
                if (i == _centerCarouselImageIndex)
                {
                    listViewItem.BorderThickness = new Thickness(10);
                    listViewItem.BorderBrush = Brushes.Red;
                }

                listView.Items.Add(listViewItem);
            }

            // Calculate and set the courasel height
            ListView_Carousal.Height = _carouselItemHeight * _carouselSize;
        }

        private void LoadBackgroundImages()
        {
            String bmpDir = AppDomain.CurrentDomain.BaseDirectory + "backgroundImages";
            if (Directory.Exists(bmpDir))
            {
                String[] filePaths = Directory.GetFiles(bmpDir, "*.bmp");

                int index = 0;
                BackgroundImage[] backgroundImages = new BackgroundImage[filePaths.Count()];
                Dictionary<string, BitmapImage> backgroundImageMap = new Dictionary<string, BitmapImage>();

                foreach (String path in filePaths)
                {
                    try
                    {
                        String name = System.IO.Path.GetFileNameWithoutExtension(path);
                        BitmapImage thisImage = new BitmapImage(new Uri(path, UriKind.RelativeOrAbsolute));
                        thisImage.Freeze();

                        // Only accept images of the correct size
                        if (thisImage.PixelHeight == 1080 && thisImage.PixelWidth == 1920)
                        {
                            backgroundImageMap.Add(name, thisImage);
                            backgroundImages[index++] = new BackgroundImage(name, thisImage);
                        }
                    }
                    catch (Exception ex)
                    { }
                }
                // Trim the array in case we didn't use all of the images
                Array.Resize<BackgroundImage>(ref backgroundImages, index);

                _backgroundIterator = new ContinousIterator<BackgroundImage>(backgroundImages);
                _imageProducer.SetConfiguration(ImageProducerConfiguration.Simple("loadBackgroundImages", backgroundImageMap));
            }
        }

        private void HandlePrintError(String errorMessages)
        {
            System.Diagnostics.Debug.WriteLine(errorMessages);
        }

        private void Button_takePicture_Click(object sender, RoutedEventArgs e)
        {
            TakePicture();
        }

        private void TakePicture()
        {
            if (_currentBatch.RemainingImageCount > 0)
                _readyForCapture = true;
        }

        private void IncreaseDepthThreshold()
        {
            if (_depthThresholdInMeters + _depthChangeStep<= _maxDepthThreshold)
            {
                _depthThresholdInMeters += _depthChangeStep;
                UpdateDepthThreshold();
            }

        }

        private void DecreaseDepthThreshold()
        {
            if (_depthThresholdInMeters - _depthChangeStep >= _minDepthThreshold)
            {

                _depthThresholdInMeters -= _depthChangeStep;
                UpdateDepthThreshold();
            }
        }

        private void UpdateDepthThreshold()
        {
            _imageProducer.SetConfiguration(ImageProducerConfiguration.Simple("depthThreshold", _depthThresholdInMeters));
        }
        
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _imageProducer.Cleanup();
            _buttonListener.Cleanup();
            _consumer.Interrupt();
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            Key key = e.Key;
            if (_keyActions.ContainsKey(key))
                _keyActions[key].BeginInvoke(null, null);
        }

        private void ButtonPressHandler(HashSet<int> buttons)
        {
            foreach (int pressedButton in buttons)
                if (_buttonActions.ContainsKey(pressedButton))
                    _buttonActions[pressedButton].BeginInvoke(null, null);
        }
    }
}
