using System;
using System.Collections.Generic;
using System.Text;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class FixSessionConformanceTests
{
    private static readonly string[] AllSystemScenarios = ["2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20"];

    internal static IEnumerable<TestCaseData> PublishedScenarios()
    {
        yield return Case("1B", false, SessionRole.Initiator);
        yield return Case("1B", true, SessionRole.Initiator);
        yield return Case("1S", false, SessionRole.Acceptor);
        yield return Case("1S", true, SessionRole.Acceptor);
        yield return Case("2S", false, SessionRole.Acceptor);
        yield return Case("2S", true, SessionRole.Acceptor);
        for (int index = 0; index < AllSystemScenarios.Length; index++)
        {
            yield return Case(AllSystemScenarios[index], false, SessionRole.Initiator);
            yield return Case(AllSystemScenarios[index], false, SessionRole.Acceptor);
            yield return Case(AllSystemScenarios[index], true, SessionRole.Initiator);
            yield return Case(AllSystemScenarios[index], true, SessionRole.Acceptor);
        }
    }

    [TestCaseSource(nameof(PublishedScenarios))]
    public void June2020SessionScenarioConforms(string scenario, bool fixt, SessionRole role)
    {
        if (scenario == "1B" || scenario == "1S") AssertLogonScenario(fixt, role);
        else if (scenario == "2S") AssertMessageBeforeLogon(fixt, role);
        else if (scenario == "2") AssertStandardHeader(fixt, role);
        else if (scenario == "3") AssertStandardTrailer(fixt, role);
        else if (scenario == "4" || scenario == "5" || scenario == "6") AssertHeartbeatScenario(scenario, fixt, role);
        else if (scenario == "7") AssertRejectScenario(fixt, role);
        else if (scenario == "8") AssertResendScenario(fixt, role);
        else if (scenario == "9") AssertSequenceSynchronization();
        else if (scenario == "10") AssertGapFillScenario();
        else if (scenario == "11") AssertResetScenario();
        else if (scenario == "12" || scenario == "13") AssertLogoutScenario(scenario, fixt, role);
        else if (scenario == "14") AssertMessageValidationReasons();
        else if (scenario == "15") AssertFieldOrderScenario(fixt, role);
        else if (scenario == "16") AssertQueueScenario(fixt, role);
        else if (scenario == "17") AssertEncryptionScenario(fixt, role);
        else if (scenario == "18") AssertThirdPartyAddressingScenario();
        else if (scenario == "19") AssertPossibleResendScenario(fixt, role);
        else if (scenario == "20") AssertSimultaneousResendScenario(fixt, role);
        else Assert.Fail("Unknown conformance scenario " + scenario);
    }

    private static TestCaseData Case(string scenario, bool fixt, SessionRole role)
    {
        string profile = fixt ? "FIXT" : "FIX4";
        return new TestCaseData(scenario, fixt, role).SetName("Scenario_" + scenario + "_" + profile + "_" + role);
    }

    private static void AssertLogonScenario(bool fixt, SessionRole role)
    {
        ConformanceFixture fixture = new ConformanceFixture(fixt, role, false);
        SessionPollResult started = fixture.Session.StartSession();
        if (role == SessionRole.Initiator)
        {
            Assert.That(started.Snapshot.OutboundQueueCount, Is.EqualTo(1));
            fixture.FlushOutbound();
        }
        else
        {
            Assert.That(started.Snapshot.OutboundQueueCount, Is.Zero);
        }
        SessionPollResult active = fixture.Receive(fixture.PeerFactory.CreateLogonMessage(1, 1, false));
        Assert.Multiple(() =>
        {
            Assert.That(active.State, Is.EqualTo(SessionState.Active), fixture.EventText(active));
            Assert.That(active.Snapshot.NextTargetSeqNum, Is.EqualTo(2));
            Assert.That(fixture.Application.CallbackOrder, Does.Contain("logon"));
        });

        ConformanceFixture invalid = new ConformanceFixture(fixt, role, false);
        invalid.Session.StartSession();
        if (role == SessionRole.Initiator) invalid.FlushOutbound();
        FixMessage bad = invalid.PeerFactory.CreateLogonMessage(1, 1, false);
        RemoveField(bad, FixTags.HeartBtInt);
        SessionPollResult rejected = invalid.Receive(bad);
        Assert.Multiple(() =>
        {
            Assert.That(rejected.Events.Exists(item => item.Code == "missing-heart-bt-int"), Is.True);
            Assert.That(rejected.Snapshot.OutboundQueueCount, Is.EqualTo(1));
        });
    }

    private static void AssertMessageBeforeLogon(bool fixt, SessionRole role)
    {
        ConformanceFixture fixture = new ConformanceFixture(fixt, role, false);
        fixture.Session.StartSession();
        SessionPollResult result = fixture.Receive(fixture.PeerFactory.CreateStandardMessage(FixAdminMessageTypes.Heartbeat, 1));
        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(SessionState.Disconnected));
            Assert.That(result.Events.Exists(item => item.Code == "message-before-logon"), Is.True);
            Assert.That(fixture.Transport.CloseCount, Is.EqualTo(1));
        });
    }

    private static void AssertStandardHeader(bool fixt, SessionRole role)
    {
        SessionSettings settings = ConformanceFixture.Settings(fixt, role, false);
        SessionHeaderValidator validator = new SessionHeaderValidator(settings, new FakeSessionClock(FixSessionTests.Now));
        AdminMessageFactory peer = new AdminMessageFactory(ConformanceFixture.PeerSettings(fixt, role), new FakeSessionClock(FixSessionTests.Now));
        FixMessage valid = peer.CreateStandardMessage("D", 5);
        SessionHeaderValidationResult accepted = validator.ValidateInboundHeader(valid, "");
        FixMessage wrongComp = peer.CreateStandardMessage("D", 5);
        SetText(wrongComp, FixTags.SenderCompID, "OTHER");
        SessionHeaderValidationResult rejected = validator.ValidateInboundHeader(wrongComp, "");
        FixMessage duplicate = peer.CreateStandardMessage("D", 4);
        duplicate.HeaderFields.Add(Field(FixTags.PossDupFlag, "Y"));
        duplicate.HeaderFields.Add(Field(FixTags.OrigSendingTime, "20260830-17:59:59.000"));
        SessionHeaderValidationResult duplicateHeader = validator.ValidateInboundHeader(duplicate, "");
        SessionSequenceProcessor sequences = Sequences(1, 5);
        SessionSequenceActionResult expected = sequences.ProcessInboundSequence(valid, 5, false, false);
        SessionSequenceActionResult high = sequences.ProcessInboundSequence(peer.CreateStandardMessage("D", 8), 8, false, false);
        SessionSequenceActionResult lowDuplicate = sequences.ProcessInboundSequence(duplicate, 4, duplicateHeader.IsPossibleDuplicate, false);
        Assert.Multiple(() =>
        {
            Assert.That(accepted.IsValid, Is.True);
            Assert.That(rejected.Issues[0].SessionRejectReason, Is.EqualTo(9));
            Assert.That(expected.Consumed, Is.True);
            Assert.That(high.Queued, Is.True);
            Assert.That(lowDuplicate.Duplicate, Is.True);
        });
    }

    private static void AssertStandardTrailer(bool fixt, SessionRole role)
    {
        ConformanceFixture fixture = new ConformanceFixture(fixt, role, false);
        byte[] bytes = fixture.Encode(fixture.PeerFactory.CreateStandardMessage(FixAdminMessageTypes.Heartbeat, 1));
        FrameResult valid = new TagValueMessageFramer().ReadFrame(bytes, new FrameOptions());
        byte[] invalid = (byte[])bytes.Clone();
        invalid[invalid.Length - 2] = invalid[invalid.Length - 2] == 48 ? (byte)49 : (byte)48;
        FrameResult bad = new TagValueMessageFramer().ReadFrame(invalid, new FrameOptions());
        Assert.Multiple(() =>
        {
            Assert.That(valid.Status, Is.EqualTo(FrameStatus.Complete));
            Assert.That(valid.FrameLength, Is.EqualTo(bytes.Length));
            Assert.That(bad.Status, Is.Not.EqualTo(FrameStatus.Complete));
        });
    }

    private static void AssertHeartbeatScenario(string scenario, bool fixt, SessionRole role)
    {
        ConformanceFixture fixture = new ConformanceFixture(fixt, role, false);
        fixture.Activate();
        if (scenario == "4")
        {
            fixture.Clock.AdvanceClock(TimeSpan.FromSeconds(5));
            SessionPollResult result = fixture.Session.PollSession();
            Assert.That(result.Events.Exists(item => item.Kind == SessionEventKind.TestRequest || item.Kind == SessionEventKind.Heartbeat), Is.True);
            return;
        }
        if (scenario == "5")
        {
            SessionPollResult result = fixture.Receive(fixture.PeerFactory.CreateHeartbeatMessage(2, ""));
            Assert.That(result.Events.Exists(item => item.Code == "heartbeat-processed"), Is.True);
            Assert.That(result.Snapshot.NextTargetSeqNum, Is.EqualTo(3));
            return;
        }
        fixture.Clock.AdvanceClock(TimeSpan.FromSeconds(5));
        SessionPollResult requested = fixture.Session.PollSession();
        Assert.That(requested.Events.Exists(item => item.Kind == SessionEventKind.TestRequest), Is.True);
    }

    private static void AssertRejectScenario(bool fixt, SessionRole role)
    {
        ConformanceFixture fixture = new ConformanceFixture(fixt, role, false);
        fixture.Activate();
        SessionPollResult result = fixture.Receive(fixture.PeerFactory.CreateRejectMessage(2, 1, FixTags.MsgType, "D", 5, "bad value"));
        Assert.Multiple(() =>
        {
            Assert.That(result.Events.Exists(item => item.Code == "reject-notification-processed"), Is.True);
            Assert.That(result.Snapshot.NextTargetSeqNum, Is.EqualTo(3));
            Assert.That(result.State, Is.EqualTo(SessionState.Active));
        });
    }

    private static void AssertResendScenario(bool fixt, SessionRole role)
    {
        ConformanceFixture fixture = new ConformanceFixture(fixt, role, false);
        fixture.Activate();
        SessionSendResult queued = fixture.Session.SendMessage(new FixMessage { MsgType = "D", BodyFields = [Field(11, "ORDER-1")] });
        fixture.FlushOutbound();
        SessionPollResult result = fixture.Receive(fixture.PeerFactory.CreateResendRequestMessage(2, queued.SequenceNumber, queued.SequenceNumber));
        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(SessionState.Active));
            Assert.That(result.Events.Exists(item => item.Message != null && item.Message.MsgType == FixAdminMessageTypes.ResendRequest), Is.True);
            Assert.That(result.Snapshot.OutboundQueueCount, Is.GreaterThan(0));
        });
    }

    private static void AssertSequenceSynchronization()
    {
        MemoryMessageStore store = new MemoryMessageStore(0);
        Assert.That(store.SaveSequenceState(40, 25).Success, Is.True);
        Assert.That(store.ResetStore().Success, Is.True);
        StoreRangeResult reset = store.RefreshStore();
        SessionSequenceProcessor sequences = Sequences(50, 10);
        SequenceResetProcessingResult synchronized = new SequenceResetProcessor(sequences, new GapTracker(new SessionSettings())).ProcessSequenceReset(new FixMessage { MsgType = FixAdminMessageTypes.SequenceReset }, 1, 100, false, false);
        Assert.Multiple(() =>
        {
            Assert.That(reset.NextSenderSeqNum, Is.EqualTo(1));
            Assert.That(reset.NextTargetSeqNum, Is.EqualTo(1));
            Assert.That(synchronized.NextTargetSeqNum, Is.EqualTo(100));
        });
    }

    private static void AssertGapFillScenario()
    {
        SessionSequenceProcessor sequences = Sequences(1, 2);
        GapTracker gaps = new GapTracker(new SessionSettings());
        SequenceResetProcessor processor = new SequenceResetProcessor(sequences, gaps);
        SequenceResetProcessingResult expected = processor.ProcessSequenceReset(new FixMessage { MsgType = FixAdminMessageTypes.SequenceReset }, 2, 5, true, false);
        SequenceResetProcessingResult stale = processor.ProcessSequenceReset(new FixMessage { MsgType = FixAdminMessageTypes.SequenceReset }, 2, 5, true, true);
        SequenceResetProcessingResult invalid = processor.ProcessSequenceReset(new FixMessage { MsgType = FixAdminMessageTypes.SequenceReset }, 5, 5, true, false);
        Assert.Multiple(() =>
        {
            Assert.That(expected.SequenceAdvanced, Is.True);
            Assert.That(expected.NextTargetSeqNum, Is.EqualTo(5));
            Assert.That(stale.Ignored, Is.True);
            Assert.That(invalid.Rejected, Is.True);
        });
    }

    private static void AssertResetScenario()
    {
        SequenceResetProcessor advanceProcessor = new SequenceResetProcessor(Sequences(1, 5), new GapTracker(new SessionSettings()));
        SequenceResetProcessingResult advance = advanceProcessor.ProcessSequenceReset(new FixMessage { MsgType = FixAdminMessageTypes.SequenceReset }, 99, 8, false, false);
        SequenceResetProcessingResult same = advanceProcessor.ProcessSequenceReset(new FixMessage { MsgType = FixAdminMessageTypes.SequenceReset }, 1, 8, false, false);
        SequenceResetProcessingResult lower = advanceProcessor.ProcessSequenceReset(new FixMessage { MsgType = FixAdminMessageTypes.SequenceReset }, 1, 7, false, false);
        Assert.Multiple(() =>
        {
            Assert.That(advance.NextTargetSeqNum, Is.EqualTo(8));
            Assert.That(same.Accepted, Is.True);
            Assert.That(same.Code, Is.EqualTo("sequence-reset-no-change"));
            Assert.That(lower.Rejected, Is.True);
            Assert.That(lower.NextTargetSeqNum, Is.EqualTo(8));
        });
    }

    private static void AssertLogoutScenario(string scenario, bool fixt, SessionRole role)
    {
        ConformanceFixture fixture = new ConformanceFixture(fixt, role, false);
        fixture.Activate();
        SessionPollResult result;
        if (scenario == "12")
        {
            result = fixture.Session.LogoutSession("done");
            Assert.That(result.State, Is.EqualTo(SessionState.LogoutPending));
            Assert.That(result.Snapshot.OutboundQueueCount, Is.EqualTo(1));
            return;
        }
        result = fixture.Receive(fixture.PeerFactory.CreateLogoutMessage(2, "peer done"));
        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(SessionState.LogoutPending));
            Assert.That(result.Snapshot.OutboundQueueCount, Is.EqualTo(1));
        });
    }

    private static void AssertMessageValidationReasons()
    {
        string[] codes = ["invalid-tag-number", "required-field-missing", "field-not-allowed", "empty-field-value", "invalid-code-set-value", "invalid-datatype-value", "body-field-order-invalid", "duplicate-body-field", "group-count-mismatch", "group-member-out-of-order", "decryption-problem"];
        int[] reasons = [0, 1, 2, 4, 5, 6, 14, 13, 16, 15, 7];
        for (int index = 0; index < codes.Length; index++)
        {
            ValidationIssue issue = new ValidationIssue { Severity = ValidationSeverity.Error, Code = codes[index] };
            RejectMapper.MapValidationIssue(issue, new FixMessage { MsgType = "D" });
            Assert.That(issue.SessionRejectReason, Is.EqualTo(reasons[index]), codes[index]);
        }
        ValidationIssue unavailable = new ValidationIssue { Severity = ValidationSeverity.Error, Code = "unsupported-scenario" };
        ValidationIssue conditional = new ValidationIssue { Severity = ValidationSeverity.Error, Code = "conditionally-required-member-missing" };
        RejectMapper.MapValidationIssue(unavailable, new FixMessage());
        RejectMapper.MapValidationIssue(conditional, new FixMessage());
        Assert.That(unavailable.BusinessRejectReason, Is.EqualTo(4));
        Assert.That(conditional.BusinessRejectReason, Is.EqualTo(5));
    }

    private static void AssertFieldOrderScenario(bool fixt, SessionRole role)
    {
        ConformanceFixture fixture = new ConformanceFixture(fixt, role, false);
        fixture.Activate();
        FixMessage first = fixture.PeerFactory.CreateStandardMessage("D", 2);
        first.BodyFields.Add(Field(11, "A"));
        first.BodyFields.Add(Field(55, "IBM"));
        FixMessage second = fixture.PeerFactory.CreateStandardMessage("D", 3);
        second.BodyFields.Add(Field(55, "IBM"));
        second.BodyFields.Add(Field(11, "B"));
        SessionPollResult firstResult = fixture.Receive(first);
        SessionPollResult secondResult = fixture.Receive(second);
        Assert.That(fixture.Application.ApplicationContexts, Has.Count.EqualTo(2), fixture.EventText(firstResult) + ";" + fixture.EventText(secondResult));
    }

    private static void AssertQueueScenario(bool fixt, SessionRole role)
    {
        ConformanceFixture fixture = new ConformanceFixture(fixt, role, true);
        fixture.Session.StartSession();
        SessionSendResult queued = fixture.Session.SendMessage(new FixMessage { MsgType = "D", BodyFields = [Field(11, "QUEUED")] });
        Assert.Multiple(() =>
        {
            Assert.That(queued.Accepted, Is.True);
            Assert.That(queued.SequenceNumber, Is.Zero);
            Assert.That(queued.Code, Is.EqualTo("message-queued-before-logon"));
        });
    }

    private static void AssertEncryptionScenario(bool fixt, SessionRole role)
    {
        SessionSettings settings = ConformanceFixture.Settings(fixt, role, false);
        FixSessionTests.EmptyDictionaryProvider provider = new FixSessionTests.EmptyDictionaryProvider();
        LogonProcessor processor = new LogonProcessor(settings, provider.Dictionary, new FixSessionTests.AcceptingMessageValidator(), provider, new FixSessionTests.AcceptingAuthenticator(), new FakeSessionClock(FixSessionTests.Now));
        LogonNegotiationState state = processor.CreateNegotiationState();
        FixMessage valid = new AdminMessageFactory(ConformanceFixture.PeerSettings(fixt, role), new FakeSessionClock(FixSessionTests.Now)).CreateLogonMessage(1, 1, false);
        LogonProcessingResult accepted = processor.ProcessInboundLogon(valid, "", "", state);
        FixMessage invalid = new AdminMessageFactory(ConformanceFixture.PeerSettings(fixt, role), new FakeSessionClock(FixSessionTests.Now)).CreateLogonMessage(1, 1, false);
        SetText(invalid, FixTags.EncryptMethod, "9");
        LogonProcessingResult rejected = processor.ProcessInboundLogon(invalid, "", "", processor.CreateNegotiationState());
        Assert.Multiple(() =>
        {
            Assert.That(accepted.Accepted, Is.True);
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.Issues[0].SessionRejectReason, Is.EqualTo(7));
        });
    }

    private static void AssertThirdPartyAddressingScenario()
    {
        ValidationIssue wrongAddress = new ValidationIssue { Severity = ValidationSeverity.Error, Code = "sender-comp-id-mismatch", HasTag = true, Tag = FixTags.OnBehalfOfCompID };
        RejectMapper.MapValidationIssue(wrongAddress, new FixMessage { MsgType = "D" });
        Assert.Multiple(() =>
        {
            Assert.That(wrongAddress.SessionRejectReason, Is.EqualTo(9));
            Assert.That(wrongAddress.RefTagId, Is.EqualTo(FixTags.OnBehalfOfCompID));
        });
    }

    private static void AssertPossibleResendScenario(bool fixt, SessionRole role)
    {
        ConformanceFixture fixture = new ConformanceFixture(fixt, role, false);
        fixture.Activate();
        fixture.Application.Decision = FixSessionTests.Decision(MessageDisposition.Drop, "", "already received");
        FixMessage message = fixture.PeerFactory.CreateStandardMessage("D", 2);
        message.HeaderFields.Add(Field(FixTags.PossResend, "Y"));
        SessionPollResult result = fixture.Receive(message);
        Assert.Multiple(() =>
        {
            Assert.That(fixture.Application.ApplicationContexts, Has.Count.EqualTo(1), fixture.EventText(result));
            Assert.That(fixture.Application.ApplicationContexts.Count == 0 ? false : fixture.Application.ApplicationContexts[0].IsPossibleResend, Is.True);
            Assert.That(result.Events.Exists(item => item.Code == "possible-resend-ignored"), Is.True);
            Assert.That(result.Snapshot.NextTargetSeqNum, Is.EqualTo(3));
            Assert.That(result.Snapshot.OutboundQueueCount, Is.Zero);
        });
    }

    private static void AssertSimultaneousResendScenario(bool fixt, SessionRole role)
    {
        ConformanceFixture fixture = new ConformanceFixture(fixt, role, false);
        fixture.Activate();
        SessionPollResult gap = fixture.Receive(fixture.PeerFactory.CreateStandardMessage("D", 4));
        fixture.FlushOutbound();
        SessionPollResult peerRequest = fixture.Receive(fixture.PeerFactory.CreateResendRequestMessage(2, 1, 1));
        Assert.Multiple(() =>
        {
            Assert.That(gap.State, Is.EqualTo(SessionState.Recovering));
            Assert.That(gap.Events.Exists(item => item.Kind == SessionEventKind.SequenceGap), Is.True);
            Assert.That(peerRequest.State, Is.EqualTo(SessionState.Recovering));
            Assert.That(peerRequest.Events.Exists(item => item.Message != null && item.Message.MsgType == FixAdminMessageTypes.ResendRequest), Is.True);
            Assert.That(peerRequest.Snapshot.OutboundQueueCount, Is.GreaterThan(0));
        });
    }

    private static SessionSequenceProcessor Sequences(int sender, int target)
    {
        MemoryMessageStore store = new MemoryMessageStore(0);
        Assert.That(store.SaveSequenceState(sender, target).Success, Is.True);
        SessionSequenceProcessor sequences = new SessionSequenceProcessor(store);
        Assert.That(sequences.RefreshSequences().Accepted, Is.True);
        return sequences;
    }

    private static FixField Field(int tag, string value)
    {
        return new FixField { Tag = tag, Value = Encoding.Latin1.GetBytes(value) };
    }

    private static void SetText(FixMessage message, int tag, string value)
    {
        FixField field = EnvelopeValidator.FindFirstMessageField(message, tag)!;
        field.Value = Encoding.Latin1.GetBytes(value);
    }

    private static void RemoveField(FixMessage message, int tag)
    {
        message.HeaderFields.RemoveAll(item => item.Tag == tag);
        message.BodyFields.RemoveAll(item => item.Tag == tag);
        message.TrailerFields.RemoveAll(item => item.Tag == tag);
    }

    private class ConformanceFixture
    {
        internal SessionSettings SettingsValue;
        internal FakeSessionClock Clock = new FakeSessionClock(FixSessionTests.Now);
        internal FixSessionTests.FakeSessionTransport Transport = new FixSessionTests.FakeSessionTransport();
        internal MemoryMessageStore Store = new MemoryMessageStore(0);
        internal FixSessionTests.RecordingSessionApplication Application = new FixSessionTests.RecordingSessionApplication();
        internal EmbeddedDictionaryProvider Provider = new EmbeddedDictionaryProvider(null);
        internal FixDictionary WireDictionary;
        internal AdminMessageFactory PeerFactory;
        internal FixSession Session;

        internal ConformanceFixture(bool fixt, SessionRole role, bool allowBeforeLogon)
        {
            SettingsValue = Settings(fixt, role, allowBeforeLogon);
            WireDictionary = Provider.ResolveDictionary(SettingsValue.TransportDictionary).Dictionary!;
            PeerFactory = new AdminMessageFactory(PeerSettings(fixt, role), Clock);
            Session = new FixSession(SettingsValue, Transport, Store, new FixSessionTests.RecordingFixLog(), Clock, new FixSessionTests.RecordingThrottle(), Application, new TagValueMessageFramer(), new TagValueMessageDecoder(), new TagValueMessageEncoder(), new FixSessionTests.AcceptingMessageValidator(), Provider, new FixSessionTests.AcceptingAuthenticator());
        }

        internal static SessionSettings Settings(bool fixt, SessionRole role, bool allowBeforeLogon)
        {
            SessionSettings settings = FixSessionTests.Settings(allowBeforeLogon);
            settings.Role = role;
            settings.HeartbeatIntervalSeconds = 5;
            if (fixt)
            {
                settings.Id.BeginString = FixBeginStrings.Fixt11;
                settings.TransportDictionary.BeginString = FixBeginStrings.Fixt11;
                settings.ApplicationDictionary.BeginString = "";
                settings.ApplicationDictionary.ApplicationVersion = FixApplicationVersions.Fix50Sp2;
                settings.DefaultApplicationVersion = FixApplicationVersions.Fix50Sp2;
            }
            return settings;
        }

        internal static SessionSettings PeerSettings(bool fixt, SessionRole localRole)
        {
            SessionSettings settings = Settings(fixt, localRole == SessionRole.Initiator ? SessionRole.Acceptor : SessionRole.Initiator, false);
            settings.Id.SenderCompID = "REMOTE";
            settings.Id.TargetCompID = "LOCAL";
            return settings;
        }

        internal void Activate()
        {
            Session.StartSession();
            if (SettingsValue.Role == SessionRole.Initiator) FlushOutbound();
            SessionPollResult active = Receive(PeerFactory.CreateLogonMessage(1, 1, false));
            Assert.That(active.State, Is.EqualTo(SessionState.Active), EventText(active));
            FlushOutbound();
        }

        internal SessionPollResult Receive(FixMessage message)
        {
            Transport.AddInbound(Encode(message));
            return Session.PollSession();
        }

        internal void FlushOutbound()
        {
            int count = 0;
            while (Session.GetSessionSnapshot().OutboundQueueCount != 0 && count < 20)
            {
                Session.PollSession();
                count++;
            }
            Assert.That(Session.GetSessionSnapshot().OutboundQueueCount, Is.Zero);
            Transport.OutboundBytes.Clear();
        }

        internal byte[] Encode(FixMessage message)
        {
            byte[] destination = new byte[FixDefaults.MaxMessageSize];
            EncodeResult result = new TagValueMessageEncoder().EncodeMessage(message, WireDictionary, destination, new EncodeOptions());
            Assert.That(result.Status, Is.EqualTo(EncodeStatus.Success), result.ErrorCode + ":" + result.ErrorMessage);
            byte[] bytes = new byte[result.BytesWritten];
            Array.Copy(destination, bytes, result.BytesWritten);
            return bytes;
        }

        internal string EventText(SessionPollResult result)
        {
            return string.Join(",", result.Events.ConvertAll(item => item.Code + ":" + item.Text));
        }
    }
}
