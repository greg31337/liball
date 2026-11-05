using System;

namespace email;
/// <summary>
/// Provides the public factory and type-level operations associated with <see cref = "EmailSourceTracker"/>.
/// </summary>
public class EmailSourceTrackerOperations : IEmailSourceTrackerOperationsContract
{
    /// <summary>Creates a source tracker at the beginning of input.</summary>
    /// <returns>The configured source tracker.</returns>
    public EmailSourceTracker CreateWith()
    {
        return EmailSourceTracker.CreateImplementationWith();
    }

    /// <summary>
    /// Creates a new source tracker.
    /// </summary>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <returns>The configured source tracker.</returns>
    public EmailSourceTracker CreateWithInitialPosition(EmailSourcePosition? initialPosition)
    {
        return EmailSourceTracker.CreateImplementationWithInitialPosition(initialPosition);
    }
}
