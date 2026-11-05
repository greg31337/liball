using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailArcMessageSignatureParser"/>.
/// </summary>
public interface IEmailArcMessageSignatureParserContract
{
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailArcMessageSignatureParseResult ParseWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode);
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailArcMessageSignatureParseResult ParseWithInputAndOptions(ReadOnlySpan<byte> input, EmailSignatureParserOptions options);
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailArcMessageSignatureParseResult ParseWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailSignatureParserOptions options, EmailSourcePosition? initialPosition);
}
