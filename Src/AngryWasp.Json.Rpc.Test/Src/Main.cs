using System;
using System.Diagnostics;
using AngryWasp.Logger;

namespace AngryWasp.Json.Rpc.Test
{
    internal class MainClass
    {
        private static void Main(string[] rawArgs)
        {
            Log.CreateInstance(true);
            var server = new JsonRpcServer(1000, "./TestCert/fullchain.pem");
            server.Start();
            Debugger.Break();
        }
    }
}