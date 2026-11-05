using System;
using System.Collections.Generic;

namespace avif;

/// <summary>Builds a timed image sequence or maintains decode position for one.</summary>
public class AvifSequence : IAvifSequence
{
    AvifImageSource? source;
    int currentFrameIndex;
    int nextFrameIndex;
    ulong iterationIndex;
    int lastDecodeStartIndex;
    int lastDecodeFrameCount;
    List<AvifFrame>? authoredFrames;
    uint authoredTimescale;
    bool authoring;
    bool repetitionInfinite;
    ulong repetitionCount;

    /// <summary>Creates an empty authoring sequence with a 1000-unit timescale.</summary>
    /// <returns>The initialized authoring sequence.</returns>
    internal static AvifSequence Create()
    {
        return new AvifSequence(1000);
    }

    /// <summary>Creates an empty authoring sequence with an explicit timescale.</summary>
    /// <param name="timescale">The positive number of media units per second.</param>
    public AvifSequence(uint timescale)
    {
        InitializeAuthoring(timescale);
    }

    /// <summary>Creates an authoring sequence with an explicit timescale.</summary>
    /// <returns>The initialized authoring sequence.</returns>
    internal static AvifSequence Create(uint timescale)
    {
        return new AvifSequence(timescale);
    }

    internal static AvifSequence FromSource(AvifImageSource source)
    {
        AvifSequence result = new AvifSequence(1000);
        result.source = source;
        result.authoredFrames = new List<AvifFrame>();
        result.authoredTimescale = 0;
        result.authoring = false;
        result.repetitionInfinite = false;
        result.repetitionCount = 0;
        result.Reset();
        return result;
    }

    /// <summary>Adds one validated frame to an authoring sequence.</summary>
    /// <param name="frame">The frame and timed sample signaling to retain.</param>
    /// <returns>Success, InvalidArgument, InvalidState, or NonConformingData.</returns>
    public AvifResultCode AddFrame(AvifFrame frame)
    {
        if (!authoring)
        {
            return AvifResultCode.InvalidState;
        }
        if (frame == null || frame.GetCode() != AvifResultCode.Success)
        {
            return AvifResultCode.InvalidArgument;
        }
        if ((authoredFrames ?? throw new InvalidOperationException("Invalid internal state.")).Count == 0 && !frame.IsKeyFrame())
        {
            return AvifResultCode.NonConformingData;
        }
        if (authoredFrames.Count > 0
            && !FramesAreCompatible(authoredFrames[0], frame))
        {
            return AvifResultCode.NonConformingData;
        }
        authoredFrames.Add(frame);
        return AvifResultCode.Success;
    }

    /// <summary>Returns one authoring frame.</summary>
    /// <param name="index">The zero-based frame index.</param>
    /// <returns>The frame, or null for a decoded sequence or invalid index.</returns>
    public AvifFrame? GetFrame(int index)
    {
        if (!authoring || index < 0 || index >= (authoredFrames ?? throw new InvalidOperationException("Invalid internal state.")).Count)
        {
            return null;
        }
        return authoredFrames[index];
    }

