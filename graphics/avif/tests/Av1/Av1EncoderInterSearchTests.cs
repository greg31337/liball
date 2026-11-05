namespace avif.Tests;

public class Av1EncoderInterSearchTests
{
    [Test]
    public void SchedulerPlacesSceneAndIntervalKeysAndRotatesReferenceSlots()
    {
        Av1EncoderFrameScheduleRequest request =
            new Av1EncoderFrameScheduleRequest();
        request.MaximumKeyFrameInterval = 4;
        request.SceneChangeThresholdQ8 = 384;
        request.Frames = new Av1EncoderFrameAnalysis[]
        {
            Frame(100, 0),
            Frame(100, 40),
            Frame(100, 50),
            Frame(100, 200),
            Frame(100, 40),
            Frame(100, 40),
            Frame(100, 40),
            Frame(100, 40)
        };

        Av1EncoderFrameScheduleResult result =
            Av1EncoderReferenceScheduler.Schedule(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(GetFrameTypes(result.Frames), Is.EqualTo(new Av1FrameType[]
            {
                Av1FrameType.Key,
                Av1FrameType.Inter,
                Av1FrameType.Inter,
                Av1FrameType.Key,
                Av1FrameType.Inter,
                Av1FrameType.Inter,
                Av1FrameType.Inter,
                Av1FrameType.Key
            }));
            Assert.That(result.Frames[0].RefreshFrameFlags, Is.EqualTo(255));
            Assert.That(result.Frames[1].RefreshFrameFlags, Is.EqualTo(1));
            Assert.That(result.Frames[2].RefreshFrameFlags, Is.EqualTo(2));
            Assert.That(result.Frames[3].RefreshFrameFlags, Is.EqualTo(255));
            Assert.That(result.Frames[2].ReferenceFrameIndexes[0], Is.EqualTo(0));
            Assert.That(result.Frames[2].ReferenceSlotFrames[1], Is.EqualTo(2));
            Assert.That(result.Frames[4].PrimaryReferenceFrame, Is.Zero);
            Assert.That(result.Frames[7].PrimaryReferenceFrame, Is.EqualTo(7));
        });
    }

    [Test]
    public void IntegerAndSubpixelSearchRecoverExactMotion()
    {
        PlaneBuffer reference = CreatePlane(20, 16);
        FillPattern(reference, 0);
        PlaneBuffer integerSource = CreatePlane(20, 16);
        CopyShifted(reference, integerSource, 4, 4, 8, 8, 1, 0);
        Av1EncoderInterSearchRequest integerRequest = CreateSearch(
            integerSource,
            reference,
            4,
            4,
            8,
            8);
        integerRequest.EnableSubpixelSearch = false;
        Av1EncoderInterSearchResult integerResult =
            Av1EncoderInterSearcher.Search(integerRequest);

        PlaneBuffer subpixelSource = CreatePlane(20, 16);
        RenderInter(
            subpixelSource,
            reference,
            4,
            4,
            8,
            8,
            new Av1MotionVector(0, 4),
            Av1MotionMode.SimpleTranslation,
            Av1WarpModelType.Identity,
            System.Array.Empty<int>());
        Av1EncoderInterSearchRequest subpixelRequest = CreateSearch(
            subpixelSource,
            reference,
            4,
            4,
            8,
            8);
        Av1EncoderInterSearchResult subpixelResult =
            Av1EncoderInterSearcher.Search(subpixelRequest);

        Assert.Multiple(() =>
        {
            Assert.That(integerResult.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((integerResult.BestCandidate ?? throw new InvalidOperationException("Invalid internal state.")).Mode,
                Is.EqualTo(Av1EncoderInterMode.Translation));
            Assert.That(integerResult.BestCandidate.FirstMotionVector.Row, Is.Zero);
            Assert.That(integerResult.BestCandidate.FirstMotionVector.Column,
                Is.EqualTo(8));
            Assert.That(integerResult.BestCandidate.Distortion, Is.Zero);
            Assert.That(subpixelResult.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((subpixelResult.BestCandidate ?? throw new InvalidOperationException("Invalid internal state.")).FirstMotionVector.Row, Is.Zero);
            Assert.That(subpixelResult.BestCandidate.FirstMotionVector.Column,
                Is.EqualTo(4));
            Assert.That(subpixelResult.BestCandidate.Distortion, Is.Zero);
        });

        reference.Close();
        integerSource.Close();
        subpixelSource.Close();
    }

    [Test]
    public void CompoundAndInterIntraCandidatesBeatEitherSingleReference()
    {
        PlaneBuffer source = CreatePlane(8, 8);
        Fill(source, 100);
        PlaneBuffer first = CreatePlane(8, 8);
        PlaneBuffer second = CreatePlane(8, 8);
        Fill(first, 50);
        Fill(second, 150);
        Av1EncoderInterSearchRequest compoundRequest =
            new Av1EncoderInterSearchRequest();
        compoundRequest.Source = source;
        compoundRequest.Width = 8;
        compoundRequest.Height = 8;
        compoundRequest.MaximumIntegerSearchRadius = 0;
        compoundRequest.EnableSubpixelSearch = false;
        compoundRequest.EnableCompound = true;
        compoundRequest.References = new Av1EncoderInterReference[]
        {
            Reference(Av1ReferenceFrame.Last, first, 0),
            Reference(Av1ReferenceFrame.Golden, second, 0)
        };
        Av1EncoderInterSearchResult compound =
            Av1EncoderInterSearcher.Search(compoundRequest);

        PlaneBuffer interIntraSource = CreatePlane(8, 8);
        int[] intra = CreateValues(8, 8, 150);
        RenderInterIntra(interIntraSource, first, intra, Av1InterIntraMode.Dc);
        Av1EncoderInterSearchRequest interIntraRequest =
            new Av1EncoderInterSearchRequest();
        interIntraRequest.Source = interIntraSource;
        interIntraRequest.Width = 8;
        interIntraRequest.Height = 8;
        interIntraRequest.MaximumIntegerSearchRadius = 0;
        interIntraRequest.EnableSubpixelSearch = false;
        interIntraRequest.EnableInterIntra = true;
        interIntraRequest.IntraPrediction = intra;
        interIntraRequest.References = new Av1EncoderInterReference[]
        {
            Reference(Av1ReferenceFrame.Last, first, 0)
        };
        Av1EncoderInterSearchResult interIntra =
            Av1EncoderInterSearcher.Search(interIntraRequest);

        Assert.Multiple(() =>
        {
            Assert.That(compound.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((compound.BestCandidate ?? throw new InvalidOperationException("Invalid internal state.")).Mode,
                Is.EqualTo(Av1EncoderInterMode.Compound));
            Assert.That(compound.BestCandidate.CompoundType,
                Is.EqualTo(Av1CompoundPredictionType.Average));
            Assert.That(compound.BestCandidate.Distortion, Is.Zero);
            Assert.That(interIntra.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((interIntra.BestCandidate ?? throw new InvalidOperationException("Invalid internal state.")).Mode,
                Is.EqualTo(Av1EncoderInterMode.InterIntra));
            Assert.That(interIntra.BestCandidate.InterIntraMode,
                Is.EqualTo(Av1InterIntraMode.Dc));
            Assert.That(interIntra.BestCandidate.Distortion, Is.Zero);
        });

        source.Close();
        first.Close();
        second.Close();
        interIntraSource.Close();
    }

    [Test]
    public void GlobalAndLocalWarpSearchUseNormativeWarpedPrediction()
    {
        PlaneBuffer reference = CreatePlane(20, 20);
        FillPattern(reference, 7);
        int[] warp = new int[] { 0, 0, 65536, 4096, 0, 65536 };
        PlaneBuffer source = CreatePlane(20, 20);
        RenderInter(
            source,
            reference,
            4,
            4,
            8,
            8,
            new Av1MotionVector(0, 0),
            Av1MotionMode.SimpleTranslation,
            Av1WarpModelType.Affine,
            warp);
        Av1EncoderInterSearchRequest globalRequest = CreateSearch(
            source,
            reference,
            4,
            4,
            8,
            8);
        globalRequest.MaximumIntegerSearchRadius = 0;
        globalRequest.EnableSubpixelSearch = false;
        globalRequest.EnableGlobalMotion = true;
        globalRequest.References[0].GlobalMotionType = Av1WarpModelType.Affine;
        globalRequest.References[0].GlobalMotionParameters = warp;
        Av1EncoderInterSearchResult global =
            Av1EncoderInterSearcher.Search(globalRequest);

        Av1EncoderInterSearchRequest warpedRequest = CreateSearch(
            source,
            reference,
            4,
            4,
            8,
            8);
        warpedRequest.MaximumIntegerSearchRadius = 0;
        warpedRequest.EnableSubpixelSearch = false;
        warpedRequest.EnableWarpedMotion = true;
        warpedRequest.References[0].LocalWarpParameters = warp;
        Av1EncoderInterSearchResult warped =
            Av1EncoderInterSearcher.Search(warpedRequest);

        Assert.Multiple(() =>
        {
            Assert.That(global.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((global.BestCandidate ?? throw new InvalidOperationException("Invalid internal state.")).Mode,
                Is.EqualTo(Av1EncoderInterMode.Global));
            Assert.That(global.BestCandidate.Distortion, Is.Zero);
            Assert.That(warped.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((warped.BestCandidate ?? throw new InvalidOperationException("Invalid internal state.")).Mode,
                Is.EqualTo(Av1EncoderInterMode.Warped));
            Assert.That(warped.BestCandidate.Distortion, Is.Zero);
        });

        reference.Close();
        source.Close();
    }

    [Test]
    public void SearchRejectsMalformedReferenceConfiguration()
    {
        PlaneBuffer source = CreatePlane(8, 8);
        Av1EncoderInterSearchRequest request = CreateSearch(
            source,
            source,
            0,
            0,
            8,
            8);
        request.References[0].LocalWarpParameters = null;

        Av1EncoderInterSearchResult result =
            Av1EncoderInterSearcher.Search(request);

        Assert.That(result.Code, Is.EqualTo(AvifResultCode.InvalidArgument));
        source.Close();
    }

    static Av1EncoderFrameAnalysis Frame(long intraCost, long interCost)
    {
        Av1EncoderFrameAnalysis frame = new Av1EncoderFrameAnalysis();
        frame.IntraCost = intraCost;
        frame.InterCost = interCost;
        return frame;
    }

    static Av1FrameType[] GetFrameTypes(
        Av1EncoderFrameScheduleDecision[] frames)
    {
        Av1FrameType[] types = new Av1FrameType[frames.Length];
        int index = 0;
        while (index < frames.Length)
        {
            types[index] = frames[index].FrameType;
            index++;
        }
        return types;
    }

    static Av1EncoderInterSearchRequest CreateSearch(
        PlaneBuffer source,
        PlaneBuffer reference,
        int x,
        int y,
        int width,
        int height)
    {
        Av1EncoderInterSearchRequest request =
            new Av1EncoderInterSearchRequest();
        request.Source = source;
        request.X = x;
        request.Y = y;
        request.Width = width;
        request.Height = height;
        request.MaximumIntegerSearchRadius = 2;
        request.References = new Av1EncoderInterReference[]
        {
            Reference(Av1ReferenceFrame.Last, reference, 0)
        };
        return request;
    }

    static Av1EncoderInterReference Reference(
        Av1ReferenceFrame frame,
        PlaneBuffer plane,
        int orderHint)
    {
        Av1EncoderInterReference reference =
            new Av1EncoderInterReference();
        reference.ReferenceFrame = frame;
        reference.Plane = plane;
        reference.OrderHint = orderHint;
        return reference;
    }

    static void RenderInter(
        PlaneBuffer destination,
        PlaneBuffer reference,
        int x,
        int y,
        int width,
        int height,
        Av1MotionVector vector,
        Av1MotionMode motionMode,
        Av1WarpModelType globalType,
        int[] warp)
    {
        Av1InterPredictionRequest request = new Av1InterPredictionRequest();
        request.Destination = destination;
        request.Reference = reference;
        request.X = x;
        request.Y = y;
        request.Width = width;
        request.Height = height;
        request.BitDepth = 8;
        request.MotionVector = vector;
        request.MotionMode = motionMode;
        request.GlobalMotionType = globalType;
        request.WarpParameters = warp;
        Assert.That(Av1InterPredictor.Predict(request),
            Is.EqualTo(AvifResultCode.Success));
    }

    static void RenderInterIntra(
        PlaneBuffer destination,
        PlaneBuffer inter,
        int[] intra,
        Av1InterIntraMode mode)
    {
        PlaneBuffer intraPlane = CreatePlane(8, 8);
        WriteValues(intraPlane, intra);
        Av1InterIntraPredictionRequest request =
            new Av1InterIntraPredictionRequest();
        request.Destination = destination;
        request.Inter = inter;
        request.Intra = intraPlane;
        request.Width = 8;
        request.Height = 8;
        request.BitDepth = 8;
        request.Mode = mode;
        Assert.That(Av1CompoundPredictor.PredictInterIntra(request),
            Is.EqualTo(AvifResultCode.Success));
        intraPlane.Close();
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
                plane.GetRowSpan(y)[x] = (byte)value;
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
                plane.GetRowSpan(y)[x] = (byte)(
                    20 + (x * x * 3 + y * y * 5 + x * y * 7
                        + x * 11 + y * 17 + offset) % 210);
                x++;
            }
            y++;
        }
    }

    static void CopyShifted(
        PlaneBuffer reference,
        PlaneBuffer destination,
        int x,
        int y,
        int width,
        int height,
        int shiftX,
        int shiftY)
    {
        int row = 0;
        while (row < height)
        {
            int column = 0;
            while (column < width)
            {
                destination.GetRowSpan(y + row)[x + column] =
                    reference.GetRowSpan(y + row + shiftY)
                        [x + column + shiftX];
                column++;
            }
            row++;
        }
    }

    static int[] CreateValues(int width, int height, int value)
    {
        int[] values = new int[width * height];
        int index = 0;
        while (index < values.Length)
        {
            values[index] = value;
            index++;
        }
        return values;
    }

    static void WriteValues(PlaneBuffer plane, int[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            int y = index / plane.Width;
            int x = index - y * plane.Width;
            plane.GetRowSpan(y)[x] = (byte)values[index];
            index++;
        }
    }
}
