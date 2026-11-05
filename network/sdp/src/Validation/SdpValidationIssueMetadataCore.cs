using System;
using System.Collections.Generic;

namespace sdp;

internal static class SdpValidationIssueMetadataCore
{
    internal static void CompleteIssues(SdpSessionDescription description, List<SdpValidationIssue> issues)
    {
        int index = 0;
        while (index < issues.Count)
        {
            CompleteIssue(description, issues[index]);
            index = index + 1;
        }
    }

    internal static void CompleteIssue(SdpSessionDescription description, SdpValidationIssue issue)
    {
        if (issue.Code == null || issue.Code.Length == 0)
        {
            issue.Code = SdpDiagnosticCodes.ValidationAttribute;
        }
        if (!IsKnownSeverity(issue.Severity))
        {
            issue.Severity = SdpValidationSeverity.Error;
        }
        if (!IsKnownScope(issue.Scope))
        {
            if (issue.Path != null && IsKnownScope(issue.Path.Scope))
            {
                issue.Scope = issue.Path.Scope;
            }
            else
            {
                issue.Scope = issue.MediaIndex >= 0 ? SdpScope.Media : SdpScope.Session;
            }
        }
        if (issue.AttributeName == null)
        {
            issue.AttributeName = string.Empty;
        }
        if (issue.FieldName == '\0' && issue.Path != null)
        {
            issue.FieldName = issue.Path.FieldName;
        }
        if (issue.AttributeName.Length == 0 && issue.Path != null && issue.Path.AttributeName != null)
        {
            issue.AttributeName = issue.Path.AttributeName;
        }
        if (issue.FieldName == '\0' && issue.AttributeName.Length > 0)
        {
            issue.FieldName = 'a';
        }
        if (issue.Scope == SdpScope.Session)
        {
            issue.MediaIndex = -1;
        }
        else if (issue.MediaIndex < 0 && issue.Path != null && issue.Path.MediaIndex >= 0)
        {
            issue.MediaIndex = issue.Path.MediaIndex;
        }
        CompletePath(description, issue);
        CompleteLocation(description, issue);
        if (issue.Detail == null || issue.Detail.Length == 0)
        {
            issue.Detail = "The identified SDP field or attribute violates the selected validation profile; replace it with a conforming value.";
        }
        issue.Detail = RedactSensitiveValues(description, issue.Detail);
    }

    internal static bool IsKnownSeverity(SdpValidationSeverity severity)
    {
        return severity == SdpValidationSeverity.Information || severity == SdpValidationSeverity.Warning || severity == SdpValidationSeverity.Error || severity == SdpValidationSeverity.Fatal;
    }

    internal static bool IsKnownScope(SdpScope scope)
    {
        return scope == SdpScope.Session || scope == SdpScope.Media;
    }

    internal static void CompletePath(SdpSessionDescription description, SdpValidationIssue issue)
    {
        if (issue.Path == null)
        {
            issue.Path = new SdpSourcePath();
        }
        issue.Path.Scope = issue.Scope;
        issue.Path.MediaIndex = issue.MediaIndex;
        issue.Path.FieldName = issue.FieldName;
        issue.Path.AttributeName = issue.AttributeName;
        if (issue.Path.MediaId == null)
        {
            issue.Path.MediaId = string.Empty;
        }
        if (issue.Scope == SdpScope.Media && description.MediaDescriptions != null && issue.MediaIndex >= 0 && issue.MediaIndex < description.MediaDescriptions.Count)
        {
            SdpMediaDescription media = description.MediaDescriptions[issue.MediaIndex];
            if (media != null && media.Attributes != null)
            {
                issue.Path.MediaId = ReadMediaId(media.Attributes);
            }
        }
        else
        {
            issue.Path.MediaId = string.Empty;
        }
    }

