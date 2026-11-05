using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="FragmentChooser"/>.
/// </summary>
public interface IFragmentChooser
{
    /// <summary>
    /// Selects the preferred parent fragment using size, composition, and deterministic tie breakers.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    Molecule Choose(Molecule? molecule);

    /// <summary>
    /// Selects the preferred parent fragment and returns its ranking details.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The selected fragment and its ranking details.</returns>
    FragmentChoice ChooseWithDetails(Molecule? molecule);
}
