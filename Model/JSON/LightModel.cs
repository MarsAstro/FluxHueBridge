using System;
using System.Text.Json.Serialization;

namespace FluxHueBridge
{
    public partial class LightModel
    {
        [JsonPropertyName("errors")]
        public object[] Errors { get; set; }

        [JsonPropertyName("data")]
        public LightData[] Data { get; set; }
    }

    public partial class LightData
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("id_v1")]
        public string IdV1 { get; set; }

        [JsonPropertyName("owner")]
        public Owner Owner { get; set; }

        [JsonPropertyName("metadata")]
        public Metadata Metadata { get; set; }

        [JsonPropertyName("on")]
        public DatumOn On { get; set; }

        [JsonPropertyName("dimming")]
        public DatumDimming Dimming { get; set; }

        [JsonPropertyName("dimming_delta")]
        public ColorTemperatureDelta DimmingDelta { get; set; }

        [JsonPropertyName("color_temperature")]
        public DatumColorTemperature ColorTemperature { get; set; }

        [JsonPropertyName("color_temperature_delta")]
        public ColorTemperatureDelta ColorTemperatureDelta { get; set; }

        [JsonPropertyName("color")]
        public DatumColor Color { get; set; }

        [JsonPropertyName("dynamics")]
        public Dynamics Dynamics { get; set; }

        [JsonPropertyName("alert")]
        public Alert Alert { get; set; }

        [JsonPropertyName("signaling")]
        public ColorTemperatureDelta Signaling { get; set; }

        [JsonPropertyName("mode")]
        public string Mode { get; set; }

        [JsonPropertyName("effects")]
        public Effects Effects { get; set; }

        [JsonPropertyName("powerup")]
        public Powerup Powerup { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }
    }

    public partial class Alert
    {
        [JsonPropertyName("action_values")]
        public string[] ActionValues { get; set; }
    }

    public partial class DatumColor
    {
        [JsonPropertyName("xy")]
        public Xy Xy { get; set; }

        [JsonPropertyName("gamut")]
        public Gamut Gamut { get; set; }

        [JsonPropertyName("gamut_type")]
        public string GamutType { get; set; }
    }

    public partial class Gamut
    {
        [JsonPropertyName("red")]
        public Xy Red { get; set; }

        [JsonPropertyName("green")]
        public Xy Green { get; set; }

        [JsonPropertyName("blue")]
        public Xy Blue { get; set; }
    }

    public partial class Xy
    {
        [JsonPropertyName("x")]
        public double X { get; set; }

        [JsonPropertyName("y")]
        public double Y { get; set; }
    }

    public partial class DatumColorTemperature
    {
        [JsonPropertyName("mirek")]
        public long? Mirek { get; set; }

        [JsonPropertyName("mirek_valid")]
        public bool MirekValid { get; set; }

        [JsonPropertyName("mirek_schema")]
        public MirekSchema MirekSchema { get; set; }
    }

    public partial class MirekSchema
    {
        [JsonPropertyName("mirek_minimum")]
        public long MirekMinimum { get; set; }

        [JsonPropertyName("mirek_maximum")]
        public long MirekMaximum { get; set; }
    }

    public partial class ColorTemperatureDelta
    {
    }

    public partial class DatumDimming
    {
        [JsonPropertyName("brightness")]
        public double Brightness { get; set; }

        [JsonPropertyName("min_dim_level")]
        public double MinDimLevel { get; set; }
    }

    public partial class Dynamics
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("status_values")]
        public string[] StatusValues { get; set; }

        [JsonPropertyName("speed")]
        public float Speed { get; set; }

        [JsonPropertyName("speed_valid")]
        public bool SpeedValid { get; set; }
    }

    public partial class Effects
    {
        [JsonPropertyName("status_values")]
        public string[] StatusValues { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("effect_values")]
        public string[] EffectValues { get; set; }
    }

    public partial class Metadata
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("archetype")]
        public string Archetype { get; set; }
    }

    public partial class DatumOn
    {
        [JsonPropertyName("on")]
        public bool On { get; set; }
    }

    public partial class Owner
    {
        [JsonPropertyName("rid")]
        public Guid Rid { get; set; }

        [JsonPropertyName("rtype")]
        public string Rtype { get; set; }
    }

    public partial class Powerup
    {
        [JsonPropertyName("preset")]
        public string Preset { get; set; }

        [JsonPropertyName("configured")]
        public bool Configured { get; set; }

        [JsonPropertyName("on")]
        public PowerupOn On { get; set; }

        [JsonPropertyName("dimming")]
        public PowerupDimming Dimming { get; set; }

        [JsonPropertyName("color")]
        public PowerupColor Color { get; set; }
    }

    public partial class PowerupColor
    {
        [JsonPropertyName("mode")]
        public string Mode { get; set; }

        [JsonPropertyName("color_temperature")]
        public ColorColorTemperature ColorTemperature { get; set; }
    }

    public partial class ColorColorTemperature
    {
        [JsonPropertyName("mirek")]
        public long Mirek { get; set; }
    }

    public partial class PowerupDimming
    {
        [JsonPropertyName("mode")]
        public string Mode { get; set; }

        [JsonPropertyName("dimming")]
        public DimmingDimming Dimming { get; set; }
    }

    public partial class DimmingDimming
    {
        [JsonPropertyName("brightness")]
        public float Brightness { get; set; }
    }

    public partial class PowerupOn
    {
        [JsonPropertyName("mode")]
        public string Mode { get; set; }

        [JsonPropertyName("on")]
        public DatumOn On { get; set; }
    }
}
