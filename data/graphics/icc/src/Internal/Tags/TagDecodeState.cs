namespace icc;

internal class TagDecodeSource
{
    internal System.ReadOnlyMemory<byte> ProfileBytes;

    internal TagDecodeSource(System.ReadOnlyMemory<byte> profileBytes)
    {
        ProfileBytes = profileBytes;
    }
}

internal class TagDecodeState
{
    internal TagDecodeSource Source;
    internal int PayloadOffset;
    internal int PayloadSize;
    internal long AbsolutePayloadOffset;
    internal IccVersionKind ProfileVersion;
    internal bool PreserveOriginalBytes;
    internal int MaximumDecompressedBytes;
    internal int MaximumProfileBytes;
    internal int MaximumTagCount;
    internal int MaximumTagBytes;
    internal int MaximumTotalTagBytes;
    internal int MaximumDiagnosticCount;
    internal int MaximumNestingDepth;
    internal int CurrentNestingDepth;
    internal int MaximumCalculatorInstructions;
    internal int MaximumCalculatorStackDepth;
    internal int MaximumCalculatorStorageValues;
    internal bool HasProfileContext;
    internal uint TagSignature;
    internal IccProfileClass ProfileClass;
    internal uint ProfileClassSignature;
    internal IccColorSpaceKind DataColorSpace;
    internal uint DataColorSpaceSignature;
    internal IccPcsKind ProfileConnectionSpace;
    internal uint ProfileConnectionSpaceSignature;
    internal uint SpectralPcsSignature;
    internal IccColorSpaceKind MaterialConnectionSpace;
    internal uint MaterialConnectionSpaceSignature;
    internal bool HasMatrixPrimaries;
    internal bool IsEmbedded;
    internal bool RequiresEmbeddedData;
    internal object SyncRoot;
    internal bool IsAttempted;
    internal IccTagData Data;
    internal IccOperationResult Result;

    internal TagDecodeState(TagDecodeSource source, int payloadOffset, int payloadSize, long absolutePayloadOffset, IccVersionKind profileVersion, bool preserveOriginalBytes)
    {
        Source = source;
        PayloadOffset = payloadOffset;
        PayloadSize = payloadSize;
        AbsolutePayloadOffset = absolutePayloadOffset;
        ProfileVersion = profileVersion;
        PreserveOriginalBytes = preserveOriginalBytes;
        MaximumDecompressedBytes = 67108864;
        MaximumProfileBytes = 67108864;
        MaximumTagCount = 4096;
        MaximumTagBytes = 33554432;
        MaximumTotalTagBytes = 67108864;
        MaximumDiagnosticCount = 1024;
        MaximumNestingDepth = 32;
        CurrentNestingDepth = 0;
        MaximumCalculatorInstructions = 100000;
        MaximumCalculatorStackDepth = 1024;
        MaximumCalculatorStorageValues = 65536;
        HasProfileContext = false;
        TagSignature = 0;
        ProfileClass = IccProfileClass.Unknown;
        ProfileClassSignature = 0;
        DataColorSpace = IccColorSpaceKind.Unknown;
        DataColorSpaceSignature = 0;
        ProfileConnectionSpace = IccPcsKind.None;
        ProfileConnectionSpaceSignature = 0;
        SpectralPcsSignature = 0;
        MaterialConnectionSpace = IccColorSpaceKind.Unknown;
        MaterialConnectionSpaceSignature = 0;
        HasMatrixPrimaries = false;
        IsEmbedded = false;
        RequiresEmbeddedData = false;
        SyncRoot = new object();
        IsAttempted = false;
        Data = new IccTagData();
        Result = new IccOperationResult();
    }
}
