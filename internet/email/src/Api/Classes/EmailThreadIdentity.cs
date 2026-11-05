using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a thread identity.
/// </summary>
public class EmailThreadIdentity : IEmailThreadIdentityContract
{
    List<EmailMessageId> messageIdentifiers;
    List<EmailMessageId> references;
    List<EmailMessageId> inReplyTo;
    EmailMessageId? parentCandidate;
    string? normalizedSubject;
    string? threadKey;
    EmailThreadIdentitySource source;
    EmailIssueList issues;
    /// <summary>
    /// Creates a new thread identity.
    /// </summary>
    /// <param name = "messageIdentifiers">The message identifiers used by the operation.</param>
    /// <param name = "references">The references used by the operation.</param>
    /// <param name = "inReplyTo">The in reply to used by the operation.</param>
    /// <param name = "parentCandidate">The parent candidate used by the operation.</param>
    /// <param name = "normalizedSubject">The normalized subject used by the operation.</param>
    /// <param name = "threadKey">The thread key used by the operation.</param>
    /// <param name = "source">The source thread identity source to read or copy from.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailThreadIdentity(List<EmailMessageId> messageIdentifiers, List<EmailMessageId> references, List<EmailMessageId> inReplyTo, EmailMessageId? parentCandidate, string? normalizedSubject, string? threadKey, EmailThreadIdentitySource source, EmailIssueList issues)
    {
        if (messageIdentifiers == null)
        {
            throw new ArgumentNullException("messageIdentifiers");
        }

        if (references == null)
        {
            throw new ArgumentNullException("references");
        }

        if (inReplyTo == null)
        {
            throw new ArgumentNullException("inReplyTo");
        }

        if (source < EmailThreadIdentitySource.None || source > EmailThreadIdentitySource.NormalizedSubject)
        {
            throw new ArgumentOutOfRangeException("source");
        }

        if (issues == null)
        {
            throw new ArgumentNullException("issues");
        }

        if (normalizedSubject != null && normalizedSubject.Length == 0)
        {
            throw new ArgumentException("A normalized subject cannot be empty when supplied.", "normalizedSubject");
        }

        if (threadKey != null && threadKey.Length == 0)
        {
            throw new ArgumentException("A thread key cannot be empty when supplied.", "threadKey");
        }

        this.messageIdentifiers = CopyIdentifiers(messageIdentifiers, "messageIdentifiers");
        this.references = CopyIdentifiers(references, "references");
        this.inReplyTo = CopyIdentifiers(inReplyTo, "inReplyTo");
        this.parentCandidate = parentCandidate;
        this.normalizedSubject = normalizedSubject;
        this.threadKey = threadKey;
        this.source = source;
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
    }

    /// <summary>
    /// Gets the message identifier count.
    /// </summary>
    /// <returns>The current message identifier count.</returns>
    public int GetMessageIdentifierCount()
    {
        return messageIdentifiers.Count;
    }

    /// <summary>
    /// Gets the message identifier at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested message identifier.</returns>
    public EmailMessageId GetMessageIdentifierAt(int index)
    {
        return GetAt(messageIdentifiers, index);
    }

    /// <summary>
    /// Gets the message identifiers.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current message identifiers.</returns>
    public List<EmailMessageId> GetMessageIdentifiers()
    {
        return new List<EmailMessageId>(messageIdentifiers);
    }

    /// <summary>
    /// Determines whether the message identifier is present or available.
    /// </summary>
    /// <returns><c>true</c> when the message identifier is available; otherwise, <c>false</c>.</returns>
    public bool HasMessageIdentifier()
    {
        return messageIdentifiers.Count > 0;
    }

    /// <summary>
    /// Gets the message identifier.
    /// </summary>
    /// <returns>The current message identifier.</returns>
    public EmailMessageId? GetMessageIdentifier()
    {
        return HasMessageIdentifier() ? messageIdentifiers[0] : null;
    }

