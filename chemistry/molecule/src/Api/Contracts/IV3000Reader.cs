using System;
using System.Collections.Generic;
using System.Globalization;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="V3000Reader"/>.
/// </summary>
public interface IV3000Reader
{
    /// <summary>
    /// Reads one molecule from the supplied format text.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    Molecule Read(string? text);

    /// <summary>
    /// Reads the molecule and returns detailed changes and diagnostics.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    MolfileReadResult ReadWithResult(string? text);

    /// <summary>
    /// Reads MOL V3000 using explicit feature and count policies.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <param name="options">The molfile read options; <c>null</c> uses feature-preserving defaults.</param>
    /// <returns>The molecule, feature counts, and diagnostics.</returns>
    MolfileReadResult ReadV3000WithOptions(string? text, MolfileReadOptions? options);
}
