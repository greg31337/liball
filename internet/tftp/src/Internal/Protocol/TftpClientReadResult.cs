using System.Collections.Generic;
using System.Net;

namespace tftp;

internal class TftpClientReadResult
{
    internal TftpClientReadAction Action = TftpClientReadAction.Wait;
    internal TftpPacket? Packet = null;
    internal IPEndPoint? TargetEndPoint = null;
    internal long WaitMilliseconds = 0;
    internal bool EnteredReceiver = false;
    internal bool RetriedWithoutOptions = false;
    internal bool IsFinalData = false;
    internal bool IsDallying = false;
    internal int EffectiveBlockSize = TftpProtocolValues.DefaultBlockSize;
    internal int EffectiveTimeoutSeconds = TftpProtocolValues.MinimumTimeoutSeconds;
    internal bool HasTransferSize = false;
    internal long TransferSize = 0;
    internal List<TftpOption> NegotiatedOptions = [];
    internal long BytesTransferred = 0;
    internal long EncodedBytesTransferred = 0;
    internal int RetransmissionCount = 0;
    internal int DuplicatePacketCount = 0;
    internal bool HasFailure = false;
    internal bool IsOptionFailure = false;
    internal TftpErrorCode FailureErrorCode = TftpErrorCode.Undefined;
    internal string FailureMessage = "";
    internal string FailureOptionName = "";
    internal string FailureOptionValue = "";
}
