namespace svg.Tests;

public class SvgHitTestingTests
{
    [Test]
    public void GeometryHitTestingCombinesFillStrokeAndTransforms()
    {
        SvgPathData square = Parse("M0 0 H10 V10 H0 Z");
        SvgGeometryHitTestOptions options = new SvgGeometryHitTestOptions();
        options.PointerEvents = SvgPointerEventsMode.All;
        options.FillPainted = false;
        options.StrokePainted = false;
        options.Stroke = Stroke(2);
        options.Transform = new SvgMatrix(1, 0, 0, 1, 20, 30);

        SvgGeometryHitTestResult fill = square.HitTest(new SvgPoint(25, 35), options);
        SvgGeometryHitTestResult stroke = square.HitTest(new SvgPoint(19.5, 35), options);
        SvgGeometryHitTestResult outside = square.HitTest(new SvgPoint(18, 35), options);

        Assert.Multiple(() =>
        {
            AssertHit(fill, true);
            Assert.That(fill.FillHit, Is.True);
            AssertHit(stroke, true);
            Assert.That(stroke.StrokeHit, Is.True);
            AssertHit(outside, false);
        });
    }

    [Test]
    public void PointerEventModesHonorVisibilityAndPaintIndependently()
    {
        SvgPathData square = Parse("M0 0 H10 V10 H0 Z");
        SvgGeometryHitTestOptions options = new SvgGeometryHitTestOptions();
        options.Visible = false;
        options.FillPainted = true;
        options.PointerEvents = SvgPointerEventsMode.VisiblePainted;
        SvgGeometryHitTestResult hiddenVisiblePainted = square.HitTest(new SvgPoint(5, 5), options);
        options.PointerEvents = SvgPointerEventsMode.Painted;
        SvgGeometryHitTestResult hiddenPainted = square.HitTest(new SvgPoint(5, 5), options);
        options.Visible = true;
        options.FillPainted = false;
        options.PointerEvents = SvgPointerEventsMode.VisibleFill;
        SvgGeometryHitTestResult unpaintedVisibleFill = square.HitTest(new SvgPoint(5, 5), options);
        options.PointerEvents = SvgPointerEventsMode.VisiblePainted;
        SvgGeometryHitTestResult unpaintedVisiblePainted = square.HitTest(new SvgPoint(5, 5), options);
        options.PointerEvents = SvgPointerEventsMode.None;
        SvgGeometryHitTestResult none = square.HitTest(new SvgPoint(5, 5), options);

        Assert.Multiple(() =>
        {
            AssertHit(hiddenVisiblePainted, false);
            AssertHit(hiddenPainted, true);
            AssertHit(unpaintedVisibleFill, true);
            AssertHit(unpaintedVisiblePainted, false);
            AssertHit(none, false);
        });
    }

    [Test]
    public void BoundingBoxModeCanHitOutsideActualFill()
    {
        SvgPathData triangle = Parse("M0 0 L10 0 L0 10 Z");
        SvgPoint point = new SvgPoint(9, 9);
        SvgGeometryHitTestOptions fillOptions = new SvgGeometryHitTestOptions();
        fillOptions.PointerEvents = SvgPointerEventsMode.Fill;
        SvgGeometryHitTestOptions boundsOptions = new SvgGeometryHitTestOptions();
        boundsOptions.PointerEvents = SvgPointerEventsMode.BoundingBox;

        SvgGeometryHitTestResult fill = triangle.HitTest(point, fillOptions);
        SvgGeometryHitTestResult bounds = triangle.HitTest(point, boundsOptions);

        Assert.Multiple(() =>
        {
            AssertHit(fill, false);
            AssertHit(bounds, true);
            Assert.That(bounds.BoundingBoxHit, Is.True);
        });
    }

    [Test]
    public void ClipAndMaskGeometryRejectOtherwiseHitPoints()
    {
        SvgPathData square = Parse("M0 0 H10 V10 H0 Z");
        SvgGeometryHitTestOptions clippedOptions = new SvgGeometryHitTestOptions();
        clippedOptions.Clips.Add(RectRegion(SvgRect.Create(0, 0, 4, 10)));
        SvgGeometryHitTestOptions maskedOptions = new SvgGeometryHitTestOptions();
        maskedOptions.Masks.Add(PathRegion(Parse("M0 0 H10 V4 H0 Z")));

        SvgGeometryHitTestResult clipped = square.HitTest(new SvgPoint(8, 2), clippedOptions);
        SvgGeometryHitTestResult masked = square.HitTest(new SvgPoint(2, 8), maskedOptions);
        SvgGeometryHitTestResult accepted = square.HitTest(new SvgPoint(2, 2), maskedOptions);

        Assert.Multiple(() =>
        {
            AssertHit(clipped, false);
            Assert.That(clipped.ClippedOut, Is.True);
            AssertHit(masked, false);
            Assert.That(masked.MaskedOut, Is.True);
            AssertHit(accepted, true);
        });
    }

