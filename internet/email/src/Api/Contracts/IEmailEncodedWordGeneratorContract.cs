using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailEncodedWordGenerator"/>.
/// </summary>
public interface IEmailEncodedWordGeneratorContract
{
    /// <summary>
    /// Encodes the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailEncodedWordEncodeResult Encode(ReadOnlySpan<byte> input, EmailEncodedWordGenerationOptions options);
    /// <summary>
    /// Encodes the text.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "characterEncoding">The character encoding used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailEncodedWordEncodeResult EncodeText(string input, Encoding characterEncoding, EmailEncodedWordGenerationOptions options);
}
