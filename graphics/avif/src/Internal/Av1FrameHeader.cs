namespace avif;

internal class Av1FrameHeaderParseResult
{
    internal AvifResultCode Code;
    internal ParsedAv1FrameHeader? Header;

    internal Av1FrameHeaderParseResult(AvifResultCode code, ParsedAv1FrameHeader? header)
    {
        Code = code;
        Header = header;
    }
}

internal class Av1FrameHeaderState
{
    internal bool[] ReferenceValid;
    internal Av1FrameType[] ReferenceFrameTypes;
    internal bool[] ReferenceShowable;
    internal int[] ReferenceOrderHints;
    internal int[] ReferenceFrameIds;
    internal int[] ReferenceUpscaledWidths;
    internal int[] ReferenceFrameWidths;
    internal int[] ReferenceFrameHeights;
    internal int[] ReferenceRenderWidths;
    internal int[] ReferenceRenderHeights;
    internal int[][] PreviousGlobalMotionParameters;
    internal bool[][] SegmentFeatureEnabled;
    internal int[][] SegmentFeatureData;
    internal int[] LoopFilterReferenceDeltas;
    internal int[] LoopFilterModeDeltas;
    internal Av1FilmGrainParameters?[] ReferenceFilmGrainParameters;
    internal int[][][] ReferenceGlobalMotionParameters;
    internal bool[][][] ReferenceSegmentFeatureEnabled;
    internal int[][][] ReferenceSegmentFeatureData;
    internal int[][] ReferenceLoopFilterReferenceDeltas;
    internal int[][] ReferenceLoopFilterModeDeltas;
    internal int[][] ReferenceSegmentIds;
    internal int[] ReferenceMiRows;
    internal int[] ReferenceMiColumns;

    internal Av1FrameHeaderState()
    {
        ReferenceValid = new bool[Av1SyntaxConstants.StoredReferenceFrames];
        ReferenceFrameTypes = new Av1FrameType[Av1SyntaxConstants.StoredReferenceFrames];
        ReferenceShowable = new bool[Av1SyntaxConstants.StoredReferenceFrames];
        ReferenceOrderHints = new int[Av1SyntaxConstants.StoredReferenceFrames];
        ReferenceFrameIds = new int[Av1SyntaxConstants.StoredReferenceFrames];
        ReferenceUpscaledWidths = new int[Av1SyntaxConstants.StoredReferenceFrames];
        ReferenceFrameWidths = new int[Av1SyntaxConstants.StoredReferenceFrames];
        ReferenceFrameHeights = new int[Av1SyntaxConstants.StoredReferenceFrames];
        ReferenceRenderWidths = new int[Av1SyntaxConstants.StoredReferenceFrames];
        ReferenceRenderHeights = new int[Av1SyntaxConstants.StoredReferenceFrames];
        PreviousGlobalMotionParameters = new int[Av1SyntaxConstants.TotalRefsPerFrame][];
        SegmentFeatureEnabled = new bool[Av1SyntaxConstants.MaxSegments][];
        SegmentFeatureData = new int[Av1SyntaxConstants.MaxSegments][];
        LoopFilterReferenceDeltas = new int[] { 1, 0, 0, 0, -1, 0, -1, -1 };
        LoopFilterModeDeltas = new int[2];
        ReferenceFilmGrainParameters =
            new Av1FilmGrainParameters[Av1SyntaxConstants.StoredReferenceFrames];
        ReferenceGlobalMotionParameters =
            new int[Av1SyntaxConstants.StoredReferenceFrames][][];
        ReferenceSegmentFeatureEnabled =
            new bool[Av1SyntaxConstants.StoredReferenceFrames][][];
        ReferenceSegmentFeatureData =
            new int[Av1SyntaxConstants.StoredReferenceFrames][][];
        ReferenceLoopFilterReferenceDeltas =
            new int[Av1SyntaxConstants.StoredReferenceFrames][];
        ReferenceLoopFilterModeDeltas =
            new int[Av1SyntaxConstants.StoredReferenceFrames][];
        ReferenceSegmentIds =
            new int[Av1SyntaxConstants.StoredReferenceFrames][];
        ReferenceMiRows = new int[Av1SyntaxConstants.StoredReferenceFrames];
        ReferenceMiColumns = new int[Av1SyntaxConstants.StoredReferenceFrames];
        int index = 0;
        while (index < Av1SyntaxConstants.TotalRefsPerFrame)
        {
            PreviousGlobalMotionParameters[index] = CreateIdentityGlobalParameters();
            if (index < Av1SyntaxConstants.MaxSegments)
            {
                SegmentFeatureEnabled[index] =
                    new bool[Av1SyntaxConstants.SegmentFeatureCount];
                SegmentFeatureData[index] =
                    new int[Av1SyntaxConstants.SegmentFeatureCount];
            }

            index++;
        }

        index = 0;
        while (index < Av1SyntaxConstants.StoredReferenceFrames)
        {
            ReferenceGlobalMotionParameters[index] =
                CreateIdentityGlobalParameterSet();
            ReferenceSegmentFeatureEnabled[index] = CreateSegmentFeatureFlags();
            ReferenceSegmentFeatureData[index] = CreateSegmentFeatureValues();
            ReferenceLoopFilterReferenceDeltas[index] =
                new int[] { 1, 0, 0, 0, -1, 0, -1, -1 };
            ReferenceLoopFilterModeDeltas[index] = new int[2];
            ReferenceSegmentIds[index] = System.Array.Empty<int>();
            index++;
        }
    }

