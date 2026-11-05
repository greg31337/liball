using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailSourceInfo"/>.
/// </summary>
public interface IEmailSourceInfoOperationsContract
{
    /// <summary>Creates source information for a complete message range.</summary>
    /// <param name = "content">The source content.</param>
    /// <param name = "messageRange">The complete message range.</param>
    /// <returns>The configured source information.</returns>
    EmailSourceInfo CreateWithContentAndMessageRange(EmailContent content, EmailRange messageRange);
    /// <summary>
    /// Creates a new source info.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "messageRange">The message range used by the operation.</param>
    /// <param name = "headerBlockRange">The header block range used by the operation.</param>
    /// <param name = "separatorRange">The separator range used by the operation.</param>
    /// <param name = "bodyRange">The body range used by the operation.</param>
    /// <param name = "sourceName">The source name used by the operation.</param>
    /// <param name = "complete">Whether to enable the complete behavior.</param>
    /// <returns>The configured source information.</returns>
    EmailSourceInfo CreateWithContentAndMessageRangeAndHeaderBlockRangeAndSeparatorRangeAndBodyRangeAndSourceNameAndComplete(EmailContent? content, EmailRange? messageRange, EmailRange? headerBlockRange, EmailRange? separatorRange, EmailRange? bodyRange, string? sourceName, bool complete);
}
