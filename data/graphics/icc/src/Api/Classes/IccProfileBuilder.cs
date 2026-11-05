namespace icc;

/// <summary>Creates and safely mutates profile data models.</summary>
public class IccProfileBuilder : IIccProfileBuilder
{
    internal ProfileConstructionCore Core;

    /// <summary>Initializes a profile builder.</summary>
    public IccProfileBuilder()
    {
        Core = new ProfileConstructionCore();
    }

    /// <summary>Creates a canonical profile skeleton with generation-correct metadata.</summary>
    /// <param name="options">The profile header and metadata definition.</param>
    /// <returns>The writable profile skeleton.</returns>
    public IccProfile CreateProfileSkeleton(IccProfileBuildOptions options)
    {
        return Core.CreateProfileSkeleton(options);
    }

    /// <summary>Creates a complete RGB matrix/TRC profile from colourimetric definition data.</summary>
    /// <param name="options">The profile metadata and RGB colourimetric definition.</param>
    /// <returns>The canonical RGB profile.</returns>
    public IccProfile CreateRgbProfile(IccProfileBuildOptions options)
    {
        return Core.CreateRgbProfile(options);
    }

    /// <summary>Creates a complete monochrome profile from white-point and tone-curve data.</summary>
    /// <param name="options">The profile metadata and gray colourimetric definition.</param>
    /// <returns>The canonical gray profile.</returns>
    public IccProfile CreateGrayProfile(IccProfileBuildOptions options)
    {
        return Core.CreateGrayProfile(options);
    }

    /// <summary>Creates a standards-valid XYZ-to-PCSXYZ identity profile.</summary>
    /// <param name="options">The profile metadata and identity-profile class selection.</param>
    /// <returns>The canonical XYZ identity profile.</returns>
    public IccProfile CreateXyzIdentityProfile(IccProfileBuildOptions options)
    {
        return Core.CreateXyzIdentityProfile(options);
    }

    /// <summary>Creates a PCS-to-PCS abstract profile from caller-supplied transform data.</summary>
    /// <param name="options">The profile metadata and abstract transform definition.</param>
    /// <returns>The canonical abstract profile.</returns>
    public IccProfile CreateAbstractProfile(IccProfileBuildOptions options)
    {
        return Core.CreateAbstractProfile(options);
    }

    /// <summary>Creates a NamedColor profile from caller-supplied names and coordinates.</summary>
    /// <param name="options">The profile metadata and named-colour definition.</param>
    /// <returns>The canonical NamedColor profile.</returns>
    public IccProfile CreateNamedColorProfile(IccProfileBuildOptions options)
    {
        return Core.CreateNamedColorProfile(options);
    }

    /// <summary>Creates an Output profile from caller-supplied intent and characterization data.</summary>
    /// <param name="options">The profile metadata and Output profile definition.</param>
    /// <returns>The canonical Output profile.</returns>
    public IccProfile CreateOutputProfile(IccProfileBuildOptions options)
    {
        return Core.CreateOutputProfile(options);
    }

    /// <summary>Creates an ICC.2 colour-encoding or abbreviated profile.</summary>
    /// <param name="options">The profile metadata and colour-encoding definition.</param>
    /// <returns>The canonical ICC.2 profile.</returns>
    public IccProfile CreateColorEncodingProfile(IccProfileBuildOptions options)
    {
        return Core.CreateColorEncodingProfile(options);
    }

    /// <summary>Creates a canonical conforming profile from a header and tags.</summary>
    /// <param name="header">The source header.</param>
    /// <param name="tags">The ordered source tags.</param>
    /// <returns>The serialized, Profile-ID-updated, independently owned profile.</returns>
    public IccProfile CreateProfile(IccProfileHeader header, IccTag[] tags)
    {
        return Core.CreateProfile(header, tags);
    }

    /// <summary>Creates a deep independent profile copy.</summary>
    /// <param name="profile">The profile to copy.</param>
    /// <returns>The copied profile.</returns>
    public IccProfile CloneProfile(IccProfile profile)
    {
        return Core.CloneProfile(profile);
    }

    /// <summary>Replaces a profile header in an independently owned and validated profile.</summary>
    /// <param name="profile">The source profile.</param>
    /// <param name="header">The replacement header.</param>
    /// <returns>The updated profile.</returns>
    public IccProfile SetProfileHeader(IccProfile profile,
        IccProfileHeader header)
    {
        return Core.SetProfileHeader(profile, header);
    }

    /// <summary>Adds or replaces a tag in an independently owned profile.</summary>
    /// <param name="profile">The source profile.</param>
    /// <param name="tag">The tag to set.</param>
    /// <returns>The updated profile.</returns>
    public IccProfile SetProfileTag(IccProfile profile, IccTag tag)
    {
        return Core.SetProfileTag(profile, tag);
    }

    /// <summary>Removes all tags having a signature from an independently owned profile.</summary>
    /// <param name="profile">The source profile.</param>
    /// <param name="tagSignature">The signature to remove.</param>
    /// <returns>The updated profile.</returns>
    public IccProfile RemoveProfileTag(IccProfile profile, uint tagSignature)
    {
        return Core.RemoveProfileTag(profile, tagSignature);
    }
}
