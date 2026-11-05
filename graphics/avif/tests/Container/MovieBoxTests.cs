using System.Buffers.Binary;

namespace avif.Tests;

public class MovieBoxTests
{
    [Test]
    public void VersionZeroMovieExposesValidatedTrackMetadata()
    {
        byte[] source = CreateMovie(0, new uint[] { 1 }, 2000, false, 640, 360);

        MovieParseResult result = Parse(source, new AvifSecurityLimits());
        AvifMovie movie = new AvifMovie(result.Movie);
        AvifTrack? track = movie.GetTrack(0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(movie.GetModificationTime(), Is.EqualTo(6));
            Assert.That(movie.GetTimescale(), Is.EqualTo(1000));
            Assert.That(movie.GetDuration(), Is.EqualTo(2000));
            Assert.That(movie.GetDurationSeconds(), Is.EqualTo(2));
            Assert.That(movie.GetNextTrackId(), Is.EqualTo(2));
            Assert.That(movie.GetTrackCount(), Is.EqualTo(1));
            Assert.That(movie.FindTrack(1), Is.SameAs(track));
            Assert.That((track ?? throw new InvalidOperationException("Invalid internal state.")).GetTrackId(), Is.EqualTo(1));
            Assert.That(track.IsEnabled(), Is.True);
            Assert.That(track.IsInMovie(), Is.True);
            Assert.That(track.IsInPreview(), Is.False);
            Assert.That(track.GetModificationTime(), Is.EqualTo(7));
            Assert.That(track.GetWidth(), Is.EqualTo(640));
            Assert.That(track.GetHeight(), Is.EqualTo(360));
            Assert.That(track.GetLayer(), Is.Zero);
            Assert.That(track.GetAlternateGroup(), Is.Zero);
            Assert.That(track.IsVisual(), Is.True);
            Assert.That(track.GetVolume(), Is.EqualTo(1));
            Assert.That(track.GetMediaTimescale(), Is.EqualTo(48000));
            Assert.That(track.GetMediaDuration(), Is.EqualTo(96000));
            Assert.That(track.GetMediaModificationTime(), Is.EqualTo(8));
            Assert.That(track.IsMediaDurationIndefinite(), Is.False);
            Assert.That(track.GetMediaDurationSeconds(), Is.EqualTo(2));
            Assert.That(track.GetLanguage(), Is.EqualTo("und"));
            Assert.That(track.GetHandlerType(), Is.EqualTo("pict"));
            Assert.That(track.GetHandlerName(), Is.EqualTo("PictureHandler"));
            Assert.That(track.HasVideoMediaHeader(), Is.True);
            Assert.That(track.GetVideoGraphicsMode(), Is.EqualTo(64));
            Assert.That(track.GetVideoOpColor(0), Is.EqualTo(1));
            Assert.That(track.GetVideoOpColor(1), Is.EqualTo(2));
            Assert.That(track.GetVideoOpColor(2), Is.EqualTo(3));
            Assert.That(track.GetEditCount(), Is.EqualTo(2));
            Assert.That((track.GetEdit(0) ?? throw new InvalidOperationException("Invalid internal state.")).IsEmpty(), Is.True);
            Assert.That((track.GetEdit(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetSegmentDuration(), Is.EqualTo(500));
            Assert.That((track.GetEdit(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetMediaRateInteger(), Is.EqualTo(1));
            Assert.That((track.GetEdit(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetMediaRateFraction(), Is.Zero);
            Assert.That((track.GetEdit(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetMediaTime(), Is.EqualTo(0));
            Assert.That(track.HasRepeatingEditList(), Is.False);
            Assert.That(track.HasKnownRepetitionCount(), Is.True);
        });
    }

    [Test]
    public void PartialMovieWithUnavailableTimescaleReportsZeroSeconds()
    {
        ParsedMovie parsed = new ParsedMovie();
        parsed.Duration = 25;
        parsed.Timescale = 0;
        AvifMovie movie = new AvifMovie(parsed);

        Assert.That(movie.GetDurationSeconds(), Is.Zero);
    }

    [Test]
    public void VersionOneMovieReportsFiniteEditRepetition()
    {
        byte[] source = CreateMovie(1, new uint[] { 7 }, 7000, true, 1920, 1080);

        MovieParseResult result = Parse(source, new AvifSecurityLimits());
        AvifMovie movie = new AvifMovie(result.Movie);
        AvifTrack? track = movie.GetTrack(0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(movie.GetCreationTime(), Is.EqualTo(4294967301));
            Assert.That((track ?? throw new InvalidOperationException("Invalid internal state.")).GetCreationTime(), Is.EqualTo(4294967302));
            Assert.That(track.GetMediaCreationTime(), Is.EqualTo(4294967303));
            Assert.That(track.GetDuration(), Is.EqualTo(7000));
            Assert.That(track.HasRepeatingEditList(), Is.True);
            Assert.That(track.HasKnownRepetitionCount(), Is.True);
            Assert.That(track.IsRepetitionInfinite(), Is.False);
            Assert.That(track.GetRepetitionCount(), Is.EqualTo(3));
            Assert.That((track.GetEdit(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetSegmentDuration(), Is.EqualTo(2000));
            Assert.That((track.GetEdit(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetSegmentDurationSeconds(), Is.EqualTo(2));
        });
    }

    [Test]
    public void IndefiniteTrackDurationProducesInfiniteEditRepetition()
    {
        byte[] source = CreateMovie(
            1, new uint[] { 4 }, ulong.MaxValue, true, 800, 600);

        MovieParseResult result = Parse(source, new AvifSecurityLimits());
        AvifTrack? track = new AvifMovie(result.Movie).GetTrack(0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((track ?? throw new InvalidOperationException("Invalid internal state.")).IsDurationIndefinite(), Is.True);
            Assert.That(track.GetDurationSeconds(), Is.EqualTo(double.PositiveInfinity));
            Assert.That(track.IsRepetitionInfinite(), Is.True);
            Assert.That(track.GetRepetitionCount(), Is.Zero);
        });
    }

    [Test]
    public void DocumentBuilderPublishesMovieAndKnownFullBoxes()
    {
        byte[] source = CreateMovie(0, new uint[] { 1 }, 2000, false, 320, 240);
        IsoBoxParseResult boxes = IsoBoxReader.Parse(
            source, new AvifSecurityLimits(), false);
        MovieParseResult parsed = MovieBoxParser.Parse(
            source, boxes.Boxes[0], boxes.Boxes, boxes.Resources, boxes.Diagnostics);
        AvifContainerValidationResult validation = new AvifContainerValidationResult(
            parsed.Code, null, null, parsed.Movie, boxes, boxes.Diagnostics);

        AvifDocument document = AvifDocumentBuilder.Create(source, validation);

        Assert.Multiple(() =>
        {
            Assert.That(document.GetMovie(), Is.Not.Null);
            Assert.That(((document.GetMovie() ?? throw new InvalidOperationException("Invalid internal state.")).GetTrack(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetWidth(), Is.EqualTo(320));
            Assert.That((document.GetTopLevelBox(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetTypeName(), Is.EqualTo("moov"));
            Assert.That((document.GetTopLevelBox(0) ?? throw new InvalidOperationException("Invalid internal state.")).IsUnknown(), Is.False);
            Assert.That((FindBox(document, "mvhd") ?? throw new InvalidOperationException("Invalid internal state.")).IsFullBox(), Is.True);
            Assert.That((FindBox(document, "elst") ?? throw new InvalidOperationException("Invalid internal state.")).IsFullBox(), Is.True);
        });
    }

    [Test]
    public void ContainerValidatorAndDocumentParseIncludeMovieMetadata()
    {
        byte[] itemDocument = MinimizedImageBoxTests.CreateDocument(
            "mif3", IsoBoxType.GetValue("avif"), false, false, false, false);
        byte[] movieBytes = CreateMovie(
            0, new uint[] { 2 }, 3000, false, 1024, 768);
        byte[] source = new byte[itemDocument.Length + movieBytes.Length];
        itemDocument.CopyTo(source.AsSpan());
        movieBytes.CopyTo(source.AsSpan(itemDocument.Length));
        AvifValidationOptions options = new AvifValidationOptions();
        options.ValidateEncodedPayload = false;

        AvifContainerValidationResult validation = AvifContainerValidator.Validate(
            source, options);
        AvifDocument document = AvifDocument.Parse(source, options);

        Assert.Multiple(() =>
        {
            Assert.That(validation.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(validation.Movie, Is.Not.Null);
            Assert.That(document.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(((document.GetMovie() ?? throw new InvalidOperationException("Invalid internal state.")).FindTrack(2) ?? throw new InvalidOperationException("Invalid internal state.")).GetHeight(), Is.EqualTo(768));
        });
    }

    [Test]
    public void ParserRejectsInvalidTimescaleDimensionsAndEditDuration()
    {
        byte[] zeroTimescale = CreateMovie(0, new uint[] { 1 }, 2000, false, 640, 360);
        SetMovieTimescale(zeroTimescale, 0);
        byte[] fractionalWidth = CreateMovie(0, new uint[] { 1 }, 2000, false, 640, 360);
        SetTrackWidth(fractionalWidth, 0x02800001);
        byte[] wrongEditDuration = CreateMovie(0, new uint[] { 1 }, 2000, false, 640, 360);
        SetFirstEditDuration(wrongEditDuration, 400);
        byte[] invalidHandler = CreateMovie(0, new uint[] { 1 }, 2000, false, 640, 360);
        SetHandlerPredefined(invalidHandler, 1);

        MovieParseResult timescaleResult = Parse(zeroTimescale, new AvifSecurityLimits());
        MovieParseResult dimensionsResult = Parse(fractionalWidth, new AvifSecurityLimits());
        MovieParseResult editResult = Parse(wrongEditDuration, new AvifSecurityLimits());
        MovieParseResult handlerResult = Parse(invalidHandler, new AvifSecurityLimits());

        Assert.Multiple(() =>
        {
            Assert.That(timescaleResult.Code, Is.EqualTo(AvifResultCode.InvalidTrack));
            Assert.That(dimensionsResult.Code, Is.EqualTo(AvifResultCode.InvalidTrack));
            Assert.That(editResult.Code, Is.EqualTo(AvifResultCode.InvalidTrack));
            Assert.That(handlerResult.Code, Is.EqualTo(AvifResultCode.InvalidTrack));
        });
    }

    [Test]
    public void ParserRejectsDuplicateTrackIdsAndHonorsTrackLimit()
    {
        byte[] source = CreateMovie(0, new uint[] { 1, 1 }, 2000, false, 640, 360);
        AvifSecurityLimits limits = new AvifSecurityLimits();
        limits.MaxTrackCount = 1;

        MovieParseResult duplicate = Parse(source, new AvifSecurityLimits());
        MovieParseResult limited = Parse(source, limits);

        Assert.Multiple(() =>
        {
            Assert.That(duplicate.Code, Is.EqualTo(AvifResultCode.InvalidTrack));
            Assert.That(limited.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
        });
    }

    [Test]
    public void ParserBoundsRetainedEditListStorage()
    {
        byte[] source = CreateMovie(
            0,
            new uint[] { 1 },
            2000,
            false,
            640,
            360);
        AvifSecurityLimits limits = new AvifSecurityLimits();
        limits.MaxTemporaryBytes = 95;

        MovieParseResult result = Parse(source, limits);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code,
                Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(result.Movie, Is.Null);
        });
    }

    [Test]
    public void ParserResolvesTrackReferencesAndRejectsDanglingTargets()
    {
        byte[] validSource = CreateReferencedMovie(1);
        byte[] danglingSource = CreateReferencedMovie(99);
        byte[] timingMismatchSource = CreateReferencedMovie(1, 1000);

        MovieParseResult valid = Parse(validSource, new AvifSecurityLimits());
        MovieParseResult dangling = Parse(danglingSource, new AvifSecurityLimits());
        MovieParseResult timingMismatch = Parse(
            timingMismatchSource, new AvifSecurityLimits());
        AvifTrack? auxiliary = new AvifMovie(valid.Movie).FindTrack(2);

        Assert.Multiple(() =>
        {
            Assert.That(valid.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((auxiliary ?? throw new InvalidOperationException("Invalid internal state.")).IsAuxiliary(), Is.True);
            Assert.That(auxiliary.GetAuxiliaryMasterTrackId(), Is.EqualTo(1));
            Assert.That(auxiliary.GetReferenceCount(), Is.EqualTo(1));
            Assert.That(dangling.Code, Is.EqualTo(AvifResultCode.InvalidTrack));
            Assert.That(timingMismatch.Code, Is.EqualTo(AvifResultCode.InvalidTrack));
        });
    }

    static MovieParseResult Parse(byte[] source, AvifSecurityLimits limits)
    {
        IsoBoxParseResult boxes = IsoBoxReader.Parse(source, limits, false);
        Assert.That(boxes.Code, Is.EqualTo(AvifResultCode.Success));
        return MovieBoxParser.Parse(
            source, boxes.Boxes[0], boxes.Boxes, boxes.Resources, boxes.Diagnostics);
    }

    static AvifBox? FindBox(AvifDocument document, string type)
    {
        int index = 0;
        while (index < document.GetBoxCount())
        {
            AvifBox? box = document.GetBox(index);
            if ((box ?? throw new InvalidOperationException("Invalid internal state.")).GetTypeName() == type)
            {
                return box;
            }
            index++;
        }
        return null;
    }

    static byte[] CreateMovie(
        int version,
        uint[] trackIds,
        ulong trackDuration,
        bool repeatingEdit,
        int width,
        int height)
    {
        IsoBoxWriter writer = new IsoBoxWriter(1024, 1024 * 1024);
        uint maximumTrackId = 0;
        int trackIdIndex = 0;
        while (trackIdIndex < trackIds.Length)
        {
            maximumTrackId = System.Math.Max(maximumTrackId, trackIds[trackIdIndex]);
            trackIdIndex++;
        }
        writer.BeginBox("moov");
        WriteMovieHeader(writer, version, trackDuration, maximumTrackId + 1);
        int index = 0;
        while (index < trackIds.Length)
        {
            WriteTrack(
                writer,
                version,
                trackIds[index],
                trackDuration,
                repeatingEdit,
                width,
                height);
            index++;
        }
        writer.EndBox();
        IsoBoxWriteResult result = writer.Finish();
        Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
        return result.Data;
    }

    static byte[] CreateReferencedMovie(
        uint masterTrackId,
        ulong auxiliaryDuration = 2000)
    {
        IsoBoxWriter writer = new IsoBoxWriter(1024, 1024 * 1024);
        writer.BeginBox("moov");
        WriteMovieHeader(writer, 0, 2000, 3);
        WriteTrack(writer, 0, 1, 2000, false, 640, 360);
        WriteTrack(
            writer, 0, 2, auxiliaryDuration, false, 640, 360, masterTrackId);
        writer.EndBox();
        IsoBoxWriteResult result = writer.Finish();
        Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
        return result.Data;
    }

    static void WriteMovieHeader(
        IsoBoxWriter writer,
        int version,
        ulong duration,
        uint nextTrackId)
    {
        byte[] payload = new byte[version == 0 ? 96 : 108];
        if (version == 0)
        {
            BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(0, 4), 5);
            BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(4, 4), 6);
            BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(8, 4), 1000);
            BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(12, 4), (uint)duration);
            BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(92, 4), nextTrackId);
        }
        else
        {
            BinaryPrimitives.WriteUInt64BigEndian(payload.AsSpan(0, 8), 4294967301);
            BinaryPrimitives.WriteUInt64BigEndian(payload.AsSpan(8, 8), 4294967302);
            BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(16, 4), 1000);
            BinaryPrimitives.WriteUInt64BigEndian(payload.AsSpan(20, 8), duration);
            BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(104, 4), nextTrackId);
        }
        writer.BeginFullBox("mvhd", version, 0);
        writer.Write(payload);
        writer.EndBox();
    }

    static void WriteTrack(
        IsoBoxWriter writer,
        int version,
        uint trackId,
        ulong duration,
        bool repeatingEdit,
        int width,
        int height,
        uint auxiliaryMasterTrackId = 0)
    {
        writer.BeginBox("trak");
        WriteTrackHeader(writer, version, trackId, duration, width, height);
        if (auxiliaryMasterTrackId != 0)
        {
            writer.BeginBox("tref");
            writer.BeginBox("auxl");
            WriteUInt32(writer, auxiliaryMasterTrackId);
            writer.EndBox();
            writer.EndBox();
        }
        writer.BeginBox("edts");
        WriteEditList(writer, version, duration, repeatingEdit);
        writer.EndBox();
        writer.BeginBox("mdia");
        WriteMediaHeader(writer, version);
        WriteHandler(writer);
        writer.BeginBox("minf");
        WriteVideoMediaHeader(writer);
        writer.BeginBox("stbl");
        writer.EndBox();
        writer.EndBox();
        writer.EndBox();
        writer.EndBox();
    }

    static void WriteTrackHeader(
        IsoBoxWriter writer,
        int version,
        uint trackId,
        ulong duration,
        int width,
        int height)
    {
        byte[] payload = new byte[version == 0 ? 80 : 92];
        int widthOffset;
        if (version == 0)
        {
            BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(0, 4), 6);
            BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(4, 4), 7);
            BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(8, 4), trackId);
            BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(16, 4), (uint)duration);
            widthOffset = 72;
        }
        else
        {
            BinaryPrimitives.WriteUInt64BigEndian(payload.AsSpan(0, 8), 4294967302);
            BinaryPrimitives.WriteUInt64BigEndian(payload.AsSpan(8, 8), 4294967303);
            BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(16, 4), trackId);
            BinaryPrimitives.WriteUInt64BigEndian(payload.AsSpan(24, 8), duration);
            widthOffset = 84;
        }
        BinaryPrimitives.WriteUInt16BigEndian(payload.AsSpan(widthOffset - 40, 2), 256);
        BinaryPrimitives.WriteUInt32BigEndian(
            payload.AsSpan(widthOffset, 4), (uint)width << 16);
        BinaryPrimitives.WriteUInt32BigEndian(
            payload.AsSpan(widthOffset + 4, 4), (uint)height << 16);
        writer.BeginFullBox("tkhd", version, 3);
        writer.Write(payload);
        writer.EndBox();
    }

    static void WriteMediaHeader(IsoBoxWriter writer, int version)
    {
        byte[] payload = new byte[version == 0 ? 20 : 32];
        int languageOffset;
        if (version == 0)
        {
            BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(0, 4), 7);
            BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(4, 4), 8);
            BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(8, 4), 48000);
            BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(12, 4), 96000);
            languageOffset = 16;
        }
        else
        {
            BinaryPrimitives.WriteUInt64BigEndian(payload.AsSpan(0, 8), 4294967303);
            BinaryPrimitives.WriteUInt64BigEndian(payload.AsSpan(8, 8), 4294967304);
            BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(16, 4), 48000);
            BinaryPrimitives.WriteUInt64BigEndian(payload.AsSpan(20, 8), 96000);
            languageOffset = 28;
        }
        BinaryPrimitives.WriteUInt16BigEndian(
            payload.AsSpan(languageOffset, 2), PackLanguage("und"));
        writer.BeginFullBox("mdhd", version, 0);
        writer.Write(payload);
        writer.EndBox();
    }

    static void WriteHandler(IsoBoxWriter writer)
    {
        byte[] payload = new byte[35];
        WriteFourCc(payload.AsSpan(4, 4), "pict");
        System.Text.Encoding.UTF8.GetBytes("PictureHandler").CopyTo(payload.AsSpan(20));
        writer.BeginFullBox("hdlr", 0, 0);
        writer.Write(payload);
        writer.EndBox();
    }

    static void WriteVideoMediaHeader(IsoBoxWriter writer)
    {
        byte[] payload = new byte[8];
        BinaryPrimitives.WriteUInt16BigEndian(payload.AsSpan(0, 2), 64);
        BinaryPrimitives.WriteUInt16BigEndian(payload.AsSpan(2, 2), 1);
        BinaryPrimitives.WriteUInt16BigEndian(payload.AsSpan(4, 2), 2);
        BinaryPrimitives.WriteUInt16BigEndian(payload.AsSpan(6, 2), 3);
        writer.BeginFullBox("vmhd", 0, 1);
        writer.Write(payload);
        writer.EndBox();
    }

    static void WriteEditList(
        IsoBoxWriter writer,
        int version,
        ulong trackDuration,
        bool repeating)
    {
        int count = repeating ? 1 : 2;
        int entrySize = version == 0 ? 12 : 20;
        byte[] payload = new byte[4 + count * entrySize];
        BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(0, 4), (uint)count);
        if (version == 0)
        {
            uint firstDuration = repeating ? 2000U : 500U;
            BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(4, 4), firstDuration);
            BinaryPrimitives.WriteInt32BigEndian(payload.AsSpan(8, 4), repeating ? 0 : -1);
            BinaryPrimitives.WriteInt16BigEndian(payload.AsSpan(12, 2), 1);
            if (!repeating)
            {
                BinaryPrimitives.WriteUInt32BigEndian(
                    payload.AsSpan(16, 4), (uint)trackDuration - 500);
                BinaryPrimitives.WriteInt32BigEndian(payload.AsSpan(20, 4), 0);
                BinaryPrimitives.WriteInt16BigEndian(payload.AsSpan(24, 2), 1);
            }
        }
        else
        {
            ulong firstDuration = repeating ? 2000UL : 500UL;
            BinaryPrimitives.WriteUInt64BigEndian(payload.AsSpan(4, 8), firstDuration);
            BinaryPrimitives.WriteInt64BigEndian(payload.AsSpan(12, 8), repeating ? 0 : -1);
            BinaryPrimitives.WriteInt16BigEndian(payload.AsSpan(20, 2), 1);
            if (!repeating)
            {
                BinaryPrimitives.WriteUInt64BigEndian(
                    payload.AsSpan(24, 8), trackDuration - 500);
                BinaryPrimitives.WriteInt64BigEndian(payload.AsSpan(32, 8), 0);
                BinaryPrimitives.WriteInt16BigEndian(payload.AsSpan(40, 2), 1);
            }
        }
        writer.BeginFullBox("elst", version, repeating ? 1 : 0);
        writer.Write(payload);
        writer.EndBox();
    }

    static ushort PackLanguage(string language)
    {
        return (ushort)(((language[0] - 96) << 10)
            | ((language[1] - 96) << 5)
            | (language[2] - 96));
    }

    static void WriteFourCc(Span<byte> destination, string value)
    {
        destination[0] = (byte)value[0];
        destination[1] = (byte)value[1];
        destination[2] = (byte)value[2];
        destination[3] = (byte)value[3];
    }

    static void WriteUInt32(IsoBoxWriter writer, uint value)
    {
        byte[] bytes = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
        writer.Write(bytes);
    }

    static void SetMovieTimescale(byte[] source, uint timescale)
    {
        IsoBox? movieHeader = FindIsoBox(source, "mvhd");
        BinaryPrimitives.WriteUInt32BigEndian(
            source.AsSpan((int)(movieHeader ?? throw new InvalidOperationException("Invalid internal state.")).PayloadOffset + 12, 4), timescale);
    }

    static void SetTrackWidth(byte[] source, uint width)
    {
        IsoBox? trackHeader = FindIsoBox(source, "tkhd");
        BinaryPrimitives.WriteUInt32BigEndian(
            source.AsSpan((int)(trackHeader ?? throw new InvalidOperationException("Invalid internal state.")).PayloadOffset + 76, 4), width);
    }

    static void SetFirstEditDuration(byte[] source, uint duration)
    {
        IsoBox? editList = FindIsoBox(source, "elst");
        BinaryPrimitives.WriteUInt32BigEndian(
            source.AsSpan((int)(editList ?? throw new InvalidOperationException("Invalid internal state.")).PayloadOffset + 8, 4), duration);
    }

    static void SetHandlerPredefined(byte[] source, uint predefined)
    {
        IsoBox? handler = FindIsoBox(source, "hdlr");
        BinaryPrimitives.WriteUInt32BigEndian(
            source.AsSpan((int)(handler ?? throw new InvalidOperationException("Invalid internal state.")).PayloadOffset + 4, 4), predefined);
    }

    static IsoBox? FindIsoBox(byte[] source, string type)
    {
        IsoBoxParseResult boxes = IsoBoxReader.Parse(
            source, new AvifSecurityLimits(), false);
        return FindIsoBox(boxes.Boxes[0], type);
    }

    static IsoBox? FindIsoBox(IsoBox parent, string type)
    {
        if (parent.Type == type)
        {
            return parent;
        }
        int index = 0;
        while (index < parent.Children.Count)
        {
            IsoBox? found = FindIsoBox(parent.Children[index], type);
            if (found != null)
            {
                return found;
            }
            index++;
        }
        return null;
    }
}
