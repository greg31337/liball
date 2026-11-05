using geo;

namespace geo.Tests;

public class RelationMatrixAlgorithmsTests
{
    [Test]
    public void DefaultMatrixHasCanonicalAllFalseText()
    {
        RelationMatrix matrix = new RelationMatrix();

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(() => RelationMatrixAlgorithms.ValidateMatrix(matrix));
            Assert.That(RelationMatrixAlgorithms.GetCanonicalText(matrix),
                Is.EqualTo("FFFFFFFFF"));
            Assert.That(RelationMatrixAlgorithms.MatchesPattern(matrix, "FF*FF****"),
                Is.True);
            Assert.That(RelationMatrixAlgorithms.MatchesPattern(matrix, "T********"),
                Is.False);
        });
    }

    [Test]
    public void CellAccessUsesInteriorBoundaryExteriorRowMajorOrder()
    {
        RelationMatrix matrix = Matrix("F012F10F2");

        Assert.Multiple(() =>
        {
            Assert.That(RelationMatrixAlgorithms.GetCell(
                matrix, TopologicalLocation.Interior, TopologicalLocation.Interior),
                Is.EqualTo(RelationDimension.False));
            Assert.That(RelationMatrixAlgorithms.GetCell(
                matrix, TopologicalLocation.Interior, TopologicalLocation.Boundary),
                Is.EqualTo(RelationDimension.Point));
            Assert.That(RelationMatrixAlgorithms.GetCell(
                matrix, TopologicalLocation.Boundary, TopologicalLocation.Interior),
                Is.EqualTo(RelationDimension.Surface));
            Assert.That(RelationMatrixAlgorithms.GetCell(
                matrix, TopologicalLocation.Exterior, TopologicalLocation.Exterior),
                Is.EqualTo(RelationDimension.Surface));
            Assert.That(RelationMatrixAlgorithms.GetCanonicalText(matrix),
                Is.EqualTo("F012F10F2"));
        });
    }

    [Test]
    public void ExactAndAtLeastCellUpdatesHaveDistinctSemantics()
    {
        RelationMatrix matrix = new RelationMatrix();

        RelationMatrixAlgorithms.SetCellAtLeast(
            matrix, TopologicalLocation.Interior,
            TopologicalLocation.Boundary, RelationDimension.Curve);
        RelationMatrixAlgorithms.SetCellAtLeast(
            matrix, TopologicalLocation.Interior,
            TopologicalLocation.Boundary, RelationDimension.Point);
        RelationMatrixAlgorithms.SetCell(
            matrix, TopologicalLocation.Boundary,
            TopologicalLocation.Exterior, RelationDimension.Surface);
        RelationMatrixAlgorithms.SetCell(
            matrix, TopologicalLocation.Interior,
            TopologicalLocation.Boundary, RelationDimension.False);

        Assert.That(RelationMatrixAlgorithms.GetCanonicalText(matrix),
            Is.EqualTo("FFFFF2FFF"));
    }

    [Test]
    public void TransposeSwapsOffDiagonalCellsDimensionsAndOwnership()
    {
        RelationMatrix matrix = Matrix("012F10F2F");
        matrix.LeftDimension = TopologicalDimension.Curve;
        matrix.RightDimension = TopologicalDimension.Surface;

        RelationMatrix transposed = RelationMatrixAlgorithms.Transpose(matrix);

        Assert.Multiple(() =>
        {
            Assert.That(RelationMatrixAlgorithms.GetCanonicalText(transposed),
                Is.EqualTo("0FF11220F"));
            Assert.That(transposed.LeftDimension,
                Is.EqualTo(TopologicalDimension.Surface));
            Assert.That(transposed.RightDimension,
                Is.EqualTo(TopologicalDimension.Curve));
            Assert.That(transposed.Cells, Is.Not.SameAs(matrix.Cells));
            Assert.That(RelationMatrixAlgorithms.GetCanonicalText(
                RelationMatrixAlgorithms.Transpose(transposed)),
                Is.EqualTo("012F10F2F"));
        });

        transposed.Cells[0] = RelationDimension.False;
        Assert.That(matrix.Cells[0], Is.EqualTo(RelationDimension.Point));
    }

    [Test]
    public void PatternMatchingSupportsTruthWildcardsAndExactDimensions()
    {
        RelationMatrix matrix = Matrix("0F1F2F102");

        Assert.Multiple(() =>
        {
            Assert.That(RelationMatrixAlgorithms.MatchesPattern(matrix, "TFTF2FT02"),
                Is.True);
            Assert.That(RelationMatrixAlgorithms.MatchesPattern(matrix, "*********"),
                Is.True);
            Assert.That(RelationMatrixAlgorithms.MatchesPattern(matrix, "1********"),
                Is.False);
            Assert.That(RelationMatrixAlgorithms.MatchesPattern(matrix, "0T*******"),
                Is.False);
            Assert.That(RelationMatrixAlgorithms.MatchesPattern(matrix, "0F1F2F102"),
                Is.True);
        });
    }

    [Test]
    public void ValidationRejectsMalformedMutableStorageAndEnumValues()
    {
        RelationMatrix nullCells = new RelationMatrix { Cells = null! };
        RelationMatrix shortCells = new RelationMatrix
        {
            Cells = new RelationDimension[8]
        };
        RelationMatrix badCell = new RelationMatrix();
        badCell.Cells[4] = (RelationDimension)3;
        RelationMatrix badLeft = new RelationMatrix
        {
            LeftDimension = (TopologicalDimension)3
        };
        RelationMatrix badRight = new RelationMatrix
        {
            RightDimension = (TopologicalDimension)(-2)
        };

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                RelationMatrixAlgorithms.ValidateMatrix(null!));
            Assert.Throws<InvalidGeometryException>(() =>
                RelationMatrixAlgorithms.ValidateMatrix(nullCells));
            Assert.Throws<InvalidGeometryException>(() =>
                RelationMatrixAlgorithms.ValidateMatrix(shortCells));
            Assert.Throws<InvalidGeometryException>(() =>
                RelationMatrixAlgorithms.ValidateMatrix(badCell));
            Assert.Throws<InvalidGeometryException>(() =>
                RelationMatrixAlgorithms.ValidateMatrix(badLeft));
            Assert.Throws<InvalidGeometryException>(() =>
                RelationMatrixAlgorithms.ValidateMatrix(badRight));
            Assert.Throws<InvalidGeometryException>(() =>
                RelationMatrixAlgorithms.GetCell(
                    new RelationMatrix(), (TopologicalLocation)3,
                    TopologicalLocation.Interior));
            Assert.Throws<InvalidGeometryException>(() =>
                RelationMatrixAlgorithms.SetCell(
                    new RelationMatrix(), TopologicalLocation.Interior,
                    TopologicalLocation.Interior, (RelationDimension)3));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("********")]
    [TestCase("**********")]
    [TestCase("T***X****")]
    [TestCase("t********")]
    public void PatternValidationRejectsNullWrongLengthAndUnknownSymbols(
        string? pattern)
    {
        Assert.Throws<InvalidGeometryException>(() =>
            RelationMatrixAlgorithms.MatchesPattern(new RelationMatrix(), pattern!));
    }

    private static RelationMatrix Matrix(string text)
    {
        RelationDimension[] cells = new RelationDimension[text.Length];
        int index = 0;
        while (index < text.Length)
        {
            cells[index] = text[index] switch
            {
                'F' => RelationDimension.False,
                '0' => RelationDimension.Point,
                '1' => RelationDimension.Curve,
                '2' => RelationDimension.Surface,
                _ => throw new AssertionException("Unsupported test matrix symbol.")
            };
            index++;
        }
        return new RelationMatrix { Cells = cells };
    }
}
