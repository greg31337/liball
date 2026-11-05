namespace svg.Tests;

public class SvgUseResolverTests
{
    [Test]
    public void ExpandsSourceLinkedShadowTreeWithoutChangingEditableDom()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><g id='art'><rect id='box'/><text>Hello</text></g><use href='#art'/></svg>");
        SvgNode source = document.DocumentElement.Children[0];
        SvgNode use = document.DocumentElement.Children[1];
        int useChildren = use.Children.Count;

        SvgUseResult result = new SvgUseResolver().ResolveWithNode(use);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Root.SourceNode, Is.SameAs(source));
            Assert.That(result.Root.InstancePath, Is.EqualTo("0"));
            Assert.That(result.Root.Children.Select(item => item.SourceNode), Is.EqualTo(source.Children));
            Assert.That(result.Instances.Select(item => item.InstancePath), Is.EqualTo(new[] { "0", "0/0", "0/1", "0/1/0" }));
            Assert.That(result.FindInstance("0/0").SourceNode, Is.SameAs(source.Children[0]));
            Assert.That(result.FindSource("0/0").Source, Is.SameAs(source.Children[0].SourceSpan));
            Assert.That(result.FindSource("0/0").Provenance[0], Is.SameAs(source.Children[0].SourceSpan));
            Assert.That(result.FindSource("0/0").CorrespondingUseElement, Is.SameAs(use));
            Assert.That(result.FindSource("0/0").ShadowHostSource, Is.SameAs(use));
            Assert.That(use.Children, Has.Count.EqualTo(useChildren));
            Assert.That(result.IsCurrent(), Is.True);
        });
    }

    [Test]
    public void SymbolUsesHostSizeFitsViewBoxAndPlacesAuthoredReferencePoint()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><symbol id='icon' viewBox='0 0 10 20' refX='2' refY='4'><rect width='10' height='20'/></symbol><use href='#icon' x='5' y='7' width='100' height='100'/></svg>");
        SvgUseResolveOptions options = ViewportOptions(300, 200);

        SvgUseResult result = new SvgUseResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[1], options);
        SvgPointTransformResult referencePoint = result.Transform.TransformPoint(new SvgPoint(2, 4));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Viewport, Is.Not.Null);
            Assert.That(result.Viewport.Symbol, Is.True);
            Assert.That(result.Viewport.Width, Is.EqualTo(100));
            Assert.That(result.Viewport.Height, Is.EqualTo(100));
            Assert.That(result.Viewport.WidthOverridden, Is.True);
            Assert.That(result.Viewport.HeightOverridden, Is.True);
            Assert.That(result.Viewport.Bounds.X, Is.EqualTo(5));
            Assert.That(result.Viewport.Bounds.Y, Is.EqualTo(7));
            Assert.That(result.Viewport.Bounds.Width, Is.EqualTo(100));
            Assert.That(result.Viewport.ViewBox.ToText(), Is.EqualTo("0 0 10 20"));
            Assert.That(result.Viewport.LengthContext.ViewportWidth, Is.EqualTo(20));
            Assert.That(result.Viewport.LengthContext.ViewportHeight, Is.EqualTo(20));
            Assert.That(result.Viewport.ClipsOverflow, Is.True);
            Assert.That(referencePoint.Success, Is.True);
            Assert.That(referencePoint.Point.X, Is.EqualTo(5).Within(1e-12));
            Assert.That(referencePoint.Point.Y, Is.EqualTo(7).Within(1e-12));
        });
    }

    [Test]
    public void ReferencedSvgKeepsOwnAutoFallbackSizeAndUseCanOverrideIt()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><svg id='panel' x='2' y='3' width='40' height='20' viewBox='0 0 20 10'><rect/></svg><use href='#panel' x='10' y='20'/><use href='#panel' x='10' y='20' width='80' height='40'/></svg>");
        SvgUseResolveOptions options = ViewportOptions(300, 200);

        SvgUseResult natural = new SvgUseResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[1], options);
        SvgUseResult overridden = new SvgUseResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[2], options);

        Assert.Multiple(() =>
        {
            Assert.That(natural.Success, Is.True);
            Assert.That(natural.Viewport.Width, Is.EqualTo(40));
            Assert.That(natural.Viewport.Height, Is.EqualTo(20));
            Assert.That(natural.Viewport.WidthOverridden, Is.False);
            Assert.That(natural.Viewport.Bounds.X, Is.EqualTo(12));
            Assert.That(natural.Viewport.Bounds.Y, Is.EqualTo(23));
            Assert.That(natural.Transform.E, Is.EqualTo(12));
            Assert.That(natural.Transform.F, Is.EqualTo(23));
            Assert.That(overridden.Success, Is.True);
            Assert.That(overridden.Viewport.Width, Is.EqualTo(80));
            Assert.That(overridden.Viewport.Height, Is.EqualTo(40));
            Assert.That(overridden.Viewport.WidthOverridden, Is.True);
            Assert.That(overridden.Viewport.HeightOverridden, Is.True);
        });
    }

    [Test]
    public void SymbolGeometrySuppliesDefaultViewportWhenUseSizeIsAuto()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><symbol id='panel' x='2' y='3' width='40' height='20' viewBox='0 0 20 10'><rect/></symbol><use href='#panel' x='10' y='20'/></svg>");
        SvgUseResolveOptions options = ViewportOptions(300, 200);

        SvgUseResult result = new SvgUseResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[1], options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Viewport.Width, Is.EqualTo(40));
            Assert.That(result.Viewport.Height, Is.EqualTo(20));
            Assert.That(result.Viewport.WidthOverridden, Is.False);
            Assert.That(result.Viewport.HeightOverridden, Is.False);
            Assert.That(result.Viewport.Bounds.X, Is.EqualTo(12));
            Assert.That(result.Viewport.Bounds.Y, Is.EqualTo(23));
            Assert.That(result.Transform.E, Is.EqualTo(12));
            Assert.That(result.Transform.F, Is.EqualTo(23));
        });
    }

    [Test]
    public void SymbolReferencePointAlsoAppliesWithoutAViewBox()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><symbol id='icon' width='40' height='20' refX='5' refY='2'><rect/></symbol><use href='#icon' x='10' y='12'/></svg>");
        SvgUseResolveOptions options = ViewportOptions(100, 100);

        SvgUseResult result = new SvgUseResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[1], options);
        SvgPointTransformResult referencePoint = result.Transform.TransformPoint(new SvgPoint(5, 2));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Viewport.ViewBox, Is.Null);
            Assert.That(referencePoint.Success, Is.True);
            Assert.That(referencePoint.Point.X, Is.EqualTo(10));
            Assert.That(referencePoint.Point.Y, Is.EqualTo(12));
        });
    }

    [Test]
    public void OrdinaryTargetsIgnoreWidthHeightAndUseContainingCoordinateSystem()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><g id='art'><rect width='50%'/></g><use href='#art' x='25%' y='10' width='0' height='0'/></svg>");
        SvgUseResolveOptions options = ViewportOptions(200, 100);

        SvgUseResult result = new SvgUseResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[1], options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Viewport, Is.Null);
            Assert.That(result.X, Is.EqualTo(50));
            Assert.That(result.Y, Is.EqualTo(10));
            Assert.That(result.Transform.E, Is.EqualTo(50));
            Assert.That(result.Transform.F, Is.EqualTo(10));
            Assert.That(result.Root.Renderable, Is.True);
            Assert.That(result.Root.LengthContext.ViewportWidth, Is.EqualTo(200));
        });
    }

    [Test]
    public void ShadowContentInheritsFromUseAndResolvesContextPaint()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><symbol id='icon' viewBox='0 0 10 10'><g><path style='fill:context-fill;stroke:context-stroke'/></g></symbol><use href='#icon' width='10' height='10' style='fill:#123456;stroke:#abcdef;color:#010203'/></svg>");
        SvgUseResolveOptions options = ViewportOptions(100, 100);

        SvgUseResult result = new SvgUseResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[1], options);
        SvgUseInstance group = result.FindInstance("0/0");
        SvgUseInstance path = result.FindInstance("0/0/0");
        SvgPaint resolvedFill = path.PaintContext.Resolve(path.ComputedStyle.GetPaint("fill"));
        SvgPaint resolvedStroke = path.PaintContext.Resolve(path.ComputedStyle.GetPaint("stroke"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            AssertColor(result.PaintContext.Fill.Color, 0x12, 0x34, 0x56);
            AssertColor(result.PaintContext.Stroke.Color, 0xab, 0xcd, 0xef);
            AssertColor(result.Root.ComputedStyle.GetColor("color"), 0x01, 0x02, 0x03);
            AssertColor(group.ComputedStyle.GetPaint("fill").Color, 0x12, 0x34, 0x56);
            Assert.That(path.ComputedStyle.GetPaint("fill").Kind, Is.EqualTo(SvgPaintKind.ContextFill));
            Assert.That(path.ComputedStyle.GetPaint("stroke").Kind, Is.EqualTo(SvgPaintKind.ContextStroke));
            AssertColor(resolvedFill.Color, 0x12, 0x34, 0x56);
            AssertColor(resolvedStroke.Color, 0xab, 0xcd, 0xef);
        });
    }

    [Test]
    public void StylesheetSelectorsMatchShadowTopologyInsteadOfSourceAncestors()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><g><g id='art'><path/></g></g><use href='#art'/></svg>");
        SvgUseResolveOptions options = new SvgUseResolveOptions();
        options.StyleSheets.Add(Sheet("svg g#art > path { fill:red; } #art > path { stroke:blue; }"));

        SvgUseResult result = new SvgUseResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[1], options);
        SvgUseInstance path = result.FindInstance("0/0");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(path.ComputedStyle.GetPaint("fill").Color.Red, Is.EqualTo(0));
            Assert.That(path.ComputedStyle.GetPaint("fill").Color.Green, Is.EqualTo(0));
            Assert.That(path.ComputedStyle.GetPaint("fill").Color.Blue, Is.EqualTo(0));
            Assert.That(path.ComputedStyle.GetPaint("stroke").Color.Blue, Is.EqualTo(1));
        });
    }

    [Test]
    public void NestedUseGetsDistinctShadowIdentityAndNestedPaintContext()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><g id='outer'><rect/><use href='#inner' style='fill:context-stroke'/></g><g id='inner'><circle style='fill:context-fill'/></g><use href='#outer' style='stroke:#224466'/></svg>");
        SvgNode use = document.DocumentElement.Children[2];

        SvgUseResult result = new SvgUseResolver().ResolveWithNode(use);
        SvgUseInstance nestedUse = result.FindInstance("0/1");
        SvgUseInstance nestedRoot = result.FindInstance("0/1/shadow");
        SvgUseInstance circle = result.FindInstance("0/1/shadow/0");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(nestedUse.Reference.Target, Is.SameAs(document.DocumentElement.Children[1]));
            Assert.That(nestedUse.ShadowRoot, Is.SameAs(nestedRoot));
            Assert.That(nestedRoot.IsInstanceRoot, Is.True);
            Assert.That(nestedRoot.Parent, Is.SameAs(nestedUse));
            Assert.That(nestedRoot.ShadowHostSource, Is.SameAs(document.DocumentElement.Children[0].Children[1]));
            Assert.That(nestedRoot.CorrespondingUseElement, Is.SameAs(use));
            AssertColor(circle.PaintContext.Fill.Color, 0x22, 0x44, 0x66);
            AssertColor(circle.PaintContext.Resolve(circle.ComputedStyle.GetPaint("fill")).Color, 0x22, 0x44, 0x66);
            Assert.That(result.Instances.Select(item => item.InstancePath).Distinct().Count(), Is.EqualTo(result.Instances.Count));
        });
    }

    [Test]
    public void RejectsDirectAncestorAndIndirectRecursiveUseReferences()
    {
        SvgDocument ancestorDocument = Load("<svg xmlns='http://www.w3.org/2000/svg'><g id='loop'><use href='#loop'/></g></svg>");
        SvgNode ancestorUse = ancestorDocument.DocumentElement.Children[0].Children[0];
        SvgDocument indirectDocument = Load("<svg xmlns='http://www.w3.org/2000/svg'><g id='a'><use href='#b'/></g><g id='b'><use href='#a'/></g><use href='#a'/></svg>");

        SvgUseResult ancestor = new SvgUseResolver().ResolveWithNode(ancestorUse);
        SvgUseResult indirect = new SvgUseResolver().ResolveWithNode(indirectDocument.DocumentElement.Children[2]);

        Assert.Multiple(() =>
        {
            Assert.That(ancestor.Success, Is.False);
            Assert.That(ancestor.Root, Is.Null);
            Assert.That(ancestor.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.ReferenceCycle));
            Assert.That(ancestor.Diagnostics.Single().InstancePath, Is.EqualTo("host"));
            Assert.That(indirect.Success, Is.False);
            Assert.That(indirect.Root, Is.Not.Null);
            Assert.That(indirect.Diagnostics.Any(item => item.Code == SvgDiagnosticCode.ReferenceCycle), Is.True);
            Assert.That(indirect.Diagnostics.Single(item => item.Code == SvgDiagnosticCode.ReferenceCycle).InstancePath, Is.EqualTo("0/0/shadow/0"));
        });
    }

    [Test]
    public void EnforcesInstanceAndReferenceDepthLimitsWithLocatedPaths()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><g id='a'><rect/><circle/></g><use href='#a'/></svg>");
        SvgUseResolveOptions instanceOptions = new SvgUseResolveOptions();
        instanceOptions.Limits.MaxUseInstances = 2;
        SvgDocument nestedDocument = Load("<svg xmlns='http://www.w3.org/2000/svg'><g id='a'><use href='#b'/></g><g id='b'><rect/></g><use href='#a'/></svg>");
        SvgUseResolveOptions depthOptions = new SvgUseResolveOptions();
        depthOptions.Limits.MaxDepth = 1;

        SvgUseResult instances = new SvgUseResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[1], instanceOptions);
        SvgUseResult depth = new SvgUseResolver().ResolveWithNodeAndOptions(nestedDocument.DocumentElement.Children[2], depthOptions);

        Assert.Multiple(() =>
        {
            Assert.That(instances.Success, Is.False);
            Assert.That(instances.Instances, Has.Count.EqualTo(2));
            Assert.That(instances.Diagnostics.Any(item => item.Code == SvgDiagnosticCode.InputLimitExceeded && item.InstancePath == "0/1"), Is.True);
            Assert.That(depth.Success, Is.False);
            Assert.That(depth.Diagnostics.Any(item => item.Code == SvgDiagnosticCode.InputLimitExceeded && item.InstancePath == "0/0"), Is.True);
        });
    }

    [Test]
    public void CapturedSourceVersionsExposeStaleExpansions()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><g id='art'><rect/></g><use href='#art'/></svg>");
        SvgNode sourceRect = document.DocumentElement.Children[0].Children[0];
        SvgUseResult result = new SvgUseResolver().ResolveWithNode(document.DocumentElement.Children[1]);

        sourceRect.MarkDirty();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.FindSource("0/0").SourceVersion, Is.LessThan(sourceRect.Version));
            Assert.That(result.IsCurrent(), Is.False);
        });
    }

    static SvgUseResolveOptions ViewportOptions(double width, double height)
    {
        SvgUseResolveOptions options = new SvgUseResolveOptions();
        options.LengthContext.HasViewport = true;
        options.LengthContext.ViewportWidth = width;
        options.LengthContext.ViewportHeight = height;
        return options;
    }

    static void AssertColor(SvgColor color, int red, int green, int blue)
    {
        Assert.That(color.Space, Is.EqualTo(SvgColorSpace.Srgb));
        Assert.That(color.Red, Is.EqualTo(red / 255.0).Within(1e-12));
        Assert.That(color.Green, Is.EqualTo(green / 255.0).Within(1e-12));
        Assert.That(color.Blue, Is.EqualTo(blue / 255.0).Within(1e-12));
        Assert.That(color.Alpha, Is.EqualTo(1));
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True);
        return result.Document;
    }

    static SvgStyleSheet Sheet(string css)
    {
        SvgCssParseResult result = new SvgCssParser().ParseStyleSheetWithValueAndLimitsAndOriginAndSource(css, new SvgCssLimits(), SvgStyleOrigin.Author, new SvgSourceUri("use.css"));
        Assert.That(result.Success, Is.True);
        return result.StyleSheet;
    }
}
