using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailMessageIdParser"/>.
/// </summary>
public interface IEmailMessageIdParserContract
{
    /// <summary>
    /// Parses the supplied message ID data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailMessageIdParseResult ParseMessageIdWithInputAndMode(ReadOnlySpan<byte> input, EmailMessageIdParseMode mode);
    /// <summary>
    /// Parses the supplied content ID data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailMessageIdParseResult ParseContentIdWithInputAndMode(ReadOnlySpan<byte> input, EmailMessageIdParseMode mode);
    /// <summary>
    /// Parses the supplied references data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailMessageIdParseResult ParseReferencesWithInputAndMode(ReadOnlySpan<byte> input, EmailMessageIdParseMode mode);
    /// <summary>
    /// Parses the supplied in reply to data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailMessageIdParseResult ParseInReplyToWithInputAndMode(ReadOnlySpan<byte> input, EmailMessageIdParseMode mode);
    /// <summary>
    /// Parses the supplied message ID data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailMessageIdParseResult ParseMessageIdWithInputAndOptions(ReadOnlySpan<byte> input, EmailMessageIdParserOptions options);
    /// <summary>
    /// Parses the supplied message ID data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailMessageIdParseResult ParseMessageIdWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMessageIdParserOptions options, EmailSourcePosition? initialPosition);
    /// <summary>
    /// Parses the supplied content ID data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailMessageIdParseResult ParseContentIdWithInputAndOptions(ReadOnlySpan<byte> input, EmailMessageIdParserOptions options);
    /// <summary>
    /// Parses the supplied content ID data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailMessageIdParseResult ParseContentIdWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMessageIdParserOptions options, EmailSourcePosition? initialPosition);
    /// <summary>
    /// Parses the supplied references data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailMessageIdParseResult ParseReferencesWithInputAndOptions(ReadOnlySpan<byte> input, EmailMessageIdParserOptions options);
    /// <summary>
    /// Parses the supplied references data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailMessageIdParseResult ParseReferencesWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMessageIdParserOptions options, EmailSourcePosition? initialPosition);
    /// <summary>
    /// Parses the supplied in reply to data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailMessageIdParseResult ParseInReplyToWithInputAndOptions(ReadOnlySpan<byte> input, EmailMessageIdParserOptions options);
    /// <summary>
    /// Parses the supplied in reply to data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailMessageIdParseResult ParseInReplyToWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMessageIdParserOptions options, EmailSourcePosition? initialPosition);
}
