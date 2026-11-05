namespace svg.Tests;

public class SvgPaintBuilderTests
{
    [Test]
    public void BuildsResolvableGradientsWithDeterministicIdsAndPaintReferences()
    {
        SvgBuilder document = new SvgBuilder();
        document.BeginElement(SvgElementKind.Rect).SetAttribute("width", "20").SetAttribute("height", "10");
        SvgNode target = document.Current;
        SvgPaintBuilder paints = new SvgPaintBuilder(document);

        SvgPaintBuilder returned = paints
            .Fill("rebeccapurple")
            .LinearGradientWithCoordinates(0, 0, 20, 0)
            .SetAttribute("gradientUnits", "userSpaceOnUse")
            .AddStop(0, "red")
            .AddStopWithOpacity(1, "rgba(0 0 255 / 50%)", .8)
            .FillLastElement();
        SvgNode linear = paints.Build();
        paints.RadialGradientWithGeometry(10, 5, 5, 10, 5, 0).StrokeLastElement();
        SvgNode radial = paints.Build();

        SvgPaintServerResolveOptions options = new SvgPaintServerResolveOptions();
        options.TargetBounds = SvgRect.Create(0, 0, 20, 10);
        SvgPaintServerResult resolved = new SvgPaintServerResolver().ResolveWithNodeAndOptions(linear, options);
        SvgNode defs = document.Root.Children.Single(item => item.ElementKind == SvgElementKind.Defs);

        Assert.Multiple(() =>
        {
            Assert.That(returned, Is.SameAs(paints));
            Assert.That(paints.Succeeded(), Is.True, paints.Message);
            Assert.That(document.Succeeded(), Is.True, document.Message);
            Assert.That(document.Current, Is.SameAs(target));
            Assert.That(document.Root.Children[0], Is.SameAs(defs));
            Assert.That(defs.Children, Is.EqualTo(new[] { linear, radial }));
            Assert.That(Attribute(linear, "id"), Is.EqualTo("svg-gradient-1"));
            Assert.That(Attribute(radial, "id"), Is.EqualTo("svg-gradient-2"));
            Assert.That(Attribute(target, "fill"), Is.EqualTo("url(#svg-gradient-1)"));
            Assert.That(Attribute(target, "stroke"), Is.EqualTo("url(#svg-gradient-2)"));
            Assert.That(resolved.Success, Is.True, Diagnostics(resolved.Diagnostics));
            Assert.That(resolved.Kind, Is.EqualTo(SvgPaintServerKind.LinearGradient));
            Assert.That(resolved.Stops, Has.Count.EqualTo(2));
            Assert.That(resolved.Stops.Select(item => item.Offset), Is.EqualTo(new[] { 0d, 1d }));
            Assert.That(resolved.Stops[1].Opacity, Is.EqualTo(.8));
            Assert.That(document.Document.Index.GetById("svg-gradient-1"), Is.SameAs(linear));
        });
    }

