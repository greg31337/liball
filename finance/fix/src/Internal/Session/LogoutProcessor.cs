using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace fix;

internal class LogoutProcessor
{
    internal SessionId Id = new SessionId();
    internal int TimeoutMilliseconds;
    internal FixDictionaryIdentity TransportIdentity = new FixDictionaryIdentity();
    internal ValidationOptions ValidationOptions = new ValidationOptions();
    internal FixDictionary TransportDictionary;
    internal IMessageValidator MessageValidator;
    internal ISessionApplication Application;
    internal ISessionClock Clock;
    internal SessionHeaderValidator HeaderValidator;

    internal LogoutProcessor(SessionSettings settings, FixDictionary transportDictionary, IMessageValidator messageValidator, ISessionApplication application, ISessionClock clock)
    {
        if (settings == null || settings.Id == null || settings.TransportDictionary == null || settings.Validation == null)
        {
            throw new FixSessionException("Logout settings are missing.");
        }
        if (transportDictionary == null || messageValidator == null || application == null || clock == null)
        {
            throw new FixSessionException("A Logout processing dependency is missing.");
        }
        if (settings.LogoutTimeoutMilliseconds <= 0)
        {
            throw new FixSessionException("The Logout timeout must be positive.");
        }
        if (settings.Id.BeginString.Length == 0 || settings.Id.SenderCompID.Length == 0 || settings.Id.TargetCompID.Length == 0)
        {
            throw new FixSessionException("The Logout session identity is incomplete.");
        }
        Id = LogonProcessor.CopySessionId(settings.Id);
        TimeoutMilliseconds = settings.LogoutTimeoutMilliseconds;
        TransportIdentity = LogonProcessor.CopyIdentity(settings.TransportDictionary);
        if (TransportIdentity.BeginString.Length == 0)
        {
            TransportIdentity.BeginString = Id.BeginString;
        }
        ValidationOptions = LogonProcessor.CopyValidationOptions(settings.Validation);
        TransportDictionary = transportDictionary;
        MessageValidator = messageValidator;
        Application = application;
        Clock = clock;
        HeaderValidator = new SessionHeaderValidator(settings, clock);
    }

    internal LogoutLifecycleState CreateLogoutState()
    {
        return new LogoutLifecycleState();
    }

    internal FixMessage CreateLogoutMessage(int senderSequenceNumber, string text)
    {
        if (senderSequenceNumber < FixDefaults.InitialSequenceNumber)
        {
            throw new FixSessionException("The Logout sequence number is invalid.");
        }
        DateTime now = ReadUtcNow();
        return CreateLogoutAtTime(senderSequenceNumber, text, now);
    }

    internal LogoutActionResult BeginGracefulLogout(int senderSequenceNumber, string text, SessionStateMachine stateMachine, LogoutLifecycleState lifecycle)
    {
        if (stateMachine == null || lifecycle == null)
        {
            throw new FixSessionException("Logout lifecycle state is missing.");
        }
        if (lifecycle.InProgress)
        {
            return RejectAction(stateMachine.State, "logout-already-in-progress", "A Logout exchange is already in progress.");
        }
        if (senderSequenceNumber < FixDefaults.InitialSequenceNumber)
        {
            return RejectAction(stateMachine.State, "invalid-logout-sequence-number", "The Logout sequence number is invalid.");
        }
        DateTime now = ReadUtcNow();
        DateTime deadline = AddTimeout(now);
        FixMessage message = CreateLogoutAtTime(senderSequenceNumber, text, now);
        SessionStateTransitionResult transition = stateMachine.BeginLogout();
        if (!transition.Accepted)
        {
            return RejectAction(stateMachine.State, transition.Code, transition.Text);
        }

        lifecycle.InProgress = true;
        lifecycle.LocalLogoutInitiated = true;
        lifecycle.LocalText = text == null ? "" : text;
        lifecycle.HasDeadline = true;
        lifecycle.DeadlineUtc = deadline;
        LogoutActionResult result = SuccessAction(stateMachine.State, "logout-initiated", lifecycle.LocalText);
        result.SendLogout = true;
        result.AwaitingPeerLogout = true;
        result.OutboundMessage = message;
        return result;
    }

