namespace avif.Tests;

public class Av1IntraPredictionTests
{
    [Test]
    public void DcPredictionAveragesAvailableAboveAndLeftEdges()
    {
        PlaneBuffer plane = CreatePlane(8, 8, 8);
        SetHorizontal(plane, 1, 2, new int[] { 10, 20, 30, 40 });
        SetVertical(plane, 2, 1, new int[] { 50, 60, 70, 80 });
        Av1IntraPredictionRequest request = CreateRequest(
            plane, 2, 2, 4, 4, 8, Av1PredictionMode.Dc);
        request.HaveAbove = true;
        request.HaveLeft = true;

        AvifResultCode code = Av1IntraPredictor.Predict(request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadBlock(plane, 2, 2, 4, 4),
                Is.EqualTo(Repeat(45, 16)));
        });
        plane.Close();
    }

    [Test]
    public void VerticalHorizontalAndPaethUseTheirNormativeEdges()
    {
        PlaneBuffer verticalPlane = CreateEdgePlane();
        Av1IntraPredictionRequest vertical = CreateRequest(
            verticalPlane, 2, 2, 4, 4, 8, Av1PredictionMode.Vertical);
        vertical.HaveAbove = true;
        vertical.HaveLeft = true;
        PlaneBuffer horizontalPlane = CreateEdgePlane();
        Av1IntraPredictionRequest horizontal = CreateRequest(
            horizontalPlane, 2, 2, 4, 4, 8, Av1PredictionMode.Horizontal);
        horizontal.HaveAbove = true;
        horizontal.HaveLeft = true;
        PlaneBuffer paethPlane = CreateEdgePlane();
        Av1IntraPredictionRequest paeth = CreateRequest(
            paethPlane, 2, 2, 4, 4, 8, Av1PredictionMode.Paeth);
        paeth.HaveAbove = true;
        paeth.HaveLeft = true;

        AvifResultCode verticalCode = Av1IntraPredictor.Predict(vertical);
        AvifResultCode horizontalCode = Av1IntraPredictor.Predict(horizontal);
        AvifResultCode paethCode = Av1IntraPredictor.Predict(paeth);

        Assert.Multiple(() =>
        {
            Assert.That(verticalCode, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadBlock(verticalPlane, 2, 2, 4, 1),
                Is.EqualTo(new int[] { 20, 30, 40, 50 }));
            Assert.That(ReadBlock(verticalPlane, 2, 5, 4, 1),
                Is.EqualTo(new int[] { 20, 30, 40, 50 }));
            Assert.That(horizontalCode, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadBlock(horizontalPlane, 2, 2, 4, 4),
                Is.EqualTo(new int[]
                {
                    60,60,60,60, 70,70,70,70,
                    80,80,80,80, 90,90,90,90
                }));
            Assert.That(paethCode, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadSample(paethPlane, 2, 2), Is.EqualTo(60));
            Assert.That(ReadSample(paethPlane, 5, 5), Is.EqualTo(90));
        });

        verticalPlane.Close();
        horizontalPlane.Close();
        paethPlane.Close();
    }

    [Test]
    public void EveryDirectionalModeSupportsAngleDeltasAndRectangularBlocks()
    {
        Av1PredictionMode[] modes = new Av1PredictionMode[]
        {
            Av1PredictionMode.Directional45,
            Av1PredictionMode.Directional135,
            Av1PredictionMode.Directional113,
            Av1PredictionMode.Directional157,
            Av1PredictionMode.Directional203,
            Av1PredictionMode.Directional67
        };
        int modeIndex = 0;
        while (modeIndex < modes.Length)
        {
            PlaneBuffer plane = CreatePlane(40, 40, 10);
            int index = 0;
            while (index < 32)
            {
                WriteSample(plane, 7, 8 + index, 100 + index * 3);
                WriteSample(plane, 8 + index, 7, 500 + index * 2);
                index++;
            }

            WriteSample(plane, 7, 7, 300);
            Av1IntraPredictionRequest request = CreateRequest(
                plane, 8, 8, 16, 4, 10, modes[modeIndex]);
            request.HaveAbove = true;
            request.HaveLeft = true;
            request.HaveAboveRight = true;
            request.HaveBelowLeft = true;
            request.AngleDelta = modeIndex % 7 - 3;

            AvifResultCode code = Av1IntraPredictor.Predict(request);
            int[] pixels = ReadBlock(plane, 8, 8, 16, 4);
            Assert.Multiple(() =>
            {
                Assert.That(code, Is.EqualTo(AvifResultCode.Success));
                Assert.That(AllInRange(pixels, 0, 1023), Is.True);
                Assert.That(HasVariation(pixels), Is.True);
            });
            plane.Close();
            modeIndex++;
        }

        PlaneBuffer exactPlane = CreatePlane(16, 16, 8);
        SetHorizontal(exactPlane, 1, 2, new int[] { 10, 20, 30, 40, 50, 60, 70, 80 });
        Av1IntraPredictionRequest exact = CreateRequest(
            exactPlane, 2, 2, 4, 4, 8, Av1PredictionMode.Directional45);
        exact.HaveAbove = true;
        exact.HaveAboveRight = true;
        Assert.That(Av1IntraPredictor.Predict(exact), Is.EqualTo(AvifResultCode.Success));
        Assert.That(ReadSample(exactPlane, 2, 2), Is.EqualTo(20));
        exactPlane.Close();
    }

    [Test]
    public void SmoothModesUseAv1QuadraticWeights()
    {
        Av1PredictionMode[] modes = new Av1PredictionMode[]
        {
            Av1PredictionMode.Smooth,
            Av1PredictionMode.SmoothVertical,
            Av1PredictionMode.SmoothHorizontal
        };
        int[] expectedFirst = new int[] { 20, 10, 30 };
        int index = 0;
        while (index < modes.Length)
        {
            PlaneBuffer plane = CreatePlane(8, 8, 8);
            SetHorizontal(plane, 1, 2, new int[] { 10, 10, 10, 10 });
            SetVertical(plane, 2, 1, new int[] { 30, 30, 30, 30 });
            Av1IntraPredictionRequest request = CreateRequest(
                plane, 2, 2, 4, 4, 8, modes[index]);
            request.HaveAbove = true;
            request.HaveLeft = true;

            Assert.That(Av1IntraPredictor.Predict(request),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadSample(plane, 2, 2), Is.EqualTo(expectedFirst[index]));
            plane.Close();
            index++;
        }
    }

    [Test]
    public void FilterIntraAppliesTheSelectedRecursiveTapSet()
    {
        PlaneBuffer plane = CreateEdgePlane();
        Av1IntraPredictionRequest request = CreateRequest(
            plane, 2, 2, 4, 4, 8, Av1PredictionMode.Dc);
        request.HaveAbove = true;
        request.HaveLeft = true;
        request.UseFilterIntra = true;
        request.FilterIntraMode = Av1FilterIntraMode.FilterVertical;

        AvifResultCode code = Av1IntraPredictor.Predict(request);
        int expectedFirst = (-10 * 10 + 16 * 20 + 10 * 60 + 8) >> 4;

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadSample(plane, 2, 2), Is.EqualTo(expectedFirst));
            Assert.That(HasVariation(ReadBlock(plane, 2, 2, 4, 4)), Is.True);
        });
        plane.Close();
    }

    [Test]
    public void CflSubsamples420LumaSubtractsAverageAndAppliesSignedAlpha()
    {
        PlaneBuffer chroma = CreatePlane(4, 4, 8);
        int[] luma = new int[64];
        int chromaRow = 0;
        while (chromaRow < 4)
        {
            int chromaColumn = 0;
            while (chromaColumn < 4)
            {
                int value = chromaRow * 4 + chromaColumn;
                int row = 0;
                while (row < 2)
                {
                    int column = 0;
                    while (column < 2)
                    {
                        luma[(chromaRow * 2 + row) * 8 + chromaColumn * 2 + column] = value;
                        column++;
                    }

                    row++;
                }

                chromaColumn++;
            }

            chromaRow++;
        }

        Av1IntraPredictionRequest request = CreateRequest(
            chroma, 0, 0, 4, 4, 8, Av1PredictionMode.Dc);
        request.UseCfl = true;
        request.CflLuma = luma;
        request.CflLumaWidth = 8;
        request.CflLumaHeight = 8;
        request.CflLumaStride = 8;
        request.SubsamplingX = true;
        request.SubsamplingY = true;
        request.CflAlphaQ3 = 8;

        AvifResultCode code = Av1IntraPredictor.Predict(request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadSample(chroma, 0, 0), Is.EqualTo(120));
            Assert.That(ReadSample(chroma, 3, 3), Is.EqualTo(136));
        });
        chroma.Close();
    }

    [Test]
    public void PaletteSupportsHighBitDepthAndRejectsInvalidColorIndexes()
    {
        PlaneBuffer plane = CreatePlane(8, 4, 12);
        Av1IntraPredictionRequest request = CreateRequest(
            plane, 0, 0, 4, 4, 12, Av1PredictionMode.Dc);
        request.UsePalette = true;
        request.PaletteColors = new int[] { 0, 1024, 4095 };
        request.PaletteStride = 4;
        request.PaletteMap = new byte[]
        {
            0,1,2,1, 2,1,0,1, 0,0,2,2, 1,2,1,0
        };

        AvifResultCode code = Av1IntraPredictor.Predict(request);
        request.X = 4;
        request.PaletteMap[15] = 3;
        AvifResultCode invalid = Av1IntraPredictor.Predict(request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadBlock(plane, 0, 0, 4, 1),
                Is.EqualTo(new int[] { 0, 1024, 4095, 1024 }));
            Assert.That(invalid, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
        });
        plane.Close();
    }

    [Test]
    public void IntraBlockCopyUsesOnlyPreviouslyDecodedNonOverlappingRegions()
    {
        PlaneBuffer plane = CreatePlane(12, 12, 8);
        int row = 0;
        while (row < 4)
        {
            int column = 0;
            while (column < 4)
            {
                WriteSample(plane, row, column, row * 4 + column + 1);
                column++;
            }

            row++;
        }

        Av1IntraPredictionRequest valid = CreateRequest(
            plane, 4, 4, 4, 4, 8, Av1PredictionMode.Dc);
        valid.UseIntraBlockCopy = true;
        valid.IntraBlockCopySourceX = 0;
        valid.IntraBlockCopySourceY = 0;
        Av1IntraPredictionRequest future = CreateRequest(
            plane, 0, 4, 4, 4, 8, Av1PredictionMode.Dc);
        future.UseIntraBlockCopy = true;
        future.IntraBlockCopySourceX = 4;
        future.IntraBlockCopySourceY = 4;

        AvifResultCode validCode = Av1IntraPredictor.Predict(valid);
        AvifResultCode futureCode = Av1IntraPredictor.Predict(future);

        Assert.Multiple(() =>
        {
            Assert.That(validCode, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadBlock(plane, 4, 4, 4, 4),
                Is.EqualTo(CreateSequence(1, 16)));
            Assert.That(futureCode, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
        });
        plane.Close();
    }

    [Test]
    public void AllLegalTransformLayoutsWorkAtEveryAv1BitDepth()
    {
        int[] bitDepths = new int[] { 8, 10, 12 };
        int bitDepthIndex = 0;
        while (bitDepthIndex < bitDepths.Length)
        {
            int size = 0;
            while (size < (int)Av1TransformSize.Invalid)
            {
                int width = Av1ConversionTables.GetTransformWidth((Av1TransformSize)size);
                int height = Av1ConversionTables.GetTransformHeight((Av1TransformSize)size);
                if (width > 0 && height > 0)
                {
                    PlaneBuffer plane = CreatePlane(width, height, bitDepths[bitDepthIndex]);
                    Av1IntraPredictionRequest request = CreateRequest(
                        plane,
                        0,
                        0,
                        width,
                        height,
                        bitDepths[bitDepthIndex],
                        Av1PredictionMode.Dc);
                    AvifResultCode code = Av1IntraPredictor.Predict(request);
                    Assert.That(code, Is.EqualTo(AvifResultCode.Success));
                    Assert.That(ReadSample(plane, height - 1, width - 1),
                        Is.EqualTo(1 << (bitDepths[bitDepthIndex] - 1)));
                    plane.Close();
                }

                size++;
            }

            bitDepthIndex++;
        }
    }

    static PlaneBuffer CreateEdgePlane()
    {
        PlaneBuffer plane = CreatePlane(8, 8, 8);
        SetHorizontal(plane, 1, 2, new int[] { 20, 30, 40, 50 });
        SetVertical(plane, 2, 1, new int[] { 60, 70, 80, 90 });
        WriteSample(plane, 1, 1, 10);
        return plane;
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

    static Av1IntraPredictionRequest CreateRequest(
        PlaneBuffer plane,
        int x,
        int y,
        int width,
        int height,
        int bitDepth,
        Av1PredictionMode mode)
    {
        Av1IntraPredictionRequest request = new Av1IntraPredictionRequest();
        request.Destination = plane;
        request.X = x;
        request.Y = y;
        request.Width = width;
        request.Height = height;
        request.BitDepth = bitDepth;
        request.Mode = mode;
        return request;
    }

    static void SetHorizontal(PlaneBuffer plane, int row, int x, int[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            WriteSample(plane, row, x + index, values[index]);
            index++;
        }
    }

    static void SetVertical(PlaneBuffer plane, int y, int column, int[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            WriteSample(plane, y + index, column, values[index]);
            index++;
        }
    }

    static int[] ReadBlock(PlaneBuffer plane, int x, int y, int width, int height)
    {
        int[] values = new int[width * height];
        int row = 0;
        while (row < height)
        {
            int column = 0;
            while (column < width)
            {
                values[row * width + column] = ReadSample(plane, y + row, x + column);
                column++;
            }

            row++;
        }

        return values;
    }

    static int ReadSample(PlaneBuffer plane, int row, int column)
    {
        Span<byte> bytes = plane.GetRowSpan(row);
        int index = column * plane.BytesPerSample;
        return plane.BytesPerSample == 1
            ? bytes[index]
            : bytes[index] | bytes[index + 1] << 8;
    }

    static void WriteSample(PlaneBuffer plane, int row, int column, int value)
    {
        Span<byte> bytes = plane.GetRowSpan(row);
        int index = column * plane.BytesPerSample;
        bytes[index] = (byte)value;
        if (plane.BytesPerSample == 2)
        {
            bytes[index + 1] = (byte)(value >> 8);
        }
    }

    static int[] Repeat(int value, int count)
    {
        int[] values = new int[count];
        System.Array.Fill(values, value);
        return values;
    }

    static int[] CreateSequence(int first, int count)
    {
        int[] values = new int[count];
        int index = 0;
        while (index < count)
        {
            values[index] = first + index;
            index++;
        }

        return values;
    }

    static bool AllInRange(int[] values, int minimum, int maximum)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] < minimum || values[index] > maximum)
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static bool HasVariation(int[] values)
    {
        int index = 1;
        while (index < values.Length)
        {
            if (values[index] != values[0])
            {
                return true;
            }

            index++;
        }

        return false;
    }
}
