namespace avif.Tests;

public class AvifSequenceValidationTests
{
    [Test]
    public void AvisBrandRequiresAnImageSequenceTrack()
    {
        AvifValidationOptions options = new AvifValidationOptions();
        options.ValidateEncodedPayload = false;
        DiagnosticLog diagnostics = new DiagnosticLog();

        AvifSequenceValidator.Validate(
            ReadOnlySpan<byte>.Empty,
            CreateFileType("avis", new string[] { "msf1", "miaf" }),
            null,
            options,
            diagnostics);

        Assert.Multiple(() =>
        {
            Assert.That(diagnostics.Code, Is.EqualTo(AvifResultCode.NonConformingData));
            Assert.That(HasMessage(diagnostics, "requires at least one AV1 image sequence"),
                Is.True);
        });
    }

    [Test]
    public void ConformingSequencePassesStructuralPayloadAndProfileValidation()
    {
        byte[] header = BuildReducedSequenceObu(13, 64, 32);
        byte[] source = Combine(header, header);
        ParsedMovie movie = CreateMovie(CreateTrack(
            1,
            "pict",
            0,
            CreateConfiguration(0, 13, header),
            new uint[] { 1000, 1000 },
            new bool[] { true, false },
            new ulong[] { 0, (ulong)header.Length },
            (uint)header.Length));
        DiagnosticLog diagnostics = Validate(
            source,
            CreateFileType("avis", new string[] { "msf1", "miaf", "MA1B" }),
            movie,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(diagnostics.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(diagnostics.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void RejectsWrongHandlerMultipleDescriptionsAndInconsistentTiming()
    {
        ParsedMovieTrack track = CreateTrack(
            1,
            "vide",
            0,
            CreateConfiguration(0, 8, null),
            new uint[] { 1000, 1000 },
            new bool[] { true, true },
            new ulong[] { 0, 10 },
            10);
        track.SampleTable.Descriptions.Add(CreateDescription(
            CreateConfiguration(0, 8, null)));
        track.SampleTable.Samples[1].DecodeTime = 900;
        track.MediaDuration = 2100;

        DiagnosticLog diagnostics = Validate(
            System.Array.Empty<byte>(),
            CreateFileType("avis", new string[] { "msf1", "miaf" }),
            CreateMovie(track),
            false);

        Assert.Multiple(() =>
        {
            Assert.That(diagnostics.Code, Is.EqualTo(AvifResultCode.NonConformingData));
            Assert.That(HasMessage(diagnostics, "pict handler"), Is.True);
            Assert.That(HasMessage(diagnostics, "exactly one sample description"), Is.True);
            Assert.That(HasMessage(diagnostics, "sample timing"), Is.True);
        });
    }

    [Test]
    public void RejectsNonidenticalInBandSequenceHeaders()
    {
        byte[] first = BuildReducedSequenceObu(4, 64, 32);
        byte[] second = BuildReducedSequenceObu(5, 64, 32);
        byte[] source = Combine(first, second);
        ParsedMovieTrack track = CreateTrack(
            7,
            "pict",
            0,
            CreateConfiguration(0, 4, first),
            new uint[] { 1000, 1000 },
            new bool[] { true, true },
            new ulong[] { 0, (ulong)first.Length },
            (uint)first.Length);

        DiagnosticLog diagnostics = Validate(
            source,
            CreateFileType("avis", new string[] { "msf1", "miaf" }),
            CreateMovie(track),
            true);
        AvifDiagnostic? finding = FindMessage(diagnostics, "Every in-band sequence header");

        Assert.Multiple(() =>
        {
            Assert.That(diagnostics.Code, Is.EqualTo(AvifResultCode.NonConformingData));
            Assert.That(finding, Is.Not.Null);
            Assert.That((finding ?? throw new InvalidOperationException("Invalid internal state.")).TrackId, Is.EqualTo(7));
            Assert.That(finding.FrameIndex, Is.EqualTo(1));
            Assert.That(finding.ByteOffset, Is.EqualTo(first.Length));
        });
    }

    [Test]
    public void EnforcesSequenceProfileAndIntraOnlyBrands()
    {
        ParsedMovieTrack track = CreateTrack(
            1,
            "pict",
            0,
            CreateConfiguration(2, 13, null),
            new uint[] { 1000, 1000 },
            new bool[] { true, false },
            new ulong[] { 0, 10 },
            10);

        DiagnosticLog diagnostics = Validate(
            System.Array.Empty<byte>(),
            CreateFileType(
                "avis",
                new string[] { "miaf", "MA1A", "avio" }),
            CreateMovie(track),
            false);

        Assert.Multiple(() =>
        {
            Assert.That(diagnostics.Code, Is.EqualTo(AvifResultCode.NonConformingData));
            Assert.That(HasMessage(diagnostics, "MA1A profile constraints"), Is.True);
            Assert.That(HasMessage(diagnostics, "only of sync samples"), Is.True);
            Assert.That((FindMessage(diagnostics, "msf1 brand") ?? throw new InvalidOperationException("Invalid internal state.")).Severity,
                Is.EqualTo(AvifDiagnosticSeverity.Error));
        });
    }

    [Test]
    public void AuxiliarySequenceMustMatchMasterFrameTimingAndBeMonochrome()
    {
        ParsedMovieTrack master = CreateTrack(
            1,
            "pict",
            0,
            CreateConfiguration(0, 8, null),
            new uint[] { 1000, 1000 },
            new bool[] { true, true },
            new ulong[] { 0, 10 },
            10);
        ParsedMovieTrack auxiliary = CreateTrack(
            2,
            "pict",
            1,
            CreateConfiguration(0, 8, null),
            new uint[] { 500, 1500 },
            new bool[] { true, true },
            new ulong[] { 20, 30 },
            10);
        auxiliary.Width = 32;
        auxiliary.SampleTable.Descriptions[0].Width = 32;
        auxiliary.SampleTable.Descriptions[0].AuxiliaryType = new ParsedAuxiliaryType(
            "urn:mpeg:mpegB:cicp:systems:auxiliary:alpha",
            System.Array.Empty<byte>());
        auxiliary.SampleTable.Descriptions[0].Configuration =
            new ParsedAv1Configuration(
                0,
                8,
                0,
                false,
                false,
                false,
                true,
                true,
                2,
                false,
                0,
                System.Array.Empty<byte>(),
                null);
        ParsedMovie movie = CreateMovie(master);
        movie.Tracks.Add(auxiliary);

        DiagnosticLog diagnostics = Validate(
            System.Array.Empty<byte>(),
            CreateFileType("avis", new string[] { "msf1", "miaf" }),
            movie,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(diagnostics.Code, Is.EqualTo(AvifResultCode.NonConformingData));
            Assert.That(HasMessage(diagnostics, "match its master sample timing"), Is.True);
            Assert.That(HasMessage(diagnostics, "must be monochrome"), Is.True);
            Assert.That(HasMessage(diagnostics, "match its master dimensions"), Is.True);
            Assert.That(HasMessage(diagnostics, "use its master bit depth"), Is.True);
        });
    }

    [Test]
    public void MultiplePrimarySequencesMustBeAlternatesWithMatchedDurations()
    {
        ParsedMovieTrack first = CreateTrack(
            1,
            "pict",
            0,
            CreateConfiguration(0, 8, null),
            new uint[] { 1000, 1000 },
            new bool[] { true, true },
            new ulong[] { 0, 10 },
            10);
        ParsedMovieTrack second = CreateTrack(
            2,
            "pict",
            0,
            CreateConfiguration(0, 8, null),
            new uint[] { 1000, 1000 },
            new bool[] { true, true },
            new ulong[] { 20, 30 },
            10);
        second.Duration = 1000;
        second.Edits.Add(new ParsedTrackEdit(1000, 0, 1, 0));
        ParsedMovie movie = CreateMovie(first);
        movie.Tracks.Add(second);

        DiagnosticLog diagnostics = Validate(
            System.Array.Empty<byte>(),
            CreateFileType("avis", new string[] { "msf1", "miaf" }),
            movie,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(diagnostics.Code, Is.EqualTo(AvifResultCode.NonConformingData));
            Assert.That(HasMessage(diagnostics, "form one alternate group"), Is.True);
            Assert.That(HasMessage(diagnostics, "same presentation duration"), Is.True);
        });
    }

    static DiagnosticLog Validate(
        byte[] source,
        FileTypeBox? fileType,
        ParsedMovie movie,
        bool validatePayload)
    {
        AvifValidationOptions options = new AvifValidationOptions();
        options.ValidateEncodedPayload = validatePayload;
        DiagnosticLog diagnostics = new DiagnosticLog();
        AvifSequenceValidator.Validate(
            source,
            fileType,
            movie,
            options,
            diagnostics);
        return diagnostics;
    }

    static ParsedMovie CreateMovie(ParsedMovieTrack track)
    {
        ParsedMovie movie = new ParsedMovie();
        movie.Timescale = 1000;
        movie.Duration = 2000;
        movie.NextTrackId = 3;
        movie.Tracks.Add(track);
        return movie;
    }

    static ParsedMovieTrack CreateTrack(
        uint trackId,
        string handler,
        uint masterTrackId,
        ParsedAv1Configuration configuration,
        uint[] durations,
        bool[] sync,
        ulong[] offsets,
        uint sampleSize)
    {
        ParsedMovieTrack track = new ParsedMovieTrack();
        track.TrackId = trackId;
        track.Flags = 3;
        track.Duration = 2000;
        track.Width = 64;
        track.Height = 32;
        track.MediaTimescale = 1000;
        track.MediaDuration = 2000;
        track.HandlerType = handler;
        track.SampleTable.Descriptions.Add(CreateDescription(configuration));
        if (masterTrackId != 0)
        {
            track.References.Add(new ParsedTrackReference("auxl", masterTrackId));
        }
        ulong decodeTime = 0;
        int index = 0;
        while (index < durations.Length)
        {
            ParsedTrackSample sample = new ParsedTrackSample(index, sampleSize);
            sample.DescriptionIndex = 1;
            sample.Offset = offsets[index];
            sample.DecodeTime = decodeTime;
            sample.Duration = durations[index];
            sample.Sync = sync[index];
            track.SampleTable.Samples.Add(sample);
            decodeTime += durations[index];
            index++;
        }
        return track;
    }

    static ParsedAv1SampleDescription CreateDescription(
        ParsedAv1Configuration configuration)
    {
        ParsedAv1SampleDescription description = new ParsedAv1SampleDescription();
        description.Index = 1;
        description.Format = "av01";
        description.DataReferenceIndex = 1;
        description.Width = 64;
        description.Height = 32;
        description.Depth = 24;
        description.Configuration = configuration;
        return description;
    }

    static ParsedAv1Configuration CreateConfiguration(
        int profile,
        int level,
        byte[]? sequenceObu)
    {
        ParsedAv1SequenceHeader? sequence = null;
        if (sequenceObu != null)
        {
            Av1SequenceHeaderParseResult parsed =
                Av1SequenceHeaderParser.ParsePayload(sequenceObu.AsSpan(2));
            Assert.That(parsed.Code, Is.EqualTo(AvifResultCode.Success));
            sequence = parsed.Header;
        }
        return new ParsedAv1Configuration(
            profile,
            level,
            0,
            true,
            false,
            false,
            true,
            true,
            2,
            false,
            0,
            sequenceObu == null ? System.Array.Empty<byte>() : sequenceObu,
            sequence);
    }

    static FileTypeBox? CreateFileType(string majorBrand, string[] compatibleBrands)
    {
        IsoBoxWriter writer = new IsoBoxWriter(0, 256);
        Assert.That(FileTypeBox.Write(writer, majorBrand, 0, compatibleBrands),
            Is.EqualTo(AvifResultCode.Success));
        IsoBoxWriteResult written = writer.Finish();
        Assert.That(written.Code, Is.EqualTo(AvifResultCode.Success));
        FileTypeParseResult parsed = FileTypeParser.Parse(
            written.Data,
            new AvifSecurityLimits());
        Assert.That(parsed.Code, Is.EqualTo(AvifResultCode.Success));
        return parsed.FileType;
    }

    static byte[] BuildReducedSequenceObu(int level, int width, int height)
    {
        byte[] payload = new byte[32];
        BitWriter writer = BitWriter.Create(payload.Length);
        writer.WriteBits(payload, 0, 3);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 1);
        writer.WriteBits(payload, (uint)level, 5);
        int widthBits = GetBitCount(width - 1);
        int heightBits = GetBitCount(height - 1);
        writer.WriteBits(payload, (uint)(widthBits - 1), 4);
        writer.WriteBits(payload, (uint)(heightBits - 1), 4);
        writer.WriteBits(payload, (uint)(width - 1), widthBits);
        writer.WriteBits(payload, (uint)(height - 1), heightBits);
        writer.WriteBits(payload, 0, 3);
        writer.WriteBits(payload, 0, 3);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 1);
        writer.WriteBits(payload, 2, 2);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 0);
        writer.WriteTrailingBits(payload);
        Assert.That(writer.Code, Is.EqualTo(AvifResultCode.Success));
        int payloadLength = (int)(writer.BitPosition / 8);
        byte[] obu = new byte[payloadLength + 2];
        obu[0] = 10;
        obu[1] = (byte)payloadLength;
        payload.AsSpan(0, payloadLength).CopyTo(obu.AsSpan(2));
        return obu;
    }

    static int GetBitCount(int value)
    {
        int count = 0;
        do
        {
            count++;
            value >>= 1;
        }
        while (value != 0);
        return count;
    }

    static byte[] Combine(byte[] first, byte[] second)
    {
        byte[] combined = new byte[first.Length + second.Length];
        first.CopyTo(combined, 0);
        second.CopyTo(combined, first.Length);
        return combined;
    }

    static bool HasMessage(DiagnosticLog diagnostics, string text)
    {
        return FindMessage(diagnostics, text) != null;
    }

    static AvifDiagnostic? FindMessage(DiagnosticLog diagnostics, string text)
    {
        int index = 0;
        while (index < diagnostics.Diagnostics.Count)
        {
            AvifDiagnostic diagnostic = diagnostics.Diagnostics[index];
            if (diagnostic.Message.Contains(text, StringComparison.Ordinal))
            {
                return diagnostic;
            }
            index++;
        }
        return null;
    }
}
