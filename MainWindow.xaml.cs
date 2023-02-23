using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
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
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private App _app;
        private HueApiService _apiService;

        private bool _hasColorChoiceInitialized = false;

        private bool _hasNameTextInitialized = false;
        private bool _nameUpdateTaskRunning = false;
        private Stopwatch _nameTextChangeTimer = new Stopwatch();
        private float _nameTextCooldown = 2.5f;

        private Brush _defaultColor = new SolidColorBrush(Color.FromRgb(234, 238, 247));
        private Brush _warningColor = new SolidColorBrush(Color.FromRgb(255, 234, 0));
        private Brush _successColor = new SolidColorBrush(Color.FromRgb(87, 197, 104));
        private Brush _errorColor = new SolidColorBrush(Color.FromRgb(237, 69, 69));

        private string _status = "Waiting for data from f.lux";
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

        private List<SceneSwitchAction>? _sceneSwitchActions = null;
        public List<SceneSwitchAction>? SceneSwitchActions
        {
            get 
            {
                var json = Properties.Settings.Default.SceneSwitchJSON;

                if (_sceneSwitchActions == null && !string.IsNullOrWhiteSpace(json))
                    _sceneSwitchActions = JsonSerializer.Deserialize<SceneSwitchList>(json)?.SceneSwitches;

                return _sceneSwitchActions;
            }
            set
            {
                if (value == null) return;

                var switchList = new SceneSwitchList() { SceneSwitches = value };

                Properties.Settings.Default.SceneSwitchJSON = JsonSerializer.Serialize(switchList);
                Properties.Settings.Default.Save();

                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        #pragma warning disable CS8601
        #pragma warning disable CS8602
        public MainWindow()
        {
            if (!(Application.Current is App)) Application.Current.Shutdown();
            _app = Application.Current as App;

            if (_app.HueApiService == null) _app.Shutdown();
            _apiService = _app.HueApiService;

            InitializeComponent();
            DataContext = this;

            if (_apiService.HasReceivedData)
                ConnectionSuccessState();

            switch(MiredShift.GetMiredShiftType())
            {
                case MiredShift.MiredShiftType.QuiteABitWarmer: QuiteWarmerSetting.IsSelected = true; break;
                case MiredShift.MiredShiftType.SlightlyWarmer: SlightlyWarmerSetting.IsSelected = true; break;
                case MiredShift.MiredShiftType.MatchScreen: MatchScreenSetting.IsSelected = true; break;
            }

            var hasAppKey   = !string.IsNullOrWhiteSpace(Properties.Settings.Default.AppKey);
            var hasBridgeIP = !string.IsNullOrWhiteSpace(Properties.Settings.Default.BridgeIP);

            NeedAccess.Visibility   = hasAppKey ? Visibility.Collapsed  : Visibility.Visible;
            HasAccess.Visibility    = hasAppKey ? Visibility.Visible    : Visibility.Collapsed;

            if (hasAppKey) return;

            Status = "Connect to a Philips Hue Bridge to continue";
            StatusColor = _warningColor;

            BridgeSearchText.Visibility     = hasBridgeIP ? Visibility.Collapsed    : Visibility.Visible;
            BridgeAppKeyRetrival.Visibility = hasBridgeIP ? Visibility.Visible      : Visibility.Collapsed;

            if (hasBridgeIP) return;

            Task.Run(() => FindHueBridge());
        }
        #pragma warning restore CS8601
        #pragma warning restore CS8602

        protected void OnPropertyChanged([CallerMemberName]string? propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                var e = new PropertyChangedEventArgs(propertyName);
                handler(this, e);
            }
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

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Reset();
            Properties.Settings.Default.Save();
            _app.Shutdown();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void QuitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
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

                    Status = "Waiting for data from f.lux";
                    StatusColor = _defaultColor;

                    await _app.EstablishConnection();
                }
                else
                {
                    AppKeyRetrievalHeaderText.Content = "Link failed, try again!";
                    AppKeyRetrievalHeaderText.Foreground = _errorColor;

                    ConfigureFluxDisclaimerText.Visibility = Visibility.Collapsed;
                    BridgeLinkingText.Visibility = Visibility.Collapsed;
                    ConnectionButtonPanel.Visibility = Visibility.Visible;
                }
            });
        }

        private void MiredShiftSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_hasColorChoiceInitialized) { _hasColorChoiceInitialized = true; return; }

            if (e.AddedItems.Count == 0) return;

            var addedItem = e.AddedItems[0] as ComboBoxItem;

            if (addedItem == null) return;

            SetAPIConsumingControlsEnabled(false);

            Status = "Changing color...";
            StatusColor = _defaultColor;

            if (addedItem == QuiteWarmerSetting)
                MiredShift.SetMiredShiftType(MiredShift.MiredShiftType.QuiteABitWarmer);

            else if (addedItem == SlightlyWarmerSetting)
                MiredShift.SetMiredShiftType(MiredShift.MiredShiftType.SlightlyWarmer);

            else if (addedItem == MatchScreenSetting)
                MiredShift.SetMiredShiftType(MiredShift.MiredShiftType.MatchScreen);

            Task.Run(async () =>
            {
                await _apiService.ForceSceneUpdate();

                Dispatcher.Invoke(() =>
                {
                    Status = "Color changed!";
                    StatusColor = _successColor;

                    SetAPIConsumingControlsEnabled(true);
                });
            });
        }

        private void NameTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_hasNameTextInitialized) { _hasNameTextInitialized = true; return; }

            var currName = Properties.Settings.Default.SceneName;
            var originalTextBox = e.OriginalSource as TextBox;
            var newTextBox = e.Source as TextBox;

            if (newTextBox == null || originalTextBox == null) return;

            _nameTextChangeTimer.Restart();

            if (newTextBox.Text.Equals(currName) || _nameUpdateTaskRunning) return;

            Status = "Waiting for user to finish typing...";
            StatusColor = _defaultColor;

            NameCheck.Visibility = Visibility.Collapsed;
            NameSpinner.Visibility = Visibility.Visible;

            _nameUpdateTaskRunning = true;
            Task.Run(() => UpdateSceneName(currName));
        }

        private async Task UpdateSceneName(string oldName)
        {
            while (_nameTextChangeTimer.Elapsed.TotalSeconds < _nameTextCooldown)
            {
                var waitTimeSeconds = _nameTextCooldown - _nameTextChangeTimer.Elapsed.TotalSeconds;
                await Task.Delay((int)(waitTimeSeconds * 1000));
            }

            var updatedName = false;

            await Dispatcher.Invoke<Task>(async () =>
            {
                if (!NameTextBox.Text.Equals(oldName))
                {
                    SetAPIConsumingControlsEnabled(false);

                    Status = "Applying new name to scenes...";
                   
                    await _apiService.UpdateSceneNames(NameTextBox.Text);
                    updatedName = true;
                }
                else
                {
                    Status = "No changes made to scene name";
                    StatusColor = _successColor;
                }
            });

            _nameUpdateTaskRunning = false;

            Dispatcher.Invoke(() =>
            {
                SetAPIConsumingControlsEnabled(true);

                NameCheck.Visibility = Visibility.Visible;
                NameSpinner.Visibility = Visibility.Collapsed;

                if (updatedName)
                {
                    Status = "Scene names updated!";
                    StatusColor = _successColor;
                }
            });
        }

        private void ApplySceneCheckbox_Check(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;

            if (checkbox == null || !(checkbox.DataContext is SceneSwitchAction)) return;

            var action = checkbox.DataContext as SceneSwitchAction;
            var switchActions = SceneSwitchActions;

            if (action == null || switchActions == null || !(switchActions.Any(sw => sw.SceneId.Equals(action.SceneId)))) return;

            switchActions.First(sw => sw.SceneId.Equals(action?.SceneId)).ShouldSwitch = action.ShouldSwitch;
            
            var switchList = new SceneSwitchList() { SceneSwitches = switchActions };

            Properties.Settings.Default.SceneSwitchJSON = JsonSerializer.Serialize(switchList);
            Properties.Settings.Default.Save();
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            Status = "Applying scenes...";
            StatusColor = _defaultColor;

            SetAPIConsumingControlsEnabled(false);

            Task.Run(async () => 
            {
                await _apiService.ApplyScenes();

                Dispatcher.Invoke(() =>
                {
                    Status = "Scenes applied!";
                    StatusColor = _successColor;

                    SetAPIConsumingControlsEnabled(true);
                });
            });
        }

        private void SetAPIConsumingControlsEnabled(bool enabled)
        {
            Keyboard.ClearFocus();
            MiredShiftSelection.IsEnabled = enabled;
            NameTextBox.IsEnabled = enabled;
            ApplyButton.IsEnabled = enabled;
        }

        public void ConnectionSuccessState()
        {
            Status = "Receiving data from f.lux";
            StatusColor = _successColor;
        }

        public void ConnectionErrorState()
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Visibility = Visibility.Collapsed;
                NeedAccess.Visibility = Visibility.Collapsed;
                HasAccess.Visibility = Visibility.Collapsed;
                ConnectionFailed.Visibility = Visibility.Visible;

            });
        }
    }
}