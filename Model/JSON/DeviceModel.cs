using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FluxHueBridge
{
    public partial class DeviceModel
    {
        [JsonPropertyName("errors")]
        public object[] Errors { get; set; }

        [JsonPropertyName("data")]
        public DeviceData[] Data { get; set; }
    }

    public partial class DeviceData
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("id_v1")]
        public string IdV1 { get; set; }

        [JsonPropertyName("product_data")]
        public ProductData ProductData { get; set; }

        [JsonPropertyName("metadata")]
        public Metadata Metadata { get; set; }

        [JsonPropertyName("services")]
        public Service[] Services { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }
    }

    public partial class ProductData
    {
        [JsonPropertyName("model_id")]
        public string ModelId { get; set; }

        [JsonPropertyName("manufacturer_name")]
        public string ManufacturerName { get; set; }

        [JsonPropertyName("product_name")]
        public string ProductName { get; set; }

        [JsonPropertyName("product_archetype")]
        public string ProductArchetype { get; set; }

        [JsonPropertyName("certified")]
        public bool Certified { get; set; }

        [JsonPropertyName("software_version")]
        public string SoftwareVersion { get; set; }

        [JsonPropertyName("hardware_platform_type")]
        public string HardwarePlatformType { get; set; }
    }

    public partial class Service
    {
        [JsonPropertyName("rid")]
        public Guid Rid { get; set; }

        [JsonPropertyName("rtype")]
        public string Rtype { get; set; }
    }
}
