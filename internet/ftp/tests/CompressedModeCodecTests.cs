namespace ftp.Tests;

public class CompressedModeCodecTests
{
    [Test]
    public void Ascii_data_uses_filler_replicate_and_literal_tokens_with_eof_escape()
    {
        FtpCompressedModeState state = CreateState(
            FtpFileStructure.File, FtpTransferType.Ascii);
        FtpStructureFrame frame = CreateFrame(
            new byte[] { 32, 32, 32, 65, 65, 66 }, false, true);

        FtpCompressedModeSegment[] segments = state.EncodeFrame(frame, false);

        Assert.Multiple(() =>
        {
            Assert.That(segments, Has.Length.EqualTo(3));
            Assert.That(segments[0].EncodedBytes, Is.EqualTo(new byte[] { 0xC3 }));
            Assert.That(segments[1].EncodedBytes, Is.EqualTo(new byte[] { 0x82, 65 }));
            Assert.That(segments[2].EncodedBytes,
                Is.EqualTo(new byte[] { 0, 0x40, 1, 66 }));
            Assert.That(segments[2].EndOfFile, Is.True);
            Assert.That(segments[2].Frame!.RequiresEndOfFileDescriptor, Is.True);
            Assert.That(state.DataBytes, Is.EqualTo(6));
            Assert.That(state.WireBytes, Is.EqualTo(7));
            Assert.That(state.SegmentCount, Is.EqualTo(3));
        });

        Assert.DoesNotThrow(() => state.RequireEndOfFile());
    }

    [TestCase(FtpTransferType.Ascii, 32)]
    [TestCase(FtpTransferType.Ebcdic, 64)]
    [TestCase(FtpTransferType.Image, 0)]
    [TestCase(FtpTransferType.LocalByte, 0)]
    public void Representation_selects_the_rfc_filler_byte(
        FtpTransferType type,
        int filler)
    {
        FtpCompressedModeState state = CreateState(FtpFileStructure.File, type);
        byte value = (byte)filler;

        FtpCompressedModeSegment[] segments = state.EncodeFrame(CreateFrame(
            new byte[] { value, value, value }, false, true), false);

        Assert.Multiple(() =>
        {
            Assert.That(state.FillerByte, Is.EqualTo(value));
            Assert.That(segments, Has.Length.EqualTo(1));
            Assert.That(segments[0].EncodedBytes,
                Is.EqualTo(new byte[] { 0, 0x40, 0xC3 }));
        });
    }

    [Test]
    public void Runs_and_literals_split_at_their_six_and_seven_bit_limits()
    {
        byte[] data = new byte[192];
        int index = 0;
        while (index < 64)
        {
            data[index] = 7;
            index++;
        }

        while (index < data.Length)
        {
            data[index] = (byte)(index - 63);
            index++;
        }

        FtpCompressedModeState state = CreateState(
            FtpFileStructure.File, FtpTransferType.Image);

        FtpCompressedModeSegment[] segments = state.EncodeFrame(CreateFrame(
            data, false, true), false);

        Assert.Multiple(() =>
        {
            Assert.That(segments, Has.Length.EqualTo(4));
            Assert.That(segments[0].EncodedBytes, Is.EqualTo(new byte[] { 0xBF, 7 }));
            Assert.That(segments[1].EncodedBytes, Is.EqualTo(new byte[] { 0x81, 7 }));
            Assert.That(segments[2].EncodedBytes[0], Is.EqualTo(127));
            Assert.That(segments[2].Count, Is.EqualTo(127));
            Assert.That(segments[3].EncodedBytes,
                Is.EqualTo(new byte[] { 0, 0x40, 1, 128 }));
        });
    }

