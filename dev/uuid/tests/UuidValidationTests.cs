using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class UuidValidationTests
{
    [Test]
    public void Validate_AcceptsParsedUuidWithRfc9562Variant()
    {
        IdentifierValidationResult result = new Uuid().Validate("01890f13-66b8-7f7a-9f2b-6f3f9c2d8a1b");

        Assert.That(result.Ok, Is.True);
        Assert.That(result.Kind, Is.EqualTo(IdentifierKind.Uuid));
        Assert.That(result.NormalizedText, Is.EqualTo("01890f13-66b8-7f7a-9f2b-6f3f9c2d8a1b"));
        Assert.That(result.Error, Is.EqualTo(""));
    }

    [Test]
    public void Validate_AcceptsNilAndMaxSpecialValues()
    {
        IdentifierValidationResult nil = new Uuid().Validate("00000000-0000-0000-0000-000000000000");
        IdentifierValidationResult max = new Uuid().Validate("ffffffff-ffff-ffff-ffff-ffffffffffff");

        Assert.That(nil.Ok, Is.True);
        Assert.That(max.Ok, Is.True);
    }

    [Test]
    public void Validate_RejectsParseFailuresAndNonRfcVariants()
    {
        IdentifierValidationResult invalidText = new Uuid().Validate("not-a-uuid");
        IdentifierValidationResult invalidVariant = new Uuid().Validate("01890f13-66b8-7f7a-1f2b-6f3f9c2d8a1b");

        Assert.That(invalidText.Ok, Is.False);
        Assert.That(invalidText.Error, Is.EqualTo("UUID text length must be 32 compact hex characters, 36 canonical characters, or urn:uuid: plus canonical UUID"));
        Assert.That(invalidVariant.Ok, Is.False);
        Assert.That(invalidVariant.Error, Is.EqualTo("UUID does not use RFC 9562 variant bits"));
    }

    [Test]
    public void ValidateRfc9562Variant_StrictlyChecksVariantBits()
    {
        IdentifierValidationResult valid = new Uuid().ValidateRfc9562Variant("01890f13-66b8-7f7a-bf2b-6f3f9c2d8a1b");
        IdentifierValidationResult nil = new Uuid().ValidateRfc9562Variant("00000000-0000-0000-0000-000000000000");

        Assert.That(valid.Ok, Is.True);
        Assert.That(nil.Ok, Is.False);
        Assert.That(nil.Error, Is.EqualTo("UUID does not use RFC 9562 variant bits"));
    }

    [Test]
    public void ValidateVersion_ChecksRfcVariantAndExpectedVersion()
    {
        IdentifierValidationResult valid = new Uuid().ValidateVersion("01890f13-66b8-7f7a-9f2b-6f3f9c2d8a1b", UuidVersion.Version7);
        IdentifierValidationResult wrongVersion = new Uuid().ValidateVersion("01890f13-66b8-7f7a-9f2b-6f3f9c2d8a1b", UuidVersion.Version4);
        IdentifierValidationResult unknownVersion = new Uuid().ValidateVersion("01890f13-66b8-7f7a-9f2b-6f3f9c2d8a1b", UuidVersion.Unknown);
        IdentifierValidationResult wrongVariant = new Uuid().ValidateVersion("01890f13-66b8-7f7a-1f2b-6f3f9c2d8a1b", UuidVersion.Version7);

        Assert.That(valid.Ok, Is.True);
        Assert.That(wrongVersion.Ok, Is.False);
        Assert.That(wrongVersion.Error, Is.EqualTo("UUID version does not match expected version"));
        Assert.That(unknownVersion.Ok, Is.False);
        Assert.That(unknownVersion.Error, Is.EqualTo("expected UUID version must be known"));
        Assert.That(wrongVariant.Ok, Is.False);
        Assert.That(wrongVariant.Error, Is.EqualTo("UUID does not use RFC 9562 variant bits"));
    }
}
