namespace icc;

/// <summary>
/// Reads and probes ICC profiles from bounded byte, memory, and stream inputs.
/// </summary>
public interface IIccProfileReader
{
    /// <summary>
    /// Reads a profile from encoded bytes.
    /// </summary>
    /// <param name="profileBytes">The complete encoded profile bytes.</param>
    /// <param name="options">The reading and resource-limit options.</param>
    /// <returns>The decoded profile.</returns>
    /// <exception cref="IccProfileFormatException">The input is malformed or incomplete.</exception>
    IccProfile ReadProfileBytes(System.ReadOnlySpan<byte> profileBytes, IccReadOptions options);

    /// <summary>
    /// Reads a profile from encoded memory whose ownership is controlled by the options.
    /// </summary>
    /// <param name="profileMemory">The complete encoded profile memory.</param>
    /// <param name="options">The reading and resource-limit options.</param>
    /// <returns>The decoded profile.</returns>
    /// <exception cref="IccProfileFormatException">The input is malformed or incomplete.</exception>
    IccProfile ReadProfileMemory(System.ReadOnlyMemory<byte> profileMemory, IccReadOptions options);

    /// <summary>
    /// Reads a profile from the current position of a readable stream.
    /// </summary>
    /// <param name="profileStream">The readable profile stream.</param>
    /// <param name="options">The reading and resource-limit options.</param>
    /// <returns>The decoded profile.</returns>
    /// <exception cref="IccProfileFormatException">The input is malformed or incomplete.</exception>
    IccProfile ReadProfileStream(System.IO.Stream profileStream, IccReadOptions options);

    /// <summary>
    /// Probes encoded bytes for a readable ICC profile without throwing for expected input failures.
    /// </summary>
    /// <param name="profileBytes">The encoded bytes to probe.</param>
    /// <param name="options">The reading and resource-limit options.</param>
    /// <returns>The probe outcome and failure location.</returns>
    IccOperationResult ProbeProfileBytes(System.ReadOnlySpan<byte> profileBytes, IccReadOptions options);

    /// <summary>
    /// Probes encoded memory for a readable ICC profile without throwing for expected input failures.
    /// </summary>
    /// <param name="profileMemory">The encoded memory to probe.</param>
    /// <param name="options">The reading and resource-limit options.</param>
    /// <returns>The probe outcome and failure location.</returns>
    IccOperationResult ProbeProfileMemory(System.ReadOnlyMemory<byte> profileMemory, IccReadOptions options);

    /// <summary>
    /// Probes a readable stream for an ICC profile without throwing for expected input failures.
    /// </summary>
    /// <param name="profileStream">The stream to probe from its current position.</param>
    /// <param name="options">The reading and resource-limit options.</param>
    /// <returns>The probe outcome and failure location.</returns>
    IccOperationResult ProbeProfileStream(System.IO.Stream profileStream, IccReadOptions options);
}
