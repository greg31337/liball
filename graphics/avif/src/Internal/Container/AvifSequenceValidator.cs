using System;
using System.Collections.Generic;

namespace avif;

internal static class AvifSequenceValidator
{
    internal static void Validate(
        ReadOnlySpan<byte> source,
        FileTypeBox? fileType,
        ParsedMovie? movie,
        AvifValidationOptions options,
        DiagnosticLog diagnostics)
    {
        if (options == null || diagnostics == null)
        {
            return;
        }
        if (movie == null)
        {
            ValidateBrands(fileType, 0, false, diagnostics, options);
            return;
        }

        int sequenceTrackCount = 0;
        bool hasAllSyncSequence = false;
        int trackIndex = 0;
        while (trackIndex < movie.Tracks.Count)
        {
            ParsedMovieTrack track = movie.Tracks[trackIndex];
            if (IsSequenceCandidate(track))
            {
                bool auxiliary = IsAuxiliary(track);
                if (!auxiliary)
                {
                    sequenceTrackCount++;
                    if (AllSamplesAreSync(track))
                    {
                        hasAllSyncSequence = true;
                    }
                }

                diagnostics.Context.PushTrack(track.TrackId);
                ValidateTrack(source, fileType, movie, track, auxiliary, options, diagnostics);
                diagnostics.Context.PopTrack();
                if (AvifContainerValidator.ShouldStop(diagnostics, options))
                {
                    return;
                }
            }
            trackIndex++;
        }

        ValidateTrackSet(movie, sequenceTrackCount, diagnostics, options);
        if (AvifContainerValidator.ShouldStop(diagnostics, options))
        {
            return;
        }

        ValidateBrands(
            fileType,
            sequenceTrackCount,
            hasAllSyncSequence,
            diagnostics,
            options);
    }

    static bool IsSequenceCandidate(ParsedMovieTrack track)
    {
        if (track == null)
        {
            return false;
        }
        return track.SampleTable.Descriptions.Count > 0;
    }

    static bool IsAuxiliary(ParsedMovieTrack track)
    {
        int index = 0;
        while (index < track.References.Count)
        {
            if (track.References[index].Type == "auxl")
            {
                return true;
            }
            index++;
        }
        return false;
    }

    static bool AllSamplesAreSync(ParsedMovieTrack track)
    {
        if (track.SampleTable.Samples.Count == 0)
        {
            return false;
        }
        int index = 0;
        while (index < track.SampleTable.Samples.Count)
        {
            if (!track.SampleTable.Samples[index].Sync)
            {
                return false;
            }
            index++;
        }
        return true;
    }

    static void ValidateTrack(
        ReadOnlySpan<byte> source,
        FileTypeBox? fileType,
        ParsedMovie movie,
        ParsedMovieTrack track,
        bool auxiliary,
        AvifValidationOptions options,
        DiagnosticLog diagnostics)
    {
        if (track.HandlerType != "pict")
        {
            AddFinding(
                diagnostics,
                options,
                AvifValidationRequirement.Mandatory,
                "An AV1 image-sequence track must use the pict handler.",
                "AVIF 1.2 section 3");
        }

        if (track.SampleTable.Descriptions.Count != 1)
        {
            AddFinding(
                diagnostics,
                options,
                AvifValidationRequirement.Mandatory,
                "An AV1 image-sequence track must contain exactly one sample description.",
                "AVIF 1.2 section 3");
        }

        if (track.SampleTable.Samples.Count == 0)
        {
            AddFinding(
                diagnostics,
                options,
                AvifValidationRequirement.Mandatory,
                "A MIAF image sequence must contain at least one media sample.",
                "MIAF image sequence");
        }

        ValidateDimensions(track, options, diagnostics);
        ValidateTiming(movie, track, options, diagnostics);
        if (auxiliary)
        {
            ValidateAuxiliary(movie, track, options, diagnostics);
        }
        else
        {
            ValidateProfile(fileType, track, options, diagnostics);
        }

        if (options.ValidateEncodedPayload
            && track.SampleTable.Descriptions.Count == 1
            && track.SampleTable.Samples.Count > 0)
        {
            ValidateSequenceHeaders(source, track, options, diagnostics);
        }
    }

