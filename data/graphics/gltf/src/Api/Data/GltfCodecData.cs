using System.Collections.Generic;

namespace gltf;

/// <summary>Describes a codec-neutral geometry compression operation.</summary>
public class GltfCompressionRequest
{
    /// <summary>The extension or codec capability name.</summary>
    public string Codec = "";

    /// <summary>The source document when the operation targets model objects.</summary>
    public GltfDocument? Document;

    /// <summary>Whether a mesh index was supplied.</summary>
    public bool HasMesh;

    /// <summary>The mesh index.</summary>
    public int Mesh = GltfConstants.AbsentIndex;

    /// <summary>Whether a primitive index was supplied.</summary>
    public bool HasPrimitive;

    /// <summary>The mesh-local primitive index.</summary>
    public int Primitive = GltfConstants.AbsentIndex;

    /// <summary>The owned compressed or uncompressed source bytes.</summary>
    public byte[] Bytes = [];

    /// <summary>Owned attribute streams keyed by semantic.</summary>
    public Dictionary<string, byte[]> Attributes = new Dictionary<string, byte[]>();

    /// <summary>The optional Draco metadata associated with a geometry request.</summary>
    public GltfDracoMeshCompressionExtension? Draco;

    /// <summary>The optional Meshopt metadata associated with a buffer-view request.</summary>
    public GltfMeshoptCompressionBufferViewExtension? Meshopt;

    /// <summary>The maximum allowed output length in bytes.</summary>
    public int MaximumOutputByteLength = GltfConstants.DefaultMaximumResourceByteLength;
}

/// <summary>Contains owned geometry compression output and diagnostics.</summary>
public class GltfCompressionResult
{
    /// <summary>The overall codec status.</summary>
    public GltfOperationStatus Status = GltfOperationStatus.Success;

    /// <summary>The codec implementation identifier.</summary>
    public string? Codec;

    /// <summary>The owned primary encoded or decoded bytes.</summary>
    public byte[] Bytes = [];

    /// <summary>Owned decoded attribute streams keyed by semantic.</summary>
    public Dictionary<string, byte[]> Attributes = new Dictionary<string, byte[]>();

    /// <summary>Codec diagnostics.</summary>
    public List<GltfValidationIssue> Diagnostics = new List<GltfValidationIssue>();
}

/// <summary>Describes image inspection, decode, encode, or transcode input.</summary>
public class GltfImageRequest
{
    /// <summary>The extension or codec capability name.</summary>
    public string Codec = "";

    /// <summary>The source document when the operation targets model objects.</summary>
    public GltfDocument? Document;

    /// <summary>Whether a texture index was supplied.</summary>
    public bool HasTexture;

    /// <summary>The texture index.</summary>
    public int Texture = GltfConstants.AbsentIndex;

    /// <summary>Whether an image index was supplied.</summary>
    public bool HasImage;

    /// <summary>The image index.</summary>
    public int Image = GltfConstants.AbsentIndex;

    /// <summary>The owned encoded or decoded source bytes.</summary>
    public byte[] Bytes = [];

    /// <summary>The declared source media type.</summary>
    public string? MimeType;

    /// <summary>The recognized or requested source format.</summary>
    public GltfImageFormat SourceFormat = GltfImageFormat.Unknown;

    /// <summary>The requested output format.</summary>
    public GltfImageFormat TargetFormat = GltfImageFormat.Unknown;

    /// <summary>Whether an output width was requested.</summary>
    public bool HasTargetWidth;

    /// <summary>The requested output width in pixels.</summary>
    public int TargetWidth;

    /// <summary>Whether an output height was requested.</summary>
    public bool HasTargetHeight;

    /// <summary>The requested output height in pixels.</summary>
    public int TargetHeight;

    /// <summary>The maximum allowed output length in bytes.</summary>
    public int MaximumOutputByteLength = GltfConstants.DefaultMaximumResourceByteLength;
}

/// <summary>Contains recognized image header metadata.</summary>
public class GltfImageInfo
{
    /// <summary>The recognized image format.</summary>
    public GltfImageFormat Format = GltfImageFormat.Unknown;

    /// <summary>The recognized image media type.</summary>
    public string? MimeType;

    /// <summary>Whether dimensions were recognized.</summary>
    public bool HasDimensions;

    /// <summary>The image width in pixels.</summary>
    public int Width;

    /// <summary>The image height in pixels.</summary>
    public int Height;

    /// <summary>Whether a component count was recognized.</summary>
    public bool HasComponentCount;

    /// <summary>The number of image components.</summary>
    public int ComponentCount;

    /// <summary>Whether a component bit depth was recognized.</summary>
    public bool HasBitDepth;

    /// <summary>The number of bits per component.</summary>
    public int BitDepth;

    /// <summary>Whether the encoded image is animated.</summary>
    public bool Animated;

    /// <summary>Whether unsupported color profile features were detected.</summary>
    public bool HasUnsupportedColorProfile;
}

/// <summary>Contains owned image codec output, metadata, and diagnostics.</summary>
public class GltfImageResult
{
    /// <summary>The overall codec status.</summary>
    public GltfOperationStatus Status = GltfOperationStatus.Success;

    /// <summary>The codec implementation identifier.</summary>
    public string? Codec;

    /// <summary>The owned encoded or decoded output bytes.</summary>
    public byte[] Bytes = [];

    /// <summary>The output image metadata.</summary>
    public GltfImageInfo Info = new GltfImageInfo();

    /// <summary>Codec diagnostics.</summary>
    public List<GltfValidationIssue> Diagnostics = new List<GltfValidationIssue>();
}
