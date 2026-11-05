using System;

namespace fix;

internal static class StoredMessageValidation
{
    internal static string ValidateStoredMessage(StoredMessage message)
    {
        if (message == null) return "missing-stored-message";
        if (message.SequenceNumber < FixDefaults.InitialSequenceNumber) return "invalid-message-sequence-number";
        if (message.Bytes == null || message.Bytes.Length == 0) return "missing-stored-message-bytes";
        if (message.SendingTimeUtc.Kind != DateTimeKind.Utc) return "invalid-stored-message-time";
        if (message.MsgType == null || message.MsgType.Length == 0) return "missing-stored-message-type";
        return FileStoreRecordFormat.ValidateAppendMessage(message);
    }
}
