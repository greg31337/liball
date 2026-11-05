namespace gltf;

/// <summary>Provides glTF version, container, alignment, sentinel, and default limit constants.</summary>
public static class GltfConstants
{
    /// <summary>The glTF core major and minor version implemented by the toolkit.</summary>
    public const string CoreVersion = "2.0";

    /// <summary>The glTF specification patch version used by the toolkit.</summary>
    public const string SpecificationVersion = "2.0.1";

    /// <summary>The magic value at the start of a GLB container.</summary>
    public const uint GlbMagic = 0x46546C67U;

    /// <summary>The supported GLB container version.</summary>
    public const int GlbVersion = 2;

    /// <summary>The GLB JSON chunk type identifier.</summary>
    public const uint GlbJsonChunkType = 0x4E4F534AU;

    /// <summary>The GLB binary chunk type identifier.</summary>
    public const uint GlbBinChunkType = 0x004E4942U;

    /// <summary>The GLB header length in bytes.</summary>
    public const int GlbHeaderByteLength = 12;

    /// <summary>The GLB chunk header length in bytes.</summary>
    public const int GlbChunkHeaderByteLength = 8;

    /// <summary>The smallest possible GLB length containing a header and a chunk header.</summary>
    public const int MinimumGlbByteLength = 20;

    /// <summary>The required byte alignment for GLB chunks and aligned glTF data.</summary>
    public const int AlignmentByteLength = 4;

    /// <summary>The largest number of padding bytes required for 4-byte alignment.</summary>
    public const int MaximumPaddingByteLength = 3;

    /// <summary>The padding byte used by a GLB JSON chunk.</summary>
    public const byte GlbJsonPaddingByte = 0x20;

    /// <summary>The padding byte used by a GLB binary chunk.</summary>
    public const byte GlbBinPaddingByte = 0x00;

    /// <summary>The sentinel used by API fields when an object index is absent.</summary>
    public const int AbsentIndex = -1;

    /// <summary>The default byte offset when a glTF byte offset property is absent.</summary>
    public const int DefaultByteOffset = 0;

    /// <summary>The minimum legal byte stride for a core vertex buffer view.</summary>
    public const int MinimumByteStride = 4;

    /// <summary>The maximum legal byte stride for a core vertex buffer view.</summary>
    public const int MaximumByteStride = 252;

    /// <summary>The default maximum nesting depth accepted for JSON input.</summary>
    public const int DefaultMaximumJsonDepth = 128;

    /// <summary>The default maximum number of JSON tokens accepted from one document.</summary>
    public const int DefaultMaximumJsonTokenCount = 10000000;

    /// <summary>The default maximum UTF-8 byte length of one JSON string.</summary>
    public const int DefaultMaximumJsonStringByteLength = 16777216;

    /// <summary>The default maximum UTF-8 source byte length of one JSON number.</summary>
    public const int DefaultMaximumJsonNumberByteLength = 1024;

    /// <summary>The default maximum number of elements in one input collection.</summary>
    public const int DefaultMaximumCollectionCount = 1000000;

    /// <summary>The default maximum byte length of primary container input.</summary>
    public const int DefaultMaximumInputByteLength = 536870912;

    /// <summary>The default maximum byte length of JSON content.</summary>
    public const int DefaultMaximumJsonByteLength = 268435456;

    /// <summary>The default maximum decoded byte length of one data URI.</summary>
    public const int DefaultMaximumDataUriByteLength = 268435456;

    /// <summary>The default maximum byte length of one resolved resource.</summary>
    public const int DefaultMaximumResourceByteLength = 536870912;

    /// <summary>The default maximum total byte length of resolved resources.</summary>
    public const int DefaultMaximumTotalResourceByteLength = 1073741824;

    /// <summary>The default maximum number of validation issues collected in one report.</summary>
    public const int DefaultMaximumIssueCount = 10000;
}
