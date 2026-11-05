namespace ftp.Tests;

public class FileStructureCodecTests
{
    [TestCase(FtpTransferMode.Stream)]
    [TestCase(FtpTransferMode.Deflate)]
    public void Stream_based_modes_pass_data_and_signal_eof_by_connection_close(FtpTransferMode mode)
    {
        FtpFileStructureState state = CreateState(mode);
        byte[] source = new byte[] { 9, 1, 2, 3, 8 };

        FtpStructureFrame first = state.FrameData(source, 1, 2);
        source[1] = 99;
        FtpStructureFrame final = state.FrameFinalData(source, 3, 1);

        Assert.Multiple(() =>
        {
            Assert.That(first.Data, Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(first.EndOfRecord, Is.False);
            Assert.That(first.EndOfFile, Is.False);
            Assert.That(first.RequiresConnectionClose, Is.False);
            Assert.That(first.RequiresEndOfFileDescriptor, Is.False);
            Assert.That(final.Data, Is.EqualTo(new byte[] { 3 }));
            Assert.That(final.EndOfFile, Is.True);
            Assert.That(final.RequiresConnectionClose, Is.True);
            Assert.That(final.RequiresEndOfFileDescriptor, Is.False);
            Assert.That(state.DataBytes, Is.EqualTo(3));
            Assert.That(state.IsComplete(), Is.True);
        });
    }

    [TestCase(FtpTransferMode.Block)]
    [TestCase(FtpTransferMode.Compressed)]
    public void Descriptor_modes_request_explicit_eof_without_connection_close(FtpTransferMode mode)
    {
        FtpFileStructureState state = CreateState(mode);

        FtpStructureFrame data = state.FrameData(new byte[] { 1, 2 }, 0, 2);
        FtpStructureFrame eof = state.CompleteFile();

        Assert.Multiple(() =>
        {
            Assert.That(data.EndOfFile, Is.False);
            Assert.That(eof.Count, Is.Zero);
            Assert.That(eof.Data, Is.Empty);
            Assert.That(eof.EndOfRecord, Is.False);
            Assert.That(eof.EndOfFile, Is.True);
            Assert.That(eof.RequiresConnectionClose, Is.False);
            Assert.That(eof.RequiresEndOfFileDescriptor, Is.True);
            Assert.That(state.DataBytes, Is.EqualTo(2));
            Assert.That(state.IsComplete(), Is.True);
        });
    }

    [Test]
    public void Consumer_accepts_fragmented_file_data_and_mode_correct_eof()
    {
        FtpFileStructureState state = CreateState(FtpTransferMode.Block);
        FtpStructureFrame first = CreateFrame(new byte[] { 1, 2 }, false, false, false, false);
        FtpStructureFrame second = CreateFrame(new byte[] { 3 }, false, true, false, true);

        state.ConsumeFrame(first);
        state.ConsumeFrame(second);

        Assert.Multiple(() =>
        {
            Assert.That(state.DataBytes, Is.EqualTo(3));
            Assert.That(state.IsComplete(), Is.True);
        });
    }

    [Test]
    public void Empty_files_have_the_selected_modes_eof_semantics()
    {
        FtpFileStructureState stream = CreateState(FtpTransferMode.Stream);
        FtpFileStructureState compressed = CreateState(FtpTransferMode.Compressed);

        FtpStructureFrame streamEnd = stream.CompleteFile();
        FtpStructureFrame compressedEnd = compressed.CompleteFile();

        Assert.Multiple(() =>
        {
            Assert.That(streamEnd.Count, Is.Zero);
            Assert.That(streamEnd.RequiresConnectionClose, Is.True);
            Assert.That(compressedEnd.Count, Is.Zero);
            Assert.That(compressedEnd.RequiresEndOfFileDescriptor, Is.True);
        });
    }

    [Test]
    public void Consumer_rejects_eor_and_incorrect_eof_signaling()
    {
        FtpFileStructureState eorState = CreateState(FtpTransferMode.Block);
        FtpStructureFrame eor = CreateFrame(new byte[] { 1 }, true, false, false, false);
        FtpFileStructureState streamState = CreateState(FtpTransferMode.Stream);
        FtpStructureFrame streamDescriptor = CreateFrame(new byte[0], false, true, false, true);
        FtpFileStructureState blockState = CreateState(FtpTransferMode.Block);
        FtpStructureFrame blockClose = CreateFrame(new byte[0], false, true, true, false);
        FtpFileStructureState prematureSignalState = CreateState(FtpTransferMode.Stream);
        FtpStructureFrame prematureSignal = CreateFrame(new byte[] { 1 }, false, false, true, false);

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpTransferException>(() => eorState.ConsumeFrame(eor));
            Assert.Throws<FtpTransferException>(() => streamState.ConsumeFrame(streamDescriptor));
            Assert.Throws<FtpTransferException>(() => blockState.ConsumeFrame(blockClose));
            Assert.Throws<FtpTransferException>(() => prematureSignalState.ConsumeFrame(prematureSignal));
        });
    }

    [Test]
    public void State_rejects_frames_after_eof_and_mixed_producer_consumer_use()
    {
        FtpFileStructureState completedProducer = CreateState(FtpTransferMode.Stream);
        completedProducer.CompleteFile();
        FtpFileStructureState completedConsumer = CreateState(FtpTransferMode.Stream);
        completedConsumer.ConsumeFrame(CreateFrame(new byte[0], false, true, true, false));
        FtpFileStructureState producer = CreateState(FtpTransferMode.Stream);
        producer.FrameData(new byte[] { 1 }, 0, 1);
        FtpFileStructureState consumer = CreateState(FtpTransferMode.Stream);
        consumer.ConsumeFrame(CreateFrame(new byte[] { 1 }, false, false, false, false));

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpTransferException>(() => completedProducer.FrameData(new byte[] { 1 }, 0, 1));
            Assert.Throws<FtpTransferException>(() => completedConsumer.ConsumeFrame(CreateFrame(new byte[] { 1 }, false, false, false, false)));
            Assert.Throws<FtpTransferException>(() => producer.ConsumeFrame(CreateFrame(new byte[] { 2 }, false, false, false, false)));
            Assert.Throws<FtpTransferException>(() => consumer.FrameData(new byte[] { 2 }, 0, 1));
        });
    }

    [Test]
    public void Framer_validates_structure_data_slices_and_count_overflow()
    {
        FtpTransferOptions recordOptions = new FtpTransferOptions();
        recordOptions.Structure = FtpFileStructure.Record;
        FtpTransferOptions invalidMode = new FtpTransferOptions();
        invalidMode.Mode = (FtpTransferMode)99;
        FtpFileStructureState state = CreateState(FtpTransferMode.Stream);
        FtpFileStructureState overflow = CreateState(FtpTransferMode.Stream);
        overflow.DataBytes = long.MaxValue;
        FtpFileStructureState malformedReceiver = CreateState(FtpTransferMode.Stream);
        FtpStructureFrame malformed = new FtpStructureFrame();
        malformed.Data = null;
        malformed.Count = 1;

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpConfigurationException>(() => FileStructureCodec.CreateFramingState(recordOptions));
            Assert.Throws<FtpConfigurationException>(() => FileStructureCodec.CreateFramingState(invalidMode));
            Assert.Throws<FtpConfigurationException>(() => state.FrameData(new byte[] { 1 }, 0, 0));
            Assert.Throws<FtpConfigurationException>(() => state.FrameData(new byte[] { 1 }, 1, 1));
            Assert.Throws<FtpConfigurationException>(() => state.FrameData(null, 0, 1));
            Assert.Throws<FtpTransferException>(() => overflow.FrameData(new byte[] { 1 }, 0, 1));
            Assert.Throws<FtpTransferException>(() => malformedReceiver.ConsumeFrame(malformed));
        });
    }

    private static FtpFileStructureState CreateState(FtpTransferMode mode)
    {
        FtpTransferOptions options = new FtpTransferOptions();
        options.Structure = FtpFileStructure.File;
        options.Mode = mode;
        return FileStructureCodec.CreateFramingState(options);
    }

    private static FtpStructureFrame CreateFrame(
        byte[] data,
        bool endOfRecord,
        bool endOfFile,
        bool connectionClose,
        bool eofDescriptor)
    {
        FtpStructureFrame frame = new FtpStructureFrame();
        frame.Data = data;
        frame.Count = data.Length;
        frame.EndOfRecord = endOfRecord;
        frame.EndOfFile = endOfFile;
        frame.RequiresConnectionClose = connectionClose;
        frame.RequiresEndOfFileDescriptor = eofDescriptor;
        return frame;
    }
}
