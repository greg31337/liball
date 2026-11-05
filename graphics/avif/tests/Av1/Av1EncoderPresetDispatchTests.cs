namespace avif.Tests;

public class Av1EncoderPresetDispatchTests
{
    [TestCase(AvifEncoderPreset.Fastest, 0, 1, true, false, 1)]
    [TestCase(AvifEncoderPreset.Fast, 0, 2, true, false, 2)]
    [TestCase(AvifEncoderPreset.Balanced, 1, 4, true, false, 4)]
    [TestCase(AvifEncoderPreset.Quality, 2, 8, true, true, 8)]
    [TestCase(AvifEncoderPreset.Best, 3, 16, false, true, 16)]
    public void PresetsConfigureExistingEncoderSearchAndDecisionRequests(
        AvifEncoderPreset preset,
        int expectedPartitionEffort,
        int expectedMotionRadius,
        bool expectedPruning,
        bool expectedWarpedMotion,
        int expectedFilterCandidates)
    {
        Av1EncoderOptions options = new Av1EncoderOptions();
        options.Preset = preset;
        Av1EncoderPresetDecision decision =
            Av1EncoderPresetResolver.Resolve(options);
        Av1PartitionSearchRequest partition = new Av1PartitionSearchRequest();
        Av1EncoderInterSearchRequest inter =
            new Av1EncoderInterSearchRequest();
        Av1EncoderBlockDecisionRequest blocks =
            new Av1EncoderBlockDecisionRequest();
        decision.ApplyTo(partition);
        decision.ApplyToInterSearch(inter);
        decision.ApplyToBlockDecision(blocks);

        Assert.Multiple(() =>
        {
            Assert.That(decision.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((int)partition.Effort,
                Is.EqualTo(expectedPartitionEffort));
            Assert.That(partition.EnablePruning, Is.EqualTo(expectedPruning));
            Assert.That(inter.MaximumIntegerSearchRadius,
                Is.EqualTo(expectedMotionRadius));
            Assert.That(inter.EnableWarpedMotion,
                Is.EqualTo(expectedWarpedMotion));
            Assert.That(decision.FilterCandidateLimit,
                Is.EqualTo(expectedFilterCandidates));
            Assert.That(blocks.EnableQuantizerMatrix,
                Is.EqualTo(preset >= AvifEncoderPreset.Quality));
            Assert.That(blocks.EnableDeltaLoopFilter,
                Is.EqualTo(preset >= AvifEncoderPreset.Balanced));
        });
    }

    [Test]
    public void ScalarAndIntrinsicKernelsProduceIdenticalExactResults()
    {
        int count = System.Numerics.Vector<int>.Count * 2 + 3;
        int[] first = new int[count];
        int[] second = new int[count];
        int index = 0;
        while (index < count)
        {
            first[index] = (index * 197 + 17) % 4096;
            second[index] = (index * 83 + 901) % 4096;
            index++;
        }

        Av1EncoderKernelResult scalar =
            Av1EncoderKernelDispatcher.SumSquaredError(
                first,
                second,
                Av1EncoderKernelBackend.Scalar);
        Av1EncoderKernelResult intrinsic =
            Av1EncoderKernelDispatcher.SumSquaredError(
                first,
                second,
                Av1EncoderKernelBackend.Intrinsic);
        Av1EncoderKernelResult automatic =
            Av1EncoderKernelDispatcher.SumSquaredError(
                first,
                second,
                Av1EncoderKernelBackend.Automatic);

        Assert.Multiple(() =>
        {
            Assert.That(scalar.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(scalar.Backend,
                Is.EqualTo(Av1EncoderKernelBackend.Scalar));
            Assert.That(intrinsic.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(intrinsic.Value, Is.EqualTo(scalar.Value));
            Assert.That(automatic.Value, Is.EqualTo(scalar.Value));
            Assert.That(intrinsic.Backend,
                Is.EqualTo(System.Numerics.Vector.IsHardwareAccelerated
                    ? Av1EncoderKernelBackend.Intrinsic
                    : Av1EncoderKernelBackend.Scalar));
            Assert.That(automatic.Backend, Is.EqualTo(intrinsic.Backend));
        });
    }

    [Test]
    public void InterSearchIsIdenticalAcrossScalarAndIntrinsicDispatch()
    {
        PlaneBuffer reference = CreatePlane(16, 16);
        FillPattern(reference);
        PlaneBuffer source = CreatePlane(16, 16);
        CopyShifted(reference, source, 3, 3, 8, 8, 1);
        Av1EncoderInterSearchRequest scalarRequest = CreateSearch(
            source,
            reference,
            Av1EncoderKernelBackend.Scalar);
        Av1EncoderInterSearchRequest intrinsicRequest = CreateSearch(
            source,
            reference,
            Av1EncoderKernelBackend.Intrinsic);

        Av1EncoderInterSearchResult scalar =
            Av1EncoderInterSearcher.Search(scalarRequest);
        Av1EncoderInterSearchResult intrinsic =
            Av1EncoderInterSearcher.Search(intrinsicRequest);

        Assert.Multiple(() =>
        {
            Assert.That(scalar.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(intrinsic.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((intrinsic.BestCandidate ?? throw new InvalidOperationException("Invalid internal state.")).Distortion,
                Is.EqualTo((scalar.BestCandidate ?? throw new InvalidOperationException("Invalid internal state.")).Distortion));
            Assert.That(intrinsic.BestCandidate.FirstMotionVector.Row,
                Is.EqualTo(scalar.BestCandidate.FirstMotionVector.Row));
            Assert.That(intrinsic.BestCandidate.FirstMotionVector.Column,
                Is.EqualTo(scalar.BestCandidate.FirstMotionVector.Column));
            Assert.That(intrinsic.BestCandidate.Prediction,
                Is.EqualTo(scalar.BestCandidate.Prediction));
            Assert.That(intrinsic.BestCandidate.FirstMotionVector.Column,
                Is.EqualTo(8));
            Assert.That(intrinsic.BestCandidate.Distortion, Is.Zero);
        });
        source.Close();
        reference.Close();
    }

    [Test]
    public void InvalidPresetBackendAndSamplesAreRejected()
    {
        Av1EncoderOptions options = new Av1EncoderOptions();
        options.Preset = (AvifEncoderPreset)99;
        Av1EncoderKernelResult backend =
            Av1EncoderKernelDispatcher.SumSquaredError(
                new int[] { 1 },
                new int[] { 1 },
                (Av1EncoderKernelBackend)99);
        Av1EncoderKernelResult sample =
            Av1EncoderKernelDispatcher.SumSquaredError(
                new int[] { 4096 },
                new int[] { 0 },
                Av1EncoderKernelBackend.Scalar);

        Assert.Multiple(() =>
        {
            Assert.That(Av1EncoderPresetResolver.Resolve(options).Code,
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(backend.Code,
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(sample.Code,
                Is.EqualTo(AvifResultCode.InvalidArgument));
        });
    }

    static Av1EncoderInterSearchRequest CreateSearch(
        PlaneBuffer source,
        PlaneBuffer reference,
        Av1EncoderKernelBackend backend)
    {
        Av1EncoderInterReference encoderReference =
            new Av1EncoderInterReference();
        encoderReference.ReferenceFrame = Av1ReferenceFrame.Last;
        encoderReference.Plane = reference;
        Av1EncoderInterSearchRequest request =
            new Av1EncoderInterSearchRequest();
        request.Source = source;
        request.References = new Av1EncoderInterReference[]
        {
            encoderReference
        };
        request.X = 3;
        request.Y = 3;
        request.Width = 8;
        request.Height = 8;
        request.MaximumIntegerSearchRadius = 1;
        request.EnableSubpixelSearch = true;
        request.KernelBackend = backend;
        return request;
    }

    static PlaneBuffer CreatePlane(int width, int height)
    {
        return new PlaneBuffer(width, height, 1, 1, 1 << 20, true);
    }

    static void FillPattern(PlaneBuffer plane)
    {
        int y = 0;
        while (y < plane.Height)
        {
            int x = 0;
            while (x < plane.Width)
            {
                plane.GetRowSpan(y)[x] = (byte)(
                    10 + (x * x * 7 + y * y * 11 + x * y * 3) % 230);
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
        int shiftX)
    {
        int row = 0;
        while (row < height)
        {
            int column = 0;
            while (column < width)
            {
                destination.GetRowSpan(y + row)[x + column] =
                    reference.GetRowSpan(y + row)[x + column + shiftX];
                column++;
            }
            row++;
        }
    }
}
