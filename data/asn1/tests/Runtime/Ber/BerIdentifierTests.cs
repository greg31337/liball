using asn1;

namespace asn1.Tests;

public class BerIdentifierTests
{
    [Test]
    public void ShortIdentifiersPreserveEveryClassAndConstructionForm()
    {
        (AsnTagClass TagClass, int Number, bool Constructed, byte Encoded)[] cases =
        [
            (AsnTagClass.Universal, 2, false, 0x02),
            (AsnTagClass.Application, 3, true, 0x63),
            (AsnTagClass.ContextSpecific, 30, false, 0x9e),
            (AsnTagClass.Private, 30, true, 0xfe)
        ];
        BerIdentifierWriter writer = new BerIdentifierWriter(
            new AsnResourceLimits());
        BerIdentifierReader reader = new BerIdentifierReader(
            new AsnResourceLimits());

        foreach ((AsnTagClass tagClass, int number, bool constructed, byte encoded)
            in cases)
        {
            BerIdentifierWriteResult written = writer.WriteIdentifier(
                new AsnTag { Class = tagClass, Number = number },
                constructed);
            BerIdentifierReadResult read = reader.ReadIdentifier(
                written.Data,
                true);

            Assert.Multiple(() =>
            {
                Assert.That(written.Success, Is.True);
                Assert.That(written.Data, Is.EqualTo(new[] { encoded }));
                Assert.That(read.Status, Is.EqualTo(AsnDecodeStatus.Done));
                Assert.That(read.Tag?.Class, Is.EqualTo(tagClass));
                Assert.That(read.Tag?.Number,
                    Is.EqualTo(new System.Numerics.BigInteger(number)));
                Assert.That(read.Constructed, Is.EqualTo(constructed));
                Assert.That(read.Minimal, Is.True);
                Assert.That(read.ConsumedCount, Is.EqualTo(1));
            });
        }
    }

