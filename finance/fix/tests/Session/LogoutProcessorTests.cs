using System;
using System.Collections.Generic;
using System.Text;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class LogoutProcessorTests
{
    internal static readonly DateTime Now = new DateTime(2026, 8, 30, 12, 0, 0, 123, DateTimeKind.Utc);

    [Test]
    public void LogoutCreationPreservesIdentitySequenceTimestampAndOptionalText()
    {
        LogoutProcessor processor = Processor(Settings(), new LogoutStubValidator(), new RecordingApplication(), new FakeSessionClock(Now));

        FixMessage withReason = processor.CreateLogoutMessage(7, "Scheduled maintenance");
        FixMessage withoutReason = processor.CreateLogoutMessage(8, "");

        Assert.Multiple(() =>
        {
            Assert.That(withReason.BeginString, Is.EqualTo(FixBeginStrings.Fixt11));
            Assert.That(withReason.MsgType, Is.EqualTo(FixAdminMessageTypes.Logout));
            Assert.That(Text(withReason, FixTags.MsgSeqNum), Is.EqualTo("7"));
            Assert.That(Text(withReason, FixTags.SenderCompID), Is.EqualTo("LOCAL"));
            Assert.That(Text(withReason, FixTags.TargetCompID), Is.EqualTo("REMOTE"));
            Assert.That(Text(withReason, FixTags.SendingTime), Is.EqualTo("20260830-12:00:00.123"));
            Assert.That(Text(withReason, FixTags.Text), Is.EqualTo("Scheduled maintenance"));
            Assert.That(FindField(withoutReason, FixTags.Text), Is.Null);
        });
    }

    [Test]
    public void CreatedLogoutRoundTripsThroughTheEmbeddedFixtCodec()
    {
        EmbeddedDictionaryProvider provider = new EmbeddedDictionaryProvider(null);
        DictionaryLoadResult transport = provider.ResolveDictionary(new FixDictionaryIdentity { BeginString = FixBeginStrings.Fixt11 });
        SessionSettings settings = Settings();
        LogoutProcessor processor = new LogoutProcessor(settings, transport.Dictionary!, new FixMessageValidator(), new RecordingApplication(), new FakeSessionClock(Now));
        FixMessage message = processor.CreateLogoutMessage(3, "End of session");
        TagValueMessageEncoder encoder = new TagValueMessageEncoder();
        EncodeOptions options = new EncodeOptions { PreserveFieldOrder = false };

        EncodeResult measured = encoder.EncodeMessage(message, transport.Dictionary!, Span<byte>.Empty, options);
        byte[] bytes = new byte[measured.RequiredSize];
        EncodeResult encoded = encoder.EncodeMessage(message, transport.Dictionary!, bytes, options);
        DecodeResult decoded = new TagValueMessageDecoder().DecodeMessage(bytes, transport.Dictionary!, new DecodeOptions());
        SessionSettings peerSettings = Settings();
        peerSettings.Id.SenderCompID = "REMOTE";
        peerSettings.Id.TargetCompID = "LOCAL";
        RecordingApplication peerApplication = new RecordingApplication();
        LogoutProcessor peer = new LogoutProcessor(peerSettings, transport.Dictionary!, new FixMessageValidator(), peerApplication, new FakeSessionClock(Now));
        SessionStateMachine peerMachine = ActiveMachine();
        LogoutLifecycleState peerLifecycle = peer.CreateLogoutState();
        LogoutActionResult processed = peer.ProcessInboundLogout(decoded.Message!, 3, 4, "PRIMARY", peerMachine, peerLifecycle);
        LogoutActionResult confirmed = peer.ConfirmLogoutSent(peerMachine, peerLifecycle);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Status, Is.EqualTo(EncodeStatus.Success), encoded.ErrorCode);
            Assert.That(decoded.Status, Is.EqualTo(DecodeStatus.Success), decoded.ErrorCode);
            Assert.That(decoded.Message!.MsgType, Is.EqualTo(FixAdminMessageTypes.Logout));
            Assert.That(Text(decoded.Message, FixTags.Text), Is.EqualTo("End of session"));
            Assert.That(processed.Accepted, Is.True, processed.Code + ": " + processed.Text);
            Assert.That(confirmed.CloseTransport, Is.True);
            Assert.That(peerApplication.Order, Is.EqualTo(new[] { "admin", "logout" }));
        });
    }

    [Test]
    public void LocalGracefulLogoutEntersPendingAndWaitsWithoutEarlyNotification()
    {
        SessionSettings settings = Settings();
        RecordingApplication application = new RecordingApplication();
        FakeSessionClock clock = new FakeSessionClock(Now);
        LogoutProcessor processor = Processor(settings, new LogoutStubValidator(), application, clock);
        SessionStateMachine machine = ActiveMachine();
        LogoutLifecycleState lifecycle = processor.CreateLogoutState();

        LogoutActionResult result = processor.BeginGracefulLogout(11, "Client shutdown", machine, lifecycle);
        LogoutActionResult repeated = processor.BeginGracefulLogout(12, "Again", machine, lifecycle);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.SendLogout, Is.True);
            Assert.That(result.AwaitingPeerLogout, Is.True);
            Assert.That(result.CloseTransport, Is.False);
            Assert.That(Text(result.OutboundMessage!, FixTags.Text), Is.EqualTo("Client shutdown"));
            Assert.That(machine.State, Is.EqualTo(SessionState.LogoutPending));
            Assert.That(lifecycle.LocalLogoutInitiated, Is.True);
            Assert.That(lifecycle.DeadlineUtc, Is.EqualTo(Now.AddMilliseconds(settings.LogoutTimeoutMilliseconds)));
            Assert.That(application.Order, Is.Empty);
            Assert.That(repeated.Accepted, Is.False);
            Assert.That(repeated.Code, Is.EqualTo("logout-already-in-progress"));
        });
    }

    [Test]
    public void PeerLogoutCompletesLocalOrSimultaneousExchangeWithAdminThenLogoutNotification()
    {
        RecordingApplication application = new RecordingApplication();
        LogoutProcessor processor = Processor(Settings(), new LogoutStubValidator(), application, new FakeSessionClock(Now));
        SessionStateMachine machine = ActiveMachine();
        LogoutLifecycleState lifecycle = processor.CreateLogoutState();
        processor.BeginGracefulLogout(4, "Local reason", machine, lifecycle);
        FixMessage peer = ValidInboundLogout("Peer reason");

        LogoutActionResult result = processor.ProcessInboundLogout(peer, 5, 6, "PRIMARY", machine, lifecycle);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.SendLogout, Is.False);
            Assert.That(result.CloseTransport, Is.True);
            Assert.That(result.SimultaneousLogout, Is.True);
            Assert.That(result.PeerText, Is.EqualTo("Peer reason"));
            Assert.That(machine.State, Is.EqualTo(SessionState.Disconnected));
            Assert.That(lifecycle.HasDeadline, Is.False);
            Assert.That(lifecycle.LogoutNotified, Is.True);
            Assert.That(application.Order, Is.EqualTo(new[] { "admin", "logout" }));
            Assert.That(application.AdministrativeContext!.Message, Is.SameAs(peer));
            Assert.That(application.LogoutContext!.Message, Is.SameAs(peer));
            Assert.That(application.LogoutContext.SequenceNumber, Is.EqualTo(5));
        });
    }

    [Test]
    public void PeerInitiatedLogoutNotifiesAdminThenWaitsForResponseWriteBeforeLogoutNotification()
    {
        RecordingApplication application = new RecordingApplication();
        LogoutProcessor processor = Processor(Settings(), new LogoutStubValidator(), application, new FakeSessionClock(Now));
        SessionStateMachine machine = ActiveMachine();
        LogoutLifecycleState lifecycle = processor.CreateLogoutState();
        FixMessage peer = ValidInboundLogout("Venue shutdown");

        LogoutActionResult received = processor.ProcessInboundLogout(peer, 17, 23, "PRIMARY", machine, lifecycle);

        Assert.Multiple(() =>
        {
            Assert.That(received.Accepted, Is.True);
            Assert.That(received.SendLogout, Is.True);
            Assert.That(received.CloseTransport, Is.False);
            Assert.That(Text(received.OutboundMessage!, FixTags.MsgSeqNum), Is.EqualTo("23"));
            Assert.That(Text(received.OutboundMessage!, FixTags.Text), Is.EqualTo("Venue shutdown"));
            Assert.That(machine.State, Is.EqualTo(SessionState.LogoutPending));
            Assert.That(application.Order, Is.EqualTo(new[] { "admin" }));
            Assert.That(lifecycle.ResponsePending, Is.True);
        });

        LogoutActionResult sent = processor.ConfirmLogoutSent(machine, lifecycle);

        Assert.Multiple(() =>
        {
            Assert.That(sent.Accepted, Is.True);
            Assert.That(sent.CloseTransport, Is.True);
            Assert.That(sent.ImmediateClose, Is.False);
            Assert.That(sent.Code, Is.EqualTo("logout-response-sent"));
            Assert.That(machine.State, Is.EqualTo(SessionState.Disconnected));
            Assert.That(application.Order, Is.EqualTo(new[] { "admin", "logout" }));
            Assert.That(application.LogoutContext!.SequenceNumber, Is.EqualTo(17));
        });
    }

    [Test]
    public void DuplicatePeerLogoutWhileResponsePendingReusesResponseAndNotifiesLogoutOnce()
    {
        RecordingApplication application = new RecordingApplication();
        LogoutProcessor processor = Processor(Settings(), new LogoutStubValidator(), application, new FakeSessionClock(Now));
        SessionStateMachine machine = ActiveMachine();
        LogoutLifecycleState lifecycle = processor.CreateLogoutState();
        FixMessage peer = ValidInboundLogout("Close");
        LogoutActionResult first = processor.ProcessInboundLogout(peer, 2, 8, "PRIMARY", machine, lifecycle);

        LogoutActionResult duplicate = processor.ProcessInboundLogout(peer, 2, 9, "PRIMARY", machine, lifecycle);
        LogoutActionResult completed = processor.ConfirmLogoutSent(machine, lifecycle);
        LogoutActionResult repeatedConfirmation = processor.ConfirmLogoutSent(machine, lifecycle);

        Assert.Multiple(() =>
        {
            Assert.That(duplicate.Accepted, Is.True);
            Assert.That(duplicate.Code, Is.EqualTo("logout-response-pending"));
            Assert.That(duplicate.OutboundMessage, Is.SameAs(first.OutboundMessage));
            Assert.That(application.Order, Is.EqualTo(new[] { "admin", "admin", "logout" }));
            Assert.That(completed.CloseTransport, Is.True);
            Assert.That(repeatedConfirmation.Accepted, Is.False);
            Assert.That(repeatedConfirmation.Code, Is.EqualTo("no-logout-response-pending"));
            Assert.That(application.LogoutCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void GracefulLogoutTimesOutExactlyAtDeadlineAndClosesImmediately()
    {
        SessionSettings settings = Settings();
        RecordingApplication application = new RecordingApplication();
        FakeSessionClock clock = new FakeSessionClock(Now);
        LogoutProcessor processor = Processor(settings, new LogoutStubValidator(), application, clock);
        SessionStateMachine machine = ActiveMachine();
        LogoutLifecycleState lifecycle = processor.CreateLogoutState();
        processor.BeginGracefulLogout(4, "Wait", machine, lifecycle);
        clock.AdvanceClock(TimeSpan.FromMilliseconds(settings.LogoutTimeoutMilliseconds - 1));

        LogoutActionResult waiting = processor.CheckLogoutTimeout(machine, lifecycle);
        clock.AdvanceClock(TimeSpan.FromMilliseconds(1));
        LogoutActionResult timedOut = processor.CheckLogoutTimeout(machine, lifecycle);

        Assert.Multiple(() =>
        {
            Assert.That(waiting.Accepted, Is.True);
            Assert.That(waiting.Code, Is.EqualTo("logout-timeout-pending"));
            Assert.That(waiting.CloseTransport, Is.False);
            Assert.That(timedOut.Accepted, Is.True);
            Assert.That(timedOut.TimedOut, Is.True);
            Assert.That(timedOut.CloseTransport, Is.True);
            Assert.That(timedOut.ImmediateClose, Is.True);
            Assert.That(machine.State, Is.EqualTo(SessionState.Disconnected));
            Assert.That(application.Order, Is.EqualTo(new[] { "logout" }));
            Assert.That(Text(application.LogoutContext!.Message, FixTags.Text), Is.EqualTo("Logout timed out."));
        });
    }

    [Test]
    public void PeerResponseTimeoutPreservesAdminBeforeLogoutOrdering()
    {
        SessionSettings settings = Settings();
        RecordingApplication application = new RecordingApplication();
        FakeSessionClock clock = new FakeSessionClock(Now);
        LogoutProcessor processor = Processor(settings, new LogoutStubValidator(), application, clock);
        SessionStateMachine machine = ActiveMachine();
        LogoutLifecycleState lifecycle = processor.CreateLogoutState();
        FixMessage peer = ValidInboundLogout("Peer timeout");
        processor.ProcessInboundLogout(peer, 3, 4, "PRIMARY", machine, lifecycle);
        clock.AdvanceClock(TimeSpan.FromMilliseconds(settings.LogoutTimeoutMilliseconds));

        LogoutActionResult result = processor.CheckLogoutTimeout(machine, lifecycle);

        Assert.Multiple(() =>
        {
            Assert.That(result.TimedOut, Is.True);
            Assert.That(result.ImmediateClose, Is.True);
            Assert.That(application.Order, Is.EqualTo(new[] { "admin", "logout" }));
            Assert.That(application.LogoutContext!.Message, Is.SameAs(peer));
            Assert.That(application.LogoutCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void ImmediateDisconnectClosesWithoutWireLogoutAndNotifiesOnlyOnce()
    {
        RecordingApplication application = new RecordingApplication();
        LogoutProcessor processor = Processor(Settings(), new LogoutStubValidator(), application, new FakeSessionClock(Now));
        SessionStateMachine machine = ActiveMachine();
        LogoutLifecycleState lifecycle = processor.CreateLogoutState();

        LogoutActionResult first = processor.DisconnectImmediately("Fatal transport error", machine, lifecycle);
        LogoutActionResult repeated = processor.DisconnectImmediately("Again", machine, lifecycle);

        Assert.Multiple(() =>
        {
            Assert.That(first.Accepted, Is.True);
            Assert.That(first.CloseTransport, Is.True);
            Assert.That(first.ImmediateClose, Is.True);
            Assert.That(first.SendLogout, Is.False);
            Assert.That(machine.State, Is.EqualTo(SessionState.Disconnected));
            Assert.That(application.Order, Is.EqualTo(new[] { "logout" }));
            Assert.That(Text(application.LogoutContext!.Message, FixTags.Text), Is.EqualTo("Fatal transport error"));
            Assert.That(repeated.LogoutDecision!.Code, Is.EqualTo("logout-already-notified"));
            Assert.That(application.LogoutCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void InboundValidationFailureDoesNotMutateStateOrNotifyApplication()
    {
        LogoutStubValidator validator = new LogoutStubValidator
        {
            Result = new ValidationResult
            {
                IsValid = false,
                Issues = [new ValidationIssue { Severity = ValidationSeverity.Error, Layer = ValidationLayer.DictionaryGrammar, Code = "required-field-missing", Message = "Missing field." }]
            }
        };
        RecordingApplication application = new RecordingApplication();
        LogoutProcessor processor = Processor(Settings(), validator, application, new FakeSessionClock(Now));
        SessionStateMachine machine = ActiveMachine();
        LogoutLifecycleState lifecycle = processor.CreateLogoutState();

        LogoutActionResult result = processor.ProcessInboundLogout(ValidInboundLogout("Invalid"), 2, 3, "PRIMARY", machine, lifecycle);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Code, Is.EqualTo("required-field-missing"));
            Assert.That(result.Issues, Has.Count.EqualTo(1));
            Assert.That(machine.State, Is.EqualTo(SessionState.Active));
            Assert.That(lifecycle.InProgress, Is.False);
            Assert.That(lifecycle.RemoteLogoutReceived, Is.False);
            Assert.That(application.Order, Is.Empty);
        });
    }

    [Test]
    public void InvalidStateAndMissingAdministrativeDecisionDoNotPartiallyStartLogout()
    {
        RecordingApplication application = new RecordingApplication();
        LogoutProcessor processor = Processor(Settings(), new LogoutStubValidator(), application, new FakeSessionClock(Now));
        SessionStateMachine stopped = new SessionStateMachine(SessionRole.Initiator);
        LogoutLifecycleState stoppedLifecycle = processor.CreateLogoutState();
        LogoutActionResult stoppedResult = processor.ProcessInboundLogout(ValidInboundLogout("Stopped"), 2, 3, "PRIMARY", stopped, stoppedLifecycle);

        RecordingApplication missingDecision = new RecordingApplication { ReturnMissingAdministrativeDecision = true };
        LogoutProcessor missingProcessor = Processor(Settings(), new LogoutStubValidator(), missingDecision, new FakeSessionClock(Now));
        SessionStateMachine active = ActiveMachine();
        LogoutLifecycleState activeLifecycle = missingProcessor.CreateLogoutState();
        LogoutActionResult missingResult = missingProcessor.ProcessInboundLogout(ValidInboundLogout("Missing"), 2, 3, "PRIMARY", active, activeLifecycle);

        Assert.Multiple(() =>
        {
            Assert.That(stoppedResult.Code, Is.EqualTo("logout-not-valid-in-session-state"));
            Assert.That(stopped.State, Is.EqualTo(SessionState.Stopped));
            Assert.That(stoppedLifecycle.RemoteLogoutReceived, Is.False);
            Assert.That(application.Order, Is.Empty);
            Assert.That(missingResult.Code, Is.EqualTo("missing-administrative-decision"));
            Assert.That(active.State, Is.EqualTo(SessionState.Active));
            Assert.That(activeLifecycle.InProgress, Is.False);
        });
    }

    [Test]
    public void DuplicateTextAndWrongMessageTypeAreRejectedBeforeCallbacks()
    {
        RecordingApplication application = new RecordingApplication();
        LogoutProcessor processor = Processor(Settings(), new LogoutStubValidator(), application, new FakeSessionClock(Now));
        FixMessage duplicate = ValidInboundLogout("One");
        duplicate.BodyFields.Add(Field(FixTags.Text, "Two"));
        LogoutActionResult duplicateResult = processor.ProcessInboundLogout(duplicate, 2, 3, "PRIMARY", ActiveMachine(), processor.CreateLogoutState());

        FixMessage wrongType = ValidInboundLogout("Wrong");
        wrongType.MsgType = FixAdminMessageTypes.Heartbeat;
        FindField(wrongType, FixTags.MsgType)!.Value = Encoding.Latin1.GetBytes(FixAdminMessageTypes.Heartbeat);
        LogoutActionResult wrongResult = processor.ProcessInboundLogout(wrongType, 2, 3, "PRIMARY", ActiveMachine(), processor.CreateLogoutState());

        Assert.Multiple(() =>
        {
            Assert.That(duplicateResult.Code, Is.EqualTo("duplicate-logout-text"));
            Assert.That(duplicateResult.Issues[0].SessionRejectReason, Is.EqualTo(13));
            Assert.That(wrongResult.Code, Is.EqualTo("unexpected-logout-message-type"));
            Assert.That(wrongResult.Issues[0].SessionRejectReason, Is.EqualTo(11));
            Assert.That(application.Order, Is.Empty);
        });
    }

    [Test]
    public void ConstructorAndOperationsRejectInvalidDependenciesAndArguments()
    {
        SessionSettings settings = Settings();
        LogoutStubValidator validator = new LogoutStubValidator();
        RecordingApplication application = new RecordingApplication();
        FakeSessionClock clock = new FakeSessionClock(Now);
        FixDictionary dictionary = new FixDictionary();
        LogoutProcessor processor = new LogoutProcessor(settings, dictionary, validator, application, clock);
        SessionSettings invalidTimeout = Settings();
        invalidTimeout.LogoutTimeoutMilliseconds = 0;

        Assert.Multiple(() =>
        {
            Assert.Throws<FixSessionException>(() => processor.CreateLogoutMessage(0, "bad"));
            Assert.Throws<FixSessionException>(() => new LogoutProcessor(null!, dictionary, validator, application, clock));
            Assert.Throws<FixSessionException>(() => new LogoutProcessor(settings, null!, validator, application, clock));
            Assert.Throws<FixSessionException>(() => new LogoutProcessor(invalidTimeout, dictionary, validator, application, clock));
            Assert.Throws<FixSessionException>(() => processor.BeginGracefulLogout(1, "bad", null!, processor.CreateLogoutState()));
        });
    }

    internal static LogoutProcessor Processor(SessionSettings settings, LogoutStubValidator validator, RecordingApplication application, FakeSessionClock clock)
    {
        FixDictionary dictionary = new FixDictionary();
        dictionary.Identity.BeginString = settings.Id.BeginString;
        return new LogoutProcessor(settings, dictionary, validator, application, clock);
    }

    internal static SessionSettings Settings()
    {
        SessionSettings settings = new SessionSettings();
        settings.Role = SessionRole.Initiator;
        settings.Id.BeginString = FixBeginStrings.Fixt11;
        settings.Id.SenderCompID = "LOCAL";
        settings.Id.TargetCompID = "REMOTE";
        settings.Id.Qualifier = "PRIMARY";
        settings.TransportDictionary.BeginString = FixBeginStrings.Fixt11;
        settings.DefaultApplicationVersion = FixApplicationVersions.Fix50Sp2;
        settings.LogoutTimeoutMilliseconds = 2000;
        return settings;
    }

    internal static SessionStateMachine ActiveMachine()
    {
        SessionStateMachine machine = new SessionStateMachine(SessionRole.Initiator);
        machine.StartSession();
        machine.BeginConnection();
        machine.CompleteConnection();
        machine.BeginLogon();
        machine.CompleteLogon();
        return machine;
    }

    internal static FixMessage ValidInboundLogout(string text)
    {
        FixMessage message = new FixMessage();
        message.BeginString = FixBeginStrings.Fixt11;
        message.MsgType = FixAdminMessageTypes.Logout;
        message.HeaderFields.Add(Field(FixTags.BeginString, FixBeginStrings.Fixt11));
        message.HeaderFields.Add(Field(FixTags.MsgType, FixAdminMessageTypes.Logout));
        message.HeaderFields.Add(Field(FixTags.MsgSeqNum, "2"));
        message.HeaderFields.Add(Field(FixTags.SenderCompID, "REMOTE"));
        message.HeaderFields.Add(Field(FixTags.TargetCompID, "LOCAL"));
        message.HeaderFields.Add(Field(FixTags.SendingTime, "20260830-12:00:00.123"));
        if (text.Length != 0)
        {
            message.BodyFields.Add(Field(FixTags.Text, text));
        }
        return message;
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

    internal class LogoutStubValidator : IMessageValidator
    {
        internal ValidationResult Result = new ValidationResult { IsValid = true };

        public ValidationResult ValidateMessage(FixMessage message, FixDictionary dictionary, ValidationOptions options, ValidationContext context)
        {
            return Result;
        }
    }

    internal class RecordingApplication : ISessionApplication
    {
        internal List<string> Order = [];
        internal ApplicationMessageContext? AdministrativeContext;
        internal ApplicationMessageContext? LogoutContext;
        internal int LogoutCount;
        internal bool ReturnMissingAdministrativeDecision;

        public ApplicationDecision OnSessionCreated(SessionId id)
        {
            return Accept("created");
        }

        public ApplicationDecision OnSessionLogon(ApplicationMessageContext context)
        {
            return Accept("logon");
        }

        public ApplicationDecision OnSessionLogout(ApplicationMessageContext context)
        {
            Order.Add("logout");
            LogoutContext = context;
            LogoutCount++;
            return Accept("logout");
        }

        public ApplicationDecision OnAdministrativeMessage(ApplicationMessageContext context)
        {
            Order.Add("admin");
            AdministrativeContext = context;
            if (ReturnMissingAdministrativeDecision)
            {
                return null!;
            }
            return Accept("admin");
        }

        public ApplicationDecision OnApplicationMessage(ApplicationMessageContext context)
        {
            return Accept("application");
        }

        public ApplicationDecision OnReplayMessage(ApplicationMessageContext context)
        {
            return Accept("replay");
        }

        internal static ApplicationDecision Accept(string code)
        {
            return new ApplicationDecision { Disposition = MessageDisposition.Accept, Code = code };
        }
    }
}
