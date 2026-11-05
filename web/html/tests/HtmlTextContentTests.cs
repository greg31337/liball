
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlTextContentTests
{
    [Test]
    public void RawOwnAndDescendantTextRespectNodeKindsAndTreeOrder()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode root = document.CreateElement("main");
        HtmlNode first = document.CreateText("alpha");
        HtmlNode comment = document.CreateComment("ignored");
        HtmlNode nested = document.CreateElement("span");
        HtmlNode second = document.CreateText(" beta");
        HtmlNode nestedComment = document.CreateComment("also ignored");
        nested.InitializeChildrenStorage(new[] { second, nestedComment });
        root.InitializeChildrenStorage(new[] { first, comment, nested });
        document.GetRoot().InitializeChildrenStorage(new[] { root });
        HtmlNode doctype = document.CreateDocumentTypeStorage(true, "html",
            false, "", false, "", false, HtmlSourceSpan.CreateUnknown());

        Assert.Multiple(() =>
        {
            Assert.That(first.GetData(), Is.EqualTo("alpha"));
            Assert.That(first.GetTextContent(), Is.EqualTo("alpha"));
            Assert.That(first.GetOwnText(), Is.EqualTo("alpha"));
            Assert.That(comment.GetData(), Is.EqualTo("ignored"));
            Assert.That(comment.GetTextContent(), Is.EqualTo("ignored"));
            Assert.That(comment.GetOwnText(), Is.Empty);
            Assert.That(root.GetOwnText(), Is.EqualTo("alpha"));
            Assert.That(root.GetTextContent(), Is.EqualTo("alpha beta"));
            Assert.That(document.GetRoot().GetTextContent(), Is.Empty);
            Assert.That(doctype.GetTextContent(), Is.Empty);
            Assert.That(doctype.GetOwnText(), Is.Empty);
        });
    }

    [Test]
    public void FragmentAndTemplateTextUseTheirDescendantAndContentTrees()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode fragment = document.CreateFragment();
        HtmlNode wrapper = document.CreateElement("div");
        wrapper.InitializeChildrenStorage(new[]
        {
            document.CreateText("nested")
        });
        fragment.InitializeChildrenStorage(new[]
        {
            document.CreateText("direct "), wrapper
        });
        HtmlNode template = document.CreateElement("template");
        template.GetTemplateContent().InitializeChildrenStorage(new[]
        {
            document.CreateText("template "),
            document.CreateElement("em")
        });
        template.GetTemplateContent().GetLastChild().InitializeChildrenStorage(
            new[] { document.CreateText("content") });
        HtmlNode host = document.CreateElement("section");
        host.InitializeChildrenStorage(new[]
        {
            document.CreateText("before "), template,
            document.CreateText(" after")
        });

        Assert.Multiple(() =>
        {
            Assert.That(fragment.GetOwnText(), Is.EqualTo("direct "));
            Assert.That(fragment.GetTextContent(), Is.EqualTo("direct nested"));
            Assert.That(template.GetOwnText(), Is.EqualTo("template "));
            Assert.That(template.GetTextContent(),
                Is.EqualTo("template content"));
            Assert.That(host.GetTextContent(),
                Is.EqualTo("before template content after"));
        });
    }

    [Test]
    public void NormalizedTextCollapsesUnicodeWhitespaceAndPreservesOtherData()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode root = document.CreateElement("p");
        HtmlNode nested = document.CreateElement("span");
        nested.InitializeChildrenStorage(new[]
        {
            document.CreateText("\u2003beta\r\n gamma\u200Bdelta\u3000")
        });
        root.InitializeChildrenStorage(new[]
        {
            document.CreateText("\t alpha\u00A0"), nested
        });

        Assert.Multiple(() =>
        {
            Assert.That(root.GetTextContent(),
                Is.EqualTo("\t alpha\u00A0\u2003beta\r\n gamma\u200Bdelta\u3000"));
            Assert.That(root.GetNormalizedTextContent(),
                Is.EqualTo("alpha beta gamma\u200Bdelta"));
            Assert.That(document.CreateText(" \u0085value\u2029 ")
                .GetNormalizedTextContent(), Is.EqualTo("value"));
            Assert.That(document.CreateComment(" a\tb ")
                .GetNormalizedTextContent(), Is.EqualTo("a b"));
        });
    }

    [Test]
    public void TextCachesReuseValuesUntilTheDocumentVersionChanges()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode root = document.CreateElement("div");
        HtmlNode first = document.CreateText(new string('a', 24));
        HtmlNode second = document.CreateText(new string('b', 24));
        root.InitializeChildrenStorage(new[] { first, second });

        string descendant = root.GetTextContent();
        string own = root.GetOwnText();
        string normalized = root.GetNormalizedTextContent();

        Assert.Multiple(() =>
        {
            Assert.That(root.GetTextContent(), Is.SameAs(descendant));
            Assert.That(root.GetOwnText(), Is.SameAs(own));
            Assert.That(root.GetNormalizedTextContent(), Is.SameAs(normalized));
            Assert.That(document.GetMutationVersionStorage(), Is.Zero);
        });

        HtmlNode unrelated = document.CreateElement("aside");
        unrelated.SetAttribute("id", "changed");

        string newDescendant = root.GetTextContent();
        string newOwn = root.GetOwnText();
        string newNormalized = root.GetNormalizedTextContent();
        Assert.Multiple(() =>
        {
            Assert.That(newDescendant, Is.EqualTo(descendant));
            Assert.That(newOwn, Is.EqualTo(own));
            Assert.That(newNormalized, Is.EqualTo(normalized));
            Assert.That(newDescendant, Is.Not.SameAs(descendant));
            Assert.That(newOwn, Is.Not.SameAs(own));
            Assert.That(newNormalized, Is.Not.SameAs(normalized));
            Assert.That(document.GetMutationVersionStorage(), Is.EqualTo(1));
        });

        first.SetData("changed ");
        Assert.Multiple(() =>
        {
            Assert.That(root.GetTextContent(),
                Is.EqualTo("changed " + new string('b', 24)));
            Assert.That(root.GetOwnText(),
                Is.EqualTo("changed " + new string('b', 24)));
            Assert.That(root.GetNormalizedTextContent(),
                Is.EqualTo("changed " + new string('b', 24)));
            Assert.That(document.GetMutationVersionStorage(), Is.EqualTo(2));
        });
    }

    [Test]
    public void StorageInitializationInvalidatesZeroVersionAncestorCaches()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode root = document.CreateElement("div");
        HtmlNode template = document.CreateElement("template");
        root.InitializeChildrenStorage(new[] { template });

        Assert.That(root.GetTextContent(), Is.Empty);
        Assert.That(template.GetTextContent(), Is.Empty);

        template.GetTemplateContent().InitializeChildrenStorage(new[]
        {
            document.CreateText("late")
        });

        Assert.Multiple(() =>
        {
            Assert.That(template.GetTextContent(), Is.EqualTo("late"));
            Assert.That(root.GetTextContent(), Is.EqualTo("late"));
            Assert.That(document.GetMutationVersionStorage(), Is.Zero);
        });
    }

    static HtmlDocument CreateDocument()
    {
        return new HtmlDocument("", "", HtmlDocumentMode.NoQuirks,
            Array.Empty<HtmlDiagnostic>(), HtmlSourceSpan.CreateUnknown());
    }
}
