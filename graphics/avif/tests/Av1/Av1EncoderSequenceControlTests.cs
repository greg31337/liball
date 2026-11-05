namespace avif.Tests;

public class Av1EncoderSequenceControlTests
{
    [Test]
    public void BitrateBudgetAndVirtualBufferDriveQuantizerFeedback()
    {
        Av1EncoderSequenceControlRequest request = CreateRequest(3);
        request.Timescale = 1000;
        request.TargetBitrate = 8000;
        request.BufferSizeBits = 2000;
        request.InitialBufferLevelBits = 2000;
        request.BaseQuantizer = 128;
        request.Frames[0].EncodedSizeBytes = 0;
        request.Frames[1].EncodedSizeBytes = 400;
        request.Frames[2].EncodedSizeBytes = 100;

        Av1EncoderSequenceControlResult result =
            Av1EncoderSequenceController.Plan(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(GetTargetBits(result.Frames),
                Is.EqualTo(new long[] { 800, 800, 800 }));
            Assert.That(result.Frames[0].BufferOverflow, Is.True);
            Assert.That(result.Frames[0].BufferLevelAfter, Is.EqualTo(2000));
            Assert.That(result.Frames[1].Quantizer, Is.LessThan(128));
            Assert.That(result.Frames[1].BufferUnderflow, Is.True);
            Assert.That(result.Frames[1].BufferLevelAfter, Is.Zero);
            Assert.That(result.Frames[2].Quantizer, Is.GreaterThan(128));
            Assert.That(result.TotalTargetBits, Is.EqualTo(2400));
            Assert.That(result.TotalActualBits, Is.EqualTo(4000));
            Assert.That(result.FinalBufferLevelBits, Is.Zero);
        });
    }

    [Test]
    public void KeyframeBoundariesLimitLaggedReorderingAndTimingIsExact()
    {
        Av1EncoderSequenceControlRequest request = CreateRequest(6);
        request.MaximumKeyFrameInterval = 3;
        request.FrameLag = 2;
        request.Frames[0].Duration = 100;
        request.Frames[1].Duration = 200;
        request.Frames[2].Duration = 100;
        request.Frames[3].Duration = 100;
        request.Frames[4].Duration = 200;
        request.Frames[5].Duration = 100;

        Av1EncoderSequenceControlResult result =
            Av1EncoderSequenceController.Plan(request);

        Assert.Multiple(() =>
        {
            Assert.That(GetPresentationIndexes(result.Frames),
                Is.EqualTo(new int[] { 0, 2, 1, 3, 5, 4 }));
            Assert.That(GetKeyFrames(result.Frames),
                Is.EqualTo(new bool[] { true, false, false, true, false, false }));
            Assert.That(GetPresentationTimes(result.Frames),
                Is.EqualTo(new long[] { 0, 300, 100, 400, 700, 500 }));
            Assert.That(GetDecodeTimes(result.Frames),
                Is.EqualTo(new long[] { 0, 100, 200, 400, 500, 600 }));
            Assert.That(GetCompositionOffsets(result.Frames),
                Is.EqualTo(new long[] { 0, 200, -100, 0, 200, -100 }));
            Assert.That(result.Duration, Is.EqualTo(800));
        });
    }

    [Test]
    public void RepeatedContentUsesAStoredShowExistingReferenceWithoutRefresh()
    {
        Av1EncoderSequenceControlRequest request = CreateRequest(4);
        request.MaximumKeyFrameInterval = 10;
        request.ShowExistingFrameBits = 32;
        request.Frames[0].ContentSignature = 11;
        request.Frames[1].ContentSignature = 22;
        request.Frames[2].ContentSignature = 11;
        request.Frames[3].ContentSignature = 33;

        Av1EncoderSequenceControlResult result =
            Av1EncoderSequenceController.Plan(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Frames[0].RefreshFrameFlags, Is.EqualTo(255));
            Assert.That(result.Frames[1].RefreshFrameFlags, Is.EqualTo(1));
            Assert.That(result.Frames[2].ShowExistingFrame, Is.True);
            Assert.That(result.Frames[2].ShowExistingSlot, Is.EqualTo(1));
            Assert.That(result.Frames[2].RefreshFrameFlags, Is.Zero);
            Assert.That(result.Frames[2].ActualBits, Is.EqualTo(32));
            Assert.That(result.Frames[3].ShowExistingFrame, Is.False);
            Assert.That(result.Frames[3].RefreshFrameFlags, Is.EqualTo(2));
        });
    }

    [Test]
    public void IdenticalInputsProduceIdenticalSequencePlans()
    {
        Av1EncoderSequenceControlRequest request = CreateRequest(5);
        request.FrameLag = 1;
        request.Frames[0].ContentSignature = 5;
        request.Frames[1].ComplexityQ8 = 400;
        request.Frames[2].ContentSignature = 5;
        request.Frames[3].ForceKeyFrame = true;
        request.Frames[4].Duration = 137;

        Av1EncoderSequenceControlResult first =
            Av1EncoderSequenceController.Plan(request);
        Av1EncoderSequenceControlResult second =
            Av1EncoderSequenceController.Plan(request);

        Assert.Multiple(() =>
        {
            Assert.That(GetPresentationIndexes(first.Frames),
                Is.EqualTo(GetPresentationIndexes(second.Frames)));
            Assert.That(GetQuantizers(first.Frames),
                Is.EqualTo(GetQuantizers(second.Frames)));
            Assert.That(GetActualBits(first.Frames),
                Is.EqualTo(GetActualBits(second.Frames)));
            Assert.That(GetDecodeTimes(first.Frames),
                Is.EqualTo(GetDecodeTimes(second.Frames)));
            Assert.That(first.TotalActualBits,
                Is.EqualTo(second.TotalActualBits));
            Assert.That(first.FinalBufferLevelBits,
                Is.EqualTo(second.FinalBufferLevelBits));
        });
    }

    [Test]
    public void InvalidTimingAndLagConfigurationIsRejected()
    {
        Av1EncoderSequenceControlRequest timing = CreateRequest(1);
        timing.Frames[0].Duration = 0;
        Av1EncoderSequenceControlRequest lag = CreateRequest(1);
        lag.FrameLag = 17;

        Assert.Multiple(() =>
        {
            Assert.That(Av1EncoderSequenceController.Plan(timing).Code,
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(Av1EncoderSequenceController.Plan(lag).Code,
                Is.EqualTo(AvifResultCode.InvalidArgument));
        });
    }

    static Av1EncoderSequenceControlRequest CreateRequest(int frameCount)
    {
        Av1EncoderSequenceControlRequest request =
            new Av1EncoderSequenceControlRequest();
        request.Frames = new Av1EncoderSequenceFrame[frameCount];
        int index = 0;
        while (index < frameCount)
        {
            Av1EncoderSequenceFrame frame = new Av1EncoderSequenceFrame();
            frame.Duration = 100;
            frame.IntraCost = 100;
            frame.InterCost = 20;
            frame.ContentSignature = index + 1;
            request.Frames[index] = frame;
            index++;
        }
        return request;
    }

    static int[] GetPresentationIndexes(
        Av1EncoderSequenceFrameDecision[] frames)
    {
        int[] values = new int[frames.Length];
        int index = 0;
        while (index < frames.Length)
        {
            values[index] = frames[index].PresentationIndex;
            index++;
        }
        return values;
    }

    static bool[] GetKeyFrames(Av1EncoderSequenceFrameDecision[] frames)
    {
        bool[] values = new bool[frames.Length];
        int index = 0;
        while (index < frames.Length)
        {
            values[index] = frames[index].KeyFrame;
            index++;
        }
        return values;
    }

    static long[] GetPresentationTimes(
        Av1EncoderSequenceFrameDecision[] frames)
    {
        long[] values = new long[frames.Length];
        int index = 0;
        while (index < frames.Length)
        {
            values[index] = frames[index].PresentationTime;
            index++;
        }
        return values;
    }

    static long[] GetDecodeTimes(Av1EncoderSequenceFrameDecision[] frames)
    {
        long[] values = new long[frames.Length];
        int index = 0;
        while (index < frames.Length)
        {
            values[index] = frames[index].DecodeTime;
            index++;
        }
        return values;
    }

    static long[] GetCompositionOffsets(
        Av1EncoderSequenceFrameDecision[] frames)
    {
        long[] values = new long[frames.Length];
        int index = 0;
        while (index < frames.Length)
        {
            values[index] = frames[index].CompositionOffset;
            index++;
        }
        return values;
    }

    static long[] GetTargetBits(Av1EncoderSequenceFrameDecision[] frames)
    {
        long[] values = new long[frames.Length];
        int index = 0;
        while (index < frames.Length)
        {
            values[index] = frames[index].TargetBits;
            index++;
        }
        return values;
    }

    static long[] GetActualBits(Av1EncoderSequenceFrameDecision[] frames)
    {
        long[] values = new long[frames.Length];
        int index = 0;
        while (index < frames.Length)
        {
            values[index] = frames[index].ActualBits;
            index++;
        }
        return values;
    }

    static int[] GetQuantizers(Av1EncoderSequenceFrameDecision[] frames)
    {
        int[] values = new int[frames.Length];
        int index = 0;
        while (index < frames.Length)
        {
            values[index] = frames[index].Quantizer;
            index++;
        }
        return values;
    }
}
