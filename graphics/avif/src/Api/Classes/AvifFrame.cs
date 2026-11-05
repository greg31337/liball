using System;
using System.Collections.Generic;

namespace avif;

/// <summary>Describes one image-sequence frame and its timed sample signaling.</summary>
public class AvifFrame : IAvifFrame
{
    AvifResultCode code;
    AvifImage? image;
    AvifImage? alphaImage;
    uint duration;
    bool keyFrame;
    int forwardKeyFrameDistance;
    bool multipleFrames;
    bool switchFrame;
    int showExistingFrameSlot;
    int refreshFrameFlags;
    int temporalId;
    int spatialId;
    long contentSignature;
    AvifFilmGrain? filmGrain;
    int colorQuality;
    int alphaQuality;
    bool globalMotion;
    bool warpedMotion;
    List<uint>? metadataGroupParameters;
    List<AvifMetadataObu>? metadataObus;

    /// <summary>Creates one synchronization frame without a separate alpha image.</summary>
    /// <param name="image">The caller-owned color image.</param>
    /// <param name="duration">The positive duration in sequence-timescale units.</param>
    /// <returns>The initialized frame.</returns>
    internal static AvifFrame Create(AvifImage image, uint duration)
    {
        return new AvifFrame(image, null, duration, true);
    }

    /// <summary>Creates one color frame with explicit synchronization signaling.</summary>
    /// <param name="image">The caller-owned color image.</param>
    /// <param name="duration">The positive duration in sequence-timescale units.</param>
    /// <param name="keyFrame">Whether the sample is a random-access synchronization point.</param>
    /// <returns>The initialized frame.</returns>
    internal static AvifFrame Create(
        AvifImage image,
        uint duration,
        bool keyFrame)
    {
        return new AvifFrame(image, null, duration, keyFrame);
    }

    /// <summary>Creates one color frame with a matched separate alpha-track image.</summary>
    /// <param name="image">The caller-owned color image.</param>
    /// <param name="alphaImage">The caller-owned monochrome alpha image.</param>
    /// <param name="duration">The positive duration in sequence-timescale units.</param>
    /// <param name="keyFrame">Whether both samples are random-access synchronization points.</param>
    public AvifFrame(
        AvifImage image,
        AvifImage? alphaImage,
        uint duration,
        bool keyFrame)
    {
        Initialize(image, alphaImage, duration, keyFrame);
    }

    /// <summary>Creates a frame with a matched separate alpha-track image.</summary>
    /// <returns>The initialized frame.</returns>
    internal static AvifFrame Create(
        AvifImage image,
        AvifImage? alphaImage,
        uint duration,
        bool keyFrame)
    {
        return new AvifFrame(image, alphaImage, duration, keyFrame);
    }

    /// <summary>Returns frame validation state.</summary>
    /// <returns>A stable result code.</returns>
    public AvifResultCode GetCode()
    {
        return code;
    }

    /// <summary>Returns the caller-owned color image.</summary>
    /// <returns>The color image.</returns>
    public AvifImage? GetImage()
    {
        return image;
    }

    /// <summary>Returns the caller-owned separate alpha image.</summary>
    /// <returns>The alpha image, or null when absent.</returns>
    public AvifImage? GetAlphaImage()
    {
        return alphaImage;
    }

    /// <summary>Returns the frame duration in sequence-timescale units.</summary>
    /// <returns>The positive duration.</returns>
    public uint GetDuration()
    {
        return duration;
    }

    /// <summary>Reports whether the frame is a synchronization point.</summary>
    /// <returns><c>true</c> for a random-access frame.</returns>
    public bool IsKeyFrame()
    {
        return keyFrame;
    }

    /// <summary>Sets explicit show-existing-frame reference selection.</summary>
    /// <param name="slot">A stored reference slot from zero through seven, or negative one to disable.</param>
    /// <returns>Success or InvalidArgument.</returns>
    public AvifResultCode SetShowExistingFrame(int slot)
    {
        if (slot < -1 || slot > 7 || slot >= 0 && keyFrame)
        {
            return AvifResultCode.InvalidArgument;
        }
        showExistingFrameSlot = slot;
        if (slot >= 0)
        {
            refreshFrameFlags = 0;
        }
        return AvifResultCode.Success;
    }

