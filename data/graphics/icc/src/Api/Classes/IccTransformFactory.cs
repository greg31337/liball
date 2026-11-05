namespace icc;

/// <summary>Selects and compiles colour transforms.</summary>
public class IccTransformFactory : IIccTransformFactory
{
    internal ServiceAvailability Core;

    /// <summary>Initializes a transform factory.</summary>
    public IccTransformFactory()
    {
        Core = new ServiceAvailability("Transform compilation");
    }

    /// <summary>Creates a profile transform.</summary>
    /// <param name="sourceProfile">The source profile.</param>
    /// <param name="destinationProfile">The destination profile.</param>
    /// <param name="intent">The rendering intent.</param>
    /// <param name="fallback">The fallback policy.</param>
    /// <param name="interpolation">The interpolation mode.</param>
    /// <returns>The compiled transform.</returns>
    public IIccColorTransform CreateProfileTransform(IccProfile sourceProfile, IccProfile destinationProfile, IccRenderingIntent intent, IccIntentFallback fallback, IccInterpolationKind interpolation)
    {
        return ProfileTransformCompiler.Compile(new IccProfile[] { sourceProfile, destinationProfile }, intent, fallback, interpolation);
    }

    /// <summary>Creates a profile transform using explicit options.</summary>
    /// <param name="sourceProfile">The source profile.</param>
    /// <param name="destinationProfile">The destination profile.</param>
    /// <param name="options">The explicit transform options.</param>
    /// <returns>The compiled transform.</returns>
    public IIccColorTransform CreateConfiguredProfileTransform(IccProfile sourceProfile, IccProfile destinationProfile, IccTransformOptions options)
    {
        return ProfileTransformCompiler.CompileWithOptions(new IccProfile[] { sourceProfile, destinationProfile }, options);
    }

    /// <summary>Creates a proofing transform.</summary>
    /// <param name="sourceProfile">The source profile.</param>
    /// <param name="proofProfile">The proof profile.</param>
    /// <param name="destinationProfile">The destination profile.</param>
    /// <param name="renderingIntent">The rendering intent.</param>
    /// <param name="proofingIntent">The proofing intent.</param>
    /// <returns>The compiled transform.</returns>
    public IIccColorTransform CreateProofingTransform(IccProfile sourceProfile, IccProfile proofProfile, IccProfile destinationProfile, IccRenderingIntent renderingIntent, IccRenderingIntent proofingIntent)
    {
        IccProofingOptions options = new IccProofingOptions();
        options.RenderingIntent = renderingIntent;
        options.ProofingIntent = proofingIntent;
        return ProofingTransformCompiler.Compile(sourceProfile, proofProfile, destinationProfile, options);
    }

    /// <summary>Creates a soft-proofing transform using explicit proofing and gamut-alarm options.</summary>
    /// <param name="sourceProfile">The source profile.</param>
    /// <param name="proofProfile">The proof profile.</param>
    /// <param name="destinationProfile">The destination profile.</param>
    /// <param name="options">The explicit proofing options.</param>
    /// <returns>The compiled proofing transform.</returns>
    public IIccColorTransform CreateConfiguredProofingTransform(IccProfile sourceProfile, IccProfile proofProfile, IccProfile destinationProfile, IccProofingOptions options)
    {
        return ProofingTransformCompiler.Compile(sourceProfile, proofProfile, destinationProfile, options);
    }

    /// <summary>Creates a transform through a profile sequence.</summary>
    /// <param name="profiles">The profile sequence.</param>
    /// <param name="intent">The rendering intent.</param>
    /// <param name="fallback">The fallback policy.</param>
    /// <returns>The compiled transform.</returns>
    public IIccColorTransform CreateProfileSequenceTransform(IccProfile[] profiles, IccRenderingIntent intent, IccIntentFallback fallback)
    {
        return ProfileTransformCompiler.Compile(profiles, intent, fallback, IccInterpolationKind.Multilinear);
    }

    /// <summary>Creates a transform through a profile sequence using explicit options.</summary>
    /// <param name="profiles">The profile sequence.</param>
    /// <param name="options">The explicit transform options.</param>
    /// <returns>The compiled transform.</returns>
    public IIccColorTransform CreateConfiguredProfileSequenceTransform(IccProfile[] profiles, IccTransformOptions options)
    {
        return ProfileTransformCompiler.CompileWithOptions(profiles, options);
    }

    /// <summary>Creates the one-way transform stored in a DeviceLink profile.</summary>
    /// <param name="deviceLinkProfile">The DeviceLink profile.</param>
    /// <param name="options">The direct transform options.</param>
    /// <returns>The compiled DeviceLink transform.</returns>
    public IIccColorTransform CreateDeviceLinkTransform(IccProfile deviceLinkProfile, IccTransformOptions options)
    {
        return DeviceLinkTransformCompiler.Compile(deviceLinkProfile, options);
    }

    /// <summary>Probes profile transform compatibility.</summary>
    /// <param name="sourceProfile">The source profile.</param>
    /// <param name="destinationProfile">The destination profile.</param>
    /// <param name="intent">The rendering intent.</param>
    /// <returns>The probe result.</returns>
    public IccOperationResult ProbeProfileTransform(IccProfile sourceProfile, IccProfile destinationProfile, IccRenderingIntent intent)
    {
        return ProfileTransformCompiler.ProbePair(sourceProfile, destinationProfile, intent);
    }
}
