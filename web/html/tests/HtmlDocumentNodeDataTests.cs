using System.Text;

namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlDocumentNodeDataTests
{
    [Test]
    public void ByteParseCopiesResponseAndEncodingMetadataToDocument()
    {
        HtmlParseOptions options = new HtmlParseOptions();
        options.SetResponseUrl("https://response.example/page");
        options.SetBaseUrl("https://base.example/assets/");
        options.SetContentLanguage("en-US");
        options.SetTransportEncodingLabel("utf-8");

        HtmlParseResult result = new HtmlApi().ParseBytesWithOptions(
            Encoding.UTF8.GetBytes("<html lang=fr dir=rtl><body><p>x</p>"),
            options, CancellationToken.None);
        IHtmlDocument document = result.GetDocument();
        Encoding firstEncoding = document.GetDetectedEncoding();
        Encoding secondEncoding = document.GetDetectedEncoding();
        HtmlNode clonedRoot = document.GetRoot().Clone(true);
        HtmlDocument clonedDocument = clonedRoot.QuerySelector("p")
            .GetOwnerDocument();

        Assert.Multiple(() =>
        {
            Assert.That(document.GetResponseUrl(),
                Is.EqualTo("https://response.example/page"));
            Assert.That(document.GetUrl(),
                Is.EqualTo("https://response.example/page"));
            Assert.That(document.GetBaseUrl(),
                Is.EqualTo("https://base.example/assets/"));
            Assert.That(document.GetContentLanguage(), Is.EqualTo("en-US"));
            Assert.That(document.GetLanguage(), Is.EqualTo("fr"));
            Assert.That(document.GetDirection(), Is.EqualTo("rtl"));
            Assert.That(document.GetDetectedEncodingLabel(),
                Is.EqualTo("UTF-8"));
            Assert.That(firstEncoding.WebName, Is.EqualTo("utf-8"));
            Assert.That(secondEncoding, Is.Not.SameAs(firstEncoding));
            Assert.That(document.GetEncodingDetection().GetSource(),
                Is.EqualTo(HtmlEncodingSource.Transport));
            Assert.That(clonedDocument.GetContentLanguage(),
                Is.EqualTo("en-US"));
            Assert.That(clonedDocument.GetDetectedEncodingLabel(),
                Is.EqualTo("UTF-8"));
        });
    }

    [Test]
    public void NodesExposeInheritedLanguageDirectionAndMutationVersion()
    {
        HtmlParseOptions options = new HtmlParseOptions();
        options.SetContentLanguage("en-GB");
        HtmlDocument document = new HtmlApi().ParseStringWithOptions(
            "<main dir=rtl><p>text</p></main>", options,
            CancellationToken.None).GetDocument();
        HtmlNode paragraph = document.QuerySelector("p");
        long initialVersion = document.GetMutationVersion();

        Assert.Multiple(() =>
        {
            Assert.That(document.GetLanguage(), Is.EqualTo("en-GB"));
            Assert.That(paragraph.GetLanguage(), Is.EqualTo("en-GB"));
            Assert.That(paragraph.GetDirection(), Is.EqualTo("rtl"));
            Assert.That(paragraph.GetMutationVersion(),
                Is.EqualTo(initialVersion));
        });

        paragraph.SetAttribute("lang", "de");
        long changedVersion = document.GetMutationVersion();

        Assert.Multiple(() =>
        {
            Assert.That(changedVersion, Is.GreaterThan(initialVersion));
            Assert.That(paragraph.GetMutationVersion(),
                Is.EqualTo(changedVersion));
            Assert.That(paragraph.GetLanguage(), Is.EqualTo("de"));
        });
    }

    [Test]
    public void SourceProvenanceIsIndependentOfTrackedCoordinates()
    {
        HtmlParseOptions options = new HtmlParseOptions();
        options.SetTrackSourceLocations(false);
        HtmlDocument document = new HtmlApi().ParseStringWithOptions(
            "<!doctype html><p>x</p>", options,
            CancellationToken.None).GetDocument();
        HtmlNode doctype = document.GetDocumentType();
        HtmlNode paragraph = document.QuerySelector("p");
        HtmlNode generatedHtml = document.GetDocumentElement();

        Assert.Multiple(() =>
        {
            Assert.That(document.GetRoot().IsSourceBacked(), Is.True);
            Assert.That(document.GetRoot().HasSourceLocation(), Is.False);
            Assert.That(doctype.IsSourceBacked(), Is.True);
            Assert.That(doctype.HasSourceLocation(), Is.False);
            Assert.That(paragraph.IsSourceBacked(), Is.True);
            Assert.That(paragraph.IsGenerated(), Is.False);
            Assert.That(paragraph.HasSourceLocation(), Is.False);
            Assert.That(paragraph.Clone(true).IsSourceBacked(), Is.True);
            Assert.That(generatedHtml.IsGenerated(), Is.True);
            Assert.That(generatedHtml.IsSourceBacked(), Is.False);
        });
    }

    [Test]
    public void DocumentTypeExposesPresenceSensitiveData()
    {
        HtmlDocument document = new HtmlApi().ParseString(
            "<!DOCTYPE html PUBLIC \"public-value\" \"system-value\"><p>x");
        IHtmlNode doctype = document.GetDocumentType();
        HtmlNode absent = document.CreateDocumentType(null, null, null, true);

        Assert.Multiple(() =>
        {
            Assert.That(doctype.HasDoctypeName(), Is.True);
            Assert.That(doctype.GetDoctypeName(), Is.EqualTo("html"));
            Assert.That(doctype.HasPublicIdentifier(), Is.True);
            Assert.That(doctype.GetPublicIdentifier(),
                Is.EqualTo("public-value"));
            Assert.That(doctype.HasSystemIdentifier(), Is.True);
            Assert.That(doctype.GetSystemIdentifier(),
                Is.EqualTo("system-value"));
            Assert.That(doctype.IsForceQuirks(), Is.False);
            Assert.That(absent.HasDoctypeName(), Is.False);
            Assert.That(absent.GetDoctypeName(), Is.Empty);
            Assert.That(absent.HasPublicIdentifier(), Is.False);
            Assert.That(absent.HasSystemIdentifier(), Is.False);
            Assert.That(absent.IsForceQuirks(), Is.True);
        });
    }

    [Test]
    public void FactoryCreatesEverySupportedNodeKind()
    {
        IHtmlFactory factory = new HtmlFactory();
        HtmlSourcePosition start = factory.CreateSourcePosition(0, 1, 1);
        HtmlSourcePosition end = factory.CreateSourcePosition(4, 1, 5);
        HtmlSourceSpan span = factory.CreateSourceSpan(start, end);
        HtmlDocument document = factory.CreateDocument(
            "https://example.test/", "https://example.test/", "en", span);
        HtmlNode doctype = factory.CreateDocumentTypeNode(document, "html",
            null, null, false, span);
        HtmlNode element = factory.CreateElementNode(document,
            "http://www.w3.org/1999/xhtml", "DIV", span);
        HtmlNode text = factory.CreateTextNode(document, "text", span);
        HtmlNode comment = factory.CreateCommentNode(document, "comment", span);
        HtmlNode fragment = factory.CreateFragmentNode(document, span);

        Assert.Multiple(() =>
        {
            Assert.That(document.GetRoot().GetKind(),
                Is.EqualTo(HtmlNodeKind.Document));
            Assert.That(doctype.GetKind(),
                Is.EqualTo(HtmlNodeKind.DocumentType));
            Assert.That(element.GetKind(), Is.EqualTo(HtmlNodeKind.Element));
            Assert.That(element.GetName(), Is.EqualTo("div"));
            Assert.That(text.GetKind(), Is.EqualTo(HtmlNodeKind.Text));
            Assert.That(comment.GetKind(), Is.EqualTo(HtmlNodeKind.Comment));
            Assert.That(fragment.GetKind(), Is.EqualTo(HtmlNodeKind.Fragment));
            Assert.That(element.GetOwnerDocument(), Is.SameAs(document));
            Assert.That(document.GetRoot().IsSourceBacked(), Is.True);
            Assert.That(element.IsSourceBacked(), Is.True);
            Assert.That(element.HasSourceLocation(), Is.True);
        });
    }
}