    [Test]
    public void Decoder_retains_escape_literal_and_replicate_state_across_reads()
    {
        byte[] wire = new byte[]
        {
            0xC3,
            0x82, 65,
            0, 0x40, 1, 66
        };
        FtpCompressedModeState state = CreateState(
            FtpFileStructure.File, FtpTransferType.Ascii);

        FtpCompressedModeSegment[] first = state.DecodeBytes(wire, 0, 2, false);
        FtpCompressedModeSegment[] second = state.DecodeBytes(wire, 2, 3, false);
        FtpCompressedModeSegment[] final = state.DecodeBytes(wire, 5, 2, true);

        Assert.Multiple(() =>
        {
            Assert.That(first, Has.Length.EqualTo(1));
            Assert.That(first[0].Data, Is.EqualTo(new byte[] { 32, 32, 32 }));
            Assert.That(state.PendingKind, Is.Zero);
            Assert.That(second, Has.Length.EqualTo(1));
            Assert.That(second[0].Data, Is.EqualTo(new byte[] { 65, 65 }));
            Assert.That(final, Has.Length.EqualTo(1));
            Assert.That(final[0].Data, Is.EqualTo(new byte[] { 66 }));
            Assert.That(final[0].EndOfFile, Is.True);
            Assert.That(state.DataBytes, Is.EqualTo(6));
            Assert.That(state.WireBytes, Is.EqualTo(wire.Length));
            Assert.That(state.EndOfFileReached, Is.True);
        });
    }

    [Test]
    public void Record_boundaries_and_suspect_flags_apply_to_the_qualified_token()
    {
        FtpCompressedModeState producer = CreateState(
            FtpFileStructure.Record, FtpTransferType.Ascii);
        FtpCompressedModeSegment record = producer.EncodeFrame(CreateFrame(
            new byte[] { 65, 66 }, true, false), true)[0];
        FtpCompressedModeSegment final = producer.EncodeFrame(CreateFrame(
            new byte[] { 67 }, true, true), false)[0];
        byte[] wire = Join(record.EncodedBytes, final.EncodedBytes);
        FtpCompressedModeState consumer = CreateState(
            FtpFileStructure.Record, FtpTransferType.Ascii);

        FtpCompressedModeSegment[] decoded = consumer.DecodeBytes(
            wire, 0, wire.Length, true);

        Assert.Multiple(() =>
        {
            Assert.That(record.EncodedBytes,
                Is.EqualTo(new byte[] { 0, 0xA0, 2, 65, 66 }));
            Assert.That(final.EncodedBytes,
                Is.EqualTo(new byte[] { 0, 0xC0, 1, 67 }));
            Assert.That(decoded[0].EndOfRecord, Is.True);
            Assert.That(decoded[0].SuspectedErrors, Is.True);
            Assert.That(decoded[0].EndOfFile, Is.False);
            Assert.That(decoded[1].EndOfRecord, Is.True);
            Assert.That(decoded[1].EndOfFile, Is.True);
            Assert.That(decoded[1].Frame!.RequiresEndOfFileDescriptor, Is.True);
        });
    }

    [Test]
    public void Empty_file_uses_a_trailing_eof_escape()
    {
        FtpCompressedModeState producer = CreateState(
            FtpFileStructure.File, FtpTransferType.Image);
        FtpCompressedModeSegment segment = producer.EncodeFrame(CreateFrame(
            new byte[0], false, true), false)[0];
        FtpCompressedModeState consumer = CreateState(
            FtpFileStructure.File, FtpTransferType.Image);

        FtpCompressedModeSegment[] decoded = consumer.DecodeBytes(
            segment.EncodedBytes, 0, segment.EncodedBytes.Length, true);

        Assert.Multiple(() =>
        {
            Assert.That(segment.EncodedBytes, Is.EqualTo(new byte[] { 0, 0x40 }));
            Assert.That(decoded, Has.Length.EqualTo(1));
            Assert.That(decoded[0].Count, Is.Zero);
            Assert.That(decoded[0].EndOfFile, Is.True);
            Assert.That(consumer.DataBytes, Is.Zero);
        });
    }

