using System.Collections.Generic;
using System.Text;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="SmilesWriter"/>.
/// </summary>
public interface ISmilesWriter
{

    /// <summary>
    /// Writes canonical SMILES including supported atom and bond stereochemistry.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    string WriteIsomeric(Molecule? molecule);

    /// <summary>
    /// Writes SMILES in deterministic traversal order without canonical atom ranking.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    string WriteNoncanonical(Molecule? molecule);

    /// <summary>
    /// Writes traversal-order SMILES including supported stereochemistry.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    string WriteNoncanonicalIsomeric(Molecule? molecule);

    /// <summary>
    /// Writes SMILES with explicit control over whether stereochemistry is included.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="includeStereo">The include stereo.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    string WriteWithOptions(Molecule? molecule, bool includeStereo);

    /// <summary>
    /// Writes canonical SMILES using canonical atom and component ordering.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting canonical SMILES text.</returns>
    string WriteCanonicalSmiles(Molecule? molecule);

    /// <summary>
    /// Writes noncanonical SMILES using molecule traversal order.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting traversal-order SMILES text.</returns>
    string WriteNoncanonicalSmiles(Molecule? molecule);

    /// <summary>
    /// Writes canonical aromatic SMILES after copy-based aromaticity perception.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting aromatic SMILES text.</returns>
    string WriteAromaticSmiles(Molecule? molecule);

    /// <summary>
    /// Writes canonical Kekulé SMILES after copy-based Kekulé bond assignment.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting Kekulé SMILES text.</returns>
    string WriteKekuleSmiles(Molecule? molecule);

    /// <summary>
    /// Writes canonical isomeric SMILES including supported stereochemistry.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting isomeric SMILES text.</returns>
    string WriteIsomericSmiles(Molecule? molecule);

    /// <summary>
    /// Writes canonical SMILES including assigned atom-map numbers.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting atom-mapped SMILES text.</returns>
    string WriteAtomMappedSmiles(Molecule? molecule);

    /// <summary>
    /// Writes canonical SMILES with bracket hydrogen annotations for atoms carrying hydrogen counts.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting explicit-hydrogen SMILES text.</returns>
    string WriteExplicitHydrogenSmiles(Molecule? molecule);

    /// <summary>
    /// Writes SMILES using independent traversal and representation policies.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The SMILES writing options; <c>null</c> preserves traversal order and representation.</param>
    /// <returns>The resulting configured SMILES text.</returns>
    string WriteSmilesWithOptions(Molecule? molecule, SmilesWriteOptions? options);
}
