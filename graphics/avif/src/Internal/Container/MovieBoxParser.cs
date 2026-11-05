using System;
using System.Collections.Generic;

namespace avif;

internal class ParsedTrackEdit
{
    internal ulong SegmentDuration;
    internal long MediaTime;
    internal short MediaRateInteger;
    internal short MediaRateFraction;

    internal ParsedTrackEdit(
        ulong segmentDuration,
        long mediaTime,
        short mediaRateInteger,
        short mediaRateFraction)
    {
        SegmentDuration = segmentDuration;
        MediaTime = mediaTime;
        MediaRateInteger = mediaRateInteger;
        MediaRateFraction = mediaRateFraction;
    }
}

internal class ParsedTrackReference
{
    internal string Type;
    internal uint TrackId;

    internal ParsedTrackReference(string type, uint trackId)
    {
        Type = type;
        TrackId = trackId;
    }
}

internal class ParsedMovieTrack
{
    internal int Flags;
    internal ulong CreationTime;
    internal ulong ModificationTime;
    internal uint TrackId;
    internal ulong Duration;
    internal int Layer;
    internal int AlternateGroup;
    internal double Volume;
    internal uint WidthFixed;
    internal uint HeightFixed;
    internal int Width;
    internal int Height;
    internal ulong MediaCreationTime;
    internal ulong MediaModificationTime;
    internal uint MediaTimescale;
    internal ulong MediaDuration;
    internal string? Language;
    internal string HandlerType;
    internal string HandlerName;
    internal bool HasVideoMediaHeader;
    internal int VideoGraphicsMode;
    internal int VideoOpColorRed;
    internal int VideoOpColorGreen;
    internal int VideoOpColorBlue;
    internal bool RepeatingEdits;
    internal bool RepetitionKnown;
    internal bool RepetitionInfinite;
    internal ulong RepetitionCount;
    internal List<ParsedTrackEdit> Edits;
    internal List<ParsedTrackReference> References;
    internal ParsedSampleTable SampleTable;

    internal ParsedMovieTrack()
    {
        Language = string.Empty;
        HandlerType = string.Empty;
        HandlerName = string.Empty;
        Edits = new List<ParsedTrackEdit>();
        References = new List<ParsedTrackReference>();
        SampleTable = new ParsedSampleTable();
    }
}

internal class ParsedMovie
{
    internal ulong CreationTime;
    internal ulong ModificationTime;
    internal uint Timescale;
    internal ulong Duration;
    internal uint NextTrackId;
    internal List<ParsedMovieTrack> Tracks;

    internal ParsedMovie()
    {
        Tracks = new List<ParsedMovieTrack>();
    }
}

internal class MovieParseResult
{
    internal AvifResultCode Code;
    internal ParsedMovie? Movie;

    internal MovieParseResult(AvifResultCode code, ParsedMovie? movie)
    {
        Code = code;
        Movie = movie;
    }
}

internal class MovieChildLookup
{
    internal AvifResultCode Code;
    internal IsoBox? Box;
    internal int Count;

    internal MovieChildLookup(AvifResultCode code, IsoBox? box, int count)
    {
        Code = code;
        Box = box;
        Count = count;
    }
}

internal static class MovieBoxParser
{
    internal static MovieParseResult Parse(
        ReadOnlySpan<byte> source,
        IsoBox? movieBox,
        List<IsoBox> topLevelBoxes,
        ResourceGuard resources,
        DiagnosticLog diagnostics)
    {
        if (movieBox == null || movieBox.Type != "moov"
            || topLevelBoxes == null || resources == null || diagnostics == null)
        {
            return new MovieParseResult(AvifResultCode.InvalidArgument, null);
        }

        diagnostics.Context.PushBox("moov");
        MovieChildLookup movieHeader = FindChild(
            movieBox, "mvhd", true, diagnostics);
        if (movieHeader.Code != AvifResultCode.Success)
        {
            diagnostics.Context.PopBox();
            return new MovieParseResult(movieHeader.Code, null);
        }

        ParsedMovie movie = new ParsedMovie();
        AvifResultCode headerCode = ParseMovieHeader(
            source, movieHeader.Box, movie, diagnostics);
        if (headerCode != AvifResultCode.Success)
        {
            diagnostics.Context.PopBox();
            return new MovieParseResult(headerCode, null);
        }

        int trackCount = CountChildren(movieBox, "trak");
        if (trackCount == 0)
        {
            AvifResultCode code = Report(
                diagnostics,
                AvifResultCode.InvalidTrack,
                "The MovieBox does not contain a TrackBox.",
                movieBox.PayloadOffset);
            diagnostics.Context.PopBox();
            return new MovieParseResult(code, null);
        }
        AvifResultCode countCode = resources.CountTracks(trackCount);
        if (countCode != AvifResultCode.Success)
        {
            AvifResultCode code = Report(
                diagnostics,
                countCode,
                "The movie track count exceeds its configured limit.",
                movieBox.PayloadOffset);
            diagnostics.Context.PopBox();
            return new MovieParseResult(code, null);
        }

        int childIndex = 0;
        while (childIndex < movieBox.Children.Count)
        {
            IsoBox child = movieBox.Children[childIndex];
            if (child.Type == "trak")
            {
                ParsedMovieTrack track = new ParsedMovieTrack();
                AvifResultCode trackCode = ParseTrack(
                    source,
                    child,
                    movie.Timescale,
                    topLevelBoxes,
                    resources,
                    diagnostics,
                    track);
                if (trackCode != AvifResultCode.Success)
                {
                    diagnostics.Context.PopBox();
                    return new MovieParseResult(trackCode, null);
                }
                if (FindTrack(movie, track.TrackId) != null)
                {
                    diagnostics.Context.PushBox("trak");
                    diagnostics.Context.PushTrack(track.TrackId);
                    AvifResultCode code = Report(
                        diagnostics,
                        AvifResultCode.InvalidTrack,
                        "Track identifiers must be unique within a movie.",
                        child.PayloadOffset);
                    diagnostics.Context.PopTrack();
                    diagnostics.Context.PopBox();
                    diagnostics.Context.PopBox();
                    return new MovieParseResult(code, null);
                }
                movie.Tracks.Add(track);
            }
            childIndex++;
        }

        uint maximumTrackId = 0;
        int trackIndex = 0;
        while (trackIndex < movie.Tracks.Count)
        {
            maximumTrackId = System.Math.Max(
                maximumTrackId, movie.Tracks[trackIndex].TrackId);
            trackIndex++;
        }
        if (movie.NextTrackId <= maximumTrackId)
        {
            AvifResultCode code = Report(
                diagnostics,
                AvifResultCode.InvalidTrack,
                "The movie next track identifier must exceed every existing track identifier.",
                (movieHeader.Box ?? throw new InvalidOperationException("Invalid internal state.")).PayloadOffset);
            diagnostics.Context.PopBox();
            return new MovieParseResult(code, null);
        }

        AvifResultCode referenceCode = ValidateTrackReferences(
            movie, diagnostics, movieBox.PayloadOffset);
        if (referenceCode != AvifResultCode.Success)
        {
            diagnostics.Context.PopBox();
            return new MovieParseResult(referenceCode, null);
        }

        diagnostics.Context.PopBox();
        return new MovieParseResult(AvifResultCode.Success, movie);
    }

