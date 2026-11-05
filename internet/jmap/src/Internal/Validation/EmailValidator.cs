using System.Collections.Generic;

namespace jmap;

internal class EmailValidator
{
    internal ValueValidator Values = new ValueValidator();
    internal EmailHeaderCreationValidator HeaderCreation = new EmailHeaderCreationValidator();

    internal List<string> ValidateCreate(Email? email)
    {
        List<string> invalid = new List<string>();
        if (email == null)
        {
            invalid.Add("");
            return invalid;
        }

        AddServerSetCreatePaths(email, invalid);
        ValidateMailboxIds(email.MailboxIds, "mailboxIds", true, invalid);
        ValidateKeywords(email.Keywords, "keywords", false, invalid);

        if (email.ReceivedAt != null && !Values.IsUtcDate(email.ReceivedAt))
        {
            invalid.Add("receivedAt");
        }

        if (email.Headers != null)
        {
            invalid.Add("headers");
        }

        if (email.HeaderProperties == null)
        {
            invalid.Add("headerProperties");
        }

        HeaderCreation.ValidateEmailHeaders(email, invalid);

        bool hasSimpleBody = email.TextBody != null || email.HtmlBody != null || email.Attachments != null;
        if (email.BodyStructure != null && hasSimpleBody)
        {
            invalid.Add("bodyStructure");
            if (email.TextBody != null)
            {
                invalid.Add("textBody");
            }

            if (email.HtmlBody != null)
            {
                invalid.Add("htmlBody");
            }

            if (email.Attachments != null)
            {
                invalid.Add("attachments");
            }
        }

        if (email.TextBody != null && (email.TextBody.Count != 1 || email.TextBody[0].Type != "text/plain"))
        {
            invalid.Add("textBody");
        }

        if (email.HtmlBody != null && (email.HtmlBody.Count != 1 || email.HtmlBody[0].Type != "text/html"))
        {
            invalid.Add("htmlBody");
        }

        if (email.BodyStructure != null)
        {
            ValidateBodyPart(email.BodyStructure, "bodyStructure", email.BodyValues, invalid);
            HeaderCreation.ValidateRootHeaderConflicts(email, invalid);
        }

        ValidateBodyPartList(email.TextBody, "textBody", email.BodyValues, invalid);
        ValidateBodyPartList(email.HtmlBody, "htmlBody", email.BodyValues, invalid);
        ValidateBodyPartList(email.Attachments, "attachments", email.BodyValues, invalid);
        ValidateBodyValues(email.BodyValues, invalid);
        return RemoveDuplicatePaths(invalid);
    }

    internal List<string> ValidateCopy(Email? email)
    {
        List<string> invalid = new List<string>();
        if (email == null)
        {
            invalid.Add("");
            return invalid;
        }

        ValidateMailboxIds(email.MailboxIds, "mailboxIds", true, invalid);
        ValidateKeywords(email.Keywords, "keywords", false, invalid);
        if (email.ReceivedAt != null && !Values.IsUtcDate(email.ReceivedAt))
        {
            invalid.Add("receivedAt");
        }

        if (!Values.IsId(email.Id))
        {
            invalid.Add("id");
        }

        if (email.BlobId != null)
        {
            invalid.Add("blobId");
        }

        if (email.ThreadId != null)
        {
            invalid.Add("threadId");
        }

        if (email.Size != 0L)
        {
            invalid.Add("size");
        }

        if (email.Headers != null)
        {
            invalid.Add("headers");
        }

        if (email.HeaderProperties == null)
        {
            invalid.Add("headerProperties");
        }
        else
        {
            foreach (string property in email.HeaderProperties.Keys)
            {
                invalid.Add(property);
            }
        }

        AddCopyPath(email.MessageId, "messageId", invalid);
        AddCopyPath(email.InReplyTo, "inReplyTo", invalid);
        AddCopyPath(email.References, "references", invalid);
        AddCopyPath(email.Sender, "sender", invalid);
        AddCopyPath(email.From, "from", invalid);
        AddCopyPath(email.To, "to", invalid);
        AddCopyPath(email.Cc, "cc", invalid);
        AddCopyPath(email.Bcc, "bcc", invalid);
        AddCopyPath(email.ReplyTo, "replyTo", invalid);
        AddCopyPath(email.Subject, "subject", invalid);
        AddCopyPath(email.SentAt, "sentAt", invalid);
        AddCopyPath(email.BodyStructure, "bodyStructure", invalid);
        AddCopyPath(email.BodyValues, "bodyValues", invalid);
        AddCopyPath(email.TextBody, "textBody", invalid);
        AddCopyPath(email.HtmlBody, "htmlBody", invalid);
        AddCopyPath(email.Attachments, "attachments", invalid);

        if (email.HasAttachment)
        {
            invalid.Add("hasAttachment");
        }

        if (email.Preview != null)
        {
            invalid.Add("preview");
        }

        return invalid;
    }

    internal void AddCopyPath(object? value, string path, List<string> invalid)
    {
        if (value != null)
        {
            invalid.Add(path);
        }
    }

    internal List<string> RemoveDuplicatePaths(List<string> paths)
    {
        List<string> unique = new List<string>();
        foreach (string path in paths)
        {
            if (!unique.Contains(path))
            {
                unique.Add(path);
            }
        }

        return unique;
    }

