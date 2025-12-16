using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using paradigm_ehb.CommandCenter.Core.Models;

namespace paradigm_ehb.CommandCenter.Core.Interfaces
{
    public interface IAgentMonitor : IAsyncDisposable
    {
        /// <summary>
        /// Starts the periodic monitoring loop for the provided agent endpoints.
        /// The implementation should run until cancelled (typically via <see cref="IAsyncDisposable.DisposeAsync"/>).
        /// </summary>
        /// <param name="agentEndpoints">Read-only collection of endpoints to probe.</param>
        Task StartAsync(IReadOnlyCollection<AgentEndpoint> agentEndpoints);
    }
}