    [Test]
    public void Restart_marker_is_a_descriptor_qualified_printable_literal()
    {
        FtpCompressedModeState producer = CreateState(
            FtpFileStructure.File, FtpTransferType.Image);
        FtpCompressedModeSegment marker = producer.EncodeRestartMarker("MARK-7");
        FtpCompressedModeSegment eof = producer.EncodeFrame(CreateFrame(
            new byte[0], false, true), false)[0];
        byte[] wire = Join(marker.EncodedBytes, eof.EncodedBytes);
        FtpCompressedModeState consumer = CreateState(
            FtpFileStructure.File, FtpTransferType.Image);

        FtpCompressedModeSegment[] decoded = consumer.DecodeBytes(
            wire, 0, wire.Length, true);

        Assert.Multiple(() =>
        {
            Assert.That(marker.EncodedBytes,
                Is.EqualTo(new byte[] { 0, 0x10, 6, 77, 65, 82, 75, 45, 55 }));
            Assert.That(decoded[0].IsRestartMarker, Is.True);
            Assert.That(decoded[0].RestartMarkerText, Is.EqualTo("MARK-7"));
            Assert.That(decoded[0].Frame, Is.Null);
            Assert.That(decoded[1].EndOfFile, Is.True);
            Assert.That(producer.DataBytes, Is.Zero);
            Assert.That(consumer.DataBytes, Is.Zero);
        });
    }

    [Test]
    public void Page_frames_are_compressed_and_the_last_page_carries_eof()
    {
        FtpTransferOptions pageOptions = new FtpTransferOptions();
        pageOptions.Structure = FtpFileStructure.Page;
        FtpPageStructureState pages = PageStructureCodec.CreateFramingState(pageOptions);
        FtpPageFrame dataPage = pages.EncodePage(CreatePage(
            new byte[] { 2 }, 1, new byte[0], new byte[] { 9 }));
        FtpPageFrame lastPage = pages.EncodePage(CreatePage(
            new byte[] { 0 }, 0, new byte[0], new byte[0]));
        FtpCompressedModeState compressed = CreateState(
            FtpFileStructure.Page, FtpTransferType.Image);

        FtpCompressedModeSegment[] dataSegments = compressed.EncodePage(dataPage, false);
        FtpCompressedModeSegment[] lastSegments = compressed.EncodePage(lastPage, false);

        Assert.Multiple(() =>
        {
            Assert.That(dataSegments[0].EndOfFile, Is.False);
            Assert.That(lastSegments, Has.Length.EqualTo(2));
            Assert.That(lastSegments[0].EncodedBytes, Is.EqualTo(new byte[] { 1, 4 }));
            Assert.That(lastSegments[1].EncodedBytes,
                Is.EqualTo(new byte[] { 0, 0x40, 0xC3 }));
            Assert.That(lastSegments[1].EndOfFile, Is.True);
            Assert.That(compressed.EndOfFileReached, Is.True);
        });
    }

