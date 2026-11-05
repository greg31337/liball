namespace ftp;

internal class FtpLocalByteBitWriter
{
    internal FtpTransferOutputBuffer Output;
    internal byte CurrentByte;
    internal int BitCount;

    internal FtpLocalByteBitWriter(FtpTransferOutputBuffer output)
    {
        Output = output;
        CurrentByte = 0;
        BitCount = 0;
    }

    internal void WriteBit(int value)
    {
        if (value != 0)
        {
            CurrentByte = (byte)(CurrentByte | (1 << (7 - BitCount)));
        }

        BitCount++;
        if (BitCount == 8)
        {
            Output.WriteByte(CurrentByte);
            CurrentByte = 0;
            BitCount = 0;
        }
    }

    internal void CompleteBits()
    {
        if (BitCount == 0)
        {
            return;
        }

        Output.WriteByte(CurrentByte);
        CurrentByte = 0;
        BitCount = 0;
    }
}
