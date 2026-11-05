using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class TetrahedralStereoTests
{
    [Test]
    public void StereoRecord_StoresTetrahedralCenterLigandsAndChirality()
    {
        StereoRecord record = StereoRecord.Create(7, "placeholder");

        record.SetTetrahedral(2, 0, 1, 3, 4, Chirality.Clockwise);
        record.GroupId = 5;
        record.Source = "test";

        Assert.That(record.Id, Is.EqualTo(7));
        Assert.That(record.IsTetrahedral(), Is.True);
        Assert.That(record.CenterAtomId, Is.EqualTo(2));
        Assert.That(record.LigandAtomIds, Is.EqualTo(new int[] { 0, 1, 3, 4 }));
        Assert.That(record.GetLigandCount(), Is.EqualTo(4));
        Assert.That(record.GetImplicitLigandCount(), Is.EqualTo(0));
        Assert.That(record.Chirality, Is.EqualTo(Chirality.Clockwise));
        Assert.That(record.GroupId, Is.EqualTo(5));
        Assert.That(record.Source, Is.EqualTo("test"));
    }

    [Test]
    public void Validate_AcceptsValidTetrahedralRecordWithFourExplicitLigands()
    {
        Molecule molecule = BuildTetrahedralCarbon();
        StereoRecord record = new StereoRecord();
        record.SetTetrahedral(0, 1, 2, 3, 4, Chirality.CounterClockwise);
        molecule.StereoRecords.Add(record);
        StereoValidator validator = new StereoValidator();

        MoleculeReport report = validator.Validate(molecule);

        Assert.That(report.GetCount(), Is.EqualTo(0));
        Assert.That(record.Id, Is.EqualTo(0));
    }

    [Test]
    public void Validate_AcceptsOneImplicitTetrahedralLigand()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int center = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 1, 0, 0, false, "", "C"));
        int fluorine = builder.AddAtom(new AtomSpec(9, 0, 0, 0, 0, 0, 0, false, "", "F"));
        int chlorine = builder.AddAtom(new AtomSpec(17, 0, 0, 0, 0, 0, 0, false, "", "Cl"));
        int bromine = builder.AddAtom(new AtomSpec(35, 0, 0, 0, 0, 0, 0, false, "", "Br"));
        builder.AddBond(center, fluorine, new BondSpec());
        builder.AddBond(center, chlorine, new BondSpec());
        builder.AddBond(center, bromine, new BondSpec());
        Molecule molecule = builder.Build();
        StereoRecord record = new StereoRecord();
        record.SetTetrahedral(center, fluorine, chlorine, bromine, -1, Chirality.Clockwise);
        molecule.StereoRecords.Add(record);
        StereoValidator validator = new StereoValidator();

        MoleculeReport report = validator.Validate(molecule);

        Assert.That(report.GetCount(), Is.EqualTo(0));
        Assert.That(record.GetImplicitLigandCount(), Is.EqualTo(1));
    }

    [Test]
    public void Validate_ReportsInvalidTetrahedralLigandsAndChirality()
    {
        Molecule molecule = BuildTetrahedralCarbon();
        StereoRecord record = new StereoRecord();
        record.SetTetrahedral(0, 1, 1, 99, 0, Chirality.None);
        molecule.StereoRecords.Add(record);
        StereoValidator validator = new StereoValidator();

        MoleculeReport report = validator.Validate(molecule);

        Assert.That(HasCode(report, "TETRAHEDRAL_CHIRALITY"), Is.True);
        Assert.That(HasCode(report, "TETRAHEDRAL_DUPLICATE_LIGAND"), Is.True);
        Assert.That(HasCode(report, "TETRAHEDRAL_INVALID_LIGAND"), Is.True);
        Assert.That(HasCode(report, "TETRAHEDRAL_SELF_LIGAND"), Is.True);
        Assert.That(report.GetErrorCount(), Is.EqualTo(4));
    }

    [Test]
    public void Validate_ReportsLigandCountAndUnbondedLigandErrors()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int center = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int ligand = builder.AddAtom(new AtomSpec(9, 0, 0, 0, 0, 0, 0, false, "", "F"));
        int unbonded = builder.AddAtom(new AtomSpec(17, 0, 0, 0, 0, 0, 0, false, "", "Cl"));
        builder.AddBond(center, ligand, new BondSpec());
        Molecule molecule = builder.Build();
        StereoRecord shortRecord = new StereoRecord();
        shortRecord.Kind = "tetrahedral";
        shortRecord.CenterAtomId = center;
        shortRecord.Chirality = Chirality.Clockwise;
        shortRecord.AddLigand(ligand);
        StereoRecord unbondedRecord = new StereoRecord();
        unbondedRecord.SetTetrahedral(center, ligand, unbonded, -1, -2, Chirality.Clockwise);
        molecule.StereoRecords.Add(shortRecord);
        molecule.StereoRecords.Add(unbondedRecord);
        StereoValidator validator = new StereoValidator();

        MoleculeReport report = validator.Validate(molecule);

        Assert.That(HasCode(report, "TETRAHEDRAL_LIGAND_COUNT"), Is.True);
        Assert.That(HasCode(report, "TETRAHEDRAL_UNBONDED_LIGAND"), Is.True);
        Assert.That(HasCode(report, "TETRAHEDRAL_IMPLICIT_LIGAND"), Is.True);
    }

    [Test]
    public void CloneMolecule_CopiesStereoRecordFieldsAndLigandsIndependently()
    {
        Molecule molecule = BuildTetrahedralCarbon();
        StereoRecord record = new StereoRecord();
        record.SetTetrahedral(0, 1, 2, 3, 4, Chirality.Clockwise);
        record.GroupId = 12;
        record.Source = "mol";
        molecule.StereoRecords.Add(record);

        Molecule clone = MoleculeBuilder.CloneMolecule(molecule);
        molecule.StereoRecords[0].LigandAtomIds[0] = 4;
        molecule.StereoRecords[0].Chirality = Chirality.CounterClockwise;

        Assert.That(clone.StereoRecords.Count, Is.EqualTo(1));
        Assert.That(clone.StereoRecords[0], Is.Not.SameAs(record));
        Assert.That(clone.StereoRecords[0].Id, Is.EqualTo(0));
        Assert.That(clone.StereoRecords[0].IsTetrahedral(), Is.True);
        Assert.That(clone.StereoRecords[0].LigandAtomIds, Is.EqualTo(new int[] { 1, 2, 3, 4 }));
        Assert.That(clone.StereoRecords[0].Chirality, Is.EqualTo(Chirality.Clockwise));
        Assert.That(clone.StereoRecords[0].GroupId, Is.EqualTo(12));
        Assert.That(clone.StereoRecords[0].Source, Is.EqualTo("mol"));
    }

    [Test]
    public void Validate_ReturnsErrorForNullMoleculeAndWarningForUnknownRecord()
    {
        StereoValidator validator = new StereoValidator();
        Molecule molecule = new Molecule();
        molecule.StereoRecords.Add(StereoRecord.Create(3, "unknown"));

        MoleculeReport nullReport = validator.Validate(null);
        MoleculeReport unknownReport = validator.Validate(molecule);

        Assert.That(nullReport.GetErrorCount(), Is.EqualTo(1));
        Assert.That(nullReport.Get(0).Code, Is.EqualTo("STEREO_NULL_MOLECULE"));
        Assert.That(unknownReport.GetWarningCount(), Is.EqualTo(1));
        Assert.That(unknownReport.Get(0).Code, Is.EqualTo("STEREO_UNKNOWN_RECORD"));
    }

    static Molecule BuildTetrahedralCarbon()
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
