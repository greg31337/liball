namespace avif;

// AV1 specification sections 3 and 9.3 (Symbols and conversion tables).
internal static class Av1SyntaxConstants
{
    internal const int RefsPerFrame = 7;
    internal const int TotalRefsPerFrame = 8;
    internal const int BlockSizeGroups = 4;
    internal const int BlockSizeCount = 22;
    internal const int TransformSizeCount = 19;
    internal const int TransformTypeCount = 16;
    internal const int MaxSuperblockSize = 128;
    internal const int MinSuperblockSize = 64;
    internal const int MiSize = 4;
    internal const int MiSizeLog2 = 2;
    internal const int MaxTileWidth = 4096;
    internal const int MaxTileRows = 64;
    internal const int MaxTileColumns = 64;
    internal const int MaxTileArea = 4096 * 2304;
    internal const int IntraBlockCopyDelayPixels = 256;
    internal const int IntraBlockCopyDelaySuperblocks64 = 4;
    internal const int StoredReferenceFrames = 8;
    internal const int IsInterContexts = 4;
    internal const int ReferenceContexts = 3;
    internal const int MaxSegments = 8;
    internal const int SegmentIdContexts = 3;
    internal const int SegmentFeatureCount = 8;
    internal const int PlaneTypes = 2;
    internal const int TransformSizeContexts = 3;
    internal const int InterpolationFilters = 3;
    internal const int InterpolationFilterContexts = 16;
    internal const int SkipModeContexts = 3;
    internal const int SkipContexts = 3;
    internal const int PartitionContexts = 4;
    internal const int SquareTransformSizeCount = 5;
    internal const int TransformModeCount = 3;
    internal const int MacroblockModeCount = 17;
    internal const int IntraModeCount = 13;
    internal const int ChromaIntraModeCountWithoutCfl = 13;
    internal const int ChromaIntraModeCountWithCfl = 14;
    internal const int CompoundModeCount = 8;
    internal const int CompoundModeContexts = 8;
    internal const int CompoundNewMotionVectorContexts = 5;
    internal const int NewMotionVectorContexts = 6;
    internal const int ZeroMotionVectorContexts = 2;
    internal const int ReferenceMotionVectorContexts = 6;
    internal const int DrlModeContexts = 3;
    internal const int MotionVectorContexts = 2;
    internal const int IntraBlockCopyMotionVectorContext = 1;
    internal const int MotionVectorJointCount = 4;
    internal const int MotionVectorClassCount = 11;
    internal const int MotionVectorClass0Size = 2;
    internal const int MotionVectorOffsetBits = 10;
    internal const int MaxOperatingPoints = 32;
    internal const int MaxTemporalLayers = 8;
    internal const int MaxSpatialLayers = 4;
    internal const int MaxLoopFilter = 63;
    internal const int ReferenceScaleShift = 14;
    internal const int SubpixelBits = 4;
    internal const int SubpixelMask = 15;
    internal const int ScaleSubpixelBits = 10;
    internal const int MotionVectorBorder = 128;
    internal const int PaletteColorContexts = 5;
    internal const int PaletteColorContextHashCount = 8;
    internal const int PaletteBlockSizeContexts = 7;
    internal const int PaletteYModeContexts = 3;
    internal const int PaletteUvModeContexts = 2;
    internal const int PaletteSizeCount = 7;
    internal const int PaletteColorCount = 8;
    internal const int PaletteNeighborCount = 3;
    internal const int DeltaQuantizerSmall = 3;
    internal const int DeltaLoopFilterSmall = 3;
    internal const int MaxCdefStrengths = 8;
    internal const int MaxAngleDelta = 3;
    internal const int DirectionalModeCount = 8;
    internal const int AngleStep = 3;
    internal const int MaxPaletteSize = 8;
    internal const int MinPaletteSize = 2;
    internal const int MaxQuantizerIndex = 255;
    internal const int IntraTransformSetTypeCount = 3;
    internal const int InterTransformSetTypeCount = 4;
    internal const int WarpedModelPrecisionBits = 16;
    internal const int GlobalMotionAbsoluteTranslationBits = 12;
    internal const int GlobalMotionTranslationOnlyBits = 9;
    internal const int GlobalMotionAbsoluteAlphaBits = 12;
    internal const int DivisionLookupPrecisionBits = 14;
    internal const int DivisionLookupBits = 8;
    internal const int DivisionLookupEntryCount = 257;
    internal const int MotionModeCount = 3;
    internal const int MaxLeastSquaresSamples = 8;
    internal const int MaxLeastSquaresMotionVector = 256;
    internal const int WarpedModelTranslationClamp = 1 << 23;
    internal const int WarpedModelNonDiagonalAffineClamp = 1 << 13;
    internal const int WarpedPixelPrecisionShifts = 1 << 6;
    internal const int WarpedDifferencePrecisionBits = 10;
    internal const int GlobalMotionAlphaPrecisionBits = 15;
    internal const int GlobalMotionTranslationPrecisionBits = 6;
    internal const int GlobalMotionTranslationOnlyPrecisionBits = 3;
    internal const int InterIntraModeCount = 4;
    internal const int MaskMasterSize = 64;
    internal const int SegmentIdPredictedContexts = 3;
    internal const int ForwardReferenceCount = 4;
    internal const int BackwardReferenceCount = 3;
    internal const int SingleReferenceCount = 7;
    internal const int UnidirectionalCompoundReferenceCount = 4;
    internal const int CompoundTypeCount = 2;
    internal const int CflJointSignCount = 8;
    internal const int CflAlphabetSize = 16;
    internal const int CompoundInterContexts = 5;
    internal const int CompoundReferenceTypeContexts = 5;
    internal const int CflAlphaContexts = 6;
    internal const int IntraModeContexts = 5;
    internal const int CompoundGroupIndexContexts = 6;
    internal const int CompoundIndexContexts = 6;
    internal const int IntraEdgeKernelCount = 3;
    internal const int IntraEdgeTapCount = 5;
    internal const int FrameLoopFilterCount = 4;
    internal const int MaxVariableTransformDepth = 2;
    internal const int TransformPartitionContexts = 21;
    internal const int ReferenceCategoryLevel = 640;
    internal const int MaxReferenceMotionVectorStackSize = 8;
    internal const int MotionFieldMotionVectorStackSize = 3;
    internal const int MaxTransformDepth = 2;
    internal const int WedgeTypeCount = 16;
    internal const int FilterBits = 7;
    internal const int WienerCoefficientCount = 3;
    internal const int SgrprojParameterBits = 4;
    internal const int SgrprojProjectionSubexpK = 4;
    internal const int SgrprojProjectionBits = 7;
    internal const int SgrprojRestorationBits = 4;
    internal const int SgrprojMtableBits = 20;
    internal const int SgrprojReciprocalBits = 12;
    internal const int SgrprojInternalBits = 8;
    internal const int CdfProbabilityTop = 32768;
    internal const int CdfCountTop = 32;
    internal const int EntropyCoderProbabilityShift = 6;
    internal const int EntropyCoderMinimumProbability = 4;
    internal const int SelectScreenContentTools = 2;
    internal const int SelectIntegerMotionVector = 2;
    internal const int MaxRestorationTileSize = 256;
    internal const int MaxFrameDistance = 31;
    internal const int MaxProjectedMotionVectorOffsetWidth = 8;
    internal const int MaxProjectedMotionVectorOffsetHeight = 0;
    internal const int WarpParameterReduceBits = 6;
    internal const int QuantizerBaseLevelCount = 2;
    internal const int CoefficientBaseRange = 12;
    internal const int CoefficientBaseRangeCdfSize = 4;
    internal const int EndOfBlockCoefficientContexts = 4;
    internal const int TwoDimensionalCoefficientContextOffset = 26;
    internal const int SignificantCoefficientContexts = 42;
    internal const int SignificantReferenceDifferenceSampleCount = 5;
    internal const int SuperresNumerator = 8;
    internal const int SuperresMinimumDenominator = 9;
    internal const int SuperresDenominatorBits = 3;
    internal const int SuperresFilterBits = 6;
    internal const int SuperresFilterShifts = 1 << 6;
    internal const int SuperresFilterTaps = 8;
    internal const int SuperresFilterOffset = 3;
    internal const int SuperresScaleBits = 14;
    internal const int SuperresScaleMask = (1 << 14) - 1;
    internal const int SuperresExtraBits = 8;
    internal const int TransformBlockSkipContexts = 13;
    internal const int EndOfBlockContexts = 9;
    internal const int DcSignContexts = 3;
    internal const int LevelContexts = 21;
    internal const int ReferenceMotionVectorLimit = (1 << 12) - 1;
    internal const int NumQuantizerMatrixLevels = 16;
    internal const int QuantizerMatrixTotalSize = 3344;
}

