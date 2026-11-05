using System;

namespace webp;

internal static class WebpLossyAlphaEncoder
{
    internal static bool HasNonOpaqueAlpha(WebpPixelBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        WebpPixelBufferHelper.ValidateRgbaBuffer(buffer);

        int y = 0;
        while (y < buffer.Height)
        {
            int x = 0;
            while (x < buffer.Width)
            {
                int offset = (y * buffer.Stride) + (x * 4);
                if (buffer.Rgba[offset + 3] != 255)
                {
                    return true;
                }

                x = x + 1;
            }

            y = y + 1;
        }

        return false;
    }

    internal static byte[] ExtractAlpha(WebpPixelBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        WebpPixelBufferHelper.ValidateRgbaBuffer(buffer);

        byte[] alpha = new byte[buffer.Width * buffer.Height];
        int y = 0;
        while (y < buffer.Height)
        {
            int x = 0;
            while (x < buffer.Width)
            {
                int sourceOffset = (y * buffer.Stride) + (x * 4);
                alpha[(y * buffer.Width) + x] = buffer.Rgba[sourceOffset + 3];
                x = x + 1;
            }

            y = y + 1;
        }

        return alpha;
    }

    internal static byte[]? EncodeAlpha(WebpPixelBuffer buffer, WebpEncodeOptions options)
    {
        return EncodeAlpha(buffer, options, EncodeLosslessAlphaStream);
    }

    internal static byte[]? EncodeAlpha(WebpPixelBuffer buffer, WebpEncodeOptions options, Func<WebpPixelBuffer, byte[]> losslessEncoder)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(losslessEncoder);
        WebpLossyEncoderSettings.ValidateOptions(options);
        if (options.LossyAlphaMode != WebpLossyAlphaMode.Preserve)
        {
            return null;
        }

        if (!HasNonOpaqueAlpha(buffer))
        {
            return null;
        }

        byte[] alpha = ExtractAlpha(buffer);
        if (!options.LosslessAlpha)
        {
            alpha = QuantizeAlpha(alpha, options.AlphaQuality);
        }

        byte filterMethod = SelectAlphaFilter(alpha, buffer.Width, buffer.Height, options.AlphaFilter);
        if (options.AlphaCompression == WebpAlphaCompressionMethod.None)
        {
            return WebpAlphaEncoder.EncodeUncompressed(alpha, buffer.Width, buffer.Height, filterMethod);
        }

