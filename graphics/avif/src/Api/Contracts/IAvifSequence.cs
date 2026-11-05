using System;
using System.Collections.Generic;

namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifSequence"/>.</summary>
public interface IAvifSequence
{
    /// <summary>Adds one validated frame to an authoring sequence.</summary>
    /// <param name="frame">The frame and timed sample signaling to retain.</param>
    /// <returns>Success, InvalidArgument, InvalidState, or NonConformingData.</returns>
    AvifResultCode AddFrame(AvifFrame frame);

    /// <summary>Returns one authoring frame.</summary>
    /// <param name="index">The zero-based frame index.</param>
    /// <returns>The frame, or null for a decoded sequence or invalid index.</returns>
    AvifFrame? GetFrame(int index);

    /// <summary>Plans coded order, show-existing reuse, and reference refresh without encoding.</summary>
    /// <param name="options">The AV1 authoring controls.</param>
    /// <returns>An independently owned deterministic plan.</returns>
    AvifSequenceEncodingPlan PlanEncoding(Av1EncoderOptions options);

    /// <summary>Sets the finite number of repetitions after the first presentation.</summary>
    /// <param name="count">The zero-based repetition count.</param>
    /// <returns>Success or InvalidState for a decoded sequence.</returns>
    AvifResultCode SetRepetitionCount(ulong count);

    /// <summary>Sets unbounded sequence repetition.</summary>
    /// <returns>Success or InvalidState for a decoded sequence.</returns>
    AvifResultCode SetInfiniteRepetition();

    /// <summary>Returns the authoring media timescale.</summary>
    /// <returns>The positive timescale, or zero for a decoded sequence cursor.</returns>
    uint GetTimescale();

    /// <summary>Returns the number of frames in one sequence iteration.</summary>
    /// <returns>The frame count.</returns>
    int GetFrameCount();

    /// <summary>Returns the current decoded frame index.</summary>
    /// <returns>The zero-based index, or minus one before the first frame.</returns>
    int GetCurrentFrameIndex();

    /// <summary>Returns the zero-based playback iteration.</summary>
    /// <returns>Zero for the first presentation, one for the first repetition, and so on.</returns>
    ulong GetIterationIndex();

    /// <summary>Advances decode state to the next frame, honoring declared repetition.</summary>
    /// <returns>Success, EndOfData, or a stable argument/state result.</returns>
    AvifResultCode DecodeNextFrame();

    /// <summary>Moves decode state to an indexed frame from the nearest required keyframe.</summary>
    /// <param name="frameIndex">The zero-based target frame index.</param>
    /// <returns>Success or NotFound for an invalid frame index.</returns>
    AvifResultCode DecodeFrame(int frameIndex);

    /// <summary>Returns the first frame scheduled by the most recent decode operation.</summary>
    /// <returns>The zero-based start index, or minus one when no frames were scheduled.</returns>
    int GetLastDecodeStartIndex();

    /// <summary>Returns the number of frames scheduled by the most recent decode operation.</summary>
    /// <returns>The scheduled frame count.</returns>
    int GetLastDecodeFrameCount();

    /// <summary>Returns timing for the current frame.</summary>
    /// <returns>The timing, or null before the first successful decode.</returns>
    AvifFrameTiming? GetCurrentFrameTiming();

    /// <summary>Returns the encoded range needed for the current indexed decode.</summary>
    /// <returns>The dependency range, or null before the first successful decode.</returns>
    AvifByteRange? GetCurrentFrameDecodeRange();

    /// <summary>Reports whether the sequence declares a finite or infinite repetition count.</summary>
    /// <returns><c>true</c> when edit-list repetition semantics are known.</returns>
    bool HasKnownRepetitionCount();

    /// <summary>Reports whether sequence repetition is unbounded.</summary>
    /// <returns><c>true</c> for an infinite repeated edit list.</returns>
    bool IsRepetitionInfinite();

    /// <summary>Returns the finite number of repetitions after the first presentation.</summary>
    /// <returns>The repetition count, or zero when unknown, absent, or infinite.</returns>
    ulong GetRepetitionCount();

    /// <summary>Resets decode position to before the first frame and first iteration.</summary>
    /// <returns>Success.</returns>
    AvifResultCode Reset();
}
