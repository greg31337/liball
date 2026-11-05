namespace webp;

internal class WebpVp8FrameHeader
{
    internal uint FrameTag;
    internal bool IsKeyFrame;
    internal int Version;
    internal bool ShowFrame;
    internal int FirstPartitionSize;
    internal bool HasValidKeyFrameSignature;
    internal int Width;
    internal int Height;
    internal int HorizontalScale;
    internal int VerticalScale;
    internal int ColorSpace;
    internal int PixelClampingType;

    internal WebpVp8FrameHeader()
    {
        FrameTag = 0;
        IsKeyFrame = false;
        Version = 0;
        ShowFrame = false;
        FirstPartitionSize = 0;
        HasValidKeyFrameSignature = false;
        Width = 0;
        Height = 0;
        HorizontalScale = 0;
        VerticalScale = 0;
        ColorSpace = 0;
        PixelClampingType = 0;
    }
}
