using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTCChecker.Items
{
    public class DTCval
    {
        [JsonProperty("DTCCodes")]
        public string DTCCodes {  get; set; }
        [JsonProperty("DTCDetails")]
        public string DTCDetails { get; set; }
    }
}
