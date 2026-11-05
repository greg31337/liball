namespace avif.Tests;

public class Av1EncoderFilterDecisionTests
{
    [Test]
    public void SearchSelectsDeblockLevelsByFinalReconstructionDistortion()
    {
        PlaneBuffer reconstructed = CreatePlane(8, 8);
        FillVerticalStep(reconstructed, 4, 100, 104);
        PlaneBuffer target = CreatePlane(8, 8);
        FillRows(target, new int[] { 100,100,101,101,102,103,104,104 });
        Av1EncoderFilterCandidate disabled = CreateCandidate(
            8, 8, reconstructed);
        disabled.Order = 0;
        Av1EncoderFilterCandidate filtered = CreateCandidate(
            8, 8, reconstructed);
        (filtered.LoopFilter.Levels ?? throw new InvalidOperationException("Invalid internal state."))[0] = 20;
        filtered.Order = 1;
        Av1EncoderFilterDecisionRequest request = CreateRequest(target);
        request.Candidates = new Av1EncoderFilterCandidate[]
        {
            disabled,
            filtered
        };

        Av1EncoderFilterDecisionResult result =
            Av1EncoderFilterDecisionEngine.Decide(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(result.BestCandidate, Is.SameAs(filtered));
            Assert.That(result.Distortion, Is.Zero);
            Assert.That(result.EvaluatedCandidateCount, Is.EqualTo(2));
            Assert.That(ReadPlane(result.ReconstructedPlanes[0]),
                Is.EqualTo(ReadPlane(target)));
            Assert.That(ReadPlane(reconstructed),
                Is.Not.EqualTo(ReadPlane(target)));
        });