    /// <summary>Returns the explicit show-existing reference slot.</summary>
    /// <returns>Zero through seven, or negative one when disabled.</returns>
    public int GetShowExistingFrameSlot()
    {
        return showExistingFrameSlot;
    }

    /// <summary>Sets the eight-bit reference refresh mask.</summary>
    /// <param name="flags">The mask, or negative one for automatic selection.</param>
    /// <returns>Success or InvalidArgument.</returns>
    public AvifResultCode SetRefreshFrameFlags(int flags)
    {
        if (flags < -1 || flags > 255
            || showExistingFrameSlot >= 0 && flags != 0)
        {
            return AvifResultCode.InvalidArgument;
        }
        refreshFrameFlags = flags;
        return AvifResultCode.Success;
    }

    /// <summary>Returns the requested reference refresh mask.</summary>
    /// <returns>Zero through 255, or negative one for automatic selection.</returns>
    public int GetRefreshFrameFlags()
    {
        return refreshFrameFlags;
    }

    /// <summary>Sets AV1 temporal and spatial layer identifiers.</summary>
    /// <param name="temporalId">The temporal identifier from zero through seven.</param>
    /// <param name="spatialId">The spatial identifier from zero through three.</param>
    /// <returns>Success or InvalidArgument.</returns>
    public AvifResultCode SetLayerIds(int temporalId, int spatialId)
    {
        if (temporalId < 0 || temporalId > 7
            || spatialId < 0 || spatialId > 3)
        {
            return AvifResultCode.InvalidArgument;
        }
        this.temporalId = temporalId;
        this.spatialId = spatialId;
        return AvifResultCode.Success;
    }

    /// <summary>Returns the temporal layer identifier.</summary>
    /// <returns>Zero through seven.</returns>
    public int GetTemporalId()
    {
        return temporalId;
    }

    /// <summary>Returns the spatial layer identifier.</summary>
    /// <returns>Zero through three.</returns>
    public int GetSpatialId()
    {
        return spatialId;
    }

    /// <summary>Sets a stable content signature used for automatic show-existing decisions.</summary>
    /// <param name="signature">A nonnegative caller-defined signature.</param>
    /// <returns>Success or InvalidArgument.</returns>
    public AvifResultCode SetContentSignature(long signature)
    {
        if (signature < 0)
        {
            return AvifResultCode.InvalidArgument;
        }
        contentSignature = signature;
        return AvifResultCode.Success;
    }

    /// <summary>Returns the automatic show-existing content signature.</summary>
    /// <returns>The nonnegative signature.</returns>
    public long GetContentSignature()
    {
        return contentSignature;
    }

    /// <summary>Snapshots film-grain synthesis parameters for this frame.</summary>
    /// <param name="grain">The parameters to validate and copy.</param>
    /// <returns>Success or InvalidArgument.</returns>
    public AvifResultCode SetFilmGrain(AvifFilmGrain grain)
    {
        if (!IsValidFilmGrain(grain))
        {
            return AvifResultCode.InvalidArgument;
        }
        filmGrain = CopyFilmGrain(grain);
        return AvifResultCode.Success;
    }

    /// <summary>Returns an independent film-grain parameter copy.</summary>
    /// <returns>The parameters, or null when absent.</returns>
    public AvifFilmGrain? GetFilmGrain()
    {
        return filmGrain == null ? null : CopyFilmGrain(filmGrain);
    }

