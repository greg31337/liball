using System.Collections.Generic;

namespace gltf;

/// <summary>Represents owned JSON and chunk data extracted from a glTF container.</summary>
public class GltfContainerData
{
    /// <summary>The detected or requested container format.</summary>
    public GltfContainerFormat Format = GltfContainerFormat.Auto;

    /// <summary>The owned UTF-8 JSON bytes.</summary>
    public byte[] JsonBytes = [];

    /// <summary>Whether a GLB binary chunk was present.</summary>
    public bool HasBinChunk;

    /// <summary>The owned GLB binary chunk bytes.</summary>
    public byte[] BinBytes = [];

    /// <summary>Preserved unknown GLB chunks in source order.</summary>
    public List<GltfGlbChunk> UnknownChunks = new List<GltfGlbChunk>();

    /// <summary>Owned bytes found after the declared container end.</summary>
    public byte[] TrailingBytes = [];

    /// <summary>The original primary input length in bytes.</summary>
    public int OriginalByteLength;
}

/// <summary>Represents an owned GLB chunk not interpreted by the core container layer.</summary>
public class GltfGlbChunk
{
    /// <summary>The unsigned binary chunk type identifier.</summary>
    public uint Type;

    /// <summary>The owned chunk payload.</summary>
    public byte[] Data = [];
}
