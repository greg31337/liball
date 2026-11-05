namespace ttf;

internal enum SfntCompositePlacementKind
{
    Offset = 0,
    PointAttachment = 1
}

internal enum SfntCompositeOffsetScaling
{
    Default = 0,
    Scaled = 1,
    Unscaled = 2
}

internal enum SfntCompositeTransformKind
{
    Identity = 0,
    UniformScale = 1,
    SeparateScale = 2,
    TwoByTwo = 3
}

internal class SfntCompositeTransform
{
    SfntCompositeTransformKind kind;
    double xScale;
    double scale01;
    double scale10;
    double yScale;

    internal SfntCompositeTransform(
        SfntCompositeTransformKind kind,
        double xScale,
        double scale01,
        double scale10,
        double yScale)
    {
        this.kind = kind;
        this.xScale = xScale;
        this.scale01 = scale01;
        this.scale10 = scale10;
        this.yScale = yScale;
    }

    internal SfntCompositeTransformKind GetKind()
    {
        return kind;
    }

    internal double GetXScale()
    {
        return xScale;
    }

    internal double GetScale01()
    {
        return scale01;
    }

    internal double GetScale10()
    {
        return scale10;
    }

    internal double GetYScale()
    {
        return yScale;
    }

    internal double TransformX(double x, double y)
    {
        return xScale * x + scale10 * y;
    }

    internal double TransformY(double x, double y)
    {
        return scale01 * x + yScale * y;
    }
}

internal class SfntCompositeComponent
{
    int componentIndex;
    int flags;
    int glyphId;
    SfntCompositePlacementKind placementKind;
    int argument1;
    int argument2;
    SfntCompositeOffsetScaling offsetScaling;
    SfntCompositeTransform transform;

    internal SfntCompositeComponent(
        int componentIndex,
        int flags,
        int glyphId,
        SfntCompositePlacementKind placementKind,
        int argument1,
        int argument2,
        SfntCompositeOffsetScaling offsetScaling,
        SfntCompositeTransform transform)
    {
        this.componentIndex = componentIndex;
        this.flags = flags;
        this.glyphId = glyphId;
        this.placementKind = placementKind;
        this.argument1 = argument1;
        this.argument2 = argument2;
        this.offsetScaling = offsetScaling;
        this.transform = transform;
    }

    internal int GetComponentIndex()
    {
        return componentIndex;
    }

    internal int GetFlags()
    {
        return flags;
    }

    internal int GetGlyphId()
    {
        return glyphId;
    }

    internal SfntCompositePlacementKind GetPlacementKind()
    {
        return placementKind;
    }

    internal int GetXOffset()
    {
        EnsureOffsetPlacement();
        return argument1;
    }

    internal int GetYOffset()
    {
        EnsureOffsetPlacement();
        return argument2;
    }

    internal double GetEffectiveXOffset()
    {
        EnsureOffsetPlacement();

        if (offsetScaling == SfntCompositeOffsetScaling.Scaled)
        {
            return transform.TransformX(argument1, argument2);
        }

        return argument1;
    }

    internal double GetEffectiveYOffset()
    {
        EnsureOffsetPlacement();

        if (offsetScaling == SfntCompositeOffsetScaling.Scaled)
        {
            return transform.TransformY(argument1, argument2);
        }

        return argument2;
    }

    internal int GetParentPointIndex()
    {
        EnsurePointAttachment();
        return argument1;
    }

    internal int GetChildPointIndex()
    {
        EnsurePointAttachment();
        return argument2;
    }

    internal bool RoundsOffsetToGrid()
    {
        return placementKind == SfntCompositePlacementKind.Offset &&
            (flags & 0x0004) != 0;
    }

    internal bool HasMoreComponents()
    {
        return (flags & 0x0020) != 0;
    }

    internal bool HasCompositeInstructions()
    {
        return (flags & 0x0100) != 0;
    }

    internal bool UsesMetrics()
    {
        return (flags & 0x0200) != 0;
    }

    internal bool HasOverlappingComponents()
    {
        return (flags & 0x0400) != 0;
    }

    internal SfntCompositeOffsetScaling GetOffsetScaling()
    {
        return offsetScaling;
    }

    internal SfntCompositeTransform GetTransform()
    {
        return transform;
    }

    void EnsureOffsetPlacement()
    {
        if (placementKind != SfntCompositePlacementKind.Offset)
        {
            throw new System.InvalidOperationException(
                "A point-attached component has no XY offset.");
        }
    }

    void EnsurePointAttachment()
    {
        if (placementKind != SfntCompositePlacementKind.PointAttachment)
        {
            throw new System.InvalidOperationException(
                "An offset-positioned component has no point attachment.");
        }
    }
}

internal class SfntCompositeGlyph
{
    int glyphId;
    SfntGlyphHeader header;
    SfntCompositeComponent[] components;
    byte[] instructions;
    int metricsComponentIndex;
    bool overlappingComponents;
    int trailingPaddingLength;

