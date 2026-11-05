namespace avif;

internal class Av1LayerSelection
{
    internal AvifResultCode Code;
    internal int OperatingPointIndex;
    internal int OperatingPointIdc;
    internal int MaximumSpatialLayer;
    internal int MaximumTemporalLayer;
    internal bool Progressive;
    internal bool LargeScaleTiles;

    internal Av1LayerSelection(AvifResultCode code)
    {
        Code = code;
        OperatingPointIndex = -1;
        MaximumSpatialLayer = -1;
        MaximumTemporalLayer = -1;
    }
}

internal class Av1LayerFrame
{
    internal int TemporalUnit;
    internal int TemporalId;
    internal int SpatialId;
    internal bool Shown;

    internal Av1LayerFrame(
        int temporalUnit,
        int temporalId,
        int spatialId,
        bool shown)
    {
        TemporalUnit = temporalUnit;
        TemporalId = temporalId;
        SpatialId = spatialId;
        Shown = shown;
    }
}

internal class Av1LayerOutputSelection
{
    internal AvifResultCode Code;
    internal bool[] Decode;
    internal bool[] Output;

    internal Av1LayerOutputSelection(
        AvifResultCode code,
        int frameCount)
    {
        Code = code;
        Decode = new bool[frameCount];
        Output = new bool[frameCount];
    }
}

// AV1 specification sections 5.2, 6.4.1, 7.2, and 7.18.1.
internal static class Av1LayerSelector
{
    internal static Av1LayerSelection Select(
        ParsedAv1SequenceHeader sequence,
        Av1DecoderOptions options)
    {
        return Resolve(sequence, options);
    }

    internal static Av1LayerSelection Resolve(
        ParsedAv1SequenceHeader sequence,
        Av1DecoderOptions options)
    {
        if (sequence == null || options == null
            || options.OperatingPoint < -1
            || options.SpatialLayer < -1
            || options.SpatialLayer >= Av1SyntaxConstants.MaxSpatialLayers
            || options.TemporalLayer < -1
            || options.TemporalLayer >= Av1SyntaxConstants.MaxTemporalLayers
            || sequence.GetOperatingPointCount() < 1)
        {
            return new Av1LayerSelection(AvifResultCode.InvalidArgument);
        }

        int operatingPointIndex = options.OperatingPoint < 0
            ? 0
            : options.OperatingPoint;
        Av1OperatingPoint? point = sequence.GetOperatingPoint(operatingPointIndex);
        if (point == null)
        {
            return new Av1LayerSelection(AvifResultCode.NotFound);
        }
        if (point.Idc < 0 || point.Idc > 4095
            || point.Idc != 0
                && ((point.Idc & 1) == 0 || (point.Idc & 256) == 0))
        {
            return new Av1LayerSelection(AvifResultCode.InvalidAv1Bitstream);
        }

        int maximumTemporal = point.Idc == 0
            ? 0
            : HighestSetBit(point.Idc & 255, 8);
        int maximumSpatial = point.Idc == 0
            ? 0
            : HighestSetBit(point.Idc >> 8 & 15, 4);
        if (options.TemporalLayer >= 0)
        {
            if (!LayerExists(point.Idc, options.TemporalLayer, false))
            {
                return new Av1LayerSelection(AvifResultCode.NotFound);
            }
            maximumTemporal = options.TemporalLayer;
        }
        if (options.SpatialLayer >= 0)
        {
            if (!LayerExists(point.Idc, options.SpatialLayer, true))
            {
                return new Av1LayerSelection(AvifResultCode.NotFound);
            }
            maximumSpatial = options.SpatialLayer;
        }

        Av1LayerSelection selection = new Av1LayerSelection(
            AvifResultCode.Success);
        selection.OperatingPointIndex = operatingPointIndex;
        selection.OperatingPointIdc = point.Idc;
        selection.MaximumTemporalLayer = maximumTemporal;
        selection.MaximumSpatialLayer = maximumSpatial;
        selection.Progressive = options.AllowProgressive;
        selection.LargeScaleTiles = options.DecodeLargeScaleTiles;
        return selection;
    }

