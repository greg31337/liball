namespace raw;

internal enum RawSrawInterpolationMode
{
    Horizontal422 = 0,
    Block420 = 1
}

internal static class RawSrawInterpolator
{
    internal static RawSrawInterpolationMode SelectMode(RawCanonCr2Metadata metadata)
    {
        if (metadata == null || metadata.SubsamplingHorizontal != 2)
        {
            throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "Canon sRAW/mRAW horizontal subsampling is not defined.", -1);
        }
        switch (metadata.SubsamplingVertical)
        {
            case 1:
                return RawSrawInterpolationMode.Horizontal422;
            case 2:
                return RawSrawInterpolationMode.Block420;
            default:
                throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "Canon sRAW/mRAW vertical subsampling is not defined.", -1);
        }
    }

    internal static int InterpolateChroma(ushort[] packed, RawCanonCr2Metadata metadata, RawSrawInterpolationMode mode, int groupX, int groupY, int localX, int localY, int component)
    {
        switch (mode)
        {
            case RawSrawInterpolationMode.Horizontal422:
                return InterpolateHorizontal(packed, metadata, groupX, groupY, localX, component);
            case RawSrawInterpolationMode.Block420:
                return InterpolateBlock(packed, metadata, groupX, groupY, localX, localY, component);
            default:
                throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "The Canon sRAW/mRAW interpolation mode is not defined.", -1);
        }
    }

    internal static int InterpolateHorizontal(ushort[] packed, RawCanonCr2Metadata metadata, int groupX, int groupY, int localX, int component)
    {
        int groupColumns = metadata.SensorWidth / metadata.SubsamplingHorizontal;
        int nextX = groupX + 1 < groupColumns ? groupX + 1 : groupX;
        int first = GetChroma(packed, metadata, groupX, groupY, component);
        if (localX == 0)
        {
            return first;
        }
        return (first + GetChroma(packed, metadata, nextX, groupY, component)) >> 1;
    }

    internal static int InterpolateBlock(ushort[] packed, RawCanonCr2Metadata metadata, int groupX, int groupY, int localX, int localY, int component)
    {
        int groupColumns = metadata.SensorWidth / metadata.SubsamplingHorizontal;
        int groupRows = metadata.SensorHeight / metadata.SubsamplingVertical;
        int nextX = groupX + 1 < groupColumns ? groupX + 1 : groupX;
        int nextY = groupY + 1 < groupRows ? groupY + 1 : groupY;
        int first = GetChroma(packed, metadata, groupX, groupY, component);
        if (localX == 0 && localY == 0)
        {
            return first;
        }
        if (localY == 0)
        {
            return (first + GetChroma(packed, metadata, nextX, groupY, component)) >> 1;
        }
        if (localX == 0)
        {
            return (first + GetChroma(packed, metadata, groupX, nextY, component)) >> 1;
        }
        int second = GetChroma(packed, metadata, nextX, groupY, component);
        int third = GetChroma(packed, metadata, groupX, nextY, component);
        int fourth = GetChroma(packed, metadata, nextX, nextY, component);
        return (first + second + third + fourth) >> 2;
    }

    internal static int GetChroma(ushort[] packed, RawCanonCr2Metadata metadata, int groupX, int groupY, int component)
    {
        int packedWidth = CanonSRawDecoder.GetPackedWidth(metadata);
        int componentsPerMcu = 2 + metadata.SubsamplingHorizontal * metadata.SubsamplingVertical;
        int value = packed[groupY * packedWidth + groupX * componentsPerMcu + component];
        return value - 16384 + metadata.SRawHue;
    }
}
