using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace jmap;

internal class HeaderSelection
{
    internal string? PropertyName;
    internal string? HeaderName;
    internal JmapHeaderForm Form;
    internal bool HasExplicitForm;
    internal bool IsAll;
}

internal class HeaderPropertySelector
{
    internal HeaderValueParser Values;
    internal AddressHeaderParser Addresses;
    internal StructuredHeaderValueParser Structured;
    internal JmapJsonReader Reader;
    internal EmailJsonCodec EmailCodec;

    internal HeaderPropertySelector()
    {
        Values = new HeaderValueParser();
        Addresses = new AddressHeaderParser();
        Structured = new StructuredHeaderValueParser();
        Reader = new JmapJsonReader(JmapProtocolLimits.SuggestedMinimumMaxSizeRequest, 64);
        EmailCodec = new EmailJsonCodec(Reader);
    }

    internal HeaderSelection ParseProperty(string? propertyName)
    {
        if (propertyName == null || !propertyName.StartsWith(JmapProperties.HeaderPrefix, global::System.StringComparison.Ordinal))
        {
            throw ValidationFailure("A dynamic header property must start with header:.", propertyName);
        }

        int nameStart = JmapProperties.HeaderPrefix.Length;
        int suffixStart = FindColon(propertyName, nameStart);
        string headerName = suffixStart < 0 ? propertyName.Substring(nameStart) : propertyName.Substring(nameStart, suffixStart - nameStart);
        if (!IsHeaderName(headerName))
        {
            throw ValidationFailure("The dynamic header property contains an invalid field name.", propertyName);
        }

        HeaderSelection selection = new HeaderSelection();
        selection.PropertyName = propertyName;
        selection.HeaderName = headerName;
        selection.Form = JmapHeaderForm.Raw;
        if (suffixStart < 0)
        {
            return selection;
        }

        string suffix = propertyName.Substring(suffixStart);
        if (suffix.EndsWith(JmapProperties.HeaderAllSuffix, global::System.StringComparison.Ordinal))
        {
            selection.IsAll = true;
            suffix = suffix.Substring(0, suffix.Length - JmapProperties.HeaderAllSuffix.Length);
        }

        if (suffix.Length != 0)
        {
            selection.HasExplicitForm = true;
            selection.Form = ParseForm(suffix, propertyName);
        }

        if (!IsFormAllowed(headerName, selection.Form))
        {
            throw ValidationFailure("The requested parsed form is not permitted for this header field.", propertyName);
        }

        return selection;
    }

    internal HeaderSelection? ParsePropertyForValidation(string? propertyName)
    {
        if (propertyName == null || !propertyName.StartsWith(JmapProperties.HeaderPrefix, global::System.StringComparison.Ordinal))
        {
            return null;
        }

        int nameStart = JmapProperties.HeaderPrefix.Length;
        int suffixStart = FindColon(propertyName, nameStart);
        string headerName = suffixStart < 0 ? propertyName.Substring(nameStart) :
            propertyName.Substring(nameStart, suffixStart - nameStart);
        if (!IsHeaderName(headerName))
        {
            return null;
        }

        HeaderSelection selection = new HeaderSelection();
        selection.PropertyName = propertyName;
        selection.HeaderName = headerName;
        selection.Form = JmapHeaderForm.Raw;
        if (suffixStart < 0)
        {
            return selection;
        }

        string suffix = propertyName.Substring(suffixStart);
        if (suffix.EndsWith(JmapProperties.HeaderAllSuffix, global::System.StringComparison.Ordinal))
        {
            selection.IsAll = true;
            suffix = suffix.Substring(0, suffix.Length - JmapProperties.HeaderAllSuffix.Length);
        }

        if (suffix.Length != 0)
        {
            selection.HasExplicitForm = true;
            if (suffix == ":asRaw")
            {
                selection.Form = JmapHeaderForm.Raw;
            }
            else if (suffix == ":asText")
            {
                selection.Form = JmapHeaderForm.Text;
            }
            else if (suffix == ":asAddresses")
            {
                selection.Form = JmapHeaderForm.Addresses;
            }
            else if (suffix == ":asGroupedAddresses")
            {
                selection.Form = JmapHeaderForm.GroupedAddresses;
            }
            else if (suffix == ":asMessageIds")
            {
                selection.Form = JmapHeaderForm.MessageIds;
            }
            else if (suffix == ":asDate")
            {
                selection.Form = JmapHeaderForm.Date;
            }
            else if (suffix == ":asURLs")
            {
                selection.Form = JmapHeaderForm.Urls;
            }
            else
            {
                return null;
            }
        }

        return IsFormAllowed(headerName, selection.Form) ? selection : null;
    }

