using System;

namespace webp;

internal static class WebpVp8FrameHeaderDataParser
{
    internal const int SegmentCount = 4;
    internal const int SegmentTreeProbabilityCount = 3;
    internal const int LoopFilterDeltaCount = 4;

    internal static WebpVp8FrameHeaderDataParseResult Parse(byte[] payload, WebpStrictness strictness)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return Parse(payload.AsSpan(), strictness);
    }

    internal static WebpVp8FrameHeaderDataParseResult Parse(ReadOnlySpan<byte> payload, WebpStrictness strictness)
    {
        WebpVp8FrameHeaderDataParseResult result = new WebpVp8FrameHeaderDataParseResult();
        WebpVp8FrameHeaderParseResult frameHeader = WebpVp8FrameHeaderParser.Parse(payload, strictness);
        result.FrameHeader = frameHeader.Header;
        CopyDiagnostics(result, frameHeader);
        if (!frameHeader.IsValid)
        {
            Finish(result);
            return result;
        }

        int firstPartitionOffset = WebpConstants.Vp8LossyKeyFrameHeaderSize;
        int firstPartitionSize = result.FrameHeader.FirstPartitionSize;
        if (firstPartitionSize < 2)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, firstPartitionOffset, "VP8 first partition is too short for boolean header parsing");
            Finish(result);
            return result;
        }

        long firstPartitionEnd = (long)firstPartitionOffset + firstPartitionSize;
        if (firstPartitionEnd > payload.Length)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, firstPartitionOffset, "VP8 payload is truncated before the end of the first partition");
            Finish(result);
            return result;
        }

        byte[] firstPartition = payload.Slice(firstPartitionOffset, firstPartitionSize).ToArray();
        WebpVp8BoolDecoder decoder = new WebpVp8BoolDecoder(firstPartition);
        if (result.FrameHeader.IsKeyFrame)
        {
            result.HeaderData.ColorSpace = decoder.DecodeBit();
            result.HeaderData.PixelClampingType = decoder.DecodeBit();
        }

        ParseSegmentation(result.HeaderData.Segmentation, decoder);
        ParseLoopFilter(result.HeaderData.LoopFilter, decoder);
        ParseTokenPartitions(result, decoder, payload, (int)firstPartitionEnd);
        ParseQuantizer(result.HeaderData.Quantizer, decoder);

        Finish(result);
        return result;
    }

    internal static void ParseSegmentation(WebpVp8SegmentationHeader header, WebpVp8BoolDecoder decoder)
    {
        header.Enabled = decoder.DecodeBit() != 0;
        if (!header.Enabled)
        {
            header.UpdateMap = false;
            header.UpdateData = false;
            return;
        }

        header.UpdateMap = decoder.DecodeBit() != 0;
        header.UpdateData = decoder.DecodeBit() != 0;
        if (header.UpdateData)
        {
            header.AbsoluteSegmentFeatureMode = decoder.DecodeBit() != 0;
            int index = 0;
            while (index < SegmentCount)
            {
                header.QuantizerDeltas[index] = DecodeMaybeSignedMagnitude(decoder, 7);
                index = index + 1;
            }

            index = 0;
            while (index < SegmentCount)
            {
                header.LoopFilterDeltas[index] = DecodeMaybeSignedMagnitude(decoder, 6);
                index = index + 1;
            }
        }

        if (header.UpdateMap)
        {
            int index = 0;
            while (index < SegmentTreeProbabilityCount)
            {
                if (decoder.DecodeBit() != 0)
                {
                    header.SegmentTreeProbabilities[index] = (byte)decoder.DecodeLiteral(8);
                }
                else
                {
                    header.SegmentTreeProbabilities[index] = 255;
                }

                index = index + 1;
            }
        }
    }

    internal static void ParseLoopFilter(WebpVp8LoopFilterHeader header, WebpVp8BoolDecoder decoder)
    {
        header.UseSimpleFilter = decoder.DecodeBit() != 0;
        header.Level = (int)decoder.DecodeLiteral(6);
        header.Sharpness = (int)decoder.DecodeLiteral(3);
        header.DeltaEnabled = decoder.DecodeBit() != 0;
        if (header.DeltaEnabled)
        {
            header.DeltaUpdate = decoder.DecodeBit() != 0;
            if (header.DeltaUpdate)
            {
                int index = 0;
                while (index < LoopFilterDeltaCount)
                {
                    header.ReferenceDeltaUpdates[index] = decoder.DecodeBit() != 0;
                    if (header.ReferenceDeltaUpdates[index])
                    {
                        header.ReferenceDeltas[index] = DecodeSignedMagnitude(decoder, 6);
                    }

                    index = index + 1;
                }

                index = 0;
                while (index < LoopFilterDeltaCount)
                {
                    header.ModeDeltaUpdates[index] = decoder.DecodeBit() != 0;
                    if (header.ModeDeltaUpdates[index])
                    {
                        header.ModeDeltas[index] = DecodeSignedMagnitude(decoder, 6);
                    }

                    index = index + 1;
                }
            }
        }
    }

    internal static void ParseTokenPartitions(WebpVp8FrameHeaderDataParseResult result, WebpVp8BoolDecoder decoder, ReadOnlySpan<byte> payload, int tokenHeaderOffset)
    {
        WebpVp8TokenPartitionHeader header = result.HeaderData.TokenPartitions;
        header.Log2PartitionCount = (int)decoder.DecodeLiteral(2);
        header.PartitionCount = 1 << header.Log2PartitionCount;
        header.PartitionSizeHeaderBytes = 3 * (header.PartitionCount - 1);
        header.TokenDataOffset = tokenHeaderOffset + header.PartitionSizeHeaderBytes;
        header.PartitionOffsets = new int[header.PartitionCount];
        header.PartitionSizes = new int[header.PartitionCount];

        int tokenDataLength = payload.Length - tokenHeaderOffset;
        if (tokenDataLength < header.PartitionSizeHeaderBytes)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, tokenHeaderOffset, "VP8 token partition size headers are truncated");
            return;
        }

        int remaining = tokenDataLength - header.PartitionSizeHeaderBytes;
        int currentOffset = header.TokenDataOffset;
        int index = 0;
        while (index < header.PartitionCount)
        {
            header.PartitionOffsets[index] = currentOffset;
            if (index < header.PartitionCount - 1)
            {
                int sizeOffset = tokenHeaderOffset + (index * 3);
                int size = (int)WebpEndianReader.ReadUInt24(payload, sizeOffset);
                header.PartitionSizes[index] = size;
                if (size > remaining)
                {
                    AddDiagnostic(result, WebpDiagnosticSeverity.Error, sizeOffset, "VP8 token partition length exceeds remaining token data");
                    remaining = 0;
                }
                else
                {
                    remaining = remaining - size;
                }

                currentOffset = currentOffset + size;
            }
            else
            {
                header.PartitionSizes[index] = remaining;
            }

            index = index + 1;
        }
    }

    internal static void ParseQuantizer(WebpVp8QuantizerHeader header, WebpVp8BoolDecoder decoder)
    {
        header.BaseQIndex = (int)decoder.DecodeLiteral(7);
        header.YDcDeltaPresent = decoder.DecodeBit() != 0;
        if (header.YDcDeltaPresent)
        {
            header.YDcDelta = DecodeSignedMagnitude(decoder, 4);
        }

        header.Y2DcDeltaPresent = decoder.DecodeBit() != 0;
        if (header.Y2DcDeltaPresent)
        {
            header.Y2DcDelta = DecodeSignedMagnitude(decoder, 4);
        }

        header.Y2AcDeltaPresent = decoder.DecodeBit() != 0;
        if (header.Y2AcDeltaPresent)
        {
            header.Y2AcDelta = DecodeSignedMagnitude(decoder, 4);
        }

        header.UvDcDeltaPresent = decoder.DecodeBit() != 0;
        if (header.UvDcDeltaPresent)
        {
            header.UvDcDelta = DecodeSignedMagnitude(decoder, 4);
        }

        header.UvAcDeltaPresent = decoder.DecodeBit() != 0;
        if (header.UvAcDeltaPresent)
        {
            header.UvAcDelta = DecodeSignedMagnitude(decoder, 4);
        }

        header.DeltaUpdate = header.YDcDeltaPresent
            || header.Y2DcDeltaPresent
            || header.Y2AcDeltaPresent
            || header.UvDcDeltaPresent
            || header.UvAcDeltaPresent;
    }

    internal static int DecodeMaybeSignedMagnitude(WebpVp8BoolDecoder decoder, int bitCount)
    {
        if (decoder.DecodeBit() == 0)
        {
            return 0;
        }

        return DecodeSignedMagnitude(decoder, bitCount);
    }

    internal static int DecodeSignedMagnitude(WebpVp8BoolDecoder decoder, int bitCount)
    {
        int magnitude = (int)decoder.DecodeLiteral(bitCount);
        if (decoder.DecodeBit() != 0)
        {
            return -magnitude;
        }

        return magnitude;
    }

    internal static void CopyDiagnostics(WebpVp8FrameHeaderDataParseResult result, WebpVp8FrameHeaderParseResult frameHeader)
    {
        int index = 0;
        while (index < frameHeader.Diagnostics.Count)
        {
            result.Diagnostics.Add(frameHeader.Diagnostics[index]);
            index = index + 1;
        }
    }

    internal static void AddDiagnostic(WebpVp8FrameHeaderDataParseResult result, WebpDiagnosticSeverity severity, long offset, string message)
    {
        result.Diagnostics.Add(new WebpDiagnostic(severity, offset, WebpConstants.FourCcVp8String, message));
    }

    internal static void Finish(WebpVp8FrameHeaderDataParseResult result)
    {
        result.IsValid = !result.HasErrors();
    }
}
