using System.Collections.Generic;
using System.Text.Json;

namespace jmap;

internal class SubmissionValidator
{
    internal ValueValidator Values = new ValueValidator();

    internal List<string> ValidateIdentityCreate(Identity? identity)
    {
        List<string> invalid = new List<string>();
        if (identity == null)
        {
            invalid.Add("");
            return invalid;
        }

        if (identity.Id != null)
        {
            invalid.Add("id");
        }

        if (identity.Name != null && !Values.IsValidUnicode(identity.Name))
        {
            invalid.Add("name");
        }

        if (identity.Email == null || identity.Email.Length == 0 || !Values.IsValidUnicode(identity.Email))
        {
            invalid.Add("email");
        }

        ValidateAddressList(identity.ReplyTo, "replyTo", invalid);
        ValidateAddressList(identity.Bcc, "bcc", invalid);
        if (identity.TextSignature != null && !Values.IsValidUnicode(identity.TextSignature))
        {
            invalid.Add("textSignature");
        }

        if (identity.HtmlSignature != null && !Values.IsValidUnicode(identity.HtmlSignature))
        {
            invalid.Add("htmlSignature");
        }

        if (identity.MayDelete)
        {
            invalid.Add("mayDelete");
        }

        return invalid;
    }

    internal List<string> ValidateIdentityWritablePatch(JmapPatchObject? patch)
    {
        List<string> invalid = new List<string>();
        if (patch == null)
        {
            invalid.Add("");
            return invalid;
        }

        foreach (string path in patch.Values.Keys)
        {
            if (path != "name" && path != "replyTo" && path != "bcc" && path != "textSignature" && path != "htmlSignature")
            {
                invalid.Add(path);
            }
        }

        return invalid;
    }

    internal List<string> ValidateEmailSubmissionCreate(EmailSubmission? submission)
    {
        List<string> invalid = new List<string>();
        if (submission == null)
        {
            invalid.Add("");
            return invalid;
        }

        if (submission.Id != null)
        {
            invalid.Add("id");
        }

        if (!IsIdOrReference(submission.IdentityId))
        {
            invalid.Add("identityId");
        }

        if (!IsIdOrReference(submission.EmailId))
        {
            invalid.Add("emailId");
        }

        if (submission.ThreadId != null)
        {
            invalid.Add("threadId");
        }

        if (submission.Envelope != null)
        {
            ValidateEnvelope(submission.Envelope, invalid);
        }

        if (submission.SendAt != null)
        {
            invalid.Add("sendAt");
        }

        if (submission.HasUndoStatus)
        {
            invalid.Add("undoStatus");
        }

        if (submission.DeliveryStatus != null)
        {
            invalid.Add("deliveryStatus");
        }

        if (submission.DsnBlobIds != null)
        {
            invalid.Add("dsnBlobIds");
        }

        if (submission.MdnBlobIds != null)
        {
            invalid.Add("mdnBlobIds");
        }

        return invalid;
    }

    internal List<string> ValidateEmailSubmissionWritablePatch(JmapPatchObject? patch)
    {
        List<string> invalid = new List<string>();
        if (patch == null)
        {
            invalid.Add("");
            return invalid;
        }

        foreach (KeyValuePair<string, JsonElement> item in patch.Values)
        {
            if (item.Key != "undoStatus")
            {
                invalid.Add(item.Key);
            }
            else if (item.Value.ValueKind != JsonValueKind.String || item.Value.GetString() != "canceled")
            {
                invalid.Add(item.Key);
            }
        }

        return invalid;
    }