    /// <summary>Sets independent color and alpha quality overrides.</summary>
    /// <param name="colorQuality">Zero through one hundred, or negative one to inherit.</param>
    /// <param name="alphaQuality">Zero through one hundred, or negative one to inherit.</param>
    /// <returns>Success or InvalidArgument.</returns>
    public AvifResultCode SetEncodingQuality(
        int colorQuality,
        int alphaQuality)
    {
        if (colorQuality < -1 || colorQuality > 100
            || alphaQuality < -1 || alphaQuality > 100)
        {
            return AvifResultCode.InvalidArgument;
        }
        this.colorQuality = colorQuality;
        this.alphaQuality = alphaQuality;
        return AvifResultCode.Success;
    }

    /// <summary>Returns the color quality override.</summary>
    /// <returns>Zero through one hundred, or negative one to inherit.</returns>
    public int GetColorQuality()
    {
        return colorQuality;
    }

    /// <summary>Returns the alpha quality override.</summary>
    /// <returns>Zero through one hundred, or negative one to inherit.</returns>
    public int GetAlphaQuality()
    {
        return alphaQuality;
    }

    /// <summary>Sets per-frame global and warped motion tool permission.</summary>
    /// <param name="globalMotion">Whether global motion is permitted.</param>
    /// <param name="warpedMotion">Whether local warped motion is permitted.</param>
    /// <returns>Success.</returns>
    public AvifResultCode SetMotionTools(bool globalMotion, bool warpedMotion)
    {
        this.globalMotion = globalMotion;
        this.warpedMotion = warpedMotion;
        return AvifResultCode.Success;
    }

    /// <summary>Reports whether global motion is permitted for this frame.</summary>
    /// <returns>The requested setting.</returns>
    public bool IsGlobalMotionEnabled()
    {
        return globalMotion;
    }

    /// <summary>Reports whether warped motion is permitted for this frame.</summary>
    /// <returns>The requested setting.</returns>
    public bool IsWarpedMotionEnabled()
    {
        return warpedMotion;
    }

    /// <summary>Signals delayed random access through an <c>av1f</c> sample group.</summary>
    /// <param name="distance">The number of intervening samples before the recovery sample.</param>
    /// <returns>Success or InvalidArgument.</returns>
    public AvifResultCode SetForwardKeyFrameDistance(int distance)
    {
        if (distance < 0 || distance > byte.MaxValue || keyFrame)
        {
            return AvifResultCode.InvalidArgument;
        }
        forwardKeyFrameDistance = distance;
        return AvifResultCode.Success;
    }

    /// <summary>Returns the <c>av1f</c> forward recovery distance.</summary>
    /// <returns>The distance, or minus one when not signaled.</returns>
    public int GetForwardKeyFrameDistance()
    {
        return forwardKeyFrameDistance;
    }

    /// <summary>Sets <c>av1m</c> multiple-frame temporal-unit signaling.</summary>
    /// <param name="value">Whether the sample carries multiple frames.</param>
    /// <returns>Success.</returns>
    public AvifResultCode SetMultipleFrames(bool value)
    {
        multipleFrames = value;
        return AvifResultCode.Success;
    }

    /// <summary>Reports whether <c>av1m</c> signaling is requested.</summary>
    /// <returns><c>true</c> when requested.</returns>
    public bool HasMultipleFrames()
    {
        return multipleFrames;
    }

    /// <summary>Sets <c>av1s</c> switch-frame signaling.</summary>
    /// <param name="value">Whether the sample starts with a switch frame.</param>
    /// <returns>Success.</returns>
    public AvifResultCode SetSwitchFrame(bool value)
    {
        switchFrame = value;
        return AvifResultCode.Success;
    }

    /// <summary>Reports whether <c>av1s</c> signaling is requested.</summary>
    /// <returns><c>true</c> when requested.</returns>
    public bool IsSwitchFrame()
    {
        return switchFrame;
    }

    /// <summary>Adds one distinct <c>av1M</c> grouping parameter.</summary>
    /// <param name="parameter">The metadata type in the high byte and type-specific low bits.</param>
    /// <returns>Success or InvalidArgument for a duplicate.</returns>
    public AvifResultCode AddMetadataGroup(uint parameter)
    {
        int index = 0;
        while (index < (metadataGroupParameters ?? throw new InvalidOperationException("Invalid internal state.")).Count)
        {
            if (metadataGroupParameters[index] == parameter)
            {
                return AvifResultCode.InvalidArgument;
            }
            index++;
        }
        metadataGroupParameters.Add(parameter);
        return AvifResultCode.Success;
    }