    [Test]
    public void Decoder_rejects_zero_runs_bad_escapes_truncation_and_missing_eof()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<FtpTransferException>(() => CreateState(
                FtpFileStructure.File, FtpTransferType.Image).DecodeBytes(
                new byte[] { 0x80 }, 0, 1, false));
            Assert.Throws<FtpTransferException>(() => CreateState(
                FtpFileStructure.File, FtpTransferType.Image).DecodeBytes(
                new byte[] { 0xC0 }, 0, 1, false));
            Assert.Throws<FtpTransferException>(() => CreateState(
                FtpFileStructure.File, FtpTransferType.Image).DecodeBytes(
                new byte[] { 0 }, 0, 1, true));
            Assert.Throws<FtpTransferException>(() => CreateState(
                FtpFileStructure.File, FtpTransferType.Image).DecodeBytes(
                new byte[] { 2, 1 }, 0, 2, true));
            Assert.Throws<FtpTransferException>(() => CreateState(
                FtpFileStructure.File, FtpTransferType.Image).DecodeBytes(
                new byte[] { 0, 1 }, 0, 2, false));
            Assert.Throws<FtpTransferException>(() => CreateState(
                FtpFileStructure.File, FtpTransferType.Image).DecodeBytes(
                new byte[] { 0, 0 }, 0, 2, false));
            Assert.Throws<FtpTransferException>(() => CreateState(
                FtpFileStructure.File, FtpTransferType.Image).DecodeBytes(
                new byte[] { 0, 0x20, 0, 0x40 }, 0, 4, false));
            Assert.Throws<FtpTransferException>(() => CreateState(
                FtpFileStructure.File, FtpTransferType.Image).DecodeBytes(
                new byte[] { 0, 0x40, 1, 9, 1, 8 }, 0, 6, true));
            Assert.Throws<FtpTransferException>(() => CreateState(
                FtpFileStructure.File, FtpTransferType.Image).DecodeBytes(
                new byte[] { 1, 9 }, 0, 2, true));
            Assert.Throws<FtpTransferException>(() => CreateState(
                FtpFileStructure.File, FtpTransferType.Image).DecodeBytes(
                new byte[] { 0, 0x10, 1, 32 }, 0, 4, false));
            Assert.Throws<FtpTransferException>(() => CreateState(
                FtpFileStructure.File, FtpTransferType.Image).DecodeBytes(
                new byte[] { 0, 0x80, 1, 9 }, 0, 4, false));
        });
    }

    [Test]
    public void Codec_and_state_validate_mode_marker_text_bounds_and_lifecycle()
    {
        FtpTransferOptions block = CreateOptions(
            FtpFileStructure.File, FtpTransferType.Image);
        block.Mode = FtpTransferMode.Block;
        FtpCompressedModeState completed = CreateState(
            FtpFileStructure.File, FtpTransferType.Image);
        completed.EncodeFrame(CreateFrame(new byte[0], false, true), false);
        FtpCompressedModeState producer = CreateState(
            FtpFileStructure.File, FtpTransferType.Image);
        producer.EncodeFrame(CreateFrame(new byte[] { 1 }, false, false), false);
        FtpCompressedModeState consumer = CreateState(
            FtpFileStructure.File, FtpTransferType.Image);
        consumer.DecodeBytes(new byte[] { 1, 1 }, 0, 2, false);

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpConfigurationException>(() => CompressedModeCodec.CreateFramingState(block));
            Assert.Throws<FtpConfigurationException>(() => CreateState(
                FtpFileStructure.File, FtpTransferType.Image).EncodeRestartMarker(""));
            Assert.Throws<FtpConfigurationException>(() => CreateState(
                FtpFileStructure.File, FtpTransferType.Image).EncodeRestartMarker("HAS SPACE"));
            Assert.Throws<FtpTransferException>(() => CreateState(
                FtpFileStructure.Record, FtpTransferType.Ascii).EncodeFrame(
                CreateFrame(new byte[0], true, false), false));
            Assert.Throws<FtpTransferException>(() => completed.EncodeFrame(
                CreateFrame(new byte[] { 1 }, false, false), false));
            Assert.Throws<FtpTransferException>(() => producer.DecodeBytes(
                new byte[] { 0, 0x40 }, 0, 2, true));
            Assert.Throws<FtpTransferException>(() => consumer.EncodeFrame(
                CreateFrame(new byte[0], false, true), false));
        });
    }

    private static FtpCompressedModeState CreateState(
        FtpFileStructure structure,
        FtpTransferType type)
    {
        return CompressedModeCodec.CreateFramingState(CreateOptions(structure, type));
    }

    private static FtpTransferOptions CreateOptions(
        FtpFileStructure structure,
        FtpTransferType type)
    {
        FtpTransferOptions options = new FtpTransferOptions();
        options.Structure = structure;
        options.Mode = FtpTransferMode.Compressed;
        options.Type = type;
        if (type == FtpTransferType.LocalByte)
        {
            options.HasLocalByteSize = true;
            options.LocalByteSize = 8;
        }

        return options;
    }

    private static FtpStructureFrame CreateFrame(
        byte[] data,
        bool endOfRecord,
        bool endOfFile)
    {
        FtpStructureFrame frame = new FtpStructureFrame();
        frame.Data = data;
        frame.Count = data.Length;
        frame.EndOfRecord = endOfRecord;
        frame.EndOfFile = endOfFile;
        if (endOfFile)
        {
            frame.RequiresEndOfFileDescriptor = true;
        }

        return frame;
    }

    private static byte[] Join(byte[] first, byte[] second)
    {
        byte[] joined = new byte[first.Length + second.Length];
        System.Array.Copy(first, 0, joined, 0, first.Length);
        System.Array.Copy(second, 0, joined, first.Length, second.Length);
        return joined;
    }

    private static FtpPageRequest CreatePage(
        byte[] pageIndex,
        int pageType,
        byte[] optionalHeaderFields,
        byte[] data)
    {
        FtpPageRequest request = new FtpPageRequest();
        request.PageIndex = pageIndex;
        request.PageType = pageType;
        request.OptionalHeaderFields = optionalHeaderFields;
        request.Data = data;
        return request;
    }
}
