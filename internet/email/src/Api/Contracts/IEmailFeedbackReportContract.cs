using System;
using System.Collections.Generic;
using System.Net;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailFeedbackReport"/>.
/// </summary>
public interface IEmailFeedbackReportContract
{
    /// <summary>
    /// Gets the field block.
    /// </summary>
    /// <returns>The current field block.</returns>
    EmailReportFieldBlock? GetFieldBlock();
    /// <summary>
    /// Determines whether the feedback type is present or available.
    /// </summary>
    /// <returns><c>true</c> when the feedback type is available; otherwise, <c>false</c>.</returns>
    bool HasFeedbackType();
    /// <summary>
    /// Gets the feedback type name.
    /// </summary>
    /// <returns>The current feedback type name.</returns>
    string? GetFeedbackTypeName();
    /// <summary>
    /// Gets the feedback type.
    /// </summary>
    /// <returns>The current feedback type.</returns>
    EmailFeedbackType GetFeedbackType();
    /// <summary>
    /// Determines whether the user agent is present or available.
    /// </summary>
    /// <returns><c>true</c> when the user agent is available; otherwise, <c>false</c>.</returns>
    bool HasUserAgent();
    /// <summary>
    /// Gets the user agent.
    /// </summary>
    /// <returns>The current user agent.</returns>
    string? GetUserAgent();
    /// <summary>
    /// Determines whether the version is present or available.
    /// </summary>
    /// <returns><c>true</c> when the version is available; otherwise, <c>false</c>.</returns>
    bool HasVersion();
    /// <summary>
    /// Gets the version.
    /// </summary>
    /// <returns>The current version.</returns>
    uint GetVersion();
    /// <summary>
    /// Determines whether the original envelope ID is present or available.
    /// </summary>
    /// <returns><c>true</c> when the original envelope ID is available; otherwise, <c>false</c>.</returns>
    bool HasOriginalEnvelopeId();
    /// <summary>
    /// Gets the original envelope ID.
    /// </summary>
    /// <returns>The current original envelope ID.</returns>
    string? GetOriginalEnvelopeId();
    /// <summary>
    /// Determines whether the original mail from is present or available.
    /// </summary>
    /// <returns><c>true</c> when the original mail from is available; otherwise, <c>false</c>.</returns>
    bool HasOriginalMailFrom();
    /// <summary>
    /// Gets the original mail from.
    /// </summary>
    /// <returns>The current original mail from.</returns>
    EmailFeedbackPath? GetOriginalMailFrom();
    /// <summary>
    /// Determines whether the arrival date is present or available.
    /// </summary>
    /// <returns><c>true</c> when the arrival date is available; otherwise, <c>false</c>.</returns>
    bool HasArrivalDate();
    /// <summary>
    /// Gets the arrival date.
    /// </summary>
    /// <returns>The current arrival date.</returns>
    EmailDateTime? GetArrivalDate();
    /// <summary>
    /// Determines whether the current value or state uses historic received date.
    /// </summary>
    /// <returns><c>true</c> when the value or state uses historic received date; otherwise, <c>false</c>.</returns>
    bool UsesHistoricReceivedDate();
    /// <summary>
    /// Determines whether the reporting mta is present or available.
    /// </summary>
    /// <returns><c>true</c> when the reporting mta is available; otherwise, <c>false</c>.</returns>
    bool HasReportingMta();
    /// <summary>
    /// Gets the reporting mta.
    /// </summary>
    /// <returns>The current reporting mta.</returns>
    EmailFeedbackTypedValue? GetReportingMta();
    /// <summary>
    /// Determines whether the source ip is present or available.
    /// </summary>
    /// <returns><c>true</c> when the source ip is available; otherwise, <c>false</c>.</returns>
    bool HasSourceIp();
    /// <summary>
    /// Gets the source ip.
    /// </summary>
    /// <returns>The current source ip.</returns>
    IPAddress? GetSourceIp();
    /// <summary>
    /// Determines whether the source port is present or available.
    /// </summary>
    /// <returns><c>true</c> when the source port is available; otherwise, <c>false</c>.</returns>
    bool HasSourcePort();
    /// <summary>
    /// Gets the source port.
    /// </summary>
    /// <returns>The current source port.</returns>
    int GetSourcePort();
    /// <summary>
    /// Determines whether the incidents is present or available.
    /// </summary>
    /// <returns><c>true</c> when the incidents is available; otherwise, <c>false</c>.</returns>
    bool HasIncidents();
    /// <summary>
    /// Gets the incidents.
    /// </summary>
    /// <returns>The current incidents.</returns>
    uint GetIncidents();
    /// <summary>
    /// Gets the authentication results.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current authentication results.</returns>
    List<EmailAuthenticationResults> GetAuthenticationResults();
    /// <summary>
    /// Gets the authentication failure name.
    /// </summary>
    /// <returns>The current authentication failure name.</returns>
    string? GetAuthenticationFailureName();
    /// <summary>
    /// Gets the authentication failure.
    /// </summary>
    /// <returns>The current authentication failure.</returns>
    EmailFeedbackAuthenticationFailure GetAuthenticationFailure();
    /// <summary>
    /// Gets the delivery result name.
    /// </summary>
    /// <returns>The current delivery result name.</returns>
    string? GetDeliveryResultName();
    /// <summary>
    /// Gets the delivery result.
    /// </summary>
    /// <returns>The current delivery result.</returns>
    EmailFeedbackDeliveryResult GetDeliveryResult();
    /// <summary>
    /// Gets the DKIM domain.
    /// </summary>
    /// <returns>The current DKIM domain.</returns>
    string? GetDkimDomain();
    /// <summary>
    /// Gets the DKIM identity.
    /// </summary>
    /// <returns>The current DKIM identity.</returns>
    string? GetDkimIdentity();
    /// <summary>
    /// Gets the DKIM selector.
    /// </summary>
    /// <returns>The current DKIM selector.</returns>
    string? GetDkimSelector();
    /// <summary>
    /// Gets the spf dns values.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current spf dns values.</returns>
    List<string> GetSpfDnsValues();
    /// <summary>
    /// Gets the original recipients.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current original recipients.</returns>
    List<EmailFeedbackPath> GetOriginalRecipients();
    /// <summary>
    /// Gets the reported domains.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current reported domains.</returns>
    List<string> GetReportedDomains();
    /// <summary>
    /// Gets the reported uris.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current reported uris.</returns>
    List<Uri> GetReportedUris();
    /// <summary>
    /// Gets the extension fields.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current extension fields.</returns>
    List<EmailHeader> GetExtensionFields();
    /// <summary>
    /// Determines whether the multipart report is present or available.
    /// </summary>
    /// <returns><c>true</c> when the multipart report is available; otherwise, <c>false</c>.</returns>
    bool HasMultipartReport();
    /// <summary>
    /// Gets the multipart report.
    /// </summary>
    /// <returns>The current multipart report.</returns>
    EmailMultipartReport? GetMultipartReport();
    /// <summary>
    /// Determines whether the original content part is present or available.
    /// </summary>
    /// <returns><c>true</c> when the original content part is available; otherwise, <c>false</c>.</returns>
    bool HasOriginalContentPart();
    /// <summary>
    /// Gets the original content part.
    /// </summary>
    /// <returns>The current original content part.</returns>
    MimeEntity? GetOriginalContentPart();
    /// <summary>
    /// Determines whether the original headers is present or available.
    /// </summary>
    /// <returns><c>true</c> when the original headers is available; otherwise, <c>false</c>.</returns>
    bool HasOriginalHeaders();
    /// <summary>
    /// Gets the original headers.
    /// </summary>
    /// <returns>The current original headers.</returns>
    EmailHeaders? GetOriginalHeaders();
    /// <summary>
    /// Determines whether the original message is present or available.
    /// </summary>
    /// <returns><c>true</c> when the original message is available; otherwise, <c>false</c>.</returns>
    bool HasOriginalMessage();
    /// <summary>
    /// Gets the original message.
    /// </summary>
    /// <returns>The current original message.</returns>
    EmailMessage? GetOriginalMessage();
}
