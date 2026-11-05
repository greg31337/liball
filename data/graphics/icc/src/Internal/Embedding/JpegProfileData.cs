namespace icc;

internal class JpegIccSegment
{
    internal int Start;
    internal int End;
    internal int PayloadOffset;
    internal int PayloadLength;
    internal int SequenceNumber;
    internal int SegmentCount;
}

internal class JpegProfileData
{
    internal bool IsValid;
    internal bool HasProfile;
    internal IccOperationStatus Status;
    internal string Message;
    internal int ErrorOffset;
    internal int ErrorLength;
    internal int InsertionOffset;
    internal JpegIccSegment[] Segments;
    internal byte[] ProfileBytes;

    internal JpegProfileData()
    {
        IsValid = false;
        HasProfile = false;
        Status = IccOperationStatus.InvalidData;
        Message = string.Empty;
        ErrorOffset = 0;
        ErrorLength = 0;
        InsertionOffset = 2;
        Segments = new JpegIccSegment[0];
        ProfileBytes = new byte[0];
    }
}
