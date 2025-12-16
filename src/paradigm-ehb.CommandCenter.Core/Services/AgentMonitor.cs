using paradigm_ehb.CommandCenter.Core.Enums;
using paradigm_ehb.CommandCenter.Core.Interfaces;
using paradigm_ehb.CommandCenter.Core.Models;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace paradigm_ehb.CommandCenter.Core.Services
{
    public sealed class AgentMonitor : IAgentMonitor, IAsyncDisposable
    {
        private readonly PeriodicTimer _timer;
        private readonly CancellationTokenSource _cts = new();
        private readonly int _maxConcurrency;

        /// <summary>
        /// Initializes a new instance of the AgentMonitor class with the specified monitoring interval and maximum
        /// concurrency level.
        /// </summary>
        /// <param name="interval">The time interval between each monitoring cycle.</param>
        /// <param name="maxConcurrency">The maximum number of monitoring operations that can run concurrently. The default is 10.</param>
        public AgentMonitor(TimeSpan? interval = null, int maxConcurrency = 10)
        {
            _timer = new PeriodicTimer(interval ?? new TimeSpan(0, 1, 0));
            _maxConcurrency = maxConcurrency;
        }

        /// <summary>
        /// Asynchronously starts the periodic probing of servers using the specified agent clients.
        /// </summary>
        /// <remarks>The probing continues until the associated cancellation token is triggered. This
        /// method is typically intended to be run as a long-lived background operation.</remarks>
        /// <param name="agentClients">A read-only collection of agent clients to use for probing servers. Cannot be null or empty.</param>
        /// <returns>A task that represents the asynchronous operation. The task completes when the probing loop is stopped.</returns>
        public async Task StartAsync(IAgentEndpointRegistry agentEndpointRegistry, TimeSpan? interval = null)
        {
            _timer.Period = interval ?? _timer.Period; // Modify the timer period if a new interval is provided

            while (await _timer.WaitForNextTickAsync(_cts.Token))
            {
                IReadOnlyCollection<AgentEndpoint> agentEndpoints = await agentEndpointRegistry.ListAsync(_cts.Token);
                await ProbeServersAsync(agentEndpoints, _cts.Token);
            }
        }

        /// <summary>
        /// Probes the specified agent servers asynchronously to determine their online status.
        /// </summary>
        /// <remarks>The method updates the health status and last seen time of each agent endpoint based
        /// on the probe results. The operation is performed concurrently, with the degree of concurrency limited by the
        /// configured maximum.</remarks>
        /// <param name="agentClients">A read-only collection of agent clients whose endpoints will be probed for connectivity.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the probe operation.</param>
        /// <returns>A task that represents the asynchronous probe operation. The task completes when all agent servers have been
        /// probed.</returns>
        private async Task ProbeServersAsync(IReadOnlyCollection<AgentEndpoint> agentEndpoints, CancellationToken cancellationToken)
        {
            using SemaphoreSlim semaphore = new SemaphoreSlim(_maxConcurrency);

            IEnumerable<Task> tasks = agentEndpoints.Select(async agentClient =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    bool online = await TcpProbeAsync(agentClient.IpAddress, agentClient.Port, cancellationToken);
                    if (online)
                    {
                        agentClient.HealthStatus = AgentHealthStatus.Online;
                        agentClient.LastSeen = DateTime.UtcNow;
                    } else
                    {
                        agentClient.HealthStatus = AgentHealthStatus.Offline;
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Attempts to establish a TCP connection to the specified IP address and port asynchronously, returning a
        /// value that indicates whether the connection was successful within the given timeout period.
        /// </summary>
        /// <remarks>If the connection attempt fails, is canceled, or does not complete within the
        /// specified timeout, the method returns <see langword="false"/>. This method does not throw exceptions for
        /// connection failures or timeouts.</remarks>
        /// <param name="ipAddress">The IP address of the remote host to connect to. This should be a valid IPv4 or IPv6 address in string
        /// format.</param>
        /// <param name="port">The port number on the remote host to connect to. Must be between 0 and 65535.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the connection attempt.</param>
        /// <param name="timeoutMs">The maximum time, in milliseconds, to wait for the connection to succeed before timing out. The default is
        /// 1000 milliseconds.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if the TCP
        /// connection is established successfully within the timeout period; otherwise, <see langword="false"/>.</returns>
        private static async Task<bool> TcpProbeAsync(string ipAddress, int port, CancellationToken cancellationToken, int timeoutMs = 1000)
        {
            try
            {
                using TcpClient tcpClient = new();
                using CancellationTokenSource timeoutCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                timeoutCancellationTokenSource.CancelAfter(timeoutMs);

                await tcpClient.ConnectAsync(ipAddress, port, timeoutCancellationTokenSource.Token);
                return true;
            } catch
            {
                return false; // Connection failed or timed out
            }
        }

        public async ValueTask DisposeAsync()
        {
            _cts.Cancel();
            _timer.Dispose();
        }
    }
}
