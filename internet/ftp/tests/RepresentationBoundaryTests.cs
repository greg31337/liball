using System.Text;

namespace ftp.Tests;

public class RepresentationBoundaryTests
{
    [Test]
    public void TYPE_A_FORMAT_N_round_trips_at_every_local_and_wire_boundary()
    {
        byte[] local = Join(
            new byte[] { 65 },
            Encoding.ASCII.GetBytes(Environment.NewLine),
            new byte[] { 66, 13, 67, 0, 68 });
        byte[] wire = new byte[]
        {
            65, 13, 10, 66, 13, 0, 67, 0, 68
        };

        AssertEveryRoundTrip(
            CreateOptions(FtpTransferType.Ascii, FtpTextFormat.NonPrint, 0),
            local, wire);
    }

    [Test]
    public void TYPE_A_FORMAT_T_round_trips_vertical_controls_at_every_boundary()
    {
        byte[] local = Join(
            new byte[] { 65, 11, 12 },
            Encoding.ASCII.GetBytes(Environment.NewLine),
            new byte[] { 66, 13, 67 });
        byte[] wire = new byte[]
        {
            65, 11, 12, 13, 10, 66, 13, 0, 67
        };

        AssertEveryRoundTrip(
            CreateOptions(FtpTransferType.Ascii, FtpTextFormat.Telnet, 0),
            local, wire);
    }

    [Test]
    public void TYPE_A_FORMAT_C_round_trips_all_ASA_controls_at_every_boundary()
    {
        byte[] newline = Encoding.ASCII.GetBytes(Environment.NewLine);
        byte[] local = Join(
            new byte[] { 32, 65 }, newline,
            new byte[] { 48, 66 }, newline,
            new byte[] { 49, 67 }, newline,
            new byte[] { 43, 68 });
        byte[] wire = new byte[]
        {
            32, 65, 13, 10,
            48, 66, 13, 10,
            49, 67, 13, 10,
            43, 68
        };

        AssertEveryRoundTrip(
            CreateOptions(FtpTransferType.Ascii, FtpTextFormat.CarriageControl, 0),
            local, wire);
    }

    [Test]
    public void TYPE_E_FORMAT_N_round_trips_multibyte_text_at_every_boundary()
    {
        byte[] local = Join(
            Encoding.UTF8.GetBytes("A"),
            Encoding.UTF8.GetBytes(Environment.NewLine),
            Encoding.UTF8.GetBytes("£éÄñ"));
        byte[] wire = new byte[]
        {
            0xC1, 0x15, 0xB1, 0x51, 0x63, 0x49
        };

        AssertEveryRoundTrip(
            CreateOptions(FtpTransferType.Ebcdic, FtpTextFormat.NonPrint, 0),
            local, wire);
    }

    [Test]
    public void TYPE_E_FORMAT_T_round_trips_vertical_controls_at_every_boundary()
    {
        byte[] local = Encoding.UTF8.GetBytes("A\r\v\f\n\u0085B");
        byte[] wire = new byte[]
        {
            0xC1, 0x0D, 0x0B, 0x0C, 0x25, 0x15, 0xC2
        };

        AssertEveryRoundTrip(
            CreateOptions(FtpTransferType.Ebcdic, FtpTextFormat.Telnet, 0),
            local, wire);
    }

    [Test]
    public void TYPE_E_FORMAT_C_round_trips_all_ASA_controls_at_every_boundary()
    {
        byte[] local = Encoding.UTF8.GetBytes(
            " A" + Environment.NewLine +
            "0B" + Environment.NewLine +
            "1C" + Environment.NewLine + "+D");
        byte[] wire = new byte[]
        {
            0x40, 0xC1, 0x15,
            0xF0, 0xC2, 0x15,
            0xF1, 0xC3, 0x15,
            0x4E, 0xC4
        };

        AssertEveryRoundTrip(
            CreateOptions(FtpTransferType.Ebcdic, FtpTextFormat.CarriageControl, 0),
            local, wire);
    }

