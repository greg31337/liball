using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailArcSetCollector"/>.
/// </summary>
public interface IEmailArcSetCollectorContract
{
    /// <summary>
    /// Collects the supplied value.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <returns>The ARC set collection result produced by the operation.</returns>
    EmailArcSetCollectionResult CollectWithHeaders(EmailHeaders headers);
    /// <summary>
    /// Collects the supplied value.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The ARC set collection result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    EmailArcSetCollectionResult CollectWithHeadersAndOptions(EmailHeaders headers, EmailArcSetCollectionOptions options);
}
