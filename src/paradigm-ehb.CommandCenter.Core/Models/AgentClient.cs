using Grpc.Health.V1;
using Grpc.Net.Client;
using System;
using paradigm_ehb.CommandCenter.Core.Services;
using Journal.V1;

namespace paradigm_ehb.CommandCenter.Core.Models
{
    public sealed class AgentClient : IDisposable
    {
        private bool disposedValue;
        private AgentHealthWatcher? _healthWatcher;

        /// <summary>
        /// Gets or sets the network endpoint information for the agent connection.
        /// </summary>
        public AgentEndpoint Endpoint { get; set; }

        public GrpcChannel Channel { get; init; }

        public Health.HealthClient Health { get; init; }

        public Greeter.GreeterClient Greeter { get; init; }

        public JournalService.JournalServiceClient Journal { get; init; }

        public bool HealthWatchEnabled { get; set; } = true;

        /// <summary>
        /// Starts background health watch for common services using AgentHealthWatcher.
        /// </summary>
        public void StartHealthWatch(System.Threading.CancellationToken cancellationToken = default)
        {
            _healthWatcher ??= new AgentHealthWatcher();
            _healthWatcher.StartAsync(this, cancellationToken);
        }

        /// <summary>
        /// Requests health watch to stop and waits a short time for graceful shutdown.
        /// </summary>
        public void StopHealthWatch()
        {
            _healthWatcher?.Stop();
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
                        _healthWatcher?.Dispose();
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
