using System.Collections.Generic;
using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class GeometryTests
{
    [Test]
    public void Distance_Measures2DAnd3DCoordinates()
    {
        Geometry geometry = new Geometry();

        double distance3D = geometry.Distance(new Coordinate3D(0.0, 0.0, 0.0), new Coordinate3D(2.0, 3.0, 6.0));
        double distance2D = geometry.Distance2D(new Coordinate2D(0.0, 0.0), new Coordinate2D(3.0, 4.0));

        Assert.That(distance3D, Is.EqualTo(7.0).Within(0.0000001));
        Assert.That(distance2D, Is.EqualTo(5.0).Within(0.0000001));
    }

    [Test]
    public void Angle_ReturnsDegreesAtCenterAtom()
    {
        Geometry geometry = new Geometry();

        double angle = geometry.Angle(new Coordinate3D(1.0, 0.0, 0.0), new Coordinate3D(0.0, 0.0, 0.0), new Coordinate3D(0.0, 1.0, 0.0));

        Assert.That(angle, Is.EqualTo(90.0).Within(0.0000001));
    }

    [Test]
    public void Torsion_ReturnsSignedDegrees()
    {
        Geometry geometry = new Geometry();

        double torsion = geometry.Torsion(new Coordinate3D(1.0, 0.0, 0.0), new Coordinate3D(0.0, 0.0, 0.0), new Coordinate3D(0.0, 1.0, 0.0), new Coordinate3D(0.0, 1.0, 1.0));

        Assert.That(torsion, Is.EqualTo(90.0).Within(0.0000001));
    }

    [Test]
    public void Centroid_IgnoresUnsetCoordinates()
    {
        Geometry geometry = new Geometry();
        Conformer conformer = new Conformer(3);
        conformer.SetAtomCoordinate(0, 0.0, 0.0, 0.0);
        conformer.SetAtomCoordinate(2, 2.0, 4.0, 6.0);

        Coordinate3D centroid = geometry.CentroidFromConformer(conformer);

        Assert.That(centroid.IsSet, Is.True);
        Assert.That(centroid.X, Is.EqualTo(1.0).Within(0.0000001));
        Assert.That(centroid.Y, Is.EqualTo(2.0).Within(0.0000001));
        Assert.That(centroid.Z, Is.EqualTo(3.0).Within(0.0000001));
    }

    [Test]
    public void BoundingBox_IgnoresUnsetCoordinatesAndReportsExtents()
    {
        Geometry geometry = new Geometry();
        Conformer conformer = new Conformer(3);
        conformer.SetAtomCoordinate(0, -1.0, 3.0, 2.0);
        conformer.SetAtomCoordinate(2, 4.0, -2.0, 8.0);

        BoundingBox3D box = geometry.BoundingBoxFromConformer(conformer);

        Assert.That(box.IsSet, Is.True);
        Assert.That(box.Minimum.X, Is.EqualTo(-1.0));
        Assert.That(box.Minimum.Y, Is.EqualTo(-2.0));
        Assert.That(box.Minimum.Z, Is.EqualTo(2.0));
        Assert.That(box.Maximum.X, Is.EqualTo(4.0));
        Assert.That(box.Maximum.Y, Is.EqualTo(3.0));
        Assert.That(box.Maximum.Z, Is.EqualTo(8.0));
        Assert.That(box.Width(), Is.EqualTo(5.0));
        Assert.That(box.Height(), Is.EqualTo(5.0));
        Assert.That(box.Depth(), Is.EqualTo(6.0));
    }

    [Test]
    public void Rmsd_UsesCoordinatesPresentInBothConformers()
    {
        Geometry geometry = new Geometry();
        Conformer left = new Conformer(3);
        Conformer right = new Conformer(3);
        left.SetAtomCoordinate(0, 0.0, 0.0, 0.0);
        left.SetAtomCoordinate(1, 2.0, 0.0, 0.0);
        right.SetAtomCoordinate(0, 0.0, 0.0, 0.0);
        right.SetAtomCoordinate(1, 4.0, 0.0, 0.0);
        right.SetAtomCoordinate(2, 10.0, 10.0, 10.0);

        double rmsd = geometry.Rmsd(left, right);

        Assert.That(rmsd, Is.EqualTo(System.Math.Sqrt(2.0)).Within(0.0000001));
    }

    [Test]
    public void EmptyAggregates_ReturnUnsetOrZeroValues()
    {
        Geometry geometry = new Geometry();

        Coordinate3D centroid = geometry.CentroidFromConformer(Conformer.CreateEmpty());
        BoundingBox3D box = geometry.BoundingBoxFromConformer(Conformer.CreateEmpty());

        Assert.That(centroid.IsSet, Is.False);
        Assert.That(box.IsSet, Is.False);
        Assert.That(box.Width(), Is.EqualTo(0.0));
        Assert.That(geometry.Rmsd(Conformer.CreateEmpty(), Conformer.CreateEmpty()), Is.EqualTo(0.0));
    }

    [Test]
    public void AngleAndTorsion_ReturnZeroForDegenerateCoordinates()
    {
        Geometry geometry = new Geometry();
        Coordinate3D origin = new Coordinate3D(0.0, 0.0, 0.0);

        double angle = geometry.Angle(origin, origin, new Coordinate3D(1.0, 0.0, 0.0));
        double torsion = geometry.Torsion(origin, new Coordinate3D(1.0, 0.0, 0.0), new Coordinate3D(2.0, 0.0, 0.0), new Coordinate3D(3.0, 0.0, 0.0));

        Assert.That(angle, Is.EqualTo(0.0));
        Assert.That(torsion, Is.EqualTo(0.0));
    }

    [Test]
    public void AggregateEntryPoints_ReturnUnsetValuesForNullInputs()
    {
        Geometry geometry = new Geometry();

        Coordinate3D listCentroid = geometry.Centroid(null);
        Coordinate3D conformerCentroid = geometry.CentroidFromConformer(null);
        BoundingBox3D listBox = geometry.BoundingBox(null);
        BoundingBox3D conformerBox = geometry.BoundingBoxFromConformer(null);

        Assert.That(listCentroid.IsSet, Is.False);
        Assert.That(conformerCentroid.IsSet, Is.False);
        Assert.That(listBox.IsSet, Is.False);
        Assert.That(conformerBox.IsSet, Is.False);
        Assert.That(geometry.Rmsd(null, new Conformer(1)), Is.EqualTo(0.0));
    }

    [Test]
    public void AggregateListEntryPointsIgnoreUnsetValuesAndRmsdUsesShorterConformer()
    {
        Geometry geometry = new Geometry();
        List<Coordinate3D> coordinates = new List<Coordinate3D>();
        coordinates.Add(Coordinate3D.CreateEmpty());
        coordinates.Add(new Coordinate3D(-2.0, 3.0, 5.0));
        coordinates.Add(new Coordinate3D(4.0, -1.0, 9.0));
        Conformer left = new Conformer(2);
        Conformer right = new Conformer(1);
        left.SetAtomCoordinate(0, 3.0, 4.0, 0.0);
        left.SetAtomCoordinate(1, 100.0, 100.0, 100.0);
        right.SetAtomCoordinate(0, 0.0, 0.0, 0.0);

        Coordinate3D centroid = geometry.Centroid(coordinates);
        BoundingBox3D box = geometry.BoundingBox(coordinates);

        Assert.That(centroid.X, Is.EqualTo(1.0));
        Assert.That(centroid.Y, Is.EqualTo(1.0));
        Assert.That(centroid.Z, Is.EqualTo(7.0));
        Assert.That(box.Minimum.X, Is.EqualTo(-2.0));
        Assert.That(box.Maximum.Z, Is.EqualTo(9.0));
        Assert.That(geometry.Rmsd(left, right), Is.EqualTo(5.0));
    }
}
