namespace gltf;

/// <summary>Represents a glTF texture binding.</summary>
public class GltfTexture
{
    /// <summary>Whether the sampler reference was present.</summary>
    public bool HasSampler;

    /// <summary>The sampler index, or <see cref="GltfConstants.AbsentIndex"/> when absent.</summary>
    public int Sampler = GltfConstants.AbsentIndex;

    /// <summary>Whether the image source reference was present.</summary>
    public bool HasSource;

    /// <summary>The image index, or <see cref="GltfConstants.AbsentIndex"/> when absent.</summary>
    public int Source = GltfConstants.AbsentIndex;

    /// <summary>The optional texture name.</summary>
    public string? Name;

    /// <summary>Whether an effective image source was selected while reading.</summary>
    public bool HasSelectedSource;

    /// <summary>The selected image index, or <see cref="GltfConstants.AbsentIndex"/> when unselected.</summary>
    public int SelectedSource = GltfConstants.AbsentIndex;

    /// <summary>The decoded extension image output when an image codec processed the selected source.</summary>
    public GltfImageResult? DecodedImage;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents glTF texture filtering and wrapping parameters.</summary>
public class GltfSampler
{
    /// <summary>Whether the magnification filter member was present.</summary>
    public bool HasMagnificationFilter;

    /// <summary>The magnification filter, ignored when its presence field is false.</summary>
    public GltfSamplerFilter MagnificationFilter = GltfSamplerFilter.Linear;

    /// <summary>Whether the minification filter member was present.</summary>
    public bool HasMinificationFilter;

    /// <summary>The minification filter, ignored when its presence field is false.</summary>
    public GltfSamplerFilter MinificationFilter = GltfSamplerFilter.Linear;

    /// <summary>Whether the S wrapping member was explicitly present.</summary>
    public bool HasWrapS;

    /// <summary>The S wrapping mode, initialized to the glTF repeat default.</summary>
    public GltfSamplerWrap WrapS = GltfSamplerWrap.Repeat;

    /// <summary>Whether the T wrapping member was explicitly present.</summary>
    public bool HasWrapT;

    /// <summary>The T wrapping mode, initialized to the glTF repeat default.</summary>
    public GltfSamplerWrap WrapT = GltfSamplerWrap.Repeat;

    /// <summary>The optional sampler name.</summary>
    public string? Name;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents an encoded glTF image reference.</summary>
public class GltfImage
{
    /// <summary>The optional external or data URI.</summary>
    public string? Uri;

    /// <summary>The optional declared image media type.</summary>
    public string? MimeType;

    /// <summary>Whether the buffer view reference was present.</summary>
    public bool HasBufferView;

    /// <summary>The image buffer view index, or <see cref="GltfConstants.AbsentIndex"/> when absent.</summary>
    public int BufferView = GltfConstants.AbsentIndex;

    /// <summary>The optional image name.</summary>
    public string? Name;

    /// <summary>Whether the encoded image bytes have been resolved.</summary>
    public bool Resolved;

    /// <summary>The independently owned encoded image bytes.</summary>
    public byte[] ResolvedBytes = [];

    /// <summary>Whether a resource-set entry is bound to this image.</summary>
    public bool HasResource;

    /// <summary>The bound resource-set index, or <see cref="GltfConstants.AbsentIndex"/> when unbound.</summary>
    public int Resource = GltfConstants.AbsentIndex;

    /// <summary>The effective resolved URI or buffer-view identity.</summary>
    public string? EffectiveUri;

    /// <summary>The effective media type retained with the resolved image.</summary>
    public string? ResolvedMimeType;

    /// <summary>The independently owned resource request retained for lazy URI resolution.</summary>
    public GltfResourceRequest? PendingResourceRequest;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}
