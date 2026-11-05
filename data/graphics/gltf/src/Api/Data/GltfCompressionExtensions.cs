using System.Collections.Generic;

namespace gltf;

/// <summary>Represents <c>KHR_draco_mesh_compression</c> primitive metadata.</summary>
public class GltfDracoMeshCompressionExtension
{
    /// <summary>The buffer-view index containing compressed geometry.</summary>
    public int BufferView = GltfConstants.AbsentIndex;

    /// <summary>Draco attribute identifiers keyed by glTF attribute semantic.</summary>
    public Dictionary<string, int> Attributes = new Dictionary<string, int>();

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents the marker payload of <c>KHR_mesh_quantization</c>.</summary>
public class GltfMeshQuantizationExtension
{
    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents buffer metadata shared by the KHR and EXT Meshopt extensions.</summary>
public class GltfMeshoptCompressionBufferExtension
{
    /// <summary>Whether the compressed-only fallback flag was explicitly provided.</summary>
    public bool HasFallback;

    /// <summary>Whether the buffer may be skipped when Meshopt is supported.</summary>
    public bool Fallback;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents buffer-view metadata shared by the KHR and EXT Meshopt extensions.</summary>
public class GltfMeshoptCompressionBufferViewExtension
{
    /// <summary>The buffer index containing compressed data.</summary>
    public int Buffer = GltfConstants.AbsentIndex;

    /// <summary>Whether the compressed byte offset was explicitly provided.</summary>
    public bool HasByteOffset;

    /// <summary>The compressed data byte offset.</summary>
    public int ByteOffset;

    /// <summary>The compressed data byte length.</summary>
    public int ByteLength;

    /// <summary>The decompressed element stride in bytes.</summary>
    public int ByteStride;

    /// <summary>The number of decompressed elements.</summary>
    public int Count;

    /// <summary>The required Meshopt compression mode.</summary>
    public string Mode = "";

    /// <summary>Whether the post-decode filter was explicitly provided.</summary>
    public bool HasFilter;

    /// <summary>The Meshopt post-decode filter.</summary>
    public string Filter = "NONE";

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents <c>KHR_texture_basisu</c> texture source selection.</summary>
public class GltfTextureBasisuExtension
{
    /// <summary>Whether a KTX2 image source was provided.</summary>
    public bool HasSource;

    /// <summary>The KTX2 image index.</summary>
    public int Source = GltfConstants.AbsentIndex;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents <c>EXT_texture_webp</c> texture source selection.</summary>
public class GltfTextureWebpExtension
{
    /// <summary>Whether a WebP image source was provided.</summary>
    public bool HasSource;

    /// <summary>The WebP image index.</summary>
    public int Source = GltfConstants.AbsentIndex;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}
