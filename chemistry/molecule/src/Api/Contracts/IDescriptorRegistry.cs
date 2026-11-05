using System.Collections.Generic;
using System.Text;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="DescriptorRegistry"/>.
/// </summary>
public interface IDescriptorRegistry
{
    /// <summary>
    /// Calculates every descriptor registered by the library.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting descriptor set.</returns>
    DescriptorSet CalculateAll(Molecule? molecule);

    /// <summary>
    /// Calculates the values represented by this calculator for the supplied molecule.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="name">The name.</param>
    /// <returns>The resulting descriptor value.</returns>
    DescriptorValue Calculate(Molecule? molecule, string? name);

    /// <summary>
    /// Calculates the number.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="name">The name.</param>
    /// <returns>The calculated numeric value.</returns>
    double CalculateNumber(Molecule? molecule, string? name);

    /// <summary>
    /// Calculates the text.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="name">The name.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    string CalculateText(Molecule? molecule, string? name);

    /// <summary>
    /// Determines whether the requested registered name or structural query is present.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    bool Contains(string? name);

    /// <summary>
    /// Returns the names.
    /// </summary>
    /// <returns>The resulting values in deterministic order.</returns>
    List<string> GetNames();
}