    static AvifResultCode ParseMovieHeader(
        ReadOnlySpan<byte> source,
        IsoBox? box,
        ParsedMovie movie,
        DiagnosticLog diagnostics)
    {
        diagnostics.Context.PushBox("mvhd");
        IsoFullBox fullBox = IsoFullBox.Parse(source, box);
        if (fullBox.Code != AvifResultCode.Success)
        {
            AvifResultCode code = Report(
                diagnostics,
                Normalize(fullBox.Code),
                "The MovieHeaderBox FullBox fields are invalid.",
                (box ?? throw new InvalidOperationException("Invalid internal state.")).PayloadOffset);
            diagnostics.Context.PopBox();
            return code;
        }
        if ((fullBox.Version != 0 && fullBox.Version != 1)
            || fullBox.Flags != 0)
        {
            AvifResultCode code = Report(
                diagnostics,
                fullBox.Version > 1
                    ? AvifResultCode.UnsupportedFeature
                    : AvifResultCode.InvalidTrack,
                "The MovieHeaderBox version or flags are not supported.",
                (box ?? throw new InvalidOperationException("Invalid internal state.")).PayloadOffset);
            diagnostics.Context.PopBox();
            return code;
        }
        int expectedSize = fullBox.Version == 0 ? 96 : 108;
        ReadOnlySpan<byte> payload = GetPayload(
            source, fullBox, expectedSize, diagnostics, box,
            "The MovieHeaderBox has an invalid payload size.");
        if (payload.Length == 0)
        {
            diagnostics.Context.PopBox();
            return diagnostics.Code;
        }

        ByteReader reader = new ByteReader(payload.Length);
        if (fullBox.Version == 0)
        {
            movie.CreationTime = reader.ReadUInt32BigEndian(payload);
            movie.ModificationTime = reader.ReadUInt32BigEndian(payload);
            movie.Timescale = reader.ReadUInt32BigEndian(payload);
            uint duration = reader.ReadUInt32BigEndian(payload);
            movie.Duration = duration == uint.MaxValue
                ? ulong.MaxValue : duration;
        }
        else
        {
            movie.CreationTime = reader.ReadUInt64BigEndian(payload);
            movie.ModificationTime = reader.ReadUInt64BigEndian(payload);
            movie.Timescale = reader.ReadUInt32BigEndian(payload);
            movie.Duration = reader.ReadUInt64BigEndian(payload);
        }
        reader.ReadUInt32BigEndian(payload);
        reader.ReadUInt16BigEndian(payload);
        ushort reserved16 = reader.ReadUInt16BigEndian(payload);
        uint reserved0 = reader.ReadUInt32BigEndian(payload);
        uint reserved1 = reader.ReadUInt32BigEndian(payload);
        int matrixIndex = 0;
        while (matrixIndex < 9)
        {
            reader.ReadUInt32BigEndian(payload);
            matrixIndex++;
        }
        int predefinedIndex = 0;
        bool predefinedZero = true;
        while (predefinedIndex < 6)
        {
            if (reader.ReadUInt32BigEndian(payload) != 0)
            {
                predefinedZero = false;
            }
            predefinedIndex++;
        }
        movie.NextTrackId = reader.ReadUInt32BigEndian(payload);
        if (reader.Code != AvifResultCode.Success
            || movie.Timescale == 0
            || movie.NextTrackId == 0
            || reserved16 != 0 || reserved0 != 0 || reserved1 != 0
            || !predefinedZero)
        {
            AvifResultCode code = Report(
                diagnostics,
                AvifResultCode.InvalidTrack,
                "The MovieHeaderBox contains invalid timing or reserved fields.",
                fullBox.PayloadOffset);
            diagnostics.Context.PopBox();
            return code;
        }
        diagnostics.Context.PopBox();
        return AvifResultCode.Success;
    }

