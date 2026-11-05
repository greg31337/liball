using System;
using System.Text;

namespace email;

internal class EmailSpecializedMessageMetadataParser
{
    internal static EmailPartialMessageMetadata ParsePartial(EmailMediaType mediaType, string? transferEncoding, EmailParseOptions options, EmailIssueList issues)
    {
        EmailParameterList parameters = mediaType.GetParameters();
        EmailParameter? idParameter = parameters.GetFirst("id");
        string? id = GetText(parameters, "id", false);
        if (id == null)
        {
            AddIssue(issues, options, EmailIssueCode.PartialMessageMalformed, "A message/partial entity requires a nonempty id parameter.", GetPosition(idParameter, mediaType), "The fragment bytes and all Content-Type parameters were retained without reassembly.");
        }

        EmailParameter? numberParameter = parameters.GetFirst("number");
        EmailSpecializedInteger number = ParseInteger(GetBytes(parameters, "number"), true);
        if (!number.IsValid())
        {
            AddIssue(issues, options, EmailIssueCode.PartialMessageMalformed, "A message/partial entity requires a positive integer number parameter.", GetPosition(numberParameter, mediaType), "The invalid parameter and fragment bytes were retained.");
        }

        EmailParameter? totalParameter = parameters.GetFirst("total");
        byte[]? totalBytes = GetBytes(parameters, "total");
        EmailSpecializedInteger total = ParseInteger(totalBytes, true);
        bool totalPresent = totalBytes != null;
        if (totalPresent && !total.IsValid())
        {
            AddIssue(issues, options, EmailIssueCode.PartialMessageMalformed, "The message/partial total parameter must be a positive integer when present.", GetPosition(totalParameter, mediaType), "The invalid parameter and fragment bytes were retained.");
        }

        if (number.IsValid() && total.IsValid() && number.GetValue() > total.GetValue())
        {
            AddIssue(issues, options, EmailIssueCode.PartialMessageMalformed, "The message/partial fragment number exceeds its declared total.", GetPosition(totalParameter, mediaType), "Both numeric values and the fragment bytes were retained.");
        }

        ValidateSevenBit(transferEncoding, EmailIssueCode.PartialMessageMalformed, "message/partial", mediaType, options, issues);
        return new EmailPartialMessageMetadata(parameters, id, number.GetValue(), number.IsValid(), total.GetValue(), totalPresent && total.IsValid());
    }

    internal static EmailExternalBodyMetadata ParseExternal(EmailMediaType mediaType, string? transferEncoding, EmailContent source, EmailRange contentRange, EmailParseOptions options, EmailIssueList issues)
    {
        EmailParameterList parameters = mediaType.GetParameters();
        EmailParameter? accessParameter = parameters.GetFirst("access-type");
        string? accessType = GetText(parameters, "access-type", false);
        bool validAccessType = accessType != null && IsToken(accessType);
        if (!validAccessType)
        {
            AddIssue(issues, options, EmailIssueCode.ExternalBodyMalformed, "A message/external-body entity requires a MIME token access-type parameter.", GetPosition(accessParameter, mediaType), "The reference parameters and body were retained without retrieval.");
        }

        EmailDateTime? expiration = ParseExpiration(parameters, mediaType, options, issues);
        EmailParameter? sizeParameter = parameters.GetFirst("size");
        byte[]? sizeBytes = GetBytes(parameters, "size");
        EmailSpecializedInteger size = ParseInteger(sizeBytes, false);
        bool sizePresent = sizeBytes != null;
        if (sizePresent && !size.IsValid())
        {
            AddIssue(issues, options, EmailIssueCode.ExternalBodyMalformed, "The external-body size parameter must be a nonnegative integer when present.", GetPosition(sizeParameter, mediaType), "The original size parameter was retained.");
        }

        string? permission = GetText(parameters, "permission", false);
        if (permission != null && !string.Equals(permission, "read", StringComparison.OrdinalIgnoreCase) && !string.Equals(permission, "read-write", StringComparison.OrdinalIgnoreCase))
        {
            AddIssue(issues, options, EmailIssueCode.ExternalBodyMalformed, "The external-body permission parameter is neither read nor read-write.", GetPosition(parameters.GetFirst("permission"), mediaType), "The extension value was retained.");
        }

        string? name = GetText(parameters, "name", true);
        string? site = GetText(parameters, "site", false);
        string? directory = GetText(parameters, "directory", true);
        string? mode = GetText(parameters, "mode", false);
        string? server = GetText(parameters, "server", false);
        string? subject = GetText(parameters, "subject", true);
        string? url = RemoveLinearWhitespace(GetText(parameters, "url", true));
        ValidateAccessSpecificParameters(accessType, name, site, server, url, mediaType, options, issues);
        ValidateSevenBit(transferEncoding, EmailIssueCode.ExternalBodyMalformed, "message/external-body", mediaType, options, issues);

        byte[] body = MaterializeRange(source, contentRange, options);
        EmailHeaderBlockParserOptions headerOptions = new EmailHeaderBlockParserOptions();
        headerOptions.SetLimits(options.GetLimits().GetParsingLimits());
        headerOptions.SetMode(options.GetMode() == EmailParseMode.Strict ? EmailStructuredValueParseMode.Strict : EmailStructuredValueParseMode.Tolerant);
        headerOptions.SetCompatibilityOptions(options.GetCompatibilityOptions());
        EmailHeaderBlockParseResult headerResult = new EmailHeaderBlockParser().ParseWithInputAndOptionsAndInitialPositionAndEndOfInput(body, headerOptions, new EmailSourcePosition(contentRange.GetStartOffset(), 1, 1), true);
        issues.AddRange(headerResult.GetIssues());
        bool headersComplete = headerResult.IsComplete();
        long headerLength = headersComplete ? headerResult.GetBodyStartOffset() - GetSeparatorLength(headerResult) : headerResult.GetConsumedByteCount();
        EmailRange referencedHeaderRange = new EmailRange(contentRange.GetStartOffset(), headerLength);
        EmailRange? phantomBodyRange = null;
        if (headersComplete)
        {
            long bodyStart = headerResult.GetBodyStartOffset();
            phantomBodyRange = new EmailRange(contentRange.GetStartOffset() + bodyStart, contentRange.GetLength() - bodyStart);
        }

        if (!headerResult.GetHeaders().Contains("Content-ID"))
        {
            AddIssue(issues, options, EmailIssueCode.ExternalBodyMalformed, "The encapsulated external-body headers do not contain the required Content-ID field.", new EmailSourcePosition(contentRange.GetStartOffset(), 1, 1), "The available encapsulated headers and phantom body were retained.");
        }

        return new EmailExternalBodyMetadata(parameters, accessType, validAccessType, expiration, size.GetValue(), sizePresent && size.IsValid(), permission, name, site, directory, mode, server, subject, url, headerResult.GetHeaders(), headersComplete, referencedHeaderRange, phantomBodyRange);
    }