    /// <summary>
    /// Gets the canonical message identifier.
    /// </summary>
    /// <returns>The current canonical message identifier.</returns>
    public string? GetCanonicalMessageIdentifier()
    {
        return HasMessageIdentifier() ? new EmailThreadAnalyzer().CanonicalizeIdentifier(messageIdentifiers[0]) : null;
    }

    /// <summary>
    /// Gets the reference count.
    /// </summary>
    /// <returns>The current reference count.</returns>
    public int GetReferenceCount()
    {
        return references.Count;
    }

    /// <summary>
    /// Gets the reference at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested reference.</returns>
    public EmailMessageId GetReferenceAt(int index)
    {
        return GetAt(references, index);
    }

    /// <summary>
    /// Gets the references.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current references.</returns>
    public List<EmailMessageId> GetReferences()
    {
        return new List<EmailMessageId>(references);
    }

    /// <summary>
    /// Gets the in reply to count.
    /// </summary>
    /// <returns>The current in reply to count.</returns>
    public int GetInReplyToCount()
    {
        return inReplyTo.Count;
    }

    /// <summary>
    /// Gets the in reply to at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested in reply to.</returns>
    public EmailMessageId GetInReplyToAt(int index)
    {
        return GetAt(inReplyTo, index);
    }

    /// <summary>
    /// Gets the in reply to.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current in reply to.</returns>
    public List<EmailMessageId> GetInReplyTo()
    {
        return new List<EmailMessageId>(inReplyTo);
    }

    /// <summary>
    /// Determines whether the parent candidate is present or available.
    /// </summary>
    /// <returns><c>true</c> when the parent candidate is available; otherwise, <c>false</c>.</returns>
    public bool HasParentCandidate()
    {
        return parentCandidate != null;
    }

    /// <summary>
    /// Gets the parent candidate.
    /// </summary>
    /// <returns>The current parent candidate.</returns>
    public EmailMessageId? GetParentCandidate()
    {
        return parentCandidate;
    }

    /// <summary>
    /// Determines whether the normalized subject is present or available.
    /// </summary>
    /// <returns><c>true</c> when the normalized subject is available; otherwise, <c>false</c>.</returns>
    public bool HasNormalizedSubject()
    {
        return normalizedSubject != null;
    }

    /// <summary>
    /// Gets the normalized subject.
    /// </summary>
    /// <returns>The current normalized subject.</returns>
    public string? GetNormalizedSubject()
    {
        return normalizedSubject;
    }

    /// <summary>
    /// Determines whether the thread key is present or available.
    /// </summary>
    /// <returns><c>true</c> when the thread key is available; otherwise, <c>false</c>.</returns>
    public bool HasThreadKey()
    {
        return threadKey != null;
    }

    /// <summary>
    /// Gets the thread key.
    /// </summary>
    /// <returns>The current thread key.</returns>
    public string? GetThreadKey()
    {
        return threadKey;
    }

    /// <summary>
    /// Gets the thread identity source.
    /// </summary>
    /// <returns>The current thread identity source.</returns>
    public EmailThreadIdentitySource GetThreadIdentitySource()
    {
        return source;
    }

    /// <summary>
    /// Determines whether the current value or state uses subject fallback.
    /// </summary>
    /// <returns><c>true</c> when the value or state uses subject fallback; otherwise, <c>false</c>.</returns>
    public bool UsesSubjectFallback()
    {
        return source == EmailThreadIdentitySource.NormalizedSubject;
    }

    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    public EmailIssueList GetIssues()
    {
        EmailIssueList copy = new EmailIssueList();
        copy.AddRange(issues);
        return copy;
    }

    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return !issues.HasSeverityAtLeast(EmailIssueSeverity.Error);
    }

    static EmailMessageId GetAt(List<EmailMessageId> values, int index)
    {
        if (index < 0 || index >= values.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return values[index];
    }

    static List<EmailMessageId> CopyIdentifiers(List<EmailMessageId> values, string parameterName)
    {
        List<EmailMessageId> copy = new List<EmailMessageId>();
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == null)
            {
                throw new ArgumentException("Identifier collections cannot contain null entries.", parameterName);
            }

            copy.Add(values[index]);
            index++;
        }

        return copy;
    }
}
