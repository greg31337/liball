using System.Collections.Generic;

namespace tftp;

internal class TftpOptionNegotiationResult
{
    internal bool Success = false;
    internal List<TftpOption> Options = [];
    internal int EncodedLength = 0;
    internal int EffectiveBlockSize = TftpProtocolValues.DefaultBlockSize;
    internal int EffectiveTimeoutSeconds = TftpProtocolValues.MinimumTimeoutSeconds;
    internal bool HasTransferSize = false;
    internal long TransferSize = 0;
    internal bool HasError = false;
    internal TftpErrorCode ErrorCode = TftpErrorCode.Undefined;
    internal string FailureMessage = "";
    internal string OptionName = "";
    internal string OptionValue = "";
}
