using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace FluxHueBridge
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public HueApiService? HueApiService { get; private set; }
        public FluxApiService? FluxApiService { get; private set; }

        private TaskbarIcon? _tbIcon;

        protected override void OnStartup(StartupEventArgs e)
        {
            HueApiService = new HueApiService();
            FluxApiService  = new FluxApiService(HueApiService);

            _tbIcon = (TaskbarIcon)FindResource("TaskbarIcon");

            if (HueApiService.IsReady)
            {
                Task.Run(() => EstablishConnection());
            }
            else
            {
                var appKey = FluxHueBridge.Properties.Settings.Default.AppKey;
                var bridgeIP = FluxHueBridge.Properties.Settings.Default.BridgeIP;

                if (string.IsNullOrWhiteSpace(appKey) || string.IsNullOrWhiteSpace(bridgeIP))
                {
                    Current.MainWindow = new MainWindow();
                    Current.MainWindow.Show();
                }
            }
        }

        public async Task EstablishConnection()
        {
            if (HueApiService == null) return;

            await HandleConnectionResult(await HueApiService.ConnectToHue());
        }

        private async Task HandleConnectionResult(bool didConnect)
        {
            await Dispatcher.Invoke<Task>(async () =>
            {
                MainWindow? window = null;

                if (MainWindow != null && MainWindow.IsVisible && MainWindow is MainWindow)
                    window = MainWindow as MainWindow;

                if (didConnect && HueApiService != null)
                {
                    var sceneSetupSuccessful = await HueApiService.InitializeScenes();

                    if (!sceneSetupSuccessful) return;

                    if (window != null) 
                        window.SceneSwitchActions = JsonSerializer.Deserialize<SceneSwitchList>(FluxHueBridge.Properties.Settings.Default.SceneSwitchJSON)?.SceneSwitches;

                    FluxApiService?.Start();
                }
                else
                {
                    if (window == null)
                        window = new MainWindow();

                    window.ConnectionErrorState();

                    MainWindow = window;
                    MainWindow.Show();
                }
            });
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _tbIcon?.Dispose(); 
            base.OnExit(e);
        }
    }
}
