namespace raw;

/// <summary>Records black- and white-clipped normalized sample locations.</summary>
public class RawClippingMask
{
    /// <summary>The stored image width.</summary>
    public int Width;
    /// <summary>The stored image height.</summary>
    public int Height;
    /// <summary>The number of pixel-interleaved logical channels.</summary>
    public int ChannelCount;
    /// <summary>The distance between mask row starts in samples.</summary>
    public int RowStrideSamples;
    /// <summary>One byte per sample, set to one when the linearized value is at or below black.</summary>
    public byte[] BlackClipped;
    /// <summary>One byte per sample, set to one when the linearized value is at or above white.</summary>
    public byte[] WhiteClipped;

    /// <summary>Creates paired clipping masks for complete stored image geometry.</summary>
    /// <param name="width">The image width.</param>
    /// <param name="height">The image height.</param>
    /// <param name="channelCount">The logical channel count.</param>
    /// <param name="rowStrideSamples">The mask row stride.</param>
    /// <param name="blackClipped">The black-clipped sample bytes.</param>
    /// <param name="whiteClipped">The white-clipped sample bytes.</param>
    public RawClippingMask(int width, int height, int channelCount, int rowStrideSamples, byte[] blackClipped, byte[] whiteClipped)
    {
        Width = width;
        Height = height;
        ChannelCount = channelCount;
        RowStrideSamples = rowStrideSamples;
        BlackClipped = blackClipped == null ? System.Array.Empty<byte>() : blackClipped;
        WhiteClipped = whiteClipped == null ? System.Array.Empty<byte>() : whiteClipped;
    }

    /// <summary>Returns whether one stored sample is black clipped.</summary>
    /// <param name="x">The stored column.</param>
    /// <param name="y">The stored row.</param>
    /// <param name="channel">The logical channel.</param>
    /// <returns><c>true</c> when the sample is at or below its black level.</returns>
    /// <exception cref="RawFormatException">The mask or coordinate is invalid.</exception>
    internal bool IsBlackClipped(int x, int y, int channel)
    {
        int index = GetIndex(x, y, channel);
        return BlackClipped[index] != 0;
    }

    /// <summary>Returns whether one stored sample is white clipped.</summary>
    /// <param name="x">The stored column.</param>
    /// <param name="y">The stored row.</param>
    /// <param name="channel">The logical channel.</param>
    /// <returns><c>true</c> when the sample is at or above its white level.</returns>
    /// <exception cref="RawFormatException">The mask or coordinate is invalid.</exception>
    internal bool IsWhiteClipped(int x, int y, int channel)
    {
        int index = GetIndex(x, y, channel);
        return WhiteClipped[index] != 0;
    }

    /// <summary>Validates mask geometry and storage.</summary>
    /// <returns>All clipping-mask validation findings.</returns>
    internal RawValidationReport Validate()
    {
        System.Collections.Generic.List<RawValidationMessage> messages = new System.Collections.Generic.List<RawValidationMessage>();
        long required = RawImageBufferValidation.RequiredSamples(Width, Height, ChannelCount, RowStrideSamples);
        if (required < 0 || BlackClipped == null || WhiteClipped == null || BlackClipped.LongLength < required || WhiteClipped.LongLength < required)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidLength, "ClippingMask.Samples", "Both clipping masks must contain the complete pixel-interleaved image geometry.");
        }
        else
        {
            int y = 0;
            while (y < Height)
            {
                int index = y * RowStrideSamples;
                int end = index + Width * ChannelCount;
                while (index < end)
                {
                    if (BlackClipped[index] > 1 || WhiteClipped[index] > 1)
                    {
                        RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "ClippingMask.Values", "Clipping-mask samples must be zero or one.");
                        return new RawValidationReport(true, messages.ToArray());
                    }
                    index++;
                }
                y++;
            }
        }
        return new RawValidationReport(true, messages.ToArray());
    }

    internal int GetIndex(int x, int y, int channel)
    {
        long required = RawImageBufferValidation.RequiredSamples(Width, Height, ChannelCount, RowStrideSamples);
        if (required < 0 || BlackClipped == null || WhiteClipped == null || BlackClipped.LongLength < required || WhiteClipped.LongLength < required || x < 0 || y < 0 || channel < 0 || x >= Width || y >= Height || channel >= ChannelCount)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Valid clipping-mask coordinates are required.", -1);
        }
        return y * RowStrideSamples + x * ChannelCount + channel;
    }
}
