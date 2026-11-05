using System;

namespace tftp;

internal static class TftpClientDatagramDecoder
{
    internal static TftpPacketDecodeResult Decode(
        TftpPacketCodec codec,
        ReadOnlySpan<byte> source,
        bool exceededMaximumLength)
    {
        if (exceededMaximumLength)
        {
            TftpPacketDecodeResult failure = new TftpPacketDecodeResult();
            failure.Success = false;
            failure.FailureMessage =
                "The received TFTP datagram exceeds the maximum packet size.";
            return failure;
        }

        return codec.TryDecode(
            source,
            TftpProtocolValues.MaximumBlockSize);
    }
}
