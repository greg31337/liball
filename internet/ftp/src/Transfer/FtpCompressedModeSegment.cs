namespace ftp;

internal class FtpCompressedModeSegment
{
    internal byte[] Data;
    internal int Count;
    internal byte[] EncodedBytes;
    internal bool EndOfRecord;
    internal bool EndOfFile;
    internal bool SuspectedErrors;
    internal bool IsRestartMarker;
    internal string? RestartMarkerText;
    internal FtpStructureFrame? Frame;

    internal FtpCompressedModeSegment()
    {
        Data = new byte[0];
        Count = 0;
        EncodedBytes = new byte[0];
        EndOfRecord = false;
        EndOfFile = false;
        SuspectedErrors = false;
        IsRestartMarker = false;
        RestartMarkerText = null;
        Frame = null;
    }
}
