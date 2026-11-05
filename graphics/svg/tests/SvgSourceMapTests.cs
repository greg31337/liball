namespace svg.Tests;

public class SvgSourceMapTests
{
    [Test]
    public void LoaderCreatesDeterministicOriginalEntriesWithRetainedSpans()
    {
        SvgDocument document = Load("<svg id='root'><g class='a'>text</g><rect/></svg>");
        SvgNode root = document.DocumentElement;

        List<SvgSourceMapEntry> entries = document.SourceMap.GetEntries();

        Assert.Multiple(() =>
        {
            Assert.That(document.SourceMap.Document, Is.SameAs(document));
            Assert.That(entries.Select(entry => entry.Node?.LocalName ?? "@" + entry.Attribute.LocalName), Is.EqualTo(new[]
            {
                "",
                "svg",
                "@id",
                "g",
                "@class",
                "",
                "rect"
            }));
            Assert.That(entries, Has.All.Matches<SvgSourceMapEntry>(entry => entry.Kind == SvgSourceMapKind.Original));
            Assert.That(entries, Has.All.Matches<SvgSourceMapEntry>(entry => entry.SourceSpan != null));
            Assert.That(document.SourceMap.GetNode(root).SourceSpan, Is.SameAs(root.SourceSpan));
            Assert.That(document.SourceMap.GetAttribute(root.Attributes[0]).SourceSpan, Is.SameAs(root.Attributes[0].SourceSpan));
        });
    }

