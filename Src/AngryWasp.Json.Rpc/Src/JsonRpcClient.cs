using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace AngryWasp.Json.Rpc
{
    public class JsonRpcClient
    {
        private ushort port;
        private string url;

        HttpClient httpClient;

        public ushort Port => port;

        public string Url => url;

        public JsonRpcClient(string url, ushort port)
        {
            this.url = url;
            this.port = port;
            this.httpClient = new HttpClient();
        }

        public async Task<string> SendRequest(string endpoint, string data)
        {
            try
            {
                HttpContent content = new StringContent(data, Encoding.ASCII, "application/json");
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                var result = await httpClient.PostAsync($"{url}:{port}/{endpoint}", content).ConfigureAwait(false);
                return await result.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch
            {
                return null;
            }
        }
    }
}