using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="BasicDescriptors"/>.
/// </summary>
public interface IBasicDescriptors
{
    /// <summary>
    /// Calculates the values represented by this calculator for the supplied molecule.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting descriptor set.</returns>
    DescriptorSet Calculate(Molecule? molecule);
}