        return WebpAlphaEncoder.EncodeCompressed(alpha, buffer.Width, buffer.Height, filterMethod, losslessEncoder);
    }

    internal static byte[] EncodeStill(WebpPixelBuffer buffer, WebpEncodeOptions options)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(options);
        WebpLossyEncoderSettings.ValidateOptions(options);

        WebpPixelBuffer colorBuffer = PrepareLossyColorBuffer(buffer, options);
        byte[] vp8Payload = WebpVp8KeyFrameEncoder.Encode(colorBuffer, options);
        byte[]? alphaPayload = EncodeAlpha(buffer, options);
        if (alphaPayload == null)
        {
            return WebpContainerWriter.WriteBasicStill(vp8Payload, WebpBitstreamKind.Lossy);
        }

        return WebpContainerWriter.WriteExtendedStill(vp8Payload, WebpBitstreamKind.Lossy, new WebpMetadata(), alphaPayload);
    }

    internal static WebpPixelBuffer PrepareColorBuffer(WebpPixelBuffer buffer, bool exactTransparentRgb)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        WebpPixelBufferHelper.ValidateRgbaBuffer(buffer);

        WebpPixelBuffer output = WebpPixelBufferHelper.Copy(buffer);
        int y = 0;
        while (y < output.Height)
        {
            int x = 0;
            while (x < output.Width)
            {
                int offset = (y * output.Stride) + (x * 4);
                if (!exactTransparentRgb && output.Rgba[offset + 3] == 0)
                {
                    output.Rgba[offset] = 0;
                    output.Rgba[offset + 1] = 0;
                    output.Rgba[offset + 2] = 0;
                }

                output.Rgba[offset + 3] = 255;
                x = x + 1;
            }

            y = y + 1;
        }

        return output;
    }

    internal static WebpPixelBuffer PrepareLossyColorBuffer(WebpPixelBuffer buffer, WebpEncodeOptions options)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(options);
        WebpPixelBufferHelper.ValidateRgbaBuffer(buffer);
        ValidateLossyAlphaMode(options.LossyAlphaMode);

        if (options.LossyAlphaMode == WebpLossyAlphaMode.Preserve)
        {
            return PrepareColorBuffer(buffer, options.ExactTransparentRgb);
        }

        WebpPixelBuffer output;
        if (options.LossyAlphaMode == WebpLossyAlphaMode.BlendWithBackground)
        {
            uint opaqueBackground = options.AlphaBackgroundColorBgra | 0xFF000000U;
            output = WebpPixelUtilities.BlendOverBackground(buffer, opaqueBackground);
        }
        else
        {
            output = WebpPixelBufferHelper.Copy(buffer);
        }

        ForceOpaqueAlpha(output);
        return output;
    }

    internal static void ForceOpaqueAlpha(WebpPixelBuffer buffer)
    {
        int row = 0;
        while (row < buffer.Height)
        {
            int column = 0;
            while (column < buffer.Width)
            {
                int offset = row * buffer.Stride + column * 4;
                buffer.Rgba[offset + 3] = 255;
                column = column + 1;
            }

            row = row + 1;
        }
    }

    internal static byte[] QuantizeAlpha(byte[] alpha, int alphaQuality)
    {
        ArgumentNullException.ThrowIfNull(alpha);
        ValidateAlphaQuality(alphaQuality);

        byte[] output = WebpUnknownChunk.CopyBytes(alpha);
        if (alphaQuality == 100)
        {
            return output;
        }

        int step = 1 + RoundDivide((100 - alphaQuality) * 15, 100);
        int index = 0;
        while (index < output.Length)
        {
            int quantized = RoundDivide(output[index], step) * step;
            output[index] = ClampToByte(quantized);
            index = index + 1;
        }

        return output;
    }

    internal static byte SelectAlphaFilter(byte[] alpha, int width, int height, WebpAlphaFilterMode mode)
    {
        ArgumentNullException.ThrowIfNull(alpha);
        ValidateAlphaFilterMode(mode);
        if (mode == WebpAlphaFilterMode.None)
        {
            ValidateAlphaPlane(alpha, width, height);
            return WebpConstants.AlphaFilterNone;
        }

        if (mode == WebpAlphaFilterMode.Fast)
        {
            ValidateAlphaPlane(alpha, width, height);
            return WebpConstants.AlphaFilterHorizontal;
        }

        return SelectBestAlphaFilter(alpha, width, height);
    }

    internal static byte SelectBestAlphaFilter(byte[] alpha, int width, int height)
    {
        byte[] filters = new byte[]
        {
            WebpConstants.AlphaFilterNone,
            WebpConstants.AlphaFilterHorizontal,
            WebpConstants.AlphaFilterVertical,
            WebpConstants.AlphaFilterGradient
        };
        byte bestFilter = WebpConstants.AlphaFilterNone;
        long bestScore = ScoreFilteredAlpha(alpha, width, height, bestFilter);
        int index = 1;
        while (index < filters.Length)
        {
            long score = ScoreFilteredAlpha(alpha, width, height, filters[index]);
            if (score < bestScore)
            {
                bestScore = score;
                bestFilter = filters[index];
            }

            index = index + 1;
        }

        return bestFilter;
    }

    internal static long ScoreFilteredAlpha(byte[] alpha, int width, int height, byte filter)
    {
        byte[] filtered = WebpAlphaFilter.Filter(alpha, width, height, filter);
        long score = 0;
        int index = 0;
        while (index < filtered.Length)
        {
            score = score + filtered[index];
            index = index + 1;
        }

        return score;
    }

    internal static byte[] EncodeLosslessAlphaStream(WebpPixelBuffer buffer)
    {
        return WebpVp8LEncoder.Encode(buffer);
    }

    internal static void ValidateAlphaPlane(byte[] alpha, int width, int height)
    {
        WebpAlphaFilter.Filter(alpha, width, height, WebpConstants.AlphaFilterNone);
    }

    internal static void ValidateAlphaFilterMode(WebpAlphaFilterMode mode)
    {
        if (mode < WebpAlphaFilterMode.None || mode > WebpAlphaFilterMode.Best)
        {
            throw new ArgumentOutOfRangeException("AlphaFilter");
        }
    }

    internal static void ValidateAlphaQuality(int alphaQuality)
    {
        if (alphaQuality < 0 || alphaQuality > 100)
        {
            throw new ArgumentOutOfRangeException("alphaQuality");
        }
    }

    internal static void ValidateLossyAlphaMode(WebpLossyAlphaMode mode)
    {
        if (mode < WebpLossyAlphaMode.Preserve || mode > WebpLossyAlphaMode.BlendWithBackground)
        {
            throw new ArgumentOutOfRangeException("LossyAlphaMode");
        }
    }

    internal static int RoundDivide(int value, int divisor)
    {
        return (value + (divisor / 2)) / divisor;
    }

    internal static byte ClampToByte(int value)
    {
        if (value < 0)
        {
            return 0;
        }

        if (value > 255)
        {
            return 255;
        }

        return (byte)value;
    }
}
