namespace ftp.Tests;

public class RestartMarkerCodecTests
{
    [Test]
    public void Generated_markers_round_trip_all_checkpoint_state()
    {
        FtpRestartCheckpoint checkpoint = new FtpRestartCheckpoint();
        checkpoint.LogicalByteOffset = 123;
        checkpoint.WireByteOffset = 456;
        checkpoint.StructureUnitCount = 7;
        checkpoint.PendingBitCount = 3;
        checkpoint.PendingCarriageReturn = true;
        RestartMarkerCodec producer = new RestartMarkerCodec();

        string marker = producer.GenerateMarker(checkpoint);
        checkpoint.LogicalByteOffset = 999;
        FtpRestartCheckpoint? retained = producer.GetLatestCheckpoint();
        RestartMarkerCodec consumer = new RestartMarkerCodec();
        FtpRestartCheckpoint parsed = consumer.ParseMarker(marker);

        Assert.Multiple(() =>
        {
            Assert.That(marker, Is.EqualTo("F1:123:456:7:3:1"));
            Assert.That(producer.GetLatestMarker(), Is.EqualTo(marker));
            Assert.That(retained, Is.Not.Null);
            Assert.That(retained!.LogicalByteOffset, Is.EqualTo(123));
            Assert.That(parsed.LogicalByteOffset, Is.EqualTo(123));
            Assert.That(parsed.WireByteOffset, Is.EqualTo(456));
            Assert.That(parsed.StructureUnitCount, Is.EqualTo(7));
            Assert.That(parsed.PendingBitCount, Is.EqualTo(3));
            Assert.That(parsed.PendingCarriageReturn, Is.True);
            Assert.That(consumer.GetLatestMarker(), Is.EqualTo(marker));
        });
    }

    [Test]
    public void Parser_accepts_maximum_offsets_and_returns_defensive_copies()
    {
        string maximum = "9223372036854775807";
        string marker = "F1:" + maximum + ":" + maximum + ":" + maximum + ":7:0";
        RestartMarkerCodec codec = new RestartMarkerCodec();

        FtpRestartCheckpoint parsed = codec.ParseMarker(marker);
        FtpRestartCheckpoint? first = codec.GetLatestCheckpoint();
        first!.WireByteOffset = 1;
        FtpRestartCheckpoint? second = codec.GetLatestCheckpoint();

        Assert.Multiple(() =>
        {
            Assert.That(parsed.LogicalByteOffset, Is.EqualTo(long.MaxValue));
            Assert.That(parsed.WireByteOffset, Is.EqualTo(long.MaxValue));
            Assert.That(parsed.StructureUnitCount, Is.EqualTo(long.MaxValue));
            Assert.That(second, Is.Not.Null);
            Assert.That(second!.WireByteOffset, Is.EqualTo(long.MaxValue));
            Assert.That(second.PendingCarriageReturn, Is.False);
        });
    }

    [Test]
    public void Opaque_markers_are_reported_without_being_interpreted()
    {
        RestartMarkerCodec codec = new RestartMarkerCodec();

        string accepted = codec.AcceptOpaqueMarker("SERVER-CHECKPOINT/42", 64);

        Assert.Multiple(() =>
        {
            Assert.That(accepted, Is.EqualTo("SERVER-CHECKPOINT/42"));
            Assert.That(codec.GetLatestMarker(), Is.EqualTo(accepted));
            Assert.That(codec.GetLatestCheckpoint(), Is.Null);
            Assert.Throws<FtpTransferException>(() => codec.AcceptOpaqueMarker("HAS SPACE", 64));
            Assert.Throws<FtpTransferException>(() => codec.AcceptOpaqueMarker("", 64));
            Assert.Throws<FtpTransferException>(() => codec.AcceptOpaqueMarker("TOO-LONG", 3));
            Assert.Throws<FtpConfigurationException>(() => codec.AcceptOpaqueMarker("A", 0));
        });
    }

