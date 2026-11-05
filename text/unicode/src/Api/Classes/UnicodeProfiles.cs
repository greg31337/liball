namespace unicode;

/// <summary>
/// Creates the built-in Unicode identifier and PRECIS-style profiles.
/// </summary>
public class UnicodeProfiles : IUnicodeProfiles
{
    internal static readonly UnicodeProfiles Instance = new UnicodeProfiles();

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public UnicodeProfiles()
    {
    }

    /// <summary>
    /// Creates a configurable identifier-profile builder with default XID behavior.
    /// </summary>
    /// <param name="name">The nonempty human-readable profile name.</param>
    /// <returns>A new identifier-profile builder.</returns>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or consists only of white space.</exception>
    public UnicodeIdentifierProfileBuilder CreateIdentifierProfileBuilder(string name)
    {
        return new UnicodeIdentifierProfileBuilder(name);
    }

    /// <summary>
    /// Creates the default XID-based identifier profile without a normalization requirement.
    /// </summary>
    /// <returns>The configured identifier profile.</returns>
    public UnicodeIdentifierProfile CreateDefaultIdentifier()
    {
        return UnicodeIdentifierProfile.CreateDefault();
    }

    /// <summary>
    /// Creates an immutable UAX #31 profile that permits all scalars except its stable exclusion classes.
    /// </summary>
    /// <returns>The configured identifier profile.</returns>
    public UnicodeIdentifierProfile CreateImmutableIdentifier()
    {
        return UnicodeIdentifierProfile.CreateImmutable();
    }

    /// <summary>
    /// Creates an XID-based identifier profile that requires NFC input.
    /// </summary>
    /// <returns>The configured identifier profile.</returns>
    public UnicodeIdentifierProfile CreateNormalizedIdentifier()
    {
        return UnicodeIdentifierProfile.CreateNormalized();
    }

    /// <summary>
    /// Creates an XID-based identifier profile using NFKC case-fold comparison keys.
    /// </summary>
    /// <returns>The configured identifier profile.</returns>
    public UnicodeIdentifierProfile CreateCaseInsensitiveIdentifier()
    {
        return UnicodeIdentifierProfile.CreateCaseInsensitive();
    }

    /// <summary>
    /// Creates a username profile that lowercases and normalizes input.
    /// </summary>
    /// <returns>The configured PRECIS-style profile.</returns>
    public UnicodePrecisProfile CreateUsernameCaseMapped()
    {
        return UnicodePrecisProfile.CreateUsernameCaseMapped();
    }

    /// <summary>
    /// Creates a username profile that preserves case while normalizing input.
    /// </summary>
    /// <returns>The configured PRECIS-style profile.</returns>
    public UnicodePrecisProfile CreateUsernameCasePreserved()
    {
        return UnicodePrecisProfile.CreateUsernameCasePreserved();
    }

    /// <summary>
    /// Creates a nickname profile that maps, trims, and collapses spaces before normalization.
    /// </summary>
    /// <returns>The configured PRECIS-style profile.</returns>
    public UnicodePrecisProfile CreateNickname()
    {
        return UnicodePrecisProfile.CreateNickname();
    }

    /// <summary>
    /// Creates a case-preserving opaque-string profile that permits spaces.
    /// </summary>
    /// <returns>The configured PRECIS-style profile.</returns>
    public UnicodePrecisProfile CreateOpaqueString()
    {
        return UnicodePrecisProfile.CreateOpaqueString();
    }
}