    /// <summary>Returns the number of <c>av1M</c> grouping parameters.</summary>
    /// <returns>The metadata group count.</returns>
    public int GetMetadataGroupCount()
    {
        return (metadataGroupParameters ?? throw new InvalidOperationException("Invalid internal state.")).Count;
    }

    /// <summary>Returns one <c>av1M</c> grouping parameter.</summary>
    /// <param name="index">The zero-based group index.</param>
    /// <returns>The parameter, or zero for an invalid index.</returns>
    public uint GetMetadataGroupParameter(int index)
    {
        if (index < 0 || index >= (metadataGroupParameters ?? throw new InvalidOperationException("Invalid internal state.")).Count)
        {
            return 0;
        }
        return metadataGroupParameters[index];
    }

    /// <summary>Adds one opaque metadata OBU to this sequence sample.</summary>
    /// <param name="metadata">The metadata value to snapshot.</param>
    /// <returns>Success or InvalidArgument.</returns>
    public AvifResultCode AddMetadataObu(AvifMetadataObu metadata)
    {
        if (code != AvifResultCode.Success || metadata == null
            || metadata.GetCode() != AvifResultCode.Success)
        {
            return AvifResultCode.InvalidArgument;
        }
        (metadataObus ?? throw new InvalidOperationException("Invalid internal state."))
            .Add(new AvifMetadataObu(
                metadata.GetMetadataType(),
                metadata.GetData()));
        if (metadata.GetMetadataType() <= byte.MaxValue)
        {
            uint parameter = (uint)metadata.GetMetadataType() << 24;
            bool present = false;
            int index = 0;
            while (index < (metadataGroupParameters
                ?? throw new InvalidOperationException("Invalid internal state.")).Count)
            {
                present = present || metadataGroupParameters[index] == parameter;
                index++;
            }
            if (!present)
            {
                metadataGroupParameters.Add(parameter);
            }
        }
        return AvifResultCode.Success;
    }

    /// <summary>Returns the number of opaque metadata OBUs attached to this sample.</summary>
    /// <returns>The metadata OBU count.</returns>
    public int GetMetadataObuCount()
    {
        return (metadataObus ?? throw new InvalidOperationException("Invalid internal state.")).Count;
    }

    /// <summary>Returns one independently owned opaque metadata OBU value.</summary>
    /// <param name="index">The zero-based metadata OBU index.</param>
    /// <returns>The metadata value, or null for an invalid index.</returns>
    public AvifMetadataObu? GetMetadataObu(int index)
    {
        if (index < 0 || index >= (metadataObus
            ?? throw new InvalidOperationException("Invalid internal state.")).Count)
        {
            return null;
        }
        AvifMetadataObu source = metadataObus[index];
        return new AvifMetadataObu(
            source.GetMetadataType(),
            source.GetData());
    }

    void Initialize(
        AvifImage requestedImage,
        AvifImage? requestedAlphaImage,
        uint requestedDuration,
        bool requestedKeyFrame)
    {
        image = requestedImage;
        alphaImage = requestedAlphaImage;
        duration = requestedDuration;
        keyFrame = requestedKeyFrame;
        forwardKeyFrameDistance = -1;
        showExistingFrameSlot = -1;
        refreshFrameFlags = -1;
        temporalId = 0;
        spatialId = 0;
        contentSignature = 0;
        filmGrain = null;
        colorQuality = -1;
        alphaQuality = -1;
        globalMotion = true;
        warpedMotion = true;
        metadataGroupParameters = new List<uint>();
        metadataObus = new List<AvifMetadataObu>();
        if (requestedImage == null
            || requestedImage.GetCode() != AvifResultCode.Success
            || requestedImage.GetAlphaMode() != AvifAlphaMode.None
            || requestedDuration == 0)
        {
            code = AvifResultCode.InvalidArgument;
            return;
        }
        if (requestedAlphaImage != null
            && (requestedAlphaImage.GetCode() != AvifResultCode.Success
                || requestedAlphaImage.GetAlphaMode() != AvifAlphaMode.None
                || requestedAlphaImage.GetChromaFormat()
                    != AvifChromaFormat.Monochrome
                || requestedAlphaImage.GetWidth() != requestedImage.GetWidth()
                || requestedAlphaImage.GetHeight() != requestedImage.GetHeight()
                || requestedAlphaImage.GetBitDepth() != requestedImage.GetBitDepth()))
        {
            code = AvifResultCode.NonConformingData;
            return;
        }
        code = AvifResultCode.Success;
    }