    internal void DeriveConvenienceProperties(Email? email, List<EmailHeader>? headers)
    {
        if (email == null)
        {
            return;
        }

        email.MessageId = null;
        email.InReplyTo = null;
        email.References = null;
        email.Sender = null;
        email.From = null;
        email.To = null;
        email.Cc = null;
        email.Bcc = null;
        email.ReplyTo = null;
        email.Subject = null;
        email.SentAt = null;
        if (headers == null)
        {
            return;
        }

        foreach (EmailHeader header in headers)
        {
            if (header == null || header.Name == null)
            {
                continue;
            }

            if (EqualsHeader(header.Name, "Message-ID"))
            {
                email.MessageId = Structured.ParseMessageIds(header.Value);
            }
            else if (EqualsHeader(header.Name, "In-Reply-To"))
            {
                email.InReplyTo = Structured.ParseMessageIds(header.Value);
            }
            else if (EqualsHeader(header.Name, "References"))
            {
                email.References = Structured.ParseMessageIds(header.Value);
            }
            else if (EqualsHeader(header.Name, "Sender"))
            {
                email.Sender = Addresses.ParseAddresses(header.Value);
            }
            else if (EqualsHeader(header.Name, "From"))
            {
                email.From = Addresses.ParseAddresses(header.Value);
            }
            else if (EqualsHeader(header.Name, "To"))
            {
                email.To = Addresses.ParseAddresses(header.Value);
            }
            else if (EqualsHeader(header.Name, "Cc"))
            {
                email.Cc = Addresses.ParseAddresses(header.Value);
            }
            else if (EqualsHeader(header.Name, "Bcc"))
            {
                email.Bcc = Addresses.ParseAddresses(header.Value);
            }
            else if (EqualsHeader(header.Name, "Reply-To"))
            {
                email.ReplyTo = Addresses.ParseAddresses(header.Value);
            }
            else if (EqualsHeader(header.Name, "Subject"))
            {
                email.Subject = Values.DecodeText(header.Value);
            }
            else if (EqualsHeader(header.Name, "Date"))
            {
                email.SentAt = Structured.ParseDate(header.Value);
            }
        }
    }

    internal JsonElement SelectProperty(List<EmailHeader>? headers, string? propertyName)
    {
        HeaderSelection selection = ParseProperty(propertyName);
        if (selection.Form == JmapHeaderForm.Raw || selection.Form == JmapHeaderForm.Text)
        {
            List<string?> selected = new List<string?>();
            if (headers != null)
            {
                foreach (EmailHeader header in headers)
                {
                    if (IsSelectedHeader(header, selection.HeaderName))
                    {
                        string? value = selection.Form == JmapHeaderForm.Text ? Values.DecodeText(header.Value) : header.Value;
                        selected.Add(value);
                    }
                }
            }

            return EncodeSelection(selected, selection.IsAll);
        }

        if (selection.Form == JmapHeaderForm.Addresses)
        {
            List<List<EmailAddress>> selected = new List<List<EmailAddress>>();
            if (headers != null)
            {
                foreach (EmailHeader header in headers)
                {
                    if (IsSelectedHeader(header, selection.HeaderName))
                    {
                        selected.Add(Addresses.ParseAddresses(header.Value));
                    }
                }
            }

            return EncodeAddressSelections(selected, selection.IsAll);
        }

        if (selection.Form == JmapHeaderForm.GroupedAddresses)
        {
            List<List<EmailAddressGroup>> selected = new List<List<EmailAddressGroup>>();
            if (headers != null)
            {
                foreach (EmailHeader header in headers)
                {
                    if (IsSelectedHeader(header, selection.HeaderName))
                    {
                        selected.Add(Addresses.ParseGroupedAddresses(header.Value));
                    }
                }
            }

            return EncodeGroupedAddressSelections(selected, selection.IsAll);
        }

        if (selection.Form == JmapHeaderForm.MessageIds || selection.Form == JmapHeaderForm.Urls)
        {
            List<List<string>?> selected = new List<List<string>?>();
            if (headers != null)
            {
                foreach (EmailHeader header in headers)
                {
                    if (IsSelectedHeader(header, selection.HeaderName))
                    {
                        List<string>? value = selection.Form == JmapHeaderForm.MessageIds ?
                            Structured.ParseMessageIds(header.Value) : Structured.ParseUrls(header.Value);
                        selected.Add(value);
                    }
                }
            }

            return EncodeStringArraySelections(selected, selection.IsAll);
        }

        if (selection.Form == JmapHeaderForm.Date)
        {
            List<string?> selected = new List<string?>();
            if (headers != null)
            {
                foreach (EmailHeader header in headers)
                {
                    if (IsSelectedHeader(header, selection.HeaderName))
                    {
                        selected.Add(Structured.ParseDate(header.Value));
                    }
                }
            }

            return EncodeSelection(selected, selection.IsAll);
        }

        throw ValidationFailure("This parsed header form is not available from the selector.", propertyName);
    }

