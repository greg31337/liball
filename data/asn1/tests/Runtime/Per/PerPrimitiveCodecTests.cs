using asn1;

namespace asn1.Tests;

public class PerPrimitiveCodecTests
{
    [Test]
    public void BooleanAndNullUseTheirExactFieldWidths()
    {
        PerPrimitiveCodec codec = CreateCodec();
        PerBitWriter writer = CreateWriter();
        PerPrimitiveResult boolean = codec.WriteBoolean(writer, true);
        PerPrimitiveResult nullValue = codec.WriteNull(writer);
        PerBitWriteResult output = writer.GetOutput();
        PerBitReader reader = CreateReader(output);
        PerPrimitiveResult decodedBoolean = codec.ReadBoolean(reader);
        PerPrimitiveResult decodedNull = codec.ReadNull(reader);

        Assert.Multiple(() =>
        {
            Assert.That(boolean.Success, Is.True);
            Assert.That(boolean.BitCount, Is.EqualTo(1));
            Assert.That(nullValue.Success, Is.True);
            Assert.That(nullValue.BitCount, Is.Zero);
            Assert.That(output.Data, Is.EqualTo(new byte[] { 0x80 }));
            Assert.That(output.WrittenBitCount, Is.EqualTo(1));
            Assert.That(decodedBoolean.Value, Is.Not.Null);
            Assert.That(decodedBoolean.Value!.BooleanValue, Is.True);
            Assert.That(decodedNull.Value, Is.Not.Null);
            Assert.That(decodedNull.Value!.Kind,
                Is.EqualTo(AsnRuntimeValueKind.Null));
        });
    }

