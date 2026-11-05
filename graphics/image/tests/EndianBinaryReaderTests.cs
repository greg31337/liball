using System.Collections.Generic;
using System.IO;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class EndianBinaryReaderTests
{
    [Test]
    public void LittleEndianReader_ReadsUnsignedPrimitiveValues()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(new byte[]
        {
            0x7f,
            0x34, 0x12,
            0x78, 0x56, 0x34, 0x12,
            0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01
        });
        BoundedStreamReader bounded = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        EndianBinaryReader reader = EndianBinaryReader.CreateLittleEndian(bounded);

        byte value8 = reader.ReadUnsigned8();
        ushort value16 = reader.ReadUnsigned16();
        uint value32 = reader.ReadUnsigned32();
        ulong value64 = reader.ReadUnsigned64();

        Assert.That(value8, Is.EqualTo(0x7f));
        Assert.That(value16, Is.EqualTo(0x1234));
        Assert.That(value32, Is.EqualTo(0x12345678));
        Assert.That(value64, Is.EqualTo(0x0102030405060708));
        Assert.That(reader.LastReadSucceeded, Is.True);
        Assert.That(bounded.Position, Is.EqualTo(15));
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void BigEndianReader_ReadsSignedPrimitiveValues()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(new byte[]
        {
            0xff,
            0xff, 0xfe,
            0xff, 0xff, 0xff, 0xfe,
            0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xfe
        });
        BoundedStreamReader bounded = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        EndianBinaryReader reader = EndianBinaryReader.CreateBigEndian(bounded);

        sbyte value8 = reader.ReadSigned8();
        short value16 = reader.ReadSigned16();
        int value32 = reader.ReadSigned32();
        long value64 = reader.ReadSigned64();

        Assert.That(value8, Is.EqualTo(-1));
        Assert.That(value16, Is.EqualTo(-2));
        Assert.That(value32, Is.EqualTo(-2));
        Assert.That(value64, Is.EqualTo(-2));
        Assert.That(reader.LastReadSucceeded, Is.True);
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void RationalReaders_ReturnMetadataValuesWithExpectedKinds()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(new byte[]
        {
            0x00, 0x00, 0x00, 0x01,
            0x00, 0x00, 0x01, 0x90,
            0xff, 0xff, 0xff, 0xff,
            0x00, 0x00, 0x00, 0x02
        });
        BoundedStreamReader bounded = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        EndianBinaryReader reader = EndianBinaryReader.CreateBigEndian(bounded);

        MetadataValue unsignedRational = reader.ReadUnsignedRational32();
        MetadataValue signedRational = reader.ReadSignedRational32();

        Assert.That(unsignedRational.Kind, Is.EqualTo(MetadataValueKind.Rational));
        Assert.That(unsignedRational.Numerator, Is.EqualTo(1));
        Assert.That(unsignedRational.Denominator, Is.EqualTo(400));
        Assert.That(signedRational.Kind, Is.EqualTo(MetadataValueKind.SignedRational));
        Assert.That(signedRational.Numerator, Is.EqualTo(-1));
        Assert.That(signedRational.Denominator, Is.EqualTo(2));
        Assert.That(reader.LastReadSucceeded, Is.True);
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void FixedPointReaders_ConvertUnsignedAndSignedValues()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(new byte[]
        {
            0x00, 0x01, 0x80, 0x00,
            0xff, 0xfe, 0x80, 0x00,
            0x01, 0x80,
            0xfe, 0x80
        });
        BoundedStreamReader bounded = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        EndianBinaryReader reader = EndianBinaryReader.CreateBigEndian(bounded);

        double unsigned16Dot16 = reader.ReadUnsignedFixed16Dot16();
        double signed16Dot16 = reader.ReadSignedFixed16Dot16();
        double unsigned8Dot8 = reader.ReadUnsignedFixed8Dot8();
        double signed8Dot8 = reader.ReadSignedFixed8Dot8();

        Assert.That(unsigned16Dot16, Is.EqualTo(1.5));
        Assert.That(signed16Dot16, Is.EqualTo(-1.5));
        Assert.That(unsigned8Dot8, Is.EqualTo(1.5));
        Assert.That(signed8Dot8, Is.EqualTo(-1.5));
        Assert.That(reader.LastReadSucceeded, Is.True);
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void SetEndian_ChangesSubsequentPrimitiveReads()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(new byte[]
        {
            0x34, 0x12,
            0x34, 0x12
        });
        BoundedStreamReader bounded = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        EndianBinaryReader reader = EndianBinaryReader.CreateLittleEndian(bounded);

        ushort little = reader.ReadUnsigned16();
        reader.SetBigEndian();
        ushort big = reader.ReadUnsigned16();

        Assert.That(little, Is.EqualTo(0x1234));
        Assert.That(big, Is.EqualTo(0x3412));
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void ShortPrimitiveRead_ReturnsZeroPaddedValueAndMarksFailure()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(new byte[] { 0x12 });
        BoundedStreamReader bounded = new BoundedStreamReader(stream, 1, 1, diagnostics);
        EndianBinaryReader reader = EndianBinaryReader.CreateBigEndian(bounded);

        uint value = reader.ReadUnsigned32();

        Assert.That(value, Is.EqualTo(0x12000000));
        Assert.That(reader.LastReadSucceeded, Is.False);
        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.Truncated));
    }

    [Test]
    public void NullReader_AddsRecoverableDiagnosticAndReturnsZeroes()
    {
        EndianBinaryReader reader = EndianBinaryReader.CreateBigEndian(null);

        ushort value = reader.ReadUnsigned16();

        Assert.That(value, Is.EqualTo(0));
        Assert.That(reader.LastReadSucceeded, Is.False);
        Assert.That(reader.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(reader.Diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.RecoverableError));
    }

    [Test]
    public void LittleEndianReader_ReadsSignedBoundaryValues()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(new byte[]
        {
            0x80,
            0x00, 0x80,
            0x00, 0x00, 0x00, 0x80,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80
        });
        BoundedStreamReader bounded = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        EndianBinaryReader reader = EndianBinaryReader.CreateLittleEndian(bounded);

        sbyte value8 = reader.ReadSigned8();
        short value16 = reader.ReadSigned16();
        int value32 = reader.ReadSigned32();
        long value64 = reader.ReadSigned64();

        Assert.That(value8, Is.EqualTo(sbyte.MinValue));
        Assert.That(value16, Is.EqualTo(short.MinValue));
        Assert.That(value32, Is.EqualTo(int.MinValue));
        Assert.That(value64, Is.EqualTo(long.MinValue));
        Assert.That(reader.LastReadSucceeded, Is.True);
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void BigEndianReader_ReadsUnsignedBoundaryValues()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(new byte[]
        {
            0xff,
            0xff, 0xff,
            0xff, 0xff, 0xff, 0xff,
            0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff
        });
        BoundedStreamReader bounded = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        EndianBinaryReader reader = EndianBinaryReader.CreateBigEndian(bounded);

        byte value8 = reader.ReadUnsigned8();
        ushort value16 = reader.ReadUnsigned16();
        uint value32 = reader.ReadUnsigned32();
        ulong value64 = reader.ReadUnsigned64();

        Assert.That(value8, Is.EqualTo(byte.MaxValue));
        Assert.That(value16, Is.EqualTo(ushort.MaxValue));
        Assert.That(value32, Is.EqualTo(uint.MaxValue));
        Assert.That(value64, Is.EqualTo(ulong.MaxValue));
        Assert.That(reader.LastReadSucceeded, Is.True);
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void ReadFixedBytes_ReturnsExactBytesAcrossShortSourceReads()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        ShortReadStream stream = new ShortReadStream(new byte[] { 9, 8, 7, 6 }, true, 1);
        BoundedStreamReader bounded = new BoundedStreamReader(stream, 4, 4, diagnostics);
        EndianBinaryReader reader = EndianBinaryReader.CreateBigEndian(bounded);

        byte[] bytes = reader.ReadFixedBytes(4);

        Assert.That(bytes, Is.EqualTo(new byte[] { 9, 8, 7, 6 }));
        Assert.That(reader.LastReadSucceeded, Is.True);
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void PartialRationalRead_PreservesAvailableBytesAndMarksFailure()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(new byte[]
        {
            0x00, 0x00, 0x00, 0x03,
            0x00, 0x02
        });
        BoundedStreamReader bounded = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        EndianBinaryReader reader = EndianBinaryReader.CreateBigEndian(bounded);

        MetadataValue value = reader.ReadUnsignedRational32();

        Assert.That(value.Numerator, Is.EqualTo(3));
        Assert.That(value.Denominator, Is.EqualTo(0x00020000));
        Assert.That(reader.LastReadSucceeded, Is.False);
        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].Code, Is.EqualTo("stream.read.bytes.truncated"));
    }

    [Test]
    public void ReadExact_HandlesZeroAndNegativeCountsWithStableState()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(new byte[] { 1 });
        BoundedStreamReader bounded = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        EndianBinaryReader reader = EndianBinaryReader.CreateBigEndian(bounded);

        byte[] empty = reader.ReadExact(0, "test.read");
        bool zeroSucceeded = reader.LastReadSucceeded;
        byte[] invalid = reader.ReadExact(-1, "test.read");

        Assert.That(empty, Is.Empty);
        Assert.That(zeroSucceeded, Is.True);
        Assert.That(invalid, Is.Empty);
        Assert.That(reader.LastReadSucceeded, Is.False);
        Assert.That(bounded.Position, Is.EqualTo(0));
        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].Code, Is.EqualTo("test.read.negative"));
    }

    [Test]
    public void PrimitiveRead_WhenBoundedByLimit_ReportsOperationLimit()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        BoundedStreamReader bounded = new BoundedStreamReader(stream, 4, 2, diagnostics);
        EndianBinaryReader reader = EndianBinaryReader.CreateBigEndian(bounded);

        uint value = reader.ReadUnsigned32();

        Assert.That(value, Is.EqualTo(0x01020000));
        Assert.That(reader.LastReadSucceeded, Is.False);
        Assert.That(bounded.Position, Is.EqualTo(2));
        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].Code, Is.EqualTo("stream.read.bytes.limit"));
        Assert.That(diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.LimitExceeded));
    }
}
