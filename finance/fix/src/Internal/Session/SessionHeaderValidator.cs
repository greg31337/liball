using System;
using System.Text;

namespace fix;

internal class SessionHeaderValidator
{
    internal SessionId ExpectedId = new SessionId();
    internal ISessionClock Clock;
    internal bool CheckCompIds;
    internal bool CheckLatency;
    internal int MaxLatencySeconds;
    internal bool RequireOrigSendingTime;

    internal SessionHeaderValidator(SessionSettings settings, ISessionClock clock)
    {
        if (settings == null || settings.Id == null)
        {
            throw new FixSessionException("Session header validation settings are missing.");
        }
        if (clock == null)
        {
            throw new FixSessionException("The session header validation clock is missing.");
        }
        if (settings.CheckLatency && settings.MaxLatencySeconds <= 0)
        {
            throw new FixSessionException("The maximum session latency must be positive when latency checking is enabled.");
        }
        ExpectedId = CopySessionId(settings.Id);
        Clock = clock;
        CheckCompIds = settings.CheckCompIds;
        CheckLatency = settings.CheckLatency;
        MaxLatencySeconds = settings.MaxLatencySeconds;
        RequireOrigSendingTime = settings.RequireOrigSendingTime;
    }

    internal SessionHeaderValidationResult ValidateInboundHeader(FixMessage message, string qualifier)
    {
        SessionHeaderValidationResult result = new SessionHeaderValidationResult();
        if (message == null)
        {
            AddIssue(result, null, "missing-session-message", "The inbound session message is missing.", false, 0, null);
            CompleteValidation(result, null);
            return result;
        }

        ValidateBeginString(message, result);
        if (CheckCompIds)
        {
            ValidateIdentityFields(message, result);
        }
        if (!TextEquals(ExpectedId.Qualifier, qualifier))
        {
            AddIssue(result, message, "session-qualifier-mismatch", "The inbound session qualifier does not match the configured session.", false, 0, null);
        }
        ValidateTimeFields(message, result);
        CompleteValidation(result, message);
        return result;
    }

    internal void ValidateBeginString(FixMessage message, SessionHeaderValidationResult result)
    {
        FixField? field = FindHeaderField(message, FixTags.BeginString);
        string actual = field == null ? message.BeginString : ReadText(field);
        if (!TextEquals(ExpectedId.BeginString, actual))
        {
            AddIssue(result, message, "begin-string-mismatch", "BeginString does not match the configured session.", true, FixTags.BeginString, field);
        }
    }

    internal void ValidateIdentityFields(FixMessage message, SessionHeaderValidationResult result)
    {
        ValidateIdentityField(message, result, FixTags.SenderCompID, ExpectedId.TargetCompID, "sender-comp-id-mismatch", "SenderCompID does not match the configured counterparty.");
        ValidateIdentityField(message, result, FixTags.SenderSubID, ExpectedId.TargetSubID, "sender-sub-id-mismatch", "SenderSubID does not match the configured counterparty.");
        ValidateIdentityField(message, result, FixTags.SenderLocationID, ExpectedId.TargetLocationID, "sender-location-id-mismatch", "SenderLocationID does not match the configured counterparty.");
        ValidateIdentityField(message, result, FixTags.TargetCompID, ExpectedId.SenderCompID, "target-comp-id-mismatch", "TargetCompID does not match the configured local identity.");
        ValidateIdentityField(message, result, FixTags.TargetSubID, ExpectedId.SenderSubID, "target-sub-id-mismatch", "TargetSubID does not match the configured local identity.");
        ValidateIdentityField(message, result, FixTags.TargetLocationID, ExpectedId.SenderLocationID, "target-location-id-mismatch", "TargetLocationID does not match the configured local identity.");
    }

    internal void ValidateIdentityField(FixMessage message, SessionHeaderValidationResult result, int tag, string expected, string code, string text)
    {
        FixField? field = FindHeaderField(message, tag);
        string actual = field == null ? "" : ReadText(field);
        if (!TextEquals(expected, actual))
        {
            AddIssue(result, message, code, text, true, tag, field);
        }
    }

