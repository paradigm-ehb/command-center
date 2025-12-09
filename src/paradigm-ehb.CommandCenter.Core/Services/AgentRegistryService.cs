using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Grpc.Net.Client;
using paradigm_ehb.CommandCenter.Core.Interfaces;
using paradigm_ehb.CommandCenter.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace paradigm_ehb.CommandCenter.Core.Services
{
    /// <summary>
    /// Thread-safe, in-memory registry of agent endpoints.
    /// Lightweight and transport-agnostic. Optionally pre-warms a channel for a newly registered endpoint.
    /// </summary>
    public sealed class AgentRegistryService : IAgentRegistry
    {
        private readonly ConcurrentDictionary<Guid, AgentEndpoint> _agentEndpoints = new();  // <summary>In-memory store of agent endpoints</summary>
        private readonly IGrpcChannelFactory? _channelFactory;
        private readonly ILogger<AgentRegistryService> _logger;

        public AgentRegistryService(IGrpcChannelFactory? channelFactory = null, ILogger<AgentRegistryService>? logger = null)
        {
            _channelFactory = channelFactory;
            _logger = logger ?? NullLogger<AgentRegistryService>.Instance;
        }
        /// <summary>
        /// Registers the specified agent endpoint for communication and management.
        /// </summary>
        /// <remarks>If a channel factory is configured, the method attempts to pre-warm or validate the
        /// gRPC channel for the agent endpoint. Any errors during channel creation are logged but do not cause the
        /// registration to fail.</remarks>
        /// <param name="endpoint">The agent endpoint to register. Must contain a valid, non-empty IP address.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the registration operation.</param>
        /// <returns>A task that represents the asynchronous registration operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="endpoint"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="endpoint"/> does not contain a valid IP address.</exception>
        public async Task<RegistrationResult> RegisterAsync(AgentEndpoint endpoint, CancellationToken cancellationToken = default)
        {
            if (endpoint is null) throw new ArgumentNullException(nameof(endpoint));
            cancellationToken.ThrowIfCancellationRequested();

            // Check IP Address
            if (string.IsNullOrWhiteSpace(endpoint.IpAddress))
            {
                throw new ArgumentException("AgentEndpoint must contain an IpAddress.", nameof(endpoint));
            }

            _agentEndpoints.AddOrUpdate(endpoint.Id, endpoint, (_, __) => endpoint);    // Add endpoint to memory or update current key-value in memory

            // Define result variables
            bool preWarmAttempted = false;
            bool preWarmSucceeded = false;
            List<string> warnings = new();

            // Optionally pre-warm / validate channel creation. Do not fail registration on channel errors.
            if (_channelFactory is not null)
            {
                preWarmAttempted = true;
                try
                {
                    // Creating the channel may throw for invalid address or network config.
                    using GrpcChannel channel = _channelFactory.CreateChannel(endpoint);
                    preWarmSucceeded = true;
                }
                catch (Exception exception)
                {
                    // Swallow exceptions, but log them
                    _logger.LogWarning(exception, "Pre-warming gRPC channel for agent {AgentId} at {AgentAddress} failed during registration.", endpoint.Id, endpoint.IpAddress);
                }
            }

            RegistrationResult result = new(
                Registered: true,
                PreWarmAttempted: preWarmAttempted,
                PreWarmSucceeded: preWarmSucceeded,
                Warnings: warnings.AsReadOnly()
                );

            return result;
        }

        /// <summary>
        /// Asynchronously deregisters the agent endpoint associated with the specified identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the agent endpoint to deregister.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the deregistration operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if the agent
        /// endpoint was successfully deregistered; otherwise, <see langword="false"/>.</returns>
        public Task<bool> DeregisterAsync(Guid id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            bool removed = _agentEndpoints.TryRemove(id, out _);
            return Task.FromResult(removed);
        }

        /// <summary>
        /// Asynchronously retrieves a snapshot of all registered agent endpoints.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only collection of agent
        /// endpoints currently registered. The collection will be empty if no endpoints are registered.</returns>
        public Task<IReadOnlyCollection<AgentEndpoint>> ListAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyCollection<AgentEndpoint> snapshot = _agentEndpoints.Values.ToList().AsReadOnly();
            return Task.FromResult(snapshot);
        }

        /// <summary>
        /// Asynchronously retrieves the agent endpoint associated with the specified identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the agent endpoint to retrieve.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="AgentEndpoint"/>
        /// associated with the specified identifier, or <see langword="null"/> if no matching endpoint is found.</returns>
        public Task<AgentEndpoint?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _agentEndpoints.TryGetValue(id, out var endpoint);
            return Task.FromResult(endpoint);
        }
    }
}