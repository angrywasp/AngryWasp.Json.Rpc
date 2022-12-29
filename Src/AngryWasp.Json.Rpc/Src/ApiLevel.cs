using Newtonsoft.Json;

namespace AngryWasp.Json.Rpc
{
    public class ApiLevel
    {
        [JsonIgnore]
        public const byte MAJOR = 1;

        [JsonIgnore]
        public const byte MINOR = 0;

        [JsonProperty("major")]
        public ushort Major { get; set; } = MAJOR;

        [JsonProperty("minor")]
        public ushort Minor { get; set; } = MINOR;
    }
}