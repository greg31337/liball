using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailThreadIdentity"/>.
/// </summary>
public interface IEmailThreadIdentityContract
{
    /// <summary>
    /// Gets the message identifier count.
    /// </summary>
    /// <returns>The current message identifier count.</returns>
    int GetMessageIdentifierCount();
    /// <summary>
    /// Gets the message identifier at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested message identifier.</returns>
    EmailMessageId GetMessageIdentifierAt(int index);
    /// <summary>
    /// Gets the message identifiers.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current message identifiers.</returns>
    List<EmailMessageId> GetMessageIdentifiers();
    /// <summary>
    /// Determines whether the message identifier is present or available.
    /// </summary>
    /// <returns><c>true</c> when the message identifier is available; otherwise, <c>false</c>.</returns>
    bool HasMessageIdentifier();
    /// <summary>
    /// Gets the message identifier.
    /// </summary>
    /// <returns>The current message identifier.</returns>
    EmailMessageId? GetMessageIdentifier();
    /// <summary>
    /// Gets the canonical message identifier.
    /// </summary>
    /// <returns>The current canonical message identifier.</returns>
    string? GetCanonicalMessageIdentifier();
    /// <summary>
    /// Gets the reference count.
    /// </summary>
    /// <returns>The current reference count.</returns>
    int GetReferenceCount();
    /// <summary>
    /// Gets the reference at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested reference.</returns>
    EmailMessageId GetReferenceAt(int index);
    /// <summary>
    /// Gets the references.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current references.</returns>
    List<EmailMessageId> GetReferences();
    /// <summary>
    /// Gets the in reply to count.
    /// </summary>
    /// <returns>The current in reply to count.</returns>
    int GetInReplyToCount();
    /// <summary>
    /// Gets the in reply to at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested in reply to.</returns>
    EmailMessageId GetInReplyToAt(int index);
    /// <summary>
    /// Gets the in reply to.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current in reply to.</returns>
    List<EmailMessageId> GetInReplyTo();
    /// <summary>
    /// Determines whether the parent candidate is present or available.
    /// </summary>
    /// <returns><c>true</c> when the parent candidate is available; otherwise, <c>false</c>.</returns>
    bool HasParentCandidate();
    /// <summary>
    /// Gets the parent candidate.
    /// </summary>
    /// <returns>The current parent candidate.</returns>
    EmailMessageId? GetParentCandidate();
    /// <summary>
    /// Determines whether the normalized subject is present or available.
    /// </summary>
    /// <returns><c>true</c> when the normalized subject is available; otherwise, <c>false</c>.</returns>
    bool HasNormalizedSubject();
    /// <summary>
    /// Gets the normalized subject.
    /// </summary>
    /// <returns>The current normalized subject.</returns>
    string? GetNormalizedSubject();
    /// <summary>
    /// Determines whether the thread key is present or available.
    /// </summary>
    /// <returns><c>true</c> when the thread key is available; otherwise, <c>false</c>.</returns>
    bool HasThreadKey();
    /// <summary>
    /// Gets the thread key.
    /// </summary>
    /// <returns>The current thread key.</returns>
    string? GetThreadKey();
    /// <summary>
    /// Gets the thread identity source.
    /// </summary>
    /// <returns>The current thread identity source.</returns>
    EmailThreadIdentitySource GetThreadIdentitySource();
    /// <summary>
    /// Determines whether the current value or state uses subject fallback.
    /// </summary>
    /// <returns><c>true</c> when the value or state uses subject fallback; otherwise, <c>false</c>.</returns>
    bool UsesSubjectFallback();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    bool IsSuccess();
}
