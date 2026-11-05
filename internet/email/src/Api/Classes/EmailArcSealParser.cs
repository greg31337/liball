using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Parses ARC seal into library objects.
/// </summary>
/// <remarks>
/// Malformed email data is normally represented by status and issue information rather than by exceptions. Configured limits still bound the amount of work performed.
/// </remarks>
public class EmailArcSealParser : IEmailArcSealParserContract
{
    internal static EmailArcSealParseResult ParseImplementationWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
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
    public EmailArcSealParseResult ParseWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        return EmailArcSealParser.ParseImplementationWithInputAndMode(input, mode);
    }

    internal static EmailArcSealParseResult ParseImplementationWithInputAndOptions(ReadOnlySpan<byte> input, EmailSignatureParserOptions options)
    {
        return ParseImplementationWithInputAndOptionsAndInitialPosition(input, options, new EmailSourcePosition(0, 1, 1));
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailArcSealParseResult ParseWithInputAndOptions(ReadOnlySpan<byte> input, EmailSignatureParserOptions options)
    {
        return EmailArcSealParser.ParseImplementationWithInputAndOptions(input, options);
    }

    internal static EmailArcSealParseResult ParseImplementationWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailSignatureParserOptions options, EmailSourcePosition? initialPosition)
    {
        return EmailSignatureParserCore.ParseArcSeal(input, options, initialPosition);
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailArcSealParseResult ParseWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailSignatureParserOptions options, EmailSourcePosition? initialPosition)
    {
        return EmailArcSealParser.ParseImplementationWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }
}
