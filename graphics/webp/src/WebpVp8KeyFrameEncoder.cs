using System;
using System.Collections.Generic;

namespace webp;

internal class WebpVp8RawBoolWriter
{
    internal List<int> Bits;

    internal WebpVp8RawBoolWriter()
    {
        Bits = new List<int>();
    }

    internal void WriteBit(int bit)
    {
        Bits.Add(bit == 0 ? 0 : 1);
    }

    internal void WriteLiteral(int value, int bitCount)
    {
        int index = bitCount - 1;
        while (index >= 0)
        {
            WriteBit((value >> index) & 1);
            index = index - 1;
        }
    }

    internal byte[] ToBytes()
    {
        int byteCount = (Bits.Count + 7) / 8;
        if (byteCount < 2)
        {
            byteCount = 2;
        }

        byte[] data = new byte[byteCount + 8];
        int index = 0;
        while (index < Bits.Count)
        {
            if (Bits[index] != 0)
            {
                data[index / 8] = (byte)(data[index / 8] | (1 << (7 - (index & 7))));
            }

            index = index + 1;
        }

        return data;
    }
}

internal static class WebpVp8KeyFrameEncoder
{
    internal const int CoefficientProbabilityUpdateCount = 4 * 8 * 3 * 11;
    internal const int DefaultFixedQuantizer = 32;

    internal static byte[] Encode(WebpPixelBuffer buffer)
    {
        return Encode(buffer, DefaultFixedQuantizer);
    }

    internal static byte[] Encode(WebpPixelBuffer buffer, int fixedQuantizer)
    {
        WebpVp8LoopFilterHeader loopFilter = WebpVp8LoopFilterEncoder.SelectKeyFrameParameters(fixedQuantizer);
        return Encode(buffer, fixedQuantizer, loopFilter);
    }

    internal static byte[] Encode(WebpPixelBuffer buffer, WebpEncodeOptions options)
    {
        WebpLossyEncodePlan plan = WebpLossyEncoderSettings.CreatePlan(options);
        WebpPixelBuffer prepared = WebpLossyEncoderSettings.ApplyPreprocessing(buffer, plan, options.ExactTransparentRgb);
        if (plan.UsesTargetSize)
        {
            return EncodeForTargetSize(prepared, plan);
        }

        if (plan.UsesTargetPsnr)
        {
            return EncodeForTargetPsnr(prepared, plan);
        }

        return Encode(prepared, plan.FixedQuantizer, plan.LoopFilter);
    }

    internal static byte[] EncodeForTargetSize(WebpPixelBuffer buffer, WebpLossyEncodePlan plan)
    {
        byte[] candidate = Array.Empty<byte>();
        int index = 0;
        while (index < plan.Passes.Length)
        {
            WebpLossyPassPlan pass = plan.Passes[index];
            WebpVp8LoopFilterHeader loopFilter = CreatePassLoopFilter(plan, pass.Quality);
            candidate = Encode(buffer, pass.FixedQuantizer, loopFilter);
            if (candidate.Length <= plan.TargetSize)
            {
                return candidate;
            }

            index = index + 1;
        }

        return candidate;
    }

    internal static byte[] EncodeForTargetPsnr(WebpPixelBuffer buffer, WebpLossyEncodePlan plan)
    {
        int targetQuality = WebpLossyEncoderSettings.TargetPsnrToQuality(plan.TargetPsnr);
        WebpLossyPassPlan selected = plan.Passes[0];
        int selectedDistance = Math.Abs(selected.Quality - targetQuality);
        int index = 1;
        while (index < plan.Passes.Length)
        {
            WebpLossyPassPlan candidate = plan.Passes[index];
            int distance = Math.Abs(candidate.Quality - targetQuality);
            if (distance < selectedDistance)
            {
                selected = candidate;
                selectedDistance = distance;
            }

            index = index + 1;
        }

        WebpVp8LoopFilterHeader loopFilter = CreatePassLoopFilter(plan, selected.Quality);
        return Encode(buffer, selected.FixedQuantizer, loopFilter);
    }

    internal static WebpVp8LoopFilterHeader CreatePassLoopFilter(WebpLossyEncodePlan plan, int quality)
    {
        WebpLossyEncodePlan passPlan = new WebpLossyEncodePlan();
        passPlan.Quality = quality;
        passPlan.Method = plan.Method;
        passPlan.FilterStrength = plan.FilterStrength;
        passPlan.FilterSharpness = plan.FilterSharpness;
        passPlan.AutoFilter = plan.AutoFilter;
        passPlan.SpatialNoiseShaping = plan.SpatialNoiseShaping;
        return WebpLossyEncoderSettings.CreateLoopFilter(passPlan);
    }