    static EmailDateTime? ParseExpiration(EmailParameterList parameters, EmailMediaType mediaType, EmailParseOptions options, EmailIssueList issues)
    {
        EmailParameter? parameter = parameters.GetFirst("expiration");
        byte[]? value = GetBytes(parameters, "expiration");
        if (value == null)
        {
            return null;
        }

        EmailDateParserOptions dateOptions = new EmailDateParserOptions();
        dateOptions.SetMode(options.GetMode() == EmailParseMode.Strict ? EmailDateParseMode.Strict : EmailDateParseMode.Tolerant);
        dateOptions.SetMaximumInputBytes(options.GetLimits().GetParsingLimits().GetMaxLineBytes());
        EmailDateParseResult result = new EmailDateParser().ParseWithInputAndOptionsAndInitialPosition(value, dateOptions, GetPosition(parameter, mediaType));
        issues.AddRange(result.GetIssues());
        if (!result.HasValue())
        {
            AddIssue(issues, options, EmailIssueCode.ExternalBodyMalformed, "The external-body expiration parameter is not a valid Internet date-time.", GetPosition(parameter, mediaType), "The original expiration parameter was retained.");
        }

        return result.GetValue();
    }

    static void ValidateAccessSpecificParameters(string? accessType, string? name, string? site, string? server, string? url, EmailMediaType mediaType, EmailParseOptions options, EmailIssueList issues)
    {
        if (accessType == null)
        {
            return;
        }

        bool fileTransfer = string.Equals(accessType, "ftp", StringComparison.OrdinalIgnoreCase) || string.Equals(accessType, "anon-ftp", StringComparison.OrdinalIgnoreCase) || string.Equals(accessType, "tftp", StringComparison.OrdinalIgnoreCase);
        if (fileTransfer && (name == null || site == null))
        {
            AddIssue(issues, options, EmailIssueCode.ExternalBodyMalformed, "The selected external-body file-transfer access type requires name and site parameters.", mediaType.GetSourcePosition(), "All supplied reference parameters were retained.");
        }
        else if (string.Equals(accessType, "local-file", StringComparison.OrdinalIgnoreCase) && name == null)
        {
            AddIssue(issues, options, EmailIssueCode.ExternalBodyMalformed, "The local-file external-body access type requires a name parameter.", mediaType.GetSourcePosition(), "All supplied reference parameters were retained.");
        }
        else if (string.Equals(accessType, "mail-server", StringComparison.OrdinalIgnoreCase) && server == null)
        {
            AddIssue(issues, options, EmailIssueCode.ExternalBodyMalformed, "The mail-server external-body access type requires a server parameter.", mediaType.GetSourcePosition(), "No request message was created or sent.");
        }
        else if (string.Equals(accessType, "url", StringComparison.OrdinalIgnoreCase) && url == null)
        {
            AddIssue(issues, options, EmailIssueCode.ExternalBodyMalformed, "The URL external-body access type requires a URL parameter.", mediaType.GetSourcePosition(), "No URL was resolved or fetched.");
        }
    }

