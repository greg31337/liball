using System;
using System.Collections.Generic;
using System.Text.Json;

namespace jmap;

internal class EmailSetValidationContext
{
    internal Dictionary<string, Mailbox> Mailboxes = new Dictionary<string, Mailbox>();
    internal JmapMailCapabilities? Capabilities;
    internal Dictionary<string, long> BlobSizes = new Dictionary<string, long>();
    internal long MaximumKeywords;
    internal bool HasMaximumKeywords;
}

internal class EmailSetValidationResult
{
    internal List<string> InvalidProperties = new List<string>();
    internal Dictionary<string, bool> MailboxIds = new Dictionary<string, bool>();
    internal Dictionary<string, bool> Keywords = new Dictionary<string, bool>();
    internal bool IsInvalidPatch;
    internal bool IsForbidden;
    internal bool HasTooManyMailboxes;
    internal bool HasTooManyKeywords;
    internal bool IsAttachmentTooLarge;
    internal long AttachmentSize;
}

internal class EmailSetValidator
{
    internal EmailValidator EmailValues = new EmailValidator();
    internal PatchValidator Patches = new PatchValidator();
    internal JsonPointer Pointers = new JsonPointer();
    internal BodyPartMapper BodyParts = new BodyPartMapper();
    internal MethodContext Access = new MethodContext();

    internal EmailSetValidationResult ValidateCreate(Email? email, EmailSetValidationContext? context)
    {
        EmailSetValidationResult result = new EmailSetValidationResult();
        AddPaths(EmailValues.ValidateCreate(email), result.InvalidProperties);
        if (email == null)
        {
            return result;
        }

        CopyBooleanMap(email.MailboxIds, result.MailboxIds);
        CopyBooleanMap(email.Keywords, result.Keywords);
        ValidateKeywordRules(result.Keywords, "keywords", result.InvalidProperties);
        ValidateContext(context, result.InvalidProperties);
        if (context == null || context.Capabilities == null)
        {
            return result;
        }

        ValidateMailboxSet(result.MailboxIds, context, true, result);
        ValidateKeywordChanges(new Dictionary<string, bool>(), result.Keywords,
            new Dictionary<string, bool>(), context, result);
        ApplySetLimits(result, context);
        result.AttachmentSize = AttachmentSize(email, context);
        result.IsAttachmentTooLarge = result.AttachmentSize > context.Capabilities.MaxSizeAttachmentsPerEmail;
        return result;
    }

    internal EmailSetValidationResult ValidateUpdate(Email? original, JmapPatchObject? patch,
        EmailSetValidationContext? context)
    {
        EmailSetValidationResult result = new EmailSetValidationResult();
        if (original == null)
        {
            result.InvalidProperties.Add("");
            return result;
        }

        CopyBooleanMap(original.MailboxIds, result.MailboxIds);
        CopyBooleanMap(original.Keywords, result.Keywords);
        if (!IsPatchStructureValid(patch))
        {
            result.IsInvalidPatch = true;
            return result;
        }

        if (patch == null)
        {
            result.IsInvalidPatch = true;
            return result;
        }

        Dictionary<string, bool> oldMailboxes = CopyMap(result.MailboxIds);
        Dictionary<string, bool> oldKeywords = CopyMap(result.Keywords);
        ApplyUpdatePatch(patch, result);
        ValidateKeywordRules(result.Keywords, "keywords", result.InvalidProperties);
        ValidateContext(context, result.InvalidProperties);
        if (context == null || context.Capabilities == null)
        {
            return result;
        }

        ValidateMailboxSet(result.MailboxIds, context, false, result);
        ValidateMailboxChanges(oldMailboxes, result.MailboxIds, context, result);
        ValidateKeywordChanges(oldKeywords, result.Keywords, oldMailboxes, context, result);
        ApplySetLimits(result, context);
        return result;
    }

    internal EmailSetValidationResult ValidateDestroy(Email? original, EmailSetValidationContext? context)
    {
        EmailSetValidationResult result = new EmailSetValidationResult();
        if (original == null)
        {
            result.InvalidProperties.Add("");
            return result;
        }

        CopyBooleanMap(original.MailboxIds, result.MailboxIds);
        CopyBooleanMap(original.Keywords, result.Keywords);
        ValidateContext(context, result.InvalidProperties);
        if (context == null)
        {
            return result;
        }

        foreach (string mailboxId in result.MailboxIds.Keys)
        {
            Mailbox? mailbox = FindMailbox(context, mailboxId);
            if (!Access.HasMailboxRight(mailbox, MailboxRightKind.RemoveItems))
            {
                result.IsForbidden = true;
            }
        }

        return result;
    }

