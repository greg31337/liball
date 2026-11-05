using System;
using System.Text;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class SessionHeaderValidatorTests
{
    internal static readonly DateTime Now = new DateTime(2026, 8, 30, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public void MatchingCounterpartyHeaderAndQualifierAreAcceptedWithoutMutation()
    {
        FakeSessionClock clock = new FakeSessionClock(Now);
        SessionHeaderValidator validator = new SessionHeaderValidator(Settings(), clock);
        FixMessage message = ValidMessage("20260830-12:00:00.123456789012");

        SessionHeaderValidationResult result = validator.ValidateInboundHeader(message, "PRIMARY");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Issues, Is.Empty);
            Assert.That(result.IsPossibleDuplicate, Is.False);
            Assert.That(result.HasSendingTime, Is.True);
            Assert.That(result.SendingTimeUtc, Is.EqualTo(new DateTime(2026, 8, 30, 12, 0, 0, DateTimeKind.Utc).AddTicks(1234567)));
            Assert.That(result.HasOrigSendingTime, Is.False);
            Assert.That(clock.ReadCount, Is.EqualTo(1));
            Assert.That(Text(message, FixTags.SenderCompID), Is.EqualTo("SELL"));
        });
    }

    [TestCase(FixTags.BeginString, "begin-string-mismatch")]
    [TestCase(FixTags.SenderCompID, "sender-comp-id-mismatch")]
    [TestCase(FixTags.SenderSubID, "sender-sub-id-mismatch")]
    [TestCase(FixTags.SenderLocationID, "sender-location-id-mismatch")]
    [TestCase(FixTags.TargetCompID, "target-comp-id-mismatch")]
    [TestCase(FixTags.TargetSubID, "target-sub-id-mismatch")]
    [TestCase(FixTags.TargetLocationID, "target-location-id-mismatch")]
    public void EveryConfiguredInboundIdentityPartMustMatchInCounterpartyDirection(int tag, string code)
    {
        SessionHeaderValidator validator = Validator(Settings());
        FixMessage message = ValidMessage("20260830-12:00:00.000");
        SetText(message, tag, "WRONG");

        SessionHeaderValidationResult result = validator.ValidateInboundHeader(message, "PRIMARY");
        ValidationIssue issue = FindIssue(result, code);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(issue.Tag, Is.EqualTo(tag));
            Assert.That(issue.Layer, Is.EqualTo(ValidationLayer.SessionHeader));
            Assert.That(issue.HasSessionRejectReason, Is.True);
            Assert.That(issue.SessionRejectReason, Is.EqualTo(tag == FixTags.BeginString ? 5 : 9));
        });
    }

    [Test]
    public void QualifierMismatchIsReportedWithoutPretendingItIsAWireField()
    {
        SessionHeaderValidationResult result = Validator(Settings()).ValidateInboundHeader(ValidMessage("20260830-12:00:00.000"), "SECONDARY");
        ValidationIssue issue = FindIssue(result, "session-qualifier-mismatch");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(issue.HasTag, Is.False);
            Assert.That(issue.HasSessionRejectReason, Is.False);
        });
    }

    [Test]
    public void OptionalIdentifiersAreExactAndCompIdCheckingCanBeDisabled()
    {
        SessionSettings strictSettings = Settings();
        FixMessage missingOptional = ValidMessage("20260830-12:00:00.000");
        RemoveField(missingOptional, FixTags.SenderSubID);
        SessionHeaderValidationResult strict = Validator(strictSettings).ValidateInboundHeader(missingOptional, "PRIMARY");

        SessionSettings relaxedSettings = Settings();
        relaxedSettings.CheckCompIds = false;
        FixMessage allWrong = ValidMessage("20260830-12:00:00.000");
        SetText(allWrong, FixTags.SenderCompID, "WRONG");
        SetText(allWrong, FixTags.SenderSubID, "WRONG");
        SetText(allWrong, FixTags.SenderLocationID, "WRONG");
        SetText(allWrong, FixTags.TargetCompID, "WRONG");
        SetText(allWrong, FixTags.TargetSubID, "WRONG");
        SetText(allWrong, FixTags.TargetLocationID, "WRONG");
        SessionHeaderValidationResult relaxed = Validator(relaxedSettings).ValidateInboundHeader(allWrong, "PRIMARY");

        Assert.Multiple(() =>
        {
            Assert.That(FindIssue(strict, "sender-sub-id-mismatch").Tag, Is.EqualTo(FixTags.SenderSubID));
            Assert.That(relaxed.IsValid, Is.True);
            Assert.That(relaxed.Issues, Is.Empty);
        });
    }

    [Test]
    public void PossibleDuplicateRequiresValidOrderedOriginalSendingTime()
    {
        FixMessage validMessage = ValidMessage("20260830-12:00:00.123456789002");
        validMessage.HeaderFields.Add(Field(FixTags.PossDupFlag, "Y"));
        validMessage.HeaderFields.Add(Field(FixTags.OrigSendingTime, "20260830-11:59:59.999999999999"));
        SessionHeaderValidationResult valid = Validator(Settings()).ValidateInboundHeader(validMessage, "PRIMARY");

        FixMessage missingMessage = ValidMessage("20260830-12:00:00.000");
        missingMessage.HeaderFields.Add(Field(FixTags.PossDupFlag, "Y"));
        SessionHeaderValidationResult missing = Validator(Settings()).ValidateInboundHeader(missingMessage, "PRIMARY");

        FixMessage reversedMessage = ValidMessage("20260830-12:00:00.123456789001");
        reversedMessage.HeaderFields.Add(Field(FixTags.PossDupFlag, "Y"));
        reversedMessage.HeaderFields.Add(Field(FixTags.OrigSendingTime, "20260830-12:00:00.123456789002"));
        SessionHeaderValidationResult reversed = Validator(Settings()).ValidateInboundHeader(reversedMessage, "PRIMARY");

        Assert.Multiple(() =>
        {
            Assert.That(valid.IsValid, Is.True);
            Assert.That(valid.IsPossibleDuplicate, Is.True);
            Assert.That(valid.HasOrigSendingTime, Is.True);
            Assert.That(FindIssue(missing, "missing-orig-sending-time").SessionRejectReason, Is.EqualTo(1));
            Assert.That(FindIssue(reversed, "orig-sending-time-after-sending-time").SessionRejectReason, Is.EqualTo(10));
        });
    }

    [Test]
    public void DuplicateFlagsAndOriginalTimestampCombinationsAreValidated()
    {
        FixMessage invalidFlagMessage = ValidMessage("20260830-12:00:00.000");
        invalidFlagMessage.HeaderFields.Add(Field(FixTags.PossDupFlag, "X"));
        SessionHeaderValidationResult invalidFlag = Validator(Settings()).ValidateInboundHeader(invalidFlagMessage, "PRIMARY");

        FixMessage unexpectedOriginalMessage = ValidMessage("20260830-12:00:00.000");
        unexpectedOriginalMessage.HeaderFields.Add(Field(FixTags.PossDupFlag, "N"));
        unexpectedOriginalMessage.HeaderFields.Add(Field(FixTags.OrigSendingTime, "20260830-11:59:59.000"));
        SessionHeaderValidationResult unexpectedOriginal = Validator(Settings()).ValidateInboundHeader(unexpectedOriginalMessage, "PRIMARY");

        SessionSettings optionalSettings = Settings();
        optionalSettings.RequireOrigSendingTime = false;
        FixMessage optionalMessage = ValidMessage("20260830-12:00:00.000");
        optionalMessage.HeaderFields.Add(Field(FixTags.PossDupFlag, "Y"));
        SessionHeaderValidationResult optional = Validator(optionalSettings).ValidateInboundHeader(optionalMessage, "PRIMARY");

        Assert.Multiple(() =>
        {
            Assert.That(FindIssue(invalidFlag, "invalid-poss-dup-flag").SessionRejectReason, Is.EqualTo(5));
            Assert.That(FindIssue(unexpectedOriginal, "orig-sending-time-without-poss-dup").SessionRejectReason, Is.EqualTo(5));
            Assert.That(optional.IsValid, Is.True);
            Assert.That(optional.IsPossibleDuplicate, Is.True);
            Assert.That(optional.HasOrigSendingTime, Is.False);
        });
    }

    [Test]
    public void MissingAndMalformedTimestampsProduceRequiredAndFormatReasons()
    {
        FixMessage missingMessage = ValidMessage("20260830-12:00:00.000");
        RemoveField(missingMessage, FixTags.SendingTime);
        SessionHeaderValidationResult missing = Validator(Settings()).ValidateInboundHeader(missingMessage, "PRIMARY");

        FixMessage malformedMessage = ValidMessage("20260830-12:00:00.000");
        SetText(malformedMessage, FixTags.SendingTime, "00000830-12:00:00.000");
        malformedMessage.HeaderFields.Add(Field(FixTags.PossDupFlag, "Y"));
        malformedMessage.HeaderFields.Add(Field(FixTags.OrigSendingTime, "not-a-timestamp"));
        SessionHeaderValidationResult malformed = Validator(Settings()).ValidateInboundHeader(malformedMessage, "PRIMARY");

        Assert.Multiple(() =>
        {
            Assert.That(FindIssue(missing, "missing-sending-time").SessionRejectReason, Is.EqualTo(1));
            Assert.That(FindIssue(malformed, "invalid-sending-time").SessionRejectReason, Is.EqualTo(6));
            Assert.That(FindIssue(malformed, "invalid-orig-sending-time").SessionRejectReason, Is.EqualTo(6));
            Assert.That(malformed.HasSendingTime, Is.False);
            Assert.That(malformed.HasOrigSendingTime, Is.False);
        });
    }

    [TestCase("20260830-11:59:58.000", true)]
    [TestCase("20260830-12:00:02.000", true)]
    [TestCase("20260830-11:59:57.999", false)]
    [TestCase("20260830-12:00:02.001", false)]
    public void AbsoluteLatencyAcceptsBoundaryAndRejectsPastOrFutureExcess(string sendingTime, bool expectedValid)
    {
        SessionSettings settings = Settings();
        settings.MaxLatencySeconds = 2;

        SessionHeaderValidationResult result = Validator(settings).ValidateInboundHeader(ValidMessage(sendingTime), "PRIMARY");

        Assert.That(result.IsValid, Is.EqualTo(expectedValid));
        if (!expectedValid)
        {
            Assert.That(FindIssue(result, "sending-time-latency-exceeded").SessionRejectReason, Is.EqualTo(10));
        }
    }

    [Test]
    public void DisabledLatencyDoesNotReadClockAndNonUtcClockIsRejectedWhenEnabled()
    {
        SessionSettings disabledSettings = Settings();
        disabledSettings.CheckLatency = false;
        FakeSessionClock disabledClock = new FakeSessionClock(DateTime.SpecifyKind(Now, DateTimeKind.Local));
        SessionHeaderValidationResult disabled = new SessionHeaderValidator(disabledSettings, disabledClock).ValidateInboundHeader(ValidMessage("19990101-00:00:00.000"), "PRIMARY");

        FakeSessionClock invalidClock = new FakeSessionClock(DateTime.SpecifyKind(Now, DateTimeKind.Unspecified));
        SessionHeaderValidator invalidValidator = new SessionHeaderValidator(Settings(), invalidClock);

        Assert.Multiple(() =>
        {
            Assert.That(disabled.IsValid, Is.True);
            Assert.That(disabledClock.ReadCount, Is.Zero);
            Assert.Throws<FixSessionException>(() => invalidValidator.ValidateInboundHeader(ValidMessage("20260830-12:00:00.000"), "PRIMARY"));
        });
    }

    [Test]
    public void ConstructorValidatesDependenciesAndCopiesConfiguration()
    {
        SessionSettings settings = Settings();
        FakeSessionClock clock = new FakeSessionClock(Now);
        SessionHeaderValidator validator = new SessionHeaderValidator(settings, clock);
        settings.Id.SenderCompID = "CHANGED";
        settings.MaxLatencySeconds = 1;

        SessionHeaderValidationResult result = validator.ValidateInboundHeader(ValidMessage("20260830-12:01:00.000"), "PRIMARY");
        SessionSettings invalidLatency = Settings();
        invalidLatency.MaxLatencySeconds = 0;

        Assert.Multiple(() =>
        {
            Assert.That(result.Issues.Exists(issue => issue.Code == "target-comp-id-mismatch"), Is.False);
            Assert.That(result.Issues.Exists(issue => issue.Code == "sending-time-latency-exceeded"), Is.False);
            Assert.Throws<FixSessionException>(() => new SessionHeaderValidator(null!, clock));
            Assert.Throws<FixSessionException>(() => new SessionHeaderValidator(settings, null!));
            Assert.Throws<FixSessionException>(() => new SessionHeaderValidator(invalidLatency, clock));
        });
    }

    [Test]
    public void MissingMessageReturnsStableFailure()
    {
        SessionHeaderValidationResult result = Validator(Settings()).ValidateInboundHeader(null!, "PRIMARY");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues, Has.Count.EqualTo(1));
            Assert.That(result.Issues[0].Code, Is.EqualTo("missing-session-message"));
            Assert.That(result.Issues[0].Layer, Is.EqualTo(ValidationLayer.SessionHeader));
        });
    }

    internal static SessionHeaderValidator Validator(SessionSettings settings)
    {
        return new SessionHeaderValidator(settings, new FakeSessionClock(Now));
    }

    internal static SessionSettings Settings()
    {
        SessionSettings settings = new SessionSettings();
        settings.Id.BeginString = FixBeginStrings.Fixt11;
        settings.Id.SenderCompID = "BUY";
        settings.Id.SenderSubID = "ORDERS";
        settings.Id.SenderLocationID = "NY";
        settings.Id.TargetCompID = "SELL";
        settings.Id.TargetSubID = "MATCH";
        settings.Id.TargetLocationID = "LDN";
        settings.Id.Qualifier = "PRIMARY";
        return settings;
    }

    internal static FixMessage ValidMessage(string sendingTime)
    {
        FixMessage message = new FixMessage();
        message.BeginString = FixBeginStrings.Fixt11;
        message.MsgType = "D";
        message.HeaderFields.Add(Field(FixTags.BeginString, FixBeginStrings.Fixt11));
        message.HeaderFields.Add(Field(FixTags.SenderCompID, "SELL"));
        message.HeaderFields.Add(Field(FixTags.SenderSubID, "MATCH"));
        message.HeaderFields.Add(Field(FixTags.SenderLocationID, "LDN"));
        message.HeaderFields.Add(Field(FixTags.TargetCompID, "BUY"));
        message.HeaderFields.Add(Field(FixTags.TargetSubID, "ORDERS"));
        message.HeaderFields.Add(Field(FixTags.TargetLocationID, "NY"));
        message.HeaderFields.Add(Field(FixTags.SendingTime, sendingTime));
        return message;
    }

    internal static FixField Field(int tag, string value)
    {
        return new FixField { Tag = tag, Value = Encoding.Latin1.GetBytes(value), SourceOffset = tag };
    }

    internal static string Text(FixMessage message, int tag)
    {
        return Encoding.Latin1.GetString(message.HeaderFields.Find(field => field.Tag == tag)!.Value);
    }

    internal static void SetText(FixMessage message, int tag, string value)
    {
        FixField field = message.HeaderFields.Find(candidate => candidate.Tag == tag)!;
        field.Value = Encoding.Latin1.GetBytes(value);
        if (tag == FixTags.BeginString)
        {
            message.BeginString = value;
        }
    }

    internal static void RemoveField(FixMessage message, int tag)
    {
        message.HeaderFields.RemoveAll(field => field.Tag == tag);
    }

    internal static ValidationIssue FindIssue(SessionHeaderValidationResult result, string code)
    {
        ValidationIssue? issue = result.Issues.Find(candidate => candidate.Code == code);
        Assert.That(issue, Is.Not.Null, $"Expected issue '{code}'. Actual: {string.Join(",", result.Issues.ConvertAll(candidate => candidate.Code))}");
        return issue!;
    }
}
