using System;
using System.Collections.Generic;

namespace avif;

internal class Av1EncoderFrameAnalysis
{
    internal long IntraCost;
    internal long InterCost;
    internal bool ForceKeyFrame;
    internal int OrderHint;
}

internal class Av1EncoderFrameScheduleRequest
{
    internal Av1EncoderFrameAnalysis[] Frames;
    internal int MaximumKeyFrameInterval;
    internal int SceneChangeThresholdQ8;

    internal Av1EncoderFrameScheduleRequest()
    {
        Frames = System.Array.Empty<Av1EncoderFrameAnalysis>();
        MaximumKeyFrameInterval = 240;
        SceneChangeThresholdQ8 = 384;
    }
}

internal class Av1EncoderFrameScheduleDecision
{
    internal int FrameIndex;
    internal Av1FrameType FrameType;
    internal bool KeyFrame;
    internal int RefreshFrameFlags;
    internal int PrimaryReferenceFrame;
    internal int[] ReferenceFrameIndexes;
    internal int[] ReferenceSlotFrames;

    internal Av1EncoderFrameScheduleDecision()
    {
        ReferenceFrameIndexes = new int[Av1SyntaxConstants.RefsPerFrame];
        ReferenceSlotFrames = new int[Av1SyntaxConstants.StoredReferenceFrames];
    }
}

internal class Av1EncoderFrameScheduleResult
{
    internal AvifResultCode Code;
    internal Av1EncoderFrameScheduleDecision[] Frames;

    internal Av1EncoderFrameScheduleResult(AvifResultCode code)
    {
        Code = code;
        Frames = System.Array.Empty<Av1EncoderFrameScheduleDecision>();
    }
}

internal static class Av1EncoderReferenceScheduler
{
    internal static Av1EncoderFrameScheduleResult Schedule(
        Av1EncoderFrameScheduleRequest request)
    {
        AvifResultCode validation = Validate(request);
        Av1EncoderFrameScheduleResult result =
            new Av1EncoderFrameScheduleResult(validation);
        if (validation != AvifResultCode.Success)
        {
            return result;
        }

        result.Frames =
            new Av1EncoderFrameScheduleDecision[request.Frames.Length];
        int[] slotFrames = new int[Av1SyntaxConstants.StoredReferenceFrames];
        int slot = 0;
        while (slot < slotFrames.Length)
        {
            slotFrames[slot] = -1;
            slot++;
        }
        int lastKeyFrame = -request.MaximumKeyFrameInterval;
        int nextRefreshSlot = 0;
        int index = 0;
        while (index < request.Frames.Length)
        {
            Av1EncoderFrameAnalysis analysis = request.Frames[index];
            bool key = index == 0 || analysis.ForceKeyFrame
                || index - lastKeyFrame >= request.MaximumKeyFrameInterval
                || IsSceneChange(analysis, request.SceneChangeThresholdQ8);
            Av1EncoderFrameScheduleDecision decision =
                new Av1EncoderFrameScheduleDecision();
            decision.FrameIndex = index;
            decision.KeyFrame = key;
            decision.FrameType = key ? Av1FrameType.Key : Av1FrameType.Inter;
            if (key)
            {
                decision.RefreshFrameFlags = 255;
                decision.PrimaryReferenceFrame = 7;
                slot = 0;
                while (slot < slotFrames.Length)
                {
                    slotFrames[slot] = index;
                    slot++;
                }
                lastKeyFrame = index;
                nextRefreshSlot = 0;
            }
            else
            {
                FillReferenceIndexes(decision.ReferenceFrameIndexes, slotFrames);
                decision.PrimaryReferenceFrame = 0;
                decision.RefreshFrameFlags = 1 << nextRefreshSlot;
                slotFrames[nextRefreshSlot] = index;
                nextRefreshSlot++;
                if (nextRefreshSlot >= Av1SyntaxConstants.StoredReferenceFrames - 1)
                {
                    nextRefreshSlot = 0;
                }
            }
            System.Array.Copy(
                slotFrames,
                decision.ReferenceSlotFrames,
                slotFrames.Length);
            result.Frames[index] = decision;
            index++;
        }
        return result;
    }

