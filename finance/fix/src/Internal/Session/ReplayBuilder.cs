using System;
using System.Globalization;
using System.Text;

namespace fix;

internal class ReplayBuilder
{
    internal SessionId Id;
    internal FixDictionary Dictionary;
    internal IMessageDecoder Decoder;
    internal IMessageEncoder Encoder;
    internal IMessageOperations Operations;
    internal ISessionClock Clock;

    internal ReplayBuilder(SessionId id, FixDictionary dictionary, IMessageDecoder decoder, IMessageEncoder encoder, IMessageOperations operations, ISessionClock clock)
    {
        if (id == null || dictionary == null || decoder == null || encoder == null || operations == null || clock == null)
        {
            throw new FixSessionException("Replay building dependencies are missing.");
        }
        if (id.BeginString.Length == 0 || id.SenderCompID.Length == 0 || id.TargetCompID.Length == 0)
        {
            throw new FixSessionException("The replay session identity is incomplete.");
        }
        Id = LogonProcessor.CopySessionId(id);
        Dictionary = dictionary;
        Decoder = decoder;
        Encoder = encoder;
        Operations = operations;
        Clock = clock;
    }

    internal ReplayMessageResult PrepareApplicationReplay(StoredMessage stored)
    {
        if (stored == null || stored.Bytes == null || stored.Bytes.Length == 0 || stored.SequenceNumber < FixDefaults.InitialSequenceNumber)
        {
            return CreateFailure("invalid-stored-replay-message", "The stored application message is invalid.");
        }
        DecodeResult decoded = Decoder.DecodeMessage(stored.Bytes, Dictionary, new DecodeOptions());
        if (decoded.Status != DecodeStatus.Success || decoded.Message == null)
        {
            string code = decoded.ErrorCode.Length == 0 ? "stored-replay-decode-failed" : decoded.ErrorCode;
            string text = decoded.ErrorMessage.Length == 0 ? "The stored application message could not be decoded." : decoded.ErrorMessage;
            return CreateFailure(code, text);
        }
        FixMessage message = decoded.Message;
        int decodedSequenceNumber = ReadSequenceNumber(message);
        if (decodedSequenceNumber != stored.SequenceNumber || message.MsgType != stored.MsgType)
        {
            return CreateFailure("stored-replay-metadata-mismatch", "The stored message metadata does not match its encoded header.");
        }
        byte[] originalSendingTime = ReadOriginalSendingTime(message, stored.SendingTimeUtc);
        if (!SetFieldValue(message, FixTags.MsgSeqNum, stored.SequenceNumber.ToString(CultureInfo.InvariantCulture)) || !SetFieldValue(message, FixTags.PossDupFlag, "Y") || !SetFieldBytes(message, FixTags.OrigSendingTime, originalSendingTime))
        {
            return CreateFailure("replay-header-edit-failed", "The stored application replay header could not be prepared.");
        }
        ReplayMessageResult result = new ReplayMessageResult();
        result.Success = true;
        result.BeginSeqNo = stored.SequenceNumber;
        result.EndSeqNo = stored.SequenceNumber;
        result.OriginalSequenceNumber = stored.SequenceNumber;
        result.MsgType = stored.MsgType;
        result.Message = message;
        result.OriginalSendingTimeValue = CopyBytes(originalSendingTime);
        result.Code = "application-replay-prepared";
        result.Text = "The stored application message is ready for replay review.";
        return result;
    }

