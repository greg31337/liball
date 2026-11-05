using System;

namespace avif;

// AV1 specification sections 5.11.4, 6.10.5, and 7.10.
internal class Av1BlockModeSyntax
{
    internal int SegmentId;
    internal bool SegmentIdPredicted;
    internal bool SkipMode;
    internal bool Skip;
    internal bool DeltaQuantizerPresent;
    internal int DeltaQuantizer;
    internal bool DeltaLoopFilterPresent;
    internal int[] DeltaLoopFilters;
    internal bool IsInter;
    internal Av1ReferenceFrame[] ReferenceFrames;
    internal Av1PredictionMode YMode;
    internal Av1PredictionMode UvMode;
    internal int TransformDepth;

    internal Av1BlockModeSyntax()
    {
        DeltaLoopFilters = new int[4];
        ReferenceFrames = new Av1ReferenceFrame[]
        {
            Av1ReferenceFrame.None,
            Av1ReferenceFrame.None
        };
        YMode = Av1PredictionMode.Dc;
        UvMode = Av1PredictionMode.Dc;
    }
}

internal class Av1ModeInfo
{
    internal int MiRow;
    internal int MiColumn;
    internal int VisibleMiWidth;
    internal int VisibleMiHeight;
    internal int SuperblockIndex;
    internal Av1BlockSize BlockSize;
    internal int SegmentId;
    internal bool SegmentIdPredicted;
    internal bool SkipMode;
    internal bool Skip;
    internal bool IsInter;
    internal Av1ReferenceFrame[] ReferenceFrames;
    internal Av1PredictionMode YMode;
    internal Av1PredictionMode UvMode;
    internal Av1TransformSize TransformSize;
    internal int CurrentQIndex;
    internal int EffectiveQIndex;
    internal int[] RawDeltaLoopFilters;
    internal int[] DeltaLoopFilters;
    internal bool Lossless;

    internal Av1ModeInfo()
    {
        ReferenceFrames = new Av1ReferenceFrame[2];
        RawDeltaLoopFilters = new int[4];
        DeltaLoopFilters = new int[4];
    }
}

internal class Av1BlockNeighborContexts
{
    internal Av1ModeInfo? Above;
    internal Av1ModeInfo? Left;
    internal Av1ModeInfo? UpperLeft;
    internal bool AboveAvailable;
    internal bool LeftAvailable;
    internal int SkipContext;
    internal int SkipModeContext;
    internal int IsInterContext;
    internal int SegmentIdContext;
    internal int SegmentPredictedContext;
    internal int TransformSizeContext;
    internal int PredictedSegmentId;
}

internal class Av1ModeInfoGrid
{
    internal int RowStart;
    internal int RowEnd;
    internal int ColumnStart;
    internal int ColumnEnd;
    internal int RowCount;
    internal int ColumnCount;
    internal int FrameMiRows;
    internal int FrameMiColumns;
    internal Av1ModeInfo[]? Cells;
    internal int[]? PreviousSegmentIds;
    internal bool Closed;
    internal AvifResultCode Code;

    internal Av1ModeInfoGrid(
        ParsedAv1FrameHeader header,
        int tileIndex,
        AvifSecurityLimits limits,
        int[]? previousSegmentIds)
    {
        Code = Initialize(header, tileIndex, limits, previousSegmentIds);
    }

    internal Av1ModeInfo? Get(int miRow, int miColumn)
    {
        if (Closed || Code != AvifResultCode.Success
            || miRow < RowStart || miRow >= RowEnd
            || miColumn < ColumnStart || miColumn >= ColumnEnd)
        {
            return null;
        }

        return (Cells ?? throw new InvalidOperationException("Invalid internal state."))[(miRow - RowStart) * ColumnCount + miColumn - ColumnStart];
    }

    internal int GetPreviousSegmentId(
        int miRow,
        int miColumn,
        int miWidth,
        int miHeight)
    {
        if (PreviousSegmentIds == null)
        {
            return 0;
        }

        int rowEnd = Minimum(miRow + miHeight, FrameMiRows);
        int columnEnd = Minimum(miColumn + miWidth, FrameMiColumns);
        int segmentId = Av1SyntaxConstants.MaxSegments - 1;
        int row = miRow;
        while (row < rowEnd)
        {
            int column = miColumn;
            while (column < columnEnd)
            {
                int value = PreviousSegmentIds[row * FrameMiColumns + column];
                if (value < segmentId)
                {
                    segmentId = value;
                }

                column++;
            }

            row++;
        }

        return segmentId;
    }

    internal AvifResultCode Store(Av1PartitionEvent block, Av1ModeInfo mode)
    {
        if (Closed)
        {
            return AvifResultCode.Closed;
        }

        if (Code != AvifResultCode.Success)
        {
            return Code;
        }

        if (block == null || mode == null
            || block.Kind != Av1PartitionEventKind.Block
            || block.VisibleMiWidth <= 0 || block.VisibleMiHeight <= 0
            || block.MiRow < RowStart || block.MiColumn < ColumnStart
            || block.MiRow > RowEnd - block.VisibleMiHeight
            || block.MiColumn > ColumnEnd - block.VisibleMiWidth)
        {
            return AvifResultCode.InvalidArgument;
        }

        int row = block.MiRow;
        while (row < block.MiRow + block.VisibleMiHeight)
        {
            int column = block.MiColumn;
            while (column < block.MiColumn + block.VisibleMiWidth)
            {
                int index = (row - RowStart) * ColumnCount + column - ColumnStart;
                if ((Cells ?? throw new InvalidOperationException("Invalid internal state."))[index] != null)
                {
                    return AvifResultCode.InvalidState;
                }

                column++;
            }

            row++;
        }

        row = block.MiRow;
        while (row < block.MiRow + block.VisibleMiHeight)
        {
            int column = block.MiColumn;
            while (column < block.MiColumn + block.VisibleMiWidth)
            {
                (Cells ?? throw new InvalidOperationException("Invalid internal state."))[(row - RowStart) * ColumnCount + column - ColumnStart] = mode;
                column++;
            }

            row++;
        }

        return AvifResultCode.Success;
    }