    /// <summary>Plans coded order, show-existing reuse, and reference refresh without encoding.</summary>
    /// <param name="options">The AV1 authoring controls.</param>
    /// <returns>An independently owned deterministic plan.</returns>
    public AvifSequenceEncodingPlan PlanEncoding(Av1EncoderOptions options)
    {
        if (!authoring || options == null || options.Limits == null
            || ResourceGuard.ValidateLimits(options.Limits)
                != AvifResultCode.Success
            || options.Mode < AvifEncoderMode.ConstantQuality
            || options.Mode > AvifEncoderMode.TargetBitrate
            || options.Preset < AvifEncoderPreset.Fastest
            || options.Preset > AvifEncoderPreset.Best
            || options.Quality < 0 || options.Quality > 100
            || options.Quantizer < -1 || options.Quantizer > 255
            || options.TargetSizeBytes < 0 || options.TargetBitrate < 0
            || options.FrameLag < 0 || options.FrameLag > 16
            || options.KeyframeInterval < 0
            || options.SequencePolicy < AvifSequenceEncodingPolicy.Automatic
            || options.SequencePolicy > AvifSequenceEncodingPolicy.InterFrame
            || options.TemporalLayerCount < 1
            || options.TemporalLayerCount > 8
            || options.SpatialLayers == null
            || options.SpatialLayers.Length > 4
            || options.OperatingPoints == null
            || options.OperatingPoints.Length > 32
            || options.SequencePolicy == AvifSequenceEncodingPolicy.StillPicture
                && ((authoredFrames ?? throw new InvalidOperationException("Invalid internal state.")).Count > 1
                    || options.FrameLag != 0
                    || options.TemporalLayerCount != 1
                    || options.SpatialLayers.Length > 1)
            || !HasValidPlanLayers(options)
            || !HasValidPlanOperatingPoints(options))
        {
            return new AvifSequenceEncodingPlan(
                AvifResultCode.InvalidArgument);
        }
        Av1EncoderSequenceControlRequest request =
            new Av1EncoderSequenceControlRequest();
        request.Timescale = authoredTimescale;
        request.TargetBitrate = options.TargetBitrate > 0
            ? options.TargetBitrate : 1000000;
        request.BufferSizeBits = request.TargetBitrate;
        request.InitialBufferLevelBits = request.TargetBitrate / 2;
        request.MaximumKeyFrameInterval = options.KeyframeInterval > 0
            ? options.KeyframeInterval : 240;
        request.FrameLag = options.FrameLag;
        request.BaseQuantizer = options.Quantizer >= 0
            ? options.Quantizer : (100 - options.Quality) * 255 / 100;
        request.Frames = new Av1EncoderSequenceFrame[
            (authoredFrames ?? throw new InvalidOperationException("Invalid internal state.")).Count];
        int index = 0;
        while (index < authoredFrames.Count)
        {
            AvifFrame frame = authoredFrames[index];
            int spatialLayerCount = options.SpatialLayers.Length == 0
                ? 1 : options.SpatialLayers.Length;
            if (frame.GetTemporalId() >= options.TemporalLayerCount
                || frame.GetSpatialId() >= spatialLayerCount
                || frame.GetShowExistingFrameSlot() >= 0
                    && !options.EnableShowExistingFrames
                || frame.GetFilmGrain() != null
                    && !options.FilmGrainParametersPresent
                || frame.IsKeyFrame() && frame.GetRefreshFrameFlags() >= 0
                    && frame.GetRefreshFrameFlags() != 255)
            {
                return new AvifSequenceEncodingPlan(
                    AvifResultCode.InvalidArgument);
            }
            Av1EncoderSequenceFrame planned =
                new Av1EncoderSequenceFrame();
            planned.Duration = frame.GetDuration();
            planned.IntraCost = 100;
            planned.InterCost = 20;
            planned.ForceKeyFrame = frame.IsKeyFrame();
            planned.ContentSignature = options.EnableShowExistingFrames
                ? frame.GetContentSignature() : 0;
            request.Frames[index] = planned;
            index++;
        }
        Av1EncoderSequenceControlResult internalPlan =
            Av1EncoderSequenceController.Plan(request);
        AvifSequenceEncodingPlan result =
            new AvifSequenceEncodingPlan(internalPlan.Code);
        if (internalPlan.Code != AvifResultCode.Success)
        {
            return result;
        }
        result.Frames = new AvifFrameEncodingDecision[
            internalPlan.Frames.Length];
        index = 0;
        while (index < internalPlan.Frames.Length)
        {
            Av1EncoderSequenceFrameDecision sourceDecision =
                internalPlan.Frames[index];
            AvifFrame sourceFrame = authoredFrames[
                sourceDecision.PresentationIndex];
            AvifFrameEncodingDecision decision =
                new AvifFrameEncodingDecision();
            decision.PresentationIndex = sourceDecision.PresentationIndex;
            decision.EncodeIndex = sourceDecision.EncodeIndex;
            decision.KeyFrame = sourceDecision.KeyFrame;
            decision.ShowExistingFrame = sourceDecision.ShowExistingFrame;
            decision.ShowExistingSlot = sourceDecision.ShowExistingSlot;
            decision.RefreshFrameFlags = sourceDecision.RefreshFrameFlags;
            decision.CompositionOffset = sourceDecision.CompositionOffset;
            if (sourceFrame.GetShowExistingFrameSlot() >= 0)
            {
                decision.ShowExistingFrame = true;
                decision.ShowExistingSlot =
                    sourceFrame.GetShowExistingFrameSlot();
                decision.RefreshFrameFlags = 0;
            }
            else if (sourceFrame.GetRefreshFrameFlags() >= 0)
            {
                decision.RefreshFrameFlags =
                    sourceFrame.GetRefreshFrameFlags();
            }
            result.Frames[index] = decision;
            index++;
        }
        return result;
    }

