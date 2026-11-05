using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="StereoCleaner"/>.
/// </summary>
public interface IStereoCleaner
{
    /// <summary>
    /// Removes invalid stereo records and rebuilds enhanced stereo groups.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    StereoCleanupResult Clean(Molecule? molecule);
}

