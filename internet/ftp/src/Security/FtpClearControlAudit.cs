using System;

namespace ftp;

internal static class FtpClearControlAudit
{
    internal static FtpEvent CreateDowngradeEvent(
        FtpCommand command, FtpReply reply, FtpSequenceState state)
    {
        FtpEvent eventData = CreateEvent(command, reply, state);
        eventData.Kind = FtpEventKind.Tls;
        eventData.Message =
            "FTP control-channel TLS was cleared by an accepted CCC downgrade.";
        return eventData;
    }

    internal static FtpEvent CreatePolicyRejectionEvent(
        FtpCommand command, FtpReply? reply, FtpSequenceState state)
    {
        FtpEvent eventData = CreateEvent(command, reply, state);
        eventData.Kind = FtpEventKind.PolicyRejection;
        eventData.Message =
            "CCC was rejected because clear control-channel operation was not enabled.";
        return eventData;
    }

    internal static bool RecordEvent(IFtpEventSink? eventSink, FtpEvent eventData)
    {
        return FtpEventPipeline.RecordEvent(eventSink, eventData);
    }

    internal static FtpEvent CreateEvent(
        FtpCommand command, FtpReply? reply, FtpSequenceState state)
    {
        FtpEvent eventData = new FtpEvent();
        eventData.HasCommandKind = true;
        eventData.CommandKind = FtpCommandKind.Ccc;
        eventData.HasDataProtectionLevel = true;
        eventData.DataProtectionLevel = state.DataProtectionLevel;
        eventData.HasSessionState = true;
        eventData.SessionState = state.SessionState;
        eventData.Command = command;
        eventData.Reply = reply;
        eventData.Redacted = true;
        eventData.Timestamp.HasValue = true;
        eventData.Timestamp.ValueUtc = DateTime.UtcNow;
        return eventData;
    }
}
