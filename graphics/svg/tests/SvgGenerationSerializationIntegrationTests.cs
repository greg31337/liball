namespace svg.Tests;

public class SvgGenerationSerializationIntegrationTests
{
    static readonly SvgWriteMode[] Modes =
    {
        SvgWriteMode.SourcePreserving,
        SvgWriteMode.Canonical,
        SvgWriteMode.Pretty,
        SvgWriteMode.Compact
    };

    [TestCase("document")]
    [TestCase("path")]
    [TestCase("paint")]
    [TestCase("filter")]
    [TestCase("canvas")]
    [TestCase("sprite")]
    public void EveryGeneratorFamilyRoundTripsThroughEveryModeAndDestination(string family)
    {
        SvgDocument generated = BuildGeneratedDocument(family);

        foreach (SvgWriteMode mode in Modes)
        {
            SvgWriteOptions options = Options(mode);
            string context = family + " / " + mode;
            SvgWriteResult first = new SvgWriter().WriteWithDocumentAndOptions(generated, options);
            SvgWriteResult repeated = new SvgWriter().WriteWithDocumentAndOptions(generated, options);

            Assert.That(first.Success, Is.True, context + ": " + first.Message);
            Assert.That(repeated.Text, Is.EqualTo(first.Text), context + " string determinism");

            using (StringWriter textDestination = new StringWriter())
            {
                SvgWriteResult textResult = new SvgWriter().WriteWithSvgDocumentAndTextWriterAndSvgWriteOptions(generated, textDestination, options);
                Assert.That(textResult.Success, Is.True, context + ": " + textResult.Message);
                Assert.That(textDestination.ToString(), Is.EqualTo(first.Text), context + " text writer");
            }

            SvgWriteBytesResult utf8 = new SvgWriter().WriteUtf8WithDocumentAndOptions(generated, options);
            Assert.That(utf8.Success, Is.True, context + ": " + utf8.WriteResult.Message);
            Assert.That(System.Text.Encoding.UTF8.GetString(utf8.Bytes), Is.EqualTo(first.Text), context + " UTF-8 array");

            using (MemoryStream streamDestination = new MemoryStream())
            {
                SvgWriteResult streamResult = new SvgWriter().WriteWithSvgDocumentAndStreamAndSvgWriteOptions(generated, streamDestination, options);
                Assert.That(streamResult.Success, Is.True, context + ": " + streamResult.Message);
                Assert.That(streamDestination.ToArray(), Is.EqualTo(utf8.Bytes), context + " stream");
            }

            char[] characters = new char[first.Text.Length];
            SvgWriteBufferResult characterResult = new SvgWriter().WriteWithSvgDocumentAndSpanOfCharAndSvgWriteOptions(generated, characters.AsSpan(), options);
            Assert.That(characterResult.Success, Is.True, context + ": " + characterResult.Message);
            Assert.That(new string(characters), Is.EqualTo(first.Text), context + " character span");

            byte[] bytes = new byte[utf8.ByteCount];
            SvgWriteBufferResult byteResult = new SvgWriter().WriteUtf8WithDocumentAndDestinationAndOptions(generated, bytes.AsSpan(), options);
            Assert.That(byteResult.Success, Is.True, context + ": " + byteResult.Message);
            Assert.That(bytes, Is.EqualTo(utf8.Bytes), context + " byte span");

            SvgDocument firstLoad = Load(first.Text, context + " first load");
            SvgWriteResult rewritten = new SvgWriter().WriteWithDocumentAndOptions(firstLoad, options);
            Assert.That(rewritten.Success, Is.True, context + ": " + rewritten.Message);
            SvgDocument secondLoad = Load(rewritten.Text, context + " second load");
            AssertSemanticallyEquivalent(firstLoad.DocumentNode, secondLoad.DocumentNode, context + " load-write-load");

            AssertSvgz(generated, firstLoad, options, context);
            AssertDataUrls(generated, firstLoad, options, context);
        }
    }

