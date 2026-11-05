using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class CoordinateOperationsTests
{
    [Test]
    public void Normalize2D_CentersRotatesAndScalesAverageBondLengthWithoutMutatingInput()
    {
        CoordinateOperations operations = new CoordinateOperations();
        Molecule molecule = BuildVerticalChain();
        CoordinateNormalizationOptions options = new CoordinateNormalizationOptions();
        options.ScalePolicy = CoordinateNormalizationScalePolicy.AverageBondLength;
        options.TargetAverageBondLength = 1.5;

        CoordinateNormalizationResult result = operations.Normalize2D(molecule, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.CoordinateCount, Is.EqualTo(3));
        Assert.That(result.TranslationX, Is.EqualTo(-4.0).Within(0.0000000001));
        Assert.That(result.TranslationY, Is.EqualTo(-3.0).Within(0.0000000001));
        Assert.That(result.RotationDegrees, Is.EqualTo(90.0).Within(0.0000000001));
        Assert.That(result.Scale, Is.EqualTo(0.75).Within(0.0000000001));
        Assert.That(result.Molecule.Atoms[0].X, Is.EqualTo(1.5).Within(0.0000000001));
        Assert.That(result.Molecule.Atoms[0].Y, Is.EqualTo(0.0).Within(0.0000000001));
        Assert.That(result.Molecule.Atoms[1].X, Is.EqualTo(0.0).Within(0.0000000001));
        Assert.That(result.Molecule.Atoms[2].X, Is.EqualTo(-1.5).Within(0.0000000001));
        Assert.That(result.Molecule.Atoms[0].Z, Is.EqualTo(7.0));
        Assert.That(molecule.Atoms[0].X, Is.EqualTo(4.0));
        Assert.That(molecule.Atoms[0].Y, Is.EqualTo(1.0));
    }

    [Test]
    public void Normalize2D_PreservesScaleAndLeavesUnsetCoordinatesUntouched()
    {
        CoordinateOperations operations = new CoordinateOperations();
        Molecule molecule = BuildVerticalChain();
        molecule.Atoms[1].HasCoordinates = false;
        molecule.Atoms[1].X = 20.0;
        molecule.Atoms[1].Y = 30.0;

        CoordinateNormalizationResult result = operations.Normalize2D(molecule, null);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Scale, Is.EqualTo(1.0));
        Assert.That(result.CoordinateCount, Is.EqualTo(2));
        Assert.That(Distance2D(result.Molecule.Atoms[0], result.Molecule.Atoms[2]), Is.EqualTo(4.0).Within(0.0000000001));
        Assert.That(result.Molecule.Atoms[1].HasCoordinates, Is.False);
        Assert.That(result.Molecule.Atoms[1].X, Is.EqualTo(20.0));
        Assert.That(result.Molecule.Atoms[1].Y, Is.EqualTo(30.0));
    }

    [Test]
    public void Normalize2D_ReturnsStableFailureDiagnostics()
    {
        CoordinateOperations operations = new CoordinateOperations();
        Molecule noCoordinates = BuildVerticalChain();
        int index = 0;
        while (index < noCoordinates.Atoms.Count)
        {
            noCoordinates.Atoms[index].HasCoordinates = false;
            index = index + 1;
        }

        CoordinateNormalizationOptions invalidOptions = new CoordinateNormalizationOptions();
        invalidOptions.ScalePolicy = CoordinateNormalizationScalePolicy.AverageBondLength;
        invalidOptions.TargetAverageBondLength = 0.0;
        CoordinateNormalizationOptions averageOptions = new CoordinateNormalizationOptions();
        averageOptions.ScalePolicy = CoordinateNormalizationScalePolicy.AverageBondLength;
        Molecule noMeasurableBond = new MoleculeBuilder().Build();
        noMeasurableBond.Atoms.Add(new Atom(0, new AtomSpec(6, 0, 0, 0, 4, 0, 0, false, "", "C")));
        SetAtomCoordinate(noMeasurableBond.Atoms[0], 1.0, 2.0, 0.0);
        Molecule nonfinite = BuildVerticalChain();
        nonfinite.Atoms[1].X = double.NaN;

        CoordinateNormalizationResult nullResult = operations.Normalize2D(null, null);
        CoordinateNormalizationResult emptyResult = operations.Normalize2D(noCoordinates, null);
        CoordinateNormalizationResult invalidResult = operations.Normalize2D(BuildVerticalChain(), invalidOptions);
        CoordinateNormalizationResult scaleResult = operations.Normalize2D(noMeasurableBond, averageOptions);
        CoordinateNormalizationResult nonfiniteResult = operations.Normalize2D(nonfinite, null);

        Assert.That(nullResult.Report.Errors[0].Code, Is.EqualTo("COORDINATE_NORMALIZATION_NULL_MOLECULE"));
        Assert.That(emptyResult.Report.Errors[0].Code, Is.EqualTo("COORDINATE_NORMALIZATION_NO_COORDINATES"));
        Assert.That(invalidResult.Report.Errors[0].Code, Is.EqualTo("COORDINATE_NORMALIZATION_OPTIONS_INVALID"));
        Assert.That(scaleResult.Report.Errors[0].Code, Is.EqualTo("COORDINATE_NORMALIZATION_SCALE_UNAVAILABLE"));
        Assert.That(nonfiniteResult.Report.Errors[0].Code, Is.EqualTo("COORDINATE_NORMALIZATION_NONFINITE"));
        Assert.That(nullResult.Success || emptyResult.Success || invalidResult.Success || scaleResult.Success || nonfiniteResult.Success, Is.False);
    }

    [Test]
    public void AlignConformer_UsesExplicitRenumberedMappingRotationTranslationAndBestFitScale()
    {
        CoordinateOperations operations = new CoordinateOperations();
        Conformer reference = BuildReferenceConformer();
        Conformer moving = BuildScaledRotatedRenumberedConformer();
        ConformerAlignmentOptions options = BuildRenumberedOptions();
        options.ScalePolicy = ConformerAlignmentScalePolicy.BestFit;

        ConformerAlignmentResult result = operations.AlignConformer(moving, reference, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.AtomMappings.Count, Is.EqualTo(4));
        Assert.That(result.AtomMappings[0].MovingAtomId, Is.EqualTo(0));
        Assert.That(result.AtomMappings[0].ReferenceAtomId, Is.EqualTo(2));
        Assert.That(result.Scale, Is.EqualTo(0.5).Within(0.000000001));
        Assert.That(result.RotationXX, Is.EqualTo(0.0).Within(0.000000001));
        Assert.That(result.RotationXY, Is.EqualTo(1.0).Within(0.000000001));
        Assert.That(result.RotationYX, Is.EqualTo(-1.0).Within(0.000000001));
        Assert.That(result.RotationYY, Is.EqualTo(0.0).Within(0.000000001));
        Assert.That(result.RotationZZ, Is.EqualTo(1.0).Within(0.000000001));
        Assert.That(result.TranslationX, Is.EqualTo(1.5).Within(0.000000001));
        Assert.That(result.TranslationY, Is.EqualTo(2.5).Within(0.000000001));
        Assert.That(result.TranslationZ, Is.EqualTo(-2.0).Within(0.000000001));
        Assert.That(result.Rmsd, Is.EqualTo(0.0).Within(0.000000001));
        AssertMappedCoordinates(result, reference);
        Assert.That(moving.Coordinates[0].X, Is.EqualTo(1.0));
        Assert.That(moving.Coordinates[0].Y, Is.EqualTo(-3.0));
    }

    [Test]
    public void AlignConformer_DefaultMappingPreservesScaleAndIsDeterministic()
    {
        CoordinateOperations operations = new CoordinateOperations();
        Conformer reference = BuildReferenceConformer();
        Conformer moving = BuildRigidlyTransformedConformer();

        ConformerAlignmentResult first = operations.AlignConformer(moving, reference, null);
        ConformerAlignmentResult second = operations.AlignConformer(moving, reference, null);

        Assert.That(first.Success, Is.True);
        Assert.That(first.Scale, Is.EqualTo(1.0));
        Assert.That(first.AtomMappings.Count, Is.EqualTo(4));
        Assert.That(first.Rmsd, Is.EqualTo(0.0).Within(0.000000001));
        Assert.That(first.RotationXX, Is.EqualTo(second.RotationXX));
        Assert.That(first.RotationXY, Is.EqualTo(second.RotationXY));
        Assert.That(first.TranslationX, Is.EqualTo(second.TranslationX));
        Assert.That(first.TranslationY, Is.EqualTo(second.TranslationY));
        Assert.That(first.TranslationZ, Is.EqualTo(second.TranslationZ));
        int index = 0;
        while (index < reference.Coordinates.Count)
        {
            AssertCoordinate(first.Conformer.Coordinates[index], reference.Coordinates[index]);
            index = index + 1;
        }
    }

    [Test]
    public void AlignConformer_ReturnsMappingCoordinateAndScaleFailureDiagnostics()
    {
        CoordinateOperations operations = new CoordinateOperations();
        Conformer reference = BuildReferenceConformer();
        Conformer moving = BuildRigidlyTransformedConformer();
        ConformerAlignmentOptions invalidMapping = new ConformerAlignmentOptions();
        invalidMapping.AtomMappings.Add(new ConformerAtomMapping(20, 0));
        ConformerAlignmentOptions duplicateMapping = new ConformerAlignmentOptions();
        duplicateMapping.AtomMappings.Add(new ConformerAtomMapping(0, 0));
        duplicateMapping.AtomMappings.Add(new ConformerAtomMapping(0, 1));
        Conformer missing = BuildRigidlyTransformedConformer();
        missing.Coordinates[0].IsSet = false;
        ConformerAlignmentOptions missingMapping = new ConformerAlignmentOptions();
        missingMapping.AtomMappings.Add(new ConformerAtomMapping(0, 0));
        Conformer coincident = new Conformer(2);
        coincident.SetAtomCoordinate(0, 1.0, 1.0, 1.0);
        coincident.SetAtomCoordinate(1, 1.0, 1.0, 1.0);
        Conformer coincidentReference = new Conformer(2);
        coincidentReference.SetAtomCoordinate(0, 0.0, 0.0, 0.0);
        coincidentReference.SetAtomCoordinate(1, 1.0, 0.0, 0.0);
        ConformerAlignmentOptions bestFit = new ConformerAlignmentOptions();
        bestFit.ScalePolicy = ConformerAlignmentScalePolicy.BestFit;
        Conformer nonfinite = BuildRigidlyTransformedConformer();
        nonfinite.Coordinates[2].Z = double.PositiveInfinity;

        ConformerAlignmentResult nullResult = operations.AlignConformer(null, reference, null);
        ConformerAlignmentResult emptyResult = operations.AlignConformer(new Conformer(0), new Conformer(0), null);
        ConformerAlignmentResult invalidResult = operations.AlignConformer(moving, reference, invalidMapping);
        ConformerAlignmentResult duplicateResult = operations.AlignConformer(moving, reference, duplicateMapping);
        ConformerAlignmentResult missingResult = operations.AlignConformer(missing, reference, missingMapping);
        ConformerAlignmentResult scaleResult = operations.AlignConformer(coincident, coincidentReference, bestFit);
        ConformerAlignmentResult nonfiniteResult = operations.AlignConformer(nonfinite, reference, null);

        Assert.That(nullResult.Report.Errors[0].Code, Is.EqualTo("CONFORMER_ALIGNMENT_NULL_CONFORMER"));
        Assert.That(emptyResult.Report.Errors[0].Code, Is.EqualTo("CONFORMER_ALIGNMENT_NO_MAPPING"));
        Assert.That(invalidResult.Report.Errors[0].Code, Is.EqualTo("CONFORMER_ALIGNMENT_INVALID_MAPPING"));
        Assert.That(duplicateResult.Report.Errors[0].Code, Is.EqualTo("CONFORMER_ALIGNMENT_DUPLICATE_MAPPING"));
        Assert.That(missingResult.Report.Errors[0].Code, Is.EqualTo("CONFORMER_ALIGNMENT_COORDINATE_MISSING"));
        Assert.That(scaleResult.Report.Errors[0].Code, Is.EqualTo("CONFORMER_ALIGNMENT_SCALE_UNAVAILABLE"));
        Assert.That(nonfiniteResult.Report.Errors[0].Code, Is.EqualTo("CONFORMER_ALIGNMENT_NONFINITE"));
        Assert.That(nullResult.Success || emptyResult.Success || invalidResult.Success || duplicateResult.Success || missingResult.Success || scaleResult.Success || nonfiniteResult.Success, Is.False);
    }

    [Test]
    public void Normalize2D_RejectsUnknownScalePolicyWithoutMutatingInput()
    {
        Molecule molecule = BuildVerticalChain();
        CoordinateNormalizationOptions options = new CoordinateNormalizationOptions();
        options.ScalePolicy = (CoordinateNormalizationScalePolicy)99;

        CoordinateNormalizationResult result = new CoordinateOperations().Normalize2D(molecule, options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Report.Errors[0].Code, Is.EqualTo("COORDINATE_NORMALIZATION_OPTIONS_INVALID"));
        Assert.That(result.Molecule, Is.Not.SameAs(molecule));
        Assert.That(result.Molecule.Atoms[0].X, Is.EqualTo(4.0));
        Assert.That(molecule.Atoms[0].X, Is.EqualTo(4.0));
    }

    [Test]
    public void AlignConformer_RejectsUnknownPolicyDuplicateReferenceAndNonfiniteReferenceCoordinates()
    {
        CoordinateOperations operations = new CoordinateOperations();
        Conformer moving = BuildRigidlyTransformedConformer();
        Conformer reference = BuildReferenceConformer();
        ConformerAlignmentOptions invalidPolicy = new ConformerAlignmentOptions();
        invalidPolicy.ScalePolicy = (ConformerAlignmentScalePolicy)99;
        ConformerAlignmentOptions duplicateReference = new ConformerAlignmentOptions();
        duplicateReference.AtomMappings.Add(new ConformerAtomMapping(0, 0));
        duplicateReference.AtomMappings.Add(new ConformerAtomMapping(1, 0));
        Conformer nonfiniteReference = BuildReferenceConformer();
        nonfiniteReference.Coordinates[1].Y = double.NaN;
        ConformerAlignmentOptions explicitNonfinite = new ConformerAlignmentOptions();
        explicitNonfinite.AtomMappings.Add(new ConformerAtomMapping(1, 1));

        ConformerAlignmentResult policyResult = operations.AlignConformer(moving, reference, invalidPolicy);
        ConformerAlignmentResult duplicateResult = operations.AlignConformer(moving, reference, duplicateReference);
        ConformerAlignmentResult nonfiniteResult = operations.AlignConformer(moving, nonfiniteReference, null);
        ConformerAlignmentResult explicitNonfiniteResult = operations.AlignConformer(moving, nonfiniteReference, explicitNonfinite);

        Assert.That(policyResult.Report.Errors[0].Code, Is.EqualTo("CONFORMER_ALIGNMENT_OPTIONS_INVALID"));
        Assert.That(duplicateResult.Report.Errors[0].Code, Is.EqualTo("CONFORMER_ALIGNMENT_DUPLICATE_MAPPING"));
        Assert.That(nonfiniteResult.Report.Errors[0].Code, Is.EqualTo("CONFORMER_ALIGNMENT_NONFINITE"));
        Assert.That(explicitNonfiniteResult.Report.Errors[0].Code, Is.EqualTo("CONFORMER_ALIGNMENT_NONFINITE"));
        Assert.That(policyResult.Success || duplicateResult.Success || nonfiniteResult.Success || explicitNonfiniteResult.Success, Is.False);
        Assert.That(moving.Coordinates[0].X, Is.EqualTo(5.0));
    }

    [Test]
    public void AlignConformer_DefaultMappingUsesSharedCoordinateRangeWhenSizesDiffer()
    {
        Conformer moving = new Conformer(2);
        moving.SetAtomCoordinate(0, 5.0, 6.0, 7.0);
        moving.SetAtomCoordinate(1, 20.0, 30.0, 40.0);
        Conformer reference = new Conformer(1);
        reference.SetAtomCoordinate(0, 1.0, 2.0, 3.0);

        ConformerAlignmentResult result = new CoordinateOperations().AlignConformer(moving, reference, null);

        Assert.That(result.Success, Is.True);
        Assert.That(result.AtomMappings, Has.Count.EqualTo(1));
        Assert.That(result.Rmsd, Is.EqualTo(0.0));
        AssertCoordinate(result.Conformer.Coordinates[0], reference.Coordinates[0]);
        Assert.That(result.Conformer.Coordinates[1].X, Is.EqualTo(16.0));
        Assert.That(moving.Coordinates[0].X, Is.EqualTo(5.0));
    }

    static Molecule BuildVerticalChain()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int first = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int second = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        int third = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 1, 0, 0, false, "", "O"));
        builder.AddBond(first, second, new BondSpec());
        builder.AddBond(second, third, new BondSpec());
        Molecule molecule = builder.Build();
        SetAtomCoordinate(molecule.Atoms[0], 4.0, 1.0, 7.0);
        SetAtomCoordinate(molecule.Atoms[1], 4.0, 3.0, 8.0);
        SetAtomCoordinate(molecule.Atoms[2], 4.0, 5.0, 9.0);
        return molecule;
    }

    static Conformer BuildReferenceConformer()
    {
        Conformer conformer = new Conformer(4);
        conformer.SetAtomCoordinate(0, 0.0, 0.0, 0.0);
        conformer.SetAtomCoordinate(1, 1.0, 0.0, 0.0);
        conformer.SetAtomCoordinate(2, 0.0, 2.0, 0.0);
        conformer.SetAtomCoordinate(3, 0.0, 0.0, 1.0);
        return conformer;
    }

    static Conformer BuildScaledRotatedRenumberedConformer()
    {
        Conformer conformer = new Conformer(4);
        conformer.SetAtomCoordinate(0, 1.0, -3.0, 4.0);
        conformer.SetAtomCoordinate(1, 5.0, -3.0, 4.0);
        conformer.SetAtomCoordinate(2, 5.0, -3.0, 6.0);
        conformer.SetAtomCoordinate(3, 5.0, -1.0, 4.0);
        return conformer;
    }

    static Conformer BuildRigidlyTransformedConformer()
    {
        Conformer conformer = new Conformer(4);
        conformer.SetAtomCoordinate(0, 5.0, -3.0, 4.0);
        conformer.SetAtomCoordinate(1, 5.0, -2.0, 4.0);
        conformer.SetAtomCoordinate(2, 3.0, -3.0, 4.0);
        conformer.SetAtomCoordinate(3, 5.0, -3.0, 5.0);
        return conformer;
    }

    static ConformerAlignmentOptions BuildRenumberedOptions()
    {
        ConformerAlignmentOptions options = new ConformerAlignmentOptions();
        options.AtomMappings.Add(new ConformerAtomMapping(0, 2));
        options.AtomMappings.Add(new ConformerAtomMapping(1, 0));
        options.AtomMappings.Add(new ConformerAtomMapping(2, 3));
        options.AtomMappings.Add(new ConformerAtomMapping(3, 1));
        return options;
    }

    static void AssertMappedCoordinates(ConformerAlignmentResult result, Conformer reference)
    {
        int index = 0;
        while (index < result.AtomMappings.Count)
        {
            ConformerAtomMapping mapping = result.AtomMappings[index];
            AssertCoordinate(result.Conformer.Coordinates[mapping.MovingAtomId], reference.Coordinates[mapping.ReferenceAtomId]);
            index = index + 1;
        }
    }

    static void AssertCoordinate(Coordinate3D actual, Coordinate3D expected)
    {
        Assert.That(actual.X, Is.EqualTo(expected.X).Within(0.000000001));
        Assert.That(actual.Y, Is.EqualTo(expected.Y).Within(0.000000001));
        Assert.That(actual.Z, Is.EqualTo(expected.Z).Within(0.000000001));
    }

    static void SetAtomCoordinate(Atom atom, double x, double y, double z)
    {
        atom.HasCoordinates = true;
        atom.X = x;
        atom.Y = y;
        atom.Z = z;
    }

    static double Distance2D(Atom first, Atom second)
    {
        double dx = second.X - first.X;
        double dy = second.Y - first.Y;
        return System.Math.Sqrt(dx * dx + dy * dy);
    }
}
