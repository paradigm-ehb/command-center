using paradigm_ehb.CommandCenter.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace paradigm_ehb.CommandCenter.Core.Models
{
    public class HealthStatusChangedEventArgs : EventArgs
    {
        public AgentHealth HealthStatus { get; init; }

        public AgentClient AgentClient { get; init; }
    }
}