// AV1 specification section 9.3, including Block_Width, Block_Height,
// Max_Tx_Size, Max_Tx_Size_Rect, Max_Tx_Depth, Split_Tx_Size, and the
// vertical/horizontal transform component tables.
internal static class Av1ConversionTables
{
    static readonly int[] blockWidths = new int[]
    {
        4, 4, 8, 8, 8, 16, 16, 16, 32, 32, 32,
        64, 64, 64, 128, 128, 4, 16, 8, 32, 16, 64
    };

    static readonly int[] blockHeights = new int[]
    {
        4, 8, 4, 8, 16, 8, 16, 32, 16, 32, 64,
        32, 64, 128, 64, 128, 16, 4, 32, 8, 64, 16
    };

    static readonly int[] sizeGroups = new int[]
    {
        0, 0, 0, 1, 1, 1, 2, 2, 2, 3, 3,
        3, 3, 3, 3, 3, 0, 0, 1, 1, 2, 2
    };

    static readonly Av1TransformSize[] maxTransformSizes = new Av1TransformSize[]
    {
        Av1TransformSize.Transform4x4,
        Av1TransformSize.Transform4x4,
        Av1TransformSize.Transform4x4,
        Av1TransformSize.Transform8x8,
        Av1TransformSize.Transform8x8,
        Av1TransformSize.Transform8x8,
        Av1TransformSize.Transform16x16,
        Av1TransformSize.Transform16x16,
        Av1TransformSize.Transform16x16,
        Av1TransformSize.Transform32x32,
        Av1TransformSize.Transform32x32,
        Av1TransformSize.Transform32x32,
        Av1TransformSize.Transform64x64,
        Av1TransformSize.Transform64x64,
        Av1TransformSize.Transform64x64,
        Av1TransformSize.Transform64x64,
        Av1TransformSize.Transform4x4,
        Av1TransformSize.Transform4x4,
        Av1TransformSize.Transform8x8,
        Av1TransformSize.Transform8x8,
        Av1TransformSize.Transform16x16,
        Av1TransformSize.Transform16x16
    };

