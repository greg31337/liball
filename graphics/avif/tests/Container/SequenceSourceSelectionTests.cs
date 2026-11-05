namespace avif.Tests;

public class SequenceSourceSelectionTests
{
    [Test]
    public void AutomaticSelectionHonorsMajorBrandAndExplicitSelection()
    {
        AvifImageItem primary = CreatePrimaryItem(7);
        AvifMovie movie = CreateMovie(new ParsedMovieTrack[]
        {
            CreateTrack(11, "pict", 0, 0, string.Empty)
        });
        AvifDocument still = CreateDocument("avif", primary, movie);
        AvifDocument sequence = CreateDocument("avis", primary, movie);
        AvifDocument neutral = CreateDocument("mif1", primary, movie);

        AvifImageSource? stillAutomatic = still.SelectSource(
            AvifSourceSelection.Automatic);
        AvifImageSource? sequenceAutomatic = sequence.SelectSource(
            AvifSourceSelection.Automatic);
        AvifImageSource? neutralAutomatic = neutral.SelectSource(
            AvifSourceSelection.Automatic);

        Assert.Multiple(() =>
        {
            Assert.That((stillAutomatic ?? throw new InvalidOperationException("Invalid internal state.")).GetSelection(),
                Is.EqualTo(AvifSourceSelection.PrimaryItem));
            Assert.That(stillAutomatic.GetPrimaryItem(), Is.SameAs(primary));
            Assert.That((sequenceAutomatic ?? throw new InvalidOperationException("Invalid internal state.")).GetSelection(),
                Is.EqualTo(AvifSourceSelection.ImageSequence));
            Assert.That((sequenceAutomatic.GetSequenceTrack() ?? throw new InvalidOperationException("Invalid internal state.")).GetTrackId(), Is.EqualTo(11));
            Assert.That((neutralAutomatic ?? throw new InvalidOperationException("Invalid internal state.")).GetSelection(),
                Is.EqualTo(AvifSourceSelection.ImageSequence));
            Assert.That(((still.SelectSource(AvifSourceSelection.ImageSequence) ?? throw new InvalidOperationException("Invalid internal state."))
                .GetSequenceTrack() ?? throw new InvalidOperationException("Invalid internal state.")).GetTrackId(), Is.EqualTo(11));
            Assert.That((sequence.SelectSource(AvifSourceSelection.PrimaryItem) ?? throw new InvalidOperationException("Invalid internal state."))
                .GetPrimaryItem(), Is.SameAs(primary));
            Assert.That(still.SelectSource((AvifSourceSelection)99), Is.Null);
        });
    }