    static void ValidateDimensions(
        ParsedMovieTrack track,
        AvifValidationOptions options,
        DiagnosticLog diagnostics)
    {
        if (track.SampleTable.Descriptions.Count != 1)
        {
            return;
        }
        ParsedAv1SampleDescription description = track.SampleTable.Descriptions[0];
        if (description.Format != "av01"
            || description.Width != track.Width
            || description.Height != track.Height)
        {
            AddFinding(
                diagnostics,
                options,
                AvifValidationRequirement.Mandatory,
                "A MIAF AV1 sequence sample entry must match the track dimensions and use av01.",
                "MIAF image sequence");
        }
    }

    static void ValidateTiming(
        ParsedMovie movie,
        ParsedMovieTrack track,
        AvifValidationOptions options,
        DiagnosticLog diagnostics)
    {
        ulong expectedDecodeTime = 0;
        bool validTimeline = true;
        int sampleIndex = 0;
        while (sampleIndex < track.SampleTable.Samples.Count)
        {
            ParsedTrackSample sample = track.SampleTable.Samples[sampleIndex];
            if (sample.Duration == 0
                || sample.DecodeTime != expectedDecodeTime
                || expectedDecodeTime > ulong.MaxValue - sample.Duration)
            {
                validTimeline = false;
                break;
            }
            expectedDecodeTime += sample.Duration;
            sampleIndex++;
        }

        if (!validTimeline
            || track.MediaDuration == ulong.MaxValue
            || expectedDecodeTime != track.MediaDuration)
        {
            AddFinding(
                diagnostics,
                options,
                AvifValidationRequirement.Mandatory,
                "Sequence sample timing must be contiguous and equal the declared media duration.",
                "MIAF image sequence timing");
        }

        if (track.Edits.Count == 0
            && track.Duration != ulong.MaxValue
            && track.MediaDuration != ulong.MaxValue
            && !DurationsMatch(
                track.Duration,
                movie.Timescale,
                track.MediaDuration,
                track.MediaTimescale))
        {
            AddFinding(
                diagnostics,
                options,
                AvifValidationRequirement.Mandatory,
                "An unedited sequence track and its media must declare the same duration.",
                "MIAF image sequence timing");
        }
    }

    static bool DurationsMatch(
        ulong trackDuration,
        uint movieTimescale,
        ulong mediaDuration,
        uint mediaTimescale)
    {
        if (movieTimescale == 0 || mediaTimescale == 0)
        {
            return false;
        }
        ulong wholeSeconds = mediaDuration / mediaTimescale;
        if (wholeSeconds > ulong.MaxValue / movieTimescale)
        {
            return false;
        }
        ulong scaledDuration = wholeSeconds * movieTimescale;
        ulong partial = mediaDuration % mediaTimescale * movieTimescale;
        ulong wholePartial = partial / mediaTimescale;
        if (scaledDuration > ulong.MaxValue - wholePartial)
        {
            return false;
        }
        ulong floorDuration = scaledDuration + wholePartial;
        if (trackDuration == floorDuration)
        {
            return true;
        }
        return partial % mediaTimescale != 0
            && floorDuration < ulong.MaxValue
            && trackDuration == floorDuration + 1;
    }

