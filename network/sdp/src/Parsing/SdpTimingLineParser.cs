using System;

namespace sdp;

internal static class SdpTimingLineParser
{
    internal static void ParseTiming(SdpParseContext context, SdpRawLine line)
    {
        if (context.Scope != SdpScope.Session)
        {
            context.AddDiagnostic(SdpDiagnosticCodes.ParseFieldInWrongScope, SdpValidationSeverity.Error, SdpScope.Media, 't', context.MediaIndex, 0, "Timing fields are only permitted in session scope.", line.Location);
            return;
        }

        int occurrenceIndex = context.TimingFieldCount;
        context.TimingFieldCount = context.TimingFieldCount + 1;
        context.CurrentTiming = null;
        context.CanAttachRepeat = false;
        context.CurrentTimingHasZone = false;
        ReadOnlySpan<char> value = line.Text.AsSpan().Slice(2);
        SdpTimingTokenizationResult tokenization = TokenizeTimingValue(context, value);
        if (!ValidateTimingTokenization(context, line, 't', occurrenceIndex, tokenization, 2, 2))
        {
            return;
        }

        SdpTokenReadResult startToken = tokenization.Tokens[0];
        SdpTokenReadResult stopToken = tokenization.Tokens[1];
        SdpTiming timing = new SdpTiming();
        timing.StartTime = value.Slice(startToken.StartOffset, startToken.Length).ToString();
        timing.StopTime = value.Slice(stopToken.StartOffset, stopToken.Length).ToString();
        timing.RawText = value.ToString();
        timing.Location = SdpDiagnosticBuilder.CopySourceLocation(line.Location);
        context.Description.Timings.Add(timing);
        context.CurrentTiming = timing;
        context.CanAttachRepeat = true;

        ValidateNtpTime(context, line, 't', occurrenceIndex, timing.StartTime, startToken.StartOffset, true);
        ValidateNtpTime(context, line, 't', occurrenceIndex, timing.StopTime, stopToken.StartOffset, true);
    }

    internal static void ParseRepeat(SdpParseContext context, SdpRawLine line)
    {
        if (context.Scope != SdpScope.Session)
        {
            context.CanAttachRepeat = false;
            context.AddDiagnostic(SdpDiagnosticCodes.ParseFieldInWrongScope, SdpValidationSeverity.Error, SdpScope.Media, 'r', context.MediaIndex, 0, "Repeat fields are only permitted in session scope.", line.Location);
            return;
        }

        int occurrenceIndex = context.RepeatFieldCount;
        context.RepeatFieldCount = context.RepeatFieldCount + 1;
        if (context.CurrentTiming == null || !context.CanAttachRepeat)
        {
            context.CanAttachRepeat = false;
            SdpFieldOrderValidator.AddFieldOrderDiagnostic(context, line, 'r', occurrenceIndex, "A repeat field must immediately follow its timing field or another attached repeat field.");
            return;
        }

        ReadOnlySpan<char> value = line.Text.AsSpan().Slice(2);
        SdpTimingTokenizationResult tokenization = TokenizeTimingValue(context, value);
        if (!ValidateTimingTokenization(context, line, 'r', occurrenceIndex, tokenization, 3, int.MaxValue))
        {
            context.CanAttachRepeat = false;
            return;
        }

        SdpRepeatTime repeat = new SdpRepeatTime();
        repeat.RawText = value.ToString();
        repeat.Location = SdpDiagnosticBuilder.CopySourceLocation(line.Location);
        SdpTokenReadResult intervalToken = tokenization.Tokens[0];
        SdpTokenReadResult durationToken = tokenization.Tokens[1];
        repeat.RepeatInterval = ParseRepeatTypedTime(context, line, 'r', occurrenceIndex, value, intervalToken, true);
        repeat.ActiveDuration = ParseRepeatTypedTime(context, line, 'r', occurrenceIndex, value, durationToken, true);
        int tokenIndex = 2;
        while (tokenIndex < tokenization.Tokens.Count)
        {
            SdpTokenReadResult offsetToken = tokenization.Tokens[tokenIndex];
            SdpTypedTime offset = ParseRepeatTypedTime(context, line, 'r', occurrenceIndex, value, offsetToken, false);
            repeat.Offsets.Add(offset);
            tokenIndex = tokenIndex + 1;
        }
        context.CurrentTiming.RepeatTimes.Add(repeat);
        context.CanAttachRepeat = true;
    }

