using System;

namespace fix;

internal class AdminMessageFactory
{
    internal SessionSettings Settings;
    internal SessionId Id;
    internal ISessionClock Clock;

    internal AdminMessageFactory(SessionSettings settings, ISessionClock clock)
    {
        if (settings == null || settings.Id == null || clock == null)
        {
            throw new FixSessionException("Administrative message settings are missing.");
        }
        if (!IsSupportedBeginString(settings.Id.BeginString) || settings.Id.SenderCompID.Length == 0 || settings.Id.TargetCompID.Length == 0)
        {
            throw new FixSessionException("The administrative session identity is incomplete or unsupported.");
        }
        if (settings.HeartbeatIntervalSeconds < 0)
        {
            throw new FixSessionException("The administrative heartbeat interval cannot be negative.");
        }
        if (settings.Id.BeginString == FixBeginStrings.Fixt11 && settings.DefaultApplicationVersion.Length == 0)
        {
            throw new FixSessionException("A FIXT administrative factory requires DefaultApplVerID.");
        }
        Settings = settings;
        Id = LogonProcessor.CopySessionId(settings.Id);
        Clock = clock;
    }

    internal FixMessage CreateHeartbeatMessage(int senderSequenceNumber, string testRequestId)
    {
        FixMessage message = CreateStandardMessage(FixAdminMessageTypes.Heartbeat, senderSequenceNumber);
        string actualId = testRequestId == null ? "" : testRequestId;
        if (actualId.Length != 0)
        {
            LogonProcessor.AddBodyText(message, FixTags.TestReqID, actualId);
        }
        return message;
    }

    internal FixMessage CreateTestRequestMessage(int senderSequenceNumber, string testRequestId)
    {
        if (testRequestId == null || testRequestId.Length == 0)
        {
            throw new FixSessionException("A TestRequest requires TestReqID.");
        }
        FixMessage message = CreateStandardMessage(FixAdminMessageTypes.TestRequest, senderSequenceNumber);
        LogonProcessor.AddBodyText(message, FixTags.TestReqID, testRequestId);
        return message;
    }

    internal FixMessage CreateResendRequestMessage(int senderSequenceNumber, int beginSeqNo, int endSeqNo)
    {
        if (beginSeqNo < FixDefaults.InitialSequenceNumber || endSeqNo < 0 || endSeqNo != 0 && endSeqNo < beginSeqNo)
        {
            throw new FixSessionException("The ResendRequest range is invalid.");
        }
        FixMessage message = CreateStandardMessage(FixAdminMessageTypes.ResendRequest, senderSequenceNumber);
        LogonProcessor.AddBodyInteger(message, FixTags.BeginSeqNo, beginSeqNo);
        LogonProcessor.AddBodyInteger(message, FixTags.EndSeqNo, endSeqNo);
        return message;
    }

    internal FixMessage CreateRejectMessage(int senderSequenceNumber, int refSeqNum, int refTagId, string refMsgType, int sessionRejectReason, string text)
    {
        if (refSeqNum < FixDefaults.InitialSequenceNumber || refTagId < 0 || sessionRejectReason < 0)
        {
            throw new FixSessionException("The session Reject references are invalid.");
        }
        FixMessage message = CreateStandardMessage(FixAdminMessageTypes.Reject, senderSequenceNumber);
        LogonProcessor.AddBodyInteger(message, FixTags.RefSeqNum, refSeqNum);
        if (refTagId > 0) LogonProcessor.AddBodyInteger(message, FixTags.RefTagID, refTagId);
        string actualType = refMsgType == null ? "" : refMsgType;
        if (actualType.Length != 0) LogonProcessor.AddBodyText(message, FixTags.RefMsgType, actualType);
        LogonProcessor.AddBodyInteger(message, FixTags.SessionRejectReason, sessionRejectReason);
        AddOptionalText(message, text);
        return message;
    }

    internal FixMessage CreateSequenceResetMessage(int senderSequenceNumber, int newSeqNo, bool gapFill)
    {
        if (newSeqNo <= senderSequenceNumber)
        {
            throw new FixSessionException("SequenceReset NewSeqNo must be higher than MsgSeqNum.");
        }
        FixMessage message = CreateStandardMessage(FixAdminMessageTypes.SequenceReset, senderSequenceNumber);
        LogonProcessor.AddBodyText(message, FixTags.GapFillFlag, gapFill ? "Y" : "N");
        LogonProcessor.AddBodyInteger(message, FixTags.NewSeqNo, newSeqNo);
        return message;
    }