    internal void SetReference(
        int index,
        Av1FrameType frameType,
        int orderHint,
        int frameId,
        int upscaledWidth,
        int frameWidth,
        int frameHeight,
        int renderWidth,
        int renderHeight)
    {
        if (index < 0 || index >= ReferenceValid.Length)
        {
            return;
        }

        ReferenceValid[index] = true;
        ReferenceFrameTypes[index] = frameType;
        ReferenceOrderHints[index] = orderHint;
        ReferenceFrameIds[index] = frameId;
        ReferenceUpscaledWidths[index] = upscaledWidth;
        ReferenceFrameWidths[index] = frameWidth;
        ReferenceFrameHeights[index] = frameHeight;
        ReferenceRenderWidths[index] = renderWidth;
        ReferenceRenderHeights[index] = renderHeight;
    }

    internal void ClearReferences()
    {
        int index = 0;
        while (index < ReferenceValid.Length)
        {
            ReferenceValid[index] = false;
            ReferenceOrderHints[index] = 0;
            index++;
        }
    }

    internal void SetReferenceFilmGrain(int index, Av1FilmGrainParameters? parameters)
    {
        if (index < 0 || index >= ReferenceFilmGrainParameters.Length)
        {
            return;
        }

        ReferenceFilmGrainParameters[index] = parameters;
    }

    internal void SetReferenceShowable(int index, bool showable)
    {
        if (index < 0 || index >= ReferenceShowable.Length)
        {
            return;
        }

        ReferenceShowable[index] = showable;
    }

    internal void SaveReferenceState(
        int index,
        Av1GlobalMotionParameters? globalMotion,
        Av1SegmentationParameters? segmentation,
        Av1LoopFilterParameters? loopFilter,
        int[]? segmentIds,
        int miRows,
        int miColumns)
    {
        if (index < 0 || index >= Av1SyntaxConstants.StoredReferenceFrames)
        {
            return;
        }

        ReferenceGlobalMotionParameters[index] =
            CopyGlobalMotionParameters(globalMotion);
        ReferenceSegmentFeatureEnabled[index] =
            CopySegmentFeatureFlags(segmentation);
        ReferenceSegmentFeatureData[index] =
            CopySegmentFeatureValues(segmentation);
        ReferenceLoopFilterReferenceDeltas[index] =
            CopyLoopFilterReferenceDeltas(loopFilter);
        ReferenceLoopFilterModeDeltas[index] =
            CopyLoopFilterModeDeltas(loopFilter);
        ReferenceSegmentIds[index] = segmentIds == null
            ? System.Array.Empty<int>()
            : (int[])segmentIds.Clone();
        ReferenceMiRows[index] = miRows;
        ReferenceMiColumns[index] = miColumns;
    }