    static readonly Av1TransformSize[] maxRectangularTransformSizes = new Av1TransformSize[]
    {
        Av1TransformSize.Transform4x4,
        Av1TransformSize.Transform4x8,
        Av1TransformSize.Transform8x4,
        Av1TransformSize.Transform8x8,
        Av1TransformSize.Transform8x16,
        Av1TransformSize.Transform16x8,
        Av1TransformSize.Transform16x16,
        Av1TransformSize.Transform16x32,
        Av1TransformSize.Transform32x16,
        Av1TransformSize.Transform32x32,
        Av1TransformSize.Transform32x64,
        Av1TransformSize.Transform64x32,
        Av1TransformSize.Transform64x64,
        Av1TransformSize.Transform64x64,
        Av1TransformSize.Transform64x64,
        Av1TransformSize.Transform64x64,
        Av1TransformSize.Transform4x16,
        Av1TransformSize.Transform16x4,
        Av1TransformSize.Transform8x32,
        Av1TransformSize.Transform32x8,
        Av1TransformSize.Transform16x64,
        Av1TransformSize.Transform64x16
    };

    static readonly int[] maxTransformDepths = new int[]
    {
        0, 1, 1, 1, 2, 2, 2, 3, 3, 3, 4,
        4, 4, 4, 4, 4, 2, 2, 3, 3, 4, 4
    };

    static readonly int[] transformWidths = new int[]
    {
        4, 8, 16, 32, 64, 4, 8, 8, 16, 16,
        32, 32, 64, 4, 16, 8, 32, 16, 64
    };

    static readonly int[] transformHeights = new int[]
    {
        4, 8, 16, 32, 64, 8, 4, 16, 8, 32,
        16, 64, 32, 16, 4, 32, 8, 64, 16
    };

