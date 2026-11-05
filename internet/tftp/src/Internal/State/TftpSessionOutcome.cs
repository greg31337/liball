using System.Collections.Generic;

namespace tftp;

internal class TftpSessionOutcome
{
    internal bool Succeeded = false;
    internal TftpTransferState TerminalState = TftpTransferState.Failed;
    internal long BytesTransferred = 0;
    internal long EncodedBytesTransferred = 0;
    internal int RetransmissionCount = 0;
    internal int DuplicatePacketCount = 0;
    internal List<TftpOption> NegotiatedOptions = [];
    internal bool HasError = false;
    internal TftpErrorCode ErrorCode = TftpErrorCode.Undefined;
    internal string Message = "";
}
