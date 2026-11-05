namespace icc;

internal class PngChunkRegion
{
    internal int Start;
    internal int End;
    internal int DataOffset;
    internal int DataLength;
    internal uint Type;
}

internal class PngProfileData
{
    internal bool IsValid;
    internal bool HasProfile;
    internal bool HasColorConflict;
    internal bool HasSrgb;
    internal bool HasCicp;
    internal IccOperationStatus Status;
    internal string Message;
    internal string ProfileName;
    internal int ErrorOffset;
    internal int ErrorLength;
    internal int InsertionOffset;
    internal int ColorType;
    internal PngChunkRegion? ProfileChunk;
    internal byte[] ProfileBytes;

    internal PngProfileData()
    {
        IsValid = false;
        HasProfile = false;
        HasColorConflict = false;
        HasSrgb = false;
        HasCicp = false;
        Status = IccOperationStatus.InvalidData;
        Message = string.Empty;
        ProfileName = string.Empty;
        ErrorOffset = 0;
        ErrorLength = 0;
        InsertionOffset = 8;
        ColorType = -1;
        ProfileChunk = null;
        ProfileBytes = new byte[0];
    }
}
