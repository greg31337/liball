using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="Layout2D"/>.
/// </summary>
public interface ILayout2D
{
    /// <summary>
    /// Assigns deterministic two-dimensional coordinates to the molecule's atoms.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    void Layout(Molecule? molecule);

    /// <summary>
    /// Assigns the stereo depiction.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting count or integer value.</returns>
    int AssignStereoDepiction(Molecule? molecule);
}