    static AvifResultCode ParseTrack(
        ReadOnlySpan<byte> source,
        IsoBox trackBox,
        uint movieTimescale,
        List<IsoBox> topLevelBoxes,
        ResourceGuard resources,
        DiagnosticLog diagnostics,
        ParsedMovieTrack track)
    {
        diagnostics.Context.PushBox("trak");
        MovieChildLookup trackHeader = FindChild(
            trackBox, "tkhd", true, diagnostics);
        MovieChildLookup media = FindChild(
            trackBox, "mdia", true, diagnostics);
        MovieChildLookup edit = FindChild(
            trackBox, "edts", false, diagnostics);
        MovieChildLookup references = FindChild(
            trackBox, "tref", false, diagnostics);
        if (trackHeader.Code != AvifResultCode.Success
            || media.Code != AvifResultCode.Success
            || edit.Code != AvifResultCode.Success
            || references.Code != AvifResultCode.Success)
        {
            AvifResultCode code = trackHeader.Code != AvifResultCode.Success
                ? trackHeader.Code
                : media.Code != AvifResultCode.Success
                    ? media.Code
                    : edit.Code != AvifResultCode.Success
                        ? edit.Code : references.Code;
            diagnostics.Context.PopBox();
            return code;
        }

        AvifResultCode headerCode = ParseTrackHeader(
            source, trackHeader.Box, track, diagnostics);
        if (headerCode != AvifResultCode.Success)
        {
            diagnostics.Context.PopBox();
            return headerCode;
        }
        diagnostics.Context.PushTrack(track.TrackId);
        AvifResultCode mediaCode = ParseMedia(
            source, media.Box, topLevelBoxes, resources, diagnostics, track);
        if (mediaCode == AvifResultCode.Success && edit.Box != null)
        {
            mediaCode = ParseEdit(
                source,
                edit.Box,
                movieTimescale,
                resources,
                diagnostics,
                track);
            if (mediaCode == AvifResultCode.Success)
            {
                track.RepetitionKnown = true;
            }
        }
        if (mediaCode == AvifResultCode.Success && references.Box != null)
        {
            mediaCode = ParseTrackReferences(
                source, references.Box, resources, diagnostics, track);
        }
        if (mediaCode == AvifResultCode.Success)
        {
            mediaCode = ValidateTrack(resources, diagnostics, track);
        }
        diagnostics.Context.PopTrack();
        diagnostics.Context.PopBox();
        return mediaCode;
    }

    static AvifResultCode ParseTrackHeader(
        ReadOnlySpan<byte> source,
        IsoBox? box,
        ParsedMovieTrack track,
        DiagnosticLog diagnostics)
    {
        diagnostics.Context.PushBox("tkhd");
        IsoFullBox fullBox = IsoFullBox.Parse(source, box);
        if (fullBox.Code != AvifResultCode.Success)
        {
            AvifResultCode code = Report(
                diagnostics, Normalize(fullBox.Code),
                "The TrackHeaderBox FullBox fields are invalid.",
                (box ?? throw new InvalidOperationException("Invalid internal state.")).PayloadOffset);
            diagnostics.Context.PopBox();
            return code;
        }
        if ((fullBox.Version != 0 && fullBox.Version != 1)
            || (fullBox.Flags & -16) != 0)
        {
            AvifResultCode code = Report(
                diagnostics,
                fullBox.Version > 1
                    ? AvifResultCode.UnsupportedFeature
                    : AvifResultCode.InvalidTrack,
                "The TrackHeaderBox version or flags are invalid.",
                (box ?? throw new InvalidOperationException("Invalid internal state.")).PayloadOffset);
            diagnostics.Context.PopBox();
            return code;
        }
        int expectedSize = fullBox.Version == 0 ? 80 : 92;
        ReadOnlySpan<byte> payload = GetPayload(
            source, fullBox, expectedSize, diagnostics, box,
            "The TrackHeaderBox has an invalid payload size.");
        if (payload.Length == 0)
        {
            diagnostics.Context.PopBox();
            return diagnostics.Code;
        }

        ByteReader reader = new ByteReader(payload.Length);
        track.Flags = fullBox.Flags;
        uint reservedAfterId;
        if (fullBox.Version == 0)
        {
            track.CreationTime = reader.ReadUInt32BigEndian(payload);
            track.ModificationTime = reader.ReadUInt32BigEndian(payload);
            track.TrackId = reader.ReadUInt32BigEndian(payload);
            reservedAfterId = reader.ReadUInt32BigEndian(payload);
            uint duration = reader.ReadUInt32BigEndian(payload);
            track.Duration = duration == uint.MaxValue
                ? ulong.MaxValue : duration;
        }
        else
        {
            track.CreationTime = reader.ReadUInt64BigEndian(payload);
            track.ModificationTime = reader.ReadUInt64BigEndian(payload);
            track.TrackId = reader.ReadUInt32BigEndian(payload);
            reservedAfterId = reader.ReadUInt32BigEndian(payload);
            track.Duration = reader.ReadUInt64BigEndian(payload);
        }
        uint reserved0 = reader.ReadUInt32BigEndian(payload);
        uint reserved1 = reader.ReadUInt32BigEndian(payload);
        track.Layer = reader.ReadInt16BigEndian(payload);
        track.AlternateGroup = reader.ReadInt16BigEndian(payload);
        track.Volume = reader.ReadFixed8_8BigEndian(payload);
        ushort reserved16 = reader.ReadUInt16BigEndian(payload);
        int matrixIndex = 0;
        while (matrixIndex < 9)
        {
            reader.ReadUInt32BigEndian(payload);
            matrixIndex++;
        }
        track.WidthFixed = reader.ReadUInt32BigEndian(payload);
        track.HeightFixed = reader.ReadUInt32BigEndian(payload);
        track.Width = (int)(track.WidthFixed >> 16);
        track.Height = (int)(track.HeightFixed >> 16);
        if (reader.Code != AvifResultCode.Success
            || track.TrackId == 0
            || reservedAfterId != 0 || reserved0 != 0 || reserved1 != 0
            || reserved16 != 0)
        {
            AvifResultCode code = Report(
                diagnostics,
                AvifResultCode.InvalidTrack,
                "The TrackHeaderBox contains an invalid identifier or reserved field.",
                fullBox.PayloadOffset);
            diagnostics.Context.PopBox();
            return code;
        }
        diagnostics.Context.PopBox();
        return AvifResultCode.Success;
    }

