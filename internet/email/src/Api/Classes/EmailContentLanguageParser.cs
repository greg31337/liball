using System;

namespace email;
/// <summary>
/// Parses content language into library objects.
/// </summary>
/// <remarks>
/// Malformed email data is normally represented by status and issue information rather than by exceptions. Configured limits still bound the amount of work performed.
/// </remarks>
public class EmailContentLanguageParser : IEmailContentLanguageParserContract
{
    internal static EmailContentLanguageParseResult ParseImplementationWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        EmailStructuredValueParserOptions options = new EmailStructuredValueParserOptions();
        options.SetMode(mode);
        return EmailMimeStructuredParserCore.ParseContentLanguage(input, options, new EmailSourcePosition(0, 1, 1));
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailContentLanguageParseResult ParseWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        return EmailContentLanguageParser.ParseImplementationWithInputAndMode(input, mode);
    }

    internal static EmailContentLanguageParseResult ParseImplementationWithInputAndOptions(ReadOnlySpan<byte> input, EmailStructuredValueParserOptions options)
    {
        return EmailMimeStructuredParserCore.ParseContentLanguage(input, options, new EmailSourcePosition(0, 1, 1));
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailContentLanguageParseResult ParseWithInputAndOptions(ReadOnlySpan<byte> input, EmailStructuredValueParserOptions options)
    {
        return EmailContentLanguageParser.ParseImplementationWithInputAndOptions(input, options);
    }

    internal static EmailContentLanguageParseResult ParseImplementationWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailStructuredValueParserOptions options, EmailSourcePosition? initialPosition)
    {
        return EmailMimeStructuredParserCore.ParseContentLanguage(input, options, initialPosition);
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailContentLanguageParseResult ParseWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailStructuredValueParserOptions options, EmailSourcePosition? initialPosition)
    {
        return EmailContentLanguageParser.ParseImplementationWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }
}
