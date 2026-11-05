using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="HybridizationModel"/>.
/// </summary>
public interface IHybridizationModel
{
    /// <summary>
    /// Assign.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting hybridization assignment.</returns>
    HybridizationAssignment Assign(Molecule? molecule);
}

