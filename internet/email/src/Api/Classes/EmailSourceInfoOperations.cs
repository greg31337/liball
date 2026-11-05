using System;

namespace email;
/// <summary>
/// Provides the public factory and type-level operations associated with <see cref = "EmailSourceInfo"/>.
/// </summary>
public class EmailSourceInfoOperations : IEmailSourceInfoOperationsContract
{
    /// <summary>Creates source information for a complete message range.</summary>
    /// <param name = "content">The source content.</param>
    /// <param name = "messageRange">The complete message range.</param>
    /// <returns>The configured source information.</returns>
    public EmailSourceInfo CreateWithContentAndMessageRange(EmailContent content, EmailRange messageRange)
    {
        return EmailSourceInfo.CreateImplementationWithContentAndMessageRange(content, messageRange);
    }

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
    public EmailSourceInfo CreateWithContentAndMessageRangeAndHeaderBlockRangeAndSeparatorRangeAndBodyRangeAndSourceNameAndComplete(EmailContent? content, EmailRange? messageRange, EmailRange? headerBlockRange, EmailRange? separatorRange, EmailRange? bodyRange, string? sourceName, bool complete)
    {
        return EmailSourceInfo.CreateImplementationWithContentAndMessageRangeAndHeaderBlockRangeAndSeparatorRangeAndBodyRangeAndSourceNameAndComplete(content, messageRange, headerBlockRange, separatorRange, bodyRange, sourceName, complete);
    }
}
