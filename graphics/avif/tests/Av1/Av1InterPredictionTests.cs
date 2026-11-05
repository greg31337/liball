namespace avif.Tests;

public class Av1InterPredictionTests
{
    [Test]
    public void IntegerMotionCopiesTheRequestedReferenceBlock()
    {
        PlaneBuffer reference = CreatePlane(8, 8, 8);
        PlaneBuffer destination = CreatePlane(4, 4, 8);
        FillGradient(reference, 10, 1);
        Av1InterPredictionRequest request = CreateRequest(
            destination, reference, 4, 4, 8);
        request.MotionVector = new Av1MotionVector(8, 16);

        AvifResultCode code = Av1InterPredictor.Predict(request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadBlock(destination, 4, 4), Is.EqualTo(new int[]
            {
                12,13,14,15,
                22,23,24,25,
                32,33,34,35,
                42,43,44,45
            }));
        });
        reference.Close();
        destination.Close();
    }

    [Test]
    public void BilinearFilterInterpolatesHalfPixelMotion()
    {
        PlaneBuffer reference = CreatePlane(8, 4, 8);
        PlaneBuffer destination = CreatePlane(4, 4, 8);
        FillGradient(reference, 0, 10);
        Av1InterPredictionRequest request = CreateRequest(
            destination, reference, 4, 4, 8);
        request.MotionVector = new Av1MotionVector(0, 4);
        request.HorizontalFilter = Av1InterpolationFilter.Bilinear;
        request.VerticalFilter = Av1InterpolationFilter.Bilinear;

        AvifResultCode code = Av1InterPredictor.Predict(request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadBlock(destination, 4, 4), Is.EqualTo(new int[]
            {
                5,15,25,35,
                5,15,25,35,
                5,15,25,35,
                5,15,25,35
            }));
        });
        reference.Close();
        destination.Close();
    }

    [Test]
    public void EverySwitchableSubpixelKernelUsesItsNormativeCoefficients()
    {
        Av1InterpolationFilter[] filters = new Av1InterpolationFilter[]
        {
            Av1InterpolationFilter.EightTap,
            Av1InterpolationFilter.EightTapSmooth,
            Av1InterpolationFilter.EightTapSharp,
            Av1InterpolationFilter.Bilinear
        };
        int[] expected = new int[] { 76,52,80,64 };
        int index = 0;
        while (index < filters.Length)
        {
            PlaneBuffer reference = CreatePlane(12, 8, 8);
            PlaneBuffer destination = CreatePlane(8, 8, 8);
            WriteSample(reference, 0, 4, 128);
            Av1InterPredictionRequest request = CreateRequest(
                destination, reference, 8, 8, 8);
            request.MotionVector = new Av1MotionVector(0, 36);
            request.HorizontalFilter = filters[index];

            AvifResultCode code = Av1InterPredictor.Predict(request);

            Assert.Multiple(() =>
            {
                Assert.That(code, Is.EqualTo(AvifResultCode.Success));
                Assert.That(ReadSample(destination, 0, 0), Is.EqualTo(expected[index]));
            });
            reference.Close();
            destination.Close();
            index++;
        }
    }

    [Test]
    public void ScaledReferencesUseIndependentHorizontalAndVerticalRatios()
    {
        PlaneBuffer reference = CreatePlane(8, 8, 8);
        PlaneBuffer destination = CreatePlane(4, 4, 8);
        FillGradient(reference, 10, 1);
        Av1InterPredictionRequest request = CreateRequest(
            destination, reference, 4, 4, 8);
        request.ScaleXNumerator = 2;
        request.ScaleYNumerator = 2;

        AvifResultCode code = Av1InterPredictor.Predict(request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadBlock(destination, 4, 4), Is.EqualTo(new int[]
            {
                0,2,4,6,
                20,22,24,26,
                40,42,44,46,
                60,62,64,66
            }));
        });
        reference.Close();
        destination.Close();
    }

    [Test]
    public void GlobalTranslationAndLocalAffineWarpMapReferenceCoordinates()
    {
        PlaneBuffer reference = CreatePlane(12, 8, 8);
        FillGradient(reference, 10, 1);
        PlaneBuffer globalDestination = CreatePlane(4, 4, 8);
        Av1InterPredictionRequest global = CreateRequest(
            globalDestination, reference, 4, 4, 8);
        global.GlobalMotionType = Av1WarpModelType.Translation;
        global.WarpParameters = new int[] { 65536,65536,65536,0,0,65536 };
        PlaneBuffer localDestination = CreatePlane(4, 4, 8);
        Av1InterPredictionRequest local = CreateRequest(
            localDestination, reference, 4, 4, 8);
        local.MotionMode = Av1MotionMode.LocalWarp;
        local.WarpParameters = new int[] { 0,0,131072,0,0,65536 };

        AvifResultCode globalCode = Av1InterPredictor.Predict(global);
        AvifResultCode localCode = Av1InterPredictor.Predict(local);

        Assert.Multiple(() =>
        {
            Assert.That(globalCode, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadBlock(globalDestination, 4, 1),
                Is.EqualTo(new int[] { 11,12,13,14 }));
            Assert.That(localCode, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadBlock(localDestination, 4, 1),
                Is.EqualTo(new int[] { 0,2,4,6 }));
        });
        reference.Close();
        globalDestination.Close();
        localDestination.Close();
    }

    [Test]
    public void ObmcBlendsAboveThenLeftPredictorsWithNormativeMasks()
    {
        PlaneBuffer reference = CreatePlane(4, 4, 8);
        PlaneBuffer destination = CreatePlane(4, 4, 8);
        Fill(reference, 100);
        Av1InterPredictionRequest request = CreateRequest(
            destination, reference, 4, 4, 8);
        request.MotionMode = Av1MotionMode.Obmc;
        request.AboveOverlap = 4;
        request.AboveStride = 4;
        request.AbovePrediction = Repeat(20, 16);
        request.LeftOverlap = 4;
        request.LeftStride = 4;
        request.LeftPrediction = Repeat(220, 16);

        AvifResultCode code = Av1InterPredictor.Predict(request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadBlock(destination, 4, 4), Is.EqualTo(new int[]
            {
                128,102,81,69,
                137,113,94,83,
                143,122,104,94,
                147,126,109,100
            }));
        });
        reference.Close();
        destination.Close();
    }

    [Test]
    public void ChromaMotionVectorsRetainOneSixteenthSamplePrecision()
    {
        PlaneBuffer reference = CreatePlane(8, 4, 8);
        PlaneBuffer destination = CreatePlane(4, 4, 8);
        FillGradient(reference, 0, 20);
        Av1InterPredictionRequest request = CreateRequest(
            destination, reference, 4, 4, 8);
        request.SubsamplingX = true;
        request.MotionVector = new Av1MotionVector(0, 8);
        request.HorizontalFilter = Av1InterpolationFilter.Bilinear;
        request.VerticalFilter = Av1InterpolationFilter.Bilinear;

        AvifResultCode code = Av1InterPredictor.Predict(request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadBlock(destination, 4, 1),
                Is.EqualTo(new int[] { 10,30,50,70 }));
        });
        reference.Close();
        destination.Close();
    }

    [TestCase(10)]
    [TestCase(12)]
    public void HighBitDepthPredictionPreservesSamplesAndClipsFilterOvershoot(int bitDepth)
    {
        int maximum = (1 << bitDepth) - 1;
        PlaneBuffer reference = CreatePlane(8, 8, bitDepth);
        PlaneBuffer destination = CreatePlane(8, 8, bitDepth);
        Fill(reference, maximum);
        Av1InterPredictionRequest request = CreateRequest(
            destination, reference, 8, 8, bitDepth);
        request.MotionVector = new Av1MotionVector(-1000, 1000);
        request.HorizontalFilter = Av1InterpolationFilter.EightTapSharp;
        request.VerticalFilter = Av1InterpolationFilter.EightTapSharp;

        AvifResultCode code = Av1InterPredictor.Predict(request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadBlock(destination, 8, 8), Is.All.EqualTo(maximum));
        });
        reference.Close();
        destination.Close();
    }

    [Test]
    public void InvalidRequestsDoNotModifyDestination()
    {
        PlaneBuffer reference = CreatePlane(4, 4, 8);
        PlaneBuffer destination = CreatePlane(4, 4, 8);
        Fill(destination, 77);
        Av1InterPredictionRequest invalidFilter = CreateRequest(
            destination, reference, 4, 4, 8);
        invalidFilter.HorizontalFilter = Av1InterpolationFilter.Switchable;
        Av1InterPredictionRequest invalidObmc = CreateRequest(
            destination, reference, 4, 4, 8);
        invalidObmc.MotionMode = Av1MotionMode.Obmc;
        invalidObmc.AboveOverlap = 3;
        invalidObmc.AboveStride = 4;
        invalidObmc.AbovePrediction = new int[12];

        AvifResultCode filterCode = Av1InterPredictor.Predict(invalidFilter);
        AvifResultCode obmcCode = Av1InterPredictor.Predict(invalidObmc);

        Assert.Multiple(() =>
        {
            Assert.That(filterCode, Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(obmcCode, Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(ReadBlock(destination, 4, 4), Is.All.EqualTo(77));
        });
        reference.Close();
        destination.Close();
    }

    static Av1InterPredictionRequest CreateRequest(
        PlaneBuffer destination,
        PlaneBuffer reference,
        int width,
        int height,
        int bitDepth)
    {
        Av1InterPredictionRequest request = new Av1InterPredictionRequest();
        request.Destination = destination;
        request.Reference = reference;
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

    static void FillGradient(PlaneBuffer plane, int rowStep, int columnStep)
    {
        int row = 0;
        while (row < plane.Height)
        {
            int column = 0;
            while (column < plane.Width)
            {
                WriteSample(plane, row, column, row * rowStep + column * columnStep);
                column++;
            }
            row++;
        }
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

    static int[] Repeat(int value, int count)
    {
        int[] values = new int[count];
        int index = 0;
        while (index < count)
        {
            values[index] = value;
            index++;
        }
        return values;
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
}
