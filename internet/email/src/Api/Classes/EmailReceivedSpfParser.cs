using System;

namespace email;
/// <summary>
/// Parses received spf into library objects.
/// </summary>
/// <remarks>
/// Malformed email data is normally represented by status and issue information rather than by exceptions. Configured limits still bound the amount of work performed.
/// </remarks>
public class EmailReceivedSpfParser : IEmailReceivedSpfParserContract
{
    internal static EmailReceivedSpfParseResult ParseImplementationWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        EmailAuthenticationParserOptions options = new EmailAuthenticationParserOptions();
        options.SetMode(mode);
        return ParseImplementationWithInputAndOptionsAndInitialPosition(input, options, new EmailSourcePosition(0, 1, 1));
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailReceivedSpfParseResult ParseWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        return EmailReceivedSpfParser.ParseImplementationWithInputAndMode(input, mode);
    }

    internal static EmailReceivedSpfParseResult ParseImplementationWithInputAndOptions(ReadOnlySpan<byte> input, EmailAuthenticationParserOptions options)
    {
        return ParseImplementationWithInputAndOptionsAndInitialPosition(input, options, new EmailSourcePosition(0, 1, 1));
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailReceivedSpfParseResult ParseWithInputAndOptions(ReadOnlySpan<byte> input, EmailAuthenticationParserOptions options)
    {
        return EmailReceivedSpfParser.ParseImplementationWithInputAndOptions(input, options);
    }

    internal static EmailReceivedSpfParseResult ParseImplementationWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailAuthenticationParserOptions options, EmailSourcePosition? initialPosition)
    {
        return EmailAuthenticationParserCore.ParseReceivedSpf(input, options, initialPosition);
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailReceivedSpfParseResult ParseWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailAuthenticationParserOptions options, EmailSourcePosition? initialPosition)
    {
        return EmailReceivedSpfParser.ParseImplementationWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }
}