    bool HasValidPlanLayers(Av1EncoderOptions options)
    {
        AvifImage? firstImage = (authoredFrames
            ?? throw new InvalidOperationException("Invalid internal state.")).Count == 0
            ? null : authoredFrames[0].GetImage();
        int previousWidth = 0;
        int previousHeight = 0;
        int index = 0;
        while (index < options.SpatialLayers.Length)
        {
            Av1SpatialLayerOptions layer = options.SpatialLayers[index];
            if (layer == null || layer.Width <= previousWidth
                || layer.Height <= previousHeight
                || layer.Quality < -1 || layer.Quality > 100
                || firstImage != null
                    && (layer.Width > firstImage.GetWidth()
                        || layer.Height > firstImage.GetHeight()))
            {
                return false;
            }
            previousWidth = layer.Width;
            previousHeight = layer.Height;
            index++;
        }
        return true;
    }

    static bool HasValidPlanOperatingPoints(Av1EncoderOptions options)
    {
        int index = 0;
        while (index < options.OperatingPoints.Length)
        {
            Av1OperatingPointOptions point = options.OperatingPoints[index];
            if (point == null || point.Idc < 0 || point.Idc > 4095
                || point.Level < -1 || point.Level > 31
                || point.Tier < 0 || point.Tier > 1)
            {
                return false;
            }
            index++;
        }
        return true;
    }

    /// <summary>Sets the finite number of repetitions after the first presentation.</summary>
    /// <param name="count">The zero-based repetition count.</param>
    /// <returns>Success or InvalidState for a decoded sequence.</returns>
    public AvifResultCode SetRepetitionCount(ulong count)
    {
        if (!authoring)
        {
            return AvifResultCode.InvalidState;
        }
        repetitionInfinite = false;
        repetitionCount = count;
        return AvifResultCode.Success;
    }

    /// <summary>Sets unbounded sequence repetition.</summary>
    /// <returns>Success or InvalidState for a decoded sequence.</returns>
    public AvifResultCode SetInfiniteRepetition()
    {
        if (!authoring)
        {
            return AvifResultCode.InvalidState;
        }
        repetitionInfinite = true;
        repetitionCount = 0;
        return AvifResultCode.Success;
    }

    /// <summary>Returns the authoring media timescale.</summary>
    /// <returns>The positive timescale, or zero for a decoded sequence cursor.</returns>
    public uint GetTimescale()
    {
        return authoring ? authoredTimescale : 0;
    }

    /// <summary>Returns the number of frames in one sequence iteration.</summary>
    /// <returns>The frame count.</returns>
    public int GetFrameCount()
    {
        return authoring ? (authoredFrames ?? throw new InvalidOperationException("Invalid internal state.")).Count : (source ?? throw new InvalidOperationException("Invalid internal state.")).GetFrameCount();
    }

    /// <summary>Returns the current decoded frame index.</summary>
    /// <returns>The zero-based index, or minus one before the first frame.</returns>
    public int GetCurrentFrameIndex()
    {
        return currentFrameIndex;
    }

    /// <summary>Returns the zero-based playback iteration.</summary>
    /// <returns>Zero for the first presentation, one for the first repetition, and so on.</returns>
    public ulong GetIterationIndex()
    {
        return iterationIndex;
    }