    internal AvifResultCode Close()
    {
        if (Closed)
        {
            return AvifResultCode.Success;
        }

        Cells = System.Array.Empty<Av1ModeInfo>();
        PreviousSegmentIds = null;
        Closed = true;
        return AvifResultCode.Success;
    }

    AvifResultCode Initialize(
        ParsedAv1FrameHeader header,
        int tileIndex,
        AvifSecurityLimits limits,
        int[]? previousSegmentIds)
    {
        if (header == null || header.Size == null || header.Tiles == null
            || limits == null || header.Tiles.MiRowStarts == null
            || header.Tiles.MiColumnStarts == null
            || header.Tiles.TileColumns <= 0 || header.Tiles.TileRows <= 0
            || tileIndex < 0
            || tileIndex >= header.Tiles.TileColumns * header.Tiles.TileRows)
        {
            return AvifResultCode.InvalidArgument;
        }

        ResourceGuard resources = new ResourceGuard(limits);
        if (resources.Code != AvifResultCode.Success)
        {
            return resources.Code;
        }

        AvifResultCode dimensions = resources.CheckImageDimensions(
            header.Size.FrameWidth,
            header.Size.FrameHeight);
        if (dimensions != AvifResultCode.Success)
        {
            return dimensions;
        }

        int tileRow = tileIndex / header.Tiles.TileColumns;
        int tileColumn = tileIndex - tileRow * header.Tiles.TileColumns;
        if (header.Tiles.MiRowStarts.Length <= tileRow + 1
            || header.Tiles.MiColumnStarts.Length <= tileColumn + 1)
        {
            return AvifResultCode.InvalidArgument;
        }

        FrameMiRows = header.Size.MiRows;
        FrameMiColumns = header.Size.MiColumns;
        RowStart = header.Tiles.MiRowStarts[tileRow];
        RowEnd = header.Tiles.MiRowStarts[tileRow + 1];
        ColumnStart = header.Tiles.MiColumnStarts[tileColumn];
        ColumnEnd = header.Tiles.MiColumnStarts[tileColumn + 1];
        RowCount = RowEnd - RowStart;
        ColumnCount = ColumnEnd - ColumnStart;
        if (FrameMiRows <= 0 || FrameMiColumns <= 0 || RowCount <= 0 || ColumnCount <= 0
            || RowStart < 0 || ColumnStart < 0
            || RowEnd > FrameMiRows || ColumnEnd > FrameMiColumns)
        {
            return AvifResultCode.InvalidAv1Bitstream;
        }

        long cellCount = (long)RowCount * ColumnCount;
        long retainedBytes = cellCount * 64;
        if (cellCount > int.MaxValue || retainedBytes > limits.MaxDecodedBytes)
        {
            return AvifResultCode.LimitExceeded;
        }

        if (previousSegmentIds != null
            && previousSegmentIds.Length != (long)FrameMiRows * FrameMiColumns)
        {
            return AvifResultCode.InvalidArgument;
        }

        PreviousSegmentIds = previousSegmentIds;
        Cells = new Av1ModeInfo[(int)cellCount];
        return AvifResultCode.Success;
    }

    static int Minimum(int first, int second)
    {
        return first < second ? first : second;
    }
}

internal class Av1BlockModeDecodeResult
{
    internal AvifResultCode Code;
    internal Av1ModeInfo? Mode;

    internal Av1BlockModeDecodeResult(AvifResultCode code, Av1ModeInfo? mode)
    {
        Code = code;
        Mode = mode;
    }
}

internal class Av1BlockModeDecoder
{
    internal ParsedAv1FrameHeader? Header;
    internal Av1ModeInfoGrid? Grid;
    internal int CurrentQIndex;
    internal int[]? DeltaLoopFilters;
    internal int PlaneCount;
    internal bool ReadDeltas;
    internal int CurrentSuperblockIndex;
    internal AvifResultCode Code;

    internal static Av1BlockModeDecoder Create(
        ParsedAv1FrameHeader header,
        int tileIndex,
        AvifSecurityLimits limits,
        int[]? previousSegmentIds)
    {
        return new Av1BlockModeDecoder(
            header,
            tileIndex,
            limits,
            previousSegmentIds,
            3);
    }

    internal Av1BlockModeDecoder(
        ParsedAv1FrameHeader header,
        int tileIndex,
        AvifSecurityLimits limits,
        int[]? previousSegmentIds,
        int planeCount)
    {
        Initialize(header, tileIndex, limits, previousSegmentIds, planeCount);
    }

    internal static Av1BlockModeDecoder Create(
        ParsedAv1FrameHeader header,
        int tileIndex,
        AvifSecurityLimits limits,
        int[]? previousSegmentIds,
        int planeCount)
    {
        return new Av1BlockModeDecoder(
            header,
            tileIndex,
            limits,
            previousSegmentIds,
            planeCount);
    }

    void Initialize(
        ParsedAv1FrameHeader header,
        int tileIndex,
        AvifSecurityLimits limits,
        int[]? previousSegmentIds,
        int planeCount)
    {
        Header = header;
        DeltaLoopFilters = new int[4];
        PlaneCount = planeCount;
        CurrentSuperblockIndex = -1;
        Grid = new Av1ModeInfoGrid(header, tileIndex, limits, previousSegmentIds);
        Code = Grid.Code;
        if (Code == AvifResultCode.Success
            && (header.Quantization == null || planeCount != 1 && planeCount != 3))
        {
            Code = AvifResultCode.InvalidArgument;
        }

        if (Code == AvifResultCode.Success)
        {
            CurrentQIndex = (header.Quantization ?? throw new InvalidOperationException("Invalid internal state.")).BaseQIndex;
        }
    }

