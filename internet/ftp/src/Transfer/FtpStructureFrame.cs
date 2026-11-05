namespace ftp;

internal class FtpStructureFrame
{
    internal byte[]? Data;
    internal int Count;
    internal bool EndOfRecord;
    internal bool EndOfFile;
    internal bool RequiresConnectionClose;
    internal bool RequiresStreamEndOfFileMarker;
    internal bool RequiresEndOfFileDescriptor;

    internal FtpStructureFrame()
    {
        Data = null;
        Count = 0;
        EndOfRecord = false;
        EndOfFile = false;
        RequiresConnectionClose = false;
        RequiresStreamEndOfFileMarker = false;
        RequiresEndOfFileDescriptor = false;
    }
}
