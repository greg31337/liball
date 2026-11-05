using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailHeaderWriter"/>.
/// </summary>
public interface IEmailHeaderWriterContract
{
    /// <summary>
    /// Writes the supplied raw to the requested destination.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <returns>The header write result produced by the operation.</returns>
    EmailHeaderWriteResult WriteRawWithHeaders(EmailHeaders headers);
    /// <summary>
    /// Writes the supplied raw to the requested destination.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The header write result produced by the operation.</returns>
    EmailHeaderWriteResult WriteRawWithHeadersAndOptions(EmailHeaders headers, EmailHeaderWriteOptions options);
    /// <summary>
    /// Writes the supplied preserving to the requested destination.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <returns>The header write result produced by the operation.</returns>
    EmailHeaderWriteResult WritePreservingWithHeaders(EmailHeaders headers);
    /// <summary>
    /// Writes the supplied preserving to the requested destination.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The header write result produced by the operation.</returns>
    EmailHeaderWriteResult WritePreservingWithHeadersAndOptions(EmailHeaders? headers, EmailHeaderWriteOptions options);
    /// <summary>
    /// Writes the supplied canonical to the requested destination.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <returns>The header write result produced by the operation.</returns>
    EmailHeaderWriteResult WriteCanonicalWithHeaders(EmailHeaders headers);
    /// <summary>
    /// Writes the supplied canonical to the requested destination.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The header write result produced by the operation.</returns>
    EmailHeaderWriteResult WriteCanonicalWithHeadersAndOptions(EmailHeaders headers, EmailHeaderWriteOptions options);
    /// <summary>
    /// Writes the supplied value to the requested destination.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The header write result produced by the operation.</returns>
    EmailHeaderWriteResult Write(EmailHeaders? headers, EmailHeaderWriteMode mode, EmailHeaderWriteOptions options);
    /// <summary>
    /// Writes the supplied raw to the requested destination.
    /// </summary>
    /// <param name = "block">The block used by the operation.</param>
    /// <returns>The header write result produced by the operation.</returns>
    EmailHeaderWriteResult WriteRawWithBlock(EmailHeaderBlockParseResult block);
    /// <summary>
    /// Writes the supplied raw to the requested destination.
    /// </summary>
    /// <param name = "block">The block used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The header write result produced by the operation.</returns>
    EmailHeaderWriteResult WriteRawWithBlockAndOptions(EmailHeaderBlockParseResult block, EmailHeaderWriteOptions options);
    /// <summary>
    /// Writes the supplied preserving to the requested destination.
    /// </summary>
    /// <param name = "block">The block used by the operation.</param>
    /// <returns>The header write result produced by the operation.</returns>
    EmailHeaderWriteResult WritePreservingWithBlock(EmailHeaderBlockParseResult block);
    /// <summary>
    /// Writes the supplied preserving to the requested destination.
    /// </summary>
    /// <param name = "block">The block used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The header write result produced by the operation.</returns>
    EmailHeaderWriteResult WritePreservingWithBlockAndOptions(EmailHeaderBlockParseResult block, EmailHeaderWriteOptions options);
}
