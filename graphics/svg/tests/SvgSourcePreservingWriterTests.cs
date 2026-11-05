namespace svg.Tests;

public class SvgSourcePreservingWriterTests
{
    [Test]
    public void ReusesAnUnchangedDocumentExactlyIncludingDeclarationDoctypeAndReferences()
    {
        string source = "<?xml version='1.0'?>\r\n<!DOCTYPE svg SYSTEM 'icon.dtd'>\n<svg xmlns='http://www.w3.org/2000/svg' xmlns:p='urn:item'  id = 'root'>A&amp;<![CDATA[x]]><p:item a = \"1\"></p:item><!--keep--><?edit value?></svg>";
        SvgDocument document = Load(source);
        SvgWriteOptions options = SourcePreservingOptions();
        options.DoctypePolicy = SvgWriteDoctypePolicy.Preserve;

        SvgWriteResult result = new SvgWriter().WriteWithDocumentAndOptions(document, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(result.Text, Is.EqualTo(source));
            Assert.That(result.ReusedSourceRegionCount, Is.GreaterThan(0));
            Assert.That(result.ReserializedRegionCount, Is.Zero);
            Assert.That(result.SourceMap, Has.All.Matches<SvgWriteSourceMapEntry>(entry => entry.Kind == SvgSourceMapKind.Original));
            Assert.That(result.SourceMap.Zip(result.SourceMap.Skip(1), (left, right) => left.OutputStart <= right.OutputStart).All(value => value), Is.True);
        });
    }

    [Test]
    public void ReserializesOnlyTheChangedStartTagAndKeepsDescendantLexicalTokens()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><g  a = '1' b='2'><path data-x = 'v'/>A&amp;B</g ></svg>";
        SvgDocument document = Load(source);
        SvgNode group = document.DocumentElement.Children.Single(node => node.Kind == SvgNodeKind.Element);
        SvgBuilder builder = SvgBuilder.Create(document);
        builder.Select(group).SetAttribute("a", "3");

        SvgWriteResult result = new SvgWriter().WriteWithDocumentAndOptions(document, SourcePreservingOptions());

        Assert.Multiple(() =>
        {
            Assert.That(builder.Error, Is.EqualTo(SvgBuilderError.None), builder.Message);
            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(result.Text, Is.EqualTo("<svg xmlns='http://www.w3.org/2000/svg'><g a=\"3\" b=\"2\"><path data-x = 'v'/>A&amp;B</g ></svg>"));
            Assert.That(result.ReusedSourceRegionCount, Is.GreaterThan(0));
            Assert.That(result.ReserializedRegionCount, Is.EqualTo(1));
            Assert.That(result.SourceMap.Count(entry => entry.Kind == SvgSourceMapKind.Rewritten), Is.EqualTo(1));
        });
    }

    [Test]
    public void RepairsAChangedAncestorPrefixBindingBeforeWritingAnUntouchedDescendant()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' xmlns:p='urn:old'><g><p:item p:a='1'/></g></svg>";
        SvgDocument document = Load(source);
        SvgBuilder builder = SvgBuilder.Create(document);
        builder.Select(document.DocumentElement).SetNamespacedAttribute("xmlns:p", "xmlns", "p", SvgNamespaces.Xmlns, "urn:new");

        SvgWriteResult result = new SvgWriter().WriteWithDocumentAndOptions(document, SourcePreservingOptions());
        SvgDocument reloaded = Load(result.Text);
        SvgNode item = reloaded.DocumentElement.Children.Single(node => node.Kind == SvgNodeKind.Element).Children.Single(node => node.Kind == SvgNodeKind.Element);

        Assert.Multiple(() =>
        {
            Assert.That(builder.Error, Is.EqualTo(SvgBuilderError.None), builder.Message);
            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(result.Text, Does.Contain("xmlns:p=\"urn:new\""));
            Assert.That(result.Text, Does.Not.Contain("<p:item p:a='1'/>") );
            Assert.That(reloaded.DocumentElement.Attributes.Single(attribute => attribute.LocalName == "p" && attribute.NamespaceUri == SvgNamespaces.Xmlns).Value, Is.EqualTo("urn:new"));
            Assert.That(item.NamespaceUri, Is.EqualTo("urn:old"));
            Assert.That(item.Attributes.Single(attribute => attribute.LocalName == "a").NamespaceUri, Is.EqualTo("urn:old"));
            Assert.That(result.ReusedSourceRegionCount, Is.GreaterThan(0));
            Assert.That(result.ReserializedRegionCount, Is.GreaterThanOrEqualTo(2));
        });
    }

    [Test]
    public void PreservesSiblingTokensAroundAnInsertedGeneratedElement()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><g><a data = 'x'/><b/></g></svg>";
        SvgDocument document = Load(source);
        SvgNode group = document.DocumentElement.Children.Single(node => node.Kind == SvgNodeKind.Element);
        SvgNode inserted = new SvgDocumentApi().CreateElement("circle", "", "circle", SvgNamespaces.Svg);
        SvgMutationResult mutation = new SvgNodeApi().InsertChild(group, 1, inserted);

        SvgWriteResult result = new SvgWriter().WriteWithDocumentAndOptions(document, SourcePreservingOptions());

        Assert.Multiple(() =>
        {
            Assert.That(mutation.Success, Is.True, mutation.Message);
            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(result.Text, Is.EqualTo("<svg xmlns='http://www.w3.org/2000/svg'><g><a data = 'x'/><circle/><b/></g></svg>"));
            Assert.That(result.SourceMap.Any(entry => object.ReferenceEquals(entry.Node, inserted) && entry.Kind == SvgSourceMapKind.Generated), Is.True);
            Assert.That(result.ReusedSourceRegionCount, Is.GreaterThan(0));
            Assert.That(result.ReserializedRegionCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void DoesNotReuseARawScalarWhenItsPublicValueNoLongerMatches()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><text>A&amp;B</text></svg>");
        SvgNode text = document.DocumentElement.Children.Single(node => node.Kind == SvgNodeKind.Element);
        SvgNode reference = text.Children.Single(node => node.RawToken.Kind == SvgXmlTokenKind.Reference);
        reference.Value = "<";

        SvgWriteResult result = new SvgWriter().WriteWithDocumentAndOptions(document, SourcePreservingOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(result.Text, Is.EqualTo("<svg xmlns='http://www.w3.org/2000/svg'><text>A&lt;B</text></svg>"));
            Assert.That(result.ReserializedRegionCount, Is.EqualTo(1));
        });
    }

    static SvgDocument Load(string source)
    {
        SvgLoadOptions options = new SvgLoadOptions();
        options.DtdPolicy = SvgDtdPolicy.PreserveOnly;
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, options);
        Assert.That(result.Success, Is.True, string.Join(" | ", result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        return result.Document;
    }

    static SvgWriteOptions SourcePreservingOptions()
    {
        SvgWriteOptions options = new SvgWriteOptions();
        options.Mode = SvgWriteMode.SourcePreserving;
        options.DeclarationPolicy = SvgXmlDeclarationPolicy.Preserve;
        return options;
    }
}