    [Test]
    public void GeneratedUnknownNamespacedContentIsRepairedEscapedAndUnicodeSafe()
    {
        SvgDocument document = BuildDocumentBuilderOutput();

        foreach (SvgWriteMode mode in Modes)
        {
            SvgWriteResult result = new SvgWriter().WriteWithDocumentAndOptions(document, Options(mode));

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True, mode + ": " + result.Message);
                Assert.That(result.Text, Does.Contain("xmlns:v=\"urn:vendor\""), mode + " namespace repair");
                Assert.That(result.Text, Does.Contain("<v:widget"), mode + " unknown element");
                Assert.That(result.Text, Does.Contain("v:label=\"He said &quot;yes&quot; &amp; &lt;ok>\""), mode + " attribute escaping");
                Assert.That(result.Text, Does.Contain("Crème 東京 &lt;tag&gt; &amp; tea"), mode + " text escaping and Unicode");
            });

            SvgDocument reloaded = Load(result.Text, mode + " namespace reload");
            SvgNode widget = FindElement(reloaded.DocumentElement, "urn:vendor", "widget");
            Assert.That(widget, Is.Not.Null, mode + " unknown element reload");
            Assert.That(Attribute(widget, "urn:vendor", "label"), Is.EqualTo("He said \"yes\" & <ok>"));
            Assert.That(new SvgNodeApi().GetTextContent(reloaded.Index.GetById("greeting")), Is.EqualTo("Crème 東京 <tag> & tea"));
        }
    }

    static SvgWriteOptions Options(SvgWriteMode mode)
    {
        SvgWriteOptions options = new SvgWriteOptions();
        options.Mode = mode;
        if (mode == SvgWriteMode.Pretty)
        {
            options.IndentText = "  ";
            options.NewLine = "\n";
        }
        return options;
    }

    static SvgDocument BuildGeneratedDocument(string family)
    {
        if (family == "document") return BuildDocumentBuilderOutput();
        if (family == "path") return BuildPathBuilderOutput();
        if (family == "paint") return BuildPaintBuilderOutput();
        if (family == "filter") return BuildFilterBuilderOutput();
        if (family == "canvas") return BuildCanvasOutput();
        if (family == "sprite") return BuildSpriteBuilderOutput();
        throw new ArgumentOutOfRangeException("family");
    }

    static SvgDocument BuildDocumentBuilderOutput()
    {
        SvgBuilder builder = new SvgBuilder();
        builder.SetAttribute("viewBox", "0 0 40 20")
            .BeginElement(SvgElementKind.Text)
            .SetAttribute("id", "greeting")
            .SetAttribute("x", "2")
            .SetAttribute("y", "12")
            .AddText("Crème 東京 <tag> & tea")
            .EndElement()
            .BeginElementWithNamespace("v:widget", "v", "widget", "urn:vendor")
            .SetNamespacedAttribute("v:label", "v", "label", "urn:vendor", "He said \"yes\" & <ok>")
            .AddText("未知 content")
            .EndElement()
            .AddComment("generated content");

        Assert.That(builder.Succeeded(), Is.True, builder.Message);
        return builder.Build();
    }

    static SvgDocument BuildPathBuilderOutput()
    {
        SvgBuilder builder = new SvgBuilder();
        builder.SetAttribute("viewBox", "0 0 20 20");
        SvgPathBuildResult result = new SvgPathBuilder()
            .MoveTo(1, 1)
            .LineTo(19, 1)
            .ArcTo(4, 4, 0, false, true, 19, 9)
            .LineTo(1, 19)
            .Close()
            .AppendTo(builder);
        builder.Select(result.Element).SetAttribute("aria-label", "Path ✓").Select(builder.Root);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(builder.Succeeded(), Is.True, builder.Message);
        });
        return builder.Build();
    }

    static SvgDocument BuildPaintBuilderOutput()
    {
        SvgBuilder builder = new SvgBuilder();
        builder.BeginElement(SvgElementKind.Rect)
            .SetAttribute("width", "32")
            .SetAttribute("height", "16");
        SvgPaintBuilder paint = new SvgPaintBuilder(builder);
        paint.LinearGradientWithCoordinates(0, 0, 32, 0)
            .AddStop(0, "#ff0000")
            .AddStopWithOpacity(1, "#0000ff", .75)
            .FillLastElement();

        Assert.Multiple(() =>
        {
            Assert.That(builder.Succeeded(), Is.True, builder.Message);
            Assert.That(paint.Succeeded(), Is.True, paint.Message);
            Assert.That(paint.Build(), Is.Not.Null);
        });
        return builder.Build();
    }

    static SvgDocument BuildFilterBuilderOutput()
    {
        SvgBuilder builder = new SvgBuilder();
        builder.BeginElement(SvgElementKind.Circle)
            .SetAttribute("cx", "8")
            .SetAttribute("cy", "8")
            .SetAttribute("r", "6");
        SvgFilterBuilder filter = new SvgFilterBuilder(builder);
        filter.Begin().Flood("rebeccapurple", .5).GaussianBlur(1.5);

        Assert.Multiple(() =>
        {
            Assert.That(builder.Succeeded(), Is.True, builder.Message);
            Assert.That(filter.Succeeded(), Is.True, filter.Message);
            Assert.That(filter.Build(), Is.Not.Null);
        });
        return builder.Build();
    }

    static SvgDocument BuildCanvasOutput()
    {
        SvgCanvas canvas = SvgCanvas.CreateWithViewport(32, 16);
        canvas.SetFill("#369")
            .BeginPath()
            .Rectangle(1, 1, 12, 8)
            .Fill()
            .SetFill("black")
            .FillText("Canvas ✓", 2, 14);

        Assert.That(canvas.Succeeded(), Is.True, canvas.Message);
        return canvas.Build();
    }

    static SvgDocument BuildSpriteBuilderOutput()
    {
        SvgBuilder sourceBuilder = new SvgBuilder();
        sourceBuilder.SetAttribute("viewBox", "0 0 12 12")
            .BeginElement(SvgElementKind.Title)
            .AddText("Sprite café")
            .EndElement();
        SvgPathBuildResult path = new SvgPathBuilder().MoveTo(1, 1).LineTo(11, 6).LineTo(1, 11).Close().AppendTo(sourceBuilder);
        sourceBuilder.Select(path.Element).SetAttribute("id", "arrow").Select(sourceBuilder.Root);
        SvgSpriteBuilderOptions options = new SvgSpriteBuilderOptions();
        options.OptimizeSafely = false;
        SvgSpriteBuildResult result = SvgSpriteBuilder.Create(options).AddNamedDocument(sourceBuilder.Build(), "arrow-icon").Build();

        Assert.Multiple(() =>
        {
            Assert.That(sourceBuilder.Succeeded(), Is.True, sourceBuilder.Message);
            Assert.That(path.Success, Is.True, path.Message);
            Assert.That(result.Success, Is.True, result.Message);
        });
        return result.Document;
    }

    static void AssertSvgz(SvgDocument generated, SvgDocument expected, SvgWriteOptions options, string context)
    {
        SvgWriteBytesResult first = new SvgWriter().WriteSvgzWithDocumentAndOptions(generated, options);
        SvgWriteBytesResult repeated = new SvgWriter().WriteSvgzWithDocumentAndOptions(generated, options);
        Assert.That(first.Success, Is.True, context + ": " + first.WriteResult.Message);
        Assert.That(repeated.Bytes, Is.EqualTo(first.Bytes), context + " SVGZ determinism");
        SvgDocument loaded = Load(first.Bytes, context + " SVGZ load");
        AssertSemanticallyEquivalent(expected.DocumentNode, loaded.DocumentNode, context + " SVGZ semantics");

        using (MemoryStream destination = new MemoryStream())
        {
            SvgWriteBytesResult streamed = new SvgWriter().WriteSvgzWithDocumentAndDestinationAndOptions(generated, destination, options);
            Assert.That(streamed.Success, Is.True, context + ": " + streamed.WriteResult.Message);
            Assert.That(destination.ToArray(), Is.EqualTo(first.Bytes), context + " SVGZ stream");
        }
    }

    static void AssertDataUrls(SvgDocument generated, SvgDocument expected, SvgWriteOptions options, string context)
    {
        SvgDataUrlWriteEncoding[] encodings =
        {
            SvgDataUrlWriteEncoding.Unencoded,
            SvgDataUrlWriteEncoding.PercentEncoded,
            SvgDataUrlWriteEncoding.Base64
        };
        foreach (SvgDataUrlWriteEncoding encoding in encodings)
        {
            SvgWriteDataUrlResult result = new SvgWriter().WriteDataUrlWithDocumentAndEncodingAndOptions(generated, encoding, options);
            SvgWriteDataUrlResult repeated = new SvgWriter().WriteDataUrlWithDocumentAndEncodingAndOptions(generated, encoding, options);
            Assert.That(result.Success, Is.True, context + ": " + result.Message);
            Assert.That(repeated.DataUrl, Is.EqualTo(result.DataUrl), context + " " + encoding + " data URL determinism");
            byte[] payload;
            if (encoding == SvgDataUrlWriteEncoding.Unencoded)
            {
                int comma = result.DataUrl.IndexOf(',', StringComparison.Ordinal);
                Assert.That(comma, Is.GreaterThan(0), context + " unencoded data URL separator");
                payload = System.Text.Encoding.UTF8.GetBytes(result.DataUrl.Substring(comma + 1));
            }
            else
            {
                SvgDataUrlResult parsed = new SvgDataUrl().ParseWithValue(result.DataUrl);
                Assert.That(parsed.Success, Is.True, context + " " + encoding + " data URL parse");
                payload = parsed.Bytes;
            }
            SvgDocument loaded = Load(payload, context + " " + encoding + " data URL load");
            AssertSemanticallyEquivalent(expected.DocumentNode, loaded.DocumentNode, context + " " + encoding + " data URL semantics");
        }
    }

    static SvgDocument Load(string source, string context)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True, context + ": " + Diagnostics(result.Diagnostics));
        return result.Document;
    }

    static SvgDocument Load(byte[] source, string context)
    {
        SvgLoadResult result = new SvgLoader().LoadWithByteArrayAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True, context + ": " + Diagnostics(result.Diagnostics));
        return result.Document;
    }

    static void AssertSemanticallyEquivalent(SvgNode expected, SvgNode actual, string context)
    {
        Assert.That(actual.Kind, Is.EqualTo(expected.Kind), context + " node kind");
        Assert.That(actual.NamespaceUri, Is.EqualTo(expected.NamespaceUri), context + " namespace");
        Assert.That(actual.LocalName, Is.EqualTo(expected.LocalName), context + " local name");
        if (expected.Kind != SvgNodeKind.Element && expected.Kind != SvgNodeKind.Document)
        {
            Assert.That(actual.Value, Is.EqualTo(expected.Value), context + " content");
        }

        List<SvgAttribute> expectedAttributes = SemanticAttributes(expected);
        List<SvgAttribute> actualAttributes = SemanticAttributes(actual);
        Assert.That(actualAttributes.Count, Is.EqualTo(expectedAttributes.Count), context + " attribute count");
        for (int index = 0; index < expectedAttributes.Count; index = index + 1)
        {
            SvgAttribute expectedAttribute = expectedAttributes[index];
            SvgAttribute actualAttribute = actualAttributes[index];
            Assert.That(actualAttribute.NamespaceUri, Is.EqualTo(expectedAttribute.NamespaceUri), context + " attribute namespace");
            Assert.That(actualAttribute.LocalName, Is.EqualTo(expectedAttribute.LocalName), context + " attribute local name");
            Assert.That(actualAttribute.Value, Is.EqualTo(expectedAttribute.Value), context + " attribute value");
        }

        List<SvgNode> expectedChildren = SemanticChildren(expected);
        List<SvgNode> actualChildren = SemanticChildren(actual);
        Assert.That(actualChildren.Count, Is.EqualTo(expectedChildren.Count), context + " child count");
        for (int index = 0; index < expectedChildren.Count; index = index + 1)
        {
            AssertSemanticallyEquivalent(expectedChildren[index], actualChildren[index], context + "/" + index);
        }
    }

    static List<SvgAttribute> SemanticAttributes(SvgNode node)
    {
        return node.Attributes
            .Where(attribute => attribute.NamespaceUri != SvgNamespaces.Xmlns)
            .OrderBy(attribute => attribute.NamespaceUri, StringComparer.Ordinal)
            .ThenBy(attribute => attribute.LocalName, StringComparer.Ordinal)
            .ToList();
    }

    static List<SvgNode> SemanticChildren(SvgNode node)
    {
        bool hasElements = node.Children.Any(child => child.Kind == SvgNodeKind.Element);
        return node.Children
            .Where(child => !hasElements || child.Kind != SvgNodeKind.Text || !string.IsNullOrWhiteSpace(child.Value))
            .ToList();
    }

    static SvgNode FindElement(SvgNode root, string namespaceUri, string localName)
    {
        if (root.Kind == SvgNodeKind.Element && root.NamespaceUri == namespaceUri && root.LocalName == localName) return root;
        foreach (SvgNode child in root.Children)
        {
            SvgNode found = FindElement(child, namespaceUri, localName);
            if (found != null) return found;
        }
        return null;
    }

    static string Attribute(SvgNode element, string namespaceUri, string localName)
    {
        SvgAttribute attribute = element.Attributes.SingleOrDefault(item => item.NamespaceUri == namespaceUri && item.LocalName == localName);
        return attribute == null ? null : attribute.Value;
    }

    static string Diagnostics(List<SvgDiagnostic> diagnostics)
    {
        return string.Join(" | ", diagnostics.Select(diagnostic => diagnostic.Message));
    }
}