    internal void ValidateTimeFields(FixMessage message, SessionHeaderValidationResult result)
    {
        FixField? possibleDuplicateField = FindHeaderField(message, FixTags.PossDupFlag);
        bool possibleDuplicateValid = true;
        if (possibleDuplicateField != null)
        {
            if (possibleDuplicateField.Value.Length == 1 && possibleDuplicateField.Value[0] == 89)
            {
                result.IsPossibleDuplicate = true;
            }
            else if (possibleDuplicateField.Value.Length != 1 || possibleDuplicateField.Value[0] != 78)
            {
                possibleDuplicateValid = false;
                AddIssue(result, message, "invalid-poss-dup-flag", "PossDupFlag must contain Y or N.", true, FixTags.PossDupFlag, possibleDuplicateField);
            }
        }

        FixField? sendingField = FindHeaderField(message, FixTags.SendingTime);
        SessionHeaderTimestampResult? sending = null;
        if (sendingField == null)
        {
            AddIssue(result, message, "missing-sending-time", "SendingTime is required in an inbound FIX header.", true, FixTags.SendingTime, null);
        }
        else
        {
            sending = ParseHeaderTimestamp(sendingField);
            if (!sending.Success)
            {
                AddIssue(result, message, "invalid-sending-time", "SendingTime is not a valid UTC timestamp.", true, FixTags.SendingTime, sendingField);
            }
            else
            {
                result.HasSendingTime = true;
                result.SendingTimeUtc = sending.ValueUtc;
                ValidateLatency(message, result, sendingField, sending.ValueUtc);
            }
        }

        FixField? originalField = FindHeaderField(message, FixTags.OrigSendingTime);
        SessionHeaderTimestampResult? original = null;
        if (originalField == null)
        {
            if (result.IsPossibleDuplicate && RequireOrigSendingTime)
            {
                AddIssue(result, message, "missing-orig-sending-time", "OrigSendingTime is required when PossDupFlag is Y.", true, FixTags.OrigSendingTime, null);
            }
        }
        else
        {
            original = ParseHeaderTimestamp(originalField);
            if (!original.Success)
            {
                AddIssue(result, message, "invalid-orig-sending-time", "OrigSendingTime is not a valid UTC timestamp.", true, FixTags.OrigSendingTime, originalField);
            }
            else
            {
                result.HasOrigSendingTime = true;
                result.OrigSendingTimeUtc = original.ValueUtc;
            }
            if (possibleDuplicateValid && !result.IsPossibleDuplicate)
            {
                AddIssue(result, message, "orig-sending-time-without-poss-dup", "OrigSendingTime is permitted only when PossDupFlag is Y.", true, FixTags.OrigSendingTime, originalField);
            }
        }

        if (sending != null && sending.Success && original != null && original.Success && CompareTimestamps(original.Parsed, sending.Parsed) > 0)
        {
            AddIssue(result, message, "orig-sending-time-after-sending-time", "OrigSendingTime must not be later than SendingTime.", true, FixTags.OrigSendingTime, originalField);
        }
    }

    internal void ValidateLatency(FixMessage message, SessionHeaderValidationResult result, FixField field, DateTime sendingTimeUtc)
    {
        if (!CheckLatency)
        {
            return;
        }
        DateTime now = Clock.GetUtcNow();
        if (now.Kind != DateTimeKind.Utc)
        {
            throw new FixSessionException("The session clock returned a non-UTC value.");
        }
        long elapsedTicks = now.Ticks - sendingTimeUtc.Ticks;
        if (elapsedTicks < 0)
        {
            elapsedTicks = -elapsedTicks;
        }
        long maximumTicks = (long)MaxLatencySeconds * TimeSpan.TicksPerSecond;
        if (elapsedTicks > maximumTicks)
        {
            AddIssue(result, message, "sending-time-latency-exceeded", "SendingTime is outside the configured absolute latency tolerance.", true, FixTags.SendingTime, field);
        }
    }