    static AvifResultCode ParseMedia(
        ReadOnlySpan<byte> source,
        IsoBox? mediaBox,
        List<IsoBox> topLevelBoxes,
        ResourceGuard resources,
        DiagnosticLog diagnostics,
        ParsedMovieTrack track)
    {
        diagnostics.Context.PushBox("mdia");
        MovieChildLookup mediaHeader = FindChild(
            mediaBox, "mdhd", true, diagnostics);
        MovieChildLookup handler = FindChild(
            mediaBox, "hdlr", true, diagnostics);
        MovieChildLookup mediaInformation = FindChild(
            mediaBox, "minf", true, diagnostics);
        if (mediaHeader.Code != AvifResultCode.Success
            || handler.Code != AvifResultCode.Success
            || mediaInformation.Code != AvifResultCode.Success)
        {
            AvifResultCode childCode = mediaHeader.Code != AvifResultCode.Success
                ? mediaHeader.Code
                : handler.Code != AvifResultCode.Success
                    ? handler.Code : mediaInformation.Code;
            diagnostics.Context.PopBox();
            return childCode;
        }

        AvifResultCode code = ParseMediaHeader(
            source, mediaHeader.Box, track, diagnostics);
        if (code == AvifResultCode.Success)
        {
            code = ParseHandler(source, handler.Box, track, diagnostics);
        }
        if (code == AvifResultCode.Success)
        {
            code = ParseMediaInformation(
                source,
                mediaInformation.Box,
                topLevelBoxes,
                resources,
                diagnostics,
                track);
        }
        diagnostics.Context.PopBox();
        return code;
    }

    static AvifResultCode ParseMediaHeader(
        ReadOnlySpan<byte> source,
        IsoBox? box,
        ParsedMovieTrack track,
        DiagnosticLog diagnostics)
    {
        diagnostics.Context.PushBox("mdhd");
        IsoFullBox fullBox = IsoFullBox.Parse(source, box);
        if (fullBox.Code != AvifResultCode.Success)
        {
            AvifResultCode code = Report(
                diagnostics, Normalize(fullBox.Code),
                "The MediaHeaderBox FullBox fields are invalid.",
                (box ?? throw new InvalidOperationException("Invalid internal state.")).PayloadOffset);
            diagnostics.Context.PopBox();
            return code;
        }
        if ((fullBox.Version != 0 && fullBox.Version != 1)
            || fullBox.Flags != 0)
        {
            AvifResultCode code = Report(
                diagnostics,
                fullBox.Version > 1
                    ? AvifResultCode.UnsupportedFeature
                    : AvifResultCode.InvalidTrack,
                "The MediaHeaderBox version or flags are invalid.",
                (box ?? throw new InvalidOperationException("Invalid internal state.")).PayloadOffset);
            diagnostics.Context.PopBox();
            return code;
        }
        int expectedSize = fullBox.Version == 0 ? 20 : 32;
        ReadOnlySpan<byte> payload = GetPayload(
            source, fullBox, expectedSize, diagnostics, box,
            "The MediaHeaderBox has an invalid payload size.");
        if (payload.Length == 0)
        {
            diagnostics.Context.PopBox();
            return diagnostics.Code;
        }

        ByteReader reader = new ByteReader(payload.Length);
        if (fullBox.Version == 0)
        {
            track.MediaCreationTime = reader.ReadUInt32BigEndian(payload);
            track.MediaModificationTime = reader.ReadUInt32BigEndian(payload);
            track.MediaTimescale = reader.ReadUInt32BigEndian(payload);
            uint duration = reader.ReadUInt32BigEndian(payload);
            track.MediaDuration = duration == uint.MaxValue
                ? ulong.MaxValue : duration;
        }
        else
        {
            track.MediaCreationTime = reader.ReadUInt64BigEndian(payload);
            track.MediaModificationTime = reader.ReadUInt64BigEndian(payload);
            track.MediaTimescale = reader.ReadUInt32BigEndian(payload);
            track.MediaDuration = reader.ReadUInt64BigEndian(payload);
        }
        ushort packedLanguage = reader.ReadUInt16BigEndian(payload);
        ushort predefined = reader.ReadUInt16BigEndian(payload);
        track.Language = DecodeLanguage(packedLanguage);
        if (reader.Code != AvifResultCode.Success
            || track.MediaTimescale == 0
            || predefined != 0
            || track.Language == null)
        {
            AvifResultCode code = Report(
                diagnostics,
                AvifResultCode.InvalidTrack,
                "The MediaHeaderBox contains invalid timing, language, or reserved fields.",
                fullBox.PayloadOffset);
            diagnostics.Context.PopBox();
            return code;
        }
        diagnostics.Context.PopBox();
        return AvifResultCode.Success;
    }

