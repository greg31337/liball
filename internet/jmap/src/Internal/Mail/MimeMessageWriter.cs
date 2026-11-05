using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace jmap;

internal class MimeWriteHeader
{
    internal string Name = "";
    internal string Value = "";
    internal string Path = "";
}

internal class MimeWriteResult
{
    internal byte[] Message = new byte[0];
    internal List<string> MissingBlobIds = new List<string>();
}

internal class MimeMessageWriter
{
    internal EmailValidator Validator = new EmailValidator();
    internal HeaderPropertySelector HeaderProperties = new HeaderPropertySelector();
    internal MimeHeaderValueParser MimeValues = new MimeHeaderValueParser();
    internal Encoding Utf8 = new UTF8Encoding(false, false);
    internal string MessageIdDomain = "localhost";
    internal string? GeneratedMessageId;
    internal string? GeneratedDate;

    internal MimeWriteResult WriteMessage(Email? email, Dictionary<string, byte[]>? blobs)
    {
        List<string> invalid = Validator.ValidateCreate(email);
        if (email != null)
        {
            ValidateWriterValues(email, invalid);
        }

        if (invalid.Count != 0 || email == null)
        {
            throw new JmapValidationException("The Email cannot be represented as a MIME message.", invalid,
                default(JsonElement));
        }

        EmailBodyPart root = EffectiveRoot(email);
        MimeWriteResult result = new MimeWriteResult();
        CollectMissingBlobs(root, blobs, result.MissingBlobIds);
        if (result.MissingBlobIds.Count != 0)
        {
            return result;
        }

        List<MimeWriteHeader> headers = MessageHeaders(email);
        if (!ContainsHeader(headers, "Message-ID"))
        {
            string messageId = GeneratedMessageId == null ? Guid.NewGuid().ToString("N") + "@" + MessageIdDomain :
                GeneratedMessageId;
            AddHeader(headers, "Message-ID", "<" + messageId + ">", "messageId");
        }

        if (!ContainsHeader(headers, "Date"))
        {
            string date = GeneratedDate == null ? FormatDate(DateTimeOffset.UtcNow) : GeneratedDate;
            AddHeader(headers, "Date", date, "sentAt");
        }

        if (!ContainsHeader(headers, "MIME-Version"))
        {
            AddHeader(headers, "MIME-Version", "1.0", "header:MIME-Version");
        }

        List<byte> output = new List<byte>();
        WritePart(root, true, headers, email.BodyValues, blobs, output, "bodyStructure");
        result.Message = output.ToArray();
        return result;
    }

    internal EmailBodyPart EffectiveRoot(Email email)
    {
        if (email.BodyStructure != null)
        {
            return email.BodyStructure;
        }

        EmailBodyPart body;
        if (email.TextBody != null && email.TextBody.Count != 0 && email.HtmlBody != null && email.HtmlBody.Count != 0)
        {
            body = MultipartPart("multipart/alternative");
            AddSubPart(body, email.TextBody[0]);
            AddSubPart(body, email.HtmlBody[0]);
        }
        else if (email.TextBody != null && email.TextBody.Count != 0)
        {
            body = email.TextBody[0];
        }
        else if (email.HtmlBody != null && email.HtmlBody.Count != 0)
        {
            body = email.HtmlBody[0];
        }
        else
        {
            body = new EmailBodyPart();
            body.Type = "text/plain";
        }

        if (email.Attachments == null || email.Attachments.Count == 0)
        {
            return body;
        }

        EmailBodyPart mixed = MultipartPart("multipart/mixed");
        AddSubPart(mixed, body);
        foreach (EmailBodyPart attachment in email.Attachments)
        {
            AddSubPart(mixed, attachment);
        }

        return mixed;
    }

    internal EmailBodyPart MultipartPart(string type)
    {
        EmailBodyPart result = new EmailBodyPart();
        result.Type = type;
        result.SubParts = new List<EmailBodyPart>();
        return result;
    }

    internal void AddSubPart(EmailBodyPart parent, EmailBodyPart child)
    {
        if (parent.SubParts == null)
        {
            parent.SubParts = new List<EmailBodyPart>();
        }

        parent.SubParts.Add(child);
    }