    internal List<string> ValidateVacationResponse(VacationResponse? response)
    {
        List<string> invalid = new List<string>();
        if (response == null)
        {
            invalid.Add("");
            return invalid;
        }

        if (response.Id != null)
        {
            invalid.Add("id");
        }

        if (response.FromDate != null && !Values.IsUtcDate(response.FromDate))
        {
            invalid.Add("fromDate");
        }

        if (response.ToDate != null && !Values.IsUtcDate(response.ToDate))
        {
            invalid.Add("toDate");
        }

        if (response.FromDate != null && response.ToDate != null && Values.IsUtcDate(response.FromDate) && Values.IsUtcDate(response.ToDate) && CompareUtcDates(response.FromDate, response.ToDate) >= 0)
        {
            invalid.Add("fromDate");
            invalid.Add("toDate");
        }

        if (response.Subject != null && !Values.IsValidUnicode(response.Subject))
        {
            invalid.Add("subject");
        }

        if (response.TextBody != null && !Values.IsValidUnicode(response.TextBody))
        {
            invalid.Add("textBody");
        }

        if (response.HtmlBody != null && !Values.IsValidUnicode(response.HtmlBody))
        {
            invalid.Add("htmlBody");
        }

        return invalid;
    }

    internal int CompareUtcDates(string left, string right)
    {
        int index = 0;
        while (index < 19)
        {
            if (left[index] < right[index])
            {
                return -1;
            }

            if (left[index] > right[index])
            {
                return 1;
            }

            index = index + 1;
        }

        int leftIndex = left[19] == '.' ? 20 : left.Length - 1;
        int rightIndex = right[19] == '.' ? 20 : right.Length - 1;
        int leftEnd = left.Length - 1;
        int rightEnd = right.Length - 1;
        while (leftIndex < leftEnd || rightIndex < rightEnd)
        {
            char leftDigit = leftIndex < leftEnd ? left[leftIndex] : '0';
            char rightDigit = rightIndex < rightEnd ? right[rightIndex] : '0';
            if (leftDigit < rightDigit)
            {
                return -1;
            }

            if (leftDigit > rightDigit)
            {
                return 1;
            }

            leftIndex = leftIndex + 1;
            rightIndex = rightIndex + 1;
        }

        return 0;
    }

    internal List<string> ValidateVacationResponseWritablePatch(JmapPatchObject? patch)
    {
        List<string> invalid = new List<string>();
        if (patch == null)
        {
            invalid.Add("");
            return invalid;
        }

        foreach (string path in patch.Values.Keys)
        {
            if (path != "isEnabled" && path != "fromDate" && path != "toDate" && path != "subject" && path != "textBody" && path != "htmlBody")
            {
                invalid.Add(path);
            }
        }

        return invalid;
    }

    internal void ValidateAddressList(List<EmailAddress>? addresses, string path, List<string> invalid)
    {
        if (addresses == null)
        {
            return;
        }

        int index = 0;
        while (index < addresses.Count)
        {
            EmailAddress address = addresses[index];
            if (address == null || address.Email == null || address.Email.Length == 0 || !Values.IsValidUnicode(address.Email))
            {
                invalid.Add(path + "/" + index + "/email");
            }

            if (address != null && address.Name != null && !Values.IsValidUnicode(address.Name))
            {
                invalid.Add(path + "/" + index + "/name");
            }

            index = index + 1;
        }
    }

    internal bool IsIdOrReference(string? value)
    {
        return Values.IsId(value) || Values.IsCreationIdReference(value);
    }

    internal void ValidateEnvelope(EmailSubmissionEnvelope envelope, List<string> invalid)
    {
        if (envelope.MailFrom == null)
        {
            invalid.Add("envelope/mailFrom");
        }
        else
        {
            ValidateSubmissionAddress(envelope.MailFrom, "envelope/mailFrom", true, invalid);
        }

        if (envelope.RcptTo == null || envelope.RcptTo.Count == 0)
        {
            invalid.Add("envelope/rcptTo");
        }

        if (envelope.RcptTo == null)
        {
            return;
        }

        int index = 0;
        while (index < envelope.RcptTo.Count)
        {
            ValidateSubmissionAddress(envelope.RcptTo[index], "envelope/rcptTo/" + index, false, invalid);
            index = index + 1;
        }
    }