    internal void ApplyUpdatePatch(JmapPatchObject patch, EmailSetValidationResult result)
    {
        bool setJunk = false;
        bool setNotJunk = false;
        foreach (KeyValuePair<string, JsonElement> item in patch.Values)
        {
            if (item.Key == "mailboxIds")
            {
                ApplyFullBooleanMap(item.Value, false, "mailboxIds", result.MailboxIds, result.InvalidProperties);
            }
            else if (item.Key == "keywords")
            {
                ApplyFullBooleanMap(item.Value, true, "keywords", result.Keywords, result.InvalidProperties);
            }
            else if (IsSingleChildPath(item.Key, "mailboxIds/"))
            {
                string key = DecodeChildKey(item.Key, "mailboxIds/");
                ApplyChildBoolean(item.Value, key, item.Key, false, result.MailboxIds, result.InvalidProperties);
            }
            else if (IsSingleChildPath(item.Key, "keywords/"))
            {
                string key = DecodeChildKey(item.Key, "keywords/");
                ApplyChildBoolean(item.Value, key, item.Key, true, result.Keywords, result.InvalidProperties);
                if (item.Value.ValueKind == JsonValueKind.True && key == JmapMailKeywords.Junk)
                {
                    setJunk = true;
                }

                if (item.Value.ValueKind == JsonValueKind.True && key == JmapMailKeywords.NotJunk)
                {
                    setNotJunk = true;
                }
            }
            else
            {
                result.InvalidProperties.Add(item.Key);
            }
        }

        if (setJunk && !setNotJunk)
        {
            result.Keywords.Remove(JmapMailKeywords.NotJunk);
        }
        else if (setNotJunk && !setJunk)
        {
            result.Keywords.Remove(JmapMailKeywords.Junk);
        }
    }

    internal void ApplyFullBooleanMap(JsonElement value, bool keywordMap, string path,
        Dictionary<string, bool> destination, List<string> invalid)
    {
        if (value.ValueKind == JsonValueKind.Null && keywordMap)
        {
            destination.Clear();
            return;
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            invalid.Add(path);
            return;
        }

        Dictionary<string, bool> replacement = new Dictionary<string, bool>();
        foreach (JsonProperty property in value.EnumerateObject())
        {
            bool validKey = keywordMap ? EmailValues.Values.IsKeyword(property.Name) :
                EmailValues.Values.IsId(property.Name);
            if (!validKey || property.Value.ValueKind != JsonValueKind.True || replacement.ContainsKey(property.Name))
            {
                invalid.Add(path + "/" + Pointers.EscapeToken(property.Name));
            }
            else
            {
                replacement.Add(property.Name, true);
            }
        }

        if (!keywordMap && replacement.Count == 0)
        {
            invalid.Add(path);
        }

        destination.Clear();
        CopyBooleanMap(replacement, destination);
    }

    internal void ApplyChildBoolean(JsonElement value, string key, string path, bool keyword,
        Dictionary<string, bool> destination, List<string> invalid)
    {
        bool validKey = keyword ? EmailValues.Values.IsKeyword(key) : EmailValues.Values.IsId(key);
        if (!validKey || value.ValueKind != JsonValueKind.True && value.ValueKind != JsonValueKind.Null)
        {
            invalid.Add(path);
            return;
        }

        if (value.ValueKind == JsonValueKind.Null)
        {
            destination.Remove(key);
        }
        else
        {
            destination[key] = true;
        }

    }

    internal bool IsPatchStructureValid(JmapPatchObject? patch)
    {
        if (patch == null || patch.Values == null || Patches.FindConflictingPaths(patch).Count != 0)
        {
            return false;
        }

        foreach (KeyValuePair<string, JsonElement> item in patch.Values)
        {
            if (!Patches.IsPatchPath(item.Key) || item.Value.ValueKind == JsonValueKind.Undefined)
            {
                return false;
            }

        }

        return true;
    }

