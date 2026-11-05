using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class UuidGeneratorTests
{
    [Test]
    public void NewV4_GeneratesVersion4UuidWithRfc9562Variant()
    {
        int index = 0;
        while (index < 100)
        {
            UuidValue value = UuidGenerator.NewV4();

            Assert.That(value.Version(), Is.EqualTo(UuidVersion.Version4));
            Assert.That(value.Variant(), Is.EqualTo(UuidVariant.Rfc9562));
            Assert.That(value.IsNil(), Is.False);
            Assert.That(value.IsMax(), Is.False);
            index = index + 1;
        }
    }

    [Test]
    public void NewV4_GeneratesUuidAcceptedByVersionValidation()
    {
        UuidValue value = new Uuid().NewV4();

        IdentifierValidationResult result = new Uuid().ValidateVersion(value.ToCanonicalString(), UuidVersion.Version4);

        Assert.That(result.Ok, Is.True);
        Assert.That(result.Kind, Is.EqualTo(IdentifierKind.Uuid));
        Assert.That(result.NormalizedText, Is.EqualTo(value.ToCanonicalString()));
    }

    [Test]
    public void NewV4_GeneratesDifferentValuesAcrossCalls()
    {
        UuidValue first = UuidGenerator.NewV4();
        UuidValue second = UuidGenerator.NewV4();

        Assert.That(first.EqualsValue(second), Is.False);
    }

    [Test]
    public void ApplyVersionAndVariant_MasksOnlyRequiredBits()
    {
        byte[] bytes = new byte[]
        {
            0xFF,
            0xFF,
            0xFF,
            0xFF,
            0xFF,
            0xFF,
            0x00,
            0xFF,
            0xFF,
            0xFF,
            0xFF,
            0xFF,
            0xFF,
            0xFF,
            0xFF,
            0xFF
        };

        UuidGenerator.ApplyVersionAndVariant(bytes, 4);

        Assert.That(bytes[6], Is.EqualTo(0x40));
        Assert.That(bytes[8], Is.EqualTo(0xBF));
    }

    [Test]
    public void ApplyVersionAndVariant_RejectsInvalidInput()
    {
        Assert.That(() => UuidGenerator.ApplyVersionAndVariant(null, 4), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => UuidGenerator.ApplyVersionAndVariant(new byte[15], 4), Throws.ArgumentException);
        Assert.That(() => UuidGenerator.ApplyVersionAndVariant(new byte[16], -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => UuidGenerator.ApplyVersionAndVariant(new byte[16], 16), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void NewV7AtUnixMilliseconds_WritesTimestampVersionAndVariant()
    {
        long unixMilliseconds = 0x0123456789AB;

        UuidValue value = UuidGenerator.NewV7AtUnixMilliseconds(unixMilliseconds);
        byte[] bytes = value.ToBytes();

        Assert.That(BigEndianTools.ReadUInt48(bytes, 0), Is.EqualTo(unixMilliseconds));
        Assert.That(value.Version(), Is.EqualTo(UuidVersion.Version7));
        Assert.That(value.Variant(), Is.EqualTo(UuidVariant.Rfc9562));
        Assert.That(new Uuid().ValidateVersion(value.ToCanonicalString(), UuidVersion.Version7).Ok, Is.True);
    }

    [Test]
    public void NewV7At_UsesSuppliedDateTimeOffset()
    {
        DateTimeOffset time = DateTimeOffset.FromUnixTimeMilliseconds(1469922850259);

        UuidValue value = new Uuid().NewV7At(time);

        Assert.That(BigEndianTools.ReadUInt48(value.ToBytes(), 0), Is.EqualTo(1469922850259));
        Assert.That(value.Version(), Is.EqualTo(UuidVersion.Version7));
        Assert.That(value.Variant(), Is.EqualTo(UuidVariant.Rfc9562));
    }

    [Test]
    public void NewV7_UsesCurrentUnixMilliseconds()
    {
        long before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        UuidValue value = new Uuid().NewV7();

        long after = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long embedded = BigEndianTools.ReadUInt48(value.ToBytes(), 0);
        Assert.That(embedded, Is.GreaterThanOrEqualTo(before));
        Assert.That(embedded, Is.LessThanOrEqualTo(after));
        Assert.That(value.Version(), Is.EqualTo(UuidVersion.Version7));
        Assert.That(value.Variant(), Is.EqualTo(UuidVariant.Rfc9562));
    }

    [Test]
    public void NewV7AtUnixMilliseconds_RejectsValuesOutsideFortyEightBitUnixRange()
    {
        Assert.That(() => UuidGenerator.NewV7AtUnixMilliseconds(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => UuidGenerator.NewV7AtUnixMilliseconds(BigEndianTools.MaxUInt48 + 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => UuidGenerator.NewV7At(DateTimeOffset.UnixEpoch.AddMilliseconds(-1)), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void NewV7AtUnixMilliseconds_RetainsRandomTailAcrossSameTimestamp()
    {
        UuidValue first = UuidGenerator.NewV7AtUnixMilliseconds(123456789);
        UuidValue second = UuidGenerator.NewV7AtUnixMilliseconds(123456789);

        Assert.That(first.EqualsValue(second), Is.False);
    }
}
