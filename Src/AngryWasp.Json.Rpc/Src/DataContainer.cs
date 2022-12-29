using Newtonsoft.Json;

namespace AngryWasp.Json.Rpc
{
    [JsonObject]
    public class JsonResponseBase
    {
        [JsonProperty("code")]
        public uint ErrorCode { get; set; } = 0;
    }

    [JsonObject]
    public class JsonResponse<T> : JsonResponseBase where T : class, new()
    {
        [JsonProperty("data")]
        public T Data { get; set; } = new T();

        public string Serialize()
        {
            string returnValue = JsonConvert.SerializeObject(this);
            return returnValue;
        }

        public static bool Deserialize(string value, out JsonResponse<T> deserialized)
        {
            deserialized = JsonConvert.DeserializeObject<JsonResponse<T>>(value);
            return deserialized != null;
        }
    }

    [JsonObject]
    public class JsonRequest<T>
    {
        [JsonProperty("api")]
        public ApiLevel ApiLevel { get; set; } = new ApiLevel();

        [JsonProperty("data")]
        public T Data { get; set; }

        public string Serialize()
        {
            string returnValue = JsonConvert.SerializeObject(this);
            return returnValue;
        }

        public static bool Deserialize(string value, out JsonRequest<T> deserialized)
        {
            deserialized = JsonConvert.DeserializeObject<JsonRequest<T>>(value);

            // major version differences indicate incompatibility between versions
            // action for variations of the minor version to be decided at the application level
            if (deserialized.ApiLevel.Major < ApiLevel.MAJOR)
            {
                deserialized = null;
                return false;
            }

            return true;
        }
    }
}