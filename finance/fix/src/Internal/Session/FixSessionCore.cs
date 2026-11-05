using System;
using System.Collections.Generic;
using System.Text;

namespace fix;

internal class FixSessionCore
{
    internal SessionSettings Settings;
    internal ISessionTransport Transport;
    internal IMessageStore Store;
    internal IFixLog Log;
    internal ISessionClock Clock;
    internal IMessageThrottle Throttle;
    internal ISessionApplication Application;
    internal IMessageFramer Framer;
    internal IMessageDecoder Decoder;
    internal IMessageEncoder Encoder;
    internal IMessageValidator Validator;
    internal IDictionaryProvider DictionaryProvider;
    internal ISessionAuthenticator Authenticator;
    internal FixDictionary TransportDictionary;
    internal FixDictionary ApplicationDictionary;
    internal FixDictionaryIdentity TransportIdentity = new FixDictionaryIdentity();
    internal FixDictionaryIdentity ApplicationIdentity = new FixDictionaryIdentity();
    internal FrameOptions FrameOptions = new FrameOptions();
    internal DecodeOptions DecodeOptions = new DecodeOptions();
    internal EncodeOptions EncodeOptions = new EncodeOptions();
    internal FixMessageOperations MessageOperations = new FixMessageOperations();
    internal SessionStateMachine StateMachine;
    internal SessionSequenceProcessor Sequences;
    internal SessionTimers Timers;
    internal GapTracker Gaps;
    internal SequenceResetProcessor ResetProcessor;
    internal ReplayBuilder ReplayBuilder;
    internal ReplayProcessor ReplayProcessor;
    internal AdminMessageFactory AdminFactory;
    internal AdminMessageProcessor AdminProcessor;
    internal LogonProcessor LogonProcessor;
    internal LogonNegotiationState Negotiation;
    internal LogoutProcessor LogoutProcessor;
    internal LogoutLifecycleState LogoutLifecycle;
    internal SessionHeaderValidator HeaderValidator;
    internal SessionScheduler Scheduler;
    internal ReconnectPlanner Reconnects;
    internal List<SessionOutboundMessage> OutboundMessages = [];
    internal List<byte> InboundBytes = [];
    internal TransportState CurrentTransportState = TransportState.Disconnected;
    internal string LastEncodingCode = "";
    internal string LastEncodingText = "";
    internal bool LogonResetApplied;
    internal bool ClosureResetApplied;
    internal bool HasConnectDeadline;
    internal DateTime ConnectDeadlineUtc;
    internal bool HasLogonDeadline;
    internal DateTime LogonDeadlineUtc;

    internal FixSessionCore(SessionSettings settings, ISessionTransport transport, IMessageStore store, IFixLog log, ISessionClock clock, IMessageThrottle throttle, ISessionApplication application, IMessageFramer framer, IMessageDecoder decoder, IMessageEncoder encoder, IMessageValidator validator, IDictionaryProvider dictionaryProvider, ISessionAuthenticator authenticator)
    {
        if (settings == null || settings.Id == null || settings.Schedule == null || settings.Validation == null || settings.Throttle == null || settings.Tls == null || settings.TransportDictionary == null || settings.ApplicationDictionary == null)
        {
            throw new FixSessionException("FixSession settings are missing.");
        }
        if (transport == null || store == null || log == null || clock == null || throttle == null || application == null || framer == null || decoder == null || encoder == null || validator == null || dictionaryProvider == null || authenticator == null)
        {
            throw new FixSessionException("A FixSession dependency is missing.");
        }
        if (settings.Endpoints == null || settings.Endpoints.Count == 0)
        {
            throw new FixSessionException("FixSession requires at least one transport endpoint.");
        }
        if (settings.ConnectTimeoutMilliseconds <= 0 || settings.LogonTimeoutMilliseconds <= 0)
        {
            throw new FixSessionException("The connect and Logon timeouts must be positive.");
        }
        if (settings.Id.BeginString.Length == 0 || settings.Id.SenderCompID.Length == 0 || settings.Id.TargetCompID.Length == 0)
        {
            throw new FixSessionException("The FixSession identity is incomplete.");
        }

        Settings = settings;
        Transport = transport;
        Store = store;
        Log = log;
        Clock = clock;
        Throttle = throttle;
        Application = application;
        Framer = framer;
        Decoder = decoder;
        Encoder = encoder;
        Validator = validator;
        DictionaryProvider = dictionaryProvider;
        Authenticator = authenticator;
        TransportIdentity = LogonProcessor.CopyIdentity(settings.TransportDictionary);
        if (TransportIdentity.BeginString.Length == 0) TransportIdentity.BeginString = settings.Id.BeginString;
        TransportDictionary = ResolveRequiredDictionary(TransportIdentity, "transport");
        ApplicationIdentity = LogonProcessor.CopyIdentity(settings.ApplicationDictionary);
        if (ApplicationIdentity.ApplicationVersion.Length == 0) ApplicationIdentity.ApplicationVersion = settings.DefaultApplicationVersion;
        ApplicationDictionary = settings.Id.BeginString == FixBeginStrings.Fixt11 ? ResolveRequiredDictionary(ApplicationIdentity, "application") : TransportDictionary;

        FrameOptions.MaxFrameSize = settings.Validation.MaxMessageSize;
        DecodeOptions.MaxMessageSize = settings.Validation.MaxMessageSize;
        DecodeOptions.MaxFieldSize = settings.Validation.MaxFieldSize;
        DecodeOptions.MaxFieldsPerMessage = settings.Validation.MaxFieldsPerMessage;
        EncodeOptions.MaxMessageSize = settings.Validation.MaxMessageSize;
        EncodeOptions.MaxFieldSize = settings.Validation.MaxFieldSize;
        EncodeOptions.PreserveFieldOrder = false;

        StateMachine = new SessionStateMachine(settings.Role);
        Sequences = new SessionSequenceProcessor(store);
        Timers = new SessionTimers(clock);
        Gaps = new GapTracker(settings);
        ResetProcessor = new SequenceResetProcessor(Sequences, Gaps);
        ReplayBuilder = new ReplayBuilder(settings.Id, ApplicationDictionary, decoder, encoder, MessageOperations, clock);
        ReplayProcessor = new ReplayProcessor(store, Sequences, ReplayBuilder, application);
        AdminFactory = new AdminMessageFactory(settings, clock);
        AdminProcessor = new AdminMessageProcessor(Timers, ReplayProcessor, ResetProcessor, AdminFactory, application);
        LogonProcessor = new LogonProcessor(settings, TransportDictionary, validator, dictionaryProvider, authenticator, clock);
        Negotiation = LogonProcessor.CreateNegotiationState();
        LogoutProcessor = new LogoutProcessor(settings, TransportDictionary, validator, application, clock);
        LogoutLifecycle = LogoutProcessor.CreateLogoutState();
        HeaderValidator = new SessionHeaderValidator(settings, clock);
        Scheduler = new SessionScheduler(settings.Schedule, clock);
        Reconnects = new ReconnectPlanner(settings.Endpoints, settings.ReconnectDelayMilliseconds, settings.MaxReconnectDelayMilliseconds);
    }

    internal SessionPollResult StartSessionWork()
    {
        lock (this)
        {
            SessionPollResult result = new SessionPollResult();
            if (StateMachine.State != SessionState.Stopped)
            {
                AddEvent(result, SessionEventKind.Error, null, 0, "session-already-started", "The session is already started.");
                return CompletePoll(result);
            }
            ApplicationDecision created = Application.OnSessionCreated(LogonProcessor.CopySessionId(Settings.Id));
            if (!IsAcceptedDecision(created))
            {
                AddDecisionFailure(result, created, "session-creation-rejected", "The application rejected session creation.");
                return CompletePoll(result);
            }
            SessionSequenceActionResult refreshed = Sequences.RefreshSequences();
            if (!refreshed.Accepted)
            {
                AddEvent(result, SessionEventKind.Error, null, 0, refreshed.Code, refreshed.Text);
                return CompletePoll(result);
            }
            SessionStateTransitionResult started = StateMachine.StartSession();
            AddTransitionEvent(result, started);
            SessionScheduleEvaluation schedule = Scheduler.EvaluateSchedule();
            if (schedule.BoundaryStarted || schedule.BoundaryEnded)
            {
                HandleScheduleEvaluation(schedule, result);
                return CompletePoll(result);
            }
            if (!schedule.Active)
            {
                AddEvent(result, SessionEventKind.None, null, 0, "session-schedule-inactive", "The session started outside its configured active schedule.");
                return CompletePoll(result);
            }
            SessionStateTransitionResult connecting = StateMachine.BeginConnection();
            if (!connecting.Accepted)
            {
                AddEvent(result, SessionEventKind.Error, null, 0, connecting.Code, connecting.Text);
                return CompletePoll(result);
            }
            BeginConnectDeadline();
            OpenTransport(result);
            return CompletePoll(result);
        }
    }

    internal SessionPollResult PollSessionWork()
    {
        lock (this)
        {
            SessionPollResult result = new SessionPollResult();
            if (StateMachine.State == SessionState.Stopped || StateMachine.State == SessionState.Faulted)
            {
                return CompletePoll(result);
            }
            SessionScheduleEvaluation schedule = Scheduler.EvaluateSchedule();
            if (HandleScheduleEvaluation(schedule, result)) return CompletePoll(result);
            if (StateMachine.State == SessionState.Reconnecting || (StateMachine.State == SessionState.Disconnected && Reconnects.Waiting))
            {
                if (!Reconnects.IsAttemptDue(ReadUtcNow())) return CompletePoll(result);
                SessionStateTransitionResult retrying = StateMachine.BeginConnection();
                if (!retrying.Accepted)
                {
                    FaultSession(result, retrying.Code, retrying.Text);
                    return CompletePoll(result);
                }
                Reconnects.MarkAttemptStarted();
                BeginConnectDeadline();
                OpenTransport(result);
                return CompletePoll(result);
            }
            if (StateMachine.State == SessionState.Disconnected) return CompletePoll(result);
            if (StateMachine.State == SessionState.Connecting)
            {
                if (CheckConnectDeadline(result)) return CompletePoll(result);
                OpenTransport(result);
                return CompletePoll(result);
            }
            if (StateMachine.State == SessionState.LogonPending && CheckLogonDeadline(result)) return CompletePoll(result);
            if (StateMachine.State == SessionState.LogoutPending && CheckLogoutDeadline(result)) return CompletePoll(result);
            if (PrepareWaitingMessage(result))
            {
                return CompletePoll(result);
            }
            if (OutboundMessages.Count != 0 && OutboundMessages[0].IsPrepared)
            {
                FlushOneOutbound(result);
                return CompletePoll(result);
            }
            if (ProcessBufferedFrame(result))
            {
                return CompletePoll(result);
            }
            ReadOneTransportChunk(result);
            if (result.Events.Count != 0 && StateMachine.State == SessionState.Disconnected || StateMachine.State == SessionState.Faulted)
            {
                return CompletePoll(result);
            }
            if (ProcessBufferedFrame(result))
            {
                return CompletePoll(result);
            }
            PollTimers(result);
            return CompletePoll(result);
        }
    }

