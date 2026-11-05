namespace avif;

/// <summary>Defines an independently owned opaque AV1 metadata OBU value.</summary>
public interface IAvifMetadataObu
{
    /// <summary>Returns the construction result.</summary>
    /// <returns>The stable result code.</returns>
    AvifResultCode GetCode();

    /// <summary>Returns the unsigned AV1 metadata type.</summary>
    /// <returns>The metadata type value.</returns>
    ulong GetMetadataType();

    /// <summary>Returns an independent copy of the opaque metadata bytes.</summary>
    /// <returns>The bytes following the metadata type and preceding trailing bits.</returns>
    byte[] GetData();
}
