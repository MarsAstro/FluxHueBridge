using System.Text.Json;
using System.Text.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows;
using System.Threading;

namespace FluxHueBridge
{
    public class HueApiService
    {
        public bool IsReady => _httpClient.BaseAddress != null;

        private bool _hasReceivedData = false;
        public bool HasReceivedData
        {
            get => _hasReceivedData;
            private set
            {
                if (_hasReceivedData != value && value == true) 
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (Application.Current.MainWindow != null && Application.Current.MainWindow is MainWindow)
                        {
                            var window = Application.Current.MainWindow as MainWindow;
                            window?.ConnectionSuccessState();
                        }

                        _hasReceivedData = value;
                    });
                }
            }
        }

        private IPAddress? _bridgeIP;
        private HttpClient _httpClient = new HttpClient();
        private Uri _bridgeDisoveryURI = new Uri("https://discovery.meethue.com");

        private int? _prevKelvin = null;
        private double? _prevBrightness = null;
        private LightColor? _prevColor = null;
        private List<Guid> _sceneIds = new List<Guid>();

        private bool _isDoingSceneWork = false;
        private Stopwatch _sceneWatch = new Stopwatch();
        private Stopwatch _lightWatch = new Stopwatch();

        private readonly SemaphoreSlim _sceneLock = new SemaphoreSlim(1,1);

        public HueApiService()
        {
            IPAddress.TryParse(Properties.Settings.Default.BridgeIP, out _bridgeIP);
            
            var appKey = Properties.Settings.Default.AppKey;

            if (!string.IsNullOrWhiteSpace(appKey) && _bridgeIP != null)
                SetupHttpClient(appKey, _bridgeIP.ToString());
        }

        public async Task ApplyScenes()
        {
            if (_prevBrightness == null || _prevColor == null) return;

            var switchList = JsonSerializer.Deserialize<SceneSwitchList>(Properties.Settings.Default.SceneSwitchJSON);

            if (switchList == null || switchList.SceneSwitches.Count == 0) return;

            var scenesToApply = switchList.SceneSwitches.Where(sw => sw.ShouldSwitch).Select(sw => sw.SceneId).ToList();

            if (scenesToApply.Count == 0) return;

            await _sceneLock.WaitAsync();
            _isDoingSceneWork = true;

            var json = JsonSerializer.Serialize(new SceneRecallPut());
            var body = new StringContent(json, Encoding.UTF8, "application/json");

            foreach (var sceneId in scenesToApply)
            {
                await Task.Delay(GetRequestDelayMS(_sceneWatch, 1000));

                await _httpClient.PutAsync("scene/" + sceneId, body);

                _sceneWatch.Restart();
            }

            _sceneWatch.Stop();

            _isDoingSceneWork = false;
            _sceneLock.Release();
        }

        public async Task ForceSceneUpdate()
        {
            if (_prevKelvin == null || _prevBrightness == null) return;

            await _sceneLock.WaitAsync();
            _isDoingSceneWork = true;
            _sceneLock.Release();

            await UpdateScenes(_prevKelvin.Value, _prevBrightness.Value, true);
        }

        public async Task UpdateScenes(int kelvin, double brightness, bool overrideSkip)
        {
            if (_sceneIds.Count == 0 || !overrideSkip && (_isDoingSceneWork || kelvin.Equals(_prevKelvin))) return;

            await _sceneLock.WaitAsync();
            _isDoingSceneWork = true;

            var lightColor = LightColorCalculator.GetLightColorFromKelvin(kelvin);

            try
            {
                foreach (var sceneId in _sceneIds)
                {
                    var json = await GenerateJsonForScenePut(kelvin, brightness, sceneId, lightColor);

                    if (json == null) continue;

                    var body = new StringContent(json, Encoding.UTF8, "application/json");

                    await Task.Delay(GetRequestDelayMS(_sceneWatch, 1000));

                    await _httpClient.PutAsync("scene/" + sceneId, body);

                    _sceneWatch.Restart();
                }

                _sceneWatch.Stop();
                _lightWatch.Stop();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            _prevKelvin = kelvin;
            _prevBrightness = brightness;
            _prevColor = lightColor;

            HasReceivedData = true;

            _isDoingSceneWork = false;
            _sceneLock.Release();
        }

        public async Task UpdateSceneNames(string newName)
        {
            Properties.Settings.Default.SceneName = newName;
            Properties.Settings.Default.Save();

            var stopWatch = new Stopwatch();

            foreach (var sceneId in _sceneIds)
            {
                var json = JsonSerializer.Serialize(new SceneNamePut());
                var body = new StringContent(json, encoding: Encoding.UTF8, "application/json");

                await Task.Delay(GetRequestDelayMS(stopWatch, 1000));

                await _httpClient.PutAsync("scene/" + sceneId, body);

                stopWatch.Restart();
            }
        }

        private async Task<string?> GenerateJsonForScenePut(int kelvin, double brightness, Guid sceneId, LightColor lightColor)
        {
            var scenePut = new ScenePut();

            var response    = await _httpClient.GetAsync("scene/" + sceneId.ToString());
            var scenes      = await GetFluxSceneData(response);

            if (scenes == null || scenes.Count == 0) return null;

            var areLightsUnchanged = true;
            var applyOnStartup = false;
            
            if (!HasReceivedData)
                applyOnStartup = JsonSerializer.Deserialize<SceneSwitchList>(Properties.Settings.Default.SceneSwitchJSON)?
                    .SceneSwitches.FirstOrDefault(sw => sw.SceneId == sceneId)?.ShouldSwitch ?? false;

            foreach (var action in scenes.First().Actions.Where(action => action.Target.Rtype.Equals("light")))
            {
                var newAction = new ActionElement();
                newAction.Target = new Group() { Rid = action.Target.Rid, Rtype = "light" };
                newAction.Action = new ActionAction();
                newAction.Action.On = new On() { OnOn = true };
                newAction.Action.Dimming = new Dimming() { Brightness = brightness };

                if (lightColor.IsMirek)
                    newAction.Action.ColorTemperature = new ActionColorTemperature() { Mirek = lightColor.Mirek };
                else
                    newAction.Action.Color = new ActionColor() { Xy = new Xy() { X = lightColor.X, Y = lightColor.Y } };

                scenePut.Actions.Add(newAction);

                areLightsUnchanged &= await IsLightEqualToPrevScene(action.Target.Rid, lightColor, applyOnStartup);
            }

            if (areLightsUnchanged) scenePut.Recall = new Recall();


            return scenePut.Actions.Count() > 0 ? JsonSerializer.Serialize(scenePut) : null;
        }

        private async Task<bool> IsLightEqualToPrevScene(Guid lightId, LightColor lightColor, bool applyOnStartup)
        {
            if (_prevBrightness == null || _prevColor == null) return applyOnStartup;

            try
            {
                await Task.Delay(GetRequestDelayMS(_lightWatch, 100));

                var response    = await _httpClient.GetAsync("light/" + lightId.ToString());

                _lightWatch.Restart();

                var content     = await response.Content.ReadAsStringAsync();
                var json        = JsonSerializer.Deserialize<LightModel>(content);

                if (json == null || json.Data == null || json.Data.Length == 0) return false;

                var lightData = json.Data.First();

                bool isOn;
                bool hasSameBrightness;
                bool hasSameColor;

                isOn                = lightData.On.On;

                var brightnessDiff = Math.Abs(_prevBrightness.Value - lightData.Dimming.Brightness);
                hasSameBrightness   = brightnessDiff < 2;

                if (_prevColor.IsMirek && lightData.ColorTemperature.Mirek != null)
                {
                    var mirekDiff = Math.Abs(_prevColor.Mirek - lightData.ColorTemperature.Mirek.Value);

                    hasSameColor = mirekDiff < 10;
                }
                else
                {
                    double xDiff = 0.0015;
                    double yDiff = 0.0003;

                    var isXSame = Math.Abs(lightData.Color.Xy.X - _prevColor.X) <= xDiff;
                    var isYSame = Math.Abs(lightData.Color.Xy.Y - _prevColor.Y) <= yDiff;

                    hasSameColor = isXSame && isYSame;
                }

                return isOn && hasSameBrightness && hasSameColor;
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());

                return false;
            }
        }

        private int GetRequestDelayMS(Stopwatch stopWatch, int maxDelayMS)
        {
            if (!stopWatch.IsRunning) return 0;

            stopWatch.Stop();
            var delay = (int)(maxDelayMS - stopWatch.ElapsedMilliseconds);

            return delay > 0 ? delay : 0;
        }

        private void SetupHttpClient(string appKey, string bridgeIP)
        {
            _httpClient.Dispose();

            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (request, cert, chain, errors) => true;

            _httpClient = new HttpClient(handler);
            _httpClient.DefaultRequestHeaders.Add("hue-application-key", appKey);
            _httpClient.BaseAddress = new Uri("https://" + bridgeIP.ToString() + "/clip/v2/resource/");
        }

        public async Task<bool> ConnectToHue()
        {
            try
            {
                var response = await _httpClient.GetAsync("device");

                if (response.IsSuccessStatusCode) return true;

                var foundIP = await DiscoverHueBridgeIP();

                if (!foundIP || _bridgeIP == null) return false;

                SetupHttpClient(Properties.Settings.Default.AppKey, _bridgeIP.ToString());

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                return false;
            }
        }

        public async Task<bool> InitializeScenes()
        {
            if (_httpClient == null) return false;

            try
            {
                var sceneResponse   = await _httpClient.GetAsync("scene");
                var scenes          = await GetFluxSceneData(sceneResponse);

                var roomResponse    = await _httpClient.GetAsync("room");
                var rooms           = await GetGroupData(roomResponse);

                var zoneResponse    = await _httpClient.GetAsync("zone");
                var zones           = await GetGroupData(zoneResponse);

                var roomAndZoneCount = rooms.Count() + zones.Count();

                var oldSceneSwitchJSON = Properties.Settings.Default.SceneSwitchJSON;
                var oldSceneSwitchList = string.IsNullOrWhiteSpace(oldSceneSwitchJSON) 
                    ? new SceneSwitchList() 
                    : JsonSerializer.Deserialize<SceneSwitchList>(oldSceneSwitchJSON);

                if (oldSceneSwitchList == null) return false;

                var newSceneSwitchList = new SceneSwitchList();

                RemoveRoomsAndZonesWithFluxScenes(scenes, rooms, zones, oldSceneSwitchList, newSceneSwitchList);

                await AddFluxSceneToGroups(rooms, newSceneSwitchList);
                await AddFluxSceneToGroups(zones, newSceneSwitchList);

                var newSceneSwitchJSON = JsonSerializer.Serialize(newSceneSwitchList);
                if (!newSceneSwitchJSON.Equals(oldSceneSwitchJSON))
                {
                    Properties.Settings.Default.SceneSwitchJSON = newSceneSwitchJSON;
                    Properties.Settings.Default.Save();
                }

                if (_sceneIds.Count == roomAndZoneCount)
                    return true;
                else
                    return false;
                    
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());

                return false;
            }
        }

        private async Task AddFluxSceneToGroups(List<GroupData> groups, SceneSwitchList sceneSwitchList)
        {
            foreach (var group in groups)
            {
                var sceneToPost = await GenerateFluxSceneForGroup(group);

                if (sceneToPost.Actions.Count == 0) continue;

                var sceneJson = JsonSerializer.Serialize(sceneToPost);

                if (sceneJson == null) continue;

                var body = new StringContent(sceneJson, Encoding.UTF8, "application/json");

                var response    = await _httpClient.PostAsync("scene", body);
                var content     = await response.Content.ReadAsStringAsync();

                var jsonObject  = JsonSerializer.Deserialize<ScenePostModel>(content);

                if (jsonObject == null || jsonObject.Data.Length == 0) continue;

                var newId = jsonObject.Data.First().Rid;
                var newName = group.Metadata.Name;

                _sceneIds.Add(newId);

                sceneSwitchList.SceneSwitches.Add(new SceneSwitchAction() { SceneId = newId, Name = newName });
            }
        }

        private async Task<ScenePost> GenerateFluxSceneForGroup(GroupData group)
        {
            var scenePost = new ScenePost();

            scenePost.Group = new Group(){ Rid = group.Id, Rtype = "room"};

            foreach (var device in group.Children)
            {
                Guid? lightServiceId = device.Rid;

                if (!device.Rtype.Equals("light"))
                    lightServiceId = await IsDeviceLight(device.Rid.ToString());

                if (lightServiceId == null) continue;

                var newAction = new ActionElement();
                newAction.Target = new Group() { Rid = lightServiceId.Value, Rtype = "light" };
                newAction.Action = new ActionAction();
                newAction.Action.On =  new On() { OnOn = true };
                newAction.Action.Dimming = new Dimming() { Brightness = 100 };
                newAction.Action.ColorTemperature = new ActionColorTemperature() { Mirek = 153 };

                scenePost.Actions.Add(newAction);
            }

            return scenePost;
        }

        private async Task<Guid?> IsDeviceLight(string rid)
        {
            if (_httpClient == null) return null;

            var response    = await _httpClient.GetAsync("device/" + rid);
            var content     = await response.Content.ReadAsStringAsync();
            var jsonObject  = JsonSerializer.Deserialize<DeviceModel>(content);

            if (jsonObject == null || jsonObject.Data == null) return null;

            foreach (var data in jsonObject.Data)
            {
                var lightService = data.Services.FirstOrDefault(service => service.Rtype.Equals("light"));

                if (lightService != null) return lightService.Rid;
            }

            return null;
        }

        private void RemoveRoomsAndZonesWithFluxScenes(List<SceneData> scenes, List<GroupData> rooms, List<GroupData> zones, SceneSwitchList oldSceneSwitchList, SceneSwitchList newSceneSwitchList)
        {
            foreach (var scene in scenes)
            {
                var roomWithScene = rooms.FirstOrDefault(room => room.Id == scene.Group.Rid);
                if (roomWithScene != null)
                {
                    _sceneIds.Add(scene.Id);

                    newSceneSwitchList.SceneSwitches.Add(
                        oldSceneSwitchList.SceneSwitches.FirstOrDefault(sw => sw.SceneId == scene.Id) 
                        ?? new SceneSwitchAction() { SceneId = scene.Id, Name = roomWithScene.Metadata.Name });

                    rooms.Remove(roomWithScene);
                    continue;
                }

                var zoneWithScene = zones.FirstOrDefault(zone => zone.Id == scene.Group.Rid);
                if (zoneWithScene != null)
                {
                    _sceneIds.Add(scene.Id);

                    newSceneSwitchList.SceneSwitches.Add(
                        oldSceneSwitchList.SceneSwitches.FirstOrDefault(sw => sw.SceneId == scene.Id)
                        ?? new SceneSwitchAction() { SceneId = scene.Id, Name = zoneWithScene.Metadata.Name });

                    zones.Remove(zoneWithScene);
                    continue;
                }
            }
        }

        public async Task<List<GroupData>> GetGroupData(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            var jsonObject = JsonSerializer.Deserialize<GroupModel>(content);

            if (jsonObject?.Data == null || jsonObject.Data.Length == 0) return new List<GroupData>();

            return jsonObject.Data.ToList();
        }

        public async Task<List<SceneData>> GetFluxSceneData(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            var jsonObject = JsonSerializer.Deserialize<SceneModel>(content);

            if (jsonObject?.Data == null || jsonObject.Data.Length == 0) return new List<SceneData>();

            var sceneName = Properties.Settings.Default.SceneName;

            return jsonObject.Data.Where(s => s.Metadata.Name.Equals(sceneName)).ToList();
        }

        public async Task<bool> GenerateAppKey()
        {
            if (_bridgeIP == null) return false;

            var attempts = 5;

            await Task.Delay(4000); // give user some initial time to press link button

            var uri = "http://" + _bridgeIP.ToString() + "/api";

            var body = "{\"devicetype\":\"flux_hue_bridge#1.0\", \"generateclientkey\":true}";
            var content = new StringContent(body, Encoding.UTF8, "application/json");

            try
            {
                while (attempts > 0)
                {
                    attempts--;

                    var response = await _httpClient.PostAsync(uri, content);
                    if (response == null) return false;

                    var json = await response.Content.ReadAsStringAsync();
                    if (json == null) return false;

                    var jsonObject = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Newtonsoft.Json.Linq.JToken>>(json);
                    if (jsonObject == null || jsonObject.Count == 0) return false;

                    var success = jsonObject.First()["success"];
                    if (success == null) { await Task.Delay(1000); continue; }

                    var appKey = success["username"];
                    if (appKey == null) return false;

                    Properties.Settings.Default.AppKey = appKey.ToString();
                    Properties.Settings.Default.Save();

                    SetupHttpClient(appKey.ToString(), _bridgeIP.ToString());

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                return false;
            }
        }

        public async Task<bool> DiscoverHueBridgeIP()
        {
            try
            {
                var response = await new HttpClient().GetAsync(_bridgeDisoveryURI);
                var content = await response.Content.ReadAsStringAsync();

                var bridges = Newtonsoft.Json.JsonConvert.DeserializeObject<List<BridgeDiscoveryModel>>(content);

                if (bridges == null || bridges.Count == 0) return false;

                var firstBridge = bridges.First();

                if (string.IsNullOrWhiteSpace(firstBridge.internalipaddress)) return false;

                if (!IPAddress.TryParse(firstBridge.internalipaddress, out _bridgeIP)) return false;

                Properties.Settings.Default.BridgeIP = _bridgeIP.ToString();
                Properties.Settings.Default.Save();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());

                return false;
            }
        }
    }
}
