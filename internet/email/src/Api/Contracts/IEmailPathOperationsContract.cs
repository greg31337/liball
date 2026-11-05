using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailPath"/>.
/// </summary>
public interface IEmailPathOperationsContract
{
    /// <summary>Creates an empty root path.</summary>
    /// <returns>The configured path.</returns>
    EmailPath CreateWith();
    /// <summary>
    /// Creates a new path.
    /// </summary>
    /// <param name = "segments">The segments used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    /// <returns>The configured path.</returns>
    EmailPath CreateWithSegments(List<int> segments);
    /// <summary>
    /// Creates a root configured for the requested behavior.
    /// </summary>
    /// <returns>The path produced by the operation.</returns>
    EmailPath CreateRoot();
}
