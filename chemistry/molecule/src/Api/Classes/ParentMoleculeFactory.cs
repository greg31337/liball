namespace molecule;


/// <summary>
/// Provides parent molecule factory data and operations for molecular graphs.
/// </summary>
public class ParentMoleculeFactory : IParentMoleculeFactory
{
    internal SaltRemover SaltRemover;
    internal TautomerCanonicalizer TautomerCanonicalizer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ParentMoleculeFactory"/> class.
    /// </summary>
    public ParentMoleculeFactory()
    {
        SaltRemover = new SaltRemover();
        TautomerCanonicalizer = new TautomerCanonicalizer();
    }

    /// <summary>
    /// Creates the stereo parent.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    public Molecule CreateStereoParent(Molecule? molecule)
    {
        Molecule parent = MoleculeBuilder.CloneMolecule(molecule);
        StripStereoInPlace(parent);
        return parent;
    }

    /// <summary>
    /// Creates the stereo parent using the specified options.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    public Molecule CreateStereoParentWithOptions(Molecule? molecule, StandardizationOptions? options)
    {
        return CreateStereoParent(molecule);
    }

    /// <summary>
    /// Creates the fragment parent.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    public Molecule CreateFragmentParent(Molecule? molecule)
    {
        return SaltRemover.RemoveSalts(molecule);
    }

    /// <summary>
    /// Creates the fragment parent using the specified options.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    public Molecule CreateFragmentParentWithOptions(Molecule? molecule, StandardizationOptions? options)
    {
        return CreateFragmentParent(molecule);
    }

    /// <summary>
    /// Creates the charge parent.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    public Molecule CreateChargeParent(Molecule? molecule)
    {
        Molecule parent = MoleculeBuilder.CloneMolecule(molecule);
        ClearChargesInPlace(parent);
        return parent;
    }

    /// <summary>
    /// Creates the charge parent using the specified options.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    public Molecule CreateChargeParentWithOptions(Molecule? molecule, StandardizationOptions? options)
    {
        return CreateChargeParent(molecule);
    }

    /// <summary>
    /// Creates an isotope parent by removing isotope mass-number distinctions.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>An independently owned isotope parent.</returns>
    public Molecule CreateIsotopeParent(Molecule? molecule)
    {
        Molecule parent = MoleculeBuilder.CloneMolecule(molecule);
        ClearIsotopesInPlace(parent);
        return parent;
    }

    /// <summary>
    /// Creates an isotope parent using the supplied parent-workflow options.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The parent-workflow options; isotope distinctions are always removed.</param>
    /// <returns>An independently owned isotope parent.</returns>
    public Molecule CreateIsotopeParentWithOptions(Molecule? molecule, StandardizationOptions? options)
    {
        return CreateIsotopeParent(molecule);
    }

    /// <summary>
    /// Creates the tautomer parent.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    public Molecule CreateTautomerParent(Molecule? molecule)
    {
        return CreateTautomerParentWithOptions(molecule, new StandardizationOptions());
    }

    /// <summary>
    /// Creates the tautomer parent using the specified options.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    public Molecule CreateTautomerParentWithOptions(Molecule? molecule, StandardizationOptions? options)
    {
        TautomerCanonicalizationResult result = TautomerCanonicalizer.CanonicalizeWithResult(molecule, GetTautomerOptions(options));
        if (result.Success)
        {
            return result.Molecule;
        }

        return MoleculeBuilder.CloneMolecule(molecule);
    }

    /// <summary>
    /// Creates the super parent.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    public Molecule CreateSuperParent(Molecule? molecule)
    {
        return CreateSuperParentWithOptions(molecule, new StandardizationOptions());
    }

    /// <summary>
    /// Creates the super parent using the specified options.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    public Molecule CreateSuperParentWithOptions(Molecule? molecule, StandardizationOptions? options)
    {
        Molecule parent = CreateFragmentParentWithOptions(molecule, options);
        parent = CreateTautomerParentWithOptions(parent, options);
        ClearChargesInPlace(parent);
        StripStereoInPlace(parent);
        ClearIsotopesInPlace(parent);
        ClearRadicalsInPlace(parent);
        parent.ClearCaches();
        return parent;
    }

    /// <summary>
    /// Creates the parent.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="policy">The identity or cleanup policy to apply.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    public Molecule CreateParent(Molecule? molecule, IdentityPolicy policy)
    {
        return CreateParentWithOptions(molecule, policy, null);
    }

    /// <summary>
    /// Creates the parent selected by an identity policy using explicit parent-workflow options.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="policy">The identity or parent policy to apply.</param>
    /// <param name="options">The options used by configurable parent workflows.</param>
    /// <returns>An independently owned policy parent.</returns>
    public Molecule CreateParentWithOptions(Molecule? molecule, IdentityPolicy policy, StandardizationOptions? options)
    {
        if (policy == IdentityPolicy.StereoParent || policy == IdentityPolicy.StereoInsensitive)
        {
            return CreateStereoParentWithOptions(molecule, options);
        }

        if (policy == IdentityPolicy.FragmentParent)
        {
            return CreateFragmentParentWithOptions(molecule, options);
        }

        if (policy == IdentityPolicy.ChargeParent || policy == IdentityPolicy.ChargeInsensitive)
        {
            return CreateChargeParentWithOptions(molecule, options);
        }

        if (policy == IdentityPolicy.IsotopeParent || policy == IdentityPolicy.IsotopeInsensitive)
        {
            return CreateIsotopeParentWithOptions(molecule, options);
        }

        if (policy == IdentityPolicy.TautomerParent || policy == IdentityPolicy.TautomerInsensitive)
        {
            return CreateTautomerParentWithOptions(molecule, options);
        }

        if (policy == IdentityPolicy.SuperParent)
        {
            return CreateSuperParentWithOptions(molecule, options);
        }

        return MoleculeBuilder.CloneMolecule(molecule);
    }

    internal void StripStereoInPlace(Molecule? molecule)
    {
        if (molecule == null)
        {
            return;
        }

        molecule.StereoRecords.Clear();
        molecule.StereoGroups = new StereoGroups();

        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            molecule.Bonds[bondIndex].Stereo = BondStereo.None;
            molecule.Bonds[bondIndex].Direction = BondDirection.None;
            bondIndex = bondIndex + 1;
        }
    }

    internal void ClearChargesInPlace(Molecule? molecule)
    {
        if (molecule == null)
        {
            return;
        }

        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            molecule.Atoms[atomIndex].FormalCharge = 0;
            atomIndex = atomIndex + 1;
        }
    }

    internal void ClearIsotopesInPlace(Molecule? molecule)
    {
        if (molecule == null)
        {
            return;
        }

        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            molecule.Atoms[atomIndex].IsotopeMassNumber = 0;
            atomIndex = atomIndex + 1;
        }
    }

    internal void ClearRadicalsInPlace(Molecule? molecule)
    {
        if (molecule == null)
        {
            return;
        }

        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            molecule.Atoms[atomIndex].RadicalElectronCount = 0;
            atomIndex = atomIndex + 1;
        }
    }

    internal TautomerEnumerationOptions GetTautomerOptions(StandardizationOptions? options)
    {
        if (options == null)
        {
            return new TautomerEnumerationOptions();
        }

        if (options.TautomerEnumeration == null)
        {
            return new TautomerEnumerationOptions();
        }

        return options.TautomerEnumeration;
    }
}