    internal Av1BlockNeighborContexts GetNeighborContexts(Av1PartitionEvent block)
    {
        Av1BlockNeighborContexts contexts = new Av1BlockNeighborContexts();
        if (block == null || Code != AvifResultCode.Success)
        {
            return contexts;
        }

        contexts.Above = (Grid ?? throw new InvalidOperationException("Invalid internal state.")).Get(block.MiRow - 1, block.MiColumn);
        contexts.Left = Grid.Get(block.MiRow, block.MiColumn - 1);
        contexts.UpperLeft = Grid.Get(block.MiRow - 1, block.MiColumn - 1);
        contexts.AboveAvailable = contexts.Above != null;
        contexts.LeftAvailable = contexts.Left != null;
        contexts.SkipContext = BooleanValue(contexts.AboveAvailable && (contexts.Above ?? throw new InvalidOperationException("Invalid internal state.")).Skip)
            + BooleanValue(contexts.LeftAvailable && (contexts.Left ?? throw new InvalidOperationException("Invalid internal state.")).Skip);
        contexts.SkipModeContext =
            BooleanValue(contexts.AboveAvailable && (contexts.Above ?? throw new InvalidOperationException("Invalid internal state.")).SkipMode)
            + BooleanValue(contexts.LeftAvailable && (contexts.Left ?? throw new InvalidOperationException("Invalid internal state.")).SkipMode);
        contexts.SegmentPredictedContext =
            BooleanValue(contexts.AboveAvailable && (contexts.Above ?? throw new InvalidOperationException("Invalid internal state.")).SegmentIdPredicted)
            + BooleanValue(contexts.LeftAvailable && (contexts.Left ?? throw new InvalidOperationException("Invalid internal state.")).SegmentIdPredicted);
        contexts.PredictedSegmentId = PredictSegmentId(contexts);
        contexts.SegmentIdContext = GetSegmentIdContext(contexts);
        contexts.IsInterContext = GetIsInterContext(contexts);
        contexts.TransformSizeContext = GetTransformSizeContext(block, contexts);
        return contexts;
    }

    internal Av1BlockModeDecodeResult Decode(
        Av1PartitionEvent block,
        Av1BlockModeSyntax syntax)
    {
        if (Code != AvifResultCode.Success)
        {
            return new Av1BlockModeDecodeResult(Code, null);
        }

        AvifResultCode validation = ValidateBlockAndSyntax(block, syntax);
        if (validation != AvifResultCode.Success)
        {
            return new Av1BlockModeDecodeResult(validation, null);
        }

        BeginSuperblock(block.SuperblockIndex);
        int segmentId = ResolveSegmentId(block, syntax);
        bool segmentSkip = IsFeatureActive(segmentId, Av1SegmentFeature.Skip);
        bool segmentGlobal = IsFeatureActive(segmentId, Av1SegmentFeature.GlobalMotion);
        bool segmentReference = IsFeatureActive(segmentId, Av1SegmentFeature.ReferenceFrame);
        bool skipModeAllowed = (Header ?? throw new InvalidOperationException("Invalid internal state.")).SkipModePresent
            && !segmentSkip && !segmentGlobal && !segmentReference
            && Av1ConversionTables.GetBlockWidth(block.BlockSize) >= 8
            && Av1ConversionTables.GetBlockHeight(block.BlockSize) >= 8;
        bool skipMode = skipModeAllowed && syntax.SkipMode;
        bool skip = segmentSkip || skipMode || syntax.Skip;

        bool wholeSuperblockSkipped = skip
            && block.VisibleMiWidth == Av1ConversionTables.GetBlockMiWidth(block.BlockSize)
            && block.VisibleMiHeight == Av1ConversionTables.GetBlockMiHeight(block.BlockSize)
            && ((block.BlockSize == Av1BlockSize.Block64x64)
                || block.BlockSize == Av1BlockSize.Block128x128);
        if (ReadDeltas && !wholeSuperblockSkipped)
        {
            ApplyDeltas(syntax);
            ReadDeltas = false;
        }

        bool isInter = ResolveIsInter(segmentId, skipMode, segmentReference, segmentGlobal, syntax);
        Av1ModeInfo mode = new Av1ModeInfo();
        mode.MiRow = block.MiRow;
        mode.MiColumn = block.MiColumn;
        mode.VisibleMiWidth = block.VisibleMiWidth;
        mode.VisibleMiHeight = block.VisibleMiHeight;
        mode.SuperblockIndex = block.SuperblockIndex;
        mode.BlockSize = block.BlockSize;
        mode.SegmentId = segmentId;
        mode.SegmentIdPredicted = syntax.SegmentIdPredicted;
        mode.SkipMode = skipMode;
        mode.Skip = skip;
        mode.IsInter = isInter;
        mode.CurrentQIndex = CurrentQIndex;
        mode.EffectiveQIndex = GetEffectiveQIndex(segmentId);
        mode.Lossless = IsLossless(mode.EffectiveQIndex);
        mode.TransformSize = ResolveTransformSize(block.BlockSize, skip, mode.Lossless, syntax.TransformDepth);
        if (mode.TransformSize == Av1TransformSize.Invalid)
        {
            return new Av1BlockModeDecodeResult(AvifResultCode.InvalidAv1Bitstream, null);
        }

        int filterIndex = 0;
        while (filterIndex < mode.DeltaLoopFilters.Length)
        {
            mode.RawDeltaLoopFilters[filterIndex] = (DeltaLoopFilters ?? throw new InvalidOperationException("Invalid internal state."))[filterIndex];
            mode.DeltaLoopFilters[filterIndex] = DeltaLoopFilters[filterIndex]
                + GetSegmentLoopFilterDelta(segmentId, filterIndex);
            filterIndex++;
        }

        SetPredictionState(mode, segmentId, skipMode, segmentReference, segmentGlobal, syntax);
        AvifResultCode stored = (Grid ?? throw new InvalidOperationException("Invalid internal state.")).Store(block, mode);
        if (stored != AvifResultCode.Success)
        {
            return new Av1BlockModeDecodeResult(stored, null);
        }

        return new Av1BlockModeDecodeResult(AvifResultCode.Success, mode);
    }