    static bool IsValidFilmGrain(AvifFilmGrain? grain)
    {
        return grain != null
            && grain.GrainSeed >= 0 && grain.GrainSeed <= ushort.MaxValue
            && grain.ReferenceIndex >= 0 && grain.ReferenceIndex <= 7
            && grain.ScalingShift >= 8 && grain.ScalingShift <= 11
            && grain.ArCoefficientLag >= 0 && grain.ArCoefficientLag <= 3
            && grain.ArCoefficientShift >= 6
            && grain.ArCoefficientShift <= 9
            && grain.GrainScaleShift >= 0 && grain.GrainScaleShift <= 3
            && HasValidPoints(grain.YPoints, 14)
            && HasValidPoints(grain.CbPoints, 10)
            && HasValidPoints(grain.CrPoints, 10)
            && grain.ArCoefficientsY != null
            && grain.ArCoefficientsCb != null
            && grain.ArCoefficientsCr != null;
    }

    static bool HasValidPoints(int[][]? points, int maximumCount)
    {
        if (points == null || points.Length > maximumCount)
        {
            return false;
        }
        int previous = -1;
        int index = 0;
        while (index < points.Length)
        {
            if (points[index] == null || points[index].Length != 2
                || points[index][0] <= previous || points[index][0] > 255
                || points[index][1] < 0 || points[index][1] > 255)
            {
                return false;
            }
            previous = points[index][0];
            index++;
        }
        return true;
    }

    static AvifFilmGrain CopyFilmGrain(AvifFilmGrain source)
    {
        AvifFilmGrain result = new AvifFilmGrain();
        result.GrainSeed = source.GrainSeed;
        result.UpdateGrain = source.UpdateGrain;
        result.ReferenceIndex = source.ReferenceIndex;
        result.YPoints = CopyPoints(source.YPoints);
        result.CbPoints = CopyPoints(source.CbPoints);
        result.CrPoints = CopyPoints(source.CrPoints);
        result.ChromaScalingFromLuma = source.ChromaScalingFromLuma;
        result.ScalingShift = source.ScalingShift;
        result.ArCoefficientLag = source.ArCoefficientLag;
        result.ArCoefficientsY = (int[])source.ArCoefficientsY.Clone();
        result.ArCoefficientsCb = (int[])source.ArCoefficientsCb.Clone();
        result.ArCoefficientsCr = (int[])source.ArCoefficientsCr.Clone();
        result.ArCoefficientShift = source.ArCoefficientShift;
        result.GrainScaleShift = source.GrainScaleShift;
        result.CbMultiplier = source.CbMultiplier;
        result.CbLumaMultiplier = source.CbLumaMultiplier;
        result.CbOffset = source.CbOffset;
        result.CrMultiplier = source.CrMultiplier;
        result.CrLumaMultiplier = source.CrLumaMultiplier;
        result.CrOffset = source.CrOffset;
        result.Overlap = source.Overlap;
        result.ClipToRestrictedRange = source.ClipToRestrictedRange;
        return result;
    }

    static int[][] CopyPoints(int[][] source)
    {
        int[][] result = new int[source.Length][];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = (int[])source[index].Clone();
            index++;
        }
        return result;
    }
}
