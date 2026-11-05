using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailRfc2231Generator"/>.
/// </summary>
public interface IEmailRfc2231GeneratorContract
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
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailRfc2231EncodeResult EncodeText(string name, string value, Encoding characterEncoding, EmailRfc2231GenerationOptions options);
}
