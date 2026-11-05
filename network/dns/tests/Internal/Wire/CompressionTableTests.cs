namespace dns.Tests.Internal.Wire;

public class CompressionTableTests
{
    [Test]
    public void FirstOccurrenceIsWrittenUncompressedAndRegistered()
    {
        byte[] destination = new byte[32];
        WireWriter writer = new WireWriter(destination, 0, destination.Length);
        CompressionTable table = new CompressionTable(0);

        WireWriteResult result = table.WriteName(
            writer,
            Parse("WwW.Example."),
            new DnsEncodeOptions(),
            true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.BytesWritten, Is.EqualTo(13));
            Assert.That(writer.CopyWrittenBytes(), Is.EqualTo(new byte[]
            {
                3, 0x57, 0x77, 0x57,
                7, 0x45, 0x78, 0x61, 0x6d, 0x70, 0x6c, 0x65,
                0
            }));
        });
    }

    [Test]
    public void RepeatedWholeNameUsesTheEarliestRegisteredOffset()
    {
        byte[] destination = new byte[32];
        WireWriter writer = new WireWriter(destination, 0, destination.Length);
        CompressionTable table = new CompressionTable(0);
        DnsEncodeOptions options = new DnsEncodeOptions();
        table.WriteName(writer, Parse("example."), options, true);

        WireWriteResult repeated = table.WriteName(
            writer,
            Parse("example."),
            options,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(repeated.Success, Is.True);
            Assert.That(repeated.StartOffset, Is.EqualTo(9));
            Assert.That(repeated.BytesWritten, Is.EqualTo(2));
            Assert.That(writer.CopyWrittenBytes(), Is.EqualTo(new byte[]
            {
                7, 0x65, 0x78, 0x61, 0x6d, 0x70, 0x6c, 0x65, 0,
                0xc0, 0x00
            }));
        });
    }

    [Test]
    public void LongestAvailableSuffixIsSelectedDeterministically()
    {
        byte[] destination = new byte[64];
        WireWriter writer = new WireWriter(destination, 0, destination.Length);
        CompressionTable table = new CompressionTable(0);
        DnsEncodeOptions options = new DnsEncodeOptions();
        table.WriteName(writer, Parse("Example."), options, true);
        table.WriteName(writer, Parse("WwW.Example."), options, true);

        WireWriteResult result = table.WriteName(
            writer,
            Parse("Api.WwW.Example."),
            options,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.StartOffset, Is.EqualTo(15));
            Assert.That(result.BytesWritten, Is.EqualTo(6));
            Assert.That(writer.CopyWrittenBytes(), Is.EqualTo(new byte[]
            {
                7, 0x45, 0x78, 0x61, 0x6d, 0x70, 0x6c, 0x65, 0,
                3, 0x57, 0x77, 0x57, 0xc0, 0x00,
                3, 0x41, 0x70, 0x69, 0xc0, 0x09
            }));
            DnsDecodeResult decoded = CompressedDnsNameDecoder.DecodeName(
                writer.CopyWrittenBytes(),
                result.StartOffset,
                new DnsDecodeOptions());
            Assert.That(Format(decoded.Name!), Is.EqualTo("Api.WwW.Example."));
        });
    }

    [Test]
    public void CompressionMatchesAsciiCaseInsensitively()
    {
        byte[] destination = new byte[32];
        WireWriter writer = new WireWriter(destination, 0, destination.Length);
        CompressionTable table = new CompressionTable(0);
        DnsEncodeOptions options = new DnsEncodeOptions();
        table.WriteName(writer, Parse("Example."), options, true);

        WireWriteResult result = table.WriteName(
            writer,
            Parse("www.eXAMPLE."),
            options,
            true);
        DnsDecodeResult decoded = CompressedDnsNameDecoder.DecodeName(
            writer.CopyWrittenBytes(),
            result.StartOffset,
            new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.BytesWritten, Is.EqualTo(6));
            Assert.That(destination[result.StartOffset + 4], Is.EqualTo(0xc0));
            Assert.That(destination[result.StartOffset + 5], Is.Zero);
            Assert.That(Format(decoded.Name!), Is.EqualTo("www.Example."));
        });
    }

    [Test]
    public void CompressionFoldsAsciiButNotBinaryLabelOctets()
    {
        byte[] destination = new byte[16];
        WireWriter writer = new WireWriter(destination, 0, destination.Length);
        CompressionTable table = new CompressionTable(0);
        DnsEncodeOptions options = new DnsEncodeOptions();
        table.WriteName(writer, CreateBinaryName([0x41, 0x80]), options, true);

        WireWriteResult asciiCase = table.WriteName(
            writer,
            CreateBinaryName([0x61, 0x80]),
            options,
            true);
        WireWriteResult binaryDifference = table.WriteName(
            writer,
            CreateBinaryName([0x61, 0x81]),
            options,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(asciiCase.BytesWritten, Is.EqualTo(2));
            Assert.That(destination[asciiCase.StartOffset], Is.EqualTo(0xc0));
            Assert.That(binaryDifference.BytesWritten, Is.EqualTo(4));
            Assert.That(destination[binaryDifference.StartOffset], Is.EqualTo(2));
            Assert.That(destination[binaryDifference.StartOffset + 2],
                Is.EqualTo(0x81));
        });
    }

    [Test]
    public void CompressionCanBeDisabledForAnEntireEncodingMode()
    {
        byte[] destination = new byte[32];
        WireWriter writer = new WireWriter(destination, 0, destination.Length);
        CompressionTable table = new CompressionTable(0);
        DnsEncodeOptions options = new DnsEncodeOptions();
        options.EnableCompression = false;

        WireWriteResult first = table.WriteName(
            writer,
            Parse("Example."),
            options,
            true);
        WireWriteResult second = table.WriteName(
            writer,
            Parse("Example."),
            options,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(first.BytesWritten, Is.EqualTo(9));
            Assert.That(second.BytesWritten, Is.EqualTo(9));
            Assert.That(destination[second.StartOffset], Is.EqualTo(7));
            Assert.That(destination[second.StartOffset + 8], Is.Zero);
        });
    }

    [Test]
    public void FieldPolicyPreventsPointersButKeepsLegalNameLocationsReusable()
    {
        byte[] destination = new byte[40];
        WireWriter writer = new WireWriter(destination, 0, destination.Length);
        CompressionTable table = new CompressionTable(0);
        DnsEncodeOptions options = new DnsEncodeOptions();

        WireWriteResult firstForbidden = table.WriteName(
            writer,
            Parse("example."),
            options,
            false);
        WireWriteResult secondForbidden = table.WriteName(
            writer,
            Parse("example."),
            options,
            false);
        WireWriteResult permitted = table.WriteName(
            writer,
            Parse("example."),
            options,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(firstForbidden.BytesWritten, Is.EqualTo(9));
            Assert.That(secondForbidden.BytesWritten, Is.EqualTo(9));
            Assert.That(destination[secondForbidden.StartOffset], Is.EqualTo(7));
            Assert.That(permitted.BytesWritten, Is.EqualTo(2));
            Assert.That(destination[permitted.StartOffset], Is.EqualTo(0xc0));
            Assert.That(destination[permitted.StartOffset + 1], Is.Zero);
        });
    }

    [Test]
    public void CanonicalModeLowercasesAndDisablesCompressionByDefault()
    {
        byte[] destination = new byte[32];
        WireWriter writer = new WireWriter(destination, 0, destination.Length);
        CompressionTable table = new CompressionTable(0);
        DnsEncodeOptions options = new DnsEncodeOptions();
        options.Canonical = true;

        WireWriteResult first = table.WriteName(
            writer,
            Parse("WwW.Example."),
            options,
            true);
        WireWriteResult second = table.WriteName(
            writer,
            Parse("WwW.Example."),
            options,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(first.BytesWritten, Is.EqualTo(13));
            Assert.That(second.BytesWritten, Is.EqualTo(13));
            Assert.That(writer.CopyWrittenBytes(), Is.EqualTo(new byte[]
            {
                3, 0x77, 0x77, 0x77,
                7, 0x65, 0x78, 0x61, 0x6d, 0x70, 0x6c, 0x65, 0,
                3, 0x77, 0x77, 0x77,
                7, 0x65, 0x78, 0x61, 0x6d, 0x70, 0x6c, 0x65, 0
            }));
        });
    }

    [Test]
    public void RootIsAlwaysCheaperAsOneLiteralOctet()
    {
        byte[] destination = new byte[2];
        WireWriter writer = new WireWriter(destination, 0, destination.Length);
        CompressionTable table = new CompressionTable(0);
        DnsEncodeOptions options = new DnsEncodeOptions();

        WireWriteResult first = table.WriteName(
            writer,
            Parse("."),
            options,
            true);
        WireWriteResult second = table.WriteName(
            writer,
            Parse("."),
            options,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(first.BytesWritten, Is.EqualTo(1));
            Assert.That(second.BytesWritten, Is.EqualTo(1));
            Assert.That(destination, Is.EqualTo(new byte[] { 0, 0 }));
        });
    }

    [Test]
    public void MaximumFourteenBitLocationCanBePointerTarget()
    {
        byte[] destination = new byte[16389];
        WireWriter writer = new WireWriter(destination, 0, destination.Length);
        writer.WriteBytes(new byte[DnsProtocolLimits.MaximumCompressionPointerOffset]);
        CompressionTable table = new CompressionTable(0);
        DnsEncodeOptions options = new DnsEncodeOptions();
        table.WriteName(writer, Parse("a."), options, true);

        WireWriteResult repeated = table.WriteName(
            writer,
            Parse("a."),
            options,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(repeated.BytesWritten, Is.EqualTo(2));
            Assert.That(destination[repeated.StartOffset], Is.EqualTo(0xff));
            Assert.That(destination[repeated.StartOffset + 1], Is.EqualTo(0xff));
        });
    }

    [Test]
    public void LocationsBeyondFourteenBitsAreNeverRegistered()
    {
        byte[] destination = new byte[16392];
        WireWriter writer = new WireWriter(destination, 0, destination.Length);
        writer.WriteBytes(
            new byte[DnsProtocolLimits.MaximumCompressionPointerOffset + 1]);
        CompressionTable table = new CompressionTable(0);
        DnsEncodeOptions options = new DnsEncodeOptions();
        table.WriteName(writer, Parse("a."), options, true);

        WireWriteResult repeated = table.WriteName(
            writer,
            Parse("a."),
            options,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(repeated.BytesWritten, Is.EqualTo(3));
            Assert.That(destination[repeated.StartOffset], Is.EqualTo(1));
            Assert.That(destination[repeated.StartOffset + 1], Is.EqualTo(0x61));
            Assert.That(destination[repeated.StartOffset + 2], Is.Zero);
        });
    }

    [Test]
    public void MessageStartOffsetIsExcludedFromPointerValues()
    {
        byte[] destination = new byte[32];
        WireWriter writer = new WireWriter(destination, 5, 20);
        CompressionTable table = new CompressionTable(5);
        DnsEncodeOptions options = new DnsEncodeOptions();
        table.WriteName(writer, Parse("a."), options, true);

        WireWriteResult repeated = table.WriteName(
            writer,
            Parse("a."),
            options,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(repeated.StartOffset, Is.EqualTo(8));
            Assert.That(destination[8], Is.EqualTo(0xc0));
            Assert.That(destination[9], Is.Zero);
        });
    }

    [Test]
    public void CountingAndBufferedWritersChooseTheSameCompressionSizes()
    {
        byte[] destination = new byte[64];
        WireWriter buffered = new WireWriter(destination, 0, destination.Length);
        WireWriter counting = new WireWriter(null, 0, destination.Length);
        CompressionTable bufferedTable = new CompressionTable(0);
        CompressionTable countingTable = new CompressionTable(0);
        DnsEncodeOptions options = new DnsEncodeOptions();
        DnsName first = Parse("www.example.");
        DnsName second = Parse("api.www.example.");

        bufferedTable.WriteName(buffered, first, options, true);
        bufferedTable.WriteName(buffered, second, options, true);
        countingTable.WriteName(counting, first, options, true);
        countingTable.WriteName(counting, second, options, true);

        Assert.Multiple(() =>
        {
            Assert.That(counting.WrittenLength(), Is.EqualTo(buffered.WrittenLength()));
            Assert.That(counting.WrittenLength(), Is.EqualTo(19));
            Assert.That(counting.CopyWrittenBytes(), Is.Empty);
        });
    }

    [Test]
    public void FailedWritesAreAtomicAndDoNotRegisterPhantomSuffixes()
    {
        byte[] destination = new byte[32];
        WireWriter writer = new WireWriter(destination, 0, destination.Length);
        CompressionTable table = new CompressionTable(0);
        DnsEncodeOptions options = new DnsEncodeOptions();
        options.MaximumMessageLength = 1;

        WireWriteResult failed = table.WriteName(
            writer,
            Parse("example."),
            options,
            true);
        options.MaximumMessageLength = destination.Length;
        WireWriteResult firstSuccess = table.WriteName(
            writer,
            Parse("example."),
            options,
            true);
        WireWriteResult repeated = table.WriteName(
            writer,
            Parse("example."),
            options,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(failed.Success, Is.False);
            Assert.That(failed.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ResourceLimitExceeded));
            Assert.That(firstSuccess.StartOffset, Is.Zero);
            Assert.That(firstSuccess.BytesWritten, Is.EqualTo(9));
            Assert.That(repeated.BytesWritten, Is.EqualTo(2));
        });
    }

    [Test]
    public void RewoundWriterCannotProduceSelfOrForwardPointers()
    {
        byte[] firstDestination = new byte[16];
        WireWriter firstWriter = new WireWriter(
            firstDestination,
            0,
            firstDestination.Length);
        CompressionTable table = new CompressionTable(0);
        DnsEncodeOptions options = new DnsEncodeOptions();
        table.WriteName(firstWriter, Parse("example."), options, true);

        byte[] secondDestination = new byte[16];
        WireWriter rewoundWriter = new WireWriter(
            secondDestination,
            0,
            secondDestination.Length);
        WireWriteResult result = table.WriteName(
            rewoundWriter,
            Parse("example."),
            options,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.BytesWritten, Is.EqualTo(9));
            Assert.That(secondDestination[0], Is.EqualTo(7));
            Assert.That(secondDestination[8], Is.Zero);
        });
    }

    [Test]
    public void InvalidNamesWritersAndOptionsFailWithoutAdvancing()
    {
        byte[] destination = new byte[32];
        WireWriter writer = new WireWriter(destination, 0, destination.Length);
        CompressionTable table = new CompressionTable(0);
        DnsName invalid = new DnsName();
        invalid.Labels.Add(new byte[64]);
        DnsEncodeOptions canonical = new DnsEncodeOptions();
        canonical.Canonical = true;
        canonical.CanonicalOrder = null!;

        WireWriteResult invalidName = table.WriteName(
            writer,
            invalid,
            new DnsEncodeOptions(),
            true);
        WireWriteResult relativeName = table.WriteName(
            writer,
            Parse("example"),
            new DnsEncodeOptions(),
            true);
        WireWriteResult missingOptions = table.WriteName(
            writer,
            Parse("example."),
            null!,
            true);
        WireWriteResult missingWriter = table.WriteName(
            null!,
            Parse("example."),
            new DnsEncodeOptions(),
            true);
        WireWriteResult missingCanonicalOptions = table.WriteName(
            writer,
            Parse("example."),
            canonical,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(invalidName.Success, Is.False);
            Assert.That(invalidName.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidLabel));
            Assert.That(relativeName.Success, Is.False);
            Assert.That(relativeName.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidLabel));
            Assert.That(missingOptions.Success, Is.False);
            Assert.That(missingWriter.Success, Is.False);
            Assert.That(missingCanonicalOptions.Success, Is.False);
            Assert.That(writer.WriteOffset(), Is.Zero);
        });
    }

    private static DnsName Parse(string text)
    {
        return PresentationDnsName.ParseName(text, null).Name!;
    }

    private static string Format(DnsName name)
    {
        return PresentationDnsName.FormatName(name).Text!;
    }

    private static DnsName CreateBinaryName(byte[] label)
    {
        return RawDnsName.CreateAbsoluteName(
            new List<byte[]> { label }).Name!;
    }
}
