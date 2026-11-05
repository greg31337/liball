using asn1;

namespace asn1.Tests;

public class BerLengthTests
{
    [Test]
    public void DefiniteLengthsRoundTripShortAndMinimalLongForms()
    {
        (long Length, byte[] Encoded)[] cases =
        [
            (0, [0x00]),
            (127, [0x7f]),
            (128, [0x81, 0x80]),
            (255, [0x81, 0xff]),
            (256, [0x82, 0x01, 0x00]),
            (0x0102030405, [0x85, 0x01, 0x02, 0x03, 0x04, 0x05]),
            (long.MaxValue,
                [0x88, 0x7f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff])
        ];
        AsnResourceLimits limits = new AsnResourceLimits
        {
            MaximumLengthOctets = 9
        };
        BerLengthWriter writer = new BerLengthWriter(limits);
        BerLengthReader reader = new BerLengthReader(limits);

        foreach ((long length, byte[] encoded) in cases)
        {
            BerLengthWriteResult written = writer.WriteLength(length, false);
            BerLengthReadResult read = reader.ReadLength(written.Data, true);

            Assert.Multiple(() =>
            {
                Assert.That(written.Success, Is.True);
                Assert.That(written.Data, Is.EqualTo(encoded));
                Assert.That(written.WrittenCount, Is.EqualTo(encoded.Length));
                Assert.That(read.Status, Is.EqualTo(AsnDecodeStatus.Done));
                Assert.That(read.Indefinite, Is.False);
                Assert.That(read.Length, Is.EqualTo(length));
                Assert.That(read.Minimal, Is.True);
                Assert.That(read.ConsumedCount, Is.EqualTo(encoded.Length));
            });
        }
    }

