using System;
using System.Collections.Generic;
using System.Net;

namespace email;
internal class EmailFeedbackReportValueParser
{
    internal static string? GetToken(EmailHeader? field)
    {
        string? value = GetNonemptyText(field);
        return IsToken(value) ? (value ?? throw new InvalidOperationException()).ToLowerInvariant() : null;
    }

    internal static EmailFeedbackType ParseFeedbackType(EmailHeader? field)
    {
        string? value = GetToken(field);
        if (value == null) return EmailFeedbackType.Unknown;
        if (value == "abuse") return EmailFeedbackType.Abuse;
        if (value == "auth-failure") return EmailFeedbackType.AuthenticationFailure;
        if (value == "fraud") return EmailFeedbackType.Fraud;
        if (value == "not-spam") return EmailFeedbackType.NotSpam;
        if (value == "other") return EmailFeedbackType.Other;
        if (value == "virus") return EmailFeedbackType.Virus;
        return EmailFeedbackType.Extension;
    }

    internal static EmailFeedbackAuthenticationFailure ParseAuthenticationFailure(EmailHeader? field)
    {
        string? value = GetToken(field);
        if (value == null) return EmailFeedbackAuthenticationFailure.Unknown;
        if (value == "adsp") return EmailFeedbackAuthenticationFailure.Adsp;
        if (value == "bodyhash") return EmailFeedbackAuthenticationFailure.BodyHash;
        if (value == "revoked") return EmailFeedbackAuthenticationFailure.Revoked;
        if (value == "signature") return EmailFeedbackAuthenticationFailure.Signature;
        if (value == "spf") return EmailFeedbackAuthenticationFailure.Spf;
        if (value == "dmarc") return EmailFeedbackAuthenticationFailure.Dmarc;
        return EmailFeedbackAuthenticationFailure.Extension;
    }

    internal static EmailFeedbackDeliveryResult ParseDeliveryResult(EmailHeader? field)
    {
        string? value = GetToken(field);
        if (value == null) return EmailFeedbackDeliveryResult.Unknown;
        if (value == "delivered") return EmailFeedbackDeliveryResult.Delivered;
        if (value == "spam") return EmailFeedbackDeliveryResult.Spam;
        if (value == "policy") return EmailFeedbackDeliveryResult.Policy;
        if (value == "reject") return EmailFeedbackDeliveryResult.Reject;
        if (value == "other") return EmailFeedbackDeliveryResult.Other;
        return EmailFeedbackDeliveryResult.Extension;
    }

