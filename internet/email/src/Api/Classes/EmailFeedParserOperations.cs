using System;
using System.Collections.Generic;
using System.IO;

namespace email;
/// <summary>
/// Provides the public factory and type-level operations associated with <see cref = "EmailFeedParser"/>.
/// </summary>
public class EmailFeedParserOperations : IEmailFeedParserOperationsContract
{
    /// <summary>Creates a feed parser using default policy.</summary>
    /// <returns>The configured feed parser.</returns>
    public EmailFeedParser CreateWith()
    {
        return EmailFeedParser.CreateImplementationWith();
    }

    /// <summary>
    /// Creates a new feed parser.
    /// </summary>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <returns>The configured feed parser.</returns>
    public EmailFeedParser CreateWithOptions(EmailParseOptions options)
    {
        return EmailFeedParser.CreateImplementationWithOptions(options);
    }
}
