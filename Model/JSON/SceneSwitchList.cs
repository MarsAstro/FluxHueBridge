using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FluxHueBridge
{
    public class SceneSwitchList
    {
        [JsonPropertyName("sceneswitches")]
        public List<SceneSwitchAction> SceneSwitches { get; set; } = new List<SceneSwitchAction>(); 
    }

    public class SceneSwitchAction
    {
        [JsonPropertyName("rid")]
        public Guid SceneId { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("switch")]
        public bool ShouldSwitch { get; set; } = false;
    }
}
