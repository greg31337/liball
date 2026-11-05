using System;
using System.Collections.Generic;

namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifFrame"/>.</summary>
public interface IAvifFrame
{
    /// <summary>Returns frame validation state.</summary>
    /// <returns>A stable result code.</returns>
    AvifResultCode GetCode();

    /// <summary>Returns the caller-owned color image.</summary>
    /// <returns>The color image.</returns>
    AvifImage? GetImage();

    /// <summary>Returns the caller-owned separate alpha image.</summary>
    /// <returns>The alpha image, or null when absent.</returns>
    AvifImage? GetAlphaImage();

    /// <summary>Returns the frame duration in sequence-timescale units.</summary>
    /// <returns>The positive duration.</returns>
    uint GetDuration();

    /// <summary>Reports whether the frame is a synchronization point.</summary>
    /// <returns><c>true</c> for a random-access frame.</returns>
    bool IsKeyFrame();

    /// <summary>Sets explicit show-existing-frame reference selection.</summary>
    /// <param name="slot">A stored reference slot from zero through seven, or negative one to disable.</param>
    /// <returns>Success or InvalidArgument.</returns>
    AvifResultCode SetShowExistingFrame(int slot);

    /// <summary>Returns the explicit show-existing reference slot.</summary>
    /// <returns>Zero through seven, or negative one when disabled.</returns>
    int GetShowExistingFrameSlot();

    /// <summary>Sets the eight-bit reference refresh mask.</summary>
    /// <param name="flags">The mask, or negative one for automatic selection.</param>
    /// <returns>Success or InvalidArgument.</returns>
    AvifResultCode SetRefreshFrameFlags(int flags);

    /// <summary>Returns the requested reference refresh mask.</summary>
    /// <returns>Zero through 255, or negative one for automatic selection.</returns>
    int GetRefreshFrameFlags();

    /// <summary>Sets AV1 temporal and spatial layer identifiers.</summary>
    /// <param name="temporalId">The temporal identifier from zero through seven.</param>
    /// <param name="spatialId">The spatial identifier from zero through three.</param>
    /// <returns>Success or InvalidArgument.</returns>
    AvifResultCode SetLayerIds(int temporalId, int spatialId);

    /// <summary>Returns the temporal layer identifier.</summary>
    /// <returns>Zero through seven.</returns>
    int GetTemporalId();

    /// <summary>Returns the spatial layer identifier.</summary>
    /// <returns>Zero through three.</returns>
    int GetSpatialId();

    /// <summary>Sets a stable content signature used for automatic show-existing decisions.</summary>
    /// <param name="signature">A nonnegative caller-defined signature.</param>
    /// <returns>Success or InvalidArgument.</returns>
    AvifResultCode SetContentSignature(long signature);

    /// <summary>Returns the automatic show-existing content signature.</summary>
    /// <returns>The nonnegative signature.</returns>
    long GetContentSignature();

    /// <summary>Snapshots film-grain synthesis parameters for this frame.</summary>
    /// <param name="grain">The parameters to validate and copy.</param>
    /// <returns>Success or InvalidArgument.</returns>
    AvifResultCode SetFilmGrain(AvifFilmGrain grain);

    /// <summary>Returns an independent film-grain parameter copy.</summary>
    /// <returns>The parameters, or null when absent.</returns>
    AvifFilmGrain? GetFilmGrain();

    /// <summary>Sets independent color and alpha quality overrides.</summary>
    /// <param name="colorQuality">Zero through one hundred, or negative one to inherit.</param>
    /// <param name="alphaQuality">Zero through one hundred, or negative one to inherit.</param>
    /// <returns>Success or InvalidArgument.</returns>
    AvifResultCode SetEncodingQuality(int colorQuality, int alphaQuality);

    /// <summary>Returns the color quality override.</summary>
    /// <returns>Zero through one hundred, or negative one to inherit.</returns>
    int GetColorQuality();

    /// <summary>Returns the alpha quality override.</summary>
    /// <returns>Zero through one hundred, or negative one to inherit.</returns>
    int GetAlphaQuality();

    /// <summary>Sets per-frame global and warped motion tool permission.</summary>
    /// <param name="globalMotion">Whether global motion is permitted.</param>
    /// <param name="warpedMotion">Whether local warped motion is permitted.</param>
    /// <returns>Success.</returns>
    AvifResultCode SetMotionTools(bool globalMotion, bool warpedMotion);

    /// <summary>Reports whether global motion is permitted for this frame.</summary>
    /// <returns>The requested setting.</returns>
    bool IsGlobalMotionEnabled();

    /// <summary>Reports whether warped motion is permitted for this frame.</summary>
    /// <returns>The requested setting.</returns>
    bool IsWarpedMotionEnabled();

    /// <summary>Signals delayed random access through an <c>av1f</c> sample group.</summary>
    /// <param name="distance">The number of intervening samples before the recovery sample.</param>
    /// <returns>Success or InvalidArgument.</returns>
    AvifResultCode SetForwardKeyFrameDistance(int distance);

    /// <summary>Returns the <c>av1f</c> forward recovery distance.</summary>
    /// <returns>The distance, or minus one when not signaled.</returns>
    int GetForwardKeyFrameDistance();

    /// <summary>Sets <c>av1m</c> multiple-frame temporal-unit signaling.</summary>
    /// <param name="value">Whether the sample carries multiple frames.</param>
    /// <returns>Success.</returns>
    AvifResultCode SetMultipleFrames(bool value);

    /// <summary>Reports whether <c>av1m</c> signaling is requested.</summary>
    /// <returns><c>true</c> when requested.</returns>
    bool HasMultipleFrames();

    /// <summary>Sets <c>av1s</c> switch-frame signaling.</summary>
    /// <param name="value">Whether the sample starts with a switch frame.</param>
    /// <returns>Success.</returns>
    AvifResultCode SetSwitchFrame(bool value);

    /// <summary>Reports whether <c>av1s</c> signaling is requested.</summary>
    /// <returns><c>true</c> when requested.</returns>
    bool IsSwitchFrame();

    /// <summary>Adds one distinct <c>av1M</c> grouping parameter.</summary>
    /// <param name="parameter">The metadata type in the high byte and type-specific low bits.</param>
    /// <returns>Success or InvalidArgument for a duplicate.</returns>
    AvifResultCode AddMetadataGroup(uint parameter);

    /// <summary>Returns the number of <c>av1M</c> grouping parameters.</summary>
    /// <returns>The metadata group count.</returns>
    int GetMetadataGroupCount();

    /// <summary>Returns one <c>av1M</c> grouping parameter.</summary>
    /// <param name="index">The zero-based group index.</param>
    /// <returns>The parameter, or zero for an invalid index.</returns>
    uint GetMetadataGroupParameter(int index);

    /// <summary>Adds one opaque metadata OBU to this sequence sample.</summary>
    /// <param name="metadata">The metadata value to snapshot.</param>
    /// <returns>Success or InvalidArgument.</returns>
    AvifResultCode AddMetadataObu(AvifMetadataObu metadata);

    /// <summary>Returns the number of opaque metadata OBUs attached to this sample.</summary>
    /// <returns>The metadata OBU count.</returns>
    int GetMetadataObuCount();

    /// <summary>Returns one independently owned opaque metadata OBU value.</summary>
    /// <param name="index">The zero-based metadata OBU index.</param>
    /// <returns>The metadata value, or null for an invalid index.</returns>
    AvifMetadataObu? GetMetadataObu(int index);
}