    internal bool IsSingleChildPath(string path, string prefix)
    {
        if (!path.StartsWith(prefix, StringComparison.Ordinal) || path.Length == prefix.Length)
        {
            return false;
        }

        return path.IndexOf('/', prefix.Length) < 0;
    }

    internal string DecodeChildKey(string path, string prefix)
    {
        return Pointers.UnescapeToken(path.Substring(prefix.Length));
    }

    internal void ValidateKeywordRules(Dictionary<string, bool> keywords, string path, List<string> invalid)
    {
        foreach (string keyword in keywords.Keys)
        {
            if (!EmailValues.Values.IsKeyword(keyword) || keyword == "$recent")
            {
                invalid.Add(path + "/" + Pointers.EscapeToken(keyword));
            }
        }

        if (keywords.ContainsKey(JmapMailKeywords.Junk) && keywords.ContainsKey(JmapMailKeywords.NotJunk))
        {
            invalid.Add(path + "/" + JmapMailKeywords.Junk);
            invalid.Add(path + "/" + JmapMailKeywords.NotJunk);
        }
    }

    internal void ValidateMailboxSet(Dictionary<string, bool> mailboxIds, EmailSetValidationContext context,
        bool addingAll, EmailSetValidationResult result)
    {
        if (mailboxIds.Count == 0)
        {
            AddUnique("mailboxIds", result.InvalidProperties);
        }

        foreach (string mailboxId in mailboxIds.Keys)
        {
            Mailbox? mailbox = FindMailbox(context, mailboxId);
            if (mailbox == null)
            {
                AddUnique("mailboxIds/" + mailboxId, result.InvalidProperties);
            }
            else if (addingAll &&
                !Access.HasMailboxRight(mailbox, MailboxRightKind.AddItems))
            {
                result.IsForbidden = true;
            }
        }
    }

    internal void ValidateMailboxChanges(Dictionary<string, bool> oldIds, Dictionary<string, bool> newIds,
        EmailSetValidationContext context, EmailSetValidationResult result)
    {
        foreach (string mailboxId in oldIds.Keys)
        {
            if (!newIds.ContainsKey(mailboxId))
            {
                Mailbox? mailbox = FindMailbox(context, mailboxId);
                if (!Access.HasMailboxRight(mailbox, MailboxRightKind.RemoveItems))
                {
                    result.IsForbidden = true;
                }
            }
        }

        foreach (string mailboxId in newIds.Keys)
        {
            if (!oldIds.ContainsKey(mailboxId))
            {
                Mailbox? mailbox = FindMailbox(context, mailboxId);
                if (!Access.HasMailboxRight(mailbox, MailboxRightKind.AddItems))
                {
                    result.IsForbidden = true;
                }
            }
        }
    }

    internal void ValidateKeywordChanges(Dictionary<string, bool> oldKeywords, Dictionary<string, bool> newKeywords,
        Dictionary<string, bool> oldMailboxes, EmailSetValidationContext context, EmailSetValidationResult result)
    {
        bool seenChanged = oldKeywords.ContainsKey(JmapMailKeywords.Seen) !=
            newKeywords.ContainsKey(JmapMailKeywords.Seen);
        bool otherChanged = OtherKeywordsChanged(oldKeywords, newKeywords);
        if (!seenChanged && !otherChanged)
        {
            return;
        }

        Dictionary<string, bool> allMailboxes = CopyMap(oldMailboxes);
        CopyBooleanMap(result.MailboxIds, allMailboxes);
        foreach (string mailboxId in allMailboxes.Keys)
        {
            Mailbox? mailbox = FindMailbox(context, mailboxId);
            if (mailbox == null ||
                seenChanged && !Access.HasMailboxRight(mailbox, MailboxRightKind.SetSeen) ||
                otherChanged && !Access.HasMailboxRight(mailbox, MailboxRightKind.SetKeywords))
            {
                result.IsForbidden = true;
            }
        }
    }

    internal bool OtherKeywordsChanged(Dictionary<string, bool> oldKeywords, Dictionary<string, bool> newKeywords)
    {
        foreach (string keyword in oldKeywords.Keys)
        {
            if (keyword != JmapMailKeywords.Seen && !newKeywords.ContainsKey(keyword))
            {
                return true;
            }
        }

        foreach (string keyword in newKeywords.Keys)
        {
            if (keyword != JmapMailKeywords.Seen && !oldKeywords.ContainsKey(keyword))
            {
                return true;
            }
        }

        return false;
    }

