namespace icc;

/// <summary>Reads and probes ICC profiles from bounded inputs.</summary>
public class IccProfileReader : IIccProfileReader
{
    internal ProfileReaderCore Core;

    /// <summary>Initializes a profile reader.</summary>
    public IccProfileReader()
    {
        Core = new ProfileReaderCore();
    }

    /// <summary>Reads a profile from bytes.</summary>
    /// <param name="profileBytes">The encoded bytes.</param>
    /// <param name="options">The reading options.</param>
    /// <returns>The decoded profile.</returns>
    public IccProfile ReadProfileBytes(System.ReadOnlySpan<byte> profileBytes, IccReadOptions options)
    {
        return Core.ReadBytes(profileBytes, options);
    }

    /// <summary>Reads a profile from memory.</summary>
    /// <param name="profileMemory">The encoded memory.</param>
    /// <param name="options">The reading options.</param>
    /// <returns>The decoded profile.</returns>
    public IccProfile ReadProfileMemory(System.ReadOnlyMemory<byte> profileMemory, IccReadOptions options)
    {
        return Core.ReadMemory(profileMemory, options);
    }

    /// <summary>Reads a profile from a stream.</summary>
    /// <param name="profileStream">The encoded stream.</param>
    /// <param name="options">The reading options.</param>
    /// <returns>The decoded profile.</returns>
    public IccProfile ReadProfileStream(System.IO.Stream profileStream, IccReadOptions options)
    {
        return Core.ReadStream(profileStream, options);
    }

    /// <summary>Probes profile bytes without throwing for unsupported input.</summary>
    /// <param name="profileBytes">The encoded bytes.</param>
    /// <param name="options">The reading options.</param>
    /// <returns>The probe result.</returns>
    public IccOperationResult ProbeProfileBytes(System.ReadOnlySpan<byte> profileBytes, IccReadOptions options)
    {
        return Core.ProbeBytes(profileBytes, options);
    }

    /// <summary>Probes profile memory without throwing for unsupported input.</summary>
    /// <param name="profileMemory">The encoded memory.</param>
    /// <param name="options">The reading options.</param>
    /// <returns>The probe result.</returns>
    public IccOperationResult ProbeProfileMemory(System.ReadOnlyMemory<byte> profileMemory, IccReadOptions options)
    {
        return Core.ProbeMemory(profileMemory, options);
    }

    /// <summary>Probes a profile stream without throwing for unsupported input.</summary>
    /// <param name="profileStream">The encoded stream.</param>
    /// <param name="options">The reading options.</param>
    /// <returns>The probe result.</returns>
    public IccOperationResult ProbeProfileStream(System.IO.Stream profileStream, IccReadOptions options)
    {
        return Core.ProbeStream(profileStream, options);
    }
}
