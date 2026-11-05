namespace svg.Tests;

public class SvgRoundTripFixtureTests
{
    static IEnumerable<TestCaseData> RoundTripFixtures()
    {
        yield return new TestCaseData(
            "<?xml version = \"1.0\"?>\r\n<!-- before <&> --><?edit keep='yes'?>" +
            "<svg xmlns='http://www.w3.org/2000/svg'><![CDATA[a < b && c > d\r\nnext]]><!--inside--><?work data?></svg>\r\n")
            .SetName("mixed-content-and-line-endings");
        yield return new TestCaseData(
            "<s:svg xmlns:s='http://www.w3.org/2000/svg' xmlns:v=\"urn:vendor\" id='root' v:mode=\"raw\">" +
            "<v:widget second='2' first=\"1\"><v:child/></v:widget><s:g/></s:svg>")
            .SetName("prefixes-vendor-content-and-attribute-order");
        yield return new TestCaseData(
            "<svg xmlns='http://www.w3.org/2000/svg' d='M0 0 X ???' style=\"fill:calc(??); custom:raw\" " +
            "data-raw='  a&#xA;\r\n\t&amp;&quot;  '>a&amp;&#65;<![CDATA[<raw>]]></svg>")
            .SetName("recoverable-raw-svg-css-and-reference-values");
        yield return new TestCaseData(
            "<!DOCTYPE svg [<!ELEMENT svg ANY><!ATTLIST svg vendor CDATA #IMPLIED>]>" +
            "<svg vendor='preserved'><unknown/></svg>")
            .SetName("preserve-only-doctype-and-unknown-element");
    }

    [TestCaseSource(nameof(RoundTripFixtures))]
    public void UntouchedTokenStreamsReconstructFixtureSourceExactly(string input)
    {
        SvgLoadOptions options = new SvgLoadOptions();
        if (input.StartsWith("<!DOCTYPE", StringComparison.Ordinal)) options.DtdPolicy = SvgDtdPolicy.PreserveOnly;

        SvgLoadResult loaded = new SvgLoader().LoadWithStringAndSvgLoadOptions(input, options);
        int nextOffset = 0;

        Assert.That(loaded.Success, Is.True);
        foreach (SvgXmlToken token in loaded.XmlTokens)
        {
            Assert.Multiple(() =>
            {
                Assert.That(token.StartOffset, Is.EqualTo(nextOffset));
                Assert.That(token.EndOffset, Is.EqualTo(token.StartOffset + token.RawText.Length));
                Assert.That(input.Substring(token.StartOffset, token.RawText.Length), Is.EqualTo(token.RawText));
            });
            nextOffset = token.EndOffset;
        }

        Assert.Multiple(() =>
        {
            Assert.That(nextOffset, Is.EqualTo(input.Length));
            Assert.That(string.Concat(loaded.XmlTokens.Select(token => token.RawText)), Is.EqualTo(input));
            Assert.That(loaded.Document.SourceText, Is.SameAs(input));
            Assert.That(loaded.Document.SourceSpan.StartOffset, Is.Zero);
            Assert.That(loaded.Document.SourceSpan.EndOffset, Is.EqualTo(input.Length));
        });
    }

    [Test]
    public void CommentsCDataAndInstructionsRetainRawAndNormalizedRepresentations()
    {
        string input = "<!--a\r\nb--><?edit a\r\nb?><svg><![CDATA[x\r\ny]]><!--tail--><?work keep?></svg>";
        SvgDocument document = Load(input);
        SvgNode before = document.DocumentNode.Children[0];
        SvgNode edit = document.DocumentNode.Children[1];
        SvgNode root = document.DocumentElement;
        SvgNode cdata = root.Children[0];
        SvgNode tail = root.Children[1];
        SvgNode work = root.Children[2];

        Assert.Multiple(() =>
        {
            Assert.That(before.Kind, Is.EqualTo(SvgNodeKind.Comment));
            Assert.That(before.Value, Is.EqualTo("a\nb"));
            Assert.That(before.RawToken.RawText, Is.EqualTo("<!--a\r\nb-->"));
            Assert.That(edit.Kind, Is.EqualTo(SvgNodeKind.ProcessingInstruction));
            Assert.That(edit.QualifiedName, Is.EqualTo("edit"));
            Assert.That(edit.Value, Is.EqualTo("a\nb"));
            Assert.That(edit.RawToken.RawText, Is.EqualTo("<?edit a\r\nb?>"));
            Assert.That(cdata.Kind, Is.EqualTo(SvgNodeKind.CData));
            Assert.That(cdata.Value, Is.EqualTo("x\ny"));
            Assert.That(cdata.RawToken.RawText, Is.EqualTo("<![CDATA[x\r\ny]]>"));
            Assert.That(tail.Kind, Is.EqualTo(SvgNodeKind.Comment));
            Assert.That(tail.RawToken.RawText, Is.EqualTo("<!--tail-->"));
            Assert.That(work.Kind, Is.EqualTo(SvgNodeKind.ProcessingInstruction));
            Assert.That(work.RawToken.RawText, Is.EqualTo("<?work keep?>"));
        });
    }

