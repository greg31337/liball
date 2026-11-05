using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class DoubleBondStereoTests
{
    [Test]
    public void StereoRecord_StoresDoubleBondStereoFields()
    {
        StereoRecord record = new StereoRecord();

        record.SetDoubleBond(3, 1, 4, DoubleBondConfiguration.E);
        record.GroupId = 2;
        record.Source = "slash";

        Assert.That(record.IsDoubleBond(), Is.True);
        Assert.That(record.IsTetrahedral(), Is.False);
        Assert.That(record.StereoBondId, Is.EqualTo(3));
        Assert.That(record.BeginLigandAtomId, Is.EqualTo(1));
        Assert.That(record.EndLigandAtomId, Is.EqualTo(4));
        Assert.That(record.Configuration, Is.EqualTo(DoubleBondConfiguration.E));
        Assert.That(record.GroupId, Is.EqualTo(2));
        Assert.That(record.Source, Is.EqualTo("slash"));
    }

    [Test]
    public void Validate_AcceptsValidExplicitDoubleBondStereoRecord()
    {
        Molecule molecule = BuildSubstitutedAlkene();
        StereoRecord record = new StereoRecord();
        record.SetDoubleBond(0, 2, 3, DoubleBondConfiguration.Z);
        molecule.StereoRecords.Add(record);
        StereoValidator validator = new StereoValidator();

        MoleculeReport report = validator.Validate(molecule);

        Assert.That(report.GetCount(), Is.EqualTo(0));
        Assert.That(record.Id, Is.EqualTo(0));
    }

    [Test]
    public void Validate_AcceptsImplicitHydrogenDoubleBondLigand()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int left = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 1, 0, 0, false, "", "C"));
        int right = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 1, 0, 0, false, "", "C"));
        int chlorine = builder.AddAtom(new AtomSpec(17, 0, 0, 0, 0, 0, 0, false, "", "Cl"));
        int bromine = builder.AddAtom(new AtomSpec(35, 0, 0, 0, 0, 0, 0, false, "", "Br"));
        builder.AddBond(left, right, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(left, chlorine, new BondSpec());
        builder.AddBond(right, bromine, new BondSpec());
        Molecule molecule = builder.Build();
        StereoRecord record = new StereoRecord();
        record.SetDoubleBond(0, -1, bromine, DoubleBondConfiguration.Trans);
        molecule.StereoRecords.Add(record);
        StereoValidator validator = new StereoValidator();

        MoleculeReport report = validator.Validate(molecule);

        Assert.That(report.GetCount(), Is.EqualTo(0));
    }

    [Test]
    public void Validate_ReportsInvalidDoubleBondStereoRecord()
    {
        Molecule molecule = BuildSubstitutedAlkene();
        StereoRecord record = new StereoRecord();
        record.SetDoubleBond(1, 2, 3, DoubleBondConfiguration.None);
        molecule.StereoRecords.Add(record);
        StereoValidator validator = new StereoValidator();

        MoleculeReport report = validator.Validate(molecule);

        Assert.That(HasCode(report, "DOUBLE_BOND_ORDER"), Is.True);
        Assert.That(HasCode(report, "DOUBLE_BOND_CONFIGURATION"), Is.True);
    }

    [Test]
    public void Validate_ReportsDoubleBondLigandErrors()
    {
        Molecule molecule = BuildSubstitutedAlkene();
        int unbonded = molecule.Atoms.Count;
        molecule.Atoms.Add(Atom.CreateWithAtomicNumber(unbonded, 9));
        StereoRecord record = new StereoRecord();
        record.SetDoubleBond(0, 1, unbonded, DoubleBondConfiguration.E);
        molecule.StereoRecords.Add(record);
        StereoValidator validator = new StereoValidator();

        MoleculeReport report = validator.Validate(molecule);

        Assert.That(HasCode(report, "DOUBLE_BOND_SELF_LIGAND"), Is.True);
        Assert.That(HasCode(report, "DOUBLE_BOND_UNBONDED_LIGAND"), Is.True);
        Assert.That(report.GetErrorCount(), Is.EqualTo(2));
    }

    [Test]
    public void Validate_ReportsInvalidAndDuplicateDoubleBondLigands()
    {
        Molecule molecule = BuildSubstitutedAlkene();
        StereoRecord invalidRecord = new StereoRecord();
        invalidRecord.SetDoubleBond(0, 99, 3, DoubleBondConfiguration.Cis);
        StereoRecord duplicateRecord = new StereoRecord();
        duplicateRecord.SetDoubleBond(0, 2, 2, DoubleBondConfiguration.Trans);
        molecule.StereoRecords.Add(invalidRecord);
        molecule.StereoRecords.Add(duplicateRecord);
        StereoValidator validator = new StereoValidator();

        MoleculeReport report = validator.Validate(molecule);

        Assert.That(HasCode(report, "DOUBLE_BOND_INVALID_LIGAND"), Is.True);
        Assert.That(HasCode(report, "DOUBLE_BOND_DUPLICATE_LIGAND"), Is.True);
    }

    [Test]
    public void Validate_ReportsInvalidDoubleBondReferenceAndImplicitLigand()
    {
        Molecule molecule = BuildSubstitutedAlkene();
        StereoRecord invalidBondRecord = new StereoRecord();
        invalidBondRecord.SetDoubleBond(99, 2, 3, DoubleBondConfiguration.E);
        StereoRecord implicitRecord = new StereoRecord();
        implicitRecord.SetDoubleBond(0, -1, 3, DoubleBondConfiguration.E);
        molecule.StereoRecords.Add(invalidBondRecord);
        molecule.StereoRecords.Add(implicitRecord);
        StereoValidator validator = new StereoValidator();

        MoleculeReport report = validator.Validate(molecule);

        Assert.That(HasCode(report, "DOUBLE_BOND_INVALID_BOND"), Is.True);
        Assert.That(HasCode(report, "DOUBLE_BOND_IMPLICIT_LIGAND"), Is.True);
    }

    [Test]
    public void CloneMolecule_CopiesDoubleBondStereoRecordFields()
    {
        Molecule molecule = BuildSubstitutedAlkene();
        StereoRecord record = new StereoRecord();
        record.SetDoubleBond(0, 2, 3, DoubleBondConfiguration.E);
        record.GroupId = 9;
        record.Source = "mol";
        molecule.StereoRecords.Add(record);

        Molecule clone = MoleculeBuilder.CloneMolecule(molecule);
        molecule.StereoRecords[0].BeginLigandAtomId = 3;
        molecule.StereoRecords[0].Configuration = DoubleBondConfiguration.Z;

        Assert.That(clone.StereoRecords.Count, Is.EqualTo(1));
        Assert.That(clone.StereoRecords[0], Is.Not.SameAs(record));
        Assert.That(clone.StereoRecords[0].IsDoubleBond(), Is.True);
        Assert.That(clone.StereoRecords[0].StereoBondId, Is.EqualTo(0));
        Assert.That(clone.StereoRecords[0].BeginLigandAtomId, Is.EqualTo(2));
        Assert.That(clone.StereoRecords[0].EndLigandAtomId, Is.EqualTo(3));
        Assert.That(clone.StereoRecords[0].Configuration, Is.EqualTo(DoubleBondConfiguration.E));
        Assert.That(clone.StereoRecords[0].GroupId, Is.EqualTo(9));
        Assert.That(clone.StereoRecords[0].Source, Is.EqualTo("mol"));
    }

    static Molecule BuildSubstitutedAlkene()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int left = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int right = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int chlorine = builder.AddAtom(new AtomSpec(17, 0, 0, 0, 0, 0, 0, false, "", "Cl"));
        int bromine = builder.AddAtom(new AtomSpec(35, 0, 0, 0, 0, 0, 0, false, "", "Br"));
        builder.AddBond(left, right, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(left, chlorine, new BondSpec());
        builder.AddBond(right, bromine, new BondSpec());
        return builder.Build();
    }

    static bool HasCode(MoleculeReport report, string code)
    {
        int index = 0;
        while (index < report.GetCount())
        {
            if (report.Get(index).Code == code)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