    internal void ValidateSubmissionAddress(EmailSubmissionAddress? address, string path, bool allowEmpty, List<string> invalid)
    {
        if (address == null || !IsSmtpMailbox(address.Email, allowEmpty))
        {
            invalid.Add(path + "/email");
        }

        if (address == null || address.Parameters == null)
        {
            return;
        }

        foreach (KeyValuePair<string, JsonElement> parameter in address.Parameters)
        {
            if (!IsSmtpParameterName(parameter.Key) ||
                (parameter.Value.ValueKind != JsonValueKind.String && parameter.Value.ValueKind != JsonValueKind.Null) ||
                (parameter.Value.ValueKind == JsonValueKind.String && !Values.IsValidUnicode(parameter.Value.GetString())))
            {
                invalid.Add(path + "/parameters/" + parameter.Key);
            }
        }
    }

    internal void ValidateEnvelopeExtensions(EmailSubmissionEnvelope envelope, JmapSubmissionCapabilities? capabilities, List<string> invalid)
    {
        if (envelope.MailFrom != null)
        {
            ValidateAddressExtensions(envelope.MailFrom, "envelope/mailFrom", false, capabilities, invalid);
        }

        if (envelope.RcptTo == null)
        {
            return;
        }

        int index = 0;
        while (index < envelope.RcptTo.Count)
        {
            EmailSubmissionAddress address = envelope.RcptTo[index];
            if (address != null)
            {
                ValidateAddressExtensions(address, "envelope/rcptTo/" + index, true, capabilities, invalid);
            }

            index = index + 1;
        }
    }

    internal void ValidateAddressExtensions(EmailSubmissionAddress address, string path, bool recipient, JmapSubmissionCapabilities? capabilities, List<string> invalid)
    {
        if (address.Parameters == null)
        {
            return;
        }

        foreach (string parameter in address.Parameters.Keys)
        {
            if (!IsAdvertisedParameter(parameter, recipient, capabilities))
            {
                invalid.Add(path + "/parameters/" + parameter);
            }
        }
    }

    internal bool IsAdvertisedParameter(string parameter, bool recipient, JmapSubmissionCapabilities? capabilities)
    {
        if (capabilities == null || capabilities.SubmissionExtensions == null)
        {
            return false;
        }

        foreach (string extension in capabilities.SubmissionExtensions.Keys)
        {
            if (string.Equals(extension, parameter, global::System.StringComparison.OrdinalIgnoreCase) ||
                IsStandardExtensionParameter(extension, parameter, recipient))
            {
                return true;
            }
        }

        return false;
    }

