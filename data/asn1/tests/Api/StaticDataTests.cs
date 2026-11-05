using asn1;

namespace asn1.Tests;

public class StaticDataTests
{
    [Test]
    public void StandardEditionIdentifiersMatchTheSupportedCorpus()
    {
        Assert.That(AsnStandardEditions.X680, Is.EqualTo("2021-02"));
        Assert.That(AsnStandardEditions.X681, Is.EqualTo("2021-02"));
        Assert.That(AsnStandardEditions.X682, Is.EqualTo("2021-02"));
        Assert.That(AsnStandardEditions.X683, Is.EqualTo("2021-02"));
        Assert.That(AsnStandardEditions.X690, Is.EqualTo("2021-02"));
        Assert.That(AsnStandardEditions.X690Erratum1, Is.EqualTo("2021-09"));
        Assert.That(AsnStandardEditions.X691, Is.EqualTo("2021-02"));
        Assert.That(AsnStandardEditions.X692, Is.EqualTo("2021-02"));
        Assert.That(AsnStandardEditions.X693, Is.EqualTo("2021-02"));
        Assert.That(AsnStandardEditions.X694, Is.EqualTo("2021-02"));
        Assert.That(AsnStandardEditions.X695, Is.EqualTo("2021-02"));
        Assert.That(AsnStandardEditions.X696, Is.EqualTo("2021-02"));
    }

    [Test]
    public void UniversalTagNumbersMatchTheX680Assignments()
    {
        Assert.That(AsnUniversalTags.ReservedForEncodingRules, Is.Zero);
        Assert.That(AsnUniversalTags.Boolean, Is.EqualTo(1));
        Assert.That(AsnUniversalTags.Integer, Is.EqualTo(2));
        Assert.That(AsnUniversalTags.BitString, Is.EqualTo(3));
        Assert.That(AsnUniversalTags.OctetString, Is.EqualTo(4));
        Assert.That(AsnUniversalTags.Null, Is.EqualTo(5));
        Assert.That(AsnUniversalTags.ObjectIdentifier, Is.EqualTo(6));
        Assert.That(AsnUniversalTags.ObjectDescriptor, Is.EqualTo(7));
        Assert.That(AsnUniversalTags.External, Is.EqualTo(8));
        Assert.That(AsnUniversalTags.InstanceOf, Is.EqualTo(8));
        Assert.That(AsnUniversalTags.Real, Is.EqualTo(9));
        Assert.That(AsnUniversalTags.Enumerated, Is.EqualTo(10));
        Assert.That(AsnUniversalTags.EmbeddedPdv, Is.EqualTo(11));
        Assert.That(AsnUniversalTags.Utf8String, Is.EqualTo(12));
        Assert.That(AsnUniversalTags.RelativeObjectIdentifier, Is.EqualTo(13));
        Assert.That(AsnUniversalTags.Time, Is.EqualTo(14));
        Assert.That(AsnUniversalTags.Reserved, Is.EqualTo(15));
        Assert.That(AsnUniversalTags.Sequence, Is.EqualTo(16));
        Assert.That(AsnUniversalTags.SequenceOf, Is.EqualTo(16));
        Assert.That(AsnUniversalTags.Set, Is.EqualTo(17));
        Assert.That(AsnUniversalTags.SetOf, Is.EqualTo(17));
        Assert.That(AsnUniversalTags.NumericString, Is.EqualTo(18));
        Assert.That(AsnUniversalTags.PrintableString, Is.EqualTo(19));
        Assert.That(AsnUniversalTags.TeletexString, Is.EqualTo(20));
        Assert.That(AsnUniversalTags.T61String, Is.EqualTo(20));
        Assert.That(AsnUniversalTags.VideotexString, Is.EqualTo(21));
        Assert.That(AsnUniversalTags.Ia5String, Is.EqualTo(22));
        Assert.That(AsnUniversalTags.UtcTime, Is.EqualTo(23));
        Assert.That(AsnUniversalTags.GeneralizedTime, Is.EqualTo(24));
        Assert.That(AsnUniversalTags.GraphicString, Is.EqualTo(25));
        Assert.That(AsnUniversalTags.VisibleString, Is.EqualTo(26));
        Assert.That(AsnUniversalTags.Iso646String, Is.EqualTo(26));
        Assert.That(AsnUniversalTags.GeneralString, Is.EqualTo(27));
        Assert.That(AsnUniversalTags.UniversalString, Is.EqualTo(28));
        Assert.That(AsnUniversalTags.CharacterString, Is.EqualTo(29));
        Assert.That(AsnUniversalTags.BmpString, Is.EqualTo(30));
        Assert.That(AsnUniversalTags.Date, Is.EqualTo(31));
        Assert.That(AsnUniversalTags.TimeOfDay, Is.EqualTo(32));
        Assert.That(AsnUniversalTags.DateTime, Is.EqualTo(33));
        Assert.That(AsnUniversalTags.Duration, Is.EqualTo(34));
        Assert.That(AsnUniversalTags.ObjectIdentifierIri, Is.EqualTo(35));
        Assert.That(AsnUniversalTags.RelativeObjectIdentifierIri, Is.EqualTo(36));
    }

