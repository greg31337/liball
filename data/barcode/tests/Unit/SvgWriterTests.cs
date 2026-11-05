namespace barcode.Tests;

public class SvgWriterTests
{
    [Test]
    public void RotatedRectanglesUseOneDeterministicCrispPath()
    {
        BarcodeSymbol symbol = CreateSymbol(
            3,
            2,
            new byte[]
            {
                1, 1, 0,
                1, 0, 0
            });
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        options.ModuleScale = 2;
        options.Rotation = BarcodeRotation.Clockwise90;
        RenderPlan plan = RenderPlanner.CreatePlan(
            CreateRequest(symbol, options));

        SvgWriteResult first = SvgWriter.Write(plan, options);
        SvgWriteResult second = SvgWriter.Write(plan, options);
        global::System.Xml.Linq.XDocument document = ParseSvg(first.Text);
        global::System.Xml.Linq.XElement root = document.Root!;
        global::System.Xml.Linq.XNamespace svg =
            "http://www.w3.org/2000/svg";
        global::System.Xml.Linq.XElement path =
            root.Elements(svg + "path").Single();

        Assert.Multiple(() =>
        {
            Assert.That(first.Text, Is.EqualTo(second.Text));
            Assert.That(first.RectangleCount, Is.EqualTo(2));
            Assert.That(first.TextRunCount, Is.EqualTo(0));
            Assert.That(root.Name.NamespaceName,
                Is.EqualTo("http://www.w3.org/2000/svg"));
            Assert.That((string?)root.Attribute("width"), Is.EqualTo("4"));
            Assert.That((string?)root.Attribute("height"), Is.EqualTo("6"));
            Assert.That((string?)root.Attribute("viewBox"),
                Is.EqualTo("0 0 4 6"));
            Assert.That((string?)root.Attribute("shape-rendering"),
                Is.EqualTo("crispEdges"));
            Assert.That(root.Elements(svg + "rect").Count(), Is.EqualTo(1));
            Assert.That((string?)path.Attribute("d"),
                Is.EqualTo("M0 0h2v2h-2Z M2 0h2v4h-2Z"));
            Assert.That((string?)path.Attribute("fill"),
                Is.EqualTo("#000000"));
        });
    }

    [Test]
    public void ColorsAndOpacityAreNumericAndTransparentBackgroundIsOmitted()
    {
        BarcodeSymbol symbol = CreateSymbol(
            1,
            1,
            new byte[] { 1 });
        BarcodeRenderOptions translucent = new BarcodeRenderOptions();
        translucent.ForegroundRgba = 0x12AB3480u;
        translucent.BackgroundRgba = 0x45678940u;
        RenderPlan translucentPlan = RenderPlanner.CreatePlan(
            CreateRequest(symbol, translucent));
        global::System.Xml.Linq.XElement translucentRoot =
            ParseSvg(SvgWriter.Write(translucentPlan, translucent).Text)
                .Root!;

        BarcodeRenderOptions transparent = new BarcodeRenderOptions();
        transparent.ForegroundRgba = 0x12AB3480u;
        transparent.BackgroundRgba = 0x456789FFu;
        transparent.TransparentBackground = true;
        RenderPlan transparentPlan = RenderPlanner.CreatePlan(
            CreateRequest(symbol, transparent));
        global::System.Xml.Linq.XElement transparentRoot =
            ParseSvg(SvgWriter.Write(transparentPlan, transparent).Text)
                .Root!;
        global::System.Xml.Linq.XNamespace svg =
            "http://www.w3.org/2000/svg";

        Assert.Multiple(() =>
        {
            global::System.Xml.Linq.XElement background =
                translucentRoot.Element(svg + "rect")!;
            global::System.Xml.Linq.XElement foreground =
                translucentRoot.Element(svg + "path")!;
            Assert.That((string?)background.Attribute("fill"),
                Is.EqualTo("#456789"));
            Assert.That((string?)background.Attribute("fill-opacity"),
                Is.EqualTo("0.25098"));
            Assert.That((string?)foreground.Attribute("fill"),
                Is.EqualTo("#12AB34"));
            Assert.That((string?)foreground.Attribute("fill-opacity"),
                Is.EqualTo("0.501961"));
            Assert.That(transparentRoot.Elements(svg + "rect"), Is.Empty);
            Assert.That((string?)transparentRoot
                .Element(svg + "path")!
                .Attribute("fill-opacity"), Is.EqualTo("0.501961"));
        });
    }