    internal Av1BlockModeDecodeResult DecodeSymbols(
        Av1PartitionEvent block,
        Av1SymbolDecoder symbols,
        Av1TileCdfContext cdfContext)
    {
        if (block == null || symbols == null || cdfContext == null
            || Code != AvifResultCode.Success
            || symbols.Code != AvifResultCode.Success)
        {
            return new Av1BlockModeDecodeResult(
                Code == AvifResultCode.Success
                    ? AvifResultCode.InvalidArgument
                    : Code,
                null);
        }

        BeginSuperblock(block.SuperblockIndex);
        Av1BlockNeighborContexts neighbors = GetNeighborContexts(block);
        Av1BlockModeSyntax syntax = new Av1BlockModeSyntax();
        bool segmentBeforeSkip = (Header ?? throw new InvalidOperationException("Invalid internal state.")).Segmentation != null
            && (Header.Segmentation ?? throw new InvalidOperationException("Invalid internal state.")).Enabled
            && Header.Segmentation.SegmentIdPreSkip;
        if (segmentBeforeSkip)
        {
            AvifResultCode segmentCode = ReadSegmentId(
                block,
                neighbors,
                false,
                symbols,
                cdfContext,
                syntax);
            if (segmentCode != AvifResultCode.Success)
            {
                return new Av1BlockModeDecodeResult(segmentCode, null);
            }
        }

        int earlySegmentId = segmentBeforeSkip ? syntax.SegmentId : 0;
        bool forcedBySegment = IsFeatureActive(earlySegmentId, Av1SegmentFeature.Skip)
            || IsFeatureActive(earlySegmentId, Av1SegmentFeature.ReferenceFrame)
            || IsFeatureActive(earlySegmentId, Av1SegmentFeature.GlobalMotion);
        bool skipModeAllowed = Header.SkipModePresent && !forcedBySegment
            && Av1ConversionTables.GetBlockWidth(block.BlockSize) >= 8
            && Av1ConversionTables.GetBlockHeight(block.BlockSize) >= 8;
        if (skipModeAllowed)
        {
            int skipMode = ReadCdfSymbol(
                symbols,
                cdfContext,
                "Default_Skip_Mode_Cdf",
                neighbors.SkipModeContext);
            if (skipMode < 0)
            {
                return new Av1BlockModeDecodeResult(AvifResultCode.InvalidAv1Bitstream, null);
            }

            syntax.SkipMode = skipMode != 0;
        }

        bool segmentForcesSkip = segmentBeforeSkip
            && IsFeatureActive(earlySegmentId, Av1SegmentFeature.Skip);
        if (!segmentForcesSkip && !syntax.SkipMode)
        {
            int skip = ReadCdfSymbol(
                symbols,
                cdfContext,
                "Default_Skip_Cdf",
                neighbors.SkipContext);
            if (skip < 0)
            {
                return new Av1BlockModeDecodeResult(AvifResultCode.InvalidAv1Bitstream, null);
            }

            syntax.Skip = skip != 0;
        }

        if (!segmentBeforeSkip)
        {
            AvifResultCode segmentCode = ReadSegmentId(
                block,
                neighbors,
                syntax.Skip || syntax.SkipMode,
                symbols,
                cdfContext,
                syntax);
            if (segmentCode != AvifResultCode.Success)
            {
                return new Av1BlockModeDecodeResult(segmentCode, null);
            }
        }

        bool wholeSuperblockSkipped = (syntax.Skip || syntax.SkipMode)
            && (block.BlockSize == Av1BlockSize.Block64x64
                || block.BlockSize == Av1BlockSize.Block128x128)
            && block.VisibleMiWidth == Av1ConversionTables.GetBlockMiWidth(block.BlockSize)
            && block.VisibleMiHeight == Av1ConversionTables.GetBlockMiHeight(block.BlockSize);
        if (ReadDeltas && !wholeSuperblockSkipped)
        {
            syntax.DeltaQuantizerPresent = true;
            syntax.DeltaQuantizer = ReadDelta(
                symbols,
                cdfContext,
                "Default_Delta_Q_Cdf",
                (Header.Quantization ?? throw new InvalidOperationException("Invalid internal state.")).DeltaQResolution);
            if (Header.Quantization.DeltaLoopFilterPresent)
            {
                syntax.DeltaLoopFilterPresent = true;
                int count = Header.Quantization.DeltaLoopFilterMulti
                    ? (PlaneCount == 1 ? 2 : 4)
                    : 1;
                int index = 0;
                while (index < count && symbols.Code == AvifResultCode.Success)
                {
                    syntax.DeltaLoopFilters[index] = ReadDelta(
                        symbols,
                        cdfContext,
                        "Default_Delta_Lf_Cdf",
                        Header.Quantization.DeltaLoopFilterResolution);
                    index++;
                }
            }
        }

        int segmentId = ResolveSegmentId(block, syntax);
        bool referenceFeature = IsFeatureActive(segmentId, Av1SegmentFeature.ReferenceFrame);
        bool globalFeature = IsFeatureActive(segmentId, Av1SegmentFeature.GlobalMotion);
        if (!Header.FrameIsIntra && !syntax.SkipMode && !referenceFeature && !globalFeature)
        {
            int isInter = ReadCdfSymbol(
                symbols,
                cdfContext,
                "Default_Is_Inter_Cdf",
                neighbors.IsInterContext);
            if (isInter < 0)
            {
                return new Av1BlockModeDecodeResult(AvifResultCode.InvalidAv1Bitstream, null);
            }

            syntax.IsInter = isInter != 0;
        }

        bool isInterBlock = ResolveIsInter(
            segmentId,
            syntax.SkipMode,
            referenceFeature,
            globalFeature,
            syntax);
        if (isInterBlock)
        {
            syntax.YMode = globalFeature
                ? Av1PredictionMode.GlobalMv
                : Av1PredictionMode.NearestMv;
            syntax.ReferenceFrames[0] = Av1ReferenceFrame.Last;
        }
        else
        {
            AvifResultCode modeCode = ReadIntraModes(
                block,
                neighbors,
                symbols,
                cdfContext,
                syntax);
            if (modeCode != AvifResultCode.Success)
            {
                return new Av1BlockModeDecodeResult(modeCode, null);
            }
        }

        if (Header.TransformMode == Av1TransformMode.Select
            && !syntax.Skip && !syntax.SkipMode
            && GetProspectiveEffectiveQIndex(segmentId, syntax) != 0)
        {
            int maximumDepth = Av1ConversionTables.GetMaxTransformDepth(block.BlockSize);
            if (maximumDepth > 0)
            {
                string tableName = GetTransformCdfName(block.BlockSize);
                syntax.TransformDepth = ReadCdfSymbol(
                    symbols,
                    cdfContext,
                    tableName,
                    neighbors.TransformSizeContext);
                if (syntax.TransformDepth < 0 || syntax.TransformDepth > maximumDepth)
                {
                    return new Av1BlockModeDecodeResult(
                        AvifResultCode.InvalidAv1Bitstream,
                        null);
                }
            }
        }

        if (symbols.Code != AvifResultCode.Success)
        {
            return new Av1BlockModeDecodeResult(symbols.Code, null);
        }

        return Decode(block, syntax);
    }