    [Test]
    public void DocumentHitTestingReturnsTopmostAndAllInPaintingOrder()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' width='30' height='30'><rect id='bottom' width='15' height='15'/><rect id='middle' x='2' y='2' width='15' height='15'/><rect id='top' x='4' y='4' width='15' height='15'/></svg>";
        SvgResolvedDocument document = Resolve(source);

        SvgDocumentHitTestResult topmost = document.HitTest(new SvgPoint(8, 8));
        SvgDocumentHitTestResult all = document.HitTestAll(new SvgPoint(8, 8));
        SvgDocumentHitTestOptions limitedOptions = new SvgDocumentHitTestOptions();
        limitedOptions.ReturnAll = true;
        limitedOptions.MaximumResults = 2;
        SvgDocumentHitTestResult limited = document.HitTestWithOptions(new SvgPoint(8, 8), limitedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(topmost.Success, Is.True);
            Assert.That(topmost.Matches, Has.Count.EqualTo(1));
            Assert.That(Id(topmost.Topmost), Is.EqualTo("top"));
            Assert.That(all.Success, Is.True);
            Assert.That(all.Matches.Select(Id), Is.EqualTo(new[] { "top", "middle", "bottom" }));
            Assert.That(all.Topmost, Is.SameAs(all.Matches[0]));
            Assert.That(limited.Matches.Select(Id), Is.EqualTo(new[] { "top", "middle" }));
            Assert.That(limited.Truncated, Is.True);
        });
    }

    [Test]
    public void DocumentModesTransformsAndViewportClipsAreHonored()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' width='20' height='20' overflow='hidden'><rect id='hiddenAll' width='10' height='10' visibility='hidden' pointer-events='all'/><rect id='disabled' width='10' height='10' pointer-events='none'/><g transform='translate(10 0)'><rect id='moved' width='5' height='5'/></g><line id='unpaintedStroke' x1='0' y1='15' x2='10' y2='15' stroke='none' stroke-width='4' pointer-events='stroke'/><rect id='clipped' x='25' width='5' height='5'/></svg>";
        SvgResolvedDocument document = Resolve(source);

        SvgDocumentHitTestResult hidden = document.HitTest(new SvgPoint(2, 2));
        SvgDocumentHitTestResult moved = document.HitTest(new SvgPoint(12, 2));
        SvgDocumentHitTestResult unpaintedStroke = document.HitTest(new SvgPoint(5, 16));
        SvgDocumentHitTestResult clipped = document.HitTest(new SvgPoint(27, 2));
        SvgDocumentHitTestOptions withoutViewportClip = new SvgDocumentHitTestOptions();
        withoutViewportClip.IncludeViewportClips = false;
        SvgDocumentHitTestResult unclipped = document.HitTestWithOptions(new SvgPoint(27, 2), withoutViewportClip);

        Assert.Multiple(() =>
        {
            Assert.That(Id(hidden.Topmost), Is.EqualTo("hiddenAll"));
            Assert.That(hidden.Topmost.PointerEvents, Is.EqualTo(SvgPointerEventsMode.All));
            Assert.That(Id(moved.Topmost), Is.EqualTo("moved"));
            Assert.That(Id(unpaintedStroke.Topmost), Is.EqualTo("unpaintedStroke"));
            Assert.That(unpaintedStroke.Topmost.StrokeHit, Is.True);
            Assert.That(clipped.Topmost, Is.Null);
            Assert.That(Id(unclipped.Topmost), Is.EqualTo("clipped"));
        });
    }

    [Test]
    public void DocumentDetailsApplyMasksAndReportUseInstanceIdentity()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' width='30' height='20'><defs><rect id='source' width='6' height='6'/></defs><use href='#source' x='10'/></svg>";
        SvgResolvedDocument document = Resolve(source);
        SvgResolvedNode instance = document.Nodes.Single(node => node.Kind == SvgResolvedNodeKind.Shape && node.InstancePath.Length != 0);
        SvgResolvedNodeHitTestDetails details = new SvgResolvedNodeHitTestDetails(instance);
        details.Masks.Add(RectRegion(SvgRect.Create(10, 0, 2, 6)));
        SvgDocumentHitTestOptions options = new SvgDocumentHitTestOptions();
        options.Details.Add(details);

        SvgDocumentHitTestResult masked = document.HitTestWithOptions(new SvgPoint(14, 2), options);
        SvgDocumentHitTestResult hit = document.HitTestWithOptions(new SvgPoint(11, 2), options);

        Assert.Multiple(() =>
        {
            Assert.That(masked.Topmost, Is.Null);
            Assert.That(hit.Topmost, Is.Not.Null);
            Assert.That(hit.Topmost.IsInstance, Is.True);
            Assert.That(hit.Topmost.SourceNode, Is.SameAs(instance.SourceNode));
            Assert.That(hit.Topmost.InstancePath, Is.EqualTo(instance.InstancePath));
            Assert.That(hit.Topmost.SourcePath, Is.EqualTo(instance.SourcePath));
        });
    }

    [Test]
    public void InvalidMutationAndLimitsReturnStableFailures()
    {
        SvgPathData invalidPath = Parse("M0 0 L1 1");
        invalidPath.Commands[1].Values[0] = double.NaN;
        SvgGeometryHitTestOptions invalidMode = new SvgGeometryHitTestOptions();
        invalidMode.PointerEvents = (SvgPointerEventsMode)99;
        SvgGeometryHitTestOptions invalidRegion = new SvgGeometryHitTestOptions();
        invalidRegion.Clips.Add(new SvgHitTestRegion());
        SvgResolvedDocument document = Resolve("<svg xmlns='http://www.w3.org/2000/svg'><rect width='2' height='2'/></svg>");
        SvgDocumentHitTestOptions limited = new SvgDocumentHitTestOptions();
        limited.MaximumNodes = 1;

        Assert.Multiple(() =>
        {
            AssertGeometryFailure(invalidPath.HitTest(new SvgPoint(0, 0), new SvgGeometryHitTestOptions()), SvgHitTestError.InvalidPath);
            AssertGeometryFailure(Parse("M0 0 L1 1").HitTest(new SvgPoint(0, 0), invalidMode), SvgHitTestError.InvalidArgument);
            AssertGeometryFailure(Parse("M0 0 L1 1").HitTest(new SvgPoint(0, 0), invalidRegion), SvgHitTestError.InvalidArgument);
            Assert.That(document.HitTestWithOptions(new SvgPoint(10, 10), limited).Error, Is.EqualTo(SvgHitTestError.LimitExceeded));
            Assert.That(new SvgHitTesting().Document(null, new SvgPoint(0, 0), new SvgDocumentHitTestOptions()).Error, Is.EqualTo(SvgHitTestError.InvalidArgument));
        });
    }

    static SvgStrokeGeometryOptions Stroke(double width)
    {
        SvgStrokeGeometryOptions options = new SvgStrokeGeometryOptions();
        options.Style.Width = width;
        return options;
    }

    static SvgHitTestRegion RectRegion(SvgRect bounds)
    {
        SvgHitTestRegion region = new SvgHitTestRegion();
        region.Bounds = bounds;
        return region;
    }

    static SvgHitTestRegion PathRegion(SvgPathData path)
    {
        SvgHitTestRegion region = new SvgHitTestRegion();
        region.PathData = path;
        return region;
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
        SvgResolvedDocument result = new SvgDocumentResolver().Resolve(loaded.Document);
        Assert.That(result.Root, Is.Not.Null);
        return result;
    }

    static string Id(SvgHitTestMatch match)
    {
        if (match == null || match.SourceNode == null) return "";
        int index = 0;
        while (index < match.SourceNode.Attributes.Count)
        {
            SvgAttribute attribute = match.SourceNode.Attributes[index];
            if (attribute.NamespaceUri.Length == 0 && attribute.LocalName == "id") return attribute.Value;
            index = index + 1;
        }
        return "";
    }

    static void AssertHit(SvgGeometryHitTestResult result, bool hit)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.EqualTo(SvgHitTestError.None));
        Assert.That(result.Hit, Is.EqualTo(hit));
    }

    static void AssertGeometryFailure(SvgGeometryHitTestResult result, SvgHitTestError error)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(error));
        Assert.That(result.Hit, Is.False);
    }
}