    internal static void ParseZone(SdpParseContext context, SdpRawLine line)
    {
        if (context.Scope != SdpScope.Session)
        {
            context.CanAttachRepeat = false;
            context.AddDiagnostic(SdpDiagnosticCodes.ParseFieldInWrongScope, SdpValidationSeverity.Error, SdpScope.Media, 'z', context.MediaIndex, 0, "Time-zone fields are only permitted in session scope.", line.Location);
            return;
        }

        int occurrenceIndex = context.ZoneFieldCount;
        context.ZoneFieldCount = context.ZoneFieldCount + 1;
        if (context.CurrentTiming == null || !context.CanAttachRepeat || context.CurrentTiming.RepeatTimes.Count == 0 || context.CurrentTimingHasZone)
        {
            context.CanAttachRepeat = false;
            SdpFieldOrderValidator.AddFieldOrderDiagnostic(context, line, 'z', occurrenceIndex, "A time-zone field may appear once after repeat fields attached to the current timing field.");
            return;
        }

        ReadOnlySpan<char> value = line.Text.AsSpan().Slice(2);
        SdpTimingTokenizationResult tokenization = TokenizeTimingValue(context, value);
        if (!ValidateTimingTokenization(context, line, 'z', occurrenceIndex, tokenization, 2, int.MaxValue))
        {
            context.CanAttachRepeat = false;
            return;
        }
        if (tokenization.Tokens.Count % 2 != 0)
        {
            context.CanAttachRepeat = false;
            AddTimingDiagnostic(context, line, 'z', occurrenceIndex, SdpDiagnosticCodes.ParseInvalidToken, SdpValidationSeverity.Error, "A time-zone field must contain adjustment-time and offset pairs.", value.Length);
            return;
        }

        int tokenIndex = 0;
        while (tokenIndex < tokenization.Tokens.Count)
        {
            SdpTokenReadResult timeToken = tokenization.Tokens[tokenIndex];
            SdpTokenReadResult offsetToken = tokenization.Tokens[tokenIndex + 1];
            string adjustmentTime = value.Slice(timeToken.StartOffset, timeToken.Length).ToString();
            string offsetText = value.Slice(offsetToken.StartOffset, offsetToken.Length).ToString();
            SdpTimeZoneAdjustment adjustment = new SdpTimeZoneAdjustment();
            adjustment.AdjustmentTime = adjustmentTime;
            adjustment.Location = CreateTimingValueLocation(line, timeToken.StartOffset);
            SdpTypedTimeParseResult parsedOffset = SdpTypedTimeParser.ParseCharacterSignedTypedTime(offsetText.AsSpan());
            parsedOffset.Value.RawValue = offsetText;
            adjustment.Offset = parsedOffset.Value;
            context.Description.TimeZoneAdjustments.Add(adjustment);

            ValidateNtpTime(context, line, 'z', occurrenceIndex, adjustmentTime, timeToken.StartOffset, false);
            if (!parsedOffset.Success)
            {
                AddTimingDiagnostic(context, line, 'z', occurrenceIndex, parsedOffset.DiagnosticCode, SdpValidationSeverity.Error, "A time-zone offset must be a signed typed-time value.", offsetToken.StartOffset);
            }
            tokenIndex = tokenIndex + 2;
        }
        context.CanAttachRepeat = false;
        context.CurrentTimingHasZone = true;
    }

