using System;
using System.Collections.Generic;
using System.Formats.Asn1;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailCmsWriter"/>.
/// </summary>
public interface IEmailCmsWriterContract
{
    /// <summary>
    /// Writes the supplied value to the requested destination.
    /// </summary>
    /// <param name = "contentInfo">The content info used by the operation.</param>
    /// <returns>The CMS write result produced by the operation.</returns>
    EmailCmsWriteResult WriteWithContentInfo(EmailCmsContentInfo contentInfo);
    /// <summary>
    /// Writes the supplied value to the requested destination.
    /// </summary>
    /// <param name = "contentInfo">The content info used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The CMS write result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailCmsWriteResult WriteWithContentInfoAndOptions(EmailCmsContentInfo contentInfo, EmailCmsWriterOptions options);
    /// <summary>
    /// Writes the supplied data to the requested destination.
    /// </summary>
    /// <param name = "data">The source bytes or data to process.</param>
    /// <returns>The CMS write result produced by the operation.</returns>
    EmailCmsWriteResult WriteDataWithData(ReadOnlySpan<byte> data);
    /// <summary>
    /// Writes the supplied data to the requested destination.
    /// </summary>
    /// <param name = "data">The source bytes or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The CMS write result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    EmailCmsWriteResult WriteDataWithDataAndOptions(ReadOnlySpan<byte> data, EmailCmsWriterOptions options);
}
