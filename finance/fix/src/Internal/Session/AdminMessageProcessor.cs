using System.Text;

namespace fix;

internal class AdminMessageProcessor
{
    internal SessionTimers Timers;
    internal ReplayProcessor ReplayProcessor;
    internal SequenceResetProcessor SequenceResetProcessor;
    internal AdminMessageFactory Factory;
    internal ISessionApplication Application;

    internal AdminMessageProcessor(SessionTimers timers, ReplayProcessor replayProcessor, SequenceResetProcessor sequenceResetProcessor, AdminMessageFactory factory, ISessionApplication application)
    {
        if (timers == null || replayProcessor == null || sequenceResetProcessor == null || factory == null || application == null)
        {
            throw new FixSessionException("Administrative message processing dependencies are missing.");
        }
        Timers = timers;
        ReplayProcessor = replayProcessor;
        SequenceResetProcessor = sequenceResetProcessor;
        Factory = factory;
        Application = application;
    }

    internal AdminMessageProcessingResult ProcessInboundAdministrative(FixMessage message, int sequenceNumber, int responseSequenceNumber, bool possibleDuplicate)
    {
        if (message == null)
        {
            return CreateFailure(sequenceNumber, "", "missing-administrative-message", "The inbound administrative message is missing.", false);
        }
        if (sequenceNumber < FixDefaults.InitialSequenceNumber || responseSequenceNumber < FixDefaults.InitialSequenceNumber)
        {
            return CreateFailure(sequenceNumber, message.MsgType, "invalid-administrative-sequence-number", "Administrative message sequence numbers must be positive.", false);
        }
        if (message.MsgType == FixAdminMessageTypes.Logon)
        {
            AdminMessageProcessingResult routed = CreateSuccess(sequenceNumber, message.MsgType, "route-logon", "The Logon requires specialized negotiation processing.");
            routed.RouteToLogonProcessor = true;
            return routed;
        }
        if (message.MsgType == FixAdminMessageTypes.Logout)
        {
            AdminMessageProcessingResult routed = CreateSuccess(sequenceNumber, message.MsgType, "route-logout", "The Logout requires specialized lifecycle processing.");
            routed.RouteToLogoutProcessor = true;
            return routed;
        }
        if (!IsSupportedAdministrativeType(message.MsgType))
        {
            return CreateFailure(sequenceNumber, message.MsgType, "unsupported-administrative-message-type", "The message type is not a supported administrative message.", false);
        }

        AdminMessageProcessingResult? validation = ValidateAdministrativeMessageFields(message, sequenceNumber);
        if (validation != null)
        {
            return validation;
        }

        ApplicationDecision decision = Application.OnAdministrativeMessage(CreateApplicationContext(message, sequenceNumber, possibleDuplicate));
        if (decision == null)
        {
            return CreateFailure(sequenceNumber, message.MsgType, "missing-administrative-decision", "The application did not return an administrative decision.", true);
        }
        if (decision.Disposition != MessageDisposition.Accept)
        {
            bool disconnect = decision.Disposition == MessageDisposition.Disconnect;
            string code = decision.Code == null || decision.Code.Length == 0 ? "administrative-message-not-accepted" : decision.Code;
            string text = decision.Text == null || decision.Text.Length == 0 ? "The application did not accept the administrative message." : decision.Text;
            AdminMessageProcessingResult declined = CreateFailure(sequenceNumber, message.MsgType, code, text, disconnect);
            declined.ApplicationDecision = decision;
            return declined;
        }

        AdminMessageProcessingResult result;
        if (message.MsgType == FixAdminMessageTypes.Heartbeat) result = ProcessHeartbeat(message, sequenceNumber);
        else if (message.MsgType == FixAdminMessageTypes.TestRequest) result = ProcessTestRequest(message, sequenceNumber, responseSequenceNumber);
        else if (message.MsgType == FixAdminMessageTypes.ResendRequest) result = ProcessResendRequest(message, sequenceNumber);
        else if (message.MsgType == FixAdminMessageTypes.SequenceReset) result = ProcessSequenceReset(message, sequenceNumber, possibleDuplicate);
        else result = ProcessRejectNotification(message, sequenceNumber);
        result.ApplicationDecision = decision;
        return result;
    }

