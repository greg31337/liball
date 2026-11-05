
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlTreeBuilderQuirksModeTests
{
    [Test]
    public void MissingDoctypeSetsQuirksModeBeforeReprocessing()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);

        Assert.That(builder.ProcessToken(Start("html")), Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(document.GetMode(), Is.EqualTo(HtmlDocumentMode.Quirks));
            Assert.That(builder.GetInsertionMode(),
                Is.EqualTo(HtmlInsertionMode.BeforeHead));
            Assert.That(builder.GetDiagnostics(), Has.Length.EqualTo(1));
        });
    }

    [TestCase(true, "html", false, "", false, "", true)]
    [TestCase(false, "", false, "", false, "", false)]
    [TestCase(true, "svg", false, "", false, "", false)]
    [TestCase(true, "html", true, "hTmL", false, "", false)]
    [TestCase(true, "html", true,
        "-//ietf//dtd html 2.0//en", false, "", false)]
    [TestCase(true, "html", false, "", true,
        "HTTP://WWW.IBM.COM/DATA/DTD/V11/IBMXHTML1-TRANSITIONAL.DTD", false)]
    public void ForceNameAndGeneratedLegacyMatchesSetQuirksMode(
        bool hasName, string name, bool hasPublicIdentifier,
        string publicIdentifier, bool hasSystemIdentifier,
        string systemIdentifier, bool forceQuirks)
    {
        AssertMode(Doctype(hasName, name, hasPublicIdentifier,
            publicIdentifier, hasSystemIdentifier, systemIdentifier,
            forceQuirks), HtmlDocumentMode.Quirks);
    }

    [TestCase("-//W3C//DTD XHTML 1.0 Frameset//EN", false, "")]
    [TestCase("-//w3c//dtd xhtml 1.0 transitional//en", true, "")]
    [TestCase("-//W3C//DTD HTML 4.01 Frameset//EN", true, "system")]
    [TestCase("-//w3c//dtd html 4.01 transitional//en", true, "system")]
    public void GeneratedLimitedQuirksPrefixesSetLimitedQuirksMode(
        string publicIdentifier, bool hasSystemIdentifier,
        string systemIdentifier)
    {
        AssertMode(Doctype(true, "html", true, publicIdentifier,
            hasSystemIdentifier, systemIdentifier, false),
            HtmlDocumentMode.LimitedQuirks);
    }

    [Test]
    public void Html401SystemIdentifierPresenceDistinguishesAllThreeModes()
    {
        const string publicIdentifier =
            "-//W3C//DTD HTML 4.01 Transitional//EN";

        AssertMode(Doctype(true, "html", true, publicIdentifier,
            false, "", false), HtmlDocumentMode.Quirks);
        AssertMode(Doctype(true, "html", true, publicIdentifier,
            true, "", false), HtmlDocumentMode.NoQuirks);
        AssertMode(Doctype(true, "html", true, publicIdentifier,
            true, "system", false), HtmlDocumentMode.LimitedQuirks);
    }

    [Test]
    public void ConformingAndUnknownDoctypesRemainInNoQuirksMode()
    {
        AssertMode(Doctype(true, "html", false, "", false, "", false),
            HtmlDocumentMode.NoQuirks);
        AssertMode(Doctype(true, "HTML", false, "", true,
            "ABOUT:LEGACY-COMPAT", false), HtmlDocumentMode.NoQuirks);
        AssertMode(Doctype(true, "html", true, "unrecognized", false, "",
            false), HtmlDocumentMode.NoQuirks);
    }

    static void AssertMode(HtmlToken token, HtmlDocumentMode expected)
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);

        Assert.That(builder.ProcessToken(token), Is.True);
        Assert.That(document.GetMode(), Is.EqualTo(expected));
    }

    static HtmlToken Doctype(bool hasName, string name,
        bool hasPublicIdentifier, string publicIdentifier,
        bool hasSystemIdentifier, string systemIdentifier, bool forceQuirks)
    {
        return HtmlToken.CreateDoctype(hasName, name, hasPublicIdentifier,
            publicIdentifier, hasSystemIdentifier, systemIdentifier,
            forceQuirks, Span());
    }

    static HtmlToken Start(string name)
    {
        return HtmlToken.CreateStartTag(name, Array.Empty<HtmlAttribute>(),
            false, Span());
    }

    static HtmlTreeBuilder CreateBuilder(HtmlDocument document)
    {
        return new HtmlTreeBuilder(document,
            new HtmlBudget(HtmlLimits.CreateDefault(), CancellationToken.None),
            null, null);
    }

    static HtmlDocument CreateDocument()
    {
        return new HtmlDocument("", "", HtmlDocumentMode.NoQuirks,
            Array.Empty<HtmlDiagnostic>(), HtmlSourceSpan.CreateUnknown());
    }

    static HtmlSourceSpan Span()
    {
        return HtmlSourceSpan.Create(
            HtmlSourcePosition.Create(0, 1, 1),
            HtmlSourcePosition.Create(1, 1, 2));
    }
}
