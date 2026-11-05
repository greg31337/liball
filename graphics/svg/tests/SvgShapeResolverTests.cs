namespace svg.Tests;

public class SvgShapeResolverTests
{
    [Test]
    public void CssGeometryPropertiesOverrideRectangleAttributesAndClampRadii()
    {
        SvgNode rectangle = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect x='1' y='2' width='10' height='20' rx='1' style='x:10%;y:20%;width:50%;height:50%;rx:80%;ry:auto'/></svg>").DocumentElement.Children[0];
        SvgComputedStyle style = new SvgStyleResolver().Resolve(rectangle, new List<SvgStyleSheet>());
        SvgShapeResolveOptions options = ViewportOptions(200, 100);
        options.ComputedStyle = style;

        SvgShapeResult result = new SvgShapeResolver().ResolveWithNodeAndOptions(rectangle, options);

        Assert.Multiple(() =>
        {
            Assert.That(style.Success, Is.True);
            Assert.That(result.Success, Is.True);
            Assert.That(result.Kind, Is.EqualTo(SvgShapeKind.Rectangle));
            Assert.That(result.UsesComputedGeometry, Is.True);
            Assert.That(result.X, Is.EqualTo(20));
            Assert.That(result.Y, Is.EqualTo(20));
            Assert.That(result.Width, Is.EqualTo(100));
            Assert.That(result.Height, Is.EqualTo(50));
            Assert.That(result.Rx, Is.EqualTo(50));
            Assert.That(result.Ry, Is.EqualTo(25));
            Assert.That(result.Closed, Is.True);
            Assert.That(result.HasGeometry, Is.True);
            Assert.That(result.Renderable, Is.True);
        });
    }

    [Test]
    public void CircleAndEllipseResolvePercentageAndAutoRadiusRules()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><circle cx='50%' cy='25%' r='50%'/><ellipse rx='20' ry='auto'/><ellipse rx='auto' ry='10'/><ellipse/></svg>");
        SvgShapeResolveOptions options = ViewportOptions(300, 400);