    internal void WritePart(EmailBodyPart part, bool root, List<MimeWriteHeader> messageHeaders,
        Dictionary<string, EmailBodyValue>? bodyValues, Dictionary<string, byte[]>? blobs, List<byte> output,
        string path)
    {
        List<MimeWriteHeader> headers = root ? messageHeaders : new List<MimeWriteHeader>();
        bool multipart = part.Type != null && part.Type.StartsWith("multipart/", StringComparison.Ordinal);
        string? boundary = multipart ? NewBoundary() : null;
        AddPartHeaders(part, boundary, headers, path);
        WriteHeaders(headers, output);
        if (multipart)
        {
            if (part.SubParts != null)
            {
                int index = 0;
                while (index < part.SubParts.Count)
                {
                    WriteAscii("--" + boundary + "\r\n", output);
                    WritePart(part.SubParts[index], false, messageHeaders, bodyValues, blobs, output,
                        path + "/subParts/" + index.ToString(CultureInfo.InvariantCulture));
                    index = index + 1;
                }
            }

            WriteAscii("--" + boundary + "--\r\n", output);
        }
        else
        {
            byte[] content = PartContent(part, bodyValues, blobs);
            WriteBase64(content, output);
        }
    }

    internal void AddPartHeaders(EmailBodyPart part, string? boundary, List<MimeWriteHeader> headers, string path)
    {
        string type = part.Type == null ? "application/octet-stream" : part.Type;
        StringBuilder contentType = new StringBuilder(type);
        if (boundary != null)
        {
            contentType.Append("; boundary=\"");
            contentType.Append(boundary);
            contentType.Append('"');
        }
        else if (type.StartsWith("text/", StringComparison.Ordinal))
        {
            string charset = part.PartId == null ? (part.Charset == null ? "us-ascii" : part.Charset) : "utf-8";
            contentType.Append("; charset=");
            contentType.Append(charset);
        }

        AddHeader(headers, "Content-Type", contentType.ToString(), path + "/type");
        if (part.Disposition != null || part.Name != null)
        {
            string disposition = part.Disposition == null ? "attachment" : part.Disposition;
            if (part.Name != null)
            {
                disposition = disposition + "; filename*=utf-8''" + EncodeRfc2231(part.Name);
            }

            AddHeader(headers, "Content-Disposition", disposition, path + "/disposition");
        }

        if (part.Cid != null)
        {
            AddHeader(headers, "Content-ID", "<" + part.Cid + ">", path + "/cid");
        }

        if (part.Language != null)
        {
            AddHeader(headers, "Content-Language", JoinStrings(part.Language, ", "), path + "/language");
        }

        if (part.Location != null)
        {
            AddHeader(headers, "Content-Location", part.Location, path + "/location");
        }

        AddDynamicHeaders(part.HeaderProperties, headers, path);
        if (boundary == null)
        {
            AddHeader(headers, "Content-Transfer-Encoding", "base64", path + "/type");
        }
    }

    internal List<MimeWriteHeader> MessageHeaders(Email email)
    {
        List<MimeWriteHeader> result = new List<MimeWriteHeader>();
        AddMessageIdHeader(email.MessageId, "Message-ID", "messageId", result);
        AddMessageIdHeader(email.InReplyTo, "In-Reply-To", "inReplyTo", result);
        AddMessageIdHeader(email.References, "References", "references", result);
        AddAddressHeader(email.Sender, "Sender", "sender", result);
        AddAddressHeader(email.From, "From", "from", result);
        AddAddressHeader(email.To, "To", "to", result);
        AddAddressHeader(email.Cc, "Cc", "cc", result);
        AddAddressHeader(email.Bcc, "Bcc", "bcc", result);
        AddAddressHeader(email.ReplyTo, "Reply-To", "replyTo", result);
        if (email.Subject != null)
        {
            AddHeader(result, "Subject", email.Subject, "subject");
        }

        if (email.SentAt != null)
        {
            AddHeader(result, "Date", FormatDate(DateTimeOffset.Parse(email.SentAt, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind)), "sentAt");
        }

        AddDynamicHeaders(email.HeaderProperties, result, "");
        return result;
    }

    internal void AddMessageIdHeader(List<string>? values, string name, string path, List<MimeWriteHeader> headers)
    {
        if (values == null)
        {
            return;
        }

        StringBuilder value = new StringBuilder();
        foreach (string item in values)
        {
            if (value.Length != 0)
            {
                value.Append(' ');
            }

            value.Append('<');
            value.Append(item);
            value.Append('>');
        }

        AddHeader(headers, name, value.ToString(), path);
    }

    internal void AddAddressHeader(List<EmailAddress>? values, string name, string path, List<MimeWriteHeader> headers)
    {
        if (values != null)
        {
            AddHeader(headers, name, FormatAddresses(values), path);
        }
    }

