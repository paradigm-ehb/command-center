using Grpc.Net.Client;
using paradigm_ehb.CommandCenter.Core.Services;

namespace paradigm_ehb.CommandCenter.Core
{
    public class Client
    {
        public static string DependencyMethod()
        {
            GrpcClientFactory grpcClientFactory = new();
            return "Hello from CommandCenter.Lib!";
        }
    }
}
