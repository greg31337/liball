using System;

namespace webp;

internal static class WebpVp8LoopFilterEncoder
{
    internal const int DeltaCount = 4;

    internal static WebpVp8LoopFilterHeader SelectKeyFrameParameters(int fixedQuantizer)
    {
        return SelectKeyFrameParameters(fixedQuantizer, 0);
    }

    internal static WebpVp8LoopFilterHeader SelectKeyFrameParameters(int fixedQuantizer, int sharpness)
    {
        ValidateFixedQuantizer(fixedQuantizer);
        ValidateSharpness(sharpness);

        WebpVp8LoopFilterHeader header = new WebpVp8LoopFilterHeader();
        header.UseSimpleFilter = true;
        header.Level = SelectLevel(fixedQuantizer);
        header.Sharpness = sharpness;
        header.DeltaEnabled = false;
        header.DeltaUpdate = false;
        return header;
    }

    internal static int SelectLevel(int fixedQuantizer)
    {
        ValidateFixedQuantizer(fixedQuantizer);
        return ((fixedQuantizer * 63) + 63) / 127;
    }

    internal static void Write(WebpVp8BoolWriter writer, WebpVp8LoopFilterHeader header)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ValidateHeader(header);

        writer.WriteBit(header.UseSimpleFilter ? 1 : 0);
        writer.WriteLiteral(header.Level, 6);
        writer.WriteLiteral(header.Sharpness, 3);
        writer.WriteBit(header.DeltaEnabled ? 1 : 0);
        if (header.DeltaEnabled)
        {
            writer.WriteBit(header.DeltaUpdate ? 1 : 0);
            if (header.DeltaUpdate)
            {
                WriteDeltaUpdates(writer, header.ReferenceDeltaUpdates, header.ReferenceDeltas);
                WriteDeltaUpdates(writer, header.ModeDeltaUpdates, header.ModeDeltas);
            }
        }
    }

    internal static void WriteDeltaUpdates(WebpVp8BoolWriter writer, bool[] updates, int[] deltas)
    {
        int index = 0;
        while (index < DeltaCount)
        {
            writer.WriteBit(updates[index] ? 1 : 0);
            if (updates[index])
            {
                WriteSignedMagnitude(writer, deltas[index], 6);
            }

            index = index + 1;
        }
    }

    internal static void WriteSignedMagnitude(WebpVp8BoolWriter writer, int value, int bitCount)
    {
        int magnitude = value;
        if (value < 0)
        {
            magnitude = -value;
        }

        writer.WriteLiteral(magnitude, bitCount);
        writer.WriteBit(value < 0 ? 1 : 0);
    }

    internal static void ValidateHeader(WebpVp8LoopFilterHeader header)
    {
        ArgumentNullException.ThrowIfNull(header);
        ValidateLoopFilterLevel(header.Level);
        ValidateSharpness(header.Sharpness);
        ValidateDeltaArrays(header.ReferenceDeltaUpdates, header.ReferenceDeltas, "Reference");
        ValidateDeltaArrays(header.ModeDeltaUpdates, header.ModeDeltas, "Mode");
        if (header.DeltaUpdate)
        {
            ValidateDeltas(header.ReferenceDeltaUpdates, header.ReferenceDeltas, "Reference");
            ValidateDeltas(header.ModeDeltaUpdates, header.ModeDeltas, "Mode");
        }
    }

    internal static void ValidateDeltaArrays(bool[] updates, int[] deltas, string name)
    {
        ArgumentNullException.ThrowIfNull(updates);
        ArgumentNullException.ThrowIfNull(deltas);
        if (updates.Length != DeltaCount)
        {
            throw new ArgumentException("VP8 " + name + " loop filter update array must contain 4 values", name + "DeltaUpdates");
        }

        if (deltas.Length != DeltaCount)
        {
            throw new ArgumentException("VP8 " + name + " loop filter delta array must contain 4 values", name + "Deltas");
        }
    }

    internal static void ValidateDeltas(bool[] updates, int[] deltas, string name)
    {
        int index = 0;
        while (index < DeltaCount)
        {
            if (updates[index] && (deltas[index] < -63 || deltas[index] > 63))
            {
                throw new ArgumentOutOfRangeException(name + "Deltas");
            }

            index = index + 1;
        }
    }

    internal static void ValidateFixedQuantizer(int fixedQuantizer)
    {
        if (fixedQuantizer < 0 || fixedQuantizer > 127)
        {
            throw new ArgumentOutOfRangeException("fixedQuantizer");
        }
    }

    internal static void ValidateLoopFilterLevel(int level)
    {
        if (level < 0 || level > 63)
        {
            throw new ArgumentOutOfRangeException("level");
        }
    }

    internal static void ValidateSharpness(int sharpness)
    {
        if (sharpness < 0 || sharpness > 7)
        {
            throw new ArgumentOutOfRangeException("sharpness");
        }
    }
}
