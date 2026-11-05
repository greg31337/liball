namespace dns.Tests.Internal.Wire;

public class WireReaderTests
{
    [Test]
    public void ConstructorRejectsRegionsOutsideTheSource()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => new WireReader(new byte[1], -1, 1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => new WireReader(new byte[1], 0, 2),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => new WireReader(new byte[1], 1, 1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    [Test]
    public void UnsignedIntegersUseNetworkByteOrderAndAdvanceExactly()
    {
        byte[] source =
        [
            0x7f,
            0x12, 0x34,
            0x89, 0xab, 0xcd, 0xef,
            0x01, 0x23, 0x45, 0x67, 0x89, 0xab
        ];
        WireReader reader = new WireReader(source, 0, source.Length);

        WireReadResult value8 = reader.ReadUnsigned8();
        WireReadResult value16 = reader.ReadUnsigned16();
        WireReadResult value32 = reader.ReadUnsigned32();
        WireReadResult value48 = reader.ReadUnsigned48();

        Assert.Multiple(() =>
        {
            Assert.That(value8.Success, Is.True);
            Assert.That(value8.UnsignedValue, Is.EqualTo(0x7fUL));
            Assert.That(value8.StartOffset, Is.Zero);
            Assert.That(value8.NextOffset, Is.EqualTo(1));
            Assert.That(value16.UnsignedValue, Is.EqualTo(0x1234UL));
            Assert.That(value16.StartOffset, Is.EqualTo(1));
            Assert.That(value16.NextOffset, Is.EqualTo(3));
            Assert.That(value32.UnsignedValue, Is.EqualTo(0x89abcdefUL));
            Assert.That(value32.StartOffset, Is.EqualTo(3));
            Assert.That(value32.NextOffset, Is.EqualTo(7));
            Assert.That(value48.UnsignedValue, Is.EqualTo(0x0123456789abUL));
            Assert.That(value48.StartOffset, Is.EqualTo(7));
            Assert.That(value48.NextOffset, Is.EqualTo(13));
            Assert.That(reader.ConsumedLength(), Is.EqualTo(source.Length));
            Assert.That(reader.RemainingLength(), Is.Zero);
        });
    }

    [Test]
    public void IntegerReadsSupportZeroAndMaximumValues()
    {
        WireReader reader = new WireReader(
            [0, 0, 0, 0, 0, 0, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff],
            0,
            12);

        WireReadResult zero = reader.ReadUnsigned48();
        WireReadResult maximum = reader.ReadUnsigned48();

        Assert.Multiple(() =>
        {
            Assert.That(zero.UnsignedValue, Is.Zero);
            Assert.That(maximum.UnsignedValue, Is.EqualTo(0xffffffffffffUL));
            Assert.That(reader.ReadOffset(), Is.EqualTo(12));
        });
    }

    [Test]
    public void TruncatedIntegerReadReportsAbsoluteOffsetWithoutAdvancing()
    {
        WireReader reader = new WireReader([0xaa, 0xbb, 0xcc], 1, 2);

        WireReadResult result = reader.ReadUnsigned32();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.StartOffset, Is.EqualTo(1));
            Assert.That(result.NextOffset, Is.EqualTo(1));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.UnexpectedEnd));
            Assert.That(result.Diagnostics[0].HasOffset, Is.True);
            Assert.That(result.Diagnostics[0].Offset, Is.EqualTo(1));
            Assert.That(result.Diagnostics[0].Field, Is.EqualTo("unsigned32"));
            Assert.That(reader.ReadOffset(), Is.EqualTo(1));
            Assert.That(reader.RemainingLength(), Is.EqualTo(2));
        });
    }

    [Test]
    public void ByteReadsHandleEmptyExactAndInvalidCounts()
    {
        WireReader reader = new WireReader([1, 2, 3], 0, 3);

        WireReadResult empty = reader.ReadBytes(0);
        WireReadResult exact = reader.ReadBytes(3);
        WireReadResult negative = reader.ReadBytes(-1);

        Assert.Multiple(() =>
        {
            Assert.That(empty.Success, Is.True);
            Assert.That(empty.Bytes, Is.Empty);
            Assert.That(empty.StartOffset, Is.Zero);
            Assert.That(empty.NextOffset, Is.Zero);
            Assert.That(exact.Success, Is.True);
            Assert.That(exact.Bytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(negative.Success, Is.False);
            Assert.That(negative.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ValueOutOfRange));
            Assert.That(reader.ReadOffset(), Is.EqualTo(3));
        });
    }

    [Test]
    public void CharacterStringsPreserveEmptyAndMaximumLengthPayloads()
    {
        byte[] source = new byte[257];
        source[0] = 0;
        source[1] = 255;
        int index = 0;
        while (index < 255)
        {
            source[index + 2] = (byte)index;
            index = index + 1;
        }

        WireReader reader = new WireReader(source, 0, source.Length);
        WireReadResult empty = reader.ReadCharacterString();
        WireReadResult maximum = reader.ReadCharacterString();

        Assert.Multiple(() =>
        {
            Assert.That(empty.Success, Is.True);
            Assert.That(empty.UnsignedValue, Is.Zero);
            Assert.That(empty.Bytes, Is.Empty);
            Assert.That(maximum.Success, Is.True);
            Assert.That(maximum.UnsignedValue, Is.EqualTo(255));
            Assert.That(maximum.Bytes, Has.Length.EqualTo(255));
            Assert.That(maximum.Bytes[0], Is.Zero);
            Assert.That(maximum.Bytes[254], Is.EqualTo(254));
            Assert.That(reader.RemainingLength(), Is.Zero);
        });
    }

    [Test]
    public void TruncatedCharacterStringDoesNotConsumeItsLength()
    {
        WireReader reader = new WireReader([0xaa, 3, 1, 2], 1, 3);

        WireReadResult result = reader.ReadCharacterString();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.UnexpectedEnd));
            Assert.That(result.Diagnostics[0].Offset, Is.EqualTo(2));
            Assert.That(result.Diagnostics[0].Field, Is.EqualTo("characterString"));
            Assert.That(reader.ReadOffset(), Is.EqualTo(1));
            Assert.That(reader.RemainingLength(), Is.EqualTo(3));
        });
    }

    [Test]
    public void AddressReadsRequireExactIpv4AndIpv6Widths()
    {
        byte[] source =
        [
            192, 0, 2, 1,
            0x20, 0x01, 0x0d, 0xb8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1
        ];
        WireReader reader = new WireReader(source, 0, source.Length);

        WireReadResult ipv4 = reader.ReadIpv4Address();
        WireReadResult ipv6 = reader.ReadIpv6Address();
        WireReadResult missing = reader.ReadIpv4Address();

        Assert.Multiple(() =>
        {
            Assert.That(ipv4.Bytes, Is.EqualTo(new byte[] { 192, 0, 2, 1 }));
            Assert.That(ipv6.Success, Is.True);
            Assert.That(ipv6.Bytes, Has.Length.EqualTo(16));
            Assert.That(ipv6.Bytes[0], Is.EqualTo(0x20));
            Assert.That(ipv6.Bytes[15], Is.EqualTo(1));
            Assert.That(missing.Success, Is.False);
            Assert.That(missing.Diagnostics[0].Field, Is.EqualTo("ipv4Address"));
            Assert.That(reader.ReadOffset(), Is.EqualTo(source.Length));
        });
    }

    [Test]
    public void SubregionsAreBoundedAndAdvanceTheParentIndependently()
    {
        WireReader parent = new WireReader([0xaa, 0xbb, 0xcc, 0xdd], 0, 4);

        WireReadResult region = parent.ReadSubregion(2);
        WireReader child = region.Subregion!;
        WireReadResult childValue = child.ReadUnsigned16();
        WireReadResult childPastEnd = child.ReadUnsigned8();
        WireReadResult parentValue = parent.ReadUnsigned16();

        Assert.Multiple(() =>
        {
            Assert.That(region.Success, Is.True);
            Assert.That(region.StartOffset, Is.Zero);
            Assert.That(region.NextOffset, Is.EqualTo(2));
            Assert.That(childValue.UnsignedValue, Is.EqualTo(0xaabbUL));
            Assert.That(childPastEnd.Success, Is.False);
            Assert.That(childPastEnd.Diagnostics[0].Offset, Is.EqualTo(2));
            Assert.That(parentValue.UnsignedValue, Is.EqualTo(0xccddUL));
            Assert.That(parent.RemainingLength(), Is.Zero);
        });
    }

    [Test]
    public void OversizedSubregionDoesNotAdvanceTheParent()
    {
        WireReader reader = new WireReader([1, 2], 0, 2);

        WireReadResult result = reader.ReadSubregion(3);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.UnexpectedEnd));
            Assert.That(result.Diagnostics[0].Field, Is.EqualTo("subregion"));
            Assert.That(reader.ReadOffset(), Is.Zero);
            Assert.That(reader.RemainingLength(), Is.EqualTo(2));
        });
    }

    [Test]
    public void TypeBitmapDecodesIncreasingWindowsAndCanonicalBits()
    {
        byte[] source = [0xff, 0, 2, 0x40, 0x01, 1, 1, 0x80, 0xee];
        WireReader reader = new WireReader(source, 1, 7);

        WireReadResult result = reader.ReadTypeBitmap(7);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.IntegerValues, Is.EqualTo(new[] { 1, 15, 256 }));
            Assert.That(result.Bytes,
                Is.EqualTo(new byte[] { 0, 2, 0x40, 0x01, 1, 1, 0x80 }));
            Assert.That(result.StartOffset, Is.EqualTo(1));
            Assert.That(result.NextOffset, Is.EqualTo(8));
            Assert.That(reader.RemainingLength(), Is.Zero);
        });
    }

    [Test]
    public void EmptyTypeBitmapConsumesNoBytes()
    {
        WireReader reader = new WireReader([1], 0, 1);

        WireReadResult result = reader.ReadTypeBitmap(0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.IntegerValues, Is.Empty);
            Assert.That(result.Bytes, Is.Empty);
            Assert.That(reader.ReadOffset(), Is.Zero);
        });
    }

    [Test]
    public void TypeBitmapRejectsUnorderedWindowsWithoutAdvancing()
    {
        WireReader reader = new WireReader([1, 1, 0x80, 0, 1, 0x40], 0, 6);

        WireReadResult result = reader.ReadTypeBitmap(6);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidTypeBitmap));
            Assert.That(result.Diagnostics[0].Offset, Is.EqualTo(3));
            Assert.That(reader.ReadOffset(), Is.Zero);
        });
    }

    [Test]
    public void TypeBitmapRejectsInvalidWindowLengthsWithoutAdvancing()
    {
        WireReader zeroLengthReader = new WireReader([0, 0], 0, 2);
        WireReader excessiveLengthReader = new WireReader([0, 33], 0, 2);

        WireReadResult zeroLength = zeroLengthReader.ReadTypeBitmap(2);
        WireReadResult excessiveLength = excessiveLengthReader.ReadTypeBitmap(2);

        Assert.Multiple(() =>
        {
            Assert.That(zeroLength.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidTypeBitmap));
            Assert.That(zeroLength.Diagnostics[0].Offset, Is.EqualTo(1));
            Assert.That(excessiveLength.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidTypeBitmap));
            Assert.That(excessiveLength.Diagnostics[0].Offset, Is.EqualTo(1));
            Assert.That(zeroLengthReader.ReadOffset(), Is.Zero);
            Assert.That(excessiveLengthReader.ReadOffset(), Is.Zero);
        });
    }

    [Test]
    public void TypeBitmapRejectsTruncationAndRedundantTrailingZeros()
    {
        WireReader truncatedHeaderReader = new WireReader([0], 0, 1);
        WireReader truncatedPayloadReader = new WireReader([0, 2, 0x80], 0, 3);
        WireReader trailingZeroReader = new WireReader([0, 2, 0x80, 0], 0, 4);

        WireReadResult truncatedHeader = truncatedHeaderReader.ReadTypeBitmap(1);
        WireReadResult truncatedPayload = truncatedPayloadReader.ReadTypeBitmap(3);
        WireReadResult trailingZero = trailingZeroReader.ReadTypeBitmap(4);

        Assert.Multiple(() =>
        {
            Assert.That(truncatedHeader.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.UnexpectedEnd));
            Assert.That(truncatedHeader.Diagnostics[0].Offset, Is.Zero);
            Assert.That(truncatedPayload.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.UnexpectedEnd));
            Assert.That(truncatedPayload.Diagnostics[0].Offset, Is.EqualTo(2));
            Assert.That(trailingZero.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidTypeBitmap));
            Assert.That(trailingZero.Diagnostics[0].Offset, Is.EqualTo(3));
            Assert.That(truncatedHeaderReader.ReadOffset(), Is.Zero);
            Assert.That(truncatedPayloadReader.ReadOffset(), Is.Zero);
            Assert.That(trailingZeroReader.ReadOffset(), Is.Zero);
        });
    }
}
