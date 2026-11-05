using System.Collections.Generic;
using System.Text;

namespace molecule;


/// <summary>
/// Computes deterministic structural identities under a selected identity policy.
/// </summary>
public class Canonicalizer : ICanonicalizer
{
    internal StructuralHash StructuralHasher;

    /// <summary>
    /// Initializes a new instance of the <see cref="Canonicalizer"/> class.
    /// </summary>
    public Canonicalizer()
    {
        StructuralHasher = new StructuralHash();
    }

    /// <summary>
    /// Returns the deterministic atom and bond order for a molecule.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The canonical atom and bond order.</returns>
    public CanonicalOrder GetCanonicalOrder(Molecule? molecule)
    {
        return CanonicalOrder.FromMolecule(molecule);
    }

    /// <summary>
    /// Returns the identity hash.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    public string GetIdentityHash(Molecule? molecule)
    {
        return StructuralHasher.Compute(molecule);
    }

    /// <summary>
    /// Returns the identity hash using the specified policy.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="policy">The identity or cleanup policy to apply.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    public string GetIdentityHashWithPolicy(Molecule? molecule, IdentityPolicy policy)
    {
        return StructuralHasher.ComputeWithPolicy(molecule, policy);
    }

    /// <summary>
    /// Returns the identity hash using independent participation options.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The identity options; <c>null</c> uses exact defaults.</param>
    /// <returns>The resulting identity hash.</returns>
    public string GetIdentityHashWithOptions(Molecule? molecule, IdentityOptions? options)
    {
        return StructuralHasher.ComputeWithOptions(molecule, options);
    }

    /// <summary>
    /// Determines whether the value has same identity.
    /// </summary>
    /// <param name="left">The left value to compare.</param>
    /// <param name="right">The right value to compare.</param>
    /// <param name="policy">The identity or cleanup policy to apply.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool HasSameIdentity(Molecule? left, Molecule? right, IdentityPolicy policy)
    {
        string leftHash = GetIdentityHashWithPolicy(left, policy);
        string rightHash = GetIdentityHashWithPolicy(right, policy);
        return leftHash == rightHash;
    }

    /// <summary>
    /// Determines whether two molecules have the same identity under independent participation options.
    /// </summary>
    /// <param name="left">The left value to compare.</param>
    /// <param name="right">The right value to compare.</param>
    /// <param name="options">The identity options; <c>null</c> uses exact defaults.</param>
    /// <returns><c>true</c> when both identities are equal; otherwise, <c>false</c>.</returns>
    public bool HasSameIdentityWithOptions(Molecule? left, Molecule? right, IdentityOptions? options)
    {
        string leftHash = GetIdentityHashWithOptions(left, options);
        string rightHash = GetIdentityHashWithOptions(right, options);
        return leftHash == rightHash;
    }

    /// <summary>
    /// Returns identity hashes for normalized connected components in deterministic sorted order.
    /// </summary>
    /// <param name="molecule">The molecule or mixture to process.</param>
    /// <param name="options">The identity options; <c>null</c> uses exact defaults.</param>
    /// <returns>The component hashes, combined mixture hash, and diagnostics.</returns>
    public MixtureIdentityResult GetComponentSortedIdentifiers(Molecule? molecule, IdentityOptions? options)
    {
        MixtureIdentityResult result = new MixtureIdentityResult();
        IdentityOptions safeOptions = StructuralHasher.NormalizeStructuralIdentityOptions(options);
        result.Policy = safeOptions.Policy;
        if (molecule == null)
        {
            result.Report.AddError("IDENTITY_NULL_MOLECULE", "Cannot build a component-sorted identity for a null molecule.");
            return result;
        }

        Molecule? normalized = StructuralHasher.NormalizeMoleculeWithIdentityOptions(molecule, safeOptions);
        if (normalized == null)
        {
            result.Report.AddError("IDENTITY_NORMALIZATION_FAILED", "Identity normalization did not produce a molecule.");
            return result;
        }

        Components components = new Components();
        List<Molecule> fragments = components.Split(normalized);
        int index = 0;
        while (index < fragments.Count)
        {
            result.ComponentIdentifiers.Add(StructuralHasher.ComputeWithOptions(fragments[index], safeOptions));
            index = index + 1;
        }

        SortComponentIdentityStrings(result.ComponentIdentifiers);
        result.ComponentCount = result.ComponentIdentifiers.Count;
        StringBuilder builder = new StringBuilder();
        builder.Append("molecule-mixture-identity-v1|");
        builder.Append(result.ComponentCount);
        builder.Append("|");
        index = 0;
        while (index < result.ComponentIdentifiers.Count)
        {
            string identifier = result.ComponentIdentifiers[index];
            builder.Append(identifier.Length);
            builder.Append(":");
            builder.Append(identifier);
            builder.Append("|");
            index = index + 1;
        }

        result.Identifier = StructuralHasher.ComputeHash(builder.ToString());
        result.Success = true;
        return result;
    }

    internal void SortComponentIdentityStrings(List<string> identifiers)
    {
        int index = 1;
        while (index < identifiers.Count)
        {
            string value = identifiers[index];
            int scan = index - 1;
            while (scan >= 0 && string.CompareOrdinal(identifiers[scan], value) > 0)
            {
                identifiers[scan + 1] = identifiers[scan];
                scan = scan - 1;
            }

            identifiers[scan + 1] = value;
            index = index + 1;
        }
    }
}
