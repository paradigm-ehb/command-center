using Grpc.Health.V1;
using paradigm_ehb.CommandCenter.Core.Enums;
using paradigm_ehb.CommandCenter.Core.Interfaces;
using paradigm_ehb.CommandCenter.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace paradigm_ehb.CommandCenter.Core.Services
{
    /// <summary>
    /// Agent Health Watcher that uses gRPC health checks to monitor the health status of active agents (AgentClients from AgentClientRegistry).
    /// </summary>
    public class AgentHealthWatcher : IAgentHealthWatcher, IAsyncDisposable
    {
        private readonly PeriodicTimer _timer;
        private readonly CancellationTokenSource _cts = new();
        private readonly int _maxConcurrency;

        public AgentHealthWatcher(TimeSpan interval, int maxConcurrency = 5)
        {
            _maxConcurrency = Math.Max(1, maxConcurrency);
            _timer = new PeriodicTimer(interval);
        }

        /// <summary>
        /// Starts monitoring the health of the specified agent clients asynchronously, polling each agent at regular
        /// intervals until cancellation is requested.
        /// </summary>
        /// <remarks>The monitoring operation runs continuously until cancellation is signaled via the
        /// associated cancellation token. Health checks for agent clients are performed concurrently, up to the
        /// configured concurrency limit. Exceptions encountered during individual health checks are handled internally
        /// and do not stop the monitoring loop.</remarks>
        /// <param name="agentClients">A read-only collection of agent clients to be monitored. Cannot be null.</param>
        /// <returns>A task that represents the asynchronous monitoring operation. The task completes when cancellation is
        /// requested.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="agentClients"/> is null.</exception>
        public async Task StartAsync(IReadOnlyCollection<AgentClient> agentClients)
        {
            if (agentClients is null) throw new ArgumentNullException(nameof(agentClients));

            while (await _timer.WaitForNextTickAsync(_cts.Token))
            {
                using SemaphoreSlim semaphore = new(_maxConcurrency);

                List<Task> tasks = new(agentClients.Count);

                foreach (AgentClient agentClient in agentClients)
                {
                    await semaphore.WaitAsync(_cts.Token).ConfigureAwait(false);

                    Task task = Task.Run(async () =>
                    {
                        try
                        {
                            // Delegate the RPC and endpoint modification to the AgentClient instance
                            AgentHealthStatus agentHealthStatus = await agentClient.CheckHealthAsync(_cts.Token).ConfigureAwait(false);
                            agentClient.Endpoint.HealthStatus = agentHealthStatus;
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }, _cts.Token);

                    tasks.Add(task);
                }

                try
                {
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    // continue running iterations; clients were marked degraded by AgentClient.CheckHealthAsync
                }
            }
        }

        /// <summary>
        /// Stops the watcher started by <see cref="StartAsync"/>.
        /// </summary>
        /// <remarks>Calling this method signals any associated tasks or operations to stop as soon as
        /// possible. The actual cancellation is cooperative and depends on the operation's support for
        /// cancellation.</remarks>
        public void Stop() => _cts.Cancel();

        ValueTask IAsyncDisposable.DisposeAsync()
        {
            _cts.Cancel();

        }
    }
}