    /// <summary>Advances decode state to the next frame, honoring declared repetition.</summary>
    /// <returns>Success, EndOfData, or a stable argument/state result.</returns>
    public AvifResultCode DecodeNextFrame()
    {
        if (authoring)
        {
            return AvifResultCode.InvalidState;
        }
        int frameCount = GetFrameCount();
        if (frameCount == 0)
        {
            ClearLastDecode();
            return AvifResultCode.EndOfData;
        }
        if (nextFrameIndex >= frameCount)
        {
            if (!CanRepeat())
            {
                ClearLastDecode();
                return AvifResultCode.EndOfData;
            }
            iterationIndex++;
            currentFrameIndex = -1;
            nextFrameIndex = 0;
        }
        return DecodeFrame(nextFrameIndex);
    }

    /// <summary>Moves decode state to an indexed frame from the nearest required keyframe.</summary>
    /// <param name="frameIndex">The zero-based target frame index.</param>
    /// <returns>Success or NotFound for an invalid frame index.</returns>
    public AvifResultCode DecodeFrame(int frameIndex)
    {
        if (authoring)
        {
            return AvifResultCode.InvalidState;
        }
        if (frameIndex < 0 || frameIndex >= GetFrameCount())
        {
            ClearLastDecode();
            return AvifResultCode.NotFound;
        }
        if (frameIndex == currentFrameIndex)
        {
            ClearLastDecode();
            nextFrameIndex = frameIndex + 1;
            return AvifResultCode.Success;
        }

        int nearestKeyFrame = (source ?? throw new InvalidOperationException("Invalid internal state.")).GetNearestKeyFrameIndex(frameIndex);
        if (nearestKeyFrame < 0)
        {
            ClearLastDecode();
            return AvifResultCode.InvalidTrack;
        }
        int decodeStart = currentFrameIndex + 1;
        if (nearestKeyFrame > decodeStart || frameIndex <= currentFrameIndex)
        {
            decodeStart = nearestKeyFrame;
        }
        lastDecodeStartIndex = decodeStart;
        lastDecodeFrameCount = frameIndex - decodeStart + 1;
        currentFrameIndex = frameIndex;
        nextFrameIndex = frameIndex + 1;
        return AvifResultCode.Success;
    }

    /// <summary>Returns the first frame scheduled by the most recent decode operation.</summary>
    /// <returns>The zero-based start index, or minus one when no frames were scheduled.</returns>
    public int GetLastDecodeStartIndex()
    {
        return lastDecodeStartIndex;
    }

    /// <summary>Returns the number of frames scheduled by the most recent decode operation.</summary>
    /// <returns>The scheduled frame count.</returns>
    public int GetLastDecodeFrameCount()
    {
        return lastDecodeFrameCount;
    }

    /// <summary>Returns timing for the current frame.</summary>
    /// <returns>The timing, or null before the first successful decode.</returns>
    public AvifFrameTiming? GetCurrentFrameTiming()
    {
        return authoring ? null : (source ?? throw new InvalidOperationException("Invalid internal state.")).GetFrameTiming(currentFrameIndex);
    }

    /// <summary>Returns the encoded range needed for the current indexed decode.</summary>
    /// <returns>The dependency range, or null before the first successful decode.</returns>
    public AvifByteRange? GetCurrentFrameDecodeRange()
    {
        return authoring ? null : (source ?? throw new InvalidOperationException("Invalid internal state.")).GetFrameDecodeRange(currentFrameIndex);
    }

    /// <summary>Reports whether the sequence declares a finite or infinite repetition count.</summary>
    /// <returns><c>true</c> when edit-list repetition semantics are known.</returns>
    public bool HasKnownRepetitionCount()
    {
        if (authoring)
        {
            return true;
        }
        AvifTrack? track = (source ?? throw new InvalidOperationException("Invalid internal state.")).GetSequenceTrack();
        return track != null && track.HasKnownRepetitionCount();
    }