    static void ValidateAuxiliary(
        ParsedMovie movie,
        ParsedMovieTrack track,
        AvifValidationOptions options,
        DiagnosticLog diagnostics)
    {
        ParsedMovieTrack? master = FindAuxiliaryMaster(movie, track);
        if (master == null)
        {
            return;
        }
        bool timingMatches = track.SampleTable.Samples.Count
            == master.SampleTable.Samples.Count;
        int index = 0;
        while (timingMatches && index < track.SampleTable.Samples.Count)
        {
            ParsedTrackSample auxiliarySample = track.SampleTable.Samples[index];
            ParsedTrackSample masterSample = master.SampleTable.Samples[index];
            timingMatches = auxiliarySample.DecodeTime == masterSample.DecodeTime
                && auxiliarySample.Duration == masterSample.Duration;
            index++;
        }
        if (!timingMatches)
        {
            AddFinding(
                diagnostics,
                options,
                AvifValidationRequirement.Mandatory,
                "An auxiliary image sequence must match its master sample timing.",
                "MIAF auxiliary image sequence");
        }

        if (track.SampleTable.Descriptions.Count == 1)
        {
            ParsedAv1SampleDescription auxiliaryDescription =
                track.SampleTable.Descriptions[0];
            ParsedAv1Configuration? configuration = auxiliaryDescription.Configuration;
            if (configuration != null && !configuration.IsMonochrome())
            {
                AddFinding(
                    diagnostics,
                    options,
                    AvifValidationRequirement.Mandatory,
                    "An AV1 auxiliary image sequence must be monochrome.",
                    "AVIF 1.2 section 4.1");
            }
            if (auxiliaryDescription.AuxiliaryType != null
                && auxiliaryDescription.AuxiliaryType.GetTypeValue()
                    == "urn:mpeg:mpegB:cicp:systems:auxiliary:alpha"
                && (track.Width != master.Width || track.Height != master.Height))
            {
                AddFinding(
                    diagnostics,
                    options,
                    AvifValidationRequirement.Mandatory,
                    "An alpha image sequence must match its master dimensions.",
                    "MIAF alpha auxiliary sequence");
            }
            if (auxiliaryDescription.AuxiliaryType != null
                && auxiliaryDescription.AuxiliaryType.GetTypeValue()
                    == "urn:mpeg:mpegB:cicp:systems:auxiliary:alpha"
                && master.SampleTable.Descriptions.Count == 1
                && configuration != null
                && master.SampleTable.Descriptions[0].Configuration != null
                && configuration.GetBitDepth()
                    != (master.SampleTable.Descriptions[0].Configuration ?? throw new InvalidOperationException("Invalid internal state.")).GetBitDepth())
            {
                AddFinding(
                    diagnostics,
                    options,
                    AvifValidationRequirement.Mandatory,
                    "An alpha image sequence must use its master bit depth.",
                    "AVIF 1.2 section 4.1");
            }
        }
    }

    static void ValidateTrackSet(
        ParsedMovie movie,
        int sequenceTrackCount,
        DiagnosticLog diagnostics,
        AvifValidationOptions options)
    {
        if (sequenceTrackCount > 1)
        {
            int alternateGroup = 0;
            bool validAlternates = true;
            int index = 0;
            while (index < movie.Tracks.Count)
            {
                ParsedMovieTrack track = movie.Tracks[index];
                if (IsSequenceCandidate(track) && !IsAuxiliary(track))
                {
                    if (alternateGroup == 0)
                    {
                        alternateGroup = track.AlternateGroup;
                        validAlternates = alternateGroup != 0;
                    }
                    else if (track.AlternateGroup != alternateGroup)
                    {
                        validAlternates = false;
                    }
                }
                index++;
            }
            if (!validAlternates)
            {
                AddFinding(
                    diagnostics,
                    options,
                    AvifValidationRequirement.Mandatory,
                    "Multiple primary image-sequence tracks must form one alternate group.",
                    "MIAF single-track condition");
            }
        }

        bool hasDuration = false;
        ulong duration = 0;
        bool durationsMatch = true;
        int trackIndex = 0;
        while (trackIndex < movie.Tracks.Count)
        {
            ParsedMovieTrack track = movie.Tracks[trackIndex];
            if (IsSequenceCandidate(track))
            {
                if (!hasDuration)
                {
                    duration = track.Duration;
                    hasDuration = true;
                }
                else if (track.Duration != duration)
                {
                    durationsMatch = false;
                }
            }
            trackIndex++;
        }
        if (!durationsMatch)
        {
            AddFinding(
                diagnostics,
                options,
                AvifValidationRequirement.Mandatory,
                "All MIAF sequence tracks must have the same presentation duration.",
                "MIAF matched-duration condition");
        }
    }

    static ParsedMovieTrack? FindAuxiliaryMaster(
        ParsedMovie movie,
        ParsedMovieTrack auxiliary)
    {
        uint masterTrackId = 0;
        int referenceIndex = 0;
        while (referenceIndex < auxiliary.References.Count)
        {
            ParsedTrackReference reference = auxiliary.References[referenceIndex];
            if (reference.Type == "auxl")
            {
                masterTrackId = reference.TrackId;
                break;
            }
            referenceIndex++;
        }
        int trackIndex = 0;
        while (trackIndex < movie.Tracks.Count)
        {
            if (movie.Tracks[trackIndex].TrackId == masterTrackId)
            {
                return movie.Tracks[trackIndex];
            }
            trackIndex++;
        }
        return null;
    }

