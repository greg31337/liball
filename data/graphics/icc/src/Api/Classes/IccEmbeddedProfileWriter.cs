namespace icc;

/// <summary>Inserts, replaces, and removes embedded ICC profiles.</summary>
public class IccEmbeddedProfileWriter : IIccEmbeddedProfileWriter
{
    internal EmbeddedProfileWriterCore Core;

    /// <summary>Initializes an embedded-profile writer.</summary>
    public IccEmbeddedProfileWriter()
    {
        Core = new EmbeddedProfileWriterCore();
    }

    /// <summary>Inserts a profile into carrier bytes.</summary>
    /// <param name="carrierBytes">The carrier bytes.</param>
    /// <param name="profile">The profile to insert.</param>
    /// <param name="format">The carrier format.</param>
    /// <param name="options">The writing options.</param>
    /// <returns>The updated carrier bytes.</returns>
    public byte[] InsertProfileBytes(System.ReadOnlySpan<byte> carrierBytes, IccProfile profile, IccEmbeddedFormat format, IccWriteOptions options)
    {
        return Core.InsertBytes(carrierBytes, profile, format, options);
    }

    /// <summary>Replaces a profile in carrier bytes.</summary>
    /// <param name="carrierBytes">The carrier bytes.</param>
    /// <param name="profileIndex">The profile index.</param>
    /// <param name="profile">The replacement profile.</param>
    /// <param name="format">The carrier format.</param>
    /// <param name="options">The writing options.</param>
    /// <returns>The updated carrier bytes.</returns>
    public byte[] ReplaceProfileBytes(System.ReadOnlySpan<byte> carrierBytes, int profileIndex, IccProfile profile, IccEmbeddedFormat format, IccWriteOptions options)
    {
        return Core.ReplaceBytes(carrierBytes, profileIndex, profile, format,
            options);
    }

    /// <summary>Removes a profile from carrier bytes.</summary>
    /// <param name="carrierBytes">The carrier bytes.</param>
    /// <param name="profileIndex">The profile index.</param>
    /// <param name="format">The carrier format.</param>
    /// <returns>The updated carrier bytes.</returns>
    public byte[] RemoveProfileBytes(System.ReadOnlySpan<byte> carrierBytes, int profileIndex, IccEmbeddedFormat format)
    {
        return Core.RemoveBytes(carrierBytes, profileIndex, format);
    }

    /// <summary>Inserts a profile while copying streams.</summary>
    /// <param name="sourceStream">The source stream.</param>
    /// <param name="destinationStream">The destination stream.</param>
    /// <param name="profile">The profile to insert.</param>
    /// <param name="format">The carrier format.</param>
    /// <param name="options">The writing options.</param>
    public void InsertProfileStream(System.IO.Stream sourceStream, System.IO.Stream destinationStream, IccProfile profile, IccEmbeddedFormat format, IccWriteOptions options)
    {
        Core.InsertStream(sourceStream, destinationStream, profile, format,
            options);
    }

    /// <summary>Replaces a profile while copying streams.</summary>
    /// <param name="sourceStream">The source stream.</param>
    /// <param name="destinationStream">The destination stream.</param>
    /// <param name="profileIndex">The profile index.</param>
    /// <param name="profile">The replacement profile.</param>
    /// <param name="format">The carrier format.</param>
    /// <param name="options">The writing options.</param>
    public void ReplaceProfileStream(System.IO.Stream sourceStream, System.IO.Stream destinationStream, int profileIndex, IccProfile profile, IccEmbeddedFormat format, IccWriteOptions options)
    {
        Core.ReplaceStream(sourceStream, destinationStream, profileIndex,
            profile, format, options);
    }

    /// <summary>Removes a profile while copying streams.</summary>
    /// <param name="sourceStream">The source stream.</param>
    /// <param name="destinationStream">The destination stream.</param>
    /// <param name="profileIndex">The profile index.</param>
    /// <param name="format">The carrier format.</param>
    public void RemoveProfileStream(System.IO.Stream sourceStream, System.IO.Stream destinationStream, int profileIndex, IccEmbeddedFormat format)
    {
        Core.RemoveStream(sourceStream, destinationStream, profileIndex,
            format);
    }

    /// <summary>Probes carrier operation support.</summary>
    /// <param name="format">The carrier format.</param>
    /// <param name="operation">The requested operation.</param>
    /// <returns>The probe result.</returns>
    public IccOperationResult ProbeEmbeddingOperation(IccEmbeddedFormat format, IccEmbeddedOperation operation)
    {
        return Core.ProbeOperation(format, operation);
    }
}
