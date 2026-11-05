using System;

namespace email;
/// <summary>
/// Provides the public factory and type-level operations associated with <see cref = "EmailProgress"/>.
/// </summary>
public class EmailProgressOperations : IEmailProgressOperationsContract
{
    /// <summary>Creates an empty progress tracker.</summary>
    /// <returns>The configured progress tracker.</returns>
    public EmailProgress CreateWith()
    {
        return EmailProgress.CreateImplementationWith();
    }

    /// <summary>
    /// Creates a new progress.
    /// </summary>
    /// <param name = "initialValues">The initial values used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <returns>The configured progress tracker.</returns>
    public EmailProgress CreateWithInitialValues(EmailProgressSnapshot initialValues)
    {
        return EmailProgress.CreateImplementationWithInitialValues(initialValues);
    }
}