    internal AdminMessageProcessingResult ProcessHeartbeat(FixMessage message, int sequenceNumber)
    {
        AdminFieldValue testRequestId = ReadTextField(message, FixTags.TestReqID, false);
        if (!testRequestId.Valid)
        {
            return CreateFailure(sequenceNumber, message.MsgType, "invalid-heartbeat-test-request-id", "Heartbeat TestReqID occurs more than once.", false);
        }
        SessionTimerActionResult timer = Timers.HandleHeartbeat(testRequestId.TextValue);
        AdminMessageProcessingResult result = FromTimerAction(timer, sequenceNumber, message.MsgType);
        if (timer.Accepted)
        {
            result.Code = timer.MatchedTestRequest ? "heartbeat-test-request-matched" : "heartbeat-processed";
            result.Text = timer.MatchedTestRequest ? "The Heartbeat matched the pending TestReqID." : "The Heartbeat was processed.";
        }
        return result;
    }

    internal AdminMessageProcessingResult? ValidateAdministrativeMessageFields(FixMessage message, int sequenceNumber)
    {
        if (message.MsgType == FixAdminMessageTypes.Heartbeat)
        {
            AdminFieldValue testRequestId = ReadTextField(message, FixTags.TestReqID, false);
            if (!testRequestId.Valid) return CreateFailure(sequenceNumber, message.MsgType, "invalid-heartbeat-test-request-id", "Heartbeat TestReqID occurs more than once.", false);
            return null;
        }
        if (message.MsgType == FixAdminMessageTypes.TestRequest)
        {
            AdminFieldValue testRequestId = ReadTextField(message, FixTags.TestReqID, true);
            if (!testRequestId.Valid || testRequestId.TextValue.Length == 0) return CreateFailure(sequenceNumber, message.MsgType, "invalid-test-request-id", "TestRequest requires exactly one non-empty TestReqID.", false);
            return null;
        }
        if (message.MsgType == FixAdminMessageTypes.ResendRequest)
        {
            AdminFieldValue begin = ReadIntegerField(message, FixTags.BeginSeqNo, true, false);
            AdminFieldValue end = ReadIntegerField(message, FixTags.EndSeqNo, true, true);
            if (!begin.Valid || begin.IntegerValue < FixDefaults.InitialSequenceNumber || !end.Valid || end.IntegerValue < 0 || end.IntegerValue != 0 && end.IntegerValue < begin.IntegerValue) return CreateFailure(sequenceNumber, message.MsgType, "invalid-resend-request-range", "ResendRequest requires one valid BeginSeqNo and EndSeqNo range.", false);
            return null;
        }
        if (message.MsgType == FixAdminMessageTypes.SequenceReset)
        {
            AdminFieldValue newSequence = ReadIntegerField(message, FixTags.NewSeqNo, true, false);
            AdminFieldValue gapFill = ReadFlagField(message, FixTags.GapFillFlag, false);
            if (!newSequence.Valid || newSequence.IntegerValue < FixDefaults.InitialSequenceNumber || !gapFill.Valid) return CreateFailure(sequenceNumber, message.MsgType, "invalid-sequence-reset-fields", "SequenceReset requires a valid NewSeqNo and optional GapFillFlag.", false);
            return null;
        }
        AdminFieldValue reference = ReadIntegerField(message, FixTags.RefSeqNum, true, false);
        if (!reference.Valid || reference.IntegerValue < FixDefaults.InitialSequenceNumber) return CreateFailure(sequenceNumber, message.MsgType, "invalid-reject-reference", "Reject messages require exactly one positive RefSeqNum.", false);
        if (message.MsgType == FixAdminMessageTypes.BusinessMessageReject)
        {
            AdminFieldValue refType = ReadTextField(message, FixTags.RefMsgType, true);
            AdminFieldValue reason = ReadIntegerField(message, FixTags.BusinessRejectReason, true, true);
            if (!refType.Valid || refType.TextValue.Length == 0 || !reason.Valid || reason.IntegerValue < 0) return CreateFailure(sequenceNumber, message.MsgType, "invalid-business-reject-reference", "BusinessMessageReject requires RefMsgType and BusinessRejectReason.", false);
        }
        return null;
    }