    [Test]
    public void SequenceSourceExposesVisualAlternatesAndAlphaDepthTracks()
    {
        ParsedMovieTrack primary = CreateTrack(1, "pict", 42, 0, string.Empty);
        ParsedMovieTrack alternate = CreateTrack(2, "pict", 42, 0, string.Empty);
        ParsedMovieTrack alpha = CreateTrack(
            3,
            "auxv",
            0,
            1,
            "urn:mpeg:mpegB:cicp:systems:auxiliary:alpha");
        ParsedMovieTrack depth = CreateTrack(
            4,
            "auxv",
            0,
            1,
            "urn:mpeg:mpegB:cicp:systems:auxiliary:depth");
        AvifDocument document = CreateDocument(
            "avis",
            null,
            CreateMovie(new ParsedMovieTrack[] { primary, alternate, alpha, depth }));

        AvifImageSource? source = document.SelectSource(
            AvifSourceSelection.ImageSequence);
        AvifTrack? alphaTrack = (source ?? throw new InvalidOperationException("Invalid internal state.")).GetAuxiliaryTrack(0);
        AvifTrack? depthTrack = source.GetAuxiliaryTrack(1);

        Assert.Multiple(() =>
        {
            Assert.That((source.GetSequenceTrack() ?? throw new InvalidOperationException("Invalid internal state.")).GetTrackId(), Is.EqualTo(1));
            Assert.That(source.GetVisualTrackCount(), Is.EqualTo(2));
            Assert.That((source.GetVisualTrack(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetTrackId(), Is.EqualTo(2));
            Assert.That(source.GetAlternateTrackCount(), Is.EqualTo(1));
            Assert.That((source.GetAlternateTrack(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetTrackId(), Is.EqualTo(2));
            Assert.That(source.GetAuxiliaryTrackCount(), Is.EqualTo(2));
            Assert.That((alphaTrack ?? throw new InvalidOperationException("Invalid internal state.")).IsAuxiliary(), Is.True);
            Assert.That(alphaTrack.GetAuxiliaryMasterTrackId(), Is.EqualTo(1));
            Assert.That(alphaTrack.GetAuxiliaryKind(), Is.EqualTo(AvifAuxiliaryKind.Alpha));
            Assert.That((depthTrack ?? throw new InvalidOperationException("Invalid internal state.")).GetAuxiliaryKind(), Is.EqualTo(AvifAuxiliaryKind.Depth));
            Assert.That(depthTrack.GetReferenceType(0), Is.EqualTo("auxl"));
            Assert.That(depthTrack.GetReferencedTrackId(0), Is.EqualTo(1));
        });
    }

    [Test]
    public void MissingExplicitSourceDoesNotFallBack()
    {
        AvifDocument stillOnly = CreateDocument(
            "avif", CreatePrimaryItem(5), null);
        AvifDocument sequenceOnly = CreateDocument(
            "avis",
            null,
            CreateMovie(new ParsedMovieTrack[]
            {
                CreateTrack(9, "pict", 0, 0, string.Empty)
            }));

        Assert.Multiple(() =>
        {
            Assert.That(stillOnly.SelectSource(AvifSourceSelection.ImageSequence), Is.Null);
            Assert.That(sequenceOnly.SelectSource(AvifSourceSelection.PrimaryItem), Is.Null);
            Assert.That(((stillOnly.SelectSource(AvifSourceSelection.Automatic) ?? throw new InvalidOperationException("Invalid internal state."))
                .GetPrimaryItem() ?? throw new InvalidOperationException("Invalid internal state.")).GetItemId(), Is.EqualTo(5));
        });
    }

    static ParsedMovieTrack CreateTrack(
        uint trackId,
        string handlerType,
        int alternateGroup,
        uint masterTrackId,
        string auxiliaryType)
    {
        ParsedMovieTrack track = new ParsedMovieTrack();
        track.TrackId = trackId;
        track.Flags = 3;
        track.HandlerType = handlerType;
        track.AlternateGroup = alternateGroup;
        track.MediaTimescale = 1000;
        track.MediaDuration = 2000;
        track.Width = 640;
        track.Height = 360;
        if (masterTrackId != 0)
        {
            track.References.Add(new ParsedTrackReference("auxl", masterTrackId));
        }
        if (auxiliaryType.Length != 0)
        {
            ParsedAv1SampleDescription description = new ParsedAv1SampleDescription();
            description.AuxiliaryType = new ParsedAuxiliaryType(
                auxiliaryType, Array.Empty<byte>());
            track.SampleTable.Descriptions.Add(description);
        }
        return track;
    }

    static AvifMovie CreateMovie(ParsedMovieTrack[] tracks)
    {
        ParsedMovie movie = new ParsedMovie();
        movie.Timescale = 1000;
        movie.Duration = 2000;
        movie.NextTrackId = 100;
        movie.Tracks.AddRange(tracks);
        return new AvifMovie(movie);
    }

    static AvifImageItem CreatePrimaryItem(uint itemId)
    {
        ParsedItem item = new ParsedItem(
            itemId,
            2,
            false,
            0,
            "av01",
            "primary",
            string.Empty,
            string.Empty,
            string.Empty);
        ParsedItemRole role = new ParsedItemRole(
            item,
            true,
            false,
            false,
            string.Empty,
            new List<ParsedItem>(),
            new List<ParsedItem>(),
            new List<ParsedItem>(),
            new List<ParsedItem>());
        return new AvifImageItem(
            item,
            role,
            true,
            new List<AvifItemExtent>(),
            new List<AvifItemProperty>(),
            new List<AvifItemReference>(),
            new List<AvifItemReference>(),
            new List<uint>());
    }

    static AvifDocument CreateDocument(
        string majorBrand,
        AvifImageItem? primary,
        AvifMovie? movie)
    {
        List<AvifImageItem> items = new List<AvifImageItem>();
        if (primary != null)
        {
            items.Add(primary);
        }
        AvifFileInfo fileInfo = new AvifFileInfo(
            majorBrand,
            0,
            new List<string>(),
            "pict",
            string.Empty,
            primary != null,
            primary == null ? 0 : primary.GetItemId());
        return new AvifDocument(
            AvifResultCode.Success,
            fileInfo,
            movie,
            new List<AvifBox>(),
            new List<AvifBox>(),
            items,
            new List<AvifItemReference>(),
            new List<AvifEntityGroup>(),
            new List<AvifDiagnostic>());
    }
}
