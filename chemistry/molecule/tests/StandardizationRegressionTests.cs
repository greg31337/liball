using System.Collections.Generic;
using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class StandardizationRegressionTests
{
    [Test]
    public void ParentCleanup_RemovesSaltsNormalizesChargesCanonicalizesTautomerAndCleansStereo()
    {
        Standardizer standardizer = new Standardizer();
        Molecule molecule = BuildDecoratedEnolNitroSaltWithInvalidStereo();
        StandardizationOptions options = BuildRegressionOptions();

        StandardizationResult result = standardizer.Standardize(molecule, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Changed, Is.True);
        Assert.That(GetStepNames(result), Is.EqualTo(new string[] { "salt-removal", "charge-normalization", "tautomer-canonicalization", "stereo-cleanup" }));
        Assert.That(GetChangedStepNames(result), Is.EqualTo(new string[] { "salt-removal", "charge-normalization", "tautomer-canonicalization" }));
        Assert.That(result.Molecule.GetAtomCount(), Is.EqualTo(6));
        Assert.That(result.Molecule.Atoms[1].ImplicitHydrogenCount, Is.EqualTo(0));
        Assert.That(result.Molecule.Atoms[2].ImplicitHydrogenCount, Is.EqualTo(2));
        Assert.That(result.Molecule.Bonds[0].Order, Is.EqualTo(BondOrder.Double));
        Assert.That(result.Molecule.Bonds[1].Order, Is.EqualTo(BondOrder.Single));
        Assert.That(result.Molecule.Atoms[3].FormalCharge, Is.EqualTo(1));
        Assert.That(CountAtomsWithCharge(result.Molecule, -1), Is.EqualTo(1));
        Assert.That(CountAtomsWithAtomicNumber(result.Molecule, 11), Is.EqualTo(0));
        Assert.That(CountAtomsWithAtomicNumber(result.Molecule, 17), Is.EqualTo(0));
        Assert.That(result.Molecule.StereoRecords, Is.Empty);
        Assert.That(result.Molecule.StereoGroups.Groups, Is.Empty);
        Assert.That(molecule.GetAtomCount(), Is.EqualTo(8));
        Assert.That(molecule.Atoms[3].FormalCharge, Is.EqualTo(0));
        Assert.That(molecule.StereoRecords.Count, Is.EqualTo(1));
    }

    [Test]
    public void SaltRegression_RemovesOnlyKnownCounterionsWhenParentHasCompetingFragments()
    {
        SaltRemover remover = new SaltRemover();
        Molecule molecule = BuildParentWithSodiumChlorideAndMethane();

        SaltRemovalResult result = remover.RemoveSaltsWithResult(molecule);

        Assert.That(result.Success, Is.True);
        Assert.That(result.InputFragmentCount, Is.EqualTo(4));
        Assert.That(result.MatchedSaltFragmentCount, Is.EqualTo(2));
        Assert.That(result.RemovedFragmentNames, Is.EqualTo(new string[] { "sodium", "chloride" }));
        Assert.That(result.Parent.GetAtomCount(), Is.EqualTo(3));
        Assert.That(new CanonicalSmilesWriter().Write(result.Parent), Is.EqualTo("OCC"));
    }

    [Test]
    public void TautomerRegression_CanonicalParentIsStableFromKetoAndEnol()
    {
        ParentMoleculeFactory factory = new ParentMoleculeFactory();
        DeterministicSerializer serializer = new DeterministicSerializer();

        Molecule ketoParent = factory.CreateTautomerParent(BuildSimpleKeto());
        Molecule enolParent = factory.CreateTautomerParent(BuildSimpleEnol());

        Assert.That(serializer.Serialize(enolParent), Is.EqualTo(serializer.Serialize(ketoParent)));
        Assert.That(enolParent.Bonds[0].Order, Is.EqualTo(BondOrder.Double));
        Assert.That(enolParent.Bonds[1].Order, Is.EqualTo(BondOrder.Single));
        Assert.That(ketoParent.Bonds[0].Order, Is.EqualTo(BondOrder.Double));
        Assert.That(ketoParent.Bonds[1].Order, Is.EqualTo(BondOrder.Single));
    }

    [Test]
    public void DuplicateRegression_SuperParentGroupsSaltChargeStereoAndTautomerVariants()
    {
        IdentityOptions options = new IdentityOptions();
        options.Policy = IdentityPolicy.SuperParent;
        DuplicateDetector detector = DuplicateDetector.Create(options);

        DuplicateResult first = detector.Add(BuildSimpleDecoratedEnolSaltChargeStereo());
        DuplicateResult second = detector.Add(BuildSimpleKeto());
        List<DuplicateGroup> groups = detector.GetDuplicateGroups();

        Assert.That(first.Success, Is.True);
        Assert.That(second.Success, Is.True);
        Assert.That(second.IsDuplicate, Is.True);
        Assert.That(second.FirstIndex, Is.EqualTo(0));
        Assert.That(second.Key, Is.EqualTo(first.Key));
        Assert.That(second.Molecule.GetAtomCount(), Is.EqualTo(3));
        Assert.That(CountAtomsWithCharge(second.Molecule, 1), Is.EqualTo(0));
        Assert.That(second.Molecule.StereoRecords, Is.Empty);
        Assert.That(groups.Count, Is.EqualTo(1));
        Assert.That(groups[0].DuplicateIndices, Is.EqualTo(new int[] { 1 }));
    }

    [Test]
    public void StereoCleanupRegression_RenumbersRecordsAfterInvalidEntriesAreRemoved()
    {
        Molecule molecule = BuildStereoCleanupRegressionMolecule();
        StereoCleaner cleaner = new StereoCleaner();

        StereoCleanupResult result = cleaner.Clean(molecule);

        Assert.That(result.Success, Is.True);
        Assert.That(result.RemovedRecordCount, Is.EqualTo(1));
        Assert.That(result.RemovedGroupCount, Is.EqualTo(1));
        Assert.That(molecule.StereoRecords.Count, Is.EqualTo(1));
        Assert.That(molecule.StereoRecords[0].Id, Is.EqualTo(0));
        Assert.That(molecule.StereoRecords[0].GroupId, Is.EqualTo(0));
        Assert.That(molecule.StereoGroups.Groups.Count, Is.EqualTo(1));
        Assert.That(molecule.StereoGroups.Groups[0].StereoRecordIds, Is.EqualTo(new int[] { 0 }));
    }

    static StandardizationOptions BuildRegressionOptions()
    {
        StandardizationOptions options = new StandardizationOptions();
        options.EnableSaltRemoval = true;
        options.EnableChargeNormalization = true;
        options.EnableTautomerCanonicalization = true;
        options.EnableStereoCleanup = true;
        return options;
    }

    static Molecule BuildDecoratedEnolNitroSaltWithInvalidStereo()
    {
        Molecule molecule = BuildEnolNitroParent();
        MoleculeBuilder builder = new MoleculeBuilder();
        int index = 0;
        while (index < molecule.Atoms.Count)
        {
            Atom atom = molecule.Atoms[index];
            builder.AddAtom(new AtomSpec(atom.AtomicNumber, atom.IsotopeMassNumber, atom.FormalCharge, atom.ExplicitHydrogenCount, atom.ImplicitHydrogenCount, atom.RadicalElectronCount, atom.AtomMapNumber, atom.IsAromatic, atom.Alias, atom.Label));
            index = index + 1;
        }

        index = 0;
        while (index < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[index];
            builder.AddBond(bond.BeginAtomId, bond.EndAtomId, BondSpec.Create(bond.Order, bond.IsAromatic, bond.Stereo, bond.Direction));
            index = index + 1;
        }

        builder.AddAtom(new AtomSpec(11, 0, 1, 0, 0, 0, 0, false, "", "Na"));
        builder.AddAtom(new AtomSpec(17, 0, -1, 0, 0, 0, 0, false, "", "Cl"));
        Molecule decorated = builder.Build();
        decorated.Bonds[0].Stereo = BondStereo.Wedge;
        decorated.Bonds[0].Direction = BondDirection.Up;
        StereoRecord invalid = new StereoRecord();
        invalid.Id = 0;
        invalid.SetTetrahedral(99, 1, 2, 3, 4, Chirality.Clockwise);
        decorated.StereoRecords.Add(invalid);
        EnhancedStereoGroup group = decorated.StereoGroups.AddGroup("or", "regression", "invalid");
        group.StereoRecordIds.Add(0);
        return decorated;
    }

    static Molecule BuildParentWithSodiumChlorideAndMethane()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbonA = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int carbonB = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        builder.AddBond(carbonA, carbonB, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        builder.AddBond(carbonB, oxygen, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 4, 0, 0, false, "", "C"));
        builder.AddAtom(new AtomSpec(11, 0, 1, 0, 0, 0, 0, false, "", "Na"));
        builder.AddAtom(new AtomSpec(17, 0, -1, 0, 0, 0, 0, false, "", "Cl"));
        return builder.Build();
    }

    static Molecule BuildKetoNitroParent()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbonylCarbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        int methylCarbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        int nitrogen = builder.AddAtom(new AtomSpec(7, 0, 0, 0, 0, 0, 0, false, "", "N"));
        int oxygenA = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        int oxygenB = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        builder.AddBond(carbonylCarbon, oxygen, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(carbonylCarbon, methylCarbon, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        builder.AddBond(methylCarbon, nitrogen, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        builder.AddBond(nitrogen, oxygenA, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(nitrogen, oxygenB, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        return builder.Build();
    }

    static Molecule BuildSimpleKeto()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbonylCarbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        int methylCarbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        builder.AddBond(carbonylCarbon, oxygen, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(carbonylCarbon, methylCarbon, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        return builder.Build();
    }

    static Molecule BuildSimpleEnol()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 1, 0, 0, false, "", "O"));
        int terminalCarbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        builder.AddBond(carbon, oxygen, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        builder.AddBond(carbon, terminalCarbon, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        return builder.Build();
    }

    static Molecule BuildSimpleDecoratedEnolSaltChargeStereo()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 1, 0, 0, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 1, 0, 0, false, "", "O"));
        int terminalCarbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        builder.AddBond(carbon, oxygen, BondSpec.Create(BondOrder.Single, false, BondStereo.Wedge, BondDirection.Up));
        builder.AddBond(carbon, terminalCarbon, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddAtom(new AtomSpec(11, 0, 1, 0, 0, 0, 0, false, "", "Na"));
        builder.AddAtom(new AtomSpec(17, 0, -1, 0, 0, 0, 0, false, "", "Cl"));
        return builder.Build();
    }

    static Molecule BuildEnolNitroParent()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 1, 0, 0, false, "", "O"));
        int terminalCarbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        int nitrogen = builder.AddAtom(new AtomSpec(7, 0, 0, 0, 0, 0, 0, false, "", "N"));
        int oxygenA = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        int oxygenB = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        builder.AddBond(carbon, oxygen, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        builder.AddBond(carbon, terminalCarbon, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(terminalCarbon, nitrogen, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        builder.AddBond(nitrogen, oxygenA, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(nitrogen, oxygenB, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        return builder.Build();
    }

    static Molecule BuildStereoCleanupRegressionMolecule()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int fluorine = builder.AddAtom(new AtomSpec(9, 0, 0, 0, 0, 0, 0, false, "", "F"));
        int chlorine = builder.AddAtom(new AtomSpec(17, 0, 0, 0, 0, 0, 0, false, "", "Cl"));
        int bromine = builder.AddAtom(new AtomSpec(35, 0, 0, 0, 0, 0, 0, false, "", "Br"));
        int iodine = builder.AddAtom(new AtomSpec(53, 0, 0, 0, 0, 0, 0, false, "", "I"));
        builder.AddBond(carbon, fluorine, new BondSpec());
        builder.AddBond(carbon, chlorine, new BondSpec());
        builder.AddBond(carbon, bromine, new BondSpec());
        builder.AddBond(carbon, iodine, new BondSpec());
        Molecule molecule = builder.Build();
        StereoRecord invalid = new StereoRecord();
        invalid.Id = 0;
        invalid.SetTetrahedral(99, fluorine, chlorine, bromine, iodine, Chirality.Clockwise);
        molecule.StereoRecords.Add(invalid);
        StereoRecord valid = new StereoRecord();
        valid.Id = 1;
        valid.SetTetrahedral(carbon, fluorine, chlorine, bromine, iodine, Chirality.Clockwise);
        molecule.StereoRecords.Add(valid);
        EnhancedStereoGroup empty = molecule.StereoGroups.AddGroup("or", "regression", "empty-after-cleanup");
        empty.StereoRecordIds.Add(0);
        EnhancedStereoGroup kept = molecule.StereoGroups.AddGroup("and", "regression", "kept");
        kept.StereoRecordIds.Add(1);
        return molecule;
    }

    static List<string> GetStepNames(StandardizationResult result)
    {
        List<string> names = new List<string>();
        int index = 0;
        while (index < result.Mutations.Count)
        {
            names.Add(result.Mutations[index].StepName);
            index = index + 1;
        }

        return names;
    }

    static List<string> GetChangedStepNames(StandardizationResult result)
    {
        List<string> names = new List<string>();
        int index = 0;
        while (index < result.Mutations.Count)
        {
            if (result.Mutations[index].Changed)
            {
                names.Add(result.Mutations[index].StepName);
            }

            index = index + 1;
        }

        return names;
    }

    static int CountAtomsWithAtomicNumber(Molecule molecule, int atomicNumber)
    {
        int count = 0;
        int index = 0;
        while (index < molecule.Atoms.Count)
        {
            if (molecule.Atoms[index].AtomicNumber == atomicNumber)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    static int CountAtomsWithCharge(Molecule molecule, int charge)
    {
        int count = 0;
        int index = 0;
        while (index < molecule.Atoms.Count)
        {
            if (molecule.Atoms[index].FormalCharge == charge)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }
}
