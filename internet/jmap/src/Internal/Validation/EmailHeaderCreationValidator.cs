using System.Collections.Generic;
using System.Text.Json;

namespace jmap;

internal class EmailHeaderOccurrence
{
    internal string? HeaderName;
    internal string? Path;
}

internal class EmailHeaderCreationValidator
{
    internal HeaderPropertySelector Headers = new HeaderPropertySelector();
    internal StructuredHeaderValueParser Structured = new StructuredHeaderValueParser();
    internal ValueValidator Values = new ValueValidator();

    internal void ValidateEmailHeaders(Email email, List<string> invalid)
    {
        ValidateMessageIdValues(email.MessageId, "messageId", invalid);
        ValidateMessageIdValues(email.InReplyTo, "inReplyTo", invalid);
        ValidateMessageIdValues(email.References, "references", invalid);
        ValidateAddressValues(email.Sender, "sender", invalid);
        ValidateAddressValues(email.From, "from", invalid);
        ValidateAddressValues(email.To, "to", invalid);
        ValidateAddressValues(email.Cc, "cc", invalid);
        ValidateAddressValues(email.Bcc, "bcc", invalid);
        ValidateAddressValues(email.ReplyTo, "replyTo", invalid);
        if (email.Subject != null && !Values.IsValidUnicode(email.Subject))
        {
            invalid.Add("subject");
        }

        if (email.SentAt != null && !Values.IsDate(email.SentAt))
        {
            invalid.Add("sentAt");
        }

        if (email.HeaderProperties == null)
        {
            return;
        }

        Dictionary<string, string> seen = new Dictionary<string, string>(global::System.StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, JsonElement> property in email.HeaderProperties)
        {
            HeaderSelection? selection = Headers.ParsePropertyForValidation(property.Key);
            if (selection == null || selection.HeaderName == null)
            {
                invalid.Add(property.Key);
                continue;
            }

            if (!IsDynamicHeaderValue(selection, property.Value))
            {
                invalid.Add(property.Key);
            }

            if (selection.HeaderName.StartsWith("Content-", global::System.StringComparison.OrdinalIgnoreCase))
            {
                invalid.Add(property.Key);
            }

            if (seen.ContainsKey(selection.HeaderName))
            {
                invalid.Add(seen[selection.HeaderName]);
                invalid.Add(property.Key);
            }
            else
            {
                seen.Add(selection.HeaderName, property.Key);
            }

            string? convenience = FindEmailConveniencePath(email, selection.HeaderName);
            if (convenience != null)
            {
                invalid.Add(convenience);
                invalid.Add(property.Key);
            }
        }
    }

    internal void ValidateBodyPartHeaders(EmailBodyPart part, string path, List<string> invalid)
    {
        if (part.Cid != null && !Structured.IsMessageIdValue(part.Cid))
        {
            invalid.Add(path + "/cid");
        }

        if (part.Location != null && Structured.ParseContentLocation(part.Location) == null)
        {
            invalid.Add(path + "/location");
        }

        if (part.HeaderProperties == null)
        {
            return;
        }

        Dictionary<string, string> seen = new Dictionary<string, string>(global::System.StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, JsonElement> property in part.HeaderProperties)
        {
            string propertyPath = path + "/" + property.Key;
            HeaderSelection? selection = Headers.ParsePropertyForValidation(property.Key);
            if (selection == null || selection.HeaderName == null)
            {
                invalid.Add(propertyPath);
                continue;
            }

            if (!IsDynamicHeaderValue(selection, property.Value))
            {
                invalid.Add(propertyPath);
            }

            if (Headers.EqualsHeader(selection.HeaderName, "Content-Transfer-Encoding"))
            {
                invalid.Add(propertyPath);
            }

            if (seen.ContainsKey(selection.HeaderName))
            {
                invalid.Add(seen[selection.HeaderName]);
                invalid.Add(propertyPath);
            }
            else
            {
                seen.Add(selection.HeaderName, propertyPath);
            }

            AddBodyConvenienceConflicts(part, path, selection.HeaderName, propertyPath, invalid);
        }
    }

    internal void ValidateRootHeaderConflicts(Email email, List<string> invalid)
    {
        if (email.BodyStructure == null)
        {
            return;
        }

        List<EmailHeaderOccurrence> emailHeaders = CollectEmailHeaderOccurrences(email);
        List<EmailHeaderOccurrence> bodyHeaders = CollectBodyHeaderOccurrences(email.BodyStructure, "bodyStructure");
        foreach (EmailHeaderOccurrence emailHeader in emailHeaders)
        {
            foreach (EmailHeaderOccurrence bodyHeader in bodyHeaders)
            {
                if (emailHeader.HeaderName != null && bodyHeader.HeaderName != null && emailHeader.Path != null &&
                    bodyHeader.Path != null && Headers.EqualsHeader(emailHeader.HeaderName, bodyHeader.HeaderName))
                {
                    invalid.Add(emailHeader.Path);
                    invalid.Add(bodyHeader.Path);
                }
            }
        }
    }

