namespace tiff;

/// <summary>Specifies the supported error code values used by TIFF data.</summary>
public enum TiffErrorCode
{
    /// <summary>Specifies none.</summary>
    None = 0,
    /// <summary>Specifies unknown error.</summary>
    UnknownError = 1,
    /// <summary>Specifies unsupported stream.</summary>
    UnsupportedStream = 2,
    /// <summary>Specifies non seekable stream.</summary>
    NonSeekableStream = 3,
    /// <summary>Specifies invalid byte order.</summary>
    InvalidByteOrder = 4,
    /// <summary>Specifies invalid TIFF version.</summary>
    InvalidTiffVersion = 5,
    /// <summary>Specifies invalid big TIFF header.</summary>
    InvalidBigTiffHeader = 6,
    /// <summary>Specifies invalid offset.</summary>
    InvalidOffset = 7,
    /// <summary>Specifies invalid byte range.</summary>
    InvalidByteRange = 8,
    /// <summary>Specifies integer overflow.</summary>
    IntegerOverflow = 9,
    /// <summary>Specifies allocation limit exceeded.</summary>
    AllocationLimitExceeded = 10,
    /// <summary>Specifies image dimension limit exceeded.</summary>
    ImageDimensionLimitExceeded = 11,
    /// <summary>Specifies IFD count limit exceeded.</summary>
    IfdCountLimitExceeded = 12,
    /// <summary>Specifies tag count limit exceeded.</summary>
    TagCountLimitExceeded = 13,
    /// <summary>Specifies cyclic IFD chain.</summary>
    CyclicIfdChain = 14,
    /// <summary>Specifies overlapping data block.</summary>
    OverlappingDataBlock = 15,
    /// <summary>Specifies duplicate tag.</summary>
    DuplicateTag = 16,
    /// <summary>Specifies missing required tag.</summary>
    MissingRequiredTag = 17,
    /// <summary>Specifies invalid tag type.</summary>
    InvalidTagType = 18,
    /// <summary>Specifies invalid tag count.</summary>
    InvalidTagCount = 19,
    /// <summary>Specifies invalid tag value.</summary>
    InvalidTagValue = 20,
    /// <summary>Specifies invalid ASCII value.</summary>
    InvalidAsciiValue = 21,
    /// <summary>Specifies invalid rational value.</summary>
    InvalidRationalValue = 22,
    /// <summary>Specifies unsupported compression.</summary>
    UnsupportedCompression = 23,
    /// <summary>Specifies unsupported photometric.</summary>
    UnsupportedPhotometric = 24,
    /// <summary>Specifies codec expansion limit exceeded.</summary>
    CodecExpansionLimitExceeded = 25,
    /// <summary>Specifies truncated stream.</summary>
    TruncatedStream = 26,
    /// <summary>Specifies that the photometric interpretation conflicts with the sample layout.</summary>
    PhotometricMismatch = 27
}
