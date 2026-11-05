using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class Layout2DTests
{
    [Test]
    public void Layout_AcceptsNullAndEmptyMoleculesAndPlacesAnIsolatedAtom()
    {
        Layout2D layout = new Layout2D();
        Molecule empty = new Molecule();
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 4, 0, 0, false, "", "C"));
        Molecule isolated = builder.Build();

        Assert.DoesNotThrow(() => layout.Layout(null));
        Assert.DoesNotThrow(() => layout.Layout(empty));
        layout.Layout(isolated);

        Assert.That(empty.Atoms, Is.Empty);
        Assert.That(isolated.Atoms[0].HasCoordinates, Is.True);
        Assert.That(isolated.Atoms[0].X, Is.EqualTo(0.0));
        Assert.That(isolated.Atoms[0].Y, Is.EqualTo(0.0));
        Assert.That(isolated.Atoms[0].Z, Is.EqualTo(0.0));
    }

    [Test]
    public void Layout_AssignsLinearCoordinatesForAcyclicChain()
    {
        Molecule molecule = BuildPropane();
        Layout2D layout = new Layout2D();
        Geometry geometry = new Geometry();

        layout.Layout(molecule);

        AssertAllAtomsHave2DCoordinates(molecule);
        Assert.That(molecule.Atoms[0].Y, Is.EqualTo(0.0).Within(0.0000001));
        Assert.That(molecule.Atoms[1].Y, Is.EqualTo(0.0).Within(0.0000001));
        Assert.That(molecule.Atoms[2].Y, Is.EqualTo(0.0).Within(0.0000001));
        Assert.That(molecule.Atoms[1].X, Is.GreaterThan(molecule.Atoms[0].X));
        Assert.That(molecule.Atoms[2].X, Is.GreaterThan(molecule.Atoms[1].X));
        Assert.That(geometry.Distance(AtomCoordinate(molecule, 0), AtomCoordinate(molecule, 1)), Is.EqualTo(layout.BondLength).Within(0.0000001));
        Assert.That(geometry.Distance(AtomCoordinate(molecule, 1), AtomCoordinate(molecule, 2)), Is.EqualTo(layout.BondLength).Within(0.0000001));
    }

    [Test]
    public void Layout_AssignsRegularCoordinatesForRing()
    {
        Molecule molecule = BuildCyclohexane();
        Layout2D layout = new Layout2D();
        Geometry geometry = new Geometry();

        layout.Layout(molecule);

        AssertAllAtomsHave2DCoordinates(molecule);
        Coordinate3D centroid = geometry.CentroidFromConformer(ToConformer(molecule));
        Assert.That(centroid.X, Is.EqualTo(0.0).Within(0.0000001));
        Assert.That(centroid.Y, Is.EqualTo(0.0).Within(0.0000001));
        Assert.That(geometry.Distance(AtomCoordinate(molecule, 0), AtomCoordinate(molecule, 1)), Is.EqualTo(layout.BondLength).Within(0.0000001));
    }

    [Test]
    public void Layout_AssignsCoordinatesForFusedRingSystem()
    {
        Molecule molecule = BuildFusedTriangles();
        Layout2D layout = new Layout2D();

        layout.Layout(molecule);

        AssertAllAtomsHave2DCoordinates(molecule);
        Assert.That(CoordinatesAreDistinct(molecule, 0, 3), Is.True);
        Assert.That(CoordinatesAreDistinct(molecule, 1, 2), Is.True);
    }

    [Test]
    public void Layout_AssignsRingSubstituentsFromPlacedRingAtoms()
    {
        Molecule molecule = BuildCyclohexanol();
        Layout2D layout = new Layout2D();

        layout.Layout(molecule);

        AssertAllAtomsHave2DCoordinates(molecule);
        Assert.That(CoordinatesAreDistinct(molecule, 0, 6), Is.True);
    }

    [Test]
    public void Layout_SeparatesDisconnectedComponents()
    {
        Molecule molecule = BuildDisconnectedEthanes();
        Layout2D layout = new Layout2D();

        layout.Layout(molecule);

        AssertAllAtomsHave2DCoordinates(molecule);
        double firstMax = MaximumX(molecule, 0, 1);
        double secondMin = MinimumX(molecule, 2, 3);
        Assert.That(secondMin, Is.GreaterThan(firstMax + 2.0));
    }

    static Molecule BuildPropane()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbonA = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int carbonB = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        int carbonC = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        builder.AddBond(carbonA, carbonB, new BondSpec());
        builder.AddBond(carbonB, carbonC, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildCyclohexane()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        AddCarbonRingAtoms(builder, 6);
        AddRingBonds(builder, 6);
        return builder.Build();
    }

    static Molecule BuildCyclohexanol()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        AddCarbonRingAtoms(builder, 6);
        AddRingBonds(builder, 6);
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 1, 0, 0, false, "", "O"));
        builder.AddBond(0, oxygen, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildFusedTriangles()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        AddCarbonRingAtoms(builder, 4);
        builder.AddBond(0, 1, new BondSpec());
        builder.AddBond(1, 2, new BondSpec());
        builder.AddBond(2, 0, new BondSpec());
        builder.AddBond(1, 3, new BondSpec());
        builder.AddBond(3, 2, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildDisconnectedEthanes()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        builder.AddBond(0, 1, new BondSpec());
        builder.AddBond(2, 3, new BondSpec());
        return builder.Build();
    }

    static void AddCarbonRingAtoms(MoleculeBuilder builder, int count)
    {
        int index = 0;
        while (index < count)
        {
            builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
            index = index + 1;
        }
    }

    static void AddRingBonds(MoleculeBuilder builder, int count)
    {
        int index = 0;
        while (index < count)
        {
            int next = index + 1;
            if (next == count)
            {
                next = 0;
            }

            builder.AddBond(index, next, new BondSpec());
            index = index + 1;
        }
    }

    static void AssertAllAtomsHave2DCoordinates(Molecule molecule)
    {
        int index = 0;
        while (index < molecule.Atoms.Count)
        {
            Assert.That(molecule.Atoms[index].HasCoordinates, Is.True);
            Assert.That(molecule.Atoms[index].Z, Is.EqualTo(0.0));
            Assert.That(double.IsNaN(molecule.Atoms[index].X), Is.False);
            Assert.That(double.IsNaN(molecule.Atoms[index].Y), Is.False);
            index = index + 1;
        }
    }

    static Coordinate3D AtomCoordinate(Molecule molecule, int atomId)
    {
        Atom atom = molecule.Atoms[atomId];
        return new Coordinate3D(atom.X, atom.Y, atom.Z);
    }

    static Conformer ToConformer(Molecule molecule)
    {
        Conformer conformer = new Conformer(molecule.Atoms.Count);
        int index = 0;
        while (index < molecule.Atoms.Count)
        {
            Atom atom = molecule.Atoms[index];
            if (atom.HasCoordinates)
            {
                conformer.SetAtomCoordinate(index, atom.X, atom.Y, atom.Z);
            }

            index = index + 1;
        }

        return conformer;
    }

    static bool CoordinatesAreDistinct(Molecule molecule, int leftAtomId, int rightAtomId)
    {
        Atom left = molecule.Atoms[leftAtomId];
        Atom right = molecule.Atoms[rightAtomId];
        if (System.Math.Abs(left.X - right.X) > 0.0000001)
        {
            return true;
        }

        if (System.Math.Abs(left.Y - right.Y) > 0.0000001)
        {
            return true;
        }

        return false;
    }

    static double MinimumX(Molecule molecule, int firstAtomId, int secondAtomId)
    {
        double minimum = molecule.Atoms[firstAtomId].X;
        if (molecule.Atoms[secondAtomId].X < minimum)
        {
            minimum = molecule.Atoms[secondAtomId].X;
        }

        return minimum;
    }

    static double MaximumX(Molecule molecule, int firstAtomId, int secondAtomId)
    {
        double maximum = molecule.Atoms[firstAtomId].X;
        if (molecule.Atoms[secondAtomId].X > maximum)
        {
            maximum = molecule.Atoms[secondAtomId].X;
        }

        return maximum;
    }
}
