using System;
using System.Collections.Generic;
using System.Text;

namespace email;
internal class EmailBodySelectorWorker
{
    EmailBodyPreference preference;
    bool sawEncrypted;

    internal EmailBodySelectorWorker(EmailBodyPreference preference)
    {
        this.preference = preference;
        sawEncrypted = false;
    }

    internal EmailBodySelection Choose(MimeEntity? root)
    {
        List<EmailBodyCandidate> candidates = new List<EmailBodyCandidate>();
        Collect(root, new EmailPathOperations().CreateRoot(), new List<EmailBodySelectionContext>(), candidates);
        if (candidates.Count == 0)
        {
            EmailBodySelectionReason unavailableReason = sawEncrypted ? EmailBodySelectionReason.EncryptedContentUnavailable : EmailBodySelectionReason.NoSupportedBody;
            return new EmailBodySelection(null, preference, EmailBodySelectionQuality.Unavailable, unavailableReason, null, new List<MimeEntity>(), new List<EmailBodySelectionContext>());
        }

        int selectedIndex = FindPreferred(candidates);
        EmailBodySelectionQuality quality = selectedIndex >= 0 ? EmailBodySelectionQuality.Preferred : EmailBodySelectionQuality.Fallback;
        EmailBodySelectionReason reason = selectedIndex >= 0 ? EmailBodySelectionReason.PreferredMediaType : EmailBodySelectionReason.FallbackMediaType;
        if (selectedIndex < 0)
        {
            selectedIndex = candidates.Count - 1;
        }

        EmailBodyCandidate selected = candidates[selectedIndex];
        List<MimeEntity> alternatives = new List<MimeEntity>();
        int index = 0;
        while (index < candidates.Count)
        {
            alternatives.Add(candidates[index].GetEntity());
            index++;
        }

        return new EmailBodySelection(selected.GetEntity(), preference, quality, reason, selected.GetPath(), alternatives, selected.GetContexts());
    }

    void Collect(MimeEntity? entity, EmailPath path, List<EmailBodySelectionContext> contexts, List<EmailBodyCandidate> candidates)
    {
        if ((entity ?? throw new InvalidOperationException()).HasDisposition() && (entity.GetDisposition() ?? throw new InvalidOperationException()).IsAttachment())
        {
            return;
        }

        if ((entity.GetMediaType() ?? throw new InvalidOperationException()).IsType("text", "plain") || (entity.GetMediaType() ?? throw new InvalidOperationException()).IsType("text", "html"))
        {
            candidates.Add(new EmailBodyCandidate(entity, path, contexts));
            return;
        }

        MimeEntityKind kind = entity.GetKind();
        if (kind == MimeEntityKind.Message)
        {
            if ((entity.GetNestedMessage() ?? throw new InvalidOperationException()).HasBody())
            {
                List<EmailBodySelectionContext> nestedContexts = AppendContext(contexts, EmailBodySelectionContext.NestedMessage);
                Collect((entity.GetNestedMessage() ?? throw new InvalidOperationException()).GetBody(), path.Append(0), nestedContexts, candidates);
            }

            return;
        }

        if (kind == MimeEntityKind.Encrypted)
        {
            sawEncrypted = true;
            return;
        }

        if (kind == MimeEntityKind.Signed)
        {
            List<EmailBodySelectionContext> signedContexts = AppendContext(contexts, EmailBodySelectionContext.SignedContent);
            Collect(entity.GetChildAt(0), path.Append(0), signedContexts, candidates);
            return;
        }

        if (kind != MimeEntityKind.Multipart)
        {
            return;
        }

        if ((entity.GetMediaType() ?? throw new InvalidOperationException()).IsType("multipart", "alternative"))
        {
            CollectAlternative(entity, path, AppendContext(contexts, EmailBodySelectionContext.Alternative), candidates);
            return;
        }

        if ((entity.GetMediaType() ?? throw new InvalidOperationException()).IsType("multipart", "related"))
        {
            CollectRelated(entity, path, AppendContext(contexts, EmailBodySelectionContext.RelatedRoot), candidates);
            return;
        }

        EmailBodySelectionContext context = (entity.GetMediaType() ?? throw new InvalidOperationException()).IsType("multipart", "mixed") ? EmailBodySelectionContext.Mixed : EmailBodySelectionContext.UnknownMultipart;
        CollectFirstBody(entity, path, AppendContext(contexts, context), candidates);
    }