    static AvifResultCode ParseHandler(
        ReadOnlySpan<byte> source,
        IsoBox? box,
        ParsedMovieTrack track,
        DiagnosticLog diagnostics)
    {
        diagnostics.Context.PushBox("hdlr");
        HandlerBox handler = HandlerBox.Parse(source, box);
        if (handler.Code != AvifResultCode.Success
            || handler.HandlerType.Length != 4)
        {
            AvifResultCode code = Report(
                diagnostics,
                handler.Code == AvifResultCode.UnsupportedFeature
                    ? handler.Code : AvifResultCode.InvalidTrack,
                "The media HandlerBox is invalid.",
                (box ?? throw new InvalidOperationException("Invalid internal state.")).PayloadOffset);
            diagnostics.Context.PopBox();
            return code;
        }
        track.HandlerType = handler.HandlerType;
        track.HandlerName = handler.Name;
        diagnostics.Context.PopBox();
        return AvifResultCode.Success;
    }

    static AvifResultCode ParseMediaInformation(
        ReadOnlySpan<byte> source,
        IsoBox? mediaInformation,
        List<IsoBox> topLevelBoxes,
        ResourceGuard resources,
        DiagnosticLog diagnostics,
        ParsedMovieTrack track)
    {
        diagnostics.Context.PushBox("minf");
        MovieChildLookup videoHeader = FindChild(
            mediaInformation, "vmhd", false, diagnostics);
        MovieChildLookup sampleTable = FindChild(
            mediaInformation, "stbl", true, diagnostics);
        if (videoHeader.Code != AvifResultCode.Success
            || sampleTable.Code != AvifResultCode.Success)
        {
            diagnostics.Context.PopBox();
            return videoHeader.Code != AvifResultCode.Success
                ? videoHeader.Code : sampleTable.Code;
        }
        if (videoHeader.Box != null)
        {
            AvifResultCode code = ParseVideoMediaHeader(
                source, videoHeader.Box, track, diagnostics);
            if (code != AvifResultCode.Success)
            {
                diagnostics.Context.PopBox();
                return code;
            }
        }
        AvifResultCode sampleCode = SampleTableBoxParser.Parse(
            source,
            sampleTable.Box,
            topLevelBoxes,
            resources,
            diagnostics,
            track.SampleTable);
        diagnostics.Context.PopBox();
        return sampleCode;
    }

    static AvifResultCode ParseVideoMediaHeader(
        ReadOnlySpan<byte> source,
        IsoBox box,
        ParsedMovieTrack track,
        DiagnosticLog diagnostics)
    {
        diagnostics.Context.PushBox("vmhd");
        IsoFullBox fullBox = IsoFullBox.Parse(source, box);
        if (fullBox.Code != AvifResultCode.Success
            || fullBox.Version != 0 || fullBox.Flags != 1)
        {
            AvifResultCode code = Report(
                diagnostics,
                fullBox.Version > 0
                    ? AvifResultCode.UnsupportedFeature
                    : AvifResultCode.InvalidTrack,
                "The VideoMediaHeaderBox version or flags are invalid.",
                box.PayloadOffset);
            diagnostics.Context.PopBox();
            return code;
        }
        ReadOnlySpan<byte> payload = GetPayload(
            source, fullBox, 8, diagnostics, box,
            "The VideoMediaHeaderBox has an invalid payload size.");
        if (payload.Length == 0)
        {
            diagnostics.Context.PopBox();
            return diagnostics.Code;
        }
        ByteReader reader = new ByteReader(payload.Length);
        track.VideoGraphicsMode = reader.ReadUInt16BigEndian(payload);
        track.VideoOpColorRed = reader.ReadUInt16BigEndian(payload);
        track.VideoOpColorGreen = reader.ReadUInt16BigEndian(payload);
        track.VideoOpColorBlue = reader.ReadUInt16BigEndian(payload);
        track.HasVideoMediaHeader = reader.Code == AvifResultCode.Success;
        if (!track.HasVideoMediaHeader)
        {
            AvifResultCode code = Report(
                diagnostics,
                AvifResultCode.InvalidTrack,
                "The VideoMediaHeaderBox is truncated.",
                fullBox.PayloadOffset);
            diagnostics.Context.PopBox();
            return code;
        }
        diagnostics.Context.PopBox();
        return AvifResultCode.Success;
    }

