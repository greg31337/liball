using icc;

namespace icc.Tests;

public class MatrixMathTests
{
    [Test]
    public void DenseMultiplicationHandlesRectangularMatricesWithoutMutatingInputs()
    {
        IIccColorMath colorMath = new IccColorMath();
        double[] left = new double[]
        {
            1.0, 2.0, 3.0,
            4.0, 5.0, 6.0
        };
        double[] right = new double[]
        {
            7.0, 8.0,
            9.0, 10.0,
            11.0, 12.0
        };

        double[] product = colorMath.MultiplyMatrices(left, 2, 3, right, 2);

        Assert.Multiple(() =>
        {
            Assert.That(product, Is.EqualTo(new double[] { 58.0, 64.0, 139.0, 154.0 }));
            Assert.That(left, Is.EqualTo(new double[] { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0 }));
            Assert.That(right, Is.EqualTo(new double[] { 7.0, 8.0, 9.0, 10.0, 11.0, 12.0 }));
        });
    }

    [Test]
    public void MatrixVectorApplicationSupportsNonSquareMatrices()
    {
        double[] matrix = new double[]
        {
            1.0, 2.0, 3.0,
            4.0, 5.0, 6.0
        };
        double[] vector = new double[] { 7.0, 8.0, 9.0 };

        double[] result = MatrixMath.ApplyMatrixToVector(matrix, 2, 3, vector);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(new double[] { 50.0, 122.0 }));
            Assert.That(matrix, Is.EqualTo(new double[] { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0 }));
            Assert.That(vector, Is.EqualTo(new double[] { 7.0, 8.0, 9.0 }));
        });
    }

    [Test]
    public void CompositionAppliesFirstMatrixBeforeSecondMatrix()
    {
        double[] first = new double[]
        {
            2.0, 0.0,
            0.0, 3.0
        };
        double[] second = new double[]
        {
            0.0, -1.0,
            1.0, 0.0
        };
        double[] vector = new double[] { 1.0, 2.0 };

        double[] composed = MatrixMath.ComposeSquareMatrices(first, second, 2);
        double[] direct = MatrixMath.ApplyMatrixToVector(composed, 2, 2, vector);
        double[] afterFirst = MatrixMath.ApplyMatrixToVector(first, 2, 2, vector);
        double[] sequential = MatrixMath.ApplyMatrixToVector(second, 2, 2, afterFirst);

        Assert.Multiple(() =>
        {
            Assert.That(composed, Is.EqualTo(new double[] { 0.0, -3.0, 2.0, 0.0 }));
            Assert.That(direct, Is.EqualTo(new double[] { -6.0, 2.0 }));
            Assert.That(direct, Is.EqualTo(sequential));
        });
    }

    [Test]
    public void DeterminantUsesPivotingAndHandlesSingularMatrices()
    {
        double[] matrix = new double[]
        {
            6.0, 1.0, 1.0,
            4.0, -2.0, 5.0,
            2.0, 8.0, 7.0
        };
        double[] pivoting = new double[]
        {
            0.0, 1.0,
            1.0, 0.0
        };
        double[] singular = new double[]
        {
            1.0, 2.0,
            2.0, 4.0
        };
        double adjacent = System.Math.BitIncrement(1.0);
        double[] numericallySingular = new double[]
        {
            1.0, 1.0,
            1.0, adjacent
        };

        Assert.Multiple(() =>
        {
            Assert.That(MatrixMath.CalculateDeterminant(matrix, 3), Is.EqualTo(-306.0).Within(1e-12));
            Assert.That(MatrixMath.CalculateDeterminant(pivoting, 2), Is.EqualTo(-1.0).Within(1e-15));
            Assert.That(MatrixMath.CalculateDeterminant(singular, 2), Is.Zero);
            Assert.That(MatrixMath.CalculateDeterminant(numericallySingular, 2), Is.EqualTo(adjacent - 1.0));
            Assert.That(matrix, Is.EqualTo(new double[] { 6.0, 1.0, 1.0, 4.0, -2.0, 5.0, 2.0, 8.0, 7.0 }));
        });
    }

    [Test]
    public void DenseInversionUsesPivotingAndProducesBothIdentityProducts()
    {
        IIccColorMath colorMath = new IccColorMath();
        double[] matrix = new double[]
        {
            0.0, 2.0, 1.0,
            1.0, 1.0, 0.0,
            2.0, 0.0, 1.0
        };

        double[] inverse = colorMath.InvertMatrix(matrix, 3);
        double[] leftIdentity = colorMath.MultiplyMatrices(matrix, 3, 3, inverse, 3);
        double[] rightIdentity = colorMath.MultiplyMatrices(inverse, 3, 3, matrix, 3);

        Assert.Multiple(() =>
        {
            Assert.That(inverse, Is.EqualTo(new double[]
            {
                -0.25, 0.5, 0.25,
                0.25, 0.5, -0.25,
                0.5, -1.0, 0.5
            }).Within(1e-15));
            Assert.That(leftIdentity, Is.EqualTo(Identity3x3()).Within(1e-14));
            Assert.That(rightIdentity, Is.EqualTo(Identity3x3()).Within(1e-14));
            Assert.That(matrix, Is.EqualTo(new double[] { 0.0, 2.0, 1.0, 1.0, 1.0, 0.0, 2.0, 0.0, 1.0 }));
        });
    }

    [Test]
    public void ThreeByThreeConveniencesMatchGeneralOperations()
    {
        double[] left = new double[]
        {
            3.0, 0.0, 2.0,
            2.0, 0.0, -2.0,
            0.0, 1.0, 1.0
        };
        double[] right = new double[]
        {
            1.0, 2.0, 0.0,
            0.0, 1.0, 0.0,
            0.0, 0.0, 1.0
        };
        double[] vector = new double[] { 0.25, 0.5, 0.75 };

        Assert.Multiple(() =>
        {
            Assert.That(MatrixMath.MultiplyMatrices3x3(left, right), Is.EqualTo(MatrixMath.MultiplyMatrices(left, 3, 3, right, 3)));
            Assert.That(MatrixMath.ComposeMatrices3x3(left, right), Is.EqualTo(MatrixMath.ComposeSquareMatrices(left, right, 3)));
            Assert.That(MatrixMath.CalculateDeterminant3x3(left), Is.EqualTo(MatrixMath.CalculateDeterminant(left, 3)));
            Assert.That(MatrixMath.InvertMatrix3x3(left), Is.EqualTo(MatrixMath.InvertMatrix(left, 3)).Within(1e-15));
            Assert.That(MatrixMath.ApplyMatrix3x3(left, vector), Is.EqualTo(MatrixMath.ApplyMatrixToVector(left, 3, 3, vector)));
        });
    }

    [Test]
    public void InversionRejectsSingularAndNumericallySingularMatrices()
    {
        IIccColorMath colorMath = new IccColorMath();
        double adjacent = System.Math.BitIncrement(1.0);

        Assert.Multiple(() =>
        {
            Assert.Throws<IccTransformException>(() => colorMath.InvertMatrix(new double[] { 1.0, 2.0, 2.0, 4.0 }, 2));
            Assert.Throws<IccTransformException>(() => colorMath.InvertMatrix(new double[] { 1.0, 1.0, 1.0, adjacent }, 2));
        });
    }

    [Test]
    public void MatrixOperationsRejectInvalidDimensionsShapesValuesAndOverflow()
    {
        IIccColorMath colorMath = new IccColorMath();

        Assert.Multiple(() =>
        {
            Assert.Throws<IccTransformException>(() => colorMath.MultiplyMatrices(null!, 1, 1, new double[] { 1.0 }, 1));
            Assert.Throws<IccTransformException>(() => colorMath.MultiplyMatrices(new double[] { 1.0 }, 0, 1, new double[] { 1.0 }, 1));
            Assert.Throws<IccTransformException>(() => colorMath.MultiplyMatrices(new double[] { 1.0, 2.0 }, 1, 2, new double[] { 1.0 }, 1));
            Assert.Throws<IccTransformException>(() => colorMath.MultiplyMatrices(new double[] { double.NaN }, 1, 1, new double[] { 1.0 }, 1));
            Assert.Throws<IccTransformException>(() => colorMath.MultiplyMatrices(new double[] { double.MaxValue }, 1, 1, new double[] { 2.0 }, 1));
            Assert.Throws<IccTransformException>(() => colorMath.InvertMatrix(new double[] { 1.0, 2.0 }, 1));
            Assert.Throws<IccTransformException>(() => MatrixMath.ApplyMatrixToVector(new double[] { 1.0 }, 1, 1, new double[0]));
            Assert.Throws<IccTransformException>(() => MatrixMath.MultiplyMatrices3x3(new double[8], new double[9]));
        });
    }

    private static double[] Identity3x3()
    {
        return new double[]
        {
            1.0, 0.0, 0.0,
            0.0, 1.0, 0.0,
            0.0, 0.0, 1.0
        };
    }
}
