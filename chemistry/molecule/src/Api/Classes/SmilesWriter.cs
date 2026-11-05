using System.Collections.Generic;
using System.Text;

namespace molecule;


/// <summary>
/// Writes molecular graphs as canonical or traversal-order SMILES text.
/// </summary>
public class SmilesWriter : IMoleculeWriter, ISmilesWriter
{
    internal CanonicalSmilesWriter CanonicalWriter;

    /// <summary>
    /// Initializes a new instance of the <see cref="SmilesWriter"/> class.
    /// </summary>
    public SmilesWriter()
    {
        CanonicalWriter = new CanonicalSmilesWriter();
    }

    /// <summary>
    /// Writes one molecule to the configured format or output stream.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    public string Write(Molecule? molecule)
    {
        return WriteWithOptions(molecule, false);
    }

    /// <summary>
    /// Writes canonical SMILES including supported atom and bond stereochemistry.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    public string WriteIsomeric(Molecule? molecule)
    {
        return WriteWithOptions(molecule, true);
    }

    /// <summary>
    /// Writes SMILES in deterministic traversal order without canonical atom ranking.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    public string WriteNoncanonical(Molecule? molecule)
    {
        return Write(molecule);
    }

    /// <summary>
    /// Writes traversal-order SMILES including supported stereochemistry.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    public string WriteNoncanonicalIsomeric(Molecule? molecule)
    {
        return WriteIsomeric(molecule);
    }

    /// <summary>
    /// Writes SMILES with explicit control over whether stereochemistry is included.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="includeStereo">The include stereo.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    public string WriteWithOptions(Molecule? molecule, bool includeStereo)
    {
        return WriteTraversalConfigured(molecule, includeStereo, true, false);
    }

    /// <summary>
    /// Writes canonical SMILES using canonical atom and component ordering.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting canonical SMILES text.</returns>
    public string WriteCanonicalSmiles(Molecule? molecule)
    {
        SmilesWriteOptions options = new SmilesWriteOptions();
        options.Canonical = true;
        return WriteSmilesWithOptions(molecule, options);
    }

    /// <summary>
    /// Writes noncanonical SMILES using molecule traversal order.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting traversal-order SMILES text.</returns>
    public string WriteNoncanonicalSmiles(Molecule? molecule)
    {
        SmilesWriteOptions options = new SmilesWriteOptions();
        options.Canonical = false;
        return WriteSmilesWithOptions(molecule, options);
    }

    /// <summary>
    /// Writes canonical aromatic SMILES after copy-based aromaticity perception.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting aromatic SMILES text.</returns>
    public string WriteAromaticSmiles(Molecule? molecule)
    {
        SmilesWriteOptions options = new SmilesWriteOptions();
        options.Canonical = true;
        options.Aromatic = true;
        return WriteSmilesWithOptions(molecule, options);
    }

    /// <summary>
    /// Writes canonical Kekulé SMILES after copy-based Kekulé bond assignment.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting Kekulé SMILES text.</returns>
    public string WriteKekuleSmiles(Molecule? molecule)
    {
        SmilesWriteOptions options = new SmilesWriteOptions();
        options.Canonical = true;
        options.Kekule = true;
        return WriteSmilesWithOptions(molecule, options);
    }

    /// <summary>
    /// Writes canonical isomeric SMILES including supported stereochemistry.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting isomeric SMILES text.</returns>
    public string WriteIsomericSmiles(Molecule? molecule)
    {
        SmilesWriteOptions options = new SmilesWriteOptions();
        options.Canonical = true;
        options.Isomeric = true;
        return WriteSmilesWithOptions(molecule, options);
    }

    /// <summary>
    /// Writes canonical SMILES including assigned atom-map numbers.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting atom-mapped SMILES text.</returns>
    public string WriteAtomMappedSmiles(Molecule? molecule)
    {
        SmilesWriteOptions options = new SmilesWriteOptions();
        options.Canonical = true;
        options.IncludeAtomMaps = true;
        return WriteSmilesWithOptions(molecule, options);
    }

    /// <summary>
    /// Writes canonical SMILES with bracket hydrogen annotations for atoms carrying hydrogen counts.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting explicit-hydrogen SMILES text.</returns>
    public string WriteExplicitHydrogenSmiles(Molecule? molecule)
    {
        SmilesWriteOptions options = new SmilesWriteOptions();
        options.Canonical = true;
        options.ExplicitHydrogens = true;
        return WriteSmilesWithOptions(molecule, options);
    }

