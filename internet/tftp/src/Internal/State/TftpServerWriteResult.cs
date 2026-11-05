using System.Collections.Generic;

namespace tftp;

internal class TftpServerWriteResult
{
    internal TftpServerWriteAction Action = TftpServerWriteAction.Wait;
    internal TftpPacket? Packet = null;
    internal long WaitMilliseconds = 0;
    internal bool IsFinalData = false;
    internal bool IsDallying = false;
    internal bool StorageCommitted = false;
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
