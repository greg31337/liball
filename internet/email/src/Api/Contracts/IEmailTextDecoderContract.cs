using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailTextDecoder"/>.
/// </summary>
public interface IEmailTextDecoderContract
{
    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailTextDecodeResult DecodeWithEntity(MimeEntity entity);
    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailTextDecodeResult DecodeWithEntityAndOptions(MimeEntity entity, EmailTextDecodeOptions options);
    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailTextDecodeResult DecodeWithEntityAndOptionsAndPath(MimeEntity entity, EmailTextDecodeOptions options, EmailPath? path);
    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <param name = "limitName">The limit name used by the operation.</param>
    /// <param name = "reportedLimit">The reported limit used by the operation.</param>
    /// <param name = "reportedObserved">The reported observed used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailTextDecodeResult DecodeWithEntityAndOptionsAndPathAndLimitNameAndReportedLimitAndReportedObserved(MimeEntity entity, EmailTextDecodeOptions options, EmailPath? path, string limitName, long reportedLimit, long reportedObserved);
    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "attachment">The attachment used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailTextDecodeResult DecodeWithAttachment(EmailAttachmentView attachment);
    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "attachment">The attachment used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailTextDecodeResult DecodeWithAttachmentAndOptions(EmailAttachmentView attachment, EmailTextDecodeOptions options);
    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "declaredCharset">The declared charset used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailTextDecodeResult DecodeWithInputAndDeclaredCharset(ReadOnlySpan<byte> input, string declaredCharset);
    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "declaredCharset">The declared charset used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    EmailTextDecodeResult DecodeWithInputAndDeclaredCharsetAndOptions(ReadOnlySpan<byte> input, string declaredCharset, EmailTextDecodeOptions options);
}
