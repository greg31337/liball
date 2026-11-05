using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownUriPolicyTests
{
    [Test]
    public void Classify_RecognizesEmptyFragmentRelativeAndMailto()
    {
        AssertClassification("", MarkdownUriKind.Empty, "", true, true);
        AssertClassification("#section", MarkdownUriKind.Fragment, "", true, true);
        AssertClassification("docs/page.md", MarkdownUriKind.Relative, "", true, true);
        AssertClassification("/docs/page.md", MarkdownUriKind.Relative, "", true, true);
        AssertClassification("mailto:user@example.com", MarkdownUriKind.Mailto, "mailto", true, true);
    }

    [Test]
    public void Classify_RecognizesHttpAndHttpsAsSafeAbsoluteLinks()
    {
        AssertClassification("http://example.com", MarkdownUriKind.Absolute, "http", true, true);
        AssertClassification("HTTPS://example.com", MarkdownUriKind.Absolute, "https", true, true);
    }

    [Test]
    public void Classify_MarksUnknownAbsoluteSchemesUnsafeForLinks()
    {
        AssertClassification("ftp://example.com/file", MarkdownUriKind.Absolute, "ftp", false, false);
        AssertClassification("custom:thing", MarkdownUriKind.Absolute, "custom", false, false);
    }

    [Test]
    public void Classify_MarksDangerousSchemesUnsafe()
    {
        AssertClassification("javascript:alert(1)", MarkdownUriKind.Unsafe, "javascript", false, false);
        AssertClassification("vbscript:alert(1)", MarkdownUriKind.Unsafe, "vbscript", false, false);
        AssertClassification("file:///etc/passwd", MarkdownUriKind.Unsafe, "file", false, false);
    }

    [Test]
    public void Classify_TrimsBoundaryWhitespaceAndControlsBeforeCheckingScheme()
    {
        MarkdownUriClassification classification = MarkdownUriPolicy.Classify(" \t\njavascript:alert(1)\r ");

        Assert.That(classification.OriginalText, Is.EqualTo(" \t\njavascript:alert(1)\r "));
        Assert.That(classification.NormalizedText, Is.EqualTo("javascript:alert(1)"));
        Assert.That(classification.Scheme, Is.EqualTo("javascript"));
        Assert.That(classification.Kind, Is.EqualTo(MarkdownUriKind.Unsafe));
        Assert.That(classification.SafeForLink, Is.False);
    }

    [Test]
    public void Classify_DoesNotTreatColonAfterSlashQuestionOrHashAsScheme()
    {
        AssertClassification("docs/a:b", MarkdownUriKind.Relative, "", true, true);
        AssertClassification("?q=a:b", MarkdownUriKind.Relative, "", true, true);
        AssertClassification("#a:b", MarkdownUriKind.Fragment, "", true, true);
    }

    [Test]
    public void DataUris_AreUnsafeForLinksButSelectedImageTypesAreSafeForImages()
    {
        MarkdownUriClassification png = MarkdownUriPolicy.Classify("data:image/png;base64,abc");
        MarkdownUriClassification svg = MarkdownUriPolicy.Classify("data:image/svg+xml;base64,abc");
        MarkdownUriClassification text = MarkdownUriPolicy.Classify("data:text/html,abc");

        Assert.That(png.Kind, Is.EqualTo(MarkdownUriKind.Data));
        Assert.That(png.SafeForLink, Is.False);
        Assert.That(png.SafeForImage, Is.True);

        Assert.That(svg.SafeForLink, Is.False);
        Assert.That(svg.SafeForImage, Is.False);
        Assert.That(text.SafeForImage, Is.False);
    }

    [Test]
    public void SafeConvenienceMethods_ReturnExpectedPolicyDecisions()
    {
        Assert.That(MarkdownUriPolicy.IsSafeLink("https://example.com"), Is.True);
        Assert.That(MarkdownUriPolicy.IsSafeLink("data:image/png;base64,abc"), Is.False);
        Assert.That(MarkdownUriPolicy.IsSafeImage("data:image/png;base64,abc"), Is.True);
        Assert.That(MarkdownUriPolicy.IsSafeImage("javascript:alert(1)"), Is.False);
    }

    [Test]
    public void SchemeHelpers_ParseValidSchemes()
    {
        Assert.That(MarkdownUriPolicy.GetScheme("a+b.c-1:value"), Is.EqualTo("a+b.c-1"));
        Assert.That(MarkdownUriPolicy.GetSchemeEnd("a+b.c-1:value"), Is.EqualTo(7));
        Assert.That(MarkdownUriPolicy.GetScheme("1bad:value"), Is.EqualTo(""));
        Assert.That(MarkdownUriPolicy.GetScheme("bad value:"), Is.EqualTo(""));
        Assert.That(MarkdownUriPolicy.IsSchemeCharacter('+'), Is.True);
        Assert.That(MarkdownUriPolicy.IsSchemeCharacter('_'), Is.False);
    }

    [Test]
    public void UriPolicy_RejectsNullArguments()
    {
        Assert.That(() => MarkdownUriPolicy.Classify(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownUriPolicy.IsSafeLink(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownUriPolicy.IsSafeImage(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownUriPolicy.TrimForPolicy(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownUriPolicy.GetScheme(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownUriPolicy.GetSchemeEnd(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownUriPolicy.IsKindSafeForLink(MarkdownUriKind.Absolute, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownUriPolicy.IsDangerousScheme(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownUriPolicy.IsSafeDataImageUri(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownUriPolicy.StringEqualsIgnoreCase(null!, ""), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownUriPolicy.StartsWithIgnoreCase(null!, ""), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownUriClassification.Create(null!, "", "", MarkdownUriKind.Empty, true, true), Throws.TypeOf<ArgumentNullException>());
    }

    static void AssertClassification(string uri, MarkdownUriKind kind, string scheme, bool safeForLink, bool safeForImage)
    {
        MarkdownUriClassification classification = MarkdownUriPolicy.Classify(uri);

        Assert.That(classification.OriginalText, Is.EqualTo(uri));
        Assert.That(classification.NormalizedText, Is.EqualTo(MarkdownUriPolicy.TrimForPolicy(uri)));
        Assert.That(classification.Kind, Is.EqualTo(kind));
        Assert.That(classification.Scheme, Is.EqualTo(scheme));
        Assert.That(classification.SafeForLink, Is.EqualTo(safeForLink));
        Assert.That(classification.SafeForImage, Is.EqualTo(safeForImage));
    }
}