    [Test]
    public void IntegerFormsRoundTripInAlignedAndUnalignedVariants()
    {
        PerPrimitiveCodec codec = CreateCodec();

        PerBitWriter constrainedWriter = CreateWriter();
        codec.WriteInteger(
            constrainedWriter,
            3,
            true,
            -2,
            true,
            5,
            false);
        PerBitWriteResult constrainedOutput = constrainedWriter.GetOutput();
        PerPrimitiveResult constrained = codec.ReadInteger(
            CreateReader(constrainedOutput),
            true,
            -2,
            true,
            5,
            false);

        PerBitWriter alignedWriter = CreateWriter();
        alignedWriter.WriteBit(true);
        codec.WriteInteger(
            alignedWriter,
            300,
            true,
            0,
            true,
            300,
            true);
        PerBitWriteResult alignedOutput = alignedWriter.GetOutput();
        PerBitReader alignedReader = CreateReader(alignedOutput);
        alignedReader.ReadBit();
        PerPrimitiveResult aligned = codec.ReadInteger(
            alignedReader,
            true,
            0,
            true,
            300,
            true);

        PerBitWriter unconstrainedWriter = CreateWriter();
        codec.WriteInteger(
            unconstrainedWriter,
            -129,
            false,
            0,
            false,
            0,
            false);
        PerBitWriteResult unconstrainedOutput =
            unconstrainedWriter.GetOutput();
        PerPrimitiveResult unconstrained = codec.ReadInteger(
            CreateReader(unconstrainedOutput),
            false,
            0,
            false,
            0,
            false);

        PerBitWriter largeWriter = CreateWriter();
        codec.WriteInteger(
            largeWriter,
            1234567,
            true,
            0,
            true,
            1234567,
            true);
        PerBitWriteResult largeOutput = largeWriter.GetOutput();
        PerPrimitiveResult large = codec.ReadInteger(
            CreateReader(largeOutput),
            true,
            0,
            true,
            1234567,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(constrainedOutput.Data,
                Is.EqualTo(new byte[] { 0xa0 }));
            Assert.That(constrainedOutput.WrittenBitCount, Is.EqualTo(3));
            Assert.That(constrained.Value!.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(3)));
            Assert.That(alignedOutput.Data,
                Is.EqualTo(new byte[] { 0x80, 0x01, 0x2c }));
            Assert.That(aligned.Value!.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(300)));
            Assert.That(unconstrainedOutput.Data,
                Is.EqualTo(new byte[] { 0x02, 0xff, 0x7f }));
            Assert.That(unconstrained.Value!.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(-129)));
            Assert.That(largeOutput.Data,
                Is.EqualTo(new byte[] { 0x80, 0x12, 0xd6, 0x87 }));
            Assert.That(large.Value!.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(1234567)));
        });
    }

    [Test]
    public void EnumeratedRootIndexUsesConstrainedWholeNumberEncoding()
    {
        PerPrimitiveCodec codec = CreateCodec();
        PerBitWriter writer = CreateWriter();
        PerPrimitiveResult encoded = codec.WriteEnumeratedIndex(
            writer,
            3,
            5,
            false);
        PerBitWriteResult output = writer.GetOutput();
        PerPrimitiveResult decoded = codec.ReadEnumeratedIndex(
            CreateReader(output),
            5,
            false);
        PerPrimitiveResult rejected = codec.WriteEnumeratedIndex(
            CreateWriter(),
            5,
            5,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(output.Data, Is.EqualTo(new byte[] { 0x60 }));
            Assert.That(output.WrittenBitCount, Is.EqualTo(3));
            Assert.That(decoded.Value!.Kind,
                Is.EqualTo(AsnRuntimeValueKind.Enumerated));
            Assert.That(decoded.Value.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(3)));
            Assert.That(rejected.ErrorCode, Is.EqualTo("ASNP0041"));
        });
    }

    [Test]
    public void OctetStringsHonorFixedAndVariableAlignmentRules()
    {
        PerPrimitiveCodec codec = CreateCodec();

        PerBitWriter shortWriter = CreateWriter();
        shortWriter.WriteBit(true);
        codec.WriteOctetString(
            shortWriter,
            [0xaa, 0x55],
            2,
            true,
            2,
            true);
        PerBitWriteResult shortOutput = shortWriter.GetOutput();

        PerBitWriter fixedWriter = CreateWriter();
        fixedWriter.WriteBit(true);
        codec.WriteOctetString(
            fixedWriter,
            [1, 2, 3],
            3,
            true,
            3,
            true);
        PerBitWriteResult fixedOutput = fixedWriter.GetOutput();
        PerBitReader fixedReader = CreateReader(fixedOutput);
        fixedReader.ReadBit();
        PerPrimitiveResult fixedDecoded = codec.ReadOctetString(
            fixedReader,
            3,
            true,
            3,
            true);

        PerBitWriter variableWriter = CreateWriter();
        variableWriter.WriteBit(true);
        codec.WriteOctetString(
            variableWriter,
            [4, 5],
            1,
            true,
            3,
            true);
        PerBitWriteResult variableOutput = variableWriter.GetOutput();
        PerBitReader variableReader = CreateReader(variableOutput);
        variableReader.ReadBit();
        PerPrimitiveResult variableDecoded = codec.ReadOctetString(
            variableReader,
            1,
            true,
            3,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(shortOutput.Data,
                Is.EqualTo(new byte[] { 0xd5, 0x2a, 0x80 }));
            Assert.That(shortOutput.WrittenBitCount, Is.EqualTo(17));
            Assert.That(fixedOutput.Data,
                Is.EqualTo(new byte[] { 0x80, 1, 2, 3 }));
            Assert.That(fixedDecoded.Value!.Octets,
                Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(variableOutput.Data,
                Is.EqualTo(new byte[] { 0xa0, 4, 5 }));
            Assert.That(variableDecoded.Value!.Octets,
                Is.EqualTo(new byte[] { 4, 5 }));
        });
    }

    [Test]
    public void BitStringsPreserveExactNonOctetLength()
    {
        PerPrimitiveCodec codec = CreateCodec();
        AsnBitStringValue value = new AsnBitStringValue
        {
            Bytes = [0xa8],
            BitLength = 5
        };
        PerBitWriter writer = CreateWriter();
        writer.WriteBit(true);
        codec.WriteBitString(writer, value, 0, true, 7, false);
        PerBitWriteResult output = writer.GetOutput();
        PerBitReader reader = CreateReader(output);
        reader.ReadBit();
        PerPrimitiveResult decoded = codec.ReadBitString(
            reader,
            0,
            true,
            7,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(output.Data, Is.EqualTo(new byte[] { 0xda, 0x80 }));
            Assert.That(output.WrittenBitCount, Is.EqualTo(9));
            Assert.That(decoded.Value!.BitStringValue, Is.Not.Null);
            Assert.That(decoded.Value.BitStringValue!.BitLength,
                Is.EqualTo(5));
            Assert.That(decoded.Value.BitStringValue.Bytes,
                Is.EqualTo(new byte[] { 0xa8 }));
        });
    }

    [Test]
    public void UnboundedBitStringsFragmentAtSixteenKBits()
    {
        byte[] bytes = new byte[2049];
        int index = 0;
        while (index < 2048)
        {
            bytes[index] = 0xa5;
            index = index + 1;
        }

        bytes[2048] = 0x80;
        AsnBitStringValue value = new AsnBitStringValue
        {
            Bytes = bytes,
            BitLength = 16385
        };
        PerPrimitiveCodec codec = CreateCodec();
        PerBitWriter writer = CreateWriter();
        PerPrimitiveResult encoded = codec.WriteBitString(
            writer,
            value,
            0,
            false,
            0,
            false);
        PerBitWriteResult output = writer.GetOutput();
        PerPrimitiveResult decoded = codec.ReadBitString(
            CreateReader(output),
            0,
            false,
            0,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(output.Data[0], Is.EqualTo(0xc1));
            Assert.That(output.Data[2049], Is.EqualTo(1));
            Assert.That(output.WrittenBitCount, Is.EqualTo(16401));
            Assert.That(decoded.Value!.BitStringValue!.BitLength,
                Is.EqualTo(16385));
            Assert.That(decoded.Value.BitStringValue.Bytes,
                Is.EqualTo(bytes));
        });
    }

    [Test]
    public void RealObjectIdentifierAndIriReuseStandardContentOctets()
    {
        PerPrimitiveCodec codec = CreateCodec();

        PerBitWriter realWriter = CreateWriter();
        codec.WriteReal(
            realWriter,
            new AsnRealValue
            {
                Kind = AsnRealKind.FiniteBinary,
                HasBinaryValue = true,
                Mantissa = 3,
                Radix = 2,
                Exponent = 2
            },
            false);
        PerBitWriteResult realOutput = realWriter.GetOutput();
        PerPrimitiveResult real = codec.ReadReal(
            CreateReader(realOutput),
            false);

        AsnObjectIdentifierValue oidValue = new AsnObjectIdentifierValue
        {
            Arcs = [1, 2, 840, 113549]
        };
        PerBitWriter oidWriter = CreateWriter();
        codec.WriteObjectIdentifier(oidWriter, oidValue, false, true);
        PerBitWriteResult oidOutput = oidWriter.GetOutput();
        PerPrimitiveResult oid = codec.ReadObjectIdentifier(
            CreateReader(oidOutput),
            false,
            true);

        PerBitWriter iriWriter = CreateWriter();
        codec.WriteIri(iriWriter, "/iso/member-body/us", false, false);
        PerBitWriteResult iriOutput = iriWriter.GetOutput();
        PerPrimitiveResult iri = codec.ReadIri(
            CreateReader(iriOutput),
            false,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(real.Success, Is.True);
            Assert.That(real.Value!.RealValue, Is.Not.Null);
            Assert.That(real.Value.RealValue!.Mantissa,
                Is.EqualTo(new System.Numerics.BigInteger(3)));
            Assert.That(real.Value.RealValue.Exponent,
                Is.EqualTo(new System.Numerics.BigInteger(2)));
            Assert.That(oidOutput.Data,
                Is.EqualTo(new byte[] { 6, 0x2a, 0x86, 0x48, 0x86, 0xf7, 0x0d }));
            Assert.That(oid.Value!.ObjectIdentifierValue!.Arcs,
                Is.EqualTo(oidValue.Arcs));
            Assert.That(iri.Value!.Kind,
                Is.EqualTo(AsnRuntimeValueKind.ObjectIdentifierIri));
            Assert.That(iri.Value.TextValue,
                Is.EqualTo("/iso/member-body/us"));
        });
    }

    [Test]
    public void Utf8AndUsefulTimeValuesRoundTripInBothVariants()
    {
        PerPrimitiveCodec codec = CreateCodec();
        PerBitWriter stringWriter = CreateWriter();
        stringWriter.WriteBit(true);
        codec.WriteCharacterString(
            stringWriter,
            AsnTypeKind.Utf8String,
            "Grüße",
            false);
        PerBitWriteResult stringOutput = stringWriter.GetOutput();
        PerBitReader stringReader = CreateReader(stringOutput);
        stringReader.ReadBit();
        PerPrimitiveResult text = codec.ReadCharacterString(
            stringReader,
            AsnTypeKind.Utf8String,
            false);

        AsnTimeValue dateValue = new AsnTimeValue
        {
            Kind = AsnTimeKind.Date,
            Text = "2026-08-28"
        };
        PerBitWriter timeWriter = CreateWriter();
        timeWriter.WriteBit(true);
        codec.WriteTime(
            timeWriter,
            AsnTypeKind.Date,
            dateValue,
            true);
        PerBitWriteResult timeOutput = timeWriter.GetOutput();
        PerBitReader timeReader = CreateReader(timeOutput);
        timeReader.ReadBit();
        PerPrimitiveResult time = codec.ReadTime(
            timeReader,
            AsnTypeKind.Date,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(text.Value!.TextValue, Is.EqualTo("Grüße"));
            Assert.That(timeOutput.Data[0], Is.EqualTo(0x80));
            Assert.That(timeOutput.Data[1], Is.EqualTo(8));
            Assert.That(time.Value!.TimeValue, Is.Not.Null);
            Assert.That(time.Value.TimeValue!.Text,
                Is.EqualTo("2026-08-28"));
        });
    }

    private static PerPrimitiveCodec CreateCodec()
    {
        return new PerPrimitiveCodec(new AsnResourceLimits());
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
}