    internal SessionSendResult SendSessionMessage(FixMessage message)
    {
        lock (this)
        {
            if (message == null)
            {
                return CreateSendFailure(MessageDisposition.Reject, "missing-send-message", "The submitted message is missing.");
            }
            bool administrative = IsAdministrativeType(message.MsgType);
            bool active = StateMachine.State == SessionState.Active || StateMachine.State == SessionState.Recovering;
            if (!active)
            {
                if (!Settings.AllowSendBeforeLogon || administrative || StateMachine.State == SessionState.Stopped || StateMachine.State == SessionState.Faulted)
                {
                    return CreateSendFailure(MessageDisposition.Reject, "session-not-active", "The session is not active for this message.");
                }
                ApplicationDecision waitingThrottle = CheckThrottle(message, false);
                if (!IsAcceptedDecision(waitingThrottle))
                {
                    return FromDecision(waitingThrottle);
                }
                SessionOutboundMessage waiting = new SessionOutboundMessage();
                waiting.Message = CloneMessage(message);
                waiting.IsAdministrative = false;
                OutboundMessages.Add(waiting);
                SessionSendResult queued = CreateSendSuccess(0, "message-queued-before-logon", "The application message was queued until Logon completes.");
                queued.QueueCount = OutboundMessages.Count;
                return queued;
            }
            if (!administrative)
            {
                ApplicationDecision throttle = CheckThrottle(message, false);
                if (!IsAcceptedDecision(throttle))
                {
                    return FromDecision(throttle);
                }
            }
            int sequenceNumber = NextAvailableSenderSequence();
            FixMessage prepared = PrepareMessageHeader(CloneMessage(message), sequenceNumber);
            SessionQueueResult queuedResult = QueuePreparedMessage(prepared, administrative, false, true);
            return FromQueueResult(queuedResult);
        }
    }

    internal SessionPollResult BeginSessionLogout(string text)
    {
        lock (this)
        {
            SessionPollResult result = new SessionPollResult();
            int sequenceNumber = NextAvailableSenderSequence();
            LogoutActionResult action = LogoutProcessor.BeginGracefulLogout(sequenceNumber, text, StateMachine, LogoutLifecycle);
            if (!action.Accepted || action.OutboundMessage == null)
            {
                AddEvent(result, SessionEventKind.Error, null, sequenceNumber, action.Code, action.Text);
                return CompletePoll(result);
            }
            SessionQueueResult queued = QueuePreparedMessage(action.OutboundMessage, true, false, true);
            if (!queued.Accepted)
            {
                AddEvent(result, SessionEventKind.Error, action.OutboundMessage, sequenceNumber, queued.Code, queued.Text);
                FaultSession(result, queued.Code, queued.Text);
                return CompletePoll(result);
            }
            AddEvent(result, SessionEventKind.Logout, action.OutboundMessage, sequenceNumber, action.Code, action.Text);
            return CompletePoll(result);
        }
    }

    internal SessionPollResult StopSessionWork()
    {
        lock (this)
        {
            SessionPollResult result = new SessionPollResult();
            bool hadTransport = StateMachine.State != SessionState.Stopped && StateMachine.State != SessionState.Disconnected;
            Timers.StopTimers();
            HasConnectDeadline = false;
            HasLogonDeadline = false;
            Reconnects.ResetPlan();
            TransportWriteResult closed = Transport.CloseTransport();
            CurrentTransportState = closed == null ? TransportState.Closed : closed.State;
            if (hadTransport && !ClosureResetApplied)
            {
                ApplyConfiguredSequenceReset(SessionResetMode.OnDisconnect, result, "disconnect-sequence-reset", "The sequence state was reset while stopping the connected session.");
            }
            OutboundMessages.Clear();
            InboundBytes.Clear();
            Gaps.ResetGapTracking();
            SessionStateTransitionResult stopped = StateMachine.StopSession();
            AddTransitionEvent(result, stopped);
            return CompletePoll(result);
        }
    }

    internal SessionSnapshot CreateSessionSnapshot()
    {
        lock (this)
        {
            SessionSnapshot snapshot = new SessionSnapshot();
            snapshot.Id = LogonProcessor.CopySessionId(Settings.Id);
            snapshot.State = StateMachine.State;
            snapshot.TransportState = CurrentTransportState;
            snapshot.IsLoggedOn = Negotiation.IsLoggedOn && (StateMachine.State == SessionState.Active || StateMachine.State == SessionState.Recovering || StateMachine.State == SessionState.LogoutPending);
            snapshot.NextSenderSeqNum = Sequences.NextSenderSeqNum;
            snapshot.NextTargetSeqNum = Sequences.NextTargetSeqNum;
            snapshot.InboundQueueCount = Sequences.QueuedMessages.Count;
            snapshot.OutboundQueueCount = OutboundMessages.Count;
            if (Timers.Running)
            {
                snapshot.HasLastInboundTime = true;
                snapshot.LastInboundTimeUtc = Timers.LastInboundUtc;
                snapshot.HasLastOutboundTime = true;
                snapshot.LastOutboundTimeUtc = Timers.LastOutboundUtc;
            }
            return snapshot;
        }
    }

    internal void OpenTransport(SessionPollResult result)
    {
        TransportWriteResult opened = Transport.OpenTransport(Reconnects.GetCurrentEndpoint(), Settings.Tls);
        if (opened == null)
        {
            FaultSession(result, "missing-transport-open-result", "The transport did not return an open result.");
            return;
        }
        CurrentTransportState = opened.State;
        if (opened.ErrorCode.Length != 0 || opened.State == TransportState.Faulted || opened.State == TransportState.Closed)
        {
            string code = opened.ErrorCode.Length == 0 ? "transport-open-failed" : opened.ErrorCode;
            string text = opened.ErrorMessage.Length == 0 ? "The transport could not be opened." : opened.ErrorMessage;
            if (TransportFailurePolicy.IsRetryableFailure(opened.State, code)) ScheduleTransportRetry(result, code, text);
            else FaultSession(result, code, text);
            return;
        }
        if (opened.State == TransportState.Connected && HasConnectDeadline && ReadUtcNow() >= ConnectDeadlineUtc)
        {
            ScheduleTransportRetry(result, "connect-timeout", "The transport connected after its deadline elapsed.");
            return;
        }
        if (opened.State != TransportState.Connected)
        {
            return;
        }
        HasConnectDeadline = false;
        ClosureResetApplied = false;
        LogonResetApplied = false;
        SessionStateTransitionResult connected = StateMachine.CompleteConnection();
        AddTransitionEvent(result, connected);
        SessionStateTransitionResult pending = StateMachine.BeginLogon();
        if (!pending.Accepted)
        {
            FaultSession(result, pending.Code, pending.Text);
            return;
        }
        BeginLogonDeadline();
        if (Settings.Role == SessionRole.Initiator)
        {
            Negotiation.ResetSequenceNumbers = (Settings.ResetMode & SessionResetMode.OnLogon) == SessionResetMode.OnLogon;
            if (!ApplyConfiguredSequenceReset(SessionResetMode.OnLogon, result, "logon-sequence-reset", "The sequence state was reset before the initiator Logon."))
            {
                FaultSession(result, "logon-sequence-reset-failed", "The Logon sequence reset could not be persisted.");
                return;
            }
            LogonResetApplied = (Settings.ResetMode & SessionResetMode.OnLogon) == SessionResetMode.OnLogon;
            int sequenceNumber = NextAvailableSenderSequence();
            FixMessage logon = LogonProcessor.CreateLogonMessage(sequenceNumber, Sequences.NextTargetSeqNum, Negotiation);
            SessionQueueResult queued = QueuePreparedMessage(logon, true, false, true);
            if (!queued.Accepted) FaultSession(result, queued.Code, queued.Text);
        }
    }

    internal bool PrepareWaitingMessage(SessionPollResult result)
    {
        if (OutboundMessages.Count == 0 || OutboundMessages[0].IsPrepared || StateMachine.State != SessionState.Active && StateMachine.State != SessionState.Recovering)
        {
            return false;
        }
        SessionOutboundMessage waiting = OutboundMessages[0];
        OutboundMessages.RemoveAt(0);
        int sequenceNumber = NextAvailableSenderSequence();
        FixMessage prepared = PrepareMessageHeader(waiting.Message, sequenceNumber);
        SessionQueueResult queued = QueuePreparedMessage(prepared, false, false, true);
        if (!queued.Accepted)
        {
            AddEvent(result, SessionEventKind.Error, prepared, sequenceNumber, queued.Code, queued.Text);
            return true;
        }
        return false;
    }

