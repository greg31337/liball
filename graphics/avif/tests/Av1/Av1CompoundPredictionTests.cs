namespace avif.Tests;

public class Av1CompoundPredictionTests
{
    [Test]
    public void AverageCompoundRoundsPairsIntoTheDestinationBlock()
    {
        PlaneBuffer first = CreatePlane(8, 8, 8);
        PlaneBuffer second = CreatePlane(8, 8, 8);
        PlaneBuffer destination = CreatePlane(8, 8, 8);
        Fill(first, 10);
        Fill(second, 21);
        Av1CompoundPredictionRequest request = CreateCompoundRequest(
            destination, first, second, 8, 8, 8);

        AvifResultCode code = Av1CompoundPredictor.Predict(request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadBlock(destination, 8, 8), Is.All.EqualTo(16));
        });
        Close(first, second, destination);
    }

    [Test]
    public void DistanceWeightsFollowReferenceOrderHintRatios()
    {
        int[] equal = Av1CompoundPredictor.DeriveDistanceWeights(4, 4);
        int[] firstCloser = Av1CompoundPredictor.DeriveDistanceWeights(1, 3);
        int[] secondCloser = Av1CompoundPredictor.DeriveDistanceWeights(3, 1);
        int[] zero = Av1CompoundPredictor.DeriveDistanceWeights(0, 5);
        int[] invalid = Av1CompoundPredictor.DeriveDistanceWeights(32, 1);

        Assert.Multiple(() =>
        {
            Assert.That(equal, Is.EqualTo(new int[] { 7,9 }));
            Assert.That(firstCloser, Is.EqualTo(new int[] { 4,12 }));
            Assert.That(secondCloser, Is.EqualTo(new int[] { 12,4 }));
            Assert.That(zero, Is.EqualTo(new int[] { 3,13 }));
            Assert.That(invalid, Is.Empty);
        });

        PlaneBuffer first = CreatePlane(8, 8, 8);
        PlaneBuffer second = CreatePlane(8, 8, 8);
        PlaneBuffer destination = CreatePlane(8, 8, 8);
        Fill(first, 0);
        Fill(second, 160);
        Av1CompoundPredictionRequest request = CreateCompoundRequest(
            destination, first, second, 8, 8, 8);
        request.Type = Av1CompoundPredictionType.DistanceWeighted;
        request.FirstDistance = 1;
        request.SecondDistance = 3;

        AvifResultCode code = Av1CompoundPredictor.Predict(request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadBlock(destination, 8, 8), Is.All.EqualTo(120));
        });
        Close(first, second, destination);
    }

    [TestCase(8, 160)]
    [TestCase(10, 640)]
    [TestCase(12, 2560)]
    public void DifferenceMasksNormalizeBitDepthAndSupportInversion(
        int bitDepth,
        int difference)
    {
        PlaneBuffer first = CreatePlane(4, 4, bitDepth);
        PlaneBuffer second = CreatePlane(4, 4, bitDepth);
        Fill(first, 0);
        Fill(second, difference);

        int[] mask = Av1CompoundPredictor.GenerateDifferenceMask(
            first, 0, 0, second, 0, 0, 4, 4, bitDepth, false);
        int[] inverted = Av1CompoundPredictor.GenerateDifferenceMask(
            first, 0, 0, second, 0, 0, 4, 4, bitDepth, true);

        Assert.Multiple(() =>
        {
            Assert.That(mask, Is.All.EqualTo(48));
            Assert.That(inverted, Is.All.EqualTo(16));
        });
        first.Close();
        second.Close();
    }

    [Test]
    public void DifferenceWeightedCompoundUsesTheGeneratedMask()
    {
        PlaneBuffer first = CreatePlane(4, 4, 8);
        PlaneBuffer second = CreatePlane(4, 4, 8);
        PlaneBuffer destination = CreatePlane(4, 4, 8);
        Fill(first, 0);
        Fill(second, 160);
        Av1CompoundPredictionRequest request = CreateCompoundRequest(
            destination, first, second, 4, 4, 8);
        request.Type = Av1CompoundPredictionType.DifferenceWeighted;

        AvifResultCode code = Av1CompoundPredictor.Predict(request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadBlock(destination, 4, 4), Is.All.EqualTo(40));
        });
        Close(first, second, destination);
    }

    [Test]
    public void WedgeMasksUseAllCodebooksAndOppositeSignsAreComplements()
    {
        int[][] sizes = new int[][]
        {
            new int[] { 8,8 },
            new int[] { 8,16 },
            new int[] { 16,8 },
            new int[] { 8,32 },
            new int[] { 32,8 },
            new int[] { 32,32 }
        };
        int sizeIndex = 0;
        while (sizeIndex < sizes.Length)
        {
            int width = sizes[sizeIndex][0];
            int height = sizes[sizeIndex][1];
            int wedgeIndex = 0;
            while (wedgeIndex < 16)
            {
                int[] positive = Av1CompoundPredictor.GenerateWedgeMask(
                    width, height, wedgeIndex, false);
                int[] negative = Av1CompoundPredictor.GenerateWedgeMask(
                    width, height, wedgeIndex, true);
                Assert.That(positive, Has.Length.EqualTo(width * height));
                int pixel = 0;
                while (pixel < positive.Length)
                {
                    Assert.That(positive[pixel], Is.InRange(0, 64));
                    Assert.That(positive[pixel] + negative[pixel], Is.EqualTo(64));
                    pixel++;
                }
                wedgeIndex++;
            }
            sizeIndex++;
        }
    }

    [Test]
    public void WedgeCompoundProducesA64MaskedPixels()
    {
        PlaneBuffer first = CreatePlane(8, 8, 8);
        PlaneBuffer second = CreatePlane(8, 8, 8);
        PlaneBuffer destination = CreatePlane(8, 8, 8);
        Fill(first, 0);
        Fill(second, 64);
        Av1CompoundPredictionRequest request = CreateCompoundRequest(
            destination, first, second, 8, 8, 8);
        request.Type = Av1CompoundPredictionType.Wedge;
        request.WedgeIndex = 7;

        AvifResultCode code = Av1CompoundPredictor.Predict(request);
        int[] pixels = ReadBlock(destination, 8, 8);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(pixels, Has.Some.LessThan(16));
            Assert.That(pixels, Has.Some.GreaterThan(48));
        });
        Close(first, second, destination);
    }

    [Test]
    public void InterIntraMasksCoverDcDirectionalAndSmoothModes()
    {
        int[] dc = Av1CompoundPredictor.GenerateInterIntraMask(
            8, 8, Av1InterIntraMode.Dc);
        int[] vertical = Av1CompoundPredictor.GenerateInterIntraMask(
            8, 8, Av1InterIntraMode.Vertical);
        int[] horizontal = Av1CompoundPredictor.GenerateInterIntraMask(
            8, 8, Av1InterIntraMode.Horizontal);
        int[] smooth = Av1CompoundPredictor.GenerateInterIntraMask(
            8, 8, Av1InterIntraMode.Smooth);
        int[] largest = Av1CompoundPredictor.GenerateInterIntraMask(
            128, 128, Av1InterIntraMode.Smooth);

        Assert.Multiple(() =>
        {
            Assert.That(dc, Is.All.EqualTo(32));
            Assert.That(vertical[0], Is.EqualTo(60));
            Assert.That(vertical[7 * 8], Is.EqualTo(1));
            Assert.That(horizontal[0], Is.EqualTo(60));
            Assert.That(horizontal[7], Is.EqualTo(1));
            Assert.That(smooth[7 * 8 + 2], Is.EqualTo(vertical[2 * 8]));
            Assert.That(smooth[2 * 8 + 7], Is.EqualTo(horizontal[2]));
            Assert.That(largest, Has.Length.EqualTo(16384));
            Assert.That(largest[16383], Is.EqualTo(1));
        });
    }

    [Test]
    public void InterIntraPredictionBlendsIntraTowardItsRelevantEdge()
    {
        PlaneBuffer inter = CreatePlane(8, 8, 8);
        PlaneBuffer intra = CreatePlane(8, 8, 8);
        PlaneBuffer destination = CreatePlane(8, 8, 8);
        Fill(inter, 40);
        Fill(intra, 200);
        Av1InterIntraPredictionRequest request = CreateInterIntraRequest(
            destination, inter, intra, 8, 8, 8);
        request.Mode = Av1InterIntraMode.Vertical;

        AvifResultCode code = Av1CompoundPredictor.PredictInterIntra(request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadSample(destination, 0, 0), Is.EqualTo(190));
            Assert.That(ReadSample(destination, 7, 0), Is.EqualTo(43));
        });
        Close(inter, intra, destination);
    }

    [Test]
    public void WedgeInterIntraUsesTheMaskForIntraAsTheFirstPredictor()
    {
        PlaneBuffer inter = CreatePlane(8, 8, 8);
        PlaneBuffer intra = CreatePlane(8, 8, 8);
        PlaneBuffer destination = CreatePlane(8, 8, 8);
        Fill(inter, 0);
        Fill(intra, 64);
        Av1InterIntraPredictionRequest request = CreateInterIntraRequest(
            destination, inter, intra, 8, 8, 8);
        request.UseWedge = true;
        request.WedgeIndex = 7;
        int[] mask = Av1CompoundPredictor.GenerateWedgeMask(8, 8, 7, false);

        AvifResultCode code = Av1CompoundPredictor.PredictInterIntra(request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadBlock(destination, 8, 8), Is.EqualTo(mask));
        });
        Close(inter, intra, destination);
    }

    [TestCase(10)]
    [TestCase(12)]
    public void CompoundOutputPreservesHighBitDepthSamples(int bitDepth)
    {
        int maximum = (1 << bitDepth) - 1;
        PlaneBuffer first = CreatePlane(4, 4, bitDepth);
        PlaneBuffer second = CreatePlane(4, 4, bitDepth);
        PlaneBuffer destination = CreatePlane(4, 4, bitDepth);
        Fill(first, maximum);
        Fill(second, maximum - 2);
        Av1CompoundPredictionRequest request = CreateCompoundRequest(
            destination, first, second, 4, 4, bitDepth);

        AvifResultCode code = Av1CompoundPredictor.Predict(request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadBlock(destination, 4, 4), Is.All.EqualTo(maximum - 1));
        });
        Close(first, second, destination);
    }

    [Test]
    public void InvalidMaskRequestsAreRejectedWithoutWritingPixels()
    {
        PlaneBuffer first = CreatePlane(8, 8, 8);
        PlaneBuffer second = CreatePlane(8, 8, 8);
        PlaneBuffer destination = CreatePlane(8, 8, 8);
        Fill(destination, 77);
        Av1CompoundPredictionRequest invalidWedge = CreateCompoundRequest(
            destination, first, second, 8, 8, 8);
        invalidWedge.Type = Av1CompoundPredictionType.Wedge;
        invalidWedge.WedgeIndex = 16;
        Av1CompoundPredictionRequest invalidDistance = CreateCompoundRequest(
            destination, first, second, 8, 8, 8);
        invalidDistance.Type = Av1CompoundPredictionType.DistanceWeighted;
        invalidDistance.FirstDistance = -1;

        AvifResultCode wedgeCode = Av1CompoundPredictor.Predict(invalidWedge);
        AvifResultCode distanceCode = Av1CompoundPredictor.Predict(invalidDistance);

        Assert.Multiple(() =>
        {
            Assert.That(wedgeCode, Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(distanceCode, Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(ReadBlock(destination, 8, 8), Is.All.EqualTo(77));
            Assert.That(Av1CompoundPredictor.GenerateWedgeMask(4, 4, 0, false),
                Is.Empty);
        });
        Close(first, second, destination);
    }

    static Av1CompoundPredictionRequest CreateCompoundRequest(
        PlaneBuffer destination,
        PlaneBuffer first,
        PlaneBuffer second,
        int width,
        int height,
        int bitDepth)
    {
        Av1CompoundPredictionRequest request = new Av1CompoundPredictionRequest();
        request.Destination = destination;
        request.First = first;
        request.Second = second;
        request.Width = width;
        request.Height = height;
        request.BitDepth = bitDepth;
        return request;
    }

    static Av1InterIntraPredictionRequest CreateInterIntraRequest(
        PlaneBuffer destination,
        PlaneBuffer inter,
        PlaneBuffer intra,
        int width,
        int height,
        int bitDepth)
    {
        Av1InterIntraPredictionRequest request = new Av1InterIntraPredictionRequest();
        request.Destination = destination;
        request.Inter = inter;
        request.Intra = intra;
        request.Width = width;
        request.Height = height;
        request.BitDepth = bitDepth;
        return request;
    }

    static PlaneBuffer CreatePlane(int width, int height, int bitDepth)
    {
        int bytesPerSample = bitDepth > 8 ? 2 : 1;
        return new PlaneBuffer(
            width,
            height,
            bytesPerSample,
            bytesPerSample,
            (long)width * height * bytesPerSample,
            true);
    }

    static void Fill(PlaneBuffer plane, int value)
    {
        int row = 0;
        while (row < plane.Height)
        {
            int column = 0;
            while (column < plane.Width)
            {
                WriteSample(plane, row, column, value);
                column++;
            }
            row++;
        }
    }

    static int[] ReadBlock(PlaneBuffer plane, int width, int height)
    {
        int[] values = new int[width * height];
        int row = 0;
        while (row < height)
        {
            int column = 0;
            while (column < width)
            {
                values[row * width + column] = ReadSample(plane, row, column);
                column++;
            }
            row++;
        }
        return values;
    }

    static int ReadSample(PlaneBuffer plane, int row, int column)
    {
        Span<byte> bytes = plane.GetRowSpan(row);
        int offset = column * plane.BytesPerSample;
        return plane.BytesPerSample == 1
            ? bytes[offset]
            : bytes[offset] | bytes[offset + 1] << 8;
    }

    static void WriteSample(PlaneBuffer plane, int row, int column, int value)
    {
        Span<byte> bytes = plane.GetRowSpan(row);
        int offset = column * plane.BytesPerSample;
        bytes[offset] = (byte)value;
        if (plane.BytesPerSample == 2)
        {
            bytes[offset + 1] = (byte)(value >> 8);
        }
    }

    static void Close(PlaneBuffer first, PlaneBuffer second, PlaneBuffer third)
    {
        first.Close();
        second.Close();
        third.Close();
    }
}
