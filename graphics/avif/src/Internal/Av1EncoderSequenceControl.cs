namespace avif;

internal class Av1EncoderSequenceFrame
{
    internal long Duration;
    internal long IntraCost;
    internal long InterCost;
    internal bool ForceKeyFrame;
    internal int ComplexityQ8;
    internal long EncodedSizeBytes;
    internal long ContentSignature;

    internal Av1EncoderSequenceFrame()
    {
        Duration = 1;
        ComplexityQ8 = 256;
        EncodedSizeBytes = -1;
    }
}

internal class Av1EncoderSequenceControlRequest
{
    internal Av1EncoderSequenceFrame[] Frames;
    internal long Timescale;
    internal long TargetBitrate;
    internal long BufferSizeBits;
    internal long InitialBufferLevelBits;
    internal int MaximumKeyFrameInterval;
    internal int SceneChangeThresholdQ8;
    internal int FrameLag;
    internal int BaseQuantizer;
    internal int MinimumQuantizer;
    internal int MaximumQuantizer;
    internal long ShowExistingFrameBits;

    internal Av1EncoderSequenceControlRequest()
    {
        Frames = System.Array.Empty<Av1EncoderSequenceFrame>();
        Timescale = 1000;
        TargetBitrate = 1000000;
        BufferSizeBits = 1000000;
        InitialBufferLevelBits = 500000;
        MaximumKeyFrameInterval = 240;
        SceneChangeThresholdQ8 = 384;
        FrameLag = 0;
        BaseQuantizer = 128;
        MinimumQuantizer = 0;
        MaximumQuantizer = Av1SyntaxConstants.MaxQuantizerIndex;
        ShowExistingFrameBits = 32;
    }
}

internal class Av1EncoderSequenceFrameDecision
{
    internal int PresentationIndex;
    internal int EncodeIndex;
    internal Av1FrameType FrameType;
    internal bool KeyFrame;
    internal bool ShowExistingFrame;
    internal int ShowExistingSlot;
    internal int RefreshFrameFlags;
    internal int Quantizer;
    internal long PresentationTime;
    internal long DecodeTime;
    internal long Duration;
    internal long CompositionOffset;
    internal long TargetBits;
    internal long ActualBits;
    internal long BufferLevelBefore;
    internal long BufferLevelAfter;
    internal bool BufferUnderflow;
    internal bool BufferOverflow;

    internal Av1EncoderSequenceFrameDecision()
    {
        ShowExistingSlot = -1;
    }
}

internal class Av1EncoderSequenceControlResult
{
    internal AvifResultCode Code;
    internal Av1EncoderSequenceFrameDecision[] Frames;
    internal long Duration;
    internal long TotalTargetBits;
    internal long TotalActualBits;
    internal long FinalBufferLevelBits;

    internal Av1EncoderSequenceControlResult(AvifResultCode code)
    {
        Code = code;
        Frames = System.Array.Empty<Av1EncoderSequenceFrameDecision>();
    }
}

internal class Av1EncoderSequenceValidation
{
    internal AvifResultCode Code;
    internal long[] PresentationTimes;
    internal long[] TargetBits;

    internal Av1EncoderSequenceValidation(AvifResultCode code)
    {
        Code = code;
        PresentationTimes = System.Array.Empty<long>();
        TargetBits = System.Array.Empty<long>();
    }
}

