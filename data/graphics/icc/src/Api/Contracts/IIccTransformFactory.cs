namespace icc;

/// <summary>
/// Selects and compiles profile, proofing, and multiprofile colour transforms.
/// </summary>
public interface IIccTransformFactory
{
    /// <summary>
    /// Compiles a transform between source and destination profiles.
    /// </summary>
    /// <param name="sourceProfile">The source profile.</param>
    /// <param name="destinationProfile">The destination profile.</param>
    /// <param name="intent">The rendering intent.</param>
    /// <param name="fallback">The intent fallback policy.</param>
    /// <param name="interpolation">The interpolation algorithm.</param>
    /// <returns>The compiled transform.</returns>
    /// <exception cref="IccTransformException">No compatible transform can be compiled.</exception>
    IIccColorTransform CreateProfileTransform(IccProfile sourceProfile, IccProfile destinationProfile, IccRenderingIntent intent, IccIntentFallback fallback, IccInterpolationKind interpolation);

    /// <summary>
    /// Compiles a transform between source and destination profiles using explicit options.
    /// </summary>
    /// <param name="sourceProfile">The source profile.</param>
    /// <param name="destinationProfile">The destination profile.</param>
    /// <param name="options">The transform options, including optional black-point compensation.</param>
    /// <returns>The compiled transform.</returns>
    /// <exception cref="IccTransformException">No compatible transform can be compiled.</exception>
    IIccColorTransform CreateConfiguredProfileTransform(IccProfile sourceProfile, IccProfile destinationProfile, IccTransformOptions options);

    /// <summary>
    /// Compiles a soft-proofing transform through a proof profile.
    /// </summary>
    /// <param name="sourceProfile">The source profile.</param>
    /// <param name="proofProfile">The proofing profile.</param>
    /// <param name="destinationProfile">The destination profile.</param>
    /// <param name="renderingIntent">The source-to-destination rendering intent.</param>
    /// <param name="proofingIntent">The proofing rendering intent.</param>
    /// <returns>The compiled proofing transform.</returns>
    /// <exception cref="IccTransformException">No compatible proofing transform can be compiled.</exception>
    IIccColorTransform CreateProofingTransform(IccProfile sourceProfile, IccProfile proofProfile, IccProfile destinationProfile, IccRenderingIntent renderingIntent, IccRenderingIntent proofingIntent);

    /// <summary>
    /// Compiles a soft-proofing transform using explicit proofing and gamut-alarm options.
    /// </summary>
    /// <param name="sourceProfile">The source profile.</param>
    /// <param name="proofProfile">The proofing profile.</param>
    /// <param name="destinationProfile">The destination profile.</param>
    /// <param name="options">The proofing, gamut-check, and alarm options.</param>
    /// <returns>The compiled proofing transform.</returns>
    /// <exception cref="IccTransformException">No compatible proofing transform can be compiled.</exception>
    IIccColorTransform CreateConfiguredProofingTransform(IccProfile sourceProfile, IccProfile proofProfile, IccProfile destinationProfile, IccProofingOptions options);

    /// <summary>
    /// Compiles a transform through an ordered profile sequence.
    /// </summary>
    /// <param name="profiles">The ordered profile sequence.</param>
    /// <param name="intent">The rendering intent.</param>
    /// <param name="fallback">The intent fallback policy.</param>
    /// <returns>The compiled multiprofile transform.</returns>
    /// <exception cref="IccTransformException">The sequence cannot be connected.</exception>
    IIccColorTransform CreateProfileSequenceTransform(IccProfile[] profiles, IccRenderingIntent intent, IccIntentFallback fallback);

    /// <summary>
    /// Compiles a transform through an ordered profile sequence using explicit options.
    /// </summary>
    /// <param name="profiles">The ordered profile sequence.</param>
    /// <param name="options">The transform options, including optional black-point compensation.</param>
    /// <returns>The compiled multiprofile transform.</returns>
    /// <exception cref="IccTransformException">The sequence cannot be connected.</exception>
    IIccColorTransform CreateConfiguredProfileSequenceTransform(IccProfile[] profiles, IccTransformOptions options);

    /// <summary>
    /// Compiles the one-way device-to-device transform stored in a DeviceLink profile.
    /// </summary>
    /// <param name="deviceLinkProfile">The DeviceLink profile.</param>
    /// <param name="options">The requested intent, fallback, interpolation, clipping, and trace options.</param>
    /// <returns>The compiled direct DeviceLink transform.</returns>
    /// <exception cref="IccTransformException">The profile is incompatible or its direct transform cannot be compiled.</exception>
    IIccColorTransform CreateDeviceLinkTransform(IccProfile deviceLinkProfile, IccTransformOptions options);

    /// <summary>
    /// Checks whether two profiles can form the requested transform without throwing for expected incompatibility.
    /// </summary>
    /// <param name="sourceProfile">The source profile.</param>
    /// <param name="destinationProfile">The destination profile.</param>
    /// <param name="intent">The rendering intent.</param>
    /// <returns>The transform capability outcome.</returns>
    IccOperationResult ProbeProfileTransform(IccProfile sourceProfile, IccProfile destinationProfile, IccRenderingIntent intent);
}
