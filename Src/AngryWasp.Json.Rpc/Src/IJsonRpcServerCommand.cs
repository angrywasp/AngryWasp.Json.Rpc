using System.Threading.Tasks;

namespace AngryWasp.Json.Rpc
{
    public class JsonRpcServerCommandResult
    {
        public bool Success { get; set; } = false;
        public object Value { get; set; } = null;
    }

    public interface IJsonRpcServerCommand
    {
        Task<JsonRpcServerCommandResult> Handle(string req);
    }
}