    internal AdminMessageProcessingResult ProcessTestRequest(FixMessage message, int sequenceNumber, int responseSequenceNumber)
    {
        AdminFieldValue testRequestId = ReadTextField(message, FixTags.TestReqID, true);
        if (!testRequestId.Valid || testRequestId.TextValue.Length == 0)
        {
            return CreateFailure(sequenceNumber, message.MsgType, "invalid-test-request-id", "TestRequest requires exactly one non-empty TestReqID.", false);
        }
        SessionTimerActionResult timer = Timers.HandleTestRequest(testRequestId.TextValue);
        AdminMessageProcessingResult result = FromTimerAction(timer, sequenceNumber, message.MsgType);
        if (timer.SendHeartbeat)
        {
            result.SendHeartbeat = true;
            result.OutboundMessage = Factory.CreateHeartbeatMessage(responseSequenceNumber, timer.TestRequestId);
            result.Code = "test-request-heartbeat-created";
            result.Text = "A matching Heartbeat was created for the TestRequest.";
        }
        return result;
    }

    internal AdminMessageProcessingResult ProcessResendRequest(FixMessage message, int sequenceNumber)
    {
        AdminFieldValue begin = ReadIntegerField(message, FixTags.BeginSeqNo, true, false);
        AdminFieldValue end = ReadIntegerField(message, FixTags.EndSeqNo, true, true);
        if (!begin.Valid || begin.IntegerValue < FixDefaults.InitialSequenceNumber || !end.Valid || end.IntegerValue < 0 || end.IntegerValue != 0 && end.IntegerValue < begin.IntegerValue)
        {
            return CreateFailure(sequenceNumber, message.MsgType, "invalid-resend-request-range", "ResendRequest requires one valid BeginSeqNo and EndSeqNo range.", false);
        }
        ReplayProcessingResult replay = ReplayProcessor.ProcessReplayRange(begin.IntegerValue, end.IntegerValue);
        AdminMessageProcessingResult result = CreateSuccess(sequenceNumber, message.MsgType, replay.Code, replay.Text);
        result.Accepted = replay.Accepted;
        result.Disconnect = replay.Disconnect;
        result.ReplayResult = replay;
        return result;
    }

    internal AdminMessageProcessingResult ProcessSequenceReset(FixMessage message, int sequenceNumber, bool possibleDuplicate)
    {
        AdminFieldValue newSequence = ReadIntegerField(message, FixTags.NewSeqNo, true, false);
        AdminFieldValue gapFill = ReadFlagField(message, FixTags.GapFillFlag, false);
        if (!newSequence.Valid || newSequence.IntegerValue < FixDefaults.InitialSequenceNumber || !gapFill.Valid)
        {
            return CreateFailure(sequenceNumber, message.MsgType, "invalid-sequence-reset-fields", "SequenceReset requires a valid NewSeqNo and optional GapFillFlag.", false);
        }
        SequenceResetProcessingResult processed = SequenceResetProcessor.ProcessSequenceReset(message, sequenceNumber, newSequence.IntegerValue, gapFill.FlagValue, possibleDuplicate);
        AdminMessageProcessingResult result = CreateSuccess(sequenceNumber, message.MsgType, processed.Code, processed.Text);
        result.Accepted = processed.Accepted;
        result.Disconnect = processed.Disconnect;
        result.SequenceResetResult = processed;
        return result;
    }

    internal AdminMessageProcessingResult ProcessRejectNotification(FixMessage message, int sequenceNumber)
    {
        AdminFieldValue reference = ReadIntegerField(message, FixTags.RefSeqNum, true, false);
        if (!reference.Valid || reference.IntegerValue < FixDefaults.InitialSequenceNumber)
        {
            return CreateFailure(sequenceNumber, message.MsgType, "invalid-reject-reference", "Reject messages require exactly one positive RefSeqNum.", false);
        }
        if (message.MsgType == FixAdminMessageTypes.BusinessMessageReject)
        {
            AdminFieldValue refType = ReadTextField(message, FixTags.RefMsgType, true);
            AdminFieldValue reason = ReadIntegerField(message, FixTags.BusinessRejectReason, true, true);
            if (!refType.Valid || refType.TextValue.Length == 0 || !reason.Valid || reason.IntegerValue < 0)
            {
                return CreateFailure(sequenceNumber, message.MsgType, "invalid-business-reject-reference", "BusinessMessageReject requires RefMsgType and BusinessRejectReason.", false);
            }
        }
        AdminMessageProcessingResult result = CreateSuccess(sequenceNumber, message.MsgType, "reject-notification-processed", "The reject notification was processed.");
        return result;
    }

