using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace FluxHueBridge
{
    public class FluxApiService
    {
        private HueApiService _hueApiService;
        private HttpListener _listener;
        private string _url = "http://localhost:8000/";

        private bool _hasBegunListening = false;

        public FluxApiService(HueApiService hueApiService)
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(_url);
            _hueApiService = hueApiService;
        }

        public void Start()
        {
            Task.Run(async () => { await HandleIncomingConnections(); });
            Task.Run(async () => { await RestartFluxProcess(); });
        }

        private async Task HandleIncomingConnections()
        {
            _listener.Start();

            try
            {
                while (true)
                {
                    _hasBegunListening = true;
                    var context = await _listener.GetContextAsync();

                    var request     = context.Request;
                    var response    = context.Response;

                    response.Close();

                    if (request.Url == null) continue;

                    var colorTemp = HttpUtility.ParseQueryString(request.Url.Query).Get("ct");
                    var brightness = HttpUtility.ParseQueryString(request.Url.Query).Get("bri");

                    if (colorTemp == null || brightness == null) continue;

                    var adaptedBrightness = Math.Round(Math.Clamp(float.Parse(brightness) * 100f, 0f, 100f), 1);
                    adaptedBrightness = CorrectBrightness(adaptedBrightness);

                    await _hueApiService.UpdateScenes(int.Parse(colorTemp), adaptedBrightness, false);
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.ToString());
                _listener.Stop();
            }
        }

        private async Task RestartFluxProcess()
        {
            while (!_hasBegunListening)
                await Task.Delay(1000);

            var process = Process.GetProcessesByName("flux");

            if (process == null || process.Length == 0) return;

            var path = process.First().MainModule?.FileName;
            
            process.First().Kill();
            Process.Start(path ?? "");
        }

        //Convert brightness from range of 30-100 (the one f.lux sends) to 52-100 (the one f.lux actually uses)
        private double CorrectBrightness(double value)
        {
            double oldMin = 30;
            double newMin = 52;
            double oldRange = 100 - oldMin;
            double newRange = 100 - newMin;

            return (((value - oldMin) * newRange) / oldRange) + newMin;
        }
    }
}