    static void FillReferenceIndexes(int[] references, int[] slotFrames)
    {
        int[] sortedSlots = new int[slotFrames.Length];
        int index = 0;
        while (index < sortedSlots.Length)
        {
            sortedSlots[index] = index;
            index++;
        }
        int first = 0;
        while (first < sortedSlots.Length - 1)
        {
            int best = first;
            int second = first + 1;
            while (second < sortedSlots.Length)
            {
                if (slotFrames[sortedSlots[second]]
                    > slotFrames[sortedSlots[best]])
                {
                    best = second;
                }
                second++;
            }
            int temporary = sortedSlots[first];
            sortedSlots[first] = sortedSlots[best];
            sortedSlots[best] = temporary;
            first++;
        }
        references[0] = sortedSlots[0];
        references[1] = sortedSlots[1];
        references[2] = sortedSlots[2];
        references[3] = Av1SyntaxConstants.StoredReferenceFrames - 1;
        references[4] = sortedSlots[4];
        references[5] = sortedSlots[5];
        references[6] = sortedSlots[6];
    }

    static bool IsSceneChange(
        Av1EncoderFrameAnalysis analysis,
        int thresholdQ8)
    {
        if (analysis.IntraCost == 0)
        {
            return analysis.InterCost > 0;
        }
        if (analysis.InterCost > long.MaxValue / 256
            || analysis.IntraCost > long.MaxValue / thresholdQ8)
        {
            return analysis.InterCost / thresholdQ8
                >= analysis.IntraCost / 256;
        }
        return analysis.InterCost * 256
            >= analysis.IntraCost * thresholdQ8;
    }

    static AvifResultCode Validate(Av1EncoderFrameScheduleRequest request)
    {
        if (request == null || request.Frames == null
            || request.MaximumKeyFrameInterval < 1
            || request.SceneChangeThresholdQ8 < 1)
        {
            return AvifResultCode.InvalidArgument;
        }
        int index = 0;
        while (index < request.Frames.Length)
        {
            if (request.Frames[index] == null
                || request.Frames[index].IntraCost < 0
                || request.Frames[index].InterCost < 0)
            {
                return AvifResultCode.InvalidArgument;
            }
            index++;
        }
        return AvifResultCode.Success;
    }
}

internal enum Av1EncoderInterMode
{
    Translation = 0,
    Compound = 1,
    InterIntra = 2,
    Global = 3,
    Warped = 4
}

internal class Av1EncoderInterReference
{
    internal Av1ReferenceFrame ReferenceFrame;
    internal PlaneBuffer? Plane;
    internal int OrderHint;
    internal int EstimatedRate;
    internal Av1WarpModelType GlobalMotionType;
    internal int[] GlobalMotionParameters;
    internal int[]? LocalWarpParameters;

    internal Av1EncoderInterReference()
    {
        ReferenceFrame = Av1ReferenceFrame.Last;
        GlobalMotionParameters = System.Array.Empty<int>();
        LocalWarpParameters = System.Array.Empty<int>();
    }
}

internal class Av1EncoderInterSearchRequest
{
    internal PlaneBuffer? Source;
    internal Av1EncoderInterReference[] References;
    internal int X;
    internal int Y;
    internal int Width;
    internal int Height;
    internal int BitDepth;
    internal int CurrentOrderHint;
    internal int MaximumIntegerSearchRadius;
    internal bool EnableSubpixelSearch;
    internal bool EnableCompound;
    internal bool EnableInterIntra;
    internal bool EnableGlobalMotion;
    internal bool EnableWarpedMotion;
    internal int[] IntraPrediction;
    internal int RateDistortionLambdaQ8;
    internal Av1EncoderKernelBackend KernelBackend;

    internal Av1EncoderInterSearchRequest()
    {
        References = System.Array.Empty<Av1EncoderInterReference>();
        BitDepth = 8;
        MaximumIntegerSearchRadius = 4;
        EnableSubpixelSearch = true;
        IntraPrediction = System.Array.Empty<int>();
        KernelBackend = Av1EncoderKernelBackend.Automatic;
    }
}

internal class Av1EncoderInterSearchCandidate
{
    internal Av1EncoderInterMode Mode;
    internal Av1ReferenceFrame FirstReference;
    internal Av1ReferenceFrame SecondReference;
    internal Av1MotionVector FirstMotionVector;
    internal Av1MotionVector SecondMotionVector;
    internal Av1CompoundPredictionType CompoundType;
    internal Av1InterIntraMode InterIntraMode;
    internal int[] Prediction;
    internal long Distortion;
    internal int EstimatedRate;
    internal long RateDistortionScore;
    internal int Order;

