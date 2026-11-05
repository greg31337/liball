using System;
using System.Collections.Generic;
using System.Globalization;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="Mol2Reader"/>.
/// </summary>
public interface IMol2Reader
{

    /// <summary>
    /// Reads the molecule and returns detailed changes and diagnostics.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    Mol2ReadResult ReadWithResult(string? text);
}
