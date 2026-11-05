using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailRfc2231"/>.
/// </summary>
public interface IEmailRfc2231Contract
{
    /// <summary>
    /// Encodes the supplied value.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailRfc2231EncodeResult Encode(string name, ReadOnlySpan<byte> value, EmailRfc2231GenerationOptions options);
    /// <summary>
    /// Encodes the text.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "characterEncoding">The character encoding used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailRfc2231EncodeResult EncodeText(string name, string value, System.Text.Encoding characterEncoding, EmailRfc2231GenerationOptions options);
    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "parameters">The parameters used by the operation.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailRfc2231DecodeResult DecodeWithParametersAndMode(List<EmailRfc2231Parameter> parameters, EmailRfc2231DecodeMode mode);
    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "parameters">The parameters used by the operation.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <param name = "maximumSegments">The maximum segments used by the operation.</param>
    /// <param name = "maximumDecodedBytes">The source bytes or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailRfc2231DecodeResult DecodeWithParametersAndModeAndMaximumSegmentsAndMaximumDecodedBytes(List<EmailRfc2231Parameter> parameters, EmailRfc2231DecodeMode mode, int maximumSegments, long maximumDecodedBytes);
}