    internal List<string> ValidateImport(EmailImport? emailImport)
    {
        List<string> invalid = new List<string>();
        if (emailImport == null)
        {
            invalid.Add("");
            return invalid;
        }

        if (!Values.IsId(emailImport.BlobId))
        {
            invalid.Add("blobId");
        }

        ValidateMailboxIds(emailImport.MailboxIds, "mailboxIds", true, invalid);
        ValidateKeywords(emailImport.Keywords, "keywords", false, invalid);
        if (emailImport.ReceivedAt != null && !Values.IsUtcDate(emailImport.ReceivedAt))
        {
            invalid.Add("receivedAt");
        }

        return invalid;
    }

    internal List<string> ValidateWritablePatch(JmapPatchObject? patch)
    {
        List<string> invalid = new List<string>();
        if (patch == null)
        {
            invalid.Add("");
            return invalid;
        }

        foreach (string path in patch.Values.Keys)
        {
            if (path != "mailboxIds" && path != "keywords" && !path.StartsWith("mailboxIds/", global::System.StringComparison.Ordinal) && !path.StartsWith("keywords/", global::System.StringComparison.Ordinal))
            {
                invalid.Add(path);
            }
        }

        return invalid;
    }

    internal void AddServerSetCreatePaths(Email email, List<string> invalid)
    {
        if (email.Id != null)
        {
            invalid.Add("id");
        }

        if (email.BlobId != null)
        {
            invalid.Add("blobId");
        }

        if (email.ThreadId != null)
        {
            invalid.Add("threadId");
        }

        if (email.Size != 0L)
        {
            invalid.Add("size");
        }

        if (email.HasAttachment)
        {
            invalid.Add("hasAttachment");
        }

        if (email.Preview != null)
        {
            invalid.Add("preview");
        }
    }

    internal void ValidateMailboxIds(Dictionary<string, bool>? mailboxIds, string path, bool requireValue, List<string> invalid)
    {
        if (mailboxIds == null || (requireValue && mailboxIds.Count == 0))
        {
            invalid.Add(path);
            return;
        }

        foreach (KeyValuePair<string, bool> item in mailboxIds)
        {
            if ((!Values.IsId(item.Key) && !Values.IsCreationIdReference(item.Key)) || !item.Value)
            {
                invalid.Add(path + "/" + item.Key);
            }
        }
    }

    internal void ValidateKeywords(Dictionary<string, bool>? keywords, string path, bool requireValue, List<string> invalid)
    {
        if (keywords == null)
        {
            if (requireValue)
            {
                invalid.Add(path);
            }

            return;
        }

        foreach (KeyValuePair<string, bool> item in keywords)
        {
            if (!Values.IsKeyword(item.Key) || !item.Value)
            {
                invalid.Add(path + "/" + item.Key);
            }
        }
    }

    internal void ValidateBodyPartList(List<EmailBodyPart>? parts, string path, Dictionary<string, EmailBodyValue>? bodyValues, List<string> invalid)
    {
        if (parts == null)
        {
            return;
        }

        int index = 0;
        while (index < parts.Count)
        {
            ValidateBodyPart(parts[index], path + "/" + index, bodyValues, invalid);
            index = index + 1;
        }
    }

    internal void ValidateBodyPart(EmailBodyPart? part, string path, Dictionary<string, EmailBodyValue>? bodyValues, List<string> invalid)
    {
        if (part == null)
        {
            invalid.Add(path);
            return;
        }

        if (part.Headers != null)
        {
            invalid.Add(path + "/headers");
        }

        if (part.PartId != null && part.BlobId != null)
        {
            invalid.Add(path + "/partId");
            invalid.Add(path + "/blobId");
        }

        if (part.PartId != null)
        {
            if (bodyValues == null || !bodyValues.ContainsKey(part.PartId))
            {
                invalid.Add(path + "/partId");
            }

            if (part.Charset != null)
            {
                invalid.Add(path + "/charset");
            }

            if (part.HasSize)
            {
                invalid.Add(path + "/size");
            }
        }

        if (part.BlobId != null && !Values.IsId(part.BlobId) && !Values.IsCreationIdReference(part.BlobId))
        {
            invalid.Add(path + "/blobId");
        }

        if (part.HasSize && !Values.IsUnsignedInt(part.Size))
        {
            invalid.Add(path + "/size");
        }

        if (part.Type == null || !Values.IsMediaType(part.Type))
        {
            invalid.Add(path + "/type");
        }

        if (part.HeaderProperties == null)
        {
            invalid.Add(path + "/headerProperties");
        }

        HeaderCreation.ValidateBodyPartHeaders(part, path, invalid);

        ValidateBodyPartList(part.SubParts, path + "/subParts", bodyValues, invalid);
    }

    internal void ValidateBodyValues(Dictionary<string, EmailBodyValue>? bodyValues, List<string> invalid)
    {
        if (bodyValues == null)
        {
            return;
        }

        foreach (KeyValuePair<string, EmailBodyValue> item in bodyValues)
        {
            string path = "bodyValues/" + item.Key;
            if (item.Value == null)
            {
                invalid.Add(path);
            }
            else
            {
                if (item.Value.Value == null)
                {
                    invalid.Add(path + "/value");
                }

                if (item.Value.IsEncodingProblem)
                {
                    invalid.Add(path + "/isEncodingProblem");
                }

                if (item.Value.IsTruncated)
                {
                    invalid.Add(path + "/isTruncated");
                }
            }
        }
    }
}
