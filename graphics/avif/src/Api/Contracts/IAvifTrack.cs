using System.Collections.Generic;

namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifTrack"/>.</summary>
public interface IAvifTrack
{
    /// <summary>Returns the nonzero track identifier.</summary>
    /// <returns>The track identifier.</returns>
    uint GetTrackId();

    /// <summary>Reports whether the track header marks the track enabled.</summary>
    /// <returns><c>true</c> when the enabled flag is present.</returns>
    bool IsEnabled();

    /// <summary>Reports whether the track participates in the presentation.</summary>
    /// <returns><c>true</c> when the in-movie flag is present.</returns>
    bool IsInMovie();

    /// <summary>Reports whether the track participates in previews.</summary>
    /// <returns><c>true</c> when the preview flag is present.</returns>
    bool IsInPreview();

    /// <summary>Returns the track creation time in seconds since the ISO epoch.</summary>
    /// <returns>The unsigned creation time.</returns>
    ulong GetCreationTime();

    /// <summary>Returns the track modification time in seconds since the ISO epoch.</summary>
    /// <returns>The unsigned modification time.</returns>
    ulong GetModificationTime();

    /// <summary>Returns the presentation duration in movie-timescale units.</summary>
    /// <returns>The duration, or the maximum unsigned value when indefinite.</returns>
    ulong GetDuration();

    /// <summary>Reports whether the track duration is indefinite.</summary>
    /// <returns><c>true</c> for an all-ones duration.</returns>
    bool IsDurationIndefinite();

    /// <summary>Returns the presentation duration in seconds.</summary>
    /// <returns>The duration or positive infinity when indefinite.</returns>
    double GetDurationSeconds();

    /// <summary>Returns the integral visual width from the track header.</summary>
    /// <returns>The width in pixels.</returns>
    int GetWidth();

    /// <summary>Returns the integral visual height from the track header.</summary>
    /// <returns>The height in pixels.</returns>
    int GetHeight();

    /// <summary>Returns the signed composition layer.</summary>
    /// <returns>The layer value.</returns>
    int GetLayer();

    /// <summary>Returns the alternate-group identifier.</summary>
    /// <returns>The signed alternate group.</returns>
    int GetAlternateGroup();

    /// <summary>Reports whether this track carries visual image samples.</summary>
    /// <returns><c>true</c> for image-sequence, auxiliary-visual, or video handlers.</returns>
    bool IsVisual();

    /// <summary>Reports whether this track references a master as auxiliary visual data.</summary>
    /// <returns><c>true</c> when an <c>auxl</c> track reference exists.</returns>
    bool IsAuxiliary();

    /// <summary>Returns the master track identifier for an auxiliary track.</summary>
    /// <returns>The master identifier, or zero when this is not an auxiliary track.</returns>
    uint GetAuxiliaryMasterTrackId();

    /// <summary>Reports whether this track is auxiliary to a specified master track.</summary>
    /// <param name="masterTrackId">The candidate master track identifier.</param>
    /// <returns><c>true</c> when an <c>auxl</c> reference targets the master.</returns>
    bool IsAuxiliaryFor(uint masterTrackId);

    /// <summary>Returns the exact auxiliary type declared by this track.</summary>
    /// <returns>The auxiliary type URI, or an empty string when absent.</returns>
    string GetAuxiliaryType();

    /// <summary>Returns the interpreted auxiliary-track purpose.</summary>
    /// <returns>The known alpha, depth, other, or non-auxiliary kind.</returns>
    AvifAuxiliaryKind GetAuxiliaryKind();

    /// <summary>Returns the number of parsed track-reference targets.</summary>
    /// <returns>The reference count.</returns>
    int GetReferenceCount();

    /// <summary>Returns one track-reference type.</summary>
    /// <param name="index">The zero-based reference index.</param>
    /// <returns>The four-character reference type, or an empty string.</returns>
    string GetReferenceType(int index);

    /// <summary>Returns one referenced track identifier.</summary>
    /// <param name="index">The zero-based reference index.</param>
    /// <returns>The track identifier, or zero for an invalid index.</returns>
    uint GetReferencedTrackId(int index);

