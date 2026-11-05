namespace ftp.Tests;

public class BlockModeCodecTests
{
    [Test]
    public void Record_blocks_encode_big_endian_counts_and_eor_eof_flags()
    {
        FtpBlockModeState state = CreateState(FtpFileStructure.Record);

        FtpBlockModeBlock record = state.EncodeFrame(CreateFrame(
            new byte[] { 10, 11 }, true, false), false)[0];
        FtpBlockModeBlock final = state.EncodeFrame(CreateFrame(
            new byte[] { 12 }, true, true), false)[0];

        Assert.Multiple(() =>
        {
            Assert.That(record.Descriptor, Is.EqualTo(0x80));
            Assert.That(record.SerializedBytes,
                Is.EqualTo(new byte[] { 0x80, 0, 2, 10, 11 }));
            Assert.That(final.Descriptor, Is.EqualTo(0xC0));
            Assert.That(final.SerializedBytes,
                Is.EqualTo(new byte[] { 0xC0, 0, 1, 12 }));
            Assert.That(final.EndOfRecord, Is.True);
            Assert.That(final.EndOfFile, Is.True);
            Assert.That(final.Frame!.RequiresEndOfFileDescriptor, Is.True);
            Assert.That(state.DataBytes, Is.EqualTo(3));
            Assert.That(state.WireBytes, Is.EqualTo(9));
            Assert.That(state.BlockCount, Is.EqualTo(2));
        });

        Assert.DoesNotThrow(() => state.RequireEndOfFile());
    }

    [Test]
    public void Large_frames_split_at_the_unsigned_sixteen_bit_count_limit()
    {
        byte[] data = new byte[65536];
        data[0] = 1;
        data[65535] = 2;
        FtpBlockModeState state = CreateState(FtpFileStructure.File);

        FtpBlockModeBlock[] blocks = state.EncodeFrame(CreateFrame(data, false, true), false);

        Assert.Multiple(() =>
        {
            Assert.That(blocks, Has.Length.EqualTo(2));
            Assert.That(blocks[0].Descriptor, Is.Zero);
            Assert.That(blocks[0].Count, Is.EqualTo(65535));
            Assert.That(blocks[0].SerializedBytes[1], Is.EqualTo(0xFF));
            Assert.That(blocks[0].SerializedBytes[2], Is.EqualTo(0xFF));
            Assert.That(blocks[1].Descriptor, Is.EqualTo(0x40));
            Assert.That(blocks[1].Count, Is.EqualTo(1));
            Assert.That(blocks[1].Data, Is.EqualTo(new byte[] { 2 }));
            Assert.That(state.DataBytes, Is.EqualTo(65536));
            Assert.That(state.WireBytes, Is.EqualTo(65542));
        });
    }

