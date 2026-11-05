namespace ftp;

internal class FtpRestartCheckpoint
{
    internal long LogicalByteOffset;
    internal long WireByteOffset;
    internal long StructureUnitCount;
    internal int PendingBitCount;
    internal bool PendingCarriageReturn;

    internal FtpRestartCheckpoint()
    {
        LogicalByteOffset = 0;
        WireByteOffset = 0;
        StructureUnitCount = 0;
        PendingBitCount = 0;
        PendingCarriageReturn = false;
    }
}
