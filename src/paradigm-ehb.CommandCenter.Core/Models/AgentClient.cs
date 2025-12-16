using Grpc.Health.V1;
using Grpc.Net.Client;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using paradigm_ehb.CommandCenter.Core.Enums;
using Grpc.Core;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace paradigm_ehb.CommandCenter.Core.Models
{
    public sealed class AgentClient : IDisposable
    {
        private bool disposedValue;

        private readonly ConcurrentDictionary<string, bool> _degradedServices = new();
        private readonly List<Task> _healthWatchTasks = new();
        private CancellationTokenSource? _healthWatchCts;

        /// <summary>
        /// Gets or sets the network endpoint information for the agent connection.
        /// </summary>
        public AgentEndpoint Endpoint { get; set; }

        public GrpcChannel Channel { get; init; }

        public Dictionary<string, bool> DegradedServices
        {
            get
            {
                // expose a snapshot to callers
                return _degradedServices.ToDictionary(kv => kv.Key, kv => kv.Value);
            }
            init { } // keep compatibility with existing init-only usage
        }

        public Health.HealthClient Health { get; init; }

        public Greeter.GreeterClient Greeter { get; init; }

        /// <summary>
        /// Starts background health watch tasks for common services. This method is fire-and-forget
        /// from the caller perspective: the client will keep the child tasks and stop them when disposed
        /// or when <paramref name="cancellationToken"/> is signaled.
        /// </summary>
        public Task StartHealthWatch(CancellationToken cancellationToken = default)
        {
            // Ensure we only start once
            if (_healthWatchCts != null && !_healthWatchCts.IsCancellationRequested)
            {
                return Task.CompletedTask;
            }

            _healthWatchCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // list services to watch - adapt as needed
            string[] services = new[] { "health", "greeter", "services", "" };

            foreach (string service in services)
            {
                CancellationToken linkedToken = _healthWatchCts.Token;

                Task task = Task.Run(async () =>
                {
                    try
                    {
                        await StartServiceHealthWatch(service, linkedToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (linkedToken.IsCancellationRequested)
                    {
                        // expected on cancellation - swallow
                    }
                    catch
                    {
                        // swallow to prevent unobserved exceptions; consider logging
                    }
                }, linkedToken);

                // keep references so we can cancel and await them on dispose
                _healthWatchTasks.Add(task);
            }

            return Task.CompletedTask;
        }

        private async Task StartServiceHealthWatch(string service, CancellationToken cancellationToken)
        {
            // Create the streaming call. We will dispose the call in finally so cancellation cleans up resources.
            AsyncServerStreamingCall<HealthCheckResponse>? call = null;

            try
            {
                call = Health.Watch(new HealthCheckRequest() { Service = service });

                await foreach (HealthCheckResponse healthCheckResponse in call.ResponseStream.ReadAllAsync(cancellationToken))
                {
                    bool degraded = healthCheckResponse.Status != HealthCheckResponse.Types.ServingStatus.Serving;

                    // update concurrent dictionary
                    _degradedServices.AddOrUpdate(service, degraded, (_, __) => degraded);

                    // update aggregate endpoint health
                    if (_degradedServices.Values.Any(v => v))
                    {
                        Endpoint.HealthStatus = AgentHealthStatus.Degraded;
                    }
                    else
                    {
                        Endpoint.HealthStatus = AgentHealthStatus.Healthy;
                    }
                }
            }
            finally
            {
                try
                {
                    call?.Dispose();
                }
                catch
                {
                    // best-effort dispose
                }
            }
        }

        /// <summary>
        /// Requests health watch tasks to stop and waits a short time for graceful shutdown.
        /// </summary>
        public void StopHealthWatch()
        {
            if (_healthWatchCts == null) return;

            try
            {
                _healthWatchCts.Cancel();

                // wait for background tasks to complete (bounded wait)
                Task[] tasks = _healthWatchTasks.ToArray();
                if (tasks.Length > 0)
                {
                    Task all = Task.WhenAll(tasks);
                    all.Wait(TimeSpan.FromSeconds(5));
                }
            }
            catch
            {
                // swallow - stop is best effort
            }
            finally
            {
                _healthWatchCts.Dispose();
                _healthWatchCts = null;
                _healthWatchTasks.Clear();
                _degradedServices.Clear();
            }
        }

        /// <summary>
        /// Disposes the resources used by the current instance of the <see cref="AgentClient"/> class.
        /// Cancels any running health watches.
        /// </summary>
        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    try
                    {
                        StopHealthWatch();
                        Channel?.Dispose();
                    }
                    catch
                    {
                        // ignore
                    }
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
