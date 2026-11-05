using System.Text;

namespace ftp.Tests;

public class StructureModeMatrixTests
{
    [TestCase(FtpFileStructure.File, FtpTransferMode.Stream)]
    [TestCase(FtpFileStructure.File, FtpTransferMode.Block)]
    [TestCase(FtpFileStructure.File, FtpTransferMode.Compressed)]
    [TestCase(FtpFileStructure.Record, FtpTransferMode.Stream)]
    [TestCase(FtpFileStructure.Record, FtpTransferMode.Block)]
    [TestCase(FtpFileStructure.Record, FtpTransferMode.Compressed)]
    [TestCase(FtpFileStructure.Page, FtpTransferMode.Stream)]
    [TestCase(FtpFileStructure.Page, FtpTransferMode.Block)]
    [TestCase(FtpFileStructure.Page, FtpTransferMode.Compressed)]
    public void Every_STRU_and_MODE_combination_round_trips_at_every_boundary(
        FtpFileStructure structure,
        FtpTransferMode mode)
    {
        FtpTransferOptions options = CreateOptions(structure, mode);
        byte[] local = CreateStructuredContent(structure);

        for (int localSplit = 0; localSplit <= local.Length; localSplit++)
        {
            byte[] wire = TransformAtSplit(
                options, true, local, localSplit);
            for (int wireSplit = 0; wireSplit <= wire.Length; wireSplit++)
            {
                byte[] decoded = TransformAtSplit(
                    options, false, wire, wireSplit);
                Assert.That(decoded, Is.EqualTo(local),
                    structure + "/" + mode +
                    ", local split " + localSplit +
                    ", wire split " + wireSplit);
            }
        }
    }

    [Test]
    public void MODE_S_uses_connection_EOF_record_EOR_EOF_and_last_page_headers()
    {
        FtpTransferOptions file = CreateOptions(
            FtpFileStructure.File, FtpTransferMode.Stream);
        FtpTransferOptions record = CreateOptions(
            FtpFileStructure.Record, FtpTransferMode.Stream);
        FtpTransferOptions page = CreateOptions(
            FtpFileStructure.Page, FtpTransferMode.Stream);
        byte[] fileData = new byte[] { 1, 0xFF, 2 };
        byte[] recordData = Join(
            new byte[] { 65 },
            Encoding.ASCII.GetBytes(Environment.NewLine));
        byte[] pageData = new byte[]
        {
            4, 1, 1, 1, 9,
            4, 0, 0, 0
        };

        Assert.Multiple(() =>
        {
            Assert.That(TransformOneChunk(file, true, fileData),
                Is.EqualTo(fileData));
            Assert.That(TransformOneChunk(record, true, recordData),
                Is.EqualTo(new byte[] { 65, 0xFF, 1, 0xFF, 2 }));
            Assert.That(TransformOneChunk(page, true, pageData),
                Is.EqualTo(pageData));
        });
    }

    [Test]
    public void MODE_B_uses_big_endian_headers_and_structure_specific_EOF_EOR_bits()
    {
        FtpTransferOptions file = CreateOptions(
            FtpFileStructure.File, FtpTransferMode.Block);
        FtpTransferOptions record = CreateOptions(
            FtpFileStructure.Record, FtpTransferMode.Block);
        FtpTransferOptions page = CreateOptions(
            FtpFileStructure.Page, FtpTransferMode.Block);
        byte[] recordData = Join(
            new byte[] { 65 },
            Encoding.ASCII.GetBytes(Environment.NewLine));
        byte[] pageData = new byte[]
        {
            4, 1, 1, 1, 9,
            4, 0, 0, 0
        };

        Assert.Multiple(() =>
        {
            Assert.That(TransformOneChunk(file, true, new byte[] { 10, 11 }),
                Is.EqualTo(new byte[] { 0x40, 0, 2, 10, 11 }));
            Assert.That(TransformOneChunk(record, true, recordData),
                Is.EqualTo(new byte[]
                {
                    0x80, 0, 1, 65,
                    0x40, 0, 0
                }));
            Assert.That(TransformOneChunk(page, true, pageData), Is.EqualTo(
                new byte[]
                {
                    0, 0, 5, 4, 1, 1, 1, 9,
                    0x40, 0, 4, 4, 0, 0, 0
                }));
        });
    }

    [Test]
    public void MODE_C_uses_filler_replicate_literal_and_EOF_descriptor_forms()
    {
        FtpTransferOptions options = CreateOptions(
            FtpFileStructure.File, FtpTransferMode.Compressed);
        byte[] local = new byte[] { 0, 0, 0, 7, 7, 8 };
        byte[] expected = new byte[]
        {
            0xC3,
            0x82, 7,
            0, 0x40, 1, 8
        };
        byte[] wire = TransformOneChunk(options, true, local);

        Assert.Multiple(() =>
        {
            Assert.That(wire, Is.EqualTo(expected));
            for (int split = 0; split <= wire.Length; split++)
            {
                Assert.That(TransformAtSplit(options, false, wire, split),
                    Is.EqualTo(local), "compressed split " + split);
            }
        });
    }

    [TestCase(FtpTransferMode.Block)]
    [TestCase(FtpTransferMode.Compressed)]
    public void Restart_markers_are_consumed_without_entering_file_data_at_every_boundary(
        FtpTransferMode mode)
    {
        FtpTransferOptions options = CreateOptions(
            FtpFileStructure.File, mode);
        byte[] local = new byte[] { 11, 22, 33, 44 };
        byte[] dataWire = TransformOneChunk(options, true, local);
        byte[] markerWire;
        if (mode == FtpTransferMode.Block)
        {
            FtpBlockModeState state =
                BlockModeCodec.CreateFramingState(options);
            markerWire = state.EncodeRestartMarker(
                "F1:4:7:1:0:0").SerializedBytes;
        }
        else
        {
            FtpCompressedModeState state =
                CompressedModeCodec.CreateFramingState(options);
            markerWire = state.EncodeRestartMarker(
                "F1:4:7:1:0:0").EncodedBytes;
        }
        byte[] wire = Join(markerWire, dataWire);

        for (int split = 0; split <= wire.Length; split++)
        {
            Assert.That(TransformAtSplit(options, false, wire, split),
                Is.EqualTo(local), mode + " restart-marker split " + split);
        }
    }

