using Grpc.Health.V1;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;

namespace paradigm_ehb.CommandCenter.Core.Models
{
    public sealed class AgentClient : IDisposable
    {
        private bool disposedValue;

        /// <summary>
        /// Gets or sets the network endpoint information for the agent connection.
        /// </summary>
        public AgentEndpoint Endpoint { get; set; }

        /// <summary>
        /// Gets the gRPC channel used for remote procedure calls.
        /// </summary>
        /// <remarks>The channel provides the underlying transport for gRPC client operations. This
        /// property is initialized during object construction and cannot be modified after initialization.</remarks>
        public GrpcChannel Channel { get; init; }

        /// <summary>
        /// Gets the gRPC client for performing health checks against the service.
        /// </summary>
        /// <remarks>Use this client to query the health status of the service or its components using
        /// standard gRPC health check methods.</remarks>
        public Health.HealthClient Health { get; init; }

        /// <summary>
        /// Gets the gRPC client used to communicate with the Greeter service.
        /// </summary>
        /// <remarks>Use this client to invoke remote procedures defined by the Greeter service, such as
        /// sending greeting requests. The property is initialized during object construction and cannot be modified
        /// afterwards.</remarks>
        public Greeter.GreeterClient Greeter { get; init; }

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

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