    internal int GetPreviousSegmentId(Av1PartitionEvent block)
    {
        if (block == null)
        {
            return 0;
        }

        return (Grid ?? throw new InvalidOperationException("Invalid internal state.")).GetPreviousSegmentId(
            block.MiRow,
            block.MiColumn,
            block.VisibleMiWidth,
            block.VisibleMiHeight);
    }

    internal AvifResultCode Close()
    {
        AvifResultCode result = (Grid ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        if (result == AvifResultCode.Success)
        {
            Code = AvifResultCode.Closed;
        }

        return result;
    }

    internal static int NegDeinterleave(int difference, int predictor, int maximum)
    {
        if (difference < 0 || predictor < 0 || predictor >= maximum || maximum <= 0)
        {
            return -1;
        }

        if (predictor == 0)
        {
            return difference;
        }

        if (predictor == maximum - 1)
        {
            return maximum - difference - 1;
        }

        int distance = predictor < maximum - predictor - 1
            ? predictor
            : maximum - predictor - 1;
        if (difference <= 2 * distance)
        {
            return predictor + ((difference + 1) >> 1) * (1 - 2 * (difference & 1));
        }

        return predictor < maximum - predictor - 1
            ? difference
            : maximum - difference - 1;
    }

    AvifResultCode ReadSegmentId(
        Av1PartitionEvent block,
        Av1BlockNeighborContexts neighbors,
        bool skip,
        Av1SymbolDecoder symbols,
        Av1TileCdfContext cdfContext,
        Av1BlockModeSyntax syntax)
    {
        if ((Header ?? throw new InvalidOperationException("Invalid internal state.")).Segmentation == null || !(Header.Segmentation ?? throw new InvalidOperationException("Invalid internal state.")).Enabled)
        {
            syntax.SegmentId = 0;
            return AvifResultCode.Success;
        }

        if (!Header.Segmentation.UpdateMap)
        {
            syntax.SegmentId = GetPreviousSegmentId(block);
            return AvifResultCode.Success;
        }

        if (Header.Segmentation.TemporalUpdate)
        {
            int predicted = ReadCdfSymbol(
                symbols,
                cdfContext,
                "Default_Segment_Id_Predicted_Cdf",
                neighbors.SegmentPredictedContext);
            if (predicted < 0)
            {
                return AvifResultCode.InvalidAv1Bitstream;
            }

            syntax.SegmentIdPredicted = predicted != 0;
            if (syntax.SegmentIdPredicted)
            {
                syntax.SegmentId = GetPreviousSegmentId(block);
                return AvifResultCode.Success;
            }
        }

        int predictor = neighbors.PredictedSegmentId;
        if (skip || Header.Segmentation.LastActiveSegmentId == 0)
        {
            syntax.SegmentId = predictor;
            return AvifResultCode.Success;
        }

        int difference = ReadCdfSymbol(
            symbols,
            cdfContext,
            "Default_Segment_Id_Cdf",
            neighbors.SegmentIdContext);
        int maximum = Header.Segmentation.LastActiveSegmentId + 1;
        syntax.SegmentId = NegDeinterleave(difference, predictor, maximum);
        return syntax.SegmentId >= 0 && syntax.SegmentId < maximum
            ? AvifResultCode.Success
            : AvifResultCode.InvalidAv1Bitstream;
    }

    AvifResultCode ReadIntraModes(
        Av1PartitionEvent block,
        Av1BlockNeighborContexts neighbors,
        Av1SymbolDecoder symbols,
        Av1TileCdfContext cdfContext,
        Av1BlockModeSyntax syntax)
    {
        int yMode;
        if ((Header ?? throw new InvalidOperationException("Invalid internal state.")).FrameIsIntra)
        {
            int aboveMode = GetIntraModeContext(neighbors.Above);
            int leftMode = GetIntraModeContext(neighbors.Left);
            yMode = ReadCdfSymbol(
                symbols,
                cdfContext,
                "Default_Intra_Frame_Y_Mode_Cdf",
                aboveMode * Av1SyntaxConstants.IntraModeContexts + leftMode);
        }
        else
        {
            yMode = ReadCdfSymbol(
                symbols,
                cdfContext,
                "Default_Y_Mode_Cdf",
                Av1ConversionTables.GetSizeGroup(block.BlockSize));
        }

        if (yMode < 0 || yMode > (int)Av1PredictionMode.Paeth)
        {
            return AvifResultCode.InvalidAv1Bitstream;
        }

        syntax.YMode = (Av1PredictionMode)yMode;
        if (PlaneCount == 1)
        {
            syntax.UvMode = Av1PredictionMode.Dc;
            return AvifResultCode.Success;
        }

        int uvMode = ReadCdfSymbol(
            symbols,
            cdfContext,
            "Default_Uv_Mode_Cfl_Not_Allowed_Cdf",
            yMode);
        if (uvMode < 0 || uvMode > (int)Av1PredictionMode.Paeth)
        {
            return AvifResultCode.InvalidAv1Bitstream;
        }

        syntax.UvMode = (Av1PredictionMode)uvMode;
        return AvifResultCode.Success;
    }

    static int ReadDelta(
        Av1SymbolDecoder symbols,
        Av1TileCdfContext cdfContext,
        string tableName,
        int resolution)
    {
        int absolute = ReadCdfSymbol(symbols, cdfContext, tableName, 0);
        if (absolute < 0)
        {
            return 0;
        }

        if (absolute == 3)
        {
            int remainderBits = (int)symbols.ReadLiteral(3) + 1;
            absolute = (int)symbols.ReadLiteral(remainderBits)
                + (1 << remainderBits) + 1;
        }

        if (absolute != 0 && symbols.ReadBoolean() != 0)
        {
            absolute = -absolute;
        }

        return absolute << resolution;
    }

    static int ReadCdfSymbol(
        Av1SymbolDecoder symbols,
        Av1TileCdfContext cdfContext,
        string tableName,
        int row)
    {
        Av1Cdf? cdf = cdfContext.GetCdf(tableName, row);
        if (cdf == null)
        {
            return -1;
        }

        return symbols.ReadCdfSymbol(cdf);
    }

    static int GetIntraModeContext(Av1ModeInfo? mode)
    {
        if (mode == null || mode.IsInter)
        {
            return 0;
        }

        int value = (int)mode.YMode;
        if (value <= (int)Av1PredictionMode.Directional135)
        {
            return value;
        }

        return value <= (int)Av1PredictionMode.Directional67 ? 4 : 0;
    }

    static string GetTransformCdfName(Av1BlockSize blockSize)
    {
        Av1TransformSize size = Av1ConversionTables.GetMaxRectangularTransformSize(blockSize);
        int maximum = Av1ConversionTables.GetTransformWidth(size);
        int height = Av1ConversionTables.GetTransformHeight(size);
        if (height > maximum)
        {
            maximum = height;
        }

        if (maximum <= 8)
        {
            return "Default_Tx_8x8_Cdf";
        }

        if (maximum <= 16)
        {
            return "Default_Tx_16x16_Cdf";
        }

        return maximum <= 32
            ? "Default_Tx_32x32_Cdf"
            : "Default_Tx_64x64_Cdf";
    }

    void BeginSuperblock(int superblockIndex)
    {
        if (CurrentSuperblockIndex == superblockIndex)
        {
            return;
        }

        CurrentSuperblockIndex = superblockIndex;
        ReadDeltas = ((Header ?? throw new InvalidOperationException("Invalid internal state.")).Quantization ?? throw new InvalidOperationException("Invalid internal state.")).DeltaQPresent;
    }

    int ResolveSegmentId(Av1PartitionEvent block, Av1BlockModeSyntax syntax)
    {
        if ((Header ?? throw new InvalidOperationException("Invalid internal state.")).Segmentation == null || !(Header.Segmentation ?? throw new InvalidOperationException("Invalid internal state.")).Enabled)
        {
            return 0;
        }

        if (!Header.Segmentation.UpdateMap)
        {
            return GetPreviousSegmentId(block);
        }

        return syntax.SegmentId;
    }

    void ApplyDeltas(Av1BlockModeSyntax syntax)
    {
        if (syntax.DeltaQuantizerPresent)
        {
            CurrentQIndex = Clip(
                1,
                Av1SyntaxConstants.MaxQuantizerIndex,
                CurrentQIndex + syntax.DeltaQuantizer);
        }

        if (((Header ?? throw new InvalidOperationException("Invalid internal state.")).Quantization ?? throw new InvalidOperationException("Invalid internal state.")).DeltaLoopFilterPresent
            && syntax.DeltaLoopFilterPresent)
        {
            int count = (Header.Quantization ?? throw new InvalidOperationException("Invalid internal state.")).DeltaLoopFilterMulti ? 4 : 1;
            int index = 0;
            while (index < count)
            {
                (DeltaLoopFilters ?? throw new InvalidOperationException("Invalid internal state."))[index] = Clip(
                    -63,
                    63,
                    DeltaLoopFilters[index] + syntax.DeltaLoopFilters[index]);
                index++;
            }
        }
    }

    bool ResolveIsInter(
        int segmentId,
        bool skipMode,
        bool segmentReference,
        bool segmentGlobal,
        Av1BlockModeSyntax syntax)
    {
        if ((Header ?? throw new InvalidOperationException("Invalid internal state.")).FrameIsIntra)
        {
            return false;
        }

        if (skipMode || segmentGlobal)
        {
            return true;
        }

        if (segmentReference)
        {
            return GetFeatureData(segmentId, Av1SegmentFeature.ReferenceFrame)
                != (int)Av1ReferenceFrame.Intra;
        }

        return syntax.IsInter;
    }

    void SetPredictionState(
        Av1ModeInfo mode,
        int segmentId,
        bool skipMode,
        bool segmentReference,
        bool segmentGlobal,
        Av1BlockModeSyntax syntax)
    {
        mode.YMode = syntax.YMode;
        mode.UvMode = syntax.UvMode;
        if (!mode.IsInter)
        {
            mode.ReferenceFrames[0] = Av1ReferenceFrame.Intra;
            mode.ReferenceFrames[1] = Av1ReferenceFrame.None;
            return;
        }

        mode.YMode = segmentGlobal ? Av1PredictionMode.GlobalMv : syntax.YMode;
        if (skipMode)
        {
            mode.ReferenceFrames[0] = (Av1ReferenceFrame)(Header ?? throw new InvalidOperationException("Invalid internal state.")).SkipModeFrames[0];
            mode.ReferenceFrames[1] = (Av1ReferenceFrame)Header.SkipModeFrames[1];
        }
        else if (segmentReference)
        {
            mode.ReferenceFrames[0] = (Av1ReferenceFrame)GetFeatureData(
                segmentId,
                Av1SegmentFeature.ReferenceFrame);
            mode.ReferenceFrames[1] = Av1ReferenceFrame.None;
        }
        else
        {
            mode.ReferenceFrames[0] = syntax.ReferenceFrames[0];
            mode.ReferenceFrames[1] = syntax.ReferenceFrames[1];
        }
    }

    Av1TransformSize ResolveTransformSize(
        Av1BlockSize blockSize,
        bool skip,
        bool lossless,
        int depth)
    {
        if (lossless || (Header ?? throw new InvalidOperationException("Invalid internal state.")).TransformMode == Av1TransformMode.Only4x4)
        {
            return Av1TransformSize.Transform4x4;
        }

        Av1TransformSize size = Av1ConversionTables.GetMaxRectangularTransformSize(blockSize);
        if (size == Av1TransformSize.Invalid)
        {
            return size;
        }

        if (Header.TransformMode == Av1TransformMode.Largest || skip)
        {
            return size;
        }

        int maximumDepth = Av1ConversionTables.GetMaxTransformDepth(blockSize);
        if (depth < 0 || depth > maximumDepth)
        {
            return Av1TransformSize.Invalid;
        }

        int index = 0;
        while (index < depth)
        {
            size = Av1ConversionTables.GetSplitTransformSize(size);
            if (size == Av1TransformSize.Invalid)
            {
                return size;
            }

            index++;
        }

        return size;
    }

    int GetEffectiveQIndex(int segmentId)
    {
        int qIndex = CurrentQIndex;
        if (IsFeatureActive(segmentId, Av1SegmentFeature.AlternateQuantizer))
        {
            qIndex += GetFeatureData(segmentId, Av1SegmentFeature.AlternateQuantizer);
        }

        return Clip(0, Av1SyntaxConstants.MaxQuantizerIndex, qIndex);
    }

    int GetProspectiveEffectiveQIndex(
        int segmentId,
        Av1BlockModeSyntax syntax)
    {
        int qIndex = CurrentQIndex;
        if (ReadDeltas && syntax.DeltaQuantizerPresent)
        {
            qIndex = Clip(
                1,
                Av1SyntaxConstants.MaxQuantizerIndex,
                qIndex + syntax.DeltaQuantizer);
        }

        if (IsFeatureActive(segmentId, Av1SegmentFeature.AlternateQuantizer))
        {
            qIndex += GetFeatureData(segmentId, Av1SegmentFeature.AlternateQuantizer);
        }

        return Clip(0, Av1SyntaxConstants.MaxQuantizerIndex, qIndex);
    }

    int GetSegmentLoopFilterDelta(int segmentId, int filterIndex)
    {
        Av1SegmentFeature feature = (Av1SegmentFeature)(
            (int)Av1SegmentFeature.AlternateLoopFilterYVertical + filterIndex);
        return IsFeatureActive(segmentId, feature) ? GetFeatureData(segmentId, feature) : 0;
    }

    bool IsLossless(int qIndex)
    {
        return qIndex == 0
            && ((Header ?? throw new InvalidOperationException("Invalid internal state.")).Quantization ?? throw new InvalidOperationException("Invalid internal state.")).DeltaYDc == 0
            && (Header.Quantization ?? throw new InvalidOperationException("Invalid internal state.")).DeltaUDc == 0
            && Header.Quantization.DeltaUAc == 0
            && Header.Quantization.DeltaVDc == 0
            && Header.Quantization.DeltaVAc == 0;
    }

    bool IsFeatureActive(int segmentId, Av1SegmentFeature feature)
    {
        if ((Header ?? throw new InvalidOperationException("Invalid internal state.")).Segmentation == null || !(Header.Segmentation ?? throw new InvalidOperationException("Invalid internal state.")).Enabled
            || Header.Segmentation.FeatureEnabled == null
            || segmentId < 0 || segmentId >= Header.Segmentation.FeatureEnabled.Length
            || Header.Segmentation.FeatureEnabled[segmentId] == null
            || (int)feature >= Header.Segmentation.FeatureEnabled[segmentId].Length)
        {
            return false;
        }

        return Header.Segmentation.FeatureEnabled[segmentId][(int)feature];
    }

    int GetFeatureData(int segmentId, Av1SegmentFeature feature)
    {
        if (((Header ?? throw new InvalidOperationException("Invalid internal state.")).Segmentation ?? throw new InvalidOperationException("Invalid internal state.")).FeatureData == null
            || segmentId < 0 || segmentId >= ((Header.Segmentation ?? throw new InvalidOperationException("Invalid internal state.")).FeatureData ?? throw new InvalidOperationException("Invalid internal state.")).Length
            || (Header.Segmentation.FeatureData ?? throw new InvalidOperationException("Invalid internal state."))[segmentId] == null
            || (int)feature >= Header.Segmentation.FeatureData[segmentId].Length)
        {
            return 0;
        }

        return Header.Segmentation.FeatureData[segmentId][(int)feature];
    }

    AvifResultCode ValidateBlockAndSyntax(
        Av1PartitionEvent block,
        Av1BlockModeSyntax syntax)
    {
        if (block == null || syntax == null
            || block.Kind != Av1PartitionEventKind.Block
            || Av1ConversionTables.GetBlockWidth(block.BlockSize) == 0
            || block.VisibleMiWidth <= 0 || block.VisibleMiHeight <= 0
            || syntax.ReferenceFrames == null || syntax.ReferenceFrames.Length != 2
            || syntax.DeltaLoopFilters == null || syntax.DeltaLoopFilters.Length != 4)
        {
            return AvifResultCode.InvalidArgument;
        }

        if (syntax.SegmentId < 0 || syntax.SegmentId >= Av1SyntaxConstants.MaxSegments
            || (syntax.DeltaQuantizerPresent
                && !((Header ?? throw new InvalidOperationException("Invalid internal state.")).Quantization ?? throw new InvalidOperationException("Invalid internal state.")).DeltaQPresent)
            || (syntax.DeltaLoopFilterPresent
                && (!((Header ?? throw new InvalidOperationException("Invalid internal state.")).Quantization ?? throw new InvalidOperationException("Invalid internal state.")).DeltaQPresent
                    || !(Header.Quantization ?? throw new InvalidOperationException("Invalid internal state.")).DeltaLoopFilterPresent)))
        {
            return AvifResultCode.InvalidAv1Bitstream;
        }

        return AvifResultCode.Success;
    }

    static int PredictSegmentId(Av1BlockNeighborContexts contexts)
    {
        if (!contexts.AboveAvailable)
        {
            return contexts.LeftAvailable ? (contexts.Left ?? throw new InvalidOperationException("Invalid internal state.")).SegmentId : 0;
        }

        if (!contexts.LeftAvailable)
        {
            return (contexts.Above ?? throw new InvalidOperationException("Invalid internal state.")).SegmentId;
        }

        return contexts.UpperLeft != null
            && contexts.UpperLeft.SegmentId == (contexts.Above ?? throw new InvalidOperationException("Invalid internal state.")).SegmentId
            ? contexts.Above.SegmentId
            : (contexts.Left ?? throw new InvalidOperationException("Invalid internal state.")).SegmentId;
    }

    static int GetSegmentIdContext(Av1BlockNeighborContexts contexts)
    {
        if (contexts.AboveAvailable && contexts.LeftAvailable)
        {
            return (contexts.Above ?? throw new InvalidOperationException("Invalid internal state.")).SegmentId == (contexts.Left ?? throw new InvalidOperationException("Invalid internal state.")).SegmentId ? 2 : 1;
        }

        return contexts.AboveAvailable || contexts.LeftAvailable ? 1 : 0;
    }

    static int GetIsInterContext(Av1BlockNeighborContexts contexts)
    {
        if (contexts.AboveAvailable && contexts.LeftAvailable)
        {
            if (!(contexts.Above ?? throw new InvalidOperationException("Invalid internal state.")).IsInter && !(contexts.Left ?? throw new InvalidOperationException("Invalid internal state.")).IsInter)
            {
                return 3;
            }

            return contexts.Above.IsInter == (contexts.Left ?? throw new InvalidOperationException("Invalid internal state.")).IsInter ? 0 : 1;
        }

        if (contexts.AboveAvailable)
        {
            return (contexts.Above ?? throw new InvalidOperationException("Invalid internal state.")).IsInter ? 0 : 2;
        }

        if (contexts.LeftAvailable)
        {
            return (contexts.Left ?? throw new InvalidOperationException("Invalid internal state.")).IsInter ? 0 : 2;
        }

        return 0;
    }

    static int GetTransformSizeContext(
        Av1PartitionEvent block,
        Av1BlockNeighborContexts contexts)
    {
        int context = 0;
        Av1TransformSize maximum =
            Av1ConversionTables.GetMaxRectangularTransformSize(block.BlockSize);
        int blockWidth = Av1ConversionTables.GetTransformWidth(maximum);
        int blockHeight = Av1ConversionTables.GetTransformHeight(maximum);
        if (contexts.AboveAvailable
            && Av1ConversionTables.GetTransformWidth((contexts.Above ?? throw new InvalidOperationException("Invalid internal state.")).TransformSize) < blockWidth)
        {
            context++;
        }

        if (contexts.LeftAvailable
            && Av1ConversionTables.GetTransformHeight((contexts.Left ?? throw new InvalidOperationException("Invalid internal state.")).TransformSize) < blockHeight)
        {
            context++;
        }

        return context;
    }

    static int BooleanValue(bool value)
    {
        return value ? 1 : 0;
    }

    static int Clip(int minimum, int maximum, int value)
    {
        if (value < minimum)
        {
            return minimum;
        }

        return value > maximum ? maximum : value;
    }
}
