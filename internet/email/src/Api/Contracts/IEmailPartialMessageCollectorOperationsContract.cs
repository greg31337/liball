using System;
using System.Collections.Generic;
using System.IO;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailPartialMessageCollector"/>.
/// </summary>
public interface IEmailPartialMessageCollectorOperationsContract
{
    /// <summary>Creates a partial message collector using default policy.</summary>
    /// <returns>The configured collector.</returns>
    EmailPartialMessageCollector CreateWith();
    /// <summary>
    /// Creates a new partial message collector.
    /// </summary>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <returns>The configured partial message collector.</returns>
    EmailPartialMessageCollector CreateWithOptions(EmailPartialMessageAssemblyOptions options);
}
