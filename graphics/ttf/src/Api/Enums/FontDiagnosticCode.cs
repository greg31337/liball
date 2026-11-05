namespace ttf;

/// <summary>Provides stable, machine-readable identifiers for font diagnostics.</summary>
/// <remarks>
/// Values from 1000 through 1999 describe input regions and limits, values from
/// 2000 through 2999 describe containers and table directories, values from
/// 3000 through 3999 describe semantic tables, values from 4000 through 4999
/// describe repairs and preservation, and values from 5000 through 5999
/// describe embedding and output decisions. Existing values are never reused.
/// </remarks>
public enum FontDiagnosticCode
{
    /// <summary>No diagnostic condition.</summary>
    None = 0,

    /// <summary>The input signature does not identify a known container.</summary>
    UnknownContainer = 1000,

    /// <summary>The input identifies a recognized but unsupported container.</summary>
    UnsupportedContainer = 1001,

    /// <summary>A declared data region ends before its required fields.</summary>
    TruncatedData = 1002,

    /// <summary>An offset does not identify a valid location in its bounded region.</summary>
    InvalidOffset = 1003,

    /// <summary>A declared length is invalid for its bounded region.</summary>
    InvalidLength = 1004,

    /// <summary>Checked offset, count, size, or coordinate arithmetic overflowed.</summary>
    ArithmeticOverflow = 1005,

    /// <summary>A configured resource limit was exceeded.</summary>
    LimitExceeded = 1006,

    /// <summary>A structure uses an unsupported major version.</summary>
    UnsupportedVersion = 1007,

    /// <summary>A reserved field contains a prohibited value.</summary>
    InvalidReservedValue = 1008,

    /// <summary>The SFNT offset table is malformed.</summary>
    InvalidSfntHeader = 2000,

    /// <summary>Stored SFNT binary-search fields are inconsistent.</summary>
    InvalidSearchFields = 2001,

    /// <summary>The table directory contains more records than permitted.</summary>
    TooManyTables = 2002,

    /// <summary>Table directory records are not in required tag order.</summary>
    UnsortedTableDirectory = 2003,

    /// <summary>More than one directory record uses the same table tag.</summary>
    DuplicateTable = 2004,

    /// <summary>A table offset does not meet its required alignment.</summary>
    MisalignedTable = 2005,

    /// <summary>Two declared table regions overlap illegally.</summary>
    OverlappingTables = 2006,

    /// <summary>A table region extends outside its container.</summary>
    TableOutOfBounds = 2007,

    /// <summary>Alignment padding contains a nonzero byte.</summary>
    NonZeroPadding = 2008,

    /// <summary>A table checksum does not match its directory record.</summary>
    TableChecksumMismatch = 2009,

    /// <summary>The whole-font checksum adjustment is invalid.</summary>
    FontChecksumMismatch = 2010,

    /// <summary>A table required by the detected outline kind is absent.</summary>
    MissingRequiredTable = 2011,

    /// <summary>The present table combination is inconsistent with the outline kind.</summary>
    InvalidTableCombination = 2012,

    /// <summary>A font collection header or face directory is malformed.</summary>
    InvalidCollection = 2013,

    /// <summary>A WOFF 1 container or directory is malformed.</summary>
    InvalidWoff1 = 2014,

    /// <summary>A WOFF 2 container or directory is malformed.</summary>
    InvalidWoff2 = 2015,

    /// <summary>Compressed font data could not be decoded.</summary>
    DecompressionFailed = 2016,

    /// <summary>Decompressed data exceeded a configured size or ratio limit.</summary>
    DecompressionLimitExceeded = 2017,

    /// <summary>A WOFF 2 variable-length integer has an invalid encoding.</summary>
    InvalidWoff2Integer = 2018,

    /// <summary>A WOFF 2 table uses an unsupported transform.</summary>
    UnsupportedWoff2Transform = 2019,

    /// <summary>A table is recognized but its semantics are unsupported.</summary>
    UnsupportedTable = 3000,

    /// <summary>A known table has an unsupported version.</summary>
    UnsupportedTableVersion = 3001,

    /// <summary>A known table is structurally malformed.</summary>
    MalformedTable = 3002,

    /// <summary>A glyph identifier is outside the font's glyph range.</summary>
    InvalidGlyphId = 3003,

    /// <summary>A requested character or sequence has no usable glyph.</summary>
    MissingGlyph = 3004,

    /// <summary>A character mapping is malformed or semantically invalid.</summary>
    InvalidCharacterMapping = 3005,

    /// <summary>A name string cannot be decoded without replacement.</summary>
    InvalidNameEncoding = 3006,

    /// <summary>Horizontal, vertical, or global metric data is invalid.</summary>
    InvalidMetrics = 3007,

    /// <summary>Glyph outline data is malformed.</summary>
    InvalidOutline = 3008,

    /// <summary>A composite glyph contains a component cycle.</summary>
    CompositeCycle = 3009,

    /// <summary>A composite glyph exceeds the configured nesting depth.</summary>
    CompositeDepthExceeded = 3010,

    /// <summary>OpenType layout data is malformed.</summary>
    InvalidLayout = 3011,

    /// <summary>Layout execution exceeded a configured operation or nesting limit.</summary>
    LayoutLimitExceeded = 3012,

    /// <summary>Variation data is malformed or inconsistent.</summary>
    InvalidVariation = 3013,

    /// <summary>Color or bitmap glyph data is malformed.</summary>
    InvalidColorGlyph = 3014,

    /// <summary>The font requests an unsupported shaping technology.</summary>
    UnsupportedShapingTechnology = 3015,

    /// <summary>A glyph outline uses an obsolete or deprecated operator.</summary>
    DeprecatedOutlineOperator = 3016,

    /// <summary>A dynamic CFF subroutine call required conservative retention.</summary>
    DynamicSubroutineFallback = 3017,

    /// <summary>A COLR paint graph contains a paint or referenced-glyph cycle.</summary>
    ColorPaintCycle = 3018,

    /// <summary>A COLR paint graph exceeds the configured nesting depth.</summary>
    ColorPaintDepthExceeded = 3019,

    /// <summary>A normalized COLR graph exceeds the configured node limit.</summary>
    ColorPaintNodeLimitExceeded = 3020,

    /// <summary>A compatible-mode repair was applied.</summary>
    RepairApplied = 4000,

    /// <summary>An unknown or unsupported table was preserved as bounded raw data.</summary>
    TablePreservedAsRaw = 4001,

    /// <summary>A table was omitted from rewritten output by the selected policy.</summary>
    TableDropped = 4002,

    /// <summary>An invalid encoded string was decoded with replacement characters.</summary>
    StringDecodingReplacement = 4003,

    /// <summary>Stored glyph or font bounds differ from calculated outline bounds.</summary>
    StoredBoundsMismatch = 4004,

    /// <summary>A preserved digital signature was not cryptographically verified.</summary>
    SignatureNotVerified = 4005,

    /// <summary>The font licensing flags restrict embedding.</summary>
    EmbeddingRestricted = 5000,

    /// <summary>The font licensing flags prohibit subsetting.</summary>
    SubsettingRestricted = 5001,

    /// <summary>The font licensing flags permit only embedded bitmap strikes.</summary>
    BitmapEmbeddingOnly = 5002,

    /// <summary>A glyph-to-Unicode mapping cannot be represented unambiguously.</summary>
    AmbiguousUnicodeMapping = 5003,

    /// <summary>The requested PDF feature requires a newer PDF compatibility level.</summary>
    PdfVersionRequired = 5004
}
