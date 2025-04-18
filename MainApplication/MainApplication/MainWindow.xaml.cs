﻿using System;
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
using System.Globalization;
using MainApplication.Configuration;
using Libs;

namespace MainApplication
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private PrintManager _printManager;
        private ConcurrentQueue<ImageCapture> _queue;
        private PrintManager.PrintBatchHandler _currentBatch;
        private ImageProducer _imageProducer;
        private bool _createNewBackground = false;
        private bool _havePrintError = false;
        private bool _loadingBackgrounds = false;
        private Thread _consumer;
        private Dictionary<Key, Action> _keyActions = new Dictionary<Key, Action>();
        private Dictionary<int, Action> _buttonActions = new Dictionary<int, Action>();
        private ButtonListener _buttonListener;
        private float _depthThresholdInMeters = 2.0f;
        private float _depthChangeStep = 0.10f;
        private float _minDepthThreshold = 0.5f;
        private float _maxDepthThreshold = 8.0f;
        private int _carouselSize = 7; // preferably an odd number
        private int _centerCarouselImageIndex;
        private int _carouselWidth = 350;  // in pixels
        private double _carouselItemHeight;
        private int _countdownLength = 3;
        private Config _config;
        private static string[] _numbers = {"first", "second", "third", "fourth", "five"};
        private ImageSource _printTemplateImage;

        // State
        private State _prevState;
        private State _currentState;

        private State SELECT_BACKGROUND;
        private State COUNTDOWN;
        private State PRINTING;
        
        private enum EventType
        {
            TRANSITION_TO_STATE,
            PREVIEW_IMAGE_ARRIVED,
            IMAGE_CAPTURED,
            TAKE_PICTURE_REQUEST,
            PRINT_ERROR
        }

        private class State
        {
            private Action<EventType, object?, Dictionary<string, object>> _eventHandler;
            private Dictionary<String, Object> _stateData;
            private String _name;

            public State(String name, Action<EventType, object?, Dictionary<string, object>> eventHandler)
            {
                _name = name;
                _eventHandler = eventHandler;
                _stateData = new Dictionary<string, object>();
            }

            public void HandleEvent(EventType thisEvent, object? args)
            {
                _eventHandler.Invoke(thisEvent, args, _stateData);
            }

            override
            public String ToString()
            { return _name; }
        }

        private void ChangeState(State newState)
        {
            _prevState = _currentState;
            _currentState = newState;
        }

        private ContinousIterator<BackgroundImage> _backgroundIterator;
        public MainWindow()
        {
            try
            {
                InitializeComponent();
                _buttonListener = new ButtonListener(ButtonPressHandler);

                // Setup key actions
                _keyActions.Add(Key.Left, NextBackgroundImage);
                _keyActions.Add(Key.Right, PrevBackgroundImage);
                _keyActions.Add(Key.Up, IncreaseDepthThreshold);
                _keyActions.Add(Key.Down, DecreaseDepthThreshold);
                _keyActions.Add(Key.Space, TakePicture);
                _keyActions.Add(Key.B, TakeBackgroundPicture);

                // Setup button actions
                _buttonActions.Add(0, TakePicture); // pin 2
                _buttonActions.Add(1, NextBackgroundImage); // pin 3
                _buttonActions.Add(2, PrevBackgroundImage); // pin 4
                _buttonActions.Add(3, IncreaseDepthThreshold); // pin 5
                _buttonActions.Add(4, DecreaseDepthThreshold); // pin 6


                _config = ConfigUtil.GetConfig();
                _printTemplateImage = new BitmapImage(new Uri(_config.PrintTemplatePath, UriKind.RelativeOrAbsolute));
                _printTemplateImage.Freeze();

                AppDomain.CurrentDomain.ProcessExit += (x, y) => ConfigUtil.SaveConfig(_config);
                _printManager = PrintManager.GetInstance(_config.PrinterName, _config);
                _printManager.SetPrintErrorInformer(HandlePrintError);
                _currentBatch = _printManager.startNewBatch(_printTemplateImage);
                _imageProducer = ImageProducerFactory.GetImageProducer();
                _imageProducer.Start();

                // Setup the background images
                _centerCarouselImageIndex = _carouselSize / 2;
                _carouselItemHeight = (double)_carouselWidth * (108.0f / 192.0f);

                LoadBackgroundImages(_config.BackgroundImagesDir);
                SetBackgroundImage();
                BuildCarousel();

                _queue = _imageProducer.GetImageQueue();

                _consumer = new Thread(new ThreadStart(Consume));
                _consumer.Start();
                UpdateStatus();

                SetupStates();
            } catch (Exception ex)
            {
                Console.Error.WriteLine($"{ex.Message}: {ex.StackTrace}");
                throw ex;
            }
        }


        private void SetupStates()
        {
            SELECT_BACKGROUND = new State("SELECT_BACKGROUND", (eventType, eventArgs, stateData) =>
            {
                switch (eventType)
                {
                    case EventType.TRANSITION_TO_STATE:
                        ChangeState(SELECT_BACKGROUND);
                        Dispatcher.Invoke(() => tabControl.SelectedIndex = 0);

                        if (_currentBatch.TemplateFull)
                            _currentBatch = _printManager.startNewBatch(_printTemplateImage);

                        UpdateStatus();
                        break;
                    case EventType.PREVIEW_IMAGE_ARRIVED:
                        if (!_havePrintError)
                            Dispatcher.Invoke(() => backgroundPreview.Source = eventArgs as ImageSource);
                        break;
                    case EventType.TAKE_PICTURE_REQUEST:
                        if (!_havePrintError)
                            COUNTDOWN.HandleEvent(EventType.TRANSITION_TO_STATE, null);
                        break;
                    case EventType.PRINT_ERROR:
                        DefaultPrintErrorHandler(eventArgs as String);
                        break;
                }
            });

            COUNTDOWN = new State("COUNTDOWN", (eventType, eventArgs, stateData) =>
            {
                Thread countdownThread;

                switch (eventType)
                {
                    case EventType.TRANSITION_TO_STATE:
                        ChangeState(COUNTDOWN);
                        Dispatcher.Invoke(() => { tabControl.SelectedIndex = 1; countdownPreview.Source = backgroundPreview.Source; });

                        countdownThread = new Thread(() =>
                        {
                            while (_havePrintError)
                            { }

                            int countDown = _countdownLength;
                            while (countDown > 0)
                            {
                                Dispatcher.Invoke(() => countdownLabel.Content = countDown.ToString());
                                Thread.Sleep(500);
                                Dispatcher.Invoke(() => countdownLabel.Content = "");
                                Thread.Sleep(500);
                                countDown--;
                            }

                            Dispatcher.Invoke(() => countdownLabel.Content = "SMILE !");
                            Thread.Sleep(2000);
                            Dispatcher.Invoke(() => countdownLabel.Content = "");
                            //_readyForCapture = true;
                            _imageProducer.SetConfiguration(new Dictionary<string, object> { { "captureHighQuality", true } });
                        });
                        countdownThread.Start();
                        break;
                    case EventType.PREVIEW_IMAGE_ARRIVED:
                        if (!_havePrintError)
                            Dispatcher.Invoke(() => countdownPreview.Source = eventArgs as ImageSource);
                        break;
                    case EventType.IMAGE_CAPTURED:
                        var image = eventArgs as BitmapSource;

                        if (_createNewBackground)
                        {
                            _createNewBackground = false;
                            var timeStamp = ImageFileService.WriteImageFile("Capture", image, _config);
                            _backgroundIterator.Add(new BackgroundImage(timeStamp, image));
                            _backgroundIterator.MoveLast();
                            UpdateCarousel();

                            Dictionary<String, BitmapSource> outMap = new Dictionary<string, BitmapSource>();
                            outMap.Add(timeStamp, image);
                            _imageProducer.SetConfiguration(new Dictionary<string, object> { { "addBackgroundImages", outMap } });
                        }
                        else
                        {
                            _currentBatch.AddImage(image);
                            ImageFileService.WriteImageFile("Capture", image, _config);
                        }

                        if (_currentBatch.TemplateFull)
                            Dispatcher.InvokeAsync(() => PRINTING.HandleEvent(EventType.TRANSITION_TO_STATE, null));
                        else
                            Dispatcher.InvokeAsync(() => SELECT_BACKGROUND.HandleEvent(EventType.TRANSITION_TO_STATE, null));

                        break;
                    case EventType.PRINT_ERROR:
                        DefaultPrintErrorHandler(eventArgs as String);
                        break;
                }
            });


            PRINTING = new State("PRINTING", (eventType, eventArgs, stateData) =>
            {
                switch (eventType)
                {
                    case EventType.TRANSITION_TO_STATE:
                        ChangeState(PRINTING);
                        Dispatcher.Invoke(() => tabControl.SelectedIndex = 2);

                        _currentBatch.CompleteBatch(_config.CopyCount);
                        Thread waitThread = new Thread(() =>
                        {   _imageProducer.Pause();
                            Thread.Sleep(3000);

                            while (_havePrintError)
                            { }

                            _imageProducer.Continue();
                            Dispatcher.Invoke(() => SELECT_BACKGROUND.HandleEvent(EventType.TRANSITION_TO_STATE, null));
                        });
                        waitThread.Start();
                        break;
                    case EventType.PRINT_ERROR:
                        DefaultPrintErrorHandler(eventArgs as String); 
                        break;
                }
            });
            SELECT_BACKGROUND.HandleEvent(EventType.TRANSITION_TO_STATE, null);
        }

        private void DefaultPrintErrorHandler(String errorMessage)
        {
            _havePrintError = true;
            PrinterErrorDialog errorDialog = new PrinterErrorDialog(errorMessage);
            
            errorDialog.BringIntoView();
            errorDialog.ShowDialog();

            if(errorDialog.WasOutOfPaper)
                _printManager.ResetRemainingPrintCount(errorDialog.PrintCount);
            _printManager.RetryPrintingAfterUserIntervention();
            _havePrintError = false;
        }

        private void UpdateStatus()
        {
            String statusText = _loadingBackgrounds ? "loading background images..." : String.Format("picture {0} of {1}", _currentBatch.AddedImageCount + 1, _currentBatch.TemplateImageCapacity);
            String promptText = String.Format("choose {0} background", _numbers[_currentBatch.AddedImageCount]);
            Dispatcher.Invoke(() => Label_remCount.Content = statusText);
            Dispatcher.Invoke(() => prompt.Content = promptText);
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
            _imageProducer.SetConfiguration(new Dictionary<string, object> { { "selectBackgroundImage", _backgroundIterator.Current.Name } });
        }

        private void Consume()
        {
            while (true)
            {
                try
                {
                    Thread.Sleep(1);
                    ImageCapture thisImage = null;
                    if (_queue.TryDequeue(out thisImage))
                    {

                        Dispatcher.Invoke(() =>
                        {
                            _currentState.HandleEvent(
                                thisImage.CaptureType == CaptureType.PREVIEW
                                    ? EventType.PREVIEW_IMAGE_ARRIVED
                                    : EventType.IMAGE_CAPTURED, thisImage.Image);
                        });
                    }
                }
                catch (ThreadInterruptedException ex)
                {
                    Console.WriteLine(ex);
                    break;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"{ex.Message} : {ex.StackTrace}");
                }
            }
        }

        private void UpdateCarousel()
        {
            // Set the carousel images
            int itemIndex = 0;
            foreach (ListViewItem item in ListView_Carousal.Items)
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

            for (int i = 0; i < _carouselSize; ++i)
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
                    listViewItem.BorderBrush = new LinearGradientBrush(new GradientStopCollection(new GradientStop[]{ new GradientStop(Colors.Red, 0), new GradientStop(Colors.Black, .5), new GradientStop(Colors.Red, 1)}));

                }
                else
                {
                    listViewItem.Opacity = 0.7f;
                }

                listView.Items.Add(listViewItem);
            }

            // Calculate and set the courasel height
            ListView_Carousal.Height = _carouselItemHeight * _carouselSize;
        }

        private void LoadBackgroundImages(String bmpDir)
        {
            _loadingBackgrounds = true;
            UpdateStatus();
            if (Directory.Exists(bmpDir))
            {
                String[] filePaths = Directory.GetFiles(bmpDir, "*.bmp");

                int index = 0;
                BackgroundImage[] backgroundImages = new BackgroundImage[filePaths.Count()];
                Dictionary<string, BitmapSource> backgroundImageMap = new Dictionary<string, BitmapSource>();

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
                            BackgroundImage thisBackgroundImage =
                                backgroundImages[index++] = new BackgroundImage(name, thisImage);
                            backgroundImageMap.Add(name, thisBackgroundImage.FlippedImage);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"{ex.Message}: {ex.StackTrace}");
                    }
                }
                // Trim the array in case we didn't use all of the images
                Array.Resize<BackgroundImage>(ref backgroundImages, index);

                _backgroundIterator = new ContinousIterator<BackgroundImage>(backgroundImages);
                _imageProducer.SetConfiguration(new Dictionary<string, object> { { "setBackgroundImages", backgroundImageMap } });
            }
            _loadingBackgrounds = false;
            UpdateStatus();
        }

        private void HandlePrintError(string? errorMessages)
        {
            _currentState.HandleEvent(EventType.PRINT_ERROR, errorMessages);
        }

        private void Button_takePicture_Click(object sender, RoutedEventArgs e)
        {
            TakePicture();
        }

        private void TakeBackgroundPicture()
        {
            _createNewBackground = true;
            TakePicture();
        }


        private void TakePicture()
        {
            _currentState.HandleEvent(EventType.TAKE_PICTURE_REQUEST, null);
        }

        private void IncreaseDepthThreshold()
        {
            if (_depthThresholdInMeters + _depthChangeStep <= _maxDepthThreshold)
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
            _imageProducer.SetConfiguration(new Dictionary<string, object> { { "depthThreshold", _depthThresholdInMeters } });
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _imageProducer.Cleanup();
            _buttonListener.Cleanup();
            _consumer.Interrupt();
            _printManager.Cleanup();
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
