using asn1;

namespace asn1.Tests;

public class PerStringCodecTests
{
    [Test]
    public void NumericStringUsesCanonicalCompactedIndexes()
    {
        PerStringCodec codec = CreateCodec();
        PerBitWriter writer = CreateWriter();
        PerStringResult encoded = codec.WriteKnownMultiplierString(
            writer,
            AsnTypeKind.NumericString,
            "12 9",
            4,
            true,
            4,
            null,
            false,
            false);
        PerBitWriteResult output = writer.GetOutput();
        PerStringResult decoded = codec.ReadKnownMultiplierString(
            CreateReader(output),
            AsnTypeKind.NumericString,
            4,
            true,
            4,
            null,
            false,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.CompactedAlphabet, Is.True);
            Assert.That(encoded.BitsPerCharacter, Is.EqualTo(4));
            Assert.That(output.Data,
                Is.EqualTo(new byte[] { 0x23, 0x0a }));
            Assert.That(output.WrittenBitCount, Is.EqualTo(16));
            Assert.That(decoded.Text, Is.EqualTo("12 9"));
        });
    }

    [Test]
    public void PermittedAlphabetIndexesFollowCanonicalScalarOrder()
    {
        PerStringCodec codec = CreateCodec();
        PermittedAlphabetResult alphabet = CreateAlphabet(codec, [
            CreateInterval('A', 'A'),
            CreateInterval('C', 'C'),
            CreateInterval('E', 'E')
        ]);
        PerBitWriter writer = CreateWriter();
        PerStringResult encoded = codec.WriteKnownMultiplierString(
            writer,
            AsnTypeKind.Ia5String,
            "ACE",
            3,
            true,
            3,
            alphabet,
            false,
            false);
        PerBitWriteResult output = writer.GetOutput();
        PerStringResult decoded = codec.ReadKnownMultiplierString(
            CreateReader(output),
            AsnTypeKind.Ia5String,
            3,
            true,
            3,
            alphabet,
            false,
            false);
        PerBitReader invalidReader = new PerBitReader(
            new AsnResourceLimits());
        invalidReader.AppendInputBits([0xc0], 0, 2, true);
        PerStringResult invalid = codec.ReadKnownMultiplierString(
            invalidReader,
            AsnTypeKind.Ia5String,
            1,
            true,
            1,
            alphabet,
            false,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(alphabet.GetRootIndex('A'), Is.EqualTo(0));
            Assert.That(alphabet.GetRootIndex('C'), Is.EqualTo(1));
            Assert.That(alphabet.GetRootIndex('E'), Is.EqualTo(2));
            Assert.That(encoded.BitsPerCharacter, Is.EqualTo(2));
            Assert.That(encoded.CompactedAlphabet, Is.True);
            Assert.That(output.Data, Is.EqualTo(new byte[] { 0x18 }));
            Assert.That(output.WrittenBitCount, Is.EqualTo(6));
            Assert.That(decoded.Text, Is.EqualTo("ACE"));
            Assert.That(invalid.ErrorCode, Is.EqualTo("ASNP0050"));
        });
    }

    [Test]
    public void AlignedFixedAndVariableStringsUseDistinctSixteenBitThresholds()
    {
        PerStringCodec codec = CreateCodec();
        PerBitWriter fixedWriter = CreateWriter();
        fixedWriter.WriteBit(true);
        PerStringResult fixedValue = codec.WriteKnownMultiplierString(
            fixedWriter,
            AsnTypeKind.NumericString,
            "12 9",
            4,
            true,
            4,
            null,
            false,
            true);
        PerBitWriteResult fixedOutput = fixedWriter.GetOutput();

        PerBitWriter variableWriter = CreateWriter();
        variableWriter.WriteBit(true);
        PerStringResult variableValue = codec.WriteKnownMultiplierString(
            variableWriter,
            AsnTypeKind.NumericString,
            "12 9",
            1,
            true,
            4,
            null,
            false,
            true);
        PerBitWriteResult variableOutput = variableWriter.GetOutput();
        PerBitReader variableReader = CreateReader(variableOutput);
        variableReader.ReadBit();
        PerStringResult variableDecoded = codec.ReadKnownMultiplierString(
            variableReader,
            AsnTypeKind.NumericString,
            1,
            true,
            4,
            null,
            false,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(fixedValue.Success, Is.True);
            Assert.That(fixedOutput.Data,
                Is.EqualTo(new byte[] { 0x91, 0x85, 0x00 }));
            Assert.That(fixedOutput.WrittenBitCount, Is.EqualTo(17));
            Assert.That(variableValue.Success, Is.True);
            Assert.That(variableOutput.Data,
                Is.EqualTo(new byte[] { 0xe0, 0x23, 0x0a }));
            Assert.That(variableOutput.WrittenBitCount, Is.EqualTo(24));
            Assert.That(variableDecoded.Text, Is.EqualTo("12 9"));
        });
    }

    [Test]
    public void AlignedAndUnalignedWidthsFollowAlphabetSize()
    {
        PerStringCodec codec = CreateCodec();
        PerBitWriter unalignedWriter = CreateWriter();
        PerStringResult unaligned = codec.WriteKnownMultiplierString(
            unalignedWriter,
            AsnTypeKind.Ia5String,
            "AZ",
            2,
            true,
            2,
            null,
            false,
            false);
        PerBitWriteResult unalignedOutput = unalignedWriter.GetOutput();

        PerBitWriter alignedWriter = CreateWriter();
        PerStringResult aligned = codec.WriteKnownMultiplierString(
            alignedWriter,
            AsnTypeKind.Ia5String,
            "AZ",
            2,
            true,
            2,
            null,
            false,
            true);
        PerBitWriteResult alignedOutput = alignedWriter.GetOutput();

        PerBitWriter universalWriter = CreateWriter();
        PerStringResult universal = codec.WriteKnownMultiplierString(
            universalWriter,
            AsnTypeKind.UniversalString,
            "😀",
            1,
            true,
            1,
            null,
            false,
            false);
        PerBitWriteResult universalOutput = universalWriter.GetOutput();
        PerStringResult universalDecoded = codec.ReadKnownMultiplierString(
            CreateReader(universalOutput),
            AsnTypeKind.UniversalString,
            1,
            true,
            1,
            null,
            false,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(unaligned.BitsPerCharacter, Is.EqualTo(7));
            Assert.That(unalignedOutput.Data,
                Is.EqualTo(new byte[] { 0x83, 0x68 }));
            Assert.That(unalignedOutput.WrittenBitCount, Is.EqualTo(14));
            Assert.That(aligned.BitsPerCharacter, Is.EqualTo(8));
            Assert.That(alignedOutput.Data,
                Is.EqualTo(new byte[] { 0x41, 0x5a }));
            Assert.That(universal.BitsPerCharacter, Is.EqualTo(21));
            Assert.That(universalOutput.WrittenBitCount, Is.EqualTo(21));
            Assert.That(universalDecoded.Text, Is.EqualTo("😀"));
        });
    }

    [Test]
    public void ExtensibleSizeUsesUnconstrainedCharacterCountOutsideRoot()
    {
        PerStringCodec codec = CreateCodec();
        PerBitWriter writer = CreateWriter();
        PerStringResult encoded = codec.WriteKnownMultiplierString(
            writer,
            AsnTypeKind.Ia5String,
            "ABC",
            1,
            true,
            2,
            null,
            true,
            false);
        PerBitWriteResult output = writer.GetOutput();
        PerStringResult decoded = codec.ReadKnownMultiplierString(
            CreateReader(output),
            AsnTypeKind.Ia5String,
            1,
            true,
            2,
            null,
            true,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.ExtensionValue, Is.True);
            Assert.That(output.Data[0], Is.EqualTo(0x81));
            Assert.That(output.WrittenBitCount, Is.EqualTo(30));
            Assert.That(decoded.Text, Is.EqualTo("ABC"));
            Assert.That(decoded.ExtensionValue, Is.True);
        });
    }

    [Test]
    public void CharacterCountFragmentationUsesStandardBoundaries()
    {
        string text = new string('0', 16385);
        PerStringCodec codec = CreateCodec();
        PerBitWriter writer = CreateWriter();
        PerStringResult encoded = codec.WriteKnownMultiplierString(
            writer,
            AsnTypeKind.NumericString,
            text,
            0,
            false,
            0,
            null,
            false,
            false);
        PerBitWriteResult output = writer.GetOutput();
        PerStringResult decoded = codec.ReadKnownMultiplierString(
            CreateReader(output),
            AsnTypeKind.NumericString,
            0,
            false,
            0,
            null,
            false,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(output.Data[0], Is.EqualTo(0xc1));
            Assert.That(output.Data[8193], Is.EqualTo(1));
            Assert.That(output.WrittenBitCount, Is.EqualTo(65556));
            Assert.That(decoded.CharacterCount, Is.EqualTo(16385));
            Assert.That(decoded.Text, Is.EqualTo(text));
        });
    }

    [Test]
    public void BaseEncodedAndUnrestrictedFormsUseOctetPayloads()
    {
        PerStringCodec codec = CreateCodec();
        PerBitWriter baseWriter = CreateWriter();
        baseWriter.WriteBit(true);
        PerStringResult baseEncoded = codec.WriteBaseEncodedString(
            baseWriter,
            AsnTypeKind.Utf8String,
            "Grüße",
            true);
        PerBitWriteResult baseOutput = baseWriter.GetOutput();
        PerBitReader baseReader = CreateReader(baseOutput);
        baseReader.ReadBit();
        PerStringResult baseDecoded = codec.ReadBaseEncodedString(
            baseReader,
            AsnTypeKind.Utf8String,
            true);

        byte[] completeEncoding = [0x04, 0x02, 0xca, 0xfe];
        PerBitWriter unrestrictedWriter = CreateWriter();
        PerStringResult unrestricted =
            codec.WriteUnrestrictedCharacterString(
                unrestrictedWriter,
                completeEncoding,
                false);
        PerBitWriteResult unrestrictedOutput =
            unrestrictedWriter.GetOutput();
        PerStringResult unrestrictedDecoded =
            codec.ReadUnrestrictedCharacterString(
                CreateReader(unrestrictedOutput),
                false);

        Assert.Multiple(() =>
        {
            Assert.That(baseEncoded.Success, Is.True);
            Assert.That(baseOutput.Data[0], Is.EqualTo(0x80));
            Assert.That(baseDecoded.Text, Is.EqualTo("Grüße"));
            Assert.That(unrestricted.Success, Is.True);
            Assert.That(unrestrictedOutput.Data,
                Is.EqualTo(new byte[] { 4, 0x04, 0x02, 0xca, 0xfe }));
            Assert.That(unrestrictedDecoded.Data,
                Is.EqualTo(completeEncoding));
        });
    }

    private static PerStringCodec CreateCodec()
    {
        return new PerStringCodec(new AsnResourceLimits());
    }

    private static PerBitWriter CreateWriter()
    {
        return new PerBitWriter(new AsnResourceLimits());
    }

    private static PerBitReader CreateReader(PerBitWriteResult output)
    {
        PerBitReader reader = new PerBitReader(new AsnResourceLimits());
        reader.AppendInputBits(
            output.Data,
            0,
            output.WrittenBitCount,
            true);
        return reader;
    }

    private static PermittedAlphabetResult CreateAlphabet(
        PerStringCodec codec,
        IntegerInterval[] intervals)
    {
        IntegerConstraintSet root = new IntegerRangeAlgebra().CreateSet(
            intervals);
        PermittedAlphabetResult alphabet = new PermittedAlphabetResult
        {
            Domain = root,
            Root = root,
            KnownScalars = root
        };
        codec.BuildAlphabetIndexes(alphabet);
        return alphabet;
    }

    private static IntegerInterval CreateInterval(int lower, int upper)
    {
        return new IntegerInterval
        {
            HasLowerBound = true,
            LowerBound = lower,
            HasUpperBound = true,
            UpperBound = upper
        };
    }
}
