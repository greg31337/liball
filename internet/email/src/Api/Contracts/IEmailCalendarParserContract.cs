using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailCalendarParser"/>.
/// </summary>
public interface IEmailCalendarParserContract
{
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailCalendarParseResult ParseWithInputUsingReadOnlySpan(ReadOnlySpan<byte> input);
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    EmailCalendarParseResult ParseWithInputAndOptionsUsingReadOnlySpanAndEmailCalendarParseOptions(ReadOnlySpan<byte> input, EmailCalendarParseOptions options);
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailCalendarParseResult ParseWithInputUsingString(string input);
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailCalendarParseResult ParseWithInputAndOptionsUsingStringAndEmailCalendarParseOptions(string input, EmailCalendarParseOptions options);
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailCalendarParseResult ParseWithEntity(MimeEntity entity);
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailCalendarParseResult ParseWithEntityAndOptions(MimeEntity entity, EmailCalendarParseOptions options);
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailCalendarParseResult ParseWithMessage(EmailMessage message);
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailCalendarParseResult ParseWithMessageAndOptions(EmailMessage message, EmailCalendarParseOptions options);
    /// <summary>
    /// Determines whether the current value or state is sue.
    /// </summary>
    /// <param name = "severity">The severity used by the operation.</param>
    /// <param name = "code">The code used by the operation.</param>
    /// <param name = "description">The description used by the operation.</param>
    /// <param name = "fieldName">The field name used by the operation.</param>
    /// <param name = "recovery">The recovery used by the operation.</param>
    /// <returns>The issue produced by the operation.</returns>
    EmailIssue Issue(EmailIssueSeverity severity, EmailIssueCode code, string description, string? fieldName, string recovery);
}
