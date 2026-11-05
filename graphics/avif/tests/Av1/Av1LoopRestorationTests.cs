namespace avif.Tests;

public class Av1LoopRestorationTests
{
    [TestCase(8)]
    [TestCase(10)]
    [TestCase(12)]
    public void WienerImpulseMatchesNormativeRounding(int bitDepth)
    {
        Av1LoopRestorationRequest request = CreateRequest(
            9, 1, bitDepth, false, false, false,
            Av1RestorationType.Wiener, 64);
        Av1RestorationUnit unit = request.Units[0][0];
        unit.Type = Av1RestorationType.Wiener;
        unit.WienerHorizontal[2] = 16;
        int scale = 1 << (bitDepth - 8);
        WriteSample(request.CdefPlanes[0], 4, 0, 255 * scale);
        CopyPlane(request.CdefPlanes[0], request.CurrentPlanes[0]);

        AvifResultCode code = Av1LoopRestoration.Filter(request);

        int side = (255 * scale * 16 + 64) >> 7;
        int center = (255 * scale * 96 + 64) >> 7;
        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadSample(request.DestinationPlanes[0], 2, 0), Is.Zero);
            Assert.That(ReadSample(request.DestinationPlanes[0], 3, 0), Is.EqualTo(side));
            Assert.That(ReadSample(request.DestinationPlanes[0], 4, 0), Is.EqualTo(center));
            Assert.That(ReadSample(request.DestinationPlanes[0], 5, 0), Is.EqualTo(side));
            Assert.That(ReadSample(request.DestinationPlanes[0], 6, 0), Is.Zero);
        });
        Close(request);
    }

    [TestCase(8)]
    [TestCase(10)]
    [TestCase(12)]
    public void IdentityWienerPreservesOddSizedColorPlanes(int bitDepth)
    {
        Av1LoopRestorationRequest request = CreateRequest(
            7, 5, bitDepth, true, true, true,
            Av1RestorationType.Wiener, 64);
        int plane = 0;
        while (plane < request.Units.Length)
        {
            request.Units[plane][0].Type = Av1RestorationType.Wiener;
            FillPattern(request.CdefPlanes[plane], bitDepth, plane * 13);
            Fill(request.CurrentPlanes[plane], 0);
            plane++;
        }

        AvifResultCode code = Av1LoopRestoration.Filter(request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(request.DestinationPlanes[1].Width, Is.EqualTo(4));
            Assert.That(request.DestinationPlanes[1].Height, Is.EqualTo(3));
            Assert.That(ReadPlane(request.DestinationPlanes[0]),
                Is.EqualTo(ReadPlane(request.CdefPlanes[0])));
            Assert.That(ReadPlane(request.DestinationPlanes[1]),
                Is.EqualTo(ReadPlane(request.CdefPlanes[1])));
            Assert.That(ReadPlane(request.DestinationPlanes[2]),
                Is.EqualTo(ReadPlane(request.CdefPlanes[2])));
        });
        Close(request);
    }

    [Test]
    public void WienerStripeBoundaryUsesDeblockedSecondContextLine()
    {
        Av1LoopRestorationRequest request = CreateRequest(
            4, 64, 8, false, false, false,
            Av1RestorationType.Wiener, 64);
        Av1RestorationUnit unit = request.Units[0][0];
        unit.Type = Av1RestorationType.Wiener;
        unit.WienerVertical[0] = 10;
        Fill(request.CdefPlanes[0], 0);
        Fill(request.CurrentPlanes[0], 0);
        WriteRow(request.CurrentPlanes[0], 53, 50);
        WriteRow(request.CurrentPlanes[0], 54, 200);
        WriteRow(request.CdefPlanes[0], 54, 250);

        AvifResultCode code = Av1LoopRestoration.Filter(request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadSample(request.DestinationPlanes[0], 0, 56),
                Is.EqualTo(16));
            Assert.That(ReadSample(request.DestinationPlanes[0], 3, 56),
                Is.EqualTo(16));
        });
        Close(request);
    }

    [Test]
    public void EverySelfGuidedSetPreservesConstantsAtEveryBitDepth()
    {
        int[] bitDepths = new int[] { 8,10,12 };
        int depthIndex = 0;
        while (depthIndex < bitDepths.Length)
        {
            int set = 0;
            while (set < 16)
            {
                Av1LoopRestorationRequest request = CreateRequest(
                    7, 5, bitDepths[depthIndex], true, true, true,
                    Av1RestorationType.Sgrproj, 64);
                int value = 173 << (bitDepths[depthIndex] - 8);
                int plane = 0;
                while (plane < request.Units.Length)
                {
                    Av1RestorationUnit unit = request.Units[plane][0];
                    unit.Type = Av1RestorationType.Sgrproj;
                    unit.SgrSet = set;
                    unit.SgrWeights[0] = Av1LoopRestoration.GetSgrParameter(set, 0)
                        == 0 ? 0 : -32;
                    unit.SgrWeights[1] = 31;
                    Fill(request.CurrentPlanes[plane], value + plane);
                    Fill(request.CdefPlanes[plane], value + plane);
                    plane++;
                }

                AvifResultCode code = Av1LoopRestoration.Filter(request);

                Assert.That(code, Is.EqualTo(AvifResultCode.Success));
                plane = 0;
                while (plane < request.DestinationPlanes.Length)
                {
                    Assert.That(ReadPlane(request.DestinationPlanes[plane]),
                        Is.All.InRange(value + plane - 1, value + plane + 1),
                        $"bitDepth={bitDepths[depthIndex]}, set={set}, plane={plane}");
                    plane++;
                }
                Close(request);
                set++;
            }
            depthIndex++;
        }
    }

    [Test]
    public void SelfGuidedImpulseIsSymmetricAndBounded()
    {
        Av1LoopRestorationRequest request = CreateRequest(
            9, 9, 8, false, false, false,
            Av1RestorationType.Sgrproj, 64);
        Av1RestorationUnit unit = request.Units[0][0];
        unit.Type = Av1RestorationType.Sgrproj;
        unit.SgrSet = 0;
        unit.SgrWeights[0] = 31;
        unit.SgrWeights[1] = 95;
        Fill(request.CdefPlanes[0], 100);
        WriteSample(request.CdefPlanes[0], 4, 4, 150);
        CopyPlane(request.CdefPlanes[0], request.CurrentPlanes[0]);

        AvifResultCode code = Av1LoopRestoration.Filter(request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadPlane(request.DestinationPlanes[0]),
                Is.All.InRange(0, 255));
            Assert.That(ReadSample(request.DestinationPlanes[0], 4, 4),
                Is.EqualTo(149));
            Assert.That(ReadSample(request.DestinationPlanes[0], 3, 4),
                Is.EqualTo(ReadSample(request.DestinationPlanes[0], 5, 4)));
            Assert.That(ReadSample(request.DestinationPlanes[0], 4, 3),
                Is.EqualTo(ReadSample(request.DestinationPlanes[0], 4, 5)));
            Assert.That(ReadSample(request.DestinationPlanes[0], 3, 4),
                Is.EqualTo(100));
        });
        Close(request);
    }

    [Test]
    public void SwitchableUnitsRemainIndependentAcrossTileBoundary()
    {
        Av1LoopRestorationRequest request = CreateRequest(
            96, 4, 8, false, false, false,
            Av1RestorationType.Switchable, 64);
        (request.Header ?? throw new InvalidOperationException("Invalid internal state.")).Tiles = new Av1TileInfo();
        (request.Header.Tiles ?? throw new InvalidOperationException("Invalid internal state.")).TileColumns = 2;
        request.Header.Tiles.TileRows = 1;
        request.Header.Tiles.MiColumnStarts = new int[] { 0,16,24 };
        request.Header.Tiles.MiRowStarts = new int[] { 0,1 };
        request.Units[0][0].Type = Av1RestorationType.Wiener;
        request.Units[0][0].WienerHorizontal[2] = 16;
        request.Units[0][1].Type = Av1RestorationType.None;
        WriteSample(request.CdefPlanes[0], 20, 1, 255);
        int x = 64;
        while (x < 96)
        {
            int y = 0;
            while (y < 4)
            {
                WriteSample(request.CdefPlanes[0], x, y, 40 + (x + y) % 90);
                y++;
            }
            x++;
        }
        CopyPlane(request.CdefPlanes[0], request.CurrentPlanes[0]);
        int[] rightBefore = ReadRegion(request.CdefPlanes[0], 64, 0, 32, 4);

        AvifResultCode code = Av1LoopRestoration.Filter(request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadSample(request.DestinationPlanes[0], 20, 1),
                Is.LessThan(255));
            Assert.That(ReadSample(request.DestinationPlanes[0], 19, 1),
                Is.GreaterThan(0));
            Assert.That(ReadRegion(request.DestinationPlanes[0], 64, 0, 32, 4),
                Is.EqualTo(rightBefore));
        });
        Close(request);
    }

    [Test]
    public void UnitRowsUseTheNormativeEightPixelVerticalOffset()
    {
        Av1LoopRestorationRequest request = CreateRequest(
            4, 96, 8, false, false, false,
            Av1RestorationType.Switchable, 64);
        request.Units[0][0].Type = Av1RestorationType.None;
        request.Units[0][1].Type = Av1RestorationType.Wiener;
        request.Units[0][1].WienerVertical[2] = 16;
        WriteRow(request.CdefPlanes[0], 52, 255);
        WriteRow(request.CdefPlanes[0], 56, 255);
        CopyPlane(request.CdefPlanes[0], request.CurrentPlanes[0]);

        AvifResultCode code = Av1LoopRestoration.Filter(request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadSample(request.DestinationPlanes[0], 0, 52),
                Is.EqualTo(255));
            Assert.That(ReadSample(request.DestinationPlanes[0], 0, 56),
                Is.EqualTo(191));
        });
        Close(request);
    }

    [Test]
    public void DisabledRestorationCopiesCdefOutput()
    {
        Av1LoopRestorationRequest request = CreateRequest(
            7, 5, 10, true, true, true,
            Av1RestorationType.None, 0);
        int plane = 0;
        while (plane < request.CdefPlanes.Length)
        {
            FillPattern(request.CdefPlanes[plane], 10, plane * 7);
            Fill(request.CurrentPlanes[plane], 0);
            plane++;
        }

        AvifResultCode code = Av1LoopRestoration.Filter(request);

        Assert.That(code, Is.EqualTo(AvifResultCode.Success));
        plane = 0;
        while (plane < request.CdefPlanes.Length)
        {
            Assert.That(ReadPlane(request.DestinationPlanes[plane]),
                Is.EqualTo(ReadPlane(request.CdefPlanes[plane])));
            plane++;
        }
        Close(request);
    }

    [Test]
    public void InvalidUnitLeavesDestinationUntouched()
    {
        Av1LoopRestorationRequest request = CreateRequest(
            8, 8, 8, false, false, false,
            Av1RestorationType.Wiener, 64);
        request.Units[0][0].Type = Av1RestorationType.Wiener;
        request.Units[0][0].WienerHorizontal[1] = 9;
        Fill(request.CurrentPlanes[0], 10);
        Fill(request.CdefPlanes[0], 20);
        Fill(request.DestinationPlanes[0], 199);

        AvifResultCode code = Av1LoopRestoration.Filter(request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(ReadPlane(request.DestinationPlanes[0]), Is.All.EqualTo(199));
        });
        Close(request);
    }

    static Av1LoopRestorationRequest CreateRequest(
        int width,
        int height,
        int bitDepth,
        bool color,
        bool subsamplingX,
        bool subsamplingY,
        Av1RestorationType frameType,
        int unitSize)
    {
        Av1LoopRestorationRequest request = new Av1LoopRestorationRequest();
        request.Header = new ParsedAv1FrameHeader();
        request.Header.Size = new Av1FrameSize();
        request.Header.Size.UpscaledWidth = width;
        request.Header.Size.FrameWidth = width;
        request.Header.Size.FrameHeight = height;
        request.Header.Size.MiColumns = (width + 3) >> 2;
        request.Header.Size.MiRows = (height + 3) >> 2;
        request.Header.LoopRestoration = new Av1LoopRestorationParameters();
        request.Header.LoopRestoration.Types = new Av1RestorationType[]
        {
            frameType,
            color ? frameType : Av1RestorationType.None,
            color ? frameType : Av1RestorationType.None
        };
        request.Header.LoopRestoration.UnitSizes = new int[]
        {
            unitSize,unitSize,unitSize
        };
        request.Header.LoopRestoration.UsesRestoration =
            frameType != Av1RestorationType.None;
        request.BitDepth = bitDepth;
        request.SubsamplingX = subsamplingX;
        request.SubsamplingY = subsamplingY;
        int planeCount = color ? 3 : 1;
        request.CurrentPlanes = new PlaneBuffer[planeCount];
        request.CdefPlanes = new PlaneBuffer[planeCount];
        request.DestinationPlanes = new PlaneBuffer[planeCount];
        request.Units = new Av1RestorationUnit[planeCount][];
        int plane = 0;
        while (plane < planeCount)
        {
            int subX = plane == 0 || !subsamplingX ? 0 : 1;
            int subY = plane == 0 || !subsamplingY ? 0 : 1;
            int planeWidth = Round2(width, subX);
            int planeHeight = Round2(height, subY);
            request.CurrentPlanes[plane] = CreatePlane(
                planeWidth, planeHeight, bitDepth);
            request.CdefPlanes[plane] = CreatePlane(
                planeWidth, planeHeight, bitDepth);
            request.DestinationPlanes[plane] = CreatePlane(
                planeWidth, planeHeight, bitDepth);
            int unitCount = frameType == Av1RestorationType.None
                ? 0
                : Av1LoopRestoration.CountUnits(unitSize, planeWidth)
                    * Av1LoopRestoration.CountUnits(unitSize, planeHeight);
            request.Units[plane] = new Av1RestorationUnit[unitCount];
            int unit = 0;
            while (unit < unitCount)
            {
                request.Units[plane][unit] = new Av1RestorationUnit();
                unit++;
            }
            plane++;
        }
        return request;
    }

    static PlaneBuffer CreatePlane(int width, int height, int bitDepth)
    {
        return new PlaneBuffer(
            width, height, bitDepth > 8 ? 2 : 1, 1, 1 << 22, true);
    }

    static int Round2(int value, int shift)
    {
        return (value + (1 << shift) - 1) >> shift;
    }

    static void Fill(PlaneBuffer plane, int value)
    {
        int y = 0;
        while (y < plane.Height)
        {
            WriteRow(plane, y, value);
            y++;
        }
    }

    static void WriteRow(PlaneBuffer plane, int y, int value)
    {
        int x = 0;
        while (x < plane.Width)
        {
            WriteSample(plane, x, y, value);
            x++;
        }
    }

    static void FillPattern(PlaneBuffer plane, int bitDepth, int offset)
    {
        int maximum = (1 << bitDepth) - 1;
        int scale = 1 << (bitDepth - 8);
        int y = 0;
        while (y < plane.Height)
        {
            int x = 0;
            while (x < plane.Width)
            {
                int value = (31 + offset + x * 9 + y * 13) * scale;
                WriteSample(plane, x, y, System.Math.Min(maximum, value));
                x++;
            }
            y++;
        }
    }

    static void CopyPlane(PlaneBuffer source, PlaneBuffer destination)
    {
        int y = 0;
        while (y < source.Height)
        {
            int x = 0;
            while (x < source.Width)
            {
                WriteSample(destination, x, y, ReadSample(source, x, y));
                x++;
            }
            y++;
        }
    }

    static int[] ReadPlane(PlaneBuffer plane)
    {
        return ReadRegion(plane, 0, 0, plane.Width, plane.Height);
    }

    static int[] ReadRegion(
        PlaneBuffer plane,
        int x,
        int y,
        int width,
        int height)
    {
        int[] values = new int[width * height];
        int row = 0;
        while (row < height)
        {
            int column = 0;
            while (column < width)
            {
                values[row * width + column] =
                    ReadSample(plane, x + column, y + row);
                column++;
            }
            row++;
        }
        return values;
    }

    static int ReadSample(PlaneBuffer plane, int x, int y)
    {
        Span<byte> row = plane.GetRowSpan(y);
        int offset = x * plane.BytesPerSample;
        return plane.BytesPerSample == 1
            ? row[offset]
            : row[offset] | row[offset + 1] << 8;
    }

    static void WriteSample(PlaneBuffer plane, int x, int y, int value)
    {
        Span<byte> row = plane.GetRowSpan(y);
        int offset = x * plane.BytesPerSample;
        row[offset] = (byte)value;
        if (plane.BytesPerSample == 2)
        {
            row[offset + 1] = (byte)(value >> 8);
        }
    }

    static void Close(Av1LoopRestorationRequest request)
    {
        int plane = 0;
        while (plane < request.CurrentPlanes.Length)
        {
            request.CurrentPlanes[plane].Close();
            request.CdefPlanes[plane].Close();
            request.DestinationPlanes[plane].Close();
            plane++;
        }
    }
}