        result.Close();
        reconstructed.Close();
        target.Close();
    }

    [Test]
    public void ReconstructionMatchesDirectDecoderFilterComposition()
    {
        PlaneBuffer reconstructed = CreatePlane(8, 8);
        FillVerticalStep(reconstructed, 4, 100, 104);
        WriteSample(reconstructed, 4, 4, 108);
        Av1EncoderFilterCandidate candidate = CreateCandidate(
            8, 8, reconstructed);
        (candidate.LoopFilter.Levels ?? throw new InvalidOperationException("Invalid internal state."))[0] = 20;
        (candidate.Cdef.YSecondaryStrengths ?? throw new InvalidOperationException("Invalid internal state."))[0] = 4;
        EnableWiener(candidate, 16);
        PlaneBuffer expected = ApplyDirectDecoderPipeline(candidate);
        Av1EncoderFilterCandidate disabled = CreateCandidate(
            8, 8, reconstructed);
        Av1EncoderFilterDecisionRequest request = CreateRequest(expected);
        request.Candidates = new Av1EncoderFilterCandidate[]
        {
            disabled,
            candidate
        };

        Av1EncoderFilterDecisionResult result =
            Av1EncoderFilterDecisionEngine.Decide(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(result.BestCandidate, Is.SameAs(candidate));
            Assert.That(result.Distortion, Is.Zero);
            Assert.That(ReadPlane(result.ReconstructedPlanes[0]),
                Is.EqualTo(ReadPlane(expected)));
            Assert.That(((result.BestCandidate ?? throw new InvalidOperationException("Invalid internal state.")).Cdef.YSecondaryStrengths ?? throw new InvalidOperationException("Invalid internal state."))[0],
                Is.EqualTo(4));
            Assert.That(result.BestCandidate.RestorationUnits[0][0].Type,
                Is.EqualTo(Av1RestorationType.Wiener));
        });

        result.Close();
        reconstructed.Close();
        expected.Close();
    }

    [Test]
    public void SearchCanChooseSuperResolutionUsingItsExactUpscaledPixels()
    {
        PlaneBuffer coded = CreatePlane(8, 8);
        FillPattern(coded, 17);
        Av1EncoderFilterCandidate superResolution = CreateCandidate(
            8, 8, coded);
        (superResolution.Size ?? throw new InvalidOperationException("Invalid internal state.")).UpscaledWidth = 16;
        superResolution.Size.RenderWidth = 16;
        superResolution.Size.UsesSuperres = true;
        superResolution.Size.SuperresDenominator = 16;
        PlaneBuffer target = CreatePlane(16, 8);
        Av1SuperResolutionRequest upscale = new Av1SuperResolutionRequest();
        upscale.Size = superResolution.Size;
        upscale.Source = new PlaneBuffer[] { coded };
        upscale.Destination = new PlaneBuffer[] { target };
        Assert.That(Av1SuperResolution.Upscale(upscale),
            Is.EqualTo(AvifResultCode.Success));

        PlaneBuffer fullWidth = CreatePlane(16, 8);
        Fill(fullWidth, 0);
        Av1EncoderFilterCandidate fullResolution = CreateCandidate(
            16, 8, fullWidth);
        fullResolution.Order = 0;
        superResolution.Order = 1;
        Av1EncoderFilterDecisionRequest request = CreateRequest(target);
        request.Candidates = new Av1EncoderFilterCandidate[]
        {
            fullResolution,
            superResolution
        };

        Av1EncoderFilterDecisionResult result =
            Av1EncoderFilterDecisionEngine.Decide(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(result.BestCandidate, Is.SameAs(superResolution));
            Assert.That(((result.BestCandidate ?? throw new InvalidOperationException("Invalid internal state.")).Size ?? throw new InvalidOperationException("Invalid internal state.")).SuperresDenominator,
                Is.EqualTo(16));
            Assert.That(result.Distortion, Is.Zero);
            Assert.That(ReadPlane(result.ReconstructedPlanes[0]),
                Is.EqualTo(ReadPlane(target)));
        });

        result.Close();
        coded.Close();
        fullWidth.Close();
        target.Close();
    }

    [Test]
    public void RateDistortionTieBreakUsesRateThenStableCandidateOrder()
    {
        PlaneBuffer reconstructed = CreatePlane(8, 8);
        Fill(reconstructed, 90);
        PlaneBuffer target = reconstructed.CreateCopy();
        Av1EncoderFilterCandidate highRate = CreateCandidate(
            8, 8, reconstructed);
        highRate.EstimatedRate = 4;
        highRate.Order = 0;
        Av1EncoderFilterCandidate lowRateLater = CreateCandidate(
            8, 8, reconstructed);
        lowRateLater.EstimatedRate = 2;
        lowRateLater.Order = 8;
        Av1EncoderFilterCandidate lowRateEarlier = CreateCandidate(
            8, 8, reconstructed);
        lowRateEarlier.EstimatedRate = 2;
        lowRateEarlier.Order = 3;
        Av1EncoderFilterDecisionRequest request = CreateRequest(target);
        request.RateDistortionLambdaQ8 = 0;
        request.Candidates = new Av1EncoderFilterCandidate[]
        {
            highRate,
            lowRateLater,
            lowRateEarlier
        };

        Av1EncoderFilterDecisionResult result =
            Av1EncoderFilterDecisionEngine.Decide(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(result.BestCandidate, Is.SameAs(lowRateEarlier));
            Assert.That(result.EstimatedRate, Is.EqualTo(2));
            Assert.That(result.RateDistortionScore, Is.Zero);
        });

        result.Close();
        reconstructed.Close();
        target.Close();
    }

    static Av1EncoderFilterDecisionRequest CreateRequest(PlaneBuffer target)
    {
        Av1EncoderFilterDecisionRequest request =
            new Av1EncoderFilterDecisionRequest();
        request.TargetPlanes = new PlaneBuffer[] { target };
        return request;
    }

    static Av1EncoderFilterCandidate CreateCandidate(
        int width,
        int height,
        PlaneBuffer reconstructed)
    {
        Av1EncoderFilterCandidate candidate =
            new Av1EncoderFilterCandidate();
        candidate.Size = new Av1FrameSize();
        candidate.Size.FrameWidth = width;
        candidate.Size.UpscaledWidth = width;
        candidate.Size.FrameHeight = height;
        candidate.Size.RenderWidth = width;
        candidate.Size.RenderHeight = height;
        candidate.Size.SuperresDenominator =
            Av1SyntaxConstants.SuperresNumerator;
        candidate.Size.MiColumns = 2 * ((width + 7) >> 3);
        candidate.Size.MiRows = 2 * ((height + 7) >> 3);
        candidate.ReconstructedPlanes = new PlaneBuffer[] { reconstructed };
        int miCount = candidate.Size.MiRows * candidate.Size.MiColumns;
        candidate.Modes = new Av1ModeInfo[miCount];
        int index = 0;
        while (index < miCount)
        {
            candidate.Modes[index] = new Av1ModeInfo();
            candidate.Modes[index].BlockSize = Av1BlockSize.Block4x4;
            candidate.Modes[index].TransformSize = Av1TransformSize.Transform4x4;
            index++;
        }
        candidate.TransformSizes = System.Array.Empty<Av1TransformSize[]>();
        int blockRows = (candidate.Size.MiRows + 15) >> 4;
        int blockColumns = (candidate.Size.MiColumns + 15) >> 4;
        candidate.CdefStrengthIndexes = new int[blockRows * blockColumns];
        candidate.LoopRestoration.Types = new Av1RestorationType[]
        {
            Av1RestorationType.None
        };
        candidate.LoopRestoration.UnitSizes = new int[] { 64 };
        candidate.RestorationUnits = new Av1RestorationUnit[][]
        {
            System.Array.Empty<Av1RestorationUnit>()
        };
        return candidate;
    }

    static void EnableWiener(
        Av1EncoderFilterCandidate candidate,
        int horizontalOuterCoefficient)
    {
        (candidate.LoopRestoration.Types ?? throw new InvalidOperationException("Invalid internal state."))[0] = Av1RestorationType.Wiener;
        candidate.LoopRestoration.UsesRestoration = true;
        int count = Av1LoopRestoration.CountUnits(
            64,
            (candidate.Size ?? throw new InvalidOperationException("Invalid internal state.")).UpscaledWidth)
            * Av1LoopRestoration.CountUnits(64, candidate.Size.FrameHeight);
        candidate.RestorationUnits[0] = new Av1RestorationUnit[count];
        int index = 0;
        while (index < count)
        {
            Av1RestorationUnit unit = new Av1RestorationUnit();
            unit.Type = Av1RestorationType.Wiener;
            unit.WienerHorizontal[2] = horizontalOuterCoefficient;
            candidate.RestorationUnits[0][index] = unit;
            index++;
        }
    }

    static PlaneBuffer ApplyDirectDecoderPipeline(
        Av1EncoderFilterCandidate candidate)
    {
        ParsedAv1FrameHeader header = new ParsedAv1FrameHeader();
        header.Size = candidate.Size;
        header.Quantization = candidate.Quantization;
        header.LoopFilter = candidate.LoopFilter;
        header.Cdef = candidate.Cdef;
        header.LoopRestoration = candidate.LoopRestoration;
        PlaneBuffer deblocked = candidate.ReconstructedPlanes[0].CreateCopy();
        Av1LoopFilterRequest loop = new Av1LoopFilterRequest();
        loop.Header = header;
        loop.Planes = new PlaneBuffer[] { deblocked };
        loop.Modes = candidate.Modes;
        loop.TransformSizes = candidate.TransformSizes;
        loop.MiRows = (candidate.Size ?? throw new InvalidOperationException("Invalid internal state.")).MiRows;
        loop.MiColumns = candidate.Size.MiColumns;
        Assert.That(Av1LoopFilter.Filter(loop), Is.EqualTo(AvifResultCode.Success));

        PlaneBuffer cdefPlane = deblocked.CreateCopy();
        Av1CdefRequest cdef = new Av1CdefRequest();
        cdef.Header = header;
        cdef.Planes = new PlaneBuffer[] { cdefPlane };
        cdef.Skips = new bool[candidate.Modes.Length];
        cdef.StrengthIndexes = candidate.CdefStrengthIndexes;
        cdef.RegionIds = candidate.CdefRegionIds;
        cdef.MiRows = candidate.Size.MiRows;
        cdef.MiColumns = candidate.Size.MiColumns;
        Assert.That(Av1CdefFilter.Filter(cdef), Is.EqualTo(AvifResultCode.Success));

        PlaneBuffer destination = CreatePlane(
            candidate.Size.UpscaledWidth,
            candidate.Size.FrameHeight);
        Av1LoopRestorationRequest restoration =
            new Av1LoopRestorationRequest();
        restoration.Header = header;
        restoration.CurrentPlanes = new PlaneBuffer[] { deblocked };
        restoration.CdefPlanes = new PlaneBuffer[] { cdefPlane };
        restoration.DestinationPlanes = new PlaneBuffer[] { destination };
        restoration.Units = candidate.RestorationUnits;
        Assert.That(Av1LoopRestoration.Filter(restoration),
            Is.EqualTo(AvifResultCode.Success));
        deblocked.Close();
        cdefPlane.Close();
        return destination;
    }

    static PlaneBuffer CreatePlane(int width, int height)
    {
        return new PlaneBuffer(width, height, 1, 1, 1 << 22, true);
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

    static void FillVerticalStep(
        PlaneBuffer plane,
        int edge,
        int left,
        int right)
    {
        int y = 0;
        while (y < plane.Height)
        {
            int x = 0;
            while (x < plane.Width)
            {
                WriteSample(plane, x, y, x < edge ? left : right);
                x++;
            }
            y++;
        }
    }

    static void FillRows(PlaneBuffer plane, int[] rowValues)
    {
        int y = 0;
        while (y < plane.Height)
        {
            int x = 0;
            while (x < plane.Width)
            {
                WriteSample(plane, x, y, rowValues[x]);
                x++;
            }
            y++;
        }
    }

    static void FillPattern(PlaneBuffer plane, int offset)
    {
        int y = 0;
        while (y < plane.Height)
        {
            int x = 0;
            while (x < plane.Width)
            {
                WriteSample(plane, x, y, 20 + offset + x * 17 + y * 3);
                x++;
            }
            y++;
        }
    }

    static int[] ReadPlane(PlaneBuffer plane)
    {
        int[] values = new int[plane.Width * plane.Height];
        int y = 0;
        while (y < plane.Height)
        {
            int x = 0;
            while (x < plane.Width)
            {
                values[y * plane.Width + x] = ReadSample(plane, x, y);
                x++;
            }
            y++;
        }
        return values;
    }

    static int ReadSample(PlaneBuffer plane, int x, int y)
    {
        return plane.GetRowSpan(y)[x];
    }

    static void WriteSample(PlaneBuffer plane, int x, int y, int value)
    {
        plane.GetRowSpan(y)[x] = (byte)value;
    }
}
