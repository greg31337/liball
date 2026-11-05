namespace avif.Tests;

public class Av1IntraCandidateEvaluatorTests
{
    [Test]
    public void DirectionalSearchFindsExactVerticalPredictionAndCommitsExplicitly()
    {
        PlaneBuffer plane = CreatePlane(12, 12, 8, 0);
        SetHorizontal(plane, 3, 4, [20, 40, 60, 80]);
        SetVertical(plane, 4, 3, [5, 15, 25, 35]);
        int[] target =
        [
            20,40,60,80,
            20,40,60,80,
            20,40,60,80,
            20,40,60,80
        ];
        Av1IntraCandidateEvaluationRequest request = CreateRequest(
            plane, target, 4, 4, 4, 4, 8);
        request.AllowDirectional = true;
        request.HaveAbove = true;
        request.HaveLeft = true;
        int[] before = ReadBlock(plane, 4, 4, 4, 4);

        Av1IntraCandidateEvaluationResult result =
            Av1IntraCandidateEvaluator.Evaluate(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(result.Candidates, Has.Length.EqualTo(45));
            Assert.That((result.BestCandidate ?? throw new InvalidOperationException("Invalid internal state.")).Mode,
                Is.EqualTo(Av1PredictionMode.Vertical));
            Assert.That(result.BestCandidate.Distortion, Is.Zero);
            Assert.That(ReadBlock(plane, 4, 4, 4, 4), Is.EqualTo(before));
            Assert.That(result.Commit(request), Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadBlock(plane, 4, 4, 4, 4), Is.EqualTo(target));
        });
        plane.Close();
    }

    [Test]
    public void SmoothPaethAndEveryFilterModeAreEvaluated()
    {
        PlaneBuffer plane = CreateEdgePlane();
        int[] target = PredictTarget(
            plane,
            Av1PredictionMode.Dc,
            true,
            Av1FilterIntraMode.FilterDiagonal,
            false,
            0,
            null);
        Av1IntraCandidateEvaluationRequest request = CreateRequest(
            plane, target, 2, 2, 4, 4, 8);
        request.AllowSmooth = true;
        request.AllowPaeth = true;
        request.AllowFilterIntra = true;
        request.HaveAbove = true;
        request.HaveLeft = true;

        Av1IntraCandidateEvaluationResult result =
            Av1IntraCandidateEvaluator.Evaluate(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(CountKind(result, Av1IntraCandidateKind.Filter), Is.EqualTo(5));
            Assert.That(HasMode(result, Av1PredictionMode.Smooth), Is.True);
            Assert.That(HasMode(result, Av1PredictionMode.SmoothVertical), Is.True);
            Assert.That(HasMode(result, Av1PredictionMode.SmoothHorizontal), Is.True);
            Assert.That(HasMode(result, Av1PredictionMode.Paeth), Is.True);
            Assert.That((result.BestCandidate ?? throw new InvalidOperationException("Invalid internal state.")).Kind,
                Is.EqualTo(Av1IntraCandidateKind.Filter));
            Assert.That(result.BestCandidate.FilterIntraMode,
                Is.EqualTo(Av1FilterIntraMode.FilterDiagonal));
            Assert.That(result.BestCandidate.Distortion, Is.Zero);
        });
        plane.Close();
    }

    [Test]
    public void CflSearchFindsSignedAlphaUsingSubsampledLuma()
    {
        PlaneBuffer plane = CreatePlane(4, 4, 8, 0);
        int[] luma = new int[64];
        int row = 0;
        while (row < 8)
        {
            int column = 0;
            while (column < 8)
            {
                luma[row * 8 + column] = row * 8 + column;
                column++;
            }
            row++;
        }
        int[] target = PredictTarget(
            plane,
            Av1PredictionMode.Dc,
            false,
            Av1FilterIntraMode.None,
            true,
            -11,
            luma);
        Av1IntraCandidateEvaluationRequest request = CreateRequest(
            plane, target, 0, 0, 4, 4, 8);
        request.AllowCfl = true;
        request.CflLuma = luma;
        request.CflLumaWidth = 8;
        request.CflLumaHeight = 8;
        request.CflLumaStride = 8;
        request.SubsamplingX = true;
        request.SubsamplingY = true;

        Av1IntraCandidateEvaluationResult result =
            Av1IntraCandidateEvaluator.Evaluate(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(CountKind(result, Av1IntraCandidateKind.Cfl), Is.EqualTo(33));
            Assert.That((result.BestCandidate ?? throw new InvalidOperationException("Invalid internal state.")).Kind,
                Is.EqualTo(Av1IntraCandidateKind.Cfl));
            Assert.That(result.BestCandidate.CflAlphaQ3, Is.EqualTo(-11));
            Assert.That(result.BestCandidate.Distortion, Is.Zero);
        });
        plane.Close();
    }

    [Test]
    public void PaletteSearchBuildsExactColorMapWithoutChangingSourcePlane()
    {
        PlaneBuffer plane = CreatePlane(8, 8, 10, 333);
        int[] target =
        [
            100,900,100,900,
            900,100,900,100,
            100,100,900,900,
            900,900,100,100
        ];
        Av1IntraCandidateEvaluationRequest request = CreateRequest(
            plane, target, 2, 2, 4, 4, 10);
        request.AllowPalette = true;

        Av1IntraCandidateEvaluationResult result =
            Av1IntraCandidateEvaluator.Evaluate(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((result.BestCandidate ?? throw new InvalidOperationException("Invalid internal state.")).Kind,
                Is.EqualTo(Av1IntraCandidateKind.Palette));
            Assert.That(result.BestCandidate.PaletteColors,
                Is.EqualTo(new int[] { 100, 900 }));
            Assert.That(result.BestCandidate.Distortion, Is.Zero);
            Assert.That(ReadBlock(plane, 2, 2, 4, 4), Is.All.EqualTo(333));
        });
        plane.Close();
    }

    [Test]
    public void IntraBlockCopySearchSelectsBestPreviouslyDecodedRegion()
    {
        PlaneBuffer plane = CreatePlane(16, 12, 8, 0);
        int[] sourceBlock =
        [
            2,4,6,8,
            10,12,14,16,
            18,20,22,24,
            26,28,30,32
        ];
        WriteBlock(plane, 4, 0, 4, 4, sourceBlock);
        Av1IntraCandidateEvaluationRequest request = CreateRequest(
            plane, sourceBlock, 8, 4, 4, 4, 8);
        request.AllowIntraBlockCopy = true;
        request.MaximumIntraBlockCopyCandidates = 4;

        Av1IntraCandidateEvaluationResult result =
            Av1IntraCandidateEvaluator.Evaluate(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((result.BestCandidate ?? throw new InvalidOperationException("Invalid internal state.")).Kind,
                Is.EqualTo(Av1IntraCandidateKind.IntraBlockCopy));
            Assert.That(result.BestCandidate.IntraBlockCopySourceX, Is.EqualTo(4));
            Assert.That(result.BestCandidate.IntraBlockCopySourceY, Is.Zero);
            Assert.That(result.BestCandidate.Distortion, Is.Zero);
            Assert.That(CountKind(result, Av1IntraCandidateKind.IntraBlockCopy),
                Is.LessThanOrEqualTo(4));
        });
        plane.Close();
    }

    [Test]
    public void RateTermAndStableOrderChooseDcForEquivalentPredictions()
    {
        PlaneBuffer plane = CreatePlane(8, 8, 8, 77);
        int[] target = Enumerable.Repeat(77, 16).ToArray();
        Av1IntraCandidateEvaluationRequest request = CreateRequest(
            plane, target, 2, 2, 4, 4, 8);
        request.AllowDirectional = true;
        request.AllowSmooth = true;
        request.AllowPaeth = true;
        request.HaveAbove = true;
        request.HaveLeft = true;
        request.RateDistortionLambdaQ8 = 512;

        Av1IntraCandidateEvaluationResult first =
            Av1IntraCandidateEvaluator.Evaluate(request);
        Av1IntraCandidateEvaluationResult second =
            Av1IntraCandidateEvaluator.Evaluate(request);

        Assert.Multiple(() =>
        {
            Assert.That((first.BestCandidate ?? throw new InvalidOperationException("Invalid internal state.")).Mode, Is.EqualTo(Av1PredictionMode.Dc));
            Assert.That(first.BestCandidate.EstimatedRate, Is.EqualTo(2));
            Assert.That(first.BestCandidate.Distortion, Is.Zero);
            Assert.That((second.BestCandidate ?? throw new InvalidOperationException("Invalid internal state.")).Mode, Is.EqualTo(first.BestCandidate.Mode));
            Assert.That(second.BestCandidate.Order, Is.EqualTo(first.BestCandidate.Order));
            Assert.That(second.BestCandidate.RateDistortionScore,
                Is.EqualTo(first.BestCandidate.RateDistortionScore));
        });
        plane.Close();
    }

    [Test]
    public void InvalidSourceGeometryAndMissingCflLumaAreRejected()
    {
        PlaneBuffer plane = CreatePlane(8, 8, 8, 0);
        Av1IntraCandidateEvaluationRequest shortSource = CreateRequest(
            plane, new int[15], 0, 0, 4, 4, 8);
        Av1IntraCandidateEvaluationRequest missingLuma = CreateRequest(
            plane, new int[16], 0, 0, 4, 4, 8);
        missingLuma.AllowCfl = true;

        Assert.Multiple(() =>
        {
            Assert.That(Av1IntraCandidateEvaluator.Evaluate(shortSource).Code,
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(Av1IntraCandidateEvaluator.Evaluate(missingLuma).Code,
                Is.EqualTo(AvifResultCode.InvalidArgument));
        });
        plane.Close();
    }

    static Av1IntraCandidateEvaluationRequest CreateRequest(
        PlaneBuffer plane,
        int[] source,
        int x,
        int y,
        int width,
        int height,
        int bitDepth)
    {
        Av1IntraCandidateEvaluationRequest request =
            new Av1IntraCandidateEvaluationRequest();
        request.Reconstructed = plane;
        request.Source = source;
        request.SourceStride = width;
        request.X = x;
        request.Y = y;
        request.Width = width;
        request.Height = height;
        request.BitDepth = bitDepth;
        request.AllowDirectional = false;
        request.AllowSmooth = false;
        request.AllowPaeth = false;
        return request;
    }

    static int[] PredictTarget(
        PlaneBuffer plane,
        Av1PredictionMode mode,
        bool filter,
        Av1FilterIntraMode filterMode,
        bool cfl,
        int alpha,
        int[]? luma)
    {
        PlaneBuffer copy = plane.CreateCopy();
        Av1IntraPredictionRequest request = new Av1IntraPredictionRequest();
        request.Destination = copy;
        request.X = plane.Width == 4 ? 0 : 2;
        request.Y = plane.Height == 4 ? 0 : 2;
        request.Width = 4;
        request.Height = 4;
        request.BitDepth = 8;
        request.Mode = mode;
        request.HaveAbove = request.Y > 0;
        request.HaveLeft = request.X > 0;
        request.UseFilterIntra = filter;
        request.FilterIntraMode = filterMode;
        request.UseCfl = cfl;
        request.CflLuma = luma;
        request.CflLumaWidth = 8;
        request.CflLumaHeight = 8;
        request.CflLumaStride = 8;
        request.SubsamplingX = true;
        request.SubsamplingY = true;
        request.CflAlphaQ3 = alpha;
        Assert.That(Av1IntraPredictor.Predict(request),
            Is.EqualTo(AvifResultCode.Success));
        int[] target = ReadBlock(copy, request.X, request.Y, 4, 4);
        copy.Close();
        return target;
    }

    static PlaneBuffer CreateEdgePlane()
    {
        PlaneBuffer plane = CreatePlane(8, 8, 8, 0);
        SetHorizontal(plane, 1, 2, [20, 30, 40, 50]);
        SetVertical(plane, 2, 1, [60, 70, 80, 90]);
        WriteSample(plane, 1, 1, 10);
        return plane;
    }

    static PlaneBuffer CreatePlane(
        int width,
        int height,
        int bitDepth,
        int value)
    {
        int bytesPerSample = bitDepth > 8 ? 2 : 1;
        PlaneBuffer plane = new PlaneBuffer(
            width,
            height,
            bytesPerSample,
            bytesPerSample,
            (long)width * height * bytesPerSample,
            true);
        int[] values = Enumerable.Repeat(value, width * height).ToArray();
        WriteBlock(plane, 0, 0, width, height, values);
        return plane;
    }

    static int CountKind(
        Av1IntraCandidateEvaluationResult result,
        Av1IntraCandidateKind kind)
    {
        return result.Candidates.Count(candidate => candidate.Kind == kind);
    }

    static bool HasMode(
        Av1IntraCandidateEvaluationResult result,
        Av1PredictionMode mode)
    {
        return result.Candidates.Any(candidate => candidate.Mode == mode);
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
                values[row * width + column] = ReadSample(
                    plane, y + row, x + column);
                column++;
            }
            row++;
        }
        return values;
    }

    static void WriteBlock(
        PlaneBuffer plane,
        int x,
        int y,
        int width,
        int height,
        int[] values)
    {
        int row = 0;
        while (row < height)
        {
            int column = 0;
            while (column < width)
            {
                WriteSample(
                    plane,
                    y + row,
                    x + column,
                    values[row * width + column]);
                column++;
            }
            row++;
        }
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
}