    static readonly Av1TransformSize[] splitTransformSizes = new Av1TransformSize[]
    {
        Av1TransformSize.Transform4x4,
        Av1TransformSize.Transform4x4,
        Av1TransformSize.Transform8x8,
        Av1TransformSize.Transform16x16,
        Av1TransformSize.Transform32x32,
        Av1TransformSize.Transform4x4,
        Av1TransformSize.Transform4x4,
        Av1TransformSize.Transform8x8,
        Av1TransformSize.Transform8x8,
        Av1TransformSize.Transform16x16,
        Av1TransformSize.Transform16x16,
        Av1TransformSize.Transform32x32,
        Av1TransformSize.Transform32x32,
        Av1TransformSize.Transform4x8,
        Av1TransformSize.Transform8x4,
        Av1TransformSize.Transform8x16,
        Av1TransformSize.Transform16x8,
        Av1TransformSize.Transform16x32,
        Av1TransformSize.Transform32x16
    };

    static readonly Av1Transform1DType[] verticalTransformTypes = new Av1Transform1DType[]
    {
        Av1Transform1DType.Dct, Av1Transform1DType.Adst,
        Av1Transform1DType.Dct, Av1Transform1DType.Adst,
        Av1Transform1DType.FlipAdst, Av1Transform1DType.Dct,
        Av1Transform1DType.FlipAdst, Av1Transform1DType.Adst,
        Av1Transform1DType.FlipAdst, Av1Transform1DType.Identity,
        Av1Transform1DType.Dct, Av1Transform1DType.Identity,
        Av1Transform1DType.Adst, Av1Transform1DType.Identity,
        Av1Transform1DType.FlipAdst, Av1Transform1DType.Identity
    };

    static readonly Av1Transform1DType[] horizontalTransformTypes = new Av1Transform1DType[]
    {
        Av1Transform1DType.Dct, Av1Transform1DType.Dct,
        Av1Transform1DType.Adst, Av1Transform1DType.Adst,
        Av1Transform1DType.Dct, Av1Transform1DType.FlipAdst,
        Av1Transform1DType.FlipAdst, Av1Transform1DType.FlipAdst,
        Av1Transform1DType.Adst, Av1Transform1DType.Identity,
        Av1Transform1DType.Identity, Av1Transform1DType.Dct,
        Av1Transform1DType.Identity, Av1Transform1DType.Adst,
        Av1Transform1DType.Identity, Av1Transform1DType.FlipAdst
    };

    static readonly Av1BlockSize[] partitionSubsize = new Av1BlockSize[]
    {
        Av1BlockSize.Block4x4, Av1BlockSize.Block8x8, Av1BlockSize.Block16x16,
        Av1BlockSize.Block32x32, Av1BlockSize.Block64x64, Av1BlockSize.Block128x128,
        Av1BlockSize.Invalid, Av1BlockSize.Block8x4, Av1BlockSize.Block16x8,
        Av1BlockSize.Block32x16, Av1BlockSize.Block64x32, Av1BlockSize.Block128x64,
        Av1BlockSize.Invalid, Av1BlockSize.Block4x8, Av1BlockSize.Block8x16,
        Av1BlockSize.Block16x32, Av1BlockSize.Block32x64, Av1BlockSize.Block64x128,
        Av1BlockSize.Invalid, Av1BlockSize.Block4x4, Av1BlockSize.Block8x8,
        Av1BlockSize.Block16x16, Av1BlockSize.Block32x32, Av1BlockSize.Block64x64,
        Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block16x8,
        Av1BlockSize.Block32x16, Av1BlockSize.Block64x32, Av1BlockSize.Block128x64,
        Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block16x8,
        Av1BlockSize.Block32x16, Av1BlockSize.Block64x32, Av1BlockSize.Block128x64,
        Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block8x16,
        Av1BlockSize.Block16x32, Av1BlockSize.Block32x64, Av1BlockSize.Block64x128,
        Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block8x16,
        Av1BlockSize.Block16x32, Av1BlockSize.Block32x64, Av1BlockSize.Block64x128,
        Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block16x4,
        Av1BlockSize.Block32x8, Av1BlockSize.Block64x16, Av1BlockSize.Invalid,
        Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block4x16,
        Av1BlockSize.Block8x32, Av1BlockSize.Block16x64, Av1BlockSize.Invalid
    };

    internal static int GetBlockWidth(Av1BlockSize size)
    {
        int index = (int)size;
        return index >= 0 && index < blockWidths.Length ? blockWidths[index] : 0;
    }

    internal static int GetBlockHeight(Av1BlockSize size)
    {
        int index = (int)size;
        return index >= 0 && index < blockHeights.Length ? blockHeights[index] : 0;
    }