    internal static string ReadMediaId(List<SdpAttribute> attributes)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            if (attribute != null && attribute.Name != null && attribute.Value != null && string.Equals(attribute.Name, SdpAttributeNames.MediaId, StringComparison.OrdinalIgnoreCase))
            {
                string value = SdpBfcpAttributeCodecCore.TrimHorizontalWhitespace(attribute.Value);
                if (SdpCommonAttributeCodecCore.IsTokenValue(value))
                {
                    return value;
                }
            }
            index = index + 1;
        }
        return string.Empty;
    }

    internal static void CompleteLocation(SdpSessionDescription description, SdpValidationIssue issue)
    {
        if (issue.Location == null)
        {
            issue.Location = new SdpSourceLocation();
        }
        if (issue.Location.LineNumber > 0 && issue.Location.ColumnNumber > 0)
        {
            return;
        }
        SdpSourceLocation? parsedLocation = FindParsedLocation(description, issue);
        if (parsedLocation != null)
        {
            issue.Location = SdpDiagnosticBuilder.CopySourceLocation(parsedLocation);
        }
    }

    internal static SdpSourceLocation? FindParsedLocation(SdpSessionDescription description, SdpValidationIssue issue)
    {
        List<SdpRawLine>? lines = null;
        if (issue.Scope == SdpScope.Session)
        {
            lines = description.RawLines;
        }
        else if (description.MediaDescriptions != null && issue.MediaIndex >= 0 && issue.MediaIndex < description.MediaDescriptions.Count)
        {
            SdpMediaDescription media = description.MediaDescriptions[issue.MediaIndex];
            if (media != null)
            {
                if (issue.FieldName == 'm' && media.Location != null && media.Location.LineNumber > 0 && media.Location.ColumnNumber > 0)
                {
                    return media.Location;
                }
                lines = media.RawLines;
            }
        }
        if (lines == null)
        {
            return null;
        }
        int requestedOccurrence = issue.Path.OccurrenceIndex < 0 ? 0 : issue.Path.OccurrenceIndex;
        int occurrence = 0;
        int index = 0;
        while (index < lines.Count)
        {
            SdpRawLine line = lines[index];
            if (line != null && LineMatchesIssue(line, issue))
            {
                if (occurrence == requestedOccurrence)
                {
                    return line.Location;
                }
                occurrence = occurrence + 1;
            }
            index = index + 1;
        }
        return null;
    }

    internal static bool LineMatchesIssue(SdpRawLine line, SdpValidationIssue issue)
    {
        if (line.FieldName != issue.FieldName)
        {
            return false;
        }
        if (issue.FieldName != 'a' || issue.AttributeName.Length == 0)
        {
            return true;
        }
        string name = ReadRawAttributeName(line.Value);
        return string.Equals(name, issue.AttributeName, StringComparison.OrdinalIgnoreCase);
    }

    internal static string ReadRawAttributeName(string value)
    {
        if (value == null)
        {
            return string.Empty;
        }
        int colonIndex = value.IndexOf(':');
        if (colonIndex < 0)
        {
            return value;
        }
        return value.Substring(0, colonIndex);
    }

    internal static string RedactSensitiveValues(SdpSessionDescription description, string detail)
    {
        string result = RedactEncryptionKey(detail, description.EncryptionKey);
        if (description.Attributes != null)
        {
            result = RedactSensitiveAttributes(result, description.Attributes);
        }
        if (description.MediaDescriptions == null)
        {
            return result;
        }
        int mediaIndex = 0;
        while (mediaIndex < description.MediaDescriptions.Count)
        {
            SdpMediaDescription media = description.MediaDescriptions[mediaIndex];
            if (media != null)
            {
                result = RedactEncryptionKey(result, media.EncryptionKey);
                if (media.Attributes != null)
                {
                    result = RedactSensitiveAttributes(result, media.Attributes);
                }
            }
            mediaIndex = mediaIndex + 1;
        }
        return result;
    }

    internal static string RedactEncryptionKey(string detail, SdpEncryptionKey? key)
    {
        if (key == null)
        {
            return detail;
        }
        string result = ReplaceSensitiveValue(detail, key.Value);
        return ReplaceSensitiveValue(result, key.RawText);
    }

    internal static string RedactSensitiveAttributes(string detail, List<SdpAttribute> attributes)
    {
        string result = detail;
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            if (attribute != null && IsSensitiveAttribute(attribute.Name))
            {
                string attributeValue = attribute.Value == null ? string.Empty : attribute.Value;
                result = ReplaceSensitiveValue(result, attributeValue);
                result = ReplaceSensitiveValue(result, attribute.RawText);
                if (attribute.Name != null && string.Equals(attribute.Name, SdpAttributeNames.Fingerprint, StringComparison.OrdinalIgnoreCase))
                {
                    int separatorIndex = attributeValue.IndexOf(' ');
                    if (separatorIndex >= 0 && separatorIndex + 1 < attributeValue.Length)
                    {
                        result = ReplaceSensitiveValue(result, attributeValue.Substring(separatorIndex + 1));
                    }
                }
                if (attribute.Name != null && string.Equals(attribute.Name, SdpAttributeNames.Crypto, StringComparison.OrdinalIgnoreCase))
                {
                    result = RedactInlineKeyMaterial(result, attributeValue);
                }
            }
            index = index + 1;
        }
        return result;
    }

    internal static bool IsSensitiveAttribute(string name)
    {
        return string.Equals(name, SdpAttributeNames.IcePassword, StringComparison.OrdinalIgnoreCase) || string.Equals(name, SdpAttributeNames.Fingerprint, StringComparison.OrdinalIgnoreCase) || string.Equals(name, SdpAttributeNames.Crypto, StringComparison.OrdinalIgnoreCase);
    }

    internal static string RedactInlineKeyMaterial(string detail, string value)
    {
        if (value == null)
        {
            return detail;
        }
        int markerIndex = value.IndexOf("inline:", StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
        {
            return detail;
        }
        int startIndex = markerIndex + 7;
        int endIndex = startIndex;
        while (endIndex < value.Length && value[endIndex] != '|' && value[endIndex] != ';' && value[endIndex] != ' ' && value[endIndex] != '\t')
        {
            endIndex = endIndex + 1;
        }
        if (endIndex == startIndex)
        {
            return detail;
        }
        return ReplaceSensitiveValue(detail, value.Substring(startIndex, endIndex - startIndex));
    }

    internal static string ReplaceSensitiveValue(string detail, string value)
    {
        if (value == null || value.Length == 0)
        {
            return detail;
        }
        return detail.Replace(value, "[redacted]", StringComparison.Ordinal);
    }
}
