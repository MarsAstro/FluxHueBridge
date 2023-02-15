using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FluxHueBridge
{
    public partial class ScenePostModel
    {
        [JsonPropertyName("data")]
        public Group[] Data { get; set; }

        [JsonPropertyName("errors")]
        public object[] Errors { get; set; }
    }

}