    internal Av1EncoderInterSearchCandidate()
    {
        SecondReference = Av1ReferenceFrame.None;
        FirstMotionVector = new Av1MotionVector(0, 0);
        SecondMotionVector = new Av1MotionVector(0, 0);
        Prediction = System.Array.Empty<int>();
    }
}

internal class Av1EncoderInterSearchResult
{
    internal AvifResultCode Code;
    internal Av1EncoderInterSearchCandidate? BestCandidate;
    internal Av1EncoderInterSearchCandidate[] Candidates;
    internal Av1EncoderKernelBackend KernelBackend;

    internal Av1EncoderInterSearchResult(AvifResultCode code)
    {
        Code = code;
        Candidates = System.Array.Empty<Av1EncoderInterSearchCandidate>();
    }
}

// Encoder-side inter search for AV1 specification sections 7.10 and 7.11.3.
internal class Av1EncoderInterSearcher
{
    internal Av1EncoderInterSearchRequest Request;
    internal List<Av1EncoderInterSearchCandidate> Candidates;
    internal int NextOrder;
    internal int[] SourcePixels;
    internal Av1EncoderKernelBackend KernelBackend;

    internal Av1EncoderInterSearcher(Av1EncoderInterSearchRequest request)
    {
        Request = request;
        Candidates = new List<Av1EncoderInterSearchCandidate>();
        SourcePixels = ReadRegion(
            request.Source,
            request.X,
            request.Y,
            request.Width,
            request.Height);
        KernelBackend = Av1EncoderKernelDispatcher.ResolveBackend(
            request.KernelBackend);
    }

    internal static Av1EncoderInterSearchResult Search(
        Av1EncoderInterSearchRequest request)
    {
        AvifResultCode validation = Validate(request);
        Av1EncoderInterSearchResult result =
            new Av1EncoderInterSearchResult(validation);
        if (validation != AvifResultCode.Success)
        {
            return result;
        }
        Av1EncoderInterSearcher searcher = new Av1EncoderInterSearcher(request);
        searcher.SearchCandidates();
        result.Candidates = searcher.Candidates.ToArray();
        result.KernelBackend = searcher.KernelBackend;
        int index = 0;
        while (index < result.Candidates.Length)
        {
            if (result.BestCandidate == null
                || Compare(result.Candidates[index], result.BestCandidate) < 0)
            {
                result.BestCandidate = result.Candidates[index];
            }
            index++;
        }
        result.Code = result.BestCandidate == null
            ? AvifResultCode.EncodeFailed
            : AvifResultCode.Success;
        return result;
    }

    void SearchCandidates()
    {
        Av1EncoderInterSearchCandidate?[] bestByReference =
            new Av1EncoderInterSearchCandidate[Request.References.Length];
        int referenceIndex = 0;
        while (referenceIndex < Request.References.Length)
        {
            Av1EncoderInterReference reference =
                Request.References[referenceIndex];
            Av1EncoderInterSearchCandidate? translation =
                SearchTranslation(reference);
            AddCandidate(translation);
            bestByReference[referenceIndex] = translation;
            if (Request.EnableGlobalMotion
                && reference.GlobalMotionType != Av1WarpModelType.Identity)
            {
                Av1EncoderInterSearchCandidate? global = RenderWarped(
                    reference,
                    Av1EncoderInterMode.Global,
                    reference.GlobalMotionType,
                    reference.GlobalMotionParameters);
                AddCandidate(global);
                if (global != null
                    && (bestByReference[referenceIndex] == null
                        || Compare(global,
                            bestByReference[referenceIndex]) < 0))
                {
                    bestByReference[referenceIndex] = global;
                }
            }
            if (Request.EnableWarpedMotion
                && (reference.LocalWarpParameters ?? throw new InvalidOperationException("Invalid internal state.")).Length >= 6)
            {
                Av1EncoderInterSearchCandidate? warped = RenderWarped(
                    reference,
                    Av1EncoderInterMode.Warped,
                    Av1WarpModelType.Identity,
                    reference.LocalWarpParameters);
                AddCandidate(warped);
                if (warped != null
                    && (bestByReference[referenceIndex] == null
                        || Compare(warped,
                            bestByReference[referenceIndex]) < 0))
                {
                    bestByReference[referenceIndex] = warped;
                }
            }
            referenceIndex++;
        }
        if (Request.EnableCompound)
        {
            AddCompoundCandidates(bestByReference);
        }
        if (Request.EnableInterIntra)
        {
            AddInterIntraCandidates(bestByReference);
        }
    }

