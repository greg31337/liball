namespace ftp;

internal class FtpBlockModeBlock
{
    internal int Descriptor;
    internal int Count;
    internal byte[] Data;
    internal byte[] SerializedBytes;
    internal bool EndOfRecord;
    internal bool EndOfFile;
    internal bool SuspectedErrors;
    internal bool IsRestartMarker;
    internal string? RestartMarkerText;
    internal FtpStructureFrame? Frame;

    internal FtpBlockModeBlock()
    {
        Descriptor = 0;
        Count = 0;
        Data = new byte[0];
        SerializedBytes = new byte[0];
        EndOfRecord = false;
        EndOfFile = false;
        SuspectedErrors = false;
        IsRestartMarker = false;
        RestartMarkerText = null;
        Frame = null;
    }
}
