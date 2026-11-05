using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailOpenPgpArmor"/>.
/// </summary>
public interface IEmailOpenPgpArmorContract
{
    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailOpenPgpArmorResult DecodeWithInput(ReadOnlySpan<byte> input);
    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    EmailOpenPgpArmorResult DecodeWithInputAndOptions(ReadOnlySpan<byte> input, EmailOpenPgpReaderOptions options);
    /// <summary>
    /// Encodes the supplied value.
    /// </summary>
    /// <param name = "data">The source bytes or data to process.</param>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    byte[] EncodeWithDataAndKind(ReadOnlySpan<byte> data, EmailOpenPgpArmorKind kind);
    /// <summary>
    /// Encodes the supplied value.
    /// </summary>
    /// <param name = "data">The source bytes or data to process.</param>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "includeChecksum">Whether to enable the include checksum behavior.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    byte[] EncodeWithDataAndKindAndIncludeChecksum(ReadOnlySpan<byte> data, EmailOpenPgpArmorKind kind, bool includeChecksum);
    /// <summary>
    /// Computes the crc 24.
    /// </summary>
    /// <param name = "data">The source bytes or data to process.</param>
    /// <returns>The computed count or size.</returns>
    int ComputeCrc24(ReadOnlySpan<byte> data);
}