    internal static string? GetPrintableAscii(EmailHeader? field)
    {
        string? value = GetNonemptyText(field);
        if (value == null) return null;
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] < 32 || value[index] > 126) return null;
            index++;
        }

        return value;
    }

    internal static string? GetNonemptyText(EmailHeader? field)
    {
        if (field == null) return null;
        string value = (field.GetDecodedValue() ?? throw new InvalidOperationException()).Trim();
        return value.Length == 0 ? null : value;
    }

    internal static uint ParsePositiveUInt32(EmailHeader? field)
    {
        string? value = GetNonemptyText(field);
        uint parsed;
        if (value == null || !IsDigits(value) || !uint.TryParse(value, out parsed) || parsed == 0) return 0;
        return parsed;
    }

    internal static int ParseSourcePort(EmailHeader? field)
    {
        string? value = GetNonemptyText(field);
        int parsed;
        if (value == null || !IsDigits(value) || !int.TryParse(value, out parsed) || parsed < 1 || parsed > 65535) return 0;
        return parsed;
    }

    internal static EmailDateTime? ParseDate(EmailHeader? field)
    {
        if (field == null) return null;
        EmailDateParseResult parsed = new EmailDateParser().ParseWithInputAndMode(field.GetUnfoldedValue(), EmailDateParseMode.Tolerant);
        return parsed.HasValue() ? parsed.GetValue() : null;
    }

    internal static EmailFeedbackTypedValue? ParseTyped(EmailHeader? field)
    {
        string? value = GetNonemptyText(field);
        if (value == null) return null;
        int separator = value.IndexOf(';');
        if (separator <= 0 || separator >= value.Length - 1) return null;
        string type = value.Substring(0, separator).Trim();
        string text = value.Substring(separator + 1).Trim();
        return IsToken(type) && text.Length > 0 ? new EmailFeedbackTypedValue(type, text) : null;
    }

    internal static IPAddress? ParseIp(EmailHeader? field)
    {
        string? value = GetNonemptyText(field);
        IPAddress? result;
        return value != null && IPAddress.TryParse(value, out result) ? result : null;
    }

    internal static EmailFeedbackPath? ParsePath(EmailHeader? field, bool allowNull)
    {
        string? value = GetNonemptyText(field);
        if (value == null || value.Length < 2 || value[0] != '<' || value[value.Length - 1] != '>') return null;
        string inner = value.Substring(1, value.Length - 2).Trim();
        if (inner.Length == 0) return allowNull ? new EmailFeedbackPath(true, null) : null;
        EmailAddressParseResult parsed = new EmailAddressParser().ParseMailbox(System.Text.Encoding.ASCII.GetBytes(inner), EmailAddressParseMode.Strict);
        return parsed.IsSuccess() && parsed.IsComplete() && parsed.HasMailbox() ? new EmailFeedbackPath(false, parsed.GetMailbox()) : null;
    }

    internal static List<EmailFeedbackPath> ParsePaths(EmailReportFieldBlock? block, string name, bool allowNull)
    {
        List<EmailHeader> fields = (block ?? throw new InvalidOperationException()).GetFieldsNamed(name);
        List<EmailFeedbackPath> result = new List<EmailFeedbackPath>();
        int index = 0;
        while (index < fields.Count)
        {
            EmailFeedbackPath? value = ParsePath(fields[index], allowNull);
            if (value != null) result.Add(value);
            index++;
        }

        return result;
    }

    internal static List<EmailAuthenticationResults> ParseAuthenticationResults(EmailReportFieldBlock? block)
    {
        List<EmailHeader> fields = (block ?? throw new InvalidOperationException()).GetFieldsNamed("Authentication-Results");
        List<EmailAuthenticationResults> result = new List<EmailAuthenticationResults>();
        int index = 0;
        while (index < fields.Count)
        {
            EmailAuthenticationResultsParseResult parsed = new EmailAuthenticationResultsParser().ParseWithInputAndMode(fields[index].GetUnfoldedValue(), EmailStructuredValueParseMode.Strict);
            if (parsed.IsSuccess() && parsed.IsComplete() && parsed.HasValue()) result.Add((parsed.GetValue() ?? throw new InvalidOperationException()));
            index++;
        }

        return result;
    }

    internal static List<string> ParseDomains(EmailReportFieldBlock? block)
    {
        List<EmailHeader> fields = (block ?? throw new InvalidOperationException()).GetFieldsNamed("Reported-Domain");
        List<string> result = new List<string>();
        int index = 0;
        while (index < fields.Count)
        {
            string? value = ParseDomain(fields[index]);
            if (value != null) result.Add(value);
            index++;
        }

        return result;
    }

    internal static string? ParseDomain(EmailHeader? field)
    {
        string? value = GetNonemptyText(field);
        if (value == null || value.Length > 253 || value[0] == '.' || value[value.Length - 1] == '.') return null;
        if (Uri.CheckHostName(value) != UriHostNameType.Dns) return null;
        string ascii = new EmailAddressIdn().ToAscii(value);
        return ascii.Length <= 253 ? ascii : null;
    }

    internal static List<Uri> ParseUris(EmailReportFieldBlock? block)
    {
        List<EmailHeader> fields = (block ?? throw new InvalidOperationException()).GetFieldsNamed("Reported-URI");
        List<Uri> result = new List<Uri>();
        int index = 0;
        while (index < fields.Count)
        {
            string? value = GetNonemptyText(fields[index]);
            Uri? uri;
            if (value != null && Uri.TryCreate(value, UriKind.Absolute, out uri)) result.Add(uri);
            index++;
        }

        return result;
    }

    internal static List<string> GetTextValues(EmailReportFieldBlock? block, string name)
    {
        List<EmailHeader> fields = (block ?? throw new InvalidOperationException()).GetFieldsNamed(name);
        List<string> result = new List<string>();
        int index = 0;
        while (index < fields.Count)
        {
            string? value = GetNonemptyText(fields[index]);
            if (value != null) result.Add(value);
            index++;
        }

        return result;
    }

    internal static List<EmailHeader> GetExtensions(EmailReportFieldBlock? block)
    {
        string[] known = new string[] { "Feedback-Type", "User-Agent", "Version", "Original-Envelope-Id", "Original-Mail-From", "Arrival-Date", "Received-Date", "Reporting-MTA", "Source-IP", "Source-Port", "Incidents", "Authentication-Results", "Auth-Failure", "Delivery-Result", "DKIM-Domain", "DKIM-Identity", "DKIM-Selector", "SPF-DNS", "Original-Rcpt-To", "Reported-Domain", "Reported-URI" };
        List<EmailHeader> result = new List<EmailHeader>();
        int index = 0;
        while (index < (block ?? throw new InvalidOperationException()).GetFieldCount())
        {
            EmailHeader field = block.GetFieldAt(index);
            if (!Contains(known, field.GetName())) result.Add(field);
            index++;
        }

        return result;
    }

    internal static bool IsToken(string? value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current < 33 || current > 126 || current == '(' || current == ')' || current == '<' || current == '>' || current == '@' || current == ',' || current == ';' || current == ':' || current == '\\' || current == '"' || current == '/' || current == '[' || current == ']' || current == '?' || current == '=') return false;
            index++;
        }

        return true;
    }

    static bool IsDigits(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] < '0' || value[index] > '9') return false;
            index++;
        }

        return value.Length > 0;
    }

    static bool Contains(string[] values, string expected)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (string.Equals(values[index], expected, StringComparison.OrdinalIgnoreCase)) return true;
            index++;
        }

        return false;
    }
}