    [Test]
    public void TYPE_I_and_TYPE_L_8_preserve_every_octet_at_every_boundary()
    {
        byte[] values = new byte[256];
        for (int index = 0; index < values.Length; index++)
        {
            values[index] = (byte)index;
        }

        AssertEveryRoundTrip(
            CreateOptions(FtpTransferType.Image, FtpTextFormat.NonPrint, 0),
            values, values);
        AssertEveryRoundTrip(
            CreateOptions(FtpTransferType.LocalByte, FtpTextFormat.NonPrint, 8),
            values, values);
    }

    [TestCase(3)]
    [TestCase(7)]
    [TestCase(9)]
    [TestCase(12)]
    [TestCase(36)]
    [TestCase(255)]
    public void TYPE_L_non_eight_bit_values_round_trip_at_every_boundary(
        int localByteSize)
    {
        byte[] local = CreateLocalByteValues(localByteSize, 8);
        FtpTransferOptions options = CreateOptions(
            FtpTransferType.LocalByte, FtpTextFormat.NonPrint, localByteSize);
        byte[] wire = ConvertOneChunk(options, true, local);

        Assert.Multiple(() =>
        {
            Assert.That(wire, Has.Length.EqualTo(localByteSize),
                "Eight logical values must occupy exactly the bit width in octets.");
            AssertEveryRoundTrip(options, local, wire);
        });
    }

    [Test]
    public void TYPE_A_rejects_invalid_network_sequences_at_every_boundary()
    {
        FtpTransferOptions nonPrint = CreateOptions(
            FtpTransferType.Ascii, FtpTextFormat.NonPrint, 0);
        byte[][] malformed =
        {
            new byte[] { 65, 13 },
            new byte[] { 65, 13, 66 },
            new byte[] { 65, 128, 66 }
        };

        for (int input = 0; input < malformed.Length; input++)
        {
            AssertInvalidAtEverySplit(nonPrint, false, malformed[input],
                "TYPE A malformed sequence " + input);
        }
    }

    [Test]
    public void FORMAT_C_rejects_invalid_ASCII_and_EBCDIC_record_controls_at_every_boundary()
    {
        FtpTransferOptions ascii = CreateOptions(
            FtpTransferType.Ascii, FtpTextFormat.CarriageControl, 0);
        FtpTransferOptions ebcdic = CreateOptions(
            FtpTransferType.Ebcdic, FtpTextFormat.CarriageControl, 0);
        byte[] invalidAsciiLocal = Join(
            new byte[] { 32, 65 },
            Encoding.ASCII.GetBytes(Environment.NewLine),
            new byte[] { 88, 66 });
        byte[] invalidAsciiWire = new byte[]
        {
            32, 65, 13, 10, 88, 66
        };
        byte[] invalidEbcdicLocal = Join(
            Encoding.UTF8.GetBytes(" A"),
            Encoding.UTF8.GetBytes(Environment.NewLine),
            Encoding.UTF8.GetBytes("XB"));
        byte[] invalidEbcdicWire = new byte[]
        {
            0x40, 0xC1, 0x15, 0xE7, 0xC2
        };

        AssertInvalidAtEverySplit(ascii, true, invalidAsciiLocal,
            "TYPE A FORMAT C local controls");
        AssertInvalidAtEverySplit(ascii, false, invalidAsciiWire,
            "TYPE A FORMAT C wire controls");
        AssertInvalidAtEverySplit(ebcdic, true, invalidEbcdicLocal,
            "TYPE E FORMAT C local controls");
        AssertInvalidAtEverySplit(ebcdic, false, invalidEbcdicWire,
            "TYPE E FORMAT C wire controls");
    }

    [Test]
    public void TYPE_E_rejects_malformed_and_unmappable_UTF8_at_every_boundary()
    {
        FtpTransferOptions options = CreateOptions(
            FtpTransferType.Ebcdic, FtpTextFormat.NonPrint, 0);
        byte[][] malformed =
        {
            new byte[] { 0xC2 },
            new byte[] { 0xC2, 0x41 },
            new byte[] { 0xC0, 0xAF },
            new byte[] { 0xED, 0xA0, 0x80 },
            new byte[] { 0xF4, 0x90, 0x80, 0x80 },
            Encoding.UTF8.GetBytes("€")
        };

        for (int input = 0; input < malformed.Length; input++)
        {
            AssertInvalidAtEverySplit(options, true, malformed[input],
                "TYPE E malformed or unmappable input " + input);
        }
    }

