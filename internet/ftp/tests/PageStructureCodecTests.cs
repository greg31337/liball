namespace ftp.Tests;

public class PageStructureCodecTests
{
    [Test]
    public void Simple_pages_encode_headers_indexes_and_restart_locations()
    {
        FtpPageStructureState state = CreateState(FtpTransferType.Image, 0);

        FtpPageFrame first = state.EncodePage(CreatePage(
            new byte[] { 7 }, 1, new byte[0], new byte[] { 0xAA, 0xBB, 0xCC }));
        FtpPageFrame second = state.EncodePage(CreatePage(
            new byte[] { 9 }, 1, new byte[0], new byte[] { 0xDD }));
        FtpPageFrame last = state.EncodePage(CreatePage(
            new byte[] { 0 }, 0, new byte[0], new byte[0]));
        FtpPageRestartLocation? firstRestart = state.FindRestartLocation(new byte[] { 7 });
        FtpPageRestartLocation? secondRestart = state.FindRestartLocation(new byte[] { 9 });

        Assert.Multiple(() =>
        {
            Assert.That(first.SerializedLogicalBytes,
                Is.EqualTo(new byte[] { 4, 7, 3, 1, 0xAA, 0xBB, 0xCC }));
            Assert.That(second.SerializedLogicalBytes,
                Is.EqualTo(new byte[] { 4, 9, 1, 1, 0xDD }));
            Assert.That(last.SerializedLogicalBytes, Is.EqualTo(new byte[] { 4, 0, 0, 0 }));
            Assert.That(firstRestart, Is.Not.Null);
            Assert.That(firstRestart!.LogicalByteOffset, Is.Zero);
            Assert.That(secondRestart, Is.Not.Null);
            Assert.That(secondRestart!.LogicalByteOffset, Is.EqualTo(7));
            Assert.That(last.RestartLocation, Is.Null);
            Assert.That(state.FindRestartLocation(new byte[] { 8 }), Is.Null);
            Assert.That(state.NextLogicalByteOffset, Is.EqualTo(16));
            Assert.That(state.EndOfFileReached, Is.True);
        });

        Assert.DoesNotThrow(() => state.RequireEndOfFile());
    }

    [Test]
    public void Decoder_recovers_page_fields_and_restart_locations()
    {
        FtpPageStructureState state = CreateState(FtpTransferType.Image, 0);

        FtpPageFrame first = state.DecodePage(
            new byte[] { 4, 3, 2, 1, 10, 11 }, 0, 6);
        FtpPageFrame last = state.DecodePage(
            new byte[] { 99, 4, 0, 0, 0, 88 }, 1, 4);

        Assert.Multiple(() =>
        {
            Assert.That(first.HeaderLogicalByteCount, Is.EqualTo(4));
            Assert.That(first.DataLogicalByteCount, Is.EqualTo(2));
            Assert.That(first.PageIndex, Is.EqualTo(new byte[] { 3 }));
            Assert.That(first.PageType, Is.EqualTo(1));
            Assert.That(first.OptionalHeaderFields, Is.Empty);
            Assert.That(first.Data, Is.EqualTo(new byte[] { 10, 11 }));
            Assert.That(first.RestartLocation!.LogicalByteOffset, Is.Zero);
            Assert.That(last.LastPage, Is.True);
            Assert.That(state.NextLogicalByteOffset, Is.EqualTo(10));
        });

        Assert.DoesNotThrow(() => state.RequireEndOfFile());
    }

