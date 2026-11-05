using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class MoleculeValidationTests
{
    [Test]
    public void Validate_ReturnsNoErrorsForValidMolecule()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 4, 0, 0, false, "", "C"));
        MoleculeValidation validation = new MoleculeValidation();

        MoleculeReport report = validation.Validate(builder.Build());

        Assert.That(report.GetCount(), Is.EqualTo(0));
        Assert.That(report.HasErrors(), Is.False);
    }

    [Test]
    public void Validate_ReportsNullAndEmptyMolecules()
    {
        MoleculeValidation validation = new MoleculeValidation();

        MoleculeReport nullReport = validation.Validate(null);
        MoleculeReport emptyReport = validation.Validate(new Molecule());

        Assert.That(HasCode(nullReport, "NULL_MOLECULE"), Is.True);
        Assert.That(HasCode(emptyReport, "EMPTY_MOLECULE"), Is.True);
        Assert.That(emptyReport.GetErrorCount(), Is.EqualTo(1));
    }

    [Test]
    public void Validate_ReportsInvalidAtomsAndCharges()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(999, 0, 99, 0, 0, 0, 0, false, "", "Xx"));
        MoleculeValidation validation = new MoleculeValidation();

        MoleculeReport report = validation.Validate(builder.Build());

        Assert.That(HasCode(report, "INVALID_ATOM"), Is.True);
        Assert.That(HasCode(report, "INVALID_CHARGE"), Is.True);
        Assert.That(FindCode(report, "INVALID_ATOM").AtomId, Is.EqualTo(0));
        Assert.That(FindCode(report, "INVALID_CHARGE").AtomId, Is.EqualTo(0));
    }

    [Test]
    public void Validate_ReportsInvalidSelfAndUndefinedBonds()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        Molecule molecule = builder.Build();
        molecule.Bonds.Add(Bond.CreateWithOrder(0, carbon, carbon, BondOrder.Single));
        molecule.Bonds.Add(Bond.CreateWithOrder(1, carbon, 99, BondOrder.Single));
        molecule.Bonds.Add(Bond.CreateWithOrder(2, carbon, carbon, BondOrder.Unknown));
        MoleculeValidation validation = new MoleculeValidation();

        MoleculeReport report = validation.Validate(molecule);

        Assert.That(HasCode(report, "SELF_BOND"), Is.True);
        Assert.That(HasCode(report, "INVALID_BOND_ATOM"), Is.True);
        Assert.That(HasCode(report, "INVALID_BOND_ORDER"), Is.True);
        Assert.That(FindCode(report, "INVALID_BOND_ATOM").BondId, Is.EqualTo(1));
        Assert.That(FindCode(report, "INVALID_BOND_ORDER").BondId, Is.EqualTo(2));
    }

    [Test]
    public void Validate_ReportsDuplicateBonds()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int oxygenA = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        int oxygenB = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        builder.AddBond(oxygenA, oxygenB, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        builder.AddBond(oxygenB, oxygenA, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        MoleculeValidation validation = new MoleculeValidation();

        MoleculeReport report = validation.Validate(builder.Build());

        Assert.That(HasCode(report, "DUPLICATE_BOND"), Is.True);
        Assert.That(FindCode(report, "DUPLICATE_BOND").BondId, Is.EqualTo(1));
    }

    [Test]
    public void Validate_ReportsValenceErrors()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 5, 0, 0, false, "", "C"));
        MoleculeValidation validation = new MoleculeValidation();

        MoleculeReport report = validation.Validate(builder.Build());

        Assert.That(HasCode(report, "VALENCE"), Is.True);
        Assert.That(FindCode(report, "VALENCE").AtomId, Is.EqualTo(0));
    }

    [Test]
    public void ValidateWithOptions_AppliesConfigurableEmptyMoleculePolicy()
    {
        MoleculeValidation validation = new MoleculeValidation();
        MoleculeValidationOptions warningOptions = new MoleculeValidationOptions();
        warningOptions.EmptyMoleculePolicy = EmptyMoleculePolicy.Warning;
        MoleculeValidationOptions allowOptions = new MoleculeValidationOptions();
        allowOptions.EmptyMoleculePolicy = EmptyMoleculePolicy.Allow;
        MoleculeValidationOptions invalidOptions = new MoleculeValidationOptions();
        invalidOptions.EmptyMoleculePolicy = (EmptyMoleculePolicy)99;

        MoleculeReport warning = validation.ValidateWithOptions(new Molecule(), warningOptions);
        MoleculeReport allowed = validation.ValidateWithOptions(new Molecule(), allowOptions);
        MoleculeReport invalid = validation.ValidateWithOptions(new Molecule(), invalidOptions);

        Assert.That(HasCode(warning, "EMPTY_MOLECULE"), Is.True);
        Assert.That(FindCode(warning, "EMPTY_MOLECULE").Severity, Is.EqualTo(ErrorSeverity.Warning));
        Assert.That(warning.HasErrors(), Is.False);
        Assert.That(allowed.GetCount(), Is.EqualTo(0));
        Assert.That(HasCode(invalid, "VALIDATION_OPTIONS_INVALID"), Is.True);
    }

    [Test]
    public void Validate_ReportsAromaticityAndUnsupportedElementInconsistencies()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int first = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 1, 0, 0, true, "", "C"));
        int second = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        builder.AddBond(first, second, BondSpec.Create(BondOrder.Single, true, BondStereo.None, BondDirection.None));
        Molecule molecule = builder.Build();
        molecule.Atoms.Add(new Atom(2, new AtomSpec(999, 0, 0, 0, 0, 0, 0, true, "", "X")));
        MoleculeValidation validation = new MoleculeValidation();

        MoleculeReport report = validation.Validate(molecule);

        Assert.That(HasCode(report, "AROMATICITY_BOND_MISMATCH"), Is.True);
        Assert.That(HasCode(report, "AROMATICITY_ATOM_BOND_MISMATCH"), Is.True);
        Assert.That(HasCode(report, "AROMATICITY_ATOM_WITHOUT_BOND"), Is.True);
        Assert.That(HasCode(report, "UNSUPPORTED_ELEMENT"), Is.True);
        Assert.That(FindCode(report, "UNSUPPORTED_ELEMENT").AtomId, Is.EqualTo(2));
    }

    [Test]
    public void Validate_ReportsInvalidStereoDepictionAndStereoRecords()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int first = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        int second = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        builder.AddBond(first, second, BondSpec.Create(BondOrder.Double, false, BondStereo.Wedge, BondDirection.Crossed));
        Molecule molecule = builder.Build();
        StereoRecord record = new StereoRecord();
        record.Kind = "tetrahedral";
        record.Type = StereoKind.Tetrahedral;
        record.CenterAtomId = 99;
        molecule.StereoRecords.Add(record);
        MoleculeValidation validation = new MoleculeValidation();

        MoleculeReport report = validation.Validate(molecule);

        Assert.That(HasCode(report, "STEREO_DEPICTION_SINGLE_BOND_REQUIRED"), Is.True);
        Assert.That(HasCode(report, "STEREO_DEPICTION_COORDINATES_MISSING"), Is.True);
        Assert.That(HasCode(report, "STEREO_DEPICTION_CONFLICT"), Is.True);
        Assert.That(HasCode(report, "TETRAHEDRAL_INVALID_CENTER"), Is.True);
    }

    [Test]
    public void Validate_ReportsImpossibleNonfiniteAndMismatchedCoordinateData()
    {
        Molecule nonfinite = BuildCoordinateBond();
        nonfinite.Atoms[0].X = double.NaN;
        Molecule impossible = BuildCoordinateBond();
        impossible.Atoms[0].X = 1000000001.0;
        Molecule coincident = BuildCoordinateBond();
        coincident.Atoms[1].X = coincident.Atoms[0].X;
        coincident.Atoms[1].Y = coincident.Atoms[0].Y;
        coincident.Atoms[1].Z = coincident.Atoms[0].Z;
        Molecule conformerMolecule = BuildCoordinateBond();
        Conformer conformer = new Conformer(3);
        conformer.SetAtomCoordinate(0, 0.0, 0.0, double.PositiveInfinity);
        conformer.SetAtomCoordinate(1, 0.0, 1000000001.0, 0.0);
        conformerMolecule.Conformers.Add(conformer);
        MoleculeValidation validation = new MoleculeValidation();

        MoleculeReport nonfiniteReport = validation.Validate(nonfinite);
        MoleculeReport impossibleReport = validation.Validate(impossible);
        MoleculeReport coincidentReport = validation.Validate(coincident);
        MoleculeReport conformerReport = validation.Validate(conformerMolecule);

        Assert.That(HasCode(nonfiniteReport, "NONFINITE_COORDINATE"), Is.True);
        Assert.That(HasCode(impossibleReport, "IMPOSSIBLE_COORDINATE"), Is.True);
        Assert.That(HasCode(coincidentReport, "IMPOSSIBLE_BOND_COORDINATES"), Is.True);
        Assert.That(HasCode(conformerReport, "CONFORMER_SIZE_MISMATCH"), Is.True);
        Assert.That(HasCode(conformerReport, "NONFINITE_CONFORMER_COORDINATE"), Is.True);
        Assert.That(HasCode(conformerReport, "IMPOSSIBLE_CONFORMER_COORDINATE"), Is.True);
    }

    [Test]
    public void ValidateQuery_ChecksDisconnectedComponentGroupConstraints()
    {
        MoleculeValidation validation = new MoleculeValidation();
        SmartsParser parser = new SmartsParser();
        QueryMolecule valid = parser.Parse("([#6].[#8])");
        QueryMolecule conflict = parser.Parse("C-C");
        conflict.ComponentGroupIdsByAtomId[0] = 0;
        conflict.ComponentGroupIdsByAtomId[1] = 1;
        conflict.Atoms[0].ComponentGroupId = 0;
        conflict.Atoms[1].ComponentGroupId = 1;
        conflict.Atoms[1].AtomicNumber = 999;
        QueryMolecule mismatched = parser.Parse("C.O");
        mismatched.ComponentGroupIdsByAtomId.RemoveAt(1);

        MoleculeReport validReport = validation.ValidateQuery(valid);
        MoleculeReport conflictReport = validation.ValidateQuery(conflict);
        MoleculeReport mismatchedReport = validation.ValidateQuery(mismatched);

        Assert.That(validReport.GetCount(), Is.EqualTo(0));
        Assert.That(HasCode(conflictReport, "QUERY_COMPONENT_GROUP_BOND_CONFLICT"), Is.True);
        Assert.That(HasCode(conflictReport, "QUERY_UNSUPPORTED_ELEMENT"), Is.True);
        Assert.That(HasCode(mismatchedReport, "QUERY_COMPONENT_GROUP_SIZE"), Is.True);
    }

    [Test]
    public void ValidateWithOptions_PropagatesParserOriginatedWarningsWithLocations()
    {
        SmilesParser parser = new SmilesParser();
        SmilesParseResult parseResult = parser.ParseWithRecovery("C)");
        MoleculeValidationOptions options = new MoleculeValidationOptions();
        options.ParserReport = parseResult.Report;
        MoleculeValidation validation = new MoleculeValidation();

        MoleculeReport report = validation.ValidateWithOptions(parseResult.Molecule, options);

        MoleculeError warning = FindCode(report, "SMILES_UNMATCHED_BRANCH_CLOSE");
        Assert.That(warning.Code, Is.EqualTo("SMILES_UNMATCHED_BRANCH_CLOSE"));
        Assert.That(warning.Severity, Is.EqualTo(ErrorSeverity.Warning));
        Assert.That(warning.Location.SourceName, Is.EqualTo("SMILES"));
        Assert.That(warning.Location.Context, Is.EqualTo("C)"));
        Assert.That(warning, Is.Not.SameAs(parseResult.Report.Errors[0]));
    }

    [Test]
    public void GetReportSeverity_MapsNullInfoWarningErrorAndFatalDiagnostics()
    {
        MoleculeValidation validation = new MoleculeValidation();
        MoleculeError info = new MoleculeError("I", "info", null, -1, -1, ErrorSeverity.Info);
        MoleculeError warning = new MoleculeError("W", "warning", null, -1, -1, ErrorSeverity.Warning);
        MoleculeError error = new MoleculeError("E", "error", null, -1, -1, ErrorSeverity.Error);
        MoleculeError fatal = new MoleculeError("F", "fatal", null, -1, -1, ErrorSeverity.Fatal);

        Assert.That(validation.GetReportSeverity(null), Is.EqualTo(ReportSeverity.Info));
        Assert.That(validation.GetReportSeverity(info), Is.EqualTo(ReportSeverity.Info));
        Assert.That(validation.GetReportSeverity(warning), Is.EqualTo(ReportSeverity.Warning));
        Assert.That(validation.GetReportSeverity(error), Is.EqualTo(ReportSeverity.Error));
        Assert.That(validation.GetReportSeverity(fatal), Is.EqualTo(ReportSeverity.Fatal));
    }

    [Test]
    public void Validate_ReportsNullConformerAndNullConformerCoordinateEntries()
    {
        Molecule nullConformerMolecule = BuildCoordinateBond();
        nullConformerMolecule.Conformers.Add(null!);
        Molecule nullCoordinateMolecule = BuildCoordinateBond();
        Conformer conformer = new Conformer(2);
        conformer.Coordinates[0] = null!;
        nullCoordinateMolecule.Conformers.Add(conformer);
        MoleculeValidation validation = new MoleculeValidation();

        MoleculeReport nullConformer = validation.Validate(nullConformerMolecule);
        MoleculeReport nullCoordinate = validation.Validate(nullCoordinateMolecule);

        Assert.That(HasCode(nullConformer, "NULL_CONFORMER"), Is.True);
        Assert.That(HasCode(nullCoordinate, "NULL_CONFORMER_COORDINATE"), Is.True);
    }

    [Test]
    public void Validate_ReportsEveryBondStereoDepictionTypeMismatch()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int first = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int second = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int third = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int fourth = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        builder.AddBond(first, second, BondSpec.Create(BondOrder.Single, false, BondStereo.E, BondDirection.None));
        builder.AddBond(second, third, BondSpec.Create(BondOrder.Double, false, BondStereo.Unknown, BondDirection.Up));
        builder.AddBond(third, fourth, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.Crossed));

        MoleculeReport report = new MoleculeValidation().Validate(builder.Build());

        Assert.That(HasCode(report, "STEREO_DEPICTION_DOUBLE_BOND_REQUIRED"), Is.True);
        Assert.That(HasCode(report, "STEREO_DEPICTION_UNKNOWN"), Is.True);
        Assert.That(HasCode(report, "STEREO_DIRECTION_SINGLE_BOND_REQUIRED"), Is.True);
        Assert.That(HasCode(report, "STEREO_DIRECTION_DOUBLE_BOND_REQUIRED"), Is.True);
    }

    [Test]
    public void ValidateQuery_ReportsNullEmptyMalformedMembersAndGroupConstraints()
    {
        MoleculeValidation validation = new MoleculeValidation();
        MoleculeReport nullReport = validation.ValidateQuery(null);
        MoleculeReport emptyReport = validation.ValidateQuery(new QueryMolecule());
        QueryMolecule malformed = new SmartsParser().Parse("C.C.C");
        malformed.Atoms[0] = null!;
        malformed.Atoms[1].AtomicNumbers.Add(999);
        malformed.ComponentGroupIdsByAtomId[1] = -2;
        malformed.Atoms[1].ComponentGroupId = 0;
        malformed.ComponentGroupIdsByAtomId[2] = 2;
        malformed.Atoms[2].ComponentGroupId = 2;
        malformed.Bonds.Add(null!);
        QueryBond invalidBond = new QueryBond();
        invalidBond.BeginQueryAtomId = 0;
        invalidBond.EndQueryAtomId = 99;
        malformed.Bonds.Add(invalidBond);

        MoleculeReport malformedReport = validation.ValidateQuery(malformed);

        Assert.That(HasCode(nullReport, "NULL_QUERY"), Is.True);
        Assert.That(HasCode(emptyReport, "EMPTY_QUERY"), Is.True);
        Assert.That(HasCode(malformedReport, "QUERY_NULL_ATOM"), Is.True);
        Assert.That(HasCode(malformedReport, "QUERY_UNSUPPORTED_ELEMENT"), Is.True);
        Assert.That(HasCode(malformedReport, "QUERY_COMPONENT_GROUP_INVALID"), Is.True);
        Assert.That(HasCode(malformedReport, "QUERY_COMPONENT_GROUP_MISMATCH"), Is.True);
        Assert.That(HasCode(malformedReport, "QUERY_NULL_BOND"), Is.True);
        Assert.That(HasCode(malformedReport, "QUERY_INVALID_BOND_ATOM"), Is.True);
        Assert.That(HasCode(malformedReport, "QUERY_COMPONENT_GROUP_GAP"), Is.True);
    }

    static Molecule BuildCoordinateBond()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int first = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int second = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 1, 0, 0, false, "", "O"));
        builder.AddBond(first, second, new BondSpec());
        Molecule molecule = builder.Build();
        SetCoordinate(molecule.Atoms[0], 0.0, 0.0, 0.0);
        SetCoordinate(molecule.Atoms[1], 1.0, 0.0, 0.0);
        return molecule;
    }

    static void SetCoordinate(Atom atom, double x, double y, double z)
    {
        atom.HasCoordinates = true;
        atom.X = x;
        atom.Y = y;
        atom.Z = z;
    }

    static bool HasCode(MoleculeReport report, string code)
    {
        return FindCode(report, code).Code == code;
    }

    static MoleculeError FindCode(MoleculeReport report, string code)
    {
        int index = 0;
        while (index < report.Errors.Count)
        {
            if (report.Errors[index].Code == code)
            {
                return report.Errors[index];
            }

            index = index + 1;
        }

        return MoleculeError.CreateEmpty();
    }
}