    [Test]
    public void TYPE_L_rejects_incomplete_values_and_nonzero_padding_at_every_boundary()
    {
        FtpTransferOptions options = CreateOptions(
            FtpTransferType.LocalByte, FtpTextFormat.NonPrint, 12);

        AssertInvalidAtEverySplit(options, true, new byte[] { 0x0A },
            "TYPE L incomplete stored value");
        AssertInvalidAtEverySplit(options, true, new byte[] { 0xFA, 0xBC },
            "TYPE L nonzero stored padding");
        AssertInvalidAtEverySplit(options, false, new byte[] { 0xAB },
            "TYPE L truncated wire value");
        AssertInvalidAtEverySplit(options, false, new byte[] { 0xAB, 0xC1 },
            "TYPE L nonzero wire padding");
    }

    private static void AssertEveryRoundTrip(
        FtpTransferOptions options,
        byte[] local,
        byte[] wire)
    {
        for (int split = 0; split <= local.Length; split++)
        {
            Assert.That(ConvertAtSplit(options, true, local, split),
                Is.EqualTo(wire), "encoding split " + split);
        }
        for (int split = 0; split <= wire.Length; split++)
        {
            Assert.That(ConvertAtSplit(options, false, wire, split),
                Is.EqualTo(local), "decoding split " + split);
        }
    }

    private static void AssertInvalidAtEverySplit(
        FtpTransferOptions options,
        bool encoding,
        byte[] input,
        string label)
    {
        for (int split = 0; split <= input.Length; split++)
        {
            int capturedSplit = split;
            Assert.Throws<InvalidDataException>(() =>
                ConvertAtSplit(options, encoding, input, capturedSplit),
                label + ", split " + capturedSplit);
        }
    }

    private static byte[] ConvertOneChunk(
        FtpTransferOptions options,
        bool encoding,
        byte[] input)
    {
        FtpRepresentationTransform transform =
            new FtpRepresentationTransform(options, encoding);
        return transform.ConvertChunk(input, input.Length, true);
    }

    private static byte[] ConvertAtSplit(
        FtpTransferOptions options,
        bool encoding,
        byte[] input,
        int split)
    {
        FtpRepresentationTransform transform =
            new FtpRepresentationTransform(options, encoding);
        byte[] first = Slice(input, 0, split);
        byte[] second = Slice(input, split, input.Length - split);
        byte[] firstOutput = transform.ConvertChunk(first, first.Length, false);
        byte[] secondOutput = transform.ConvertChunk(second, second.Length, true);
        return Join(firstOutput, secondOutput);
    }

    private static FtpTransferOptions CreateOptions(
        FtpTransferType type,
        FtpTextFormat format,
        int localByteSize)
    {
        FtpTransferOptions options = new FtpTransferOptions();
        options.Type = type;
        options.TextFormat = format;
        options.BufferBytes = 3;
        if (type == FtpTransferType.LocalByte)
        {
            options.HasLocalByteSize = true;
            options.LocalByteSize = localByteSize;
        }
        return options;
    }

    private static byte[] CreateLocalByteValues(
        int localByteSize,
        int logicalByteCount)
    {
        int storageBytes = (localByteSize + 7) / 8;
        int unusedHighBits = storageBytes * 8 - localByteSize;
        byte[] values = new byte[storageBytes * logicalByteCount];
        for (int logical = 0; logical < logicalByteCount; logical++)
        {
            for (int storage = 0; storage < storageBytes; storage++)
            {
                values[logical * storageBytes + storage] =
                    (byte)(17 + logical * 29 + storage * 43);
            }
            if (unusedHighBits != 0)
            {
                values[logical * storageBytes] = (byte)(
                    values[logical * storageBytes] & (0xFF >> unusedHighBits));
            }
        }
        return values;
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