    internal bool LoadReferenceState(int index)
    {
        if (index < 0 || index >= Av1SyntaxConstants.StoredReferenceFrames)
        {
            return false;
        }

        PreviousGlobalMotionParameters =
            CopyGlobalMotionParameterSet(ReferenceGlobalMotionParameters[index]);
        SegmentFeatureEnabled =
            CopySegmentFeatureFlagSet(ReferenceSegmentFeatureEnabled[index]);
        SegmentFeatureData =
            CopySegmentFeatureValueSet(ReferenceSegmentFeatureData[index]);
        LoopFilterReferenceDeltas =
            (int[])ReferenceLoopFilterReferenceDeltas[index].Clone();
        LoopFilterModeDeltas =
            (int[])ReferenceLoopFilterModeDeltas[index].Clone();
        return true;
    }

    internal void LoadFrameState(
        Av1GlobalMotionParameters? globalMotion,
        Av1SegmentationParameters? segmentation,
        Av1LoopFilterParameters? loopFilter)
    {
        PreviousGlobalMotionParameters = CopyGlobalMotionParameters(globalMotion);
        SegmentFeatureEnabled = CopySegmentFeatureFlags(segmentation);
        SegmentFeatureData = CopySegmentFeatureValues(segmentation);
        LoopFilterReferenceDeltas =
            CopyLoopFilterReferenceDeltas(loopFilter);
        LoopFilterModeDeltas = CopyLoopFilterModeDeltas(loopFilter);
    }

    internal int[]? GetReferenceSegmentIds(
        int index,
        int miRows,
        int miColumns)
    {
        if (index < 0 || index >= Av1SyntaxConstants.StoredReferenceFrames
            || !ReferenceValid[index]
            || ReferenceMiRows[index] != miRows
            || ReferenceMiColumns[index] != miColumns
            || ReferenceSegmentIds[index] == null
            || ReferenceSegmentIds[index].Length != (long)miRows * miColumns)
        {
            return null;
        }
        return (int[])ReferenceSegmentIds[index].Clone();
    }

    internal static int[] CreateIdentityGlobalParameters()
    {
        return new int[] { 0, 0, 65536, 0, 0, 65536 };
    }

    static int[][] CreateIdentityGlobalParameterSet()
    {
        int[][] values = new int[Av1SyntaxConstants.TotalRefsPerFrame][];
        int index = 0;
        while (index < values.Length)
        {
            values[index] = CreateIdentityGlobalParameters();
            index++;
        }
        return values;
    }

    static bool[][] CreateSegmentFeatureFlags()
    {
        bool[][] values = new bool[Av1SyntaxConstants.MaxSegments][];
        int index = 0;
        while (index < values.Length)
        {
            values[index] = new bool[Av1SyntaxConstants.SegmentFeatureCount];
            index++;
        }
        return values;
    }

    static int[][] CreateSegmentFeatureValues()
    {
        int[][] values = new int[Av1SyntaxConstants.MaxSegments][];
        int index = 0;
        while (index < values.Length)
        {
            values[index] = new int[Av1SyntaxConstants.SegmentFeatureCount];
            index++;
        }
        return values;
    }

    static int[][] CopyGlobalMotionParameters(
        Av1GlobalMotionParameters? globalMotion)
    {
        if (globalMotion == null || globalMotion.Parameters == null
            || globalMotion.Parameters.Length
                != Av1SyntaxConstants.TotalRefsPerFrame)
        {
            return CreateIdentityGlobalParameterSet();
        }
        return CopyGlobalMotionParameterSet(globalMotion.Parameters);
    }

    static int[][] CopyGlobalMotionParameterSet(int[][] source)
    {
        int[][] copy = new int[Av1SyntaxConstants.TotalRefsPerFrame][];
        int index = 0;
        while (index < copy.Length)
        {
            copy[index] = source == null || index >= source.Length
                || source[index] == null || source[index].Length != 6
                ? CreateIdentityGlobalParameters()
                : (int[])source[index].Clone();
            index++;
        }
        return copy;
    }

    static bool[][] CopySegmentFeatureFlags(
        Av1SegmentationParameters? segmentation)
    {
        return segmentation == null
            ? CreateSegmentFeatureFlags()
            : CopySegmentFeatureFlagSet(segmentation.FeatureEnabled);
    }

    static bool[][] CopySegmentFeatureFlagSet(bool[][]? source)
    {
        bool[][] copy = CreateSegmentFeatureFlags();
        int segment = 0;
        while (source != null && segment < copy.Length
            && segment < source.Length)
        {
            if (source[segment] != null
                && source[segment].Length
                    == Av1SyntaxConstants.SegmentFeatureCount)
            {
                copy[segment] = (bool[])source[segment].Clone();
            }
            segment++;
        }
        return copy;
    }

