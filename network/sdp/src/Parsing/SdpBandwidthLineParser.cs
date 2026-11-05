using System;

namespace sdp;

internal static class SdpBandwidthLineParser
{
    internal static void ParseBandwidth(SdpParseContext context, SdpRawLine line)
    {
        int occurrenceIndex;
        if (context.Scope == SdpScope.Media)
        {
            occurrenceIndex = context.MediaBandwidthCount;
            context.MediaBandwidthCount = context.MediaBandwidthCount + 1;
        }
        else
        {
            occurrenceIndex = context.SessionBandwidthCount;
            context.SessionBandwidthCount = context.SessionBandwidthCount + 1;
        }

        ReadOnlySpan<char> text = line.Text.AsSpan().Slice(2);
        int colonOffset = text.IndexOf(':');
        if (colonOffset < 0)
        {
            if (IsBandwidthComponentTooLong(context, text))
            {
                AddBandwidthDiagnostic(context, line, occurrenceIndex, SdpDiagnosticCodes.ParseTokenLengthLimitExceeded, SdpValidationSeverity.Fatal, "The SDP bandwidth field contains an overlong token.", 0);
                context.Stop = true;
                return;
            }
            AddBandwidthDiagnostic(context, line, occurrenceIndex, SdpDiagnosticCodes.ParseInvalidToken, SdpValidationSeverity.Error, "The SDP bandwidth field must separate its type and value with a colon.", text.Length);
            return;
        }

        ReadOnlySpan<char> typeText = text.Slice(0, colonOffset);
        ReadOnlySpan<char> valueText = text.Slice(colonOffset + 1);
        if (IsBandwidthComponentTooLong(context, typeText))
        {
            AddBandwidthDiagnostic(context, line, occurrenceIndex, SdpDiagnosticCodes.ParseTokenLengthLimitExceeded, SdpValidationSeverity.Fatal, "The SDP bandwidth type exceeds the configured token-length limit.", 0);
            context.Stop = true;
            return;
        }
        if (IsBandwidthComponentTooLong(context, valueText))
        {
            AddBandwidthDiagnostic(context, line, occurrenceIndex, SdpDiagnosticCodes.ParseTokenLengthLimitExceeded, SdpValidationSeverity.Fatal, "The SDP bandwidth value exceeds the configured token-length limit.", colonOffset + 1);
            context.Stop = true;
            return;
        }

        SdpBandwidth bandwidth = new SdpBandwidth();
        bandwidth.Type = typeText.ToString();
        bandwidth.RawValue = valueText.ToString();
        bandwidth.RawText = text.ToString();
        bandwidth.Location = SdpDiagnosticBuilder.CopySourceLocation(line.Location);
        if (context.Scope == SdpScope.Media)
        {
            if (context.CurrentMedia != null)
            {
                context.CurrentMedia.Bandwidths.Add(bandwidth);
            }
        }
        else
        {
            context.Description.Bandwidths.Add(bandwidth);
        }

        ValidateBandwidthType(context, line, occurrenceIndex, bandwidth.Type);
        SdpNumberParseResult number = SdpNumberParser.ParseCharacterUnsignedDecimal(valueText);
        if (!number.Success)
        {
            AddBandwidthDiagnostic(context, line, occurrenceIndex, number.DiagnosticCode, SdpValidationSeverity.Error, "The SDP bandwidth value must be a nonnegative decimal integer.", colonOffset + 1);
            return;
        }
        if (number.UnsignedValue > long.MaxValue)
        {
            AddBandwidthDiagnostic(context, line, occurrenceIndex, SdpDiagnosticCodes.ParseNumberOverflow, SdpValidationSeverity.Error, "The SDP bandwidth value exceeds the supported integer representation.", colonOffset + 1);
            return;
        }
        bandwidth.Value = (long)number.UnsignedValue;
    }

    internal static void ValidateBandwidthType(SdpParseContext context, SdpRawLine line, int occurrenceIndex, string value)
    {
        if (value.Length == 0)
        {
            AddBandwidthDiagnostic(context, line, occurrenceIndex, SdpDiagnosticCodes.ParseInvalidToken, SdpValidationSeverity.Error, "The SDP bandwidth type must not be empty.", 0);
            return;
        }
        int index = 0;
        while (index < value.Length)
        {
            if (!SdpCharacterClasses.IsTokenCharacter(value[index]))
            {
                AddBandwidthDiagnostic(context, line, occurrenceIndex, SdpDiagnosticCodes.ParseInvalidToken, SdpValidationSeverity.Error, "The SDP bandwidth type contains a character outside the token grammar.", index);
                return;
            }
            index = index + 1;
        }
    }

    internal static bool IsBandwidthComponentTooLong(SdpParseContext context, ReadOnlySpan<char> value)
    {
        if (context.InputIsUtf8)
        {
            int byteLength = SdpLineReader.Utf8.GetByteCount(value);
            return byteLength > context.Options.MaximumTokenLength;
        }
        return value.Length > context.Options.MaximumTokenLength;
    }

    internal static void AddBandwidthDiagnostic(SdpParseContext context, SdpRawLine line, int occurrenceIndex, string code, SdpValidationSeverity severity, string detail, int relativeOffset)
    {
        SdpSourceLocation location = CreateBandwidthValueLocation(line, relativeOffset);
        context.AddDiagnostic(code, severity, context.Scope, 'b', context.MediaIndex, occurrenceIndex, detail, location);
    }

    internal static SdpSourceLocation CreateBandwidthValueLocation(SdpRawLine line, int relativeOffset)
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
