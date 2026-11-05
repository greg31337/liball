using cbor;

namespace cbor.Tests;

[TestFixture]
public class BinaryPrimitiveTests
{
    [Test]
    public void BigEndianArgumentsRoundTripExactBytes()
    {
        byte[] uint8Bytes = new byte[1];
        byte[] uint16Bytes = new byte[2];
        byte[] uint32Bytes = new byte[4];
        byte[] uint64Bytes = new byte[8];

        CborBinaryPrimitives.WriteUInt8(uint8Bytes, 0xab);
        CborBinaryPrimitives.WriteUInt16BigEndian(uint16Bytes, 0xabcd);
        CborBinaryPrimitives.WriteUInt32BigEndian(uint32Bytes, 0x89abcdef);
        CborBinaryPrimitives.WriteUInt64BigEndian(uint64Bytes, 0x0123456789abcdef);

        Assert.Multiple(() =>
        {
            Assert.That(uint8Bytes, Is.EqualTo(new byte[] { 0xab }));
            Assert.That(uint16Bytes, Is.EqualTo(new byte[] { 0xab, 0xcd }));
            Assert.That(uint32Bytes, Is.EqualTo(new byte[] { 0x89, 0xab, 0xcd, 0xef }));
            Assert.That(uint64Bytes, Is.EqualTo(new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef }));
            Assert.That(CborBinaryPrimitives.ReadUInt8(uint8Bytes), Is.EqualTo(0xab));
            Assert.That(CborBinaryPrimitives.ReadUInt16BigEndian(uint16Bytes), Is.EqualTo(0xabcd));
            Assert.That(CborBinaryPrimitives.ReadUInt32BigEndian(uint32Bytes), Is.EqualTo(0x89abcdef));
            Assert.That(CborBinaryPrimitives.ReadUInt64BigEndian(uint64Bytes), Is.EqualTo(0x0123456789abcdef));
        });
    }

    [Test]
    public void ShortestHeadsCoverEveryUnsignedArgumentBoundary()
    {
        ulong[] arguments =
        [
            0,
            23,
            24,
            byte.MaxValue,
            (ulong)byte.MaxValue + 1,
            ushort.MaxValue,
            (ulong)ushort.MaxValue + 1,
            uint.MaxValue,
            (ulong)uint.MaxValue + 1,
            ulong.MaxValue
        ];
        byte[][] expected =
        [
            [0xc0],
            [0xd7],
            [0xd8, 0x18],
            [0xd8, 0xff],
            [0xd9, 0x01, 0x00],
            [0xd9, 0xff, 0xff],
            [0xda, 0x00, 0x01, 0x00, 0x00],
            [0xda, 0xff, 0xff, 0xff, 0xff],
            [0xdb, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00],
            [0xdb, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff]
        ];

        for (int index = 0; index < arguments.Length; index++)
        {
            byte[] destination = new byte[9];
            int written = CborBinaryPrimitives.WriteHead(destination, 6, arguments[index]);
            CborHead head = CborBinaryPrimitives.ReadHead(destination.AsSpan(0, written));

            Assert.Multiple(() =>
            {
                Assert.That(written, Is.EqualTo(expected[index].Length));
                Assert.That(destination.AsSpan(0, written).ToArray(), Is.EqualTo(expected[index]));
                Assert.That(CborBinaryPrimitives.GetHeadSize(arguments[index]), Is.EqualTo(written));
                Assert.That(head.MajorType, Is.EqualTo(6));
                Assert.That(head.Argument, Is.EqualTo(arguments[index]));
                Assert.That(head.ArgumentWidth, Is.EqualTo(written - 1));
                Assert.That(head.HeadLength, Is.EqualTo(written));
                Assert.That(head.IsIndefinite, Is.False);
                Assert.That(head.IsBreak, Is.False);
            });
        }
    }

    [Test]
    public void IndefiniteHeadsAndBreakRoundTripTheirExactBytes()
    {
        int[] majorTypes = [2, 3, 4, 5];
        byte[] expected = [0x5f, 0x7f, 0x9f, 0xbf];

        for (int index = 0; index < majorTypes.Length; index++)
        {
            byte[] destination = new byte[1];
            int written = CborBinaryPrimitives.WriteIndefiniteHead(destination, majorTypes[index]);
            CborHead head = CborBinaryPrimitives.ReadHead(destination);

            Assert.Multiple(() =>
            {
                Assert.That(written, Is.EqualTo(1));
                Assert.That(destination[0], Is.EqualTo(expected[index]));
                Assert.That(head.MajorType, Is.EqualTo(majorTypes[index]));
                Assert.That(head.AdditionalInformation, Is.EqualTo(31));
                Assert.That(head.IsIndefinite, Is.True);
                Assert.That(head.IsBreak, Is.False);
            });
        }

        byte[] breakDestination = new byte[1];
        Assert.That(CborBinaryPrimitives.WriteBreak(breakDestination), Is.EqualTo(1));
        CborHead breakHead = CborBinaryPrimitives.ReadHead(breakDestination);
        Assert.Multiple(() =>
        {
            Assert.That(breakDestination[0], Is.EqualTo(0xff));
            Assert.That(breakHead.MajorType, Is.EqualTo(7));
            Assert.That(breakHead.IsBreak, Is.True);
            Assert.That(breakHead.IsIndefinite, Is.False);
        });
    }

    [Test]
    public void TruncatedReadsReportTheAvailableOffset()
    {
        CborDecodeException? uint8Failure = Assert.Throws<CborDecodeException>(
            () => CborBinaryPrimitives.ReadUInt8([]));
        CborDecodeException? uint16Failure = Assert.Throws<CborDecodeException>(
            () => CborBinaryPrimitives.ReadUInt16BigEndian([0]));
        CborDecodeException? uint32Failure = Assert.Throws<CborDecodeException>(
            () => CborBinaryPrimitives.ReadUInt32BigEndian([0, 0, 0]));
        CborDecodeException? uint64Failure = Assert.Throws<CborDecodeException>(
            () => CborBinaryPrimitives.ReadUInt64BigEndian([0, 0, 0, 0, 0, 0, 0]));
        CborDecodeException? emptyHeadFailure = Assert.Throws<CborDecodeException>(
            () => CborBinaryPrimitives.ReadHead([]));
        CborDecodeException? headFailure = Assert.Throws<CborDecodeException>(
            () => CborBinaryPrimitives.ReadHead([0x1b, 0, 0, 0, 0, 0, 0, 0]));

        Assert.Multiple(() =>
        {
            Assert.That(uint8Failure?.ErrorCode, Is.EqualTo("CBOR_TRUNCATED_ARGUMENT"));
            Assert.That(uint8Failure?.ErrorOffset, Is.EqualTo(0));
            Assert.That(uint16Failure?.ErrorOffset, Is.EqualTo(1));
            Assert.That(uint32Failure?.ErrorOffset, Is.EqualTo(3));
            Assert.That(uint64Failure?.ErrorOffset, Is.EqualTo(7));
            Assert.That(emptyHeadFailure?.ErrorCode, Is.EqualTo("CBOR_TRUNCATED_HEAD"));
            Assert.That(emptyHeadFailure?.ErrorOffset, Is.EqualTo(0));
            Assert.That(headFailure?.ErrorCode, Is.EqualTo("CBOR_TRUNCATED_HEAD"));
            Assert.That(headFailure?.ErrorOffset, Is.EqualTo(8));
        });
    }

    [Test]
    public void DestinationFailuresOccurBeforeAnyBytesAreChanged()
    {
        byte[] destination = [0xa5, 0xa5, 0xa5, 0xa5, 0xa5, 0xa5, 0xa5, 0xa5];

        CborEncodeException? failure = Assert.Throws<CborEncodeException>(
            () => CborBinaryPrimitives.WriteUInt64BigEndian(destination.AsSpan(0, 7), ulong.MaxValue));

        Assert.Multiple(() =>
        {
            Assert.That(failure?.ErrorCode, Is.EqualTo("CBOR_DESTINATION_TOO_SMALL"));
            Assert.That(failure?.ErrorOffset, Is.EqualTo(7));
            Assert.That(destination, Is.EqualTo(new byte[] { 0xa5, 0xa5, 0xa5, 0xa5, 0xa5, 0xa5, 0xa5, 0xa5 }));
        });
    }

    [Test]
    public void ReservedAndIllegalHeadsAreRejected()
    {
        CborDecodeException? reserved28 = Assert.Throws<CborDecodeException>(
            () => CborBinaryPrimitives.ReadHead([0x1c]));
        CborDecodeException? reserved29 = Assert.Throws<CborDecodeException>(
            () => CborBinaryPrimitives.ReadHead([0x1d]));
        CborDecodeException? reserved30 = Assert.Throws<CborDecodeException>(
            () => CborBinaryPrimitives.ReadHead([0x1e]));
        CborDecodeException? indefiniteInteger = Assert.Throws<CborDecodeException>(
            () => CborBinaryPrimitives.ReadHead([0x1f]));
        CborDecodeException? indefiniteTag = Assert.Throws<CborDecodeException>(
            () => CborBinaryPrimitives.ReadHead([0xdf]));
        CborEncodeException? invalidMajorType = Assert.Throws<CborEncodeException>(
            () => CborBinaryPrimitives.WriteHead(new byte[9], 8, 0));
        CborEncodeException? invalidIndefiniteType = Assert.Throws<CborEncodeException>(
            () => CborBinaryPrimitives.WriteIndefiniteHead(new byte[1], 6));

        Assert.Multiple(() =>
        {
            Assert.That(reserved28?.ErrorCode, Is.EqualTo("CBOR_RESERVED_ADDITIONAL_INFORMATION"));
            Assert.That(reserved29?.ErrorCode, Is.EqualTo("CBOR_RESERVED_ADDITIONAL_INFORMATION"));
            Assert.That(reserved30?.ErrorCode, Is.EqualTo("CBOR_RESERVED_ADDITIONAL_INFORMATION"));
            Assert.That(indefiniteInteger?.ErrorCode, Is.EqualTo("CBOR_ILLEGAL_INDEFINITE_HEAD"));
            Assert.That(indefiniteTag?.ErrorCode, Is.EqualTo("CBOR_ILLEGAL_INDEFINITE_HEAD"));
            Assert.That(invalidMajorType?.ErrorCode, Is.EqualTo("CBOR_INVALID_MAJOR_TYPE"));
            Assert.That(invalidIndefiniteType?.ErrorCode, Is.EqualTo("CBOR_ILLEGAL_INDEFINITE_HEAD"));
        });
    }
}