    static int[][] CopySegmentFeatureValues(
        Av1SegmentationParameters? segmentation)
    {
        return segmentation == null
            ? CreateSegmentFeatureValues()
            : CopySegmentFeatureValueSet(segmentation.FeatureData);
    }

    static int[][] CopySegmentFeatureValueSet(int[][]? source)
    {
        int[][] copy = CreateSegmentFeatureValues();
        int segment = 0;
        while (source != null && segment < copy.Length
            && segment < source.Length)
        {
            if (source[segment] != null
                && source[segment].Length
                    == Av1SyntaxConstants.SegmentFeatureCount)
            {
                copy[segment] = (int[])source[segment].Clone();
            }
            segment++;
        }
        return copy;
    }

    static int[] CopyLoopFilterReferenceDeltas(
        Av1LoopFilterParameters? loopFilter)
    {
        return loopFilter == null || loopFilter.ReferenceDeltas == null
            || loopFilter.ReferenceDeltas.Length
                != Av1SyntaxConstants.TotalRefsPerFrame
            ? new int[] { 1, 0, 0, 0, -1, 0, -1, -1 }
            : (int[])loopFilter.ReferenceDeltas.Clone();
    }

    static int[] CopyLoopFilterModeDeltas(Av1LoopFilterParameters? loopFilter)
    {
        return loopFilter == null || loopFilter.ModeDeltas == null
            || loopFilter.ModeDeltas.Length != 2
            ? new int[2]
            : (int[])loopFilter.ModeDeltas.Clone();
    }
}

internal class Av1FrameSize
{
    internal int UpscaledWidth;
    internal int FrameWidth;
    internal int FrameHeight;
    internal int RenderWidth;
    internal int RenderHeight;
    internal bool UsesSuperres;
    internal int SuperresDenominator;
    internal int MiColumns;
    internal int MiRows;
}

internal class Av1TileInfo
{
    internal bool UniformSpacing;
    internal int TileColumnsLog2;
    internal int TileRowsLog2;
    internal int TileColumns;
    internal int TileRows;
    internal int[]? MiColumnStarts;
    internal int[]? MiRowStarts;
    internal int ContextUpdateTileId;
    internal int TileSizeBytes;
}

internal class Av1QuantizationParameters
{
    internal int BaseQIndex;
    internal int DeltaYDc;
    internal int DeltaUDc;
    internal int DeltaUAc;
    internal int DeltaVDc;
    internal int DeltaVAc;
    internal bool DifferentUvDelta;
    internal bool UsesQuantizerMatrix;
    internal int MatrixLevelY;
    internal int MatrixLevelU;
    internal int MatrixLevelV;
    internal bool DeltaQPresent;
    internal int DeltaQResolution;
    internal bool DeltaLoopFilterPresent;
    internal int DeltaLoopFilterResolution;
    internal bool DeltaLoopFilterMulti;
}

internal class Av1SegmentationParameters
{
    internal bool Enabled;
    internal bool UpdateMap;
    internal bool TemporalUpdate;
    internal bool UpdateData;
    internal bool[][]? FeatureEnabled;
    internal int[][]? FeatureData;
    internal bool SegmentIdPreSkip;
    internal int LastActiveSegmentId;
}

internal class Av1LoopFilterParameters
{
    internal int[]? Levels;
    internal int Sharpness;
    internal bool DeltaEnabled;
    internal bool DeltaUpdate;
    internal int[]? ReferenceDeltas;
    internal int[]? ModeDeltas;
}

internal class Av1CdefParameters
{
    internal int Damping;
    internal int Bits;
    internal int[]? YPrimaryStrengths;
    internal int[]? YSecondaryStrengths;
    internal int[]? UvPrimaryStrengths;
    internal int[]? UvSecondaryStrengths;
}

internal class Av1LoopRestorationParameters
{
    internal Av1RestorationType[]? Types;
    internal int[]? UnitSizes;
    internal bool UsesRestoration;
}

internal class Av1GlobalMotionParameters
{
    internal Av1WarpModelType[]? Types;
    internal int[][]? Parameters;
}

