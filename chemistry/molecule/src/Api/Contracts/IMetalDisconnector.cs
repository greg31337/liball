using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="MetalDisconnector"/>.
/// </summary>
public interface IMetalDisconnector
{
    /// <summary>
    /// Returns a copy with supported bonds between metals and organic atoms removed.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    Molecule Disconnect(Molecule? molecule);

    /// <summary>
    /// Disconnects the molecule and returns detailed changes and diagnostics.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    MetalDisconnectionResult DisconnectWithResult(Molecule? molecule);
}

