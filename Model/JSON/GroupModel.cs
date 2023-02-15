using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FluxHueBridge
{
    public partial class GroupModel
    {
        [JsonPropertyName("errors")]
        public object[] Errors { get; set; }

        [JsonPropertyName("data")]
        public GroupData[] Data { get; set; }
    }

    public partial class GroupData
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("id_v1")]
        public string IdV1 { get; set; }

        [JsonPropertyName("children")]
        public Group[] Children { get; set; }

        [JsonPropertyName("services")]
        public Group[] Services { get; set; }

        [JsonPropertyName("metadata")]
        public Metadata Metadata { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }
    }
}
