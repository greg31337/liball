namespace icc;

internal class WebPChunkRegion
{
    internal int Start;
    internal int End;
    internal int DataOffset;
    internal int DataLength;
    internal uint Type;

    internal WebPChunkRegion()
    {
        Start = 0;
        End = 0;
        DataOffset = 0;
        DataLength = 0;
        Type = 0;
    }
}

internal class WebPProfileData
{
    internal bool IsValid;
    internal bool HasProfile;
    internal bool HasExtendedHeader;
    internal bool IccFlagSet;
    internal IccOperationStatus Status;
    internal string Message;
    internal int ErrorOffset;
    internal int ErrorLength;
    internal WebPChunkRegion? ProfileChunk;
    internal WebPChunkRegion? ExtendedHeaderChunk;
    internal WebPChunkRegion? ImageChunk;

    internal WebPProfileData()
    {
        IsValid = false;
        HasProfile = false;
        HasExtendedHeader = false;
        IccFlagSet = false;
        Status = IccOperationStatus.InvalidData;
        Message = string.Empty;
        ErrorOffset = 0;
        ErrorLength = 0;
        ProfileChunk = null;
        ExtendedHeaderChunk = null;
        ImageChunk = null;
    }
}

internal class WebPCanvasSize
{
    internal int Width;
    internal int Height;

    internal WebPCanvasSize()
    {
        Width = 0;
        Height = 0;
    }
}
