using System.Collections.Generic;

namespace avif;

/// <summary>Exposes validated track, media, handler, video-header, and edit-list metadata.</summary>
public class AvifTrack : IAvifTrack
{
    uint trackId;
    int flags;
    ulong creationTime;
    ulong modificationTime;
    ulong duration;
    uint movieTimescale;
    int width;
    int height;
    int layer;
    int alternateGroup;
    double volume;
    ulong mediaCreationTime;
    ulong mediaModificationTime;
    uint mediaTimescale;
    ulong mediaDuration;
    string? language;
    string handlerType;
    string handlerName;
    int videoGraphicsMode;
    int videoOpColorRed;
    int videoOpColorGreen;
    int videoOpColorBlue;
    bool hasVideoMediaHeader;
    bool repeatingEdits;
    bool repetitionKnown;
    bool repetitionInfinite;
    ulong repetitionCount;
    List<AvifTrackEdit> edits;
    List<AvifSampleDescription> sampleDescriptions;
    List<AvifSample> samples;
    List<string> referenceTypes;
    List<uint> referencedTrackIds;
    string auxiliaryType;

    internal AvifTrack(ParsedMovieTrack parsed, uint requestedMovieTimescale)
    {
        trackId = parsed.TrackId;
        flags = parsed.Flags;
        creationTime = parsed.CreationTime;
        modificationTime = parsed.ModificationTime;
        duration = parsed.Duration;
        movieTimescale = requestedMovieTimescale;
        width = parsed.Width;
        height = parsed.Height;
        layer = parsed.Layer;
        alternateGroup = parsed.AlternateGroup;
        volume = parsed.Volume;
        mediaCreationTime = parsed.MediaCreationTime;
        mediaModificationTime = parsed.MediaModificationTime;
        mediaTimescale = parsed.MediaTimescale;
        mediaDuration = parsed.MediaDuration;
        language = parsed.Language;
        handlerType = parsed.HandlerType;
        handlerName = parsed.HandlerName;
        videoGraphicsMode = parsed.VideoGraphicsMode;
        videoOpColorRed = parsed.VideoOpColorRed;
        videoOpColorGreen = parsed.VideoOpColorGreen;
        videoOpColorBlue = parsed.VideoOpColorBlue;
        hasVideoMediaHeader = parsed.HasVideoMediaHeader;
        repeatingEdits = parsed.RepeatingEdits;
        repetitionKnown = parsed.RepetitionKnown;
        repetitionInfinite = parsed.RepetitionInfinite;
        repetitionCount = parsed.RepetitionCount;
        edits = new List<AvifTrackEdit>();
        sampleDescriptions = new List<AvifSampleDescription>();
        samples = new List<AvifSample>();
        referenceTypes = new List<string>();
        referencedTrackIds = new List<uint>();
        auxiliaryType = string.Empty;
        int index = 0;
        while (index < parsed.Edits.Count)
        {
            ParsedTrackEdit edit = parsed.Edits[index];
            edits.Add(new AvifTrackEdit(
                edit.SegmentDuration,
                movieTimescale,
                edit.MediaTime,
                edit.MediaRateInteger,
                edit.MediaRateFraction));
            index++;
        }
        index = 0;
        while (index < parsed.SampleTable.Descriptions.Count)
        {
            AvifSampleDescription description = new AvifSampleDescription(
                parsed.SampleTable.Descriptions[index]);
            sampleDescriptions.Add(description);
            if (auxiliaryType.Length == 0
                && description.GetAuxiliaryType().Length != 0)
            {
                auxiliaryType = description.GetAuxiliaryType();
            }
            index++;
        }
        index = 0;
        while (index < parsed.SampleTable.Samples.Count)
        {
            samples.Add(new AvifSample(parsed.SampleTable.Samples[index]));
            index++;
        }
        index = 0;
        while (index < parsed.References.Count)
        {
            referenceTypes.Add(parsed.References[index].Type);
            referencedTrackIds.Add(parsed.References[index].TrackId);
            index++;
        }
    }

