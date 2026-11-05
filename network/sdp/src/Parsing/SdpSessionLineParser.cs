using System;

namespace sdp;

internal static class SdpSessionLineParser
{
    internal static void ParseProtocolVersion(SdpParseContext context, SdpRawLine line)
    {
        if (context.Scope != SdpScope.Session)
        {
            context.AddDiagnostic(SdpDiagnosticCodes.ParseFieldInWrongScope, SdpValidationSeverity.Error, SdpScope.Media, 'v', context.MediaIndex, 0, "The SDP protocol-version field is only permitted in session scope.", line.Location);
            return;
        }

        int occurrenceIndex = context.ProtocolVersionCount;
        context.ProtocolVersionCount = context.ProtocolVersionCount + 1;
        if (occurrenceIndex > 0)
        {
            context.AddDiagnostic(SdpDiagnosticCodes.ParseDuplicateField, SdpValidationSeverity.Error, SdpScope.Session, 'v', -1, occurrenceIndex, "The SDP protocol-version field may appear only once.", line.Location);
            return;
        }

        context.Description.HasProtocolVersion = true;
        if (context.Result.LinesConsumed != 1)
        {
            SdpFieldOrderValidator.AddFieldOrderDiagnostic(context, line, 'v', occurrenceIndex, "The SDP protocol-version field must be the first field.");
        }

        SdpSourceLocation valueLocation = CreateProtocolVersionValueLocation(line);
        ReadOnlySpan<char> value = line.Text.AsSpan().Slice(2);
        SdpNumberParseResult number = SdpNumberParser.ParseCharacterUnsignedDecimal(value);
        if (!number.Success)
        {
            context.AddDiagnostic(number.DiagnosticCode, SdpValidationSeverity.Error, SdpScope.Session, 'v', -1, 0, "The SDP protocol version must be an unsigned decimal integer.", valueLocation);
            return;
        }
        if (number.UnsignedValue > int.MaxValue)
        {
            context.AddDiagnostic(SdpDiagnosticCodes.ParseNumberOverflow, SdpValidationSeverity.Error, SdpScope.Session, 'v', -1, 0, "The SDP protocol version exceeds the supported integer representation.", valueLocation);
            return;
        }

        context.Description.ProtocolVersion = (int)number.UnsignedValue;
        if (context.Description.ProtocolVersion != 0)
        {
            context.AddDiagnostic(SdpDiagnosticCodes.ValidationProtocolVersion, SdpValidationSeverity.Error, SdpScope.Session, 'v', -1, 0, "The SDP protocol version must be zero.", valueLocation);
        }
    }

    internal static SdpSourceLocation CreateProtocolVersionValueLocation(SdpRawLine line)
    {
        SdpSourceLocation result = SdpDiagnosticBuilder.CopySourceLocation(line.Location);
        if (result.CharacterOffset >= 0)
        {
            result.CharacterOffset = result.CharacterOffset + 2;
        }
        if (result.ByteOffset >= 0)
        {
            result.ByteOffset = result.ByteOffset + 2;
        }
        result.ColumnNumber = 3;
        return result;
    }

