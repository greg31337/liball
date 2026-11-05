using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Parses ARC message signature into library objects.
/// </summary>
/// <remarks>
/// Malformed email data is normally represented by status and issue information rather than by exceptions. Configured limits still bound the amount of work performed.
/// </remarks>
public class EmailArcMessageSignatureParser : IEmailArcMessageSignatureParserContract
{
    internal static EmailArcMessageSignatureParseResult ParseImplementationWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        EmailSignatureParserOptions options = new EmailSignatureParserOptions();
        options.SetMode(mode);
        return ParseImplementationWithInputAndOptionsAndInitialPosition(input, options, new EmailSourcePosition(0, 1, 1));
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailArcMessageSignatureParseResult ParseWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        return EmailArcMessageSignatureParser.ParseImplementationWithInputAndMode(input, mode);
    }

    internal static EmailArcMessageSignatureParseResult ParseImplementationWithInputAndOptions(ReadOnlySpan<byte> input, EmailSignatureParserOptions options)
    {
        return ParseImplementationWithInputAndOptionsAndInitialPosition(input, options, new EmailSourcePosition(0, 1, 1));
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailArcMessageSignatureParseResult ParseWithInputAndOptions(ReadOnlySpan<byte> input, EmailSignatureParserOptions options)
    {
        return EmailArcMessageSignatureParser.ParseImplementationWithInputAndOptions(input, options);
    }

    internal static EmailArcMessageSignatureParseResult ParseImplementationWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailSignatureParserOptions options, EmailSourcePosition? initialPosition)
    {
        return EmailSignatureParserCore.ParseArcMessageSignature(input, options, initialPosition);
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailArcMessageSignatureParseResult ParseWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailSignatureParserOptions options, EmailSourcePosition? initialPosition)
    {
        return EmailArcMessageSignatureParser.ParseImplementationWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }
}
