namespace gltf;

/// <summary>Represents a declared glTF binary buffer.</summary>
public class GltfBuffer
{
    /// <summary>The optional external or data URI.</summary>
    public string? Uri;

    /// <summary>The declared buffer length in bytes.</summary>
    public int ByteLength;

    /// <summary>The optional buffer name.</summary>
    public string? Name;

    /// <summary>Whether the declared logical buffer bytes have been resolved.</summary>
    public bool Resolved;

    /// <summary>Owned resolved bytes limited to the declared buffer length.</summary>
    public byte[] ResolvedBytes = [];

    /// <summary>Whether a resource-set entry is bound to this buffer.</summary>
    public bool HasResource;

    /// <summary>The bound resource-set index, or <see cref="GltfConstants.AbsentIndex"/> when unbound.</summary>
    public int Resource = GltfConstants.AbsentIndex;

    /// <summary>The independently owned resource request retained for lazy resolution.</summary>
    public GltfResourceRequest? PendingResourceRequest;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents a contiguous view into a glTF buffer.</summary>
public class GltfBufferView
{
    /// <summary>The required buffer index.</summary>
    public int Buffer = GltfConstants.AbsentIndex;

    /// <summary>Whether the byte offset member was explicitly present.</summary>
    public bool HasByteOffset;

    /// <summary>The byte offset into the buffer, initialized to the glTF zero default.</summary>
    public int ByteOffset;

    /// <summary>The length of the buffer view in bytes.</summary>
    public int ByteLength;

    /// <summary>Whether the byte stride member was present.</summary>
    public bool HasByteStride;

    /// <summary>The byte stride between vertex attribute elements.</summary>
    public int ByteStride;

    /// <summary>Whether the target member was present.</summary>
    public bool HasTarget;

    /// <summary>The intended GPU buffer target, or <see cref="GltfBufferViewTarget.None"/> when absent.</summary>
    public GltfBufferViewTarget Target = GltfBufferViewTarget.None;

    /// <summary>The optional buffer view name.</summary>
    public string? Name;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}