    internal List<EmailHeaderOccurrence> CollectEmailHeaderOccurrences(Email email)
    {
        List<EmailHeaderOccurrence> result = new List<EmailHeaderOccurrence>();
        AddOccurrenceIfPresent(email.MessageId, "Message-ID", "messageId", result);
        AddOccurrenceIfPresent(email.InReplyTo, "In-Reply-To", "inReplyTo", result);
        AddOccurrenceIfPresent(email.References, "References", "references", result);
        AddOccurrenceIfPresent(email.Sender, "Sender", "sender", result);
        AddOccurrenceIfPresent(email.From, "From", "from", result);
        AddOccurrenceIfPresent(email.To, "To", "to", result);
        AddOccurrenceIfPresent(email.Cc, "Cc", "cc", result);
        AddOccurrenceIfPresent(email.Bcc, "Bcc", "bcc", result);
        AddOccurrenceIfPresent(email.ReplyTo, "Reply-To", "replyTo", result);
        AddOccurrenceIfPresent(email.Subject, "Subject", "subject", result);
        AddOccurrenceIfPresent(email.SentAt, "Date", "sentAt", result);
        if (email.HeaderProperties != null)
        {
            foreach (string property in email.HeaderProperties.Keys)
            {
                HeaderSelection? selection = Headers.ParsePropertyForValidation(property);
                if (selection != null && selection.HeaderName != null)
                {
                    AddHeaderOccurrence(selection.HeaderName, property, result);
                }
            }
        }

        return result;
    }

    internal List<EmailHeaderOccurrence> CollectBodyHeaderOccurrences(EmailBodyPart part, string path)
    {
        List<EmailHeaderOccurrence> result = new List<EmailHeaderOccurrence>();
        AddOccurrenceIfPresent(part.Type, "Content-Type", path + "/type", result);
        AddOccurrenceIfPresent(part.Charset, "Content-Type", path + "/charset", result);
        if (part.Name != null)
        {
            AddHeaderOccurrence("Content-Type", path + "/name", result);
            AddHeaderOccurrence("Content-Disposition", path + "/name", result);
        }

        AddOccurrenceIfPresent(part.Disposition, "Content-Disposition", path + "/disposition", result);
        AddOccurrenceIfPresent(part.Cid, "Content-ID", path + "/cid", result);
        AddOccurrenceIfPresent(part.Language, "Content-Language", path + "/language", result);
        AddOccurrenceIfPresent(part.Location, "Content-Location", path + "/location", result);
        if (part.HeaderProperties != null)
        {
            foreach (string property in part.HeaderProperties.Keys)
            {
                HeaderSelection? selection = Headers.ParsePropertyForValidation(property);
                if (selection != null && selection.HeaderName != null)
                {
                    AddHeaderOccurrence(selection.HeaderName, path + "/" + property, result);
                }
            }
        }

        return result;
    }

    internal void AddBodyConvenienceConflicts(EmailBodyPart part, string path, string headerName, string propertyPath,
        List<string> invalid)
    {
        if (Headers.EqualsHeader(headerName, "Content-Type"))
        {
            AddConflictPath(part.Type, path + "/type", propertyPath, invalid);
            AddConflictPath(part.Charset, path + "/charset", propertyPath, invalid);
            AddConflictPath(part.Name, path + "/name", propertyPath, invalid);
        }
        else if (Headers.EqualsHeader(headerName, "Content-Disposition"))
        {
            AddConflictPath(part.Disposition, path + "/disposition", propertyPath, invalid);
            AddConflictPath(part.Name, path + "/name", propertyPath, invalid);
        }
        else if (Headers.EqualsHeader(headerName, "Content-ID"))
        {
            AddConflictPath(part.Cid, path + "/cid", propertyPath, invalid);
        }
        else if (Headers.EqualsHeader(headerName, "Content-Language"))
        {
            AddConflictPath(part.Language, path + "/language", propertyPath, invalid);
        }
        else if (Headers.EqualsHeader(headerName, "Content-Location"))
        {
            AddConflictPath(part.Location, path + "/location", propertyPath, invalid);
        }
    }

    internal void AddConflictPath(object? value, string conveniencePath, string propertyPath, List<string> invalid)
    {
        if (value != null)
        {
            invalid.Add(conveniencePath);
            invalid.Add(propertyPath);
        }
    }

    internal string? FindEmailConveniencePath(Email email, string headerName)
    {
        if (Headers.EqualsHeader(headerName, "Message-ID") && email.MessageId != null)
        {
            return "messageId";
        }

        if (Headers.EqualsHeader(headerName, "In-Reply-To") && email.InReplyTo != null)
        {
            return "inReplyTo";
        }

        if (Headers.EqualsHeader(headerName, "References") && email.References != null)
        {
            return "references";
        }

        if (Headers.EqualsHeader(headerName, "Sender") && email.Sender != null)
        {
            return "sender";
        }

        if (Headers.EqualsHeader(headerName, "From") && email.From != null)
        {
            return "from";
        }

        if (Headers.EqualsHeader(headerName, "To") && email.To != null)
        {
            return "to";
        }

        if (Headers.EqualsHeader(headerName, "Cc") && email.Cc != null)
        {
            return "cc";
        }

        if (Headers.EqualsHeader(headerName, "Bcc") && email.Bcc != null)
        {
            return "bcc";
        }

        if (Headers.EqualsHeader(headerName, "Reply-To") && email.ReplyTo != null)
        {
            return "replyTo";
        }

        if (Headers.EqualsHeader(headerName, "Subject") && email.Subject != null)
        {
            return "subject";
        }

        if (Headers.EqualsHeader(headerName, "Date") && email.SentAt != null)
        {
            return "sentAt";
        }

        return null;
    }

