using System.Reflection;

namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlSerializerRoundTripTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void DocumentSerializationReparsesToAnEquivalentTree(bool compact)
    {
        const string Input = "<!doctype html><html lang=en><head>"
            + "<title>Round &amp; trip</title></head><body>"
            + "<main data-b=2 data-a='1 &amp; 2'>"
            + "<p>Hello <em>world</em>.</p>"
            + "<template><svg viewBox='0 0 10 10'>"
            + "<circle cx=5 cy=5></circle></svg><input disabled></template>"
            + "<script>if (a < b) value++;</script></main></body></html>";
        HtmlDocument original = Html.Parse(Input);
        HtmlSerializerOptions options = compact
            ? HtmlSerializerOptions.CreateCompact()
            : HtmlSerializerOptions.CreateCanonical();

        string markup = HtmlSerializer.Serialize(original.GetRoot(), options,
            CancellationToken.None);
        HtmlDocument reparsed = Html.Parse(markup);

        AssertEquivalent(original.GetRoot(), reparsed.GetRoot(), "document");
    }

    [Test]
    public void FragmentSerializationReparsesToAnEquivalentTree()
    {
        const string Input = "<section><p>A &amp; B</p>"
            + "<math><mi>x</mi></math></section><!--end-->";
        HtmlNode original = Html.ParseFragment(Input, "div").GetFragment();

        string markup = HtmlSerializer.Serialize(original);
        HtmlNode reparsed = Html.ParseFragment(markup, "div").GetFragment();

        AssertEquivalent(original, reparsed, "fragment");
    }

    [Test]
    public void GoldenOutputsAreStableForCanonicalPrettyAndCompactModes()
    {
        string input = ReadGolden("input.html");
        HtmlNode document = Html.Parse(input).GetRoot();
        HtmlSerializerOptions[] options =
        [
            HtmlSerializerOptions.CreateCanonical(),
            HtmlSerializerOptions.CreatePretty(),
            HtmlSerializerOptions.CreateCompact()
        ];
        string[] names = ["canonical.html", "pretty.html", "compact.html"];

        Assert.Multiple(() =>
        {
            int index = 0;
            while (index < options.Length)
            {
                string first = HtmlSerializer.Serialize(document,
                    options[index], CancellationToken.None);
                string second = HtmlSerializer.Serialize(document,
                    options[index], CancellationToken.None);
                Assert.That(first, Is.EqualTo(ReadGolden(names[index])),
                    names[index]);
                Assert.That(second, Is.EqualTo(first),
                    names[index] + " repeat");
                index++;
            }
        });
    }

    [Test]
    public void StringAndTextWriterEntryPointsHaveParityAcrossModes()
    {
        HtmlNode document = Html.Parse(ReadGolden("input.html")).GetRoot();
        HtmlSerializerOptions shortest = HtmlSerializerOptions.CreateDefault();
        shortest.SetQuoteMode(HtmlSerializerQuoteMode.Shortest);
        shortest.SetAsciiOnly(true);
        shortest.SetSortAttributes(true);
        shortest.SetSelfCloseForeignElements(true);
        HtmlSerializerOptions[] options =
        [
            HtmlSerializerOptions.CreateDefault(),
            HtmlSerializerOptions.CreateCanonical(),
            HtmlSerializerOptions.CreatePreserve(),
            HtmlSerializerOptions.CreatePretty(),
            HtmlSerializerOptions.CreateCompact(),
            shortest
        ];

        Assert.Multiple(() =>
        {
            int index = 0;
            while (index < options.Length)
            {
                string outer = HtmlSerializer.Serialize(document,
                    options[index], CancellationToken.None);
                StringWriter outerWriter = new StringWriter();
                HtmlSerializer.Serialize(document, outerWriter, options[index],
                    CancellationToken.None);
                Assert.That(outerWriter.ToString(), Is.EqualTo(outer),
                    "outer mode " + index);

                string inner = HtmlSerializer.SerializeChildren(document,
                    options[index], CancellationToken.None);
                StringWriter innerWriter = new StringWriter();
                HtmlSerializer.SerializeChildren(document, innerWriter,
                    options[index], CancellationToken.None);
                Assert.That(innerWriter.ToString(), Is.EqualTo(inner),
                    "inner mode " + index);
                index++;
            }
        });
    }

    [Test]
    public void DuplicateExpandedAttributesAreRejectedBeforeWriting()
    {
        HtmlDocument document = CreateDocument();
        HtmlSourceSpan span = HtmlSourceSpan.CreateUnknown();
        HtmlAttribute first = HtmlAttribute.Create("data-value", "one", span);
        HtmlAttribute duplicate = HtmlAttribute.Create("data-value", "two",
            span);
        HtmlNode element = document.CreateElementStorage(HtmlNamespace.Html,
            "http://www.w3.org/1999/xhtml", "div", "div", "",
            new[] { first, duplicate }, span);
        StringWriter writer = new StringWriter();
        writer.Write("existing");

        HtmlSerializationException exception = Assert.Throws<
            HtmlSerializationException>(() => HtmlSerializer.Serialize(element,
                writer, HtmlSerializerOptions.CreateDefault(),
                CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Does.Contain("malformed HTML tree"));
            Assert.That(exception.Message, Does.Contain("duplicate"));
            Assert.That(writer.ToString(), Is.EqualTo("existing"));
        });
    }

    [Test]
    public void MissingTemplateContentIsRejectedBeforeWriting()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode template = document.CreateElementStorage(HtmlNamespace.Html,
            "http://www.w3.org/1999/xhtml", "template", "template", "",
            Array.Empty<HtmlAttribute>(), HtmlSourceSpan.CreateUnknown());
        StringWriter writer = new StringWriter();

        Assert.Throws<HtmlSerializationException>(() =>
            HtmlSerializer.Serialize(template, writer,
                HtmlSerializerOptions.CreateDefault(),
                CancellationToken.None));
        Assert.That(writer.ToString(), Is.Empty);
    }

    [Test]
    public void BrokenParentLinksAndCyclesAreRejectedBeforeWriting()
    {
        HtmlDocument parentDocument = CreateDocument();
        HtmlNode parent = parentDocument.CreateElement("div");
        HtmlNode child = parentDocument.CreateElement("span");
        parent.Append(child);
        SetNodeField(child, "parent", null);
        StringWriter parentWriter = new StringWriter();

        HtmlDocument cycleDocument = CreateDocument();
        HtmlNode cycle = cycleDocument.CreateElement("div");
        SetNodeField(cycle, "children", new[] { cycle });
        SetNodeField(cycle, "childCount", 1);
        SetNodeField(cycle, "firstChild", cycle);
        SetNodeField(cycle, "lastChild", cycle);
        SetNodeField(cycle, "parent", cycle);
        StringWriter cycleWriter = new StringWriter();

        Assert.Multiple(() =>
        {
            Assert.Throws<HtmlSerializationException>(() =>
                HtmlSerializer.Serialize(parent, parentWriter,
                    HtmlSerializerOptions.CreateDefault(),
                    CancellationToken.None));
            Assert.Throws<HtmlSerializationException>(() =>
                HtmlSerializer.Serialize(cycle, cycleWriter,
                    HtmlSerializerOptions.CreateDefault(),
                    CancellationToken.None));
            Assert.That(parentWriter.ToString(), Is.Empty);
            Assert.That(cycleWriter.ToString(), Is.Empty);
        });
    }

    static void AssertEquivalent(HtmlNode expected, HtmlNode actual,
        string path)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.GetKind(), Is.EqualTo(expected.GetKind()),
                path + " kind");
            Assert.That(actual.GetName(), Is.EqualTo(expected.GetName()),
                path + " name");
            Assert.That(actual.GetLocalName(),
                Is.EqualTo(expected.GetLocalName()), path + " local name");
            Assert.That(actual.GetNamespaceUri(),
                Is.EqualTo(expected.GetNamespaceUri()), path + " namespace");
            Assert.That(actual.GetPrefix(), Is.EqualTo(expected.GetPrefix()),
                path + " prefix");
            Assert.That(actual.GetData(), Is.EqualTo(expected.GetData()),
                path + " data");
            Assert.That(actual.HasDoctypeNameStorage(),
                Is.EqualTo(expected.HasDoctypeNameStorage()),
                path + " doctype name presence");
            Assert.That(actual.HasPublicIdentifierStorage(),
                Is.EqualTo(expected.HasPublicIdentifierStorage()),
                path + " public identifier presence");
            Assert.That(actual.GetPublicIdentifierStorage(),
                Is.EqualTo(expected.GetPublicIdentifierStorage()),
                path + " public identifier");
            Assert.That(actual.HasSystemIdentifierStorage(),
                Is.EqualTo(expected.HasSystemIdentifierStorage()),
                path + " system identifier presence");
            Assert.That(actual.GetSystemIdentifierStorage(),
                Is.EqualTo(expected.GetSystemIdentifierStorage()),
                path + " system identifier");
            Assert.That(actual.IsForceQuirksStorage(),
                Is.EqualTo(expected.IsForceQuirksStorage()),
                path + " force quirks");
        });

        HtmlAttribute[] expectedAttributes = expected.GetAttributes();
        HtmlAttribute[] actualAttributes = actual.GetAttributes();
        Assert.That(actualAttributes.Length,
            Is.EqualTo(expectedAttributes.Length), path + " attributes");
        int attributeIndex = 0;
        while (attributeIndex < expectedAttributes.Length)
        {
            AssertAttributeEquivalent(expectedAttributes[attributeIndex],
                actualAttributes[attributeIndex], path + " attribute "
                    + attributeIndex);
            attributeIndex++;
        }

        HtmlNode[] expectedChildren = expected.GetChildren();
        HtmlNode[] actualChildren = actual.GetChildren();
        Assert.That(actualChildren.Length, Is.EqualTo(expectedChildren.Length),
            path + " children");
        int childIndex = 0;
        while (childIndex < expectedChildren.Length)
        {
            AssertEquivalent(expectedChildren[childIndex],
                actualChildren[childIndex], path + "/" + childIndex);
            childIndex++;
        }

        HtmlNode expectedContent = expected.GetTemplateContent();
        HtmlNode actualContent = actual.GetTemplateContent();
        Assert.That(actualContent == null, Is.EqualTo(expectedContent == null),
            path + " template content presence");
        if (expectedContent != null)
        {
            AssertEquivalent(expectedContent, actualContent,
                path + "/template-content");
        }
    }

    static void AssertAttributeEquivalent(HtmlAttribute expected,
        HtmlAttribute actual, string path)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.GetName(), Is.EqualTo(expected.GetName()),
                path + " name");
            Assert.That(actual.GetLocalName(),
                Is.EqualTo(expected.GetLocalName()), path + " local name");
            Assert.That(actual.GetPrefix(), Is.EqualTo(expected.GetPrefix()),
                path + " prefix");
            Assert.That(actual.GetNamespaceUri(),
                Is.EqualTo(expected.GetNamespaceUri()), path + " namespace");
            Assert.That(actual.GetValue(), Is.EqualTo(expected.GetValue()),
                path + " value");
        });
    }

    static string ReadGolden(string name)
    {
        DirectoryInfo directory = new DirectoryInfo(
            TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            string path = Path.Combine(directory.FullName, "tests", "data",
                "serialization", name);
            if (File.Exists(path))
            {
                return File.ReadAllText(path).TrimEnd('\r', '\n');
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not find the serializer golden fixture " + name + ".");
    }

    static void SetNodeField(HtmlNode node, string name, object value)
    {
        FieldInfo field = typeof(HtmlNode).GetField(name,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, name);
        field.SetValue(node, value);
    }

    static HtmlDocument CreateDocument()
    {
        return new HtmlDocument("", "", HtmlDocumentMode.NoQuirks,
            Array.Empty<HtmlDiagnostic>(), HtmlSourceSpan.CreateUnknown());
    }
}
