namespace avif;

// Normative AV1 syntax values from sections 3, 5, 6, and 9.3 of the AV1
// Bitstream and Decoding Process Specification.
internal enum Av1ObuType
{
    Reserved = 0,
    SequenceHeader = 1,
    TemporalDelimiter = 2,
    FrameHeader = 3,
    TileGroup = 4,
    Metadata = 5,
    Frame = 6,
    RedundantFrameHeader = 7,
    TileList = 8,
    Padding = 15
}

internal enum Av1FrameType
{
    Key = 0,
    Inter = 1,
    IntraOnly = 2,
    Switch = 3
}

internal enum Av1ReferenceFrame
{
    None = -1,
    Intra = 0,
    Last = 1,
    Last2 = 2,
    Last3 = 3,
    Golden = 4,
    Backward = 5,
    Alternate2 = 6,
    Alternate = 7
}

internal enum Av1BlockSize
{
    Block4x4 = 0,
    Block4x8 = 1,
    Block8x4 = 2,
    Block8x8 = 3,
    Block8x16 = 4,
    Block16x8 = 5,
    Block16x16 = 6,
    Block16x32 = 7,
    Block32x16 = 8,
    Block32x32 = 9,
    Block32x64 = 10,
    Block64x32 = 11,
    Block64x64 = 12,
    Block64x128 = 13,
    Block128x64 = 14,
    Block128x128 = 15,
    Block4x16 = 16,
    Block16x4 = 17,
    Block8x32 = 18,
    Block32x8 = 19,
    Block16x64 = 20,
    Block64x16 = 21,
    Invalid = 22
}

internal enum Av1PartitionType
{
    None = 0,
    Horizontal = 1,
    Vertical = 2,
    Split = 3,
    HorizontalA = 4,
    HorizontalB = 5,
    VerticalA = 6,
    VerticalB = 7,
    Horizontal4 = 8,
    Vertical4 = 9,
    Invalid = 255
}

internal enum Av1TransformSize
{
    Transform4x4 = 0,
    Transform8x8 = 1,
    Transform16x16 = 2,
    Transform32x32 = 3,
    Transform64x64 = 4,
    Transform4x8 = 5,
    Transform8x4 = 6,
    Transform8x16 = 7,
    Transform16x8 = 8,
    Transform16x32 = 9,
    Transform32x16 = 10,
    Transform32x64 = 11,
    Transform64x32 = 12,
    Transform4x16 = 13,
    Transform16x4 = 14,
    Transform8x32 = 15,
    Transform32x8 = 16,
    Transform16x64 = 17,
    Transform64x16 = 18,
    Invalid = 255
}

internal enum Av1TransformType
{
    DctDct = 0,
    AdstDct = 1,
    DctAdst = 2,
    AdstAdst = 3,
    FlipAdstDct = 4,
    DctFlipAdst = 5,
    FlipAdstFlipAdst = 6,
    AdstFlipAdst = 7,
    FlipAdstAdst = 8,
    Identity = 9,
    VerticalDct = 10,
    HorizontalDct = 11,
    VerticalAdst = 12,
    HorizontalAdst = 13,
    VerticalFlipAdst = 14,
    HorizontalFlipAdst = 15
}

internal enum Av1Transform1DType
{
    Dct = 0,
    Adst = 1,
    FlipAdst = 2,
    Identity = 3
}

internal enum Av1TransformClass
{
    TwoDimensional = 0,
    Horizontal = 1,
    Vertical = 2
}

internal enum Av1TransformMode
{
    Only4x4 = 0,
    Largest = 1,
    Select = 2
}

internal enum Av1PredictionMode
{
    Dc = 0,
    Vertical = 1,
    Horizontal = 2,
    Directional45 = 3,
    Directional135 = 4,
    Directional113 = 5,
    Directional157 = 6,
    Directional203 = 7,
    Directional67 = 8,
    Smooth = 9,
    SmoothVertical = 10,
    SmoothHorizontal = 11,
    Paeth = 12,
    NearestMv = 13,
    NearMv = 14,
    GlobalMv = 15,
    NewMv = 16,
    NearestNearestMv = 17,
    NearNearMv = 18,
    NearestNewMv = 19,
    NewNearestMv = 20,
    NearNewMv = 21,
    NewNearMv = 22,
    GlobalGlobalMv = 23,
    NewNewMv = 24
}

internal enum Av1InterpolationFilter
{
    EightTap = 0,
    EightTapSmooth = 1,
    EightTapSharp = 2,
    Bilinear = 3,
    Switchable = 4
}

internal enum Av1MotionMode
{
    SimpleTranslation = 0,
    Obmc = 1,
    LocalWarp = 2
}

internal enum Av1RestorationType
{
    None = 0,
    Wiener = 1,
    Sgrproj = 2,
    Switchable = 3
}

internal enum Av1WarpModelType
{
    Identity = 0,
    Translation = 1,
    RotZoom = 2,
    Affine = 3
}

internal enum Av1SegmentFeature
{
    AlternateQuantizer = 0,
    AlternateLoopFilterYVertical = 1,
    AlternateLoopFilterYHorizontal = 2,
    AlternateLoopFilterU = 3,
    AlternateLoopFilterV = 4,
    ReferenceFrame = 5,
    Skip = 6,
    GlobalMotion = 7
}

internal enum Av1MotionVectorJoint
{
    Zero = 0,
    HorizontalNonzero = 1,
    VerticalNonzero = 2,
    HorizontalAndVerticalNonzero = 3
}

internal enum Av1MotionVectorClass
{
    Class0 = 0,
    Class1 = 1,
    Class2 = 2,
    Class3 = 3,
    Class4 = 4,
    Class5 = 5,
    Class6 = 6,
    Class7 = 7,
    Class8 = 8,
    Class9 = 9,
    Class10 = 10
}

internal enum Av1MetadataType
{
    Reserved = 0,
    HdrContentLightLevel = 1,
    HdrMasteringDisplayColorVolume = 2,
    Scalability = 3,
    ItuTT35 = 4,
    Timecode = 5
}

internal enum Av1ScanKind
{
    Default = 0,
    Row = 1,
    Column = 2
}