    Av1EncoderInterSearchCandidate? SearchTranslation(
        Av1EncoderInterReference reference)
    {
        Av1EncoderInterSearchCandidate? best = null;
        int row = -Request.MaximumIntegerSearchRadius;
        while (row <= Request.MaximumIntegerSearchRadius)
        {
            int column = -Request.MaximumIntegerSearchRadius;
            while (column <= Request.MaximumIntegerSearchRadius)
            {
                Av1EncoderInterSearchCandidate? candidate = RenderTranslation(
                    reference,
                    new Av1MotionVector(row * 8, column * 8));
                if (candidate != null
                    && (best == null || Compare(candidate, best) < 0))
                {
                    best = candidate;
                }
                column++;
            }
            row++;
        }
        if (!Request.EnableSubpixelSearch || best == null)
        {
            return best;
        }
        int step = 4;
        while (step >= 1)
        {
            Av1EncoderInterSearchCandidate refined = best;
            int rowOffset = -step;
            while (rowOffset <= step)
            {
                int columnOffset = -step;
                while (columnOffset <= step)
                {
                    Av1MotionVector vector = new Av1MotionVector(
                        best.FirstMotionVector.Row + rowOffset,
                        best.FirstMotionVector.Column + columnOffset);
                    Av1EncoderInterSearchCandidate? candidate =
                        RenderTranslation(reference, vector);
                    if (candidate != null && Compare(candidate, refined) < 0)
                    {
                        refined = candidate;
                    }
                    columnOffset += step;
                }
                rowOffset += step;
            }
            best = refined;
            step >>= 1;
        }
        return best;
    }

    Av1EncoderInterSearchCandidate? RenderTranslation(
        Av1EncoderInterReference reference,
        Av1MotionVector vector)
    {
        Av1InterPredictionRequest prediction = CreatePredictionRequest(reference);
        prediction.MotionVector = vector;
        int[]? pixels = RenderPrediction(prediction);
        if (pixels == null)
        {
            return null;
        }
        Av1EncoderInterSearchCandidate candidate = CreateCandidate(
            Av1EncoderInterMode.Translation,
            reference,
            pixels,
            reference.EstimatedRate + 2 + MotionVectorRate(vector));
        candidate.FirstMotionVector = vector;
        return candidate;
    }

    Av1EncoderInterSearchCandidate? RenderWarped(
        Av1EncoderInterReference reference,
        Av1EncoderInterMode mode,
        Av1WarpModelType globalType,
        int[] parameters)
    {
        Av1InterPredictionRequest prediction = CreatePredictionRequest(reference);
        prediction.WarpParameters = parameters;
        if (mode == Av1EncoderInterMode.Global)
        {
            prediction.GlobalMotionType = globalType;
        }
        else
        {
            prediction.MotionMode = Av1MotionMode.LocalWarp;
        }
        int[]? pixels = RenderPrediction(prediction);
        if (pixels == null)
        {
            return null;
        }
        return CreateCandidate(
            mode,
            reference,
            pixels,
            reference.EstimatedRate
                + (mode == Av1EncoderInterMode.Global ? 4 : 8));
    }

    Av1InterPredictionRequest CreatePredictionRequest(
        Av1EncoderInterReference reference)
    {
        PlaneBuffer destination = new PlaneBuffer(
            Request.X + Request.Width,
            Request.Y + Request.Height,
            Request.BitDepth > 8 ? 2 : 1,
            1,
            (Request.Source ?? throw new InvalidOperationException("Invalid internal state.")).MaxBytes,
            true);
        Av1InterPredictionRequest prediction = new Av1InterPredictionRequest();
        prediction.Destination = destination;
        prediction.Reference = reference.Plane;
        prediction.X = Request.X;
        prediction.Y = Request.Y;
        prediction.Width = Request.Width;
        prediction.Height = Request.Height;
        prediction.BitDepth = Request.BitDepth;
        return prediction;
    }

