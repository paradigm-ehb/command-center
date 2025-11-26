using Grpc.Net.Client;

namespace paradigm_ehb.CommandCenter.Lib
{
    public class Client
    {
        public static string DependencyMethod()
        {
            return "Hello from CommandCenter.Lib!";
        }

        public static async Task<string> GrpcGreetAsync(string address)
        {
            GrpcChannel channel = GrpcChannel.ForAddress(address);
            Greeter.GreeterClient client = new Greeter.GreeterClient(channel);

            HelloReply response = await client.SayHelloAsync(
                new HelloRequest { Name = "World" });

            return response.Message;
        }
    }
}
