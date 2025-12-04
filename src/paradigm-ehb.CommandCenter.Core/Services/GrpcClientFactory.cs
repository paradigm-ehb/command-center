using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Grpc.Net.Client;
using Microsoft.Extensions.DependencyInjection;

namespace paradigm_ehb.CommandCenter.Core.Services
{
    internal class GrpcClientFactory : Grpc.Net.ClientFactory.GrpcClientFactory
    {
        public override TClient CreateClient<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TClient>(string name)
        {
            IServiceCollection services = new ServiceCollection();
            services.AddGrpcClient<TClient>(o =>
            {
                o.BaseAddress = new Uri("http://localhost");
            });
        }
    }
}