    [Test]
    public void Decoder_retains_partial_headers_and_payloads_across_reads()
    {
        byte[] wire = new byte[]
        {
            0x80, 0, 2, 10, 11,
            0xC0, 0, 1, 12
        };
        FtpBlockModeState state = CreateState(FtpFileStructure.Record);

        FtpBlockModeBlock[] first = state.DecodeBytes(wire, 0, 2, false);
        FtpBlockModeBlock[] second = state.DecodeBytes(wire, 2, 4, false);
        FtpBlockModeBlock[] final = state.DecodeBytes(wire, 6, 3, true);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Empty);
            Assert.That(second, Has.Length.EqualTo(1));
            Assert.That(second[0].Data, Is.EqualTo(new byte[] { 10, 11 }));
            Assert.That(second[0].EndOfRecord, Is.True);
            Assert.That(final, Has.Length.EqualTo(1));
            Assert.That(final[0].Data, Is.EqualTo(new byte[] { 12 }));
            Assert.That(final[0].EndOfRecord, Is.True);
            Assert.That(final[0].EndOfFile, Is.True);
            Assert.That(state.PendingHeaderCount, Is.Zero);
            Assert.That(state.PendingPayloadCount, Is.Zero);
            Assert.That(state.WireBytes, Is.EqualTo(wire.Length));
            Assert.That(state.DataBytes, Is.EqualTo(3));
        });
    }

    [Test]
    public void Restart_marker_blocks_use_printable_ascii_and_are_not_file_data()
    {
        FtpBlockModeState producer = CreateState(FtpFileStructure.File);
        FtpBlockModeBlock marker = producer.EncodeRestartMarker("MARK-12");
        FtpBlockModeBlock eof = producer.EncodeFrame(CreateFrame(
            new byte[0], false, true), false)[0];
        byte[] wire = Join(marker.SerializedBytes, eof.SerializedBytes);
        FtpBlockModeState consumer = CreateState(FtpFileStructure.File);

        FtpBlockModeBlock[] decoded = consumer.DecodeBytes(wire, 0, wire.Length, true);

        Assert.Multiple(() =>
        {
            Assert.That(marker.SerializedBytes,
                Is.EqualTo(new byte[] { 0x10, 0, 7, 77, 65, 82, 75, 45, 49, 50 }));
            Assert.That(decoded, Has.Length.EqualTo(2));
            Assert.That(decoded[0].IsRestartMarker, Is.True);
            Assert.That(decoded[0].RestartMarkerText, Is.EqualTo("MARK-12"));
            Assert.That(decoded[0].Frame, Is.Null);
            Assert.That(decoded[1].EndOfFile, Is.True);
            Assert.That(producer.DataBytes, Is.Zero);
            Assert.That(consumer.DataBytes, Is.Zero);
            Assert.That(consumer.WireBytes, Is.EqualTo(wire.Length));
        });
    }

    [Test]
    public void Suspected_error_flag_is_preserved_independently_of_eof()
    {
        FtpBlockModeState producer = CreateState(FtpFileStructure.File);
        FtpBlockModeBlock suspect = producer.EncodeFrame(CreateFrame(
            new byte[] { 1, 2 }, false, false), true)[0];
        FtpBlockModeBlock eof = producer.EncodeFrame(CreateFrame(
            new byte[0], false, true), false)[0];
        byte[] wire = Join(suspect.SerializedBytes, eof.SerializedBytes);
        FtpBlockModeState consumer = CreateState(FtpFileStructure.File);

        FtpBlockModeBlock[] blocks = consumer.DecodeBytes(wire, 0, wire.Length, true);

        Assert.Multiple(() =>
        {
            Assert.That(suspect.Descriptor, Is.EqualTo(0x20));
            Assert.That(blocks[0].SuspectedErrors, Is.True);
            Assert.That(blocks[0].EndOfFile, Is.False);
            Assert.That(blocks[1].SuspectedErrors, Is.False);
            Assert.That(blocks[1].EndOfFile, Is.True);
        });
    }

    [Test]
    public void Empty_final_record_can_combine_eor_and_eof_in_one_header()
    {
        FtpBlockModeState producer = CreateState(FtpFileStructure.Record);

        FtpBlockModeBlock block = producer.EncodeFrame(CreateFrame(
            new byte[0], true, true), false)[0];
        FtpBlockModeState consumer = CreateState(FtpFileStructure.Record);
        FtpBlockModeBlock[] decoded = consumer.DecodeBytes(
            block.SerializedBytes, 0, block.SerializedBytes.Length, true);

        Assert.Multiple(() =>
        {
            Assert.That(block.SerializedBytes, Is.EqualTo(new byte[] { 0xC0, 0, 0 }));
            Assert.That(decoded[0].Count, Is.Zero);
            Assert.That(decoded[0].EndOfRecord, Is.True);
            Assert.That(decoded[0].EndOfFile, Is.True);
        });
    }

    [Test]
    public void Page_frames_keep_page_bytes_and_mark_the_last_page_block_as_eof()
    {
        FtpPageStructureState pageState = CreatePageState();
        FtpPageFrame dataPage = pageState.EncodePage(CreatePage(
            new byte[] { 3 }, 1, new byte[0], new byte[] { 9 }));
        FtpPageFrame lastPage = pageState.EncodePage(CreatePage(
            new byte[] { 0 }, 0, new byte[0], new byte[0]));
        FtpBlockModeState blockState = CreateState(FtpFileStructure.Page);

        FtpBlockModeBlock dataBlock = blockState.EncodePage(dataPage, false)[0];
        FtpBlockModeBlock lastBlock = blockState.EncodePage(lastPage, false)[0];

        Assert.Multiple(() =>
        {
            Assert.That(dataBlock.Data, Is.EqualTo(dataPage.SerializedLogicalBytes));
            Assert.That(dataBlock.Descriptor, Is.Zero);
            Assert.That(lastBlock.Data, Is.EqualTo(lastPage.SerializedLogicalBytes));
            Assert.That(lastBlock.Descriptor, Is.EqualTo(0x40));
            Assert.That(lastBlock.EndOfFile, Is.True);
        });
    }

    [Test]
    public void Decoder_rejects_malformed_descriptors_markers_and_truncation()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<FtpTransferException>(() => CreateState(FtpFileStructure.File).DecodeBytes(
                new byte[] { 1, 0, 1, 9 }, 0, 4, false));
            Assert.Throws<FtpTransferException>(() => CreateState(FtpFileStructure.File).DecodeBytes(
                new byte[] { 0, 0 }, 0, 2, true));
            Assert.Throws<FtpTransferException>(() => CreateState(FtpFileStructure.File).DecodeBytes(
                new byte[] { 0, 0, 2, 1 }, 0, 4, true));
            Assert.Throws<FtpTransferException>(() => CreateState(FtpFileStructure.File).DecodeBytes(
                new byte[] { 0, 0, 1, 1 }, 0, 4, true));
            Assert.Throws<FtpTransferException>(() => CreateState(FtpFileStructure.File).DecodeBytes(
                new byte[] { 0x10, 0, 3, 65, 32, 66 }, 0, 6, false));
            Assert.Throws<FtpTransferException>(() => CreateState(FtpFileStructure.File).DecodeBytes(
                new byte[] { 0x80, 0, 0 }, 0, 3, false));
            Assert.Throws<FtpTransferException>(() => CreateState(FtpFileStructure.File).DecodeBytes(
                new byte[] { 0, 0, 0 }, 0, 3, false));
            Assert.Throws<FtpTransferException>(() => CreateState(FtpFileStructure.File).DecodeBytes(
                new byte[] { 0x40, 0, 0, 0x40, 0, 0 }, 0, 6, true));
        });
    }

    [Test]
    public void Codec_and_state_validate_mode_marker_text_bounds_and_lifecycle()
    {
        FtpTransferOptions stream = CreateOptions(FtpFileStructure.File);
        stream.Mode = FtpTransferMode.Stream;
        FtpBlockModeState completed = CreateState(FtpFileStructure.File);
        completed.EncodeFrame(CreateFrame(new byte[0], false, true), false);
        FtpBlockModeState producer = CreateState(FtpFileStructure.File);
        producer.EncodeFrame(CreateFrame(new byte[] { 1 }, false, false), false);
        FtpBlockModeState consumer = CreateState(FtpFileStructure.File);
        consumer.DecodeBytes(new byte[] { 0, 0, 1, 1 }, 0, 4, false);

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpConfigurationException>(() => BlockModeCodec.CreateFramingState(stream));
            Assert.Throws<FtpConfigurationException>(() => CreateState(
                FtpFileStructure.File).EncodeRestartMarker(""));
            Assert.Throws<FtpConfigurationException>(() => CreateState(
                FtpFileStructure.File).EncodeRestartMarker("HAS SPACE"));
            Assert.Throws<FtpTransferException>(() => completed.EncodeFrame(
                CreateFrame(new byte[] { 1 }, false, false), false));
            Assert.Throws<FtpTransferException>(() => producer.DecodeBytes(
                new byte[] { 0x40, 0, 0 }, 0, 3, true));
            Assert.Throws<FtpTransferException>(() => consumer.EncodeFrame(
                CreateFrame(new byte[0], false, true), false));
        });
    }

    private static FtpBlockModeState CreateState(FtpFileStructure structure)
    {
        return BlockModeCodec.CreateFramingState(CreateOptions(structure));
    }

    private static FtpTransferOptions CreateOptions(FtpFileStructure structure)
    {
        FtpTransferOptions options = new FtpTransferOptions();
        options.Structure = structure;
        options.Mode = FtpTransferMode.Block;
        if (structure == FtpFileStructure.Record)
        {
            options.Type = FtpTransferType.Ascii;
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

    private static FtpPageStructureState CreatePageState()
    {
        FtpTransferOptions options = new FtpTransferOptions();
        options.Structure = FtpFileStructure.Page;
        return PageStructureCodec.CreateFramingState(options);
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
