namespace dns.Tests.Internal.Wire;

public class WireWriterTests
{
    [Test]
    public void ConstructorRejectsInvalidBufferAndCountingRegions()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => new WireWriter(new byte[1], -1, 1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => new WireWriter(new byte[1], 0, 2),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => new WireWriter(null, 1, 1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => new WireWriter(null, 0, -1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    [Test]
    public void UnsignedIntegersUseNetworkByteOrderAndAdvanceExactly()
    {
        byte[] destination = new byte[13];
        WireWriter writer = new WireWriter(destination, 0, destination.Length);

        WireWriteResult value8 = writer.WriteUnsigned8(0x7f);
        WireWriteResult value16 = writer.WriteUnsigned16(0x1234);
        WireWriteResult value32 = writer.WriteUnsigned32(0x89abcdef);
        WireWriteResult value48 = writer.WriteUnsigned48(0x0123456789ab);

        Assert.Multiple(() =>
        {
            Assert.That(value8.Success, Is.True);
            Assert.That(value8.BytesWritten, Is.EqualTo(1));
            Assert.That(value8.StartOffset, Is.Zero);
            Assert.That(value8.NextOffset, Is.EqualTo(1));
            Assert.That(value16.StartOffset, Is.EqualTo(1));
            Assert.That(value16.NextOffset, Is.EqualTo(3));
            Assert.That(value32.StartOffset, Is.EqualTo(3));
            Assert.That(value32.NextOffset, Is.EqualTo(7));
            Assert.That(value48.StartOffset, Is.EqualTo(7));
            Assert.That(value48.NextOffset, Is.EqualTo(13));
            Assert.That(destination, Is.EqualTo(new byte[]
            {
                0x7f,
                0x12, 0x34,
                0x89, 0xab, 0xcd, 0xef,
                0x01, 0x23, 0x45, 0x67, 0x89, 0xab
            }));
            Assert.That(writer.WrittenLength(), Is.EqualTo(13));
            Assert.That(writer.RemainingLength(), Is.Zero);
        });
    }

    [Test]
    public void UnsignedIntegerBoundariesRoundTripThroughWireReader()
    {
        byte[] destination = new byte[13];
        WireWriter writer = new WireWriter(destination, 0, destination.Length);
        writer.WriteUnsigned8(255);
        writer.WriteUnsigned16(65535);
        writer.WriteUnsigned32(4294967295);
        writer.WriteUnsigned48(281474976710655);
        WireReader reader = new WireReader(destination, 0, destination.Length);

        Assert.Multiple(() =>
        {
            Assert.That(reader.ReadUnsigned8().UnsignedValue, Is.EqualTo(255));
            Assert.That(reader.ReadUnsigned16().UnsignedValue, Is.EqualTo(65535));
            Assert.That(reader.ReadUnsigned32().UnsignedValue, Is.EqualTo(4294967295));
            Assert.That(reader.ReadUnsigned48().UnsignedValue,
                Is.EqualTo(281474976710655));
        });
    }

    [Test]
    public void OversizedUnsignedValuesFailWithoutWriting()
    {
        byte[] destination = [0xaa, 0xaa, 0xaa, 0xaa, 0xaa, 0xaa];
        WireWriter writer = new WireWriter(destination, 0, destination.Length);

        WireWriteResult value8 = writer.WriteUnsigned8(256);
        WireWriteResult value16 = writer.WriteUnsigned16(65536);
        WireWriteResult value32 = writer.WriteUnsigned32(4294967296);
        WireWriteResult value48 = writer.WriteUnsigned48(281474976710656);

        Assert.Multiple(() =>
        {
            Assert.That(value8.Success, Is.False);
            Assert.That(value16.Success, Is.False);
            Assert.That(value32.Success, Is.False);
            Assert.That(value48.Success, Is.False);
            Assert.That(value8.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ValueOutOfRange));
            Assert.That(value48.Diagnostics[0].Field, Is.EqualTo("unsigned48"));
            Assert.That(value48.Diagnostics[0].HasOffset, Is.True);
            Assert.That(value48.Diagnostics[0].Offset, Is.Zero);
            Assert.That(writer.WriteOffset(), Is.Zero);
            Assert.That(destination,
                Is.EqualTo(new byte[] { 0xaa, 0xaa, 0xaa, 0xaa, 0xaa, 0xaa }));
        });
    }

    [Test]
    public void CapacityFailureIsAtomicAndReportsTheWriteOffset()
    {
        byte[] destination = [0xaa, 0xaa, 0xaa];
        WireWriter writer = new WireWriter(destination, 0, destination.Length);

        WireWriteResult result = writer.WriteUnsigned32(1);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.BytesWritten, Is.Zero);
            Assert.That(result.StartOffset, Is.Zero);
            Assert.That(result.NextOffset, Is.Zero);
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ResourceLimitExceeded));
            Assert.That(result.Diagnostics[0].Offset, Is.Zero);
            Assert.That(destination, Is.EqualTo(new byte[] { 0xaa, 0xaa, 0xaa }));
            Assert.That(writer.WrittenLength(), Is.Zero);
        });
    }

    [Test]
    public void CountingModeMatchesBufferedSizeWithoutProducingBytes()
    {
        byte[] destination = new byte[100];
        WireWriter buffered = new WireWriter(destination, 0, destination.Length);
        WireWriter counting = new WireWriter(null, 0, destination.Length);

        buffered.WriteUnsigned16(1);
        buffered.WriteCharacterString([1, 2]);
        buffered.WriteBytes([3, 4, 5]);
        buffered.WriteTypeBitmap(new List<int> { 256, 1 });
        counting.WriteUnsigned16(1);
        counting.WriteCharacterString([1, 2]);
        counting.WriteBytes([3, 4, 5]);
        counting.WriteTypeBitmap(new List<int> { 256, 1 });

        Assert.Multiple(() =>
        {
            Assert.That(buffered.IsCounting(), Is.False);
            Assert.That(counting.IsCounting(), Is.True);
            Assert.That(counting.WrittenLength(), Is.EqualTo(buffered.WrittenLength()));
            Assert.That(counting.RemainingLength(), Is.EqualTo(buffered.RemainingLength()));
            Assert.That(counting.CopyWrittenBytes(), Is.Empty);
            Assert.That(buffered.CopyWrittenBytes(), Has.Length.EqualTo(14));
        });
    }

    [Test]
    public void CountingModeEnforcesItsConfiguredLimit()
    {
        WireWriter writer = new WireWriter(null, 0, 2);
        WireWriteResult first = writer.WriteUnsigned16(1);

        WireWriteResult overflow = writer.WriteUnsigned8(1);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(overflow.Success, Is.False);
            Assert.That(overflow.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ResourceLimitExceeded));
            Assert.That(writer.WrittenLength(), Is.EqualTo(2));
        });
    }

    [Test]
    public void CharacterStringsEncodeEmptyAndMaximumPayloads()
    {
        byte[] maximumPayload = new byte[255];
        int index = 0;
        while (index < maximumPayload.Length)
        {
            maximumPayload[index] = (byte)index;
            index = index + 1;
        }

        byte[] destination = new byte[257];
        WireWriter writer = new WireWriter(destination, 0, destination.Length);
        WireWriteResult empty = writer.WriteCharacterString([]);
        WireWriteResult maximum = writer.WriteCharacterString(maximumPayload);

        Assert.Multiple(() =>
        {
            Assert.That(empty.Success, Is.True);
            Assert.That(empty.BytesWritten, Is.EqualTo(1));
            Assert.That(maximum.Success, Is.True);
            Assert.That(maximum.BytesWritten, Is.EqualTo(256));
            Assert.That(destination[0], Is.Zero);
            Assert.That(destination[1], Is.EqualTo(255));
            Assert.That(destination[2], Is.Zero);
            Assert.That(destination[256], Is.EqualTo(254));
            Assert.That(writer.RemainingLength(), Is.Zero);
        });
    }

    [Test]
    public void OverlongCharacterStringFailsWithoutWritingItsLength()
    {
        byte[] destination = [0xaa];
        WireWriter writer = new WireWriter(destination, 0, 1);

        WireWriteResult result = writer.WriteCharacterString(new byte[256]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ValueOutOfRange));
            Assert.That(result.Diagnostics[0].Field, Is.EqualTo("characterString"));
            Assert.That(destination[0], Is.EqualTo(0xaa));
            Assert.That(writer.WriteOffset(), Is.Zero);
        });
    }

    [Test]
    public void AddressWritesEnforceExactWidths()
    {
        byte[] destination = new byte[20];
        WireWriter writer = new WireWriter(destination, 0, destination.Length);
        WireWriteResult invalidIpv4 = writer.WriteIpv4Address([1, 2, 3]);
        WireWriteResult ipv4 = writer.WriteIpv4Address([192, 0, 2, 1]);
        WireWriteResult ipv6 = writer.WriteIpv6Address(
            [0x20, 0x01, 0x0d, 0xb8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1]);

        Assert.Multiple(() =>
        {
            Assert.That(invalidIpv4.Success, Is.False);
            Assert.That(invalidIpv4.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ValueOutOfRange));
            Assert.That(ipv4.Success, Is.True);
            Assert.That(ipv6.Success, Is.True);
            Assert.That(destination[0], Is.EqualTo(192));
            Assert.That(destination[3], Is.EqualTo(1));
            Assert.That(destination[4], Is.EqualTo(0x20));
            Assert.That(destination[19], Is.EqualTo(1));
            Assert.That(writer.WrittenLength(), Is.EqualTo(20));
        });
    }

    [Test]
    public void TypeBitmapCanonicalizesOrderAndDuplicates()
    {
        byte[] destination = new byte[7];
        WireWriter writer = new WireWriter(destination, 0, destination.Length);

        WireWriteResult result = writer.WriteTypeBitmap(
            new List<int> { 256, 15, 1, 1 });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.BytesWritten, Is.EqualTo(7));
            Assert.That(destination,
                Is.EqualTo(new byte[] { 0, 2, 0x40, 0x01, 1, 1, 0x80 }));
        });
    }

    [Test]
    public void TypeBitmapHandlesEmptyInputAndRejectsInvalidTypes()
    {
        byte[] destination = [0xaa];
        WireWriter writer = new WireWriter(destination, 0, 1);
        WireWriteResult empty = writer.WriteTypeBitmap(new List<int>());
        WireWriteResult negative = writer.WriteTypeBitmap(new List<int> { -1 });
        WireWriteResult excessive = writer.WriteTypeBitmap(new List<int> { 65536 });

        Assert.Multiple(() =>
        {
            Assert.That(empty.Success, Is.True);
            Assert.That(empty.BytesWritten, Is.Zero);
            Assert.That(negative.Success, Is.False);
            Assert.That(excessive.Success, Is.False);
            Assert.That(negative.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidTypeBitmap));
            Assert.That(excessive.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidTypeBitmap));
            Assert.That(writer.WriteOffset(), Is.Zero);
            Assert.That(destination[0], Is.EqualTo(0xaa));
        });
    }

    [Test]
    public void TypeBitmapCapacityFailureDoesNotWritePartialWindows()
    {
        byte[] destination = [0xaa, 0xaa, 0xaa];
        WireWriter writer = new WireWriter(destination, 0, destination.Length);

        WireWriteResult result = writer.WriteTypeBitmap(new List<int> { 1, 15 });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ResourceLimitExceeded));
            Assert.That(destination, Is.EqualTo(new byte[] { 0xaa, 0xaa, 0xaa }));
            Assert.That(writer.WriteOffset(), Is.Zero);
        });
    }

    [Test]
    public void ReservedSectionCountCanBePatchedWithoutChangingLength()
    {
        byte[] destination = new byte[5];
        WireWriter writer = new WireWriter(destination, 0, destination.Length);
        WireWriteResult reservation = writer.ReserveSectionCount();
        writer.WriteBytes([0xaa, 0xbb, 0xcc]);

        WireWriteResult patch = writer.PatchSectionCount(reservation.PatchOffset, 0x1234);

        Assert.Multiple(() =>
        {
            Assert.That(reservation.Success, Is.True);
            Assert.That(reservation.PatchOffset, Is.Zero);
            Assert.That(reservation.BytesWritten, Is.EqualTo(2));
            Assert.That(patch.Success, Is.True);
            Assert.That(patch.BytesWritten, Is.Zero);
            Assert.That(patch.NextOffset, Is.EqualTo(5));
            Assert.That(destination,
                Is.EqualTo(new byte[] { 0x12, 0x34, 0xaa, 0xbb, 0xcc }));
            Assert.That(writer.WrittenLength(), Is.EqualTo(5));
        });
    }

    [Test]
    public void RecordDataLengthPatchUsesBytesWrittenAfterItsStart()
    {
        byte[] destination = new byte[6];
        WireWriter writer = new WireWriter(destination, 0, destination.Length);
        WireWriteResult reservation = writer.ReserveRecordDataLength();
        int recordDataStart = writer.WriteOffset();
        writer.WriteBytes([1, 2, 3, 4]);

        WireWriteResult patch = writer.PatchRecordDataLength(
            reservation.PatchOffset,
            recordDataStart);

        Assert.Multiple(() =>
        {
            Assert.That(patch.Success, Is.True);
            Assert.That(destination,
                Is.EqualTo(new byte[] { 0, 4, 1, 2, 3, 4 }));
            Assert.That(writer.WriteOffset(), Is.EqualTo(6));
        });
    }

    [Test]
    public void RecordDataLengthOverflowFailsExplicitly()
    {
        WireWriter writer = new WireWriter(null, 0, 70000);
        WireWriteResult reservation = writer.ReserveRecordDataLength();
        int recordDataStart = writer.WriteOffset();
        writer.WriteBytes(new byte[65536]);

        WireWriteResult patch = writer.PatchRecordDataLength(
            reservation.PatchOffset,
            recordDataStart);

        Assert.Multiple(() =>
        {
            Assert.That(patch.Success, Is.False);
            Assert.That(patch.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ValueOutOfRange));
            Assert.That(patch.Diagnostics[0].Field, Is.EqualTo("recordDataLength"));
            Assert.That(writer.WrittenLength(), Is.EqualTo(65538));
        });
    }

    [Test]
    public void InvalidPatchValuesAndOffsetsLeavePreviouslyWrittenBytesUnchanged()
    {
        byte[] destination = new byte[4];
        WireWriter writer = new WireWriter(destination, 0, destination.Length);
        WireWriteResult reservation = writer.ReserveSectionCount();
        writer.WriteUnsigned16(0xabcd);
        byte[] before = (byte[])destination.Clone();

        WireWriteResult excessive = writer.PatchSectionCount(
            reservation.PatchOffset,
            65536);
        WireWriteResult outside = writer.PatchSectionCount(int.MaxValue, 1);
        WireWriteResult invalidStart = writer.PatchRecordDataLength(
            reservation.PatchOffset,
            int.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(excessive.Success, Is.False);
            Assert.That(outside.Success, Is.False);
            Assert.That(invalidStart.Success, Is.False);
            Assert.That(excessive.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ValueOutOfRange));
            Assert.That(outside.Diagnostics[0].HasOffset, Is.True);
            Assert.That(destination, Is.EqualTo(before));
            Assert.That(writer.WriteOffset(), Is.EqualTo(4));
        });
    }

    [Test]
    public void NonzeroBufferRegionAndWrittenCopyExcludeSurroundingBytes()
    {
        byte[] destination = [0xaa, 0xaa, 0xaa, 0xaa, 0xaa];
        WireWriter writer = new WireWriter(destination, 1, 3);

        writer.WriteBytes([1, 2, 3]);

        Assert.Multiple(() =>
        {
            Assert.That(destination,
                Is.EqualTo(new byte[] { 0xaa, 1, 2, 3, 0xaa }));
            Assert.That(writer.CopyWrittenBytes(), Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(writer.WriteOffset(), Is.EqualTo(4));
            Assert.That(writer.WrittenLength(), Is.EqualTo(3));
        });
    }
}
