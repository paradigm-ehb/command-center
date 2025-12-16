using Grpc.Health.V1;
using Grpc.Net.Client;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using paradigm_ehb.CommandCenter.Core.Enums;

namespace paradigm_ehb.CommandCenter.Core.Models
{
    public sealed class AgentClient : IDisposable
    {
        private bool disposedValue;
        private readonly object _endpointSync = new();

        /// <summary>
        /// Gets or sets the network endpoint information for the agent connection.
        /// </summary>
        public AgentEndpoint Endpoint { get; set; }

        public GrpcChannel Channel { get; init; }

        public Health.HealthClient Health { get; init; }

        public Greeter.GreeterClient Greeter { get; init; }

        /// <summary>
        /// Performs a health check against the remote agent and updates the local endpoint health.
        /// Returns the resolved <see cref="AgentHealthStatus"/>.
        /// </summary>
        public async Task<AgentHealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Use the async Response task so exceptions propagate and can be handled.
                HealthListResponse response = await Health.ListAsync(new HealthListRequest(), cancellationToken: cancellationToken).ResponseAsync.ConfigureAwait(false);

                bool anyUnhealthy = response?.Statuses == null || response.Statuses.Values.Any(service => service.Status != HealthCheckResponse.Types.ServingStatus.Serving);

                AgentHealthStatus resolved = anyUnhealthy ? AgentHealthStatus.Degraded : AgentHealthStatus.Healthy;

                return resolved;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Bubble cancellation to caller.
                throw;
            }
            catch
            {
                return AgentHealthStatus.Unknown;
            }
        }

        /// <summary>
        /// Disposes the resources used by the current instance of the <see cref="AgentClient"/> class.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    try
                    {
                        Channel?.Dispose();
                    } catch
                    {

                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~AgentClient()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        /// <summary>
        /// Disposes the resources used by the current instance of the <see cref="AgentClient"/> class.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
