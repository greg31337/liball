namespace barcode.Tests;

public class Pdf417MacroCodecTests
{
    [Test]
    public void PublishedRequiredHeaderVectorEncodesAndDecodesExactly()
    {
        Pdf417Options options = CreateOptions(0, 0, "100200300", false);

        int[] encoded = Pdf417MacroCodec.EncodeControlBlock(options);
        Pdf417MacroBlock decoded = Pdf417MacroCodec.DecodeControlBlock(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(encoded, Is.EqualTo(new int[]
            {
                928, 111, 100, 100, 200, 300
            }));
            Assert.That(decoded.Macro.Format, Is.EqualTo(BarcodeFormat.MacroPdf417));
            Assert.That(decoded.Macro.Index, Is.EqualTo(0));
            Assert.That(decoded.Macro.Count, Is.EqualTo(0));
            Assert.That(decoded.Macro.FileIdentifier, Is.EqualTo("100200300"));
            Assert.That(decoded.Macro.IsLastSegment, Is.False);
            Assert.That(decoded.Codewords, Is.EqualTo(encoded));
        });
    }

    [Test]
    public void PublishedOptionalFieldExampleUsesCanonicalFieldCompaction()
    {
        Pdf417Options options = CreateOptions(3, 4, "017053", true);
        options.Sender = "CEN BE";
        options.Addressee = "ISO CH";

        int[] encoded = Pdf417MacroCodec.EncodeControlBlock(options);
        Pdf417MacroBlock decoded = Pdf417MacroCodec.DecodeControlBlock(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(encoded, Is.EqualTo(new int[]
            {
                928, 111, 103, 17, 53,
                923, 1, 111, 104,
                923, 3, 64, 416, 34,
                923, 4, 258, 446, 67,
                922
            }));
            Assert.That(decoded.Macro.Index, Is.EqualTo(3));
            Assert.That(decoded.Macro.Count, Is.EqualTo(4));
            Assert.That(decoded.Macro.FileIdentifier, Is.EqualTo("017053"));
            Assert.That(decoded.Macro.IsLastSegment, Is.True);
            Assert.That(decoded.Sender, Is.EqualTo("CEN BE"));
            Assert.That(decoded.Addressee, Is.EqualTo("ISO CH"));
        });
    }

    [Test]
    public void EveryDefinedOptionalFieldRoundTripsWithPresenceMetadata()
    {
        Pdf417Options options = CreateOptions(1, 3, "000899042", false);
        options.FileName = "manifest-01.txt";
        options.Timestamp = 1_725_000_000;
        options.Sender = "Warehouse A";
        options.Addressee = "Store #12";
        options.FileSize = 4_294_967_296;
        options.FileChecksum = 65535;

        int[] encoded = Pdf417MacroCodec.EncodeControlBlock(options);
        Pdf417MacroBlock decoded = Pdf417MacroCodec.DecodeControlBlock(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(GetFieldDesignators(encoded), Is.EqualTo(new int[]
            {
                0, 1, 2, 3, 4, 5, 6
            }));
            Assert.That(decoded.Macro.Index, Is.EqualTo(1));
            Assert.That(decoded.Macro.Count, Is.EqualTo(3));
            Assert.That(decoded.Macro.FileIdentifier, Is.EqualTo("000899042"));
            Assert.That(decoded.Macro.IsLastSegment, Is.False);
            Assert.That(decoded.FileName, Is.EqualTo("manifest-01.txt"));
            Assert.That(decoded.HasTimestamp, Is.True);
            Assert.That(decoded.Timestamp, Is.EqualTo(1_725_000_000));
            Assert.That(decoded.Sender, Is.EqualTo("Warehouse A"));
            Assert.That(decoded.Addressee, Is.EqualTo("Store #12"));
            Assert.That(decoded.HasFileSize, Is.True);
            Assert.That(decoded.FileSize, Is.EqualTo(4_294_967_296));
            Assert.That(decoded.HasFileChecksum, Is.True);
            Assert.That(decoded.FileChecksum, Is.EqualTo(65535));
            Assert.That(encoded[^1], Is.Not.EqualTo(Pdf417MacroCodec.TerminatorCodeword));
        });

        encoded[0] = 0;
        options.Macro!.FileIdentifier = "changed";
        Assert.Multiple(() =>
        {
            Assert.That(decoded.Codewords[0], Is.EqualTo(Pdf417MacroCodec.MacroMarkerCodeword));
            Assert.That(decoded.Macro.FileIdentifier, Is.EqualTo("000899042"));
        });
    }

    [Test]
    public void DecoderDistinguishesPresentZeroNumericFieldsFromAbsentFields()
    {
        int[] zeroTimestamp = Pdf417NumericCompaction.EncodeDigits("00000000000").DataCodewords;
        int[] zeroFileSize = Pdf417NumericCompaction.EncodeDigits("0").DataCodewords;
        int[] zeroChecksum = Pdf417NumericCompaction.EncodeDigits("00000").DataCodewords;
        List<int> values = new List<int>
        {
            928, 111, 100, 129,
            923, 2
        };
        values.AddRange(zeroTimestamp);
        values.Add(923);
        values.Add(5);
        values.AddRange(zeroFileSize);
        values.Add(923);
        values.Add(6);
        values.AddRange(zeroChecksum);

        Pdf417MacroBlock decoded = Pdf417MacroCodec.DecodeControlBlock(values.ToArray());
        Pdf417MacroBlock absent = Pdf417MacroCodec.DecodeControlBlock(
            new int[] { 928, 111, 100, 129 });

        Assert.Multiple(() =>
        {
            Assert.That(decoded.HasTimestamp, Is.True);
            Assert.That(decoded.Timestamp, Is.EqualTo(0));
            Assert.That(decoded.HasFileSize, Is.True);
            Assert.That(decoded.FileSize, Is.EqualTo(0));
            Assert.That(decoded.HasFileChecksum, Is.True);
            Assert.That(decoded.FileChecksum, Is.EqualTo(0));
            Assert.That(absent.HasTimestamp, Is.False);
            Assert.That(absent.HasFileSize, Is.False);
            Assert.That(absent.HasFileChecksum, Is.False);
        });
    }

    [Test]
    public void FileIdentifierPreservesEveryThreeDigitBase900Group()
    {
        int[] encoded = Pdf417MacroCodec.EncodeFileIdentifier(
            "000001009099100899");
        Pdf417MacroBlock decoded = Pdf417MacroCodec.DecodeControlBlock(
            new int[] { 928, 111, 100, 0, 1, 9, 99, 100, 899 });

        Assert.Multiple(() =>
        {
            Assert.That(encoded, Is.EqualTo(new int[] { 0, 1, 9, 99, 100, 899 }));
            Assert.That(decoded.Macro.FileIdentifier, Is.EqualTo("000001009099100899"));
        });
    }

    [Test]
    public void InvalidEncodingMetadataAndOptionalValuesAreRejected()
    {
        Pdf417Options noMacro = new Pdf417Options();
        Pdf417Options wrongFormat = CreateOptions(0, 1, "001", true);
        wrongFormat.Macro!.Format = BarcodeFormat.DataMatrix;
        Pdf417Options highIndex = CreateOptions(99999, 0, "001", false);
        Pdf417Options inconsistentCount = CreateOptions(2, 2, "001", true);
        Pdf417Options inconsistentLast = CreateOptions(0, 2, "001", true);
        Pdf417Options highTimestamp = CreateOptions(0, 0, "001", false);
        highTimestamp.Timestamp = 100_000_000_000;
        Pdf417Options negativeSize = CreateOptions(0, 0, "001", false);
        negativeSize.FileSize = -1;
        Pdf417Options highChecksum = CreateOptions(0, 0, "001", false);
        highChecksum.FileChecksum = 65536;
        Pdf417Options emptyName = CreateOptions(0, 0, "001", false);
        emptyName.FileName = "";
        Pdf417Options nonAsciiSender = CreateOptions(0, 0, "001", false);
        nonAsciiSender.Sender = "caf\u00e9";

        Assert.Multiple(() =>
        {
            Assert.That(
                () => Pdf417MacroCodec.EncodeControlBlock(null!),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417MacroCodec.EncodeControlBlock(noMacro),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417MacroCodec.EncodeControlBlock(wrongFormat),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417MacroCodec.EncodeControlBlock(highIndex),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417MacroCodec.EncodeControlBlock(inconsistentCount),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417MacroCodec.EncodeControlBlock(inconsistentLast),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417MacroCodec.EncodeControlBlock(highTimestamp),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417MacroCodec.EncodeControlBlock(negativeSize),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417MacroCodec.EncodeControlBlock(highChecksum),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417MacroCodec.EncodeControlBlock(emptyName),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417MacroCodec.EncodeControlBlock(nonAsciiSender),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417MacroCodec.EncodeFileIdentifier(null),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417MacroCodec.EncodeFileIdentifier("12"),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417MacroCodec.EncodeFileIdentifier("ABC"),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417MacroCodec.EncodeFileIdentifier("900"),
                Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    [Test]
    public void MalformedControlBlocksAndOptionalFieldsAreRejected()
    {
        int[] duplicateCount = new int[]
        {
            928, 111, 100, 129,
            923, 1, 111, 102,
            923, 1, 111, 102
        };
        int[] inconsistentTerminator = new int[]
        {
            928, 111, 100, 129,
            923, 1, 111, 102,
            922
        };

        Assert.Multiple(() =>
        {
            Assert.That(
                () => Pdf417MacroCodec.DecodeControlBlock(null!),
                Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(
                () => Pdf417MacroCodec.DecodeControlBlock(new int[] { 900, 111, 100, 129 }),
                Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(
                () => Pdf417MacroCodec.DecodeControlBlock(new int[] { 928, 111, 100, 923, 1, 111, 102 }),
                Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(
                () => Pdf417MacroCodec.DecodeControlBlock(new int[] { 928, 111, 100, 129, 923, 7, 10 }),
                Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(
                () => Pdf417MacroCodec.DecodeControlBlock(new int[] { 928, 111, 100, 129, 923, 0 }),
                Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(
                () => Pdf417MacroCodec.DecodeControlBlock(new int[] { 928, 111, 100, 129, 923, 1, 12 }),
                Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(
                () => Pdf417MacroCodec.DecodeControlBlock(duplicateCount),
                Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(
                () => Pdf417MacroCodec.DecodeControlBlock(inconsistentTerminator),
                Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(
                () => Pdf417MacroCodec.DecodeControlBlock(new int[] { 928, 111, 100, 129, 922, 10 }),
                Throws.TypeOf<BarcodeDecodingException>());
        });
    }

    static Pdf417Options CreateOptions(
        int index,
        int count,
        string fileIdentifier,
        bool isLast)
    {
        StructuredAppendInfo macro = new StructuredAppendInfo();
        macro.Format = BarcodeFormat.MacroPdf417;
        macro.Index = index;
        macro.Count = count;
        macro.FileIdentifier = fileIdentifier;
        macro.IsLastSegment = isLast;
        Pdf417Options options = new Pdf417Options();
        options.Macro = macro;
        return options;
    }

    static int[] GetFieldDesignators(int[] codewords)
    {
        List<int> designators = new List<int>();
        int index = 3;
        while (index < codewords.Length && codewords[index] != 923)
        {
            index++;
        }

        while (index < codewords.Length)
        {
            if (codewords[index] == 923)
            {
                designators.Add(codewords[index + 1]);
                index = index + 2;
                while (index < codewords.Length
                    && codewords[index] != 923
                    && codewords[index] != 922)
                {
                    index++;
                }
            }
            else
            {
                index++;
            }
        }

        return designators.ToArray();
    }
}