    internal static void ParseOrigin(SdpParseContext context, SdpRawLine line)
    {
        if (context.Scope != SdpScope.Session)
        {
            context.AddDiagnostic(SdpDiagnosticCodes.ParseFieldInWrongScope, SdpValidationSeverity.Error, SdpScope.Media, 'o', context.MediaIndex, 0, "The SDP origin field is only permitted in session scope.", line.Location);
            return;
        }

        int occurrenceIndex = context.OriginCount;
        context.OriginCount = context.OriginCount + 1;
        if (occurrenceIndex > 0)
        {
            context.AddDiagnostic(SdpDiagnosticCodes.ParseDuplicateField, SdpValidationSeverity.Error, SdpScope.Session, 'o', -1, occurrenceIndex, "The SDP origin field may appear only once.", line.Location);
            return;
        }

        ReadOnlySpan<char> value = line.Text.AsSpan().Slice(2);
        SdpTokenReadResult[] tokens = new SdpTokenReadResult[6];
        int tokenIndex = 0;
        int offset = 0;
        int invalidOffset = 0;
        bool invalidCardinality = false;
        while (tokenIndex < tokens.Length && !invalidCardinality)
        {
            SdpTokenReadResult token = SdpTokenReader.ReadCharacterNonWhitespaceToken(value, offset, context.Options.MaximumTokenLength);
            bool utf8TokenLengthExceeded = false;
            if (context.InputIsUtf8 && !token.TokenLengthExceeded && token.Length > 0)
            {
                int byteLength = SdpLineReader.Utf8.GetByteCount(value.Slice(token.StartOffset, token.Length));
                utf8TokenLengthExceeded = byteLength > context.Options.MaximumTokenLength;
            }
            if (token.TokenLengthExceeded || utf8TokenLengthExceeded)
            {
                SdpSourceLocation tokenLocation = CreateOriginValueLocation(line, token.StartOffset);
                context.AddDiagnostic(SdpDiagnosticCodes.ParseTokenLengthLimitExceeded, SdpValidationSeverity.Fatal, SdpScope.Session, 'o', -1, 0, "An SDP origin token exceeds the configured token-length limit.", tokenLocation);
                context.Stop = true;
                return;
            }
            if (!token.Success)
            {
                invalidOffset = offset;
                invalidCardinality = true;
                continue;
            }

            tokens[tokenIndex] = token;
            if (tokenIndex < tokens.Length - 1)
            {
                int separatorOffset = token.NextOffset;
                if (separatorOffset >= value.Length || value[separatorOffset] != ' ')
                {
                    invalidOffset = separatorOffset;
                    invalidCardinality = true;
                }
                else if (separatorOffset + 1 >= value.Length || value[separatorOffset + 1] == ' ')
                {
                    invalidOffset = separatorOffset + 1;
                    invalidCardinality = true;
                }
                else
                {
                    offset = separatorOffset + 1;
                }
            }
            else if (token.NextOffset != value.Length)
            {
                invalidOffset = token.NextOffset;
                invalidCardinality = true;
            }
            tokenIndex = tokenIndex + 1;
        }

        if (invalidCardinality || tokenIndex != tokens.Length)
        {
            SdpSourceLocation cardinalityLocation = CreateOriginValueLocation(line, invalidOffset);
            context.AddDiagnostic(SdpDiagnosticCodes.ParseInvalidToken, SdpValidationSeverity.Error, SdpScope.Session, 'o', -1, 0, "The SDP origin field must contain exactly six tokens separated by single spaces.", cardinalityLocation);
            return;
        }

        SdpOrigin origin = new SdpOrigin();
        origin.Username = value.Slice(tokens[0].StartOffset, tokens[0].Length).ToString();
        origin.SessionId = value.Slice(tokens[1].StartOffset, tokens[1].Length).ToString();
        origin.SessionVersion = value.Slice(tokens[2].StartOffset, tokens[2].Length).ToString();
        origin.NetworkType = value.Slice(tokens[3].StartOffset, tokens[3].Length).ToString();
        origin.AddressType = value.Slice(tokens[4].StartOffset, tokens[4].Length).ToString();
        origin.UnicastAddress = value.Slice(tokens[5].StartOffset, tokens[5].Length).ToString();
        origin.AddressKind = ReadOriginAddressKind(origin.AddressType, origin.UnicastAddress);
        origin.RawText = value.ToString();
        origin.Location = SdpDiagnosticBuilder.CopySourceLocation(line.Location);
        context.Description.Origin = origin;

        ValidateOriginIdentifier(context, line, origin.SessionId, tokens[1], "session identifier");
        ValidateOriginIdentifier(context, line, origin.SessionVersion, tokens[2], "session version");
        ValidateOriginToken(context, line, origin.NetworkType, tokens[3], "network type");
        ValidateOriginToken(context, line, origin.AddressType, tokens[4], "address type");
    }

    internal static void ValidateOriginIdentifier(SdpParseContext context, SdpRawLine line, string value, SdpTokenReadResult token, string componentName)
    {
        SdpNumberParseResult number = SdpNumberParser.ParseCharacterUnsignedDecimal(value.AsSpan());
        if (!number.Success)
        {
            SdpSourceLocation location = CreateOriginValueLocation(line, token.StartOffset);
            context.AddDiagnostic(number.DiagnosticCode, SdpValidationSeverity.Error, SdpScope.Session, 'o', -1, 0, "The SDP origin " + componentName + " must be an unsigned 64-bit decimal integer.", location);
        }
    }

