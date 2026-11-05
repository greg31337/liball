using uspto;

namespace uspto.Tests;

public class CitedIdentifierNormalizerTests
{
    [Test]
    public void SourceParsedPublicationNormalizesWithoutChangingVerbatimReference()
    {
        CitedReferenceIdentifier result = CitedIdentifierNormalizer.NormalizeCitedIdentifier(
            "US 2019/0123456 A1, Smith et al.",
            "US20190123456A1",
            null,
            null,
            null,
            false,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(result.VerbatimText, Is.EqualTo("US 2019/0123456 A1, Smith et al."));
            Assert.That(result.CanonicalPatentIdentifier, Is.EqualTo("US20190123456A1"));
            Assert.That(result.PatentIdentifierKind, Is.EqualTo(IdentifierKind.PatentPublication));
            Assert.That(result.IsPatentDocument, Is.True);
            Assert.That(result.IsNonPatentLiterature, Is.False);
            Assert.That(result.IsClassified, Is.True);
        });
    }

    [Test]
    public void StructuredPublicationComponentsProduceCanonicalPatentIdentity()
    {
        CitedReferenceIdentifier result = CitedIdentifierNormalizer.NormalizeCitedIdentifier(
            "Source display citation",
            null,
            "20190123456",
            "A1",
            "US",
            false,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(result.VerbatimText, Is.EqualTo("Source display citation"));
            Assert.That(result.CanonicalPatentIdentifier, Is.EqualTo("US20190123456A1"));
            Assert.That(result.PatentIdentifierKind, Is.EqualTo(IdentifierKind.PatentPublication));
            Assert.That(result.IsPatentDocument, Is.True);
            Assert.That(result.IsClassified, Is.True);
        });
    }

    [Test]
    public void ExplicitNplStateOverridesPatentLikeTextAndStructuredFields()
    {
        CitedReferenceIdentifier result = CitedIdentifierNormalizer.NormalizeCitedIdentifier(
            "US 2019/0123456 A1 discussed in an article",
            "US20190123456A1",
            "20190123456",
            "A1",
            "US",
            true,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(result.VerbatimText, Is.EqualTo("US 2019/0123456 A1 discussed in an article"));
            Assert.That(result.CanonicalPatentIdentifier, Is.Null);
            Assert.That(result.PatentIdentifierKind, Is.EqualTo(IdentifierKind.Unknown));
            Assert.That(result.IsPatentDocument, Is.False);
            Assert.That(result.IsNonPatentLiterature, Is.True);
            Assert.That(result.IsClassified, Is.True);
        });
    }

    [TestCase("EP 1234567 A1")]
    [TestCase("WO 2024/123456")]
    [TestCase("JP-2020123456-A")]
    public void RecognizableForeignPatentReferencesAreClassifiedWithoutUsNormalization(string reference)
    {
        CitedReferenceIdentifier result = CitedIdentifierNormalizer.NormalizeCitedIdentifier(reference, null, null, null, null, false, false);

        Assert.Multiple(() =>
        {
            Assert.That(result.VerbatimText, Is.EqualTo(reference));
            Assert.That(result.CanonicalPatentIdentifier, Is.Null);
            Assert.That(result.PatentIdentifierKind, Is.EqualTo(IdentifierKind.Unknown));
            Assert.That(result.IsPatentDocument, Is.True);
            Assert.That(result.IsNonPatentLiterature, Is.False);
            Assert.That(result.IsClassified, Is.True);
        });
    }

    [TestCase("NPL Smith, Database Systems, 2019")]
    [TestCase("NPL: IEEE article 10.1000/182")]
    [TestCase("Non-patent literature by Jones")]
    public void ExplicitLiteratureMarkersClassifyNplWithoutParsingEmbeddedNumbers(string reference)
    {
        CitedReferenceIdentifier result = CitedIdentifierNormalizer.NormalizeCitedIdentifier(reference, null, null, null, null, false, false);

        Assert.Multiple(() =>
        {
            Assert.That(result.CanonicalPatentIdentifier, Is.Null);
            Assert.That(result.IsPatentDocument, Is.False);
            Assert.That(result.IsNonPatentLiterature, Is.True);
            Assert.That(result.IsClassified, Is.True);
        });
    }

    [TestCase("Smith, Database Systems, volume 12, 2019")]
    [TestCase("1234567")]
    [TestCase("AN 2011-D34107")]
    public void UnmarkedLiteratureAndAmbiguousNumbersRemainUnclassified(string reference)
    {
        CitedReferenceIdentifier result = CitedIdentifierNormalizer.NormalizeCitedIdentifier(reference, null, null, null, null, false, false);

        Assert.Multiple(() =>
        {
            Assert.That(result.VerbatimText, Is.EqualTo(reference));
            Assert.That(result.CanonicalPatentIdentifier, Is.Null);
            Assert.That(result.PatentIdentifierKind, Is.EqualTo(IdentifierKind.Unknown));
            Assert.That(result.IsPatentDocument, Is.False);
            Assert.That(result.IsNonPatentLiterature, Is.False);
            Assert.That(result.IsClassified, Is.False);
        });
    }

    [Test]
    public void ExactUsGrantCanNormalizeDirectlyWithoutSourceParsedValue()
    {
        CitedReferenceIdentifier result = CitedIdentifierNormalizer.NormalizeCitedIdentifier("US 12,156,579 B2", null, null, null, null, false, false);

        Assert.Multiple(() =>
        {
            Assert.That(result.CanonicalPatentIdentifier, Is.EqualTo("US12156579B2"));
            Assert.That(result.PatentIdentifierKind, Is.EqualTo(IdentifierKind.PatentGrant));
            Assert.That(result.IsPatentDocument, Is.True);
            Assert.That(result.IsClassified, Is.True);
        });
    }

    [Test]
    public void ExplicitNonNplStateClassifiesUnnormalizableForeignStructuredReferenceAsPatent()
    {
        CitedReferenceIdentifier result = CitedIdentifierNormalizer.NormalizeCitedIdentifier("KR 102345678 B1", null, "102345678", "B1", "KR", false, true);

        Assert.Multiple(() =>
        {
            Assert.That(result.CanonicalPatentIdentifier, Is.Null);
            Assert.That(result.IsPatentDocument, Is.True);
            Assert.That(result.IsNonPatentLiterature, Is.False);
            Assert.That(result.IsClassified, Is.True);
        });
    }
}
