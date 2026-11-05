using System;
using System.Formats.Asn1;
using System.IO;
using System.IO.Compression;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailSmimeCompressor"/>.
/// </summary>
public interface IEmailSmimeCompressorContract
{
    /// <summary>
    /// Compresses the supplied value.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <returns>The S/MIME compression result produced by the operation.</returns>
    EmailSmimeCompressionResult CompressWithContent(ReadOnlySpan<byte> content);
    /// <summary>
    /// Compresses the supplied value.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The S/MIME compression result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    EmailSmimeCompressionResult CompressWithContentAndOptions(ReadOnlySpan<byte> content, EmailSmimeCompressionOptions options);
}
