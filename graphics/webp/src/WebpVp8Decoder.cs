using System;

namespace webp;

internal static class WebpVp8Decoder
{
    internal const int CoefficientProbabilityUpdateCount = 4 * 8 * 3 * 11;

    internal static WebpVp8DecodeResult Decode(byte[] payload, WebpStrictness strictness)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return Decode(payload.AsSpan(), strictness);
    }

    internal static WebpVp8DecodeResult Decode(ReadOnlySpan<byte> payload, WebpStrictness strictness)
    {
        WebpVp8DecodeResult result = new WebpVp8DecodeResult();
        WebpVp8FrameHeaderDataParseResult header = WebpVp8FrameHeaderDataParser.Parse(payload, strictness);
        result.FrameHeader = header.FrameHeader;
        result.HeaderData = header.HeaderData;
        CopyHeaderDiagnostics(result, header);
        if (!header.IsValid)
        {
            Finish(result);
            return result;
        }

        int firstPartitionOffset = WebpConstants.Vp8LossyKeyFrameHeaderSize;
        int firstPartitionSize = result.FrameHeader.FirstPartitionSize;
        if ((long)firstPartitionOffset + firstPartitionSize > payload.Length)
        {
            AddDiagnostic(result, "VP8 payload is truncated before the end of the first partition");
            Finish(result);
            return result;
        }

        byte[] firstPartition = payload.Slice(firstPartitionOffset, firstPartitionSize).ToArray();
        WebpVp8BoolDecoder decoder = new WebpVp8BoolDecoder(firstPartition);
        SkipFrameHeaderData(decoder, result.HeaderData, result.FrameHeader.IsKeyFrame);
        bool coefficientSkipEnabled = ParseEntropyHeader(decoder);
        int skipProbability = 128;
        if (coefficientSkipEnabled)
        {
            skipProbability = (int)decoder.DecodeLiteral(8);
        }

        if (decoder.IsTruncated)
        {
            AddDiagnostic(result, "VP8 first partition is truncated before macroblock mode data");
            Finish(result);
            return result;
        }

        int macroblockColumns = (result.FrameHeader.Width + 15) / 16;
        int macroblockRows = (result.FrameHeader.Height + 15) / 16;
        result.ModeResult = WebpVp8IntraModeParser.ParseKeyFrameModes(decoder, macroblockColumns, macroblockRows, result.HeaderData.Segmentation, coefficientSkipEnabled, skipProbability);
        CopyModeDiagnostics(result, result.ModeResult);
        ValidateSupportedResidualPath(result, coefficientSkipEnabled);
        if (result.HasErrors())
        {
            Finish(result);
            return result;
        }

        result.PixelBuffer = DecodeNeutralPredictionFrame(result.FrameHeader.Width, result.FrameHeader.Height);
        Finish(result);
        return result;
    }

    internal static void SkipFrameHeaderData(WebpVp8BoolDecoder decoder, WebpVp8FrameHeaderData headerData, bool isKeyFrame)
    {
        if (isKeyFrame)
        {
            decoder.DecodeBit();
            decoder.DecodeBit();
        }

        SkipSegmentation(decoder, headerData.Segmentation);
        SkipLoopFilter(decoder, headerData.LoopFilter);
        decoder.DecodeLiteral(2);
        SkipQuantizer(decoder);
    }

    internal static void SkipSegmentation(WebpVp8BoolDecoder decoder, WebpVp8SegmentationHeader segmentation)
    {
        bool enabled = decoder.DecodeBit() != 0;
        if (!enabled)
        {
            return;
        }

        bool updateMap = decoder.DecodeBit() != 0;
        bool updateData = decoder.DecodeBit() != 0;
        if (updateData)
        {
            decoder.DecodeBit();
            int index = 0;
            while (index < 4)
            {
                SkipMaybeSigned(decoder, 7);
                index = index + 1;
            }

            index = 0;
            while (index < 4)
            {
                SkipMaybeSigned(decoder, 6);
                index = index + 1;
            }
        }

        if (updateMap)
        {
            int index = 0;
            while (index < 3)
            {
                if (decoder.DecodeBit() != 0)
                {
                    decoder.DecodeLiteral(8);
                }

                index = index + 1;
            }
        }

        segmentation.Enabled = enabled;
    }

    internal static void SkipLoopFilter(WebpVp8BoolDecoder decoder, WebpVp8LoopFilterHeader loopFilter)
    {
        decoder.DecodeBit();
        decoder.DecodeLiteral(6);
        decoder.DecodeLiteral(3);
        bool deltaEnabled = decoder.DecodeBit() != 0;
        if (deltaEnabled)
        {
            bool deltaUpdate = decoder.DecodeBit() != 0;
            if (deltaUpdate)
            {
                int index = 0;
                while (index < 8)
                {
                    if (decoder.DecodeBit() != 0)
                    {
                        DecodeSignedMagnitude(decoder, 6);
                    }

                    index = index + 1;
                }
            }
        }

        loopFilter.DeltaEnabled = deltaEnabled;
    }

    internal static void SkipQuantizer(WebpVp8BoolDecoder decoder)
    {
        decoder.DecodeLiteral(7);
        int index = 0;
        while (index < 5)
        {
            if (decoder.DecodeBit() != 0)
            {
                DecodeSignedMagnitude(decoder, 4);
            }

            index = index + 1;
        }
    }

    internal static bool ParseEntropyHeader(WebpVp8BoolDecoder decoder)
    {
        decoder.DecodeBit();
        int index = 0;
        while (index < WebpVp8ProbabilityTables.CoefficientUpdate.Length)
        {
            if (decoder.DecodeBool(WebpVp8ProbabilityTables.CoefficientUpdate[index]) != 0)
            {
                decoder.DecodeLiteral(8);
            }

            index = index + 1;
        }

        return decoder.DecodeBit() != 0;
    }

    internal static void SkipMaybeSigned(WebpVp8BoolDecoder decoder, int bitCount)
    {
        if (decoder.DecodeBit() != 0)
        {
            DecodeSignedMagnitude(decoder, bitCount);
        }
    }

    internal static int DecodeSignedMagnitude(WebpVp8BoolDecoder decoder, int bitCount)
    {
        int value = (int)decoder.DecodeLiteral(bitCount);
        if (decoder.DecodeBit() != 0)
        {
            return -value;
        }

        return value;
    }

    internal static void ValidateSupportedResidualPath(WebpVp8DecodeResult result, bool coefficientSkipEnabled)
    {
        if (!coefficientSkipEnabled)
        {
            AddDiagnostic(result, "VP8 lossy decode currently requires coefficient-skip signaling for all macroblocks");
            return;
        }

        int index = 0;
        while (index < result.ModeResult.Macroblocks.Count)
        {
            if (!result.ModeResult.Macroblocks[index].SkipCoefficients)
            {
                AddDiagnostic(result, "VP8 lossy decode currently supports only skipped-residual macroblocks");
                return;
            }

            index = index + 1;
        }
    }

    internal static WebpPixelBuffer DecodeNeutralPredictionFrame(int width, int height)
    {
        int uvWidth = (width + 1) >> 1;
        int uvHeight = (height + 1) >> 1;
        byte[] yPlane = NewFilledPlane(width, height, 127);
        byte[] uPlane = NewFilledPlane(uvWidth, uvHeight, 128);
        byte[] vPlane = NewFilledPlane(uvWidth, uvHeight, 128);
        return WebpVp8YuvConverter.ConvertYuv420ToRgba(yPlane, uPlane, vPlane, width, height, width, uvWidth);
    }

    internal static byte[] NewFilledPlane(int width, int height, byte value)
    {
        byte[] plane = new byte[width * height];
        int index = 0;
        while (index < plane.Length)
        {
            plane[index] = value;
            index = index + 1;
        }

        return plane;
    }

    internal static void CopyHeaderDiagnostics(WebpVp8DecodeResult result, WebpVp8FrameHeaderDataParseResult header)
    {
        int index = 0;
        while (index < header.Diagnostics.Count)
        {
            result.Diagnostics.Add(header.Diagnostics[index]);
            index = index + 1;
        }
    }

    internal static void CopyModeDiagnostics(WebpVp8DecodeResult result, WebpVp8IntraModeParseResult modeResult)
    {
        int index = 0;
        while (index < modeResult.Diagnostics.Count)
        {
            result.Diagnostics.Add(modeResult.Diagnostics[index]);
            index = index + 1;
        }
    }

    internal static void AddDiagnostic(WebpVp8DecodeResult result, string message)
    {
        result.Diagnostics.Add(new WebpDiagnostic(WebpDiagnosticSeverity.Error, -1, WebpConstants.FourCcVp8String, message));
    }

    internal static void Finish(WebpVp8DecodeResult result)
    {
        result.IsValid = !result.HasErrors();
    }
}
