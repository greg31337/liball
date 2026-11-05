namespace ftp.Tests;

public class StreamModeCodecTests
{
    [Test]
    public void File_structure_transmits_raw_octets_and_uses_connection_close_for_eof()
    {
        FtpStreamModeState producer = CreateState(FtpFileStructure.File, false, 0);

        FtpStreamModeChunk data = producer.EncodeFrame(CreateFrame(
            new byte[] { 1, 0xFF, 2 }, false, false, false, false));
        FtpStreamModeChunk final = producer.EncodeFrame(CreateFrame(
            new byte[] { 3 }, false, true, true, false));
        FtpStreamModeState consumer = CreateState(FtpFileStructure.File, false, 0);
        FtpStructureFrame[] firstFrames = consumer.DecodeBytes(data.Data, 0, data.Count, false);
        FtpStructureFrame[] finalFrames = consumer.DecodeBytes(final.Data, 0, final.Count, true);

        Assert.Multiple(() =>
        {
            Assert.That(data.Data, Is.EqualTo(new byte[] { 1, 0xFF, 2 }));
            Assert.That(data.CloseConnection, Is.False);
            Assert.That(final.Data, Is.EqualTo(new byte[] { 3 }));
            Assert.That(final.CloseConnection, Is.True);
            Assert.That(firstFrames, Has.Length.EqualTo(1));
            Assert.That(firstFrames[0].Data, Is.EqualTo(data.Data));
            Assert.That(firstFrames[0].EndOfFile, Is.False);
            Assert.That(finalFrames, Has.Length.EqualTo(1));
            Assert.That(finalFrames[0].EndOfFile, Is.True);
            Assert.That(finalFrames[0].RequiresConnectionClose, Is.True);
            Assert.That(producer.StreamOctets, Is.EqualTo(4));
            Assert.That(consumer.StreamOctets, Is.EqualTo(4));
        });

        Assert.DoesNotThrow(() => producer.RequireEndOfFile());
        Assert.DoesNotThrow(() => consumer.RequireEndOfFile());
    }

    [Test]
    public void Record_structure_escapes_data_and_emits_combined_eor_eof_codes()
    {
        FtpStreamModeState state = CreateState(FtpFileStructure.Record, false, 0);

        FtpStreamModeChunk record = state.EncodeFrame(CreateFrame(
            new byte[] { 1, 0xFF, 2 }, true, false, false, false));
        FtpStreamModeChunk final = state.EncodeFrame(CreateFrame(
            new byte[] { 3, 0xFF }, true, true, false, true));

        Assert.Multiple(() =>
        {
            Assert.That(record.Data,
                Is.EqualTo(new byte[] { 1, 0xFF, 0xFF, 2, 0xFF, 1 }));
            Assert.That(final.Data,
                Is.EqualTo(new byte[] { 3, 0xFF, 0xFF, 0xFF, 3 }));
            Assert.That(record.CloseConnection, Is.False);
            Assert.That(final.CloseConnection, Is.False);
            Assert.That(state.StreamOctets, Is.EqualTo(11));
            Assert.That(state.EndOfFileReached, Is.True);
        });
    }

    [Test]
    public void Record_decoder_retains_escape_state_across_input_boundaries()
    {
        FtpStreamModeState state = CreateState(FtpFileStructure.Record, false, 0);

        FtpStructureFrame[] first = state.DecodeBytes(new byte[] { 1, 0xFF }, 0, 2, false);
        FtpStructureFrame[] second = state.DecodeBytes(
            new byte[] { 0xFF, 2, 0xFF, 1, 3, 0xFF }, 0, 6, false);
        FtpStructureFrame[] final = state.DecodeBytes(new byte[] { 3 }, 0, 1, true);

        Assert.Multiple(() =>
        {
            Assert.That(first, Has.Length.EqualTo(1));
            Assert.That(first[0].Data, Is.EqualTo(new byte[] { 1 }));
            Assert.That(second, Has.Length.EqualTo(2));
            Assert.That(second[0].Data, Is.EqualTo(new byte[] { 0xFF, 2 }));
            Assert.That(second[0].EndOfRecord, Is.True);
            Assert.That(second[1].Data, Is.EqualTo(new byte[] { 3 }));
            Assert.That(second[1].EndOfRecord, Is.False);
            Assert.That(final, Has.Length.EqualTo(1));
            Assert.That(final[0].Data, Is.Empty);
            Assert.That(final[0].EndOfRecord, Is.True);
            Assert.That(final[0].EndOfFile, Is.True);
            Assert.That(final[0].RequiresStreamEndOfFileMarker, Is.True);
            Assert.That(state.PendingEscape, Is.False);
        });

        Assert.DoesNotThrow(() => state.RequireEndOfFile());
    }