    internal static byte[] Encode(WebpPixelBuffer buffer, int fixedQuantizer, WebpVp8LoopFilterHeader loopFilter)
    {
        WebpPixelBufferHelper.ValidateRgbaBuffer(buffer);
        ValidateFixedQuantizer(fixedQuantizer);
        ValidateSupportedSize(buffer.Width, buffer.Height);
        WebpVp8Yuv420Buffer yuv = WebpVp8RgbaToYuvConverter.ConvertRgbaToYuv420(buffer);
        if (yuv.Width != buffer.Width || yuv.Height != buffer.Height)
        {
            throw new ArgumentException("RGBA to YUV conversion dimensions changed", "buffer");
        }

        WebpVp8BoolWriter writer = new WebpVp8BoolWriter();
        WriteFrameHeaderData(writer, fixedQuantizer, loopFilter);
        WriteEntropyHeader(writer);
        WriteSkippedMacroblockMode(writer);
        byte[] firstPartition = writer.ToBytes();
        return WriteVp8Payload(buffer.Width, buffer.Height, firstPartition);
    }

    internal static void WriteFrameHeaderData(WebpVp8BoolWriter writer, int fixedQuantizer, WebpVp8LoopFilterHeader loopFilter)
    {
        writer.WriteLiteral(0, 2);
        writer.WriteBit(0);
        WebpVp8LoopFilterEncoder.Write(writer, loopFilter);
        writer.WriteLiteral(0, 2);
        writer.WriteLiteral(fixedQuantizer, 7);
        int index = 0;
        while (index < 5)
        {
            writer.WriteBit(0);
            index = index + 1;
        }
    }

    internal static void WriteEntropyHeader(WebpVp8BoolWriter writer)
    {
        writer.WriteBit(0);
        int index = 0;
        while (index < WebpVp8ProbabilityTables.CoefficientUpdate.Length)
        {
            writer.WriteBool(WebpVp8ProbabilityTables.CoefficientUpdate[index], 0);
            index = index + 1;
        }

        writer.WriteBit(1);
        writer.WriteLiteral(128, 8);
    }

    internal static void WriteSkippedMacroblockMode(WebpVp8BoolWriter writer)
    {
        writer.WriteBool(128, 1);
        writer.WriteBool(WebpVp8IntraModeParser.KfYModeProbabilities[0], 1);
        writer.WriteBool(WebpVp8IntraModeParser.KfYModeProbabilities[1], 0);
        writer.WriteBool(WebpVp8IntraModeParser.KfYModeProbabilities[2], 0);
        writer.WriteBool(WebpVp8IntraModeParser.KfUvModeProbabilities[0], 0);
    }

    internal static byte[] WriteVp8Payload(int width, int height, byte[] firstPartition)
    {
        byte[] payload = new byte[WebpConstants.Vp8LossyKeyFrameHeaderSize + firstPartition.Length];
        uint frameTag = 0x10U | ((uint)firstPartition.Length << 5);
        payload[0] = (byte)(frameTag & 255);
        payload[1] = (byte)((frameTag >> 8) & 255);
        payload[2] = (byte)((frameTag >> 16) & 255);
        payload[3] = WebpConstants.Vp8LossyKeyFrameSignature0;
        payload[4] = WebpConstants.Vp8LossyKeyFrameSignature1;
        payload[5] = WebpConstants.Vp8LossyKeyFrameSignature2;
        WebpEndianWriter.WriteUInt16(payload, 6, (ushort)width);
        WebpEndianWriter.WriteUInt16(payload, 8, (ushort)height);
        Array.Copy(firstPartition, 0, payload, WebpConstants.Vp8LossyKeyFrameHeaderSize, firstPartition.Length);
        return payload;
    }

    internal static void ValidateFixedQuantizer(int fixedQuantizer)
    {
        if (fixedQuantizer < 0 || fixedQuantizer > 127)
        {
            throw new ArgumentOutOfRangeException("fixedQuantizer");
        }
    }

    internal static void ValidateSupportedSize(int width, int height)
    {
        if (width > 16 || height > 16)
        {
            throw new ArgumentOutOfRangeException("buffer", "simple VP8 key-frame encoder currently supports one macroblock");
        }
    }
}