    static AvifResultCode ParseEdit(
        ReadOnlySpan<byte> source,
        IsoBox editBox,
        uint movieTimescale,
        ResourceGuard resources,
        DiagnosticLog diagnostics,
        ParsedMovieTrack track)
    {
        diagnostics.Context.PushBox("edts");
        MovieChildLookup editList = FindChild(
            editBox, "elst", true, diagnostics);
        if (editList.Code != AvifResultCode.Success)
        {
            diagnostics.Context.PopBox();
            return editList.Code;
        }
        diagnostics.Context.PushBox("elst");
        IsoFullBox fullBox = IsoFullBox.Parse(source, editList.Box);
        if (fullBox.Code != AvifResultCode.Success
            || (fullBox.Version != 0 && fullBox.Version != 1)
            || (fullBox.Flags & -2) != 0)
        {
            AvifResultCode code = Report(
                diagnostics,
                fullBox.Version > 1
                    ? AvifResultCode.UnsupportedFeature
                    : AvifResultCode.InvalidTrack,
                "The EditListBox version or flags are invalid.",
                (editList.Box ?? throw new InvalidOperationException("Invalid internal state.")).PayloadOffset);
            diagnostics.Context.PopBox();
            diagnostics.Context.PopBox();
            return code;
        }
        CheckedRangeResult range = CheckedMath.ResolveRange(
            fullBox.PayloadOffset,
            0,
            fullBox.PayloadSize,
            source.Length);
        if (!range.Success || fullBox.PayloadSize < 4)
        {
            AvifResultCode code = Report(
                diagnostics,
                AvifResultCode.InvalidTrack,
                "The EditListBox payload is truncated.",
                (editList.Box ?? throw new InvalidOperationException("Invalid internal state.")).PayloadOffset);
            diagnostics.Context.PopBox();
            diagnostics.Context.PopBox();
            return code;
        }
        ReadOnlySpan<byte> payload = source.Slice(
            (int)fullBox.PayloadOffset,
            (int)fullBox.PayloadSize);
        ByteReader reader = new ByteReader(payload.Length);
        uint entryCount = reader.ReadUInt32BigEndian(payload);
        int entrySize = fullBox.Version == 0 ? 12 : 20;
        ulong expectedSize = 4 + (ulong)entryCount * (ulong)entrySize;
        if (entryCount == 0 || expectedSize != (ulong)payload.Length
            || (fullBox.Flags == 1 && entryCount != 1))
        {
            AvifResultCode code = Report(
                diagnostics,
                AvifResultCode.InvalidTrack,
                "The EditListBox entry count or payload size is invalid.",
                fullBox.PayloadOffset);
            diagnostics.Context.PopBox();
            diagnostics.Context.PopBox();
            return code;
        }

        CheckedIntegerResult editStorage = CheckedMath.MultiplyInt64(
            entryCount,
            48);
        if (!editStorage.Success
            || resources.ReserveTemporaryBytes(editStorage.Value)
                != AvifResultCode.Success)
        {
            AvifResultCode code = Report(
                diagnostics,
                AvifResultCode.LimitExceeded,
                "The edit-list storage exceeds the configured temporary-byte limit.",
                fullBox.PayloadOffset);
            diagnostics.Context.PopBox();
            diagnostics.Context.PopBox();
            return code;
        }

        ulong totalDuration = 0;
        uint entryIndex = 0;
        while (entryIndex < entryCount)
        {
            ulong segmentDuration;
            long mediaTime;
            if (fullBox.Version == 0)
            {
                segmentDuration = reader.ReadUInt32BigEndian(payload);
                mediaTime = reader.ReadInt32BigEndian(payload);
            }
            else
            {
                segmentDuration = reader.ReadUInt64BigEndian(payload);
                mediaTime = reader.ReadInt64BigEndian(payload);
            }
            short rateInteger = reader.ReadInt16BigEndian(payload);
            short rateFraction = reader.ReadInt16BigEndian(payload);
            if (reader.Code != AvifResultCode.Success
                || segmentDuration == 0
                || mediaTime < -1
                || (rateInteger != 0 && rateInteger != 1)
                || rateFraction != 0
                || ulong.MaxValue - totalDuration < segmentDuration)
            {
                AvifResultCode code = Report(
                    diagnostics,
                    AvifResultCode.InvalidTrack,
                    "An EditListBox entry contains an invalid duration, time, or rate.",
                    fullBox.PayloadOffset + reader.Position);
                diagnostics.Context.PopBox();
                diagnostics.Context.PopBox();
                return code;
            }
            if (mediaTime >= 0
                && track.MediaDuration != ulong.MaxValue
                && (ulong)mediaTime >= track.MediaDuration)
            {
                AvifResultCode code = Report(
                    diagnostics,
                    AvifResultCode.InvalidTrack,
                    "An edit starts outside the declared media duration.",
                    fullBox.PayloadOffset + reader.Position);
                diagnostics.Context.PopBox();
                diagnostics.Context.PopBox();
                return code;
            }
            totalDuration += segmentDuration;
            track.Edits.Add(new ParsedTrackEdit(
                segmentDuration, mediaTime, rateInteger, rateFraction));
            entryIndex++;
        }

        track.RepeatingEdits = fullBox.Flags == 1;
        if (track.RepeatingEdits)
        {
            if (track.Duration == ulong.MaxValue)
            {
                track.RepetitionInfinite = true;
            }
            else if (track.Duration == 0)
            {
                AvifResultCode code = Report(
                    diagnostics,
                    AvifResultCode.InvalidTrack,
                    "A repeated edit list requires a positive or indefinite track duration.",
                    fullBox.PayloadOffset);
                diagnostics.Context.PopBox();
                diagnostics.Context.PopBox();
                return code;
            }
            else
            {
                ulong presentations = track.Duration / totalDuration;
                if (track.Duration % totalDuration != 0)
                {
                    presentations++;
                }
                track.RepetitionCount = presentations - 1;
            }
        }
        else if (track.Duration != ulong.MaxValue
            && totalDuration != track.Duration)
        {
            AvifResultCode code = Report(
                diagnostics,
                AvifResultCode.InvalidTrack,
                "The edit-list duration does not equal the track duration.",
                fullBox.PayloadOffset);
            diagnostics.Context.PopBox();
            diagnostics.Context.PopBox();
            return code;
        }

        diagnostics.Context.PopBox();
        diagnostics.Context.PopBox();
        return AvifResultCode.Success;
    }

