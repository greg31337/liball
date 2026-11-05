using asn1;

namespace asn1.Tests;

public class OpaquePayloadPolicyTests
{
    [TestCase(AsnEncodingRules.Ber)]
    [TestCase(AsnEncodingRules.Der)]
    [TestCase(AsnEncodingRules.Cer)]
    [TestCase(AsnEncodingRules.Aper)]
    [TestCase(AsnEncodingRules.Uper)]
    [TestCase(AsnEncodingRules.CanonicalAlignedPer)]
    [TestCase(AsnEncodingRules.CanonicalUnalignedPer)]
    [TestCase(AsnEncodingRules.Oer)]
    [TestCase(AsnEncodingRules.Coer)]
    [TestCase(AsnEncodingRules.BasicXer)]
    [TestCase(AsnEncodingRules.CanonicalXer)]
    [TestCase(AsnEncodingRules.ExtendedXer)]
    [TestCase(AsnEncodingRules.Ecn)]
    public void OpenPayloadReuseRequiresExactSourceRules(
        AsnEncodingRules rules)
    {
        AsnOpenTypeValue open = Open(rules);
        AsnEncodingRules different = rules == AsnEncodingRules.Ber
            ? AsnEncodingRules.Oer
            : AsnEncodingRules.Ber;
        OpaquePayloadPolicy policy = new OpaquePayloadPolicy();

        Assert.Multiple(() =>
        {
            Assert.That(policy.CanReuseOpenType(open, rules), Is.True);
            Assert.That(policy.CanReuseOpenType(open, different), Is.False);
        });
    }

    [Test]
    public void UnknownContainersRequireRuleProvenanceAndMatchingPayloadForm()
    {
        AsnUnknownExtension binary = new AsnUnknownExtension
        {
            Index = 0,
            Present = true,
            EncodingRules = AsnEncodingRules.Uper,
            HasEncodingRules = true,
            HasBinaryData = true,
            BinaryData = [0x80]
        };
        AsnUnknownExtension missingRules = new AsnUnknownExtension
        {
            Index = 1,
            Present = true,
            EncodingRules = AsnEncodingRules.Uper,
            HasBinaryData = true,
            BinaryData = [0x80]
        };
        AsnUnknownExtension wrongForm = new AsnUnknownExtension
        {
            Index = 2,
            Present = true,
            EncodingRules = AsnEncodingRules.Uper,
            HasEncodingRules = true,
            HasTextData = true,
            TextData = "opaque"
        };
        AsnUnknownField text = new AsnUnknownField
        {
            Name = "future",
            EncodingRules = AsnEncodingRules.ExtendedXer,
            HasEncodingRules = true,
            HasTextData = true,
            TextData = "<future/>"
        };
        AsnUnknownField binaryXer = new AsnUnknownField
        {
            Name = "future",
            EncodingRules = AsnEncodingRules.ExtendedXer,
            HasEncodingRules = true,
            HasBinaryData = true,
            BinaryData = [1]
        };
        OpaquePayloadPolicy policy = new OpaquePayloadPolicy();

        Assert.Multiple(() =>
        {
            Assert.That(binary.IsValid(), Is.True);
            Assert.That(policy.CanReuseUnknownExtension(
                binary,
                AsnEncodingRules.Uper), Is.True);
            Assert.That(policy.CanReuseUnknownExtension(
                binary,
                AsnEncodingRules.Aper), Is.False);
            Assert.That(missingRules.IsValid(), Is.False);
            Assert.That(wrongForm.IsValid(), Is.False);
            Assert.That(text.IsValid(), Is.True);
            Assert.That(policy.CanReuseUnknownField(
                text,
                AsnEncodingRules.ExtendedXer), Is.True);
            Assert.That(policy.CanReuseUnknownField(
                text,
                AsnEncodingRules.BasicXer), Is.False);
            Assert.That(binaryXer.IsValid(), Is.False);
        });
    }

