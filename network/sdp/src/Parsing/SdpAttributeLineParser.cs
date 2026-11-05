using System;

namespace sdp;

internal static class SdpAttributeLineParser
{
    internal static void ParseAttribute(SdpParseContext context, SdpRawLine line)
    {
        int occurrenceIndex;
        if (context.Scope == SdpScope.Media)
        {
            occurrenceIndex = context.MediaAttributeCount - 1;
        }
        else
        {
            occurrenceIndex = context.SessionAttributeCount - 1;
        }

        ReadOnlySpan<char> rawText = line.Text.AsSpan().Slice(2);
        int colonOffset = rawText.IndexOf(':');
        int nameLength = colonOffset < 0 ? rawText.Length : colonOffset;
        ReadOnlySpan<char> nameText = rawText.Slice(0, nameLength);
        if (IsAttributeNameTooLong(context, nameText))
        {
            SdpSourceLocation limitLocation = CreateAttributeValueLocation(line, 0);
            context.AddAttributeDiagnostic(SdpDiagnosticCodes.ParseTokenLengthLimitExceeded, SdpValidationSeverity.Fatal, nameText.ToString(), occurrenceIndex, "The SDP attribute name exceeds the configured token-length limit.", limitLocation);
            context.Stop = true;
            return;
        }

        SdpAttribute attribute = new SdpAttribute();
        attribute.Name = nameText.ToString();
        attribute.RawText = rawText.ToString();
        attribute.Location = SdpDiagnosticBuilder.CopySourceLocation(line.Location);
        if (colonOffset < 0)
        {
            attribute.Kind = SdpAttributeKind.Property;
        }
        else
        {
            attribute.Kind = SdpAttributeKind.Value;
            attribute.Value = rawText.Slice(colonOffset + 1).ToString();
        }

        if (context.Scope == SdpScope.Media)
        {
            if (context.CurrentMedia != null)
            {
                context.CurrentMedia.Attributes.Add(attribute);
            }
        }
        else
        {
            context.Description.Attributes.Add(attribute);
        }

        ValidateAttributeName(context, line, occurrenceIndex, attribute.Name);
    }

    internal static void ValidateAttributeName(SdpParseContext context, SdpRawLine line, int occurrenceIndex, string name)
    {
        if (name.Length == 0)
        {
            SdpSourceLocation emptyLocation = CreateAttributeValueLocation(line, 0);
            context.AddAttributeDiagnostic(SdpDiagnosticCodes.ParseInvalidToken, SdpValidationSeverity.Error, name, occurrenceIndex, "The SDP attribute name must not be empty.", emptyLocation);
            return;
        }
        int index = 0;
        while (index < name.Length)
        {
            if (!SdpCharacterClasses.IsTokenCharacter(name[index]))
            {
                SdpSourceLocation invalidLocation = CreateAttributeValueLocation(line, index);
                context.AddAttributeDiagnostic(SdpDiagnosticCodes.ParseInvalidToken, SdpValidationSeverity.Error, name, occurrenceIndex, "The SDP attribute name contains a character outside the token grammar.", invalidLocation);
                return;
            }
            index = index + 1;
        }
    }

    internal static bool IsAttributeNameTooLong(SdpParseContext context, ReadOnlySpan<char> name)
    {
        if (context.InputIsUtf8)
        {
            int byteLength = SdpLineReader.Utf8.GetByteCount(name);
            return byteLength > context.Options.MaximumTokenLength;
        }
        return name.Length > context.Options.MaximumTokenLength;
    }

    internal static SdpSourceLocation CreateAttributeValueLocation(SdpRawLine line, int relativeOffset)
    {
        SdpSourceLocation result = SdpDiagnosticBuilder.CopySourceLocation(line.Location);
        result.CharacterOffset = result.CharacterOffset + 2 + relativeOffset;
        if (result.ByteOffset >= 0)
        {
            int byteLength = SdpLineReader.Utf8.GetByteCount(line.Text.AsSpan(0, 2 + relativeOffset));
            result.ByteOffset = result.ByteOffset + byteLength;
        }
        result.ColumnNumber = 3 + relativeOffset;
        return result;
    }
}