    [Test]
    public void BuildsPatternMarkerClipAndMaskContentWithoutMovingTheTarget()
    {
        SvgBuilder document = new SvgBuilder();
        document.BeginElement(SvgElementKind.Path).SetAttribute("d", "M0 0L20 0");
        SvgNode target = document.Current;
        SvgPaintBuilder paints = new SvgPaintBuilder(document);

        paints.Pattern(0, 0, 4, 4)
            .SetAttribute("patternUnits", "userSpaceOnUse")
            .AddContentWithBuilder(content => content.BeginElement(SvgElementKind.Rect).SetAttribute("width", "2").SetAttribute("height", "4").SetAttribute("fill", "orange").EndElement())
            .FillLastElement();
        SvgNode pattern = paints.Definition;

        SvgPathBuildResult markerPath = new SvgPathBuilder().MoveTo(0, 0).LineTo(3, 1.5).LineTo(0, 3).Close().ToPathElement();
        paints.Marker(3, 3, 3, 1.5, "auto").AddContent(markerPath.Element).ApplyMarker();
        SvgNode marker = paints.Definition;

        paints.ClipPath()
            .AddContentWithBuilder(content => content.BeginElement(SvgElementKind.Circle).SetAttribute("cx", "10").SetAttribute("cy", "5").SetAttribute("r", "5").EndElement())
            .ApplyClipPath();
        SvgNode clip = paints.Definition;

        paints.MaskWithBounds(0, 0, 20, 10)
            .SetAttribute("maskUnits", "userSpaceOnUse")
            .AddContentWithBuilder(content => content.BeginElement(SvgElementKind.Rect).SetAttribute("width", "20").SetAttribute("height", "10").SetAttribute("fill", "white").EndElement())
            .ApplyMask();
        SvgNode mask = paints.Definition;

        SvgPaintServerResolveOptions options = new SvgPaintServerResolveOptions();
        options.TargetBounds = SvgRect.Create(0, 0, 20, 10);
        SvgPaintServerResult resolvedPattern = new SvgPaintServerResolver().ResolveWithNodeAndOptions(pattern, options);

        Assert.Multiple(() =>
        {
            Assert.That(markerPath.Success, Is.True, markerPath.Message);
            Assert.That(paints.Succeeded(), Is.True, paints.Message);
            Assert.That(document.Current, Is.SameAs(target));
            Assert.That(Attribute(pattern, "id"), Is.EqualTo("svg-pattern-1"));
            Assert.That(Attribute(marker, "id"), Is.EqualTo("svg-marker-1"));
            Assert.That(Attribute(clip, "id"), Is.EqualTo("svg-clip-1"));
            Assert.That(Attribute(mask, "id"), Is.EqualTo("svg-mask-1"));
            Assert.That(pattern.Children.Single().ElementKind, Is.EqualTo(SvgElementKind.Rect));
            Assert.That(marker.Children.Single().ElementKind, Is.EqualTo(SvgElementKind.Path));
            Assert.That(clip.Children.Single().ElementKind, Is.EqualTo(SvgElementKind.Circle));
            Assert.That(mask.Children.Single().ElementKind, Is.EqualTo(SvgElementKind.Rect));
            Assert.That(Attribute(target, "fill"), Is.EqualTo("url(#svg-pattern-1)"));
            Assert.That(Attribute(target, "marker-start"), Is.EqualTo("url(#svg-marker-1)"));
            Assert.That(Attribute(target, "marker-mid"), Is.EqualTo("url(#svg-marker-1)"));
            Assert.That(Attribute(target, "marker-end"), Is.EqualTo("url(#svg-marker-1)"));
            Assert.That(Attribute(target, "clip-path"), Is.EqualTo("url(#svg-clip-1)"));
            Assert.That(Attribute(target, "mask"), Is.EqualTo("url(#svg-mask-1)"));
            Assert.That(resolvedPattern.Success, Is.True, Diagnostics(resolvedPattern.Diagnostics));
            Assert.That(resolvedPattern.Content.Single(), Is.SameAs(pattern.Children.Single()));
            Assert.That(resolvedPattern.Renderable, Is.True);
        });
    }

    [Test]
    public void AvoidsExistingIdsAndReportsStickyAtomicArgumentFailures()
    {
        SvgBuilder document = new SvgBuilder();
        document.BeginElement(SvgElementKind.Defs)
            .BeginElement(SvgElementKind.LinearGradient).SetAttribute("id", "svg-gradient-1").EndElement()
            .EndElement()
            .BeginElement(SvgElementKind.Rect);
        SvgPaintBuilder paints = new SvgPaintBuilder(document);
        paints.LinearGradient();
        SvgNode gradient = paints.Definition;
        paints.AddStop(double.NaN, "red");

        Assert.Multiple(() =>
        {
            Assert.That(Attribute(gradient, "id"), Is.EqualTo("svg-gradient-2"));
            Assert.That(paints.Error, Is.EqualTo(SvgPaintBuilderError.InvalidArgument));
            Assert.That(gradient.Children, Is.Empty);
            Assert.That(paints.Build(), Is.Null);
        });

        paints.ClearError().ApplyMask();
        Assert.That(paints.Error, Is.EqualTo(SvgPaintBuilderError.InvalidState));

        SvgPaintBuilder missing = new SvgPaintBuilder(null);
        Assert.Multiple(() =>
        {
            Assert.That(missing.Error, Is.EqualTo(SvgPaintBuilderError.InvalidArgument));
            Assert.That(missing.Succeeded(), Is.False);
        });
    }

    static string Attribute(SvgNode element, string name)
    {
        return element.Attributes.Single(item => item.NamespaceUri.Length == 0 && item.LocalName == name).Value;
    }

    static string Diagnostics(List<SvgDiagnostic> diagnostics)
    {
        return string.Join(" | ", diagnostics.Select(item => item.Message));
    }
}