    internal static int GetBlockMiWidth(Av1BlockSize size)
    {
        return GetBlockWidth(size) >> Av1SyntaxConstants.MiSizeLog2;
    }

    internal static int GetBlockMiHeight(Av1BlockSize size)
    {
        return GetBlockHeight(size) >> Av1SyntaxConstants.MiSizeLog2;
    }

    internal static int GetSizeGroup(Av1BlockSize size)
    {
        int index = (int)size;
        return index >= 0 && index < sizeGroups.Length ? sizeGroups[index] : -1;
    }

    internal static Av1BlockSize FindBlockSize(int width, int height)
    {
        int index = 0;
        while (index < blockWidths.Length)
        {
            if (blockWidths[index] == width && blockHeights[index] == height)
            {
                return (Av1BlockSize)index;
            }

            index++;
        }

        return Av1BlockSize.Invalid;
    }

    internal static Av1TransformSize GetMaxTransformSize(Av1BlockSize size)
    {
        int index = (int)size;
        return index >= 0 && index < maxTransformSizes.Length
            ? maxTransformSizes[index]
            : Av1TransformSize.Invalid;
    }

    internal static Av1TransformSize GetMaxRectangularTransformSize(Av1BlockSize size)
    {
        int index = (int)size;
        return index >= 0 && index < maxRectangularTransformSizes.Length
            ? maxRectangularTransformSizes[index]
            : Av1TransformSize.Invalid;
    }

    internal static int GetMaxTransformDepth(Av1BlockSize size)
    {
        int index = (int)size;
        return index >= 0 && index < maxTransformDepths.Length
            ? maxTransformDepths[index]
            : -1;
    }

    internal static int GetTransformWidth(Av1TransformSize size)
    {
        int index = (int)size;
        return index >= 0 && index < transformWidths.Length ? transformWidths[index] : 0;
    }

    internal static int GetTransformHeight(Av1TransformSize size)
    {
        int index = (int)size;
        return index >= 0 && index < transformHeights.Length ? transformHeights[index] : 0;
    }

    internal static int GetTransformCoefficientCount(Av1TransformSize size)
    {
        Av1TransformSize adjusted = GetAdjustedTransformSize(size);
        return GetTransformWidth(adjusted) * GetTransformHeight(adjusted);
    }

    internal static Av1TransformSize GetSplitTransformSize(Av1TransformSize size)
    {
        int index = (int)size;
        return index >= 0 && index < splitTransformSizes.Length
            ? splitTransformSizes[index]
            : Av1TransformSize.Invalid;
    }

    internal static Av1TransformSize GetAdjustedTransformSize(Av1TransformSize size)
    {
        if (size == Av1TransformSize.Transform64x64
            || size == Av1TransformSize.Transform64x32
            || size == Av1TransformSize.Transform32x64)
        {
            return Av1TransformSize.Transform32x32;
        }

        if (size == Av1TransformSize.Transform64x16)
        {
            return Av1TransformSize.Transform32x16;
        }

        if (size == Av1TransformSize.Transform16x64)
        {
            return Av1TransformSize.Transform16x32;
        }

        return size;
    }

    internal static Av1Transform1DType GetVerticalTransformType(Av1TransformType type)
    {
        int index = (int)type;
        return index >= 0 && index < verticalTransformTypes.Length
            ? verticalTransformTypes[index]
            : Av1Transform1DType.Identity;
    }

    internal static Av1Transform1DType GetHorizontalTransformType(Av1TransformType type)
    {
        int index = (int)type;
        return index >= 0 && index < horizontalTransformTypes.Length
            ? horizontalTransformTypes[index]
            : Av1Transform1DType.Identity;
    }

    internal static Av1TransformClass GetTransformClass(Av1TransformType type)
    {
        if (type == Av1TransformType.VerticalDct
            || type == Av1TransformType.VerticalAdst
            || type == Av1TransformType.VerticalFlipAdst)
        {
            return Av1TransformClass.Vertical;
        }

        if (type == Av1TransformType.HorizontalDct
            || type == Av1TransformType.HorizontalAdst
            || type == Av1TransformType.HorizontalFlipAdst)
        {
            return Av1TransformClass.Horizontal;
        }

        return Av1TransformClass.TwoDimensional;
    }

