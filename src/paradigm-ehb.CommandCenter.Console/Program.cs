// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

Console.WriteLine(Client.DependencyMethod());

Console.WriteLine(await Client.GrpcGreetAsync("http://localhost:50051"));