    internal static SdpTypedTime ParseRepeatTypedTime(SdpParseContext context, SdpRawLine line, char fieldName, int occurrenceIndex, ReadOnlySpan<char> value, SdpTokenReadResult token, bool requirePositive)
    {
        ReadOnlySpan<char> tokenText = value.Slice(token.StartOffset, token.Length);
        SdpTypedTimeParseResult parsed = SdpTypedTimeParser.ParseCharacterTypedTime(tokenText);
        parsed.Value.RawValue = tokenText.ToString();
        if (!parsed.Success)
        {
            AddTimingDiagnostic(context, line, fieldName, occurrenceIndex, parsed.DiagnosticCode, SdpValidationSeverity.Error, "A repeat component must be an unsigned typed-time value.", token.StartOffset);
            return parsed.Value;
        }
        if (requirePositive && parsed.Value.Value == 0)
        {
            AddTimingDiagnostic(context, line, fieldName, occurrenceIndex, SdpDiagnosticCodes.ParseInvalidNumber, SdpValidationSeverity.Error, "The repeat interval and active duration must be positive typed-time values.", token.StartOffset);
        }
        return parsed.Value;
    }

    internal static void ValidateNtpTime(SdpParseContext context, SdpRawLine line, char fieldName, int occurrenceIndex, string value, int relativeOffset, bool allowZero)
    {
        if (allowZero && value == "0")
        {
            return;
        }
        bool valid = value.Length >= 10 && value[0] >= '1' && value[0] <= '9';
        int index = 1;
        while (index < value.Length && valid)
        {
            if (value[index] < '0' || value[index] > '9')
            {
                valid = false;
            }
            index = index + 1;
        }
        if (!valid)
        {
            AddTimingDiagnostic(context, line, fieldName, occurrenceIndex, SdpDiagnosticCodes.ParseInvalidNumber, SdpValidationSeverity.Error, "An SDP NTP time must be zero where allowed or a nonzero decimal value containing at least ten digits.", relativeOffset);
        }
    }

    internal static SdpTimingTokenizationResult TokenizeTimingValue(SdpParseContext context, ReadOnlySpan<char> value)
    {
        SdpTimingTokenizationResult result = new SdpTimingTokenizationResult();
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

    internal static bool ValidateTimingTokenization(SdpParseContext context, SdpRawLine line, char fieldName, int occurrenceIndex, SdpTimingTokenizationResult tokenization, int minimumCount, int maximumCount)
    {
        if (tokenization.TokenLengthExceeded)
        {
            AddTimingDiagnostic(context, line, fieldName, occurrenceIndex, SdpDiagnosticCodes.ParseTokenLengthLimitExceeded, SdpValidationSeverity.Fatal, "An SDP timing token exceeds the configured token-length limit.", tokenization.InvalidOffset);
            context.Stop = true;
            return false;
        }
        if (!tokenization.Success || tokenization.Tokens.Count < minimumCount || tokenization.Tokens.Count > maximumCount)
        {
            int diagnosticOffset = tokenization.InvalidOffset;
            if (tokenization.Success && tokenization.Tokens.Count < minimumCount)
            {
                diagnosticOffset = line.Text.Length - 2;
            }
            if (tokenization.Success && tokenization.Tokens.Count > maximumCount)
            {
                diagnosticOffset = tokenization.Tokens[maximumCount].StartOffset;
            }
            AddTimingDiagnostic(context, line, fieldName, occurrenceIndex, SdpDiagnosticCodes.ParseInvalidToken, SdpValidationSeverity.Error, "The SDP timing field contains an invalid number of space-separated components.", diagnosticOffset);
            return false;
        }
        return true;
    }

    internal static void AddTimingDiagnostic(SdpParseContext context, SdpRawLine line, char fieldName, int occurrenceIndex, string code, SdpValidationSeverity severity, string detail, int relativeOffset)
    {
        SdpSourceLocation location = CreateTimingValueLocation(line, relativeOffset);
        context.AddDiagnostic(code, severity, context.Scope, fieldName, context.MediaIndex, occurrenceIndex, detail, location);
    }

    internal static SdpSourceLocation CreateTimingValueLocation(SdpRawLine line, int relativeOffset)
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
