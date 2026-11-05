using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailValueWriter"/>.
/// </summary>
public interface IEmailValueWriterContract
{
    /// <summary>
    /// Writes the supplied address to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The value format result produced by the operation.</returns>
    EmailValueFormatResult WriteAddressWithValue(EmailAddress value);
    /// <summary>
    /// Writes the supplied address to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The value format result produced by the operation.</returns>
    EmailValueFormatResult WriteAddressWithValueAndOptions(EmailAddress value, EmailAddressFormatOptions options);
    /// <summary>
    /// Writes the supplied address group to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The value format result produced by the operation.</returns>
    EmailValueFormatResult WriteAddressGroupWithValue(EmailAddressGroup value);
    /// <summary>
    /// Writes the supplied address group to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The value format result produced by the operation.</returns>
    EmailValueFormatResult WriteAddressGroupWithValueAndOptions(EmailAddressGroup value, EmailAddressFormatOptions options);
    /// <summary>
    /// Writes the supplied address list to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The value format result produced by the operation.</returns>
    EmailValueFormatResult WriteAddressListWithValue(EmailAddressList value);
    /// <summary>
    /// Writes the supplied address list to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The value format result produced by the operation.</returns>
    EmailValueFormatResult WriteAddressListWithValueAndOptions(EmailAddressList value, EmailAddressFormatOptions options);
    /// <summary>
    /// Writes the supplied date to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The value format result produced by the operation.</returns>
    EmailValueFormatResult WriteDateWithValue(EmailDateTime value);
    /// <summary>
    /// Writes the supplied date to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The value format result produced by the operation.</returns>
    EmailValueFormatResult WriteDateWithValueAndOptions(EmailDateTime value, EmailDateFormatOptions options);
    /// <summary>
    /// Writes the supplied message ID to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The value format result produced by the operation.</returns>
    EmailValueFormatResult WriteMessageIdWithValue(EmailMessageId value);
    /// <summary>
    /// Writes the supplied message ID to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The value format result produced by the operation.</returns>
    EmailValueFormatResult WriteMessageIdWithValueAndOptions(EmailMessageId value, EmailMessageIdFormatOptions options);
    /// <summary>
    /// Writes the supplied content ID to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The value format result produced by the operation.</returns>
    EmailValueFormatResult WriteContentIdWithValue(EmailMessageId value);
    /// <summary>
    /// Writes the supplied content ID to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The value format result produced by the operation.</returns>
    EmailValueFormatResult WriteContentIdWithValueAndOptions(EmailMessageId value, EmailMessageIdFormatOptions options);
    /// <summary>
    /// Writes the supplied message ID list to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The value format result produced by the operation.</returns>
    EmailValueFormatResult WriteMessageIdListWithValue(EmailMessageIdList value);
    /// <summary>
    /// Writes the supplied message ID list to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The value format result produced by the operation.</returns>
    EmailValueFormatResult WriteMessageIdListWithValueAndOptions(EmailMessageIdList value, EmailMessageIdFormatOptions options);
    /// <summary>
    /// Writes the supplied references to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The value format result produced by the operation.</returns>
    EmailValueFormatResult WriteReferencesWithValue(EmailMessageIdList value);
    /// <summary>
    /// Writes the supplied references to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The value format result produced by the operation.</returns>
    EmailValueFormatResult WriteReferencesWithValueAndOptions(EmailMessageIdList value, EmailMessageIdFormatOptions options);
    /// <summary>
    /// Writes the supplied in reply to to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The value format result produced by the operation.</returns>
    EmailValueFormatResult WriteInReplyToWithValue(EmailMessageIdList value);
    /// <summary>
    /// Writes the supplied in reply to to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The value format result produced by the operation.</returns>
    EmailValueFormatResult WriteInReplyToWithValueAndOptions(EmailMessageIdList value, EmailMessageIdFormatOptions options);
    /// <summary>
    /// Writes the supplied parameters to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The value format result produced by the operation.</returns>
    EmailValueFormatResult WriteParametersWithValue(EmailParameterList value);
    /// <summary>
    /// Writes the supplied parameters to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The value format result produced by the operation.</returns>
    EmailValueFormatResult WriteParametersWithValueAndOptions(EmailParameterList value, EmailValueFormatOptions options);
    /// <summary>
    /// Writes the supplied media type to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The value format result produced by the operation.</returns>
    EmailValueFormatResult WriteMediaTypeWithValue(EmailMediaType? value);
    /// <summary>
    /// Writes the supplied media type to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The value format result produced by the operation.</returns>
    EmailValueFormatResult WriteMediaTypeWithValueAndOptions(EmailMediaType? value, EmailValueFormatOptions options);
    /// <summary>
    /// Writes the supplied disposition to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The value format result produced by the operation.</returns>
    EmailValueFormatResult WriteDispositionWithValue(EmailDisposition value);
    /// <summary>
    /// Writes the supplied disposition to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The value format result produced by the operation.</returns>
    EmailValueFormatResult WriteDispositionWithValueAndOptions(EmailDisposition value, EmailValueFormatOptions options);
    /// <summary>
    /// Writes the supplied content language to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The value format result produced by the operation.</returns>
    EmailValueFormatResult WriteContentLanguageWithValue(EmailContentLanguage value);
    /// <summary>
    /// Writes the supplied content language to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The value format result produced by the operation.</returns>
    EmailValueFormatResult WriteContentLanguageWithValueAndOptions(EmailContentLanguage value, EmailValueFormatOptions options);
    /// <summary>
    /// Writes the supplied received to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The value format result produced by the operation.</returns>
    EmailValueFormatResult WriteReceivedWithValue(EmailReceived value);
    /// <summary>
    /// Writes the supplied received to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The value format result produced by the operation.</returns>
    EmailValueFormatResult WriteReceivedWithValueAndOptions(EmailReceived value, EmailValueFormatOptions options);
    /// <summary>
    /// Writes the supplied authentication results to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The value format result produced by the operation.</returns>
    EmailValueFormatResult WriteAuthenticationResultsWithValue(EmailAuthenticationResults value);
    /// <summary>
    /// Writes the supplied authentication results to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The value format result produced by the operation.</returns>
    EmailValueFormatResult WriteAuthenticationResultsWithValueAndOptions(EmailAuthenticationResults value, EmailAuthenticationFormatOptions options);
    /// <summary>
    /// Writes the supplied received spf to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The value format result produced by the operation.</returns>
    EmailValueFormatResult WriteReceivedSpfWithValue(EmailReceivedSpf value);
    /// <summary>
    /// Writes the supplied received spf to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The value format result produced by the operation.</returns>
    EmailValueFormatResult WriteReceivedSpfWithValueAndOptions(EmailReceivedSpf value, EmailAuthenticationFormatOptions options);
    /// <summary>
    /// Writes the supplied DKIM signature to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The value format result produced by the operation.</returns>
    EmailValueFormatResult WriteDkimSignatureWithValue(EmailDkimSignature value);
    /// <summary>
    /// Writes the supplied DKIM signature to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The value format result produced by the operation.</returns>
    EmailValueFormatResult WriteDkimSignatureWithValueAndOptions(EmailDkimSignature value, EmailSignatureFormatOptions options);
    /// <summary>
    /// Writes a deterministic canonical DKIM DNS key record.
    /// </summary>
    /// <param name = "value">The DKIM key to write.</param>
    /// <returns>The value format result.</returns>
    EmailValueFormatResult WriteDkimKeyWithValue(EmailDkimKey value);
    /// <summary>
    /// Writes a deterministic canonical DKIM DNS key record with options.
    /// </summary>
    /// <param name = "value">The DKIM key to write.</param>
    /// <param name = "options">The formatting limits and initial column.</param>
    /// <returns>The value format result.</returns>
    EmailValueFormatResult WriteDkimKeyWithValueAndOptions(EmailDkimKey value, EmailValueFormatOptions options);
    /// <summary>
    /// Writes the supplied ARC message signature to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The value format result produced by the operation.</returns>
    EmailValueFormatResult WriteArcMessageSignatureWithValue(EmailDkimSignature value);
    /// <summary>
    /// Writes the supplied ARC message signature to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The value format result produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailValueFormatResult WriteArcMessageSignatureWithValueAndOptions(EmailDkimSignature value, EmailSignatureFormatOptions options);
    /// <summary>
    /// Writes the supplied ARC seal to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The value format result produced by the operation.</returns>
    EmailValueFormatResult WriteArcSealWithValue(EmailArcSeal value);
    /// <summary>
    /// Writes the supplied ARC seal to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The value format result produced by the operation.</returns>
    EmailValueFormatResult WriteArcSealWithValueAndOptions(EmailArcSeal value, EmailSignatureFormatOptions options);
    /// <summary>
    /// Writes the supplied ARC authentication results to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The value format result produced by the operation.</returns>
    EmailValueFormatResult WriteArcAuthenticationResultsWithValue(EmailArcAuthenticationResults value);
    /// <summary>
    /// Writes the supplied ARC authentication results to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The value format result produced by the operation.</returns>
    EmailValueFormatResult WriteArcAuthenticationResultsWithValueAndOptions(EmailArcAuthenticationResults value, EmailSignatureFormatOptions options);
    /// <summary>Writes any supported standard header value.</summary>
    EmailValueFormatResult WriteStandardHeaderWithValue(EmailStandardHeaderValue value);
    /// <summary>Writes any supported standard header value with options.</summary>
    EmailValueFormatResult WriteStandardHeaderWithValueAndOptions(EmailStandardHeaderValue value, EmailValueFormatOptions options);
    /// <summary>Writes Return-Path.</summary>
    EmailValueFormatResult WriteReturnPathWithValue(EmailStandardHeaderValue value);
    /// <summary>Writes Return-Path with options.</summary>
    EmailValueFormatResult WriteReturnPathWithValueAndOptions(EmailStandardHeaderValue value, EmailValueFormatOptions options);
    /// <summary>Writes MIME-Version.</summary>
    EmailValueFormatResult WriteMimeVersionWithValue(EmailStandardHeaderValue value);
    /// <summary>Writes MIME-Version with options.</summary>
    EmailValueFormatResult WriteMimeVersionWithValueAndOptions(EmailStandardHeaderValue value, EmailValueFormatOptions options);
    /// <summary>Writes Content-Transfer-Encoding.</summary>
    EmailValueFormatResult WriteContentTransferEncodingWithValue(EmailStandardHeaderValue value);
    /// <summary>Writes Content-Transfer-Encoding with options.</summary>
    EmailValueFormatResult WriteContentTransferEncodingWithValueAndOptions(EmailStandardHeaderValue value, EmailValueFormatOptions options);
    /// <summary>Writes Content-Location.</summary>
    EmailValueFormatResult WriteContentLocationWithValue(EmailStandardHeaderValue value);
    /// <summary>Writes Content-Location with options.</summary>
    EmailValueFormatResult WriteContentLocationWithValueAndOptions(EmailStandardHeaderValue value, EmailValueFormatOptions options);
    /// <summary>Writes Newsgroups.</summary>
    EmailValueFormatResult WriteNewsgroupsWithValue(EmailStandardHeaderValue value);
    /// <summary>Writes Newsgroups with options.</summary>
    EmailValueFormatResult WriteNewsgroupsWithValueAndOptions(EmailStandardHeaderValue value, EmailValueFormatOptions options);
    /// <summary>Writes Followup-To.</summary>
    EmailValueFormatResult WriteFollowupToWithValue(EmailStandardHeaderValue value);
    /// <summary>Writes Followup-To with options.</summary>
    EmailValueFormatResult WriteFollowupToWithValueAndOptions(EmailStandardHeaderValue value, EmailValueFormatOptions options);
    /// <summary>Writes List-ID.</summary>
    EmailValueFormatResult WriteListIdWithValue(EmailStandardHeaderValue value);
    /// <summary>Writes List-ID with options.</summary>
    EmailValueFormatResult WriteListIdWithValueAndOptions(EmailStandardHeaderValue value, EmailValueFormatOptions options);
    /// <summary>Writes a trace value.</summary>
    EmailValueFormatResult WriteTraceWithValue(EmailStandardHeaderValue value);
    /// <summary>Writes a trace value with options.</summary>
    EmailValueFormatResult WriteTraceWithValueAndOptions(EmailStandardHeaderValue value, EmailValueFormatOptions options);
    /// <summary>Writes Keywords.</summary>
    EmailValueFormatResult WriteKeywordsWithValue(EmailKeywordList value);
    /// <summary>Writes Keywords with options.</summary>
    EmailValueFormatResult WriteKeywordsWithValueAndOptions(EmailKeywordList value, EmailValueFormatOptions options);
    /// <summary>Writes Priority or X-Priority.</summary>
    EmailValueFormatResult WritePriorityWithValue(EmailPriority value);
    /// <summary>Writes Priority or X-Priority with options.</summary>
    EmailValueFormatResult WritePriorityWithValueAndOptions(EmailPriority value, EmailValueFormatOptions options);
    /// <summary>Writes Importance.</summary>
    EmailValueFormatResult WriteImportanceWithValue(EmailImportance value);
    /// <summary>Writes Importance with options.</summary>
    EmailValueFormatResult WriteImportanceWithValueAndOptions(EmailImportance value, EmailValueFormatOptions options);
    /// <summary>Writes Sensitivity.</summary>
    EmailValueFormatResult WriteSensitivityWithValue(EmailSensitivity value);
    /// <summary>Writes Sensitivity with options.</summary>
    EmailValueFormatResult WriteSensitivityWithValueAndOptions(EmailSensitivity value, EmailValueFormatOptions options);
    /// <summary>Writes Auto-Submitted.</summary>
    EmailValueFormatResult WriteAutoSubmittedWithValue(EmailAutoSubmitted value);
    /// <summary>Writes Auto-Submitted with options.</summary>
    EmailValueFormatResult WriteAutoSubmittedWithValueAndOptions(EmailAutoSubmitted value, EmailValueFormatOptions options);
    /// <summary>Writes a list-management value.</summary>
    EmailValueFormatResult WriteListManagementWithValue(EmailListManagementValue value);
    /// <summary>Writes a list-management value with options.</summary>
    EmailValueFormatResult WriteListManagementWithValueAndOptions(EmailListManagementValue value, EmailValueFormatOptions options);
    /// <summary>Writes List-Unsubscribe-Post.</summary>
    EmailValueFormatResult WriteListUnsubscribePostWithValue(EmailListUnsubscribePost value);
    /// <summary>Writes List-Unsubscribe-Post with options.</summary>
    EmailValueFormatResult WriteListUnsubscribePostWithValueAndOptions(EmailListUnsubscribePost value, EmailValueFormatOptions options);
    /// <summary>Writes a resent field.</summary>
    EmailValueFormatResult WriteResentFieldWithValue(EmailResentField value);
    /// <summary>Writes a resent field with options.</summary>
    EmailValueFormatResult WriteResentFieldWithValueAndOptions(EmailResentField value, EmailValueFormatOptions options);
}