        SvgShapeResult circle = new SvgShapeResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[0], options);
        SvgShapeResult horizontal = new SvgShapeResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[1], options);
        SvgShapeResult vertical = new SvgShapeResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[2], options);
        SvgShapeResult empty = new SvgShapeResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[3], options);

        Assert.Multiple(() =>
        {
            Assert.That(circle.Success, Is.True);
            Assert.That(circle.Cx, Is.EqualTo(150));
            Assert.That(circle.Cy, Is.EqualTo(100));
            Assert.That(circle.R, Is.EqualTo(176.7766952966369).Within(1e-12));
            Assert.That(circle.Renderable, Is.True);
            Assert.That(horizontal.Rx, Is.EqualTo(20));
            Assert.That(horizontal.Ry, Is.EqualTo(20));
            Assert.That(horizontal.Renderable, Is.True);
            Assert.That(vertical.Rx, Is.EqualTo(10));
            Assert.That(vertical.Ry, Is.EqualTo(10));
            Assert.That(vertical.Renderable, Is.True);
            Assert.That(empty.Success, Is.True);
            Assert.That(empty.Rx, Is.Zero);
            Assert.That(empty.Ry, Is.Zero);
            Assert.That(empty.HasGeometry, Is.False);
            Assert.That(empty.Renderable, Is.False);
        });
    }

    [Test]
    public void NegativeDimensionsFailWhileZeroDimensionsAreValidAndEmpty()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect width='-1' height='10'/><rect width='0' height='10'/><circle r='-2'/><circle r='0'/></svg>");

        SvgShapeResult negativeRectangle = new SvgShapeResolver().ResolveWithNode(document.DocumentElement.Children[0]);
        SvgShapeResult zeroRectangle = new SvgShapeResolver().ResolveWithNode(document.DocumentElement.Children[1]);
        SvgShapeResult negativeCircle = new SvgShapeResolver().ResolveWithNode(document.DocumentElement.Children[2]);
        SvgShapeResult zeroCircle = new SvgShapeResolver().ResolveWithNode(document.DocumentElement.Children[3]);

        Assert.Multiple(() =>
        {
            Assert.That(negativeRectangle.Success, Is.False);
            Assert.That(negativeRectangle.HasGeometry, Is.False);
            Assert.That(negativeRectangle.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InvalidValue));
            Assert.That(negativeRectangle.Diagnostics.Single().Source, Is.SameAs(document.DocumentElement.Children[0].Attributes[0].SourceSpan));
            Assert.That(zeroRectangle.Success, Is.True);
            Assert.That(zeroRectangle.HasGeometry, Is.False);
            Assert.That(zeroRectangle.Renderable, Is.False);
            Assert.That(negativeCircle.Success, Is.False);
            Assert.That(negativeCircle.HasGeometry, Is.False);
            Assert.That(negativeCircle.Diagnostics.Single().Source, Is.SameAs(document.DocumentElement.Children[2].Attributes[0].SourceSpan));
            Assert.That(zeroCircle.Success, Is.True);
            Assert.That(zeroCircle.HasGeometry, Is.False);
        });
    }

    [Test]
    public void LineCoordinatesResolvePercentagesAndRetainDegenerateSegments()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><line x1='10%' y1='20%' x2='90%' y2='80%'/><line x1='5' y1='6' x2='5' y2='6'/></svg>");
        SvgShapeResolveOptions options = ViewportOptions(200, 50);

        SvgShapeResult line = new SvgShapeResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[0], options);
        SvgShapeResult pointLine = new SvgShapeResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[1], options);

        Assert.Multiple(() =>
        {
            Assert.That(line.Success, Is.True);
            Assert.That(line.X1, Is.EqualTo(20));
            Assert.That(line.Y1, Is.EqualTo(10));
            Assert.That(line.X2, Is.EqualTo(180));
            Assert.That(line.Y2, Is.EqualTo(40));
            Assert.That(line.Renderable, Is.True);
            Assert.That(pointLine.Success, Is.True);
            Assert.That(pointLine.HasGeometry, Is.True);
            Assert.That(pointLine.Renderable, Is.True);
        });
    }

    [Test]
    public void PointListsRecoverCompletePairsAndHonorPointLimits()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><polygon points='0,0 10,0 10'/><polyline points='0,0 5,5 10,10'/><polyline/></svg>");
        SvgShapeResult odd = new SvgShapeResolver().ResolveWithNode(document.DocumentElement.Children[0]);
        SvgShapeResolveOptions limitedOptions = new SvgShapeResolveOptions();
        limitedOptions.GeometryLimits.MaxPoints = 2;
        SvgShapeResult limited = new SvgShapeResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[1], limitedOptions);
        SvgShapeResult empty = new SvgShapeResolver().ResolveWithNode(document.DocumentElement.Children[2]);

        Assert.Multiple(() =>
        {
            Assert.That(odd.Success, Is.False);
            Assert.That(odd.Closed, Is.True);
            Assert.That(odd.PointCount, Is.EqualTo(2));
            Assert.That(odd.Points, Is.EqualTo(new double[] { 0, 0, 10, 0 }));
            Assert.That(odd.HasGeometry, Is.True);
            Assert.That(odd.Renderable, Is.True);
            Assert.That(odd.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InvalidValue));
            Assert.That(limited.Success, Is.False);
            Assert.That(limited.PointCount, Is.EqualTo(2));
            Assert.That(limited.Points, Is.EqualTo(new double[] { 0, 0, 5, 5 }));
            Assert.That(limited.Renderable, Is.True);
            Assert.That(limited.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
            Assert.That(empty.Success, Is.True);
            Assert.That(empty.PointCount, Is.Zero);
            Assert.That(empty.Renderable, Is.False);
        });
    }

    [Test]
    public void CssPathOverridesPresentationDataAndNoneDisablesGeometry()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><path d='M0 0L1 1' style=\"d:path('M2 3 L4 5')\"/><path d='M0 0L1 1' style='d:none'/><path d='M6 7L8 9'/></svg>");
        SvgNode overriddenNode = document.DocumentElement.Children[0];
        SvgNode noneNode = document.DocumentElement.Children[1];
        SvgShapeResolveOptions overriddenOptions = new SvgShapeResolveOptions();
        overriddenOptions.ComputedStyle = new SvgStyleResolver().Resolve(overriddenNode, new List<SvgStyleSheet>());
        SvgShapeResolveOptions noneOptions = new SvgShapeResolveOptions();
        noneOptions.ComputedStyle = new SvgStyleResolver().Resolve(noneNode, new List<SvgStyleSheet>());
        SvgNode presentationNode = document.DocumentElement.Children[2];
        SvgShapeResolveOptions presentationOptions = new SvgShapeResolveOptions();
        presentationOptions.ComputedStyle = new SvgStyleResolver().Resolve(presentationNode, new List<SvgStyleSheet>());

        SvgShapeResult overridden = new SvgShapeResolver().ResolveWithNodeAndOptions(overriddenNode, overriddenOptions);
        SvgShapeResult none = new SvgShapeResolver().ResolveWithNodeAndOptions(noneNode, noneOptions);
        SvgShapeResult presentation = new SvgShapeResolver().ResolveWithNodeAndOptions(presentationNode, presentationOptions);

        Assert.Multiple(() =>
        {
            Assert.That(overriddenOptions.ComputedStyle.Success, Is.True);
            Assert.That(overridden.Success, Is.True);
            Assert.That(overridden.UsesComputedGeometry, Is.True);
            Assert.That(overridden.PathData.Commands, Has.Count.EqualTo(2));
            Assert.That(overridden.PathData.Commands[0].EndX, Is.EqualTo(2));
            Assert.That(overridden.PathData.Commands[0].EndY, Is.EqualTo(3));
            Assert.That(overridden.PathData.Commands[1].EndX, Is.EqualTo(4));
            Assert.That(overridden.PathData.Commands[1].EndY, Is.EqualTo(5));
            Assert.That(overridden.Renderable, Is.True);
            Assert.That(noneOptions.ComputedStyle.Success, Is.True);
            Assert.That(none.Success, Is.True);
            Assert.That(none.PathData, Is.Null);
            Assert.That(none.HasGeometry, Is.False);
            Assert.That(none.Renderable, Is.False);
            Assert.That(presentationOptions.ComputedStyle.Success, Is.True);
            Assert.That(presentation.Success, Is.True);
            Assert.That(presentation.PathData.Commands, Has.Count.EqualTo(2));
            Assert.That(presentation.PathData.Commands[0].EndX, Is.EqualTo(6));
            Assert.That(presentation.PathData.Commands[1].EndX, Is.EqualTo(8));
            Assert.That(presentation.Renderable, Is.True);
        });
    }

    [Test]
    public void PathRecoveryAndPathLengthRetainUsableSemanticPrefixes()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><path d='M0 0 L10 0 X M20 20 L30 30' pathLength='0'/><circle r='4' pathLength='-1'/></svg>");

        SvgShapeResult path = new SvgShapeResolver().ResolveWithNode(document.DocumentElement.Children[0]);
        SvgShapeResult circle = new SvgShapeResolver().ResolveWithNode(document.DocumentElement.Children[1]);

        Assert.Multiple(() =>
        {
            Assert.That(path.Success, Is.False);
            Assert.That(path.PathData.Commands, Has.Count.GreaterThanOrEqualTo(4));
            Assert.That(path.HasGeometry, Is.True);
            Assert.That(path.Renderable, Is.True);
            Assert.That(path.HasPathLength, Is.True);
            Assert.That(path.PathLength.Value, Is.Zero);
            Assert.That(path.PathLength.GetScale(10).Success, Is.False);
            Assert.That(path.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InvalidValue));
            Assert.That(circle.Success, Is.False);
            Assert.That(circle.HasGeometry, Is.True);
            Assert.That(circle.HasPathLength, Is.True);
            Assert.That(circle.PathLength, Is.Null);
            Assert.That(circle.Diagnostics.Single().Source.Source, Is.SameAs(document.DocumentElement.Children[1].Attributes[1].SourceSpan.Source));
        });
    }

    [Test]
    public void DisplayAndVisibilitySuppressPaintingWithoutDiscardingGeometry()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect width='10' height='20' style='display:none'/><circle r='5' visibility='hidden'/></svg>");
        SvgNode rectangleNode = document.DocumentElement.Children[0];
        SvgShapeResolveOptions rectangleOptions = new SvgShapeResolveOptions();
        rectangleOptions.ComputedStyle = new SvgStyleResolver().Resolve(rectangleNode, new List<SvgStyleSheet>());

        SvgShapeResult rectangle = new SvgShapeResolver().ResolveWithNodeAndOptions(rectangleNode, rectangleOptions);
        SvgShapeResult circle = new SvgShapeResolver().ResolveWithNode(document.DocumentElement.Children[1]);

        Assert.Multiple(() =>
        {
            Assert.That(rectangle.Success, Is.True);
            Assert.That(rectangle.HasGeometry, Is.True);
            Assert.That(rectangle.Displayed, Is.False);
            Assert.That(rectangle.Renderable, Is.False);
            Assert.That(circle.Success, Is.True);
            Assert.That(circle.HasGeometry, Is.True);
            Assert.That(circle.Visible, Is.False);
            Assert.That(circle.Renderable, Is.False);
        });
    }

    static SvgShapeResolveOptions ViewportOptions(double width, double height)
    {
        SvgShapeResolveOptions options = new SvgShapeResolveOptions();
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
}
