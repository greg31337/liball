namespace png;

internal class PngEncodingBailout
{
    internal static bool RecordBytes(PngEncoderOptions options, long byteCount)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (byteCount < 0 || options.ProducedByteCount > long.MaxValue - byteCount)
        {
            throw new OverflowException();
        }

        options.ProducedByteCount += byteCount;
        if (options.EarlyBailoutByteCount > 0 && options.ProducedByteCount > options.EarlyBailoutByteCount)
        {
            options.WasEarlyBailedOut = true;
        }

        return options.WasEarlyBailedOut;
    }

    internal static void ResetByteCount(PngEncoderOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.ProducedByteCount = 0;
    }
}
