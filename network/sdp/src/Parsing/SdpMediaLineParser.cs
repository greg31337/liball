using System;

namespace sdp;

internal static class SdpMediaLineParser
{
    internal static void ParseMedia(SdpParseContext context, SdpRawLine line)
    {
        if (context.CurrentMedia == null)
        {
            return;
        }

        ReadOnlySpan<char> value = line.Text.AsSpan().Slice(2);
        SdpMediaTokenizationResult tokenization = TokenizeMediaValue(context, value);
        if (tokenization.TokenLengthExceeded)
        {
            AddMediaDiagnostic(context, line, SdpDiagnosticCodes.ParseTokenLengthLimitExceeded, SdpValidationSeverity.Fatal, "An SDP media-field token exceeds the configured token-length limit.", tokenization.InvalidOffset);
            context.Stop = true;
            return;
        }
        if (!tokenization.Success || tokenization.Tokens.Count < 4)
        {
            int invalidOffset = tokenization.InvalidOffset;
            if (tokenization.Success)
            {
                invalidOffset = value.Length;
            }
            AddMediaDiagnostic(context, line, SdpDiagnosticCodes.ParseInvalidToken, SdpValidationSeverity.Error, "The SDP media field requires media, port, protocol, and at least one format token separated by single spaces.", invalidOffset);
            return;
        }

        SdpTokenReadResult mediaToken = tokenization.Tokens[0];
        SdpTokenReadResult portToken = tokenization.Tokens[1];
        SdpTokenReadResult protocolToken = tokenization.Tokens[2];
        context.CurrentMedia.MediaType = value.Slice(mediaToken.StartOffset, mediaToken.Length).ToString();
        context.CurrentMedia.MediaKind = ReadMediaKind(context.CurrentMedia.MediaType);
        context.CurrentMedia.Protocol = value.Slice(protocolToken.StartOffset, protocolToken.Length).ToString();
        int tokenIndex = 3;
        while (tokenIndex < tokenization.Tokens.Count)
        {
            SdpTokenReadResult formatToken = tokenization.Tokens[tokenIndex];
            context.CurrentMedia.Formats.Add(value.Slice(formatToken.StartOffset, formatToken.Length).ToString());
            tokenIndex = tokenIndex + 1;
        }

        ValidateToken(context, line, context.CurrentMedia.MediaType, mediaToken.StartOffset, "media type");
        ParsePort(context, line, value.Slice(portToken.StartOffset, portToken.Length), portToken.StartOffset);
        ValidateProtocol(context, line, context.CurrentMedia.Protocol, protocolToken.StartOffset);
        tokenIndex = 3;
        while (tokenIndex < tokenization.Tokens.Count)
        {
            SdpTokenReadResult formatToken = tokenization.Tokens[tokenIndex];
            string format = context.CurrentMedia.Formats[tokenIndex - 3];
            ValidateToken(context, line, format, formatToken.StartOffset, "media format");
            tokenIndex = tokenIndex + 1;
        }
    }