    internal SfntCompositeGlyph(
        int glyphId,
        SfntGlyphHeader header,
        SfntCompositeComponent[] components,
        byte[] instructions,
        int metricsComponentIndex,
        bool overlappingComponents,
        int trailingPaddingLength)
    {
        this.glyphId = glyphId;
        this.header = header;
        this.components = components;
        this.instructions = instructions;
        this.metricsComponentIndex = metricsComponentIndex;
        this.overlappingComponents = overlappingComponents;
        this.trailingPaddingLength = trailingPaddingLength;
    }

    internal int GetGlyphId()
    {
        return glyphId;
    }

    internal SfntGlyphHeader GetHeader()
    {
        return header;
    }

    internal int GetComponentCount()
    {
        return components.Length;
    }

    internal SfntCompositeComponent GetComponent(int index)
    {
        if (index < 0 || index >= components.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return components[index];
    }

    internal int GetInstructionLength()
    {
        return instructions.Length;
    }

    internal byte[] CopyInstructions()
    {
        return instructions.AsSpan().ToArray();
    }

    internal bool HasMetricsComponent()
    {
        return metricsComponentIndex >= 0;
    }

    internal int GetMetricsComponentIndex()
    {
        if (metricsComponentIndex < 0)
        {
            throw new System.InvalidOperationException(
                "The composite glyph does not inherit component metrics.");
        }

        return metricsComponentIndex;
    }

    internal int GetMetricsGlyphId()
    {
        return components[GetMetricsComponentIndex()].GetGlyphId();
    }

    internal bool HasOverlappingComponents()
    {
        return overlappingComponents;
    }

    internal int GetTrailingPaddingLength()
    {
        return trailingPaddingLength;
    }
}

internal static class SfntCompositeGlyphParser
{
    const int HeaderLength = 10;
    const int ArgumentsAreWords = 0x0001;
    const int ArgumentsAreXYValues = 0x0002;
    const int WeHaveScale = 0x0008;
    const int MoreComponents = 0x0020;
    const int WeHaveSeparateScale = 0x0040;
    const int WeHaveTwoByTwo = 0x0080;
    const int WeHaveInstructions = 0x0100;
    const int UseMyMetrics = 0x0200;
    const int OverlapCompound = 0x0400;
    const int ScaledComponentOffset = 0x0800;
    const int UnscaledComponentOffset = 0x1000;
    const int ReservedFlags = 0xE010;

    internal static SfntCompositeGlyph? Parse(
        SfntGlyfRecord record,
        SfntMaxpTable maxp,
        FontLimits limits)
    {
        if (record == null)
        {
            throw new System.ArgumentNullException("record");
        }

        if (maxp == null)
        {
            throw new System.ArgumentNullException("maxp");
        }

        if (limits == null)
        {
            throw new System.ArgumentNullException("limits");
        }

        SfntGlyphHeader? header = SfntGlyphHeaderParser.Parse(record);

        if (header == null || header.GetContourCount() >= 0)
        {
            return null;
        }

        BigEndianReader reader = new BigEndianReader(record.GetDataSlice());
        SkipHeader(reader);
        System.Collections.Generic.List<SfntCompositeComponent> components =
            new System.Collections.Generic.List<SfntCompositeComponent>();
        int metricsComponentIndex = -1;
        bool hasInstructions = false;
        bool moreComponents = true;

        while (moreComponents)
        {
            int componentIndex = components.Count;
            int flags = reader.ReadUInt16();
            int glyphId = reader.ReadUInt16();
            ValidateFlags(flags, componentIndex);

            if (glyphId >= maxp.GetGlyphCount())
            {
                throw FontFormatException.Create(
                    "A composite component references a glyph outside maxp.");
            }

            SfntCompositePlacementKind placementKind =
                (flags & ArgumentsAreXYValues) != 0
                    ? SfntCompositePlacementKind.Offset
                    : SfntCompositePlacementKind.PointAttachment;
            int argument1 = ReadArgument(reader, flags, placementKind);
            int argument2 = ReadArgument(reader, flags, placementKind);
            SfntCompositeTransform transform = ReadTransform(reader, flags);
            SfntCompositeOffsetScaling offsetScaling = ReadOffsetScaling(
                flags,
                placementKind);
            components.Add(new SfntCompositeComponent(
                componentIndex,
                flags,
                glyphId,
                placementKind,
                argument1,
                argument2,
                offsetScaling,
                transform));

            if (components.Count > maxp.GetMaxComponentElements())
            {
                throw FontFormatException.Create(
                    "A composite glyph exceeds maxp maxComponentElements.");
            }

            if ((flags & UseMyMetrics) != 0)
            {
                metricsComponentIndex = componentIndex;
            }

            if ((flags & WeHaveInstructions) != 0)
            {
                hasInstructions = true;
            }

            moreComponents = (flags & MoreComponents) != 0;
        }

        int instructionLength = hasInstructions ? reader.ReadUInt16() : 0;
        maxp.VerifyCompositeGlyph(
            0,
            0,
            instructionLength,
            components.Count,
            1,
            limits);
        byte[] instructions = ReadBytes(reader, instructionLength);
        int trailingPaddingLength = ValidateTrailingPadding(reader);
        SfntCompositeComponent[] componentArray = components.ToArray();
        bool overlappingComponents =
            (componentArray[0].GetFlags() & OverlapCompound) != 0;

        return new SfntCompositeGlyph(
            record.GetGlyphId(),
            header,
            componentArray,
            instructions,
            metricsComponentIndex,
            overlappingComponents,
            trailingPaddingLength);
    }

