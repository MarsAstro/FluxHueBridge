using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FluxHueBridge
{
    public partial class SceneModel
    {
        [JsonPropertyName("errors")]
        public object[] Errors { get; set; }

        [JsonPropertyName("data")]
        public SceneData[] Data { get; set; }
    }

    public partial class SceneData
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("id_v1")]
        public string IdV1 { get; set; }

        [JsonPropertyName("actions")]
        public ActionElement[] Actions { get; set; }

        [JsonPropertyName("metadata")]
        public SceneMetadata Metadata { get; set; }

        [JsonPropertyName("group")]
        public Group Group { get; set; }

        [JsonPropertyName("palette")]
        public Palette Palette { get; set; }

        [JsonPropertyName("speed")]
        public double Speed { get; set; }

        [JsonPropertyName("auto_dynamic")]
        public bool AutoDynamic { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }
    }

    public partial class ActionElement
    {
        [JsonPropertyName("target")]
        public Group Target { get; set; }

        [JsonPropertyName("action")]
        public ActionAction Action { get; set; }
    }

    public partial class ActionAction
    {
        [JsonPropertyName("on")]
        public On On { get; set; }

        [JsonPropertyName("dimming")]
        public Dimming Dimming { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("color_temperature")]
        public ActionColorTemperature ColorTemperature { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("color")]
        public ActionColor Color { get; set; }
    }

    public partial class ActionColor
    {
        [JsonPropertyName("xy")]
        public Xy Xy { get; set; }
    }

    public partial class ActionColorTemperature
    {
        [JsonPropertyName("mirek")]
        public long Mirek { get; set; }
    }

    public partial class Dimming
    {
        [JsonPropertyName("brightness")]
        public double Brightness { get; set; }
    }

    public partial class On
    {
        [JsonPropertyName("on")]
        public bool OnOn { get; set; }
    }

    public partial class Group
    {
        [JsonPropertyName("rid")]
        public Guid Rid { get; set; }

        [JsonPropertyName("rtype")]
        public string Rtype { get; set; }
    }

    public partial class SceneMetadata
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = Properties.Settings.Default.SceneName;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("image")]
        public Group? Image { get; set; } = null;
    }

    public partial class Palette
    {
        [JsonPropertyName("color")]
        public ColorElement[] Color { get; set; }

        [JsonPropertyName("dimming")]
        public object[] Dimming { get; set; }

        [JsonPropertyName("color_temperature")]
        public ColorTemperatureElement[] ColorTemperature { get; set; }
    }

    public partial class ColorElement
    {
        [JsonPropertyName("color")]
        public ActionColor Color { get; set; }

        [JsonPropertyName("dimming")]
        public Dimming Dimming { get; set; }
    }

    public partial class ColorTemperatureElement
    {
        [JsonPropertyName("color_temperature")]
        public ActionColorTemperature ColorTemperature { get; set; }

        [JsonPropertyName("dimming")]
        public Dimming Dimming { get; set; }
    }
}
