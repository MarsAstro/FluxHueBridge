using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FluxHueBridge
{
    public partial class ScenePost
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "scene";

        [JsonPropertyName("actions")]
        public List<ActionElement> Actions { get; set; } = new List<ActionElement>();

        [JsonPropertyName("metadata")]
        public SceneMetadata Metadata { get; set; } = new SceneMetadata();

        [JsonPropertyName("group")]
        public Group Group { get; set; }
    }

    public partial class ScenePut
    {
        [JsonPropertyName("actions")]
        public List<ActionElement> Actions { get; set; } = new List<ActionElement>();

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("recall")]
        public Recall? Recall { get; set; }
    }

    public partial class Recall
    {
        [JsonPropertyName("action")]
        public string Action { get; set; } = "active";
    }
}