internal static class Av1EncoderSequenceController
{
    internal static Av1EncoderSequenceControlResult Plan(
        Av1EncoderSequenceControlRequest request)
    {
        Av1EncoderSequenceValidation validation = Validate(request);
        Av1EncoderSequenceControlResult result =
            new Av1EncoderSequenceControlResult(validation.Code);
        if (validation.Code != AvifResultCode.Success)
        {
            return result;
        }
        if (request.Frames.Length == 0)
        {
            result.FinalBufferLevelBits = request.InitialBufferLevelBits;
            return result;
        }

        Av1EncoderFrameScheduleResult schedule = CreateSchedule(request);
        if (schedule.Code != AvifResultCode.Success)
        {
            result.Code = schedule.Code;
            return result;
        }
        int[] encodeOrder = BuildEncodeOrder(schedule.Frames, request.FrameLag);
        result.Frames =
            new Av1EncoderSequenceFrameDecision[request.Frames.Length];
        bool[] slotValid = new bool[Av1SyntaxConstants.StoredReferenceFrames];
        long[] slotSignatures =
            new long[Av1SyntaxConstants.StoredReferenceFrames];
        int nextRefreshSlot = 0;
        long bufferLevel = request.InitialBufferLevelBits;
        long decodeTime = 0;
        int encodeIndex = 0;
        while (encodeIndex < encodeOrder.Length)
        {
            int presentationIndex = encodeOrder[encodeIndex];
            Av1EncoderSequenceFrame frame = request.Frames[presentationIndex];
            Av1EncoderFrameScheduleDecision scheduled =
                schedule.Frames[presentationIndex];
            Av1EncoderSequenceFrameDecision decision =
                new Av1EncoderSequenceFrameDecision();
            decision.PresentationIndex = presentationIndex;
            decision.EncodeIndex = encodeIndex;
            decision.KeyFrame = scheduled.KeyFrame;
            decision.FrameType = scheduled.FrameType;
            decision.PresentationTime =
                validation.PresentationTimes[presentationIndex];
            decision.DecodeTime = decodeTime;
            decision.Duration = frame.Duration;
            decision.CompositionOffset = decision.PresentationTime - decodeTime;
            decision.TargetBits = validation.TargetBits[presentationIndex];
            decision.BufferLevelBefore = bufferLevel;

            if (!decision.KeyFrame && frame.ContentSignature != 0)
            {
                decision.ShowExistingSlot = FindReferenceSlot(
                    frame.ContentSignature,
                    slotValid,
                    slotSignatures);
                decision.ShowExistingFrame = decision.ShowExistingSlot >= 0;
            }
            decision.Quantizer = ChooseQuantizer(
                request,
                frame,
                decision.KeyFrame,
                bufferLevel);
            decision.ActualBits = decision.ShowExistingFrame
                ? request.ShowExistingFrameBits
                : GetActualBits(frame, decision.TargetBits,
                    decision.Quantizer, decision.KeyFrame);

            if (decision.KeyFrame)
            {
                decision.RefreshFrameFlags = 255;
                int slot = 0;
                while (slot < slotValid.Length)
                {
                    slotValid[slot] = frame.ContentSignature != 0;
                    slotSignatures[slot] = frame.ContentSignature;
                    slot++;
                }
                nextRefreshSlot = 0;
            }
            else if (!decision.ShowExistingFrame)
            {
                decision.RefreshFrameFlags = 1 << nextRefreshSlot;
                slotValid[nextRefreshSlot] = frame.ContentSignature != 0;
                slotSignatures[nextRefreshSlot] = frame.ContentSignature;
                nextRefreshSlot++;
                if (nextRefreshSlot
                    >= Av1SyntaxConstants.StoredReferenceFrames - 1)
                {
                    nextRefreshSlot = 0;
                }
            }

            bufferLevel = UpdateBuffer(request, decision);
            decision.BufferLevelAfter = bufferLevel;
            result.TotalTargetBits = AddSaturated(
                result.TotalTargetBits,
                decision.TargetBits);
            result.TotalActualBits = AddSaturated(
                result.TotalActualBits,
                decision.ActualBits);
            result.Frames[encodeIndex] = decision;
            decodeTime += frame.Duration;
            encodeIndex++;
        }
        result.Duration = validation.PresentationTimes[
            validation.PresentationTimes.Length - 1]
            + request.Frames[request.Frames.Length - 1].Duration;
        result.FinalBufferLevelBits = bufferLevel;
        return result;
    }

    static Av1EncoderFrameScheduleResult CreateSchedule(
        Av1EncoderSequenceControlRequest request)
    {
        Av1EncoderFrameScheduleRequest scheduleRequest =
            new Av1EncoderFrameScheduleRequest();
        scheduleRequest.MaximumKeyFrameInterval =
            request.MaximumKeyFrameInterval;
        scheduleRequest.SceneChangeThresholdQ8 =
            request.SceneChangeThresholdQ8;
        scheduleRequest.Frames =
            new Av1EncoderFrameAnalysis[request.Frames.Length];
        int index = 0;
        while (index < request.Frames.Length)
        {
            Av1EncoderSequenceFrame frame = request.Frames[index];
            Av1EncoderFrameAnalysis analysis =
                new Av1EncoderFrameAnalysis();
            analysis.IntraCost = frame.IntraCost;
            analysis.InterCost = frame.InterCost;
            analysis.ForceKeyFrame = frame.ForceKeyFrame;
            analysis.OrderHint = index;
            scheduleRequest.Frames[index] = analysis;
            index++;
        }
        return Av1EncoderReferenceScheduler.Schedule(scheduleRequest);
    }

    static int[] BuildEncodeOrder(
        Av1EncoderFrameScheduleDecision[] schedule,
        int frameLag)
    {
        int[] order = new int[schedule.Length];
        int output = 0;
        int presentation = 0;
        while (presentation < schedule.Length)
        {
            order[output] = presentation;
            output++;
            presentation++;
            int gopEnd = presentation;
            while (gopEnd < schedule.Length && !schedule[gopEnd].KeyFrame)
            {
                gopEnd++;
            }
            while (presentation < gopEnd)
            {
                int anchor = presentation + frameLag;
                if (anchor >= gopEnd)
                {
                    anchor = gopEnd - 1;
                }
                order[output] = anchor;
                output++;
                int delayed = presentation;
                while (delayed < anchor)
                {
                    order[output] = delayed;
                    output++;
                    delayed++;
                }
                presentation = anchor + 1;
            }
        }
        return order;
    }

    static int FindReferenceSlot(
        long signature,
        bool[] slotValid,
        long[] slotSignatures)
    {
        int slot = 0;
        while (slot < slotValid.Length)
        {
            if (slotValid[slot] && slotSignatures[slot] == signature)
            {
                return slot;
            }
            slot++;
        }
        return -1;
    }

