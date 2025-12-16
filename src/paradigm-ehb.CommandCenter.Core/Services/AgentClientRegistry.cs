using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using paradigm_ehb.CommandCenter.Core.Interfaces;
using paradigm_ehb.CommandCenter.Core.Models;

namespace paradigm_ehb.CommandCenter.Core.Services
{
    /// <summary>
    /// Thread-safe in-memory registry of active agent client entries.
    /// Responsibility: store, lookup, usage tracking and disposal.
    /// Creation of AgentClient is performed by factories; callers register created entries here.
    /// </summary>
    internal sealed class AgentClientRegistry : IAgentClientRegistry, IDisposable
    {
        private readonly ConcurrentDictionary<Guid, AgentClient> _clients = new();
        private readonly ConcurrentDictionary<Guid, DateTimeOffset> _lastUsed = new();
        private readonly ILogger<AgentClientRegistry> _logger;
        // Use an int for atomic disposal operations (0 = not disposed, 1 = disposed).
        private int _disposed;

        public AgentClientRegistry(ILogger<AgentClientRegistry>? logger = null)
        {
            _logger = logger ?? NullLogger<AgentClientRegistry>.Instance;
        }

        public Task<AgentClientRegistrationResult> RegisterAsync(AgentClient entry, CancellationToken cancellationToken = default)
        {
            if (entry is null) throw new ArgumentNullException(nameof(entry));
            cancellationToken.ThrowIfCancellationRequested();
            EnsureNotDisposed();

            AgentClient stored = _clients.GetOrAdd(entry.Endpoint.Id, entry);
            bool created = ReferenceEquals(stored, entry);
            List<string> warnings = new();

            if (created)
            {
                _lastUsed[entry.Endpoint.Id] = DateTimeOffset.UtcNow;
                _logger.LogInformation($"Registered client entry for endpoint {entry.Endpoint.DisplayName}({entry.Endpoint.Id}).");
                return Task.FromResult(new AgentClientRegistrationResult(
                    Registered: true,
                    Entry: entry,
                    Warnings: warnings.AsReadOnly(),
                    Message: null
                    ));
            }

            // Existing entry: update last-used timestamp for the stored entry, dispose the caller-created one.
            _lastUsed[stored.Endpoint.Id] = DateTimeOffset.UtcNow;

            try
            {
                (entry.Channel as IDisposable)?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed disposing duplicate channel for endpoint {entry.Endpoint.DisplayName}({entry.Endpoint.Id}).");
            }

            _logger.LogDebug($"Registration skipped for endpoint {entry.Endpoint.DisplayName}({entry.Endpoint.Id}) because an entry already exists.");

            return Task.FromResult(new AgentClientRegistrationResult(
                Registered: false,
                Entry: stored,
                Warnings: warnings.AsReadOnly(),
                Message: "An entry for the specified endpoint ID already exists."
                ));
        }

        public Task<bool> DeregisterAsync(Guid endpointId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureNotDisposed();

            if (_clients.TryRemove(endpointId, out var entry))
            {
                try
                {
                    (entry.Channel as IDisposable)?.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Disposing channel for endpoint {EndpointId} threw an exception.", endpointId);
                }

                _lastUsed.TryRemove(endpointId, out _);
                _logger.LogInformation("Deregistered client entry for endpoint {EndpointId}.", endpointId);
                return Task.FromResult(true);
            }

            _logger.LogDebug("Attempt to deregister unknown endpoint {EndpointId}.", endpointId);
            return Task.FromResult(false);
        }

        public Task<IReadOnlyCollection<AgentClient>> ListAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureNotDisposed();

            IReadOnlyCollection<AgentClient> snapshot = _clients.Values.ToList().AsReadOnly();
            return Task.FromResult(snapshot);
        }

        public Task<AgentClient?> GetAsync(Guid endpointId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureNotDisposed();

            if (_clients.TryGetValue(endpointId, out var entry))
            {
                // Update last-used timestamp on successful lookup.
                _lastUsed[endpointId] = DateTimeOffset.UtcNow;
                return Task.FromResult<AgentClient?>(entry);
            }

            return Task.FromResult<AgentClient?>(null);
        }

        public void Dispose()
        {
            // Ensure disposal only happens once (atomic).
            if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

            // Take a snapshot to avoid enumeration issues while disposing.
            var snapshot = _clients.ToArray();
            foreach (var kv in snapshot)
            {
                try
                {
                    (kv.Value.Channel as IDisposable)?.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error while disposing channel for endpoint {EndpointId}.", kv.Key);
                }
            }

            _clients.Clear();
            _lastUsed.Clear();
            _logger.LogInformation("AgentClientRegistry disposed.");
        }

        private void EnsureNotDisposed()
        {
            if (Volatile.Read(ref _disposed) != 0) throw new ObjectDisposedException(nameof(AgentClientRegistry));
        }
    }
}