    [Test]
    public void IndefiniteLengthAndEndOfContentsUseDistinctForms()
    {
        BerLengthWriter writer = new BerLengthWriter(
            new AsnResourceLimits());
        BerLengthReader reader = new BerLengthReader(
            new AsnResourceLimits());

        BerLengthWriteResult indefinite = writer.WriteLength(0, true);
        BerLengthReadResult readIndefinite = reader.ReadLength(
            indefinite.Data,
            true);
        BerLengthWriteResult eoc = writer.WriteEndOfContents();
        BerEndOfContentsResult readEoc = reader.ReadEndOfContents(eoc.Data);
        BerEndOfContentsResult ordinary = reader.ReadEndOfContents([0x02, 0x00]);
        BerEndOfContentsResult malformed = reader.ReadEndOfContents([0x00, 0x01]);

        Assert.Multiple(() =>
        {
            Assert.That(indefinite.Success, Is.True);
            Assert.That(indefinite.Data, Is.EqualTo(new byte[] { 0x80 }));
            Assert.That(readIndefinite.Status,
                Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(readIndefinite.Indefinite, Is.True);
            Assert.That(readIndefinite.ConsumedCount, Is.EqualTo(1));
            Assert.That(eoc.Success, Is.True);
            Assert.That(eoc.Data, Is.EqualTo(new byte[] { 0x00, 0x00 }));
            Assert.That(readEoc.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(readEoc.Matched, Is.True);
            Assert.That(readEoc.ConsumedCount, Is.EqualTo(2));
            Assert.That(ordinary.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(ordinary.Matched, Is.False);
            Assert.That(ordinary.ConsumedCount, Is.Zero);
            Assert.That(malformed.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(malformed.ErrorCode, Is.EqualTo("ASND0005"));
        });
    }

    [Test]
    public void ReaderSeparatesTruncationReservedFormsAndOverflow()
    {
        AsnResourceLimits wideLimits = new AsnResourceLimits
        {
            MaximumLengthOctets = 9
        };
        BerLengthReader reader = new BerLengthReader(wideLimits);

        BerLengthReadResult empty = reader.ReadLength([], true);
        BerLengthReadResult truncated = reader.ReadLength([0x82, 0x01], true);
        BerLengthReadResult reserved = reader.ReadLength([0xff], true);
        BerLengthReadResult overflow = reader.ReadLength(
            [0x88, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],
            true);
        BerEndOfContentsResult truncatedEoc = reader.ReadEndOfContents([0x00]);

        Assert.Multiple(() =>
        {
            Assert.That(empty.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            Assert.That(empty.ConsumedCount, Is.Zero);
            Assert.That(truncated.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            Assert.That(truncated.ConsumedCount, Is.EqualTo(2));
            Assert.That(reserved.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(reserved.ErrorCode, Is.EqualTo("ASND0002"));
            Assert.That(overflow.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(overflow.ErrorCode, Is.EqualTo("ASND0003"));
            Assert.That(overflow.ConsumedCount, Is.EqualTo(9));
            Assert.That(truncatedEoc.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            Assert.That(truncatedEoc.ConsumedCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void ReaderReportsNonMinimalLongFormsAndCanRequireMinimality()
    {
        BerLengthReader reader = new BerLengthReader(
            new AsnResourceLimits());

        BerLengthReadResult shortLong = reader.ReadLength(
            [0x81, 0x7f],
            false);
        BerLengthReadResult leadingZero = reader.ReadLength(
            [0x82, 0x00, 0x80],
            false);
        BerLengthReadResult strict = reader.ReadLength(
            [0x82, 0x00, 0x80],
            true);

        Assert.Multiple(() =>
        {
            Assert.That(shortLong.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(shortLong.Length, Is.EqualTo(127));
            Assert.That(shortLong.Minimal, Is.False);
            Assert.That(leadingZero.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(leadingZero.Length, Is.EqualTo(128));
            Assert.That(leadingZero.Minimal, Is.False);
            Assert.That(strict.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(strict.ErrorCode, Is.EqualTo("ASND0004"));
            Assert.That(strict.Length, Is.EqualTo(128));
            Assert.That(strict.ConsumedCount, Is.EqualTo(3));
        });
    }

    [Test]
    public void ReaderAndWriterEnforceLengthOctetLimitsBeforeAllocation()
    {
        AsnResourceLimits limits = new AsnResourceLimits
        {
            MaximumLengthOctets = 2
        };
        BerLengthReadResult read = new BerLengthReader(limits).ReadLength(
            [0x82, 0x01, 0x00],
            true);
        BerLengthWriteResult write = new BerLengthWriter(limits).WriteLength(
            256,
            false);
        BerLengthWriteResult negative = new BerLengthWriter(limits).WriteLength(
            -1,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(read.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(read.LimitExceeded, Is.True);
            Assert.That(read.ErrorCode, Is.EqualTo("ASNR0003"));
            Assert.That(read.ConsumedCount, Is.EqualTo(1));
            Assert.That(write.Success, Is.False);
            Assert.That(write.LimitExceeded, Is.True);
            Assert.That(write.ErrorCode, Is.EqualTo("ASNR0003"));
            Assert.That(write.Data, Is.Empty);
            Assert.That(negative.Success, Is.False);
            Assert.That(negative.LimitExceeded, Is.False);
            Assert.That(negative.ErrorCode, Is.EqualTo("ASNE0003"));
        });
    }

    [Test]
    public void TrackerAccountsForEveryNestedDefiniteOctet()
    {
        BerLengthTracker tracker = new BerLengthTracker(
            new AsnResourceLimits());

        BerLengthAccountingResult outer = tracker.EnterLength(10, false);
        BerLengthAccountingResult header = tracker.ConsumeOctets(2);
        BerLengthAccountingResult child = tracker.EnterLength(3, false);
        BerLengthAccountingResult childContent = tracker.ConsumeOctets(3);

        Assert.Multiple(() =>
        {
            Assert.That(outer.Success, Is.True);
            Assert.That(header.Success, Is.True);
            Assert.That(child.Success, Is.True);
            Assert.That(childContent.Success, Is.True);
            Assert.That(childContent.CompletedScopeCount, Is.EqualTo(1));
            Assert.That(tracker.GetDepth(), Is.EqualTo(1));
            Assert.That(tracker.GetInnermostRemaining(), Is.EqualTo(5));
        });

        BerLengthAccountingResult outerContent = tracker.ConsumeOctets(5);

        Assert.Multiple(() =>
        {
            Assert.That(outerContent.Success, Is.True);
            Assert.That(outerContent.CompletedScopeCount, Is.EqualTo(1));
            Assert.That(tracker.GetDepth(), Is.Zero);
        });
    }

    [Test]
    public void TrackerRejectsBoundaryViolationsAndClosesIndefiniteScopesWithEoc()
    {
        BerLengthTracker fitTracker = new BerLengthTracker(
            new AsnResourceLimits());
        fitTracker.EnterLength(4, false);
        fitTracker.ConsumeOctets(2);
        BerLengthAccountingResult oversizedChild = fitTracker.EnterLength(
            3,
            false);
        BerLengthAccountingResult oversizedConsumption =
            fitTracker.ConsumeOctets(3);

        BerLengthTracker indefiniteTracker = new BerLengthTracker(
            new AsnResourceLimits());
        indefiniteTracker.EnterLength(4, false);
        indefiniteTracker.EnterLength(0, true);
        BerLengthAccountingResult eoc =
            indefiniteTracker.ConsumeEndOfContents();

        AsnResourceLimits shallow = new AsnResourceLimits
        {
            MaximumNestingDepth = 1
        };
        BerLengthTracker shallowTracker = new BerLengthTracker(shallow);
        shallowTracker.EnterLength(0, true);
        BerLengthAccountingResult tooDeep = shallowTracker.EnterLength(0, true);
        BerLengthAccountingResult unexpectedEoc =
            new BerLengthTracker(new AsnResourceLimits())
                .ConsumeEndOfContents();

        Assert.Multiple(() =>
        {
            Assert.That(oversizedChild.Success, Is.False);
            Assert.That(oversizedChild.ErrorCode, Is.EqualTo("ASND0007"));
            Assert.That(oversizedConsumption.Success, Is.False);
            Assert.That(oversizedConsumption.ErrorCode,
                Is.EqualTo("ASND0009"));
            Assert.That(fitTracker.GetInnermostRemaining(), Is.EqualTo(2));
            Assert.That(eoc.Success, Is.True);
            Assert.That(eoc.CompletedScopeCount, Is.EqualTo(1));
            Assert.That(indefiniteTracker.GetDepth(), Is.EqualTo(1));
            Assert.That(indefiniteTracker.GetInnermostRemaining(),
                Is.EqualTo(2));
            Assert.That(tooDeep.Success, Is.False);
            Assert.That(tooDeep.LimitExceeded, Is.True);
            Assert.That(tooDeep.ErrorCode, Is.EqualTo("ASNR0004"));
            Assert.That(unexpectedEoc.Success, Is.False);
            Assert.That(unexpectedEoc.ErrorCode, Is.EqualTo("ASND0010"));
        });
    }
}
