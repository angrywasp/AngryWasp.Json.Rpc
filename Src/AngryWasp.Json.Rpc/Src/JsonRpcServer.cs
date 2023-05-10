using AngryWasp.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Log = AngryWasp.Logger.Log;

namespace AngryWasp.Json.Rpc
{
    public class JsonRpcServer
    {
        public const uint API_LEVEL = 1;
        HttpListener listener;

        private ushort port = 0;
        private string sslCertPath = null;
        private X509Certificate2 certificate;

        public ushort Port => port;

        public string SslCertPath => sslCertPath;

        public delegate Task<JsonRpcServerCommandResult> RpcFunc(string args);

        private Dictionary<string, RpcFunc> commands = new Dictionary<string, RpcFunc>();

        public JsonRpcServer(ushort port, string sslCertPath = null)
        {
            this.port = port;
            this.sslCertPath = sslCertPath;
        }

        public void RegisterCommand(string key, RpcFunc value)
        {
            if (!commands.ContainsKey(key))
                commands.Add(key, value);
        }

        public void RegisterCommands()
        {
            RegisterCommands(Assembly.GetEntryAssembly());
        }

        public void RegisterCommands(Assembly assembly)
        {
            var types = ReflectionHelper.Instance.GetTypesInheritingOrImplementing(assembly, typeof(IJsonRpcServerCommand))
                .Where(m => m.GetCustomAttributes(typeof(JsonRpcServerCommandAttribute), false).Length > 0)
                .ToArray();

            foreach (var type in types)
            {
                IJsonRpcServerCommand i = (IJsonRpcServerCommand)Activator.CreateInstance(type);
                JsonRpcServerCommandAttribute a = i.GetType().GetCustomAttributes(true).OfType<JsonRpcServerCommandAttribute>().FirstOrDefault();
                RegisterCommand(a.Key, i.Handle);
            }
        }

        public void Start()
        {
            listener = new HttpListener();
            if (sslCertPath == null)
            {
                listener.Prefixes.Add($"http://*:{port}/");
                listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
            }
            else if (File.Exists(sslCertPath))
            {
                Log.Instance.WriteInfo($"Initializing RPC Server with SSL");

                certificate = new X509Certificate2(X509Certificate2.CreateFromCertFile(sslCertPath));
                listener.Prefixes.Add($"https://*:{port}/");
                listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
            }
            else
                throw new Exception("SSL Certificate not found");

            try
            {
                listener.Start();
            }
            catch (Exception ex)
            {
                Log.Instance.WriteError("Failed to start RPC Server");
                Log.Instance.WriteError(ex.Message);
            }

            Log.Instance.WriteInfo($"Local RPC endpoint on port {port}");
            Log.Instance.WriteInfo("RPC server initialized");

            Task.Run(async () =>
            {
                if (certificate == null)
                    while (true)
                    {
                        HttpListenerContext context = listener.GetContext();
                        Log.Instance.WriteInfo($"Request from {context.Request.LocalEndPoint.Address}");
                        await HandleRequest(context).ConfigureAwait(false);
                    }
                else
                    while (true)
                    {
                        HttpListenerContext context = listener.GetContext();
                        Log.Instance.WriteInfo($"SSL Request from {context.Request.LocalEndPoint.Address}");
                        await HandleSslRequest(context).ConfigureAwait(false);
                    }

            });
        }

        private async Task HandleRequest(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            if (request.Url.Segments.Length < 2)
            {
                response.StatusCode = 400;
                context.Response.Close();
                return;
            }

            await GenerateResponse(context).ConfigureAwait(false);
        }

        private async Task HandleSslRequest(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            if (request.Url.Segments.Length < 2)
            {
                response.StatusCode = 400;
                response.Close();
                return;
            }

            var clientCertificate = request.GetClientCertificate();
            if (clientCertificate == null || !clientCertificate.Verify())
            {
                response.StatusCode = 403;
                response.Close();
                return;
            }

            // Perform SSL/TLS negotiation manually
            var sslStream = new SslStream(response.OutputStream, false);
            try
            {
                await sslStream.AuthenticateAsServerAsync(certificate, false, SslProtocols.Tls12, true);
            }
            catch (AuthenticationException ex)
            {
                Log.Instance.WriteException(ex, "SSL/TLS handshake failed");
                response.StatusCode = 500;
                response.Close();
                return;
            }

            response.ContentType = "application/json";
            response.AppendHeader("Access-Control-Allow-Origin", "*");

            await GenerateResponse(context).ConfigureAwait(false);
        }

        private async Task GenerateResponse(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            string text;
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                text = reader.ReadToEnd();
            }

            string method = request.Url.Segments[1];
            JsonRpcServerCommandResult result;

            if (commands.ContainsKey(method))
            {
                try
                {
                    result = await commands[method].Invoke(text).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    var exMsg = $"Exception in RPC request: {ex.Message}";

                    result = new JsonRpcServerCommandResult
                    {
                        Success = false,
                        Value = new JsonResponse<object>() { Data = exMsg }
                    };

                    Log.Instance.WriteError(exMsg);
                }
            }
            else
            {
                result = new JsonRpcServerCommandResult
                {
                    Success = false,
                    Value = new JsonResponse<object>() { Data = "The specified method does not exist" }
                };
            }

            //we can get to this point if a handler exits without assigning to resultData.
            //Ideally a command handler should assign a dummy object, but we shouldn't count on it
            if (result.Value == null)
            {
                Log.Instance.WriteWarning($"The RPC handler '{method}' returned an empty result value. Please correct this in the handler code");
                result = new JsonRpcServerCommandResult
                {
                    Success = false,
                    Value = new JsonResponse<object>() { Data = $"The RPC handler '{method}' returned an empty result" }
                };
            }

            response.StatusCode = result.Success ? (int)Response_Code.OK : (int)Response_Code.Error;
            byte[] bytes = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(result.Value));
            response.OutputStream.Write(bytes, 0, bytes.Length);
            context.Response.Close();
        }
    }
}