    internal void AddDynamicHeaders(Dictionary<string, JsonElement>? properties, List<MimeWriteHeader> headers,
        string parentPath)
    {
        if (properties == null)
        {
            return;
        }

        foreach (KeyValuePair<string, JsonElement> property in properties)
        {
            HeaderSelection? selection = HeaderProperties.ParsePropertyForValidation(property.Key);
            if (selection == null || selection.HeaderName == null)
            {
                continue;
            }

            string path = parentPath.Length == 0 ? property.Key : parentPath + "/" + property.Key;
            if (selection.IsAll)
            {
                foreach (JsonElement item in property.Value.EnumerateArray())
                {
                    AddHeader(headers, selection.HeaderName, FormatDynamicHeader(selection.Form, item), path);
                }
            }
            else
            {
                AddHeader(headers, selection.HeaderName, FormatDynamicHeader(selection.Form, property.Value), path);
            }
        }
    }

    internal string FormatDynamicHeader(JmapHeaderForm form, JsonElement value)
    {
        if (form == JmapHeaderForm.Raw || form == JmapHeaderForm.Text)
        {
            string? text = value.GetString();
            return text == null ? "" : text;
        }

        if (form == JmapHeaderForm.Addresses)
        {
            return FormatAddressJson(value);
        }

        if (form == JmapHeaderForm.GroupedAddresses)
        {
            return FormatGroupedAddressJson(value);
        }

        if (form == JmapHeaderForm.MessageIds)
        {
            return FormatMessageIdJson(value);
        }

        if (form == JmapHeaderForm.Date)
        {
            if (value.ValueKind == JsonValueKind.Null)
            {
                return "";
            }

            string? date = value.GetString();
            return FormatDate(DateTimeOffset.Parse(date == null ? "" : date,
                CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
        }

        return FormatUrlJson(value);
    }

    internal string FormatAddresses(List<EmailAddress> addresses)
    {
        StringBuilder result = new StringBuilder();
        foreach (EmailAddress address in addresses)
        {
            if (result.Length != 0)
            {
                result.Append(", ");
            }

            result.Append(FormatMailbox(address.Name, address.Email == null ? "" : address.Email));
        }

        return result.ToString();
    }

    internal string FormatAddressJson(JsonElement value)
    {
        StringBuilder result = new StringBuilder();
        foreach (JsonElement item in value.EnumerateArray())
        {
            if (result.Length != 0)
            {
                result.Append(", ");
            }

            JsonElement name = FindJsonMember(item, "name");
            JsonElement email = FindJsonMember(item, "email");
            string? displayName = name.ValueKind == JsonValueKind.String ? name.GetString() : null;
            string? address = email.GetString();
            result.Append(FormatMailbox(displayName, address == null ? "" : address));
        }

        return result.ToString();
    }

    internal string FormatGroupedAddressJson(JsonElement value)
    {
        StringBuilder result = new StringBuilder();
        foreach (JsonElement group in value.EnumerateArray())
        {
            if (result.Length != 0)
            {
                result.Append(", ");
            }

            JsonElement name = FindJsonMember(group, "name");
            JsonElement addresses = FindJsonMember(group, "addresses");
            string members = FormatAddressJson(addresses);
            if (name.ValueKind == JsonValueKind.String)
            {
                string? groupName = name.GetString();
                result.Append(QuotePhrase(groupName == null ? "" : groupName));
                result.Append(": ");
                result.Append(members);
                result.Append(';');
            }
            else
            {
                result.Append(members);
            }
        }

        return result.ToString();
    }

    internal string FormatMessageIdJson(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Null)
        {
            return "";
        }

        StringBuilder result = new StringBuilder();
        foreach (JsonElement item in value.EnumerateArray())
        {
            if (result.Length != 0)
            {
                result.Append(' ');
            }

            result.Append('<');
            result.Append(item.GetString());
            result.Append('>');
        }

        return result.ToString();
    }

    internal string FormatUrlJson(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Null)
        {
            return "";
        }

        StringBuilder result = new StringBuilder();
        foreach (JsonElement item in value.EnumerateArray())
        {
            if (result.Length != 0)
            {
                result.Append(", ");
            }

            result.Append('<');
            result.Append(item.GetString());
            result.Append('>');
        }

        return result.ToString();
    }

    internal string FormatMailbox(string? name, string email)
    {
        if (name == null)
        {
            return email;
        }

        return QuotePhrase(name) + " <" + email + ">";
    }

