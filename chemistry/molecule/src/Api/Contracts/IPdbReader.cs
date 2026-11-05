using System;
using System.Collections.Generic;
using System.Globalization;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="PdbReader"/>.
/// </summary>
public interface IPdbReader
{

    /// <summary>
    /// Reads the molecule and returns detailed changes and diagnostics.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    PdbReadResult ReadWithResult(string? text);

    /// <summary>
    /// Extracts non-water hetero residues as independently mutable ligand molecules.
    /// </summary>
    /// <param name="molecule">The parsed PDB molecular graph.</param>
    /// <param name="options">Ligand selection options; <c>null</c> uses non-water defaults.</param>
    /// <returns>The extracted ligands, source residues, counts, and diagnostics.</returns>
    PdbLigandExtractionResult ExtractLigands(Molecule? molecule, PdbLigandOptions? options);

    /// <summary>
    /// Reads PDB text and extracts selected hetero residues as ligand molecules.
    /// </summary>
    /// <param name="text">The source PDB text.</param>
    /// <param name="options">Ligand selection options; <c>null</c> uses non-water defaults.</param>
    /// <returns>The extracted ligands, source residues, counts, and diagnostics.</returns>
    PdbLigandExtractionResult ReadLigands(string? text, PdbLigandOptions? options);
}