    internal ReplayMessageResult EncodeApplicationReplay(ReplayMessageResult prepared)
    {
        if (prepared == null || !prepared.Success || prepared.Message == null || prepared.IsGapFill || prepared.OriginalSequenceNumber < FixDefaults.InitialSequenceNumber || prepared.OriginalSendingTimeValue.Length == 0)
        {
            return CreateFailure("invalid-prepared-replay", "The prepared application replay is invalid.");
        }
        DateTime now = ReadUtcNow();
        FixMessage message = prepared.Message;
        if (!SetFieldValue(message, FixTags.MsgSeqNum, prepared.OriginalSequenceNumber.ToString(CultureInfo.InvariantCulture)) || !SetFieldValue(message, FixTags.PossDupFlag, "Y") || !SetFieldBytes(message, FixTags.OrigSendingTime, prepared.OriginalSendingTimeValue) || !SetFieldValue(message, FixTags.SendingTime, LogonProcessor.FormatSendingTime(now)))
        {
            return CreateFailure("replay-header-edit-failed", "The application replay header could not be finalized.");
        }
        ReplayMessageResult encoded = EncodeReplayMessage(message, false, prepared.BeginSeqNo, prepared.EndSeqNo, prepared.OriginalSequenceNumber, prepared.MsgType);
        encoded.OriginalSendingTimeValue = CopyBytes(prepared.OriginalSendingTimeValue);
        encoded.ApplicationDecision = prepared.ApplicationDecision;
        if (encoded.Success)
        {
            encoded.Code = "application-message-replayed";
            encoded.Text = "The stored application message was rewritten for replay.";
        }
        return encoded;
    }

    internal ReplayMessageResult CreateGapFillReplay(int beginSeqNo, int endSeqNo)
    {
        if (beginSeqNo < FixDefaults.InitialSequenceNumber || endSeqNo < beginSeqNo || endSeqNo == int.MaxValue)
        {
            return CreateFailure("invalid-gap-fill-range", "The replay GapFill range is invalid.");
        }
        DateTime now = ReadUtcNow();
        FixMessage message = new FixMessage();
        message.BeginString = Id.BeginString;
        message.MsgType = FixAdminMessageTypes.SequenceReset;
        LogonProcessor.AddHeaderText(message, FixTags.BeginString, Id.BeginString);
        LogonProcessor.AddHeaderText(message, FixTags.MsgType, FixAdminMessageTypes.SequenceReset);
        LogonProcessor.AddHeaderInteger(message, FixTags.MsgSeqNum, beginSeqNo);
        LogonProcessor.AddHeaderText(message, FixTags.SenderCompID, Id.SenderCompID);
        LogonProcessor.AddOptionalHeaderText(message, FixTags.SenderSubID, Id.SenderSubID);
        LogonProcessor.AddOptionalHeaderText(message, FixTags.SenderLocationID, Id.SenderLocationID);
        LogonProcessor.AddHeaderText(message, FixTags.TargetCompID, Id.TargetCompID);
        LogonProcessor.AddOptionalHeaderText(message, FixTags.TargetSubID, Id.TargetSubID);
        LogonProcessor.AddOptionalHeaderText(message, FixTags.TargetLocationID, Id.TargetLocationID);
        string sendingTime = LogonProcessor.FormatSendingTime(now);
        LogonProcessor.AddHeaderText(message, FixTags.SendingTime, sendingTime);
        LogonProcessor.AddHeaderText(message, FixTags.PossDupFlag, "Y");
        LogonProcessor.AddHeaderText(message, FixTags.OrigSendingTime, sendingTime);
        LogonProcessor.AddBodyText(message, FixTags.GapFillFlag, "Y");
        LogonProcessor.AddBodyInteger(message, FixTags.NewSeqNo, endSeqNo + 1);
        ReplayMessageResult result = EncodeReplayMessage(message, true, beginSeqNo, endSeqNo, beginSeqNo, FixAdminMessageTypes.SequenceReset);
        if (result.Success)
        {
            result.Code = "replay-gap-fill-created";
            result.Text = "A SequenceReset-GapFill was created for skipped replay sequences.";
        }
        return result;
    }

