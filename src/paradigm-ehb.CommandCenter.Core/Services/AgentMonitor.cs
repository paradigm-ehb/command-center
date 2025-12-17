using paradigm_ehb.CommandCenter.Core.Enums;
using paradigm_ehb.CommandCenter.Core.Interfaces;
using paradigm_ehb.CommandCenter.Core.Models;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Linq;
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
        /// <param name="interval">The time interval between monitoring cycles. If null, a default interval of one minute is used.</param>
        /// <param name="maxConcurrency">The maximum number of monitoring operations that can run concurrently. Must be greater than zero.</param>
        public AgentMonitor(TimeSpan? interval = null, int maxConcurrency = 10)
        {
            _timer = new PeriodicTimer(interval ?? new TimeSpan(0, 1, 0));
            _maxConcurrency = maxConcurrency;
        }

        /// <summary>
        /// Starts the periodic probing process for agent endpoints using the specified registry.
        /// </summary>
        /// <remarks>The probing process continues until the associated cancellation token is triggered.
        /// If a new interval is provided, it updates the timer period for subsequent probe cycles.</remarks>
        /// <param name="agentEndpointRegistry">The registry used to retrieve the list of agent endpoints to probe. Cannot be null.</param>
        /// <param name="interval">An optional interval that specifies how often to perform the probing operation. If null, the existing timer
        /// period is used.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task StartAsync(IAgentEndpointRegistry agentEndpointRegistry, TimeSpan? interval = null)
        {
            _timer.Period = interval ?? _timer.Period; // Modify the timer period if a new interval is provided

            while (await _timer.WaitForNextTickAsync(_cts.Token))
            {
                IReadOnlyCollection<AgentEndpoint> agentEndpoints = await agentEndpointRegistry.ListMonitoringEnabledAsync(_cts.Token);
                await ProbeServersAsync(agentEndpoints, _cts.Token);
            }
        }

        public async Task StartAsync(AgentEndpoint agentEndpoint, TimeSpan? interval = null)
        {
            _timer.Period = interval ?? _timer.Period; // Modify the timer period if a new interval is provided
            while (await _timer.WaitForNextTickAsync(_cts.Token))
            {
                List<AgentEndpoint> agentEndpoints = new() { agentEndpoint };
                await ProbeServersAsync(agentEndpoints, _cts.Token);
            }
        }

        /// <summary>
        /// Probes the specified agent endpoints asynchronously to determine their online status.
        /// </summary>
        /// <remarks>The method updates the HealthStatus and LastSeen properties of each AgentEndpoint
        /// based on the probe result. The operation is performed concurrently, with the degree of concurrency limited
        /// by the configured maximum. Consumers can subscribe to the HealthStatusChanged event on AgentEndpoint to be
        /// notified of status changes.</remarks>
        /// <param name="agentEndpoints">A read-only collection of agent endpoints to probe for availability. Each endpoint's health status will be
        /// updated based on the probe result.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the probe operation.</param>
        /// <returns>A task that represents the asynchronous probe operation. The task completes when all endpoints have been
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
                        agentClient.Reachability = AgentReachability.Online;
                        agentClient.LastSeen = DateTimeOffset.UtcNow;
                    }
                    else
                    {
                        agentClient.Reachability = AgentReachability.Offline;
                    }
                    // Consumers can subscribe to AgentEndpoint.HealthStatusChanged directly.
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Attempts to establish a TCP connection to the specified IP address and port asynchronously, with support for
        /// cancellation and a configurable timeout.
        /// </summary>
        /// <remarks>If the connection attempt fails, is canceled, or times out, the method returns <see
        /// langword="false"/>. This method does not throw exceptions for connection failures or timeouts.</remarks>
        /// <param name="ipAddress">The IP address of the remote host to connect to. Must be a valid IPv4 or IPv6 address.</param>
        /// <param name="port">The port number on the remote host to connect to. Must be between 0 and 65535.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the connection attempt.</param>
        /// <param name="timeoutMs">The maximum time, in milliseconds, to wait for the connection to succeed before timing out. Must be greater
        /// than 0. The default is 1000 milliseconds.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if the TCP
        /// connection is established successfully; otherwise, <see langword="false"/>.</returns>
        private static async Task<bool> TcpProbeAsync(string ipAddress, int port, CancellationToken cancellationToken, int timeoutMs = 1000)
        {
            try
            {
                using TcpClient tcpClient = new();
                using CancellationTokenSource timeoutCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                timeoutCancellationTokenSource.CancelAfter(timeoutMs);

                await tcpClient.ConnectAsync(ipAddress, port, timeoutCancellationTokenSource.Token);
                return true;
            }
            catch
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