    [Test]
    public void PrefixesUnknownNodesAndAttributeOrderRemainRepresentable()
    {
        string input = "<s:svg xmlns:s='http://www.w3.org/2000/svg' xmlns:v='urn:vendor' z='last' a='first'>" +
            "<v:widget v:second='2' plain='middle' v:first='1'><v:child/></v:widget></s:svg>";
        SvgDocument document = Load(input);
        SvgNode root = document.DocumentElement;
        SvgNode widget = root.Children[0];
        SvgNode child = widget.Children[0];

        Assert.Multiple(() =>
        {
            Assert.That(root.QualifiedName, Is.EqualTo("s:svg"));
            Assert.That(root.Prefix, Is.EqualTo("s"));
            Assert.That(root.NamespaceUri, Is.EqualTo(SvgNamespaces.Svg));
            Assert.That(root.Attributes.Select(attribute => attribute.QualifiedName), Is.EqualTo(new[] { "xmlns:s", "xmlns:v", "z", "a" }));
            Assert.That(widget.ElementKind, Is.EqualTo(SvgElementKind.Unknown));
            Assert.That(widget.QualifiedName, Is.EqualTo("v:widget"));
            Assert.That(widget.Prefix, Is.EqualTo("v"));
            Assert.That(widget.NamespaceUri, Is.EqualTo("urn:vendor"));
            Assert.That(widget.Attributes.Select(attribute => attribute.QualifiedName), Is.EqualTo(new[] { "v:second", "plain", "v:first" }));
            Assert.That(widget.Attributes.Select(attribute => attribute.NamespaceUri), Is.EqualTo(new[] { "urn:vendor", "", "urn:vendor" }));
            Assert.That(child.ElementKind, Is.EqualTo(SvgElementKind.Unknown));
            Assert.That(child.QualifiedName, Is.EqualTo("v:child"));
            Assert.That(root.RawToken.Attributes.Select(attribute => attribute.RawText), Is.EqualTo(new[] { "xmlns:s='http://www.w3.org/2000/svg'", "xmlns:v='urn:vendor'", "z='last'", "a='first'" }));
        });
    }

    [Test]
    public void InvalidSvgCssAndPathValuesKeepExactRawTextAlongsideXmlNormalization()
    {
        string input = "<svg d='M0 0 X ???' style=\"fill:calc(??); custom:raw\" data-raw='  a&#xA;\r\n\t&amp;&quot;  '/>";
        SvgDocument document = Load(input);
        SvgNode root = document.DocumentElement;
        SvgAttribute path = root.Attributes[0];
        SvgAttribute style = root.Attributes[1];
        SvgAttribute raw = root.Attributes[2];

        Assert.Multiple(() =>
        {
            Assert.That(path.Value, Is.EqualTo("M0 0 X ???"));
            Assert.That(path.RawToken.RawText, Is.EqualTo("d='M0 0 X ???'"));
            Assert.That(style.Value, Is.EqualTo("fill:calc(??); custom:raw"));
            Assert.That(style.RawToken.RawText, Is.EqualTo("style=\"fill:calc(??); custom:raw\""));
            Assert.That(raw.Value, Is.EqualTo("  a\n  &\"  "));
            Assert.That(raw.RawToken.Value, Is.EqualTo("  a&#xA;\r\n\t&amp;&quot;  "));
            Assert.That(raw.RawToken.Quote, Is.EqualTo('\''));
            Assert.That(raw.RawToken.References.Select(reference => reference.RawText), Is.EqualTo(new[] { "&#xA;", "&amp;", "&quot;" }));
            Assert.That(root.Attributes.Select(attribute => attribute.QualifiedName), Is.EqualTo(new[] { "d", "style", "data-raw" }));
        });
    }

    [Test]
    public void EditingOneRegionLeavesUntouchedVendorLexemesAndOriginalSnapshotAvailable()
    {
        SvgDocumentApi documents = new SvgDocumentApi();
        SvgNodeApi mutations = new SvgNodeApi();
        string input = "<svg id='old' xmlns:v='urn:vendor'><v:widget odd='  raw  '><![CDATA[keep <this>]]><!--keep--></v:widget></svg>";
        SvgLoadResult loaded = new SvgLoader().LoadWithStringAndSvgLoadOptions(input, new SvgLoadOptions());
        SvgDocument document = loaded.Document;
        SvgNode root = document.DocumentElement;
        SvgNode widget = root.Children[0];
        SvgSourceSpan widgetSpan = widget.SourceSpan;
        SvgXmlToken widgetStart = widget.RawToken;
        SvgXmlToken widgetEnd = widget.EndToken;
        SvgAttribute odd = widget.Attributes[0];
        SvgXmlAttributeToken oddToken = odd.RawToken;

        mutations.ReplaceAttribute(root, root.Attributes[0], documents.CreateAttribute("id", "", "id", "", "new"));

        Assert.Multiple(() =>
        {
            Assert.That(widget.SourceMapKind, Is.EqualTo(SvgSourceMapKind.Original));
            Assert.That(widget.SourceSpan, Is.SameAs(widgetSpan));
            Assert.That(widget.RawToken, Is.SameAs(widgetStart));
            Assert.That(widget.EndToken, Is.SameAs(widgetEnd));
            Assert.That(widget.RawToken.RawText, Is.EqualTo("<v:widget odd='  raw  '>"));
            Assert.That(widget.EndToken.RawText, Is.EqualTo("</v:widget>"));
            Assert.That(odd.SourceMapKind, Is.EqualTo(SvgSourceMapKind.Original));
            Assert.That(odd.RawToken, Is.SameAs(oddToken));
            Assert.That(odd.RawToken.RawText, Is.EqualTo("odd='  raw  '"));
            Assert.That(widget.Children[0].RawToken.RawText, Is.EqualTo("<![CDATA[keep <this>]]>"));
            Assert.That(widget.Children[1].RawToken.RawText, Is.EqualTo("<!--keep-->"));
            Assert.That(document.SourceText, Is.SameAs(input));
            Assert.That(string.Concat(loaded.XmlTokens.Select(token => token.RawText)), Is.EqualTo(input));
        });
    }

    static SvgDocument Load(string input)
    {
        SvgLoadResult loaded = new SvgLoader().LoadWithStringAndSvgLoadOptions(input, new SvgLoadOptions());
        Assert.That(loaded.Success, Is.True);
        return loaded.Document;
    }
}
