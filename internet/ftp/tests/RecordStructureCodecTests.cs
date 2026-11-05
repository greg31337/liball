namespace ftp.Tests;

public class RecordStructureCodecTests
{
    [Test]
    public void Ascii_record_conversion_retains_crlf_state_across_chunks()
    {
        FtpRecordStructureState producer = CreateState(FtpTransferMode.Stream, FtpTransferType.Ascii);
        List<FtpStructureFrame> frames = new List<FtpStructureFrame>();
        frames.AddRange(producer.ConvertData(new byte[] { 65, 13 }, 0, 2));
        frames.AddRange(producer.ConvertData(new byte[] { 10, 66, 13, 10, 13 }, 0, 5));
        frames.AddRange(producer.ConvertData(new byte[] { 10, 67 }, 0, 2));
        frames.AddRange(producer.CompleteRecords());

        FtpRecordStructureState consumer = CreateState(FtpTransferMode.Stream, FtpTransferType.Ascii);
        MemoryStream converted = new MemoryStream();
        int index = 0;
        while (index < frames.Count)
        {
            byte[] bytes = consumer.ConvertFrame(frames[index]);
            converted.Write(bytes, 0, bytes.Length);
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(converted.ToArray(), Is.EqualTo(new byte[] { 65, 13, 10, 66, 13, 10, 13, 10, 67, 13, 10 }));
            Assert.That(producer.RecordCount, Is.EqualTo(4));
            Assert.That(consumer.RecordCount, Is.EqualTo(4));
            Assert.That(producer.DataBytes, Is.EqualTo(3));
            Assert.That(consumer.DataBytes, Is.EqualTo(3));
            Assert.That(producer.IsComplete(), Is.True);
            Assert.That(consumer.IsComplete(), Is.True);
        });
    }

    [Test]
    public void Ebcdic_new_line_bytes_convert_to_explicit_record_boundaries()
    {
        byte[] represented = new byte[] { 0xC1, 0x15, 0xC2, 0x15, 0x15, 0xC3 };
        FtpRecordStructureState producer = CreateState(FtpTransferMode.Block, FtpTransferType.Ebcdic);
        List<FtpStructureFrame> frames = new List<FtpStructureFrame>();
        int index = 0;
        while (index < represented.Length)
        {
            frames.AddRange(producer.ConvertData(represented, index, 1));
            index++;
        }

        frames.AddRange(producer.CompleteRecords());
        FtpRecordStructureState consumer = CreateState(FtpTransferMode.Block, FtpTransferType.Ebcdic);
        MemoryStream converted = new MemoryStream();
        index = 0;
        while (index < frames.Count)
        {
            byte[] bytes = consumer.ConvertFrame(frames[index]);
            converted.Write(bytes, 0, bytes.Length);
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(converted.ToArray(), Is.EqualTo(new byte[] { 0xC1, 0x15, 0xC2, 0x15, 0x15, 0xC3, 0x15 }));
            Assert.That(producer.RecordCount, Is.EqualTo(4));
            Assert.That(consumer.RecordCount, Is.EqualTo(4));
            Assert.That(producer.DataBytes, Is.EqualTo(3));
        });
    }

    [Test]
    public void Stream_mode_uses_explicit_eor_and_stream_eof_markers()
    {
        FtpRecordStructureState state = CreateState(FtpTransferMode.Stream, FtpTransferType.Ascii);

        FtpStructureFrame[] data = state.ConvertData(new byte[] { 65 }, 0, 1);
        FtpStructureFrame eof = state.CompleteRecords()[0];

        Assert.Multiple(() =>
        {
            Assert.That(data, Has.Length.EqualTo(1));
            Assert.That(data[0].EndOfRecord, Is.False);
            Assert.That(eof.EndOfRecord, Is.True);
            Assert.That(eof.EndOfFile, Is.True);
            Assert.That(eof.RequiresConnectionClose, Is.False);
            Assert.That(eof.RequiresStreamEndOfFileMarker, Is.True);
            Assert.That(eof.RequiresEndOfFileDescriptor, Is.False);
        });
    }

    [TestCase(FtpTransferMode.Block)]
    [TestCase(FtpTransferMode.Compressed)]
    public void Descriptor_modes_use_eor_and_eof_flags(FtpTransferMode mode)
    {
        FtpRecordStructureState state = CreateState(mode, FtpTransferType.Ascii);

        FtpStructureFrame record = state.ConvertData(new byte[] { 65, 13, 10 }, 0, 3)[0];
        FtpStructureFrame eof = state.CompleteRecords()[0];

        Assert.Multiple(() =>
        {
            Assert.That(record.Data, Is.EqualTo(new byte[] { 65 }));
            Assert.That(record.EndOfRecord, Is.True);
            Assert.That(record.EndOfFile, Is.False);
            Assert.That(eof.EndOfRecord, Is.False);
            Assert.That(eof.EndOfFile, Is.True);
            Assert.That(eof.RequiresConnectionClose, Is.False);
            Assert.That(eof.RequiresStreamEndOfFileMarker, Is.False);
            Assert.That(eof.RequiresEndOfFileDescriptor, Is.True);
            Assert.That(state.RecordCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void Empty_files_and_empty_records_remain_distinct()
    {
        FtpRecordStructureState emptyFile = CreateState(FtpTransferMode.Stream, FtpTransferType.Ascii);
        FtpStructureFrame emptyFileEnd = emptyFile.CompleteRecords()[0];
        FtpRecordStructureState emptyRecord = CreateState(FtpTransferMode.Stream, FtpTransferType.Ascii);
        FtpStructureFrame record = emptyRecord.ConvertData(new byte[] { 13, 10 }, 0, 2)[0];
        FtpStructureFrame recordEnd = emptyRecord.CompleteRecords()[0];

        Assert.Multiple(() =>
        {
            Assert.That(emptyFileEnd.EndOfRecord, Is.False);
            Assert.That(emptyFile.RecordCount, Is.Zero);
            Assert.That(record.Count, Is.Zero);
            Assert.That(record.EndOfRecord, Is.True);
            Assert.That(recordEnd.EndOfRecord, Is.False);
            Assert.That(emptyRecord.RecordCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void Consumer_rejects_missing_final_eor_and_wrong_eof_signals()
    {
        FtpRecordStructureState missingEorState = CreateState(FtpTransferMode.Stream, FtpTransferType.Ascii);
        missingEorState.ConvertFrame(CreateFrame(new byte[] { 65 }, false, false, false, false, false));
        FtpStructureFrame missingEor = CreateFrame(new byte[0], false, true, false, true, false);
        FtpRecordStructureState connectionCloseState = CreateState(FtpTransferMode.Stream, FtpTransferType.Ascii);
        FtpStructureFrame connectionClose = CreateFrame(new byte[0], false, true, true, false, false);
        FtpRecordStructureState blockMarkerState = CreateState(FtpTransferMode.Block, FtpTransferType.Ascii);
        FtpStructureFrame streamMarker = CreateFrame(new byte[0], false, true, false, true, false);

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpTransferException>(() => missingEorState.ConvertFrame(missingEor));
            Assert.Throws<FtpTransferException>(() => connectionCloseState.ConvertFrame(connectionClose));
            Assert.Throws<FtpTransferException>(() => blockMarkerState.ConvertFrame(streamMarker));
        });
    }

    [Test]
    public void State_rejects_data_after_eof_and_mixed_conversion_directions()
    {
        FtpRecordStructureState completedProducer = CreateState(FtpTransferMode.Stream, FtpTransferType.Ascii);
        completedProducer.CompleteRecords();
        FtpRecordStructureState completedConsumer = CreateState(FtpTransferMode.Stream, FtpTransferType.Ascii);
        completedConsumer.ConvertFrame(CreateFrame(new byte[0], false, true, false, true, false));
        FtpRecordStructureState producer = CreateState(FtpTransferMode.Stream, FtpTransferType.Ascii);
        producer.ConvertData(new byte[] { 65 }, 0, 1);
        FtpRecordStructureState consumer = CreateState(FtpTransferMode.Stream, FtpTransferType.Ascii);
        consumer.ConvertFrame(CreateFrame(new byte[] { 65 }, false, false, false, false, false));

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpTransferException>(() => completedProducer.ConvertData(new byte[] { 65 }, 0, 1));
            Assert.Throws<FtpTransferException>(() => completedConsumer.ConvertFrame(CreateFrame(new byte[] { 65 }, false, false, false, false, false)));
            Assert.Throws<FtpTransferException>(() => producer.ConvertFrame(CreateFrame(new byte[] { 65 }, false, false, false, false, false)));
            Assert.Throws<FtpTransferException>(() => consumer.ConvertData(new byte[] { 65 }, 0, 1));
        });
    }

    [Test]
    public void Codec_validates_structure_mode_representation_and_data_bounds()
    {
        FtpTransferOptions file = CreateOptions(FtpTransferMode.Stream, FtpTransferType.Ascii);
        file.Structure = FtpFileStructure.File;
        FtpTransferOptions deflate = CreateOptions(FtpTransferMode.Deflate, FtpTransferType.Ascii);
        FtpTransferOptions image = CreateOptions(FtpTransferMode.Stream, FtpTransferType.Image);
        FtpRecordStructureState state = CreateState(FtpTransferMode.Stream, FtpTransferType.Ascii);
        FtpRecordStructureState malformedState = CreateState(FtpTransferMode.Stream, FtpTransferType.Ascii);
        FtpStructureFrame malformed = new FtpStructureFrame();
        malformed.Data = null;
        malformed.Count = 1;

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpConfigurationException>(() => RecordStructureCodec.CreateFramingState(file));
            Assert.DoesNotThrow(() => RecordStructureCodec.CreateFramingState(deflate));
            Assert.Throws<FtpConfigurationException>(() => RecordStructureCodec.CreateFramingState(image));
            Assert.Throws<FtpConfigurationException>(() => state.ConvertData(new byte[] { 1 }, 1, 1));
            Assert.Throws<FtpTransferException>(() => malformedState.ConvertFrame(malformed));
        });
    }

    private static FtpRecordStructureState CreateState(FtpTransferMode mode, FtpTransferType type)
    {
        return RecordStructureCodec.CreateFramingState(CreateOptions(mode, type));
    }

    private static FtpTransferOptions CreateOptions(FtpTransferMode mode, FtpTransferType type)
    {
        FtpTransferOptions options = new FtpTransferOptions();
        options.Structure = FtpFileStructure.Record;
        options.Mode = mode;
        options.Type = type;
        return options;
    }

    private static FtpStructureFrame CreateFrame(
        byte[] data,
        bool endOfRecord,
        bool endOfFile,
        bool connectionClose,
        bool streamMarker,
        bool eofDescriptor)
    {
        FtpStructureFrame frame = new FtpStructureFrame();
        frame.Data = data;
        frame.Count = data.Length;
        frame.EndOfRecord = endOfRecord;
        frame.EndOfFile = endOfFile;
        frame.RequiresConnectionClose = connectionClose;
        frame.RequiresStreamEndOfFileMarker = streamMarker;
        frame.RequiresEndOfFileDescriptor = eofDescriptor;
        return frame;
    }
}
