namespace avif.Tests;

public class Av1CdefFilterTests
{
    [TestCase(0)]
    [TestCase(2)]
    [TestCase(4)]
    [TestCase(6)]
    public void DirectionSearchFindsCardinalAndDiagonalPatterns(int expectedDirection)
    {
        PlaneBuffer plane = CreatePlane(8, 8, 8);
        int y = 0;
        while (y < 8)
        {
            int x = 0;
            while (x < 8)
            {
                int coordinate = expectedDirection == 0
                    ? x + y
                    : (expectedDirection == 2
                        ? y
                        : (expectedDirection == 4 ? x - y + 7 : x));
                WriteSample(plane, x, y, 32 + coordinate * 12);
                x++;
            }
            y++;
        }

        Av1CdefDirectionResult result =
            Av1CdefFilter.FindDirection(plane, 0, 0, 8);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(result.Direction, Is.EqualTo(expectedDirection));
            Assert.That(result.Variance, Is.GreaterThan(0));
        });
        plane.Close();
    }

    [Test]
    public void UniformBlockHasZeroDirectionalVariance()
    {
        PlaneBuffer plane = CreatePlane(8, 8, 10);
        Fill(plane, 400);

        Av1CdefDirectionResult result =
            Av1CdefFilter.FindDirection(plane, 0, 0, 10);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(result.Direction, Is.Zero);
            Assert.That(result.Variance, Is.Zero);
        });
        plane.Close();
    }

    [Test]
    public void ConstrainMatchesEveryStrengthAndDampingCombination()
    {
        int[] thresholds = new int[] { 0,1,2,3,4,7,8,15 };
        int thresholdIndex = 0;
        while (thresholdIndex < thresholds.Length)
        {
            int damping = 3;
            while (damping <= 8)
            {
                int difference = -64;
                while (difference <= 64)
                {
                    Assert.That(Av1CdefFilter.Constrain(
                        difference, thresholds[thresholdIndex], damping),
                        Is.EqualTo(ReferenceConstrain(
                            difference, thresholds[thresholdIndex], damping)),
                        $"difference={difference}, threshold={thresholds[thresholdIndex]}, damping={damping}");
                    difference++;
                }
                damping++;
            }
            thresholdIndex++;
        }
    }

    [Test]
    public void SecondaryFilterProducesTheNormativeBoundedResult()
    {
        Av1CdefRequest request = CreateRequest(8, 8, 8, false, false, false);
        (((request.Header ?? throw new InvalidOperationException("Invalid internal state.")).Cdef ?? throw new InvalidOperationException("Invalid internal state.")).YPrimaryStrengths ?? throw new InvalidOperationException("Invalid internal state."))[0] = 0;
        ((request.Header.Cdef ?? throw new InvalidOperationException("Invalid internal state.")).YSecondaryStrengths ?? throw new InvalidOperationException("Invalid internal state."))[0] = 4;
        Fill(request.Planes[0], 100);
        WriteSample(request.Planes[0], 4, 4, 104);

        AvifResultCode code = Av1CdefFilter.Filter(request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadSample(request.Planes[0], 4, 4), Is.EqualTo(102));
            Assert.That(ReadSample(request.Planes[0], 0, 0), Is.EqualTo(100));
        });
        Close(request);
    }

    [Test]
    public void PrimaryFilterFollowsTheDetectedDirection()
    {
        Av1CdefRequest request = CreateRequest(8, 8, 8, false, false, false);
        (((request.Header ?? throw new InvalidOperationException("Invalid internal state.")).Cdef ?? throw new InvalidOperationException("Invalid internal state.")).YPrimaryStrengths ?? throw new InvalidOperationException("Invalid internal state."))[0] = 15;
        int y = 0;
        while (y < 8)
        {
            int x = 0;
            while (x < 8)
            {
                WriteSample(request.Planes[0], x, y, 60 + y * 10);
                x++;
            }
            y++;
        }
        WriteSample(request.Planes[0], 4, 4, 104);
        Av1CdefDirectionResult direction =
            Av1CdefFilter.FindDirection(request.Planes[0], 0, 0, 8);

        AvifResultCode code = Av1CdefFilter.Filter(request);

        Assert.Multiple(() =>
        {
            Assert.That(direction.Direction, Is.EqualTo(2));
            Assert.That(direction.Variance, Is.GreaterThan(0));
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadSample(request.Planes[0], 4, 4), Is.LessThan(104));
            Assert.That(ReadSample(request.Planes[0], 4, 4), Is.GreaterThanOrEqualTo(100));
        });
        Close(request);
    }

    [Test]
    public void DisabledStrengthIndexCopiesTheBlockWithoutFiltering()
    {
        Av1CdefRequest request = CreateRequest(8, 8, 8, false, false, false);
        (((request.Header ?? throw new InvalidOperationException("Invalid internal state.")).Cdef ?? throw new InvalidOperationException("Invalid internal state.")).YPrimaryStrengths ?? throw new InvalidOperationException("Invalid internal state."))[0] = 15;
        ((request.Header.Cdef ?? throw new InvalidOperationException("Invalid internal state.")).YSecondaryStrengths ?? throw new InvalidOperationException("Invalid internal state."))[0] = 4;
        request.StrengthIndexes[0] = -1;
        FillPattern(request.Planes[0], 8);
        int[] before = ReadPlane(request.Planes[0]);

        AvifResultCode code = Av1CdefFilter.Filter(request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadPlane(request.Planes[0]), Is.EqualTo(before));
        });
        Close(request);
    }

    [Test]
    public void FullySkippedEightByEightBlockIsNotFiltered()
    {
        Av1CdefRequest request = CreateRequest(8, 8, 8, false, false, false);
        (((request.Header ?? throw new InvalidOperationException("Invalid internal state.")).Cdef ?? throw new InvalidOperationException("Invalid internal state.")).YSecondaryStrengths ?? throw new InvalidOperationException("Invalid internal state."))[0] = 4;
        int index = 0;
        while (index < request.Skips.Length)
        {
            request.Skips[index] = true;
            index++;
        }
        Fill(request.Planes[0], 100);
        WriteSample(request.Planes[0], 4, 4, 104);

        AvifResultCode code = Av1CdefFilter.Filter(request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadSample(request.Planes[0], 4, 4), Is.EqualTo(104));
        });
        Close(request);
    }

    [Test]
    public void ChromaStrengthFiltersSubsampledPlanesIndependently()
    {
        Av1CdefRequest request = CreateRequest(16, 16, 8, true, true, true);
        (((request.Header ?? throw new InvalidOperationException("Invalid internal state.")).Cdef ?? throw new InvalidOperationException("Invalid internal state.")).YPrimaryStrengths ?? throw new InvalidOperationException("Invalid internal state."))[0] = 0;
        ((request.Header.Cdef ?? throw new InvalidOperationException("Invalid internal state.")).YSecondaryStrengths ?? throw new InvalidOperationException("Invalid internal state."))[0] = 0;
        (request.Header.Cdef.UvPrimaryStrengths ?? throw new InvalidOperationException("Invalid internal state."))[0] = 0;
        (request.Header.Cdef.UvSecondaryStrengths ?? throw new InvalidOperationException("Invalid internal state."))[0] = 4;
        Fill(request.Planes[0], 80);
        Fill(request.Planes[1], 100);
        Fill(request.Planes[2], 110);
        WriteSample(request.Planes[1], 2, 2, 102);

        AvifResultCode code = Av1CdefFilter.Filter(request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadPlane(request.Planes[0]), Is.All.EqualTo(80));
            Assert.That(ReadSample(request.Planes[1], 2, 2), Is.LessThan(102));
            Assert.That(ReadPlane(request.Planes[2]), Is.All.EqualTo(110));
        });
        Close(request);
    }

    [TestCase(8)]
    [TestCase(10)]
    [TestCase(12)]
    public void EveryStrengthAndDampingCombinationStaysInSampleRange(int bitDepth)
    {
        int[] secondaryStrengths = new int[] { 0,1,2,4 };
        int damping = 3;
        while (damping <= 6)
        {
            int primary = 0;
            while (primary <= 15)
            {
                int secondaryIndex = 0;
                while (secondaryIndex < secondaryStrengths.Length)
                {
                    Av1CdefRequest request = CreateRequest(
                        8, 8, bitDepth, false, false, false);
                    ((request.Header ?? throw new InvalidOperationException("Invalid internal state.")).Cdef ?? throw new InvalidOperationException("Invalid internal state.")).Damping = damping;
                    ((request.Header.Cdef ?? throw new InvalidOperationException("Invalid internal state.")).YPrimaryStrengths ?? throw new InvalidOperationException("Invalid internal state."))[0] = primary;
                    (request.Header.Cdef.YSecondaryStrengths ?? throw new InvalidOperationException("Invalid internal state."))[0] =
                        secondaryStrengths[secondaryIndex];
                    FillPattern(request.Planes[0], bitDepth);

                    AvifResultCode code = Av1CdefFilter.Filter(request);

                    Assert.That(code, Is.EqualTo(AvifResultCode.Success));
                    Assert.That(ReadPlane(request.Planes[0]),
                        Is.All.InRange(0, (1 << bitDepth) - 1));
                    Close(request);
                    secondaryIndex++;
                }
                primary++;
            }
            damping++;
        }
    }

    [Test]
    public void StrengthIndexSelectsTheRequestedParameterSet()
    {
        Av1CdefRequest disabled = CreateRequest(8, 8, 8, false, false, false);
        ConfigureTwoStrengths(disabled);
        disabled.StrengthIndexes[0] = 0;
        Fill(disabled.Planes[0], 100);
        WriteSample(disabled.Planes[0], 4, 4, 104);
        Av1CdefRequest enabled = CreateRequest(8, 8, 8, false, false, false);
        ConfigureTwoStrengths(enabled);
        enabled.StrengthIndexes[0] = 1;
        Fill(enabled.Planes[0], 100);
        WriteSample(enabled.Planes[0], 4, 4, 104);

        Av1CdefFilter.Filter(disabled);
        Av1CdefFilter.Filter(enabled);

        Assert.Multiple(() =>
        {
            Assert.That(ReadSample(disabled.Planes[0], 4, 4), Is.EqualTo(104));
            Assert.That(ReadSample(enabled.Planes[0], 4, 4), Is.EqualTo(102));
        });
        Close(disabled);
        Close(enabled);
    }

    [Test]
    public void PartialEdgeBlocksAreFilteredWithoutReadingOutsideTheFrame()
    {
        Av1CdefRequest request = CreateRequest(10, 10, 8, false, false, false);
        (((request.Header ?? throw new InvalidOperationException("Invalid internal state.")).Cdef ?? throw new InvalidOperationException("Invalid internal state.")).YSecondaryStrengths ?? throw new InvalidOperationException("Invalid internal state."))[0] = 4;
        Fill(request.Planes[0], 100);
        WriteSample(request.Planes[0], 9, 9, 104);

        AvifResultCode code = Av1CdefFilter.Filter(request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadPlane(request.Planes[0]), Is.All.InRange(0, 255));
        });
        Close(request);
    }

    [Test]
    public void InvalidStrengthMapLeavesTheFrameUntouched()
    {
        Av1CdefRequest request = CreateRequest(8, 8, 8, false, false, false);
        FillPattern(request.Planes[0], 8);
        int[] before = ReadPlane(request.Planes[0]);
        request.StrengthIndexes[0] = 1;

        AvifResultCode code = Av1CdefFilter.Filter(request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(ReadPlane(request.Planes[0]), Is.EqualTo(before));
        });
        Close(request);
    }

    static Av1CdefRequest CreateRequest(
        int width,
        int height,
        int bitDepth,
        bool subsamplingX,
        bool subsamplingY,
        bool color)
    {
        Av1CdefRequest request = new Av1CdefRequest();
        request.Header = new ParsedAv1FrameHeader();
        request.Header.Size = new Av1FrameSize();
        request.Header.Size.FrameWidth = width;
        request.Header.Size.FrameHeight = height;
        request.Header.Cdef = new Av1CdefParameters();
        request.Header.Cdef.Damping = 3;
        request.Header.Cdef.YPrimaryStrengths = new int[1];
        request.Header.Cdef.YSecondaryStrengths = new int[1];
        request.Header.Cdef.UvPrimaryStrengths = new int[1];
        request.Header.Cdef.UvSecondaryStrengths = new int[1];
        request.MiRows = (height + 3) >> 2;
        request.MiColumns = (width + 3) >> 2;
        request.BitDepth = bitDepth;
        request.SubsamplingX = subsamplingX;
        request.SubsamplingY = subsamplingY;
        request.Skips = new bool[request.MiRows * request.MiColumns];
        request.StrengthIndexes = new int[
            ((request.MiRows + 15) >> 4) * ((request.MiColumns + 15) >> 4)];
        request.Planes = new PlaneBuffer[] { CreatePlane(width, height, bitDepth) };
        if (color)
        {
            int chromaWidth = (width + (subsamplingX ? 1 : 0))
                >> (subsamplingX ? 1 : 0);
            int chromaHeight = (height + (subsamplingY ? 1 : 0))
                >> (subsamplingY ? 1 : 0);
            request.Planes = new PlaneBuffer[]
            {
                request.Planes[0],
                CreatePlane(chromaWidth, chromaHeight, bitDepth),
                CreatePlane(chromaWidth, chromaHeight, bitDepth)
            };
        }
        return request;
    }

    static void ConfigureTwoStrengths(Av1CdefRequest request)
    {
        ((request.Header ?? throw new InvalidOperationException("Invalid internal state.")).Cdef ?? throw new InvalidOperationException("Invalid internal state.")).Bits = 1;
        (request.Header.Cdef ?? throw new InvalidOperationException("Invalid internal state.")).YPrimaryStrengths = new int[] { 0,0 };
        request.Header.Cdef.YSecondaryStrengths = new int[] { 0,4 };
        request.Header.Cdef.UvPrimaryStrengths = new int[2];
        request.Header.Cdef.UvSecondaryStrengths = new int[2];
    }

    static int ReferenceConstrain(int difference, int threshold, int damping)
    {
        if (threshold == 0)
        {
            return 0;
        }
        int log = 0;
        int value = threshold;
        while (value > 1)
        {
            value >>= 1;
            log++;
        }
        int adjustment = System.Math.Max(0, damping - log);
        int magnitude = System.Math.Abs(difference);
        int constrained = System.Math.Clamp(
            threshold - (magnitude >> adjustment), 0, magnitude);
        return difference < 0 ? -constrained : constrained;
    }

    static PlaneBuffer CreatePlane(int width, int height, int bitDepth)
    {
        return new PlaneBuffer(
            width, height, bitDepth > 8 ? 2 : 1, 1, 1048576, true);
    }

    static void Fill(PlaneBuffer plane, int value)
    {
        int y = 0;
        while (y < plane.Height)
        {
            int x = 0;
            while (x < plane.Width)
            {
                WriteSample(plane, x, y, value);
                x++;
            }
            y++;
        }
    }

    static void FillPattern(PlaneBuffer plane, int bitDepth)
    {
        int maximum = (1 << bitDepth) - 1;
        int scale = 1 << bitDepth - 8;
        int y = 0;
        while (y < plane.Height)
        {
            int x = 0;
            while (x < plane.Width)
            {
                int value = (40 + x * 7 + y * 11 + ((x * y) % 5) * 3) * scale;
                WriteSample(plane, x, y, System.Math.Min(maximum, value));
                x++;
            }
            y++;
        }
    }

    static int[] ReadPlane(PlaneBuffer plane)
    {
        int[] samples = new int[plane.Width * plane.Height];
        int y = 0;
        while (y < plane.Height)
        {
            int x = 0;
            while (x < plane.Width)
            {
                samples[y * plane.Width + x] = ReadSample(plane, x, y);
                x++;
            }
            y++;
        }
        return samples;
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

    static void Close(Av1CdefRequest request)
    {
        int plane = 0;
        while (plane < request.Planes.Length)
        {
            request.Planes[plane].Close();
            plane++;
        }
    }
}