    internal static bool ShouldDecodeObu(
        Av1LayerSelection selection,
        Av1ObuHeader header)
    {
        if (selection == null || selection.Code != AvifResultCode.Success
            || header == null)
        {
            return false;
        }
        if (header.Type == Av1ObuType.SequenceHeader
            || header.Type == Av1ObuType.TemporalDelimiter
            || header.Type == Av1ObuType.TileList
            || !header.HasExtension)
        {
            return true;
        }
        return IncludesLayer(
            selection, header.TemporalId, header.SpatialId);
    }

    internal static Av1LayerOutputSelection SelectOutputs(
        Av1LayerSelection? selection,
        Av1LayerFrame[] frames)
    {
        int count = frames == null ? 0 : frames.Length;
        Av1LayerOutputSelection result = new Av1LayerOutputSelection(
            AvifResultCode.Success, count);
        if (selection == null || selection.Code != AvifResultCode.Success
            || frames == null)
        {
            result.Code = AvifResultCode.InvalidArgument;
            return result;
        }

        int previousTemporalUnit = -1;
        int index = 0;
        while (index < frames.Length)
        {
            Av1LayerFrame frame = frames[index];
            if (frame == null || frame.TemporalUnit < 0
                || frame.TemporalUnit < previousTemporalUnit
                || frame.TemporalId < 0
                || frame.TemporalId >= Av1SyntaxConstants.MaxTemporalLayers
                || frame.SpatialId < 0
                || frame.SpatialId >= Av1SyntaxConstants.MaxSpatialLayers)
            {
                result.Code = AvifResultCode.InvalidArgument;
                return result;
            }
            previousTemporalUnit = frame.TemporalUnit;
            result.Decode[index] = IncludesLayer(
                selection, frame.TemporalId, frame.SpatialId);
            if (selection.Progressive && result.Decode[index] && frame.Shown)
            {
                result.Output[index] = true;
            }
            index++;
        }

        if (!selection.Progressive)
        {
            SelectHighestSpatialOutputs(frames, result);
        }
        return result;
    }

    static void SelectHighestSpatialOutputs(
        Av1LayerFrame[] frames,
        Av1LayerOutputSelection result)
    {
        int index = 0;
        while (index < frames.Length)
        {
            int temporalUnit = frames[index].TemporalUnit;
            int selected = -1;
            int selectedSpatial = -1;
            while (index < frames.Length
                && frames[index].TemporalUnit == temporalUnit)
            {
                if (result.Decode[index] && frames[index].Shown
                    && frames[index].SpatialId >= selectedSpatial)
                {
                    selected = index;
                    selectedSpatial = frames[index].SpatialId;
                }
                index++;
            }
            if (selected >= 0)
            {
                result.Output[selected] = true;
            }
        }
    }

    static bool IncludesLayer(
        Av1LayerSelection selection,
        int temporalId,
        int spatialId)
    {
        if (temporalId < 0 || temporalId > selection.MaximumTemporalLayer
            || spatialId < 0 || spatialId > selection.MaximumSpatialLayer)
        {
            return false;
        }
        if (selection.OperatingPointIdc == 0)
        {
            return temporalId == 0 && spatialId == 0;
        }
        return (selection.OperatingPointIdc & (1 << temporalId)) != 0
            && (selection.OperatingPointIdc & (1 << (spatialId + 8))) != 0;
    }

    static bool LayerExists(int idc, int layer, bool spatial)
    {
        if (idc == 0)
        {
            return layer == 0;
        }
        int shift = spatial ? layer + 8 : layer;
        return (idc & (1 << shift)) != 0;
    }

    static int HighestSetBit(int value, int bitCount)
    {
        int bit = bitCount - 1;
        while (bit >= 0)
        {
            if ((value & (1 << bit)) != 0)
            {
                return bit;
            }
            bit--;
        }
        return -1;
    }
}
