namespace ftp;

internal class FtpEbcdicEncodingState
{
    internal int CodePoint;
    internal int RemainingUtf8Bytes;
    internal int MinimumCodePoint;
    internal bool PendingCarriageReturn;
    internal bool AtRecordStart;
    internal FtpTextFormat Format;

    internal FtpEbcdicEncodingState(FtpTextFormat format)
    {
        CodePoint = 0;
        RemainingUtf8Bytes = 0;
        MinimumCodePoint = 0;
        PendingCarriageReturn = false;
        AtRecordStart = true;
        Format = format;
    }
}
