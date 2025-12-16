using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using paradigm_ehb.CommandCenter.Core.Models;

namespace paradigm_ehb.CommandCenter.Core.Interfaces
{
    public interface IAgentHealthWatcher
    {
        /// <summary>
        /// Starts watching the health of the given agent clients until stopped.
        /// </summary>
        /// <param name="agentClients">Read-only collection of AgentClient instances to monitor.</param>
        Task StartAsync(IReadOnlyCollection<AgentClient> agentClients);

        /// <summary>
        /// Requests the watcher to stop (cooperative cancellation).
        /// </summary>
        void Stop();
    }
}
