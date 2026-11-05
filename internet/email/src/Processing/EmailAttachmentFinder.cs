using System;
using System.Collections.Generic;
using System.Text;

namespace email;
internal enum EmailAttachmentFilterKind
{
    Downloadable = 0,
    Body = 1,
    InlineResource = 2,
    NestedMessage = 3,
    Cryptographic = 4
}
internal class EmailAttachmentFinderWorker
{
    internal List<EmailAttachmentView> Enumerate(MimeEntity? root)
    {
        List<EmailAttachmentView> views = new List<EmailAttachmentView>();
        List<MimeEntity> bodyEntities = GetBodyEntities(root);
        Collect(root, new EmailPathOperations().CreateRoot(), bodyEntities, false, EmailAttachmentKind.Attachment, false, views);
        return views;
    }

    void Collect(MimeEntity? entity, EmailPath path, List<MimeEntity> bodyEntities, bool forceKind, EmailAttachmentKind forcedKind, bool relatedResource, List<EmailAttachmentView> views)
    {
        string? filename = GetFilename(entity);
        if (!forceKind && (entity ?? throw new InvalidOperationException()).GetKind() != MimeEntityKind.Message && entity.IsContainer() && (HasAttachmentDisposition(entity) || filename != null && !relatedResource))
        {
            views.Add(new EmailAttachmentView(entity, path, EmailAttachmentKind.Attachment, filename));
            return;
        }

        if ((entity ?? throw new InvalidOperationException()).GetKind() == MimeEntityKind.Message)
        {
            EmailAttachmentKind messageKind = forceKind ? forcedKind : EmailAttachmentKind.NestedMessage;
            views.Add(new EmailAttachmentView(entity, path, messageKind, filename));
            if ((entity.GetNestedMessage() ?? throw new InvalidOperationException()).HasBody())
            {
                MimeEntity? nestedRoot = (entity.GetNestedMessage() ?? throw new InvalidOperationException()).GetBody();
                List<MimeEntity> nestedBodies = GetBodyEntities(nestedRoot);
                Collect(nestedRoot, path.Append(0), nestedBodies, forceKind, forcedKind, relatedResource, views);
            }

            return;
        }

        if (entity.GetKind() == MimeEntityKind.Signed)
        {
            Collect(entity.GetChildAt(0), path.Append(0), bodyEntities, forceKind, forcedKind, relatedResource, views);
            Collect(entity.GetChildAt(1), path.Append(1), bodyEntities, true, EmailAttachmentKind.Signature, false, views);
            return;
        }

        if (entity.GetKind() == MimeEntityKind.Encrypted)
        {
            Collect(entity.GetChildAt(0), path.Append(0), bodyEntities, true, EmailAttachmentKind.EncryptionControl, false, views);
            Collect(entity.GetChildAt(1), path.Append(1), bodyEntities, true, EmailAttachmentKind.EncryptedPayload, false, views);
            return;
        }

        if (entity.GetKind() == MimeEntityKind.Multipart)
        {
            CollectMultipart(entity, path, bodyEntities, forceKind, forcedKind, relatedResource, views);
            return;
        }

        EmailAttachmentKind kind = Classify(entity, bodyEntities, forceKind, forcedKind, relatedResource, filename);
        views.Add(new EmailAttachmentView(entity, path, kind, filename));
    }

    void CollectMultipart(MimeEntity entity, EmailPath path, List<MimeEntity> bodyEntities, bool forceKind, EmailAttachmentKind forcedKind, bool relatedResource, List<EmailAttachmentView> views)
    {
        int relatedRoot = -1;
        if ((entity.GetMediaType() ?? throw new InvalidOperationException()).IsType("multipart", "related"))
        {
            relatedRoot = GetRelatedRootIndex(entity);
        }

        int childIndex = 0;
        while (childIndex < entity.GetChildCount())
        {
            bool childRelated = relatedResource || relatedRoot >= 0 && childIndex != relatedRoot;
            Collect(entity.GetChildAt(childIndex), path.Append(childIndex), bodyEntities, forceKind, forcedKind, childRelated, views);
            childIndex++;
        }
    }

    static EmailAttachmentKind Classify(MimeEntity entity, List<MimeEntity> bodyEntities, bool forceKind, EmailAttachmentKind forcedKind, bool relatedResource, string? filename)
    {
        if (forceKind)
        {
            return forcedKind;
        }

        if (HasAttachmentDisposition(entity))
        {
            return EmailAttachmentKind.Attachment;
        }

        if (relatedResource)
        {
            return EmailAttachmentKind.InlineResource;
        }

        if (filename != null)
        {
            return EmailAttachmentKind.Attachment;
        }

        if (ContainsReference(bodyEntities, entity))
        {
            return EmailAttachmentKind.Body;
        }

        if (entity.HasDisposition() && (entity.GetDisposition() ?? throw new InvalidOperationException()).IsInline())
        {
            return EmailAttachmentKind.InlineResource;
        }

        if (entity.HasContentId() || entity.HasContentLocation())
        {
            return EmailAttachmentKind.InlineResource;
        }

        return EmailAttachmentKind.Attachment;
    }

    static List<MimeEntity> GetBodyEntities(MimeEntity? root)
    {
        EmailBodySelectorWorker selector = new EmailBodySelectorWorker(EmailBodyPreference.Html);
        return selector.Choose(root).GetAlternatives();
    }

    static bool HasAttachmentDisposition(MimeEntity entity)
    {
        return entity.HasDisposition() && !(entity.GetDisposition() ?? throw new InvalidOperationException()).IsInline();
    }

    static bool ContainsReference(List<MimeEntity> entities, MimeEntity expected)
    {
        int index = 0;
        while (index < entities.Count)
        {
            if (object.ReferenceEquals(entities[index], expected))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    static string? GetFilename(MimeEntity? entity)
    {
        return new EmailFilename().Decode(entity);
    }

    static int GetRelatedRootIndex(MimeEntity entity)
    {
        if (entity.GetChildCount() == 0)
        {
            return -1;
        }

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
}
