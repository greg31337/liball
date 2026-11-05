using System;

namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifImageSource"/>.</summary>
public interface IAvifImageSource
{
    /// <summary>Returns the source kind selected after resolving automatic selection.</summary>
    /// <returns>The primary-item or image-sequence selection.</returns>
    AvifSourceSelection GetSelection();

    /// <summary>Returns the selected primary image item.</summary>
    /// <returns>The item, or null when a timed sequence was selected.</returns>
    AvifImageItem? GetPrimaryItem();

    /// <summary>Returns the selected timed image-sequence track.</summary>
    /// <returns>The track, or null when a primary item was selected.</returns>
    AvifTrack? GetSequenceTrack();

    /// <summary>Returns the number of available non-auxiliary visual tracks.</summary>
    /// <returns>The track count, or zero for a primary-item source.</returns>
    int GetVisualTrackCount();

    /// <summary>Returns one available non-auxiliary visual track.</summary>
    /// <param name="index">The zero-based visual-track index.</param>
    /// <returns>The track, or null for a primary-item source or invalid index.</returns>
    AvifTrack? GetVisualTrack(int index);

    /// <summary>Returns the number of alternatives to the selected sequence track.</summary>
    /// <returns>The alternate-track count.</returns>
    int GetAlternateTrackCount();

    /// <summary>Returns one alternative to the selected sequence track.</summary>
    /// <param name="index">The zero-based alternate index.</param>
    /// <returns>The alternate track, or null when unavailable.</returns>
    AvifTrack? GetAlternateTrack(int index);

    /// <summary>Returns the number of auxiliary tracks associated with the selected sequence.</summary>
    /// <returns>The associated auxiliary-track count.</returns>
    int GetAuxiliaryTrackCount();

    /// <summary>Returns one auxiliary track associated with the selected sequence.</summary>
    /// <param name="index">The zero-based auxiliary-track index.</param>
    /// <returns>The auxiliary track, or null when unavailable.</returns>
    AvifTrack? GetAuxiliaryTrack(int index);

    /// <summary>Returns the number of frames in the selected timed sequence.</summary>
    /// <returns>The color-track sample count, or zero for a primary-item source.</returns>
    int GetFrameCount();

    /// <summary>Reports whether every associated track can begin decoding at a frame.</summary>
    /// <param name="frameIndex">The zero-based frame index.</param>
    /// <returns><c>true</c> when the color and all auxiliary samples are synchronization samples.</returns>
    bool IsFrameKeyFrame(int frameIndex);

    /// <summary>Returns the nearest synchronization frame at or before a frame.</summary>
    /// <param name="frameIndex">The zero-based frame index.</param>
    /// <returns>The nearest keyframe index, or minus one for an invalid index.</returns>
    int GetNearestKeyFrameIndex(int frameIndex);

    /// <summary>Returns timing for one sequence frame.</summary>
    /// <param name="frameIndex">The zero-based frame index.</param>
    /// <returns>The frame timing, or null for a primary item or invalid index.</returns>
    AvifFrameTiming? GetFrameTiming(int frameIndex);

    /// <summary>Returns the maximal encoded range containing one frame on all associated tracks.</summary>
    /// <param name="frameIndex">The zero-based frame index.</param>
    /// <returns>The encompassing range, or null for a primary item or invalid index.</returns>
    AvifByteRange? GetFrameEncodedRange(int frameIndex);

    /// <summary>Returns the maximal encoded range needed to decode one frame from its keyframe.</summary>
    /// <param name="frameIndex">The zero-based frame index.</param>
    /// <returns>The encompassing dependency range, or null for an invalid index.</returns>
    AvifByteRange? GetFrameDecodeRange(int frameIndex);

    /// <summary>Creates an independent stateful decode cursor for this timed source.</summary>
    /// <returns>The sequence cursor, or null for a primary-item source.</returns>
    AvifSequence? CreateSequence();
}
