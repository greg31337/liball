using System.Collections.Generic;
using System.Text.Json;

namespace jmap;

internal class EmailJsonCodec
{
    internal JmapJsonReader Reader;
    internal SessionJsonCodec Values;
    internal ProtocolJsonCodec Optional;
    internal StandardMethodJsonCodec Standard;

    internal EmailJsonCodec(JmapJsonReader reader)
    {
        if (reader == null)
        {
            throw new System.ArgumentNullException("reader");
        }

        Reader = reader;
        Values = new SessionJsonCodec(reader);
        Optional = new ProtocolJsonCodec(reader);
        Standard = new StandardMethodJsonCodec(reader);
    }

    internal Email DecodeEmail(JsonElement value, string path)
    {
        Values.RequireObject(value, path);
        Email result = new Email();
        result.Id = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.Id), path + "/id");
        result.BlobId = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.BlobId), path + "/blobId");
        result.ThreadId = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.ThreadId), path + "/threadId");
        result.MailboxIds = DecodeOptionalBooleanMap(Values.GetMember(value, JmapProperties.MailboxIds), path + "/mailboxIds");
        result.Keywords = DecodeOptionalBooleanMap(Values.GetMember(value, JmapProperties.Keywords), path + "/keywords");
        JsonElement size = Values.GetMember(value, JmapProperties.Size);
        if (size.ValueKind != JsonValueKind.Undefined)
        {
            result.Size = Values.ReadRequiredUnsignedInteger(size, path + "/size");
        }

        result.ReceivedAt = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.ReceivedAt), path + "/receivedAt");
        result.Headers = DecodeOptionalHeaderList(Values.GetMember(value, JmapProperties.Headers), path + "/headers");
        result.MessageId = Optional.ReadOptionalStringArray(Values.GetMember(value, JmapProperties.MessageId), path + "/messageId");
        result.InReplyTo = Optional.ReadOptionalStringArray(Values.GetMember(value, JmapProperties.InReplyTo), path + "/inReplyTo");
        result.References = Optional.ReadOptionalStringArray(Values.GetMember(value, JmapProperties.References), path + "/references");
        result.Sender = DecodeOptionalAddressList(Values.GetMember(value, JmapProperties.Sender), path + "/sender");
        result.From = DecodeOptionalAddressList(Values.GetMember(value, JmapProperties.From), path + "/from");
        result.To = DecodeOptionalAddressList(Values.GetMember(value, JmapProperties.To), path + "/to");
        result.Cc = DecodeOptionalAddressList(Values.GetMember(value, JmapProperties.Cc), path + "/cc");
        result.Bcc = DecodeOptionalAddressList(Values.GetMember(value, JmapProperties.Bcc), path + "/bcc");
        result.ReplyTo = DecodeOptionalAddressList(Values.GetMember(value, JmapProperties.ReplyTo), path + "/replyTo");
        result.Subject = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.Subject), path + "/subject");
        result.SentAt = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.SentAt), path + "/sentAt");
        JsonElement bodyStructure = Values.GetMember(value, JmapProperties.BodyStructure);
        if (bodyStructure.ValueKind != JsonValueKind.Undefined && bodyStructure.ValueKind != JsonValueKind.Null)
        {
            result.BodyStructure = DecodeBodyPart(bodyStructure, path + "/bodyStructure");
        }

        result.BodyValues = DecodeOptionalBodyValueMap(Values.GetMember(value, JmapProperties.BodyValues), path + "/bodyValues");
        result.TextBody = DecodeOptionalBodyPartList(Values.GetMember(value, JmapProperties.TextBody), path + "/textBody");
        result.HtmlBody = DecodeOptionalBodyPartList(Values.GetMember(value, JmapProperties.HtmlBody), path + "/htmlBody");
        result.Attachments = DecodeOptionalBodyPartList(Values.GetMember(value, JmapProperties.Attachments), path + "/attachments");
        JsonElement hasAttachment = Values.GetMember(value, JmapProperties.HasAttachment);
        if (hasAttachment.ValueKind != JsonValueKind.Undefined)
        {
            result.HasAttachment = Values.ReadRequiredBoolean(hasAttachment, path + "/hasAttachment");
        }

        result.Preview = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.Preview), path + "/preview");
        CaptureEmailMembers(value, result, EmailNames());
        return result;
    }

    internal Email DecodeEmailCreate(JsonElement value, string path)
    {
        Values.RequireObject(value, path);
        Email result = new Email();
        result.Id = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.Id), path + "/id");
        result.BlobId = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.BlobId), path + "/blobId");
        result.ThreadId = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.ThreadId), path + "/threadId");
        result.MailboxIds = DecodeOptionalBooleanMap(Values.GetMember(value, JmapProperties.MailboxIds), path + "/mailboxIds");
        result.Keywords = DecodeOptionalBooleanMap(Values.GetMember(value, JmapProperties.Keywords), path + "/keywords");
        JsonElement size = Values.GetMember(value, JmapProperties.Size);
        if (size.ValueKind != JsonValueKind.Undefined)
        {
            result.Size = Values.ReadRequiredUnsignedInteger(size, path + "/size");
        }

        result.ReceivedAt = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.ReceivedAt), path + "/receivedAt");
        result.Headers = DecodeOptionalHeaderList(Values.GetMember(value, JmapProperties.Headers), path + "/headers");
        result.MessageId = Optional.ReadOptionalStringArray(Values.GetMember(value, JmapProperties.MessageId), path + "/messageId");
        result.InReplyTo = Optional.ReadOptionalStringArray(Values.GetMember(value, JmapProperties.InReplyTo), path + "/inReplyTo");
        result.References = Optional.ReadOptionalStringArray(Values.GetMember(value, JmapProperties.References), path + "/references");
        result.Sender = DecodeOptionalAddressList(Values.GetMember(value, JmapProperties.Sender), path + "/sender");
        result.From = DecodeOptionalAddressList(Values.GetMember(value, JmapProperties.From), path + "/from");
        result.To = DecodeOptionalAddressList(Values.GetMember(value, JmapProperties.To), path + "/to");
        result.Cc = DecodeOptionalAddressList(Values.GetMember(value, JmapProperties.Cc), path + "/cc");
        result.Bcc = DecodeOptionalAddressList(Values.GetMember(value, JmapProperties.Bcc), path + "/bcc");
        result.ReplyTo = DecodeOptionalAddressList(Values.GetMember(value, JmapProperties.ReplyTo), path + "/replyTo");
        result.Subject = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.Subject), path + "/subject");
        result.SentAt = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.SentAt), path + "/sentAt");
        JsonElement bodyStructure = Values.GetMember(value, JmapProperties.BodyStructure);
        if (bodyStructure.ValueKind != JsonValueKind.Undefined && bodyStructure.ValueKind != JsonValueKind.Null)
        {
            result.BodyStructure = DecodeBodyPart(bodyStructure, path + "/bodyStructure");
        }

        result.BodyValues = DecodeOptionalCreateBodyValueMap(Values.GetMember(value, JmapProperties.BodyValues), path + "/bodyValues");
        result.TextBody = DecodeOptionalBodyPartList(Values.GetMember(value, JmapProperties.TextBody), path + "/textBody");
        result.HtmlBody = DecodeOptionalBodyPartList(Values.GetMember(value, JmapProperties.HtmlBody), path + "/htmlBody");
        result.Attachments = DecodeOptionalBodyPartList(Values.GetMember(value, JmapProperties.Attachments), path + "/attachments");
        JsonElement hasAttachment = Values.GetMember(value, JmapProperties.HasAttachment);
        if (hasAttachment.ValueKind != JsonValueKind.Undefined)
        {
            result.HasAttachment = Values.ReadRequiredBoolean(hasAttachment, path + "/hasAttachment");
        }

        result.Preview = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.Preview), path + "/preview");
        CaptureEmailMembers(value, result, EmailNames());
        return result;
    }

    internal void EncodeEmail(Email value, JmapJsonWriter writer, string path)
    {
        RequireEmail(value, writer, path);
        writer.WriteObjectStart();
        writer.WriteStringMember(JmapProperties.Id, value.Id, false);
        writer.WriteStringMember(JmapProperties.BlobId, value.BlobId, false);
        writer.WriteStringMember(JmapProperties.ThreadId, value.ThreadId, false);
        WriteOptionalBooleanMap(JmapProperties.MailboxIds, value.MailboxIds, writer, path + "/mailboxIds");
        WriteOptionalBooleanMap(JmapProperties.Keywords, value.Keywords, writer, path + "/keywords");
        Values.WriteUnsignedIntegerMember(writer, JmapProperties.Size, value.Size, path + "/size");
        writer.WriteStringMember(JmapProperties.ReceivedAt, value.ReceivedAt, false);
        WriteOptionalHeaderList(JmapProperties.Headers, value.Headers, writer, path + "/headers");
        WriteDynamicHeaderMembers(value.HeaderProperties, writer, path);
        Optional.WriteOptionalStringArray(JmapProperties.MessageId, value.MessageId, writer, path + "/messageId");
        Optional.WriteOptionalStringArray(JmapProperties.InReplyTo, value.InReplyTo, writer, path + "/inReplyTo");
        Optional.WriteOptionalStringArray(JmapProperties.References, value.References, writer, path + "/references");
        WriteOptionalAddressList(JmapProperties.Sender, value.Sender, writer, path + "/sender");
        WriteOptionalAddressList(JmapProperties.From, value.From, writer, path + "/from");
        WriteOptionalAddressList(JmapProperties.To, value.To, writer, path + "/to");
        WriteOptionalAddressList(JmapProperties.Cc, value.Cc, writer, path + "/cc");
        WriteOptionalAddressList(JmapProperties.Bcc, value.Bcc, writer, path + "/bcc");
        WriteOptionalAddressList(JmapProperties.ReplyTo, value.ReplyTo, writer, path + "/replyTo");
        writer.WriteStringMember(JmapProperties.Subject, value.Subject, false);
        writer.WriteStringMember(JmapProperties.SentAt, value.SentAt, false);
        if (value.BodyStructure != null)
        {
            writer.WriteMemberName(JmapProperties.BodyStructure);
            EncodeBodyPart(value.BodyStructure, writer, path + "/bodyStructure");
        }

        WriteOptionalBodyValueMap(JmapProperties.BodyValues, value.BodyValues, writer, path + "/bodyValues");
        WriteOptionalBodyPartList(JmapProperties.TextBody, value.TextBody, writer, path + "/textBody");
        WriteOptionalBodyPartList(JmapProperties.HtmlBody, value.HtmlBody, writer, path + "/htmlBody");
        WriteOptionalBodyPartList(JmapProperties.Attachments, value.Attachments, writer, path + "/attachments");
        writer.WriteBooleanMember(JmapProperties.HasAttachment, value.HasAttachment, true);
        writer.WriteStringMember(JmapProperties.Preview, value.Preview, false);
        Values.WriteUnknownMembers(value.UnknownProperties, EmailNames(), writer);
        writer.WriteObjectEnd();
    }

    internal void EncodeEmailCreate(Email value, JmapJsonWriter writer, string path)
    {
        RequireEmail(value, writer, path);
        writer.WriteObjectStart();
        WriteOptionalBooleanMap(JmapProperties.MailboxIds, value.MailboxIds, writer, path + "/mailboxIds");
        WriteOptionalBooleanMap(JmapProperties.Keywords, value.Keywords, writer, path + "/keywords");
        writer.WriteStringMember(JmapProperties.ReceivedAt, value.ReceivedAt, false);
        WriteOptionalHeaderList(JmapProperties.Headers, value.Headers, writer, path + "/headers");
        WriteDynamicHeaderMembers(value.HeaderProperties, writer, path);
        Optional.WriteOptionalStringArray(JmapProperties.MessageId, value.MessageId, writer, path + "/messageId");
        Optional.WriteOptionalStringArray(JmapProperties.InReplyTo, value.InReplyTo, writer, path + "/inReplyTo");
        Optional.WriteOptionalStringArray(JmapProperties.References, value.References, writer, path + "/references");
        WriteOptionalAddressList(JmapProperties.Sender, value.Sender, writer, path + "/sender");
        WriteOptionalAddressList(JmapProperties.From, value.From, writer, path + "/from");
        WriteOptionalAddressList(JmapProperties.To, value.To, writer, path + "/to");
        WriteOptionalAddressList(JmapProperties.Cc, value.Cc, writer, path + "/cc");
        WriteOptionalAddressList(JmapProperties.Bcc, value.Bcc, writer, path + "/bcc");
        WriteOptionalAddressList(JmapProperties.ReplyTo, value.ReplyTo, writer, path + "/replyTo");
        writer.WriteStringMember(JmapProperties.Subject, value.Subject, false);
        writer.WriteStringMember(JmapProperties.SentAt, value.SentAt, false);
        if (value.BodyStructure != null)
        {
            writer.WriteMemberName(JmapProperties.BodyStructure);
            EncodeBodyPart(value.BodyStructure, writer, path + "/bodyStructure");
        }

        WriteOptionalBodyValueMap(JmapProperties.BodyValues, value.BodyValues, writer, path + "/bodyValues");
        WriteOptionalBodyPartList(JmapProperties.TextBody, value.TextBody, writer, path + "/textBody");
        WriteOptionalBodyPartList(JmapProperties.HtmlBody, value.HtmlBody, writer, path + "/htmlBody");
        WriteOptionalBodyPartList(JmapProperties.Attachments, value.Attachments, writer, path + "/attachments");
        Values.WriteUnknownMembers(value.UnknownProperties, EmailNames(), writer);
        writer.WriteObjectEnd();
    }

    internal void EncodeEmailSetCreated(Email value, JmapJsonWriter writer, string path)
    {
        RequireEmail(value, writer, path);
        writer.WriteObjectStart();
        writer.WriteStringMember(JmapProperties.Id, value.Id, false);
        writer.WriteStringMember(JmapProperties.BlobId, value.BlobId, false);
        writer.WriteStringMember(JmapProperties.ThreadId, value.ThreadId, false);
        Values.WriteUnsignedIntegerMember(writer, JmapProperties.Size, value.Size, path + "/size");
        Values.WriteUnknownMembers(value.UnknownProperties, EmailNames(), writer);
        writer.WriteObjectEnd();
    }

    internal void EncodeEmailCopy(Email value, JmapJsonWriter writer, string path)
    {
        RequireEmail(value, writer, path);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.Id, value.Id, path + "/id");
        WriteOptionalBooleanMap(JmapProperties.MailboxIds, value.MailboxIds, writer, path + "/mailboxIds");
        WriteOptionalBooleanMap(JmapProperties.Keywords, value.Keywords, writer, path + "/keywords");
        writer.WriteStringMember(JmapProperties.ReceivedAt, value.ReceivedAt, false);
        Values.WriteUnknownMembers(value.UnknownProperties, EmailNames(), writer);
        writer.WriteObjectEnd();
    }

    internal EmailAddress DecodeAddress(JsonElement value, string path)
    {
        Values.RequireObject(value, path);
        EmailAddress result = new EmailAddress();
        result.Name = Standard.ReadRequiredNullableString(Values.GetMember(value, JmapProperties.Name), path + "/name");
        result.Email = Standard.ReadRequiredNullableString(Values.GetMember(value, JmapProperties.Email), path + "/email");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, new List<string> { JmapProperties.Name, JmapProperties.Email });
        return result;
    }

    internal void EncodeAddress(EmailAddress value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("An Email address cannot be null.", path);
        }

        writer.WriteObjectStart();
        writer.WriteStringMember(JmapProperties.Name, value.Name, true);
        writer.WriteStringMember(JmapProperties.Email, value.Email, true);
        Values.WriteUnknownMembers(value.UnknownProperties, new List<string> { JmapProperties.Name, JmapProperties.Email }, writer);
        writer.WriteObjectEnd();
    }

    internal EmailAddressGroup DecodeAddressGroup(JsonElement value, string path)
    {
        Values.RequireObject(value, path);
        EmailAddressGroup result = new EmailAddressGroup();
        result.Name = Standard.ReadRequiredNullableString(Values.GetMember(value, JmapProperties.Name), path + "/name");
        result.Addresses = DecodeAddressList(Values.GetMember(value, JmapProperties.Addresses), path + "/addresses");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, new List<string> { JmapProperties.Name, JmapProperties.Addresses });
        return result;
    }

    internal void EncodeAddressGroup(EmailAddressGroup value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("An Email address group cannot be null.", path);
        }

        writer.WriteObjectStart();
        writer.WriteStringMember(JmapProperties.Name, value.Name, true);
        writer.WriteMemberName(JmapProperties.Addresses);
        EncodeAddressList(value.Addresses, writer, path + "/addresses");
        Values.WriteUnknownMembers(value.UnknownProperties, new List<string> { JmapProperties.Name, JmapProperties.Addresses }, writer);
        writer.WriteObjectEnd();
    }

    internal List<EmailAddressGroup> DecodeAddressGroups(JsonElement value, string path)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new JmapProtocolException("An Email address-group array is required.", path, Values.CloneContext(value));
        }

        List<EmailAddressGroup> result = new List<EmailAddressGroup>();
        int index = 0;
        foreach (JsonElement item in value.EnumerateArray())
        {
            result.Add(DecodeAddressGroup(item, path + "/" + index));
            index = index + 1;
        }

        return result;
    }

    internal void EncodeAddressGroups(List<EmailAddressGroup> value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("An Email address-group array cannot be null.", path);
        }

        writer.WriteArrayStart();
        int index = 0;
        foreach (EmailAddressGroup item in value)
        {
            EncodeAddressGroup(item, writer, path + "/" + index);
            index = index + 1;
        }

        writer.WriteArrayEnd();
    }

    internal EmailHeader DecodeHeader(JsonElement value, string path)
    {
        Values.RequireObject(value, path);
        EmailHeader result = new EmailHeader();
        result.Name = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.Name), path + "/name");
        result.Value = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.Value), path + "/value");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, new List<string> { JmapProperties.Name, JmapProperties.Value });
        return result;
    }

    internal void EncodeHeader(EmailHeader value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("An Email header cannot be null.", path);
        }

        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.Name, value.Name, path + "/name");
        Values.WriteRequiredStringMember(writer, JmapProperties.Value, value.Value, path + "/value");
        Values.WriteUnknownMembers(value.UnknownProperties, new List<string> { JmapProperties.Name, JmapProperties.Value }, writer);
        writer.WriteObjectEnd();
    }

    internal EmailBodyPart DecodeBodyPart(JsonElement value, string path)
    {
        Values.RequireObject(value, path);
        EmailBodyPart result = new EmailBodyPart();
        result.PartId = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.PartId), path + "/partId");
        result.BlobId = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.BlobId), path + "/blobId");
        JsonElement size = Values.GetMember(value, JmapProperties.Size);
        if (size.ValueKind != JsonValueKind.Undefined)
        {
            result.Size = Values.ReadRequiredUnsignedInteger(size, path + "/size");
            result.HasSize = true;
        }

        result.Headers = DecodeOptionalHeaderList(Values.GetMember(value, JmapProperties.Headers), path + "/headers");
        result.Name = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.Name), path + "/name");
        result.Type = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.Type), path + "/type");
        result.Charset = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.Charset), path + "/charset");
        result.Disposition = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.Disposition), path + "/disposition");
        result.Cid = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.Cid), path + "/cid");
        result.Language = Optional.ReadOptionalStringArray(Values.GetMember(value, JmapProperties.Language), path + "/language");
        result.Location = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.Location), path + "/location");
        result.SubParts = DecodeOptionalBodyPartList(Values.GetMember(value, JmapProperties.SubParts), path + "/subParts");
        CaptureBodyPartMembers(value, result, BodyPartNames());
        return result;
    }

    internal void EncodeBodyPart(EmailBodyPart value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("An Email body part cannot be null.", path);
        }

        writer.WriteObjectStart();
        writer.WriteStringMember(JmapProperties.PartId, value.PartId, false);
        writer.WriteStringMember(JmapProperties.BlobId, value.BlobId, false);
        if (value.HasSize)
        {
            Values.WriteUnsignedIntegerMember(writer, JmapProperties.Size, value.Size, path + "/size");
        }

        WriteOptionalHeaderList(JmapProperties.Headers, value.Headers, writer, path + "/headers");
        WriteDynamicHeaderMembers(value.HeaderProperties, writer, path);
        writer.WriteStringMember(JmapProperties.Name, value.Name, false);
        writer.WriteStringMember(JmapProperties.Type, value.Type, false);
        writer.WriteStringMember(JmapProperties.Charset, value.Charset, false);
        writer.WriteStringMember(JmapProperties.Disposition, value.Disposition, false);
        writer.WriteStringMember(JmapProperties.Cid, value.Cid, false);
        Optional.WriteOptionalStringArray(JmapProperties.Language, value.Language, writer, path + "/language");
        writer.WriteStringMember(JmapProperties.Location, value.Location, false);
        WriteOptionalBodyPartList(JmapProperties.SubParts, value.SubParts, writer, path + "/subParts");
        Values.WriteUnknownMembers(value.UnknownProperties, BodyPartNames(), writer);
        writer.WriteObjectEnd();
    }

    internal EmailBodyValue DecodeBodyValue(JsonElement value, string path)
    {
        Values.RequireObject(value, path);
        EmailBodyValue result = new EmailBodyValue();
        result.Value = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.Value), path + "/value");
        result.IsEncodingProblem = Values.ReadRequiredBoolean(Values.GetMember(value, JmapProperties.IsEncodingProblem), path + "/isEncodingProblem");
        result.IsTruncated = Values.ReadRequiredBoolean(Values.GetMember(value, JmapProperties.IsTruncated), path + "/isTruncated");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, new List<string> { JmapProperties.Value, JmapProperties.IsEncodingProblem, JmapProperties.IsTruncated });
        return result;
    }

    internal EmailBodyValue DecodeCreateBodyValue(JsonElement value, string path)
    {
        Values.RequireObject(value, path);
        EmailBodyValue result = new EmailBodyValue();
        result.Value = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.Value), path + "/value");
        JsonElement encodingProblem = Values.GetMember(value, JmapProperties.IsEncodingProblem);
        if (encodingProblem.ValueKind != JsonValueKind.Undefined)
        {
            result.IsEncodingProblem = Values.ReadRequiredBoolean(encodingProblem, path + "/isEncodingProblem");
        }

        JsonElement truncated = Values.GetMember(value, JmapProperties.IsTruncated);
        if (truncated.ValueKind != JsonValueKind.Undefined)
        {
            result.IsTruncated = Values.ReadRequiredBoolean(truncated, path + "/isTruncated");
        }

        Values.CaptureUnknownMembers(value, result.UnknownProperties,
            new List<string> { JmapProperties.Value, JmapProperties.IsEncodingProblem, JmapProperties.IsTruncated });
        return result;
    }

    internal void EncodeBodyValue(EmailBodyValue value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("An Email body value cannot be null.", path);
        }

        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.Value, value.Value, path + "/value");
        writer.WriteBooleanMember(JmapProperties.IsEncodingProblem, value.IsEncodingProblem, true);
        writer.WriteBooleanMember(JmapProperties.IsTruncated, value.IsTruncated, true);
        Values.WriteUnknownMembers(value.UnknownProperties, new List<string> { JmapProperties.Value, JmapProperties.IsEncodingProblem, JmapProperties.IsTruncated }, writer);
        writer.WriteObjectEnd();
    }

    internal Dictionary<string, bool> DecodeBooleanMap(JsonElement value, string path)
    {
        Values.RequireObject(value, path);
        Dictionary<string, bool> result = new Dictionary<string, bool>();
        foreach (JsonProperty property in value.EnumerateObject())
        {
            result.Add(property.Name, Values.ReadRequiredBoolean(property.Value, path + "/" + property.Name));
        }

        return result;
    }

    internal void EncodeBooleanMap(Dictionary<string, bool> value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("A boolean map cannot be null.", path);
        }

        writer.WriteObjectStart();
        foreach (string key in Values.SortNames(value.Keys))
        {
            writer.WriteBooleanMember(key, value[key], true);
        }

        writer.WriteObjectEnd();
    }

    internal Dictionary<string, bool>? DecodeOptionalBooleanMap(JsonElement value, string path)
    {
        if (value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return DecodeBooleanMap(value, path);
    }

    internal void WriteOptionalBooleanMap(string name, Dictionary<string, bool>? value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            return;
        }

        writer.WriteMemberName(name);
        EncodeBooleanMap(value, writer, path);
    }

    internal List<EmailAddress> DecodeAddressList(JsonElement value, string path)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new JmapProtocolException("An Email address array is required.", path, Values.CloneContext(value));
        }

        List<EmailAddress> result = new List<EmailAddress>();
        int index = 0;
        foreach (JsonElement item in value.EnumerateArray())
        {
            result.Add(DecodeAddress(item, path + "/" + index));
            index = index + 1;
        }

        return result;
    }

    internal void EncodeAddressList(List<EmailAddress> value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("An Email address array cannot be null.", path);
        }

        writer.WriteArrayStart();
        int index = 0;
        foreach (EmailAddress item in value)
        {
            EncodeAddress(item, writer, path + "/" + index);
            index = index + 1;
        }

        writer.WriteArrayEnd();
    }

    internal List<EmailAddress>? DecodeOptionalAddressList(JsonElement value, string path)
    {
        if (value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return DecodeAddressList(value, path);
    }

    internal void WriteOptionalAddressList(string name, List<EmailAddress>? value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            return;
        }

        writer.WriteMemberName(name);
        EncodeAddressList(value, writer, path);
    }

    internal List<EmailHeader> DecodeHeaderList(JsonElement value, string path)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new JmapProtocolException("An Email header array is required.", path, Values.CloneContext(value));
        }

        List<EmailHeader> result = new List<EmailHeader>();
        int index = 0;
        foreach (JsonElement item in value.EnumerateArray())
        {
            result.Add(DecodeHeader(item, path + "/" + index));
            index = index + 1;
        }

        return result;
    }

    internal void EncodeHeaderList(List<EmailHeader> value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("An Email header array cannot be null.", path);
        }

        writer.WriteArrayStart();
        int index = 0;
        foreach (EmailHeader item in value)
        {
            EncodeHeader(item, writer, path + "/" + index);
            index = index + 1;
        }

        writer.WriteArrayEnd();
    }

    internal List<EmailHeader>? DecodeOptionalHeaderList(JsonElement value, string path)
    {
        if (value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return DecodeHeaderList(value, path);
    }

    internal void WriteOptionalHeaderList(string name, List<EmailHeader>? value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            return;
        }

        writer.WriteMemberName(name);
        EncodeHeaderList(value, writer, path);
    }

    internal List<EmailBodyPart> DecodeBodyPartList(JsonElement value, string path)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new JmapProtocolException("An Email body-part array is required.", path, Values.CloneContext(value));
        }

        List<EmailBodyPart> result = new List<EmailBodyPart>();
        int index = 0;
        foreach (JsonElement item in value.EnumerateArray())
        {
            result.Add(DecodeBodyPart(item, path + "/" + index));
            index = index + 1;
        }

        return result;
    }

    internal void EncodeBodyPartList(List<EmailBodyPart> value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("An Email body-part array cannot be null.", path);
        }

        writer.WriteArrayStart();
        int index = 0;
        foreach (EmailBodyPart item in value)
        {
            EncodeBodyPart(item, writer, path + "/" + index);
            index = index + 1;
        }

        writer.WriteArrayEnd();
    }

    internal List<EmailBodyPart>? DecodeOptionalBodyPartList(JsonElement value, string path)
    {
        if (value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return DecodeBodyPartList(value, path);
    }

    internal void WriteOptionalBodyPartList(string name, List<EmailBodyPart>? value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            return;
        }

        writer.WriteMemberName(name);
        EncodeBodyPartList(value, writer, path);
    }

    internal Dictionary<string, EmailBodyValue>? DecodeOptionalBodyValueMap(JsonElement value, string path)
    {
        if (value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        Values.RequireObject(value, path);
        Dictionary<string, EmailBodyValue> result = new Dictionary<string, EmailBodyValue>();
        foreach (JsonProperty property in value.EnumerateObject())
        {
            result.Add(property.Name, DecodeBodyValue(property.Value, path + "/" + property.Name));
        }

        return result;
    }

    internal Dictionary<string, EmailBodyValue>? DecodeOptionalCreateBodyValueMap(JsonElement value, string path)
    {
        if (value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        Values.RequireObject(value, path);
        Dictionary<string, EmailBodyValue> result = new Dictionary<string, EmailBodyValue>();
        foreach (JsonProperty property in value.EnumerateObject())
        {
            result.Add(property.Name, DecodeCreateBodyValue(property.Value, path + "/" + property.Name));
        }

        return result;
    }

    internal void WriteOptionalBodyValueMap(string name, Dictionary<string, EmailBodyValue>? value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            return;
        }

        writer.WriteMemberName(name);
        writer.WriteObjectStart();
        foreach (string key in Values.SortNames(value.Keys))
        {
            writer.WriteMemberName(key);
            EncodeBodyValue(value[key], writer, path + "/" + key);
        }

        writer.WriteObjectEnd();
    }

    internal Dictionary<string, Email>? DecodeOptionalEmailMap(JsonElement value, string path)
    {
        if (value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        Values.RequireObject(value, path);
        Dictionary<string, Email> result = new Dictionary<string, Email>();
        foreach (JsonProperty property in value.EnumerateObject())
        {
            result.Add(property.Name, DecodeEmail(property.Value, path + "/" + property.Name));
        }

        return result;
    }

    internal Dictionary<string, Email>? DecodeOptionalEmailCreateMap(JsonElement value, string path)
    {
        if (value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        Values.RequireObject(value, path);
        Dictionary<string, Email> result = new Dictionary<string, Email>();
        foreach (JsonProperty property in value.EnumerateObject())
        {
            result.Add(property.Name, DecodeEmailCreate(property.Value, path + "/" + property.Name));
        }

        return result;
    }

    internal void WriteOptionalEmailMap(string name, Dictionary<string, Email>? value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            return;
        }

        writer.WriteMemberName(name);
        writer.WriteObjectStart();
        foreach (string key in Values.SortNames(value.Keys))
        {
            writer.WriteMemberName(key);
            EncodeEmail(value[key], writer, path + "/" + key);
        }

        writer.WriteObjectEnd();
    }

    internal void WriteOptionalEmailCreateMap(string name, Dictionary<string, Email>? value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            return;
        }

        writer.WriteMemberName(name);
        writer.WriteObjectStart();
        foreach (string key in Values.SortNames(value.Keys))
        {
            writer.WriteMemberName(key);
            EncodeEmailCreate(value[key], writer, path + "/" + key);
        }

        writer.WriteObjectEnd();
    }

    internal void WriteOptionalEmailSetCreatedMap(string name, Dictionary<string, Email>? value,
        JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            return;
        }

        writer.WriteMemberName(name);
        writer.WriteObjectStart();
        foreach (string key in Values.SortNames(value.Keys))
        {
            writer.WriteMemberName(key);
            EncodeEmailSetCreated(value[key], writer, path + "/" + key);
        }

        writer.WriteObjectEnd();
    }

    internal void EncodeEmailCopyMap(Dictionary<string, Email> value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("An Email copy map cannot be null.", path);
        }

        writer.WriteObjectStart();
        foreach (string key in Values.SortNames(value.Keys))
        {
            writer.WriteMemberName(key);
            EncodeEmailCopy(value[key], writer, path + "/" + key);
        }

        writer.WriteObjectEnd();
    }

    internal Dictionary<string, Email> DecodeEmailMap(JsonElement value, string path)
    {
        Values.RequireObject(value, path);
        Dictionary<string, Email> result = new Dictionary<string, Email>();
        foreach (JsonProperty property in value.EnumerateObject())
        {
            result.Add(property.Name, DecodeEmail(property.Value, path + "/" + property.Name));
        }

        return result;
    }

    internal JmapHeaderForm ClassifyHeaderForm(string value)
    {
        switch (value)
        {
            case "asRaw":
            case ":asRaw":
                return JmapHeaderForm.Raw;
            case "asText":
            case ":asText":
                return JmapHeaderForm.Text;
            case "asAddresses":
            case ":asAddresses":
                return JmapHeaderForm.Addresses;
            case "asGroupedAddresses":
            case ":asGroupedAddresses":
                return JmapHeaderForm.GroupedAddresses;
            case "asMessageIds":
            case ":asMessageIds":
                return JmapHeaderForm.MessageIds;
            case "asDate":
            case ":asDate":
                return JmapHeaderForm.Date;
            case "asURLs":
            case ":asURLs":
                return JmapHeaderForm.Urls;
            default:
                throw new JmapValidationException("The Email header form is not recognized.", new List<string> { value }, default(JsonElement));
        }
    }

    internal bool IsDynamicHeaderProperty(string name)
    {
        return name.StartsWith(JmapProperties.HeaderPrefix, System.StringComparison.Ordinal);
    }

    internal void CaptureEmailMembers(JsonElement source, Email destination, List<string> knownNames)
    {
        foreach (JsonProperty property in source.EnumerateObject())
        {
            if (knownNames.Contains(property.Name))
            {
                continue;
            }

            if (IsDynamicHeaderProperty(property.Name))
            {
                destination.HeaderProperties.Add(property.Name, Reader.CloneUnknownValue(property.Value));
            }
            else
            {
                destination.UnknownProperties.Add(property.Name, Reader.CloneUnknownValue(property.Value));
            }
        }
    }

    internal void CaptureBodyPartMembers(JsonElement source, EmailBodyPart destination, List<string> knownNames)
    {
        foreach (JsonProperty property in source.EnumerateObject())
        {
            if (knownNames.Contains(property.Name))
            {
                continue;
            }

            if (IsDynamicHeaderProperty(property.Name))
            {
                destination.HeaderProperties.Add(property.Name, Reader.CloneUnknownValue(property.Value));
            }
            else
            {
                destination.UnknownProperties.Add(property.Name, Reader.CloneUnknownValue(property.Value));
            }
        }
    }

    internal void WriteDynamicHeaderMembers(Dictionary<string, JsonElement> properties, JmapJsonWriter writer, string path)
    {
        if (properties == null)
        {
            throw Standard.Validation("Dynamic Email header property storage cannot be null.", path);
        }

        foreach (string key in Values.SortNames(properties.Keys))
        {
            if (!IsDynamicHeaderProperty(key))
            {
                throw Standard.Validation("A dynamic Email header property must start with header:.", path + "/" + key);
            }

            writer.WriteElementMember(key, properties[key]);
        }
    }

    internal void RequireEmail(Email? value, JmapJsonWriter? writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("An Email cannot be null.", path);
        }

        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }
    }

    internal List<string> EmailNames()
    {
        return new List<string>
        {
            JmapProperties.Id, JmapProperties.BlobId, JmapProperties.ThreadId, JmapProperties.MailboxIds,
            JmapProperties.Keywords, JmapProperties.Size, JmapProperties.ReceivedAt, JmapProperties.Headers,
            JmapProperties.MessageId, JmapProperties.InReplyTo, JmapProperties.References, JmapProperties.Sender,
            JmapProperties.From, JmapProperties.To, JmapProperties.Cc, JmapProperties.Bcc, JmapProperties.ReplyTo,
            JmapProperties.Subject, JmapProperties.SentAt, JmapProperties.BodyStructure, JmapProperties.BodyValues,
            JmapProperties.TextBody, JmapProperties.HtmlBody, JmapProperties.Attachments, JmapProperties.HasAttachment,
            JmapProperties.Preview
        };
    }

    internal List<string> BodyPartNames()
    {
        return new List<string>
        {
            JmapProperties.PartId, JmapProperties.BlobId, JmapProperties.Size, JmapProperties.Headers,
            JmapProperties.Name, JmapProperties.Type, JmapProperties.Charset, JmapProperties.Disposition,
            JmapProperties.Cid, JmapProperties.Language, JmapProperties.Location, JmapProperties.SubParts
        };
    }
}
