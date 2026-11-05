using System;
using System.Collections.Generic;
using System.IO;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailReportFieldBlockParser"/>.
/// </summary>
public interface IEmailReportFieldBlockParserContract
{
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailReportFieldBlockParseResult ParseWithInput(ReadOnlySpan<byte> input);
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailReportFieldBlockParseResult ParseWithInputAndOptions(ReadOnlySpan<byte> input, EmailReportParseOptions options);
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    EmailReportFieldBlockParseResult ParseWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailReportParseOptions options, EmailSourcePosition initialPosition);
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailReportFieldBlockParseResult ParseWithEntity(MimeEntity entity);
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailReportFieldBlockParseResult ParseWithEntityAndOptions(MimeEntity entity, EmailReportParseOptions options);
}