    internal static SessionHeaderTimestampResult ParseHeaderTimestamp(FixField field)
    {
        SessionHeaderTimestampResult result = new SessionHeaderTimestampResult();
        result.Parsed = TimestampParser.ParseTimestamp(field.Value, FixFieldType.UtcTimestamp, FixDefaults.MaxFieldSize);
        if (!result.Parsed.Success || result.Parsed.Year < 1 || result.Parsed.Year > 9999)
        {
            return result;
        }
        if (result.Parsed.Second == 60 && result.Parsed.Year == 9999 && result.Parsed.Month == 12 && result.Parsed.Day == 31)
        {
            return result;
        }
        int second = result.Parsed.Second == 60 ? 59 : result.Parsed.Second;
        DateTime value = new DateTime(result.Parsed.Year, result.Parsed.Month, result.Parsed.Day, result.Parsed.Hour, result.Parsed.Minute, second, DateTimeKind.Utc);
        if (result.Parsed.Second == 60)
        {
            value = value.AddSeconds(1);
        }
        value = value.AddTicks(FractionTicks(result.Parsed.Fraction));
        result.Success = true;
        result.ValueUtc = value;
        return result;
    }

    internal static long FractionTicks(string fraction)
    {
        long ticks = 0;
        int digits = fraction.Length < 7 ? fraction.Length : 7;
        for (int index = 0; index < digits; index++)
        {
            ticks = ticks * 10 + fraction[index] - 48;
        }
        for (int index = digits; index < 7; index++)
        {
            ticks *= 10;
        }
        return ticks;
    }

    internal static int CompareTimestamps(TimestampParseResult left, TimestampParseResult right)
    {
        int comparison = CompareInteger(left.Year, right.Year);
        if (comparison != 0) return comparison;
        comparison = CompareInteger(left.Month, right.Month);
        if (comparison != 0) return comparison;
        comparison = CompareInteger(left.Day, right.Day);
        if (comparison != 0) return comparison;
        comparison = CompareInteger(left.Hour, right.Hour);
        if (comparison != 0) return comparison;
        comparison = CompareInteger(left.Minute, right.Minute);
        if (comparison != 0) return comparison;
        comparison = CompareInteger(left.Second, right.Second);
        if (comparison != 0) return comparison;
        for (int index = 0; index < 12; index++)
        {
            int leftDigit = index < left.Fraction.Length ? left.Fraction[index] - 48 : 0;
            int rightDigit = index < right.Fraction.Length ? right.Fraction[index] - 48 : 0;
            comparison = CompareInteger(leftDigit, rightDigit);
            if (comparison != 0) return comparison;
        }
        return 0;
    }

    internal static int CompareInteger(int left, int right)
    {
        if (left < right) return -1;
        if (left > right) return 1;
        return 0;
    }

    internal static FixField? FindHeaderField(FixMessage message, int tag)
    {
        return EnvelopeValidator.FindFirstSectionField(message.HeaderFields, tag);
    }

    internal static string ReadText(FixField field)
    {
        return Encoding.Latin1.GetString(field.Value);
    }

    internal static bool TextEquals(string expected, string actual)
    {
        return string.Equals(expected, actual, StringComparison.Ordinal);
    }

    internal static SessionId CopySessionId(SessionId source)
    {
        SessionId copy = new SessionId();
        copy.BeginString = source.BeginString;
        copy.SenderCompID = source.SenderCompID;
        copy.SenderSubID = source.SenderSubID;
        copy.SenderLocationID = source.SenderLocationID;
        copy.TargetCompID = source.TargetCompID;
        copy.TargetSubID = source.TargetSubID;
        copy.TargetLocationID = source.TargetLocationID;
        copy.Qualifier = source.Qualifier;
        return copy;
    }

    internal static void AddIssue(SessionHeaderValidationResult result, FixMessage? message, string code, string text, bool hasTag, int tag, FixField? field)
    {
        ValidationIssue issue = new ValidationIssue();
        issue.Severity = ValidationSeverity.Error;
        issue.Layer = ValidationLayer.SessionHeader;
        issue.Code = code;
        issue.Message = text;
        issue.HasTag = hasTag;
        issue.Tag = tag;
        if (message != null)
        {
            issue.MsgType = message.MsgType;
        }
        if (field != null && field.SourceOffset >= 0)
        {
            issue.HasSourceOffset = true;
            issue.SourceOffset = field.SourceOffset;
        }
        result.Issues.Add(issue);
    }

    internal static void CompleteValidation(SessionHeaderValidationResult result, FixMessage? message)
    {
        result.IsValid = result.Issues.Count == 0;
        if (message != null)
        {
            RejectMapper.MapValidationIssues(result.Issues, message);
        }
    }
}