    internal LogoutActionResult ProcessInboundLogout(FixMessage message, int sequenceNumber, int responseSequenceNumber, string qualifier, SessionStateMachine stateMachine, LogoutLifecycleState lifecycle)
    {
        if (stateMachine == null || lifecycle == null)
        {
            throw new FixSessionException("Logout lifecycle state is missing.");
        }
        if (message == null)
        {
            return RejectAction(stateMachine.State, "missing-logout-message", "The inbound Logout message is missing.");
        }
        if (sequenceNumber < FixDefaults.InitialSequenceNumber || responseSequenceNumber < FixDefaults.InitialSequenceNumber)
        {
            return RejectAction(stateMachine.State, "invalid-logout-sequence-number", "The inbound Logout sequence number is invalid.");
        }

        LogoutActionResult validation = ValidateInboundLogout(message, qualifier, stateMachine.State);
        if (!validation.Accepted)
        {
            return validation;
        }
        bool localExchange = stateMachine.State == SessionState.LogoutPending && lifecycle.LocalLogoutInitiated;
        bool responseExchange = stateMachine.State == SessionState.LogoutPending && lifecycle.ResponsePending;
        bool remoteExchange = stateMachine.State == SessionState.Active || stateMachine.State == SessionState.Recovering;
        if (!localExchange && !responseExchange && !remoteExchange)
        {
            return RejectAction(stateMachine.State, "logout-not-valid-in-session-state", "An inbound Logout is not valid in the current session state.");
        }
        ApplicationMessageContext context = CreateApplicationContext(message, sequenceNumber);
        ApplicationDecision administrative = Application.OnAdministrativeMessage(context);
        if (administrative == null)
        {
            return RejectAction(stateMachine.State, "missing-administrative-decision", "The application did not return a Logout administrative decision.");
        }

        string peerText = ReadLogoutText(message);
        lifecycle.RemoteLogoutReceived = true;
        lifecycle.PeerText = peerText;
        lifecycle.PeerMessage = message;
        lifecycle.PeerSequenceNumber = sequenceNumber;
        if (localExchange)
        {
            lifecycle.InProgress = true;
            lifecycle.ResponsePending = false;
            lifecycle.HasDeadline = false;
            SessionStateTransitionResult completed = stateMachine.CompleteLogout();
            if (!completed.Accepted)
            {
                return RejectAction(stateMachine.State, completed.Code, completed.Text);
            }
            LogoutActionResult simultaneous = SuccessAction(stateMachine.State, "simultaneous-logout-completed", peerText);
            simultaneous.CloseTransport = true;
            simultaneous.SimultaneousLogout = true;
            simultaneous.PeerText = peerText;
            simultaneous.AdministrativeDecision = administrative;
            simultaneous.LogoutDecision = NotifyLogout(lifecycle, message, sequenceNumber);
            lifecycle.InProgress = false;
            return simultaneous;
        }
        if (responseExchange)
        {
            LogoutActionResult duplicate = SuccessAction(stateMachine.State, "logout-response-pending", peerText);
            duplicate.SendLogout = true;
            duplicate.OutboundMessage = lifecycle.PendingResponse;
            duplicate.PeerText = peerText;
            duplicate.AdministrativeDecision = administrative;
            return duplicate;
        }
        DateTime now = ReadUtcNow();
        DateTime deadline = AddTimeout(now);
        FixMessage responseMessage = CreateLogoutAtTime(responseSequenceNumber, peerText, now);
        SessionStateTransitionResult pending = stateMachine.BeginLogout();
        if (!pending.Accepted)
        {
            return RejectAction(stateMachine.State, pending.Code, pending.Text);
        }
        lifecycle.InProgress = true;
        lifecycle.ResponsePending = true;
        lifecycle.HasDeadline = true;
        lifecycle.DeadlineUtc = deadline;
        lifecycle.PendingResponse = responseMessage;
        LogoutActionResult response = SuccessAction(stateMachine.State, "logout-response-required", peerText);
        response.SendLogout = true;
        response.OutboundMessage = lifecycle.PendingResponse;
        response.PeerText = peerText;
        response.AdministrativeDecision = administrative;
        return response;
    }