    [Test]
    public void MutationsMarkGeneratedAndRewrittenRegionsWithoutChangingUntouchedSpans()
    {
        SvgDocumentApi documents = new SvgDocumentApi();
        SvgNodeApi mutations = new SvgNodeApi();
        SvgDocument document = Load("<svg id='root'><g>old</g><rect/></svg>");
        SvgNode root = document.DocumentElement;
        SvgNode group = root.Children[0];
        SvgNode text = group.Children[0];
        SvgNode rectangle = root.Children[1];
        SvgSourceSpan rootSpan = root.SourceSpan;
        SvgSourceSpan groupSpan = group.SourceSpan;
        SvgSourceSpan rectangleSpan = rectangle.SourceSpan;
        SvgNode circle = documents.CreateElement("circle", "", "circle", SvgNamespaces.Svg);
        SvgAttribute radius = documents.CreateAttribute("r", "", "r", "", "4");

        Assert.That(mutations.AppendAttribute(circle, radius).Success, Is.True);
        Assert.That(mutations.AppendChild(root, circle).Success, Is.True);
        Assert.That(mutations.SetTextContent(text, "new").Success, Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(document.DocumentNode.SourceMapKind, Is.EqualTo(SvgSourceMapKind.Rewritten));
            Assert.That(root.SourceMapKind, Is.EqualTo(SvgSourceMapKind.Rewritten));
            Assert.That(text.SourceMapKind, Is.EqualTo(SvgSourceMapKind.Rewritten));
            Assert.That(circle.SourceMapKind, Is.EqualTo(SvgSourceMapKind.Generated));
            Assert.That(radius.SourceMapKind, Is.EqualTo(SvgSourceMapKind.Generated));
            Assert.That(group.SourceMapKind, Is.EqualTo(SvgSourceMapKind.Original));
            Assert.That(rectangle.SourceMapKind, Is.EqualTo(SvgSourceMapKind.Original));
            Assert.That(root.SourceSpan, Is.SameAs(rootSpan));
            Assert.That(group.SourceSpan, Is.SameAs(groupSpan));
            Assert.That(rectangle.SourceSpan, Is.SameAs(rectangleSpan));
            Assert.That(circle.SourceSpan, Is.Null);
            Assert.That(radius.SourceSpan, Is.Null);
            Assert.That(document.SourceMap.GetEntriesByKind(SvgSourceMapKind.Generated), Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void MovingAnOriginalNodeRewritesPlacementButNotUntouchedDescendants()
    {
        SvgNodeApi mutations = new SvgNodeApi();
        SvgDocument document = Load("<svg><g><rect id='r'/></g><circle/></svg>");
        SvgNode root = document.DocumentElement;
        SvgNode group = root.Children[0];
        SvgNode rectangle = group.Children[0];
        SvgAttribute id = rectangle.Attributes[0];
        SvgNode circle = root.Children[1];
        SvgSourceSpan rectangleSpan = rectangle.SourceSpan;
        SvgSourceSpan idSpan = id.SourceSpan;

        SvgMutationResult moved = mutations.MoveChild(rectangle, root, 2);

        Assert.Multiple(() =>
        {
            Assert.That(moved.Success, Is.True);
            Assert.That(group.SourceMapKind, Is.EqualTo(SvgSourceMapKind.Rewritten));
            Assert.That(root.SourceMapKind, Is.EqualTo(SvgSourceMapKind.Rewritten));
            Assert.That(rectangle.SourceMapKind, Is.EqualTo(SvgSourceMapKind.Rewritten));
            Assert.That(id.SourceMapKind, Is.EqualTo(SvgSourceMapKind.Original));
            Assert.That(circle.SourceMapKind, Is.EqualTo(SvgSourceMapKind.Original));
            Assert.That(rectangle.SourceSpan, Is.SameAs(rectangleSpan));
            Assert.That(id.SourceSpan, Is.SameAs(idSpan));
        });
    }

    [Test]
    public void AttributeDirtyTrackingRewritesAttributeAndOwningStartTag()
    {
        SvgDocument document = Load("<svg id='one'><g/></svg>");
        SvgNode root = document.DocumentElement;
        SvgAttribute id = root.Attributes[0];
        SvgNode group = root.Children[0];
        SvgSourceSpan idSpan = id.SourceSpan;

        id.Value = "two";
        id.MarkDirty();

        Assert.Multiple(() =>
        {
            Assert.That(id.SourceMapKind, Is.EqualTo(SvgSourceMapKind.Rewritten));
            Assert.That(root.SourceMapKind, Is.EqualTo(SvgSourceMapKind.Rewritten));
            Assert.That(group.SourceMapKind, Is.EqualTo(SvgSourceMapKind.Original));
            Assert.That(id.SourceSpan, Is.SameAs(idSpan));
        });
    }

    [Test]
    public void SourceDisabledAndReaderLoadsRemainOriginalWithoutInventingSpans()
    {
        SvgLoadOptions options = new SvgLoadOptions();
        options.PreserveSource = false;
        SvgDocument sourceDisabled = new SvgLoader().LoadWithStringAndSvgLoadOptions("<svg id='one'>text</svg>", options).Document;
        using StringReader text = new StringReader("<svg><g/></svg>");
        using System.Xml.XmlReader reader = System.Xml.XmlReader.Create(text);
        SvgDocument readerDocument = new SvgLoader().LoadWithXmlReaderAndSvgLoadOptions(reader, new SvgLoadOptions()).Document;

        Assert.Multiple(() =>
        {
            Assert.That(sourceDisabled.SourceMap.GetEntries(), Has.All.Matches<SvgSourceMapEntry>(entry => entry.Kind == SvgSourceMapKind.Original));
            Assert.That(sourceDisabled.SourceMap.GetEntries(), Has.All.Matches<SvgSourceMapEntry>(entry => entry.SourceSpan == null));
            Assert.That(readerDocument.SourceMap.GetEntries(), Has.All.Matches<SvgSourceMapEntry>(entry => entry.Kind == SvgSourceMapKind.Original));
            Assert.That(readerDocument.SourceMap.GetEntries(), Has.All.Matches<SvgSourceMapEntry>(entry => entry.SourceSpan == null));
        });
    }

    [Test]
    public void RollbackRestoresOriginalSourceMapKindsAndSpans()
    {
        SvgDocumentApi documents = new SvgDocumentApi();
        SvgNodeApi mutations = new SvgNodeApi();
        SvgDocument document = Load("<svg><g>old</g><rect/></svg>");
        SvgNode root = document.DocumentElement;
        SvgNode group = root.Children[0];
        SvgNode text = group.Children[0];
        SvgNode rectangle = root.Children[1];
        SvgSourceSpan textSpan = text.SourceSpan;
        SvgChangeSet changes = documents.BeginChangeSet(document);
        SvgNode generated = documents.CreateElement("circle", "", "circle", SvgNamespaces.Svg);

        mutations.SetTextContent(text, "changed");
        mutations.AppendChild(root, generated);
        mutations.RemoveChild(root, rectangle);
        Assert.That(changes.Rollback(), Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(document.SourceMap.GetEntries(), Has.All.Matches<SvgSourceMapEntry>(entry => entry.Kind == SvgSourceMapKind.Original));
            Assert.That(text.SourceMapKind, Is.EqualTo(SvgSourceMapKind.Original));
            Assert.That(text.SourceSpan, Is.SameAs(textSpan));
            Assert.That(rectangle.SourceMapKind, Is.EqualTo(SvgSourceMapKind.Original));
            Assert.That(rectangle.Parent, Is.SameAs(root));
            Assert.That(generated.SourceMapKind, Is.EqualTo(SvgSourceMapKind.Generated));
            Assert.That(generated.Parent, Is.Null);
        });
    }

    static SvgDocument Load(string input)
    {
        SvgLoadResult loaded = new SvgLoader().LoadWithStringAndSvgLoadOptions(input, new SvgLoadOptions());
        Assert.That(loaded.Success, Is.True);
        return loaded.Document;
    }
}
