using System.Text;

namespace ftp.Tests;

public class FtpTransferPipelineTransformTests
{
    [Test]
    public void ASCII_EBCDIC_and_local_byte_representations_round_trip_fragmented_input()
    {
        FtpTransferOptions ascii = CreateOptions(
            FtpTransferType.Ascii,
            FtpFileStructure.File,
            FtpTransferMode.Stream);
        FtpTransferOptions ebcdic = CreateOptions(
            FtpTransferType.Ebcdic,
            FtpFileStructure.File,
            FtpTransferMode.Stream);
        FtpTransferOptions local = CreateOptions(
            FtpTransferType.LocalByte,
            FtpFileStructure.File,
            FtpTransferMode.Stream);
        local.HasLocalByteSize = true;
        local.LocalByteSize = 3;
        byte[] asciiContent = Encoding.ASCII.GetBytes("one\ntwo\rthree\n");
        byte[] ebcdicContent = Encoding.UTF8.GetBytes("ONE\nTWO\n");
        byte[] localContent = new byte[] { 5, 3 };

        byte[] asciiWire = Transform(ascii, true, asciiContent, 1);
        byte[] ebcdicWire = Transform(ebcdic, true, ebcdicContent, 2);
        byte[] localWire = Transform(local, true, localContent, 1);

        Assert.Multiple(() =>
        {
            Assert.That(Transform(ascii, false, asciiWire, 1),
                Is.EqualTo(asciiContent));
            Assert.That(Transform(ebcdic, false, ebcdicWire, 1),
                Is.EqualTo(ebcdicContent));
            Assert.That(Transform(local, false, localWire, 1),
                Is.EqualTo(localContent));
            Assert.That(asciiWire,
                Is.EqualTo(Encoding.ASCII.GetBytes(
                    "one\r\ntwo\r\0three\r\n")));
            Assert.That(localWire, Is.EqualTo(new byte[] { 0xAC }));
        });
    }

    [TestCase(FtpTransferMode.Stream)]
    [TestCase(FtpTransferMode.Block)]
    [TestCase(FtpTransferMode.Compressed)]
    [TestCase(FtpTransferMode.Deflate)]
    public void Record_structure_round_trips_across_supported_modes(
        FtpTransferMode mode)
    {
        FtpTransferOptions options = CreateOptions(
            FtpTransferType.Ascii,
            FtpFileStructure.Record,
            mode);
        byte[] content = Encoding.ASCII.GetBytes("first\nsecond\n");

        byte[] wire = Transform(options, true, content, 2);
        byte[] decoded = Transform(options, false, wire, 1);

        Assert.That(decoded, Is.EqualTo(content));
    }

    [TestCase(FtpTransferMode.Stream)]
    [TestCase(FtpTransferMode.Block)]
    [TestCase(FtpTransferMode.Compressed)]
    [TestCase(FtpTransferMode.Deflate)]
    public void Page_structure_round_trips_serialized_pages_across_supported_modes(
        FtpTransferMode mode)
    {
        FtpTransferOptions options = CreateOptions(
            FtpTransferType.Image,
            FtpFileStructure.Page,
            mode);
        byte[] pages = new byte[]
        {
            4, 1, 3, 1, 10, 20, 30,
            4, 0, 0, 0
        };

        byte[] wire = Transform(options, true, pages, 2);
        byte[] decoded = Transform(options, false, wire, 1);

        Assert.That(decoded, Is.EqualTo(pages));
    }

    [Test]
    public void Block_restart_marker_is_consumed_without_becoming_file_data()
    {
        FtpTransferOptions options = CreateOptions(
            FtpTransferType.Image,
            FtpFileStructure.File,
            FtpTransferMode.Block);
        byte[] content = Encoding.ASCII.GetBytes("payload");
        byte[] dataWire = Transform(options, true, content, 3);
        FtpBlockModeState markerState =
            BlockModeCodec.CreateFramingState(options);
        FtpBlockModeBlock marker = markerState.EncodeRestartMarker(
            "F1:0:0:0:0:0");
        byte[] wire = Join(marker.SerializedBytes, dataWire);

        byte[] decoded = Transform(options, false, wire, 1);

        Assert.That(decoded, Is.EqualTo(content));
    }

    private static byte[] Transform(
        FtpTransferOptions options,
        bool encoding,
        byte[] input,
        int chunkBytes)
    {
        FtpTransferPipelineTransform transform =
            new FtpTransferPipelineTransform(options, encoding, null, 0);
        List<byte> output = new List<byte>();
        int offset = 0;
        while (offset < input.Length)
        {
            int count = input.Length - offset;
            if (count > chunkBytes)
            {
                count = chunkBytes;
            }
            byte[] chunk = new byte[count];
            Array.Copy(input, offset, chunk, 0, count);
            Append(output, transform.TransformChunk(
                chunk, count, false));
            offset += count;
        }
        Append(output, transform.TransformChunk(
            Array.Empty<byte>(), 0, true));
        return output.ToArray();
    }

    private static void Append(
        List<byte> destination,
        FtpTransferPumpChunk[] chunks)
    {
        int index = 0;
        while (index < chunks.Length)
        {
            FtpTransferPumpChunk chunk = chunks[index];
            Assert.That(chunk.Data, Is.Not.Null);
            byte[] data = chunk.Data ?? Array.Empty<byte>();
            int byteIndex = 0;
            while (byteIndex < chunk.Count)
            {
                destination.Add(data[byteIndex]);
                byteIndex++;
            }
            index++;
        }
    }

    private static byte[] Join(byte[] first, byte[] second)
    {
        byte[] joined = new byte[first.Length + second.Length];
        Array.Copy(first, 0, joined, 0, first.Length);
        Array.Copy(second, 0, joined, first.Length, second.Length);
        return joined;
    }

    private static FtpTransferOptions CreateOptions(
        FtpTransferType type,
        FtpFileStructure structure,
        FtpTransferMode mode)
    {
        FtpTransferOptions options = new FtpTransferOptions();
        options.Type = type;
        options.Structure = structure;
        options.Mode = mode;
        options.VerificationMode = FtpVerificationMode.None;
        options.BufferBytes = 3;
        return options;
    }
}
