using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using paradigm_ehb.CommandCenter.Core.Interfaces;
using paradigm_ehb.CommandCenter.Core.Models;

namespace paradigm_ehb.CommandCenter.Core.Services
{
    /// <summary>
    /// Thread-safe in-memory registry of active agent clients (gRPC channels + generated clients).
    /// Responsible for creating channels (via optional factory), returning existing entries,
    /// recording usage timestamps and disposing channels when removed or when the registry is disposed.
    /// </summary>
    internal sealed class AgentClientRegistry : IAgentClientRegistry
    {
        private readonly ConcurrentDictionary<Guid, AgentClientEntry> _clients = new();
        private readonly ConcurrentDictionary<Guid, DateTimeOffset> _lastUsed = new();
        private readonly IGrpcChannelFactory? _channelFactory;
        private readonly ILogger<AgentClientRegistry> _logger;
        private readonly object _selectionLock = new();
        private Guid? _selectedEndpointId;
        private bool _disposed;

        public AgentClientRegistry(IGrpcChannelFactory? channelFactory = null, ILogger<AgentClientRegistry>? logger = null)
        {
            _channelFactory = channelFactory;
            _logger = logger ?? NullLogger<AgentClientRegistry>.Instance;
        }

        public AgentClientEntry CreateOrGet(AgentEndpoint endpoint)
        {
            if (endpoint is null) throw new ArgumentNullException(nameof(endpoint));
            EnsureNotDisposed();

            return _clients.GetOrAdd(endpoint.Id, id =>
            {
                GrpcChannel channel;

                if (_channelFactory is not null)
                {
                    try
                    {
                        channel = _channelFactory.CreateChannel(endpoint);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Channel factory failed for agent {AgentId} at {AgentAddress}. Falling back to direct channel creation.", endpoint.Id, endpoint.IpAddress);
                        channel = CreateFallbackChannel(endpoint);
                    }
                }
                else
                {
                    channel = CreateFallbackChannel(endpoint);
                }

                AgentClientEntry entry = new AgentClientEntry
                {
                    EndpointId = endpoint.Id,
                    Channel = channel,
                    Greeter = new Greeter.GreeterClient(channel),
                };

                // record initial usage timestamp
                _lastUsed[endpoint.Id] = DateTimeOffset.UtcNow;

                return entry;
            });
        }

        public AgentClientEntry? Get(Guid endpointId)
        {
            EnsureNotDisposed();
            _clients.TryGetValue(endpointId, out AgentClientEntry? entry);
            return entry;
        }

        public Task<IReadOnlyCollection<AgentClientEntry>> ListAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureNotDisposed();
            IReadOnlyCollection<AgentClientEntry> snapshot = _clients.Values.ToList().AsReadOnly();
            return Task.FromResult(snapshot);
        }

        public Task<bool> RemoveAsync(Guid endpointId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureNotDisposed();

            if (_clients.TryRemove(endpointId, out AgentClientEntry? entry))
            {
                try
                {
                    // Dispose channel if possible
                    (entry.Channel as IDisposable)?.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Disposing channel for agent {AgentId} threw an exception.", endpointId);
                }

                _lastUsed.TryRemove(endpointId, out _);

                lock (_selectionLock)
                {
                    if (_selectedEndpointId == endpointId)
                    {
                        _selectedEndpointId = null;
                    }
                }

                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public Task MarkUsed(Guid endpointId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureNotDisposed();

            if (_clients.ContainsKey(endpointId))
            {
                _lastUsed[endpointId] = DateTimeOffset.UtcNow;
            }

            return Task.CompletedTask;
        }

        public Task Select(Guid? endpointId)
        {
            EnsureNotDisposed();

            if (endpointId is null)
            {
                lock (_selectionLock)
                {
                    _selectedEndpointId = null;
                }

                return Task.CompletedTask;
            }

            // If selecting a specific endpoint ensure it exists.
            if (!_clients.ContainsKey(endpointId.Value))
            {
                throw new InvalidOperationException($"Cannot select endpoint {endpointId}. Endpoint not found in registry.");
            }

            lock (_selectionLock)
            {
                _selectedEndpointId = endpointId;
            }

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (KeyValuePair<Guid, AgentClientEntry> kvp in _clients)
            {
                try
                {
                    (kvp.Value.Channel as IDisposable)?.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error while disposing channel for agent {AgentId}.", kvp.Key);
                }
            }

            _clients.Clear();
            _lastUsed.Clear();

            lock (_selectionLock)
            {
                _selectedEndpointId = null;
            }
        }

        private static GrpcChannel CreateFallbackChannel(AgentEndpoint endpoint)
        {
            string scheme = endpoint.UseTls ? "https" : "http";
            string address = $"{scheme}://{endpoint.IpAddress}:{endpoint.Port}";
            return GrpcChannel.ForAddress(address);
        }

        private void EnsureNotDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(AgentClientRegistry));
        }
    }
}