    internal string QuotePhrase(string value)
    {
        StringBuilder result = new StringBuilder();
        result.Append('"');
        foreach (char character in value)
        {
            if (character == '\\' || character == '"')
            {
                result.Append('\\');
            }

            result.Append(character);
        }

        result.Append('"');
        return result.ToString();
    }

    internal JsonElement FindJsonMember(JsonElement value, string name)
    {
        foreach (JsonProperty property in value.EnumerateObject())
        {
            if (property.Name == name)
            {
                return property.Value;
            }
        }

        return default(JsonElement);
    }

    internal byte[] PartContent(EmailBodyPart part, Dictionary<string, EmailBodyValue>? bodyValues,
        Dictionary<string, byte[]>? blobs)
    {
        if (part.PartId != null && bodyValues != null && bodyValues.ContainsKey(part.PartId))
        {
            EmailBodyValue bodyValue = bodyValues[part.PartId];
            string value = bodyValue.Value == null ? "" : bodyValue.Value;
            if (part.Type != null && part.Type.StartsWith("text/", StringComparison.Ordinal))
            {
                value = NormalizeTextLineEndings(value);
            }

            return Utf8.GetBytes(value);
        }

        if (part.BlobId != null && blobs != null && blobs.ContainsKey(part.BlobId))
        {
            return blobs[part.BlobId];
        }

        return new byte[0];
    }

    internal void CollectMissingBlobs(EmailBodyPart part, Dictionary<string, byte[]>? blobs, List<string> missing)
    {
        if (part.BlobId != null && (blobs == null || !blobs.ContainsKey(part.BlobId)) && !missing.Contains(part.BlobId))
        {
            missing.Add(part.BlobId);
        }

        if (part.SubParts != null)
        {
            foreach (EmailBodyPart child in part.SubParts)
            {
                CollectMissingBlobs(child, blobs, missing);
            }
        }
    }

    internal void WriteHeaders(List<MimeWriteHeader> headers, List<byte> output)
    {
        foreach (MimeWriteHeader header in headers)
        {
            if (!IsSafeHeaderValue(header.Value))
            {
                throw new JmapValidationException("A header value contains an unsafe line break or control character.",
                    new List<string> { header.Path }, default(JsonElement));
            }

            WriteUtf8(header.Name + ": " + header.Value + "\r\n", output);
        }

        WriteAscii("\r\n", output);
    }

    internal void WriteBase64(byte[] content, List<byte> output)
    {
        string encoded = Convert.ToBase64String(content);
        int offset = 0;
        while (offset < encoded.Length)
        {
            int length = encoded.Length - offset > 76 ? 76 : encoded.Length - offset;
            WriteAscii(encoded.Substring(offset, length), output);
            WriteAscii("\r\n", output);
            offset = offset + length;
        }
    }

