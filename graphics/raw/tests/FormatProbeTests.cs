using raw;

namespace raw.Tests;

public class FormatProbeTests
{
    [TestCase(false, RawByteOrder.LittleEndian)]
    [TestCase(false, RawByteOrder.BigEndian)]
    [TestCase(true, RawByteOrder.LittleEndian)]
    [TestCase(true, RawByteOrder.BigEndian)]
    public void TiffAndBigTiffAreRecognizedFromContentInBothByteOrders(bool bigTiff, RawByteOrder byteOrder)
    {
        byte[] source = WriteTiff(bigTiff, byteOrder, Array.Empty<TiffLayoutEntryInput>(), Array.Empty<TiffLayoutIfdInput>());

        RawProbe probe = Probe(source, "misleading.bin", RawFormat.Unknown, false);

        Assert.Multiple(() =>
        {
            Assert.That(probe.Format, Is.EqualTo(RawFormat.Tiff));
            Assert.That(probe.Confidence, Is.EqualTo(100));
            Assert.That(probe.Candidates, Is.EqualTo(new RawFormat[] { RawFormat.Tiff }));
            Assert.That(probe.Diagnostics, Is.Empty);
            Assert.That(probe.ByteOrderKnown, Is.True);
            Assert.That(probe.ByteOrder, Is.EqualTo(byteOrder));
            Assert.That(probe.BigTiff, Is.EqualTo(bigTiff));
            Assert.That(probe.IsAmbiguous(), Is.False);
            Assert.That(probe.Succeeded(), Is.True);
        });
    }

