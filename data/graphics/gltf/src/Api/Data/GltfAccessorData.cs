namespace gltf;

/// <summary>Describes a typed accessor read operation.</summary>
public class GltfAccessorReadRequest
{
    /// <summary>The source document.</summary>
    public GltfDocument? Document;

    /// <summary>The accessor index to read.</summary>
    public int Accessor = GltfConstants.AbsentIndex;

    /// <summary>Whether normalized integer values are converted to normalized floating-point values.</summary>
    public bool ApplyNormalization = true;

    /// <summary>Whether sparse values are overlaid on the base accessor.</summary>
    public bool ApplySparseValues = true;

    /// <summary>The largest output element count permitted.</summary>
    public int MaximumElementCount = GltfConstants.DefaultMaximumCollectionCount;
}

/// <summary>Contains one owned typed representation of decoded accessor data.</summary>
public class GltfAccessorData
{
    /// <summary>The overall decode status.</summary>
    public GltfOperationStatus Status = GltfOperationStatus.Success;

    /// <summary>Whether an accessor index was created or returned.</summary>
    public bool HasAccessor;

    /// <summary>The created or returned accessor index.</summary>
    public int Accessor = GltfConstants.AbsentIndex;

    /// <summary>The source component representation.</summary>
    public GltfComponentType ComponentType = GltfComponentType.Float;

    /// <summary>The logical accessor element shape.</summary>
    public GltfAccessorType Type = GltfAccessorType.Scalar;

    /// <summary>The number of accessor elements.</summary>
    public int Count;

    /// <summary>The logical number of components per element.</summary>
    public int ComponentCount;

    /// <summary>Whether decoded integer data was normalized.</summary>
    public bool Normalized;

    /// <summary>Owned floating-point components when requested.</summary>
    public double[] FloatingValues = [];

    /// <summary>Owned signed integer components when requested.</summary>
    public int[] SignedValues = [];

    /// <summary>Owned unsigned integer components when requested.</summary>
    public uint[] UnsignedValues = [];

    /// <summary>Owned decoded indices when requested.</summary>
    public uint[] Indices = [];

    /// <summary>Owned matrix components in column-major logical order when requested.</summary>
    public double[] MatrixValues = [];

    /// <summary>Owned tightly concatenated raw element bytes when requested.</summary>
    public byte[] RawBytes = [];

    /// <summary>Decode diagnostics.</summary>
    public GltfValidationIssue? Diagnostic;
}

/// <summary>Describes creation of an accessor and its owned input values.</summary>
public class GltfAccessorWriteRequest
{
    /// <summary>The destination document.</summary>
    public GltfDocument? Document;

    /// <summary>The desired component representation.</summary>
    public GltfComponentType ComponentType = GltfComponentType.Float;

    /// <summary>The desired logical element shape.</summary>
    public GltfAccessorType Type = GltfAccessorType.Scalar;

    /// <summary>Whether integer values use normalized encoding.</summary>
    public bool Normalized;

    /// <summary>Owned floating-point input components.</summary>
    public double[] FloatingValues = [];

    /// <summary>Owned signed integer input components.</summary>
    public int[] SignedValues = [];

    /// <summary>Owned unsigned integer input components.</summary>
    public uint[] UnsignedValues = [];

    /// <summary>Owned raw element bytes.</summary>
    public byte[] RawBytes = [];

    /// <summary>Whether sparse storage may be generated.</summary>
    public bool AllowSparse;

    /// <summary>The zero tolerance used when sparse storage is allowed.</summary>
    public double SparseZeroTolerance;

    /// <summary>Whether accessor minima and maxima are generated.</summary>
    public bool CalculateBounds = true;
}
