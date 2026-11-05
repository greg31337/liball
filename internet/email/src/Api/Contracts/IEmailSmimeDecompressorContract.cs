using System;
using System.Formats.Asn1;
using System.IO;
using System.IO.Compression;
using System.Numerics;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailSmimeDecompressor"/>.
/// </summary>
public interface IEmailSmimeDecompressorContract
{
    /// <summary>
    /// Decompresses the supplied value.
    /// </summary>
    /// <param name = "cms">The cms used by the operation.</param>
    /// <returns>The S/MIME decompression result produced by the operation.</returns>
    EmailSmimeDecompressionResult DecompressWithCms(ReadOnlySpan<byte> cms);
    /// <summary>
    /// Decompresses the supplied value.
    /// </summary>
    /// <param name = "cms">The cms used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The S/MIME decompression result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailSmimeDecompressionResult DecompressWithCmsAndOptions(ReadOnlySpan<byte> cms, EmailSmimeDecompressionOptions options);
    /// <summary>
    /// Decompresses the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <returns>The S/MIME decompression result produced by the operation.</returns>
    EmailSmimeDecompressionResult DecompressWithEntity(MimeEntity entity);
    /// <summary>
    /// Decompresses the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The S/MIME decompression result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailSmimeDecompressionResult DecompressWithEntityAndOptions(MimeEntity entity, EmailSmimeDecompressionOptions options);
}