    [Test]
    public void EncodingObjectIdentifiersMatchTheStandardAssignments()
    {
        AssertIdentifierPair(AsnEncodingObjectIdentifiers.BerOid, AsnEncodingObjectIdentifiers.BerIri, "2.1.1", "/Joint-ISO-ITU-T/ASN.1/Basic-Encoding");
        AssertIdentifierPair(AsnEncodingObjectIdentifiers.CerOid, AsnEncodingObjectIdentifiers.CerIri, "2.1.2.0", "/Joint-ISO-ITU-T/ASN.1/BER-Derived/Canonical-Encoding");
        AssertIdentifierPair(AsnEncodingObjectIdentifiers.DerOid, AsnEncodingObjectIdentifiers.DerIri, "2.1.2.1", "/Joint-ISO-ITU-T/ASN.1/BER-Derived/Distinguished-Encoding");
        AssertIdentifierPair(AsnEncodingObjectIdentifiers.AperOid, AsnEncodingObjectIdentifiers.AperIri, "2.1.3.0.0", "/Joint-ISO-ITU-T/ASN.1/Packed-Encoding/Basic/Aligned");
        AssertIdentifierPair(AsnEncodingObjectIdentifiers.UperOid, AsnEncodingObjectIdentifiers.UperIri, "2.1.3.0.1", "/Joint-ISO-ITU-T/ASN.1/Packed-Encoding/Basic/Unaligned");
        AssertIdentifierPair(AsnEncodingObjectIdentifiers.CanonicalAlignedPerOid, AsnEncodingObjectIdentifiers.CanonicalAlignedPerIri, "2.1.3.1.0", "/Joint-ISO-ITU-T/ASN.1/Packed-Encoding/Canonical/Aligned");
        AssertIdentifierPair(AsnEncodingObjectIdentifiers.CanonicalUnalignedPerOid, AsnEncodingObjectIdentifiers.CanonicalUnalignedPerIri, "2.1.3.1.1", "/Joint-ISO-ITU-T/ASN.1/Packed-Encoding/Canonical/Unaligned");
        AssertIdentifierPair(AsnEncodingObjectIdentifiers.BasicXerOid, AsnEncodingObjectIdentifiers.BasicXerIri, "2.1.5.0", "/Joint-ISO-ITU-T/ASN.1/XML-Encoding/Basic");
        AssertIdentifierPair(AsnEncodingObjectIdentifiers.CanonicalXerOid, AsnEncodingObjectIdentifiers.CanonicalXerIri, "2.1.5.1", "/Joint-ISO-ITU-T/ASN.1/XML-Encoding/Canonical");
        AssertIdentifierPair(AsnEncodingObjectIdentifiers.ExtendedXerOid, AsnEncodingObjectIdentifiers.ExtendedXerIri, "2.1.5.2", "/Joint-ISO-ITU-T/ASN.1/XML-Encoding/Extended");
        AssertIdentifierPair(AsnEncodingObjectIdentifiers.OerOid, AsnEncodingObjectIdentifiers.OerIri, "2.1.6.0", "/Joint-ISO-ITU-T/ASN.1/OER-Encoding/Basic");
        AssertIdentifierPair(AsnEncodingObjectIdentifiers.CoerOid, AsnEncodingObjectIdentifiers.CoerIri, "2.1.6.1", "/Joint-ISO-ITU-T/ASN.1/OER-Encoding/Canonical");
    }

    private static void AssertIdentifierPair(string actualOid, string actualIri, string expectedOid, string expectedIri)
    {
        Assert.That(actualOid, Is.EqualTo(expectedOid));
        Assert.That(actualIri, Is.EqualTo(expectedIri));
    }
}
