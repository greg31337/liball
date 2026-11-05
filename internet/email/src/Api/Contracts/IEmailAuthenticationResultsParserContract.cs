using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailAuthenticationResultsParser"/>.
/// </summary>
public interface IEmailAuthenticationResultsParserContract
{
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailAuthenticationResultsParseResult ParseWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode);
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailAuthenticationResultsParseResult ParseWithInputAndOptions(ReadOnlySpan<byte> input, EmailAuthenticationParserOptions options);
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailAuthenticationResultsParseResult ParseWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailAuthenticationParserOptions options, EmailSourcePosition? initialPosition);
}