    int[]? RenderPrediction(Av1InterPredictionRequest prediction)
    {
        AvifResultCode code = Av1InterPredictor.Predict(prediction);
        if (code != AvifResultCode.Success)
        {
            (prediction.Destination ?? throw new InvalidOperationException("Invalid internal state.")).Close();
            return null;
        }
        int[] pixels = ReadRegion(
            prediction.Destination,
            Request.X,
            Request.Y,
            Request.Width,
            Request.Height);
        (prediction.Destination ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        return pixels;
    }

    void AddCompoundCandidates(
        Av1EncoderInterSearchCandidate?[] bestByReference)
    {
        int first = 0;
        while (first < bestByReference.Length)
        {
            int second = first + 1;
            while (second < bestByReference.Length)
            {
                Av1EncoderInterSearchCandidate? firstCandidate =
                    bestByReference[first];
                Av1EncoderInterSearchCandidate? secondCandidate =
                    bestByReference[second];
                if (firstCandidate != null && secondCandidate != null)
                {
                    int type = (int)Av1CompoundPredictionType.Average;
                    while (type <= (int)Av1CompoundPredictionType.DifferenceWeighted)
                    {
                        AddCandidate(RenderCompound(
                            firstCandidate,
                            secondCandidate,
                            (Av1CompoundPredictionType)type));
                        type++;
                    }
                }
                second++;
            }
            first++;
        }
    }

    Av1EncoderInterSearchCandidate? RenderCompound(
        Av1EncoderInterSearchCandidate first,
        Av1EncoderInterSearchCandidate second,
        Av1CompoundPredictionType type)
    {
        PlaneBuffer firstPlane = CreatePredictionPlane(first.Prediction);
        PlaneBuffer secondPlane = CreatePredictionPlane(second.Prediction);
        PlaneBuffer destination = CreatePredictionPlane(
            new int[Request.Width * Request.Height]);
        Av1CompoundPredictionRequest compound =
            new Av1CompoundPredictionRequest();
        compound.Destination = destination;
        compound.First = firstPlane;
        compound.Second = secondPlane;
        compound.Width = Request.Width;
        compound.Height = Request.Height;
        compound.BitDepth = Request.BitDepth;
        compound.Type = type;
        compound.FirstDistance = GetReferenceDistance(first.FirstReference);
        compound.SecondDistance = GetReferenceDistance(second.FirstReference);
        AvifResultCode code = Av1CompoundPredictor.Predict(compound);
        int[]? pixels = code == AvifResultCode.Success
            ? ReadRegion(destination, 0, 0, Request.Width, Request.Height)
            : null;
        firstPlane.Close();
        secondPlane.Close();
        destination.Close();
        if (pixels == null)
        {
            return null;
        }
        Av1EncoderInterSearchCandidate candidate = CreateCandidate(
            Av1EncoderInterMode.Compound,
            GetReference(first.FirstReference),
            pixels,
            first.EstimatedRate + second.EstimatedRate + 5 + (int)type);
        candidate.SecondReference = second.FirstReference;
        candidate.FirstMotionVector = first.FirstMotionVector;
        candidate.SecondMotionVector = second.FirstMotionVector;
        candidate.CompoundType = type;
        return candidate;
    }

    void AddInterIntraCandidates(
        Av1EncoderInterSearchCandidate?[] bestByReference)
    {
        int reference = 0;
        while (reference < bestByReference.Length)
        {
            Av1EncoderInterSearchCandidate? candidate =
                bestByReference[reference];
            if (candidate != null)
            {
                int mode = (int)Av1InterIntraMode.Dc;
                while (mode <= (int)Av1InterIntraMode.Smooth)
                {
                    AddCandidate(RenderInterIntra(
                        candidate,
                        (Av1InterIntraMode)mode));
                    mode++;
                }
            }
            reference++;
        }
    }

    Av1EncoderInterSearchCandidate? RenderInterIntra(
        Av1EncoderInterSearchCandidate inter,
        Av1InterIntraMode mode)
    {
        PlaneBuffer interPlane = CreatePredictionPlane(inter.Prediction);
        PlaneBuffer intraPlane = CreatePredictionPlane(Request.IntraPrediction);
        PlaneBuffer destination = CreatePredictionPlane(
            new int[Request.Width * Request.Height]);
        Av1InterIntraPredictionRequest blend =
            new Av1InterIntraPredictionRequest();
        blend.Destination = destination;
        blend.Inter = interPlane;
        blend.Intra = intraPlane;
        blend.Width = Request.Width;
        blend.Height = Request.Height;
        blend.BitDepth = Request.BitDepth;
        blend.Mode = mode;
        AvifResultCode code = Av1CompoundPredictor.PredictInterIntra(blend);
        int[]? pixels = code == AvifResultCode.Success
            ? ReadRegion(destination, 0, 0, Request.Width, Request.Height)
            : null;
        interPlane.Close();
        intraPlane.Close();
        destination.Close();
        if (pixels == null)
        {
            return null;
        }
        Av1EncoderInterSearchCandidate candidate = CreateCandidate(
            Av1EncoderInterMode.InterIntra,
            GetReference(inter.FirstReference),
            pixels,
            inter.EstimatedRate + 4 + (int)mode);
        candidate.FirstMotionVector = inter.FirstMotionVector;
        candidate.InterIntraMode = mode;
        return candidate;
    }

    Av1EncoderInterSearchCandidate CreateCandidate(
        Av1EncoderInterMode mode,
        Av1EncoderInterReference? reference,
        int[] prediction,
        int rate)
    {
        Av1EncoderInterSearchCandidate candidate =
            new Av1EncoderInterSearchCandidate();
        candidate.Mode = mode;
        candidate.FirstReference = (reference ?? throw new InvalidOperationException("Invalid internal state.")).ReferenceFrame;
        candidate.Prediction = prediction;
        candidate.Distortion = GetDistortion(prediction);
        candidate.EstimatedRate = rate;
        candidate.RateDistortionScore = GetScore(
            candidate.Distortion,
            rate,
            Request.RateDistortionLambdaQ8);
        candidate.Order = NextOrder++;
        return candidate;
    }

    void AddCandidate(Av1EncoderInterSearchCandidate? candidate)
    {
        if (candidate != null)
        {
            Candidates.Add(candidate);
        }
    }

    PlaneBuffer CreatePredictionPlane(int[] pixels)
    {
        PlaneBuffer plane = new PlaneBuffer(
            Request.Width,
            Request.Height,
            Request.BitDepth > 8 ? 2 : 1,
            1,
            (Request.Source ?? throw new InvalidOperationException("Invalid internal state.")).MaxBytes,
            true);
        int row = 0;
        while (row < Request.Height)
        {
            Span<byte> bytes = plane.GetRowSpan(row);
            int column = 0;
            while (column < Request.Width)
            {
                int value = pixels[row * Request.Width + column];
                int offset = column * plane.BytesPerSample;
                bytes[offset] = IntegerConversion.ToByte(value);
                if (plane.BytesPerSample == 2)
                {
                    bytes[offset + 1] = IntegerConversion.ToByte(value >> 8);
                }
                column++;
            }
            row++;
        }
        return plane;
    }

    long GetDistortion(int[] prediction)
    {
        Av1EncoderKernelResult result =
            Av1EncoderKernelDispatcher.SumSquaredError(
                SourcePixels,
                prediction,
                Request.KernelBackend);
        return result.Code == AvifResultCode.Success
            ? result.Value
            : long.MaxValue;
    }

    int GetReferenceDistance(Av1ReferenceFrame frame)
    {
        Av1EncoderInterReference? reference = GetReference(frame);
        int distance = reference == null
            ? 1
            : Absolute(Request.CurrentOrderHint - reference.OrderHint);
        return distance < 1 ? 1 : distance;
    }

    Av1EncoderInterReference? GetReference(Av1ReferenceFrame frame)
    {
        int index = 0;
        while (index < Request.References.Length)
        {
            if (Request.References[index].ReferenceFrame == frame)
            {
                return Request.References[index];
            }
            index++;
        }
        return null;
    }

    static int[] ReadRegion(
        PlaneBuffer? plane,
        int x,
        int y,
        int width,
        int height)
    {
        int[] pixels = new int[width * height];
        int row = 0;
        while (row < height)
        {
            Span<byte> bytes = (plane ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(y + row);
            int column = 0;
            while (column < width)
            {
                int offset = (x + column) * plane.BytesPerSample;
                pixels[row * width + column] = plane.BytesPerSample == 1
                    ? bytes[offset]
                    : bytes[offset] | bytes[offset + 1] << 8;
                column++;
            }
            row++;
        }
        return pixels;
    }

    static int MotionVectorRate(Av1MotionVector vector)
    {
        return SignedBits(vector.Row) + SignedBits(vector.Column);
    }

    static int SignedBits(int value)
    {
        int magnitude = Absolute(value);
        int bits = 1;
        while (magnitude > 0)
        {
            magnitude >>= 1;
            bits++;
        }
        return bits;
    }

    static int Compare(
        Av1EncoderInterSearchCandidate first,
        Av1EncoderInterSearchCandidate? second)
    {
        if (first.RateDistortionScore != (second ?? throw new InvalidOperationException("Invalid internal state.")).RateDistortionScore)
        {
            return first.RateDistortionScore < second.RateDistortionScore ? -1 : 1;
        }
        if (first.Distortion != second.Distortion)
        {
            return first.Distortion < second.Distortion ? -1 : 1;
        }
        if (first.EstimatedRate != second.EstimatedRate)
        {
            return first.EstimatedRate < second.EstimatedRate ? -1 : 1;
        }
        if (first.Mode != second.Mode)
        {
            return first.Mode < second.Mode ? -1 : 1;
        }
        if (first.FirstReference != second.FirstReference)
        {
            return first.FirstReference < second.FirstReference ? -1 : 1;
        }
        if (first.FirstMotionVector.Row != second.FirstMotionVector.Row)
        {
            return first.FirstMotionVector.Row < second.FirstMotionVector.Row ? -1 : 1;
        }
        if (first.FirstMotionVector.Column != second.FirstMotionVector.Column)
        {
            return first.FirstMotionVector.Column < second.FirstMotionVector.Column ? -1 : 1;
        }
        return first.Order < second.Order
            ? -1
            : (first.Order > second.Order ? 1 : 0);
    }

    static long GetScore(long distortion, int rate, int lambdaQ8)
    {
        if (lambdaQ8 == 0 || rate == 0)
        {
            return distortion;
        }
        if ((long)rate > (long.MaxValue - 128) / lambdaQ8)
        {
            return long.MaxValue;
        }
        return AddSaturated(
            distortion,
            ((long)rate * lambdaQ8 + 128) >> 8);
    }

    static long AddSaturated(long first, long second)
    {
        return second > long.MaxValue - first
            ? long.MaxValue
            : first + second;
    }

    static int Absolute(int value)
    {
        return value < 0 ? -value : value;
    }

    static AvifResultCode Validate(Av1EncoderInterSearchRequest request)
    {
        if (request == null || request.Source == null
            || request.Source.GetStateCode() != AvifResultCode.Success
            || request.References == null || request.References.Length == 0
            || request.X < 0 || request.Y < 0
            || request.Width <= 0 || request.Height <= 0
            || request.X > request.Source.Width - request.Width
            || request.Y > request.Source.Height - request.Height
            || request.BitDepth != 8 && request.BitDepth != 10
                && request.BitDepth != 12
            || request.Source.BytesPerSample != (request.BitDepth > 8 ? 2 : 1)
            || request.MaximumIntegerSearchRadius < 0
            || request.MaximumIntegerSearchRadius > 64
            || request.RateDistortionLambdaQ8 < 0
            || request.KernelBackend < Av1EncoderKernelBackend.Automatic
            || request.KernelBackend > Av1EncoderKernelBackend.Intrinsic
            || request.EnableInterIntra
                && (request.IntraPrediction == null
                    || request.IntraPrediction.Length
                        != request.Width * request.Height))
        {
            return AvifResultCode.InvalidArgument;
        }
        int index = 0;
        while (index < request.References.Length)
        {
            Av1EncoderInterReference reference = request.References[index];
            if (reference == null || reference.Plane == null
                || reference.Plane.GetStateCode() != AvifResultCode.Success
                || reference.Plane.BytesPerSample != request.Source.BytesPerSample
                || reference.ReferenceFrame < Av1ReferenceFrame.Last
                || reference.ReferenceFrame > Av1ReferenceFrame.Alternate
                || reference.EstimatedRate < 0
                || reference.GlobalMotionType < Av1WarpModelType.Identity
                || reference.GlobalMotionType > Av1WarpModelType.Affine
                || reference.GlobalMotionParameters == null
                || reference.LocalWarpParameters == null
                || HasDuplicateReference(request.References, index))
            {
                return AvifResultCode.InvalidArgument;
            }
            index++;
        }
        return AvifResultCode.Success;
    }

    static bool HasDuplicateReference(
        Av1EncoderInterReference[] references,
        int index)
    {
        int previous = 0;
        while (previous < index)
        {
            if (references[previous].ReferenceFrame
                == references[index].ReferenceFrame)
            {
                return true;
            }
            previous++;
        }
        return false;
    }
}
