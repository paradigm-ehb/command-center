using System;
using System.Collections.Generic;
using System.Text;

namespace paradigm_ehb.CommandCenter.Core.Models
{
    /// <summary>
    /// Represents the result of a registration operation, including status indicators and any warnings encountered.
    /// </summary>
    /// <remarks>Use this type to inspect the outcome of a registration process, such as whether registration
    /// succeeded, whether a pre-warm step was attempted and succeeded, and to review any warnings generated during the
    /// operation. All properties are immutable and set during initialization.</remarks>
    public sealed record RegistrationResult
    (
        bool Registered,
        bool PreWarmAttempted,
        bool PreWarmSucceeded,
        IReadOnlyCollection<string> Warnings
    );
}