    static void ValidateProfile(
        FileTypeBox? fileType,
        ParsedMovieTrack track,
        AvifValidationOptions options,
        DiagnosticLog diagnostics)
    {
        if (fileType == null
            || track.SampleTable.Descriptions.Count != 1
            || track.SampleTable.Descriptions[0].Configuration == null)
        {
            return;
        }
        ParsedAv1SampleDescription description = track.SampleTable.Descriptions[0];
        ParsedAv1Configuration? configuration = description.Configuration;
        long pixels = (long)description.Width * description.Height;
        bool commonLimits = (configuration ?? throw new InvalidOperationException("Invalid internal state.")).GetSequenceLevelIndex() <= 13
            && description.Width <= 8192
            && description.Height <= 4352
            && pixels <= 8912896;
        if (fileType.HasBrand("MA1B")
            && (configuration.GetSequenceProfile() != 0 || !commonLimits))
        {
            AddFinding(
                diagnostics,
                options,
                AvifValidationRequirement.Mandatory,
                "The AV1 image sequence exceeds the MA1B profile constraints.",
                "AVIF 1.2 section 8.2");
        }
        if (fileType.HasBrand("MA1A")
            && ((configuration.GetSequenceProfile() != 0
                    && configuration.GetSequenceProfile() != 1)
                || !commonLimits))
        {
            AddFinding(
                diagnostics,
                options,
                AvifValidationRequirement.Mandatory,
                "The AV1 image sequence exceeds the MA1A profile constraints.",
                "AVIF 1.2 section 8.3");
        }
    }

    static void ValidateSequenceHeaders(
        ReadOnlySpan<byte> source,
        ParsedMovieTrack track,
        AvifValidationOptions options,
        DiagnosticLog diagnostics)
    {
        ParsedAv1Configuration? configuration =
            track.SampleTable.Descriptions[0].Configuration;
        if (configuration == null)
        {
            return;
        }
        ParsedAv1SequenceHeader? expected = configuration.GetConfigSequenceHeader();
        int sampleIndex = 0;
        while (sampleIndex < track.SampleTable.Samples.Count)
        {
            ParsedTrackSample sample = track.SampleTable.Samples[sampleIndex];
            if (sample.Offset > (ulong)source.Length
                || sample.Size > (ulong)source.Length - sample.Offset)
            {
                AddPayloadFinding(
                    diagnostics,
                    options,
                    sampleIndex,
                    sample.Offset,
                    AvifResultCode.TruncatedData,
                    "A sequence sample payload is outside the input source.");
                return;
            }
            ReadOnlySpan<byte> payload = source.Slice((int)sample.Offset, (int)sample.Size);
            Av1ObuParseResult parsed = Av1ObuParser.ParseLowOverhead(
                payload,
                options.Limits,
                0);
            if (parsed.Code != AvifResultCode.Success)
            {
                AddPayloadFinding(
                    diagnostics,
                    options,
                    sampleIndex,
                    sample.Offset,
                    parsed.Code,
                    "A sequence sample contains an invalid low-overhead OBU stream.");
                return;
            }

            int obuIndex = 0;
            while (obuIndex < parsed.Obus.Count)
            {
                Av1Obu obu = parsed.Obus[obuIndex];
                if (obu.Header.Type == Av1ObuType.SequenceHeader)
                {
                    Av1SequenceHeaderParseResult sequence =
                        Av1SequenceHeaderParser.ParsePayload(obu.GetPayload());
                    if (sequence.Code != AvifResultCode.Success
                        || !configuration.MatchesSequenceHeader(sequence.Header)
                        || expected != null
                            && !expected.PayloadEquals((sequence.Header ?? throw new InvalidOperationException("Invalid internal state.")).GetRawPayload()))
                    {
                        AvifResultCode code = sequence.Code == AvifResultCode.Success
                            ? AvifResultCode.NonConformingData : sequence.Code;
                        AddPayloadFinding(
                            diagnostics,
                            options,
                            sampleIndex,
                            sample.Offset + (ulong)obu.Offset,
                            code,
                            "Every in-band sequence header must be valid, identical, and match av1C.");
                        return;
                    }
                    if (expected == null)
                    {
                        expected = sequence.Header;
                    }
                }
                obuIndex++;
            }
            sampleIndex++;
        }

        if (expected == null)
        {
            AddFinding(
                diagnostics,
                options,
                AvifValidationRequirement.Mandatory,
                "An AV1 image sequence must provide a sequence header in av1C or sample data.",
                "AV1-ISOBMFF sequence configuration");
            return;
        }

        List<ParsedColourInformation> colours = track
            .SampleTable
            .Descriptions[0]
            .ColourInformation;
        int colourIndex = 0;
        while (colourIndex < colours.Count)
        {
            AvifResultCode colourCode = configuration.ReconcileColourInformationWithSequenceHeader(
                colours[colourIndex],
                expected);
            if (colourCode != AvifResultCode.Success)
            {
                AddFinding(
                    diagnostics,
                    options,
                    AvifValidationRequirement.Mandatory,
                    "The sequence color property does not agree with its AV1 sequence header.",
                    "AVIF color information reconciliation");
                return;
            }
            colourIndex++;
        }
    }