    [Test]
    public void RuntimeCloningRetainsOpaqueRuleProvenance()
    {
        AsnUnknownExtension extension = new AsnUnknownExtension
        {
            Index = 3,
            Present = true,
            EncodingRules = AsnEncodingRules.Coer,
            HasEncodingRules = true,
            HasBinaryData = true,
            BinaryData = [0xca, 0xfe]
        };
        AsnUnknownField field = new AsnUnknownField
        {
            Name = "future",
            EncodingRules = AsnEncodingRules.CanonicalXer,
            HasEncodingRules = true,
            HasTextData = true,
            TextData = "<future/>"
        };
        ValueAccess access = new ValueAccess();

        AsnUnknownExtension clonedExtension =
            access.CloneUnknownExtensions([extension])[0];
        AsnUnknownField clonedField =
            access.CloneUnknownFields([field])[0];

        Assert.Multiple(() =>
        {
            Assert.That(clonedExtension, Is.Not.SameAs(extension));
            Assert.That(clonedExtension.HasEncodingRules, Is.True);
            Assert.That(clonedExtension.EncodingRules,
                Is.EqualTo(AsnEncodingRules.Coer));
            Assert.That(clonedExtension.BinaryData,
                Is.EqualTo(extension.BinaryData));
            Assert.That(clonedExtension.BinaryData,
                Is.Not.SameAs(extension.BinaryData));
            Assert.That(clonedField, Is.Not.SameAs(field));
            Assert.That(clonedField.HasEncodingRules, Is.True);
            Assert.That(clonedField.EncodingRules,
                Is.EqualTo(AsnEncodingRules.CanonicalXer));
            Assert.That(clonedField.TextData, Is.EqualTo("<future/>"));
        });
    }

    [Test]
    public void EcnRejectsOpaqueBytesFromAnotherRule()
    {
        EcnPrimitiveCodecs codec = new EcnPrimitiveCodecs(
            new AsnResourceLimits());
        AsnRuntimeValue ecn = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.OpenType,
            OpenTypeValue = Open(AsnEncodingRules.Ecn)
        };
        AsnRuntimeValue ber = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.OpenType,
            OpenTypeValue = Open(AsnEncodingRules.Ber)
        };

        EcnPrimitiveResult accepted = codec.EncodePrimitive(
            EcnClassCategory.OpenType,
            [],
            ecn);
        EcnPrimitiveResult rejected = codec.EncodePrimitive(
            EcnClassCategory.OpenType,
            [],
            ber);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Success, Is.True);
            Assert.That(accepted.Data,
                Is.EqualTo(new byte[] { 0xca, 0xfe }));
            Assert.That(rejected.Success, Is.False);
            Assert.That(rejected.ErrorCode, Is.EqualTo("ASNECN0118"));
        });
    }

    [Test]
    public void ResolvedOpenValuesRemainEligibleForExplicitReencoding()
    {
        AsnOpenTypeValue resolved = new AsnOpenTypeValue
        {
            Resolved = true,
            ResolvedTypeId = "type.Payload",
            TypeName = "Schema.Payload",
            Value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Integer,
                HasIntegerValue = true,
                IntegerValue = 42
            },
            EncodingRules = AsnEncodingRules.Ber,
            HasEncodingRules = true,
            HasOriginalData = true,
            OriginalData = [0x02, 0x01, 0x2a]
        };

        Assert.Multiple(() =>
        {
            Assert.That(resolved.IsValid(), Is.True);
            Assert.That(new OpaquePayloadPolicy().CanReuseOpenType(
                resolved,
                AsnEncodingRules.Ber), Is.False);
            Assert.That(resolved.Value, Is.Not.Null);
        });
    }

    private static AsnOpenTypeValue Open(AsnEncodingRules rules)
    {
        return new AsnOpenTypeValue
        {
            Unresolved = true,
            EncodingRules = rules,
            HasEncodingRules = true,
            HasOriginalData = true,
            OriginalData = [0xca, 0xfe]
        };
    }
}
