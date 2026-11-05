using System;
using fix;

namespace fix.Tests;

[TestFixture]
public class SessionDataTests
{
    [Test]
    public void SessionSettingsRetainIndependentNestedConfiguration()
    {
        SessionEndpoint primary = new SessionEndpoint { Host = "primary.example", Port = 9876, Priority = 0, UseTls = true };
        SessionEndpoint failover = new SessionEndpoint { Host = "backup.example", Port = 9876, Priority = 1, UseTls = true };
        SessionTimeWindow window = new SessionTimeWindow
        {
            StartDay = DayOfWeek.Monday,
            StartTime = TimeSpan.FromHours(8),
            EndDay = DayOfWeek.Friday,
            EndTime = TimeSpan.FromHours(17)
        };
        SessionSettings settings = new SessionSettings
        {
            Id = new SessionId
            {
                BeginString = FixBeginStrings.Fixt11,
                SenderCompID = "BUY",
                TargetCompID = "SELL",
                Qualifier = "LONDON"
            },
            Role = SessionRole.Initiator,
            Endpoints = [primary, failover],
            Schedule = new SessionSchedule { NonStop = false, TimeZoneId = "Europe/London", Windows = [window] },
            Tls = new SessionTlsSettings { Enabled = true, TargetHost = "primary.example" },
            Throttle = new SessionThrottleSettings { MaxMessages = 250, QueueCapacity = 512 },
            TransportDictionary = new FixDictionaryIdentity { BeginString = FixBeginStrings.Fixt11 },
            ApplicationDictionary = new FixDictionaryIdentity { ApplicationVersion = FixApplicationVersions.Fix50Sp2 },
            DefaultApplicationVersion = FixApplicationVersions.Fix50Sp2,
            ResetMode = SessionResetMode.OnSessionBoundary
        };

        Assert.Multiple(() =>
        {
            Assert.That(settings.Id.SenderCompID, Is.EqualTo("BUY"));
            Assert.That(settings.Endpoints[0], Is.SameAs(primary));
            Assert.That(settings.Endpoints[1], Is.SameAs(failover));
            Assert.That(settings.Schedule.Windows[0], Is.SameAs(window));
            Assert.That(settings.Schedule.TimeZoneId, Is.EqualTo("Europe/London"));
            Assert.That(settings.Tls.Alpn, Is.EqualTo("fix"));
            Assert.That(settings.Tls.CheckCertificateRevocation, Is.True);
            Assert.That(settings.Throttle.MaxMessages, Is.EqualTo(250));
            Assert.That(settings.Throttle.QueueCapacity, Is.EqualTo(512));
            Assert.That(settings.HeartbeatIntervalSeconds, Is.EqualTo(FixDefaults.HeartbeatIntervalSeconds));
            Assert.That(settings.CheckCompIds, Is.True);
            Assert.That(settings.CheckLatency, Is.True);
            Assert.That(settings.MaxLatencySeconds, Is.EqualTo(FixDefaults.MaxLatencySeconds));
            Assert.That(settings.RequireOrigSendingTime, Is.True);
            Assert.That(settings.ApplicationDictionary.ApplicationVersion, Is.EqualTo("9"));
            Assert.That(settings.ResetMode, Is.EqualTo(SessionResetMode.OnSessionBoundary));
        });
    }

    [Test]
    public void StoreRangeRetainsRawMessagesSequenceOrderAndGapState()
    {
        StoredMessage first = new StoredMessage
        {
            SequenceNumber = 7,
            SendingTimeUtc = new DateTime(2026, 8, 30, 12, 0, 0, DateTimeKind.Utc),
            Bytes = [56, 61, 70, 73, 88, 1],
            MsgType = "D"
        };
        StoredMessage second = new StoredMessage
        {
            SequenceNumber = 9,
            SendingTimeUtc = new DateTime(2026, 8, 30, 12, 0, 1, DateTimeKind.Utc),
            Bytes = [56, 61, 70, 73, 88, 1, 255],
            MsgType = FixAdminMessageTypes.SequenceReset,
            IsAdministrative = true
        };
        StoreRangeResult range = new StoreRangeResult
        {
            Success = true,
            BeginSeqNo = 7,
            EndSeqNo = 9,
            Messages = [first, second],
            HasGap = true
        };

        Assert.Multiple(() =>
        {
            Assert.That(range.Messages[0], Is.SameAs(first));
            Assert.That(range.Messages[1], Is.SameAs(second));
            Assert.That(range.Messages[0].Bytes, Is.EqualTo(new byte[] { 56, 61, 70, 73, 88, 1 }));
            Assert.That(range.Messages[1].Bytes[6], Is.EqualTo(255));
            Assert.That(range.Messages[1].IsAdministrative, Is.True);
            Assert.That(range.HasGap, Is.True);
            Assert.That(range.IsCorrupt, Is.False);
        });
    }