    internal static Av1BlockSize GetPartitionSubsize(
        Av1PartitionType partition,
        Av1BlockSize size)
    {
        int squareIndex = GetSquareBlockIndex(size);
        int partitionIndex = (int)partition;
        if (squareIndex < 0 || partitionIndex < 0 || partitionIndex >= 10)
        {
            return Av1BlockSize.Invalid;
        }

        return partitionSubsize[partitionIndex * 6 + squareIndex];
    }

    internal static int GetSquareBlockIndex(Av1BlockSize size)
    {
        if (size == Av1BlockSize.Block4x4)
        {
            return 0;
        }

        if (size == Av1BlockSize.Block8x8)
        {
            return 1;
        }

        if (size == Av1BlockSize.Block16x16)
        {
            return 2;
        }

        if (size == Av1BlockSize.Block32x32)
        {
            return 3;
        }

        if (size == Av1BlockSize.Block64x64)
        {
            return 4;
        }

        if (size == Av1BlockSize.Block128x128)
        {
            return 5;
        }

        return -1;
    }
}

// AV1 specification sections 9.4.1 and 9.4.2. The normative explicit scan
// tables are generated from their specified diagonal/row/column definitions.
internal static class Av1ScanTables
{
    internal static int[] GetScan(Av1TransformSize size, Av1TransformType type)
    {
        Av1ScanKind kind = GetScanKind(type);
        return GetScan(size, kind);
    }

    internal static int[] GetScan(Av1TransformSize size, Av1ScanKind kind)
    {
        Av1TransformSize adjusted = Av1ConversionTables.GetAdjustedTransformSize(size);
        int scanWidth = Av1ConversionTables.GetTransformHeight(adjusted);
        int scanHeight = Av1ConversionTables.GetTransformWidth(adjusted);
        if (scanWidth == 0 || scanHeight == 0)
        {
            return System.Array.Empty<int>();
        }

        if (kind == Av1ScanKind.Column)
        {
            return CreateColumnScan(scanWidth, scanHeight);
        }

        if (kind == Av1ScanKind.Row)
        {
            return CreateRowScan(scanWidth, scanHeight);
        }

        return CreateDefaultScan(scanWidth, scanHeight);
    }

    internal static int[] GetInverseScan(Av1TransformSize size, Av1TransformType type)
    {
        int[] scan = GetScan(size, type);
        int[] inverse = new int[scan.Length];
        int index = 0;
        while (index < scan.Length)
        {
            inverse[scan[index]] = index;
            index++;
        }

        return inverse;
    }

    internal static Av1ScanKind GetScanKind(Av1TransformType type)
    {
        Av1TransformClass transformClass = Av1ConversionTables.GetTransformClass(type);
        if (transformClass == Av1TransformClass.Vertical)
        {
            return Av1ScanKind.Row;
        }

        if (transformClass == Av1TransformClass.Horizontal)
        {
            return Av1ScanKind.Column;
        }

        return Av1ScanKind.Default;
    }

    internal static int[] CreateColumnScan(int width, int height)
    {
        int[] scan = new int[width * height];
        int index = 0;
        while (index < scan.Length)
        {
            scan[index] = index;
            index++;
        }

        return scan;
    }

    internal static int[] CreateRowScan(int width, int height)
    {
        int[] scan = new int[width * height];
        int index = 0;
        int x = 0;
        while (x < width)
        {
            int y = 0;
            while (y < height)
            {
                scan[index] = y * width + x;
                index++;
                y++;
            }

            x++;
        }

        return scan;
    }

    internal static int[] CreateDefaultScan(int width, int height)
    {
        int[] scan = new int[width * height];
        int index = 0;
        int diagonal = 0;
        while (diagonal < width + height - 1)
        {
            int minimumX = diagonal - (height - 1);
            if (minimumX < 0)
            {
                minimumX = 0;
            }

            int maximumX = diagonal;
            if (maximumX >= width)
            {
                maximumX = width - 1;
            }

            bool descending = width < height
                || (width == height && (diagonal & 1) == 0);
            if (descending)
            {
                int x = maximumX;
                while (x >= minimumX)
                {
                    int y = diagonal - x;
                    scan[index] = y * width + x;
                    index++;
                    x--;
                }
            }
            else
            {
                int x = minimumX;
                while (x <= maximumX)
                {
                    int y = diagonal - x;
                    scan[index] = y * width + x;
                    index++;
                    x++;
                }
            }

            diagonal++;
        }

        return scan;
    }
}
