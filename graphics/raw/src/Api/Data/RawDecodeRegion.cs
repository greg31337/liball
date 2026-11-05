namespace raw;

/// <summary>Describes a rectangular generic raw decode into a caller-owned sample buffer.</summary>
public class RawDecodeRegion
{
    /// <summary>The first source pixel column.</summary>
    public int X;
    /// <summary>The first source pixel row.</summary>
    public int Y;
    /// <summary>The number of source pixel columns.</summary>
    public int Width;
    /// <summary>The number of source pixel rows.</summary>
    public int Height;
    /// <summary>The first source channel in descriptor order.</summary>
    public int FirstChannel;
    /// <summary>The number of consecutive source channels.</summary>
    public int ChannelCount;
    /// <summary>The first sample index written in the destination buffer.</summary>
    public int DestinationOffset;
    /// <summary>The sample distance between destination row starts.</summary>
    public int DestinationRowStride;

    /// <summary>Creates a rectangular decode request.</summary>
    /// <param name="x">The first source column.</param>
    /// <param name="y">The first source row.</param>
    /// <param name="width">The number of columns.</param>
    /// <param name="height">The number of rows.</param>
    /// <param name="firstChannel">The first source channel.</param>
    /// <param name="channelCount">The number of source channels.</param>
    /// <param name="destinationOffset">The first destination sample index.</param>
    /// <param name="destinationRowStride">The destination row stride in samples.</param>
    public RawDecodeRegion(int x, int y, int width, int height, int firstChannel, int channelCount, int destinationOffset, int destinationRowStride)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        FirstChannel = firstChannel;
        ChannelCount = channelCount;
        DestinationOffset = destinationOffset;
        DestinationRowStride = destinationRowStride;
    }

    /// <summary>Creates a tightly packed request for the complete descriptor image and all channels.</summary>
    /// <param name="descriptor">The source descriptor.</param>
    /// <returns>A whole-image decode request.</returns>
    /// <exception cref="RawFormatException">The descriptor is missing or its output row size overflows.</exception>
    internal static RawDecodeRegion CreateWhole(RawDescriptor descriptor)
    {
        if (descriptor == null || descriptor.Width <= 0 || descriptor.Height <= 0 || descriptor.ChannelCount <= 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A populated descriptor is required for a whole-image region.", -1);
        }
        long stride = RawDescriptor.MultiplyOrNegative(descriptor.Width, descriptor.ChannelCount);
        if (stride < 0 || stride > int.MaxValue)
        {
            throw new RawFormatException(RawDiagnosticCode.ArithmeticOverflow, "The whole-image destination row stride overflows.", -1);
        }
        return new RawDecodeRegion(0, 0, descriptor.Width, descriptor.Height, 0, descriptor.ChannelCount, 0, (int)stride);
    }
}