    internal void FlushOneOutbound(SessionPollResult result)
    {
        SessionOutboundMessage outbound = OutboundMessages[0];
        if (!outbound.IsPrepared)
        {
            return;
        }
        TransportWriteResult written = Transport.WriteTransport(outbound.Bytes.AsSpan(outbound.Offset));
        if (written == null)
        {
            FaultSession(result, "missing-transport-write-result", "The transport did not return a write result.");
            return;
        }
        CurrentTransportState = written.State;
        int remaining = outbound.Bytes.Length - outbound.Offset;
        if (written.BytesWritten < 0 || written.BytesWritten > remaining)
        {
            FaultSession(result, "invalid-transport-write-count", "The transport returned an invalid write count.");
            return;
        }
        if (written.ErrorCode.Length != 0 || written.State == TransportState.Faulted || written.State == TransportState.Closed)
        {
            string code = written.ErrorCode.Length == 0 ? "transport-write-failed" : written.ErrorCode;
            string text = written.ErrorMessage.Length == 0 ? "The transport write failed." : written.ErrorMessage;
            if (TransportFailurePolicy.IsRetryableFailure(written.State, code)) ScheduleTransportRetry(result, code, text);
            else FaultSession(result, code, text);
            return;
        }
        outbound.Offset += written.BytesWritten;
        if (outbound.Offset < outbound.Bytes.Length)
        {
            AddEvent(result, SessionEventKind.Backpressure, outbound.Message, outbound.SequenceNumber, "partial-transport-write", "The outbound message remains partially written.");
            return;
        }
        OutboundMessages.RemoveAt(0);
        if (outbound.ConsumeSequence)
        {
            SessionSequenceActionResult consumed = Sequences.ProcessOutboundSequence(outbound.SequenceNumber, true);
            if (!consumed.Accepted)
            {
                FaultSession(result, consumed.Code, consumed.Text);
                return;
            }
        }
        if (Timers.Running) Timers.RecordOutboundActivity();
        WriteMessageLog(LogDirection.Outgoing, outbound.Bytes, outbound.Message);
        AddEvent(result, SessionEventKind.MessageSent, outbound.Message, outbound.SequenceNumber, "message-sent", "The complete message was written to the transport.");
        if (outbound.ConfirmLogoutResponse)
        {
            LogoutActionResult confirmed = LogoutProcessor.ConfirmLogoutSent(StateMachine, LogoutLifecycle);
            if (confirmed.CloseTransport)
            {
                AddEvent(result, SessionEventKind.Logout, outbound.Message, outbound.SequenceNumber, confirmed.Code, confirmed.Text);
                if (!ApplyConfiguredSequenceReset(SessionResetMode.OnLogout, result, "logout-sequence-reset", "The sequence state was reset after Logout completion."))
                {
                    FaultSession(result, "logout-sequence-reset-failed", "The Logout sequence reset could not be persisted.");
                    return;
                }
                ClosureResetApplied = (Settings.ResetMode & SessionResetMode.OnLogout) == SessionResetMode.OnLogout;
                CloseTransport(result, confirmed.Code, confirmed.Text, false);
            }
        }
        if (outbound.DisconnectAfterSend)
        {
            CloseTransport(result, "invalid-logon-disconnect", "The rejected Logon response was sent before disconnecting the transport.", false);
        }
    }

    internal void ReadOneTransportChunk(SessionPollResult result)
    {
        byte[] destination = new byte[8192];
        TransportReadResult read = Transport.ReadTransport(destination);
        if (read == null)
        {
            FaultSession(result, "missing-transport-read-result", "The transport did not return a read result.");
            return;
        }
        CurrentTransportState = read.State;
        if (read.BytesRead < 0 || read.BytesRead > destination.Length)
        {
            FaultSession(result, "invalid-transport-read-count", "The transport returned an invalid read count.");
            return;
        }
        for (int index = 0; index < read.BytesRead; index++) InboundBytes.Add(destination[index]);
        if (InboundBytes.Count > FrameOptions.MaxFrameSize)
        {
            FaultSession(result, "inbound-buffer-limit-exceeded", "The pending inbound frame exceeds the configured message limit.");
            return;
        }
        if (read.ErrorCode.Length != 0)
        {
            string text = read.ErrorMessage.Length == 0 ? "The transport read failed." : read.ErrorMessage;
            if (TransportFailurePolicy.IsRetryableFailure(read.State, read.ErrorCode)) ScheduleTransportRetry(result, read.ErrorCode, text);
            else FaultSession(result, read.ErrorCode, text);
            return;
        }
        if (read.EndOfStream || read.State == TransportState.Closed || read.State == TransportState.Faulted)
        {
            if (TransportFailurePolicy.IsRetryableFailure(read.State, "transport-end-of-stream")) ScheduleTransportRetry(result, "transport-end-of-stream", "The peer closed the transport.");
            else FaultSession(result, "transport-end-of-stream", "The peer closed the transport.");
        }
    }

    internal bool ProcessBufferedFrame(SessionPollResult result)
    {
        if (InboundBytes.Count == 0)
        {
            return false;
        }
        byte[] buffered = InboundBytes.ToArray();
        FrameResult frame = Framer.ReadFrame(buffered, FrameOptions);
        if (frame == null)
        {
            FaultSession(result, "missing-frame-result", "The message framer did not return a result.");
            return true;
        }
        if (frame.Status == FrameStatus.Incomplete)
        {
            if (frame.Consumed > 0 && frame.Consumed <= InboundBytes.Count) InboundBytes.RemoveRange(0, frame.Consumed);
            return false;
        }
        if (frame.Status != FrameStatus.Complete && frame.Status != FrameStatus.Recovered || frame.FrameOffset < 0 || frame.FrameLength <= 0 || frame.FrameOffset + frame.FrameLength > buffered.Length)
        {
            FaultSession(result, frame.ErrorCode.Length == 0 ? "invalid-inbound-frame" : frame.ErrorCode, frame.ErrorMessage.Length == 0 ? "The inbound FIX frame is invalid." : frame.ErrorMessage);
            return true;
        }
        byte[] frameBytes = new byte[frame.FrameLength];
        Array.Copy(buffered, frame.FrameOffset, frameBytes, 0, frame.FrameLength);
        int consumed = frame.Consumed > 0 ? frame.Consumed : frame.FrameOffset + frame.FrameLength;
        if (consumed > InboundBytes.Count) consumed = InboundBytes.Count;
        InboundBytes.RemoveRange(0, consumed);
        ProcessInboundFrame(frameBytes, result);
        return true;
    }

    internal void ProcessInboundFrame(byte[] frameBytes, SessionPollResult result)
    {
        DecodeResult decoded = Decoder.DecodeMessage(frameBytes, TransportDictionary, DecodeOptions);
        if (decoded == null || decoded.Status != DecodeStatus.Success || decoded.Message == null)
        {
            string code = decoded == null || decoded.ErrorCode.Length == 0 ? "inbound-decode-failed" : decoded.ErrorCode;
            string text = decoded == null || decoded.ErrorMessage.Length == 0 ? "The inbound frame could not be decoded." : decoded.ErrorMessage;
            FaultSession(result, code, text);
            return;
        }
        FixMessage message = decoded.Message;
        bool administrative = IsAdministrativeType(message.MsgType);
        SessionDictionaryResolution dictionaryResolution = ResolveMessageDictionary(message, administrative);
        if (!dictionaryResolution.Success)
        {
            FaultSession(result, dictionaryResolution.Code, dictionaryResolution.Text);
            return;
        }
        if (!administrative && Settings.Id.BeginString == FixBeginStrings.Fixt11 && ApplicationDictionary != TransportDictionary)
        {
            FixDictionary wireApplicationDictionary = CreateWireApplicationDictionary(dictionaryResolution.Dictionary);
            DecodeResult applicationDecoded = Decoder.DecodeMessage(frameBytes, wireApplicationDictionary, DecodeOptions);
            if (applicationDecoded == null || applicationDecoded.Status != DecodeStatus.Success || applicationDecoded.Message == null)
            {
                FaultSession(result, "application-decode-failed", "The inbound FIXT application message could not be decoded.");
                return;
            }
            message = applicationDecoded.Message;
            dictionaryResolution.Dictionary = wireApplicationDictionary;
        }
        WriteMessageLog(LogDirection.Incoming, frameBytes, message);
        int sequenceNumber = ReadPositiveHeaderInteger(message, FixTags.MsgSeqNum);
        if (message.MsgType == FixAdminMessageTypes.Logon)
        {
            ProcessInboundLogon(message, sequenceNumber, result);
            return;
        }
        if (StateMachine.State != SessionState.Active && StateMachine.State != SessionState.Recovering && StateMachine.State != SessionState.LogoutPending)
        {
            AddEvent(result, SessionEventKind.Error, message, sequenceNumber, "message-before-logon", "A non-Logon message arrived before Logon completed.");
            CloseTransport(result, "message-before-logon", "A non-Logon message arrived before Logon completed.", false);
            return;
        }
        if (message.MsgType == FixAdminMessageTypes.Logout)
        {
            ProcessInboundLogout(message, sequenceNumber, result);
            return;
        }
        ProcessEstablishedMessage(message, sequenceNumber, administrative, dictionaryResolution.Dictionary, dictionaryResolution.Identity, result);
    }

    internal FixDictionary CreateWireApplicationDictionary(FixDictionary dictionary)
    {
        FixDictionary copy = new FixDictionary();
        copy.Identity = LogonProcessor.CopyIdentity(dictionary.Identity);
        copy.Identity.BeginString = Settings.Id.BeginString;
        copy.Options = dictionary.Options;
        copy.Datatypes = dictionary.Datatypes;
        copy.CodeSets = dictionary.CodeSets;
        copy.Fields = new List<FixFieldDefinition>();
        for (int index = 0; index < dictionary.Fields.Count; index++) copy.Fields.Add(dictionary.Fields[index]);
        for (int index = 0; index < TransportDictionary.Fields.Count; index++)
        {
            FixFieldDefinition transportField = TransportDictionary.Fields[index];
            if (MessageBuilder.FindFieldDefinition(copy, transportField.Tag) == null) copy.Fields.Add(transportField);
        }
        copy.Header = TransportDictionary.Header;
        copy.Trailer = TransportDictionary.Trailer;
        Dictionary<int, int> componentIds = CreateWireComponentIds(dictionary);
        Dictionary<int, int> groupIds = CreateWireGroupIds(dictionary);
        copy.Components = new List<FixComponentDefinition>();
        for (int index = 0; index < TransportDictionary.Components.Count; index++) copy.Components.Add(TransportDictionary.Components[index]);
        for (int index = 0; index < dictionary.Components.Count; index++) copy.Components.Add(CloneWireComponent(dictionary.Components[index], componentIds, groupIds));
        copy.Groups = new List<FixGroupDefinition>();
        for (int index = 0; index < TransportDictionary.Groups.Count; index++) copy.Groups.Add(TransportDictionary.Groups[index]);
        for (int index = 0; index < dictionary.Groups.Count; index++) copy.Groups.Add(CloneWireGroup(dictionary.Groups[index], componentIds, groupIds));
        copy.Messages = new List<FixMessageDefinition>();
        for (int index = 0; index < dictionary.Messages.Count; index++) copy.Messages.Add(CloneWireMessage(dictionary.Messages[index], componentIds, groupIds));
        copy.Scenarios = new List<FixScenarioDefinition>();
        for (int index = 0; index < dictionary.Scenarios.Count; index++) copy.Scenarios.Add(CloneWireScenario(dictionary.Scenarios[index], componentIds, groupIds));
        copy.Diagnostics = dictionary.Diagnostics;
        return copy;
    }

    internal Dictionary<int, int> CreateWireComponentIds(FixDictionary dictionary)
    {
        Dictionary<int, int> ids = new Dictionary<int, int>();
        int next = FindNextWireComponentId(dictionary);
        for (int index = 0; index < dictionary.Components.Count; index++)
        {
            ids[dictionary.Components[index].Id] = next;
            next++;
        }
        return ids;
    }

