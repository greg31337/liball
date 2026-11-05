using System;
using System.Collections.Generic;

namespace sdp;

internal static class SdpGenerationValidationCore
{
    internal static bool ValidateGenerationDescription(SdpSessionDescription description, SdpWriteOptions options, SdpWriteResult result)
    {
        SdpValidationOptions validationOptions = new SdpValidationOptions();
        validationOptions.Profile = SdpValidationProfile.Rfc8866;
        validationOptions.MaximumDiagnosticCount = options.MaximumDiagnosticCount;
        SdpValidationResult validation = SdpValidationCore.ValidateDescription(description, validationOptions);
        if (HasBlockingValidationIssue(validation, options))
        {
            SdpWriterCore.CopyValidationDiagnostics(result, validation, options);
            return false;
        }
        if (!options.IncludeObsoleteFields)
        {
            return true;
        }
        if (!ValidateEncryptionKey(description.EncryptionKey, SdpScope.Session, -1, options, result))
        {
            return false;
        }
        int mediaIndex = 0;
        while (mediaIndex < description.MediaDescriptions.Count)
        {
            if (!ValidateEncryptionKey(description.MediaDescriptions[mediaIndex].EncryptionKey, SdpScope.Media, mediaIndex, options, result))
            {
                return false;
            }
            mediaIndex = mediaIndex + 1;
        }
        return true;
    }

