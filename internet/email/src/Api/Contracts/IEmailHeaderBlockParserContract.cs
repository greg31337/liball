using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailHeaderBlockParser"/>.
/// </summary>
public interface IEmailHeaderBlockParserContract
{
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailHeaderBlockParseResult ParseWithInput(ReadOnlySpan<byte> input);
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "endOfInput">Whether to enable the end of input behavior.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailHeaderBlockParseResult ParseWithInputAndEndOfInput(ReadOnlySpan<byte> input, bool endOfInput);
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "endOfInput">Whether to enable the end of input behavior.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailHeaderBlockParseResult ParseWithInputAndOptionsAndEndOfInput(ReadOnlySpan<byte> input, EmailHeaderBlockParserOptions options, bool endOfInput);
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailHeaderBlockParseResult ParseWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailHeaderBlockParserOptions options, EmailSourcePosition initialPosition);
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <param name = "endOfInput">Whether to enable the end of input behavior.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailHeaderBlockParseResult ParseWithInputAndOptionsAndInitialPositionAndEndOfInput(ReadOnlySpan<byte> input, EmailHeaderBlockParserOptions options, EmailSourcePosition initialPosition, bool endOfInput);
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <param name = "endOfInput">Whether to enable the end of input behavior.</param>
    /// <param name = "fieldCountLimitName">The field count limit name used by the operation.</param>
    /// <param name = "reportedFieldCountLimit">The reported field count limit used by the operation.</param>
    /// <param name = "reportedFieldCountObserved">The reported field count observed used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailHeaderBlockParseResult ParseWithInputAndOptionsAndInitialPositionAndEndOfInputAndFieldCountLimitNameAndReportedFieldCountLimitAndReportedFieldCountObserved(ReadOnlySpan<byte> input, EmailHeaderBlockParserOptions? options, EmailSourcePosition initialPosition, bool endOfInput, string fieldCountLimitName, long reportedFieldCountLimit, long reportedFieldCountObserved);
}