internal class Av1FilmGrainParameters
{
    internal bool ApplyGrain;
    internal int GrainSeed;
    internal bool UpdateGrain;
    internal int ReferenceIndex;
    internal int[][] YPoints;
    internal int[][] CbPoints;
    internal int[][] CrPoints;
    internal bool ChromaScalingFromLuma;
    internal int ScalingShift;
    internal int ArCoefficientLag;
    internal int[] ArCoefficientsY;
    internal int[] ArCoefficientsCb;
    internal int[] ArCoefficientsCr;
    internal int ArCoefficientShift;
    internal int GrainScaleShift;
    internal int CbMultiplier;
    internal int CbLumaMultiplier;
    internal int CbOffset;
    internal int CrMultiplier;
    internal int CrLumaMultiplier;
    internal int CrOffset;
    internal bool Overlap;
    internal bool ClipToRestrictedRange;

    internal Av1FilmGrainParameters()
    {
        YPoints = System.Array.Empty<int[]>();
        CbPoints = System.Array.Empty<int[]>();
        CrPoints = System.Array.Empty<int[]>();
        ArCoefficientsY = System.Array.Empty<int>();
        ArCoefficientsCb = System.Array.Empty<int>();
        ArCoefficientsCr = System.Array.Empty<int>();
    }
}

internal class ParsedAv1FrameHeader
{
    internal int TemporalId;
    internal int SpatialId;
    internal bool ShowExistingFrame;
    internal int FrameToShowMapIndex;
    internal Av1FrameType FrameType;
    internal bool FrameIsIntra;
    internal bool ShowFrame;
    internal bool ShowableFrame;
    internal bool FramePresentationTimePresent;
    internal ulong FramePresentationTime;
    internal bool ErrorResilientMode;
    internal bool DisableCdfUpdate;
    internal bool AllowScreenContentTools;
    internal bool ForceIntegerMotionVectors;
    internal int CurrentFrameId;
    internal int DisplayFrameId;
    internal bool FrameSizeOverride;
    internal int OrderHint;
    internal int PrimaryReferenceFrame;
    internal bool BufferRemovalTimePresent;
    internal ulong[] BufferRemovalTimes;
    internal int RefreshFrameFlags;
    internal int[] ReferenceFrameIndexes;
    internal int[] DeltaFrameIds;
    internal bool FrameReferencesShortSignaling;
    internal int LastFrameIndex;
    internal int GoldenFrameIndex;
    internal bool AllowHighPrecisionMotionVectors;
    internal Av1InterpolationFilter InterpolationFilter;
    internal bool MotionModeSwitchable;
    internal bool UseReferenceFrameMotionVectors;
    internal bool AllowIntraBlockCopy;
    internal bool DisableFrameEndUpdateCdf;
    internal Av1FrameSize? Size;
    internal Av1TileInfo? Tiles;
    internal Av1QuantizationParameters? Quantization;
    internal Av1SegmentationParameters? Segmentation;
    internal bool CodedLossless;
    internal bool AllLossless;
    internal Av1LoopFilterParameters? LoopFilter;
    internal Av1CdefParameters? Cdef;
    internal Av1LoopRestorationParameters? LoopRestoration;
    internal Av1TransformMode TransformMode;
    internal bool ReferenceSelect;
    internal bool SkipModePresent;
    internal int[] SkipModeFrames;
    internal bool AllowWarpedMotion;
    internal bool ReducedTransformSet;
    internal Av1GlobalMotionParameters? GlobalMotion;
    internal Av1FilmGrainParameters? FilmGrain;
    internal long BitsConsumed;
    internal byte[] SourcePayload;

    internal ParsedAv1FrameHeader()
    {
        FrameToShowMapIndex = -1;
        PrimaryReferenceFrame = 7;
        BufferRemovalTimes = new ulong[Av1SyntaxConstants.MaxOperatingPoints];
        ReferenceFrameIndexes = new int[Av1SyntaxConstants.RefsPerFrame];
        DeltaFrameIds = new int[Av1SyntaxConstants.RefsPerFrame];
        LastFrameIndex = -1;
        GoldenFrameIndex = -1;
        InterpolationFilter = Av1InterpolationFilter.EightTap;
        SkipModeFrames = new int[] { -1, -1 };
        SourcePayload = System.Array.Empty<byte>();
    }
}