    internal ReplayMessageResult EncodeReplayMessage(FixMessage message, bool gapFill, int beginSeqNo, int endSeqNo, int originalSequenceNumber, string msgType)
    {
        EncodeOptions options = new EncodeOptions();
        options.PreserveFieldOrder = false;
        EncodeResult measured = Encoder.EncodeMessage(message, Dictionary, Span<byte>.Empty, options);
        if (measured.RequiredSize <= 0 || measured.Status == EncodeStatus.Invalid || measured.Status == EncodeStatus.LimitExceeded)
        {
            string code = measured.ErrorCode.Length == 0 ? "replay-encode-failed" : measured.ErrorCode;
            string text = measured.ErrorMessage.Length == 0 ? "The replay message could not be measured." : measured.ErrorMessage;
            return CreateFailure(code, text);
        }
        byte[] bytes = new byte[measured.RequiredSize];
        EncodeResult encoded = Encoder.EncodeMessage(message, Dictionary, bytes, options);
        if (encoded.Status != EncodeStatus.Success || encoded.BytesWritten != bytes.Length)
        {
            string code = encoded.ErrorCode.Length == 0 ? "replay-encode-failed" : encoded.ErrorCode;
            string text = encoded.ErrorMessage.Length == 0 ? "The replay message could not be encoded." : encoded.ErrorMessage;
            return CreateFailure(code, text);
        }
        ReplayMessageResult result = new ReplayMessageResult();
        result.Success = true;
        result.IsGapFill = gapFill;
        result.BeginSeqNo = beginSeqNo;
        result.EndSeqNo = endSeqNo;
        result.OriginalSequenceNumber = originalSequenceNumber;
        result.MsgType = msgType;
        result.Message = message;
        result.Bytes = bytes;
        return result;
    }

    internal bool SetFieldValue(FixMessage message, int tag, string value)
    {
        return SetFieldBytes(message, tag, Encoding.Latin1.GetBytes(value));
    }

    internal bool SetFieldBytes(FixMessage message, int tag, byte[] value)
    {
        FixField field = new FixField();
        field.Tag = tag;
        field.Value = CopyBytes(value);
        MessageEditResult edited = Operations.SetField(message, field);
        return edited != null && edited.Success;
    }

    internal static byte[] ReadOriginalSendingTime(FixMessage message, DateTime storedSendingTimeUtc)
    {
        FixField? original = EnvelopeValidator.FindFirstMessageField(message, FixTags.OrigSendingTime);
        if (original != null && original.Value.Length != 0)
        {
            return CopyBytes(original.Value);
        }
        FixField? sending = EnvelopeValidator.FindFirstMessageField(message, FixTags.SendingTime);
        if (sending != null && sending.Value.Length != 0)
        {
            return CopyBytes(sending.Value);
        }
        if (storedSendingTimeUtc.Kind == DateTimeKind.Utc)
        {
            return Encoding.Latin1.GetBytes(LogonProcessor.FormatSendingTime(storedSendingTimeUtc));
        }
        return [];
    }

    internal static int ReadSequenceNumber(FixMessage message)
    {
        FixField? field = EnvelopeValidator.FindFirstMessageField(message, FixTags.MsgSeqNum);
        if (field == null)
        {
            return 0;
        }
        ScalarParseResult parsed = ScalarParser.ParseScalar(field.Value, FixFieldType.SeqNum, FixDefaults.MaxFieldSize);
        if (!parsed.Success)
        {
            return 0;
        }
        return parsed.IntegerValue;
    }

    internal DateTime ReadUtcNow()
    {
        DateTime now = Clock.GetUtcNow();
        if (now.Kind != DateTimeKind.Utc)
        {
            throw new FixSessionException("The replay clock returned a non-UTC value.");
        }
        return now;
    }

    internal static byte[] CopyBytes(byte[] source)
    {
        byte[] copy = new byte[source.Length];
        for (int index = 0; index < source.Length; index++)
        {
            copy[index] = source[index];
        }
        return copy;
    }

    internal static ReplayMessageResult CreateFailure(string code, string text)
    {
        ReplayMessageResult result = new ReplayMessageResult();
        result.Code = code;
        result.Text = text;
        return result;
    }
}
