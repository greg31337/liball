namespace random.Tests;

public class ArrayMatrixTests
{
    [Test]
    public void CopyPreservesRowMajorOrderAndSupportsCallerDestination()
    {
        double[] source = [1.0, 2.0, 3.0, 4.0, 5.0, 6.0];
        double[] copy = ArrayMatrix.Copy(source, 2, 3);
        double[] destination = new double[6];
        ArrayMatrix.Copy(source, 2, 3, destination);

        Assert.Multiple(() =>
        {
            Assert.That(copy, Is.EqualTo(source));
            Assert.That(copy, Is.Not.SameAs(source));
            Assert.That(destination, Is.EqualTo(source));
        });

        source[0] = 99.0;
        Assert.That(copy[0], Is.EqualTo(1.0));
        Assert.DoesNotThrow(() => ArrayMatrix.Copy(source, 2, 3, source));
    }

    [Test]
    public void SymmetryChecksExactToleranceAndNonFiniteValues()
    {
        double[] symmetric = [4.0, 2.0, -1.0, 2.0, 5.0, 3.0, -1.0, 3.0, 6.0];
        double[] nearlySymmetric = [1.0, 2.000000001, 2.0, 3.0];
        double[] nonFinite = [1.0, double.NaN, double.NaN, 1.0];

        Assert.Multiple(() =>
        {
            Assert.That(ArrayMatrix.IsSymmetric(symmetric, 3), Is.True);
            Assert.That(ArrayMatrix.IsSymmetric(nearlySymmetric, 2), Is.False);
            Assert.That(ArrayMatrix.IsSymmetric(nearlySymmetric, 2, 1.1e-9), Is.True);
            Assert.That(ArrayMatrix.IsSymmetric(nearlySymmetric, 2, 5.0e-10), Is.False);
            Assert.That(ArrayMatrix.IsSymmetric(nonFinite, 2), Is.False);
        });
    }

    [Test]
    public void CholeskyProducesDeterministicLowerTriangularFactor()
    {
        double[] matrix = [4.0, 12.0, -16.0, 12.0, 37.0, -43.0, -16.0, -43.0, 98.0];
        double[] expected = [2.0, 0.0, 0.0, 6.0, 1.0, 0.0, -8.0, 5.0, 3.0];

        double[] factor = ArrayMatrix.Cholesky(matrix, 3);
        Assert.That(factor, Is.EqualTo(expected));

        double[] aliased = (double[])matrix.Clone();
        ArrayMatrix.Cholesky(aliased, 3, aliased);
        Assert.That(aliased, Is.EqualTo(expected));
    }

    [Test]
    public void CholeskyRejectsAsymmetryAndNonPositiveDefiniteInputsAtomically()
    {
        double[] asymmetric = [2.0, 1.0, 0.0, 2.0];
        double[] semidefinite = [1.0, 1.0, 1.0, 1.0];
        double[] destination = [9.0, 9.0, 9.0, 9.0];

        AssertArgument("matrix", () => ArrayMatrix.Cholesky(asymmetric, 2, destination));
        Assert.That(destination, Is.EqualTo(new double[] { 9.0, 9.0, 9.0, 9.0 }));
        AssertArgument("matrix", () => ArrayMatrix.Cholesky(semidefinite, 2, destination));
        Assert.That(destination, Is.EqualTo(new double[] { 9.0, 9.0, 9.0, 9.0 }));
    }

    [Test]
    public void TriangularSolvesRecoverKnownVectorsAndAllowAliasing()
    {
        double[] lower = [2.0, 0.0, 0.0, 6.0, 1.0, 0.0, -8.0, 5.0, 3.0];
        double[] lowerRightHandSide = [2.0, 8.0, 11.0];
        double[] expected = [1.0, 2.0, 3.0];
        Assert.That(ArrayMatrix.SolveLowerTriangular(lower, 3, lowerRightHandSide), Is.EqualTo(expected));

        double[] upper = [2.0, 6.0, -8.0, 0.0, 1.0, 5.0, 0.0, 0.0, 3.0];
        double[] upperRightHandSide = [-10.0, 17.0, 9.0];
        ArrayMatrix.SolveUpperTriangular(upper, 3, upperRightHandSide, upperRightHandSide);
        Assert.That(upperRightHandSide, Is.EqualTo(expected));
    }

