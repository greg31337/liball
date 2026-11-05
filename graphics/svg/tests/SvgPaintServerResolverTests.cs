namespace svg.Tests;

public class SvgPaintServerResolverTests
{
    [Test]
    public void LinearGradientInheritsAttributesAndStopsAcrossTemplateChain()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><linearGradient id='base' x2='75%' spreadMethod='reflect'><stop offset='0' stop-color='red'/><stop offset='1' stop-color='blue'/></linearGradient><linearGradient id='middle' href='#base' y1='10%'/><linearGradient id='paint' href='#middle' x1='25%'/></svg>");
        SvgNode paint = document.DocumentElement.Children[2];
        SvgPaintServerResolveOptions options = BoundsOptions(10, 20, 200, 100);

        SvgPaintServerResult result = new SvgPaintServerResolver().ResolveWithNodeAndOptions(paint, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Kind, Is.EqualTo(SvgPaintServerKind.LinearGradient));
            Assert.That(result.TemplateChain, Is.EqualTo(new[] { document.DocumentElement.Children[2], document.DocumentElement.Children[1], document.DocumentElement.Children[0] }));
            Assert.That(result.References, Has.Count.EqualTo(2));
            Assert.That(result.ContentSource, Is.SameAs(document.DocumentElement.Children[0]));
            Assert.That(result.Stops, Has.Count.EqualTo(2));
            Assert.That(result.GradientUnits, Is.EqualTo(SvgPaintServerUnits.ObjectBoundingBox));
            Assert.That(result.SpreadMethod, Is.EqualTo(SvgGradientSpreadMethod.Reflect));
            Assert.That(result.Start.X, Is.EqualTo(0.25));
            Assert.That(result.Start.Y, Is.EqualTo(0.1));
            Assert.That(result.End.X, Is.EqualTo(0.75));
            Assert.That(result.End.Y, Is.Zero);
            Assert.That(result.UserStart.X, Is.EqualTo(60));
            Assert.That(result.UserStart.Y, Is.EqualTo(30));
            Assert.That(result.UserEnd.X, Is.EqualTo(160));
            Assert.That(result.UserEnd.Y, Is.EqualTo(20));
            Assert.That(result.FindAttribute("x1").SourceNode, Is.SameAs(paint));
            Assert.That(result.FindAttribute("y1").SourceNode, Is.SameAs(document.DocumentElement.Children[1]));
            Assert.That(result.FindAttribute("x2").SourceNode, Is.SameAs(document.DocumentElement.Children[0]));
            Assert.That(result.FindAttribute("y2").Defaulted, Is.True);
            Assert.That(result.GradientPaintKind, Is.EqualTo(SvgGradientPaintKind.Gradient));
            Assert.That(result.Renderable, Is.True);
            Assert.That(result.IsCurrent(), Is.True);
        });
    }

    [Test]
    public void GradientStopsClampOffsetsEnforceOrderAndCombineOpacity()
    {
        SvgNode gradient = Load("<svg xmlns='http://www.w3.org/2000/svg'><linearGradient gradientUnits='userSpaceOnUse'><stop offset='-20%' stop-color='#102030'/><stop offset='.6' stop-color='#405060' stop-opacity='.5'/><stop offset='.3' stop-color='#708090'/><stop offset='150%' stop-color='rgba(1 2 3 / 25%)' stop-opacity='.5'/></linearGradient></svg>").DocumentElement.Children[0];
        SvgPaintServerResolveOptions options = UserSpaceOptions(200, 100);

        SvgPaintServerResult result = new SvgPaintServerResolver().ResolveWithNodeAndOptions(gradient, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Stops.Select(stop => stop.AuthoredOffset), Is.EqualTo(new[] { -0.2, 0.6, 0.3, 1.5 }).Within(1e-12));
            Assert.That(result.Stops.Select(stop => stop.Offset), Is.EqualTo(new[] { 0.0, 0.6, 0.6, 1.0 }).Within(1e-12));
            Assert.That(result.Stops[1].Opacity, Is.EqualTo(0.5));
            Assert.That(result.Stops[3].Color.Alpha, Is.EqualTo(0.25));
            Assert.That(result.Stops[3].EffectiveAlpha, Is.EqualTo(0.125));
            Assert.That(result.Stops[0].Source, Is.SameAs(gradient.Children[0].Attributes[0].SourceSpan));
        });
    }

    [Test]
    public void InheritedStopsMatchCurrentTemplateHostForStyleScoping()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><linearGradient id='base'><stop/><stop offset='1'/></linearGradient><linearGradient id='paint' href='#base'/></svg>");
        SvgPaintServerResolveOptions options = BoundsOptions(0, 0, 100, 100);
        options.StyleSheets.Add(Sheet("#base > stop { stop-color:red; } #paint > stop { stop-color:blue; }"));

        SvgPaintServerResult result = new SvgPaintServerResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[1], options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.ContentSource, Is.SameAs(document.DocumentElement.Children[0]));
            Assert.That(result.Stops.All(stop => stop.Color.Blue == 1), Is.True);
            Assert.That(result.Stops.All(stop => stop.Color.Red == 0), Is.True);
        });
    }

    [Test]
    public void SingleStopAndDegenerateLinearGradientsUseSolidTerminalColor()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><linearGradient id='one' gradientUnits='userSpaceOnUse'><stop stop-color='green' stop-opacity='.25'/></linearGradient><linearGradient id='flat' gradientUnits='userSpaceOnUse' x1='5' y1='6' x2='5' y2='6'><stop stop-color='red'/><stop stop-color='blue' stop-opacity='.5'/></linearGradient></svg>");
        SvgPaintServerResolveOptions options = UserSpaceOptions(100, 100);

        SvgPaintServerResult one = new SvgPaintServerResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[0], options);
        SvgPaintServerResult flat = new SvgPaintServerResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[1], options);

        Assert.Multiple(() =>
        {
            Assert.That(one.Success, Is.True);
            Assert.That(one.GradientPaintKind, Is.EqualTo(SvgGradientPaintKind.Solid));
            Assert.That(one.SolidColor.Green, Is.GreaterThan(0));
            Assert.That(one.SolidAlpha, Is.EqualTo(0.25));
            Assert.That(flat.Success, Is.True);
            Assert.That(flat.GradientPaintKind, Is.EqualTo(SvgGradientPaintKind.Solid));
            Assert.That(flat.SolidColor.Blue, Is.EqualTo(1));
            Assert.That(flat.SolidAlpha, Is.EqualTo(0.5));
        });
    }

    [Test]
    public void RadialGradientAppliesInheritedFocalValuesAndCurrentCenterDefaults()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><radialGradient id='base' cx='20%' fx='80%' fr='10%'><stop/><stop offset='1'/></radialGradient><radialGradient id='paint' href='#base' cy='30%' r='60%'/></svg>");
        SvgPaintServerResolveOptions options = BoundsOptions(10, 20, 200, 100);

        SvgPaintServerResult result = new SvgPaintServerResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[1], options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Kind, Is.EqualTo(SvgPaintServerKind.RadialGradient));
            Assert.That(result.Center.X, Is.EqualTo(0.2));
            Assert.That(result.Center.Y, Is.EqualTo(0.3));
            Assert.That(result.Focus.X, Is.EqualTo(0.8));
            Assert.That(result.Focus.Y, Is.EqualTo(0.3));
            Assert.That(result.Radius, Is.EqualTo(0.6));
            Assert.That(result.FocalRadius, Is.EqualTo(0.1));
            Assert.That(result.UserCenter.X, Is.EqualTo(50));
            Assert.That(result.UserCenter.Y, Is.EqualTo(50));
            Assert.That(result.UserFocus.X, Is.EqualTo(170));
            Assert.That(result.UserFocus.Y, Is.EqualTo(50));
            Assert.That(result.FindAttribute("fy").Defaulted, Is.True);
            Assert.That(result.FindAttribute("fy").SourceNode, Is.SameAs(document.DocumentElement.Children[1]));
        });
    }

    [Test]
    public void CrossKindGradientTemplateSharesCommonAttributesAndStopsOnly()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><radialGradient id='base' gradientUnits='userSpaceOnUse' spreadMethod='repeat' cx='12'><stop stop-color='red'/><stop offset='1' stop-color='blue'/></radialGradient><linearGradient href='#base'/></svg>");
        SvgPaintServerResolveOptions options = UserSpaceOptions(100, 100);

        SvgPaintServerResult result = new SvgPaintServerResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[1], options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.TemplateChain, Has.Count.EqualTo(2));
            Assert.That(result.GradientUnits, Is.EqualTo(SvgPaintServerUnits.UserSpaceOnUse));
            Assert.That(result.SpreadMethod, Is.EqualTo(SvgGradientSpreadMethod.Repeat));
            Assert.That(result.Stops, Has.Count.EqualTo(2));
            Assert.That(result.Start.X, Is.Zero);
            Assert.That(result.End.X, Is.EqualTo(100));
            Assert.That(result.FindAttribute("x1").Defaulted, Is.True);
            Assert.That(result.FindAttribute("cx"), Is.Null);
        });
    }

    [Test]
    public void RejectsNegativeRadialRadiiAndInvalidObjectBoundingBoxUnits()
    {
        SvgNode negative = Load("<svg xmlns='http://www.w3.org/2000/svg'><radialGradient gradientUnits='userSpaceOnUse' r='-1'><stop/><stop offset='1'/></radialGradient></svg>").DocumentElement.Children[0];
        SvgNode absoluteBox = Load("<svg xmlns='http://www.w3.org/2000/svg'><linearGradient x1='2px'><stop/><stop offset='1'/></linearGradient></svg>").DocumentElement.Children[0];

        SvgPaintServerResult negativeResult = new SvgPaintServerResolver().ResolveWithNodeAndOptions(negative, UserSpaceOptions(100, 100));
        SvgPaintServerResult absoluteResult = new SvgPaintServerResolver().ResolveWithNodeAndOptions(absoluteBox, BoundsOptions(0, 0, 100, 100));

        Assert.Multiple(() =>
        {
            Assert.That(negativeResult.Success, Is.False);
            Assert.That(negativeResult.Diagnostics.Any(item => item.Code == SvgDiagnosticCode.InvalidValue), Is.True);
            Assert.That(negativeResult.Renderable, Is.False);
            Assert.That(absoluteResult.Success, Is.False);
            Assert.That(absoluteResult.Diagnostics.Any(item => item.Message.Contains("numbers or percentages")), Is.True);
        });
    }

    [Test]
    public void PatternInheritsTileGeometryViewBoxAndNonDescriptiveContent()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><pattern id='base' viewBox='0 0 10 10'><title>template</title><rect width='10' height='10'/></pattern><pattern id='middle' href='#base' width='25%' height='50%' patternContentUnits='objectBoundingBox'/><pattern id='paint' href='#middle' x='10%' y='20%'/></svg>");
        SvgPaintServerResolveOptions options = BoundsOptions(100, 50, 200, 100);

        SvgPaintServerResult result = new SvgPaintServerResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[2], options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Kind, Is.EqualTo(SvgPaintServerKind.Pattern));
            Assert.That(result.TemplateChain, Is.EqualTo(new[] { document.DocumentElement.Children[2], document.DocumentElement.Children[1], document.DocumentElement.Children[0] }));
            Assert.That(result.ContentSource, Is.SameAs(document.DocumentElement.Children[0]));
            Assert.That(result.Content, Has.Count.EqualTo(1));
            Assert.That(result.Content[0].ElementKind, Is.EqualTo(SvgElementKind.Rect));
            Assert.That(result.PatternUnits, Is.EqualTo(SvgPaintServerUnits.ObjectBoundingBox));
            Assert.That(result.PatternContentUnits, Is.EqualTo(SvgPaintServerUnits.ObjectBoundingBox));
            Assert.That(result.TileBounds.X, Is.EqualTo(0.1));
            Assert.That(result.TileBounds.Y, Is.EqualTo(0.2));
            Assert.That(result.TileBounds.Width, Is.EqualTo(0.25));
            Assert.That(result.TileBounds.Height, Is.EqualTo(0.5));
            Assert.That(result.UserTileBounds.X, Is.EqualTo(120));
            Assert.That(result.UserTileBounds.Y, Is.EqualTo(70));
            Assert.That(result.UserTileBounds.Width, Is.EqualTo(50));
            Assert.That(result.UserTileBounds.Height, Is.EqualTo(50));
            Assert.That(result.ViewBox.ToText(), Is.EqualTo("0 0 10 10"));
            Assert.That(result.ContentTransform.A, Is.EqualTo(5).Within(1e-12));
            Assert.That(result.ContentTransform.D, Is.EqualTo(2.5).Within(1e-12));
            Assert.That(result.ContentTransform.E, Is.EqualTo(120).Within(1e-12));
            Assert.That(result.ContentTransform.F, Is.EqualTo(82.5).Within(1e-12));
            Assert.That(result.Renderable, Is.True);
        });
    }

    [Test]
    public void PatternTransformComposesAfterBoundingBoxConversion()
    {
        SvgNode pattern = Load("<svg xmlns='http://www.w3.org/2000/svg'><pattern x='10%' y='20%' width='25%' height='30%' patternTransform='translate(.1 .2) scale(2)'><rect/></pattern></svg>").DocumentElement.Children[0];
        SvgPaintServerResolveOptions options = BoundsOptions(10, 20, 200, 100);

        SvgPaintServerResult result = new SvgPaintServerResolver().ResolveWithNodeAndOptions(pattern, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.ExplicitTransform.A, Is.EqualTo(2));
            Assert.That(result.ExplicitTransform.E, Is.EqualTo(0.1));
            Assert.That(result.Transform.A, Is.EqualTo(400));
            Assert.That(result.Transform.D, Is.EqualTo(200));
            Assert.That(result.Transform.E, Is.EqualTo(30));
            Assert.That(result.Transform.F, Is.EqualTo(40));
        });
    }

    [Test]
    public void ZeroPatternTileAndDegenerateBoundingBoxProduceNoPaintWithoutRangeErrors()
    {
        SvgNode zero = Load("<svg xmlns='http://www.w3.org/2000/svg'><pattern><rect/></pattern></svg>").DocumentElement.Children[0];
        SvgNode bounded = Load("<svg xmlns='http://www.w3.org/2000/svg'><pattern width='50%' height='50%'><rect/></pattern></svg>").DocumentElement.Children[0];

        SvgPaintServerResult zeroResult = new SvgPaintServerResolver().ResolveWithNodeAndOptions(zero, BoundsOptions(0, 0, 100, 100));
        SvgPaintServerResult boundedResult = new SvgPaintServerResolver().ResolveWithNodeAndOptions(bounded, BoundsOptions(10, 20, 0, 100));

        Assert.Multiple(() =>
        {
            Assert.That(zeroResult.Success, Is.True);
            Assert.That(zeroResult.Renderable, Is.False);
            Assert.That(zeroResult.TileBounds.IsEmpty, Is.True);
            Assert.That(boundedResult.Success, Is.True);
            Assert.That(boundedResult.Renderable, Is.False);
            Assert.That(boundedResult.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void CompleteGradientDoesNotProcessUnusedCyclicHref()
    {
        SvgNode gradient = Load("<svg xmlns='http://www.w3.org/2000/svg'><linearGradient id='paint' href='#paint' x1='0' y1='0' x2='1' y2='0' gradientUnits='userSpaceOnUse' gradientTransform='matrix(1 0 0 1 0 0)' spreadMethod='pad'><stop/></linearGradient></svg>").DocumentElement.Children[0];

        SvgPaintServerResult result = new SvgPaintServerResolver().ResolveWithNodeAndOptions(gradient, UserSpaceOptions(100, 100));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.References, Is.Empty);
            Assert.That(result.TemplateChain, Has.Count.EqualTo(1));
            Assert.That(result.GradientPaintKind, Is.EqualTo(SvgGradientPaintKind.Solid));
        });
    }

    [Test]
    public void DetectsRequiredTemplateCyclesAndIncompatibleTargets()
    {
        SvgDocument cycleDocument = Load("<svg xmlns='http://www.w3.org/2000/svg'><linearGradient id='a' href='#b'/><linearGradient id='b' href='#a'/></svg>");
        SvgDocument incompatibleDocument = Load("<svg xmlns='http://www.w3.org/2000/svg'><pattern id='p'/><linearGradient href='#p'/></svg>");
        SvgPaintServerResolveOptions cycleOptions = BoundsOptions(0, 0, 100, 100);

        SvgPaintServerResult cycle = new SvgPaintServerResolver().ResolveWithNodeAndOptions(cycleDocument.DocumentElement.Children[0], cycleOptions);
        SvgPaintServerResult incompatible = new SvgPaintServerResolver().ResolveWithNodeAndOptions(incompatibleDocument.DocumentElement.Children[1], cycleOptions);

        Assert.Multiple(() =>
        {
            Assert.That(cycle.Success, Is.False);
            Assert.That(cycle.TemplateChain, Has.Count.EqualTo(2));
            Assert.That(cycle.Diagnostics.Any(item => item.Code == SvgDiagnosticCode.ReferenceCycle), Is.True);
            Assert.That(incompatible.Success, Is.False);
            Assert.That(incompatible.Diagnostics.Any(item => item.Code == SvgDiagnosticCode.InvalidReference), Is.True);
        });
    }

    [Test]
    public void EnforcesTemplateDepthAndStopCountLimits()
    {
        SvgDocument depthDocument = Load("<svg xmlns='http://www.w3.org/2000/svg'><linearGradient id='a' href='#b'/><linearGradient id='b' href='#c'/><linearGradient id='c'/></svg>");
        SvgNode stops = Load("<svg xmlns='http://www.w3.org/2000/svg'><linearGradient><stop/><stop/><stop/></linearGradient></svg>").DocumentElement.Children[0];
        SvgPaintServerResolveOptions depthOptions = BoundsOptions(0, 0, 100, 100);
        depthOptions.Limits.MaxPaintServerDepth = 2;
        SvgPaintServerResolveOptions stopOptions = BoundsOptions(0, 0, 100, 100);
        stopOptions.MaxStops = 2;

        SvgPaintServerResult depth = new SvgPaintServerResolver().ResolveWithNodeAndOptions(depthDocument.DocumentElement.Children[0], depthOptions);
        SvgPaintServerResult stopCount = new SvgPaintServerResolver().ResolveWithNodeAndOptions(stops, stopOptions);

        Assert.Multiple(() =>
        {
            Assert.That(depth.Success, Is.False);
            Assert.That(depth.Diagnostics.Any(item => item.Code == SvgDiagnosticCode.InputLimitExceeded), Is.True);
            Assert.That(stopCount.Success, Is.False);
            Assert.That(stopCount.Diagnostics.Any(item => item.Code == SvgDiagnosticCode.InputLimitExceeded), Is.True);
        });
    }

    [Test]
    public void TemplateMutationMakesResolvedSnapshotStale()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><linearGradient id='base'><stop/></linearGradient><linearGradient href='#base'/></svg>");
        SvgNode template = document.DocumentElement.Children[0];
        SvgPaintServerResult result = new SvgPaintServerResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[1], BoundsOptions(0, 0, 100, 100));

        template.MarkDirty();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.IsCurrent(), Is.False);
        });
    }

    static SvgPaintServerResolveOptions BoundsOptions(double x, double y, double width, double height)
    {
        SvgPaintServerResolveOptions options = UserSpaceOptions(width, height);
        options.TargetBounds = SvgRect.Create(x, y, width, height, width == 0 || height == 0);
        return options;
    }

    static SvgPaintServerResolveOptions UserSpaceOptions(double width, double height)
    {
        SvgPaintServerResolveOptions options = new SvgPaintServerResolveOptions();
        options.LengthContext.HasViewport = true;
        options.LengthContext.ViewportWidth = width;
        options.LengthContext.ViewportHeight = height;
        return options;
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True);
        return result.Document;
    }

    static SvgStyleSheet Sheet(string css)
    {
        SvgCssParseResult result = new SvgCssParser().ParseStyleSheetWithValueAndLimitsAndOriginAndSource(css, new SvgCssLimits(), SvgStyleOrigin.Author, new SvgSourceUri("paint-server.css"));
        Assert.That(result.Success, Is.True);
        return result.StyleSheet;
    }
}
