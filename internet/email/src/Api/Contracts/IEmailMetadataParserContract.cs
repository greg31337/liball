using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailMetadataParser"/>.
/// </summary>
public interface IEmailMetadataParserContract
{
    /// <summary>
    /// Parses the supplied keywords data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailMetadataParseResult ParseKeywordsWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode);
    /// <summary>
    /// Parses the supplied keywords data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailMetadataParseResult ParseKeywordsWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMetadataParserOptions options, EmailSourcePosition? initialPosition);
    /// <summary>
    /// Parses the supplied priority data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailMetadataParseResult ParsePriorityWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode);
    /// <summary>
    /// Parses the supplied priority data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailMetadataParseResult ParsePriorityWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMetadataParserOptions options, EmailSourcePosition? initialPosition);
    /// <summary>
    /// Parses the supplied x priority data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailMetadataParseResult ParseXPriorityWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode);
    /// <summary>
    /// Parses the supplied x priority data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailMetadataParseResult ParseXPriorityWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMetadataParserOptions options, EmailSourcePosition? initialPosition);
    /// <summary>
    /// Parses the supplied importance data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailMetadataParseResult ParseImportanceWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode);
    /// <summary>
    /// Parses the supplied importance data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailMetadataParseResult ParseImportanceWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMetadataParserOptions options, EmailSourcePosition? initialPosition);
    /// <summary>
    /// Parses the supplied sensitivity data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailMetadataParseResult ParseSensitivityWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode);
    /// <summary>
    /// Parses the supplied sensitivity data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailMetadataParseResult ParseSensitivityWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMetadataParserOptions options, EmailSourcePosition? initialPosition);
    /// <summary>
    /// Parses the supplied auto submitted data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailMetadataParseResult ParseAutoSubmittedWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode);
    /// <summary>
    /// Parses the supplied auto submitted data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailMetadataParseResult ParseAutoSubmittedWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMetadataParserOptions options, EmailSourcePosition? initialPosition);
    /// <summary>
    /// Parses the supplied list management data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailMetadataParseResult ParseListManagementWithInputAndKindAndMode(ReadOnlySpan<byte> input, EmailListManagementKind kind, EmailStructuredValueParseMode mode);
    /// <summary>
    /// Parses the supplied list management data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailMetadataParseResult ParseListManagementWithInputAndKindAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailListManagementKind kind, EmailMetadataParserOptions options, EmailSourcePosition? initialPosition);
    /// <summary>
    /// Parses the supplied list unsubscribe post data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailMetadataParseResult ParseListUnsubscribePostWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode);
    /// <summary>
    /// Parses the supplied list unsubscribe post data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailMetadataParseResult ParseListUnsubscribePostWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMetadataParserOptions options, EmailSourcePosition? initialPosition);
    /// <summary>
    /// Parses the supplied resent field data.
    /// </summary>
    /// <param name = "fieldName">The field name used by the operation.</param>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailMetadataParseResult ParseResentFieldWithFieldNameAndInputAndMode(string fieldName, ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode);
    /// <summary>
    /// Parses the supplied resent field data.
    /// </summary>
    /// <param name = "fieldName">The field name used by the operation.</param>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailMetadataParseResult ParseResentFieldWithFieldNameAndInputAndOptionsAndInitialPosition(string fieldName, ReadOnlySpan<byte> input, EmailMetadataParserOptions options, EmailSourcePosition? initialPosition);
}