    /// <summary>Returns the track volume as an 8.8 fixed-point value.</summary>
    /// <returns>The decoded volume.</returns>
    double GetVolume();

    /// <summary>Returns the media creation time in seconds since the ISO epoch.</summary>
    /// <returns>The unsigned creation time.</returns>
    ulong GetMediaCreationTime();

    /// <summary>Returns the media modification time in seconds since the ISO epoch.</summary>
    /// <returns>The unsigned modification time.</returns>
    ulong GetMediaModificationTime();

    /// <summary>Returns the number of media time units per second.</summary>
    /// <returns>The positive media timescale.</returns>
    uint GetMediaTimescale();

    /// <summary>Returns the media duration in media-timescale units.</summary>
    /// <returns>The duration, or the maximum unsigned value when indefinite.</returns>
    ulong GetMediaDuration();

    /// <summary>Reports whether the media duration is indefinite.</summary>
    /// <returns><c>true</c> for an all-ones duration.</returns>
    bool IsMediaDurationIndefinite();

    /// <summary>Returns the media duration in seconds.</summary>
    /// <returns>The duration or positive infinity when indefinite.</returns>
    double GetMediaDurationSeconds();

    /// <summary>Returns the decoded three-letter media language.</summary>
    /// <returns>The lower-case ISO-639-2/T code or an empty string.</returns>
    string? GetLanguage();

    /// <summary>Returns the media handler type.</summary>
    /// <returns>The four-character handler type.</returns>
    string GetHandlerType();

    /// <summary>Returns the media handler name.</summary>
    /// <returns>The UTF-8 handler name.</returns>
    string GetHandlerName();

    /// <summary>Reports whether a validated video media header is present.</summary>
    /// <returns><c>true</c> when a <c>vmhd</c> box was parsed.</returns>
    bool HasVideoMediaHeader();

    /// <summary>Returns the video composition graphics mode.</summary>
    /// <returns>The unsigned graphics-mode value.</returns>
    int GetVideoGraphicsMode();

    /// <summary>Returns one video composition operation-color component.</summary>
    /// <param name="index">Zero for red, one for green, or two for blue.</param>
    /// <returns>The unsigned component, or zero for an invalid index.</returns>
    int GetVideoOpColor(int index);

    /// <summary>Returns the number of parsed edit-list entries.</summary>
    /// <returns>The entry count.</returns>
    int GetEditCount();

    /// <summary>Returns one edit-list entry.</summary>
    /// <param name="index">The zero-based edit index.</param>
    /// <returns>The edit, or null for an invalid index.</returns>
    AvifTrackEdit? GetEdit(int index);

    /// <summary>Reports whether the edit list is repeated to fill the track duration.</summary>
    /// <returns><c>true</c> when edit-list flag one is set.</returns>
    bool HasRepeatingEditList();

    /// <summary>Reports whether the container specifies a repeat count.</summary>
    /// <returns><c>true</c> when an edit list defines finite or infinite playback.</returns>
    bool HasKnownRepetitionCount();

    /// <summary>Reports whether edit repetition is unbounded.</summary>
    /// <returns><c>true</c> when a repeated edit has indefinite track duration.</returns>
    bool IsRepetitionInfinite();

    /// <summary>Returns the number of repetitions after the first edit-list presentation.</summary>
    /// <returns>The zero-based repetition count; zero when not repeated or infinite.</returns>
    ulong GetRepetitionCount();

    /// <summary>Returns the number of parsed sample descriptions.</summary>
    /// <returns>The sample-description count.</returns>
    int GetSampleDescriptionCount();

    /// <summary>Returns one parsed AV1 sample description.</summary>
    /// <param name="index">The zero-based description index.</param>
    /// <returns>The description, or null for an invalid index.</returns>
    AvifSampleDescription? GetSampleDescription(int index);

    /// <summary>Returns the number of expanded media samples.</summary>
    /// <returns>The sample count.</returns>
    int GetSampleCount();

    /// <summary>Returns one expanded media sample.</summary>
    /// <param name="index">The zero-based sample index.</param>
    /// <returns>The sample, or null for an invalid index.</returns>
    AvifSample? GetSample(int index);
}