    /// <summary>
    /// Writes SMILES using independent traversal and representation policies.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The SMILES writing options; <c>null</c> preserves traversal order and representation.</param>
    /// <returns>The resulting configured SMILES text.</returns>
    public string WriteSmilesWithOptions(Molecule? molecule, SmilesWriteOptions? options)
    {
        if (molecule == null || molecule.Atoms.Count == 0)
        {
            return "";
        }

        SmilesWriteOptions safeOptions = NormalizeSmilesWriteOptions(options);
        Molecule working = PrepareMoleculeForWriting(molecule, safeOptions);
        if (safeOptions.Canonical)
        {
            return CanonicalWriter.WriteConfigured(working, safeOptions.Isomeric, safeOptions.IncludeAtomMaps, safeOptions.ExplicitHydrogens);
        }

        return WriteTraversalConfigured(working, safeOptions.Isomeric, safeOptions.IncludeAtomMaps, safeOptions.ExplicitHydrogens);
    }

    internal string WriteTraversalConfigured(Molecule? molecule, bool includeStereo, bool includeAtomMaps, bool explicitHydrogens)
    {
        if (molecule == null || molecule.Atoms.Count == 0)
        {
            return "";
        }

        bool[] treeBonds = BuildTreeBondSet(molecule);
        bool[] visitedAtoms = new bool[molecule.Atoms.Count];
        int[] ringNumbersByBondId = new int[molecule.Bonds.Count];
        SmilesRingCounter counter = new SmilesRingCounter();
        StringBuilder builder = new StringBuilder();

        int atomId = 0;
        bool wroteComponent = false;
        while (atomId < molecule.Atoms.Count)
        {
            if (!visitedAtoms[atomId])
            {
                if (wroteComponent)
                {
                    builder.Append(".");
                }

                AppendAtomTree(builder, molecule, atomId, -1, visitedAtoms, treeBonds, ringNumbersByBondId, counter, includeStereo, includeAtomMaps, explicitHydrogens);
                wroteComponent = true;
            }

            atomId = atomId + 1;
        }

        return builder.ToString();
    }

    internal SmilesWriteOptions NormalizeSmilesWriteOptions(SmilesWriteOptions? options)
    {
        SmilesWriteOptions safeOptions = new SmilesWriteOptions();
        if (options != null)
        {
            safeOptions.Canonical = options.Canonical;
            safeOptions.Aromatic = options.Aromatic;
            safeOptions.Kekule = options.Kekule;
            safeOptions.Isomeric = options.Isomeric;
            safeOptions.IncludeAtomMaps = options.IncludeAtomMaps;
            safeOptions.ExplicitHydrogens = options.ExplicitHydrogens;
        }

        return safeOptions;
    }

    internal Molecule PrepareMoleculeForWriting(Molecule molecule, SmilesWriteOptions options)
    {
        Molecule working = MoleculeBuilder.CloneMolecule(molecule);
        if (options.Aromatic)
        {
            AromaticityPerceptionResult aromatic = new AromaticityModel().Dekekulize(working, null);
            working = aromatic.Molecule;
        }

        if (options.Kekule)
        {
            KekulizationResult kekule = new Kekulizer().Kekulize(working);
            working = kekule.Molecule;
        }

        if (!options.IncludeAtomMaps)
        {
            int atomIndex = 0;
            while (atomIndex < working.Atoms.Count)
            {
                working.Atoms[atomIndex].AtomMapNumber = 0;
                atomIndex = atomIndex + 1;
            }
        }

        return working;
    }

    internal bool[] BuildTreeBondSet(Molecule molecule)
    {
        bool[] treeBonds = new bool[molecule.Bonds.Count];
        bool[] visitedAtoms = new bool[molecule.Atoms.Count];

        int atomId = 0;
        while (atomId < molecule.Atoms.Count)
        {
            if (!visitedAtoms[atomId])
            {
                MarkTreeBonds(molecule, atomId, visitedAtoms, treeBonds);
            }

            atomId = atomId + 1;
        }

        return treeBonds;
    }