    internal LogoutActionResult ConfirmLogoutSent(SessionStateMachine stateMachine, LogoutLifecycleState lifecycle)
    {
        if (stateMachine == null || lifecycle == null)
        {
            throw new FixSessionException("Logout lifecycle state is missing.");
        }
        if (!lifecycle.InProgress || !lifecycle.ResponsePending || lifecycle.PeerMessage == null)
        {
            return RejectAction(stateMachine.State, "no-logout-response-pending", "No peer-initiated Logout response is pending.");
        }
        lifecycle.ResponsePending = false;
        lifecycle.HasDeadline = false;
        SessionStateTransitionResult completed = stateMachine.CompleteLogout();
        if (!completed.Accepted)
        {
            return RejectAction(stateMachine.State, completed.Code, completed.Text);
        }
        LogoutActionResult result = SuccessAction(stateMachine.State, "logout-response-sent", lifecycle.PeerText);
        result.CloseTransport = true;
        result.PeerText = lifecycle.PeerText;
        result.LogoutDecision = NotifyLogout(lifecycle, lifecycle.PeerMessage, lifecycle.PeerSequenceNumber);
        lifecycle.InProgress = false;
        return result;
    }

    internal LogoutActionResult CheckLogoutTimeout(SessionStateMachine stateMachine, LogoutLifecycleState lifecycle)
    {
        if (stateMachine == null || lifecycle == null)
        {
            throw new FixSessionException("Logout lifecycle state is missing.");
        }
        if (!lifecycle.InProgress || !lifecycle.HasDeadline)
        {
            return RejectAction(stateMachine.State, "no-logout-timeout-pending", "No Logout timeout is pending.");
        }
        DateTime now = ReadUtcNow();
        if (now < lifecycle.DeadlineUtc)
        {
            LogoutActionResult waiting = SuccessAction(stateMachine.State, "logout-timeout-pending", "The Logout timeout has not elapsed.");
            waiting.AwaitingPeerLogout = lifecycle.LocalLogoutInitiated;
            return waiting;
        }

        lifecycle.HasDeadline = false;
        lifecycle.ResponsePending = false;
        CompleteOrDisconnect(stateMachine);
        FixMessage notification = lifecycle.PeerMessage == null ? CreateSyntheticLogout("Logout timed out.") : lifecycle.PeerMessage;
        int sequenceNumber = lifecycle.PeerMessage == null ? 0 : lifecycle.PeerSequenceNumber;
        LogoutActionResult result = SuccessAction(stateMachine.State, "logout-timeout", "The Logout exchange timed out.");
        result.CloseTransport = true;
        result.ImmediateClose = true;
        result.TimedOut = true;
        result.LogoutDecision = NotifyLogout(lifecycle, notification, sequenceNumber);
        lifecycle.InProgress = false;
        return result;
    }

    internal LogoutActionResult DisconnectImmediately(string reason, SessionStateMachine stateMachine, LogoutLifecycleState lifecycle)
    {
        if (stateMachine == null || lifecycle == null)
        {
            throw new FixSessionException("Logout lifecycle state is missing.");
        }
        string text = reason == null ? "" : reason;
        CompleteOrDisconnect(stateMachine);
        lifecycle.InProgress = false;
        lifecycle.ResponsePending = false;
        lifecycle.HasDeadline = false;
        FixMessage notification = lifecycle.PeerMessage == null ? CreateSyntheticLogout(text) : lifecycle.PeerMessage;
        int sequenceNumber = lifecycle.PeerMessage == null ? 0 : lifecycle.PeerSequenceNumber;
        LogoutActionResult result = SuccessAction(stateMachine.State, "immediate-disconnect", text);
        result.CloseTransport = true;
        result.ImmediateClose = true;
        result.LogoutDecision = NotifyLogout(lifecycle, notification, sequenceNumber);
        return result;
    }

