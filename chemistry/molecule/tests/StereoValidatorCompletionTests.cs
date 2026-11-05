using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class StereoValidatorCompletionTests
{
    [Test]
    public void Validate_ReportsNullRecordsAndInvalidDoubleBondAtomReferences()
    {
        Molecule molecule = new Molecule();
        molecule.Atoms.Add(new Atom(0, new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C")));
        molecule.Bonds.Add(new Bond(0, 0, 99, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None)));
        molecule.StereoRecords.Add(null!);
        StereoRecord doubleBond = new StereoRecord();
        doubleBond.SetDoubleBond(0, -1, -1, DoubleBondConfiguration.E);
        molecule.StereoRecords.Add(doubleBond);

        MoleculeReport report = new StereoValidator().Validate(molecule);

        Assert.That(HasCode(report, "STEREO_NULL_RECORD"), Is.True);
        Assert.That(HasCode(report, "DOUBLE_BOND_INVALID_BOND"), Is.True);
    }

    [Test]
    public void Validate_ReportsEveryExtendedStereoPlaceholderFailure()
    {
        Molecule molecule = new Molecule();
        molecule.Atoms.Add(new Atom(0, new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C")));
        StereoRecord resolvedAllene = new StereoRecord();
        resolvedAllene.Kind = "allene";
        resolvedAllene.Type = StereoKind.Unknown;
        resolvedAllene.CenterAtomId = 99;
        resolvedAllene.LigandAtomIds.Add(0);
        resolvedAllene.LigandAtomIds.Add(0);
        resolvedAllene.LigandAtomIds.Add(99);
        StereoRecord atropisomer = new StereoRecord();
        atropisomer.Kind = "atropisomeric";
        atropisomer.Type = StereoKind.Atropisomeric;
        atropisomer.StereoBondId = 99;
        StereoRecord invalidType = new StereoRecord();
        invalidType.Kind = "allene";
        invalidType.Type = (StereoKind)99;
        molecule.StereoRecords.Add(resolvedAllene);
        molecule.StereoRecords.Add(atropisomer);
        molecule.StereoRecords.Add(invalidType);

        MoleculeReport report = new StereoValidator().Validate(molecule);

        Assert.That(HasCode(report, "EXTENDED_STEREO_INVALID_CENTER"), Is.True);
        Assert.That(HasCode(report, "EXTENDED_STEREO_INVALID_AXIS"), Is.True);
        Assert.That(HasCode(report, "EXTENDED_STEREO_INVALID_TYPE"), Is.True);
        Assert.That(HasCode(report, "EXTENDED_STEREO_INVALID_CARRIER"), Is.True);
        Assert.That(HasCode(report, "EXTENDED_STEREO_DUPLICATE_CARRIER"), Is.True);
    }

    static bool HasCode(MoleculeReport report, string code)
    {
        int index = 0;
        while (index < report.Errors.Count)
        {
            if (report.Errors[index].Code == code)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
