using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class CoordinateConformerTests
{
    [Test]
    public void Coordinate2D_StoresClearsAndClonesValues()
    {
        Coordinate2D coordinate = new Coordinate2D(1.5, -2.0);

        Coordinate2D clone = coordinate.Clone();
        coordinate.Clear();

        Assert.That(clone.IsSet, Is.True);
        Assert.That(clone.X, Is.EqualTo(1.5));
        Assert.That(clone.Y, Is.EqualTo(-2.0));
        Assert.That(coordinate.IsSet, Is.False);
    }

    [Test]
    public void Coordinate3D_StoresClearsClonesAndProjectsTo2D()
    {
        Coordinate3D coordinate = new Coordinate3D(1.0, 2.0, 3.0);

        Coordinate3D clone = coordinate.Clone();
        Coordinate2D projected = coordinate.ToCoordinate2D();
        coordinate.Clear();

        Assert.That(clone.IsSet, Is.True);
        Assert.That(clone.Z, Is.EqualTo(3.0));
        Assert.That(projected.IsSet, Is.True);
        Assert.That(projected.X, Is.EqualTo(1.0));
        Assert.That(projected.Y, Is.EqualTo(2.0));
        Assert.That(coordinate.IsSet, Is.False);
    }

    [Test]
    public void Conformer_StoresCoordinatesByAtomIndex()
    {
        Conformer conformer = new Conformer(2);

        conformer.SetAtomCoordinate(1, 1.0, 2.0, 3.0);

        Assert.That(conformer.Coordinates.Count, Is.EqualTo(2));
        Assert.That(conformer.HasAtomCoordinate(0), Is.False);
        Assert.That(conformer.HasAtomCoordinate(1), Is.True);
        Assert.That(conformer.GetAtomCoordinate(1).Y, Is.EqualTo(2.0));
        Assert.That(conformer.CountAssignedCoordinates(), Is.EqualTo(1));

        conformer.ClearAtomCoordinate(1);

        Assert.That(conformer.HasAtomCoordinate(1), Is.False);
        Assert.That(conformer.CountAssignedCoordinates(), Is.EqualTo(0));
    }

    [Test]
    public void Molecule_AddConformerClonesInputAssignsIdAndEnsuresAtomCount()
    {
        MoleculeBuilder builder = BuildCarbonOxygenBuilder();
        Conformer conformer = Conformer.CreateEmpty();
        conformer.Name = "first";
        conformer.HasEnergy = true;
        conformer.Energy = -1.25;
        conformer.SetAtomCoordinate(0, 0.0, 0.0, 0.0);

        int conformerId = builder.Molecule.AddConformer(conformer);
        conformer.SetAtomCoordinate(0, 9.0, 9.0, 9.0);

        Conformer stored = builder.Molecule.GetConformer(conformerId);
        Assert.That(conformerId, Is.EqualTo(0));
        Assert.That(stored.Id, Is.EqualTo(0));
        Assert.That(stored.Name, Is.EqualTo("first"));
        Assert.That(stored.HasEnergy, Is.True);
        Assert.That(stored.Energy, Is.EqualTo(-1.25));
        Assert.That(stored.Coordinates.Count, Is.EqualTo(2));
        Assert.That(stored.GetAtomCoordinate(0).X, Is.EqualTo(0.0));
        Assert.That(builder.Molecule.GetConformerCount(), Is.EqualTo(1));
    }

    [Test]
    public void MoleculeBuilder_ClonePreservesConformerCoordinatesWithoutAliasing()
    {
        MoleculeBuilder builder = BuildCarbonOxygenBuilder();
        Conformer conformer = new Conformer(2);
        conformer.SetAtomCoordinate(0, 0.0, 0.0, 0.0);
        conformer.SetAtomCoordinate(1, 1.2, 0.0, 0.0);
        builder.Molecule.AddConformer(conformer);

        Molecule clone = builder.Clone();
        builder.Molecule.Conformers[0].SetAtomCoordinate(1, 9.0, 9.0, 9.0);

        Assert.That(clone.Conformers.Count, Is.EqualTo(1));
        Assert.That(clone.Conformers[0].Coordinates.Count, Is.EqualTo(2));
        Assert.That(clone.Conformers[0].GetAtomCoordinate(1).X, Is.EqualTo(1.2));
        Assert.That(clone.Conformers[0].GetAtomCoordinate(1), Is.Not.SameAs(builder.Molecule.Conformers[0].GetAtomCoordinate(1)));
    }

    [Test]
    public void MoleculeBuilder_AddAndRemoveAtomKeepConformerCoordinatesAligned()
    {
        MoleculeBuilder builder = BuildCarbonOxygenBuilder();
        Conformer conformer = new Conformer(2);
        conformer.SetAtomCoordinate(0, 0.0, 0.0, 0.0);
        conformer.SetAtomCoordinate(1, 1.0, 0.0, 0.0);
        builder.Molecule.AddConformer(conformer);

        builder.AddAtom(new AtomSpec(7, 0, 0, 0, 2, 0, 0, false, "", "N"));

        Assert.That(builder.Molecule.Conformers[0].Coordinates.Count, Is.EqualTo(3));
        Assert.That(builder.Molecule.Conformers[0].HasAtomCoordinate(2), Is.False);

        builder.RemoveAtom(0);

        Assert.That(builder.Molecule.Atoms.Count, Is.EqualTo(2));
        Assert.That(builder.Molecule.Conformers[0].Coordinates.Count, Is.EqualTo(2));
        Assert.That(builder.Molecule.Conformers[0].GetAtomCoordinate(0).X, Is.EqualTo(1.0));
    }

    static MoleculeBuilder BuildCarbonOxygenBuilder()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 1, 0, 0, false, "", "O"));
        builder.AddBond(carbon, oxygen, new BondSpec());
        return builder;
    }
}
