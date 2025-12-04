namespace paradigm_ehb.CommandCenter.Core
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

            HelloReply response = client.SayHello(
                new HelloRequest { Name = "World" });

            return response.Message;
        }
    }
}
