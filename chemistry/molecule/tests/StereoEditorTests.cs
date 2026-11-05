using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class StereoEditorTests
{
    [Test]
    public void CreateTetrahedralStereo_AddsValidatedTypedRecordOnCopy()
    {
        Molecule source = BuildTetrahedralMolecule();
        IStereoEditor editor = new StereoEditor();

        StereoEditResult result = editor.CreateTetrahedralStereo(source, 0, new int[] { 1, 2, 3, 4 }, Chirality.Clockwise);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Changed, Is.True);
        Assert.That(result.StereoRecordId, Is.EqualTo(0));
        Assert.That(result.StereoKind, Is.EqualTo(StereoKind.Tetrahedral));
        Assert.That(result.Report.Errors, Is.Empty);
        Assert.That(result.Molecule.StereoRecords, Has.Count.EqualTo(1));
        Assert.That(result.Molecule.StereoRecords[0].Type, Is.EqualTo(StereoKind.Tetrahedral));
        Assert.That(result.Molecule.StereoRecords[0].Kind, Is.EqualTo("tetrahedral"));
        Assert.That(result.Molecule.StereoRecords[0].LigandAtomIds, Is.EqualTo(new int[] { 1, 2, 3, 4 }));
        Assert.That(result.Molecule.StereoRecords[0].Chirality, Is.EqualTo(Chirality.Clockwise));
        Assert.That(result.Molecule.StereoRecords[0].Source, Is.EqualTo("editor"));
        Assert.That(source.StereoRecords, Is.Empty);
    }

    [Test]
    public void CreateDoubleBondStereo_AddsValidatedTypedRecordOnCopy()
    {
        Molecule source = BuildSubstitutedAlkene();
        IStereoEditor editor = new StereoEditor();

        StereoEditResult result = editor.CreateDoubleBondStereo(source, 0, 2, 3, DoubleBondConfiguration.E);

        Assert.That(result.Success, Is.True);
        Assert.That(result.StereoKind, Is.EqualTo(StereoKind.DoubleBond));
        Assert.That(result.Molecule.StereoRecords[0].Type, Is.EqualTo(StereoKind.DoubleBond));
        Assert.That(result.Molecule.StereoRecords[0].StereoBondId, Is.EqualTo(0));
        Assert.That(result.Molecule.StereoRecords[0].BeginLigandAtomId, Is.EqualTo(2));
        Assert.That(result.Molecule.StereoRecords[0].EndLigandAtomId, Is.EqualTo(3));
        Assert.That(result.Molecule.StereoRecords[0].Configuration, Is.EqualTo(DoubleBondConfiguration.E));
        Assert.That(source.StereoRecords, Is.Empty);
    }

    [Test]
    public void InvertAndMarkUnspecified_EditTetrahedralStereoWithoutMutatingInputs()
    {
        IStereoEditor editor = new StereoEditor();
        StereoEditResult created = editor.CreateTetrahedralStereo(BuildTetrahedralMolecule(), 0, new int[] { 1, 2, 3, 4 }, Chirality.Clockwise);

        StereoEditResult inverted = editor.InvertStereo(created.Molecule, 0);
        StereoEditResult unspecified = editor.MarkStereoUnspecified(inverted.Molecule, 0);
        StereoEditResult cannotInvert = editor.InvertStereo(unspecified.Molecule, 0);

        Assert.That(inverted.Success, Is.True);
        Assert.That(inverted.Molecule.StereoRecords[0].Chirality, Is.EqualTo(Chirality.CounterClockwise));
        Assert.That(created.Molecule.StereoRecords[0].Chirality, Is.EqualTo(Chirality.Clockwise));
        Assert.That(unspecified.Success, Is.True);
        Assert.That(unspecified.Changed, Is.True);
        Assert.That(unspecified.Molecule.StereoRecords[0].Chirality, Is.EqualTo(Chirality.Unspecified));
        Assert.That(inverted.Molecule.StereoRecords[0].Chirality, Is.EqualTo(Chirality.CounterClockwise));
        Assert.That(cannotInvert.Success, Is.False);
        Assert.That(cannotInvert.Changed, Is.False);
        AssertDiagnostic(cannotInvert.Report, "STEREO_EDIT_NOT_INVERTIBLE");
    }

    [Test]
    public void InvertAndMarkUnspecified_EditDoubleBondConfigurations()
    {
        IStereoEditor editor = new StereoEditor();
        StereoEditResult created = editor.CreateDoubleBondStereo(BuildSubstitutedAlkene(), 0, 2, 3, DoubleBondConfiguration.E);

        StereoEditResult inverted = editor.InvertStereo(created.Molecule, 0);
        StereoEditResult unspecified = editor.MarkStereoUnspecified(inverted.Molecule, 0);

        Assert.That(inverted.Success, Is.True);
        Assert.That(inverted.Molecule.StereoRecords[0].Configuration, Is.EqualTo(DoubleBondConfiguration.Z));
        Assert.That(created.Molecule.StereoRecords[0].Configuration, Is.EqualTo(DoubleBondConfiguration.E));
        Assert.That(unspecified.Success, Is.True);
        Assert.That(unspecified.Molecule.StereoRecords[0].Configuration, Is.EqualTo(DoubleBondConfiguration.Unspecified));
    }

    [Test]
    public void PlaceholderOperations_CreateTypedAlleneAtropisomericAndNonTetrahedralRecords()
    {
        Molecule source = BuildPlaceholderMolecule();
        IStereoEditor editor = new StereoEditor();

        StereoEditResult allene = editor.CreateAlleneStereoPlaceholder(source, 0, new int[] { 1, 2 });
        StereoEditResult atropisomer = editor.CreateAtropisomericStereoPlaceholder(allene.Molecule, 0, new int[] { 0, 1, 2 });
        StereoEditResult nonTetrahedral = editor.CreateNonTetrahedralStereoPlaceholder(atropisomer.Molecule, 2, new int[] { 0, 1 });
        MoleculeReport validation = new StereoValidator().Validate(nonTetrahedral.Molecule);

        Assert.That(allene.Success, Is.True);
        Assert.That(atropisomer.Success, Is.True);
        Assert.That(nonTetrahedral.Success, Is.True);
        Assert.That(nonTetrahedral.Molecule.StereoRecords, Has.Count.EqualTo(3));
        AssertPlaceholder(nonTetrahedral.Molecule.StereoRecords[0], StereoKind.Allene, "allene");
        AssertPlaceholder(nonTetrahedral.Molecule.StereoRecords[1], StereoKind.Atropisomeric, "atropisomeric");
        AssertPlaceholder(nonTetrahedral.Molecule.StereoRecords[2], StereoKind.NonTetrahedral, "non-tetrahedral");
        Assert.That(nonTetrahedral.Molecule.StereoRecords[0].CenterAtomId, Is.EqualTo(0));
        Assert.That(nonTetrahedral.Molecule.StereoRecords[1].StereoBondId, Is.EqualTo(0));
        Assert.That(nonTetrahedral.Molecule.StereoRecords[2].LigandAtomIds, Is.EqualTo(new int[] { 0, 1 }));
        Assert.That(validation.Errors, Is.Empty);
        Assert.That(source.StereoRecords, Is.Empty);
    }

    [Test]
    public void RemoveStereo_RepairsRecordAndEnhancedGroupIdentifiersOnCopy()
    {
        Molecule source = new Molecule();
        StereoRecord first = new StereoRecord();
        first.Id = 0;
        first.Type = StereoKind.Tetrahedral;
        first.Kind = "tetrahedral";
        first.GroupId = 0;
        StereoRecord second = new StereoRecord();
        second.Id = 1;
        second.Type = StereoKind.DoubleBond;
        second.Kind = "double-bond";
        second.GroupId = 1;
        source.StereoRecords.Add(first);
        source.StereoRecords.Add(second);
        EnhancedStereoGroup firstGroup = NewStereoGroup(0, 0);
        EnhancedStereoGroup secondGroup = NewStereoGroup(1, 1);
        source.StereoGroups.Groups.Add(firstGroup);
        source.StereoGroups.Groups.Add(secondGroup);
        IStereoEditor editor = new StereoEditor();

        StereoEditResult result = editor.RemoveStereo(source, 0);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Changed, Is.True);
        Assert.That(result.StereoKind, Is.EqualTo(StereoKind.Tetrahedral));
        Assert.That(result.Molecule.StereoRecords, Has.Count.EqualTo(1));
        Assert.That(result.Molecule.StereoRecords[0].Id, Is.EqualTo(0));
        Assert.That(result.Molecule.StereoRecords[0].GroupId, Is.EqualTo(0));
        Assert.That(result.Molecule.StereoGroups.Groups, Has.Count.EqualTo(1));
        Assert.That(result.Molecule.StereoGroups.Groups[0].Id, Is.EqualTo(0));
        Assert.That(result.Molecule.StereoGroups.Groups[0].StereoRecordIds, Is.EqualTo(new int[] { 0 }));
        Assert.That(source.StereoRecords, Has.Count.EqualTo(2));
        Assert.That(source.StereoGroups.Groups, Has.Count.EqualTo(2));
    }

    [Test]
    public void CreateOperations_RejectInvalidRecordsAndDuplicateFoci()
    {
        IStereoEditor editor = new StereoEditor();
        Molecule tetrahedral = BuildTetrahedralMolecule();
        Molecule alkene = BuildSubstitutedAlkene();
        Molecule singleBond = BuildPlaceholderMolecule();

        StereoEditResult invalidTetrahedral = editor.CreateTetrahedralStereo(tetrahedral, 0, new int[] { 1, 1, 3, 4 }, Chirality.Clockwise);
        StereoEditResult invalidDoubleBond = editor.CreateDoubleBondStereo(singleBond, 0, 0, 2, DoubleBondConfiguration.E);
        StereoEditResult invalidPlaceholder = editor.CreateAlleneStereoPlaceholder(singleBond, 99, new int[] { 0, 1 });
        StereoEditResult created = editor.CreateDoubleBondStereo(alkene, 0, 2, 3, DoubleBondConfiguration.Z);
        StereoEditResult duplicate = editor.CreateDoubleBondStereo(created.Molecule, 0, 2, 3, DoubleBondConfiguration.E);

        Assert.That(invalidTetrahedral.Success, Is.False);
        Assert.That(invalidTetrahedral.Molecule.StereoRecords, Is.Empty);
        AssertDiagnostic(invalidTetrahedral.Report, "TETRAHEDRAL_DUPLICATE_LIGAND");
        Assert.That(invalidDoubleBond.Success, Is.False);
        AssertDiagnostic(invalidDoubleBond.Report, "DOUBLE_BOND_ORDER");
        Assert.That(invalidPlaceholder.Success, Is.False);
        AssertDiagnostic(invalidPlaceholder.Report, "EXTENDED_STEREO_INVALID_CENTER");
        Assert.That(duplicate.Success, Is.False);
        Assert.That(duplicate.Molecule.StereoRecords, Has.Count.EqualTo(1));
        AssertDiagnostic(duplicate.Report, "STEREO_EDIT_DUPLICATE_FOCUS");
    }

    [Test]
    public void EditOperations_ReportNullMoleculeAndInvalidRecordIdentifiers()
    {
        IStereoEditor editor = new StereoEditor();

        StereoEditResult nullResult = editor.CreateAlleneStereoPlaceholder(null, 0, null);
        StereoEditResult invalidInvert = editor.InvertStereo(new Molecule(), 0);
        StereoEditResult invalidMark = editor.MarkStereoUnspecified(new Molecule(), -1);
        StereoEditResult invalidRemove = editor.RemoveStereo(new Molecule(), 2);

        Assert.That(nullResult.Success, Is.False);
        AssertDiagnostic(nullResult.Report, "STEREO_EDIT_NULL_MOLECULE");
        AssertDiagnostic(invalidInvert.Report, "STEREO_EDIT_INVALID_RECORD");
        AssertDiagnostic(invalidMark.Report, "STEREO_EDIT_INVALID_RECORD");
        AssertDiagnostic(invalidRemove.Report, "STEREO_EDIT_INVALID_RECORD");
    }

    [Test]
    public void CreateOperations_RejectNullLigandsAndUnspecifiedOrientations()
    {
        IStereoEditor editor = new StereoEditor();
        Molecule tetrahedral = BuildTetrahedralMolecule();
        Molecule alkene = BuildSubstitutedAlkene();

        StereoEditResult nullTetrahedral = editor.CreateTetrahedralStereo(null, 0, null, Chirality.Clockwise);
        StereoEditResult missingLigands = editor.CreateTetrahedralStereo(tetrahedral, 0, null, Chirality.Clockwise);
        StereoEditResult shortLigands = editor.CreateTetrahedralStereo(tetrahedral, 0, new int[] { 1, 2, 3 }, Chirality.Clockwise);
        StereoEditResult unspecifiedChirality = editor.CreateTetrahedralStereo(tetrahedral, 0, new int[] { 1, 2, 3, 4 }, Chirality.Unspecified);
        StereoEditResult nullDoubleBond = editor.CreateDoubleBondStereo(null, 0, 2, 3, DoubleBondConfiguration.E);
        StereoEditResult unspecifiedConfiguration = editor.CreateDoubleBondStereo(alkene, 0, 2, 3, DoubleBondConfiguration.Unspecified);

        AssertDiagnostic(nullTetrahedral.Report, "STEREO_EDIT_NULL_MOLECULE");
        AssertDiagnostic(missingLigands.Report, "STEREO_EDIT_TETRAHEDRAL_LIGANDS");
        AssertDiagnostic(shortLigands.Report, "STEREO_EDIT_TETRAHEDRAL_LIGANDS");
        AssertDiagnostic(unspecifiedChirality.Report, "STEREO_EDIT_TETRAHEDRAL_CHIRALITY");
        AssertDiagnostic(nullDoubleBond.Report, "STEREO_EDIT_NULL_MOLECULE");
        AssertDiagnostic(unspecifiedConfiguration.Report, "STEREO_EDIT_DOUBLE_BOND_CONFIGURATION");
    }

    [Test]
    public void InvertStereo_RejectsUnspecifiedAndUnsupportedRecordsWithoutMutatingSource()
    {
        IStereoEditor editor = new StereoEditor();
        StereoEditResult placeholder = editor.CreateAlleneStereoPlaceholder(BuildPlaceholderMolecule(), 1, new int[] { 0, 2 });
        Molecule unknown = BuildPlaceholderMolecule();
        StereoRecord unknownRecord = new StereoRecord();
        unknownRecord.Id = 0;
        unknownRecord.Kind = "future";
        unknown.StereoRecords.Add(unknownRecord);

        StereoEditResult unspecified = editor.InvertStereo(placeholder.Molecule, 0);
        StereoEditResult unsupported = editor.InvertStereo(unknown, 0);

        AssertDiagnostic(unspecified.Report, "STEREO_EDIT_NOT_INVERTIBLE");
        AssertDiagnostic(unsupported.Report, "STEREO_EDIT_UNSUPPORTED_RECORD");
        Assert.That(placeholder.Molecule.StereoRecords[0].Chirality, Is.EqualTo(Chirality.Unspecified));
        Assert.That(unknown.StereoRecords[0].Kind, Is.EqualTo("future"));
    }

    static Molecule BuildTetrahedralMolecule()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(NewAtomSpec(6));
        int fluorine = builder.AddAtom(NewAtomSpec(9));
        int chlorine = builder.AddAtom(NewAtomSpec(17));
        int bromine = builder.AddAtom(NewAtomSpec(35));
        int iodine = builder.AddAtom(NewAtomSpec(53));
        builder.AddBond(carbon, fluorine, new BondSpec());
        builder.AddBond(carbon, chlorine, new BondSpec());
        builder.AddBond(carbon, bromine, new BondSpec());
        builder.AddBond(carbon, iodine, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildSubstitutedAlkene()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int left = builder.AddAtom(NewAtomSpec(6));
        int right = builder.AddAtom(NewAtomSpec(6));
        int chlorine = builder.AddAtom(NewAtomSpec(17));
        int bromine = builder.AddAtom(NewAtomSpec(35));
        BondSpec doubleBond = new BondSpec();
        doubleBond.Order = BondOrder.Double;
        builder.AddBond(left, right, doubleBond);
        builder.AddBond(left, chlorine, new BondSpec());
        builder.AddBond(right, bromine, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildPlaceholderMolecule()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(NewAtomSpec(6));
        builder.AddAtom(NewAtomSpec(6));
        builder.AddAtom(NewAtomSpec(6));
        builder.AddBond(0, 1, new BondSpec());
        builder.AddBond(1, 2, new BondSpec());
        return builder.Build();
    }

    static AtomSpec NewAtomSpec(int atomicNumber)
    {
        return new AtomSpec(atomicNumber, 0, 0, 0, 0, 0, 0, false, "", "");
    }

    static EnhancedStereoGroup NewStereoGroup(int groupId, int stereoRecordId)
    {
        EnhancedStereoGroup group = new EnhancedStereoGroup();
        group.Id = groupId;
        group.Type = "abs";
        group.StereoRecordIds.Add(stereoRecordId);
        return group;
    }

    static void AssertPlaceholder(StereoRecord record, StereoKind type, string kind)
    {
        Assert.That(record.Type, Is.EqualTo(type));
        Assert.That(record.Kind, Is.EqualTo(kind));
        Assert.That(record.IsPlaceholder, Is.True);
        Assert.That(record.Chirality, Is.EqualTo(Chirality.Unspecified));
    }

    static void AssertDiagnostic(MoleculeReport report, string code)
    {
        int index = 0;
        while (index < report.Errors.Count)
        {
            if (report.Errors[index].Code == code)
            {
                return;
            }

            index = index + 1;
        }

        Assert.Fail("Expected diagnostic " + code + ".");
    }
}
