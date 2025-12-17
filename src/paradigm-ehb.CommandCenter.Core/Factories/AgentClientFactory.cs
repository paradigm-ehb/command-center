using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Health.V1;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using paradigm_ehb.CommandCenter.Core.Interfaces;
using paradigm_ehb.CommandCenter.Core.Models;
using paradigm_ehb.CommandCenter.Core.Services;

namespace paradigm_ehb.CommandCenter.Core.Factories
{
    /// <summary>
    /// Provides methods for creating, registering, and retrieving agent client entries for specified agent endpoints.
    /// </summary>
    /// <remarks>This class manages the lifecycle of agent client entries, including temporary creation and
    /// registration with an agent client registry. It is thread-safe for concurrent use. Dispose the factory when it is
    /// no longer needed to release resources.</remarks>
    public sealed class AgentClientFactory : IAgentClientFactory
    {
        private readonly IAgentClientRegistry _registry;
        private readonly IGrpcChannelFactory _channelFactory;
        private readonly ILogger<AgentClientFactory> _logger;
        private bool _disposed;

        public AgentClientFactory(IAgentClientRegistry registry, IGrpcChannelFactory channelFactory, ILogger<AgentClientFactory>? logger = null)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _channelFactory = channelFactory ?? throw new ArgumentNullException(nameof(channelFactory));
            _logger = logger ?? NullLogger<AgentClientFactory>.Instance;
        }

        /// <summary>
        /// Creates a new temporary client entry for the specified agent endpoint.
        /// </summary>
        /// <param name="endpoint">The agent endpoint for which to create the client entry. Cannot be null.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the created client entry for the
        /// specified endpoint.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="endpoint"/> is null.</exception>
        public Task<AgentClient> CreateClientAsync(AgentEndpoint endpoint, CancellationToken cancellationToken = default)
        {
            if (endpoint is null) throw new ArgumentNullException(nameof(endpoint));
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();

            GrpcChannel channel = _channelFactory.CreateChannel(endpoint);

            AgentClient createdEntry = new AgentClient
            {
                Endpoint = endpoint,
                Channel = channel,
                Health = new Health.HealthClient(channel),
                Greeter = new Greeter.GreeterClient(channel),
            };

            // Start background health monitoring as part of client creation
            if (endpoint.MonitoringEnabled) createdEntry.StartHealthWatch(cancellationToken);

            _logger.LogDebug("Created temporary client entry for endpoint {EndpointId}", endpoint.Id);
            return Task.FromResult(createdEntry);
        }

        /// <summary>
        /// Creates a new agent client for the specified endpoint and registers it with the agent registry
        /// asynchronously.
        /// </summary>
        /// <remarks>If registration fails, any resources allocated for the new client are cleaned up
        /// before the exception is thrown. If a client for the specified endpoint already exists, the newly created
        /// client is disposed and the existing entry is returned.</remarks>
        /// <param name="endpoint">The endpoint information used to create and register the agent client. Cannot be null.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>An <see cref="AgentClient"/> representing the registered agent client. If the client was already
        /// registered, returns the existing entry.</returns>
        public async Task<AgentClient> CreateAndRegisterClientAsync(AgentEndpoint endpoint, CancellationToken cancellationToken = default)
        {
            AgentClient created = await CreateClientAsync(endpoint, cancellationToken);

            AgentClientRegistrationResult result;
            try
            {
                result = await _registry.RegisterAsync(created, cancellationToken);
            }
            catch
            {
                // registration failed — clean up created resources
                try
                {
                    created.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to dispose AgentClient after failed registration.");
                }
                throw;
            }

            if (!result.Registered)
            {
                // registry returned an existing entry; dispose what we created
                try
                {
                    created.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to dispose AgentClient after registry returned existing entry.");
                }
                _logger.LogDebug("Registry returned existing entry for {EndpointId}; disposed newly created channel.", endpoint.Id);
            }
            else
            {
                _logger.LogInformation("Created and registered client entry for {EndpointId}.", endpoint.Id);
            }

            return result.Entry;
        }

        public Task<AgentClient?> GetClientAsync(Guid endpointId, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();
            return _registry.GetAsync(endpointId, cancellationToken);
        }

        public Task<IReadOnlyCollection<AgentClient>> GetAllClientsAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();
            return _registry.ListAsync(cancellationToken);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _logger.LogInformation("AgentClientFactory disposed.");
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(AgentClientFactory));
        }
    }
}