    static void AddPayloadFinding(
        DiagnosticLog diagnostics,
        AvifValidationOptions options,
        int frameIndex,
        ulong offset,
        AvifResultCode code,
        string message)
    {
        diagnostics.Context.PushFrame(frameIndex);
        if (offset <= long.MaxValue)
        {
            diagnostics.Context.SetLocation((long)offset, AvifDiagnostic.UnknownBitOffset);
        }
        AvifContainerValidator.AddFinding(
            diagnostics,
            options,
            code == AvifResultCode.NonConformingData
                ? AvifValidationRequirement.Mandatory
                : AvifValidationRequirement.Hard,
            code,
            AvifDiagnosticCategory.Av1,
            message,
            "AVIF 1.2 section 3");
        diagnostics.Context.ClearLocation();
        diagnostics.Context.PopFrame();
    }

    static void ValidateBrands(
        FileTypeBox? fileType,
        int sequenceTrackCount,
        bool hasAllSyncSequence,
        DiagnosticLog diagnostics,
        AvifValidationOptions options)
    {
        if (fileType == null || fileType.Code != AvifResultCode.Success)
        {
            return;
        }
        diagnostics.Context.PushBox("ftyp");
        if (fileType.HasBrand("avis") && sequenceTrackCount == 0)
        {
            AddFinding(
                diagnostics,
                options,
                AvifValidationRequirement.Mandatory,
                "The avis brand requires at least one AV1 image sequence.",
                "AVIF 1.2 section 6.3");
        }
        if (sequenceTrackCount > 0 && !fileType.HasBrand("avis"))
        {
            AddFinding(
                diagnostics,
                options,
                AvifValidationRequirement.Advisory,
                "A file containing an AV1 image sequence should list the avis brand.",
                "AVIF 1.2 section 6.3");
        }
        if (fileType.HasBrand("avis") && !fileType.HasBrand("msf1"))
        {
            AddFinding(
                diagnostics,
                options,
                AvifValidationRequirement.Mandatory,
                "An AVIF image-sequence file must conform to and list the msf1 brand.",
                "MIAF box-level image-sequence requirements");
        }
        if (fileType.HasBrand("avio") && !hasAllSyncSequence)
        {
            AddFinding(
                diagnostics,
                options,
                AvifValidationRequirement.Mandatory,
                "The avio brand requires at least one image sequence made only of sync samples.",
                "AVIF 1.2 section 6.3");
        }
        if ((fileType.HasBrand("MA1B") || fileType.HasBrand("MA1A"))
            && sequenceTrackCount > 1)
        {
            AddFinding(
                diagnostics,
                options,
                AvifValidationRequirement.Advisory,
                "An AVIF profile image-sequence file should contain one primary sequence track.",
                "MIAF profile single-track condition");
        }
        diagnostics.Context.PopBox();
    }

    static void AddFinding(
        DiagnosticLog diagnostics,
        AvifValidationOptions options,
        AvifValidationRequirement requirement,
        string message,
        string standardReference)
    {
        AvifContainerValidator.AddFinding(
            diagnostics,
            options,
            requirement,
            AvifResultCode.NonConformingData,
            AvifDiagnosticCategory.Validation,
            message,
            standardReference);
    }
}
