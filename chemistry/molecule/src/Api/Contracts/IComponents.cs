using System.Collections.Generic;
using System.Text;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="Components"/>.
/// </summary>
public interface IComponents
{
    /// <summary>
    /// Finds the components.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting values in deterministic order.</returns>
    List<ComponentInfo> FindComponents(Molecule? molecule);

    /// <summary>
    /// Returns the component identifiers.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting values in deterministic order.</returns>
    List<int> GetComponentIds(Molecule? molecule);

    /// <summary>
    /// Split.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting values in deterministic order.</returns>
    List<Molecule> Split(Molecule? molecule);
}