    /// <summary>Reports whether sequence repetition is unbounded.</summary>
    /// <returns><c>true</c> for an infinite repeated edit list.</returns>
    public bool IsRepetitionInfinite()
    {
        if (authoring)
        {
            return repetitionInfinite;
        }
        AvifTrack? track = (source ?? throw new InvalidOperationException("Invalid internal state.")).GetSequenceTrack();
        return track != null && track.IsRepetitionInfinite();
    }

    /// <summary>Returns the finite number of repetitions after the first presentation.</summary>
    /// <returns>The repetition count, or zero when unknown, absent, or infinite.</returns>
    public ulong GetRepetitionCount()
    {
        if (authoring)
        {
            return repetitionInfinite ? 0 : repetitionCount;
        }
        AvifTrack? track = (source ?? throw new InvalidOperationException("Invalid internal state.")).GetSequenceTrack();
        return track == null ? 0 : track.GetRepetitionCount();
    }

    /// <summary>Resets decode position to before the first frame and first iteration.</summary>
    /// <returns>Success.</returns>
    public AvifResultCode Reset()
    {
        currentFrameIndex = -1;
        nextFrameIndex = 0;
        iterationIndex = 0;
        ClearLastDecode();
        return AvifResultCode.Success;
    }

    bool CanRepeat()
    {
        AvifTrack? track = (source ?? throw new InvalidOperationException("Invalid internal state.")).GetSequenceTrack();
        if (track == null || !track.HasKnownRepetitionCount())
        {
            return false;
        }
        return track.IsRepetitionInfinite()
            || iterationIndex < track.GetRepetitionCount();
    }

    void InitializeAuthoring(uint timescale)
    {
        source = null;
        authoredFrames = new List<AvifFrame>();
        authoredTimescale = timescale;
        authoring = true;
        repetitionInfinite = false;
        repetitionCount = 0;
        Reset();
    }

    static bool FramesAreCompatible(AvifFrame first, AvifFrame second)
    {
        AvifImage? firstImage = first.GetImage();
        AvifImage? secondImage = second.GetImage();
        bool firstHasAlpha = first.GetAlphaImage() != null;
        bool secondHasAlpha = second.GetAlphaImage() != null;
        return SequencePropertiesEqual(firstImage, secondImage)
            && firstHasAlpha == secondHasAlpha
            && (!firstHasAlpha || SequencePropertiesEqual(
                first.GetAlphaImage(),
                second.GetAlphaImage()));
    }

    static bool SequencePropertiesEqual(AvifImage? first, AvifImage? second)
    {
        return (first ?? throw new InvalidOperationException("Invalid internal state.")).GetWidth() == (second ?? throw new InvalidOperationException("Invalid internal state.")).GetWidth()
            && first.GetHeight() == second.GetHeight()
            && first.GetBitDepth() == second.GetBitDepth()
            && first.GetChromaFormat() == second.GetChromaFormat()
            && first.GetChromaSamplePosition()
                == second.GetChromaSamplePosition()
            && ColorInformationEquals(
                first.GetColorInfo(),
                second.GetColorInfo())
            && IccProfilesEqual(
                first.GetIccProfile(),
                second.GetIccProfile());
    }

    static bool ColorInformationEquals(
        AvifColorInfo? first,
        AvifColorInfo? second)
    {
        return first != null
            && second != null
            && first.ColorPrimaries == second.ColorPrimaries
            && first.TransferCharacteristics == second.TransferCharacteristics
            && first.MatrixCoefficients == second.MatrixCoefficients
            && first.Range == second.Range;
    }

    static bool IccProfilesEqual(
        AvifIccProfile? first,
        AvifIccProfile? second)
    {
        if (first == null || second == null)
        {
            return first == second;
        }
        byte[] firstData = first.GetData();
        byte[] secondData = second.GetData();
        if (firstData.Length != secondData.Length)
        {
            return false;
        }
        int index = 0;
        while (index < firstData.Length)
        {
            if (firstData[index] != secondData[index])
            {
                return false;
            }
            index++;
        }
        return true;
    }

    void ClearLastDecode()
    {
        lastDecodeStartIndex = -1;
        lastDecodeFrameCount = 0;
    }
}
