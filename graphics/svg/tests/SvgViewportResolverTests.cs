namespace svg.Tests;

public class SvgViewportResolverTests
{
    [Test]
    public void RootViewportUsesHostSizingIgnoresXYAndFitsViewBox()
    {
        SvgNode root = Load("<svg xmlns='http://www.w3.org/2000/svg' x='99' y='88' width='50%' height='25%' viewBox='0 0 100 100'/>").DocumentElement;

        SvgViewportResult result = new SvgViewportResolver().ResolveRoot(root, 640, 480);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.IsRoot, Is.True);
            Assert.That(result.X, Is.Zero);
            Assert.That(result.Y, Is.Zero);
            Assert.That(result.Width, Is.EqualTo(320));
            Assert.That(result.Height, Is.EqualTo(120));
            Assert.That(result.Bounds.Width, Is.EqualTo(320));
            Assert.That(result.ViewBox.ToText(), Is.EqualTo("0 0 100 100"));
            Assert.That(result.AspectRatio.Alignment, Is.EqualTo(SvgAspectRatioAlignment.XMidYMid));
            Assert.That(result.Transform.A, Is.EqualTo(1.2).Within(1e-12));
            Assert.That(result.Transform.D, Is.EqualTo(1.2).Within(1e-12));
            Assert.That(result.Transform.E, Is.EqualTo(100).Within(1e-12));
            Assert.That(result.Transform.F, Is.Zero.Within(1e-12));
            Assert.That(result.LengthContext.ViewportWidth, Is.EqualTo(320.0 / 1.2).Within(1e-12));
            Assert.That(result.LengthContext.ViewportHeight, Is.EqualTo(100).Within(1e-12));
            Assert.That(result.ClipsOverflow, Is.False);
            Assert.That(result.ClipRect, Is.Null);
            Assert.That(result.Renderable, Is.True);
        });
    }

    [Test]
    public void StandaloneRootAutoSizeUsesDeterministicFallback()
    {
        SvgNode root = Load("<svg xmlns='http://www.w3.org/2000/svg'/>").DocumentElement;

        SvgViewportResult result = new SvgViewportResolver().Resolve(root, new SvgViewportResolveOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Width, Is.EqualTo(300));
            Assert.That(result.Height, Is.EqualTo(150));
            Assert.That(result.Transform.A, Is.EqualTo(1));
            Assert.That(result.Transform.D, Is.EqualTo(1));
            Assert.That(result.LengthContext.ViewportWidth, Is.EqualTo(300));
            Assert.That(result.LengthContext.ViewportHeight, Is.EqualTo(150));
        });
    }

    [Test]
    public void NestedViewportResolvesPercentagesInParentUserSpaceAndClipsByDefault()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg' width='400' height='200' viewBox='0 0 100 50'><svg x='10%' y='20%' width='50%' height='40%' viewBox='0 0 10 10' preserveAspectRatio='xMaxYMin meet'/><svg overflow='visible'/></svg>");
        SvgNode root = document.DocumentElement;
        SvgViewportResult parent = new SvgViewportResolver().ResolveRoot(root, 400, 200);

        SvgViewportResult nested = new SvgViewportResolver().ResolveNested(root.Children[0], parent);
        SvgViewportResult visible = new SvgViewportResolver().ResolveNested(root.Children[1], parent);

        Assert.Multiple(() =>
        {
            Assert.That(parent.Success, Is.True);
            Assert.That(parent.LengthContext.ViewportWidth, Is.EqualTo(100));
            Assert.That(parent.LengthContext.ViewportHeight, Is.EqualTo(50));
            Assert.That(nested.Success, Is.True);
            Assert.That(nested.IsRoot, Is.False);
            Assert.That(nested.X, Is.EqualTo(10));
            Assert.That(nested.Y, Is.EqualTo(10));
            Assert.That(nested.Width, Is.EqualTo(50));
            Assert.That(nested.Height, Is.EqualTo(20));
            Assert.That(nested.Transform.A, Is.EqualTo(2));
            Assert.That(nested.Transform.D, Is.EqualTo(2));
            Assert.That(nested.Transform.E, Is.EqualTo(40));
            Assert.That(nested.Transform.F, Is.EqualTo(10));
            Assert.That(nested.UserBounds.X, Is.EqualTo(-15));
            Assert.That(nested.UserBounds.Y, Is.Zero);
            Assert.That(nested.LengthContext.ViewportWidth, Is.EqualTo(25));
            Assert.That(nested.LengthContext.ViewportHeight, Is.EqualTo(10));
            Assert.That(nested.ClipsOverflow, Is.True);
            Assert.That(nested.ClipRect.X, Is.EqualTo(10));
            Assert.That(nested.ClipRect.Width, Is.EqualTo(50));
            Assert.That(visible.Success, Is.True);
            Assert.That(visible.Width, Is.EqualTo(100));
            Assert.That(visible.Height, Is.EqualTo(50));
            Assert.That(visible.ClipsOverflow, Is.False);
        });
    }

    [Test]
    public void ComputedGeometryAndOverflowOverridePresentationAttributes()
    {
        SvgNode root = Load("<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100' overflow='visible' style='width:50px;height:40px;overflow:hidden'/>").DocumentElement;
        SvgComputedStyle style = new SvgStyleResolver().Resolve(root, new List<SvgStyleSheet>());
        SvgViewportResolveOptions options = new SvgViewportResolveOptions();
        options.ComputedStyle = style;
        options.LengthContext.HasViewport = true;
        options.LengthContext.ViewportWidth = 500;
        options.LengthContext.ViewportHeight = 500;

        SvgViewportResult result = new SvgViewportResolver().Resolve(root, options);

        Assert.Multiple(() =>
        {
            Assert.That(style.Success, Is.True);
            Assert.That(result.Success, Is.True);
            Assert.That(result.Width, Is.EqualTo(50));
            Assert.That(result.Height, Is.EqualTo(40));
            Assert.That(result.ClipsOverflow, Is.True);
            Assert.That(result.ClipRect.Width, Is.EqualTo(50));
            Assert.That(result.ClipRect.Height, Is.EqualTo(40));
        });
    }

    [Test]
    public void ViewElementAndOrdinaryBareFragmentsSelectTargetsAndOverrideOnlyViews()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg' width='200' height='100' viewBox='0 0 200 100'><view id='crop' viewBox='50 25 50 25' preserveAspectRatio='none'/><rect id='focus'/></svg>");
        SvgNode root = document.DocumentElement;
        SvgViewportResolveOptions cropOptions = HostOptions(200, 100, "#crop");
        SvgViewportResolveOptions focusOptions = HostOptions(200, 100, "focus");

        SvgViewportResult crop = new SvgViewportResolver().Resolve(root, cropOptions);
        SvgViewportResult focus = new SvgViewportResolver().Resolve(root, focusOptions);

        Assert.Multiple(() =>
        {
            Assert.That(crop.Success, Is.True);
            Assert.That(crop.FragmentKind, Is.EqualTo(SvgViewportFragmentKind.ViewElement));
            Assert.That(crop.FragmentTarget, Is.SameAs(root.Children[0]));
            Assert.That(crop.ViewSpecificationNode, Is.SameAs(root.Children[0]));
            Assert.That(crop.ViewBox.ToText(), Is.EqualTo("50 25 50 25"));
            Assert.That(crop.AspectRatio.Alignment, Is.EqualTo(SvgAspectRatioAlignment.None));
            Assert.That(crop.Transform.A, Is.EqualTo(4));
            Assert.That(crop.Transform.D, Is.EqualTo(4));
            Assert.That(crop.Transform.E, Is.EqualTo(-200));
            Assert.That(crop.Transform.F, Is.EqualTo(-100));
            Assert.That(focus.Success, Is.True);
            Assert.That(focus.FragmentKind, Is.EqualTo(SvgViewportFragmentKind.Element));
            Assert.That(focus.FragmentTarget, Is.SameAs(root.Children[1]));
            Assert.That(focus.ViewSpecificationNode, Is.Null);
            Assert.That(focus.ViewBox.ToText(), Is.EqualTo("0 0 200 100"));
            Assert.That(focus.Transform.A, Is.EqualTo(1));
            Assert.That(focus.Transform.D, Is.EqualTo(1));
        });
    }

    [Test]
    public void SvgViewAndMediaFragmentsOverrideRootViewParameters()
    {
        SvgNode root = Load("<svg xmlns='http://www.w3.org/2000/svg' width='200' height='100' viewBox='0 0 200 100' preserveAspectRatio='none'/>").DocumentElement;
        SvgViewportResolveOptions viewOptions = HostOptions(200, 100, "#svgView(viewBox(0,0,20,10);preserveAspectRatio(xMidYMid meet);transform(translate(1 2)))");
        SvgViewportResolveOptions mediaOptions = HostOptions(200, 100, "#xywh=percent:10,20,50,50");

        SvgViewportResult view = new SvgViewportResolver().Resolve(root, viewOptions);
        SvgViewportResult media = new SvgViewportResolver().Resolve(root, mediaOptions);

        Assert.Multiple(() =>
        {
            Assert.That(view.Success, Is.True);
            Assert.That(view.FragmentKind, Is.EqualTo(SvgViewportFragmentKind.SvgView));
            Assert.That(view.ViewBox.ToText(), Is.EqualTo("0 0 20 10"));
            Assert.That(view.AspectRatio.Alignment, Is.EqualTo(SvgAspectRatioAlignment.XMidYMid));
            Assert.That(view.FragmentTransform.Items, Has.Count.EqualTo(1));
            Assert.That(view.Transform.A, Is.EqualTo(10));
            Assert.That(view.Transform.D, Is.EqualTo(10));
            Assert.That(view.Transform.E, Is.EqualTo(1));
            Assert.That(view.Transform.F, Is.EqualTo(2));
            Assert.That(media.Success, Is.True);
            Assert.That(media.FragmentKind, Is.EqualTo(SvgViewportFragmentKind.MediaFragment));
            Assert.That(media.ViewBox.ToText(), Is.EqualTo("20 20 100 50"));
            Assert.That(media.Transform.A, Is.EqualTo(2));
            Assert.That(media.Transform.D, Is.EqualTo(2));
            Assert.That(media.Transform.E, Is.EqualTo(-40));
            Assert.That(media.Transform.F, Is.EqualTo(-40));
        });
    }

    [Test]
    public void InvalidDimensionsAndFragmentsAreLocatedWhileDegenerateViewBoxesDisableRendering()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100'><svg width='-1'/></svg>");
        SvgViewportResult parent = new SvgViewportResolver().ResolveRoot(document.DocumentElement, 100, 100);
        SvgViewportResult negative = new SvgViewportResolver().ResolveNested(document.DocumentElement.Children[0], parent);
        SvgNode degenerateRoot = Load("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 0 10'/>").DocumentElement;
        SvgViewportResult degenerate = new SvgViewportResolver().ResolveRoot(degenerateRoot, 100, 100);
        SvgViewportResolveOptions missingOptions = HostOptions(100, 100, "#missing");
        SvgViewportResult missing = new SvgViewportResolver().Resolve(degenerateRoot, missingOptions);

        Assert.Multiple(() =>
        {
            Assert.That(negative.Success, Is.False);
            Assert.That(negative.Diagnostics.Any(diagnostic => diagnostic.Code == SvgDiagnosticCode.InvalidValue), Is.True);
            Assert.That(negative.Diagnostics[0].Source, Is.Not.Null);
            Assert.That(negative.Diagnostics[0].Source.Source, Is.SameAs(document.DocumentElement.Children[0].Attributes[0].SourceSpan.Source));
            Assert.That(degenerate.Success, Is.True);
            Assert.That(degenerate.Renderable, Is.False);
            Assert.That(degenerate.UserBounds.IsEmpty, Is.True);
            Assert.That(missing.Success, Is.False);
            Assert.That(missing.FragmentKind, Is.EqualTo(SvgViewportFragmentKind.Invalid));
            Assert.That(missing.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InvalidReference));
            Assert.That(missing.Diagnostics.Single().Source, Is.SameAs(degenerateRoot.SourceSpan));
        });
    }

    static SvgViewportResolveOptions HostOptions(double width, double height, string fragment)
    {
        SvgViewportResolveOptions options = new SvgViewportResolveOptions();
        options.LengthContext.HasViewport = true;
        options.LengthContext.ViewportWidth = width;
        options.LengthContext.ViewportHeight = height;
        options.FragmentIdentifier = fragment;
        return options;
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True);
        return result.Document;
    }
}
