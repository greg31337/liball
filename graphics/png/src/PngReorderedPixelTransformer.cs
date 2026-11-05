namespace png;

internal class PngReorderedPixelTransformer
{
    internal static bool IsReordered(PngPixelFormat format)
    {
        return format == PngPixelFormat.Bgr8
            || format == PngPixelFormat.Bgr16
            || format == PngPixelFormat.Bgra8
            || format == PngPixelFormat.Bgra16
            || format == PngPixelFormat.Argb8
            || format == PngPixelFormat.Argb16;
    }

    internal static PngPixelBuffer Normalize(PngPixelBuffer buffer)
    {
        return PngEncoder.NormalizeChannelOrder(buffer);
    }

    internal static PngPixelBuffer Restore(PngPixelFormat sourceFormat, PngPixelBuffer buffer)
    {
        if (sourceFormat == PngPixelFormat.Bgr8
            || sourceFormat == PngPixelFormat.Bgr16
            || sourceFormat == PngPixelFormat.Bgra8
            || sourceFormat == PngPixelFormat.Bgra16)
        {
            if (buffer.Format == PngPixelFormat.Rgb8
                || buffer.Format == PngPixelFormat.Rgb16
                || buffer.Format == PngPixelFormat.Rgba8
                || buffer.Format == PngPixelFormat.Rgba16)
            {
                return PngChannelTransformer.SwapRgbBgr(buffer);
            }
        }

        if (sourceFormat == PngPixelFormat.Argb8 || sourceFormat == PngPixelFormat.Argb16)
        {
            if (buffer.Format == PngPixelFormat.Rgba8 || buffer.Format == PngPixelFormat.Rgba16)
            {
                return PngChannelTransformer.SwapRgbaArgb(buffer);
            }
        }

        return buffer;
    }
}