    [Test]
    public void PollSnapshotAndTransportResultsRetainOperationalState()
    {
        SessionId id = new SessionId { SenderCompID = "BUY", TargetCompID = "SELL" };
        SessionEvent connected = new SessionEvent
        {
            Id = id,
            Kind = SessionEventKind.Connected,
            TimestampUtc = new DateTime(2026, 8, 30, 12, 0, 0, DateTimeKind.Utc)
        };
        SessionSnapshot snapshot = new SessionSnapshot
        {
            Id = id,
            State = SessionState.Active,
            TransportState = TransportState.Connected,
            IsLoggedOn = true,
            NextSenderSeqNum = 12,
            NextTargetSeqNum = 15,
            OutboundQueueCount = 2,
            HasLastInboundTime = true,
            LastInboundTimeUtc = connected.TimestampUtc
        };
        SessionPollResult poll = new SessionPollResult
        {
            State = SessionState.Active,
            WorkRemaining = true,
            Events = [connected],
            Snapshot = snapshot
        };
        SessionSendResult send = new SessionSendResult
        {
            Accepted = true,
            Disposition = MessageDisposition.Accept,
            SequenceNumber = 12,
            QueueCount = 2
        };
        TransportReadResult read = new TransportReadResult { State = TransportState.Connected, BytesRead = 42 };
        TransportWriteResult write = new TransportWriteResult { State = TransportState.Connected, BytesWritten = 21, WouldBlock = true };

        Assert.Multiple(() =>
        {
            Assert.That(poll.Events[0], Is.SameAs(connected));
            Assert.That(poll.Snapshot, Is.SameAs(snapshot));
            Assert.That(poll.WorkRemaining, Is.True);
            Assert.That(snapshot.NextSenderSeqNum, Is.EqualTo(12));
            Assert.That(snapshot.NextTargetSeqNum, Is.EqualTo(15));
            Assert.That(send.Accepted, Is.True);
            Assert.That(send.SequenceNumber, Is.EqualTo(12));
            Assert.That(read.BytesRead, Is.EqualTo(42));
            Assert.That(read.EndOfStream, Is.False);
            Assert.That(write.BytesWritten, Is.EqualTo(21));
            Assert.That(write.WouldBlock, Is.True);
        });
    }

    [Test]
    public void AuthenticationApplicationAndAuditDataRetainSensitiveBoundaries()
    {
        SessionId id = new SessionId { SenderCompID = "BUY", TargetCompID = "SELL" };
        FixMessage logon = new FixMessage { MsgType = FixAdminMessageTypes.Logon };
        AuthenticationRequest request = new AuthenticationRequest
        {
            Id = id,
            Username = "operator",
            Password = "secret",
            RawData = [0, 1, 255],
            RemoteAddress = "192.0.2.10",
            LogonMessage = logon
        };
        AuthenticationResult authentication = new AuthenticationResult { Authenticated = true };
        ApplicationMessageContext context = new ApplicationMessageContext
        {
            Id = id,
            Message = logon,
            SequenceNumber = 1,
            IsAdministrative = true,
            IsReplay = true,
            IsPossibleDuplicate = true
        };
        ApplicationDecision decision = new ApplicationDecision { Disposition = MessageDisposition.Accept, Code = "accepted" };
        LogEntry entry = new LogEntry
        {
            Id = id,
            Direction = LogDirection.Incoming,
            EventKind = SessionEventKind.Logon,
            Bytes = [56, 61, 42, 42, 42, 1],
            IsRedacted = true
        };

        Assert.Multiple(() =>
        {
            Assert.That(request.LogonMessage, Is.SameAs(logon));
            Assert.That(request.RawData, Is.EqualTo(new byte[] { 0, 1, 255 }));
            Assert.That(authentication.Authenticated, Is.True);
            Assert.That(context.Message, Is.SameAs(logon));
            Assert.That(context.IsAdministrative, Is.True);
            Assert.That(context.IsReplay, Is.True);
            Assert.That(context.IsPossibleDuplicate, Is.True);
            Assert.That(decision.Disposition, Is.EqualTo(MessageDisposition.Accept));
            Assert.That(entry.IsRedacted, Is.True);
            Assert.That(entry.Bytes, Is.EqualTo(new byte[] { 56, 61, 42, 42, 42, 1 }));
        });
    }
}
