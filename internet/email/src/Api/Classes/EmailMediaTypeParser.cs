using System;

namespace email;
/// <summary>
/// Parses media type into library objects.
/// </summary>
/// <remarks>
/// Malformed email data is normally represented by status and issue information rather than by exceptions. Configured limits still bound the amount of work performed.
/// </remarks>
public class EmailMediaTypeParser : IEmailMediaTypeParserContract
{
    internal static EmailMediaTypeParseResult ParseImplementationWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        EmailStructuredValueParserOptions options = new EmailStructuredValueParserOptions();
        options.SetMode(mode);
        return EmailMimeStructuredParserCore.ParseMediaType(input, options, new EmailSourcePosition(0, 1, 1));
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailMediaTypeParseResult ParseWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        return EmailMediaTypeParser.ParseImplementationWithInputAndMode(input, mode);
    }

    internal static EmailMediaTypeParseResult ParseImplementationWithInputAndOptions(ReadOnlySpan<byte> input, EmailStructuredValueParserOptions options)
    {
        return EmailMimeStructuredParserCore.ParseMediaType(input, options, new EmailSourcePosition(0, 1, 1));
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailMediaTypeParseResult ParseWithInputAndOptions(ReadOnlySpan<byte> input, EmailStructuredValueParserOptions options)
    {
        return EmailMediaTypeParser.ParseImplementationWithInputAndOptions(input, options);
    }

    internal static EmailMediaTypeParseResult ParseImplementationWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailStructuredValueParserOptions options, EmailSourcePosition? initialPosition)
    {
        return EmailMimeStructuredParserCore.ParseMediaType(input, options, initialPosition);
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailMediaTypeParseResult ParseWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailStructuredValueParserOptions options, EmailSourcePosition? initialPosition)
    {
        return EmailMediaTypeParser.ParseImplementationWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }
}
