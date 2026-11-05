namespace svg.Tests;

public class SvgXmlWriterTests
{
    [Test]
    public void RepairsConflictingElementAttributeAndDefaultNamespacesDeterministically()
    {
        SvgBuilder builder = new SvgBuilder();
        builder.SetNamespacedAttribute("xmlns:u", "xmlns", "u", SvgNamespaces.Xmlns, "urn:unused")
            .BeginElementWithNamespace("p:item", "p", "item", "urn:element")
            .SetNamespacedAttribute("p:value", "p", "value", "urn:attribute", "<&\"'")
            .SetNamespacedAttribute("href", "", "href", SvgNamespaces.XLink, "#target")
            .SetNamespacedAttribute("hint", "", "hint", "urn:unused", "ok")
            .BeginElementWithNamespace("span", "", "span", SvgNamespaces.XHtml)
            .SetAttribute("lang", "en")
            .EndElement()
            .BeginElementWithNamespace("plain", "", "plain", "")
            .EndElement()
            .EndElement();
        SvgWriteOptions options = new SvgWriteOptions();
        options.QuoteStyle = SvgAttributeQuoteStyle.Single;

        SvgWriteResult first = new SvgWriter().WriteWithDocumentAndOptions(builder.Document, options);
        SvgWriteResult second = new SvgWriter().WriteWithDocumentAndOptions(builder.Document, options);
        SvgWriteOptions omitUnusedOptions = new SvgWriteOptions();
        omitUnusedOptions.QuoteStyle = SvgAttributeQuoteStyle.Single;
        omitUnusedOptions.PreserveNamespaceDeclarations = false;
        SvgWriteResult withoutUnused = new SvgWriter().WriteWithDocumentAndOptions(builder.Document, omitUnusedOptions);
        SvgLoadResult loadedWithoutUnused = Load(withoutUnused.Text);
        SvgLoadResult loaded = Load(first.Text);
        SvgNode item = loaded.Document.DocumentElement.Children.Single();
        SvgNode html = item.Children[0];
        SvgNode plain = item.Children[1];

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True, first.Message);
            Assert.That(first.Text, Is.EqualTo(second.Text));
            Assert.That(first.Text, Is.EqualTo("<svg xmlns='http://www.w3.org/2000/svg' xmlns:u='urn:unused'><p:item xmlns:ns1='urn:attribute' xmlns:p='urn:element' xmlns:xlink='http://www.w3.org/1999/xlink' xlink:href='#target' ns1:value='&lt;&amp;\"&apos;' u:hint='ok'><span xmlns='http://www.w3.org/1999/xhtml' lang='en'/><plain xmlns=''/></p:item></svg>"));
            Assert.That(withoutUnused.Text, Does.Not.Contain("xmlns:u"));
            Assert.That(loadedWithoutUnused.Success, Is.True, Diagnostics(loadedWithoutUnused.Diagnostics));
            Assert.That(loaded.Success, Is.True, Diagnostics(loaded.Diagnostics));
            Assert.That(item.NamespaceUri, Is.EqualTo("urn:element"));
            Assert.That(item.Attributes.Single(attribute => attribute.LocalName == "value").NamespaceUri, Is.EqualTo("urn:attribute"));
            Assert.That(item.Attributes.Single(attribute => attribute.LocalName == "value").Value, Is.EqualTo("<&\"'"));
            Assert.That(item.Attributes.Single(attribute => attribute.LocalName == "href").NamespaceUri, Is.EqualTo(SvgNamespaces.XLink));
            Assert.That(html.NamespaceUri, Is.EqualTo(SvgNamespaces.XHtml));
            Assert.That(plain.NamespaceUri, Is.Empty);
            Assert.That(first.WrittenNodeCount, Is.EqualTo(4));
            Assert.That(first.WrittenAttributeCount, Is.EqualTo(11));
        });
    }

    [Test]
    public void AppliesDeclarationDoctypeEmptyTagQuoteWhitespaceAndAttributeOrderPolicies()
    {
        SvgLoadOptions loadOptions = new SvgLoadOptions();
        loadOptions.DtdPolicy = SvgDtdPolicy.PreserveOnly;
        SvgDocument document = new SvgLoader().LoadWithStringAndSvgLoadOptions("<!DOCTYPE svg SYSTEM 'icon.dtd'><svg xmlns='http://www.w3.org/2000/svg' z='2' a='1'><g title=\"a'b\"/></svg>", loadOptions).Document;
        SvgWriteOptions options = new SvgWriteOptions();
        options.Mode = SvgWriteMode.Pretty;
        options.DeclarationPolicy = SvgXmlDeclarationPolicy.Include;
        options.DeclarationEncoding = "UTF-8";
        options.DeclarationStandalone = "yes";
        options.DoctypePolicy = SvgWriteDoctypePolicy.Preserve;
        options.EmptyElementPolicy = SvgEmptyElementPolicy.ExplicitEndTag;
        options.QuoteStyle = SvgAttributeQuoteStyle.Single;
        options.NewLine = "\r\n";
        options.IndentText = "\t";

        SvgWriteResult result = new SvgWriter().WriteXml(document, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(result.Text, Is.EqualTo("<?xml version='1.0' encoding='UTF-8' standalone='yes'?>\r\n<!DOCTYPE svg SYSTEM 'icon.dtd'>\r\n<svg xmlns='http://www.w3.org/2000/svg' a='1' z='2'>\r\n\t<g title='a&apos;b'></g>\r\n</svg>"));
            Assert.That(result.CharacterCount, Is.EqualTo(result.Text.Length));
            Assert.That(result.WrittenNodeCount, Is.EqualTo(3));
            Assert.That(result.WrittenAttributeCount, Is.EqualTo(4));
        });
    }

    [Test]
    public void EscapesTextAttributesAndCDataWithoutChangingReloadedCharacterContent()
    {
        SvgBuilder builder = new SvgBuilder();
        builder.BeginElement(SvgElementKind.Text)
            .SetAttribute("data", "line\tone\ntwo\rthree\"quoted\"")
            .AddText("A<&>\r")
            .AddCData("x]]>y")
            .EndElement()
            .AddComment("safe")
            .Append(new SvgDocumentApi().CreateProcessingInstruction("edit", "keep"))
            .AddElement(SvgElementKind.Circle);
        SvgWriteOptions options = new SvgWriteOptions();
        options.QuoteStyle = SvgAttributeQuoteStyle.MinimizeEscapes;
        options.SpaceBeforeEmptyElementSlash = true;

        SvgWriteResult result = new SvgWriter().WriteWithDocumentAndOptions(builder.Document, options);
        SvgLoadResult loaded = Load(result.Text);
        SvgNode text = loaded.Document.DocumentElement.Children.Single(item => item.ElementKind == SvgElementKind.Text);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(result.Text, Does.Contain("data='line&#x9;one&#xA;two&#xD;three\"quoted\"'"));
            Assert.That(result.Text, Does.Contain("A&lt;&amp;&gt;&#xD;"));
            Assert.That(result.Text, Does.Contain("<![CDATA[x]]]]><![CDATA[>y]]>"));
            Assert.That(result.Text, Does.EndWith("<!--safe--><?edit keep?><circle /></svg>"));
            Assert.That(loaded.Success, Is.True, Diagnostics(loaded.Diagnostics));
            Assert.That(text.Attributes.Single(item => item.LocalName == "data").Value, Is.EqualTo("line\tone\ntwo\rthree\"quoted\""));
            Assert.That(new SvgNodeApi().GetTextContent(text), Is.EqualTo("A<&>\rx]]>y"));
        });
    }

    [Test]
    public void FailsAtomicallyForRejectedDoctypesInvalidCharactersDuplicatesAndBounds()
    {
        SvgLoadOptions loadOptions = new SvgLoadOptions();
        loadOptions.DtdPolicy = SvgDtdPolicy.PreserveOnly;
        SvgDocument withDoctype = new SvgLoader().LoadWithStringAndSvgLoadOptions("<!DOCTYPE svg><svg/>", loadOptions).Document;
        SvgWriteOptions rejectOptions = new SvgWriteOptions();
        rejectOptions.DoctypePolicy = SvgWriteDoctypePolicy.Reject;
        SvgWriteResult rejected = new SvgWriter().WriteWithDocumentAndOptions(withDoctype, rejectOptions);

        SvgBuilder invalidBuilder = new SvgBuilder();
        invalidBuilder.AddText("bad\0text");
        SvgWriteResult invalid = new SvgWriter().WriteWithDocument(invalidBuilder.Document);

        SvgDocumentApi documents = new SvgDocumentApi();
        SvgNode duplicate = documents.CreateElement("item", "", "item", "");
        duplicate.Attributes.Add(documents.CreateAttribute("a", "", "a", "", "1"));
        duplicate.Attributes.Add(documents.CreateAttribute("b:a", "b", "a", "", "2"));
        SvgWriteResult duplicateResult = new SvgWriter().WriteWithNodeAndOptions(duplicate, new SvgWriteOptions());

        SvgWriteOptions boundedOptions = new SvgWriteOptions();
        boundedOptions.MaximumCharacters = 5;
        SvgWriteResult bounded = new SvgWriter().WriteWithDocumentAndOptions(new SvgBuilder().Document, boundedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Error, Is.EqualTo(SvgWriteError.DoctypeRejected));
            Assert.That(rejected.Text, Is.Empty);
            Assert.That(invalid.Error, Is.EqualTo(SvgWriteError.InvalidXml));
            Assert.That(invalid.Text, Is.Empty);
            Assert.That(duplicateResult.Error, Is.EqualTo(SvgWriteError.DuplicateAttribute));
            Assert.That(duplicateResult.Text, Is.Empty);
            Assert.That(bounded.Error, Is.EqualTo(SvgWriteError.LimitExceeded));
            Assert.That(bounded.Text, Is.Empty);
            Assert.That(new[] { rejected, invalid, duplicateResult, bounded }, Has.All.Matches<SvgWriteResult>(item => !item.Success && item.Diagnostics.Count == 1));
        });
    }

    static SvgLoadResult Load(string source)
    {
        SvgLoadOptions options = new SvgLoadOptions();
        options.DtdPolicy = SvgDtdPolicy.PreserveOnly;
        return new SvgLoader().LoadWithStringAndSvgLoadOptions(source, options);
    }

    static string Diagnostics(List<SvgDiagnostic> diagnostics)
    {
        return string.Join(" | ", diagnostics.Select(item => item.Message));
    }
}