    [Test]
    public void MetadataAndOptInTextAreEscapedAndRotatedSafely()
    {
        BarcodeSymbol symbol = CreateSymbol(
            2,
            1,
            new byte[] { 1, 0 });
        symbol.SymbologyIdentifier = "]d2<&>\"'";
        BarcodePayload payload = new BarcodePayload();
        payload.Text = "payload<&>\"'";
        symbol.Payload = payload;
        string text = "A<&>\"'</text><script href=\"https://evil\">x";
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        options.ModuleScale = 2;
        options.Rotation = BarcodeRotation.Clockwise270;
        options.TextMode = BarcodeTextMode.Custom;
        options.CustomText = text;
        options.UseSvgText = true;
        RenderPlan plan = RenderPlanner.CreatePlan(
            CreateRequest(symbol, options));

        SvgWriteResult written = SvgWriter.Write(plan, options);
        global::System.Xml.Linq.XDocument document = ParseSvg(written.Text);
        global::System.Xml.Linq.XNamespace svg =
            "http://www.w3.org/2000/svg";
        global::System.Xml.Linq.XElement root = document.Root!;
        global::System.Xml.Linq.XElement textElement =
            root.Element(svg + "text")!;

        Assert.Multiple(() =>
        {
            Assert.That(written.TextRunCount, Is.EqualTo(1));
            Assert.That(root.Element(svg + "title")!.Value,
                Is.EqualTo("]d2<&>\"'"));
            Assert.That(root.Element(svg + "desc")!.Value,
                Is.EqualTo("payload<&>\"'"));
            Assert.That(textElement.Value, Is.EqualTo(text));
            Assert.That((string?)textElement.Attribute("transform"),
                Is.EqualTo("matrix(0 -1 1 0 0 4)"));
            Assert.That(written.Text, Does.Contain("&lt;/text&gt;"));
            Assert.That(written.Text, Does.Not.Contain("<script"));
            Assert.That(ContainsUnsafeElementsOrReferences(document),
                Is.False);
        });
    }

    [Test]
    public void BuiltInTextUsesGlyphPathsUnlessSvgTextIsEnabled()
    {
        BarcodeSymbol symbol = CreateSymbol(
            11,
            1,
            new byte[11]);
        BarcodeTextLayout layout = new BarcodeTextLayout();
        layout.ReservedHeight = 7.0;
        symbol.TextLayout = layout;
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        options.ModuleScale = 2;
        options.TextMode = BarcodeTextMode.Custom;
        options.CustomText = "A";
        RenderPlan plan = RenderPlanner.CreatePlan(
            CreateRequest(symbol, options));

        SvgWriteResult written = SvgWriter.Write(plan, options);
        global::System.Xml.Linq.XNamespace svg =
            "http://www.w3.org/2000/svg";
        global::System.Xml.Linq.XDocument document =
            ParseSvg(written.Text);

        Assert.Multiple(() =>
        {
            Assert.That(written.TextRunCount, Is.EqualTo(1));
            Assert.That(written.GlyphRectangleCount, Is.EqualTo(12));
            Assert.That(document.Descendants(svg + "text"), Is.Empty);
            Assert.That(document.Descendants(svg + "path").Count(),
                Is.EqualTo(1));
        });
    }

    [Test]
    public void InvalidPlansAndInvalidXmlCharactersAreRejected()
    {
        BarcodeSymbol symbol = CreateSymbol(
            1,
            1,
            new byte[] { 1 });
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        options.TextMode = BarcodeTextMode.Custom;
        options.CustomText = "invalid\u0001text";
        options.UseSvgText = true;
        RenderPlan plan = RenderPlanner.CreatePlan(
            CreateRequest(symbol, options));
        RenderPlan invalid = RenderPlanner.CreatePlan(
            CreateRequest(symbol, new BarcodeRenderOptions()));
        invalid.Size.HeightPixels = 0;

        Assert.Multiple(() =>
        {
            Assert.That(
                () => SvgWriter.Write(null, options),
                Throws.TypeOf<BarcodeRenderingException>());
            Assert.That(
                () => SvgWriter.Write(invalid, options),
                Throws.TypeOf<BarcodeRenderingException>());
            Assert.That(
                () => SvgWriter.Write(plan, options),
                Throws.TypeOf<BarcodeRenderingException>());
        });
    }

    static global::System.Xml.Linq.XDocument ParseSvg(string source)
    {
        return global::System.Xml.Linq.XDocument.Parse(
            source,
            global::System.Xml.Linq.LoadOptions.PreserveWhitespace);
    }

    static bool ContainsUnsafeElementsOrReferences(
        global::System.Xml.Linq.XDocument document)
    {
        string[] unsafeElements = new string[]
        {
            "script", "image", "use", "foreignObject", "style"
        };
        foreach (global::System.Xml.Linq.XElement element
            in document.Descendants())
        {
            if (unsafeElements.Contains(element.Name.LocalName))
            {
                return true;
            }

            foreach (global::System.Xml.Linq.XAttribute attribute
                in element.Attributes())
            {
                if (attribute.Name.LocalName == "href"
                    || attribute.Name.LocalName == "src"
                    || attribute.Value.Contains("url(",
                        global::System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    static BarcodeRenderRequest CreateRequest(
        BarcodeSymbol symbol,
        BarcodeRenderOptions options)
    {
        BarcodeRenderRequest request = new BarcodeRenderRequest();
        request.Symbol = symbol;
        request.Options = options;
        return request;
    }

    static BarcodeSymbol CreateSymbol(
        int width,
        int height,
        byte[] modules)
    {
        BarcodeQuietZone quietZone = new BarcodeQuietZone();
        BarcodeSymbol symbol = new BarcodeSymbol();
        symbol.Dimension = BarcodeDimension.Matrix;
        symbol.WidthModules = width;
        symbol.HeightModules = height;
        symbol.Modules = modules;
        symbol.RequiredQuietZone = quietZone;
        return symbol;
    }
}
