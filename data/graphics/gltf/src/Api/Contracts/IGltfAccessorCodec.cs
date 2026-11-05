namespace gltf;

/// <summary>Decodes typed accessor values and creates dense or sparse accessors.</summary>
public interface IGltfAccessorCodec
{
    /// <summary>Reads accessor values as floating-point components.</summary>
    /// <param name="request">The accessor read request.</param>
    /// <returns>The decoded accessor data.</returns>
    GltfAccessorData ReadFloatingValues(GltfAccessorReadRequest request);

    /// <summary>Reads accessor values as signed integer components.</summary>
    /// <param name="request">The accessor read request.</param>
    /// <returns>The decoded accessor data.</returns>
    GltfAccessorData ReadSignedValues(GltfAccessorReadRequest request);

    /// <summary>Reads accessor values as unsigned integer components.</summary>
    /// <param name="request">The accessor read request.</param>
    /// <returns>The decoded accessor data.</returns>
    GltfAccessorData ReadUnsignedValues(GltfAccessorReadRequest request);

    /// <summary>Reads accessor values as indices.</summary>
    /// <param name="request">The accessor read request.</param>
    /// <returns>The decoded accessor data.</returns>
    GltfAccessorData ReadIndices(GltfAccessorReadRequest request);

    /// <summary>Reads accessor values as column-major matrix components.</summary>
    /// <param name="request">The accessor read request.</param>
    /// <returns>The decoded accessor data.</returns>
    GltfAccessorData ReadMatrices(GltfAccessorReadRequest request);

    /// <summary>Reads tightly concatenated raw accessor elements.</summary>
    /// <param name="request">The accessor read request.</param>
    /// <returns>The decoded accessor data.</returns>
    GltfAccessorData ReadRawElements(GltfAccessorReadRequest request);

    /// <summary>Creates a dense accessor from owned input values.</summary>
    /// <param name="request">The accessor write request.</param>
    /// <returns>The created accessor data.</returns>
    GltfAccessorData CreateDenseAccessor(GltfAccessorWriteRequest request);

    /// <summary>Creates a sparse accessor from owned input values.</summary>
    /// <param name="request">The accessor write request.</param>
    /// <returns>The created accessor data.</returns>
    GltfAccessorData CreateSparseAccessor(GltfAccessorWriteRequest request);
}