    [TestCase(FtpFileStructure.Record, FtpTransferMode.Stream)]
    [TestCase(FtpFileStructure.Page, FtpTransferMode.Stream)]
    [TestCase(FtpFileStructure.File, FtpTransferMode.Block)]
    [TestCase(FtpFileStructure.Record, FtpTransferMode.Block)]
    [TestCase(FtpFileStructure.Page, FtpTransferMode.Block)]
    [TestCase(FtpFileStructure.File, FtpTransferMode.Compressed)]
    [TestCase(FtpFileStructure.Record, FtpTransferMode.Compressed)]
    [TestCase(FtpFileStructure.Page, FtpTransferMode.Compressed)]
    public void Malformed_or_truncated_framed_streams_are_rejected(
        FtpFileStructure structure,
        FtpTransferMode mode)
    {
        FtpTransferOptions options = CreateOptions(structure, mode);
        byte[] malformed;
        if (mode == FtpTransferMode.Stream)
        {
            malformed = structure == FtpFileStructure.Record ?
                new byte[] { 0xFF } :
                new byte[] { 4, 1, 1, 1, 9 };
        }
        else if (mode == FtpTransferMode.Block)
        {
            malformed = new byte[] { 0x40, 0, 1 };
        }
        else
        {
            malformed = new byte[] { 0 };
        }

        Assert.Throws<FtpTransferException>(() =>
            TransformOneChunk(options, false, malformed),
            structure + "/" + mode);
    }

    [Test]
    public void MODE_S_file_stream_rejects_data_after_connection_EOF_completion()
    {
        FtpTransferOptions options = CreateOptions(
            FtpFileStructure.File, FtpTransferMode.Stream);
        FtpTransferPipelineTransform transform =
            new FtpTransferPipelineTransform(options, false, null, 0);

        transform.TransformChunk(new byte[] { 1, 2 }, 2, true);

        Assert.Throws<FtpTransferException>(() =>
            transform.TransformChunk(new byte[] { 3 }, 1, false));
    }

    private static byte[] CreateStructuredContent(
        FtpFileStructure structure)
    {
        if (structure == FtpFileStructure.Record)
        {
            return Encoding.ASCII.GetBytes(
                "first" + Environment.NewLine +
                "second" + Environment.NewLine);
        }
        if (structure == FtpFileStructure.Page)
        {
            return new byte[]
            {
                4, 1, 3, 1, 10, 20, 30,
                4, 2, 2, 1, 40, 50,
                4, 0, 0, 0
            };
        }
        return new byte[]
        {
            0, 0, 0, 7, 7, 8, 0xFF, 13, 10
        };
    }

    private static FtpTransferOptions CreateOptions(
        FtpFileStructure structure,
        FtpTransferMode mode)
    {
        FtpTransferOptions options = new FtpTransferOptions();
        options.Structure = structure;
        options.Mode = mode;
        options.Type = structure == FtpFileStructure.Record ?
            FtpTransferType.Ascii : FtpTransferType.Image;
        options.BufferBytes = 3;
        options.VerificationMode = FtpVerificationMode.None;
        return options;
    }

    private static byte[] TransformOneChunk(
        FtpTransferOptions options,
        bool encoding,
        byte[] input)
    {
        FtpTransferPipelineTransform transform =
            new FtpTransferPipelineTransform(options, encoding, null, 0);
        return Collect(transform.TransformChunk(
            input, input.Length, true));
    }

    private static byte[] TransformAtSplit(
        FtpTransferOptions options,
        bool encoding,
        byte[] input,
        int split)
    {
        FtpTransferPipelineTransform transform =
            new FtpTransferPipelineTransform(options, encoding, null, 0);
        byte[] first = Slice(input, 0, split);
        byte[] second = Slice(input, split, input.Length - split);
        byte[] firstOutput = Collect(transform.TransformChunk(
            first, first.Length, false));
        byte[] secondOutput = Collect(transform.TransformChunk(
            second, second.Length, true));
        return Join(firstOutput, secondOutput);
    }

    private static byte[] Collect(FtpTransferPumpChunk[] chunks)
    {
        List<byte> output = new List<byte>();
        for (int index = 0; index < chunks.Length; index++)
        {
            FtpTransferPumpChunk chunk = chunks[index];
            byte[] data = chunk.Data ?? Array.Empty<byte>();
            for (int byteIndex = 0; byteIndex < chunk.Count; byteIndex++)
            {
                output.Add(data[byteIndex]);
            }
        }
        return output.ToArray();
    }

    private static byte[] Slice(byte[] source, int offset, int count)
    {
        byte[] result = new byte[count];
        Array.Copy(source, offset, result, 0, count);
        return result;
    }

    private static byte[] Join(params byte[][] parts)
    {
        int length = 0;
        for (int index = 0; index < parts.Length; index++)
        {
            length += parts[index].Length;
        }
        byte[] result = new byte[length];
        int offset = 0;
        for (int index = 0; index < parts.Length; index++)
        {
            Array.Copy(parts[index], 0, result, offset, parts[index].Length);
            offset += parts[index].Length;
        }
        return result;
    }
}
