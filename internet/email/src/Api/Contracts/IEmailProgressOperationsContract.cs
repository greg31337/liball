using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailProgress"/>.
/// </summary>
public interface IEmailProgressOperationsContract
{
    /// <summary>Creates an empty progress tracker.</summary>
    /// <returns>The configured progress tracker.</returns>
    EmailProgress CreateWith();
    /// <summary>
    /// Creates a new progress.
    /// </summary>
    /// <param name = "initialValues">The initial values used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <returns>The configured progress tracker.</returns>
    EmailProgress CreateWithInitialValues(EmailProgressSnapshot initialValues);
}