    internal static void ValidateOriginToken(SdpParseContext context, SdpRawLine line, string value, SdpTokenReadResult token, string componentName)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (!SdpCharacterClasses.IsTokenCharacter(value[index]))
            {
                SdpSourceLocation location = CreateOriginValueLocation(line, token.StartOffset + index);
                context.AddDiagnostic(SdpDiagnosticCodes.ParseInvalidToken, SdpValidationSeverity.Error, SdpScope.Session, 'o', -1, 0, "The SDP origin " + componentName + " contains a character outside the token grammar.", location);
                return;
            }
            index = index + 1;
        }
    }

    internal static SdpAddressKind ReadOriginAddressKind(string addressType, string address)
    {
        if (addressType == SdpAddressTypes.Ip4)
        {
            if (HasIpv4AddressShape(address))
            {
                return SdpAddressKind.Ipv4;
            }
            return SdpAddressKind.HostName;
        }
        if (addressType == SdpAddressTypes.Ip6)
        {
            if (address.IndexOf(':') >= 0)
            {
                return SdpAddressKind.Ipv6;
            }
            return SdpAddressKind.HostName;
        }
        return SdpAddressKind.Unknown;
    }

    internal static bool HasIpv4AddressShape(string address)
    {
        int dotCount = 0;
        int index = 0;
        while (index < address.Length)
        {
            char character = address[index];
            if (character == '.')
            {
                dotCount = dotCount + 1;
            }
            else if (character < '0' || character > '9')
            {
                return false;
            }
            index = index + 1;
        }
        return dotCount == 3;
    }

    internal static SdpSourceLocation CreateOriginValueLocation(SdpRawLine line, int relativeOffset)
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

    internal static void ParseSessionName(SdpParseContext context, SdpRawLine line)
    {
        if (context.Scope != SdpScope.Session)
        {
            context.AddDiagnostic(SdpDiagnosticCodes.ParseFieldInWrongScope, SdpValidationSeverity.Error, SdpScope.Media, 's', context.MediaIndex, 0, "The SDP session-name field is only permitted in session scope.", line.Location);
            return;
        }

        int occurrenceIndex = context.SessionNameCount;
        context.SessionNameCount = context.SessionNameCount + 1;
        if (occurrenceIndex > 0)
        {
            context.AddDiagnostic(SdpDiagnosticCodes.ParseDuplicateField, SdpValidationSeverity.Error, SdpScope.Session, 's', -1, occurrenceIndex, "The SDP session-name field may appear only once.", line.Location);
            return;
        }

        string value = line.Text.Substring(2);
        context.Description.HasSessionName = true;
        context.Description.SessionName = value;
        if (value.Length == 0 && context.Options.Mode != SdpParseMode.Strict)
        {
            SdpSourceLocation location = CreateTextFieldValueLocation(line, 0);
            context.AddDiagnostic(SdpDiagnosticCodes.ParseInvalidToken, SdpValidationSeverity.Warning, SdpScope.Session, 's', -1, 0, "The empty SDP session name was retained for compatibility with legacy implementations.", location);
            return;
        }
        ValidateRequiredText(context, line, value, SdpScope.Session, 's', -1, 0, "session name");
    }

    internal static void ParseInformation(SdpParseContext context, SdpRawLine line)
    {
        string value = line.Text.Substring(2);
        if (context.Scope == SdpScope.Media)
        {
            int mediaOccurrenceIndex = context.MediaInformationCount;
            context.MediaInformationCount = context.MediaInformationCount + 1;
            if (mediaOccurrenceIndex > 0)
            {
                context.AddDiagnostic(SdpDiagnosticCodes.ParseDuplicateField, SdpValidationSeverity.Error, SdpScope.Media, 'i', context.MediaIndex, mediaOccurrenceIndex, "A media description may contain only one information field.", line.Location);
                return;
            }
            if (context.CurrentMedia != null)
            {
                context.CurrentMedia.Information = value;
            }
            ValidateRequiredText(context, line, value, SdpScope.Media, 'i', context.MediaIndex, 0, "media information");
            return;
        }

        int occurrenceIndex = context.SessionInformationCount;
        context.SessionInformationCount = context.SessionInformationCount + 1;
        if (occurrenceIndex > 0)
        {
            context.AddDiagnostic(SdpDiagnosticCodes.ParseDuplicateField, SdpValidationSeverity.Error, SdpScope.Session, 'i', -1, occurrenceIndex, "The session may contain only one information field.", line.Location);
            return;
        }
        context.Description.Information = value;
        ValidateRequiredText(context, line, value, SdpScope.Session, 'i', -1, 0, "session information");
    }

    internal static void ParseUri(SdpParseContext context, SdpRawLine line)
    {
        if (context.Scope != SdpScope.Session)
        {
            context.AddDiagnostic(SdpDiagnosticCodes.ParseFieldInWrongScope, SdpValidationSeverity.Error, SdpScope.Media, 'u', context.MediaIndex, 0, "The SDP URI field is only permitted in session scope.", line.Location);
            return;
        }

        int occurrenceIndex = context.SessionUriCount;
        context.SessionUriCount = context.SessionUriCount + 1;
        if (occurrenceIndex > 0)
        {
            context.AddDiagnostic(SdpDiagnosticCodes.ParseDuplicateField, SdpValidationSeverity.Error, SdpScope.Session, 'u', -1, occurrenceIndex, "The SDP URI field may appear only once.", line.Location);
            return;
        }

        string value = line.Text.Substring(2);
        context.Description.Uri = value;
        ValidateUriReference(context, line, value);
    }

    internal static void ParseEmail(SdpParseContext context, SdpRawLine line)
    {
        if (context.Scope != SdpScope.Session)
        {
            context.AddDiagnostic(SdpDiagnosticCodes.ParseFieldInWrongScope, SdpValidationSeverity.Error, SdpScope.Media, 'e', context.MediaIndex, context.Description.Emails.Count, "SDP email fields must appear before the first media description.", line.Location);
            return;
        }

        string value = line.Text.Substring(2);
        int occurrenceIndex = context.Description.Emails.Count;
        context.Description.Emails.Add(value);
        ValidateRequiredText(context, line, value, SdpScope.Session, 'e', -1, occurrenceIndex, "email address");
    }

    internal static void ParsePhone(SdpParseContext context, SdpRawLine line)
    {
        if (context.Scope != SdpScope.Session)
        {
            context.AddDiagnostic(SdpDiagnosticCodes.ParseFieldInWrongScope, SdpValidationSeverity.Error, SdpScope.Media, 'p', context.MediaIndex, context.Description.Phones.Count, "SDP phone fields must appear before the first media description.", line.Location);
            return;
        }

        string value = line.Text.Substring(2);
        int occurrenceIndex = context.Description.Phones.Count;
        context.Description.Phones.Add(value);
        ValidateRequiredText(context, line, value, SdpScope.Session, 'p', -1, occurrenceIndex, "phone number");
    }

    internal static void ValidateRequiredText(SdpParseContext context, SdpRawLine line, string value, SdpScope scope, char fieldName, int mediaIndex, int occurrenceIndex, string componentName)
    {
        if (value.Length == 0)
        {
            SdpSourceLocation location = CreateTextFieldValueLocation(line, 0);
            context.AddDiagnostic(SdpDiagnosticCodes.ParseInvalidToken, SdpValidationSeverity.Error, scope, fieldName, mediaIndex, occurrenceIndex, "The SDP " + componentName + " must not be empty.", location);
        }
    }

    internal static void ValidateUriReference(SdpParseContext context, SdpRawLine line, string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '%')
            {
                if (index + 2 >= value.Length || !IsUriHexDigit(value[index + 1]) || !IsUriHexDigit(value[index + 2]))
                {
                    SdpSourceLocation escapeLocation = CreateTextFieldValueLocation(line, index);
                    context.AddDiagnostic(SdpDiagnosticCodes.ParseInvalidToken, SdpValidationSeverity.Error, SdpScope.Session, 'u', -1, 0, "The SDP URI contains an invalid percent-encoded octet.", escapeLocation);
                    return;
                }
                index = index + 3;
                continue;
            }
            if (!IsUriReferenceCharacter(character))
            {
                SdpSourceLocation characterLocation = CreateTextFieldValueLocation(line, index);
                context.AddDiagnostic(SdpDiagnosticCodes.ParseInvalidToken, SdpValidationSeverity.Error, SdpScope.Session, 'u', -1, 0, "The SDP URI contains a character outside the URI-reference grammar.", characterLocation);
                return;
            }
            index = index + 1;
        }
    }

    internal static bool IsUriReferenceCharacter(char value)
    {
        bool alphaNumeric = value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z' || value >= '0' && value <= '9';
        bool unreserved = value == '-' || value == '.' || value == '_' || value == '~';
        bool generalDelimiter = value == ':' || value == '/' || value == '?' || value == '#' || value == '[' || value == ']' || value == '@';
        bool subDelimiter = value == '!' || value == '$' || value == '&' || value == '\'' || value == '(' || value == ')' || value == '*' || value == '+' || value == ',' || value == ';' || value == '=';
        return alphaNumeric || unreserved || generalDelimiter || subDelimiter;
    }

    internal static bool IsUriHexDigit(char value)
    {
        return value >= '0' && value <= '9' || value >= 'A' && value <= 'F' || value >= 'a' && value <= 'f';
    }

    internal static SdpSourceLocation CreateTextFieldValueLocation(SdpRawLine line, int relativeOffset)
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
