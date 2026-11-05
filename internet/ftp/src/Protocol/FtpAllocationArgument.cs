namespace ftp;

internal class FtpAllocationArgument
{
    internal long ByteCount;
    internal bool HasRecordSize;
    internal long RecordSize;

    internal FtpAllocationArgument()
    {
        ByteCount = 0;
        HasRecordSize = false;
        RecordSize = 0;
    }
}
