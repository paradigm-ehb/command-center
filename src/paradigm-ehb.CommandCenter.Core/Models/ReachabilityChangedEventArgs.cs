using paradigm_ehb.CommandCenter.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace paradigm_ehb.CommandCenter.Core.Models
{
    public class ReachabilityChangedEventArgs : EventArgs
    {
        public AgentReachability AgentReachability { get; init; }

        public AgentEndpoint AgentEndpoint { get; init; }
    }
}
