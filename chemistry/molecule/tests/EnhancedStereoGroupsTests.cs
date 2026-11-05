using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class EnhancedStereoGroupsTests
{
    [Test]
    public void Molecule_InitializesEnhancedStereoGroupStorage()
    {
        Molecule molecule = new Molecule();

        Assert.That(molecule.StereoGroups, Is.Not.Null);
        Assert.That(molecule.GetStereoGroups(), Is.SameAs(molecule.StereoGroups));
        Assert.That(molecule.StereoGroups.Groups, Is.Empty);
    }

    [Test]
    public void AddGroup_StoresV3000AndSdfMetadataAndAssignsRecords()
    {
        Molecule molecule = BuildMoleculeWithStereoRecords();
        EnhancedStereoGroup group = molecule.StereoGroups.AddGroup("OR", "V3000", "MDLV30/STEREL1");

        molecule.StereoGroups.AssignRecordToGroup(molecule, 0, group.Id);
        molecule.StereoGroups.AssignRecordToGroup(molecule, 1, group.Id);

        Assert.That(group.Id, Is.EqualTo(0));
        Assert.That(group.IsOr(), Is.True);
        Assert.That(group.Type, Is.EqualTo("or"));
        Assert.That(group.Source, Is.EqualTo("V3000"));
        Assert.That(group.Label, Is.EqualTo("MDLV30/STEREL1"));
        Assert.That(group.StereoRecordIds, Is.EqualTo(new int[] { 0, 1 }));
        Assert.That(molecule.StereoRecords[0].GroupId, Is.EqualTo(group.Id));
        Assert.That(molecule.StereoGroups.GetGroup(group.Id), Is.SameAs(group));
        Assert.That(molecule.StereoGroups.GetGroupsForRecord(1), Is.EqualTo(new EnhancedStereoGroup[] { group }));
    }

    [Test]
    public void Validate_AcceptsAbsoluteOrAndGroupsWithValidRecords()
    {
        Molecule molecule = BuildMoleculeWithStereoRecords();
        EnhancedStereoGroup absolute = molecule.StereoGroups.AddGroup("abs", "SDF", "ABSOLUTE");
        EnhancedStereoGroup andGroup = molecule.StereoGroups.AddGroup("AND", "V3000", "MDLV30/STERAC1");
        molecule.StereoGroups.AssignRecordToGroup(molecule, 0, absolute.Id);
        molecule.StereoGroups.AssignRecordToGroup(molecule, 1, andGroup.Id);

        MoleculeReport report = molecule.StereoGroups.Validate(molecule);

        Assert.That(report.GetCount(), Is.EqualTo(0));
        Assert.That(absolute.IsAbsolute(), Is.True);
        Assert.That(andGroup.IsAnd(), Is.True);
    }

    [Test]
    public void Validate_ReportsInvalidGroupMetadata()
    {
        Molecule molecule = BuildMoleculeWithStereoRecords();
        EnhancedStereoGroup invalidType = molecule.StereoGroups.AddGroup("relative", "V3000", "bad");
        invalidType.StereoRecordIds.Add(0);
        EnhancedStereoGroup missingRecord = molecule.StereoGroups.AddGroup("or", "SDF", "missing");
        missingRecord.StereoRecordIds.Add(99);
        EnhancedStereoGroup mismatch = molecule.StereoGroups.AddGroup("and", "SDF", "mismatch");
        mismatch.StereoRecordIds.Add(1);
        molecule.StereoRecords[1].GroupId = 42;

        MoleculeReport report = molecule.StereoGroups.Validate(molecule);

        Assert.That(HasCode(report, "ENHANCED_STEREO_GROUP_TYPE"), Is.True);
        Assert.That(HasCode(report, "ENHANCED_STEREO_GROUP_RECORD"), Is.True);
        Assert.That(HasCode(report, "ENHANCED_STEREO_GROUP_MISMATCH"), Is.True);
        Assert.That(report.GetErrorCount(), Is.EqualTo(2));
        Assert.That(report.GetWarningCount(), Is.EqualTo(1));
    }

    [Test]
    public void Validate_ReportsRecordAssignedToMultipleGroupsAndNullMolecule()
    {
        Molecule molecule = BuildMoleculeWithStereoRecords();
        EnhancedStereoGroup first = molecule.StereoGroups.AddGroup("or", "V3000", "one");
        EnhancedStereoGroup second = molecule.StereoGroups.AddGroup("and", "V3000", "two");
        first.StereoRecordIds.Add(0);
        second.StereoRecordIds.Add(0);

        MoleculeReport duplicateReport = molecule.StereoGroups.Validate(molecule);
        MoleculeReport nullReport = molecule.StereoGroups.Validate(null);

        Assert.That(HasCode(duplicateReport, "ENHANCED_STEREO_GROUP_DUPLICATE_RECORD"), Is.True);
        Assert.That(nullReport.GetErrorCount(), Is.EqualTo(1));
        Assert.That(nullReport.Get(0).Code, Is.EqualTo("ENHANCED_STEREO_NULL_MOLECULE"));
    }

    [Test]
    public void CloneMolecule_CopiesEnhancedStereoGroupsIndependently()
    {
        Molecule molecule = BuildMoleculeWithStereoRecords();
        EnhancedStereoGroup group = molecule.StereoGroups.AddGroup("OR", "V3000", "MDLV30/STEREL1");
        molecule.StereoGroups.AssignRecordToGroup(molecule, 0, group.Id);
        molecule.StereoGroups.AssignRecordToGroup(molecule, 1, group.Id);

        Molecule clone = MoleculeBuilder.CloneMolecule(molecule);
        molecule.StereoGroups.Groups[0].StereoRecordIds[0] = 1;
        molecule.StereoGroups.Groups[0].Label = "changed";
        molecule.StereoRecords[0].GroupId = -1;

        Assert.That(clone.StereoGroups, Is.Not.SameAs(molecule.StereoGroups));
        Assert.That(clone.StereoGroups.Groups.Count, Is.EqualTo(1));
        Assert.That(clone.StereoGroups.Groups[0], Is.Not.SameAs(group));
        Assert.That(clone.StereoGroups.Groups[0].Type, Is.EqualTo("or"));
        Assert.That(clone.StereoGroups.Groups[0].Source, Is.EqualTo("V3000"));
        Assert.That(clone.StereoGroups.Groups[0].Label, Is.EqualTo("MDLV30/STEREL1"));
        Assert.That(clone.StereoGroups.Groups[0].StereoRecordIds, Is.EqualTo(new int[] { 0, 1 }));
        Assert.That(clone.StereoRecords[0].GroupId, Is.EqualTo(0));
    }

    static Molecule BuildMoleculeWithStereoRecords()
    {
        Molecule molecule = new Molecule();
        StereoRecord first = new StereoRecord();
        first.Id = 0;
        first.SetTetrahedral(0, 1, 2, 3, 4, Chirality.Clockwise);
        StereoRecord second = new StereoRecord();
        second.Id = 1;
        second.SetDoubleBond(0, 1, 2, DoubleBondConfiguration.E);
        molecule.StereoRecords.Add(first);
        molecule.StereoRecords.Add(second);
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