    static void ValidateFlags(int flags, int componentIndex)
    {
        if ((flags & ReservedFlags) != 0)
        {
            throw FontFormatException.Create(
                "A composite component uses reserved flag bits.");
        }

        if (componentIndex == 0 && (flags & ArgumentsAreXYValues) == 0)
        {
            throw FontFormatException.Create(
                "The first composite component must use XY placement.");
        }

        if (componentIndex != 0 && (flags & OverlapCompound) != 0)
        {
            throw FontFormatException.Create(
                "The composite-overlap flag is not on the first component.");
        }

        int transformKindCount = 0;

        if ((flags & WeHaveScale) != 0)
        {
            transformKindCount += 1;
        }

        if ((flags & WeHaveSeparateScale) != 0)
        {
            transformKindCount += 1;
        }

        if ((flags & WeHaveTwoByTwo) != 0)
        {
            transformKindCount += 1;
        }

        if (transformKindCount > 1)
        {
            throw FontFormatException.Create(
                "A composite component declares multiple transform forms.");
        }

        if ((flags & ScaledComponentOffset) != 0 &&
            (flags & UnscaledComponentOffset) != 0)
        {
            throw FontFormatException.Create(
                "A composite component declares conflicting offset scaling.");
        }
    }

    static int ReadArgument(
        BigEndianReader reader,
        int flags,
        SfntCompositePlacementKind placementKind)
    {
        bool words = (flags & ArgumentsAreWords) != 0;

        if (placementKind == SfntCompositePlacementKind.Offset)
        {
            return words ? reader.ReadInt16() : reader.ReadInt8();
        }

        return words ? reader.ReadUInt16() : reader.ReadUInt8();
    }

    static SfntCompositeTransform ReadTransform(
        BigEndianReader reader,
        int flags)
    {
        if ((flags & WeHaveScale) != 0)
        {
            double scale = reader.ReadF2Dot14();
            return new SfntCompositeTransform(
                SfntCompositeTransformKind.UniformScale,
                scale,
                0,
                0,
                scale);
        }

        if ((flags & WeHaveSeparateScale) != 0)
        {
            return new SfntCompositeTransform(
                SfntCompositeTransformKind.SeparateScale,
                reader.ReadF2Dot14(),
                0,
                0,
                reader.ReadF2Dot14());
        }

        if ((flags & WeHaveTwoByTwo) != 0)
        {
            return new SfntCompositeTransform(
                SfntCompositeTransformKind.TwoByTwo,
                reader.ReadF2Dot14(),
                reader.ReadF2Dot14(),
                reader.ReadF2Dot14(),
                reader.ReadF2Dot14());
        }

        return new SfntCompositeTransform(
            SfntCompositeTransformKind.Identity,
            1,
            0,
            0,
            1);
    }

    static SfntCompositeOffsetScaling ReadOffsetScaling(
        int flags,
        SfntCompositePlacementKind placementKind)
    {
        if (placementKind == SfntCompositePlacementKind.PointAttachment)
        {
            return SfntCompositeOffsetScaling.Default;
        }

        if ((flags & ScaledComponentOffset) != 0)
        {
            return SfntCompositeOffsetScaling.Scaled;
        }

        if ((flags & UnscaledComponentOffset) != 0)
        {
            return SfntCompositeOffsetScaling.Unscaled;
        }

        return SfntCompositeOffsetScaling.Default;
    }

    static void SkipHeader(BigEndianReader reader)
    {
        int remaining = HeaderLength;

        while (remaining > 0)
        {
            reader.ReadUInt8();
            remaining -= 1;
        }
    }

    static byte[] ReadBytes(BigEndianReader reader, int length)
    {
        byte[] bytes = new byte[length];
        int index = 0;

        while (index < bytes.Length)
        {
            bytes[index] = reader.ReadUInt8();
            index += 1;
        }

        return bytes;
    }

    static int ValidateTrailingPadding(BigEndianReader reader)
    {
        int paddingLength = reader.GetRemainingLength();

        while (reader.GetRemainingLength() != 0)
        {
            if (reader.ReadUInt8() != 0)
            {
                throw FontFormatException.Create(
                    "A composite glyph has nonzero trailing data.");
            }
        }

        return paddingLength;
    }
}