    internal bool IsStandardExtensionParameter(string extension, string parameter, bool recipient)
    {
        if (string.Equals(extension, "DSN", global::System.StringComparison.OrdinalIgnoreCase))
        {
            if (recipient)
            {
                return string.Equals(parameter, "NOTIFY", global::System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(parameter, "ORCPT", global::System.StringComparison.OrdinalIgnoreCase);
            }

            return string.Equals(parameter, "RET", global::System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(parameter, "ENVID", global::System.StringComparison.OrdinalIgnoreCase);
        }

        if (!recipient && string.Equals(extension, "FUTURERELEASE", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(parameter, "HOLDFOR", global::System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(parameter, "HOLDUNTIL", global::System.StringComparison.OrdinalIgnoreCase);
        }

        if (!recipient && (string.Equals(extension, "DELIVERBY", global::System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, "DELIVERYBY", global::System.StringComparison.OrdinalIgnoreCase)))
        {
            return string.Equals(parameter, "BY", global::System.StringComparison.OrdinalIgnoreCase);
        }

        if (!recipient && (string.Equals(extension, "8BITMIME", global::System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, "BINARYMIME", global::System.StringComparison.OrdinalIgnoreCase)))
        {
            return string.Equals(parameter, "BODY", global::System.StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    internal bool IsSmtpMailbox(string? value, bool allowEmpty)
    {
        if (value == null || !Values.IsValidUnicode(value))
        {
            return false;
        }

        if (value.Length == 0)
        {
            return allowEmpty;
        }

        int separator = FindMailboxSeparator(value);
        if (separator <= 0 || separator >= value.Length - 1)
        {
            return false;
        }

        string localPart = value.Substring(0, separator);
        string domain = value.Substring(separator + 1);
        return Values.IsUtf8WithinSize(value, 254L) && Values.IsUtf8WithinSize(localPart, 64L) &&
            Values.IsUtf8WithinSize(domain, 255L) && IsSmtpLocalPart(localPart) && IsSmtpDomain(domain);
    }

    internal int FindMailboxSeparator(string value)
    {
        int separator = -1;
        int index = 0;
        bool quoted = false;
        bool escaped = false;
        while (index < value.Length)
        {
            char character = value[index];
            if (escaped)
            {
                escaped = false;
            }
            else if (quoted && character == '\\')
            {
                escaped = true;
            }
            else if (character == '"')
            {
                quoted = !quoted;
            }
            else if (!quoted && character == '@')
            {
                if (separator >= 0)
                {
                    return -1;
                }

                separator = index;
            }

            index = index + 1;
        }

        return quoted || escaped ? -1 : separator;
    }

    internal bool IsSmtpLocalPart(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        if (value[0] == '"')
        {
            if (value.Length < 2 || value[value.Length - 1] != '"')
            {
                return false;
            }

            int quotedIndex = 1;
            bool escaped = false;
            while (quotedIndex < value.Length - 1)
            {
                char quotedCharacter = value[quotedIndex];
                if (escaped)
                {
                    if (quotedCharacter < 32 || quotedCharacter == 127)
                    {
                        return false;
                    }

                    escaped = false;
                }
                else if (quotedCharacter == '\\')
                {
                    escaped = true;
                }
                else if (quotedCharacter == '"' || quotedCharacter < 32 || quotedCharacter == 127)
                {
                    return false;
                }

                quotedIndex = quotedIndex + 1;
            }

            return !escaped;
        }

        int index = 0;
        bool previousDot = true;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '.')
            {
                if (previousDot)
                {
                    return false;
                }

                previousDot = true;
            }
            else
            {
                if (!IsSmtpAtomCharacter(character))
                {
                    return false;
                }

                previousDot = false;
            }

            index = index + 1;
        }

        return !previousDot;
    }

    internal bool IsSmtpAtomCharacter(char character)
    {
        bool alphaNumeric = character >= 'A' && character <= 'Z' || character >= 'a' && character <= 'z' || character >= '0' && character <= '9';
        if (alphaNumeric || character > 127)
        {
            return true;
        }

        return character == '!' || character == '#' || character == '$' || character == '%' || character == '&' ||
            character == '\'' || character == '*' || character == '+' || character == '-' || character == '/' ||
            character == '=' || character == '?' || character == '^' || character == '_' || character == '`' ||
            character == '{' || character == '|' || character == '}' || character == '~';
    }

    internal bool IsSmtpDomain(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        if (value[0] == '[')
        {
            if (value.Length < 3 || value[value.Length - 1] != ']')
            {
                return false;
            }

            int literalIndex = 1;
            while (literalIndex < value.Length - 1)
            {
                char literalCharacter = value[literalIndex];
                if (literalCharacter <= 32 || literalCharacter == '[' || literalCharacter == ']' || literalCharacter == '\\' || literalCharacter == 127)
                {
                    return false;
                }

                literalIndex = literalIndex + 1;
            }

            return true;
        }

        int labelStart = 0;
        int index = 0;
        while (index <= value.Length)
        {
            if (index == value.Length || value[index] == '.')
            {
                if (index == labelStart || value[labelStart] == '-' || value[index - 1] == '-')
                {
                    return false;
                }

                labelStart = index + 1;
            }
            else
            {
                char character = value[index];
                bool alphaNumeric = character >= 'A' && character <= 'Z' || character >= 'a' && character <= 'z' || character >= '0' && character <= '9';
                if (!alphaNumeric && character != '-' && character <= 127)
                {
                    return false;
                }
            }

            index = index + 1;
        }

        return true;
    }

    internal bool IsSmtpParameterName(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        char first = value[0];
        bool firstAlphaNumeric = first >= 'A' && first <= 'Z' || first >= 'a' && first <= 'z' || first >= '0' && first <= '9';
        if (!firstAlphaNumeric)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            bool alphaNumeric = character >= 'A' && character <= 'Z' || character >= 'a' && character <= 'z' || character >= '0' && character <= '9';
            if (!alphaNumeric && character != '-')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }
}
