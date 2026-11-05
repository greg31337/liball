using System;
using System.Collections.Generic;
using System.IO;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailFeedParser"/>.
/// </summary>
public interface IEmailFeedParserOperationsContract
{
    /// <summary>Creates a feed parser using default policy.</summary>
    /// <returns>The configured feed parser.</returns>
    EmailFeedParser CreateWith();
    /// <summary>
    /// Creates a new feed parser.
    /// </summary>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <returns>The configured feed parser.</returns>
    EmailFeedParser CreateWithOptions(EmailParseOptions options);
}
