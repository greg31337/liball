using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class StereoCleanupParentTests
{
    [Test]
    public void Clean_RemovesInvalidStereoRecordsAndRebuildsGroups()
    {
        Molecule molecule = BuildStereoMolecule();
        StereoRecord invalid = new StereoRecord();
        invalid.Id = 1;
        invalid.SetTetrahedral(99, 1, 2, 3, 4, Chirality.Clockwise);
        molecule.StereoRecords.Add(invalid);
        EnhancedStereoGroup group = molecule.StereoGroups.AddGroup("or", "V3000", "MDLV30/STEREL1");
        group.StereoRecordIds.Add(0);
        group.StereoRecordIds.Add(1);
        group.StereoRecordIds.Add(99);
        EnhancedStereoGroup invalidGroup = molecule.StereoGroups.AddGroup("bad", "SDF", "bad");
        invalidGroup.StereoRecordIds.Add(0);
        StereoCleaner cleaner = new StereoCleaner();

        StereoCleanupResult result = cleaner.Clean(molecule);

        Assert.That(result.Success, Is.True);
        Assert.That(result.RemovedRecordCount, Is.EqualTo(1));
        Assert.That(result.RemovedGroupCount, Is.EqualTo(1));
        Assert.That(molecule.StereoRecords.Count, Is.EqualTo(1));
        Assert.That(molecule.StereoRecords[0].Id, Is.EqualTo(0));
        Assert.That(molecule.StereoRecords[0].GroupId, Is.EqualTo(0));
        Assert.That(molecule.StereoGroups.Groups.Count, Is.EqualTo(1));
        Assert.That(molecule.StereoGroups.Groups[0].Id, Is.EqualTo(0));
        Assert.That(molecule.StereoGroups.Groups[0].StereoRecordIds, Is.EqualTo(new int[] { 0 }));
        Assert.That(HasCode(result.Report, "STEREO_RECORD_REMOVED"), Is.True);
        Assert.That(HasCode(result.Report, "STEREO_GROUP_REMOVED"), Is.True);
    }

    [Test]
    public void Clean_RemovesDuplicateGroupAssignmentsDeterministically()
    {
        Molecule molecule = BuildStereoMolecule();
        EnhancedStereoGroup first = molecule.StereoGroups.AddGroup("or", "V3000", "first");
        EnhancedStereoGroup second = molecule.StereoGroups.AddGroup("and", "V3000", "second");
        first.StereoRecordIds.Add(0);
        second.StereoRecordIds.Add(0);
        StereoCleaner cleaner = new StereoCleaner();

        StereoCleanupResult result = cleaner.Clean(molecule);

        Assert.That(result.Success, Is.True);
        Assert.That(molecule.StereoGroups.Groups.Count, Is.EqualTo(1));
        Assert.That(molecule.StereoGroups.Groups[0].Label, Is.EqualTo("first"));
        Assert.That(molecule.StereoRecords[0].GroupId, Is.EqualTo(0));
        Assert.That(result.RemovedGroupCount, Is.EqualTo(1));
    }

    [Test]
    public void Clean_ReturnsErrorForNullMolecule()
    {
        StereoCleaner cleaner = new StereoCleaner();

        StereoCleanupResult result = cleaner.Clean(null);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Report.GetErrorCount(), Is.EqualTo(1));
        Assert.That(result.Report.Get(0).Code, Is.EqualTo("STEREO_CLEANUP_NULL_MOLECULE"));
    }

    [Test]
    public void CreateStereoParent_ClonesAndStripsStereoWithoutChangingSource()
    {
        Molecule molecule = BuildStereoMolecule();
        EnhancedStereoGroup group = molecule.StereoGroups.AddGroup("or", "V3000", "group");
        molecule.StereoGroups.AssignRecordToGroup(molecule, 0, group.Id);
        molecule.Bonds[0].Stereo = BondStereo.Wedge;
        molecule.Bonds[0].Direction = BondDirection.Up;
        ParentMoleculeFactory factory = new ParentMoleculeFactory();

        Molecule parent = factory.CreateStereoParent(molecule);

        Assert.That(parent, Is.Not.SameAs(molecule));
        Assert.That(parent.GetAtomCount(), Is.EqualTo(molecule.GetAtomCount()));
        Assert.That(parent.GetBondCount(), Is.EqualTo(molecule.GetBondCount()));
        Assert.That(parent.StereoRecords, Is.Empty);
        Assert.That(parent.StereoGroups.Groups, Is.Empty);
        Assert.That(parent.Bonds[0].Stereo, Is.EqualTo(BondStereo.None));
        Assert.That(parent.Bonds[0].Direction, Is.EqualTo(BondDirection.None));
        Assert.That(molecule.StereoRecords.Count, Is.EqualTo(1));
        Assert.That(molecule.StereoGroups.Groups.Count, Is.EqualTo(1));
        Assert.That(molecule.Bonds[0].Stereo, Is.EqualTo(BondStereo.Wedge));
        Assert.That(molecule.Bonds[0].Direction, Is.EqualTo(BondDirection.Up));
    }

    [Test]
    public void CreateParent_UsesStereoParentForStereoPoliciesOnly()
    {
        Molecule molecule = BuildStereoMolecule();
        ParentMoleculeFactory factory = new ParentMoleculeFactory();

        Molecule stereoParent = factory.CreateParent(molecule, IdentityPolicy.StereoParent);
        Molecule exactParent = factory.CreateParent(molecule, IdentityPolicy.Exact);

        Assert.That(stereoParent.StereoRecords, Is.Empty);
        Assert.That(exactParent.StereoRecords.Count, Is.EqualTo(1));
    }

    static Molecule BuildStereoMolecule()
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
        StereoRecord record = new StereoRecord();
        record.Id = 0;
        record.SetTetrahedral(carbon, fluorine, chlorine, bromine, iodine, Chirality.Clockwise);
        molecule.StereoRecords.Add(record);
        return molecule;
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
