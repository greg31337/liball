using System;
using System.Collections.Generic;
using System.IO;

namespace email;
/// <summary>
/// Provides the public factory and type-level operations associated with <see cref = "EmailPartialMessageCollector"/>.
/// </summary>
public class EmailPartialMessageCollectorOperations : IEmailPartialMessageCollectorOperationsContract
{
    /// <summary>Creates a partial message collector using default policy.</summary>
    /// <returns>The configured collector.</returns>
    public EmailPartialMessageCollector CreateWith()
    {
        return EmailPartialMessageCollector.CreateImplementationWith();
    }

    /// <summary>
    /// Creates a new partial message collector.
    /// </summary>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <returns>The configured partial message collector.</returns>
    public EmailPartialMessageCollector CreateWithOptions(EmailPartialMessageAssemblyOptions options)
    {
        return EmailPartialMessageCollector.CreateImplementationWithOptions(options);
    }
}