    internal void ApplySetLimits(EmailSetValidationResult result, EmailSetValidationContext context)
    {
        JmapMailCapabilities? capabilities = context.Capabilities;
        if (capabilities != null && capabilities.HasMaxMailboxesPerEmail &&
            !capabilities.IsMaxMailboxesPerEmailNull && result.MailboxIds.Count > capabilities.MaxMailboxesPerEmail)
        {
            result.HasTooManyMailboxes = true;
        }

        if (context.HasMaximumKeywords && result.Keywords.Count > context.MaximumKeywords)
        {
            result.HasTooManyKeywords = true;
        }
    }

    internal long AttachmentSize(Email email, EmailSetValidationContext context)
    {
        List<EmailBodyPart> attachments = new List<EmailBodyPart>();
        if (email.BodyStructure != null)
        {
            List<EmailBodyPart> roots = new List<EmailBodyPart>();
            roots.Add(email.BodyStructure);
            BodyParts.ParseStructure(roots, "mixed", false, new List<EmailBodyPart>(), new List<EmailBodyPart>(),
                attachments);
        }
        else if (email.Attachments != null)
        {
            foreach (EmailBodyPart attachment in email.Attachments)
            {
                attachments.Add(attachment);
            }
        }

        long total = 0;
        foreach (EmailBodyPart attachment in attachments)
        {
            long size = PartSize(attachment, email.BodyValues, context);
            if (size > long.MaxValue - total)
            {
                return long.MaxValue;
            }

            total = total + size;
        }

        return total;
    }

    internal long PartSize(EmailBodyPart part, Dictionary<string, EmailBodyValue>? bodyValues,
        EmailSetValidationContext context)
    {
        if (part.SubParts != null)
        {
            long total = 0;
            foreach (EmailBodyPart child in part.SubParts)
            {
                long childSize = PartSize(child, bodyValues, context);
                if (childSize > long.MaxValue - total)
                {
                    return long.MaxValue;
                }

                total = total + childSize;
            }

            return total;
        }

        if (part.BlobId != null && context.BlobSizes.ContainsKey(part.BlobId))
        {
            long size = context.BlobSizes[part.BlobId];
            return size < 0 ? 0 : size;
        }

        if (part.PartId != null && bodyValues != null && bodyValues.ContainsKey(part.PartId) &&
            bodyValues[part.PartId] != null)
        {
            long size = EmailValues.Values.GetUtf8Size(bodyValues[part.PartId].Value);
            return size < 0 ? 0 : size;
        }

        return 0;
    }

    internal Mailbox? FindMailbox(EmailSetValidationContext context, string id)
    {
        return context.Mailboxes.ContainsKey(id) ? context.Mailboxes[id] : null;
    }

    internal void ValidateContext(EmailSetValidationContext? context, List<string> invalid)
    {
        if (context == null)
        {
            invalid.Add("context");
        }
        else if (context.Capabilities == null)
        {
            invalid.Add("capabilities");
        }
        else
        {
            if (context.HasMaximumKeywords && context.MaximumKeywords < 0)
            {
                invalid.Add("maximumKeywords");
            }

            if (!EmailValues.Values.IsUnsignedInt(context.Capabilities.MaxSizeAttachmentsPerEmail))
            {
                invalid.Add("capabilities/maxSizeAttachmentsPerEmail");
            }

            foreach (KeyValuePair<string, long> blob in context.BlobSizes)
            {
                if (blob.Value < 0)
                {
                    invalid.Add("blobSizes/" + blob.Key);
                }
            }
        }
    }

    internal Dictionary<string, bool> CopyMap(Dictionary<string, bool> source)
    {
        Dictionary<string, bool> result = new Dictionary<string, bool>();
        CopyBooleanMap(source, result);
        return result;
    }

    internal void CopyBooleanMap(Dictionary<string, bool>? source, Dictionary<string, bool> destination)
    {
        if (source == null)
        {
            return;
        }

        foreach (KeyValuePair<string, bool> item in source)
        {
            destination[item.Key] = item.Value;
        }
    }

    internal void AddPaths(List<string> paths, List<string> destination)
    {
        foreach (string path in paths)
        {
            AddUnique(path, destination);
        }
    }

    internal void AddUnique(string path, List<string> destination)
    {
        if (!destination.Contains(path))
        {
            destination.Add(path);
        }
    }
}