    internal Dictionary<int, int> CreateWireGroupIds(FixDictionary dictionary)
    {
        Dictionary<int, int> ids = new Dictionary<int, int>();
        int next = FindNextWireGroupId(dictionary);
        for (int index = 0; index < dictionary.Groups.Count; index++)
        {
            ids[dictionary.Groups[index].Id] = next;
            next++;
        }
        return ids;
    }

    internal int FindNextWireComponentId(FixDictionary dictionary)
    {
        int maximum = 0;
        for (int index = 0; index < TransportDictionary.Components.Count; index++)
        {
            if (TransportDictionary.Components[index].Id > maximum) maximum = TransportDictionary.Components[index].Id;
        }
        for (int index = 0; index < dictionary.Components.Count; index++)
        {
            if (dictionary.Components[index].Id > maximum) maximum = dictionary.Components[index].Id;
        }
        return maximum + 1;
    }

    internal int FindNextWireGroupId(FixDictionary dictionary)
    {
        int maximum = 0;
        for (int index = 0; index < TransportDictionary.Groups.Count; index++)
        {
            if (TransportDictionary.Groups[index].Id > maximum) maximum = TransportDictionary.Groups[index].Id;
        }
        for (int index = 0; index < dictionary.Groups.Count; index++)
        {
            if (dictionary.Groups[index].Id > maximum) maximum = dictionary.Groups[index].Id;
        }
        return maximum + 1;
    }

    internal FixComponentDefinition CloneWireComponent(FixComponentDefinition source, Dictionary<int, int> componentIds, Dictionary<int, int> groupIds)
    {
        FixComponentDefinition clone = DictionaryComposition.CloneComponentMetadata(source, source.Source);
        clone.Id = componentIds[source.Id];
        CloneWireMembers(source.Members, clone.Members, componentIds, groupIds);
        return clone;
    }

    internal FixGroupDefinition CloneWireGroup(FixGroupDefinition source, Dictionary<int, int> componentIds, Dictionary<int, int> groupIds)
    {
        FixGroupDefinition clone = DictionaryComposition.CloneGroupMetadata(source, source.Source);
        clone.Id = groupIds[source.Id];
        CloneWireMembers(source.Members, clone.Members, componentIds, groupIds);
        return clone;
    }

    internal FixMessageDefinition CloneWireMessage(FixMessageDefinition source, Dictionary<int, int> componentIds, Dictionary<int, int> groupIds)
    {
        FixMessageDefinition clone = DictionaryComposition.CloneMessageMetadata(source, source.Source);
        CloneWireMembers(source.Members, clone.Members, componentIds, groupIds);
        return clone;
    }

    internal FixScenarioDefinition CloneWireScenario(FixScenarioDefinition source, Dictionary<int, int> componentIds, Dictionary<int, int> groupIds)
    {
        FixScenarioDefinition clone = DictionaryComposition.CloneScenarioMetadata(source, source.Source);
        CloneWireMembers(source.Members, clone.Members, componentIds, groupIds);
        DictionaryComposition.CloneRuleList(source.Rules, clone.Rules);
        return clone;
    }

    internal void CloneWireMembers(List<FixMemberDefinition> source, List<FixMemberDefinition> destination, Dictionary<int, int> componentIds, Dictionary<int, int> groupIds)
    {
        for (int index = 0; index < source.Count; index++)
        {
            if (source[index].IsRemoved) continue;
            FixMemberDefinition clone = DictionaryComposition.CloneMember(source[index], source[index].Source);
            if (clone.Kind == FixMemberKind.Component && componentIds.ContainsKey(clone.ReferenceId)) clone.ReferenceId = componentIds[clone.ReferenceId];
            if (clone.Kind == FixMemberKind.Group && groupIds.ContainsKey(clone.ReferenceId)) clone.ReferenceId = groupIds[clone.ReferenceId];
            destination.Add(clone);
        }
    }

    internal SessionDictionaryResolution ResolveMessageDictionary(FixMessage message, bool administrative)
    {
        SessionDictionaryResolution result = new SessionDictionaryResolution();
        if (administrative)
        {
            result.Success = true;
            result.Identity = LogonProcessor.CopyIdentity(TransportIdentity);
            result.Dictionary = TransportDictionary;
            return result;
        }
        if (Settings.Id.BeginString != FixBeginStrings.Fixt11)
        {
            result.Success = true;
            result.Identity = LogonProcessor.CopyIdentity(ApplicationIdentity);
            result.Dictionary = ApplicationDictionary;
            return result;
        }

        DictionarySelectionResult selection = DictionaryVersionSelector.SelectDictionaryIdentities(message, TransportIdentity, ApplicationIdentity, Negotiation.DefaultApplicationVersion);
        if (!selection.Success)
        {
            result.Code = "application-dictionary-selection-failed";
            result.Text = "The FIXT application dictionary could not be selected for the message.";
            if (selection.Diagnostics.Count != 0)
            {
                result.Code = selection.Diagnostics[0].Code;
                result.Text = selection.Diagnostics[0].Message;
            }
            return result;
        }
        result.Identity = selection.ApplicationIdentity;
        if (ApplicationIdentitiesMatch(selection.ApplicationIdentity, ApplicationIdentity))
        {
            result.Success = true;
            result.Dictionary = ApplicationDictionary;
            return result;
        }

        DictionaryLoadResult loaded = DictionaryProvider.ResolveDictionary(selection.ApplicationIdentity);
        if (loaded == null || !loaded.Success || loaded.Dictionary == null)
        {
            result.Code = "application-dictionary-not-found";
            result.Text = "The selected FIXT application dictionary could not be resolved.";
            if (loaded != null && loaded.Diagnostics.Count != 0)
            {
                result.Code = loaded.Diagnostics[0].Code;
                result.Text = loaded.Diagnostics[0].Message;
            }
            return result;
        }
        result.Success = true;
        result.Dictionary = loaded.Dictionary;
        return result;
    }

    internal static bool ApplicationIdentitiesMatch(FixDictionaryIdentity left, FixDictionaryIdentity right)
    {
        return left.BeginString == right.BeginString && left.ApplicationVersion == right.ApplicationVersion && left.HasExtensionPack == right.HasExtensionPack && left.ExtensionPack == right.ExtensionPack && left.CustomVersion == right.CustomVersion && left.Scenario == right.Scenario;
    }

    internal void ProcessInboundLogon(FixMessage message, int sequenceNumber, SessionPollResult result)
    {
        if (StateMachine.State != SessionState.LogonPending)
        {
            FaultSession(result, "unexpected-logon", "Logon is not valid in the current session state.");
            return;
        }
        LogonProcessingResult processed = LogonProcessor.ProcessInboundLogon(message, Settings.Id.Qualifier, "", Negotiation);
        if (!processed.Accepted)
        {
            AddEvent(result, SessionEventKind.Error, message, sequenceNumber, processed.Code, processed.Text);
            if (processed.SendLogout)
            {
                int responseSequence = NextAvailableSenderSequence();
                FixMessage logout = AdminFactory.CreateLogoutMessage(responseSequence, processed.Text);
                SessionQueueResult queued = QueuePreparedMessage(logout, true, false, true);
                if (!queued.Accepted)
                {
                    FaultSession(result, queued.Code, queued.Text);
                    return;
                }
                if (processed.Disconnect)
                {
                    OutboundMessages[OutboundMessages.Count - 1].DisconnectAfterSend = true;
                    HasLogonDeadline = false;
                }
            }
            else if (processed.Disconnect)
            {
                CloseTransport(result, processed.Code, processed.Text, false);
            }
            return;
        }
        bool configuredLogonReset = (Settings.ResetMode & SessionResetMode.OnLogon) == SessionResetMode.OnLogon;
        if (!LogonResetApplied && (processed.ResetSequenceNumbers || configuredLogonReset))
        {
            bool resetAccepted = configuredLogonReset ? ApplyConfiguredSequenceReset(SessionResetMode.OnLogon, result, "logon-sequence-reset", "The sequence state was reset while accepting Logon.") : ResetSequenceState(result, "peer-logon-sequence-reset", "The sequence state was reset at the peer's Logon request.");
            if (!resetAccepted)
            {
                FaultSession(result, "logon-sequence-reset-failed", "Logon sequence reset could not be persisted.");
                return;
            }
            LogonResetApplied = true;
        }
        Negotiation.ResetSequenceNumbers = processed.ResetSequenceNumbers || configuredLogonReset;
        SessionSequenceActionResult sequence = Sequences.ProcessInboundSequence(message, sequenceNumber, false, false);
        if (!HandleSequenceResult(message, sequence, result)) return;
        ApplicationDecision administrative = Application.OnAdministrativeMessage(CreateApplicationContext(message, sequenceNumber, true, false));
        if (!IsAcceptedDecision(administrative))
        {
            AddDecisionFailure(result, administrative, "logon-administrative-rejected", "The application rejected the Logon administrative callback.");
            if (administrative != null && administrative.Disposition == MessageDisposition.Disconnect) CloseTransport(result, administrative.Code, administrative.Text, false);
            return;
        }
        if (processed.ApplicationDictionary != null)
        {
            ApplicationDictionary = processed.ApplicationDictionary;
            ApplicationIdentity = LogonProcessor.CopyIdentity(processed.ApplicationIdentity);
            ReplayBuilder.Dictionary = ApplicationDictionary;
        }
        Negotiation.IsLoggedOn = true;
        Negotiation.HeartbeatIntervalSeconds = processed.HeartbeatIntervalSeconds;
        Negotiation.DefaultApplicationVersion = processed.DefaultApplicationVersion;
        Negotiation.ApplicationDictionary = ApplicationDictionary;
        if (Settings.Role == SessionRole.Acceptor)
        {
            int responseSequence = NextAvailableSenderSequence();
            FixMessage response = LogonProcessor.CreateLogonMessage(responseSequence, Sequences.NextTargetSeqNum, Negotiation);
            SessionQueueResult queued = QueuePreparedMessage(response, true, false, true);
            if (!queued.Accepted)
            {
                FaultSession(result, queued.Code, queued.Text);
                return;
            }
        }
        SessionStateTransitionResult active = StateMachine.CompleteLogon();
        if (!active.Accepted)
        {
            FaultSession(result, active.Code, active.Text);
            return;
        }
        HasLogonDeadline = false;
        Reconnects.MarkConnected();
        Timers.StartTimers(processed.HeartbeatIntervalSeconds);
        ApplicationDecision logon = Application.OnSessionLogon(CreateApplicationContext(message, sequenceNumber, true, false));
        if (!IsAcceptedDecision(logon))
        {
            AddDecisionFailure(result, logon, "session-logon-rejected", "The application rejected session Logon.");
            CloseTransport(result, "session-logon-rejected", "The application rejected session Logon.", false);
            return;
        }
        AddEvent(result, SessionEventKind.MessageReceived, message, sequenceNumber, "logon-message-received", "The peer Logon was received.");
        AddTransitionEvent(result, active);
    }

