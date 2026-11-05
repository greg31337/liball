namespace avif.Tests;

public class Av1InverseTransformTests
{
    [Test]
    public void FourByFourDctDcProducesAUniformNormativeResidual()
    {
        Av1InverseTransformRequest request = CreateRequest(
            Av1TransformSize.Transform4x4,
            Av1TransformType.DctDct,
            32);
        request.EndOfBlock = 1;

        Av1InverseTransformResult result = Av1InverseTransformer.Transform(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(result.Width, Is.EqualTo(4));
            Assert.That(result.Height, Is.EqualTo(4));
            Assert.That(result.Residual, Is.All.EqualTo(1));
        });
    }

    [Test]
    public void LosslessFourByFourUsesTheInverseWalshHadamardTransform()
    {
        Av1InverseTransformRequest request = CreateRequest(
            Av1TransformSize.Transform4x4,
            Av1TransformType.DctDct,
            16);
        request.Lossless = true;
        request.EndOfBlock = 1;

        Av1InverseTransformResult result = Av1InverseTransformer.Transform(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(result.Residual, Is.All.EqualTo(1));
        });
    }

    [Test]
    public void IdentityTransformUsesItsSizeDependentScaling()
    {
        Av1InverseTransformRequest request = CreateRequest(
            Av1TransformSize.Transform4x4,
            Av1TransformType.Identity,
            8);
        request.EndOfBlock = 1;

        Av1InverseTransformResult result = Av1InverseTransformer.Transform(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(result.Residual[0], Is.EqualTo(1));
            Assert.That(result.Residual.Skip(1), Is.All.Zero);
        });
    }

    [Test]
    public void EveryTransformSizeSupportsDctDctAndItsDeclaredDimensions()
    {
        int size = 0;
        while (size < Av1SyntaxConstants.TransformSizeCount)
        {
            Av1TransformSize transformSize = (Av1TransformSize)size;
            Av1InverseTransformRequest request = CreateRequest(
                transformSize,
                Av1TransformType.DctDct,
                32);
            request.EndOfBlock = 1;

            Av1InverseTransformResult result = Av1InverseTransformer.Transform(request);

            Assert.Multiple(() =>
            {
                Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
                Assert.That(result.Width, Is.EqualTo(
                    Av1ConversionTables.GetTransformWidth(transformSize)));
                Assert.That(result.Height, Is.EqualTo(
                    Av1ConversionTables.GetTransformHeight(transformSize)));
                Assert.That(result.Residual, Has.Length.EqualTo(
                    result.Width * result.Height));
            });
            size++;
        }
    }

    [Test]
    public void FourByFourSupportsEveryDeclaredTransformType()
    {
        int type = 0;
        while (type < Av1SyntaxConstants.TransformTypeCount)
        {
            Av1InverseTransformRequest request = CreateRequest(
                Av1TransformSize.Transform4x4,
                (Av1TransformType)type,
                64);
            request.EndOfBlock = 1;

            Av1InverseTransformResult result = Av1InverseTransformer.Transform(request);

            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success),
                $"transform type {type}");
            type++;
        }
    }

    [Test]
    public void SixtyFourPixelTransformsPadTheReducedCoefficientDomain()
    {
        Av1InverseTransformRequest request = CreateRequest(
            Av1TransformSize.Transform64x16,
            Av1TransformType.DctDct,
            0);
        request.Coefficients[request.Coefficients.Length - 1] = 256;

        Av1InverseTransformResult result = Av1InverseTransformer.Transform(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(result.Width, Is.EqualTo(64));
            Assert.That(result.Height, Is.EqualTo(16));
            Assert.That(result.Residual, Has.Some.Not.Zero);
        });
    }

    [Test]
    public void EndOfBlockExcludesCoefficientsAfterTheScanBoundary()
    {
        Av1InverseTransformRequest truncated = CreateRequest(
            Av1TransformSize.Transform8x4,
            Av1TransformType.DctDct,
            32);
        int[] scan = Av1ScanTables.GetScan(
            truncated.TransformSize, truncated.TransformType);
        truncated.Coefficients[scan[1]] = 2048;
        truncated.EndOfBlock = 1;
        Av1InverseTransformRequest dcOnly = CreateRequest(
            Av1TransformSize.Transform8x4,
            Av1TransformType.DctDct,
            32);
        dcOnly.EndOfBlock = 1;

        Av1InverseTransformResult truncatedResult =
            Av1InverseTransformer.Transform(truncated);
        Av1InverseTransformResult dcOnlyResult =
            Av1InverseTransformer.Transform(dcOnly);

        Assert.That(truncatedResult.Residual, Is.EqualTo(dcOnlyResult.Residual));
    }

    [TestCase(8, 250, 2048, 255)]
    [TestCase(8, 5, -2048, 0)]
    [TestCase(10, 1000, 8192, 1023)]
    [TestCase(10, 20, -8192, 0)]
    [TestCase(12, 4080, 32768, 4095)]
    [TestCase(12, 30, -32768, 0)]
    public void ReconstructionAddsResidualAndClipsAtEveryBitDepth(
        int bitDepth,
        int prediction,
        int coefficient,
        int expected)
    {
        PlaneBuffer plane = CreatePlane(4, 4, bitDepth, prediction);
        Av1InverseTransformRequest request = CreateRequest(
            Av1TransformSize.Transform4x4,
            Av1TransformType.Identity,
            coefficient);
        request.BitDepth = bitDepth;
        request.EndOfBlock = 1;
        request.Destination = plane;

        AvifResultCode code = Av1InverseTransformer.Reconstruct(request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadSample(plane, 0, 0), Is.EqualTo(expected));
            Assert.That(ReadSample(plane, 3, 3), Is.EqualTo(prediction));
        });
        plane.Close();
    }

    [Test]
    public void ReconstructionAppliesFlipAdstOutputCoordinates()
    {
        PlaneBuffer plane = CreatePlane(4, 4, 8, 128);
        Av1InverseTransformRequest request = CreateRequest(
            Av1TransformSize.Transform4x4,
            Av1TransformType.DctFlipAdst,
            64);
        request.EndOfBlock = 1;
        request.Destination = plane;
        Av1InverseTransformResult transformed = Av1InverseTransformer.Transform(request);

        AvifResultCode code = Av1InverseTransformer.Reconstruct(request);

        Assert.That(code, Is.EqualTo(AvifResultCode.Success));
        int row = 0;
        while (row < 4)
        {
            int column = 0;
            while (column < 4)
            {
                Assert.That(ReadSample(plane, row, 3 - column), Is.EqualTo(
                    System.Math.Clamp(128 + transformed.Residual[row * 4 + column],
                        0, 255)));
                column++;
            }
            row++;
        }
        plane.Close();
    }

    [Test]
    public void InvalidDestinationDoesNotPartiallyReconstruct()
    {
        PlaneBuffer plane = CreatePlane(4, 4, 8, 77);
        Av1InverseTransformRequest request = CreateRequest(
            Av1TransformSize.Transform4x4,
            Av1TransformType.DctDct,
            32);
        request.Destination = plane;
        request.X = 1;

        AvifResultCode code = Av1InverseTransformer.Reconstruct(request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(plane.GetRowSpan(0).ToArray(), Is.All.EqualTo(77));
            Assert.That(plane.GetRowSpan(3).ToArray(), Is.All.EqualTo(77));
        });
        plane.Close();
    }

    [Test]
    public void SplitProducesRasterOrderedChildTransformGeometry()
    {
        Av1TransformSplitResult once = Av1InverseTransformer.Split(
            Av1TransformSize.Transform16x8, 5, 7, 1);
        Av1TransformSplitResult twice = Av1InverseTransformer.Split(
            Av1TransformSize.Transform64x16, 2);

        Assert.Multiple(() =>
        {
            Assert.That(once.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(once.Partitions, Has.Length.EqualTo(2));
            Assert.That(once.Partitions.Select(partition => partition.X),
                Is.EqualTo(new int[] { 5,13 }));
            Assert.That(once.Partitions.Select(partition => partition.Y),
                Is.All.EqualTo(7));
            Assert.That(once.Partitions.Select(partition => partition.TransformSize),
                Is.All.EqualTo(Av1TransformSize.Transform8x8));
            Assert.That(twice.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(twice.Partitions, Has.Length.EqualTo(4));
            Assert.That(twice.Partitions.Select(partition => partition.TransformSize),
                Is.All.EqualTo(Av1TransformSize.Transform16x16));
        });
    }

    [Test]
    public void IllegalTransformLengthAndLosslessCombinationAreRejected()
    {
        Av1InverseTransformRequest adst64 = CreateRequest(
            Av1TransformSize.Transform64x64,
            Av1TransformType.AdstAdst,
            1);
        Av1InverseTransformRequest lossless8 = CreateRequest(
            Av1TransformSize.Transform8x8,
            Av1TransformType.DctDct,
            1);
        lossless8.Lossless = true;

        Assert.Multiple(() =>
        {
            Assert.That(Av1InverseTransformer.Transform(adst64).Code,
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(Av1InverseTransformer.Transform(lossless8).Code,
                Is.EqualTo(AvifResultCode.InvalidArgument));
        });
    }

    static Av1InverseTransformRequest CreateRequest(
        Av1TransformSize size,
        Av1TransformType type,
        int dcCoefficient)
    {
        Av1InverseTransformRequest request = new Av1InverseTransformRequest();
        request.TransformSize = size;
        request.TransformType = type;
        request.Coefficients = new int[
            Av1ConversionTables.GetTransformCoefficientCount(size)];
        request.Coefficients[0] = dcCoefficient;
        return request;
    }

    static PlaneBuffer CreatePlane(
        int width,
        int height,
        int bitDepth,
        int value)
    {
        int bytesPerSample = bitDepth > 8 ? 2 : 1;
        PlaneBuffer plane = new PlaneBuffer(
            width, height, bytesPerSample, 1, 4096, true);
        int row = 0;
        while (row < height)
        {
            Span<byte> samples = plane.GetRowSpan(row);
            int column = 0;
            while (column < width)
            {
                int offset = column * bytesPerSample;
                samples[offset] = (byte)value;
                if (bytesPerSample == 2)
                {
                    samples[offset + 1] = (byte)(value >> 8);
                }
                column++;
            }
            row++;
        }
        return plane;
    }

    static int ReadSample(PlaneBuffer plane, int row, int column)
    {
        Span<byte> samples = plane.GetRowSpan(row);
        int offset = column * plane.BytesPerSample;
        return plane.BytesPerSample == 1
            ? samples[offset]
            : samples[offset] | samples[offset + 1] << 8;
    }
}