    internal bool IsSelectedHeader(EmailHeader header, string? headerName)
    {
        return header != null && header.Name != null && headerName != null &&
            string.Equals(header.Name, headerName, global::System.StringComparison.OrdinalIgnoreCase);
    }

    internal JsonElement EncodeSelection(List<string?> selected, bool all)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        if (all)
        {
            writer.WriteArrayStart();
            foreach (string? value in selected)
            {
                writer.WriteStringValue(value);
            }

            writer.WriteArrayEnd();
        }
        else if (selected.Count == 0)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStringValue(selected[selected.Count - 1]);
        }

        writer.DisposeWriter();
        return Reader.ReadUtf8Memory(stream.ToArray());
    }

    internal JsonElement EncodeAddressSelections(List<List<EmailAddress>> selected, bool all)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        if (all)
        {
            writer.WriteArrayStart();
            int index = 0;
            foreach (List<EmailAddress> value in selected)
            {
                EmailCodec.EncodeAddressList(value, writer, "header/" + index);
                index = index + 1;
            }

            writer.WriteArrayEnd();
        }
        else if (selected.Count == 0)
        {
            writer.WriteNullValue();
        }
        else
        {
            EmailCodec.EncodeAddressList(selected[selected.Count - 1], writer, "header");
        }

        writer.DisposeWriter();
        return Reader.ReadUtf8Memory(stream.ToArray());
    }

    internal JsonElement EncodeGroupedAddressSelections(List<List<EmailAddressGroup>> selected, bool all)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        if (all)
        {
            writer.WriteArrayStart();
            int index = 0;
            foreach (List<EmailAddressGroup> value in selected)
            {
                EmailCodec.EncodeAddressGroups(value, writer, "header/" + index);
                index = index + 1;
            }

            writer.WriteArrayEnd();
        }
        else if (selected.Count == 0)
        {
            writer.WriteNullValue();
        }
        else
        {
            EmailCodec.EncodeAddressGroups(selected[selected.Count - 1], writer, "header");
        }

        writer.DisposeWriter();
        return Reader.ReadUtf8Memory(stream.ToArray());
    }

    internal JsonElement EncodeStringArraySelections(List<List<string>?> selected, bool all)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        if (all)
        {
            writer.WriteArrayStart();
            foreach (List<string>? value in selected)
            {
                WriteStringArrayValue(value, writer);
            }

            writer.WriteArrayEnd();
        }
        else if (selected.Count == 0)
        {
            writer.WriteNullValue();
        }
        else
        {
            WriteStringArrayValue(selected[selected.Count - 1], writer);
        }

        writer.DisposeWriter();
        return Reader.ReadUtf8Memory(stream.ToArray());
    }

    internal void WriteStringArrayValue(List<string>? value, JmapJsonWriter writer)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteArrayStart();
        foreach (string item in value)
        {
            writer.WriteStringValue(item);
        }

        writer.WriteArrayEnd();
    }

    internal JmapHeaderForm ParseForm(string suffix, string propertyName)
    {
        switch (suffix)
        {
            case ":asRaw":
                return JmapHeaderForm.Raw;
            case ":asText":
                return JmapHeaderForm.Text;
            case ":asAddresses":
                return JmapHeaderForm.Addresses;
            case ":asGroupedAddresses":
                return JmapHeaderForm.GroupedAddresses;
            case ":asMessageIds":
                return JmapHeaderForm.MessageIds;
            case ":asDate":
                return JmapHeaderForm.Date;
            case ":asURLs":
                return JmapHeaderForm.Urls;
            default:
                throw ValidationFailure("The dynamic header property has invalid or misordered suffixes.", propertyName);
        }
    }

    internal JmapHeaderForm DefaultForm(string? headerName)
    {
        if (headerName == null)
        {
            return JmapHeaderForm.Raw;
        }

        if (IsTextHeader(headerName))
        {
            return JmapHeaderForm.Text;
        }

        if (IsAddressHeader(headerName))
        {
            return JmapHeaderForm.Addresses;
        }

        if (IsMessageIdHeader(headerName))
        {
            return JmapHeaderForm.MessageIds;
        }

        if (IsDateHeader(headerName))
        {
            return JmapHeaderForm.Date;
        }

        if (IsUrlHeader(headerName))
        {
            return JmapHeaderForm.Urls;
        }

        return JmapHeaderForm.Raw;
    }

    internal bool IsFormAllowed(string headerName, JmapHeaderForm form)
    {
        if (form == JmapHeaderForm.Raw)
        {
            return true;
        }

        if (!IsKnownHeader(headerName))
        {
            return true;
        }

        if (form == JmapHeaderForm.Text)
        {
            return IsTextHeader(headerName);
        }

        if (form == JmapHeaderForm.Addresses || form == JmapHeaderForm.GroupedAddresses)
        {
            return IsAddressHeader(headerName);
        }

        if (form == JmapHeaderForm.MessageIds)
        {
            return IsMessageIdHeader(headerName);
        }

        if (form == JmapHeaderForm.Date)
        {
            return IsDateHeader(headerName);
        }

        return form == JmapHeaderForm.Urls && IsUrlHeader(headerName);
    }

    internal bool IsHeaderName(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character < 33 || character > 126 || character == ':')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal int FindColon(string value, int start)
    {
        int index = start;
        while (index < value.Length)
        {
            if (value[index] == ':')
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal bool IsKnownHeader(string name)
    {
        return IsTextHeader(name) || IsAddressHeader(name) || IsMessageIdHeader(name) || IsDateHeader(name) ||
            IsUrlHeader(name) || EqualsHeader(name, "Return-Path") || EqualsHeader(name, "Received");
    }

    internal bool IsTextHeader(string name)
    {
        return EqualsHeader(name, "Subject") || EqualsHeader(name, "Comments") || EqualsHeader(name, "Keywords") ||
            EqualsHeader(name, "List-Id");
    }

    internal bool IsAddressHeader(string name)
    {
        return EqualsHeader(name, "From") || EqualsHeader(name, "Sender") || EqualsHeader(name, "Reply-To") ||
            EqualsHeader(name, "To") || EqualsHeader(name, "Cc") || EqualsHeader(name, "Bcc") ||
            EqualsHeader(name, "Resent-From") || EqualsHeader(name, "Resent-Sender") ||
            EqualsHeader(name, "Resent-Reply-To") || EqualsHeader(name, "Resent-To") ||
            EqualsHeader(name, "Resent-Cc") || EqualsHeader(name, "Resent-Bcc");
    }

    internal bool IsMessageIdHeader(string name)
    {
        return EqualsHeader(name, "Message-ID") || EqualsHeader(name, "In-Reply-To") ||
            EqualsHeader(name, "References") || EqualsHeader(name, "Resent-Message-ID");
    }

    internal bool IsDateHeader(string name)
    {
        return EqualsHeader(name, "Date") || EqualsHeader(name, "Resent-Date");
    }

    internal bool IsUrlHeader(string name)
    {
        return EqualsHeader(name, "List-Help") || EqualsHeader(name, "List-Unsubscribe") ||
            EqualsHeader(name, "List-Subscribe") || EqualsHeader(name, "List-Post") ||
            EqualsHeader(name, "List-Owner") || EqualsHeader(name, "List-Archive");
    }

    internal bool EqualsHeader(string left, string right)
    {
        return string.Equals(left, right, global::System.StringComparison.OrdinalIgnoreCase);
    }

    internal JmapValidationException ValidationFailure(string message, string? propertyName)
    {
        string path = propertyName == null ? "header" : propertyName;
        return new JmapValidationException(message, new List<string> { path }, default(JsonElement));
    }
}
