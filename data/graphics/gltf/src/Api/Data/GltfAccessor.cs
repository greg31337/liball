namespace gltf;

/// <summary>Represents a typed view of glTF binary data.</summary>
public class GltfAccessor
{
    /// <summary>Whether the buffer view reference was present.</summary>
    public bool HasBufferView;

    /// <summary>The buffer view index, or <see cref="GltfConstants.AbsentIndex"/> when absent.</summary>
    public int BufferView = GltfConstants.AbsentIndex;

    /// <summary>Whether the byte offset member was explicitly present.</summary>
    public bool HasByteOffset;

    /// <summary>The byte offset into the buffer view, initialized to the glTF zero default.</summary>
    public int ByteOffset;

    /// <summary>The required binary component representation.</summary>
    public GltfComponentType ComponentType = GltfComponentType.Float;

    /// <summary>Whether the normalized member was explicitly present.</summary>
    public bool HasNormalized;

    /// <summary>Whether integer components are normalized before use.</summary>
    public bool Normalized;

    /// <summary>The number of elements in the accessor.</summary>
    public int Count;

    /// <summary>The required logical element shape.</summary>
    public GltfAccessorType Type = GltfAccessorType.Scalar;

    /// <summary>The optional declared maximum value for each component.</summary>
    public double[]? Maximum;

    /// <summary>The optional declared minimum value for each component.</summary>
    public double[]? Minimum;

    /// <summary>The optional sparse storage description.</summary>
    public GltfAccessorSparse? Sparse;

    /// <summary>The optional accessor name.</summary>
    public string? Name;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Describes sparse values that override an accessor's base data.</summary>
public class GltfAccessorSparse
{
    /// <summary>The number of sparse elements.</summary>
    public int Count;

    /// <summary>The required sparse index storage.</summary>
    public GltfAccessorSparseIndices Indices = new GltfAccessorSparseIndices();

    /// <summary>The required sparse value storage.</summary>
    public GltfAccessorSparseValues Values = new GltfAccessorSparseValues();

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Describes the index data of a sparse accessor.</summary>
public class GltfAccessorSparseIndices
{
    /// <summary>The required buffer view index.</summary>
    public int BufferView = GltfConstants.AbsentIndex;

    /// <summary>Whether the byte offset member was explicitly present.</summary>
    public bool HasByteOffset;

    /// <summary>The byte offset into the buffer view, initialized to the glTF zero default.</summary>
    public int ByteOffset;

    /// <summary>The unsigned integer representation of sparse indices.</summary>
    public GltfComponentType ComponentType = GltfComponentType.UnsignedShort;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Describes the replacement value data of a sparse accessor.</summary>
public class GltfAccessorSparseValues
{
    /// <summary>The required buffer view index.</summary>
    public int BufferView = GltfConstants.AbsentIndex;

    /// <summary>Whether the byte offset member was explicitly present.</summary>
    public bool HasByteOffset;

    /// <summary>The byte offset into the buffer view, initialized to the glTF zero default.</summary>
    public int ByteOffset;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}