    /// <summary>Returns the nonzero track identifier.</summary>
    /// <returns>The track identifier.</returns>
    public uint GetTrackId()
    {
        return trackId;
    }

    /// <summary>Reports whether the track header marks the track enabled.</summary>
    /// <returns><c>true</c> when the enabled flag is present.</returns>
    public bool IsEnabled()
    {
        return (flags & 1) != 0;
    }

    /// <summary>Reports whether the track participates in the presentation.</summary>
    /// <returns><c>true</c> when the in-movie flag is present.</returns>
    public bool IsInMovie()
    {
        return (flags & 2) != 0;
    }

    /// <summary>Reports whether the track participates in previews.</summary>
    /// <returns><c>true</c> when the preview flag is present.</returns>
    public bool IsInPreview()
    {
        return (flags & 4) != 0;
    }

    /// <summary>Returns the track creation time in seconds since the ISO epoch.</summary>
    /// <returns>The unsigned creation time.</returns>
    public ulong GetCreationTime()
    {
        return creationTime;
    }

    /// <summary>Returns the track modification time in seconds since the ISO epoch.</summary>
    /// <returns>The unsigned modification time.</returns>
    public ulong GetModificationTime()
    {
        return modificationTime;
    }

    /// <summary>Returns the presentation duration in movie-timescale units.</summary>
    /// <returns>The duration, or the maximum unsigned value when indefinite.</returns>
    public ulong GetDuration()
    {
        return duration;
    }

    /// <summary>Reports whether the track duration is indefinite.</summary>
    /// <returns><c>true</c> for an all-ones duration.</returns>
    public bool IsDurationIndefinite()
    {
        return duration == ulong.MaxValue;
    }

    /// <summary>Returns the presentation duration in seconds.</summary>
    /// <returns>The duration or positive infinity when indefinite.</returns>
    public double GetDurationSeconds()
    {
        if (IsDurationIndefinite())
        {
            return double.PositiveInfinity;
        }
        return movieTimescale == 0 ? 0 : duration / (double)movieTimescale;
    }

    /// <summary>Returns the integral visual width from the track header.</summary>
    /// <returns>The width in pixels.</returns>
    public int GetWidth()
    {
        return width;
    }

    /// <summary>Returns the integral visual height from the track header.</summary>
    /// <returns>The height in pixels.</returns>
    public int GetHeight()
    {
        return height;
    }

    /// <summary>Returns the signed composition layer.</summary>
    /// <returns>The layer value.</returns>
    public int GetLayer()
    {
        return layer;
    }

    /// <summary>Returns the alternate-group identifier.</summary>
    /// <returns>The signed alternate group.</returns>
    public int GetAlternateGroup()
    {
        return alternateGroup;
    }

    /// <summary>Reports whether this track carries visual image samples.</summary>
    /// <returns><c>true</c> for image-sequence, auxiliary-visual, or video handlers.</returns>
    public bool IsVisual()
    {
        return handlerType == "pict"
            || handlerType == "auxv"
            || handlerType == "vide";
    }

    /// <summary>Reports whether this track references a master as auxiliary visual data.</summary>
    /// <returns><c>true</c> when an <c>auxl</c> track reference exists.</returns>
    public bool IsAuxiliary()
    {
        return GetFirstReferencedTrackId("auxl") != 0;
    }

    /// <summary>Returns the master track identifier for an auxiliary track.</summary>
    /// <returns>The master identifier, or zero when this is not an auxiliary track.</returns>
    public uint GetAuxiliaryMasterTrackId()
    {
        return GetFirstReferencedTrackId("auxl");
    }