    [Test]
    public void HighTagIdentifiersRoundTripBoundariesAndLargeNumbers()
    {
        System.Numerics.BigInteger large =
            System.Numerics.BigInteger.One << 100;
        (System.Numerics.BigInteger Number, byte[] Encoded)[] cases =
        [
            (31, [0x5f, 0x1f]),
            (127, [0x5f, 0x7f]),
            (128, [0x5f, 0x81, 0x00]),
            (16384, [0x5f, 0x81, 0x80, 0x00])
        ];
        BerIdentifierWriter writer = new BerIdentifierWriter(
            new AsnResourceLimits());
        BerIdentifierReader reader = new BerIdentifierReader(
            new AsnResourceLimits());

        foreach ((System.Numerics.BigInteger number, byte[] encoded) in cases)
        {
            BerIdentifierWriteResult written = writer.WriteIdentifier(
                new AsnTag
                {
                    Class = AsnTagClass.Application,
                    Number = number
                },
                false);
            BerIdentifierReadResult read = reader.ReadIdentifier(
                written.Data,
                true);

            Assert.That(written.Data, Is.EqualTo(encoded));
            Assert.That(read.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(read.Tag?.Number, Is.EqualTo(number));
            Assert.That(read.Tag?.Class, Is.EqualTo(AsnTagClass.Application));
        }

        BerIdentifierWriteResult largeWritten = writer.WriteIdentifier(
            new AsnTag
            {
                Class = AsnTagClass.Private,
                Number = large
            },
            true);
        BerIdentifierReadResult largeRead = reader.ReadIdentifier(
            largeWritten.Data,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(largeWritten.Success, Is.True);
            Assert.That(largeWritten.WrittenCount, Is.EqualTo(16));
            Assert.That(largeRead.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(largeRead.Tag?.Number, Is.EqualTo(large));
            Assert.That(largeRead.Tag?.Class, Is.EqualTo(AsnTagClass.Private));
            Assert.That(largeRead.Constructed, Is.True);
        });
    }

    [Test]
    public void ReaderReportsTruncationWithoutTreatingItAsMalformedInput()
    {
        BerIdentifierReader reader = new BerIdentifierReader(
            new AsnResourceLimits());

        BerIdentifierReadResult empty = reader.ReadIdentifier([], true);
        BerIdentifierReadResult marker = reader.ReadIdentifier([0x9f], true);
        BerIdentifierReadResult continuation = reader.ReadIdentifier(
            [0x9f, 0x81],
            true);

        Assert.Multiple(() =>
        {
            Assert.That(empty.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            Assert.That(empty.ConsumedCount, Is.Zero);
            Assert.That(marker.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            Assert.That(marker.ConsumedCount, Is.EqualTo(1));
            Assert.That(marker.Tag?.Class,
                Is.EqualTo(AsnTagClass.ContextSpecific));
            Assert.That(continuation.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            Assert.That(continuation.ConsumedCount, Is.EqualTo(2));
            Assert.That(continuation.ErrorCode, Is.Empty);
        });
    }

    [Test]
    public void ReaderDetectsNonMinimalHighTagFormsInStrictMode()
    {
        BerIdentifierReader reader = new BerIdentifierReader(
            new AsnResourceLimits());

        BerIdentifierReadResult leadingZero = reader.ReadIdentifier(
            [0x1f, 0x80, 0x1f],
            false);
        BerIdentifierReadResult shortValue = reader.ReadIdentifier(
            [0x1f, 0x1e],
            false);
        BerIdentifierReadResult strict = reader.ReadIdentifier(
            [0x1f, 0x80, 0x1f],
            true);

        Assert.Multiple(() =>
        {
            Assert.That(leadingZero.Status,
                Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(leadingZero.Tag?.Number,
                Is.EqualTo(new System.Numerics.BigInteger(31)));
            Assert.That(leadingZero.Minimal, Is.False);
            Assert.That(shortValue.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(shortValue.Tag?.Number,
                Is.EqualTo(new System.Numerics.BigInteger(30)));
            Assert.That(shortValue.Minimal, Is.False);
            Assert.That(strict.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(strict.ErrorCode, Is.EqualTo("ASND0001"));
            Assert.That(strict.ConsumedCount, Is.EqualTo(3));
        });
    }

    [Test]
    public void ReaderAndWriterRejectConfiguredTagAndIntegerLimits()
    {
        AsnResourceLimits octetLimits = new AsnResourceLimits
        {
            MaximumTagOctets = 2
        };
        AsnResourceLimits integerLimits = new AsnResourceLimits
        {
            MaximumIntegerBits = 7
        };

        BerIdentifierReadResult readOctets = new BerIdentifierReader(
            octetLimits).ReadIdentifier([0x1f, 0x81, 0x00], true);
        BerIdentifierWriteResult writeOctets = new BerIdentifierWriter(
            octetLimits).WriteIdentifier(new AsnTag { Number = 128 }, false);
        BerIdentifierReadResult readInteger = new BerIdentifierReader(
            integerLimits).ReadIdentifier([0x1f, 0x81, 0x00], true);
        BerIdentifierWriteResult writeInteger = new BerIdentifierWriter(
            integerLimits).WriteIdentifier(new AsnTag { Number = 128 }, false);

        Assert.Multiple(() =>
        {
            Assert.That(readOctets.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(readOctets.LimitExceeded, Is.True);
            Assert.That(readOctets.ErrorCode, Is.EqualTo("ASNR0001"));
            Assert.That(readOctets.ConsumedCount, Is.EqualTo(2));
            Assert.That(writeOctets.Success, Is.False);
            Assert.That(writeOctets.LimitExceeded, Is.True);
            Assert.That(writeOctets.ErrorCode, Is.EqualTo("ASNR0001"));
            Assert.That(readInteger.Status,
                Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(readInteger.LimitExceeded, Is.True);
            Assert.That(readInteger.ErrorCode, Is.EqualTo("ASNR0002"));
            Assert.That(readInteger.ConsumedCount, Is.EqualTo(3));
            Assert.That(writeInteger.Success, Is.False);
            Assert.That(writeInteger.LimitExceeded, Is.True);
            Assert.That(writeInteger.ErrorCode, Is.EqualTo("ASNR0002"));
        });
    }

    [Test]
    public void WriterRejectsNegativeNumbersAndUnknownClasses()
    {
        BerIdentifierWriter writer = new BerIdentifierWriter(
            new AsnResourceLimits());

        BerIdentifierWriteResult negative = writer.WriteIdentifier(
            new AsnTag { Number = -1 },
            false);
        BerIdentifierWriteResult unknownClass = writer.WriteIdentifier(
            new AsnTag
            {
                Class = (AsnTagClass)4,
                Number = 1
            },
            false);

        Assert.Multiple(() =>
        {
            Assert.That(negative.Success, Is.False);
            Assert.That(negative.LimitExceeded, Is.False);
            Assert.That(negative.ErrorCode, Is.EqualTo("ASNE0002"));
            Assert.That(unknownClass.Success, Is.False);
            Assert.That(unknownClass.ErrorCode, Is.EqualTo("ASNE0001"));
        });
    }
}
