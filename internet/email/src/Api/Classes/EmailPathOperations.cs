using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Provides the public factory and type-level operations associated with <see cref = "EmailPath"/>.
/// </summary>
public class EmailPathOperations : IEmailPathOperationsContract
{
    /// <summary>Creates an empty root path.</summary>
    /// <returns>The configured path.</returns>
    public EmailPath CreateWith()
    {
        return EmailPath.CreateImplementationWith();
    }

    /// <summary>
    /// Creates a new path.
    /// </summary>
    /// <param name = "segments">The segments used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    /// <returns>The configured path.</returns>
    public EmailPath CreateWithSegments(List<int> segments)
    {
        return EmailPath.CreateImplementationWithSegments(segments);
    }

    /// <summary>
    /// Creates a root configured for the requested behavior.
    /// </summary>
    /// <returns>The path produced by the operation.</returns>
    public EmailPath CreateRoot()
    {
        return EmailPath.CreateRootImplementation();
    }
}
