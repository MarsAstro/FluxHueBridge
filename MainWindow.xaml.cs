﻿using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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

namespace FluxHueBridge
{
    public partial class MainWindow : Window
    {
        private App _app;
        private HueApiService _apiService;

        private string _status = "Connect to a Philips Hue Bridge to continue";
        public string Status { get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
            }
        }

        private Brush _statusColor = new SolidColorBrush(Color.FromRgb(234, 238, 247));
        public Brush StatusColor { get => _statusColor;
            set
            {
                _statusColor = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #pragma warning disable CS8601
        #pragma warning disable CS8602
        #pragma warning disable CS8618 
        public MainWindow()
        {
            if (!(Application.Current is App)) Application.Current.Shutdown();
            _app = Application.Current as App;

            if (_app.HueApiService == null) _app.Shutdown();
            _apiService = _app.HueApiService;

            InitializeComponent();
            DataContext = this;

            var hasAppKey   = !string.IsNullOrWhiteSpace(Properties.Settings.Default.AppKey);
            var hasBridgeIP = !string.IsNullOrWhiteSpace(Properties.Settings.Default.BridgeIP);

            NeedAccess.Visibility   = hasAppKey ? Visibility.Collapsed  : Visibility.Visible;
            HasAccess.Visibility    = hasAppKey ? Visibility.Visible    : Visibility.Collapsed;

            if (hasAppKey) return;

            BridgeSearchText.Visibility     = hasBridgeIP ? Visibility.Collapsed    : Visibility.Visible;
            BridgeAppKeyRetrival.Visibility = hasBridgeIP ? Visibility.Visible      : Visibility.Collapsed;

            if (hasBridgeIP) return;

            Task.Run(() => FindHueBridge());
        }
        #pragma warning restore CS8601
        #pragma warning restore CS8602
        #pragma warning restore CS8618 

        protected void OnPropertyChanged([CallerMemberName]string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                var e = new PropertyChangedEventArgs(propertyName);
                handler(this, e);
            }
        }

        public void ConnectionErrorState()
        {
            Dispatcher.Invoke(() =>
            {
                NeedAccess.Visibility       = Visibility.Collapsed;
                HasAccess.Visibility        = Visibility.Collapsed;
                ConnectionFailed.Visibility = Visibility.Visible;
            });
        }

        public async Task FindHueBridge()
        {
            // delay to make user feel like finding bridge takes more time than a fraction of a second, is all about that UX baby
            await Task.Delay(1500); 

            var foundIP = await _apiService.DiscoverHueBridgeIP();

            Dispatcher.Invoke(() =>
            {
                BridgeSearchText.Visibility = Visibility.Collapsed;

                if (foundIP)
                    BridgeAppKeyRetrival.Visibility = Visibility.Visible;
                else
                    BridgeSearchFailedText.Visibility = Visibility.Visible;
            });
        }

        private void ButtonQuit_Click(object sender, RoutedEventArgs e)
        {
            _app.Shutdown();
        }

        private void ConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            ConnectionButtonPanel.Visibility = Visibility.Collapsed;
            BridgeLinkingText.Visibility = Visibility.Visible;

            Task.Run(() => Connect());
        }

        private async Task Connect()
        {
            var generatedAppKey = await _apiService.GenerateAppKey();

            await Dispatcher.Invoke(async () =>
            {
                if (generatedAppKey)
                {
                    NeedAccess.Visibility = Visibility.Collapsed;
                    HasAccess.Visibility = Visibility.Visible;

                    await _app.EstablishConnection();
                }
                else
                {
                    AppKeyRetrievalHeaderText.Content = "Link failed, try again!";
                    AppKeyRetrievalHeaderText.Foreground = new SolidColorBrush(Color.FromRgb(237, 69, 69));
                    ConfigureFluxDisclaimerText.Visibility = Visibility.Collapsed;
                    BridgeLinkingText.Visibility = Visibility.Collapsed;
                    ConnectionButtonPanel.Visibility = Visibility.Visible;
                }
            });
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Reset();
            Properties.Settings.Default.Save();
            _app.Shutdown();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}