    [Test]
    public void TriangularSolvesRejectSingularDiagonalWithoutChangingDestination()
    {
        double[] lower = [1.0, 0.0, 2.0, 0.0];
        double[] rightHandSide = [1.0, 2.0];
        double[] destination = [7.0, 8.0];

        AssertArgument("lower", () => ArrayMatrix.SolveLowerTriangular(lower, 2, rightHandSide, destination));
        Assert.That(destination, Is.EqualTo(new double[] { 7.0, 8.0 }));
    }

    [Test]
    public void MultiplyUsesRectangularRowMajorShapesAndSupportsAliasing()
    {
        double[] left = [1.0, 2.0, 3.0, 4.0, 5.0, 6.0];
        double[] right = [7.0, 8.0, 9.0, 10.0, 11.0, 12.0];
        double[] expected = [58.0, 64.0, 139.0, 154.0];
        Assert.That(ArrayMatrix.Multiply(left, 2, 3, right, 3, 2), Is.EqualTo(expected));

        double[] square = [1.0, 2.0, 3.0, 4.0];
        double[] identity = [1.0, 0.0, 0.0, 1.0];
        ArrayMatrix.Multiply(square, 2, 2, identity, 2, 2, square);
        Assert.That(square, Is.EqualTo(new double[] { 1.0, 2.0, 3.0, 4.0 }));
    }

    [Test]
    public void InvertHandlesGeneralMatricesAndPivotRowSwaps()
    {
        double[] matrix = [4.0, 7.0, 2.0, 6.0];
        double[] inverse = ArrayMatrix.Invert(matrix, 2);
        AssertArrayClose(inverse, [0.6, -0.7, -0.2, 0.4], 2.0e-15);
        AssertArrayClose(ArrayMatrix.Multiply(matrix, 2, 2, inverse, 2, 2), [1.0, 0.0, 0.0, 1.0], 2.0e-15);

        double[] needsPivotSwap = [0.0, 1.0, 2.0, 3.0];
        AssertArrayClose(ArrayMatrix.Invert(needsPivotSwap, 2), [-1.5, 0.5, 1.0, 0.0], 2.0e-15);
    }

    [Test]
    public void InvertRejectsSingularAndNonFiniteMatricesAtomically()
    {
        double[] singular = [1.0, 2.0, 2.0, 4.0];
        double[] destination = [5.0, 6.0, 7.0, 8.0];
        AssertArgument("matrix", () => ArrayMatrix.Invert(singular, 2, destination));
        Assert.That(destination, Is.EqualTo(new double[] { 5.0, 6.0, 7.0, 8.0 }));

        double[] nonFinite = [1.0, 0.0, 0.0, double.PositiveInfinity];
        AssertArgument("matrix", () => ArrayMatrix.Invert(nonFinite, 2, destination));
        Assert.That(destination, Is.EqualTo(new double[] { 5.0, 6.0, 7.0, 8.0 }));
    }

    [Test]
    public void MatrixOperationsValidateShapesAndDimensionsBeforeMutation()
    {
        double[] matrix = [1.0, 0.0, 0.0, 1.0];
        double[] destination = [3.0, 3.0, 3.0, 3.0];

        AssertArgument("source", () => ArrayMatrix.Copy([1.0, 2.0, 3.0], 2, 2));
        AssertArgument("tolerance", () => ArrayMatrix.IsSymmetric(matrix, 2, -1.0));
        AssertArgument("rightRows", () => ArrayMatrix.Multiply(matrix, 2, 2, matrix, 1, 4));
        AssertArgument("destination", () => ArrayMatrix.Multiply(matrix, 2, 2, matrix, 2, 2, new double[3]));
        Assert.That(destination, Is.EqualTo(new double[] { 3.0, 3.0, 3.0, 3.0 }));
    }

    static void AssertArrayClose(double[] actual, double[] expected, double tolerance)
    {
        Assert.That(actual.Length, Is.EqualTo(expected.Length));
        for (int index = 0; index < expected.Length; index++)
        {
            Assert.That(actual[index], Is.EqualTo(expected[index]).Within(tolerance), $"index={index}, actual={actual[index]:R}, expected={expected[index]:R}");
        }
    }

    static void AssertArgument(string expectedName, TestDelegate action)
    {
        RandomArgumentException error = Assert.Throws<RandomArgumentException>(action);
        Assert.That(error.ArgumentName(), Is.EqualTo(expectedName));
    }
}
