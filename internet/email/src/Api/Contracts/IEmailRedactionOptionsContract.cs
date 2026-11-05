using System;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailRedactionOptions"/>.
/// </summary>
public interface IEmailRedactionOptionsContract
{
    /// <summary>
    /// Gets the redact addresses.
    /// </summary>
    /// <returns><c>true</c> when the redact addresses setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetRedactAddresses();
    /// <summary>
    /// Sets the redact addresses used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the redact addresses behavior.</param>
    void SetRedactAddresses(bool value);
    /// <summary>
    /// Gets the redact message ids.
    /// </summary>
    /// <returns><c>true</c> when the redact message ids setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetRedactMessageIds();
    /// <summary>
    /// Sets the redact message ids used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the redact message ids behavior.</param>
    void SetRedactMessageIds(bool value);
    /// <summary>
    /// Gets the remove received headers.
    /// </summary>
    /// <returns><c>true</c> when the remove received headers setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetRemoveReceivedHeaders();
    /// <summary>
    /// Sets the remove received headers used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the remove received headers behavior.</param>
    void SetRemoveReceivedHeaders(bool value);
    /// <summary>
    /// Gets the redact ip addresses.
    /// </summary>
    /// <returns><c>true</c> when the redact ip addresses setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetRedactIpAddresses();
    /// <summary>
    /// Sets the redact ip addresses used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the redact ip addresses behavior.</param>
    void SetRedactIpAddresses(bool value);
    /// <summary>
    /// Gets the remove authentication headers.
    /// </summary>
    /// <returns><c>true</c> when the remove authentication headers setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetRemoveAuthenticationHeaders();
    /// <summary>
    /// Sets the remove authentication headers used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the remove authentication headers behavior.</param>
    void SetRemoveAuthenticationHeaders(bool value);
    /// <summary>
    /// Gets the redact subjects.
    /// </summary>
    /// <returns><c>true</c> when the redact subjects setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetRedactSubjects();
    /// <summary>
    /// Sets the redact subjects used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the redact subjects behavior.</param>
    void SetRedactSubjects(bool value);
    /// <summary>
    /// Gets the redact bodies.
    /// </summary>
    /// <returns><c>true</c> when the redact bodies setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetRedactBodies();
    /// <summary>
    /// Sets the redact bodies used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the redact bodies behavior.</param>
    void SetRedactBodies(bool value);
    /// <summary>
    /// Gets the redact attachment names.
    /// </summary>
    /// <returns><c>true</c> when the redact attachment names setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetRedactAttachmentNames();
    /// <summary>
    /// Sets the redact attachment names used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the redact attachment names behavior.</param>
    void SetRedactAttachmentNames(bool value);
    /// <summary>
    /// Gets the redact attachment content.
    /// </summary>
    /// <returns><c>true</c> when the redact attachment content setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetRedactAttachmentContent();
    /// <summary>
    /// Sets the redact attachment content used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the redact attachment content behavior.</param>
    void SetRedactAttachmentContent(bool value);
    /// <summary>
    /// Gets the pseudonym domain.
    /// </summary>
    /// <returns>The current pseudonym domain.</returns>
    string GetPseudonymDomain();
    /// <summary>
    /// Sets the pseudonym domain used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetPseudonymDomain(string value);
    /// <summary>
    /// Gets the subject replacement.
    /// </summary>
    /// <returns>The current subject replacement.</returns>
    string GetSubjectReplacement();
    /// <summary>
    /// Sets the subject replacement used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetSubjectReplacement(string value);
    /// <summary>
    /// Gets the body replacement.
    /// </summary>
    /// <returns>The current body replacement.</returns>
    string GetBodyReplacement();
    /// <summary>
    /// Sets the body replacement used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetBodyReplacement(string value);
    /// <summary>
    /// Gets the attachment name prefix.
    /// </summary>
    /// <returns>The current attachment name prefix.</returns>
    string GetAttachmentNamePrefix();
    /// <summary>
    /// Sets the attachment name prefix used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetAttachmentNamePrefix(string value);
    /// <summary>
    /// Gets the attachment content replacement.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current attachment content replacement.</returns>
    byte[] GetAttachmentContentReplacement();
    /// <summary>
    /// Sets the attachment content replacement used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetAttachmentContentReplacement(byte[] value);
    /// <summary>
    /// Gets the maximum depth.
    /// </summary>
    /// <returns>The current maximum depth.</returns>
    int GetMaximumDepth();
    /// <summary>
    /// Sets the maximum depth used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMaximumDepth(int value);
    /// <summary>
    /// Gets the maximum entity count.
    /// </summary>
    /// <returns>The current maximum entity count.</returns>
    int GetMaximumEntityCount();
    /// <summary>
    /// Sets the maximum entity count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMaximumEntityCount(int value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailRedactionOptions Clone();
}