    internal void ProcessInboundLogout(FixMessage message, int sequenceNumber, SessionPollResult result)
    {
        SessionHeaderValidationResult header = HeaderValidator.ValidateInboundHeader(message, Settings.Id.Qualifier);
        if (!header.IsValid)
        {
            RejectInvalidInbound(message, sequenceNumber, header.Issues, result);
            return;
        }
        SessionSequenceActionResult sequence = Sequences.ProcessInboundSequence(message, sequenceNumber, header.IsPossibleDuplicate, false);
        if (!HandleSequenceResult(message, sequence, result)) return;
        int responseSequence = NextAvailableSenderSequence();
        LogoutActionResult action = LogoutProcessor.ProcessInboundLogout(message, sequenceNumber, responseSequence, Settings.Id.Qualifier, StateMachine, LogoutLifecycle);
        if (!action.Accepted)
        {
            AddEvent(result, SessionEventKind.Error, message, sequenceNumber, action.Code, action.Text);
            return;
        }
        if (action.SendLogout && action.OutboundMessage != null)
        {
            SessionQueueResult queued = QueuePreparedMessage(action.OutboundMessage, true, true, true);
            if (!queued.Accepted)
            {
                FaultSession(result, queued.Code, queued.Text);
                return;
            }
        }
        AddEvent(result, SessionEventKind.MessageReceived, message, sequenceNumber, "logout-message-received", "The peer Logout was received.");
        if (action.CloseTransport)
        {
            AddEvent(result, SessionEventKind.Logout, message, sequenceNumber, action.Code, action.Text);
            if (!ApplyConfiguredSequenceReset(SessionResetMode.OnLogout, result, "logout-sequence-reset", "The sequence state was reset after Logout completion."))
            {
                FaultSession(result, "logout-sequence-reset-failed", "The Logout sequence reset could not be persisted.");
                return;
            }
            ClosureResetApplied = (Settings.ResetMode & SessionResetMode.OnLogout) == SessionResetMode.OnLogout;
            CloseTransport(result, action.Code, action.Text, false);
        }
    }

    internal void ProcessEstablishedMessage(FixMessage message, int sequenceNumber, bool administrative, FixDictionary dictionary, FixDictionaryIdentity dictionaryIdentity, SessionPollResult result)
    {
        SessionHeaderValidationResult header = HeaderValidator.ValidateInboundHeader(message, Settings.Id.Qualifier);
        ValidationResult validation = ValidateMessageWithDictionary(message, dictionary, dictionaryIdentity);
        if (!header.IsValid || validation == null || !validation.IsValid)
        {
            List<ValidationIssue> issues = [];
            LogonProcessor.CopyIssues(issues, header.Issues);
            if (validation != null) LogonProcessor.CopyIssues(issues, validation.Issues);
            RejectInvalidInbound(message, sequenceNumber, issues, result);
            return;
        }
        if (message.MsgType == FixAdminMessageTypes.SequenceReset)
        {
            AdminMessageProcessingResult reset = AdminProcessor.ProcessInboundAdministrative(message, sequenceNumber, NextAvailableSenderSequence(), header.IsPossibleDuplicate);
            HandleAdministrativeResult(message, reset, result);
            return;
        }
        SessionSequenceActionResult sequence = Sequences.ProcessInboundSequence(message, sequenceNumber, header.IsPossibleDuplicate, false);
        if (!HandleSequenceResult(message, sequence, result)) return;
        if (administrative)
        {
            AdminMessageProcessingResult processed = AdminProcessor.ProcessInboundAdministrative(message, sequenceNumber, NextAvailableSenderSequence(), header.IsPossibleDuplicate);
            HandleAdministrativeResult(message, processed, result);
            ProgressOrdinarySequenceRecovery(result);
            return;
        }
        Timers.RecordInboundActivity();
        ApplicationMessageContext context = CreateApplicationContext(message, sequenceNumber, false, header.IsPossibleDuplicate);
        ApplicationDecision decision = Application.OnApplicationMessage(context);
        if (decision == null)
        {
            CloseTransport(result, "missing-application-decision", "The application did not return a message decision.", false);
            return;
        }
        if (decision.Disposition == MessageDisposition.Disconnect)
        {
            AddDecisionFailure(result, decision, "application-disconnect", "The application requested disconnection.");
            CloseTransport(result, decision.Code, decision.Text, false);
            return;
        }
        if (decision.Disposition == MessageDisposition.Reject)
        {
            int responseSequence = NextAvailableSenderSequence();
            FixMessage reject = AdminFactory.CreateBusinessRejectMessage(responseSequence, sequenceNumber, message.MsgType, "", 0, decision.Text);
            SessionQueueResult queued = QueuePreparedMessage(reject, true, false, true);
            AddEvent(result, SessionEventKind.Error, message, sequenceNumber, decision.Code.Length == 0 ? "application-message-rejected" : decision.Code, decision.Text);
            if (!queued.Accepted)
            {
                FaultSession(result, queued.Code, queued.Text);
                return;
            }
            ProgressOrdinarySequenceRecovery(result);
            return;
        }
        if (decision.Disposition == MessageDisposition.Drop)
        {
            string droppedCode = decision.Code.Length == 0 ? context.IsPossibleResend ? "possible-resend-ignored" : "application-message-dropped" : decision.Code;
            AddEvent(result, SessionEventKind.MessageReceived, message, sequenceNumber, droppedCode, decision.Text);
            ProgressOrdinarySequenceRecovery(result);
            return;
        }
        string code = decision.Code.Length == 0 ? "application-message-received" : decision.Code;
        AddEvent(result, SessionEventKind.MessageReceived, message, sequenceNumber, code, decision.Text);
        ProgressOrdinarySequenceRecovery(result);
    }

    internal void HandleAdministrativeResult(FixMessage message, AdminMessageProcessingResult processed, SessionPollResult result)
    {
        if (!processed.Accepted)
        {
            AddEvent(result, SessionEventKind.Error, message, processed.SequenceNumber, processed.Code, processed.Text);
            if (processed.Disconnect) CloseTransport(result, processed.Code, processed.Text, false);
            return;
        }
        if (Timers.Running) Timers.RecordInboundActivity();
        if (processed.OutboundMessage != null)
        {
            SessionQueueResult queued = QueuePreparedMessage(processed.OutboundMessage, true, false, true);
            if (!queued.Accepted)
            {
                FaultSession(result, queued.Code, queued.Text);
                return;
            }
        }
        if (processed.ReplayResult != null)
        {
            for (int index = 0; index < processed.ReplayResult.Messages.Count; index++) QueueReplayMessage(processed.ReplayResult.Messages[index]);
        }
        if (processed.SequenceResetResult != null && processed.SequenceResetResult.RecoveryCompleted && StateMachine.State == SessionState.Recovering)
        {
            AddTransitionEvent(result, StateMachine.CompleteRecovery());
        }
        if (processed.SequenceResetResult != null)
        {
            SequenceResetProcessingResult reset = processed.SequenceResetResult;
            if (reset.Queued && StateMachine.State == SessionState.Active) AddTransitionEvent(result, StateMachine.BeginRecovery());
            if (reset.ResendAction.GenerateResendRequest)
            {
                int senderSequence = NextAvailableSenderSequence();
                FixMessage request = AdminFactory.CreateResendRequestMessage(senderSequence, reset.ResendAction.BeginSeqNo, reset.ResendAction.EndSeqNo);
                SessionQueueResult queued = QueuePreparedMessage(request, true, false, true);
                if (!queued.Accepted)
                {
                    FaultSession(result, queued.Code, queued.Text);
                    return;
                }
            }
            for (int index = 0; index < reset.ReleasedMessages.Count; index++) ProcessReleasedMessage(reset.ReleasedMessages[index], result);
            if (reset.PendingSequenceReset != null && StateMachine.State != SessionState.Disconnected && StateMachine.State != SessionState.Faulted)
            {
                ProcessReleasedMessage(reset.PendingSequenceReset, result);
            }
        }
        SessionEventKind kind = message.MsgType == FixAdminMessageTypes.Heartbeat ? SessionEventKind.Heartbeat : message.MsgType == FixAdminMessageTypes.TestRequest ? SessionEventKind.TestRequest : SessionEventKind.MessageReceived;
        AddEvent(result, kind, message, processed.SequenceNumber, processed.Code, processed.Text);
    }

    internal void ProcessReleasedMessage(QueuedSessionMessage released, SessionPollResult result)
    {
        if (released.Rejected)
        {
            AddEvent(result, SessionEventKind.Error, released.Message, released.SequenceNumber, "released-rejected-message", "The queued rejected message was released after sequence recovery.");
            return;
        }
        bool administrative = IsAdministrativeType(released.Message.MsgType);
        if (released.Message.MsgType == FixAdminMessageTypes.Logout)
        {
            int responseSequence = NextAvailableSenderSequence();
            LogoutActionResult logout = LogoutProcessor.ProcessInboundLogout(released.Message, released.SequenceNumber, responseSequence, Settings.Id.Qualifier, StateMachine, LogoutLifecycle);
            if (!logout.Accepted)
            {
                AddEvent(result, SessionEventKind.Error, released.Message, released.SequenceNumber, logout.Code, logout.Text);
                return;
            }
            if (logout.SendLogout && logout.OutboundMessage != null)
            {
                SessionQueueResult queued = QueuePreparedMessage(logout.OutboundMessage, true, true, true);
                if (!queued.Accepted) FaultSession(result, queued.Code, queued.Text);
            }
            return;
        }
        if (administrative)
        {
            AdminMessageProcessingResult admin = AdminProcessor.ProcessInboundAdministrative(released.Message, released.SequenceNumber, NextAvailableSenderSequence(), released.PossibleDuplicate);
            HandleAdministrativeResult(released.Message, admin, result);
            return;
        }
        if (Timers.Running) Timers.RecordInboundActivity();
        ApplicationDecision decision = Application.OnApplicationMessage(CreateApplicationContext(released.Message, released.SequenceNumber, false, released.PossibleDuplicate));
        if (decision == null || decision.Disposition == MessageDisposition.Disconnect)
        {
            string code = decision == null || decision.Code.Length == 0 ? "released-message-disconnect" : decision.Code;
            string text = decision == null || decision.Text.Length == 0 ? "The released message could not be accepted by the application." : decision.Text;
            CloseTransport(result, code, text, false);
            return;
        }
        AddEvent(result, SessionEventKind.MessageReceived, released.Message, released.SequenceNumber, decision.Code.Length == 0 ? "queued-message-released" : decision.Code, decision.Text);
    }

