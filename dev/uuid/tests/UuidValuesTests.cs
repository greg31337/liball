using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class UuidValuesTests
{
    [Test]
    public void NilAndMax_HaveCanonicalRfcValues()
    {
        Assert.That(UuidValues.Nil.ToCanonicalString(), Is.EqualTo("00000000-0000-0000-0000-000000000000"));
        Assert.That(UuidValues.Nil.ToUpperString(), Is.EqualTo("00000000-0000-0000-0000-000000000000"));
        Assert.That(UuidValues.Nil.ToCompactString(), Is.EqualTo("00000000000000000000000000000000"));
        Assert.That(UuidValues.Max.ToCanonicalString(), Is.EqualTo("ffffffff-ffff-ffff-ffff-ffffffffffff"));
        Assert.That(UuidValues.Max.ToUpperString(), Is.EqualTo("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"));
        Assert.That(UuidValues.Max.ToCompactUpperString(), Is.EqualTo("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF"));
    }

    [Test]
    public void NilAndMax_WorkWithDetectionVersionAndVariantOperations()
    {
        Assert.That(UuidValues.Nil.IsNil(), Is.True);
        Assert.That(UuidValues.Nil.IsMax(), Is.False);
        Assert.That(UuidValues.Nil.Version(), Is.EqualTo(UuidVersion.Unknown));
        Assert.That(UuidValues.Nil.Variant(), Is.EqualTo(UuidVariant.Ncs));
        Assert.That(UuidValues.Max.IsNil(), Is.False);
        Assert.That(UuidValues.Max.IsMax(), Is.True);
        Assert.That(UuidValues.Max.Version(), Is.EqualTo(UuidVersion.Unknown));
        Assert.That(UuidValues.Max.Variant(), Is.EqualTo(UuidVariant.Future));
    }

    [Test]
    public void NilAndMax_RoundTripThroughParsingAndGuidConversion()
    {
        Uuid uuid = new Uuid();
        UuidParseResult nil = uuid.Parse(UuidValues.Nil.ToCompactString());
        UuidParseResult max = uuid.Parse("URN:UUID:" + UuidValues.Max.ToUpperString());

        Assert.That(nil.Ok, Is.True);
        Assert.That(nil.Value, Is.Not.Null);
        Assert.That(nil.Value!.EqualsValue(UuidValues.Nil), Is.True);
        Assert.That(max.Ok, Is.True);
        Assert.That(max.Value, Is.Not.Null);
        Assert.That(max.Value!.EqualsValue(UuidValues.Max), Is.True);
        Assert.That(UuidValues.Nil.ToGuid(), Is.EqualTo(Guid.Empty));
        Assert.That(uuid.FromGuid(UuidValues.Max.ToGuid()).EqualsValue(UuidValues.Max), Is.True);
    }

    [Test]
    public void NilAndMax_WorkWithValidationAndComparisonOperations()
    {
        Uuid uuid = new Uuid();
        IdentifierValidationResult nil = uuid.Validate(UuidValues.Nil.ToCanonicalString());
        IdentifierValidationResult max = uuid.Validate(UuidValues.Max.ToCanonicalString());
        IdentifierValidationResult strictNil = uuid.ValidateRfc9562Variant(UuidValues.Nil.ToCanonicalString());
        IdentifierValidationResult strictMax = uuid.ValidateRfc9562Variant(UuidValues.Max.ToCanonicalString());

        Assert.That(nil.Ok, Is.True);
        Assert.That(max.Ok, Is.True);
        Assert.That(strictNil.Code, Is.EqualTo(IdentifierErrorCode.InvalidUuidVariant));
        Assert.That(strictMax.Code, Is.EqualTo(IdentifierErrorCode.InvalidUuidVariant));
        Assert.That(uuid.CompareValues(UuidValues.Nil, UuidValues.Max), Is.LessThan(0));
        Assert.That(new Identifier().CompareValues(UuidValues.Max, UuidValues.Nil), Is.GreaterThan(0));
    }

    [Test]
    public void NilAndMax_DefensivelyCopyBytes()
    {
        byte[] nilBytes = UuidValues.Nil.ToBytes();
        byte[] maxBytes = UuidValues.Max.ToBytes();
        byte[] nilDestination = new byte[16];
        byte[] maxDestination = new byte[16];

        nilBytes[0] = 0xFF;
        maxBytes[0] = 0x00;
        UuidValues.Nil.CopyTo(nilDestination);
        UuidValues.Max.CopyTo(maxDestination);
        nilDestination[1] = 0xFF;
        maxDestination[1] = 0x00;

        Assert.That(UuidValues.Nil.IsNil(), Is.True);
        Assert.That(UuidValues.Nil.ToBytes()[0], Is.EqualTo(0x00));
        Assert.That(UuidValues.Nil.ToBytes()[1], Is.EqualTo(0x00));
        Assert.That(UuidValues.Max.IsMax(), Is.True);
        Assert.That(UuidValues.Max.ToBytes()[0], Is.EqualTo(0xFF));
        Assert.That(UuidValues.Max.ToBytes()[1], Is.EqualTo(0xFF));
    }
}