    [Test]
    public void Record_decoder_accepts_a_standalone_eof_control_for_an_empty_file()
    {
        FtpStreamModeState state = CreateState(FtpFileStructure.Record, false, 0);

        FtpStructureFrame[] frames = state.DecodeBytes(new byte[] { 0xFF, 2 }, 0, 2, true);

        Assert.Multiple(() =>
        {
            Assert.That(frames, Has.Length.EqualTo(1));
            Assert.That(frames[0].Count, Is.Zero);
            Assert.That(frames[0].EndOfRecord, Is.False);
            Assert.That(frames[0].EndOfFile, Is.True);
        });
    }

    [Test]
    public void Record_decoder_rejects_undefined_and_incomplete_control_sequences()
    {
        FtpStreamModeState undefined = CreateState(FtpFileStructure.Record, false, 0);
        FtpStreamModeState incomplete = CreateState(FtpFileStructure.Record, false, 0);
        FtpStreamModeState prematureClose = CreateState(FtpFileStructure.Record, false, 0);
        FtpStreamModeState trailingData = CreateState(FtpFileStructure.Record, false, 0);

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpTransferException>(() => undefined.DecodeBytes(
                new byte[] { 0xFF, 4 }, 0, 2, false));
            Assert.Throws<FtpTransferException>(() => incomplete.DecodeBytes(
                new byte[] { 0xFF }, 0, 1, true));
            Assert.Throws<FtpTransferException>(() => prematureClose.DecodeBytes(
                new byte[] { 1, 2 }, 0, 2, true));
            Assert.Throws<FtpTransferException>(() => trailingData.DecodeBytes(
                new byte[] { 0xFF, 2, 1 }, 0, 3, false));
        });
    }

    [Test]
    public void Page_structure_remains_raw_and_requires_a_last_page_eof()
    {
        FtpPageStructureState pageProducer = CreatePageState();
        FtpPageFrame pageFrame = pageProducer.EncodePage(CreatePage(
            new byte[] { 1 }, 1, new byte[0], new byte[] { 0xFF }));
        FtpPageFrame lastFrame = pageProducer.EncodePage(CreatePage(
            new byte[] { 0 }, 0, new byte[0], new byte[0]));
        FtpStreamModeState producer = CreateState(FtpFileStructure.Page, false, 0);
        FtpStreamModeChunk page = producer.EncodePage(pageFrame);
        FtpStreamModeChunk last = producer.EncodePage(lastFrame);
        FtpStreamModeState consumer = CreateState(FtpFileStructure.Page, false, 0);
        FtpStructureFrame[] received = consumer.DecodeBytes(page.Data, 0, page.Count, false);
        FtpPageStructureState pageConsumer = CreatePageState();
        pageConsumer.DecodePage(page.Data, 0, page.Count);
        FtpPageFrame decodedLast = pageConsumer.DecodePage(last.Data, 0, last.Count);
        consumer.CompletePageStructure(decodedLast);
        FtpStreamModeState prematureClose = CreateState(FtpFileStructure.Page, false, 0);
        FtpStreamModeState missingLastPage = CreateState(FtpFileStructure.Page, false, 0);

        Assert.Multiple(() =>
        {
            Assert.That(page.Data, Is.EqualTo(new byte[] { 4, 1, 1, 1, 0xFF }));
            Assert.That(last.Data, Is.EqualTo(new byte[] { 4, 0, 0, 0 }));
            Assert.That(last.CloseConnection, Is.False);
            Assert.That(received[0].Data, Is.EqualTo(page.Data));
            Assert.That(producer.EndOfFileReached, Is.True);
            Assert.That(consumer.EndOfFileReached, Is.True);
            Assert.Throws<FtpTransferException>(() => prematureClose.DecodeBytes(
                new byte[0], 0, 0, true));
            Assert.Throws<FtpTransferException>(() => missingLastPage.CompletePageStructure(pageFrame));
        });
    }

    [Test]
    public void Restart_offset_skips_framed_octets_before_emitting_file_data()
    {
        FtpStreamModeState state = CreateState(FtpFileStructure.File, true, 3);

        FtpStreamModeChunk skipped = state.EncodeFrame(CreateFrame(
            new byte[] { 0, 1 }, false, false, false, false));
        FtpStreamModeChunk remainder = state.EncodeFrame(CreateFrame(
            new byte[] { 2, 3, 4, 5 }, false, true, true, false));

        Assert.Multiple(() =>
        {
            Assert.That(skipped.Data, Is.Empty);
            Assert.That(skipped.CloseConnection, Is.False);
            Assert.That(remainder.Data, Is.EqualTo(new byte[] { 3, 4, 5 }));
            Assert.That(remainder.CloseConnection, Is.True);
            Assert.That(state.RestartBytesRemaining, Is.Zero);
            Assert.That(state.StreamOctets, Is.EqualTo(6));
            Assert.That(state.EmittedOctets, Is.EqualTo(3));
        });

        Assert.DoesNotThrow(() => state.RequireEndOfFile());
    }

    [Test]
    public void Restarted_decoder_reports_absolute_and_received_octet_counts()
    {
        FtpStreamModeState state = CreateState(FtpFileStructure.File, true, 5);

        FtpStructureFrame[] frames = state.DecodeBytes(new byte[] { 9, 8 }, 0, 2, true);

        Assert.Multiple(() =>
        {
            Assert.That(frames[0].Data, Is.EqualTo(new byte[] { 9, 8 }));
            Assert.That(frames[0].EndOfFile, Is.True);
            Assert.That(state.StreamOctets, Is.EqualTo(7));
            Assert.That(state.EmittedOctets, Is.EqualTo(2));
            Assert.That(state.RestartBytesRemaining, Is.Zero);
        });
    }

    [Test]
    public void Restart_offset_beyond_eof_is_rejected_when_the_transfer_completes()
    {
        FtpStreamModeState state = CreateState(FtpFileStructure.File, true, 4);

        Assert.Throws<FtpTransferException>(() => state.EncodeFrame(CreateFrame(
            new byte[] { 1, 2, 3 }, false, true, true, false)));

        Assert.Multiple(() =>
        {
            Assert.That(state.EndOfFileReached, Is.False);
            Assert.That(state.RestartBytesRemaining, Is.EqualTo(4));
            Assert.Throws<FtpTransferException>(() => state.RequireEndOfFile());
        });
    }

    [Test]
    public void Codec_and_state_reject_invalid_mode_restart_and_lifecycle_use()
    {
        FtpTransferOptions block = CreateOptions(FtpFileStructure.File);
        block.Mode = FtpTransferMode.Block;
        FtpStreamModeState completed = CreateState(FtpFileStructure.File, false, 0);
        completed.EncodeFrame(CreateFrame(new byte[0], false, true, true, false));
        FtpStreamModeState producer = CreateState(FtpFileStructure.File, false, 0);
        producer.EncodeFrame(CreateFrame(new byte[] { 1 }, false, false, false, false));
        FtpStreamModeState consumer = CreateState(FtpFileStructure.File, false, 0);
        consumer.DecodeBytes(new byte[] { 1 }, 0, 1, false);

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpConfigurationException>(() => StreamModeCodec.CreateFramingState(
                block, false, 0));
            Assert.Throws<FtpConfigurationException>(() => CreateState(
                FtpFileStructure.File, false, 1));
            Assert.Throws<FtpConfigurationException>(() => CreateState(
                FtpFileStructure.File, true, -1));
            Assert.Throws<FtpConfigurationException>(() => CreateState(
                FtpFileStructure.Record, true, 1));
            Assert.Throws<FtpTransferException>(() => completed.EncodeFrame(CreateFrame(
                new byte[] { 1 }, false, false, false, false)));
            Assert.Throws<FtpTransferException>(() => producer.DecodeBytes(
                new byte[] { 1 }, 0, 1, false));
            Assert.Throws<FtpTransferException>(() => consumer.EncodeFrame(CreateFrame(
                new byte[] { 1 }, false, false, false, false)));
        });
    }

    private static FtpStreamModeState CreateState(
        FtpFileStructure structure,
        bool hasRestartOffset,
        long restartOffset)
    {
        return StreamModeCodec.CreateFramingState(
            CreateOptions(structure), hasRestartOffset, restartOffset);
    }

    private static FtpTransferOptions CreateOptions(FtpFileStructure structure)
    {
        FtpTransferOptions options = new FtpTransferOptions();
        options.Structure = structure;
        options.Mode = FtpTransferMode.Stream;
        if (structure == FtpFileStructure.Record)
        {
            options.Type = FtpTransferType.Ascii;
        }

        return options;
    }

    private static FtpPageStructureState CreatePageState()
    {
        FtpTransferOptions options = CreateOptions(FtpFileStructure.Page);
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

    private static FtpStructureFrame CreateFrame(
        byte[] data,
        bool endOfRecord,
        bool endOfFile,
        bool connectionClose,
        bool streamMarker)
    {
        FtpStructureFrame frame = new FtpStructureFrame();
        frame.Data = data;
        frame.Count = data.Length;
        frame.EndOfRecord = endOfRecord;
        frame.EndOfFile = endOfFile;
        frame.RequiresConnectionClose = connectionClose;
        frame.RequiresStreamEndOfFileMarker = streamMarker;
        return frame;
    }
}
