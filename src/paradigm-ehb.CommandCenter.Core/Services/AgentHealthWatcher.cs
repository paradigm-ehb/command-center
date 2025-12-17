using Grpc.Core;
using Grpc.Health.V1;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using paradigm_ehb.CommandCenter.Core.Enums;
using paradigm_ehb.CommandCenter.Core.Models;
using Google.Protobuf.WellKnownTypes;

namespace paradigm_ehb.CommandCenter.Core.Services
{
    /// <summary>
    /// Monitors the health status of agent services and updates the endpoint's health.
    /// </summary>
    /// <remarks>AgentHealthWatcher observes the health of multiple predefined services using a gRPC health
    /// client. This class is not thread-safe for concurrent StartAsync or Stop calls. Dispose the instance to
    /// release resources when monitoring is no longer needed.</remarks>
    public sealed class AgentHealthWatcher : IDisposable
    {
        private static readonly string[] _servicesToWatch = [string.Empty];

        private AgentClient? _client;
        private readonly ConcurrentDictionary<string, AgentHealth> _degradedServices = new();
        private readonly List<Task> _watchTasks = new();
        private CancellationTokenSource? _cts;
        private int _degradedCount;
        private bool _disposed;

        public AgentHealthWatcher()
        {
        }

        /// <summary>
        /// Snapshot of service health states.
        /// </summary>
        public IReadOnlyDictionary<string, AgentHealth> DegradedServices
        {
            get { return _degradedServices.ToDictionary(entry => entry.Key, entry => entry.Value); }
        }

        /// <summary>
        /// Starts monitoring the configured services using the specified agent client.
        /// </summary>
        public Task StartAsync(AgentClient client, CancellationToken cancellationToken = default)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                throw new InvalidOperationException("AgentHealthWatcher is already running. Stop it before calling StartAsync again.");
            }

            _client = client;   // lazy init

            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            foreach (string service in _servicesToWatch)
            {
                CancellationToken token = _cts.Token;

                Task task = Task.Run(async () =>
                {
                    try
                    {
                        await WatchServiceAsync(service, token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                        // expected on cancellation
                    }
                    catch
                    {
                        // swallow to prevent unobserved exceptions; consider logging
                    }
                }, token);

                _watchTasks.Add(task);
            }

            return Task.CompletedTask;
        }

        private async Task WatchServiceAsync(string service, CancellationToken cancellationToken)
        {
            Health.HealthClient healthClient = _client!.Health;
            AgentEndpoint endpoint = _client.Endpoint;

            using AsyncServerStreamingCall<HealthCheckResponse> call =
                healthClient.Watch(new HealthCheckRequest { Service = service });

            await foreach (HealthCheckResponse response in call.ResponseStream.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                HealthCheckResponse.Types.ServingStatus responseStatus = response.Status;

                switch (responseStatus)
                {
                    case HealthCheckResponse.Types.ServingStatus.Serving:
                        endpoint.HealthStatus = AgentHealth.Healthy;
                        break;
                    case HealthCheckResponse.Types.ServingStatus.NotServing:
                        endpoint.HealthStatus = AgentHealth.Degraded;
                        break;
                    case HealthCheckResponse.Types.ServingStatus.Unknown:
                    default:
                        endpoint.HealthStatus = AgentHealth.Unknown;
                        break;
                }
            }
        }

        private void UpdateHealthStatus(AgentEndpoint endpoint)
        {
            AgentHealth newStatus = _degradedCount > 0 ? AgentHealth.Degraded : AgentHealth.Healthy;
            endpoint.HealthStatus = newStatus; // AgentEndpoint raises its own event on change
        }

        /// <summary>
        /// Requests health watch to stop and waits a short time for graceful shutdown.
        /// </summary>
        public void Stop()
        {
            if (_cts == null) return;

            try
            {
                _cts.Cancel();

                Task[] tasks = _watchTasks.ToArray();
                if (tasks.Length > 0)
                {
                    Task all = Task.WhenAll(tasks);
                    all.Wait(TimeSpan.FromSeconds(5));
                }
            }
            catch
            {
                // best effort
            }
            finally
            {
                _cts.Dispose();
                _cts = null;
                _watchTasks.Clear();
                _degradedServices.Clear();
                Interlocked.Exchange(ref _degradedCount, 0);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            try { Stop(); } catch { }
            _disposed = true;
        }
    }
}