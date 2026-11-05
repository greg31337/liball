namespace icc;

/// <summary>
/// Creates and safely mutates basic ICC profile models.
/// </summary>
public interface IIccProfileBuilder
{
    /// <summary>
    /// Creates a writable generation-correct profile skeleton containing shared required metadata.
    /// </summary>
    /// <param name="options">The profile header and metadata definition.</param>
    /// <returns>A canonical independently owned profile skeleton.</returns>
    /// <exception cref="IccProfileValidationException">The supplied definition is incomplete or invalid.</exception>
    /// <exception cref="IccProfileWriteException">The resulting skeleton cannot pass serialization preflight.</exception>
    IccProfile CreateProfileSkeleton(IccProfileBuildOptions options);

    /// <summary>
    /// Creates a complete RGB matrix/TRC Input, Display, or ColorSpace profile.
    /// </summary>
    /// <param name="options">The profile metadata and RGB colourimetric definition.</param>
    /// <returns>A canonical independently owned RGB profile.</returns>
    /// <exception cref="IccProfileValidationException">The RGB definition is incomplete, inconsistent, or invalid.</exception>
    /// <exception cref="IccProfileWriteException">The resulting profile cannot pass serialization preflight.</exception>
    IccProfile CreateRgbProfile(IccProfileBuildOptions options);

    /// <summary>
    /// Creates a complete monochrome Input, Display, or Output profile.
    /// </summary>
    /// <param name="options">The profile metadata and gray colourimetric definition.</param>
    /// <returns>A canonical independently owned gray profile.</returns>
    /// <exception cref="IccProfileValidationException">The gray definition is incomplete, inconsistent, or invalid.</exception>
    /// <exception cref="IccProfileWriteException">The resulting profile cannot pass serialization preflight.</exception>
    IccProfile CreateGrayProfile(IccProfileBuildOptions options);

    /// <summary>
    /// Creates an XYZ-to-PCSXYZ identity Input, Display, ColorSpace, or Abstract profile.
    /// </summary>
    /// <param name="options">The profile metadata and identity-profile class selection.</param>
    /// <returns>A canonical independently owned XYZ identity profile.</returns>
    /// <exception cref="IccProfileValidationException">The identity-profile definition is inconsistent or invalid.</exception>
    /// <exception cref="IccProfileWriteException">The resulting profile cannot pass serialization preflight.</exception>
    IccProfile CreateXyzIdentityProfile(IccProfileBuildOptions options);

    /// <summary>
    /// Creates a PCS-to-PCS abstract profile with a required LUT fallback and optional version-4 MPE.
    /// </summary>
    /// <param name="options">The profile metadata and abstract transform definition.</param>
    /// <returns>A canonical independently owned abstract profile.</returns>
    /// <exception cref="IccProfileValidationException">The abstract definition is incomplete, inconsistent, or invalid.</exception>
    /// <exception cref="IccProfileWriteException">The resulting profile cannot pass serialization preflight.</exception>
    /// <exception cref="IccTransformException">A supplied transform is not executable.</exception>
    IccProfile CreateAbstractProfile(IccProfileBuildOptions options);

    /// <summary>
    /// Creates a NamedColor profile with PCS values and optional device coordinates, legacy tints, and colorants.
    /// </summary>
    /// <param name="options">The profile metadata and named-colour definition.</param>
    /// <returns>A canonical independently owned NamedColor profile.</returns>
    /// <exception cref="IccProfileValidationException">The named-colour definition is incomplete or inconsistent.</exception>
    /// <exception cref="IccProfileWriteException">A name or coordinate cannot be encoded, or serialization preflight fails.</exception>
    IccProfile CreateNamedColorProfile(IccProfileBuildOptions options);

    /// <summary>
    /// Creates an Output profile from intent, gamut, colorant, measurement, and viewing-condition data.
    /// </summary>
    /// <param name="options">The profile metadata and Output profile definition.</param>
    /// <returns>A canonical independently owned Output profile.</returns>
    /// <exception cref="IccProfileValidationException">The Output definition is incomplete or inconsistent.</exception>
    /// <exception cref="IccProfileWriteException">A supplied tag cannot be encoded, or serialization preflight fails.</exception>
    /// <exception cref="IccTransformException">A supplied transform is not executable.</exception>
    IccProfile CreateOutputProfile(IccProfileBuildOptions options);

    /// <summary>
    /// Creates an ICC.2 colour-encoding or abbreviated ColorEncodingSpace profile.
    /// </summary>
    /// <param name="options">The profile metadata and colour-encoding definition.</param>
    /// <returns>A canonical independently owned ICC.2 profile.</returns>
    /// <exception cref="IccProfileValidationException">The colour-encoding definition is incomplete or inconsistent.</exception>
    /// <exception cref="IccProfileWriteException">A supplied tag cannot be encoded, or serialization preflight fails.</exception>
    /// <exception cref="IccTransformException">A supplied fallback transform is not executable.</exception>
    IccProfile CreateColorEncodingProfile(IccProfileBuildOptions options);

    /// <summary>
    /// Creates a canonical conforming profile from a header and a set of tags.
    /// </summary>
    /// <param name="header">The profile header definition.</param>
    /// <param name="tags">The profile tags.</param>
    /// <returns>A serialized, Profile-ID-updated, newly owned profile model.</returns>
    /// <exception cref="IccProfileValidationException">The supplied definition is incomplete or invalid.</exception>
    IccProfile CreateProfile(IccProfileHeader header, IccTag[] tags);

    /// <summary>
    /// Creates a deep independent copy of a profile model.
    /// </summary>
    /// <param name="profile">The profile to copy.</param>
    /// <returns>The independent profile copy.</returns>
    IccProfile CloneProfile(IccProfile profile);

    /// <summary>
    /// Replaces the header in a copied profile and refuses an invalid result.
    /// </summary>
    /// <param name="profile">The source profile.</param>
    /// <param name="header">The replacement header.</param>
    /// <returns>A validated profile containing the replacement header.</returns>
    /// <exception cref="IccProfileValidationException">The replacement would produce an invalid profile.</exception>
    /// <exception cref="IccProfileWriteException">The replacement cannot be serialized safely.</exception>
    IccProfile SetProfileHeader(IccProfile profile, IccProfileHeader header);

    /// <summary>
    /// Adds or replaces one tag in a copied profile model.
    /// </summary>
    /// <param name="profile">The source profile.</param>
    /// <param name="tag">The tag to add or replace.</param>
    /// <returns>A validated profile containing the tag.</returns>
    /// <exception cref="IccProfileValidationException">The mutation would produce an invalid profile.</exception>
    /// <exception cref="IccProfileWriteException">The mutation cannot be serialized safely.</exception>
    IccProfile SetProfileTag(IccProfile profile, IccTag tag);

    /// <summary>
    /// Removes one tag from a copied profile model.
    /// </summary>
    /// <param name="profile">The source profile.</param>
    /// <param name="tagSignature">The signature of the tag to remove.</param>
    /// <returns>A validated profile without the tag.</returns>
    /// <exception cref="IccProfileValidationException">The mutation would produce an invalid profile.</exception>
    /// <exception cref="IccProfileWriteException">The mutation cannot be serialized safely.</exception>
    IccProfile RemoveProfileTag(IccProfile profile, uint tagSignature);
}
