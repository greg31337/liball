using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents an Internet message with headers, an optional MIME body, envelope metadata, source information, and diagnostics.
/// </summary>
public class EmailMessage : IEmailMessageContract
{
    EmailHeaders? headers;
    MimeEntity? body;
    EmailEnvelope? envelope;
    EmailSourceInfo? sourceInfo;
    EmailIssueList? ownIssues;
    List<EmailMessageHeaderSnapshot>? headerSnapshots;
    EmailMessageEnvelopeSnapshot? envelopeSnapshot;
    MimeEntity? bodySnapshot;
    List<EmailRange>? explicitlyDirtyRanges;
    bool forcedExactDirty;
    bool forcedSemanticDirty;
    bool signatureMutationGuardEnabled;
    /// <summary>
    /// Creates a message with the supplied headers.
    /// </summary>
    public EmailMessage()
    {
        Initialize(new EmailHeadersOperations().CreateWith(), null, new EmailEnvelopeOperations().CreateWith(), null, new EmailIssueList());
    }

    internal static EmailMessage CreateImplementationWith()
    {
        return new EmailMessage();
    }

    internal static EmailMessage CreateImplementationWithHeaders(EmailHeaders headers)
    {
        EmailMessage value = new EmailMessage();
        value.Initialize(headers, null, new EmailEnvelopeOperations().CreateWith(), null, new EmailIssueList());
        return value;
    }

    internal static EmailMessage CreateImplementationWithHeadersAndEnvelopeAndSourceInfoAndIssues(EmailHeaders headers, EmailEnvelope envelope, EmailSourceInfo sourceInfo, EmailIssueList issues)
    {
        EmailMessage value = new EmailMessage();
        value.Initialize(headers, null, envelope, sourceInfo, issues);
        return value;
    }

    internal static EmailMessage CreateImplementationWithHeadersAndBodyAndEnvelopeAndSourceInfoAndIssues(EmailHeaders? headers, MimeEntity? body, EmailEnvelope envelope, EmailSourceInfo? sourceInfo, EmailIssueList? issues)
    {
        EmailMessage value = new EmailMessage();
        value.Initialize(headers, body, envelope, sourceInfo, issues);
        return value;
    }

    void Initialize(EmailHeaders? suppliedHeaders, MimeEntity? suppliedBody, EmailEnvelope suppliedEnvelope, EmailSourceInfo? suppliedSourceInfo, EmailIssueList? suppliedIssues)
    {
        if (suppliedHeaders == null)
        {
            throw new ArgumentNullException("suppliedHeaders");
        }

        if (suppliedEnvelope == null)
        {
            throw new ArgumentNullException("suppliedEnvelope");
        }

        if (suppliedIssues == null)
        {
            throw new ArgumentNullException("suppliedIssues");
        }

        ValidateBody(suppliedBody);
        if (suppliedSourceInfo != null && suppliedEnvelope.HasMboxFromLineRange() && !(suppliedSourceInfo.GetMessageRange() ?? throw new InvalidOperationException()).ContainsRange(suppliedEnvelope.GetMboxFromLineRange()))
        {
            throw new ArgumentException("The mbox envelope range must be contained by the message source range.", "suppliedEnvelope");
        }

        headers = suppliedHeaders;
        body = suppliedBody;
        envelope = suppliedEnvelope;
        sourceInfo = suppliedSourceInfo;
        ownIssues = new EmailIssueList();
        ownIssues.AddRange(suppliedIssues);
        explicitlyDirtyRanges = new List<EmailRange>();
        forcedExactDirty = false;
        forcedSemanticDirty = false;
        signatureMutationGuardEnabled = false;
        CaptureSnapshots();
    }

    /// <summary>
    /// Gets the headers.
    /// </summary>
    /// <returns>The current headers.</returns>
    public EmailHeaders? GetHeaders()
    {
        return headers;
    }

    /// <summary>
    /// Determines whether the body is present or available.
    /// </summary>
    /// <returns><c>true</c> when the body is available; otherwise, <c>false</c>.</returns>
    public bool HasBody()
    {
        return body != null;
    }

    /// <summary>
    /// Gets the body.
    /// </summary>
    /// <returns>The current body.</returns>
    public MimeEntity? GetBody()
    {
        return body;
    }

