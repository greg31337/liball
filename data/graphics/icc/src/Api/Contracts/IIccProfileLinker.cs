namespace icc;

/// <summary>
/// Compiles compatible profile sequences into DeviceLink profiles.
/// </summary>
public interface IIccProfileLinker
{
    /// <summary>
    /// Links an ordered profile sequence into a DeviceLink profile.
    /// </summary>
    /// <param name="profiles">The ordered profile sequence.</param>
    /// <param name="intent">The rendering intent retained by the link.</param>
    /// <param name="options">The serialization policy for deterministic link data.</param>
    /// <returns>The DeviceLink profile.</returns>
    /// <exception cref="IccProfileValidationException">The profile sequence is incompatible or invalid.</exception>
    IccProfile LinkProfileSequence(IccProfile[] profiles, IccRenderingIntent intent, IccWriteOptions options);

    /// <summary>
    /// Links an ordered profile sequence using explicit grid, encoding, intent, and metadata options.
    /// </summary>
    /// <param name="profiles">The ordered profile sequence.</param>
    /// <param name="linkOptions">The DeviceLink construction options.</param>
    /// <param name="writeOptions">The deterministic serialization policy and resource limits.</param>
    /// <returns>The encoded and decoded DeviceLink profile.</returns>
    /// <exception cref="IccProfileValidationException">The profile sequence or link options are incompatible or invalid.</exception>
    IccProfile LinkConfiguredProfileSequence(IccProfile[] profiles, IccProfileLinkOptions linkOptions, IccWriteOptions writeOptions);

    /// <summary>
    /// Checks whether an ordered profile sequence can be linked without throwing for expected incompatibility.
    /// </summary>
    /// <param name="profiles">The ordered profile sequence.</param>
    /// <param name="intent">The requested rendering intent.</param>
    /// <returns>The link capability outcome.</returns>
    IccOperationResult ProbeProfileSequence(IccProfile[] profiles, IccRenderingIntent intent);
}