    internal LogoutActionResult ValidateInboundLogout(FixMessage message, string qualifier, SessionState state)
    {
        List<ValidationIssue> issues = [];
        SessionHeaderValidationResult header = HeaderValidator.ValidateInboundHeader(message, qualifier);
        LogonProcessor.CopyIssues(issues, header.Issues);
        if (message.MsgType != FixAdminMessageTypes.Logout)
        {
            LogonProcessor.AddIssue(issues, message, "unexpected-logout-message-type", "The inbound message is not a Logout.", true, FixTags.MsgType, EnvelopeValidator.FindFirstMessageField(message, FixTags.MsgType));
        }
        ValidationContext context = new ValidationContext();
        context.DictionaryIdentity = LogonProcessor.CopyIdentity(TransportIdentity);
        context.ExpectedBeginString = Id.BeginString;
        ValidationResult validation = MessageValidator.ValidateMessage(message, TransportDictionary, ValidationOptions, context);
        if (validation == null)
        {
            LogonProcessor.AddIssue(issues, message, "missing-logout-validation-result", "The message validator did not return a result.", false, 0, null);
        }
        else if (!validation.IsValid)
        {
            LogonProcessor.CopyIssues(issues, validation.Issues);
            if (validation.Issues.Count == 0)
            {
                LogonProcessor.AddIssue(issues, message, "logout-validation-failed", "The inbound Logout failed message validation.", false, 0, null);
            }
        }
        if (CountField(message, FixTags.Text) > 1)
        {
            LogonProcessor.AddIssue(issues, message, "duplicate-logout-text", "Text occurs more than once in the Logout message.", true, FixTags.Text, EnvelopeValidator.FindFirstMessageField(message, FixTags.Text));
        }
        if (issues.Count != 0)
        {
            RejectMapper.MapValidationIssues(issues, message);
            LogoutActionResult rejected = RejectAction(state, issues[0].Code, issues[0].Message);
            LogonProcessor.CopyIssues(rejected.Issues, issues);
            return rejected;
        }
        return SuccessAction(state, "logout-validated", "The inbound Logout passed validation.");
    }

    internal FixMessage CreateLogoutAtTime(int senderSequenceNumber, string text, DateTime sendingTime)
    {
        FixMessage message = new FixMessage();
        message.BeginString = Id.BeginString;
        message.MsgType = FixAdminMessageTypes.Logout;
        LogonProcessor.AddHeaderText(message, FixTags.BeginString, Id.BeginString);
        LogonProcessor.AddHeaderText(message, FixTags.MsgType, FixAdminMessageTypes.Logout);
        LogonProcessor.AddHeaderInteger(message, FixTags.MsgSeqNum, senderSequenceNumber);
        LogonProcessor.AddHeaderText(message, FixTags.SenderCompID, Id.SenderCompID);
        LogonProcessor.AddOptionalHeaderText(message, FixTags.SenderSubID, Id.SenderSubID);
        LogonProcessor.AddOptionalHeaderText(message, FixTags.SenderLocationID, Id.SenderLocationID);
        LogonProcessor.AddHeaderText(message, FixTags.TargetCompID, Id.TargetCompID);
        LogonProcessor.AddOptionalHeaderText(message, FixTags.TargetSubID, Id.TargetSubID);
        LogonProcessor.AddOptionalHeaderText(message, FixTags.TargetLocationID, Id.TargetLocationID);
        LogonProcessor.AddHeaderText(message, FixTags.SendingTime, LogonProcessor.FormatSendingTime(sendingTime));
        string actualText = text == null ? "" : text;
        if (actualText.Length != 0)
        {
            LogonProcessor.AddBodyText(message, FixTags.Text, actualText);
        }
        return message;
    }

