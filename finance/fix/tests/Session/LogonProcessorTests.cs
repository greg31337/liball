using System;
using System.Collections.Generic;
using System.Text;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class LogonProcessorTests
{
    internal static readonly DateTime Now = new DateTime(2026, 8, 30, 12, 0, 0, 123, DateTimeKind.Utc);

    [Test]
    public void InitiatorCreationIncludesNegotiationIdentityCredentialsAndResetSequence()
    {
        SessionSettings settings = Settings(SessionRole.Initiator, FixBeginStrings.Fixt11);
        settings.ResetMode = SessionResetMode.OnLogon;
        settings.Username = "operator";
        settings.Password = "secret";
        LogonProcessor processor = Processor(settings, new StubValidator(), new StubProvider(), new CapturingAuthenticator());
        LogonNegotiationState state = processor.CreateNegotiationState();

        FixMessage message = processor.CreateLogonMessage(47, 9, state);

        Assert.Multiple(() =>
        {
            Assert.That(message.BeginString, Is.EqualTo(FixBeginStrings.Fixt11));
            Assert.That(message.MsgType, Is.EqualTo(FixAdminMessageTypes.Logon));
            Assert.That(Text(message, FixTags.MsgSeqNum), Is.EqualTo("1"));
            Assert.That(Text(message, FixTags.SenderCompID), Is.EqualTo("LOCAL"));
            Assert.That(Text(message, FixTags.TargetCompID), Is.EqualTo("REMOTE"));
            Assert.That(Text(message, FixTags.SendingTime), Is.EqualTo("20260830-12:00:00.123"));
            Assert.That(Text(message, FixTags.EncryptMethod), Is.EqualTo("0"));
            Assert.That(Text(message, FixTags.HeartBtInt), Is.EqualTo("30"));
            Assert.That(Text(message, FixTags.ResetSeqNumFlag), Is.EqualTo("Y"));
            Assert.That(Text(message, FixTags.NextExpectedMsgSeqNum), Is.EqualTo("9"));
            Assert.That(Text(message, FixTags.DefaultApplVerID), Is.EqualTo(FixApplicationVersions.Fix50Sp2));
            Assert.That(Text(message, FixTags.Username), Is.EqualTo("operator"));
            Assert.That(Text(message, FixTags.Password), Is.EqualTo("secret"));
        });
    }

    [Test]
    public void AcceptorResponseUsesNegotiatedValuesWithoutClientCredentials()
    {
        SessionSettings settings = Settings(SessionRole.Acceptor, FixBeginStrings.Fixt11);
        settings.Username = "must-not-be-sent";
        settings.Password = "must-not-be-sent";
        LogonProcessor processor = Processor(settings, new StubValidator(), new StubProvider(), new CapturingAuthenticator());
        LogonNegotiationState state = processor.CreateNegotiationState();
        state.HeartbeatIntervalSeconds = 15;
        state.DefaultApplicationVersion = FixApplicationVersions.Fix50Sp1;

        FixMessage message = processor.CreateLogonMessage(4, 0, state);

        Assert.Multiple(() =>
        {
            Assert.That(Text(message, FixTags.MsgSeqNum), Is.EqualTo("4"));
            Assert.That(Text(message, FixTags.HeartBtInt), Is.EqualTo("15"));
            Assert.That(Text(message, FixTags.DefaultApplVerID), Is.EqualTo(FixApplicationVersions.Fix50Sp1));
            Assert.That(FindField(message, FixTags.NextExpectedMsgSeqNum), Is.Null);
            Assert.That(FindField(message, FixTags.Username), Is.Null);
            Assert.That(FindField(message, FixTags.Password), Is.Null);
            Assert.That(FindField(message, FixTags.ResetSeqNumFlag), Is.Null);
        });
    }

    [Test]
    public void CreatedFixtLogonEncodesAndDecodesWithTheEmbeddedTransportDictionary()
    {
        EmbeddedDictionaryProvider provider = new EmbeddedDictionaryProvider(null);
        DictionaryLoadResult transport = provider.ResolveDictionary(new FixDictionaryIdentity { BeginString = FixBeginStrings.Fixt11 });
        SessionSettings settings = Settings(SessionRole.Initiator, FixBeginStrings.Fixt11);
        settings.Username = "operator";
        settings.Password = "secret";
        LogonProcessor processor = new LogonProcessor(settings, transport.Dictionary!, new FixMessageValidator(), provider, new CapturingAuthenticator(), new FakeSessionClock(Now));
        FixMessage message = processor.CreateLogonMessage(1, 1, processor.CreateNegotiationState());
        TagValueMessageEncoder encoder = new TagValueMessageEncoder();
        EncodeOptions options = new EncodeOptions { PreserveFieldOrder = false };

        EncodeResult measured = encoder.EncodeMessage(message, transport.Dictionary!, Span<byte>.Empty, options);
        byte[] bytes = new byte[measured.RequiredSize];
        EncodeResult encoded = encoder.EncodeMessage(message, transport.Dictionary!, bytes, options);
        DecodeResult decoded = new TagValueMessageDecoder().DecodeMessage(bytes, transport.Dictionary!, new DecodeOptions());
        SessionSettings acceptorSettings = Settings(SessionRole.Acceptor, FixBeginStrings.Fixt11);
        acceptorSettings.Id.SenderCompID = "REMOTE";
        acceptorSettings.Id.TargetCompID = "LOCAL";
        acceptorSettings.Username = "operator";
        acceptorSettings.Password = "secret";
        LogonProcessor acceptor = new LogonProcessor(acceptorSettings, transport.Dictionary!, new FixMessageValidator(), provider, new BasicSessionAuthenticator(acceptorSettings, null), new FakeSessionClock(Now));
        LogonProcessingResult processed = acceptor.ProcessInboundLogon(decoded.Message!, "PRIMARY", "loopback", acceptor.CreateNegotiationState());

        Assert.Multiple(() =>
        {
            Assert.That(transport.Success, Is.True);
            Assert.That(encoded.Status, Is.EqualTo(EncodeStatus.Success), encoded.ErrorCode);
            Assert.That(decoded.Status, Is.EqualTo(DecodeStatus.Success), decoded.ErrorCode);
            Assert.That(decoded.Message!.MsgType, Is.EqualTo(FixAdminMessageTypes.Logon));
            Assert.That(Text(decoded.Message, FixTags.HeartBtInt), Is.EqualTo("30"));
            Assert.That(Text(decoded.Message, FixTags.DefaultApplVerID), Is.EqualTo(FixApplicationVersions.Fix50Sp2));
            Assert.That(Text(decoded.Message, FixTags.Username), Is.EqualTo("operator"));
            Assert.That(processed.Accepted, Is.True, processed.Code + ": " + processed.Text);
            Assert.That(processed.ApplicationDictionary, Is.Not.Null);
        });
    }

    [Test]
    public void AcceptorAtomicallyNegotiatesHeartbeatApplicationVersionAndNextExpectedSequence()
    {
        SessionSettings settings = Settings(SessionRole.Acceptor, FixBeginStrings.Fixt11);
        StubValidator validator = new StubValidator();
        StubProvider provider = new StubProvider();
        CapturingAuthenticator authenticator = new CapturingAuthenticator();
        LogonProcessor processor = Processor(settings, validator, provider, authenticator);
        LogonNegotiationState state = processor.CreateNegotiationState();
        state.NextSenderSeqNum = 20;
        state.NextTargetSeqNum = 30;
        FixMessage message = ValidInbound(settings, "15", FixApplicationVersions.Fix50Sp1);
        message.BodyFields.Add(Field(FixTags.NextExpectedMsgSeqNum, "7"));
        message.BodyFields.Add(Field(FixTags.Username, "operator"));
        message.BodyFields.Add(Field(FixTags.Password, "secret"));
        message.BodyFields.Add(new FixField { Tag = FixTags.RawDataLength, Value = Encoding.Latin1.GetBytes("3") });
        message.BodyFields.Add(new FixField { Tag = FixTags.RawData, Value = [0, 1, 255] });

        LogonProcessingResult result = processor.ProcessInboundLogon(message, "PRIMARY", "192.0.2.10", state);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Code, Is.EqualTo("logon-accepted"));
            Assert.That(result.HeartbeatIntervalSeconds, Is.EqualTo(15));
            Assert.That(result.DefaultApplicationVersion, Is.EqualTo(FixApplicationVersions.Fix50Sp1));
            Assert.That(result.HasNextExpectedMsgSeqNum, Is.True);
            Assert.That(result.NextExpectedMsgSeqNum, Is.EqualTo(7));
            Assert.That(result.ApplicationDictionary, Is.SameAs(provider.Dictionary));
            Assert.That(state.IsLoggedOn, Is.True);
            Assert.That(state.IsAuthenticated, Is.True);
            Assert.That(state.HeartbeatIntervalSeconds, Is.EqualTo(15));
            Assert.That(state.DefaultApplicationVersion, Is.EqualTo(FixApplicationVersions.Fix50Sp1));
            Assert.That(state.NextSenderSeqNum, Is.EqualTo(20));
            Assert.That(state.NextTargetSeqNum, Is.EqualTo(30));
            Assert.That(state.PeerNextExpectedMsgSeqNum, Is.EqualTo(7));
            Assert.That(provider.RequestedIdentity.ApplicationVersion, Is.EqualTo(FixApplicationVersions.Fix50Sp1));
            Assert.That(validator.CallCount, Is.EqualTo(1));
            Assert.That(authenticator.CallCount, Is.EqualTo(1));
            Assert.That(authenticator.Request!.Username, Is.EqualTo("operator"));
            Assert.That(authenticator.Request.Password, Is.EqualTo("secret"));
            Assert.That(authenticator.Request.RawData, Is.EqualTo(new byte[] { 0, 1, 255 }));
            Assert.That(authenticator.Request.RemoteAddress, Is.EqualTo("192.0.2.10"));
            Assert.That(authenticator.Request.LogonMessage, Is.SameAs(message));
            Assert.That(authenticator.Request.Id.SenderCompID, Is.EqualTo("LOCAL"));
        });
    }

    [Test]
    public void ResetLogonCommitsSequenceResetOnlyAfterEveryCheckSucceeds()
    {
        SessionSettings settings = Settings(SessionRole.Acceptor, FixBeginStrings.Fixt11);
        LogonProcessor processor = Processor(settings, new StubValidator(), new StubProvider(), new CapturingAuthenticator());
        LogonNegotiationState state = processor.CreateNegotiationState();
        state.NextSenderSeqNum = 40;
        state.NextTargetSeqNum = 50;
        FixMessage message = ValidInbound(settings, "30", FixApplicationVersions.Fix50Sp2);
        message.BodyFields.Add(Field(FixTags.ResetSeqNumFlag, "Y"));

        LogonProcessingResult result = processor.ProcessInboundLogon(message, "PRIMARY", "peer", state);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.ResetSequenceNumbers, Is.True);
            Assert.That(state.ResetSequenceNumbers, Is.True);
            Assert.That(state.NextSenderSeqNum, Is.EqualTo(FixDefaults.InitialSequenceNumber));
            Assert.That(state.NextTargetSeqNum, Is.EqualTo(FixDefaults.InitialSequenceNumber));
        });
    }

    [Test]
    public void MessageValidationFailurePreventsParsingResolutionAuthenticationAndMutation()
    {
        SessionSettings settings = Settings(SessionRole.Acceptor, FixBeginStrings.Fixt11);
        StubValidator validator = new StubValidator();
        validator.Result = new ValidationResult
        {
            IsValid = false,
            Issues = [new ValidationIssue { Layer = ValidationLayer.DictionaryGrammar, Severity = ValidationSeverity.Error, Code = "required-field-missing", Message = "A field is missing." }]
        };
        StubProvider provider = new StubProvider();
        CapturingAuthenticator authenticator = new CapturingAuthenticator();
        LogonProcessor processor = Processor(settings, validator, provider, authenticator);
        LogonNegotiationState state = SeededState(processor);

        LogonProcessingResult result = processor.ProcessInboundLogon(ValidInbound(settings, "15", FixApplicationVersions.Fix50Sp1), "PRIMARY", "peer", state);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Disconnect, Is.True);
            Assert.That(result.SendLogout, Is.True);
            Assert.That(result.Code, Is.EqualTo("required-field-missing"));
            Assert.That(provider.CallCount, Is.Zero);
            Assert.That(authenticator.CallCount, Is.Zero);
            AssertStateUnchanged(state);
        });
    }

    [TestCase(FixTags.EncryptMethod, "9", "unsupported-encrypt-method")]
    [TestCase(FixTags.EncryptMethod, "bad", "invalid-encrypt-method")]
    [TestCase(FixTags.HeartBtInt, "-1", "invalid-heart-bt-int")]
    [TestCase(FixTags.ResetSeqNumFlag, "X", "invalid-reset-seq-num-flag")]
    [TestCase(FixTags.NextExpectedMsgSeqNum, "0", "invalid-next-expected-msg-seq-num")]
    [TestCase(FixTags.DefaultApplVerID, "", "invalid-default-application-version")]
    public void InvalidLogonNegotiationFieldsRejectWithoutMutation(int tag, string value, string code)
    {
        SessionSettings settings = Settings(SessionRole.Acceptor, FixBeginStrings.Fixt11);
        CapturingAuthenticator authenticator = new CapturingAuthenticator();
        LogonProcessor processor = Processor(settings, new StubValidator(), new StubProvider(), authenticator);
        LogonNegotiationState state = SeededState(processor);
        FixMessage message = ValidInbound(settings, "30", FixApplicationVersions.Fix50Sp2);
        SetOrAdd(message, tag, value);

        LogonProcessingResult result = processor.ProcessInboundLogon(message, "PRIMARY", "peer", state);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Code, Is.EqualTo(code));
            Assert.That(result.Issues.Exists(issue => issue.Code == code), Is.True);
            Assert.That(authenticator.CallCount, Is.Zero);
            AssertStateUnchanged(state);
        });
    }

    [TestCase(FixTags.EncryptMethod, "missing-encrypt-method")]
    [TestCase(FixTags.HeartBtInt, "missing-heart-bt-int")]
    [TestCase(FixTags.DefaultApplVerID, "missing-default-application-version")]
    public void RequiredLogonNegotiationFieldsAreEnforced(int tag, string code)
    {
        SessionSettings settings = Settings(SessionRole.Acceptor, FixBeginStrings.Fixt11);
        LogonProcessor processor = Processor(settings, new StubValidator(), new StubProvider(), new CapturingAuthenticator());
        LogonNegotiationState state = SeededState(processor);
        FixMessage message = ValidInbound(settings, "30", FixApplicationVersions.Fix50Sp2);
        RemoveField(message, tag);

        LogonProcessingResult result = processor.ProcessInboundLogon(message, "PRIMARY", "peer", state);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(code));
            Assert.That(FindIssue(result, code).SessionRejectReason, Is.EqualTo(1));
            AssertStateUnchanged(state);
        });
    }

    [Test]
    public void ResetRequestRequiresMsgSeqNumOneBeforeResettingState()
    {
        SessionSettings settings = Settings(SessionRole.Acceptor, FixBeginStrings.Fixt11);
        LogonProcessor processor = Processor(settings, new StubValidator(), new StubProvider(), new CapturingAuthenticator());
        LogonNegotiationState state = SeededState(processor);
        FixMessage message = ValidInbound(settings, "30", FixApplicationVersions.Fix50Sp2);
        message.BodyFields.Add(Field(FixTags.ResetSeqNumFlag, "Y"));
        SetOrAdd(message, FixTags.MsgSeqNum, "2");

        LogonProcessingResult result = processor.ProcessInboundLogon(message, "PRIMARY", "peer", state);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo("reset-logon-sequence-number-not-one"));
            Assert.That(FindIssue(result, "reset-logon-sequence-number-not-one").SessionRejectReason, Is.EqualTo(5));
            AssertStateUnchanged(state);
        });
    }

    [Test]
    public void UnsupportedApplicationVersionRejectsBeforeAuthentication()
    {
        SessionSettings settings = Settings(SessionRole.Acceptor, FixBeginStrings.Fixt11);
        StubProvider provider = new StubProvider { Success = false };
        CapturingAuthenticator authenticator = new CapturingAuthenticator();
        LogonProcessor processor = Processor(settings, new StubValidator(), provider, authenticator);
        LogonNegotiationState state = SeededState(processor);

        LogonProcessingResult result = processor.ProcessInboundLogon(ValidInbound(settings, "30", "99"), "PRIMARY", "peer", state);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo("unsupported-default-application-version"));
            Assert.That(FindIssue(result, "unsupported-default-application-version").SessionRejectReason, Is.EqualTo(18));
            Assert.That(authenticator.CallCount, Is.Zero);
            AssertStateUnchanged(state);
        });
    }

    [Test]
    public void AuthenticationRejectionPreservesReasonAndAllNegotiatedState()
    {
        SessionSettings settings = Settings(SessionRole.Acceptor, FixBeginStrings.Fixt11);
        CapturingAuthenticator authenticator = new CapturingAuthenticator
        {
            Result = new AuthenticationResult { Authenticated = false, Disconnect = false, Code = "bad-credentials", Text = "Credentials were denied." }
        };
        LogonProcessor processor = Processor(settings, new StubValidator(), new StubProvider(), authenticator);
        LogonNegotiationState state = SeededState(processor);

        LogonProcessingResult result = processor.ProcessInboundLogon(ValidInbound(settings, "15", FixApplicationVersions.Fix50Sp1), "PRIMARY", "peer", state);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.SendLogout, Is.True);
            Assert.That(result.Disconnect, Is.False);
            Assert.That(result.Code, Is.EqualTo("bad-credentials"));
            Assert.That(result.Text, Is.EqualTo("Credentials were denied."));
            Assert.That(authenticator.CallCount, Is.EqualTo(1));
            AssertStateUnchanged(state);
        });
    }

    [Test]
    public void InitiatorRequiresHeartbeatConfirmationAndDoesNotAuthenticateAcceptorResponse()
    {
        SessionSettings settings = Settings(SessionRole.Initiator, FixBeginStrings.Fixt11);
        CapturingAuthenticator authenticator = new CapturingAuthenticator();
        LogonProcessor processor = Processor(settings, new StubValidator(), new StubProvider(), authenticator);
        LogonNegotiationState rejectedState = SeededState(processor);
        LogonProcessingResult rejected = processor.ProcessInboundLogon(ValidInbound(settings, "15", FixApplicationVersions.Fix50Sp2), "PRIMARY", "peer", rejectedState);

        LogonNegotiationState acceptedState = processor.CreateNegotiationState();
        LogonProcessingResult accepted = processor.ProcessInboundLogon(ValidInbound(settings, "30", FixApplicationVersions.Fix50Sp2), "PRIMARY", "peer", acceptedState);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Code, Is.EqualTo("heart-bt-int-mismatch"));
            AssertStateUnchanged(rejectedState);
            Assert.That(accepted.Accepted, Is.True);
            Assert.That(acceptedState.IsLoggedOn, Is.True);
            Assert.That(authenticator.CallCount, Is.Zero);
        });
    }

    [Test]
    public void ClassicFixLogonUsesBeginStringApplicationModelWithoutDefaultApplVerIdResolution()
    {
        SessionSettings settings = Settings(SessionRole.Acceptor, FixBeginStrings.Fix44);
        settings.DefaultApplicationVersion = FixApplicationVersions.Fix44;
        StubProvider provider = new StubProvider();
        LogonProcessor processor = Processor(settings, new StubValidator(), provider, new CapturingAuthenticator());
        LogonNegotiationState state = processor.CreateNegotiationState();
        FixMessage message = ValidInbound(settings, "0", "");
        RemoveField(message, FixTags.DefaultApplVerID);

        LogonProcessingResult result = processor.ProcessInboundLogon(message, "PRIMARY", "peer", state);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.HeartbeatIntervalSeconds, Is.Zero);
            Assert.That(result.DefaultApplicationVersion, Is.EqualTo(FixApplicationVersions.Fix44));
            Assert.That(result.ApplicationIdentity.BeginString, Is.EqualTo(FixBeginStrings.Fix44));
            Assert.That(result.ApplicationIdentity.ApplicationVersion, Is.Empty);
            Assert.That(result.ApplicationDictionary, Is.SameAs(processor.TransportDictionary));
            Assert.That(provider.CallCount, Is.Zero);
        });
    }

    [Test]
    public void RawDataLengthMismatchAndDuplicateFieldsAreRejected()
    {
        SessionSettings settings = Settings(SessionRole.Acceptor, FixBeginStrings.Fixt11);
        LogonProcessor processor = Processor(settings, new StubValidator(), new StubProvider(), new CapturingAuthenticator());
        FixMessage mismatchMessage = ValidInbound(settings, "30", FixApplicationVersions.Fix50Sp2);
        mismatchMessage.BodyFields.Add(Field(FixTags.RawDataLength, "2"));
        mismatchMessage.BodyFields.Add(new FixField { Tag = FixTags.RawData, Value = [1, 2, 3] });
        LogonProcessingResult mismatch = processor.ProcessInboundLogon(mismatchMessage, "PRIMARY", "peer", processor.CreateNegotiationState());

        FixMessage duplicateMessage = ValidInbound(settings, "30", FixApplicationVersions.Fix50Sp2);
        duplicateMessage.BodyFields.Add(Field(FixTags.HeartBtInt, "30"));
        LogonProcessingResult duplicate = processor.ProcessInboundLogon(duplicateMessage, "PRIMARY", "peer", processor.CreateNegotiationState());

        Assert.Multiple(() =>
        {
            Assert.That(mismatch.Code, Is.EqualTo("raw-data-length-mismatch"));
            Assert.That(FindIssue(mismatch, "raw-data-length-mismatch").SessionRejectReason, Is.EqualTo(5));
            Assert.That(duplicate.Code, Is.EqualTo("duplicate-logon-field"));
            Assert.That(FindIssue(duplicate, "duplicate-logon-field").SessionRejectReason, Is.EqualTo(13));
        });
    }

    [Test]
    public void CreationAndConstructionRejectInvalidDependenciesAndSequenceInputs()
    {
        SessionSettings settings = Settings(SessionRole.Initiator, FixBeginStrings.Fixt11);
        StubValidator validator = new StubValidator();
        StubProvider provider = new StubProvider();
        CapturingAuthenticator authenticator = new CapturingAuthenticator();
        FakeSessionClock clock = new FakeSessionClock(Now);
        LogonProcessor processor = new LogonProcessor(settings, new FixDictionary(), validator, provider, authenticator, clock);
        SessionSettings incomplete = Settings(SessionRole.Initiator, FixBeginStrings.Fixt11);
        incomplete.Validation = null!;

        Assert.Multiple(() =>
        {
            Assert.Throws<FixSessionException>(() => processor.CreateLogonMessage(0, 1, processor.CreateNegotiationState()));
            Assert.Throws<FixSessionException>(() => processor.CreateLogonMessage(1, -1, processor.CreateNegotiationState()));
            Assert.Throws<FixSessionException>(() => processor.CreateLogonMessage(1, 1, null!));
            Assert.Throws<FixSessionException>(() => new LogonProcessor(null!, new FixDictionary(), validator, provider, authenticator, clock));
            Assert.Throws<FixSessionException>(() => new LogonProcessor(settings, null!, validator, provider, authenticator, clock));
            Assert.Throws<FixSessionException>(() => new LogonProcessor(incomplete, new FixDictionary(), validator, provider, authenticator, clock));
        });
    }

    internal static LogonProcessor Processor(SessionSettings settings, StubValidator validator, StubProvider provider, CapturingAuthenticator authenticator)
    {
        FixDictionary transport = new FixDictionary();
        transport.Identity.BeginString = settings.Id.BeginString;
        return new LogonProcessor(settings, transport, validator, provider, authenticator, new FakeSessionClock(Now));
    }

    internal static SessionSettings Settings(SessionRole role, string beginString)
    {
        SessionSettings settings = new SessionSettings();
        settings.Role = role;
        settings.Id.BeginString = beginString;
        settings.Id.SenderCompID = "LOCAL";
        settings.Id.TargetCompID = "REMOTE";
        settings.Id.Qualifier = "PRIMARY";
        settings.TransportDictionary.BeginString = beginString;
        settings.ApplicationDictionary.ApplicationVersion = FixApplicationVersions.Fix50Sp2;
        settings.DefaultApplicationVersion = FixApplicationVersions.Fix50Sp2;
        settings.HeartbeatIntervalSeconds = 30;
        return settings;
    }

    internal static FixMessage ValidInbound(SessionSettings settings, string heartbeat, string applicationVersion)
    {
        FixMessage message = new FixMessage();
        message.BeginString = settings.Id.BeginString;
        message.MsgType = FixAdminMessageTypes.Logon;
        message.HeaderFields.Add(Field(FixTags.BeginString, settings.Id.BeginString));
        message.HeaderFields.Add(Field(FixTags.MsgType, FixAdminMessageTypes.Logon));
        message.HeaderFields.Add(Field(FixTags.MsgSeqNum, "1"));
        message.HeaderFields.Add(Field(FixTags.SenderCompID, settings.Id.TargetCompID));
        message.HeaderFields.Add(Field(FixTags.TargetCompID, settings.Id.SenderCompID));
        message.HeaderFields.Add(Field(FixTags.SendingTime, "20260830-12:00:00.123"));
        message.BodyFields.Add(Field(FixTags.EncryptMethod, "0"));
        message.BodyFields.Add(Field(FixTags.HeartBtInt, heartbeat));
        if (applicationVersion.Length != 0)
        {
            message.BodyFields.Add(Field(FixTags.DefaultApplVerID, applicationVersion));
        }
        return message;
    }

    internal static LogonNegotiationState SeededState(LogonProcessor processor)
    {
        LogonNegotiationState state = processor.CreateNegotiationState();
        state.HeartbeatIntervalSeconds = 77;
        state.DefaultApplicationVersion = "unchanged";
        state.NextSenderSeqNum = 41;
        state.NextTargetSeqNum = 51;
        state.HasPeerNextExpectedMsgSeqNum = true;
        state.PeerNextExpectedMsgSeqNum = 61;
        return state;
    }

    internal static void AssertStateUnchanged(LogonNegotiationState state)
    {
        Assert.Multiple(() =>
        {
            Assert.That(state.IsLoggedOn, Is.False);
            Assert.That(state.IsAuthenticated, Is.False);
            Assert.That(state.HeartbeatIntervalSeconds, Is.EqualTo(77));
            Assert.That(state.DefaultApplicationVersion, Is.EqualTo("unchanged"));
            Assert.That(state.NextSenderSeqNum, Is.EqualTo(41));
            Assert.That(state.NextTargetSeqNum, Is.EqualTo(51));
            Assert.That(state.HasPeerNextExpectedMsgSeqNum, Is.True);
            Assert.That(state.PeerNextExpectedMsgSeqNum, Is.EqualTo(61));
        });
    }

    internal static FixField Field(int tag, string value)
    {
        return new FixField { Tag = tag, Value = Encoding.Latin1.GetBytes(value) };
    }

    internal static FixField? FindField(FixMessage message, int tag)
    {
        FixField? field = message.HeaderFields.Find(candidate => candidate.Tag == tag);
        return field ?? message.BodyFields.Find(candidate => candidate.Tag == tag);
    }

    internal static string Text(FixMessage message, int tag)
    {
        FixField? field = FindField(message, tag);
        return field == null ? "" : Encoding.Latin1.GetString(field.Value);
    }

    internal static void SetOrAdd(FixMessage message, int tag, string value)
    {
        FixField? field = FindField(message, tag);
        if (field == null)
        {
            message.BodyFields.Add(Field(tag, value));
        }
        else
        {
            field.Value = Encoding.Latin1.GetBytes(value);
        }
    }

    internal static void RemoveField(FixMessage message, int tag)
    {
        message.HeaderFields.RemoveAll(field => field.Tag == tag);
        message.BodyFields.RemoveAll(field => field.Tag == tag);
    }

    internal static ValidationIssue FindIssue(LogonProcessingResult result, string code)
    {
        ValidationIssue? issue = result.Issues.Find(candidate => candidate.Code == code);
        Assert.That(issue, Is.Not.Null, $"Expected issue '{code}'.");
        return issue!;
    }

    internal class StubValidator : IMessageValidator
    {
        internal int CallCount;
        internal ValidationResult Result = new ValidationResult { IsValid = true };

        public ValidationResult ValidateMessage(FixMessage message, FixDictionary dictionary, ValidationOptions options, ValidationContext context)
        {
            CallCount++;
            return Result;
        }
    }

    internal class StubProvider : IDictionaryProvider
    {
        internal bool Success = true;
        internal int CallCount;
        internal FixDictionary Dictionary = new FixDictionary();
        internal FixDictionaryIdentity RequestedIdentity = new FixDictionaryIdentity();

        public DictionaryLoadResult ResolveDictionary(FixDictionaryIdentity identity)
        {
            CallCount++;
            RequestedIdentity = LogonProcessor.CopyIdentity(identity);
            return new DictionaryLoadResult { Success = Success, Dictionary = Success ? Dictionary : null };
        }
    }

    internal class CapturingAuthenticator : ISessionAuthenticator
    {
        internal int CallCount;
        internal AuthenticationRequest? Request;
        internal AuthenticationResult Result = new AuthenticationResult { Authenticated = true, Code = "authenticated" };

        public AuthenticationResult AuthenticateSession(AuthenticationRequest request)
        {
            CallCount++;
            Request = request;
            return Result;
        }
    }
}