    [Test]
    public void Descriptor_and_access_controlled_pages_preserve_optional_header_fields()
    {
        FtpPageStructureState state = CreateState(FtpTransferType.Ascii, 0);

        FtpPageFrame descriptor = state.EncodePage(CreatePage(
            new byte[] { 1 }, 2, new byte[] { 20, 21 }, new byte[] { 30 }));
        FtpPageFrame accessControlled = state.EncodePage(CreatePage(
            new byte[] { 2 }, 3, new byte[] { 44 }, new byte[] { 50, 51 }));

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.HeaderLogicalByteCount, Is.EqualTo(6));
            Assert.That(descriptor.SerializedLogicalBytes,
                Is.EqualTo(new byte[] { 6, 1, 1, 2, 20, 21, 30 }));
            Assert.That(accessControlled.HeaderLogicalByteCount, Is.EqualTo(5));
            Assert.That(accessControlled.SerializedLogicalBytes,
                Is.EqualTo(new byte[] { 5, 2, 2, 3, 44, 50, 51 }));
        });
    }

    [Test]
    public void Local_byte_pages_use_one_full_storage_unit_for_every_header_field()
    {
        FtpPageStructureState producer = CreateState(FtpTransferType.LocalByte, 36);
        byte[] index = new byte[] { 0, 0, 0, 0, 2 };
        byte[] data = new byte[] { 0x0A, 0xBC, 0xDE, 0xF1, 0x23 };

        FtpPageFrame encoded = producer.EncodePage(CreatePage(index, 1, new byte[0], data));
        byte[] expected = new byte[]
        {
            0, 0, 0, 0, 4,
            0, 0, 0, 0, 2,
            0, 0, 0, 0, 1,
            0, 0, 0, 0, 1,
            0x0A, 0xBC, 0xDE, 0xF1, 0x23
        };
        FtpPageStructureState consumer = CreateState(FtpTransferType.LocalByte, 36);
        FtpPageFrame decoded = consumer.DecodePage(encoded.SerializedLogicalBytes, 0,
            encoded.SerializedLogicalBytes.Length);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.SerializedLogicalBytes, Is.EqualTo(expected));
            Assert.That(decoded.PageIndex, Is.EqualTo(index));
            Assert.That(decoded.Data, Is.EqualTo(data));
            Assert.That(decoded.HeaderLogicalByteCount, Is.EqualTo(4));
            Assert.That(decoded.DataLogicalByteCount, Is.EqualTo(1));
            Assert.That(consumer.FindRestartLocation(index)!.LogicalByteOffset, Is.Zero);
        });
    }

    [Test]
    public void Encoder_rejects_invalid_page_descriptors()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<FtpTransferException>(() => CreateState(FtpTransferType.Image, 0).EncodePage(
                CreatePage(new byte[] { 0 }, 0, new byte[0], new byte[] { 1 })));
            Assert.Throws<FtpTransferException>(() => CreateState(FtpTransferType.Image, 0).EncodePage(
                CreatePage(new byte[] { 1 }, 1, new byte[] { 2 }, new byte[0])));
            Assert.Throws<FtpTransferException>(() => CreateState(FtpTransferType.Image, 0).EncodePage(
                CreatePage(new byte[] { 1 }, 3, new byte[0], new byte[0])));
            Assert.Throws<FtpTransferException>(() => CreateState(FtpTransferType.Image, 0).EncodePage(
                CreatePage(new byte[] { 1 }, 4, new byte[0], new byte[0])));
        });
    }

    [Test]
    public void Decoder_rejects_malformed_headers_lengths_and_logical_bytes()
    {
        FtpPageStructureState malformedType = CreateState(FtpTransferType.Image, 0);
        FtpPageStructureState wrongLength = CreateState(FtpTransferType.Image, 0);
        FtpPageStructureState shortHeader = CreateState(FtpTransferType.Image, 0);
        FtpPageStructureState badPadding = CreateState(FtpTransferType.LocalByte, 36);
        byte[] padded = new byte[20];
        padded[0] = 0x10;
        padded[4] = 4;
        padded[9] = 1;
        padded[19] = 1;

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpTransferException>(() => malformedType.DecodePage(
                new byte[] { 4, 1, 0, 4 }, 0, 4));
            Assert.Throws<FtpTransferException>(() => wrongLength.DecodePage(
                new byte[] { 4, 1, 2, 1, 9 }, 0, 5));
            Assert.Throws<FtpTransferException>(() => shortHeader.DecodePage(
                new byte[] { 4, 1, 0 }, 0, 3));
            Assert.Throws<FtpTransferException>(() => badPadding.DecodePage(padded, 0, padded.Length));
        });
    }

    [Test]
    public void State_rejects_duplicate_indexes_missing_eof_and_pages_after_eof()
    {
        FtpPageStructureState duplicate = CreateState(FtpTransferType.Image, 0);
        duplicate.EncodePage(CreatePage(new byte[] { 1 }, 1, new byte[0], new byte[0]));
        FtpPageStructureState missingEof = CreateState(FtpTransferType.Image, 0);
        missingEof.EncodePage(CreatePage(new byte[] { 2 }, 1, new byte[0], new byte[0]));
        FtpPageStructureState completed = CreateState(FtpTransferType.Image, 0);
        completed.EncodePage(CreatePage(new byte[] { 0 }, 0, new byte[0], new byte[0]));

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpTransferException>(() => duplicate.EncodePage(
                CreatePage(new byte[] { 1 }, 2, new byte[0], new byte[0])));
            Assert.Throws<FtpTransferException>(() => missingEof.RequireEndOfFile());
            Assert.Throws<FtpTransferException>(() => completed.EncodePage(
                CreatePage(new byte[] { 2 }, 1, new byte[0], new byte[0])));
        });
    }

    [Test]
    public void Codec_validates_structure_width_and_conversion_direction()
    {
        FtpTransferOptions file = CreateOptions(FtpTransferType.Image, 0);
        file.Structure = FtpFileStructure.File;
        FtpTransferOptions narrow = CreateOptions(FtpTransferType.LocalByte, 2);
        FtpPageStructureState producer = CreateState(FtpTransferType.Image, 0);
        producer.EncodePage(CreatePage(new byte[] { 1 }, 1, new byte[0], new byte[0]));
        FtpPageStructureState consumer = CreateState(FtpTransferType.Image, 0);
        consumer.DecodePage(new byte[] { 4, 1, 0, 1 }, 0, 4);

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpConfigurationException>(() => PageStructureCodec.CreateFramingState(file));
            Assert.Throws<FtpConfigurationException>(() => PageStructureCodec.CreateFramingState(narrow));
            Assert.Throws<FtpTransferException>(() => producer.DecodePage(
                new byte[] { 4, 2, 0, 1 }, 0, 4));
            Assert.Throws<FtpTransferException>(() => consumer.EncodePage(
                CreatePage(new byte[] { 2 }, 1, new byte[0], new byte[0])));
        });
    }

    private static FtpPageStructureState CreateState(FtpTransferType type, int localByteSize)
    {
        return PageStructureCodec.CreateFramingState(CreateOptions(type, localByteSize));
    }

    private static FtpTransferOptions CreateOptions(FtpTransferType type, int localByteSize)
    {
        FtpTransferOptions options = new FtpTransferOptions();
        options.Structure = FtpFileStructure.Page;
        options.Type = type;
        if (type == FtpTransferType.LocalByte)
        {
            options.HasLocalByteSize = true;
            options.LocalByteSize = localByteSize;
        }

        return options;
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
