using System;
using System.Collections.Generic;
using Grpc.Net.Client;
using paradigm_ehb.CommandCenter.Core.Interfaces;
using paradigm_ehb.CommandCenter.Core.Models;

namespace paradigm_ehb.CommandCenter.Core.Factories
{
    internal class GrpcClientFactory : IGrpcClientFactory
    {
        private readonly IGrpcChannelFactory _channelFactory;
        private readonly Dictionary<Guid, AgentClientEntry> _clients = new();
        private readonly object _sync = new();
        private bool _disposed;

        public GrpcClientFactory(IGrpcChannelFactory channelFactory)
        {
            _channelFactory = channelFactory ?? throw new ArgumentNullException(nameof(channelFactory));
        }

        public AgentClientEntry CreateClient(AgentEndpoint endpoint)
        {
            if (endpoint is null) throw new ArgumentNullException(nameof(endpoint));

            lock (_sync)
            {
                if (_disposed) throw new ObjectDisposedException(nameof(GrpcClientFactory));

                if (_clients.TryGetValue(endpoint.Id, out var existing))
                {
                    return existing;
                }

                // Create channel and strongly-typed gRPC clients
                GrpcChannel channel = _channelFactory.CreateChannel(endpoint);

                var entry = new AgentClientEntry
                {
                    Channel = channel,
                    Greeter = new Greeter.GreeterClient(channel),
                    Tester = new Tester.TesterClient(channel)
                };

                _clients[endpoint.Id] = entry;
                return entry;
            }
        }

        public AgentClientEntry? GetClient(Guid endpointId)
        {
            lock (_sync)
            {
                _clients.TryGetValue(endpointId, out var entry);
                return entry;
            }
        }

        public IEnumerable<AgentClientEntry> GetAllClients()
        {
            lock (_sync)
            {
                return new List<AgentClientEntry>(_clients.Values);
            }
        }

        public void Dispose()
        {
            lock (_sync)
            {
                if (_disposed) return;
                _disposed = true;

                foreach (var entry in _clients.Values)
                {
                    try
                    {
                        entry.Channel?.Dispose();
                    }
                    catch
                    {
                        // swallow: disposing best-effort
                    }
                }

                _clients.Clear();
            }
        }
    }
}
