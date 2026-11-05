using raw;
using System.Diagnostics;

namespace raw.Tests;

public class TiffInteroperabilityTests
{
    [TestCase(false, RawByteOrder.LittleEndian)]
    [TestCase(false, RawByteOrder.BigEndian)]
    [TestCase(true, RawByteOrder.LittleEndian)]
    [TestCase(true, RawByteOrder.BigEndian)]
    public void ParseWriteParsePreservesValuesUnknownBytesAndImagePayload(bool bigTiff, RawByteOrder byteOrder)
    {
        TiffLayoutRequest originalRequest = CreateImageRequest(bigTiff, byteOrder, false);
        byte[] first = Write(originalRequest);
        TiffIfdGraph firstGraph = Parse(first);
        TiffLayoutRequest rebuiltRequest = Rebuild(firstGraph, first);
        byte[] second = Write(rebuiltRequest);
        TiffIfdGraph secondGraph = Parse(second);
        byte[] unknownInline = new byte[] { 0xa1, 0xb2, 0xc3 };
        byte[] unknownOutside = Enumerable.Range(0, 11).Select(value => (byte)(0xe0 + value)).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.EqualTo(first));
            Assert.That(secondGraph.Header.BigTiff, Is.EqualTo(bigTiff));
            Assert.That(secondGraph.Header.ByteOrder, Is.EqualTo(byteOrder));
            Assert.That(((secondGraph.FindIfd("IFD0") ?? throw new System.InvalidOperationException("A required value is unavailable.")).FindEntry(64999) ?? throw new System.InvalidOperationException("A required value is unavailable.")).RawBytes, Is.EqualTo(unknownInline));
            Assert.That(((secondGraph.FindIfd("IFD0") ?? throw new System.InvalidOperationException("A required value is unavailable.")).FindEntry(65000) ?? throw new System.InvalidOperationException("A required value is unavailable.")).RawBytes, Is.EqualTo(unknownOutside));
            Assert.That(secondGraph.Images.Single().Width, Is.EqualTo(3));
            Assert.That(secondGraph.Images.Single().Height, Is.EqualTo(2));
            Assert.That(ReadPayload(secondGraph, second), Is.EqualTo(new byte[] { 0, 32, 64, 96, 160, 255 }));
            Assert.That(TiffValidator.Validate(secondGraph, true).Messages, Is.Empty);
        });
    }

    [TestCase(false, RawByteOrder.LittleEndian)]
    [TestCase(true, RawByteOrder.BigEndian)]
    public void EquivalentEntryOrdersAndRepeatedPlanningProduceIdenticalOutput(bool bigTiff, RawByteOrder byteOrder)
    {
        TiffLayoutRequest forward = CreateImageRequest(bigTiff, byteOrder, false);
        TiffLayoutRequest reverse = CreateImageRequest(bigTiff, byteOrder, true);

        byte[] first = Write(forward);
        byte[] repeated = Write(forward);
        byte[] reordered = Write(reverse);

        Assert.Multiple(() =>
        {
            Assert.That(repeated, Is.EqualTo(first));
            Assert.That(reordered, Is.EqualTo(first));
            Assert.That((Parse(reordered).FindIfd("IFD0") ?? throw new System.InvalidOperationException("A required value is unavailable.")).Entries.Select(entry => entry.Tag), Is.Ordered);
        });
    }

    [TestCase(false, RawByteOrder.LittleEndian)]
    [TestCase(false, RawByteOrder.BigEndian)]
    [TestCase(true, RawByteOrder.LittleEndian)]
    [TestCase(true, RawByteOrder.BigEndian)]
    public void PreplannedNonSeekableOutputIsCompleteAndByteIdentical(bool bigTiff, RawByteOrder byteOrder)
    {
        TiffLayoutPlan plan = TiffLayoutPlanner.Plan(CreateImageRequest(bigTiff, byteOrder, false), new RawLimits(), CancellationToken.None);
        byte[] expected = TiffWriter.WriteToArray(plan, new RawLimits(), CancellationToken.None);
        InteropNonSeekableStream destination = new InteropNonSeekableStream();

        TiffWriter.Write(destination, plan, new RawLimits(), CancellationToken.None);
        byte[] actual = destination.ToArray();
        TiffIfdGraph parsed = Parse(actual);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actual.Length, Is.EqualTo(plan.PredictedSize));
            Assert.That(parsed.Images.Single().Blocks.Single().ByteCount, Is.EqualTo(6));
            Assert.That(ReadPayload(parsed, actual), Is.EqualTo(new byte[] { 0, 32, 64, 96, 160, 255 }));
        });
    }

    [TestCase(false, RawByteOrder.LittleEndian)]
    [TestCase(true, RawByteOrder.BigEndian)]
    public void LibTiffReadsGeneratedFilesWhenAvailable(bool bigTiff, RawByteOrder byteOrder)
    {
        string? executable = FindExecutable("tiffinfo");
        if (executable == null)
        {
            Assert.Ignore("LibTIFF tiffinfo is not installed on this test host.");
        }
        ExternalResult result = RunExternal(executable, new string[] { "-s" }, Write(CreateImageRequest(bigTiff, byteOrder, false)), ".tif");

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.Zero, result.StandardError);
            Assert.That(result.StandardOutput, Does.Contain("Image Width: 3 Image Length: 2"));
            Assert.That(result.StandardOutput, Does.Contain("Bits/Sample: 8"));
            Assert.That(result.StandardOutput, Does.Contain("Compression Scheme: None"));
            Assert.That(result.StandardOutput, Does.Contain("1 Strips"));
        });
    }

    [TestCase(false, RawByteOrder.LittleEndian)]
    [TestCase(true, RawByteOrder.BigEndian)]
    public void ExifToolReadsGeneratedFilesWhenAvailable(bool bigTiff, RawByteOrder byteOrder)
    {
        ExternalToolAdapter tool = IndependentToolAdapters.ExifTool();
        tool.IgnoreIfUnavailable();
        ExternalToolResult result = tool.RunOnBytes(new string[] { "-s", "-n", "-ImageWidth", "-ImageHeight", "-BitsPerSample", "-Compression", "-PhotometricInterpretation", "-Software" }, Write(CreateImageRequest(bigTiff, byteOrder, false)), ".tif");

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.Zero, result.StandardError);
            Assert.That(result.TimedOut, Is.False);
            Assert.That(result.StandardOutput, Does.Match("ImageWidth\\s*:\\s*3"));
            Assert.That(result.StandardOutput, Does.Match("ImageHeight\\s*:\\s*2"));
            Assert.That(result.StandardOutput, Does.Match("BitsPerSample\\s*:\\s*8"));
            Assert.That(result.StandardOutput, Does.Match("Compression\\s*:\\s*1"));
            Assert.That(result.StandardOutput, Does.Match("PhotometricInterpretation\\s*:\\s*1"));
            Assert.That(result.StandardOutput, Does.Match("Software\\s*:\\s*raw-tests"));
        });
    }

    static TiffLayoutRequest CreateImageRequest(bool bigTiff, RawByteOrder byteOrder, bool reverseEntries)
    {
        byte[] payload = new byte[] { 0, 32, 64, 96, 160, 255 };
        List<TiffLayoutEntryInput> entries = new List<TiffLayoutEntryInput>();
        entries.Add(LongEntry(256, 3, byteOrder));
        entries.Add(LongEntry(257, 2, byteOrder));
        entries.Add(ShortEntry(258, 8, byteOrder));
        entries.Add(ShortEntry(259, 1, byteOrder));
        entries.Add(ShortEntry(262, 1, byteOrder));
        entries.Add(OffsetEntry(273, bigTiff, byteOrder, "strip-0"));
        entries.Add(ShortEntry(277, 1, byteOrder));
        entries.Add(LongEntry(278, 2, byteOrder));
        entries.Add(bigTiff ? Long8Entry(279, 6, byteOrder) : LongEntry(279, 6, byteOrder));
        entries.Add(ShortEntry(284, 1, byteOrder));
        entries.Add(AsciiEntry(305, "raw-tests\0"));
        entries.Add(UndefinedEntry(64999, new byte[] { 0xa1, 0xb2, 0xc3 }));
        entries.Add(UndefinedEntry(65000, Enumerable.Range(0, 11).Select(value => (byte)(0xe0 + value)).ToArray()));
        if (reverseEntries)
        {
            entries.Reverse();
        }
        TiffLayoutIfdInput root = new TiffLayoutIfdInput("IFD0", entries.ToArray(), null);
        TiffLayoutBlockInput strip = new TiffLayoutBlockInput("strip-0", TiffLayoutBlockKind.Strip, payload.Length, payload);
        return new TiffLayoutRequest(bigTiff, byteOrder, "IFD0", new TiffLayoutIfdInput[] { root }, new TiffLayoutBlockInput[] { strip });
    }

    static TiffLayoutRequest Rebuild(TiffIfdGraph graph, byte[] source)
    {
        List<TiffLayoutIfdInput> directories = new List<TiffLayoutIfdInput>();
        for (int ifdIndex = 0; ifdIndex < graph.Ifds.Length; ifdIndex++)
        {
            TiffIfd ifd = graph.Ifds[ifdIndex];
            List<TiffLayoutEntryInput> entries = new List<TiffLayoutEntryInput>();
            for (int entryIndex = 0; entryIndex < ifd.Entries.Length; entryIndex++)
            {
                TiffEntry entry = ifd.Entries[entryIndex];
                TiffLayoutReference[]? references = null;
                if (entry.Tag == 273)
                {
                    references = new TiffLayoutReference[checked((int)entry.Count)];
                    for (int valueIndex = 0; valueIndex < references.Length; valueIndex++)
                    {
                        references[valueIndex] = new TiffLayoutReference(valueIndex, TiffLayoutReferenceKind.Block, "strip-" + valueIndex);
                    }
                }
                entries.Add(new TiffLayoutEntryInput(entry.Tag, entry.FieldType, entry.Count, entry.RawBytes.ToArray(), references));
            }
            directories.Add(new TiffLayoutIfdInput(ifd.Path, entries.ToArray(), null));
        }
        TiffImageDescriptor image = graph.Images.Single();
        TiffLayoutBlockInput[] blocks = new TiffLayoutBlockInput[image.Blocks.Length];
        for (int index = 0; index < image.Blocks.Length; index++)
        {
            TiffDataBlock block = image.Blocks[index];
            byte[] bytes = source.AsSpan(checked((int)block.Offset), checked((int)block.ByteCount)).ToArray();
            blocks[index] = new TiffLayoutBlockInput("strip-" + index, TiffLayoutBlockKind.Strip, bytes.Length, bytes);
        }
        return new TiffLayoutRequest(graph.Header.BigTiff, graph.Header.ByteOrder, graph.Ifds[0].Path, directories.ToArray(), blocks);
    }

    static TiffLayoutEntryInput OffsetEntry(ushort tag, bool bigTiff, RawByteOrder byteOrder, string blockName)
    {
        TiffLayoutReference[] references = new TiffLayoutReference[] { new TiffLayoutReference(0, TiffLayoutReferenceKind.Block, blockName) };
        if (bigTiff)
        {
            return new TiffLayoutEntryInput(tag, TiffFieldType.Long8, 1, UInt64Bytes(0, byteOrder), references);
        }
        return new TiffLayoutEntryInput(tag, TiffFieldType.Long, 1, UInt32Bytes(0, byteOrder), references);
    }

    static TiffLayoutEntryInput ShortEntry(ushort tag, ushort value, RawByteOrder byteOrder)
    {
        byte[] bytes = new byte[2];
        EndianWriter.WriteUInt16(bytes, value, byteOrder);
        return new TiffLayoutEntryInput(tag, TiffFieldType.Short, 1, bytes, null);
    }

    static TiffLayoutEntryInput LongEntry(ushort tag, uint value, RawByteOrder byteOrder)
    {
        return new TiffLayoutEntryInput(tag, TiffFieldType.Long, 1, UInt32Bytes(value, byteOrder), null);
    }

    static TiffLayoutEntryInput Long8Entry(ushort tag, ulong value, RawByteOrder byteOrder)
    {
        return new TiffLayoutEntryInput(tag, TiffFieldType.Long8, 1, UInt64Bytes(value, byteOrder), null);
    }

    static TiffLayoutEntryInput AsciiEntry(ushort tag, string value)
    {
        byte[] bytes = System.Text.Encoding.ASCII.GetBytes(value);
        return new TiffLayoutEntryInput(tag, TiffFieldType.Ascii, (ulong)bytes.Length, bytes, null);
    }

    static TiffLayoutEntryInput UndefinedEntry(ushort tag, byte[] value)
    {
        return new TiffLayoutEntryInput(tag, TiffFieldType.Undefined, (ulong)value.Length, value, null);
    }

    static byte[] UInt32Bytes(uint value, RawByteOrder byteOrder)
    {
        byte[] bytes = new byte[4];
        EndianWriter.WriteUInt32(bytes, value, byteOrder);
        return bytes;
    }

    static byte[] UInt64Bytes(ulong value, RawByteOrder byteOrder)
    {
        byte[] bytes = new byte[8];
        EndianWriter.WriteUInt64(bytes, value, byteOrder);
        return bytes;
    }

    static byte[] Write(TiffLayoutRequest request)
    {
        TiffLayoutPlan plan = TiffLayoutPlanner.Plan(request, new RawLimits(), CancellationToken.None);
        return TiffWriter.WriteToArray(plan, new RawLimits(), CancellationToken.None);
    }

    static TiffIfdGraph Parse(byte[] source)
    {
        return TiffParser.Parse(new BoundedInput(new ReadOnlyMemory<byte>(source), 0, source.Length, new RawLimits()));
    }

    static byte[] ReadPayload(TiffIfdGraph graph, byte[] source)
    {
        TiffDataBlock block = graph.Images.Single().Blocks.Single();
        return source.AsSpan(checked((int)block.Offset), checked((int)block.ByteCount)).ToArray();
    }

    static string? FindExecutable(string name)
    {
        string executableName = OperatingSystem.IsWindows() ? name + ".exe" : name;
        string? pathValue = Environment.GetEnvironmentVariable("PATH");
        if (pathValue != null)
        {
            foreach (string directory in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                string candidate = Path.Combine(directory, executableName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }
        return null;
    }

    static ExternalResult RunExternal(string? executable, string[] arguments, byte[] source, string extension)
    {
        string directory = Path.Combine(Path.GetTempPath(), "raw-tiff-interop-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "generated" + extension);
        try
        {
            File.WriteAllBytes(path, source);
            ProcessStartInfo start = new ProcessStartInfo((executable ?? throw new System.InvalidOperationException("A required value is unavailable.")));
            for (int index = 0; index < arguments.Length; index++)
            {
                start.ArgumentList.Add(arguments[index]);
            }
            start.ArgumentList.Add(path);
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;
            start.UseShellExecute = false;
            using Process? process = Process.Start(start);
            string output = (process ?? throw new System.InvalidOperationException("A required value is unavailable.")).StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            return new ExternalResult(process.ExitCode, output, error);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    class ExternalResult
    {
        public int ExitCode;
        public string StandardOutput;
        public string StandardError;

        public ExternalResult(int exitCode, string standardOutput, string standardError)
        {
            ExitCode = exitCode;
            StandardOutput = standardOutput;
            StandardError = standardError;
        }
    }

    class InteropNonSeekableStream : Stream
    {
        MemoryStream Inner;

        public InteropNonSeekableStream()
        {
            Inner = new MemoryStream();
        }

        public override bool CanRead
        {
            get
            {
                return false;
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
                return true;
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
            throw new NotSupportedException();
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
            Inner.Write(buffer, offset, count);
        }

        public byte[] ToArray()
        {
            return Inner.ToArray();
        }
    }
}