    internal void MarkTreeBonds(Molecule molecule, int atomId, bool[] visitedAtoms, bool[] treeBonds)
    {
        visitedAtoms[atomId] = true;
        List<int> neighbors = molecule.GetNeighborAtomIds(atomId);
        int index = 0;
        while (index < neighbors.Count)
        {
            int neighborAtomId = neighbors[index];
            if (!visitedAtoms[neighborAtomId])
            {
                int bondId = molecule.GetBondIdBetween(atomId, neighborAtomId);
                if (bondId >= 0 && bondId < treeBonds.Length)
                {
                    treeBonds[bondId] = true;
                }

                MarkTreeBonds(molecule, neighborAtomId, visitedAtoms, treeBonds);
            }

            index = index + 1;
        }
    }

    internal void AppendAtomTree(StringBuilder builder, Molecule molecule, int atomId, int parentAtomId, bool[] visitedAtoms, bool[] treeBonds, int[] ringNumbersByBondId, SmilesRingCounter counter, bool includeStereo, bool includeAtomMaps, bool explicitHydrogens)
    {
        visitedAtoms[atomId] = true;
        builder.Append(CanonicalWriter.GetConfiguredAtomTokenForMolecule(molecule, atomId, includeStereo, includeAtomMaps, explicitHydrogens));
        AppendRingClosures(builder, molecule, atomId, treeBonds, ringNumbersByBondId, counter, includeStereo);

        List<int> children = GetTreeChildren(molecule, atomId, parentAtomId, visitedAtoms, treeBonds);
        int childIndex = 0;
        while (childIndex < children.Count)
        {
            int childAtomId = children[childIndex];
            int bondId = molecule.GetBondIdBetween(atomId, childAtomId);
            Bond bond = molecule.Bonds[bondId];
            if (childIndex < children.Count - 1)
            {
                builder.Append("(");
                builder.Append(CanonicalWriter.GetBondSymbolWithStereo(bond, includeStereo));
                AppendAtomTree(builder, molecule, childAtomId, atomId, visitedAtoms, treeBonds, ringNumbersByBondId, counter, includeStereo, includeAtomMaps, explicitHydrogens);
                builder.Append(")");
            }
            else
            {
                builder.Append(CanonicalWriter.GetBondSymbolWithStereo(bond, includeStereo));
                AppendAtomTree(builder, molecule, childAtomId, atomId, visitedAtoms, treeBonds, ringNumbersByBondId, counter, includeStereo, includeAtomMaps, explicitHydrogens);
            }

            childIndex = childIndex + 1;
        }
    }

    internal void AppendRingClosures(StringBuilder builder, Molecule molecule, int atomId, bool[] treeBonds, int[] ringNumbersByBondId, SmilesRingCounter counter, bool includeStereo)
    {
        List<int> neighbors = molecule.GetNeighborAtomIds(atomId);
        int index = 0;
        while (index < neighbors.Count)
        {
            int neighborAtomId = neighbors[index];
            int bondId = molecule.GetBondIdBetween(atomId, neighborAtomId);
            if (bondId >= 0 && bondId < treeBonds.Length && !treeBonds[bondId])
            {
                if (ringNumbersByBondId[bondId] == 0)
                {
                    ringNumbersByBondId[bondId] = counter.NextRingNumber;
                    counter.NextRingNumber = counter.NextRingNumber + 1;
                    builder.Append(CanonicalWriter.GetBondSymbolWithStereo(molecule.Bonds[bondId], includeStereo));
                    CanonicalWriter.AppendRingNumber(builder, ringNumbersByBondId[bondId]);
                }
                else
                {
                    CanonicalWriter.AppendRingNumber(builder, ringNumbersByBondId[bondId]);
                }
            }

            index = index + 1;
        }
    }

    internal List<int> GetTreeChildren(Molecule molecule, int atomId, int parentAtomId, bool[] visitedAtoms, bool[] treeBonds)
    {
        List<int> children = new List<int>();
        List<int> neighbors = molecule.GetNeighborAtomIds(atomId);
        int index = 0;
        while (index < neighbors.Count)
        {
            int neighborAtomId = neighbors[index];
            int bondId = molecule.GetBondIdBetween(atomId, neighborAtomId);
            if (neighborAtomId != parentAtomId && !visitedAtoms[neighborAtomId] && bondId >= 0 && treeBonds[bondId])
            {
                children.Add(neighborAtomId);
            }

            index = index + 1;
        }

        return children;
    }
}