    /// <summary>
    /// Sets the body used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetBody(MimeEntity value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        ValidateBody(value);
        EnsureSignatureMutationAllowed();
        body = value;
    }

    /// <summary>
    /// Gets the envelope.
    /// </summary>
    /// <returns>The current envelope.</returns>
    public EmailEnvelope? GetEnvelope()
    {
        return envelope;
    }

    /// <summary>
    /// Determines whether the source info is present or available.
    /// </summary>
    /// <returns><c>true</c> when the source info is available; otherwise, <c>false</c>.</returns>
    public bool HasSourceInfo()
    {
        return sourceInfo != null;
    }

    /// <summary>
    /// Gets the source info.
    /// </summary>
    /// <returns>The current source info.</returns>
    public EmailSourceInfo? GetSourceInfo()
    {
        return sourceInfo;
    }

    /// <summary>
    /// Gets the own issues.
    /// </summary>
    /// <returns>The current own issues.</returns>
    public EmailIssueList GetOwnIssues()
    {
        EmailIssueList copy = new EmailIssueList();
        copy.AddRange(ownIssues);
        return copy;
    }

    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    public EmailIssueList GetIssues()
    {
        EmailIssueList result = GetOwnIssues();
        int index = 0;
        while (index < (headers ?? throw new InvalidOperationException()).GetCount())
        {
            result.AddRange(headers.GetAt(index).GetIssues());
            index++;
        }

        if (body != null)
        {
            result.AddRange(body.GetIssues());
        }

        return result;
    }

    /// <summary>
    /// Adds an issue to the current collection or model.
    /// </summary>
    /// <param name = "issue">The issue used by the operation.</param>
    public void AddIssue(EmailIssue issue)
    {
        (ownIssues ?? throw new InvalidOperationException()).Add(issue);
    }

    /// <summary>
    /// Clears the stored own issues value or values.
    /// </summary>
    public void ClearOwnIssues()
    {
        (ownIssues ?? throw new InvalidOperationException()).Clear();
    }

    /// <summary>
    /// Gets the dirty state.
    /// </summary>
    /// <returns>The current dirty state.</returns>
    public EmailDirtyState GetDirtyState()
    {
        bool exact = IsExactDirty();
        bool semantic = IsSemanticDirty();
        if (exact && semantic)
        {
            return EmailDirtyState.ExactAndSemantic;
        }

        if (exact)
        {
            return EmailDirtyState.ExactOnly;
        }

        return semantic ? EmailDirtyState.SemanticOnly : EmailDirtyState.Clean;
    }

    /// <summary>
    /// Determines whether the current value or state is exact dirty.
    /// </summary>
    /// <returns><c>true</c> when the value or state is exact dirty; otherwise, <c>false</c>.</returns>
    public bool IsExactDirty()
    {
        if (sourceInfo == null || forcedExactDirty || (explicitlyDirtyRanges ?? throw new InvalidOperationException()).Count > 0)
        {
            return true;
        }

        if (HasHeaderStructureChange() || HasEnvelopeExactChange() || HasBodyExactChange())
        {
            return true;
        }

        int index = 0;
        while (index < (headerSnapshots ?? throw new InvalidOperationException()).Count)
        {
            if (!Equal(headerSnapshots[index].GetRawFieldBytes(), (headers ?? throw new InvalidOperationException()).GetAt(index).GetRawFieldBytes()))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    /// <summary>
    /// Determines whether the current value or state is semantic dirty.
    /// </summary>
    /// <returns><c>true</c> when the value or state is semantic dirty; otherwise, <c>false</c>.</returns>
    public bool IsSemanticDirty()
    {
        if (forcedSemanticDirty || HasHeaderStructureChange() || HasEnvelopeSemanticChange() || HasBodySemanticChange())
        {
            return true;
        }

        int index = 0;
        while (index < (headerSnapshots ?? throw new InvalidOperationException()).Count)
        {
            EmailMessageHeaderSnapshot snapshot = headerSnapshots[index];
            EmailHeader current = (headers ?? throw new InvalidOperationException()).GetAt(index);
            if (!AsciiNameEquals(snapshot.GetNameBytes(), current.GetNameBytes()) || !Equal(snapshot.GetUnfoldedValue(), current.GetUnfoldedValue()) || snapshot.GetValueKind() != current.GetValueKind() || snapshot.GetParseMode() != current.GetParseMode())
            {
                return true;
            }

            index++;
        }

        return false;
    }

    /// <summary>
    /// Determines whether the current value or state is signature mutation guard enabled.
    /// </summary>
    /// <returns><c>true</c> when the value or state is signature mutation guard enabled; otherwise, <c>false</c>.</returns>
    public bool IsSignatureMutationGuardEnabled()
    {
        return signatureMutationGuardEnabled;
    }

    /// <summary>
    /// Enables the signature mutation guard.
    /// </summary>
    public void EnableSignatureMutationGuard()
    {
        bool headerSignature = HasHeaderSignature();
        if (headerSignature)
        {
            signatureMutationGuardEnabled = true;
            (headers ?? throw new InvalidOperationException()).SetMutationGuard(EnsureSignatureMutationAllowed);
            if (body != null)
                body.EnableAllSignatureMutationGuard();
        }
        else if (body != null)
        {
            body.EnableSignatureMutationGuard();
        }
    }

    /// <summary>
    /// Disables the signature mutation guard.
    /// </summary>
    public void DisableSignatureMutationGuard()
    {
        signatureMutationGuardEnabled = false;
        (headers ?? throw new InvalidOperationException()).SetMutationGuard(null);
        if (body != null)
            body.DisableSignatureMutationGuard();
    }

    internal void EnableAllSignatureMutationGuard()
    {
        signatureMutationGuardEnabled = true;
        (headers ?? throw new InvalidOperationException()).SetMutationGuard(EnsureSignatureMutationAllowed);
        if (body != null)
            body.EnableAllSignatureMutationGuard();
    }

    /// <summary>
    /// Marks the exact dirty.
    /// </summary>
    public void MarkExactDirty()
    {
        EnsureSignatureMutationAllowed();
        forcedExactDirty = true;
    }

    /// <summary>
    /// Marks the semantic dirty.
    /// </summary>
    public void MarkSemanticDirty()
    {
        EnsureSignatureMutationAllowed();
        forcedSemanticDirty = true;
    }

    /// <summary>
    /// Marks the dirty.
    /// </summary>
    public void MarkDirty()
    {
        EnsureSignatureMutationAllowed();
        forcedExactDirty = true;
        forcedSemanticDirty = true;
    }

    /// <summary>
    /// Marks the range dirty.
    /// </summary>
    /// <param name = "range">The range used by the operation.</param>
    /// <param name = "semanticChange">Whether to enable the semantic change behavior.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    public void MarkRangeDirty(EmailRange range, bool semanticChange)
    {
        if (range == null)
        {
            throw new ArgumentNullException("range");
        }

        if (sourceInfo == null)
        {
            throw new InvalidOperationException("A source range cannot be marked dirty on a message without source information.");
        }

        if (!(sourceInfo.GetMessageRange() ?? throw new InvalidOperationException()).ContainsRange(range))
        {
            throw new ArgumentOutOfRangeException("range");
        }

        EnsureSignatureMutationAllowed();
        (explicitlyDirtyRanges ?? throw new InvalidOperationException()).Add(range);
        if (semanticChange)
        {
            forcedSemanticDirty = true;
        }
    }

    /// <summary>
    /// Gets the clean ranges.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current clean ranges.</returns>
    public List<EmailRange> GetCleanRanges()
    {
        List<EmailRange> clean = new List<EmailRange>();
        if (sourceInfo == null || forcedExactDirty)
        {
            return clean;
        }

        clean.Add((sourceInfo.GetMessageRange() ?? throw new InvalidOperationException()));
        if (HasHeaderStructureChange())
        {
            clean = Subtract(clean, GetHeaderBlockOrMessageRange());
        }
        else
        {
            int index = 0;
            while (index < (headerSnapshots ?? throw new InvalidOperationException()).Count)
            {
                EmailMessageHeaderSnapshot snapshot = headerSnapshots[index];
                if (!Equal(snapshot.GetRawFieldBytes(), (headers ?? throw new InvalidOperationException()).GetAt(index).GetRawFieldBytes()))
                {
                    EmailRange? range = snapshot.GetSourceRange();
                    clean = Subtract(clean, range == null ? GetHeaderBlockOrMessageRange() : range);
                }

                index++;
            }
        }

        if (HasEnvelopeExactChange())
        {
            EmailRange? envelopeRange = (envelopeSnapshot ?? throw new InvalidOperationException()).GetMboxFromLineRange();
            if (envelopeRange == null)
            {
                envelopeRange = (envelope ?? throw new InvalidOperationException()).GetMboxFromLineRange();
            }

            if (envelopeRange != null)
            {
                clean = Subtract(clean, envelopeRange);
            }
        }

        if (HasBodyExactChange())
        {
            if (HasBodyChange() || body == null)
            {
                EmailRange? bodyRange = sourceInfo.HasBodyRange() ? sourceInfo.GetBodyRange() : sourceInfo.GetMessageRange();
                clean = Subtract(clean, bodyRange);
            }
            else
            {
                List<EmailRange> dirtyBodyRanges = body.GetDirtySourceRanges();
                if (dirtyBodyRanges.Count == 0)
                {
                    EmailRange? bodyRange = sourceInfo.HasBodyRange() ? sourceInfo.GetBodyRange() : sourceInfo.GetMessageRange();
                    clean = Subtract(clean, bodyRange);
                }
                else
                {
                    int bodyRangeIndex = 0;
                    while (bodyRangeIndex < dirtyBodyRanges.Count)
                    {
                        clean = Subtract(clean, dirtyBodyRanges[bodyRangeIndex]);
                        bodyRangeIndex++;
                    }
                }
            }
        }

        int dirtyIndex = 0;
        while (dirtyIndex < (explicitlyDirtyRanges ?? throw new InvalidOperationException()).Count)
        {
            clean = Subtract(clean, explicitlyDirtyRanges[dirtyIndex]);
            dirtyIndex++;
        }

        return clean;
    }

    /// <summary>
    /// Determines whether the current value or state is range clean.
    /// </summary>
    /// <param name = "range">The range used by the operation.</param>
    /// <returns><c>true</c> when the value or state is range clean; otherwise, <c>false</c>.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public bool IsRangeClean(EmailRange? range)
    {
        if (range == null)
        {
            throw new ArgumentNullException("range");
        }

        List<EmailRange> clean = GetCleanRanges();
        int index = 0;
        while (index < clean.Count)
        {
            if (clean[index].ContainsRange(range))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    void CaptureSnapshots()
    {
        headerSnapshots = new List<EmailMessageHeaderSnapshot>();
        int index = 0;
        while (index < (headers ?? throw new InvalidOperationException()).GetCount())
        {
            headerSnapshots.Add(new EmailMessageHeaderSnapshot(headers.GetAt(index)));
            index++;
        }

        envelopeSnapshot = new EmailMessageEnvelopeSnapshot(envelope);
        bodySnapshot = body;
    }

    bool HasHeaderStructureChange()
    {
        if ((headers ?? throw new InvalidOperationException()).GetCount() != (headerSnapshots ?? throw new InvalidOperationException()).Count)
        {
            return true;
        }

        int index = 0;
        while (index < headerSnapshots.Count)
        {
            if (!object.ReferenceEquals(headers.GetAt(index), headerSnapshots[index].GetHeader()))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    bool HasEnvelopeExactChange()
    {
        return !EqualNullable((envelopeSnapshot ?? throw new InvalidOperationException()).GetMboxFromLine(), (envelope ?? throw new InvalidOperationException()).GetMboxFromLine());
    }

    bool HasBodyChange()
    {
        return !object.ReferenceEquals(body, bodySnapshot);
    }

    bool HasBodyExactChange()
    {
        return HasBodyChange() || body != null && body.IsExactDirty();
    }

    bool HasBodySemanticChange()
    {
        return HasBodyChange() || body != null && body.IsSemanticDirty();
    }

    bool HasEnvelopeSemanticChange()
    {
        if (!AddressEquals((envelopeSnapshot ?? throw new InvalidOperationException()).GetSender(), (envelope ?? throw new InvalidOperationException()).GetSender()))
        {
            return true;
        }

        List<EmailAddress> originalRecipients = envelopeSnapshot.GetRecipients();
        if (originalRecipients.Count != envelope.GetRecipientCount())
        {
            return true;
        }

        int index = 0;
        while (index < originalRecipients.Count)
        {
            if (!AddressEquals(originalRecipients[index], envelope.GetRecipientAt(index)))
            {
                return true;
            }

            index++;
        }

        return HasEnvelopeExactChange();
    }

    EmailRange? GetHeaderBlockOrMessageRange()
    {
        return (sourceInfo ?? throw new InvalidOperationException()).HasHeaderBlockRange() ? sourceInfo.GetHeaderBlockRange() : sourceInfo.GetMessageRange();
    }

    void ValidateBody(MimeEntity? value)
    {
        if (value == null)
        {
            return;
        }

        if (value.HasParent())
        {
            throw new ArgumentException("A message body must be the root of its MIME tree.", "value");
        }

        if (EntityContainsMessage(value, this))
        {
            throw new ArgumentException("The MIME body would create a nested-message cycle.", "value");
        }
    }

    static bool EntityContainsMessage(MimeEntity? entity, EmailMessage message)
    {
        if ((entity ?? throw new InvalidOperationException()).HasNestedMessage())
        {
            EmailMessage? nested = entity.GetNestedMessage();
            if (object.ReferenceEquals(nested, message))
            {
                return true;
            }

            if ((nested ?? throw new InvalidOperationException()).HasBody() && EntityContainsMessage(nested.GetBody(), message))
            {
                return true;
            }
        }

        int index = 0;
        while (index < entity.GetChildCount())
        {
            if (EntityContainsMessage(entity.GetChildAt(index), message))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailMessage Clone()
    {
        MimeEntity? bodyCopy = body == null ? null : body.Clone();
        EmailMessage clone = CreateImplementationWithHeadersAndBodyAndEnvelopeAndSourceInfoAndIssues((headers ?? throw new InvalidOperationException()).Clone(), bodyCopy, (envelope ?? throw new InvalidOperationException()).Clone(), sourceInfo, ownIssues);
        if (IsExactDirty())
        {
            clone.forcedExactDirty = true;
        }

        if (IsSemanticDirty())
        {
            clone.forcedSemanticDirty = true;
        }

        return clone;
    }

    /// <summary>
    /// Detaches the source.
    /// </summary>
    /// <param name = "maximumContentBytes">The source bytes or data to process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void DetachSource(long maximumContentBytes)
    {
        if (maximumContentBytes < 0)
        {
            throw new ArgumentOutOfRangeException("maximumContentBytes");
        }

        int index = 0;
        while (index < (headers ?? throw new InvalidOperationException()).GetCount())
        {
            headers.GetAt(index).DetachSource();
            index++;
        }

        (envelope ?? throw new InvalidOperationException()).DetachSource();
        if (body != null)
        {
            body.DetachSource(maximumContentBytes);
        }

        sourceInfo = null;
        (explicitlyDirtyRanges ?? throw new InvalidOperationException()).Clear();
        forcedExactDirty = false;
        forcedSemanticDirty = false;
        CaptureSnapshots();
    }

    bool HasHeaderSignature()
    {
        return (headers ?? throw new InvalidOperationException()).Contains("DKIM-Signature") || headers.Contains("ARC-Message-Signature") || headers.Contains("ARC-Seal");
    }

    void EnsureSignatureMutationAllowed()
    {
        if (signatureMutationGuardEnabled)
            throw new EmailSignatureMutationException();
    }

    static List<EmailRange> Subtract(List<EmailRange> source, EmailRange? removed)
    {
        List<EmailRange> result = new List<EmailRange>();
        int index = 0;
        while (index < source.Count)
        {
            EmailRange current = source[index];
            if (!current.Intersects(removed))
            {
                result.Add(current);
            }
            else
            {
                long overlapStart = current.GetStartOffset() > (removed ?? throw new InvalidOperationException()).GetStartOffset() ? current.GetStartOffset() : removed.GetStartOffset();
                long overlapEnd = current.GetEndOffsetExclusive() < removed.GetEndOffsetExclusive() ? current.GetEndOffsetExclusive() : removed.GetEndOffsetExclusive();
                if (overlapStart > current.GetStartOffset())
                {
                    result.Add(new EmailRange(current.GetStartOffset(), overlapStart - current.GetStartOffset()));
                }

                if (overlapEnd < current.GetEndOffsetExclusive())
                {
                    result.Add(new EmailRange(overlapEnd, current.GetEndOffsetExclusive() - overlapEnd));
                }
            }

            index++;
        }

        return result;
    }

    static bool AddressEquals(EmailAddress? first, EmailAddress? second)
    {
        if (first == null || second == null)
        {
            return first == null && second == null;
        }

        return string.Equals(first.GetLocalPart(), second.GetLocalPart(), StringComparison.Ordinal) && string.Equals(first.GetDomain(), second.GetDomain(), StringComparison.OrdinalIgnoreCase) && first.IsDomainLiteral() == second.IsDomainLiteral();
    }

    static bool AsciiNameEquals(byte[] first, byte[] second)
    {
        if (first.Length != second.Length)
        {
            return false;
        }

        int index = 0;
        while (index < first.Length)
        {
            int left = first[index];
            int right = second[index];
            if (left >= 65 && left <= 90)
            {
                left += 32;
            }

            if (right >= 65 && right <= 90)
            {
                right += 32;
            }

            if (left != right)
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static bool EqualNullable(byte[]? first, byte[]? second)
    {
        if (first == null || second == null)
        {
            return first == null && second == null;
        }

        return Equal(first, second);
    }

    static bool Equal(byte[] first, byte[] second)
    {
        if (first.Length != second.Length)
        {
            return false;
        }

        int index = 0;
        while (index < first.Length)
        {
            if (first[index] != second[index])
            {
                return false;
            }

            index++;
        }

        return true;
    }
}
