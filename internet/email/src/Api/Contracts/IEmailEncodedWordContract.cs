using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailEncodedWord"/>.
/// </summary>
public interface IEmailEncodedWordContract
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
    EmailEncodedWordEncodeResult EncodeText(string input, System.Text.Encoding characterEncoding, EmailEncodedWordGenerationOptions options);
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <param name = "context">The context used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailEncodedWordParseResult ParseWithInputAndModeAndContext(ReadOnlySpan<byte> input, EmailEncodedWordParseMode mode, EmailEncodedWordContext context);
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <param name = "context">The context used by the operation.</param>
    /// <param name = "maximumEncodedWords">The maximum encoded words used by the operation.</param>
    /// <param name = "maximumDecodedBytes">The source bytes or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailEncodedWordParseResult ParseWithInputAndModeAndContextAndMaximumEncodedWordsAndMaximumDecodedBytes(ReadOnlySpan<byte> input, EmailEncodedWordParseMode mode, EmailEncodedWordContext context, int maximumEncodedWords, long maximumDecodedBytes);
}