    internal bool HandleSequenceResult(FixMessage message, SessionSequenceActionResult sequence, SessionPollResult result)
    {
        if (sequence.Queued)
        {
            ResendRequestActionResult gap = Gaps.ObserveGap(sequence.ExpectedSequenceNumber, sequence.SequenceNumber);
            if (StateMachine.State == SessionState.Active) AddTransitionEvent(result, StateMachine.BeginRecovery());
            AddEvent(result, SessionEventKind.SequenceGap, message, sequence.SequenceNumber, sequence.Code, sequence.Text);
            if (gap.GenerateResendRequest)
            {
                int senderSequence = NextAvailableSenderSequence();
                FixMessage request = AdminFactory.CreateResendRequestMessage(senderSequence, gap.BeginSeqNo, gap.EndSeqNo);
                SessionQueueResult queued = QueuePreparedMessage(request, true, false, true);
                if (!queued.Accepted) FaultSession(result, queued.Code, queued.Text);
            }
            return false;
        }
        if (!sequence.Accepted)
        {
            AddEvent(result, SessionEventKind.Error, message, sequence.SequenceNumber, sequence.Code, sequence.Text);
            if (sequence.Disconnect) CloseTransport(result, sequence.Code, sequence.Text, false);
            return false;
        }
        if (sequence.Duplicate)
        {
            AddEvent(result, SessionEventKind.MessageReceived, message, sequence.SequenceNumber, sequence.Code, sequence.Text);
            return false;
        }
        return true;
    }

    internal void RejectInvalidInbound(FixMessage message, int sequenceNumber, List<ValidationIssue> issues, SessionPollResult result)
    {
        string code = issues.Count == 0 ? "inbound-validation-failed" : issues[0].Code;
        string text = issues.Count == 0 ? "The inbound message failed validation." : issues[0].Message;
        SessionSequenceActionResult sequence = Sequences.ProcessInboundSequence(message, sequenceNumber, false, true);
        AddEvent(result, SessionEventKind.Error, message, sequenceNumber, code, text);
        if (sequence.Queued || sequence.Disconnect)
        {
            HandleSequenceResult(message, sequence, result);
            return;
        }
        int referenceTag = issues.Count != 0 && issues[0].HasRefTagId ? issues[0].RefTagId : 0;
        int reason = issues.Count != 0 && issues[0].HasSessionRejectReason ? issues[0].SessionRejectReason : 0;
        int senderSequence = NextAvailableSenderSequence();
        FixMessage reject = AdminFactory.CreateRejectMessage(senderSequence, sequenceNumber, referenceTag, message.MsgType, reason, text);
        SessionQueueResult queued = QueuePreparedMessage(reject, true, false, true);
        if (!queued.Accepted)
        {
            FaultSession(result, queued.Code, queued.Text);
            return;
        }
        ProgressOrdinarySequenceRecovery(result);
    }

    internal void ProgressOrdinarySequenceRecovery(SessionPollResult result)
    {
        if (StateMachine.State != SessionState.Recovering)
        {
            return;
        }
        SequenceResetProcessingResult release = new SequenceResetProcessingResult();
        ResetProcessor.ReleaseContiguousMessages(release);
        if (release.PersistenceFailed || release.Disconnect)
        {
            string code = release.Code.Length == 0 ? "recovery-queue-release-failed" : release.Code;
            string text = release.Text.Length == 0 ? "A queued recovery message could not consume its sequence number." : release.Text;
            CloseTransport(result, code, text, false);
            return;
        }
        for (int index = 0; index < release.ReleasedMessages.Count; index++)
        {
            ProcessReleasedMessage(release.ReleasedMessages[index], result);
            if (StateMachine.State == SessionState.Disconnected || StateMachine.State == SessionState.Faulted)
            {
                return;
            }
        }
        if (release.PendingSequenceReset != null)
        {
            ProcessReleasedMessage(release.PendingSequenceReset, result);
            return;
        }
        if (!Gaps.GapActive)
        {
            AddTransitionEvent(result, StateMachine.CompleteRecovery());
            return;
        }
        ResendRequestActionResult progress = Gaps.AdvanceGap(Sequences.NextTargetSeqNum);
        if (!progress.Accepted)
        {
            FaultSession(result, progress.Code, progress.Text);
            return;
        }
        if (progress.GenerateResendRequest)
        {
            int senderSequence = NextAvailableSenderSequence();
            FixMessage request = AdminFactory.CreateResendRequestMessage(senderSequence, progress.BeginSeqNo, progress.EndSeqNo);
            SessionQueueResult queued = QueuePreparedMessage(request, true, false, true);
            if (!queued.Accepted)
            {
                FaultSession(result, queued.Code, queued.Text);
                return;
            }
        }
        if (!progress.GapActive && StateMachine.State == SessionState.Recovering)
        {
            AddTransitionEvent(result, StateMachine.CompleteRecovery());
        }
    }

    internal bool HandleScheduleEvaluation(SessionScheduleEvaluation schedule, SessionPollResult result)
    {
        if (schedule.BoundaryEnded)
        {
            AddEvent(result, SessionEventKind.None, null, 0, "session-window-ended", "The configured session window ended.");
            if (!ApplyConfiguredSequenceReset(SessionResetMode.OnSessionBoundary, result, "boundary-sequence-reset", "The sequence state was reset at the session boundary."))
            {
                FaultSession(result, "boundary-sequence-reset-failed", "The session-boundary sequence reset could not be persisted.");
                return true;
            }
            ClosureResetApplied = (Settings.ResetMode & SessionResetMode.OnSessionBoundary) == SessionResetMode.OnSessionBoundary;
            if (StateMachine.State != SessionState.Disconnected)
            {
                CloseTransport(result, "session-window-ended", "The configured session window ended.", false);
            }
            return true;
        }
        if (!schedule.Active)
        {
            return true;
        }
        if (schedule.BoundaryStarted)
        {
            AddEvent(result, SessionEventKind.None, null, 0, "session-window-started", "The configured session window started.");
            if (!ApplyConfiguredSequenceReset(SessionResetMode.OnSessionBoundary, result, "boundary-sequence-reset", "The sequence state was reset at the session boundary."))
            {
                FaultSession(result, "boundary-sequence-reset-failed", "The session-boundary sequence reset could not be persisted.");
                return true;
            }
            if (StateMachine.State == SessionState.Disconnected)
            {
                Reconnects.ResetPlan();
                SessionStateTransitionResult connecting = StateMachine.BeginConnection();
                if (!connecting.Accepted)
                {
                    FaultSession(result, connecting.Code, connecting.Text);
                    return true;
                }
                BeginConnectDeadline();
                OpenTransport(result);
            }
            return true;
        }
        return false;
    }

    internal bool ApplyConfiguredSequenceReset(SessionResetMode trigger, SessionPollResult result, string code, string text)
    {
        if ((Settings.ResetMode & trigger) != trigger)
        {
            return true;
        }
        return ResetSequenceState(result, code, text);
    }

    internal bool ResetSequenceState(SessionPollResult result, string code, string text)
    {
        StoreRangeResult reset = Store.ResetStore();
        if (reset == null || !reset.Success)
        {
            string failureCode = reset == null || reset.Code.Length == 0 ? "sequence-reset-store-failed" : reset.Code;
            string failureText = reset == null || reset.Text.Length == 0 ? "The durable sequence store could not be reset." : reset.Text;
            AddEvent(result, SessionEventKind.Error, null, 0, failureCode, failureText);
            return false;
        }
        SessionSequenceActionResult refreshed = Sequences.RefreshSequences();
        if (!refreshed.Accepted)
        {
            AddEvent(result, SessionEventKind.Error, null, 0, refreshed.Code, refreshed.Text);
            return false;
        }
        DiscardPreparedOutboundMessages();
        Gaps.ResetGapTracking();
        Negotiation.NextSenderSeqNum = FixDefaults.InitialSequenceNumber;
        Negotiation.NextTargetSeqNum = FixDefaults.InitialSequenceNumber;
        AddEvent(result, SessionEventKind.None, null, 0, code, text);
        return true;
    }

    internal void DiscardPreparedOutboundMessages()
    {
        int index = OutboundMessages.Count - 1;
        while (index >= 0)
        {
            if (OutboundMessages[index].IsPrepared) OutboundMessages.RemoveAt(index);
            index--;
        }
    }

    internal void PollTimers(SessionPollResult result)
    {
        if (StateMachine.State != SessionState.Active && StateMachine.State != SessionState.Recovering)
        {
            return;
        }
        SessionTimerActionResult timer = Timers.PollTimers();
        if (timer.Disconnect)
        {
            AddEvent(result, SessionEventKind.Error, null, 0, timer.Code, timer.Text);
            CloseTransport(result, timer.Code, timer.Text, false);
            return;
        }
        if (timer.SendHeartbeat || timer.SendTestRequest)
        {
            int sequenceNumber = NextAvailableSenderSequence();
            FixMessage message = timer.SendHeartbeat ? AdminFactory.CreateHeartbeatMessage(sequenceNumber, timer.TestRequestId) : AdminFactory.CreateTestRequestMessage(sequenceNumber, timer.TestRequestId);
            SessionQueueResult queued = QueuePreparedMessage(message, true, false, true);
            if (!queued.Accepted)
            {
                FaultSession(result, queued.Code, queued.Text);
                return;
            }
            AddEvent(result, timer.EventKind, message, sequenceNumber, timer.Code, timer.Text);
        }
    }