    void CollectAlternative(MimeEntity entity, EmailPath path, List<EmailBodySelectionContext> contexts, List<EmailBodyCandidate> candidates)
    {
        int childIndex = 0;
        while (childIndex < entity.GetChildCount())
        {
            Collect(entity.GetChildAt(childIndex), path.Append(childIndex), contexts, candidates);
            childIndex++;
        }
    }

    void CollectRelated(MimeEntity entity, EmailPath path, List<EmailBodySelectionContext> contexts, List<EmailBodyCandidate> candidates)
    {
        if (entity.GetChildCount() == 0)
        {
            return;
        }

        int rootIndex = GetRelatedRootIndex(entity);
        Collect(entity.GetChildAt(rootIndex), path.Append(rootIndex), contexts, candidates);
    }

    void CollectFirstBody(MimeEntity entity, EmailPath path, List<EmailBodySelectionContext> contexts, List<EmailBodyCandidate> candidates)
    {
        int childIndex = 0;
        while (childIndex < entity.GetChildCount())
        {
            List<EmailBodyCandidate> childCandidates = new List<EmailBodyCandidate>();
            Collect(entity.GetChildAt(childIndex), path.Append(childIndex), contexts, childCandidates);
            if (childCandidates.Count > 0)
            {
                candidates.AddRange(childCandidates);
                return;
            }

            childIndex++;
        }
    }

    int FindPreferred(List<EmailBodyCandidate> candidates)
    {
        int selected = -1;
        int index = 0;
        while (index < candidates.Count)
        {
            MimeEntity entity = candidates[index].GetEntity();
            bool preferredType = preference == EmailBodyPreference.Html ? (entity.GetMediaType() ?? throw new InvalidOperationException()).IsType("text", "html") : (entity.GetMediaType() ?? throw new InvalidOperationException()).IsType("text", "plain");
            if (preferredType)
            {
                selected = index;
            }

            index++;
        }

        return selected;
    }

    static int GetRelatedRootIndex(MimeEntity entity)
    {
        EmailRfc2231DecodedParameter? start = (entity.GetMediaType() ?? throw new InvalidOperationException()).GetParameters().GetDecoded("start");
        if (start == null)
        {
            return 0;
        }

        string expected = Encoding.UTF8.GetString(start.GetDecodedBytes());
        int childIndex = 0;
        while (childIndex < entity.GetChildCount())
        {
            MimeEntity child = entity.GetChildAt(childIndex);
            if (child.HasContentId() && ContentIdMatches(child.GetContentId(), expected))
            {
                return childIndex;
            }

            childIndex++;
        }

        return 0;
    }

    static bool ContentIdMatches(EmailMessageId? identifier, string expected)
    {
        string value = expected.Trim();
        if (value.Length >= 2 && value[0] == '<' && value[value.Length - 1] == '>')
        {
            value = value.Substring(1, value.Length - 2);
        }

        string actualRight = (identifier ?? throw new InvalidOperationException()).IsRightDomainLiteral() ? "[" + identifier.GetIdRight() + "]" : identifier.GetIdRight();
        string actual = identifier.GetIdLeft() + "@" + actualRight;
        if (identifier.IsRightDomainLiteral())
        {
            return string.Equals(actual, value, StringComparison.Ordinal);
        }

        int separator = value.LastIndexOf('@');
        if (separator < 1 || separator + 1 >= value.Length)
        {
            return false;
        }

        return string.Equals(identifier.GetIdLeft(), value.Substring(0, separator), StringComparison.Ordinal)
            && string.Equals(identifier.GetIdRight(), value.Substring(separator + 1), StringComparison.OrdinalIgnoreCase);
    }

    static List<EmailBodySelectionContext> AppendContext(List<EmailBodySelectionContext> contexts, EmailBodySelectionContext context)
    {
        List<EmailBodySelectionContext> result = new List<EmailBodySelectionContext>(contexts);
        result.Add(context);
        return result;
    }
}
internal class EmailBodyCandidate
{
    MimeEntity entity;
    EmailPath path;
    List<EmailBodySelectionContext> contexts;

    internal EmailBodyCandidate(MimeEntity entity, EmailPath path, List<EmailBodySelectionContext> contexts)
    {
        this.entity = entity;
        this.path = path;
        this.contexts = new List<EmailBodySelectionContext>(contexts);
    }

    internal MimeEntity GetEntity()
    {
        return entity;
    }

    internal EmailPath GetPath()
    {
        return path;
    }

    internal List<EmailBodySelectionContext> GetContexts()
    {
        return new List<EmailBodySelectionContext>(contexts);
    }
}
