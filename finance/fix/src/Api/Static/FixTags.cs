namespace fix;

/// <summary>Defines core FIX field tag numbers used by wire and session processing.</summary>
public static class FixTags
{
    /// <summary>The first sequence number requested by a resend request.</summary>
    public const int BeginSeqNo = 7;
    /// <summary>The FIX session profile identifier.</summary>
    public const int BeginString = 8;
    /// <summary>The encoded byte length of the message body.</summary>
    public const int BodyLength = 9;
    /// <summary>The modulo-256 message checksum.</summary>
    public const int CheckSum = 10;
    /// <summary>The final sequence number requested by a resend request.</summary>
    public const int EndSeqNo = 16;
    /// <summary>The sender's message sequence number.</summary>
    public const int MsgSeqNum = 34;
    /// <summary>The message type identifier.</summary>
    public const int MsgType = 35;
    /// <summary>The next valid sequence number after a sequence reset.</summary>
    public const int NewSeqNo = 36;
    /// <summary>Indicates that a message may duplicate an earlier transmission.</summary>
    public const int PossDupFlag = 43;
    /// <summary>The sequence number referenced by a reject.</summary>
    public const int RefSeqNum = 45;
    /// <summary>The sender's firm or system identifier.</summary>
    public const int SenderCompID = 49;
    /// <summary>The sender's subsystem identifier.</summary>
    public const int SenderSubID = 50;
    /// <summary>The time at which the message was sent.</summary>
    public const int SendingTime = 52;
    /// <summary>The target's firm or system identifier.</summary>
    public const int TargetCompID = 56;
    /// <summary>The target's subsystem identifier.</summary>
    public const int TargetSubID = 57;
    /// <summary>Human-readable diagnostic text.</summary>
    public const int Text = 58;
    /// <summary>The length of the secure data field.</summary>
    public const int SecureDataLength = 90;
    /// <summary>Authentication data protected by the negotiated mechanism.</summary>
    public const int SecureData = 91;
    /// <summary>The length of the raw data field.</summary>
    public const int RawDataLength = 95;
    /// <summary>An arbitrary raw byte value.</summary>
    public const int RawData = 96;
    /// <summary>Indicates that a message is being resent without duplicate handling.</summary>
    public const int PossResend = 97;
    /// <summary>The logon encryption method.</summary>
    public const int EncryptMethod = 98;
    /// <summary>The negotiated heartbeat interval in seconds.</summary>
    public const int HeartBtInt = 108;
    /// <summary>The identifier echoed by a test-request heartbeat.</summary>
    public const int TestReqID = 112;
    /// <summary>The firm or system represented by the sender.</summary>
    public const int OnBehalfOfCompID = 115;
    /// <summary>The subsystem represented by the sender.</summary>
    public const int OnBehalfOfSubID = 116;
    /// <summary>The original transmission time of a possible duplicate.</summary>
    public const int OrigSendingTime = 122;
    /// <summary>Indicates that a sequence reset is a gap fill.</summary>
    public const int GapFillFlag = 123;
    /// <summary>The firm or system to which the message is delivered.</summary>
    public const int DeliverToCompID = 128;
    /// <summary>The subsystem to which the message is delivered.</summary>
    public const int DeliverToSubID = 129;
    /// <summary>Requests sequence-number reset during logon.</summary>
    public const int ResetSeqNumFlag = 141;
    /// <summary>The sender's location identifier.</summary>
    public const int SenderLocationID = 142;
    /// <summary>The target's location identifier.</summary>
    public const int TargetLocationID = 143;
    /// <summary>The location represented by the sender.</summary>
    public const int OnBehalfOfLocationID = 144;
    /// <summary>The location to which the message is delivered.</summary>
    public const int DeliverToLocationID = 145;
    /// <summary>The most recent inbound sequence number processed by the sender.</summary>
    public const int LastMsgSeqNumProcessed = 369;
    /// <summary>The tag number referenced by a reject.</summary>
    public const int RefTagID = 371;
    /// <summary>The message type referenced by a reject.</summary>
    public const int RefMsgType = 372;
    /// <summary>The session reject reason code.</summary>
    public const int SessionRejectReason = 373;
    /// <summary>The business identifier referenced by a business reject.</summary>
    public const int BusinessRejectRefID = 379;
    /// <summary>The business reject reason code.</summary>
    public const int BusinessRejectReason = 380;
    /// <summary>The maximum inbound message size advertised at logon.</summary>
    public const int MaxMessageSize = 383;
    /// <summary>The number of per-message-type logon rules.</summary>
    public const int NoMsgTypes = 384;
    /// <summary>The direction covered by a per-message-type logon rule.</summary>
    public const int MsgDirection = 385;
    /// <summary>The logon username.</summary>
    public const int Username = 553;
    /// <summary>The logon password.</summary>
    public const int Password = 554;
    /// <summary>The number of hop records in the standard header.</summary>
    public const int NoHops = 627;
    /// <summary>The firm or system identifier for a hop.</summary>
    public const int HopCompID = 628;
    /// <summary>The sending time recorded for a hop.</summary>
    public const int HopSendingTime = 629;
    /// <summary>The reference identifier for a hop.</summary>
    public const int HopRefID = 630;
    /// <summary>The next inbound sequence number expected by the sender.</summary>
    public const int NextExpectedMsgSeqNum = 789;
    /// <summary>The application version selected for one message.</summary>
    public const int ApplVerID = 1128;
    /// <summary>A bilateral application version selected for one message.</summary>
    public const int CstmApplVerID = 1129;
    /// <summary>The application version referenced by a reject or logon rule.</summary>
    public const int RefApplVerID = 1130;
    /// <summary>The bilateral application version referenced by a reject or logon rule.</summary>
    public const int RefCstmApplVerID = 1131;
    /// <summary>The session's default application version.</summary>
    public const int DefaultApplVerID = 1137;
    /// <summary>The application extension pack selected for one message.</summary>
    public const int ApplExtID = 1156;
    /// <summary>The application extension pack referenced by a reject or logon rule.</summary>
    public const int RefApplExtID = 1406;
    /// <summary>The session's default bilateral application version.</summary>
    public const int DefaultCstmApplVerID = 1408;
}