    internal AdminFieldValue ReadTextField(FixMessage message, int tag, bool required)
    {
        AdminFieldValue result = new AdminFieldValue();
        result.Count = LogoutProcessor.CountField(message, tag);
        result.Present = result.Count != 0;
        result.Valid = result.Count == 1 || !required && result.Count == 0;
        if (result.Count == 1)
        {
            FixField? field = EnvelopeValidator.FindFirstMessageField(message, tag);
            result.TextValue = field == null ? "" : Encoding.Latin1.GetString(field.Value);
        }
        return result;
    }

    internal AdminFieldValue ReadIntegerField(FixMessage message, int tag, bool required, bool allowZero)
    {
        AdminFieldValue result = ReadTextField(message, tag, required);
        if (!result.Valid || !result.Present)
        {
            return result;
        }
        FixField? field = EnvelopeValidator.FindFirstMessageField(message, tag);
        ScalarParseResult parsed = ScalarParser.ParseScalar(field == null ? [] : field.Value, FixFieldType.Int, FixDefaults.MaxFieldSize);
        result.Valid = parsed.Success && (allowZero ? parsed.IntegerValue >= 0 : parsed.IntegerValue > 0);
        result.IntegerValue = parsed.IntegerValue;
        return result;
    }

    internal AdminFieldValue ReadFlagField(FixMessage message, int tag, bool required)
    {
        AdminFieldValue result = ReadTextField(message, tag, required);
        if (!result.Valid || !result.Present)
        {
            return result;
        }
        result.Valid = result.TextValue == "Y" || result.TextValue == "N";
        result.FlagValue = result.TextValue == "Y";
        return result;
    }

    internal ApplicationMessageContext CreateApplicationContext(FixMessage message, int sequenceNumber, bool possibleDuplicate)
    {
        ApplicationMessageContext context = new ApplicationMessageContext();
        context.Id = LogonProcessor.CopySessionId(Factory.Id);
        context.Message = message;
        context.SequenceNumber = sequenceNumber;
        context.IsAdministrative = true;
        context.IsPossibleDuplicate = possibleDuplicate;
        return context;
    }

    internal static bool IsSupportedAdministrativeType(string msgType)
    {
        return msgType == FixAdminMessageTypes.Heartbeat || msgType == FixAdminMessageTypes.TestRequest || msgType == FixAdminMessageTypes.ResendRequest || msgType == FixAdminMessageTypes.Reject || msgType == FixAdminMessageTypes.SequenceReset || msgType == FixAdminMessageTypes.BusinessMessageReject;
    }

    internal AdminMessageProcessingResult FromTimerAction(SessionTimerActionResult timer, int sequenceNumber, string msgType)
    {
        AdminMessageProcessingResult result = CreateSuccess(sequenceNumber, msgType, timer.Code, timer.Text);
        result.Accepted = timer.Accepted;
        result.Disconnect = timer.Disconnect;
        result.TimerAction = timer;
        return result;
    }

    internal static AdminMessageProcessingResult CreateSuccess(int sequenceNumber, string msgType, string code, string text)
    {
        AdminMessageProcessingResult result = new AdminMessageProcessingResult();
        result.Accepted = true;
        result.SequenceNumber = sequenceNumber;
        result.MsgType = msgType;
        result.Code = code;
        result.Text = text;
        return result;
    }

    internal static AdminMessageProcessingResult CreateFailure(int sequenceNumber, string msgType, string code, string text, bool disconnect)
    {
        AdminMessageProcessingResult result = new AdminMessageProcessingResult();
        result.Disconnect = disconnect;
        result.SequenceNumber = sequenceNumber;
        result.MsgType = msgType;
        result.Code = code;
        result.Text = text;
        return result;
    }
}