    internal FixMessage CreateSyntheticLogout(string text)
    {
        FixMessage message = new FixMessage();
        message.BeginString = Id.BeginString;
        message.MsgType = FixAdminMessageTypes.Logout;
        if (text.Length != 0)
        {
            LogonProcessor.AddBodyText(message, FixTags.Text, text);
        }
        return message;
    }

    internal ApplicationMessageContext CreateApplicationContext(FixMessage message, int sequenceNumber)
    {
        ApplicationMessageContext context = new ApplicationMessageContext();
        context.Id = LogonProcessor.CopySessionId(Id);
        context.Message = message;
        context.SequenceNumber = sequenceNumber;
        context.IsAdministrative = true;
        FixField? possibleDuplicate = EnvelopeValidator.FindFirstMessageField(message, FixTags.PossDupFlag);
        context.IsPossibleDuplicate = possibleDuplicate != null && possibleDuplicate.Value.Length == 1 && possibleDuplicate.Value[0] == 89;
        return context;
    }

    internal ApplicationDecision NotifyLogout(LogoutLifecycleState lifecycle, FixMessage message, int sequenceNumber)
    {
        if (lifecycle.LogoutNotified)
        {
            ApplicationDecision repeated = new ApplicationDecision();
            repeated.Disposition = MessageDisposition.Accept;
            repeated.Code = "logout-already-notified";
            repeated.Text = "The application was already notified of Logout completion.";
            return repeated;
        }
        lifecycle.LogoutNotified = true;
        ApplicationDecision decision = Application.OnSessionLogout(CreateApplicationContext(message, sequenceNumber));
        if (decision == null)
        {
            ApplicationDecision missing = new ApplicationDecision();
            missing.Disposition = MessageDisposition.Disconnect;
            missing.Code = "missing-logout-decision";
            missing.Text = "The application did not return a Logout decision.";
            return missing;
        }
        return decision;
    }

    internal void CompleteOrDisconnect(SessionStateMachine stateMachine)
    {
        if (stateMachine.State == SessionState.LogoutPending)
        {
            stateMachine.CompleteLogout();
        }
        else if (stateMachine.State == SessionState.Connecting || stateMachine.State == SessionState.Connected || stateMachine.State == SessionState.LogonPending || stateMachine.State == SessionState.Active || stateMachine.State == SessionState.Recovering)
        {
            stateMachine.DisconnectTransport();
        }
    }

    internal DateTime ReadUtcNow()
    {
        DateTime now = Clock.GetUtcNow();
        if (now.Kind != DateTimeKind.Utc)
        {
            throw new FixSessionException("The session clock returned a non-UTC value.");
        }
        return now;
    }

    internal DateTime AddTimeout(DateTime start)
    {
        long timeoutTicks = (long)TimeoutMilliseconds * TimeSpan.TicksPerMillisecond;
        if (start.Ticks > DateTime.MaxValue.Ticks - timeoutTicks)
        {
            throw new FixSessionException("The Logout deadline exceeds the supported UTC range.");
        }
        return start.AddTicks(timeoutTicks);
    }

    internal static int CountField(FixMessage message, int tag)
    {
        return EnvelopeValidator.CountSectionFields(message.HeaderFields, tag) + EnvelopeValidator.CountSectionFields(message.BodyFields, tag) + EnvelopeValidator.CountSectionFields(message.TrailerFields, tag);
    }

    internal static string ReadLogoutText(FixMessage message)
    {
        FixField? field = EnvelopeValidator.FindFirstMessageField(message, FixTags.Text);
        return field == null ? "" : Encoding.Latin1.GetString(field.Value);
    }

    internal static LogoutActionResult SuccessAction(SessionState state, string code, string text)
    {
        LogoutActionResult result = new LogoutActionResult();
        result.Accepted = true;
        result.State = state;
        result.Code = code;
        result.Text = text;
        return result;
    }

    internal static LogoutActionResult RejectAction(SessionState state, string code, string text)
    {
        LogoutActionResult result = new LogoutActionResult();
        result.State = state;
        result.Code = code;
        result.Text = text;
        return result;
    }
}
