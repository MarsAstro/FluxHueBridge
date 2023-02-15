using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluxHueBridge
{
    public class BridgeDiscoveryModel
    {
        public string? id { get; set; }
        public string? internalipaddress { get; set; }
        public string? macaddress { get; set; }
        public string? name { get; set; }
        public int? port { get; set; }
    }
}