    /// <summary>Reports whether this track is auxiliary to a specified master track.</summary>
    /// <param name="masterTrackId">The candidate master track identifier.</param>
    /// <returns><c>true</c> when an <c>auxl</c> reference targets the master.</returns>
    public bool IsAuxiliaryFor(uint masterTrackId)
    {
        if (masterTrackId == 0)
        {
            return false;
        }
        int index = 0;
        while (index < referenceTypes.Count)
        {
            if (referenceTypes[index] == "auxl"
                && referencedTrackIds[index] == masterTrackId)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    /// <summary>Returns the exact auxiliary type declared by this track.</summary>
    /// <returns>The auxiliary type URI, or an empty string when absent.</returns>
    public string GetAuxiliaryType()
    {
        return auxiliaryType;
    }

    /// <summary>Returns the interpreted auxiliary-track purpose.</summary>
    /// <returns>The known alpha, depth, other, or non-auxiliary kind.</returns>
    public AvifAuxiliaryKind GetAuxiliaryKind()
    {
        if (!IsAuxiliary())
        {
            return AvifAuxiliaryKind.None;
        }
        if (auxiliaryType == "urn:mpeg:mpegB:cicp:systems:auxiliary:alpha")
        {
            return AvifAuxiliaryKind.Alpha;
        }
        if (auxiliaryType == "urn:mpeg:mpegB:cicp:systems:auxiliary:depth")
        {
            return AvifAuxiliaryKind.Depth;
        }
        return AvifAuxiliaryKind.Other;
    }

    /// <summary>Returns the number of parsed track-reference targets.</summary>
    /// <returns>The reference count.</returns>
    public int GetReferenceCount()
    {
        return referenceTypes.Count;
    }

    /// <summary>Returns one track-reference type.</summary>
    /// <param name="index">The zero-based reference index.</param>
    /// <returns>The four-character reference type, or an empty string.</returns>
    public string GetReferenceType(int index)
    {
        if (index < 0 || index >= referenceTypes.Count)
        {
            return string.Empty;
        }
        return referenceTypes[index];
    }

    /// <summary>Returns one referenced track identifier.</summary>
    /// <param name="index">The zero-based reference index.</param>
    /// <returns>The track identifier, or zero for an invalid index.</returns>
    public uint GetReferencedTrackId(int index)
    {
        if (index < 0 || index >= referencedTrackIds.Count)
        {
            return 0;
        }
        return referencedTrackIds[index];
    }

    uint GetFirstReferencedTrackId(string type)
    {
        int index = 0;
        while (index < referenceTypes.Count)
        {
            if (referenceTypes[index] == type)
            {
                return referencedTrackIds[index];
            }
            index++;
        }
        return 0;
    }

    /// <summary>Returns the track volume as an 8.8 fixed-point value.</summary>
    /// <returns>The decoded volume.</returns>
    public double GetVolume()
    {
        return volume;
    }

    /// <summary>Returns the media creation time in seconds since the ISO epoch.</summary>
    /// <returns>The unsigned creation time.</returns>
    public ulong GetMediaCreationTime()
    {
        return mediaCreationTime;
    }

    /// <summary>Returns the media modification time in seconds since the ISO epoch.</summary>
    /// <returns>The unsigned modification time.</returns>
    public ulong GetMediaModificationTime()
    {
        return mediaModificationTime;
    }

    /// <summary>Returns the number of media time units per second.</summary>
    /// <returns>The positive media timescale.</returns>
    public uint GetMediaTimescale()
    {
        return mediaTimescale;
    }

    /// <summary>Returns the media duration in media-timescale units.</summary>
    /// <returns>The duration, or the maximum unsigned value when indefinite.</returns>
    public ulong GetMediaDuration()
    {
        return mediaDuration;
    }

    /// <summary>Reports whether the media duration is indefinite.</summary>
    /// <returns><c>true</c> for an all-ones duration.</returns>
    public bool IsMediaDurationIndefinite()
    {
        return mediaDuration == ulong.MaxValue;
    }

    /// <summary>Returns the media duration in seconds.</summary>
    /// <returns>The duration or positive infinity when indefinite.</returns>
    public double GetMediaDurationSeconds()
    {
        if (IsMediaDurationIndefinite())
        {
            return double.PositiveInfinity;
        }
        return mediaTimescale == 0 ? 0 : mediaDuration / (double)mediaTimescale;
    }

    /// <summary>Returns the decoded three-letter media language.</summary>
    /// <returns>The lower-case ISO-639-2/T code or an empty string.</returns>
    public string? GetLanguage()
    {
        return language;
    }

    /// <summary>Returns the media handler type.</summary>
    /// <returns>The four-character handler type.</returns>
    public string GetHandlerType()
    {
        return handlerType;
    }

    /// <summary>Returns the media handler name.</summary>
    /// <returns>The UTF-8 handler name.</returns>
    public string GetHandlerName()
    {
        return handlerName;
    }

    /// <summary>Reports whether a validated video media header is present.</summary>
    /// <returns><c>true</c> when a <c>vmhd</c> box was parsed.</returns>
    public bool HasVideoMediaHeader()
    {
        return hasVideoMediaHeader;
    }

    /// <summary>Returns the video composition graphics mode.</summary>
    /// <returns>The unsigned graphics-mode value.</returns>
    public int GetVideoGraphicsMode()
    {
        return videoGraphicsMode;
    }

    /// <summary>Returns one video composition operation-color component.</summary>
    /// <param name="index">Zero for red, one for green, or two for blue.</param>
    /// <returns>The unsigned component, or zero for an invalid index.</returns>
    public int GetVideoOpColor(int index)
    {
        if (index == 0)
        {
            return videoOpColorRed;
        }
        if (index == 1)
        {
            return videoOpColorGreen;
        }
        return index == 2 ? videoOpColorBlue : 0;
    }

    /// <summary>Returns the number of parsed edit-list entries.</summary>
    /// <returns>The entry count.</returns>
    public int GetEditCount()
    {
        return edits.Count;
    }

    /// <summary>Returns one edit-list entry.</summary>
    /// <param name="index">The zero-based edit index.</param>
    /// <returns>The edit, or null for an invalid index.</returns>
    public AvifTrackEdit? GetEdit(int index)
    {
        if (index < 0 || index >= edits.Count)
        {
            return null;
        }
        return edits[index];
    }

    /// <summary>Reports whether the edit list is repeated to fill the track duration.</summary>
    /// <returns><c>true</c> when edit-list flag one is set.</returns>
    public bool HasRepeatingEditList()
    {
        return repeatingEdits;
    }

    /// <summary>Reports whether the container specifies a repeat count.</summary>
    /// <returns><c>true</c> when an edit list defines finite or infinite playback.</returns>
    public bool HasKnownRepetitionCount()
    {
        return repetitionKnown;
    }

    /// <summary>Reports whether edit repetition is unbounded.</summary>
    /// <returns><c>true</c> when a repeated edit has indefinite track duration.</returns>
    public bool IsRepetitionInfinite()
    {
        return repetitionInfinite;
    }

    /// <summary>Returns the number of repetitions after the first edit-list presentation.</summary>
    /// <returns>The zero-based repetition count; zero when not repeated or infinite.</returns>
    public ulong GetRepetitionCount()
    {
        return repetitionCount;
    }

    /// <summary>Returns the number of parsed sample descriptions.</summary>
    /// <returns>The sample-description count.</returns>
    public int GetSampleDescriptionCount()
    {
        return sampleDescriptions.Count;
    }

    /// <summary>Returns one parsed AV1 sample description.</summary>
    /// <param name="index">The zero-based description index.</param>
    /// <returns>The description, or null for an invalid index.</returns>
    public AvifSampleDescription? GetSampleDescription(int index)
    {
        if (index < 0 || index >= sampleDescriptions.Count)
        {
            return null;
        }
        return sampleDescriptions[index];
    }

    /// <summary>Returns the number of expanded media samples.</summary>
    /// <returns>The sample count.</returns>
    public int GetSampleCount()
    {
        return samples.Count;
    }

    /// <summary>Returns one expanded media sample.</summary>
    /// <param name="index">The zero-based sample index.</param>
    /// <returns>The sample, or null for an invalid index.</returns>
    public AvifSample? GetSample(int index)
    {
        if (index < 0 || index >= samples.Count)
        {
            return null;
        }
        return samples[index];
    }
}