    internal FixMessage CreateLogonMessage(int senderSequenceNumber, int nextExpectedMsgSeqNum, bool resetSequenceNumbers)
    {
        if (senderSequenceNumber < FixDefaults.InitialSequenceNumber || nextExpectedMsgSeqNum < 0)
        {
            throw new FixSessionException("The Logon next expected sequence number is invalid.");
        }
        int actualSequenceNumber = resetSequenceNumbers ? FixDefaults.InitialSequenceNumber : senderSequenceNumber;
        FixMessage message = CreateStandardMessage(FixAdminMessageTypes.Logon, actualSequenceNumber);
        LogonProcessor.AddBodyInteger(message, FixTags.EncryptMethod, 0);
        LogonProcessor.AddBodyInteger(message, FixTags.HeartBtInt, Settings.HeartbeatIntervalSeconds);
        if (resetSequenceNumbers) LogonProcessor.AddBodyText(message, FixTags.ResetSeqNumFlag, "Y");
        if (nextExpectedMsgSeqNum > 0) LogonProcessor.AddBodyInteger(message, FixTags.NextExpectedMsgSeqNum, nextExpectedMsgSeqNum);
        if (Id.BeginString == FixBeginStrings.Fixt11) LogonProcessor.AddBodyText(message, FixTags.DefaultApplVerID, Settings.DefaultApplicationVersion);
        if (Settings.Role == SessionRole.Initiator)
        {
            LogonProcessor.AddOptionalBodyText(message, FixTags.Username, Settings.Username == null ? "" : Settings.Username);
            LogonProcessor.AddOptionalBodyText(message, FixTags.Password, Settings.Password == null ? "" : Settings.Password);
        }
        return message;
    }

    internal FixMessage CreateLogoutMessage(int senderSequenceNumber, string text)
    {
        FixMessage message = CreateStandardMessage(FixAdminMessageTypes.Logout, senderSequenceNumber);
        AddOptionalText(message, text);
        return message;
    }

    internal FixMessage CreateBusinessRejectMessage(int senderSequenceNumber, int refSeqNum, string refMsgType, string businessRejectRefId, int businessRejectReason, string text)
    {
        if (refSeqNum < FixDefaults.InitialSequenceNumber || refMsgType == null || refMsgType.Length == 0 || businessRejectReason < 0)
        {
            throw new FixSessionException("The BusinessMessageReject references are invalid.");
        }
        FixMessage message = CreateStandardMessage(FixAdminMessageTypes.BusinessMessageReject, senderSequenceNumber);
        LogonProcessor.AddBodyInteger(message, FixTags.RefSeqNum, refSeqNum);
        LogonProcessor.AddBodyText(message, FixTags.RefMsgType, refMsgType);
        string actualReference = businessRejectRefId == null ? "" : businessRejectRefId;
        if (actualReference.Length != 0) LogonProcessor.AddBodyText(message, FixTags.BusinessRejectRefID, actualReference);
        LogonProcessor.AddBodyInteger(message, FixTags.BusinessRejectReason, businessRejectReason);
        AddOptionalText(message, text);
        return message;
    }

    internal FixMessage CreateStandardMessage(string msgType, int senderSequenceNumber)
    {
        if (msgType == null || msgType.Length == 0 || senderSequenceNumber < FixDefaults.InitialSequenceNumber)
        {
            throw new FixSessionException("The administrative message type or sender sequence number is invalid.");
        }
        DateTime now = ReadUtcNow();
        FixMessage message = new FixMessage();
        message.BeginString = Id.BeginString;
        message.MsgType = msgType;
        LogonProcessor.AddHeaderText(message, FixTags.BeginString, Id.BeginString);
        LogonProcessor.AddHeaderText(message, FixTags.MsgType, msgType);
        LogonProcessor.AddHeaderInteger(message, FixTags.MsgSeqNum, senderSequenceNumber);
        LogonProcessor.AddHeaderText(message, FixTags.SenderCompID, Id.SenderCompID);
        LogonProcessor.AddOptionalHeaderText(message, FixTags.SenderSubID, Id.SenderSubID);
        LogonProcessor.AddOptionalHeaderText(message, FixTags.SenderLocationID, Id.SenderLocationID);
        LogonProcessor.AddHeaderText(message, FixTags.TargetCompID, Id.TargetCompID);
        LogonProcessor.AddOptionalHeaderText(message, FixTags.TargetSubID, Id.TargetSubID);
        LogonProcessor.AddOptionalHeaderText(message, FixTags.TargetLocationID, Id.TargetLocationID);
        LogonProcessor.AddHeaderText(message, FixTags.SendingTime, LogonProcessor.FormatSendingTime(now));
        return message;
    }

    internal static void AddOptionalText(FixMessage message, string text)
    {
        string actualText = text == null ? "" : text;
        if (actualText.Length != 0) LogonProcessor.AddBodyText(message, FixTags.Text, actualText);
    }

    internal DateTime ReadUtcNow()
    {
        DateTime now = Clock.GetUtcNow();
        if (now.Kind != DateTimeKind.Utc)
        {
            throw new FixSessionException("The administrative message clock returned a non-UTC value.");
        }
        return now;
    }

    internal static bool IsSupportedBeginString(string beginString)
    {
        return beginString == FixBeginStrings.Fix40 || beginString == FixBeginStrings.Fix41 || beginString == FixBeginStrings.Fix42 || beginString == FixBeginStrings.Fix43 || beginString == FixBeginStrings.Fix44 || beginString == FixBeginStrings.Fixt11;
    }
}
