namespace unicode;

/// <summary>
/// Creates the built-in Unicode identifier and PRECIS-style profiles.
/// </summary>
public interface IUnicodeProfiles
{
    /// <summary>
    /// Creates a configurable identifier-profile builder with default XID behavior.
    /// </summary>
    /// <param name="name">The nonempty human-readable profile name.</param>
    /// <returns>A new identifier-profile builder.</returns>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or consists only of white space.</exception>
    UnicodeIdentifierProfileBuilder CreateIdentifierProfileBuilder(string name);

    /// <summary>
    /// Creates the default XID-based identifier profile without a normalization requirement.
    /// </summary>
    /// <returns>The configured identifier profile.</returns>
    UnicodeIdentifierProfile CreateDefaultIdentifier();

    /// <summary>
    /// Creates an ID-based identifier profile that permits unassigned code points.
    /// </summary>
    /// <returns>The configured identifier profile.</returns>
    UnicodeIdentifierProfile CreateImmutableIdentifier();

    /// <summary>
    /// Creates an XID-based identifier profile that requires NFC input.
    /// </summary>
    /// <returns>The configured identifier profile.</returns>
    UnicodeIdentifierProfile CreateNormalizedIdentifier();

    /// <summary>
    /// Creates an XID-based identifier profile using NFKC case-fold comparison keys.
    /// </summary>
    /// <returns>The configured identifier profile.</returns>
    UnicodeIdentifierProfile CreateCaseInsensitiveIdentifier();

    /// <summary>
    /// Creates a username profile that lowercases and normalizes input.
    /// </summary>
    /// <returns>The configured PRECIS-style profile.</returns>
    UnicodePrecisProfile CreateUsernameCaseMapped();

    /// <summary>
    /// Creates a username profile that preserves case while normalizing input.
    /// </summary>
    /// <returns>The configured PRECIS-style profile.</returns>
    UnicodePrecisProfile CreateUsernameCasePreserved();

    /// <summary>
    /// Creates a nickname profile that maps, trims, and collapses spaces before normalization.
    /// </summary>
    /// <returns>The configured PRECIS-style profile.</returns>
    UnicodePrecisProfile CreateNickname();

    /// <summary>
    /// Creates a case-preserving opaque-string profile that permits spaces.
    /// </summary>
    /// <returns>The configured PRECIS-style profile.</returns>
    UnicodePrecisProfile CreateOpaqueString();
}
