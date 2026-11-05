using System.Collections.Generic;
using System.Net;

namespace tftp;

internal class TftpClientWriteResult
{
    internal TftpClientWriteAction Action = TftpClientWriteAction.Wait;
    internal TftpPacket? Packet = null;
    internal IPEndPoint? TargetEndPoint = null;
    internal long WaitMilliseconds = 0;
    internal bool EnteredSender = false;
    internal bool RetriedWithoutOptions = false;
    internal bool IsFinalData = false;
    internal int EffectiveBlockSize = TftpProtocolValues.DefaultBlockSize;
    internal int EffectiveTimeoutSeconds = TftpProtocolValues.MinimumTimeoutSeconds;
    internal bool HasSourceSize = false;
    internal long SourceSize = 0;
    internal bool HasEncodedSourceSize = false;
    internal long EncodedSourceSize = 0;
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