    internal SessionQueueResult QueuePreparedMessage(FixMessage message, bool administrative, bool confirmLogoutResponse, bool consumeSequence)
    {
        SessionQueueResult result = new SessionQueueResult();
        int sequenceNumber = ReadPositiveHeaderInteger(message, FixTags.MsgSeqNum);
        if (sequenceNumber < FixDefaults.InitialSequenceNumber)
        {
            result.Code = "missing-outbound-sequence-number";
            result.Text = "The outbound message does not contain a positive MsgSeqNum.";
            return result;
        }
        SessionDictionaryResolution dictionaryResolution = ResolveMessageDictionary(message, administrative);
        if (!dictionaryResolution.Success)
        {
            result.Code = dictionaryResolution.Code;
            result.Text = dictionaryResolution.Text;
            return result;
        }
        FixDictionary dictionary = dictionaryResolution.Dictionary;
        if (!administrative && Settings.Id.BeginString == FixBeginStrings.Fixt11 && dictionary != TransportDictionary)
        {
            dictionary = CreateWireApplicationDictionary(dictionary);
        }
        byte[] encoded = EncodeCompleteMessage(message, dictionary);
        if (encoded.Length == 0)
        {
            result.Code = LastEncodingCode.Length == 0 ? "outbound-encode-failed" : LastEncodingCode;
            result.Text = LastEncodingText.Length == 0 ? "The outbound message could not be encoded." : LastEncodingText;
            return result;
        }
        DecodeResult decoded = Decoder.DecodeMessage(encoded, dictionary, DecodeOptions);
        if (decoded == null || decoded.Status != DecodeStatus.Success || decoded.Message == null)
        {
            result.Code = decoded == null || decoded.ErrorCode.Length == 0 ? "outbound-decode-failed" : decoded.ErrorCode;
            result.Text = decoded == null || decoded.ErrorMessage.Length == 0 ? "The encoded outbound message could not be decoded for validation." : decoded.ErrorMessage;
            return result;
        }
        ValidationResult validation = ValidateMessageWithDictionary(decoded.Message, dictionary, dictionaryResolution.Identity);
        if (validation == null || !validation.IsValid)
        {
            result.Code = validation == null || validation.Issues.Count == 0 ? "outbound-validation-failed" : validation.Issues[0].Code;
            result.Text = validation == null || validation.Issues.Count == 0 ? "The outbound message failed validation." : validation.Issues[0].Message;
            return result;
        }
        if (consumeSequence)
        {
            StoredMessage stored = new StoredMessage();
            stored.SequenceNumber = sequenceNumber;
            stored.SendingTimeUtc = ReadUtcNow();
            stored.Bytes = CopyBytes(encoded);
            stored.MsgType = message.MsgType;
            stored.IsAdministrative = administrative;
            StoreRangeResult persisted = Store.StoreMessage(stored);
            if (persisted == null || !persisted.Success)
            {
                result.Disconnect = true;
                result.Code = persisted == null || persisted.Code.Length == 0 ? "outbound-store-failed" : persisted.Code;
                result.Text = persisted == null || persisted.Text.Length == 0 ? "The outbound message could not be stored before send." : persisted.Text;
                return result;
            }
        }
        SessionOutboundMessage outbound = new SessionOutboundMessage();
        outbound.Message = decoded.Message;
        outbound.Bytes = encoded;
        outbound.SequenceNumber = sequenceNumber;
        outbound.ConsumeSequence = consumeSequence;
        outbound.IsAdministrative = administrative;
        outbound.IsPrepared = true;
        outbound.ConfirmLogoutResponse = confirmLogoutResponse;
        OutboundMessages.Add(outbound);
        result.Accepted = true;
        result.SequenceNumber = sequenceNumber;
        result.Code = "message-queued";
        result.Text = "The message was validated, stored, and queued for transport.";
        return result;
    }

    internal void QueueReplayMessage(ReplayMessageResult replay)
    {
        if (!replay.Success || replay.Message == null || replay.Bytes.Length == 0)
        {
            return;
        }
        SessionOutboundMessage outbound = new SessionOutboundMessage();
        outbound.Message = replay.Message;
        outbound.Bytes = CopyBytes(replay.Bytes);
        outbound.SequenceNumber = replay.OriginalSequenceNumber;
        outbound.IsAdministrative = replay.IsGapFill;
        outbound.IsPrepared = true;
        OutboundMessages.Add(outbound);
    }

    internal byte[] EncodeCompleteMessage(FixMessage message, FixDictionary dictionary)
    {
        LastEncodingCode = "";
        LastEncodingText = "";
        byte[] destination = new byte[EncodeOptions.MaxMessageSize];
        EncodeResult encoded = Encoder.EncodeMessage(message, dictionary, destination, EncodeOptions);
        if (encoded == null || encoded.Status != EncodeStatus.Success || encoded.BytesWritten <= 0)
        {
            LastEncodingCode = encoded == null || encoded.ErrorCode.Length == 0 ? "outbound-encode-failed" : encoded.ErrorCode;
            LastEncodingText = encoded == null || encoded.ErrorMessage.Length == 0 ? "The outbound message could not be encoded." : encoded.ErrorMessage;
            return [];
        }
        byte[] complete = new byte[encoded.BytesWritten];
        Array.Copy(destination, complete, encoded.BytesWritten);
        return complete;
    }

    internal FixMessage PrepareMessageHeader(FixMessage message, int sequenceNumber)
    {
        if (message.MsgType == null || message.MsgType.Length == 0)
        {
            return message;
        }
        SetTextField(message, FixTags.BeginString, Settings.Id.BeginString);
        SetTextField(message, FixTags.MsgType, message.MsgType);
        SetTextField(message, FixTags.MsgSeqNum, sequenceNumber.ToString(System.Globalization.CultureInfo.InvariantCulture));
        SetTextField(message, FixTags.SenderCompID, Settings.Id.SenderCompID);
        SetOptionalTextField(message, FixTags.SenderSubID, Settings.Id.SenderSubID);
        SetOptionalTextField(message, FixTags.SenderLocationID, Settings.Id.SenderLocationID);
        SetTextField(message, FixTags.TargetCompID, Settings.Id.TargetCompID);
        SetOptionalTextField(message, FixTags.TargetSubID, Settings.Id.TargetSubID);
        SetOptionalTextField(message, FixTags.TargetLocationID, Settings.Id.TargetLocationID);
        SetTextField(message, FixTags.SendingTime, LogonProcessor.FormatSendingTime(ReadUtcNow()));
        message.BeginString = Settings.Id.BeginString;
        return message;
    }

    internal void SetTextField(FixMessage message, int tag, string value)
    {
        FixField field = new FixField();
        field.Tag = tag;
        field.Value = Encoding.Latin1.GetBytes(value == null ? "" : value);
        MessageOperations.SetField(message, field);
    }

    internal void SetOptionalTextField(FixMessage message, int tag, string value)
    {
        if (value != null && value.Length != 0) SetTextField(message, tag, value);
    }

    internal ValidationResult ValidateMessageWithDictionary(FixMessage message, FixDictionary dictionary, FixDictionaryIdentity identity)
    {
        ValidationContext context = new ValidationContext();
        context.DictionaryIdentity = LogonProcessor.CopyIdentity(identity);
        context.ExpectedBeginString = Settings.Id.BeginString;
        context.DefaultApplicationVersion = Negotiation.DefaultApplicationVersion;
        return Validator.ValidateMessage(message, dictionary, Settings.Validation, context);
    }

    internal ApplicationDecision CheckThrottle(FixMessage message, bool administrative)
    {
        ApplicationMessageContext context = CreateApplicationContext(message, 0, administrative, false);
        context.PendingQueueCount = CountPendingApplicationMessages();
        return Throttle.CheckMessageThrottle(context, Settings.Throttle);
    }

    internal ApplicationMessageContext CreateApplicationContext(FixMessage message, int sequenceNumber, bool administrative, bool possibleDuplicate)
    {
        ApplicationMessageContext context = new ApplicationMessageContext();
        context.Id = LogonProcessor.CopySessionId(Settings.Id);
        context.Message = message;
        context.SequenceNumber = sequenceNumber;
        context.IsAdministrative = administrative;
        context.IsPossibleDuplicate = possibleDuplicate;
        FixField? possibleResend = EnvelopeValidator.FindFirstMessageField(message, FixTags.PossResend);
        context.IsPossibleResend = possibleResend != null && possibleResend.Value.Length == 1 && possibleResend.Value[0] == 89;
        context.PendingQueueCount = CountPendingApplicationMessages();
        return context;
    }

    internal int CountPendingApplicationMessages()
    {
        int count = 0;
        for (int index = 0; index < OutboundMessages.Count; index++)
        {
            if (!OutboundMessages[index].IsAdministrative) count++;
        }
        return count;
    }

    internal int NextAvailableSenderSequence()
    {
        long sequenceNumber = Sequences.NextSenderSeqNum;
        for (int index = 0; index < OutboundMessages.Count; index++)
        {
            if (OutboundMessages[index].ConsumeSequence) sequenceNumber++;
        }
        if (sequenceNumber > int.MaxValue)
        {
            throw new FixSessionException("The outbound sender sequence number is exhausted.");
        }
        return (int)sequenceNumber;
    }

    internal bool IsAdministrativeType(string msgType)
    {
        return MessageCracking.IsAdministrativeMessageType(msgType);
    }

    internal int ReadPositiveHeaderInteger(FixMessage message, int tag)
    {
        FixField? field = EnvelopeValidator.FindFirstMessageField(message, tag);
        return LogonProcessor.ParsePositiveSequenceNumber(field);
    }

    internal FixMessage CloneMessage(FixMessage message)
    {
        MessageEditResult cloned = MessageOperations.CloneMessage(message);
        return cloned.Message == null ? new FixMessage() : cloned.Message;
    }

    internal FixDictionary ResolveRequiredDictionary(FixDictionaryIdentity identity, string role)
    {
        DictionaryLoadResult resolved = DictionaryProvider.ResolveDictionary(identity);
        if (resolved == null || !resolved.Success || resolved.Dictionary == null)
        {
            throw new FixSessionException("The configured " + role + " dictionary could not be resolved.");
        }
        return resolved.Dictionary;
    }

    internal SessionPollResult CompletePoll(SessionPollResult result)
    {
        result.State = StateMachine.State;
        result.WorkRemaining = OutboundMessages.Count != 0;
        result.Snapshot = CreateSessionSnapshot();
        return result;
    }

    internal void AddTransitionEvent(SessionPollResult result, SessionStateTransitionResult transition)
    {
        if (transition.Accepted && transition.EventKind != SessionEventKind.None)
        {
            AddEvent(result, transition.EventKind, null, 0, transition.Code, transition.Text);
        }
    }

