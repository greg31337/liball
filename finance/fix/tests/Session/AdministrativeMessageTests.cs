using System;
using System.Collections.Generic;
using System.Text;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class AdministrativeMessageTests
{
    internal static readonly DateTime Now = new DateTime(2026, 8, 30, 14, 0, 0, DateTimeKind.Utc);

    [Test]
    public void ClassicFactoryCreatesEveryAdministrativeMessageWithSharedHeader()
    {
        AdminFixture fixture = new AdminFixture(FixBeginStrings.Fix44);

        FixMessage heartbeat = fixture.Factory.CreateHeartbeatMessage(1, "TEST-1");
        FixMessage testRequest = fixture.Factory.CreateTestRequestMessage(2, "TEST-2");
        FixMessage resend = fixture.Factory.CreateResendRequestMessage(3, 4, 0);
        FixMessage reject = fixture.Factory.CreateRejectMessage(4, 3, 11, "D", 5, "bad field");
        FixMessage reset = fixture.Factory.CreateSequenceResetMessage(5, 8, true);
        FixMessage logon = fixture.Factory.CreateLogonMessage(47, 9, true);
        FixMessage logout = fixture.Factory.CreateLogoutMessage(7, "done");
        FixMessage business = fixture.Factory.CreateBusinessRejectMessage(8, 7, "D", "ORDER-7", 3, "unsupported");

        Assert.Multiple(() =>
        {
            AssertHeader(heartbeat, FixBeginStrings.Fix44, FixAdminMessageTypes.Heartbeat, 1);
            Assert.That(Text(heartbeat, FixTags.TestReqID), Is.EqualTo("TEST-1"));
            AssertHeader(testRequest, FixBeginStrings.Fix44, FixAdminMessageTypes.TestRequest, 2);
            Assert.That(Text(testRequest, FixTags.TestReqID), Is.EqualTo("TEST-2"));
            AssertHeader(resend, FixBeginStrings.Fix44, FixAdminMessageTypes.ResendRequest, 3);
            Assert.That(Text(resend, FixTags.BeginSeqNo), Is.EqualTo("4"));
            Assert.That(Text(resend, FixTags.EndSeqNo), Is.EqualTo("0"));
            AssertHeader(reject, FixBeginStrings.Fix44, FixAdminMessageTypes.Reject, 4);
            Assert.That(Text(reject, FixTags.RefSeqNum), Is.EqualTo("3"));
            Assert.That(Text(reject, FixTags.RefTagID), Is.EqualTo("11"));
            Assert.That(Text(reject, FixTags.RefMsgType), Is.EqualTo("D"));
            Assert.That(Text(reject, FixTags.SessionRejectReason), Is.EqualTo("5"));
            Assert.That(Text(reject, FixTags.Text), Is.EqualTo("bad field"));
            AssertHeader(reset, FixBeginStrings.Fix44, FixAdminMessageTypes.SequenceReset, 5);
            Assert.That(Text(reset, FixTags.GapFillFlag), Is.EqualTo("Y"));
            Assert.That(Text(reset, FixTags.NewSeqNo), Is.EqualTo("8"));
            AssertHeader(logon, FixBeginStrings.Fix44, FixAdminMessageTypes.Logon, 1);
            Assert.That(Text(logon, FixTags.EncryptMethod), Is.EqualTo("0"));
            Assert.That(Text(logon, FixTags.HeartBtInt), Is.EqualTo("30"));
            Assert.That(Text(logon, FixTags.ResetSeqNumFlag), Is.EqualTo("Y"));
            Assert.That(Text(logon, FixTags.NextExpectedMsgSeqNum), Is.EqualTo("9"));
            Assert.That(Field(logon, FixTags.DefaultApplVerID), Is.Null);
            AssertHeader(logout, FixBeginStrings.Fix44, FixAdminMessageTypes.Logout, 7);
            Assert.That(Text(logout, FixTags.Text), Is.EqualTo("done"));
            AssertHeader(business, FixBeginStrings.Fix44, FixAdminMessageTypes.BusinessMessageReject, 8);
            Assert.That(Text(business, FixTags.RefSeqNum), Is.EqualTo("7"));
            Assert.That(Text(business, FixTags.RefMsgType), Is.EqualTo("D"));
            Assert.That(Text(business, FixTags.BusinessRejectRefID), Is.EqualTo("ORDER-7"));
            Assert.That(Text(business, FixTags.BusinessRejectReason), Is.EqualTo("3"));
        });
    }

    [Test]
    public void FixtLogonCarriesApplicationVersionAndCredentialsOnlyOnLogon()
    {
        AdminFixture fixture = new AdminFixture(FixBeginStrings.Fixt11);
        fixture.Settings.Username = "operator";
        fixture.Settings.Password = "secret";
        AdminMessageFactory factory = new AdminMessageFactory(fixture.Settings, fixture.Clock);

        FixMessage logon = factory.CreateLogonMessage(10, 4, false);
        FixMessage heartbeat = factory.CreateHeartbeatMessage(11, "");

        Assert.Multiple(() =>
        {
            AssertHeader(logon, FixBeginStrings.Fixt11, FixAdminMessageTypes.Logon, 10);
            Assert.That(Text(logon, FixTags.DefaultApplVerID), Is.EqualTo(FixApplicationVersions.Fix50Sp2));
            Assert.That(Text(logon, FixTags.Username), Is.EqualTo("operator"));
            Assert.That(Text(logon, FixTags.Password), Is.EqualTo("secret"));
            AssertHeader(heartbeat, FixBeginStrings.Fixt11, FixAdminMessageTypes.Heartbeat, 11);
            Assert.That(Field(heartbeat, FixTags.DefaultApplVerID), Is.Null);
            Assert.That(Field(heartbeat, FixTags.Username), Is.Null);
        });
    }

    [Test]
    public void TestRequestCreatesMatchingHeartbeatAndHeartbeatClearsPendingProbe()
    {
        AdminFixture fixture = new AdminFixture(FixBeginStrings.Fix44);
        FixMessage request = fixture.Factory.CreateTestRequestMessage(1, "VENUE-42");

        AdminMessageProcessingResult response = fixture.Processor.ProcessInboundAdministrative(request, 1, 10, false);
        fixture.Clock.AdvanceClock(TimeSpan.FromSeconds(30));
        SessionTimerActionResult probe = fixture.Timers.PollTimers();
        FixMessage heartbeat = fixture.Factory.CreateHeartbeatMessage(2, probe.TestRequestId);
        AdminMessageProcessingResult matched = fixture.Processor.ProcessInboundAdministrative(heartbeat, 2, 11, false);

        Assert.Multiple(() =>
        {
            Assert.That(response.Accepted, Is.True);
            Assert.That(response.SendHeartbeat, Is.True);
            Assert.That(response.OutboundMessage, Is.Not.Null);
            AssertHeader(response.OutboundMessage!, FixBeginStrings.Fix44, FixAdminMessageTypes.Heartbeat, 10);
            Assert.That(Text(response.OutboundMessage!, FixTags.TestReqID), Is.EqualTo("VENUE-42"));
            Assert.That(probe.SendTestRequest, Is.True);
            Assert.That(matched.Accepted, Is.True);
            Assert.That(matched.TimerAction!.MatchedTestRequest, Is.True);
            Assert.That(matched.Code, Is.EqualTo("heartbeat-test-request-matched"));
            Assert.That(fixture.Timers.TestRequestPending, Is.False);
            Assert.That(fixture.Application.AdministrativeContexts, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void ResendRequestDispatchesReplayAndSequenceResetDispatchesGapFillProcessing()
    {
        AdminFixture fixture = new AdminFixture(FixBeginStrings.Fix44);
        FixMessage resend = fixture.Factory.CreateResendRequestMessage(1, 1, 0);
        FixMessage reset = fixture.Factory.CreateSequenceResetMessage(1, 5, true);

        AdminMessageProcessingResult replayed = fixture.Processor.ProcessInboundAdministrative(resend, 1, 2, false);
        AdminMessageProcessingResult advanced = fixture.Processor.ProcessInboundAdministrative(reset, 1, 2, false);
        StoreRangeResult persisted = fixture.Store.RefreshStore();

        Assert.Multiple(() =>
        {
            Assert.That(replayed.Accepted, Is.True);
            Assert.That(replayed.ReplayResult, Is.Not.Null);
            Assert.That(replayed.ReplayResult!.Code, Is.EqualTo("replay-range-empty"));
            Assert.That(replayed.ReplayResult.Messages, Is.Empty);
            Assert.That(advanced.Accepted, Is.True);
            Assert.That(advanced.SequenceResetResult, Is.Not.Null);
            Assert.That(advanced.SequenceResetResult!.SequenceAdvanced, Is.True);
            Assert.That(advanced.SequenceResetResult.NextTargetSeqNum, Is.EqualTo(5));
            Assert.That(persisted.NextTargetSeqNum, Is.EqualTo(5));
            Assert.That(fixture.Sequences.NextSenderSeqNum, Is.EqualTo(1));
        });
    }

    [Test]
    public void RejectNotificationsReachApplicationAndLifecycleTypesRouteExplicitly()
    {
        AdminFixture fixture = new AdminFixture(FixBeginStrings.Fix44);
        FixMessage reject = fixture.Factory.CreateRejectMessage(1, 7, 0, "", 0, "");
        FixMessage business = fixture.Factory.CreateBusinessRejectMessage(2, 8, "D", "", 3, "");
        FixMessage logon = fixture.Factory.CreateLogonMessage(3, 0, false);
        FixMessage logout = fixture.Factory.CreateLogoutMessage(4, "");

        AdminMessageProcessingResult rejected = fixture.Processor.ProcessInboundAdministrative(reject, 1, 5, false);
        AdminMessageProcessingResult businessRejected = fixture.Processor.ProcessInboundAdministrative(business, 2, 5, true);
        int callbacksBeforeRoutes = fixture.Application.AdministrativeContexts.Count;
        AdminMessageProcessingResult routedLogon = fixture.Processor.ProcessInboundAdministrative(logon, 3, 5, false);
        AdminMessageProcessingResult routedLogout = fixture.Processor.ProcessInboundAdministrative(logout, 4, 5, false);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Accepted, Is.True);
            Assert.That(rejected.Code, Is.EqualTo("reject-notification-processed"));
            Assert.That(businessRejected.Accepted, Is.True);
            Assert.That(fixture.Application.AdministrativeContexts[1].IsPossibleDuplicate, Is.True);
            Assert.That(callbacksBeforeRoutes, Is.EqualTo(2));
            Assert.That(routedLogon.RouteToLogonProcessor, Is.True);
            Assert.That(routedLogon.Code, Is.EqualTo("route-logon"));
            Assert.That(routedLogout.RouteToLogoutProcessor, Is.True);
            Assert.That(routedLogout.Code, Is.EqualTo("route-logout"));
            Assert.That(fixture.Application.AdministrativeContexts, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void MalformedAdministrativeFieldsRejectBeforeApplicationOrStateMutation()
    {
        AdminFixture fixture = new AdminFixture(FixBeginStrings.Fix44);
        FixMessage missingTestId = fixture.Factory.CreateHeartbeatMessage(1, "");
        missingTestId.MsgType = FixAdminMessageTypes.TestRequest;
        SetText(missingTestId, FixTags.MsgType, FixAdminMessageTypes.TestRequest);
        FixMessage invertedResend = fixture.Factory.CreateResendRequestMessage(2, 5, 5);
        SetText(invertedResend, FixTags.EndSeqNo, "4");
        FixMessage invalidReset = fixture.Factory.CreateSequenceResetMessage(3, 5, true);
        SetText(invalidReset, FixTags.GapFillFlag, "X");
        FixMessage unknown = fixture.Factory.CreateHeartbeatMessage(4, "");
        unknown.MsgType = "Z";
        SetText(unknown, FixTags.MsgType, "Z");

        AdminMessageProcessingResult test = fixture.Processor.ProcessInboundAdministrative(missingTestId, 1, 5, false);
        AdminMessageProcessingResult resend = fixture.Processor.ProcessInboundAdministrative(invertedResend, 2, 5, false);
        AdminMessageProcessingResult reset = fixture.Processor.ProcessInboundAdministrative(invalidReset, 3, 5, false);
        AdminMessageProcessingResult unsupported = fixture.Processor.ProcessInboundAdministrative(unknown, 4, 5, false);

        Assert.Multiple(() =>
        {
            Assert.That(test.Code, Is.EqualTo("invalid-test-request-id"));
            Assert.That(resend.Code, Is.EqualTo("invalid-resend-request-range"));
            Assert.That(reset.Code, Is.EqualTo("invalid-sequence-reset-fields"));
            Assert.That(unsupported.Code, Is.EqualTo("unsupported-administrative-message-type"));
            Assert.That(fixture.Application.AdministrativeContexts, Is.Empty);
            Assert.That(fixture.Sequences.NextTargetSeqNum, Is.EqualTo(1));
        });
    }

    [Test]
    public void ApplicationDeclinePreventsAdministrativeSideEffects()
    {
        AdminFixture fixture = new AdminFixture(FixBeginStrings.Fix44);
        fixture.Application.Decision = new ApplicationDecision { Disposition = MessageDisposition.Disconnect, Code = "admin-stop", Text = "stop" };
        FixMessage request = fixture.Factory.CreateTestRequestMessage(1, "NO-RESPONSE");
        DateTime inboundBefore = fixture.Timers.LastInboundUtc;

        AdminMessageProcessingResult result = fixture.Processor.ProcessInboundAdministrative(request, 1, 2, false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Disconnect, Is.True);
            Assert.That(result.Code, Is.EqualTo("admin-stop"));
            Assert.That(result.SendHeartbeat, Is.False);
            Assert.That(result.OutboundMessage, Is.Null);
            Assert.That(fixture.Timers.LastInboundUtc, Is.EqualTo(inboundBefore));
        });
    }

    [Test]
    public void FactoryAndProcessorContractsRejectInvalidDependenciesAndValues()
    {
        AdminFixture fixture = new AdminFixture(FixBeginStrings.Fix44);
        SessionSettings unsupported = Settings("FIX.9.9");
        SessionSettings missingFixtVersion = Settings(FixBeginStrings.Fixt11);
        missingFixtVersion.DefaultApplicationVersion = "";
        AdminMessageFactory localClock = new AdminMessageFactory(fixture.Settings, new FakeSessionClock(DateTime.SpecifyKind(Now, DateTimeKind.Local)));

        Assert.Multiple(() =>
        {
            Assert.Throws<FixSessionException>(() => new AdminMessageFactory(unsupported, fixture.Clock));
            Assert.Throws<FixSessionException>(() => new AdminMessageFactory(missingFixtVersion, fixture.Clock));
            Assert.Throws<FixSessionException>(() => fixture.Factory.CreateTestRequestMessage(1, ""));
            Assert.Throws<FixSessionException>(() => fixture.Factory.CreateResendRequestMessage(1, 5, 4));
            Assert.Throws<FixSessionException>(() => fixture.Factory.CreateSequenceResetMessage(5, 5, true));
            Assert.Throws<FixSessionException>(() => fixture.Factory.CreateBusinessRejectMessage(1, 1, "", "", 0, ""));
            Assert.Throws<FixSessionException>(() => localClock.CreateHeartbeatMessage(1, ""));
            Assert.Throws<FixSessionException>(() => new AdminMessageProcessor(null!, fixture.ReplayProcessor, fixture.ResetProcessor, fixture.Factory, fixture.Application));
            Assert.That(fixture.Processor.ProcessInboundAdministrative(null!, 1, 2, false).Code, Is.EqualTo("missing-administrative-message"));
        });
    }

    private static SessionSettings Settings(string beginString)
    {
        return new SessionSettings
        {
            Id = new SessionId
            {
                BeginString = beginString,
                SenderCompID = "LOCAL",
                SenderSubID = "ORDERS",
                SenderLocationID = "NY",
                TargetCompID = "REMOTE",
                TargetSubID = "MATCH",
                TargetLocationID = "LDN",
                Qualifier = "PRIMARY"
            },
            Role = SessionRole.Initiator,
            DefaultApplicationVersion = FixApplicationVersions.Fix50Sp2,
            HeartbeatIntervalSeconds = 30
        };
    }

    private static void AssertHeader(FixMessage message, string beginString, string messageType, int sequenceNumber)
    {
        Assert.That(message.BeginString, Is.EqualTo(beginString));
        Assert.That(message.MsgType, Is.EqualTo(messageType));
        Assert.That(Text(message, FixTags.BeginString), Is.EqualTo(beginString));
        Assert.That(Text(message, FixTags.MsgType), Is.EqualTo(messageType));
        Assert.That(Text(message, FixTags.MsgSeqNum), Is.EqualTo(sequenceNumber.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        Assert.That(Text(message, FixTags.SenderCompID), Is.EqualTo("LOCAL"));
        Assert.That(Text(message, FixTags.TargetCompID), Is.EqualTo("REMOTE"));
        Assert.That(Text(message, FixTags.SendingTime), Is.EqualTo("20260830-14:00:00.000"));
    }

    private static FixField? Field(FixMessage message, int tag)
    {
        return EnvelopeValidator.FindFirstMessageField(message, tag);
    }

    private static string Text(FixMessage message, int tag)
    {
        FixField? field = Field(message, tag);
        return field == null ? "" : Encoding.Latin1.GetString(field.Value);
    }

    private static void SetText(FixMessage message, int tag, string value)
    {
        FixField? field = Field(message, tag);
        Assert.That(field, Is.Not.Null);
        field!.Value = Encoding.Latin1.GetBytes(value);
    }

    private class AdminFixture
    {
        internal SessionSettings Settings;
        internal FakeSessionClock Clock = new FakeSessionClock(Now);
        internal MemoryMessageStore Store = new MemoryMessageStore(0);
        internal SessionSequenceProcessor Sequences;
        internal SessionTimers Timers;
        internal GapTracker Gaps;
        internal SequenceResetProcessor ResetProcessor;
        internal RecordingAdminApplication Application = new RecordingAdminApplication();
        internal AdminMessageFactory Factory;
        internal ReplayBuilder ReplayBuilder;
        internal ReplayProcessor ReplayProcessor;
        internal AdminMessageProcessor Processor;

        internal AdminFixture(string beginString)
        {
            Settings = AdministrativeMessageTests.Settings(beginString);
            Assert.That(Store.SaveSequenceState(1, 1).Success, Is.True);
            Sequences = new SessionSequenceProcessor(Store);
            Assert.That(Sequences.RefreshSequences().Accepted, Is.True);
            Timers = new SessionTimers(Clock);
            Timers.StartTimers(Settings.HeartbeatIntervalSeconds);
            Gaps = new GapTracker(Settings);
            ResetProcessor = new SequenceResetProcessor(Sequences, Gaps);
            Factory = new AdminMessageFactory(Settings, Clock);
            ReplayBuilder = new ReplayBuilder(Settings.Id, new FixDictionary(), new TagValueMessageDecoder(), new TagValueMessageEncoder(), new FixMessageOperations(), Clock);
            ReplayProcessor = new ReplayProcessor(Store, Sequences, ReplayBuilder, Application);
            Processor = new AdminMessageProcessor(Timers, ReplayProcessor, ResetProcessor, Factory, Application);
        }
    }

    private class RecordingAdminApplication : ISessionApplication
    {
        internal List<ApplicationMessageContext> AdministrativeContexts = [];
        internal ApplicationDecision Decision = new ApplicationDecision { Disposition = MessageDisposition.Accept, Code = "accepted" };

        public ApplicationDecision OnSessionCreated(SessionId id)
        {
            return Decision;
        }

        public ApplicationDecision OnSessionLogon(ApplicationMessageContext context)
        {
            return Decision;
        }

        public ApplicationDecision OnSessionLogout(ApplicationMessageContext context)
        {
            return Decision;
        }

        public ApplicationDecision OnAdministrativeMessage(ApplicationMessageContext context)
        {
            AdministrativeContexts.Add(context);
            return Decision;
        }

        public ApplicationDecision OnApplicationMessage(ApplicationMessageContext context)
        {
            return Decision;
        }

        public ApplicationDecision OnReplayMessage(ApplicationMessageContext context)
        {
            return Decision;
        }
    }
}