    static void ValidateSevenBit(string? transferEncoding, EmailIssueCode code, string mediaTypeName, EmailMediaType mediaType, EmailParseOptions options, EmailIssueList issues)
    {
        if (transferEncoding != null && !string.Equals(transferEncoding, "7bit", StringComparison.OrdinalIgnoreCase))
        {
            AddIssue(issues, options, code, mediaTypeName + " permits only the 7bit content-transfer-encoding.", mediaType.GetSourcePosition(), "The declared encoding and original content bytes were retained without transformation.");
        }
    }

    static EmailSpecializedInteger ParseInteger(byte[]? value, bool positive)
    {
        if (value == null)
        {
            return new EmailSpecializedInteger(0, false);
        }

        int start = 0;
        int end = value.Length;
        while (start < end && (value[start] == 32 || value[start] == 9))
        {
            start++;
        }

        while (end > start && (value[end - 1] == 32 || value[end - 1] == 9))
        {
            end--;
        }

        if (start == end)
        {
            return new EmailSpecializedInteger(0, false);
        }

        long number = 0;
        int index = start;
        while (index < end)
        {
            int digit = value[index] - 48;
            if (digit < 0 || digit > 9 || number > (long.MaxValue - digit) / 10)
            {
                return new EmailSpecializedInteger(0, false);
            }

            number = number * 10 + digit;
            index++;
        }

        return new EmailSpecializedInteger(number, positive ? number > 0 : true);
    }

    static byte[]? GetBytes(EmailParameterList parameters, string name)
    {
        EmailRfc2231DecodedParameter? decoded = parameters.GetDecoded(name);
        if (decoded != null)
        {
            return decoded.GetDecodedBytes();
        }

        EmailParameter? physical = parameters.GetFirst(name);
        return physical == null ? null : physical.GetValueBytes();
    }

    static string? GetText(EmailParameterList parameters, string name, bool utf8)
    {
        byte[]? value = GetBytes(parameters, name);
        if (value == null)
        {
            return null;
        }

        string text = utf8 ? Encoding.UTF8.GetString(value) : Encoding.Latin1.GetString(value);
        text = text.Trim();
        return text.Length == 0 ? null : text;
    }

    static string? RemoveLinearWhitespace(string? value)
    {
        if (value == null)
        {
            return null;
        }

        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current != ' ' && current != '\t' && current != '\r' && current != '\n')
            {
                result.Append(current);
            }

            index++;
        }

        return result.Length == 0 ? null : result.ToString();
    }

    static EmailSourcePosition GetPosition(EmailParameter? parameter, EmailMediaType mediaType)
    {
        return parameter == null ? mediaType.GetSourcePosition() : parameter.GetSourcePosition();
    }

    static void AddIssue(EmailIssueList issues, EmailParseOptions options, EmailIssueCode code, string message, EmailSourcePosition position, string recovery)
    {
        EmailIssueSeverity severity = options.GetMode() == EmailParseMode.Strict ? EmailIssueSeverity.Error : EmailIssueSeverity.Warning;
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, code, message, position, null, "Content-Type", recovery));
    }

    static bool IsToken(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current < 33 || current > 126 || current == '(' || current == ')' || current == '<' || current == '>' || current == '@' || current == ',' || current == ';' || current == ':' || current == '\\' || current == '"' || current == '/' || current == '[' || current == ']' || current == '?' || current == '=')
            {
                return false;
            }

            index++;
        }

        return value.Length > 0;
    }

    static byte[] MaterializeRange(EmailContent source, EmailRange range, EmailParseOptions options)
    {
        long maximum = options.GetLimits().GetParsingLimits().GetMaxMessageBytes();
        if (maximum > int.MaxValue)
        {
            maximum = int.MaxValue;
        }

        return new EmailContentOperations().CreateSlice(source, range.GetStartOffset(), range.GetLength()).Materialize(maximum);
    }

    static int GetLineEndingLength(EmailLineEnding ending)
    {
        return ending == EmailLineEnding.CrLf ? 2 : 1;
    }

    static int GetSeparatorLength(EmailHeaderBlockParseResult result)
    {
        return result.HasSeparator() ? GetLineEndingLength(result.GetSeparatorLineEnding()) : 0;
    }
}
internal class EmailSpecializedInteger
{
    long value;
    bool valid;

    internal EmailSpecializedInteger(long value, bool valid)
    {
        this.value = value;
        this.valid = valid;
    }

    internal long GetValue()
    {
        return value;
    }

    internal bool IsValid()
    {
        return valid;
    }
}
