using System.Collections.Generic;

namespace fix;

internal class LogonProcessingResult
{
    internal bool Accepted;
    internal bool Disconnect;
    internal bool SendLogout;
    internal bool ResetSequenceNumbers;
    internal int HeartbeatIntervalSeconds;
    internal string DefaultApplicationVersion = "";
    internal bool HasNextExpectedMsgSeqNum;
    internal int NextExpectedMsgSeqNum;
    internal string Code = "";
    internal string Text = "";
    internal FixDictionaryIdentity ApplicationIdentity = new FixDictionaryIdentity();
    internal FixDictionary? ApplicationDictionary;
    internal List<ValidationIssue> Issues = [];
}
