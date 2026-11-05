using System;
using System.Security.Cryptography;
using System.Text;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="StructuralHash"/>.
/// </summary>
public interface IStructuralHash
{
    /// <summary>
    /// Compute.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    string Compute(Molecule? molecule);

    /// <summary>
    /// Computes a structural hash using the specified identity policy.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="policy">The identity or cleanup policy to apply.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    string ComputeWithPolicy(Molecule? molecule, IdentityPolicy policy);

    /// <summary>
    /// Computes a structural hash using independent identity participation options.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The identity options; <c>null</c> uses exact defaults.</param>
    /// <returns>The resulting identity hash.</returns>
    string ComputeWithOptions(Molecule? molecule, IdentityOptions? options);

    /// <summary>
    /// Computes the exact.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    string ComputeExact(Molecule? molecule);

    /// <summary>
    /// Builds the canonical text.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    string BuildCanonicalText(Molecule? molecule);

    /// <summary>
    /// Builds canonical text using the specified identity policy.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="policy">The identity or cleanup policy to apply.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    string BuildCanonicalTextWithPolicy(Molecule? molecule, IdentityPolicy policy);

    /// <summary>
    /// Builds canonical text using independent identity participation options.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The identity options; <c>null</c> uses exact defaults.</param>
    /// <returns>The canonical identity text.</returns>
    string BuildCanonicalTextWithOptions(Molecule? molecule, IdentityOptions? options);
}