    [Test]
    public void Invalid_generated_marker_formats_are_rejected_without_replacing_latest()
    {
        RestartMarkerCodec codec = new RestartMarkerCodec();
        codec.AcceptOpaqueMarker("CURRENT", 20);

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpTransferException>(() => codec.ParseMarker("F2:1:2:3:4:0"));
            Assert.Throws<FtpTransferException>(() => codec.ParseMarker("F1:1:2:3:8:0"));
            Assert.Throws<FtpTransferException>(() => codec.ParseMarker("F1:1:2:3:0:2"));
            Assert.Throws<FtpTransferException>(() => codec.ParseMarker("F1:1:x:3:0:0"));
            Assert.Throws<FtpTransferException>(() => codec.ParseMarker(
                "F1:9223372036854775808:2:3:0:0"));
            Assert.That(codec.GetLatestMarker(), Is.EqualTo("CURRENT"));
        });
    }

    [Test]
    public void Generator_rejects_invalid_checkpoint_values()
    {
        FtpRestartCheckpoint negative = new FtpRestartCheckpoint();
        negative.LogicalByteOffset = -1;
        FtpRestartCheckpoint pendingBits = new FtpRestartCheckpoint();
        pendingBits.PendingBitCount = 8;
        RestartMarkerCodec codec = new RestartMarkerCodec();

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpConfigurationException>(() => codec.GenerateMarker(null));
            Assert.Throws<FtpConfigurationException>(() => codec.GenerateMarker(negative));
            Assert.Throws<FtpConfigurationException>(() => codec.GenerateMarker(pendingBits));
        });
    }

    [Test]
    public void Block_mode_reports_the_latest_emitted_and_received_marker()
    {
        string marker = CreateGeneratedMarker();
        FtpBlockModeState producer = CreateBlockState();
        FtpBlockModeBlock markerBlock = producer.EncodeRestartMarker(marker);
        FtpBlockModeBlock eof = producer.EncodeFrame(CreateEofFrame(), false)[0];
        byte[] wire = Join(markerBlock.SerializedBytes, eof.SerializedBytes);
        FtpBlockModeState consumer = CreateBlockState();

        consumer.DecodeBytes(wire, 0, wire.Length, true);

        Assert.Multiple(() =>
        {
            Assert.That(producer.RestartMarkers.GetLatestMarker(), Is.EqualTo(marker));
            Assert.That(consumer.RestartMarkers.GetLatestMarker(), Is.EqualTo(marker));
            Assert.That(consumer.RestartMarkers.GetLatestCheckpoint(), Is.Null);
        });
    }

    [Test]
    public void Compressed_mode_reports_the_latest_emitted_and_received_marker()
    {
        string marker = CreateGeneratedMarker();
        FtpCompressedModeState producer = CreateCompressedState();
        FtpCompressedModeSegment markerSegment = producer.EncodeRestartMarker(marker);
        FtpCompressedModeSegment eof = producer.EncodeFrame(CreateEofFrame(), false)[0];
        byte[] wire = Join(markerSegment.EncodedBytes, eof.EncodedBytes);
        FtpCompressedModeState consumer = CreateCompressedState();

        consumer.DecodeBytes(wire, 0, wire.Length, true);

        Assert.Multiple(() =>
        {
            Assert.That(producer.RestartMarkers.GetLatestMarker(), Is.EqualTo(marker));
            Assert.That(consumer.RestartMarkers.GetLatestMarker(), Is.EqualTo(marker));
            Assert.That(consumer.RestartMarkers.GetLatestCheckpoint(), Is.Null);
        });
    }

    [Test]
    public void Transfer_option_combinations_follow_representation_structure_and_mode_rules()
    {
        FtpTransferOptions recordStream = CreateOptions(
            FtpTransferType.Ascii, FtpFileStructure.Record, FtpTransferMode.Stream, 0);
        FtpTransferOptions recordBlock = CreateOptions(
            FtpTransferType.Ebcdic, FtpFileStructure.Record, FtpTransferMode.Block, 0);
        FtpTransferOptions recordCompressed = CreateOptions(
            FtpTransferType.Ascii, FtpFileStructure.Record, FtpTransferMode.Compressed, 0);
        FtpTransferOptions recordImage = CreateOptions(
            FtpTransferType.Image, FtpFileStructure.Record, FtpTransferMode.Stream, 0);
        FtpTransferOptions recordDeflate = CreateOptions(
            FtpTransferType.Ascii, FtpFileStructure.Record, FtpTransferMode.Deflate, 0);
        FtpTransferOptions pageDeflate = CreateOptions(
            FtpTransferType.Image, FtpFileStructure.Page, FtpTransferMode.Deflate, 0);
        FtpTransferOptions narrowPage = CreateOptions(
            FtpTransferType.LocalByte, FtpFileStructure.Page, FtpTransferMode.Block, 2);
        FtpTransferOptions validPage = CreateOptions(
            FtpTransferType.LocalByte, FtpFileStructure.Page, FtpTransferMode.Compressed, 3);
        FtpTransferOptions fileDeflate = CreateOptions(
            FtpTransferType.Image, FtpFileStructure.File, FtpTransferMode.Deflate, 0);

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(() => FtpTransferValidator.ValidateTransferOptions(recordStream));
            Assert.DoesNotThrow(() => FtpTransferValidator.ValidateTransferOptions(recordBlock));
            Assert.DoesNotThrow(() => FtpTransferValidator.ValidateTransferOptions(recordCompressed));
            Assert.Throws<FtpConfigurationException>(() => FtpTransferValidator.ValidateTransferOptions(recordImage));
            Assert.DoesNotThrow(() => FtpTransferValidator.ValidateTransferOptions(recordDeflate));
            Assert.DoesNotThrow(() => FtpTransferValidator.ValidateTransferOptions(pageDeflate));
            Assert.Throws<FtpConfigurationException>(() => FtpTransferValidator.ValidateTransferOptions(narrowPage));
            Assert.DoesNotThrow(() => FtpTransferValidator.ValidateTransferOptions(validPage));
            Assert.DoesNotThrow(() => FtpTransferValidator.ValidateTransferOptions(fileDeflate));
        });
    }

    [Test]
    public void Restart_offsets_follow_stream_file_and_mode_z_rules()
    {
        FtpTransferRequest streamFile = CreateRequest(CreateOptions(
            FtpTransferType.Image, FtpFileStructure.File, FtpTransferMode.Stream, 0));
        FtpTransferRequest streamRecord = CreateRequest(CreateOptions(
            FtpTransferType.Ascii, FtpFileStructure.Record, FtpTransferMode.Stream, 0));
        FtpTransferRequest blockRecord = CreateRequest(CreateOptions(
            FtpTransferType.Ascii, FtpFileStructure.Record, FtpTransferMode.Block, 0));
        FtpTransferRequest compressedFile = CreateRequest(CreateOptions(
            FtpTransferType.Image, FtpFileStructure.File, FtpTransferMode.Compressed, 0));
        FtpTransferRequest deflateFile = CreateRequest(CreateOptions(
            FtpTransferType.Image, FtpFileStructure.File, FtpTransferMode.Deflate, 0));

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(() => FtpTransferValidator.ValidateTransferRequest(streamFile));
            Assert.Throws<FtpConfigurationException>(() => FtpTransferValidator.ValidateTransferRequest(streamRecord));
            Assert.DoesNotThrow(() => FtpTransferValidator.ValidateTransferRequest(blockRecord));
            Assert.DoesNotThrow(() => FtpTransferValidator.ValidateTransferRequest(compressedFile));
            Assert.DoesNotThrow(() => FtpTransferValidator.ValidateTransferRequest(deflateFile));
        });

        streamFile.Content!.Dispose();
        streamRecord.Content!.Dispose();
        blockRecord.Content!.Dispose();
        compressedFile.Content!.Dispose();
        deflateFile.Content!.Dispose();
    }

    private static string CreateGeneratedMarker()
    {
        FtpRestartCheckpoint checkpoint = new FtpRestartCheckpoint();
        checkpoint.LogicalByteOffset = 10;
        checkpoint.WireByteOffset = 12;
        checkpoint.StructureUnitCount = 2;
        RestartMarkerCodec codec = new RestartMarkerCodec();
        return codec.GenerateMarker(checkpoint);
    }

    private static FtpBlockModeState CreateBlockState()
    {
        FtpTransferOptions options = CreateOptions(
            FtpTransferType.Image, FtpFileStructure.File, FtpTransferMode.Block, 0);
        return BlockModeCodec.CreateFramingState(options);
    }

    private static FtpCompressedModeState CreateCompressedState()
    {
        FtpTransferOptions options = CreateOptions(
            FtpTransferType.Image, FtpFileStructure.File, FtpTransferMode.Compressed, 0);
        return CompressedModeCodec.CreateFramingState(options);
    }

    private static FtpStructureFrame CreateEofFrame()
    {
        FtpStructureFrame frame = new FtpStructureFrame();
        frame.Data = new byte[0];
        frame.Count = 0;
        frame.EndOfFile = true;
        frame.RequiresEndOfFileDescriptor = true;
        return frame;
    }

    private static FtpTransferOptions CreateOptions(
        FtpTransferType type,
        FtpFileStructure structure,
        FtpTransferMode mode,
        int localByteSize)
    {
        FtpTransferOptions options = new FtpTransferOptions();
        options.Type = type;
        options.Structure = structure;
        options.Mode = mode;
        if (type == FtpTransferType.LocalByte)
        {
            options.HasLocalByteSize = true;
            options.LocalByteSize = localByteSize;
        }

        return options;
    }

    private static FtpTransferRequest CreateRequest(FtpTransferOptions options)
    {
        FtpTransferRequest request = new FtpTransferRequest();
        request.RemotePath = "/remote";
        request.Content = new MemoryStream();
        request.Options = options;
        request.HasRestartOffset = true;
        request.RestartOffset = 5;
        return request;
    }

    private static byte[] Join(byte[] first, byte[] second)
    {
        byte[] joined = new byte[first.Length + second.Length];
        System.Array.Copy(first, 0, joined, 0, first.Length);
        System.Array.Copy(second, 0, joined, first.Length, second.Length);
        return joined;
    }
}