    internal void AddEvent(SessionPollResult result, SessionEventKind kind, FixMessage? message, int sequenceNumber, string code, string text)
    {
        SessionEvent sessionEvent = new SessionEvent();
        sessionEvent.Id = LogonProcessor.CopySessionId(Settings.Id);
        sessionEvent.Kind = kind;
        sessionEvent.TimestampUtc = ReadUtcNow();
        sessionEvent.Message = message;
        sessionEvent.SequenceNumber = sequenceNumber;
        sessionEvent.Code = code == null ? "" : code;
        sessionEvent.Text = text == null ? "" : text;
        result.Events.Add(sessionEvent);
        LogEntry entry = new LogEntry();
        entry.TimestampUtc = sessionEvent.TimestampUtc;
        entry.Id = LogonProcessor.CopySessionId(Settings.Id);
        entry.Direction = LogDirection.Event;
        entry.EventKind = kind;
        entry.Text = sessionEvent.Code + ": " + sessionEvent.Text;
        Log.WriteLog(entry);
    }

    internal void WriteMessageLog(LogDirection direction, byte[] bytes, FixMessage message)
    {
        LogEntry entry = new LogEntry();
        entry.TimestampUtc = ReadUtcNow();
        entry.Id = LogonProcessor.CopySessionId(Settings.Id);
        entry.Direction = direction;
        entry.EventKind = direction == LogDirection.Incoming ? SessionEventKind.MessageReceived : SessionEventKind.MessageSent;
        entry.Bytes = CopyBytes(bytes);
        entry.Text = message.MsgType;
        Log.WriteLog(entry);
    }

    internal void AddDecisionFailure(SessionPollResult result, ApplicationDecision decision, string fallbackCode, string fallbackText)
    {
        string code = decision == null || decision.Code == null || decision.Code.Length == 0 ? fallbackCode : decision.Code;
        string text = decision == null || decision.Text == null || decision.Text.Length == 0 ? fallbackText : decision.Text;
        AddEvent(result, SessionEventKind.Error, null, 0, code, text);
    }

    internal bool IsAcceptedDecision(ApplicationDecision decision)
    {
        return decision != null && decision.Disposition == MessageDisposition.Accept;
    }

    internal SessionSendResult FromDecision(ApplicationDecision decision)
    {
        if (decision == null) return CreateSendFailure(MessageDisposition.Disconnect, "missing-throttle-decision", "The message throttle did not return a decision.");
        return CreateSendFailure(decision.Disposition, decision.Code, decision.Text);
    }

    internal SessionSendResult FromQueueResult(SessionQueueResult queued)
    {
        if (!queued.Accepted) return CreateSendFailure(queued.Disconnect ? MessageDisposition.Disconnect : MessageDisposition.Reject, queued.Code, queued.Text);
        SessionSendResult result = CreateSendSuccess(queued.SequenceNumber, queued.Code, queued.Text);
        result.QueueCount = OutboundMessages.Count;
        return result;
    }

    internal SessionSendResult CreateSendSuccess(int sequenceNumber, string code, string text)
    {
        SessionSendResult result = new SessionSendResult();
        result.Accepted = true;
        result.Disposition = MessageDisposition.Accept;
        result.SequenceNumber = sequenceNumber;
        result.QueueCount = OutboundMessages.Count;
        result.Code = code;
        result.Text = text;
        return result;
    }

    internal SessionSendResult CreateSendFailure(MessageDisposition disposition, string code, string text)
    {
        SessionSendResult result = new SessionSendResult();
        result.Disposition = disposition;
        result.QueueCount = OutboundMessages.Count;
        result.Code = code == null ? "" : code;
        result.Text = text == null ? "" : text;
        return result;
    }

    internal void BeginConnectDeadline()
    {
        ConnectDeadlineUtc = CreateSessionDeadline(ReadUtcNow(), Settings.ConnectTimeoutMilliseconds, "connection");
        HasConnectDeadline = true;
        HasLogonDeadline = false;
    }

    internal void BeginLogonDeadline()
    {
        LogonDeadlineUtc = CreateSessionDeadline(ReadUtcNow(), Settings.LogonTimeoutMilliseconds, "Logon");
        HasLogonDeadline = true;
    }

    internal bool CheckConnectDeadline(SessionPollResult result)
    {
        if (!HasConnectDeadline || ReadUtcNow() < ConnectDeadlineUtc)
        {
            return false;
        }
        ScheduleTransportRetry(result, "connect-timeout", "The transport connection deadline elapsed.");
        return true;
    }

    internal bool CheckLogonDeadline(SessionPollResult result)
    {
        if (!HasLogonDeadline || ReadUtcNow() < LogonDeadlineUtc)
        {
            return false;
        }
        ScheduleTransportRetry(result, "logon-timeout", "The FIX Logon deadline elapsed.");
        return true;
    }

    internal bool CheckLogoutDeadline(SessionPollResult result)
    {
        if (!LogoutLifecycle.HasDeadline)
        {
            return false;
        }
        LogoutActionResult timeout = LogoutProcessor.CheckLogoutTimeout(StateMachine, LogoutLifecycle);
        if (!timeout.TimedOut)
        {
            return false;
        }
        AddEvent(result, SessionEventKind.Logout, null, 0, timeout.Code, timeout.Text);
        if (!ApplyConfiguredSequenceReset(SessionResetMode.OnLogout, result, "logout-sequence-reset", "The sequence state was reset after Logout timeout."))
        {
            FaultSession(result, "logout-sequence-reset-failed", "The Logout sequence reset could not be persisted.");
            return true;
        }
        ClosureResetApplied = (Settings.ResetMode & SessionResetMode.OnLogout) == SessionResetMode.OnLogout;
        CloseTransport(result, timeout.Code, timeout.Text, false);
        return true;
    }

    internal void ScheduleTransportRetry(SessionPollResult result, string code, string text)
    {
        TransportWriteResult closed = Transport.CloseTransport();
        CurrentTransportState = closed == null ? TransportState.Closed : closed.State;
        HasConnectDeadline = false;
        HasLogonDeadline = false;
        Timers.StopTimers();
        Negotiation.IsLoggedOn = false;
        LogoutLifecycle = LogoutProcessor.CreateLogoutState();
        InboundBytes.Clear();
        DiscardPreparedOutboundMessages();
        SessionStateTransitionResult disconnected = StateMachine.DisconnectTransport();
        if (!disconnected.Accepted)
        {
            FaultSession(result, disconnected.Code, disconnected.Text);
            return;
        }
        AddEvent(result, SessionEventKind.Error, null, 0, code, text);
        AddTransitionEvent(result, disconnected);
        if (!ClosureResetApplied && (Settings.ResetMode & SessionResetMode.OnDisconnect) == SessionResetMode.OnDisconnect)
        {
            if (!ResetSequenceState(result, "disconnect-sequence-reset", "The sequence state was reset before transport retry."))
            {
                FaultSession(result, "disconnect-sequence-reset-failed", "The retry sequence reset could not be persisted.");
                return;
            }
            ClosureResetApplied = true;
        }
        Reconnects.ScheduleNextAttempt(ReadUtcNow());
        if (Settings.Role == SessionRole.Initiator)
        {
            SessionStateTransitionResult scheduled = StateMachine.ScheduleReconnect();
            if (!scheduled.Accepted)
            {
                FaultSession(result, scheduled.Code, scheduled.Text);
                return;
            }
        }
        AddEvent(result, SessionEventKind.None, null, 0, "transport-retry-scheduled", "The retryable transport outcome scheduled another endpoint attempt.");
    }

    internal static DateTime CreateSessionDeadline(DateTime start, int timeoutMilliseconds, string name)
    {
        long timeoutTicks = (long)timeoutMilliseconds * TimeSpan.TicksPerMillisecond;
        if (start.Ticks > DateTime.MaxValue.Ticks - timeoutTicks)
        {
            throw new FixSessionException("The " + name + " deadline exceeds the supported UTC range.");
        }
        return start.AddTicks(timeoutTicks);
    }

    internal void FaultSession(SessionPollResult result, string code, string text)
    {
        Transport.CloseTransport();
        CurrentTransportState = TransportState.Faulted;
        HasConnectDeadline = false;
        HasLogonDeadline = false;
        Reconnects.ResetPlan();
        Timers.StopTimers();
        SessionStateTransitionResult faulted = StateMachine.FaultSession();
        AddEvent(result, SessionEventKind.Error, null, 0, code, text);
        AddTransitionEvent(result, faulted);
        if (!ClosureResetApplied && (Settings.ResetMode & SessionResetMode.OnDisconnect) == SessionResetMode.OnDisconnect)
        {
            ResetSequenceState(result, "disconnect-sequence-reset", "The sequence state was reset after transport failure.");
            ClosureResetApplied = true;
        }
    }

    internal void CloseTransport(SessionPollResult result, string code, string text, bool fault)
    {
        TransportWriteResult closed = Transport.CloseTransport();
        CurrentTransportState = fault ? TransportState.Faulted : closed == null ? TransportState.Closed : closed.State;
        HasConnectDeadline = false;
        HasLogonDeadline = false;
        Reconnects.ResetPlan();
        Timers.StopTimers();
        Negotiation.IsLoggedOn = false;
        if (fault)
        {
            SessionStateTransitionResult faulted = StateMachine.FaultSession();
            AddEvent(result, SessionEventKind.Error, null, 0, code, text);
            AddTransitionEvent(result, faulted);
            if (!ClosureResetApplied && (Settings.ResetMode & SessionResetMode.OnDisconnect) == SessionResetMode.OnDisconnect)
            {
                ResetSequenceState(result, "disconnect-sequence-reset", "The sequence state was reset after transport failure.");
                ClosureResetApplied = true;
            }
            return;
        }
        SessionStateTransitionResult disconnected = StateMachine.DisconnectTransport();
        if (disconnected.Accepted) AddTransitionEvent(result, disconnected);
        else AddEvent(result, SessionEventKind.Disconnected, null, 0, code, text);
        if (!ClosureResetApplied && (Settings.ResetMode & SessionResetMode.OnDisconnect) == SessionResetMode.OnDisconnect)
        {
            if (!ResetSequenceState(result, "disconnect-sequence-reset", "The sequence state was reset after transport disconnect."))
            {
                CurrentTransportState = TransportState.Faulted;
                AddTransitionEvent(result, StateMachine.FaultSession());
            }
            ClosureResetApplied = true;
        }
    }

    internal DateTime ReadUtcNow()
    {
        DateTime now = Clock.GetUtcNow();
        if (now.Kind != DateTimeKind.Utc)
        {
            throw new FixSessionException("The FixSession clock returned a non-UTC value.");
        }
        return now;
    }

    internal static byte[] CopyBytes(byte[] source)
    {
        byte[] copy = new byte[source.Length];
        Array.Copy(source, copy, source.Length);
        return copy;
    }
}
