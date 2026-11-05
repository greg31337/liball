namespace molecule;


/// <summary>
/// Writes molecular objects as molfile data.
/// </summary>
public class MolfileWriter : IMoleculeWriter, IMolfileWriter
{
    internal V2000Writer V2000;
    internal V3000Writer V3000;

    /// <summary>
    /// Initializes a new instance of the <see cref="MolfileWriter"/> class.
    /// </summary>
    public MolfileWriter()
    {
        V2000 = new V2000Writer();
        V3000 = new V3000Writer();
    }

    /// <summary>
    /// Writes one molecule to the configured format or output stream.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    public string Write(Molecule? molecule)
    {
        return V2000.Write(molecule);
    }

    /// <summary>
    /// Writes the v 3000.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    public string WriteV3000(Molecule? molecule)
    {
        return V3000.Write(molecule);
    }

    /// <summary>
    /// Writes a molfile using automatic or explicitly selected version and feature policies.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The molfile write options; <c>null</c> uses automatic feature-preserving defaults.</param>
    /// <returns>The output text, selected version, and diagnostics.</returns>
    public MolfileWriteResult WriteMolfileWithOptions(Molecule? molecule, MolfileWriteOptions? options)
    {
        MolfileWriteOptions safeOptions = NormalizeWriteOptions(options);
        MolfileVersion version = safeOptions.Version;
        if (version == MolfileVersion.Auto)
        {
            version = SelectVersion(molecule, safeOptions);
        }

        if (version == MolfileVersion.V3000)
        {
            return V3000.WriteV3000WithOptions(molecule, safeOptions);
        }

        return V2000.WriteV2000WithOptions(molecule, safeOptions);
    }

    internal MolfileVersion SelectVersion(Molecule? molecule, MolfileWriteOptions options)
    {
        if (molecule != null)
        {
            if (molecule.Atoms.Count > 999 || molecule.Bonds.Count > 999 || molecule.Atoms.Count > options.V2000AtomLimit || molecule.Bonds.Count > options.V2000BondLimit)
            {
                return MolfileVersion.V3000;
            }

            if (options.WriteEnhancedStereo && molecule.StereoGroups.Groups.Count > 0)
            {
                return MolfileVersion.V3000;
            }
        }

        return MolfileVersion.V2000;
    }

    internal MolfileWriteOptions NormalizeWriteOptions(MolfileWriteOptions? options)
    {
        if (options == null)
        {
            return new MolfileWriteOptions();
        }

        return options;
    }
}