    internal void WriteAscii(string value, List<byte> output)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(value);
        foreach (byte item in bytes)
        {
            output.Add(item);
        }
    }

    internal void WriteUtf8(string value, List<byte> output)
    {
        byte[] bytes = Utf8.GetBytes(value);
        foreach (byte item in bytes)
        {
            output.Add(item);
        }
    }

    internal void AddHeader(List<MimeWriteHeader> headers, string name, string value, string path)
    {
        MimeWriteHeader header = new MimeWriteHeader();
        header.Name = name;
        header.Value = value;
        header.Path = path;
        headers.Add(header);
    }

    internal bool ContainsHeader(List<MimeWriteHeader> headers, string name)
    {
        foreach (MimeWriteHeader header in headers)
        {
            if (string.Equals(header.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    internal string NewBoundary()
    {
        return "=_jmap_" + Guid.NewGuid().ToString("N");
    }

    internal string EncodeRfc2231(string value)
    {
        byte[] bytes = Utf8.GetBytes(value);
        StringBuilder result = new StringBuilder();
        foreach (byte item in bytes)
        {
            if (IsRfc2231AttributeByte(item))
            {
                result.Append((char)item);
            }
            else
            {
                result.Append('%');
                result.Append(item.ToString("X2", CultureInfo.InvariantCulture));
            }
        }

        return result.ToString();
    }

    internal bool IsRfc2231AttributeByte(byte value)
    {
        return value >= 65 && value <= 90 || value >= 97 && value <= 122 || value >= 48 && value <= 57 ||
            value == 33 || value == 35 || value == 36 || value == 38 || value == 43 || value == 45 || value == 46 ||
            value == 94 || value == 95 || value == 96 || value == 124 || value == 126;
    }

    internal string NormalizeTextLineEndings(string value)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '\r')
            {
                result.Append("\r\n");
                if (index + 1 < value.Length && value[index + 1] == '\n')
                {
                    index = index + 2;
                }
                else
                {
                    index = index + 1;
                }
            }
            else if (value[index] == '\n')
            {
                result.Append("\r\n");
                index = index + 1;
            }
            else
            {
                result.Append(value[index]);
                index = index + 1;
            }
        }

        return result.ToString();
    }

    internal string JoinStrings(List<string> values, string separator)
    {
        StringBuilder result = new StringBuilder();
        foreach (string value in values)
        {
            if (result.Length != 0)
            {
                result.Append(separator);
            }

            result.Append(value);
        }

        return result.ToString();
    }

    internal string FormatDate(DateTimeOffset value)
    {
        string formatted = value.ToString("ddd, dd MMM yyyy HH:mm:ss zzz", CultureInfo.InvariantCulture);
        return formatted.Substring(0, formatted.Length - 3) + formatted.Substring(formatted.Length - 2);
    }

    internal bool IsSafeHeaderValue(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '\r')
            {
                if (index + 2 >= value.Length || value[index + 1] != '\n' ||
                    (value[index + 2] != ' ' && value[index + 2] != '\t'))
                {
                    return false;
                }

                index = index + 2;
            }
            else if (character == '\n' || character < 32 && character != '\t' || character == 127)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal void ValidateWriterValues(Email email, List<string> invalid)
    {
        if (!IsSafeMessageIdDomain(MessageIdDomain))
        {
            invalid.Add("messageIdDomain");
        }

        if (email.BodyValues != null)
        {
            foreach (KeyValuePair<string, EmailBodyValue> item in email.BodyValues)
            {
                if (item.Value != null && !Validator.Values.IsValidUnicode(item.Value.Value))
                {
                    invalid.Add("bodyValues/" + item.Key + "/value");
                }
            }
        }

        EmailBodyPart? root = email.BodyStructure;
        if (root != null)
        {
            ValidatePartShape(root, "bodyStructure", invalid);
        }

        ValidatePartShapeList(email.TextBody, "textBody", invalid);
        ValidatePartShapeList(email.HtmlBody, "htmlBody", invalid);
        ValidatePartShapeList(email.Attachments, "attachments", invalid);
    }

    internal void ValidatePartShapeList(List<EmailBodyPart>? parts, string path, List<string> invalid)
    {
        if (parts == null)
        {
            return;
        }

        int index = 0;
        while (index < parts.Count)
        {
            if (parts[index] != null)
            {
                ValidatePartShape(parts[index], path + "/" + index.ToString(CultureInfo.InvariantCulture), invalid);
            }

            index = index + 1;
        }
    }

    internal void ValidatePartShape(EmailBodyPart part, string path, List<string> invalid)
    {
        bool multipart = part.Type != null && part.Type.StartsWith("multipart/", StringComparison.Ordinal);
        if (multipart && (part.PartId != null || part.BlobId != null))
        {
            invalid.Add(path);
        }

        if (!multipart && part.SubParts != null)
        {
            invalid.Add(path + "/subParts");
        }

        if (part.Charset != null && !MimeValues.IsMimeToken(part.Charset))
        {
            invalid.Add(path + "/charset");
        }

        if (part.Disposition != null && !MimeValues.IsMimeToken(part.Disposition))
        {
            invalid.Add(path + "/disposition");
        }

        if (part.Name != null && !Validator.Values.IsValidUnicode(part.Name))
        {
            invalid.Add(path + "/name");
        }

        if (part.Language != null)
        {
            MimeMessageReader reader = new MimeMessageReader();
            int index = 0;
            while (index < part.Language.Count)
            {
                if (!reader.IsLanguageTag(part.Language[index]))
                {
                    invalid.Add(path + "/language/" + index.ToString(CultureInfo.InvariantCulture));
                }

                index = index + 1;
            }
        }

        if (part.SubParts != null)
        {
            int index = 0;
            while (index < part.SubParts.Count)
            {
                if (part.SubParts[index] != null)
                {
                    ValidatePartShape(part.SubParts[index], path + "/subParts/" + index.ToString(CultureInfo.InvariantCulture),
                        invalid);
                }

                index = index + 1;
            }
        }
    }

    internal bool IsSafeMessageIdDomain(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        foreach (char character in value)
        {
            bool alphaNumeric = character >= 'A' && character <= 'Z' || character >= 'a' && character <= 'z' ||
                character >= '0' && character <= '9';
            if (!alphaNumeric && character != '.' && character != '-')
            {
                return false;
            }
        }

        return true;
    }
}