    internal void ValidateMessageIdValues(List<string>? values, string path, List<string> invalid)
    {
        if (values == null)
        {
            return;
        }

        int index = 0;
        foreach (string value in values)
        {
            if (value == null || !Structured.IsMessageIdValue(value))
            {
                invalid.Add(path + "/" + index);
            }

            index = index + 1;
        }
    }

    internal void ValidateAddressValues(List<EmailAddress>? addresses, string path, List<string> invalid)
    {
        if (addresses == null)
        {
            return;
        }

        int index = 0;
        foreach (EmailAddress address in addresses)
        {
            if (address == null || address.Email == null || !Values.IsValidUnicode(address.Email) ||
                (address.Name != null && !Values.IsValidUnicode(address.Name)))
            {
                invalid.Add(path + "/" + index);
            }

            index = index + 1;
        }
    }

    internal bool IsDynamicHeaderValue(HeaderSelection selection, JsonElement value)
    {
        if (!selection.IsAll)
        {
            return IsSingleHeaderValue(selection.Form, value);
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (JsonElement item in value.EnumerateArray())
        {
            if (!IsSingleHeaderValue(selection.Form, item))
            {
                return false;
            }
        }

        return true;
    }

    internal bool IsSingleHeaderValue(JmapHeaderForm form, JsonElement value)
    {
        if (form == JmapHeaderForm.Raw || form == JmapHeaderForm.Text)
        {
            return value.ValueKind == JsonValueKind.String && Values.IsValidUnicode(value.GetString());
        }

        if (form == JmapHeaderForm.Addresses)
        {
            return IsAddressArrayValue(value);
        }

        if (form == JmapHeaderForm.GroupedAddresses)
        {
            return IsGroupedAddressArrayValue(value);
        }

        if (form == JmapHeaderForm.MessageIds)
        {
            return value.ValueKind == JsonValueKind.Null || IsMessageIdArrayValue(value);
        }

        if (form == JmapHeaderForm.Date)
        {
            return value.ValueKind == JsonValueKind.Null ||
                (value.ValueKind == JsonValueKind.String && Values.IsDate(value.GetString()));
        }

        return form == JmapHeaderForm.Urls && (value.ValueKind == JsonValueKind.Null || IsUrlArrayValue(value));
    }

    internal bool IsAddressArrayValue(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (JsonElement item in value.EnumerateArray())
        {
            if (!IsAddressObjectValue(item))
            {
                return false;
            }
        }

        return true;
    }

    internal bool IsGroupedAddressArrayValue(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (JsonElement item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            JsonElement name = FindJsonMember(item, "name");
            JsonElement addresses = FindJsonMember(item, "addresses");
            if (!IsNullableStringValue(name) || !IsAddressArrayValue(addresses))
            {
                return false;
            }
        }

        return true;
    }

    internal bool IsAddressObjectValue(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        JsonElement name = FindJsonMember(value, "name");
        JsonElement email = FindJsonMember(value, "email");
        return IsNullableStringValue(name) && email.ValueKind == JsonValueKind.String &&
            Values.IsValidUnicode(email.GetString());
    }

    internal bool IsMessageIdArrayValue(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (JsonElement item in value.EnumerateArray())
        {
            string? itemValue = item.ValueKind == JsonValueKind.String ? item.GetString() : null;
            if (itemValue == null || !Structured.IsMessageIdValue(itemValue))
            {
                return false;
            }
        }

        return true;
    }

    internal bool IsUrlArrayValue(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (JsonElement item in value.EnumerateArray())
        {
            string? itemValue = item.ValueKind == JsonValueKind.String ? item.GetString() : null;
            if (itemValue == null || !Structured.IsAbsoluteUrl(itemValue))
            {
                return false;
            }
        }

        return true;
    }

    internal bool IsNullableStringValue(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.Null ||
            (value.ValueKind == JsonValueKind.String && Values.IsValidUnicode(value.GetString()));
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

    internal void AddOccurrenceIfPresent(object? value, string headerName, string path,
        List<EmailHeaderOccurrence> occurrences)
    {
        if (value != null)
        {
            AddHeaderOccurrence(headerName, path, occurrences);
        }
    }

    internal void AddHeaderOccurrence(string headerName, string path, List<EmailHeaderOccurrence> occurrences)
    {
        EmailHeaderOccurrence occurrence = new EmailHeaderOccurrence();
        occurrence.HeaderName = headerName;
        occurrence.Path = path;
        occurrences.Add(occurrence);
    }
}