    [TestCase(RawByteOrder.LittleEndian)]
    [TestCase(RawByteOrder.BigEndian)]
    public void Cr2HeaderIsDecisiveWithoutDependingOnExtension(RawByteOrder byteOrder)
    {
        byte[] source = CreateCr2(byteOrder);

        RawProbe probe = Probe(source, "capture.data", RawFormat.Unknown, false);

        Assert.Multiple(() =>
        {
            Assert.That(probe.Format, Is.EqualTo(RawFormat.Cr2));
            Assert.That(probe.Confidence, Is.EqualTo(100));
            Assert.That(probe.Candidates, Is.EqualTo(new RawFormat[] { RawFormat.Cr2 }));
            Assert.That(probe.ByteOrder, Is.EqualTo(byteOrder));
            Assert.That(probe.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void NikonMakeAndRawIfdEvidenceIdentifyNef()
    {
        byte[] source = CreateNef(true, false);

        RawProbe probe = Probe(source, "capture.bin", RawFormat.Unknown, false);

        Assert.Multiple(() =>
        {
            Assert.That(probe.Format, Is.EqualTo(RawFormat.Nef));
            Assert.That(probe.Confidence, Is.EqualTo(95));
            Assert.That(probe.Candidates, Is.EqualTo(new RawFormat[] { RawFormat.Nef }));
            Assert.That(probe.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void LoneNikonMakeIsReportedAsTiffNefAmbiguity()
    {
        byte[] source = CreateNef(false, false);

        RawProbe probe = Probe(source, "capture.nef", RawFormat.Unknown, false);

        Assert.Multiple(() =>
        {
            Assert.That(probe.Format, Is.EqualTo(RawFormat.Unknown));
            Assert.That(probe.Confidence, Is.EqualTo(50));
            Assert.That(probe.Candidates, Is.EqualTo(new RawFormat[] { RawFormat.Nef, RawFormat.Tiff }));
            Assert.That(probe.Diagnostics.Single().Code, Is.EqualTo(RawDiagnosticCode.AmbiguousFormat));
            Assert.That(probe.Diagnostics.Single().Severity, Is.EqualTo(RawSeverity.Error));
            Assert.That(probe.IsAmbiguous(), Is.True);
            Assert.That(probe.Succeeded(), Is.False);
        });
    }

    [Test]
    public void DngEvidencePreventsNikonMakeFromBeingClassifiedAsNef()
    {
        byte[] source = CreateNef(true, true);

        RawProbe probe = Probe(source, "converted.tif", RawFormat.Unknown, false);

        Assert.Multiple(() =>
        {
            Assert.That(probe.Format, Is.EqualTo(RawFormat.Tiff));
            Assert.That(probe.Candidates, Is.EqualTo(new RawFormat[] { RawFormat.Tiff }));
            Assert.That(probe.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void GenericDescriptorIdentifiesHeaderlessDataButConflictsWithContainerContent()
    {
        byte[] raw = new byte[] { 3, 1, 4, 1, 5, 9 };
        byte[] tiff = WriteTiff(false, RawByteOrder.LittleEndian, Array.Empty<TiffLayoutEntryInput>(), Array.Empty<TiffLayoutIfdInput>());

        RawProbe generic = Probe(raw, "samples.raw", RawFormat.GenericRaw, true);
        RawProbe missing = Probe(raw, "samples.bin", RawFormat.Unknown, false);
        RawProbe conflict = Probe(tiff, "samples.raw", RawFormat.Unknown, true);

        Assert.Multiple(() =>
        {
            Assert.That(generic.Format, Is.EqualTo(RawFormat.GenericRaw));
            Assert.That(generic.Confidence, Is.EqualTo(100));
            Assert.That(generic.Succeeded(), Is.True);
            Assert.That(missing.Format, Is.EqualTo(RawFormat.Unknown));
            Assert.That(missing.GenericDescriptorRequired, Is.True);
            Assert.That(missing.Diagnostics.Single().Code, Is.EqualTo(RawDiagnosticCode.UnknownFormat));
            Assert.That(conflict.Format, Is.EqualTo(RawFormat.Unknown));
            Assert.That(conflict.Candidates, Is.EqualTo(new RawFormat[] { RawFormat.Tiff, RawFormat.GenericRaw }));
            Assert.That(conflict.Diagnostics.Single().Code, Is.EqualTo(RawDiagnosticCode.AmbiguousFormat));
            Assert.That(conflict.IsAmbiguous(), Is.True);
        });
    }

    [Test]
    public void FilenameAndCallerFormatMismatchesWarnWithoutChangingContentDetection()
    {
        byte[] source = WriteTiff(false, RawByteOrder.LittleEndian, Array.Empty<TiffLayoutEntryInput>(), Array.Empty<TiffLayoutIfdInput>());

        RawProbe probe = Probe(source, "capture.cr2", RawFormat.Nef, false);

        Assert.Multiple(() =>
        {
            Assert.That(probe.Format, Is.EqualTo(RawFormat.Tiff));
            Assert.That(probe.Diagnostics.Length, Is.EqualTo(2));
            Assert.That(probe.Diagnostics.All(diagnostic => diagnostic.Code == RawDiagnosticCode.FormatMismatch), Is.True);
            Assert.That(probe.Diagnostics.All(diagnostic => diagnostic.Severity == RawSeverity.Warning), Is.True);
            Assert.That(probe.Diagnostics[0].Message, Does.Contain("caller format"));
            Assert.That(probe.Diagnostics[1].Message, Does.Contain("filename extension"));
            Assert.That(probe.Succeeded(), Is.True);
        });
    }

    [Test]
    public void NonSeekableStreamsAreProbedWithoutDisposalAndSeekableInputsReturnToWindowStart()
    {
        byte[] source = CreateCr2(RawByteOrder.LittleEndian);
        ProbeNonSeekableStream nonSeekable = new ProbeNonSeekableStream(source, 3);
        MemoryStream seekable = new MemoryStream(source, false);

        RawProbe streamed = FormatProbe.Probe(nonSeekable, 0, source.Length, "capture.cr2", RawFormat.Unknown, false, new RawLimits(), CancellationToken.None);
        RawProbe randomAccess = FormatProbe.Probe(seekable, 0, source.Length, "capture.cr2", RawFormat.Unknown, false, new RawLimits(), CancellationToken.None);
        int trailing = nonSeekable.ReadByte();

        Assert.Multiple(() =>
        {
            Assert.That(streamed.Format, Is.EqualTo(RawFormat.Cr2));
            Assert.That(randomAccess.Format, Is.EqualTo(RawFormat.Cr2));
            Assert.That(nonSeekable.WasDisposed, Is.False);
            Assert.That(trailing, Is.EqualTo(-1));
            Assert.That(seekable.Position, Is.Zero);
        });
    }

    [Test]
    public void InvalidArgumentsLimitsCancellationAndTruncatedSignaturesAreHandled()
    {
        byte[] source = CreateCr2(RawByteOrder.LittleEndian);
        RawLimits limits = new RawLimits();
        limits.MaxAllocationBytes = 8;
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        RawProbe truncated = Probe(new byte[] { 0x49, 0x49, 0x2a }, null, RawFormat.Unknown, false);

        Assert.Multiple(() =>
        {
            Assert.That(truncated.Format, Is.EqualTo(RawFormat.Unknown));
            Assert.That(truncated.ByteOrderKnown, Is.False);
            Assert.That(truncated.Diagnostics.Single().Code, Is.EqualTo(RawDiagnosticCode.UnknownFormat));
            Assert.Throws<RawFormatException>(() => FormatProbe.Probe((BoundedInput?)null, null, RawFormat.Unknown, false, CancellationToken.None));
            Assert.Throws<RawFormatException>(() => Probe(source, null, (RawFormat)99, false));
            Assert.Throws<RawLimitException>(() => FormatProbe.Probe(new ReadOnlyMemory<byte>(source), null, RawFormat.Unknown, false, limits, CancellationToken.None));
            Assert.Throws<OperationCanceledException>(() => FormatProbe.Probe(new ReadOnlyMemory<byte>(source), null, RawFormat.Unknown, false, new RawLimits(), cancellation.Token));
        });
    }

    static RawProbe Probe(byte[] source, string? fileName, RawFormat expectedFormat, bool genericDescriptorProvided)
    {
        return FormatProbe.Probe(new ReadOnlyMemory<byte>(source), fileName, expectedFormat, genericDescriptorProvided, new RawLimits(), CancellationToken.None);
    }

    static byte[] CreateCr2(RawByteOrder byteOrder)
    {
        byte[] source = new byte[32];
        if (byteOrder == RawByteOrder.LittleEndian)
        {
            source[0] = 0x49;
            source[1] = 0x49;
        }
        else
        {
            source[0] = 0x4d;
            source[1] = 0x4d;
        }
        EndianWriter.WriteUInt16(source.AsSpan(2, 2), 42, byteOrder);
        EndianWriter.WriteUInt32(source.AsSpan(4, 4), 16, byteOrder);
        source[8] = 0x43;
        source[9] = 0x52;
        source[10] = 2;
        source[11] = 0;
        EndianWriter.WriteUInt32(source.AsSpan(12, 4), 24, byteOrder);
        EndianWriter.WriteUInt16(source.AsSpan(16, 2), 0, byteOrder);
        EndianWriter.WriteUInt32(source.AsSpan(18, 4), 0, byteOrder);
        EndianWriter.WriteUInt16(source.AsSpan(24, 2), 0, byteOrder);
        EndianWriter.WriteUInt32(source.AsSpan(26, 4), 0, byteOrder);
        return source;
    }

    static byte[] CreateNef(bool rawEvidence, bool dng)
    {
        List<TiffLayoutEntryInput> rootEntries = new List<TiffLayoutEntryInput>();
        rootEntries.Add(AsciiEntry(271, "NIKON CORPORATION\0"));
        List<TiffLayoutIfdInput> additional = new List<TiffLayoutIfdInput>();
        if (rawEvidence)
        {
            rootEntries.Add(LongReference(330, "IFD0/SubIFD0"));
            List<TiffLayoutEntryInput> rawEntries = new List<TiffLayoutEntryInput>();
            rawEntries.Add(ShortEntry(259, 34713));
            rawEntries.Add(ShortArrayEntry(33421, new ushort[] { 2, 2 }));
            rawEntries.Add(new TiffLayoutEntryInput(33422, TiffFieldType.Byte, 4, new byte[] { 0, 1, 1, 2 }, null));
            additional.Add(new TiffLayoutIfdInput("IFD0/SubIFD0", rawEntries.ToArray(), null));
        }
        if (dng)
        {
            rootEntries.Add(new TiffLayoutEntryInput(50706, TiffFieldType.Byte, 4, new byte[] { 1, 4, 0, 0 }, null));
        }
        return WriteTiff(false, RawByteOrder.LittleEndian, rootEntries.ToArray(), additional.ToArray());
    }

    static byte[] WriteTiff(bool bigTiff, RawByteOrder byteOrder, TiffLayoutEntryInput[] rootEntries, TiffLayoutIfdInput[] additional)
    {
        TiffLayoutIfdInput[] directories = new TiffLayoutIfdInput[additional.Length + 1];
        directories[0] = new TiffLayoutIfdInput("IFD0", rootEntries, null);
        additional.CopyTo(directories, 1);
        TiffLayoutRequest request = new TiffLayoutRequest(bigTiff, byteOrder, "IFD0", directories, Array.Empty<TiffLayoutBlockInput>());
        TiffLayoutPlan plan = TiffLayoutPlanner.Plan(request, new RawLimits(), CancellationToken.None);
        return TiffWriter.WriteToArray(plan, new RawLimits(), CancellationToken.None);
    }

    static TiffLayoutEntryInput AsciiEntry(ushort tag, string value)
    {
        byte[] bytes = System.Text.Encoding.ASCII.GetBytes(value);
        return new TiffLayoutEntryInput(tag, TiffFieldType.Ascii, (ulong)bytes.Length, bytes, null);
    }

    static TiffLayoutEntryInput ShortEntry(ushort tag, ushort value)
    {
        return ShortArrayEntry(tag, new ushort[] { value });
    }

    static TiffLayoutEntryInput ShortArrayEntry(ushort tag, ushort[] values)
    {
        byte[] bytes = new byte[values.Length * 2];
        for (int index = 0; index < values.Length; index++)
        {
            EndianWriter.WriteUInt16(bytes.AsSpan(index * 2, 2), values[index], RawByteOrder.LittleEndian);
        }
        return new TiffLayoutEntryInput(tag, TiffFieldType.Short, (ulong)values.Length, bytes, null);
    }

    static TiffLayoutEntryInput LongReference(ushort tag, string target)
    {
        return new TiffLayoutEntryInput(tag, TiffFieldType.Long, 1, new byte[4], new TiffLayoutReference[] { new TiffLayoutReference(0, TiffLayoutReferenceKind.Ifd, target) });
    }

    class ProbeNonSeekableStream : Stream
    {
        MemoryStream Inner;
        int MaximumRead;
        public bool WasDisposed;

        public ProbeNonSeekableStream(byte[] source, int maximumRead)
        {
            Inner = new MemoryStream(source, false);
            MaximumRead = maximumRead;
            WasDisposed = false;
        }

        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        public override long Length
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return Inner.Read(buffer, offset, Math.Min(count, MaximumRead));
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            Inner.Dispose();
            base.Dispose(disposing);
        }
    }
}
