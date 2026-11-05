namespace svg.Tests;

public class SvgDrawingBoundsTests
{
    [Test]
    public void FillBoundsUseExactCurveExtremaAndRepresentEmptyPaths()
    {
        SvgDrawingBoundsResult curve = Parse("M0 0 Q5 10 10 0").GetFillBounds();
        SvgDrawingBoundsResult empty = Parse("M4 6").GetFillBounds();

        Assert.Multiple(() =>
        {
            AssertBounds(curve, 0, 0, 10, 5);
            Assert.That(curve.FillBounds.ValueEquals(curve.Bounds), Is.True);
            Assert.That(empty.Success, Is.True);
            Assert.That(empty.Bounds.IsEmpty, Is.True);
        });
    }

    [Test]
    public void WorldTransformIsAppliedBeforeFillExtremaAreMeasured()
    {
        SvgDrawingBoundsOptions options = new SvgDrawingBoundsOptions();
        options.Transform = new SvgMatrix(0, 1, -1, 0, 20, 30);

        SvgDrawingBoundsResult result = Parse("M0 0 H10 V5 H0 Z").GetDrawingBounds(options);

        AssertBounds(result, 15, 30, 5, 10);
    }

    [Test]
    public void ShapeBoundsConvertResolvedBasicGeometry()
    {
        SvgResolvedDocument document = Resolve("<svg xmlns='http://www.w3.org/2000/svg' width='20' height='20'><circle cx='5' cy='6' r='3'/><path d='M10 10 h4 v2 h-4 z'/></svg>");
        SvgResolvedNode circle = document.Nodes.Single(node => node.SourceNode.LocalName == "circle");
        SvgResolvedNode path = document.Nodes.Single(node => node.SourceNode.LocalName == "path");

        SvgDrawingBoundsResult result = circle.Geometry.GetDefaultDrawingBounds();
        SvgDrawingBoundsResult pathResult = path.Geometry.GetDefaultDrawingBounds();

        Assert.Multiple(() =>
        {
            AssertBounds(result, 2, 3, 6, 6, 0.000001);
            AssertBounds(pathResult, 10, 10, 4, 2);
        });
    }