    internal static bool HasBlockingValidationIssue(SdpValidationResult validation, SdpWriteOptions options)
    {
        int index = 0;
        while (index < validation.Issues.Count)
        {
            SdpValidationIssue issue = validation.Issues[index];
            if ((issue.Severity == SdpValidationSeverity.Error || issue.Severity == SdpValidationSeverity.Fatal) && ShouldApplyValidationIssue(issue, options))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static bool ShouldApplyValidationIssue(SdpValidationIssue issue, SdpWriteOptions options)
    {
        if (!options.IncludeObsoleteFields && issue.FieldName == 'k')
        {
            return false;
        }
        if (issue.FieldName != 'a')
        {
            return true;
        }
        string name = issue.AttributeName == null ? string.Empty : issue.AttributeName;
        string canonicalName = SdpCommonAttributeCodecCore.ReadCanonicalName(name);
        if (!options.IncludeObsoleteFields && (canonicalName == SdpAttributeNames.Category || canonicalName == SdpAttributeNames.Keywords))
        {
            return false;
        }
        if (!options.IncludeUnknownFields && name.Length > 0)
        {
            SdpAttributeRegistration registration = SdpRegistryLookup.FindAttributeRegistration(name);
            if (!registration.Found)
            {
                return false;
            }
        }
        return true;
    }

    internal static bool ValidateEncryptionKey(SdpEncryptionKey? key, SdpScope scope, int mediaIndex, SdpWriteOptions options, SdpWriteResult result)
    {
        if (key == null)
        {
            return true;
        }
        string method = key.Method == null ? string.Empty : key.Method;
        string value = key.Value == null ? string.Empty : key.Value;
        bool valid = key.Method != null && key.Value != null && SdpCommonAttributeCodecCore.IsTokenValue(method);
        if (valid && !key.HasValue && value.Length > 0)
        {
            valid = false;
        }
        if (valid && key.HasValue && !SdpCommonAttributeCodecCore.IsByteStringValue(value))
        {
            valid = false;
        }
        if (valid && method == "prompt")
        {
            valid = !key.HasValue && value.Length == 0;
        }
        else if (valid && method == "clear")
        {
            valid = key.HasValue && value.Length > 0;
        }
        else if (valid && method == "base64")
        {
            valid = key.HasValue && IsValidBase64Value(value);
        }
        else if (valid && method == "uri")
        {
            valid = key.HasValue && IsValidUriValue(value);
        }
        if (!valid)
        {
            SdpWriterCore.AddWriteDiagnostic(result, options, SdpDiagnosticCodes.WriteInvalidDescription, scope, 'k', string.Empty, mediaIndex, "The encryption-key field cannot be represented because its method and value state is invalid.", key.Location);
            return false;
        }
        return true;
    }

    internal static bool IsValidBase64Value(string value)
    {
        if (value.Length % 4 != 0)
        {
            return false;
        }
        int paddingLength = 0;
        if (value.Length > 0 && value[value.Length - 1] == '=')
        {
            paddingLength = 1;
        }
        if (value.Length > 1 && value[value.Length - 2] == '=')
        {
            paddingLength = 2;
        }
        int dataLength = value.Length - paddingLength;
        int index = 0;
        while (index < dataLength)
        {
            if (!SdpEncryptionKeyLineParser.IsBase64Character(value[index]))
            {
                return false;
            }
            index = index + 1;
        }
        if (paddingLength == 1 && dataLength % 4 != 3)
        {
            return false;
        }
        if (paddingLength == 2 && dataLength % 4 != 2)
        {
            return false;
        }
        return true;
    }

    internal static bool IsValidUriValue(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '%')
            {
                if (index + 2 >= value.Length || !SdpSessionLineParser.IsUriHexDigit(value[index + 1]) || !SdpSessionLineParser.IsUriHexDigit(value[index + 2]))
                {
                    return false;
                }
                index = index + 3;
                continue;
            }
            if (!SdpSessionLineParser.IsUriReferenceCharacter(character))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool ValidateRetainedLines(SdpSessionDescription description, SdpWriteOptions options, SdpWriteResult result)
    {
        if (!ValidateRawLineList(description.RawLines, SdpScope.Session, -1, options, result))
        {
            return false;
        }
        int mediaIndex = 0;
        while (mediaIndex < description.MediaDescriptions.Count)
        {
            if (!ValidateRawLineList(description.MediaDescriptions[mediaIndex].RawLines, SdpScope.Media, mediaIndex, options, result))
            {
                return false;
            }
            mediaIndex = mediaIndex + 1;
        }
        return true;
    }

    internal static bool ValidateRawLineList(List<SdpRawLine> lines, SdpScope scope, int mediaIndex, SdpWriteOptions options, SdpWriteResult result)
    {
        int index = 0;
        while (index < lines.Count)
        {
            SdpRawLine line = lines[index];
            if (line == null)
            {
                SdpWriterCore.AddWriteDiagnostic(result, options, SdpDiagnosticCodes.WriteInvalidDescription, scope, '\0', string.Empty, mediaIndex, "A retained source line contains uninitialized, inconsistent, or unrepresentable state.", new SdpSourceLocation());
                return false;
            }
            string text = line.Text == null ? string.Empty : line.Text;
            string value = line.Value == null ? string.Empty : line.Value;
            bool valid = line.Text != null && line.Value != null && line.Location != null && IsKnownRetainedLineEnding(line.LineEnding) && SdpCommonAttributeCodecCore.IsByteStringValue(text);
            if (valid && !line.IsMalformed)
            {
                valid = text.Length >= 2 && text[0] == line.FieldName && text[1] == '=' && string.Equals(value, text.Substring(2), StringComparison.Ordinal);
            }
            if (!valid)
            {
                SdpSourceLocation location = line.Location == null ? new SdpSourceLocation() : line.Location;
                SdpWriterCore.AddWriteDiagnostic(result, options, SdpDiagnosticCodes.WriteInvalidDescription, scope, line.FieldName, string.Empty, mediaIndex, "A retained source line contains uninitialized, inconsistent, or unrepresentable state.", location);
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool IsKnownRetainedLineEnding(SdpLineEnding lineEnding)
    {
        return lineEnding == SdpLineEnding.None || lineEnding == SdpLineEnding.CrLf || lineEnding == SdpLineEnding.Lf || lineEnding == SdpLineEnding.Cr;
    }
}
