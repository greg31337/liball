using System.Collections.Generic;

namespace fix;

internal class LogonCandidate
{
    internal int HeartbeatIntervalSeconds;
    internal bool ResetSequenceNumbers;
    internal bool HasNextExpectedMsgSeqNum;
    internal int NextExpectedMsgSeqNum;
    internal string Username = "";
    internal string Password = "";
    internal byte[] RawData = [];
    internal string DefaultApplicationVersion = "";
    internal FixDictionaryIdentity ApplicationIdentity = new FixDictionaryIdentity();
    internal FixDictionary? ApplicationDictionary;
    internal List<ValidationIssue> Issues = [];
}