    [Test]
    public void DrawingBoundsUnionStrokeAndMarkersInWorldSpace()
    {
        SvgDrawingBoundsOptions options = new SvgDrawingBoundsOptions();
        options.IncludeFill = false;
        options.Transform = new SvgMatrix(2, 0, 0, 3, 5, 7);
        options.Stroke = new SvgStrokeGeometryOptions();
        options.Stroke.Style.Width = 2;
        options.Stroke.Style.LineCap = SvgStrokeLineCap.Square;
        options.Markers = new SvgMarkerGeometryOptions();
        options.Markers.StartMarker = Marker(SvgRect.Create(-2, -2, 4, 4));
        options.Markers.EndMarker = Marker(SvgRect.Create(-2, -2, 4, 4));

        SvgDrawingBoundsResult result = Parse("M0 0 L10 0").GetDrawingBounds(options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.FillBounds.IsEmpty, Is.True);
            AssertRect(result.StrokeBounds, 3, 4, 24, 6);
            AssertRect(result.MarkerBounds, 1, 1, 28, 12);
            AssertRect(result.Bounds, 1, 1, 28, 12);
        });
    }

    [Test]
    public void FilterClipAndMaskRegionsApplyInPaintingOrder()
    {
        SvgPathData path = Parse("M0 0 H10 V10 H0 Z");
        SvgDrawingBoundsOptions options = new SvgDrawingBoundsOptions();
        options.FilterBounds = SvgRect.Create(-2, -2, 14, 14);
        options.ClipBounds = SvgRect.Create(0, 0, 8, 8);
        options.MaskBounds = SvgRect.Create(2, 2, 10, 10);
        SvgDrawingBoundsOptions clipped = new SvgDrawingBoundsOptions();
        clipped.ClipBounds = SvgRect.Create(0, 0, 0, 0, true);
        SvgDrawingBoundsOptions masked = new SvgDrawingBoundsOptions();
        masked.MaskBounds = SvgRect.Create(0, 0, 0, 0, true);

        SvgDrawingBoundsResult result = path.GetDrawingBounds(options);
        SvgDrawingBoundsResult clippedOut = path.GetDrawingBounds(clipped);
        SvgDrawingBoundsResult maskedOut = path.GetDrawingBounds(masked);

        Assert.Multiple(() =>
        {
            AssertBounds(result, 2, 2, 6, 6);
            Assert.That(result.Filtered, Is.True);
            Assert.That(result.ClippedOut, Is.False);
            Assert.That(result.MaskedOut, Is.False);
            Assert.That(clippedOut.Bounds.IsEmpty, Is.True);
            Assert.That(clippedOut.ClippedOut, Is.True);
            Assert.That(maskedOut.Bounds.IsEmpty, Is.True);
            Assert.That(maskedOut.MaskedOut, Is.True);
        });
    }

    [Test]
    public void DocumentBoundsAggregateWorldTransformsAndViewportClipping()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' width='20' height='20' overflow='hidden'><g transform='translate(5 3)'><rect x='1' y='2' width='4' height='5'/></g><rect x='18' y='18' width='5' height='5'/><rect x='30' y='30' width='2' height='2'/><rect display='none' width='50' height='50'/></svg>";
        SvgResolvedDocument document = Resolve(source);

        SvgDocumentBoundsResult result = document.GetDefaultDrawingBounds();
        SvgDocumentBoundsOptions unclippedOptions = new SvgDocumentBoundsOptions();
        unclippedOptions.ClipToViewports = false;
        SvgDocumentBoundsResult unclipped = document.GetDrawingBounds(unclippedOptions);

        Assert.Multiple(() =>
        {
            AssertDocumentBounds(result, 6, 5, 14, 15);
            Assert.That(result.ContributingNodeCount, Is.EqualTo(2));
            Assert.That(result.EmptyNodeCount, Is.EqualTo(1));
            Assert.That(result.ClippedNodeCount, Is.EqualTo(1));
            Assert.That(result.SkippedNodeCount, Is.GreaterThanOrEqualTo(3));
            AssertDocumentBounds(unclipped, 6, 5, 26, 27);
            Assert.That(unclipped.ContributingNodeCount, Is.EqualTo(3));
            Assert.That(unclipped.ClippedNodeCount, Is.EqualTo(0));
        });
    }

    [Test]
    public void DocumentBoundsIncludeComputedStrokeAndPerNodeEffects()
    {
        SvgResolvedDocument strokeDocument = Resolve("<svg xmlns='http://www.w3.org/2000/svg' width='20' height='20'><line x1='2' y1='5' x2='12' y2='5' fill='none' stroke='red' stroke-width='4' stroke-dasharray='2 20'/></svg>");
        SvgDocumentBoundsResult stroke = strokeDocument.GetDefaultDrawingBounds();
        SvgDocumentBoundsOptions withoutStrokeOptions = new SvgDocumentBoundsOptions();
        withoutStrokeOptions.IncludeComputedStroke = false;
        SvgDocumentBoundsResult withoutStroke = strokeDocument.GetDrawingBounds(withoutStrokeOptions);
        SvgResolvedDocument effectDocument = Resolve("<svg xmlns='http://www.w3.org/2000/svg' width='20' height='20'><rect x='1' y='1' width='2' height='2'/></svg>");
        SvgResolvedNode rectangle = effectDocument.Nodes.Single(node => node.Kind == SvgResolvedNodeKind.Shape);
        SvgResolvedNodeBoundsDetails details = new SvgResolvedNodeBoundsDetails(rectangle);
        details.FilterBounds = SvgRect.Create(10, 10, 4, 4);
        details.ClipBounds = SvgRect.Create(11, 11, 2, 2);
        SvgDocumentBoundsOptions effectOptions = new SvgDocumentBoundsOptions();
        effectOptions.Details.Add(details);

        SvgDocumentBoundsResult effect = effectDocument.GetDrawingBounds(effectOptions);
        details.MaskBounds = SvgRect.Create(0, 0, 0, 0, true);
        SvgDocumentBoundsResult masked = effectDocument.GetDrawingBounds(effectOptions);

        Assert.Multiple(() =>
        {
            AssertDocumentBounds(stroke, 2, 3, 2, 4);
            Assert.That(withoutStroke.Success, Is.True);
            Assert.That(withoutStroke.Bounds.IsEmpty, Is.True);
            AssertDocumentBounds(effect, 11, 11, 2, 2);
            Assert.That(effect.FilteredNodeCount, Is.EqualTo(1));
            Assert.That(masked.Bounds.IsEmpty, Is.True);
            Assert.That(masked.MaskedNodeCount, Is.EqualTo(1));
            Assert.That(masked.ContributingNodeCount, Is.EqualTo(0));
        });
    }

    [Test]
    public void InvalidMutationAndTraversalLimitsReturnStableFailures()
    {
        SvgPathData invalidPath = Parse("M0 0 L1 1");
        invalidPath.Commands[1].Values[0] = double.NaN;
        SvgDrawingBoundsOptions invalidTransform = new SvgDrawingBoundsOptions();
        invalidTransform.Transform = new SvgMatrix(1, 0, 0, 1, 0, 0);
        invalidTransform.Transform.A = double.NaN;
        SvgResolvedDocument document = Resolve("<svg xmlns='http://www.w3.org/2000/svg'><rect width='2' height='2'/></svg>");
        SvgDocumentBoundsOptions limited = new SvgDocumentBoundsOptions();
        limited.MaximumNodes = 1;

        Assert.Multiple(() =>
        {
            AssertFailure(invalidPath.GetFillBounds(), SvgDrawingBoundsError.InvalidPath);
            AssertFailure(Parse("M0 0 L1 1").GetDrawingBounds(invalidTransform), SvgDrawingBoundsError.InvalidArgument);
            AssertFailure(new SvgDrawingBounds().Shape(null, new SvgDrawingBoundsOptions()), SvgDrawingBoundsError.InvalidArgument);
            Assert.That(document.GetDrawingBounds(limited).Error, Is.EqualTo(SvgDrawingBoundsError.LimitExceeded));
            Assert.That(new SvgDrawingBounds().Document(null, new SvgDocumentBoundsOptions()).Error, Is.EqualTo(SvgDrawingBoundsError.InvalidArgument));
        });
    }

    static SvgMarkerDefinition Marker(SvgRect bounds)
    {
        SvgMarkerDefinition marker = new SvgMarkerDefinition();
        marker.Bounds = bounds;
        marker.OrientKind = SvgMarkerOrientKind.Angle;
        return marker;
    }

    static SvgPathData Parse(string source)
    {
        SvgPathParseResult result = new SvgPathParser().Parse(source, 128, 128);
        Assert.That(result.Success, Is.True);
        return result.PathData;
    }

    static SvgResolvedDocument Resolve(string source)
    {
        SvgLoadResult loaded = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(loaded.Success, Is.True);
        SvgResolvedDocument resolved = new SvgDocumentResolver().Resolve(loaded.Document);
        Assert.That(resolved.Root, Is.Not.Null);
        return resolved;
    }

    static void AssertBounds(SvgDrawingBoundsResult result, double x, double y, double width, double height, double tolerance = 0.0000000001)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.EqualTo(SvgDrawingBoundsError.None));
        AssertRect(result.Bounds, x, y, width, height, tolerance);
    }

    static void AssertDocumentBounds(SvgDocumentBoundsResult result, double x, double y, double width, double height)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.EqualTo(SvgDrawingBoundsError.None));
        AssertRect(result.Bounds, x, y, width, height);
    }

    static void AssertRect(SvgRect rectangle, double x, double y, double width, double height, double tolerance = 0.0000000001)
    {
        Assert.That(rectangle, Is.Not.Null);
        Assert.That(rectangle.IsEmpty, Is.False);
        Assert.That(rectangle.X, Is.EqualTo(x).Within(tolerance));
        Assert.That(rectangle.Y, Is.EqualTo(y).Within(tolerance));
        Assert.That(rectangle.Width, Is.EqualTo(width).Within(tolerance));
        Assert.That(rectangle.Height, Is.EqualTo(height).Within(tolerance));
    }

    static void AssertFailure(SvgDrawingBoundsResult result, SvgDrawingBoundsError error)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(error));
        Assert.That(result.Bounds, Is.Null);
    }
}
