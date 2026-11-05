using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailSourceTracker"/>.
/// </summary>
public interface IEmailSourceTrackerOperationsContract
{
    /// <summary>Creates a source tracker at the beginning of input.</summary>
    /// <returns>The configured source tracker.</returns>
    EmailSourceTracker CreateWith();
    /// <summary>
    /// Creates a new source tracker.
    /// </summary>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <returns>The configured source tracker.</returns>
    EmailSourceTracker CreateWithInitialPosition(EmailSourcePosition? initialPosition);
}
