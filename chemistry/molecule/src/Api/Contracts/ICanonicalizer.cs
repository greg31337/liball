namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="Canonicalizer"/>.
/// </summary>
public interface ICanonicalizer
{
    /// <summary>
    /// Returns the deterministic atom and bond order for a molecule.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The canonical atom and bond order.</returns>
    CanonicalOrder GetCanonicalOrder(Molecule? molecule);

    /// <summary>
    /// Returns the identity hash.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    string GetIdentityHash(Molecule? molecule);

    /// <summary>
    /// Returns the identity hash using the specified policy.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="policy">The identity or cleanup policy to apply.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    string GetIdentityHashWithPolicy(Molecule? molecule, IdentityPolicy policy);

    /// <summary>
    /// Returns the identity hash using independent participation options.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The identity options; <c>null</c> uses exact defaults.</param>
    /// <returns>The resulting identity hash.</returns>
    string GetIdentityHashWithOptions(Molecule? molecule, IdentityOptions? options);

    /// <summary>
    /// Determines whether the value has same identity.
    /// </summary>
    /// <param name="left">The left value to compare.</param>
    /// <param name="right">The right value to compare.</param>
    /// <param name="policy">The identity or cleanup policy to apply.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    bool HasSameIdentity(Molecule? left, Molecule? right, IdentityPolicy policy);

    /// <summary>
    /// Determines whether two molecules have the same identity under independent participation options.
    /// </summary>
    /// <param name="left">The left value to compare.</param>
    /// <param name="right">The right value to compare.</param>
    /// <param name="options">The identity options; <c>null</c> uses exact defaults.</param>
    /// <returns><c>true</c> when both identities are equal; otherwise, <c>false</c>.</returns>
    bool HasSameIdentityWithOptions(Molecule? left, Molecule? right, IdentityOptions? options);

    /// <summary>
    /// Returns identity hashes for normalized connected components in deterministic sorted order.
    /// </summary>
    /// <param name="molecule">The molecule or mixture to process.</param>
    /// <param name="options">The identity options; <c>null</c> uses exact defaults.</param>
    /// <returns>The component hashes, combined mixture hash, and diagnostics.</returns>
    MixtureIdentityResult GetComponentSortedIdentifiers(Molecule? molecule, IdentityOptions? options);
}
