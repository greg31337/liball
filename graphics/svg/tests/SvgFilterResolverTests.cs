namespace svg.Tests;

public class SvgFilterResolverTests
{
    [Test]
    public void DefaultsFilterRegionAndChainsImplicitPrimitiveInputs()
    {
        SvgNode filter = Load("<svg xmlns='http://www.w3.org/2000/svg'><filter><feGaussianBlur result='blur'/><feOffset/></filter></svg>").DocumentElement.Children[0];

        SvgFilterResult result = new SvgFilterResolver().ResolveWithNodeAndOptions(filter, BoundsOptions(10, 20, 100, 50));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.FilterUnits, Is.EqualTo(SvgPaintServerUnits.ObjectBoundingBox));
            Assert.That(result.PrimitiveUnits, Is.EqualTo(SvgPaintServerUnits.UserSpaceOnUse));
            Assert.That(result.Region.X, Is.EqualTo(-0.1));
            Assert.That(result.Region.Y, Is.EqualTo(-0.1));
            Assert.That(result.Region.Width, Is.EqualTo(1.2));
            Assert.That(result.Region.Height, Is.EqualTo(1.2));
            Assert.That(result.UserRegion.X, Is.Zero.Within(1e-12));
            Assert.That(result.UserRegion.Y, Is.EqualTo(15).Within(1e-12));
            Assert.That(result.UserRegion.Width, Is.EqualTo(120).Within(1e-12));
            Assert.That(result.UserRegion.Height, Is.EqualTo(60).Within(1e-12));
            Assert.That(result.Primitives, Has.Count.EqualTo(2));
            Assert.That(result.Primitives[0].Inputs.Single().Kind, Is.EqualTo(SvgFilterInputKind.Standard));
            Assert.That(result.Primitives[0].Inputs.Single().StandardInput, Is.EqualTo(SvgFilterStandardInput.SourceGraphic));
            Assert.That(result.Primitives[1].Inputs.Single().Kind, Is.EqualTo(SvgFilterInputKind.Previous));
            Assert.That(result.Primitives[1].Inputs.Single().Primitive, Is.SameAs(result.Primitives[0]));
            Assert.That(result.Primitives[1].UserRegion.X, Is.EqualTo(result.UserRegion.X));
            Assert.That(result.Primitives[1].UserRegion.Width, Is.EqualTo(result.UserRegion.Width));
            Assert.That(result.Renderable, Is.True);
        });
    }

    [Test]
    public void ResolvesStandardInputsAndClosestPrecedingNamedResults()
    {
        SvgNode filter = Load("<svg xmlns='http://www.w3.org/2000/svg'><filter><feFlood result='paint'/><feOffset result='dup'/><feGaussianBlur result='dup'/><feBlend in='dup' in2='SourceAlpha'/><feComposite in='future'/><feFlood result='future'/></filter></svg>").DocumentElement.Children[0];

        SvgFilterResult result = new SvgFilterResolver().ResolveWithNodeAndOptions(filter, BoundsOptions(0, 0, 100, 100));
        SvgFilterPrimitive blend = result.Primitives[3];
        SvgFilterPrimitive forward = result.Primitives[4];

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(blend.Inputs[0].Kind, Is.EqualTo(SvgFilterInputKind.NamedResult));
            Assert.That(blend.Inputs[0].Primitive, Is.SameAs(result.Primitives[2]));
            Assert.That(blend.Inputs[1].Kind, Is.EqualTo(SvgFilterInputKind.Standard));
            Assert.That(blend.Inputs[1].StandardInput, Is.EqualTo(SvgFilterStandardInput.SourceAlpha));
            Assert.That(forward.Inputs[0].Kind, Is.EqualTo(SvgFilterInputKind.TransparentBlack));
            Assert.That(result.FindResult("dup", 4), Is.SameAs(result.Primitives[2]));
            Assert.That(result.Diagnostics.Any(item => item.Severity == SvgDiagnosticSeverity.Warning && item.Code == SvgDiagnosticCode.InvalidReference), Is.True);
        });
    }

    [Test]
    public void MergeNodesResolveNamedAndImplicitPreviousInputsInOrder()
    {
        SvgNode filter = Load("<svg xmlns='http://www.w3.org/2000/svg'><filter><feFlood result='bottom'/><feOffset result='top'/><feMerge><feMergeNode in='bottom'/><feMergeNode/></feMerge></filter></svg>").DocumentElement.Children[0];

        SvgFilterResult result = new SvgFilterResolver().ResolveWithNodeAndOptions(filter, BoundsOptions(0, 0, 100, 100));
        SvgFilterPrimitive merge = result.Primitives[2];

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(merge.Inputs, Has.Count.EqualTo(2));
            Assert.That(merge.Inputs[0].Kind, Is.EqualTo(SvgFilterInputKind.NamedResult));
            Assert.That(merge.Inputs[0].Primitive, Is.SameAs(result.Primitives[0]));
            Assert.That(merge.Inputs[1].Kind, Is.EqualTo(SvgFilterInputKind.Previous));
            Assert.That(merge.Inputs[1].Primitive, Is.SameAs(result.Primitives[1]));
        });
    }

    [Test]
    public void FilterHrefInheritsAttributesAndPrimitiveListAcrossChain()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><filter id='base' filterUnits='userSpaceOnUse' x='5'><feGaussianBlur result='baseResult'/></filter><filter id='middle' href='#base' primitiveUnits='objectBoundingBox' y='10'/><filter id='paint' href='#middle' width='80' height='60'/></svg>");
        SvgFilterResolveOptions options = BoundsOptions(10, 20, 200, 100);
        options.StyleSheets.Add(Sheet("#base > feGaussianBlur { color-interpolation-filters:linearRGB; } #paint > feGaussianBlur { color-interpolation-filters:sRGB; }"));

        SvgFilterResult result = new SvgFilterResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[2], options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.TemplateChain, Is.EqualTo(new[] { document.DocumentElement.Children[2], document.DocumentElement.Children[1], document.DocumentElement.Children[0] }));
            Assert.That(result.References, Has.Count.EqualTo(2));
            Assert.That(result.ContentSource, Is.SameAs(document.DocumentElement.Children[0]));
            Assert.That(result.FilterUnits, Is.EqualTo(SvgPaintServerUnits.UserSpaceOnUse));
            Assert.That(result.PrimitiveUnits, Is.EqualTo(SvgPaintServerUnits.ObjectBoundingBox));
            Assert.That(result.Region.X, Is.EqualTo(5));
            Assert.That(result.Region.Y, Is.EqualTo(10));
            Assert.That(result.Region.Width, Is.EqualTo(80));
            Assert.That(result.Region.Height, Is.EqualTo(60));
            Assert.That(result.FindAttribute(SvgAttributeNames.X).SourceNode, Is.SameAs(document.DocumentElement.Children[0]));
            Assert.That(result.FindAttribute(SvgAttributeNames.Width).SourceNode, Is.SameAs(document.DocumentElement.Children[2]));
            Assert.That(result.Primitives.Single().SourceNode, Is.SameAs(document.DocumentElement.Children[0].Children[0]));
            Assert.That(result.Primitives.Single().ColorSpace, Is.EqualTo(SvgFilterColorSpace.Srgb));
        });
    }

    [Test]
    public void LocalPrimitivesOverrideTemplateContentAndCompleteFiltersIgnoreUnusedHref()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><filter id='base'><feFlood/></filter><filter id='local' href='#base'><feOffset/></filter><filter id='complete' href='#complete' filterUnits='userSpaceOnUse' primitiveUnits='userSpaceOnUse' x='0' y='0' width='10' height='10'><feGaussianBlur/></filter></svg>");

        SvgFilterResult local = new SvgFilterResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[1], BoundsOptions(0, 0, 100, 100));
        SvgFilterResult complete = new SvgFilterResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[2], UserOptions(100, 100));

        Assert.Multiple(() =>
        {
            Assert.That(local.Success, Is.True);
            Assert.That(local.ContentSource, Is.SameAs(document.DocumentElement.Children[1]));
            Assert.That(local.Primitives.Single().ElementKind, Is.EqualTo(SvgElementKind.FeOffset));
            Assert.That(complete.Success, Is.True);
            Assert.That(complete.References, Is.Empty);
            Assert.That(complete.TemplateChain, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void ResolvesExternalFilterTemplatesWithBaseUriProvenance()
    {
        SvgDocument external = Load("<svg xmlns='http://www.w3.org/2000/svg'><filter id='remote' filterUnits='userSpaceOnUse'><feFlood/></filter></svg>", "https://example.test/assets/filters.svg");
        SvgDocument source = Load("<svg xmlns='http://www.w3.org/2000/svg'><filter href='assets/filters.svg#remote'/></svg>", "https://example.test/root.svg");
        SvgFilterResolveOptions options = UserOptions(100, 100);
        options.ReferenceOptions.ExternalDocument = external;

        SvgFilterResult result = new SvgFilterResolver().ResolveWithNodeAndOptions(source.DocumentElement.Children[0], options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.References.Single().ResolvedUri, Is.EqualTo("https://example.test/assets/filters.svg#remote"));
            Assert.That(result.ContentSource, Is.SameAs(external.DocumentElement.Children[0]));
            Assert.That(result.FilterUnits, Is.EqualTo(SvgPaintServerUnits.UserSpaceOnUse));
        });
    }

    [Test]
    public void DetectsFilterHrefCyclesIncompatibleTargetsAndDepthLimits()
    {
        SvgDocument cycleDocument = Load("<svg xmlns='http://www.w3.org/2000/svg'><filter id='a' href='#b'/><filter id='b' href='#a'/></svg>");
        SvgDocument incompatibleDocument = Load("<svg xmlns='http://www.w3.org/2000/svg'><clipPath id='clip'/><filter href='#clip'/></svg>");
        SvgDocument depthDocument = Load("<svg xmlns='http://www.w3.org/2000/svg'><filter id='a' href='#b'/><filter id='b' href='#c'/><filter id='c'><feFlood/></filter></svg>");
        SvgFilterResolveOptions depthOptions = BoundsOptions(0, 0, 100, 100);
        depthOptions.ReferenceLimits.MaxEffectDepth = 1;

        SvgFilterResult cycle = new SvgFilterResolver().ResolveWithNodeAndOptions(cycleDocument.DocumentElement.Children[0], BoundsOptions(0, 0, 100, 100));
        SvgFilterResult incompatible = new SvgFilterResolver().ResolveWithNodeAndOptions(incompatibleDocument.DocumentElement.Children[1], BoundsOptions(0, 0, 100, 100));
        SvgFilterResult depth = new SvgFilterResolver().ResolveWithNodeAndOptions(depthDocument.DocumentElement.Children[0], depthOptions);

        Assert.Multiple(() =>
        {
            Assert.That(cycle.Success, Is.False);
            Assert.That(cycle.Diagnostics.Any(item => item.Code == SvgDiagnosticCode.ReferenceCycle), Is.True);
            Assert.That(incompatible.Success, Is.False);
            Assert.That(incompatible.Diagnostics.Any(item => item.Code == SvgDiagnosticCode.InvalidReference), Is.True);
            Assert.That(depth.Success, Is.False);
            Assert.That(depth.Diagnostics.Any(item => item.Code == SvgDiagnosticCode.InputLimitExceeded), Is.True);
        });
    }

    [Test]
    public void ConvertsUserFilterRegionAndObjectBoundingBoxPrimitiveSubregions()
    {
        SvgNode filter = Load("<svg xmlns='http://www.w3.org/2000/svg'><filter filterUnits='userSpaceOnUse' primitiveUnits='objectBoundingBox' x='10' y='20' width='40' height='30'><feFlood x='10%' y='20%' width='50%' height='60%'/></filter></svg>").DocumentElement.Children[0];

        SvgFilterResult result = new SvgFilterResolver().ResolveWithNodeAndOptions(filter, BoundsOptions(100, 50, 200, 100));
        SvgFilterPrimitive primitive = result.Primitives.Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.UserRegion.X, Is.EqualTo(10));
            Assert.That(result.UserRegion.Y, Is.EqualTo(20));
            Assert.That(primitive.Region.X, Is.EqualTo(0.1));
            Assert.That(primitive.Region.Y, Is.EqualTo(0.2));
            Assert.That(primitive.UserRegion.X, Is.EqualTo(120));
            Assert.That(primitive.UserRegion.Y, Is.EqualTo(70));
            Assert.That(primitive.UserRegion.Width, Is.EqualTo(100));
            Assert.That(primitive.UserRegion.Height, Is.EqualTo(60));
        });
    }

    [Test]
    public void EmptyNonpositiveAndDegenerateBoundingBoxFiltersDoNotRender()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><filter id='empty'/><filter id='zero' width='0'><feFlood/></filter><filter id='negative' height='-1'><feFlood/></filter><filter id='box'><feFlood/></filter><filter id='primitive'><feFlood width='0'/></filter><filter id='absolute' x='2px'><feFlood/></filter></svg>");

        SvgFilterResult empty = new SvgFilterResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[0], BoundsOptions(0, 0, 100, 100));
        SvgFilterResult zero = new SvgFilterResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[1], BoundsOptions(0, 0, 100, 100));
        SvgFilterResult negative = new SvgFilterResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[2], BoundsOptions(0, 0, 100, 100));
        SvgFilterResult degenerate = new SvgFilterResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[3], BoundsOptions(0, 0, 0, 100));
        SvgFilterResult primitive = new SvgFilterResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[4], BoundsOptions(0, 0, 100, 100));
        SvgFilterResult absolute = new SvgFilterResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[5], BoundsOptions(0, 0, 100, 100));

        Assert.Multiple(() =>
        {
            Assert.That(empty.Success, Is.True);
            Assert.That(empty.Empty, Is.True);
            Assert.That(empty.Renderable, Is.False);
            Assert.That(zero.Success, Is.True);
            Assert.That(zero.Renderable, Is.False);
            Assert.That(negative.Success, Is.True);
            Assert.That(negative.Region, Is.Null);
            Assert.That(negative.Renderable, Is.False);
            Assert.That(degenerate.Success, Is.True);
            Assert.That(degenerate.Renderable, Is.False);
            Assert.That(primitive.Success, Is.True);
            Assert.That(primitive.Primitives.Single().Enabled, Is.False);
            Assert.That(primitive.Renderable, Is.False);
            Assert.That(absolute.Success, Is.False);
            Assert.That(absolute.Diagnostics.Any(item => item.Message.Contains("numbers and percentages")), Is.True);
        });
    }

    [Test]
    public void PrimitiveColorInterpolationUsesInheritedDefaultAndOverrides()
    {
        SvgNode filter = Load("<svg xmlns='http://www.w3.org/2000/svg'><filter color-interpolation-filters='sRGB'><feGaussianBlur/><feColorMatrix color-interpolation-filters='auto'/><feComposite color-interpolation-filters='linearRGB'/></filter></svg>").DocumentElement.Children[0];

        SvgFilterResult result = new SvgFilterResolver().ResolveWithNodeAndOptions(filter, BoundsOptions(0, 0, 100, 100));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Primitives.Select(item => item.ColorSpace), Is.EqualTo(new[] { SvgFilterColorSpace.Srgb, SvgFilterColorSpace.Auto, SvgFilterColorSpace.LinearRgb }));
        });
    }

    [Test]
    public void ParsesPrimitiveDefaultsAndTypedParameters()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><image id='source'/><filter><feBlend/><feComposite/><feDropShadow/><feTurbulence/><feConvolveMatrix order='2 2' kernelMatrix='1 2 3 4'/><feComponentTransfer><feFuncR slope='.5'/></feComponentTransfer><feDiffuseLighting><feDistantLight azimuth='45'/></feDiffuseLighting><feImage href='#source'/></filter></svg>");
        SvgNode filter = document.DocumentElement.Children[1];

        SvgFilterResult result = new SvgFilterResolver().ResolveWithNodeAndOptions(filter, BoundsOptions(0, 0, 100, 100));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Primitives[0].FindAttribute(SvgAttributeNames.Mode).TypedValue.EffectiveText, Is.EqualTo("normal"));
            Assert.That(result.Primitives[1].FindAttribute(SvgAttributeNames.Operator).TypedValue.EffectiveText, Is.EqualTo("over"));
            Assert.That(result.Primitives[2].FindAttribute(SvgAttributeNames.Dx).TypedValue.EffectiveText, Is.EqualTo("2"));
            Assert.That((int)result.Primitives[3].FindAttribute(SvgAttributeNames.NumOctaves).TypedValue.Value, Is.EqualTo(1));
            Assert.That((int[])result.Primitives[4].FindAttribute(SvgAttributeNames.Order).TypedValue.Value, Is.EqualTo(new[] { 2, 2 }));
            Assert.That((double[])result.Primitives[4].FindAttribute(SvgAttributeNames.KernelMatrix).TypedValue.Value, Is.EqualTo(new[] { 1.0, 2.0, 3.0, 4.0 }));
            Assert.That(result.Primitives[5].Children.Single().ElementKind, Is.EqualTo(SvgElementKind.FeFuncR));
            Assert.That(result.Primitives[5].Children.Single().FindAttribute(SvgAttributeNames.Type).TypedValue.EffectiveText, Is.EqualTo("identity"));
            Assert.That((double)result.Primitives[5].Children.Single().FindAttribute(SvgAttributeNames.Slope).TypedValue.Value, Is.EqualTo(0.5));
            Assert.That(result.Primitives[6].Children.Single().ElementKind, Is.EqualTo(SvgElementKind.FeDistantLight));
            Assert.That((SvgAngle)result.Primitives[6].Children.Single().FindAttribute(SvgAttributeNames.Azimuth).TypedValue.Value, Is.Not.Null);
            Assert.That(result.Primitives[7].Reference.Success, Is.True);
            Assert.That(result.Primitives[7].Reference.Target, Is.SameAs(document.DocumentElement.Children[0]));
        });
    }

    [Test]
    public void EnforcesPrimitiveSpecificKernelBlurAndTurbulenceLimits()
    {
        SvgNode invalid = Load("<svg xmlns='http://www.w3.org/2000/svg'><filter><feGaussianBlur stdDeviation='-1'/><feMorphology radius='-2'/><feTurbulence baseFrequency='-.1' numOctaves='0'/><feConvolveMatrix order='2 2' kernelMatrix='1 2'/></filter></svg>").DocumentElement.Children[0];
        SvgNode limited = Load("<svg xmlns='http://www.w3.org/2000/svg'><filter><feGaussianBlur stdDeviation='11'/><feTurbulence numOctaves='4'/><feConvolveMatrix order='3 3' kernelMatrix='1 1 1 1 1 1 1 1 1'/></filter></svg>").DocumentElement.Children[0];
        SvgFilterResolveOptions limitOptions = BoundsOptions(0, 0, 100, 100);
        limitOptions.FilterLimits.MaxBlurStandardDeviation = 10;
        limitOptions.FilterLimits.MaxTurbulenceOctaves = 3;
        limitOptions.FilterLimits.MaxKernelElements = 8;

        SvgFilterResult invalidResult = new SvgFilterResolver().ResolveWithNodeAndOptions(invalid, BoundsOptions(0, 0, 100, 100));
        SvgFilterResult limitedResult = new SvgFilterResolver().ResolveWithNodeAndOptions(limited, limitOptions);

        Assert.Multiple(() =>
        {
            Assert.That(invalidResult.Success, Is.False);
            Assert.That(invalidResult.Diagnostics.Count(item => item.Code == SvgDiagnosticCode.InvalidValue), Is.GreaterThanOrEqualTo(4));
            Assert.That(limitedResult.Success, Is.False);
            Assert.That(limitedResult.Diagnostics.Count(item => item.Code == SvgDiagnosticCode.InputLimitExceeded), Is.EqualTo(3));
        });
    }

    [Test]
    public void EnforcesPrimitiveCountAndFilterAreaLimits()
    {
        SvgNode primitives = Load("<svg xmlns='http://www.w3.org/2000/svg'><filter><feFlood/><feFlood/><feFlood/></filter></svg>").DocumentElement.Children[0];
        SvgNode area = Load("<svg xmlns='http://www.w3.org/2000/svg'><filter><feFlood/></filter></svg>").DocumentElement.Children[0];
        SvgFilterResolveOptions primitiveOptions = BoundsOptions(0, 0, 100, 100);
        primitiveOptions.FilterLimits.MaxPrimitives = 2;
        SvgFilterResolveOptions areaOptions = BoundsOptions(0, 0, 100, 100);
        areaOptions.FilterLimits.MaxAreaPixels = 1000;

        SvgFilterResult primitiveResult = new SvgFilterResolver().ResolveWithNodeAndOptions(primitives, primitiveOptions);
        SvgFilterResult areaResult = new SvgFilterResolver().ResolveWithNodeAndOptions(area, areaOptions);

        Assert.Multiple(() =>
        {
            Assert.That(primitiveResult.Success, Is.False);
            Assert.That(primitiveResult.Diagnostics.Any(item => item.Message.Contains("MaxPrimitives")), Is.True);
            Assert.That(areaResult.Success, Is.False);
            Assert.That(areaResult.Diagnostics.Any(item => item.Message.Contains("MaxAreaPixels")), Is.True);
        });
    }

    [Test]
    public void TemplateAndPrimitiveMutationsInvalidateResolvedSnapshot()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><filter id='base'><feFlood/></filter><filter href='#base'/></svg>");
        SvgFilterResult result = new SvgFilterResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[1], BoundsOptions(0, 0, 100, 100));

        document.DocumentElement.Children[0].Children[0].MarkDirty();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.IsCurrent(), Is.False);
        });
    }

    static SvgFilterResolveOptions BoundsOptions(double x, double y, double width, double height)
    {
        SvgFilterResolveOptions options = UserOptions(width, height);
        options.TargetBounds = SvgRect.Create(x, y, width, height, width == 0 || height == 0);
        return options;
    }

    static SvgFilterResolveOptions UserOptions(double width, double height)
    {
        SvgFilterResolveOptions options = new SvgFilterResolveOptions();
        options.LengthContext.HasViewport = true;
        options.LengthContext.ViewportWidth = width;
        options.LengthContext.ViewportHeight = height;
        return options;
    }

    static SvgDocument Load(string source)
    {
        return Load(source, "");
    }

    static SvgDocument Load(string source, string baseUri)
    {
        SvgLoadOptions options = new SvgLoadOptions();
        options.BaseUri = baseUri;
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, options);
        Assert.That(result.Success, Is.True);
        return result.Document;
    }

    static SvgStyleSheet Sheet(string css)
    {
        SvgCssParseResult result = new SvgCssParser().ParseStyleSheetWithValueAndLimitsAndOriginAndSource(css, new SvgCssLimits(), SvgStyleOrigin.Author, new SvgSourceUri("filter.css"));
        Assert.That(result.Success, Is.True);
        return result.StyleSheet;
    }
}
