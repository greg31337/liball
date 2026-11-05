namespace gltf;

/// <summary>Provides typed accessor decoding and creation operations.</summary>
public class GltfAccessorCodec : IGltfAccessorCodec
{
    internal AccessorCodecEngine Engine;

    /// <summary>Initializes an accessor codec.</summary>
    public GltfAccessorCodec()
    {
        Engine = new AccessorCodecEngine();
    }

    /// <summary>Reads accessor values as floating-point components.</summary>
    /// <param name="request">The accessor read request.</param>
    /// <returns>The decoded accessor data.</returns>
    public GltfAccessorData ReadFloatingValues(GltfAccessorReadRequest request)
    {
        return Engine.ReadFloatingData(request);
    }

    /// <summary>Reads accessor values as signed integer components.</summary>
    /// <param name="request">The accessor read request.</param>
    /// <returns>The decoded accessor data.</returns>
    public GltfAccessorData ReadSignedValues(GltfAccessorReadRequest request)
    {
        return Engine.ReadSignedData(request);
    }

    /// <summary>Reads accessor values as unsigned integer components.</summary>
    /// <param name="request">The accessor read request.</param>
    /// <returns>The decoded accessor data.</returns>
    public GltfAccessorData ReadUnsignedValues(GltfAccessorReadRequest request)
    {
        return Engine.ReadUnsignedData(request);
    }

    /// <summary>Reads accessor values as indices.</summary>
    /// <param name="request">The accessor read request.</param>
    /// <returns>The decoded accessor data.</returns>
    public GltfAccessorData ReadIndices(GltfAccessorReadRequest request)
    {
        return Engine.ReadIndexData(request);
    }

    /// <summary>Reads accessor values as column-major matrix components.</summary>
    /// <param name="request">The accessor read request.</param>
    /// <returns>The decoded accessor data.</returns>
    public GltfAccessorData ReadMatrices(GltfAccessorReadRequest request)
    {
        return Engine.ReadMatrixData(request);
    }

    /// <summary>Reads tightly concatenated raw accessor elements.</summary>
    /// <param name="request">The accessor read request.</param>
    /// <returns>The decoded accessor data.</returns>
    public GltfAccessorData ReadRawElements(GltfAccessorReadRequest request)
    {
        return Engine.ReadRawData(request);
    }

    /// <summary>Creates a dense accessor from owned input values.</summary>
    /// <param name="request">The accessor write request.</param>
    /// <returns>The created accessor data.</returns>
    public GltfAccessorData CreateDenseAccessor(GltfAccessorWriteRequest request)
    {
        return Engine.CreateDenseData(request);
    }

    /// <summary>Creates a sparse accessor from owned input values.</summary>
    /// <param name="request">The accessor write request.</param>
    /// <returns>The created accessor data.</returns>
    public GltfAccessorData CreateSparseAccessor(GltfAccessorWriteRequest request)
    {
        return Engine.CreateSparseData(request);
    }
}
