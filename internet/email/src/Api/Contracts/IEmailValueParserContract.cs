using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailValueParser"/>.
/// </summary>
public interface IEmailValueParserContract
{
    /// <summary>
    /// Parses the supplied mailbox data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailAddressParseResult ParseMailbox(ReadOnlySpan<byte> input, EmailAddressParseMode mode);
    /// <summary>
    /// Parses the supplied address group data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailAddressParseResult ParseAddressGroup(ReadOnlySpan<byte> input, EmailAddressParseMode mode);
    /// <summary>
    /// Parses the supplied group data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailAddressParseResult ParseGroup(ReadOnlySpan<byte> input, EmailAddressParseMode mode);
    /// <summary>
    /// Parses the supplied address list data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailAddressParseResult ParseAddressListWithInputAndMode(ReadOnlySpan<byte> input, EmailAddressParseMode mode);
    /// <summary>
    /// Parses the supplied address list data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailAddressParseResult ParseAddressListWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailAddressParserOptions options, EmailSourcePosition? initialPosition);
    /// <summary>
    /// Parses the supplied date data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailDateParseResult ParseDateWithInputAndMode(ReadOnlySpan<byte> input, EmailDateParseMode mode);
    /// <summary>
    /// Parses the supplied date data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailDateParseResult ParseDateWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailDateParserOptions options, EmailSourcePosition? initialPosition);
    /// <summary>
    /// Parses the supplied message ID data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailMessageIdParseResult ParseMessageIdWithInputAndMode(ReadOnlySpan<byte> input, EmailMessageIdParseMode mode);
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
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailMessageIdParseResult ParseContentIdWithInputAndMode(ReadOnlySpan<byte> input, EmailMessageIdParseMode mode);
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
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailMessageIdParseResult ParseReferencesWithInputAndMode(ReadOnlySpan<byte> input, EmailMessageIdParseMode mode);
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
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailMessageIdParseResult ParseInReplyToWithInputAndMode(ReadOnlySpan<byte> input, EmailMessageIdParseMode mode);
    /// <summary>
    /// Parses the supplied in reply to data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailMessageIdParseResult ParseInReplyToWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMessageIdParserOptions options, EmailSourcePosition? initialPosition);
    /// <summary>
    /// Parses the supplied parameters data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailParameterParseResult ParseParametersWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode);
    /// <summary>
    /// Parses the supplied parameters data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailParameterParseResult ParseParametersWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailStructuredValueParserOptions options, EmailSourcePosition? initialPosition);
    /// <summary>
    /// Parses the supplied media type data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailMediaTypeParseResult ParseMediaTypeWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode);
    /// <summary>
    /// Parses the supplied media type data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailMediaTypeParseResult ParseMediaTypeWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailStructuredValueParserOptions options, EmailSourcePosition? initialPosition);
    /// <summary>
    /// Parses the supplied disposition data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailDispositionParseResult ParseDispositionWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode);
    /// <summary>
    /// Parses the supplied disposition data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailDispositionParseResult ParseDispositionWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailStructuredValueParserOptions options, EmailSourcePosition? initialPosition);
    /// <summary>
    /// Parses the supplied content language data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailContentLanguageParseResult ParseContentLanguageWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode);
    /// <summary>
    /// Parses the supplied content language data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailContentLanguageParseResult ParseContentLanguageWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailStructuredValueParserOptions options, EmailSourcePosition? initialPosition);
    /// <summary>
    /// Parses the supplied received data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailReceivedParseResult ParseReceivedWithInputAndMode(ReadOnlySpan<byte> input, EmailReceivedParseMode mode);
    /// <summary>
    /// Parses the supplied received data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailReceivedParseResult ParseReceivedWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailReceivedParserOptions options, EmailSourcePosition? initialPosition);
    /// <summary>
    /// Parses the supplied authentication results data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailAuthenticationResultsParseResult ParseAuthenticationResultsWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode);
    /// <summary>
    /// Parses the supplied authentication results data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailAuthenticationResultsParseResult ParseAuthenticationResultsWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailAuthenticationParserOptions options, EmailSourcePosition? initialPosition);
    /// <summary>
    /// Parses the supplied received spf data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailReceivedSpfParseResult ParseReceivedSpfWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode);
    /// <summary>
    /// Parses the supplied received spf data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailReceivedSpfParseResult ParseReceivedSpfWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailAuthenticationParserOptions options, EmailSourcePosition? initialPosition);
    /// <summary>
    /// Parses the supplied DKIM signature data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailDkimSignatureParseResult ParseDkimSignatureWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode);
    /// <summary>
    /// Parses the supplied DKIM signature data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailDkimSignatureParseResult ParseDkimSignatureWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailSignatureParserOptions options, EmailSourcePosition? initialPosition);
    /// <summary>
    /// Parses the supplied ARC message signature data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailArcMessageSignatureParseResult ParseArcMessageSignatureWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode);
    /// <summary>
    /// Parses the supplied ARC message signature data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailArcMessageSignatureParseResult ParseArcMessageSignatureWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailSignatureParserOptions options, EmailSourcePosition? initialPosition);
    /// <summary>
    /// Parses the supplied ARC seal data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailArcSealParseResult ParseArcSealWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode);
    /// <summary>
    /// Parses the supplied ARC seal data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailArcSealParseResult ParseArcSealWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailSignatureParserOptions options, EmailSourcePosition? initialPosition);
    /// <summary>
    /// Parses the supplied ARC authentication results data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailArcAuthenticationResultsParseResult ParseArcAuthenticationResultsWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode);
    /// <summary>
    /// Parses the supplied ARC authentication results data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailArcAuthenticationResultsParseResult ParseArcAuthenticationResultsWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailSignatureParserOptions options, EmailSourcePosition? initialPosition);
    /// <summary>Parses Return-Path.</summary>
    EmailStandardHeaderParseResult ParseReturnPathWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode);
    /// <summary>Parses Return-Path with options.</summary>
    EmailStandardHeaderParseResult ParseReturnPathWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailStructuredValueParserOptions options, EmailSourcePosition? initialPosition);
    /// <summary>Parses MIME-Version.</summary>
    EmailStandardHeaderParseResult ParseMimeVersionWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode);
    /// <summary>Parses MIME-Version with options.</summary>
    EmailStandardHeaderParseResult ParseMimeVersionWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailStructuredValueParserOptions options, EmailSourcePosition? initialPosition);
    /// <summary>Parses Content-Transfer-Encoding.</summary>
    EmailStandardHeaderParseResult ParseContentTransferEncodingWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode);
    /// <summary>Parses Content-Transfer-Encoding with options.</summary>
    EmailStandardHeaderParseResult ParseContentTransferEncodingWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailStructuredValueParserOptions options, EmailSourcePosition? initialPosition);
    /// <summary>Parses Content-Location.</summary>
    EmailStandardHeaderParseResult ParseContentLocationWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode);
    /// <summary>Parses Content-Location with options.</summary>
    EmailStandardHeaderParseResult ParseContentLocationWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailStructuredValueParserOptions options, EmailSourcePosition? initialPosition);
    /// <summary>Parses Newsgroups.</summary>
    EmailStandardHeaderParseResult ParseNewsgroupsWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode);
    /// <summary>Parses Newsgroups with options.</summary>
    EmailStandardHeaderParseResult ParseNewsgroupsWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailStructuredValueParserOptions options, EmailSourcePosition? initialPosition);
    /// <summary>Parses Followup-To.</summary>
    EmailStandardHeaderParseResult ParseFollowupToWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode);
    /// <summary>Parses Followup-To with options.</summary>
    EmailStandardHeaderParseResult ParseFollowupToWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailStructuredValueParserOptions options, EmailSourcePosition? initialPosition);
    /// <summary>Parses List-ID.</summary>
    EmailStandardHeaderParseResult ParseListIdWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode);
    /// <summary>Parses List-ID with options.</summary>
    EmailStandardHeaderParseResult ParseListIdWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailStructuredValueParserOptions options, EmailSourcePosition? initialPosition);
    /// <summary>Parses a common trace field.</summary>
    EmailStandardHeaderParseResult ParseTraceWithFieldNameAndInputAndMode(string fieldName, ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode);
    /// <summary>Parses a common trace field with options.</summary>
    EmailStandardHeaderParseResult ParseTraceWithFieldNameAndInputAndOptionsAndInitialPosition(string fieldName, ReadOnlySpan<byte> input, EmailStructuredValueParserOptions options, EmailSourcePosition? initialPosition);
    /// <summary>Parses Keywords.</summary>
    EmailMetadataParseResult ParseKeywordsWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode);
    /// <summary>Parses Keywords with options.</summary>
    EmailMetadataParseResult ParseKeywordsWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMetadataParserOptions options, EmailSourcePosition? initialPosition);
    /// <summary>Parses Priority.</summary>
    EmailMetadataParseResult ParsePriorityWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode);
    /// <summary>Parses Priority with options.</summary>
    EmailMetadataParseResult ParsePriorityWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMetadataParserOptions options, EmailSourcePosition? initialPosition);
    /// <summary>Parses X-Priority.</summary>
    EmailMetadataParseResult ParseXPriorityWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode);
    /// <summary>Parses X-Priority with options.</summary>
    EmailMetadataParseResult ParseXPriorityWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMetadataParserOptions options, EmailSourcePosition? initialPosition);
    /// <summary>Parses Importance.</summary>
    EmailMetadataParseResult ParseImportanceWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode);
    /// <summary>Parses Importance with options.</summary>
    EmailMetadataParseResult ParseImportanceWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMetadataParserOptions options, EmailSourcePosition? initialPosition);
    /// <summary>Parses Sensitivity.</summary>
    EmailMetadataParseResult ParseSensitivityWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode);
    /// <summary>Parses Sensitivity with options.</summary>
    EmailMetadataParseResult ParseSensitivityWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMetadataParserOptions options, EmailSourcePosition? initialPosition);
    /// <summary>Parses Auto-Submitted.</summary>
    EmailMetadataParseResult ParseAutoSubmittedWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode);
    /// <summary>Parses Auto-Submitted with options.</summary>
    EmailMetadataParseResult ParseAutoSubmittedWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMetadataParserOptions options, EmailSourcePosition? initialPosition);
    /// <summary>Parses a list-management value.</summary>
    EmailMetadataParseResult ParseListManagementWithInputAndKindAndMode(ReadOnlySpan<byte> input, EmailListManagementKind kind, EmailStructuredValueParseMode mode);
    /// <summary>Parses a list-management value with options.</summary>
    EmailMetadataParseResult ParseListManagementWithInputAndKindAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailListManagementKind kind, EmailMetadataParserOptions options, EmailSourcePosition? initialPosition);
    /// <summary>Parses List-Unsubscribe-Post.</summary>
    EmailMetadataParseResult ParseListUnsubscribePostWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode);
    /// <summary>Parses List-Unsubscribe-Post with options.</summary>
    EmailMetadataParseResult ParseListUnsubscribePostWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMetadataParserOptions options, EmailSourcePosition? initialPosition);
    /// <summary>Parses a resent field.</summary>
    EmailMetadataParseResult ParseResentFieldWithFieldNameAndInputAndMode(string fieldName, ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode);
    /// <summary>Parses a resent field with options.</summary>
    EmailMetadataParseResult ParseResentFieldWithFieldNameAndInputAndOptionsAndInitialPosition(string fieldName, ReadOnlySpan<byte> input, EmailMetadataParserOptions options, EmailSourcePosition? initialPosition);
}