    static int ChooseQuantizer(
        Av1EncoderSequenceControlRequest request,
        Av1EncoderSequenceFrame frame,
        bool keyFrame,
        long bufferLevel)
    {
        int complexityAdjustment = (frame.ComplexityQ8 - 256) / 8;
        long halfBuffer = request.BufferSizeBits / 2;
        int bufferAdjustment = halfBuffer == 0
            ? 0
            : (int)((halfBuffer - bufferLevel) * 32 / halfBuffer);
        int quantizer = request.BaseQuantizer
            + complexityAdjustment + bufferAdjustment;
        if (keyFrame)
        {
            quantizer -= 8;
        }
        return Clip(
            request.MinimumQuantizer,
            request.MaximumQuantizer,
            quantizer);
    }

    static long GetActualBits(
        Av1EncoderSequenceFrame frame,
        long targetBits,
        int quantizer,
        bool keyFrame)
    {
        if (frame.EncodedSizeBytes >= 0)
        {
            return frame.EncodedSizeBytes * 8;
        }
        long actual = MultiplyDivideSaturated(
            targetBits,
            frame.ComplexityQ8,
            256);
        actual = MultiplyDivideSaturated(actual, 384 - quantizer, 256);
        if (keyFrame)
        {
            actual = MultiplyDivideSaturated(actual, 3, 2);
        }
        return actual < 1 ? 1 : actual;
    }

    static long UpdateBuffer(
        Av1EncoderSequenceControlRequest request,
        Av1EncoderSequenceFrameDecision decision)
    {
        long available = AddSaturated(
            decision.BufferLevelBefore,
            decision.TargetBits);
        decision.BufferUnderflow = decision.ActualBits > available;
        long remaining = decision.BufferUnderflow
            ? 0
            : available - decision.ActualBits;
        decision.BufferOverflow = remaining > request.BufferSizeBits;
        return decision.BufferOverflow ? request.BufferSizeBits : remaining;
    }

    static long MultiplyDivideSaturated(
        long value,
        int multiplier,
        int divisor)
    {
        long quotient = value / divisor;
        long remainder = value - quotient * divisor;
        if (quotient > long.MaxValue / multiplier)
        {
            return long.MaxValue;
        }
        long high = quotient * multiplier;
        long low = remainder * multiplier / divisor;
        return AddSaturated(high, low);
    }

    static long AddSaturated(long first, long second)
    {
        return second > long.MaxValue - first
            ? long.MaxValue
            : first + second;
    }

    static int Clip(int minimum, int maximum, int value)
    {
        if (value < minimum)
        {
            return minimum;
        }
        return value > maximum ? maximum : value;
    }

    static Av1EncoderSequenceValidation Validate(
        Av1EncoderSequenceControlRequest request)
    {
        if (request == null || request.Frames == null
            || request.Timescale <= 0 || request.TargetBitrate <= 0
            || request.BufferSizeBits <= 0
            || request.BufferSizeBits > long.MaxValue / 64
            || request.InitialBufferLevelBits < 0
            || request.InitialBufferLevelBits > request.BufferSizeBits
            || request.MaximumKeyFrameInterval < 1
            || request.SceneChangeThresholdQ8 < 1
            || request.FrameLag < 0 || request.FrameLag > 16
            || request.MinimumQuantizer < 0
            || request.MaximumQuantizer
                > Av1SyntaxConstants.MaxQuantizerIndex
            || request.MinimumQuantizer > request.MaximumQuantizer
            || request.BaseQuantizer < request.MinimumQuantizer
            || request.BaseQuantizer > request.MaximumQuantizer
            || request.ShowExistingFrameBits < 0)
        {
            return new Av1EncoderSequenceValidation(
                AvifResultCode.InvalidArgument);
        }
        Av1EncoderSequenceValidation validation =
            new Av1EncoderSequenceValidation(AvifResultCode.Success);
        validation.PresentationTimes = new long[request.Frames.Length];
        validation.TargetBits = new long[request.Frames.Length];
        long duration = 0;
        long previousCumulativeBits = 0;
        int index = 0;
        while (index < request.Frames.Length)
        {
            Av1EncoderSequenceFrame frame = request.Frames[index];
            if (frame == null || frame.Duration <= 0
                || duration > long.MaxValue - frame.Duration
                || frame.IntraCost < 0 || frame.InterCost < 0
                || frame.ComplexityQ8 < 1 || frame.ComplexityQ8 > 4096
                || frame.EncodedSizeBytes < -1
                || frame.EncodedSizeBytes > long.MaxValue / 8)
            {
                return new Av1EncoderSequenceValidation(
                    AvifResultCode.InvalidArgument);
            }
            validation.PresentationTimes[index] = duration;
            duration += frame.Duration;
            if (duration > long.MaxValue / request.TargetBitrate)
            {
                return new Av1EncoderSequenceValidation(
                    AvifResultCode.InvalidArgument);
            }
            long cumulativeBits = duration * request.TargetBitrate
                / request.Timescale;
            validation.TargetBits[index] =
                cumulativeBits - previousCumulativeBits;
            previousCumulativeBits = cumulativeBits;
            index++;
        }
        return validation;
    }
}