    internal static SdpMediaTokenizationResult TokenizeMediaValue(SdpParseContext context, ReadOnlySpan<char> value)
    {
        SdpMediaTokenizationResult result = new SdpMediaTokenizationResult();
        int offset = 0;
        while (offset < value.Length)
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
                result.TokenLengthExceeded = true;
                result.InvalidOffset = token.StartOffset;
                return result;
            }
            if (!token.Success)
            {
                result.InvalidOffset = offset;
                return result;
            }
            result.Tokens.Add(token);
            if (token.NextOffset == value.Length)
            {
                result.Success = true;
                return result;
            }
            if (value[token.NextOffset] != ' ' || token.NextOffset + 1 >= value.Length || value[token.NextOffset + 1] == ' ')
            {
                result.InvalidOffset = token.NextOffset;
                return result;
            }
            offset = token.NextOffset + 1;
        }
        result.InvalidOffset = offset;
        return result;
    }

    internal static void ParsePort(SdpParseContext context, SdpRawLine line, ReadOnlySpan<char> portText, int portOffset)
    {
        if (context.CurrentMedia == null)
        {
            return;
        }
        int slashOffset = portText.IndexOf('/');
        ReadOnlySpan<char> numberText = slashOffset < 0 ? portText : portText.Slice(0, slashOffset);
        SdpNumberParseResult port = SdpNumberParser.ParseCharacterPort(numberText);
        if (!port.Success)
        {
            AddMediaDiagnostic(context, line, port.DiagnosticCode, SdpValidationSeverity.Error, "The SDP media port must be a decimal value from zero through 65535.", portOffset);
        }
        else
        {
            context.CurrentMedia.Port = port.Int32Value;
        }

        if (slashOffset < 0)
        {
            return;
        }
        context.CurrentMedia.HasPortCount = true;
        ReadOnlySpan<char> countText = portText.Slice(slashOffset + 1);
        int secondSlashOffset = countText.IndexOf('/');
        if (secondSlashOffset >= 0)
        {
            AddMediaDiagnostic(context, line, SdpDiagnosticCodes.ParseInvalidToken, SdpValidationSeverity.Error, "The SDP media port token may contain only one port-count delimiter.", portOffset + slashOffset + 1 + secondSlashOffset);
            return;
        }
        SdpNumberParseResult count = SdpNumberParser.ParseCharacterUnsignedDecimal(countText);
        if (!count.Success)
        {
            AddMediaDiagnostic(context, line, count.DiagnosticCode, SdpValidationSeverity.Error, "The SDP media port count must be a positive decimal integer.", portOffset + slashOffset + 1);
            return;
        }
        if (count.UnsignedValue > int.MaxValue)
        {
            AddMediaDiagnostic(context, line, SdpDiagnosticCodes.ParseNumberOverflow, SdpValidationSeverity.Error, "The SDP media port count exceeds the supported integer representation.", portOffset + slashOffset + 1);
            return;
        }
        if (count.UnsignedValue == 0 || countText[0] == '0')
        {
            AddMediaDiagnostic(context, line, SdpDiagnosticCodes.ParseInvalidNumber, SdpValidationSeverity.Error, "The SDP media port count must be a positive decimal integer without leading zeroes.", portOffset + slashOffset + 1);
            return;
        }
        context.CurrentMedia.PortCount = (int)count.UnsignedValue;
    }

    internal static void ValidateProtocol(SdpParseContext context, SdpRawLine line, string protocol, int protocolOffset)
    {
        bool componentStart = true;
        int index = 0;
        while (index < protocol.Length)
        {
            char character = protocol[index];
            if (character == '/')
            {
                if (componentStart || index + 1 == protocol.Length)
                {
                    AddMediaDiagnostic(context, line, SdpDiagnosticCodes.ParseInvalidToken, SdpValidationSeverity.Error, "The SDP media protocol must contain nonempty slash-separated token components.", protocolOffset + index);
                    return;
                }
                componentStart = true;
            }
            else
            {
                if (!SdpCharacterClasses.IsTokenCharacter(character))
                {
                    AddMediaDiagnostic(context, line, SdpDiagnosticCodes.ParseInvalidToken, SdpValidationSeverity.Error, "The SDP media protocol contains a character outside the protocol grammar.", protocolOffset + index);
                    return;
                }
                componentStart = false;
            }
            index = index + 1;
        }
    }

    internal static void ValidateToken(SdpParseContext context, SdpRawLine line, string value, int valueOffset, string componentName)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (!SdpCharacterClasses.IsTokenCharacter(value[index]))
            {
                AddMediaDiagnostic(context, line, SdpDiagnosticCodes.ParseInvalidToken, SdpValidationSeverity.Error, "The SDP " + componentName + " contains a character outside the token grammar.", valueOffset + index);
                return;
            }
            index = index + 1;
        }
    }

    internal static SdpMediaKind ReadMediaKind(string mediaType)
    {
        if (string.Equals(mediaType, "audio", StringComparison.OrdinalIgnoreCase))
        {
            return SdpMediaKind.Audio;
        }
        if (string.Equals(mediaType, "video", StringComparison.OrdinalIgnoreCase))
        {
            return SdpMediaKind.Video;
        }
        if (string.Equals(mediaType, "text", StringComparison.OrdinalIgnoreCase))
        {
            return SdpMediaKind.Text;
        }
        if (string.Equals(mediaType, "application", StringComparison.OrdinalIgnoreCase))
        {
            return SdpMediaKind.Application;
        }
        if (string.Equals(mediaType, "message", StringComparison.OrdinalIgnoreCase))
        {
            return SdpMediaKind.Message;
        }
        return SdpMediaKind.Unknown;
    }

    internal static void AddMediaDiagnostic(SdpParseContext context, SdpRawLine line, string code, SdpValidationSeverity severity, string detail, int relativeOffset)
    {
        SdpSourceLocation location = CreateMediaValueLocation(line, relativeOffset);
        context.AddDiagnostic(code, severity, SdpScope.Media, 'm', context.MediaIndex, context.MediaIndex, detail, location);
    }

    internal static SdpSourceLocation CreateMediaValueLocation(SdpRawLine line, int relativeOffset)
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
