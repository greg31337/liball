using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class BondOrderPerceiverTests
{
    [Test]
    public void PerceiveBondOrders_AssignsDoubleBondFromScaled2DCoordinatesOnCopy()
    {
        Molecule source = BuildTwoAtomMolecule(6, 8, BondOrder.Unknown);
        SetAtomCoordinates(source, 0, 0.0, 0.0, 7.0);
        SetAtomCoordinates(source, 1, 2.5, 0.0, 7.0);
        BondOrderPerceptionOptions options = new BondOrderPerceptionOptions();
        options.CoordinateDimension = BondOrderCoordinateDimension.TwoDimensional;
        options.CoordinateScale = 0.5;
        IBondOrderPerceiver perceiver = new BondOrderPerceiver();

        BondOrderPerceptionResult result = perceiver.PerceiveBondOrders(source, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Changed, Is.True);
        Assert.That(result.UsedCoordinateDimension, Is.EqualTo(BondOrderCoordinateDimension.TwoDimensional));
        Assert.That(result.AppliedCount, Is.EqualTo(1));
        Assert.That(result.UnresolvedCount, Is.EqualTo(0));
        Assert.That(result.Assignments, Has.Count.EqualTo(1));
        Assert.That(result.Assignments[0].PerceivedOrder, Is.EqualTo(BondOrder.Double));
        Assert.That(result.Assignments[0].Confidence, Is.EqualTo(BondOrderConfidence.High));
        Assert.That(result.Assignments[0].Distance, Is.EqualTo(1.25).Within(0.000001));
        Assert.That(result.Assignments[0].Applied, Is.True);
        Assert.That(result.Molecule.Bonds[0].Order, Is.EqualTo(BondOrder.Double));
        Assert.That(source.Bonds[0].Order, Is.EqualTo(BondOrder.Unknown));
    }

    [Test]
    public void PerceiveBondOrders_AutomaticallyUses3DAndAssignsTripleBond()
    {
        Molecule source = BuildTwoAtomMolecule(6, 6, BondOrder.Unknown);
        SetAtomCoordinates(source, 0, 0.0, 0.0, 0.0);
        SetAtomCoordinates(source, 1, 0.0, 0.0, 1.186);
        IBondOrderPerceiver perceiver = new BondOrderPerceiver();

        BondOrderPerceptionResult result = perceiver.PerceiveBondOrders(source, null);

        Assert.That(result.Success, Is.True);
        Assert.That(result.UsedCoordinateDimension, Is.EqualTo(BondOrderCoordinateDimension.ThreeDimensional));
        Assert.That(result.Assignments[0].PerceivedOrder, Is.EqualTo(BondOrder.Triple));
        Assert.That(result.Assignments[0].Confidence, Is.EqualTo(BondOrderConfidence.High));
        Assert.That(result.Molecule.Bonds[0].Order, Is.EqualTo(BondOrder.Triple));
    }

    [Test]
    public void PerceiveBondOrders_UsesSelectedConformerCoordinates()
    {
        Molecule source = BuildTwoAtomMolecule(6, 6, BondOrder.Unknown);
        SetAtomCoordinates(source, 0, 0.0, 0.0, 0.0);
        SetAtomCoordinates(source, 1, 1.52, 0.0, 0.0);
        Conformer conformer = new Conformer(2);
        conformer.Id = 0;
        conformer.Coordinates[0] = new Coordinate3D(0.0, 0.0, 0.0);
        conformer.Coordinates[1] = new Coordinate3D(1.338, 0.0, 0.0);
        source.Conformers.Add(conformer);
        BondOrderPerceptionOptions options = new BondOrderPerceptionOptions();
        options.ConformerId = 0;
        IBondOrderPerceiver perceiver = new BondOrderPerceiver();

        BondOrderPerceptionResult result = perceiver.PerceiveBondOrders(source, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.UsedCoordinateDimension, Is.EqualTo(BondOrderCoordinateDimension.TwoDimensional));
        Assert.That(result.Assignments[0].PerceivedOrder, Is.EqualTo(BondOrder.Double));
        Assert.That(result.Molecule.Bonds[0].Order, Is.EqualTo(BondOrder.Double));
    }

    [Test]
    public void PerceiveBondOrders_LeavesDistanceMidpointUnresolved()
    {
        Molecule source = BuildTwoAtomMolecule(6, 6, BondOrder.Unknown);
        SetAtomCoordinates(source, 0, 0.0, 0.0, 0.0);
        SetAtomCoordinates(source, 1, 1.4288, 0.0, 0.0);
        IBondOrderPerceiver perceiver = new BondOrderPerceiver();

        BondOrderPerceptionResult result = perceiver.PerceiveBondOrders(source, null);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Changed, Is.False);
        Assert.That(result.UnresolvedCount, Is.EqualTo(1));
        Assert.That(result.Assignments[0].IsAmbiguous, Is.True);
        Assert.That(result.Assignments[0].Applied, Is.False);
        Assert.That(result.Molecule.Bonds[0].Order, Is.EqualTo(BondOrder.Unknown));
        AssertDiagnostic(result.Report, "BOND_ORDER_AMBIGUOUS_DISTANCE", 0);
    }

    [Test]
    public void PerceiveBondOrders_RejectsGeometricProposalThatWouldBeHypervalent()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(NewAtomSpec(6, false));
        builder.AddAtom(NewAtomSpec(1, false));
        builder.AddAtom(NewAtomSpec(1, false));
        builder.AddAtom(NewAtomSpec(1, false));
        builder.AddAtom(NewAtomSpec(8, false));
        builder.AddBond(0, 1, NewBondSpec(BondOrder.Single));
        builder.AddBond(0, 2, NewBondSpec(BondOrder.Single));
        builder.AddBond(0, 3, NewBondSpec(BondOrder.Single));
        builder.AddBond(0, 4, NewBondSpec(BondOrder.Unknown));
        Molecule source = builder.Build();
        SetAtomCoordinates(source, 0, 0.0, 0.0, 0.0);
        SetAtomCoordinates(source, 4, 1.25, 0.0, 0.0);
        IBondOrderPerceiver perceiver = new BondOrderPerceiver();

        BondOrderPerceptionResult result = perceiver.PerceiveBondOrders(source, null);

        Assert.That(result.Success, Is.True);
        Assert.That(result.AppliedCount, Is.EqualTo(0));
        Assert.That(result.Assignments, Has.Count.EqualTo(1));
        Assert.That(result.Assignments[0].PerceivedOrder, Is.EqualTo(BondOrder.Double));
        Assert.That(result.Assignments[0].Confidence, Is.EqualTo(BondOrderConfidence.Low));
        Assert.That(result.Molecule.Bonds[3].Order, Is.EqualTo(BondOrder.Unknown));
        AssertDiagnostic(result.Report, "BOND_ORDER_HYPERVALENT_REJECTED", 3);
    }

    [Test]
    public void PerceiveBondOrders_ReportsAromaticAndMetalCoordinationCasesWithoutMutation()
    {
        Molecule aromatic = BuildTwoAtomMolecule(6, 6, BondOrder.Unknown);
        aromatic.Atoms[0].IsAromatic = true;
        SetAtomCoordinates(aromatic, 0, 0.0, 0.0, 0.0);
        SetAtomCoordinates(aromatic, 1, 1.4, 0.0, 0.0);
        Molecule metal = BuildTwoAtomMolecule(26, 8, BondOrder.Unknown);
        SetAtomCoordinates(metal, 0, 0.0, 0.0, 0.0);
        SetAtomCoordinates(metal, 1, 2.0, 0.0, 0.0);
        IBondOrderPerceiver perceiver = new BondOrderPerceiver();

        BondOrderPerceptionResult aromaticResult = perceiver.PerceiveBondOrders(aromatic, null);
        BondOrderPerceptionResult metalResult = perceiver.PerceiveBondOrders(metal, null);

        Assert.That(aromaticResult.Assignments[0].PerceivedOrder, Is.EqualTo(BondOrder.Aromatic));
        Assert.That(aromaticResult.Assignments[0].Applied, Is.False);
        Assert.That(aromaticResult.Molecule.Bonds[0].Order, Is.EqualTo(BondOrder.Unknown));
        AssertDiagnostic(aromaticResult.Report, "BOND_ORDER_AROMATIC_AMBIGUOUS", 0);
        Assert.That(metalResult.Assignments[0].PerceivedOrder, Is.EqualTo(BondOrder.Dative));
        Assert.That(metalResult.Assignments[0].Applied, Is.False);
        Assert.That(metalResult.Molecule.Bonds[0].Order, Is.EqualTo(BondOrder.Unknown));
        AssertDiagnostic(metalResult.Report, "BOND_ORDER_METAL_COORDINATION", 0);
    }

    [Test]
    public void PerceiveBondOrders_ReportsMissingCoordinatesAndInvalidInputs()
    {
        Molecule missing = BuildTwoAtomMolecule(6, 8, BondOrder.Unknown);
        SetAtomCoordinates(missing, 0, 0.0, 0.0, 0.0);
        BondOrderPerceptionOptions invalidOptions = new BondOrderPerceptionOptions();
        invalidOptions.ConformerId = 2;
        IBondOrderPerceiver perceiver = new BondOrderPerceiver();

        BondOrderPerceptionResult missingResult = perceiver.PerceiveBondOrders(missing, null);
        BondOrderPerceptionResult invalidResult = perceiver.PerceiveBondOrders(missing, invalidOptions);
        BondOrderPerceptionResult nullResult = perceiver.PerceiveBondOrders(null, null);

        Assert.That(missingResult.Success, Is.True);
        Assert.That(missingResult.UnresolvedCount, Is.EqualTo(1));
        AssertDiagnostic(missingResult.Report, "BOND_ORDER_COORDINATE_MISSING", 0);
        Assert.That(invalidResult.Success, Is.False);
        Assert.That(invalidResult.Report.Errors[0].Code, Is.EqualTo("BOND_ORDER_INVALID_CONFORMER"));
        Assert.That(nullResult.Success, Is.False);
        Assert.That(nullResult.Report.Errors[0].Code, Is.EqualTo("BOND_ORDER_NULL_MOLECULE"));
    }

    [Test]
    public void PerceiveBondOrders_PreservesAssignedOrdersByDefault()
    {
        Molecule source = BuildTwoAtomMolecule(6, 6, BondOrder.Single);
        SetAtomCoordinates(source, 0, 0.0, 0.0, 0.0);
        SetAtomCoordinates(source, 1, 1.186, 0.0, 0.0);
        IBondOrderPerceiver perceiver = new BondOrderPerceiver();

        BondOrderPerceptionResult result = perceiver.PerceiveBondOrders(source, null);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Assignments, Is.Empty);
        Assert.That(result.Changed, Is.False);
        Assert.That(result.Molecule.Bonds[0].Order, Is.EqualTo(BondOrder.Single));
    }

    [Test]
    public void PerceiveBondOrders_RejectsEveryInvalidOptionFamily()
    {
        BondOrderPerceptionOptions dimension = new BondOrderPerceptionOptions();
        dimension.CoordinateDimension = (BondOrderCoordinateDimension)99;
        BondOrderPerceptionOptions conformer = new BondOrderPerceptionOptions();
        conformer.ConformerId = -2;
        BondOrderPerceptionOptions scale = new BondOrderPerceptionOptions();
        scale.CoordinateScale = double.NaN;
        BondOrderPerceptionOptions error = new BondOrderPerceptionOptions();
        error.MaximumRelativeError = 2.0;
        BondOrderPerceptionOptions gap = new BondOrderPerceptionOptions();
        gap.MinimumAlternativeGap = -1.0;
        BondOrderPerceptionOptions confidence = new BondOrderPerceptionOptions();
        confidence.MinimumConfidenceToApply = (BondOrderConfidence)99;
        IBondOrderPerceiver perceiver = new BondOrderPerceiver();
        Molecule molecule = new Molecule();

        Assert.That(perceiver.PerceiveBondOrders(molecule, dimension).Report.Errors[0].Code, Is.EqualTo("BOND_ORDER_INVALID_DIMENSION"));
        Assert.That(perceiver.PerceiveBondOrders(molecule, conformer).Report.Errors[0].Code, Is.EqualTo("BOND_ORDER_INVALID_CONFORMER"));
        Assert.That(perceiver.PerceiveBondOrders(molecule, scale).Report.Errors[0].Code, Is.EqualTo("BOND_ORDER_INVALID_SCALE"));
        Assert.That(perceiver.PerceiveBondOrders(molecule, error).Report.Errors[0].Code, Is.EqualTo("BOND_ORDER_INVALID_ERROR_LIMIT"));
        Assert.That(perceiver.PerceiveBondOrders(molecule, gap).Report.Errors[0].Code, Is.EqualTo("BOND_ORDER_INVALID_ALTERNATIVE_GAP"));
        Assert.That(perceiver.PerceiveBondOrders(molecule, confidence).Report.Errors[0].Code, Is.EqualTo("BOND_ORDER_INVALID_CONFIDENCE"));
    }

    static Molecule BuildTwoAtomMolecule(int beginAtomicNumber, int endAtomicNumber, BondOrder order)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(NewAtomSpec(beginAtomicNumber, false));
        builder.AddAtom(NewAtomSpec(endAtomicNumber, false));
        builder.AddBond(0, 1, NewBondSpec(order));
        return builder.Build();
    }

    static AtomSpec NewAtomSpec(int atomicNumber, bool aromatic)
    {
        return new AtomSpec(atomicNumber, 0, 0, 0, 0, 0, 0, aromatic, "", "");
    }

    static BondSpec NewBondSpec(BondOrder order)
    {
        BondSpec spec = new BondSpec();
        spec.Order = order;
        return spec;
    }

    static void SetAtomCoordinates(Molecule molecule, int atomId, double x, double y, double z)
    {
        molecule.Atoms[atomId].HasCoordinates = true;
        molecule.Atoms[atomId].X = x;
        molecule.Atoms[atomId].Y = y;
        molecule.Atoms[atomId].Z = z;
    }

    static void AssertDiagnostic(MoleculeReport report, string code, int bondId)
    {
        int index = 0;
        while (index < report.Errors.Count)
        {
            if (report.Errors[index].Code == code)
            {
                Assert.That(report.Errors[index].BondId, Is.EqualTo(bondId));
                Assert.That(report.Errors[index].Severity, Is.EqualTo(ErrorSeverity.Warning));
                return;
            }

            index = index + 1;
        }

        Assert.Fail("Expected diagnostic " + code + ".");
    }
}