    static AvifResultCode ValidateTrack(
        ResourceGuard resources,
        DiagnosticLog diagnostics,
        ParsedMovieTrack track)
    {
        bool visual = track.HandlerType == "pict"
            || track.HandlerType == "vide"
            || track.HandlerType == "auxv";
        if (!visual)
        {
            return AvifResultCode.Success;
        }
        if (track.Width == 0 || track.Height == 0
            || (track.WidthFixed & 65535) != 0
            || (track.HeightFixed & 65535) != 0
            || !track.HasVideoMediaHeader)
        {
            return Report(
                diagnostics,
                AvifResultCode.InvalidTrack,
                "A visual track requires integral positive dimensions and a VideoMediaHeaderBox.",
                AvifDiagnostic.UnknownByteOffset);
        }
        AvifResultCode dimensionCode = resources.CheckImageDimensions(
            track.Width, track.Height);
        if (dimensionCode != AvifResultCode.Success)
        {
            return Report(
                diagnostics,
                dimensionCode,
                "The visual track dimensions exceed configured limits.",
                AvifDiagnostic.UnknownByteOffset);
        }
        return AvifResultCode.Success;
    }

    static ReadOnlySpan<byte> GetPayload(
        ReadOnlySpan<byte> source,
        IsoFullBox fullBox,
        int expectedSize,
        DiagnosticLog diagnostics,
        IsoBox? box,
        string message)
    {
        if (fullBox.PayloadSize != expectedSize)
        {
            Report(
                diagnostics,
                AvifResultCode.InvalidTrack,
                message,
                (box ?? throw new InvalidOperationException("Invalid internal state.")).PayloadOffset);
            return ReadOnlySpan<byte>.Empty;
        }
        CheckedRangeResult range = CheckedMath.ResolveRange(
            fullBox.PayloadOffset,
            0,
            expectedSize,
            source.Length);
        if (!range.Success)
        {
            Report(
                diagnostics,
                Normalize(range.Code),
                message,
                (box ?? throw new InvalidOperationException("Invalid internal state.")).PayloadOffset);
            return ReadOnlySpan<byte>.Empty;
        }
        return source.Slice((int)fullBox.PayloadOffset, expectedSize);
    }

    static MovieChildLookup FindChild(
        IsoBox? parent,
        string type,
        bool required,
        DiagnosticLog diagnostics)
    {
        IsoBox? found = null;
        int count = 0;
        int index = 0;
        while (index < (parent ?? throw new InvalidOperationException("Invalid internal state.")).Children.Count)
        {
            if (parent.Children[index].Type == type)
            {
                count++;
                if (found == null)
                {
                    found = parent.Children[index];
                }
            }
            index++;
        }
        if (count > 1 || (required && count != 1))
        {
            AvifResultCode code = Report(
                diagnostics,
                AvifResultCode.InvalidTrack,
                "The " + parent.Type + " box must contain "
                    + (required ? "exactly one " : "at most one ")
                    + type + " box.",
                parent.PayloadOffset);
            return new MovieChildLookup(code, null, count);
        }
        return new MovieChildLookup(AvifResultCode.Success, found, count);
    }

    static int CountChildren(IsoBox parent, string type)
    {
        int count = 0;
        int index = 0;
        while (index < parent.Children.Count)
        {
            if (parent.Children[index].Type == type)
            {
                count++;
            }
            index++;
        }
        return count;
    }

    static ParsedMovieTrack? FindTrack(ParsedMovie movie, uint trackId)
    {
        int index = 0;
        while (index < movie.Tracks.Count)
        {
            if (movie.Tracks[index].TrackId == trackId)
            {
                return movie.Tracks[index];
            }
            index++;
        }
        return null;
    }

    static AvifResultCode ParseTrackReferences(
        ReadOnlySpan<byte> source,
        IsoBox box,
        ResourceGuard resources,
        DiagnosticLog diagnostics,
        ParsedMovieTrack track)
    {
        diagnostics.Context.PushBox("tref");
        int childIndex = 0;
        while (childIndex < box.Children.Count)
        {
            IsoBox child = box.Children[childIndex];
            if (child.PayloadSize == 0 || (child.PayloadSize & 3) != 0)
            {
                AvifResultCode code = Report(
                    diagnostics,
                    AvifResultCode.InvalidTrack,
                    "A TrackReferenceTypeBox must contain one or more complete track identifiers.",
                    child.PayloadOffset);
                diagnostics.Context.PopBox();
                return code;
            }
            CheckedRangeResult range = CheckedMath.ResolveRange(
                child.PayloadOffset, 0, child.PayloadSize, source.Length);
            if (!range.Success)
            {
                AvifResultCode code = Report(
                    diagnostics,
                    Normalize(range.Code),
                    "A TrackReferenceTypeBox payload is truncated.",
                    child.PayloadOffset);
                diagnostics.Context.PopBox();
                return code;
            }
            int referenceCount = (int)(child.PayloadSize / 4);
            AvifResultCode countCode = resources.CountReferences(referenceCount);
            if (countCode != AvifResultCode.Success)
            {
                AvifResultCode code = Report(
                    diagnostics,
                    countCode,
                    "The track reference count exceeds its configured limit.",
                    child.PayloadOffset);
                diagnostics.Context.PopBox();
                return code;
            }
            ReadOnlySpan<byte> payload = source.Slice(
                (int)child.PayloadOffset, (int)child.PayloadSize);
            ByteReader reader = new ByteReader(payload.Length);
            while (reader.Position < payload.Length)
            {
                uint trackId = reader.ReadUInt32BigEndian(payload);
                if (trackId == 0)
                {
                    AvifResultCode code = Report(
                        diagnostics,
                        AvifResultCode.InvalidTrack,
                        "A track reference identifier must be nonzero.",
                        child.PayloadOffset + reader.Position - 4);
                    diagnostics.Context.PopBox();
                    return code;
                }
                track.References.Add(new ParsedTrackReference(
                    child.Type, trackId));
            }
            childIndex++;
        }
        diagnostics.Context.PopBox();
        return AvifResultCode.Success;
    }

    static AvifResultCode ValidateTrackReferences(
        ParsedMovie movie,
        DiagnosticLog diagnostics,
        long offset)
    {
        int trackIndex = 0;
        while (trackIndex < movie.Tracks.Count)
        {
            ParsedMovieTrack track = movie.Tracks[trackIndex];
            int referenceIndex = 0;
            while (referenceIndex < track.References.Count)
            {
                ParsedTrackReference reference = track.References[referenceIndex];
                ParsedMovieTrack? target = FindTrack(movie, reference.TrackId);
                if (reference.TrackId == track.TrackId || target == null)
                {
                    diagnostics.Context.PushTrack(track.TrackId);
                    AvifResultCode code = Report(
                        diagnostics,
                        AvifResultCode.InvalidTrack,
                        "A track reference must identify a different track in the same movie.",
                        offset);
                    diagnostics.Context.PopTrack();
                    return code;
                }
                if (reference.Type == "auxl"
                    && (track.Duration != target.Duration
                        || track.MediaTimescale != target.MediaTimescale
                        || track.MediaDuration != target.MediaDuration))
                {
                    diagnostics.Context.PushTrack(track.TrackId);
                    AvifResultCode code = Report(
                        diagnostics,
                        AvifResultCode.InvalidTrack,
                        "An auxiliary track must match its master track duration and media timescale.",
                        offset);
                    diagnostics.Context.PopTrack();
                    return code;
                }
                referenceIndex++;
            }
            trackIndex++;
        }
        return AvifResultCode.Success;
    }

    static string? DecodeLanguage(ushort packed)
    {
        if (packed == 0)
        {
            return string.Empty;
        }
        if ((packed & 32768) != 0)
        {
            return null;
        }
        int first = packed >> 10 & 31;
        int second = packed >> 5 & 31;
        int third = packed & 31;
        if (first < 1 || first > 26
            || second < 1 || second > 26
            || third < 1 || third > 26)
        {
            return null;
        }
        return new string(new char[]
        {
            (char)(first + 96),
            (char)(second + 96),
            (char)(third + 96)
        });
    }

    static AvifResultCode Normalize(AvifResultCode code)
    {
        return code == AvifResultCode.UnsupportedFeature
            || code == AvifResultCode.LimitExceeded
            ? code : AvifResultCode.InvalidTrack;
    }

    static AvifResultCode Report(
        DiagnosticLog diagnostics,
        AvifResultCode code,
        string message,
        long offset)
    {
        if (offset >= 0)
        {
            diagnostics.Context.SetLocation(
                offset, AvifDiagnostic.UnknownBitOffset);
        }
        diagnostics.Context.SetStandardReference("ISO/IEC 14496-12");
        diagnostics.Add(
            code,
            AvifDiagnosticSeverity.Error,
            code == AvifResultCode.LimitExceeded
                ? AvifDiagnosticCategory.Resource
                : AvifDiagnosticCategory.Track,
            message);
        diagnostics.Context.SetStandardReference(string.Empty);
        diagnostics.Context.ClearLocation();
        return code;
    }
}
