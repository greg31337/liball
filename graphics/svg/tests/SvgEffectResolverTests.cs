namespace svg.Tests;

public class SvgEffectResolverTests
{
    [Test]
    public void MarkerEstablishesViewportReferencePointStrokeScaleAndStartReverseOrientation()
    {
        SvgNode marker = Load("<svg xmlns='http://www.w3.org/2000/svg'><marker markerWidth='10' markerHeight='20' viewBox='0 0 10 20' refX='center' refY='center' orient='auto-start-reverse'><path d='M0 0L10 10'/></marker></svg>").DocumentElement.Children[0];
        SvgEffectResolveOptions options = UserOptions(300, 200);
        options.StrokeWidth = 2;
        options.MarkerPosition = new SvgPoint(100, 50);
        options.MarkerAutoAngle = 90;
        options.MarkerPlacement = SvgMarkerPlacement.Start;

        SvgEffectResult result = new SvgEffectResolver().ResolveWithNodeAndOptions(marker, options);
        SvgPointTransformResult reference = result.Transform.TransformPoint(new SvgPoint(result.RefX, result.RefY));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Kind, Is.EqualTo(SvgEffectKind.Marker));
            Assert.That(result.MarkerUnits, Is.EqualTo(SvgMarkerUnits.StrokeWidth));
            Assert.That(result.Viewport.Width, Is.EqualTo(10));
            Assert.That(result.Viewport.Height, Is.EqualTo(20));
            Assert.That(result.ViewBox.ToText(), Is.EqualTo("0 0 10 20"));
            Assert.That(result.RefX, Is.EqualTo(5));
            Assert.That(result.RefY, Is.EqualTo(10));
            Assert.That(result.OrientKind, Is.EqualTo(SvgMarkerOrientKind.AutoStartReverse));
            Assert.That(result.AppliedAngle, Is.EqualTo(270));
            Assert.That(reference.Success, Is.True);
            Assert.That(reference.Point.X, Is.EqualTo(100).Within(1e-10));
            Assert.That(reference.Point.Y, Is.EqualTo(50).Within(1e-10));
            Assert.That(result.ContentLengthContext.ViewportWidth, Is.EqualTo(10));
            Assert.That(result.ContentLengthContext.ViewportHeight, Is.EqualTo(20));
            Assert.That(result.ClipsOverflow, Is.True);
            Assert.That(result.ClipRect, Is.SameAs(result.Viewport));
            Assert.That(result.Renderable, Is.True);
        });
    }

    [Test]
    public void MarkerSupportsFixedAnglesAndUserSpaceUnits()
    {
        SvgNode marker = Load("<svg xmlns='http://www.w3.org/2000/svg'><marker markerUnits='userSpaceOnUse' markerWidth='4' markerHeight='6' refX='25%' refY='bottom' orient='.5turn' overflow='visible'><rect/></marker></svg>").DocumentElement.Children[0];
        SvgEffectResolveOptions options = UserOptions(100, 100);
        options.StrokeWidth = 8;

        SvgEffectResult result = new SvgEffectResolver().ResolveWithNodeAndOptions(marker, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.MarkerUnits, Is.EqualTo(SvgMarkerUnits.UserSpaceOnUse));
            Assert.That(result.OrientKind, Is.EqualTo(SvgMarkerOrientKind.Angle));
            Assert.That(result.OrientAngle, Is.EqualTo(180));
            Assert.That(result.AppliedAngle, Is.EqualTo(180));
            Assert.That(result.RefX, Is.EqualTo(1));
            Assert.That(result.RefY, Is.EqualTo(6));
            Assert.That(Math.Abs(result.Transform.A), Is.EqualTo(1).Within(1e-12));
            Assert.That(result.ClipsOverflow, Is.False);
            Assert.That(result.ClipRect, Is.Null);
        });
    }

    [Test]
    public void EmptyZeroAndNegativeMarkersHaveDefinedNonRenderingBehavior()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><marker id='empty'><g/></marker><marker id='zero' markerWidth='0'><rect/></marker><marker id='negative' markerHeight='-1'><rect/></marker></svg>");
        SvgEffectResolveOptions options = UserOptions(100, 100);

        SvgEffectResult empty = new SvgEffectResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[0], options);
        SvgEffectResult zero = new SvgEffectResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[1], options);
        SvgEffectResult negative = new SvgEffectResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[2], options);

        Assert.Multiple(() =>
        {
            Assert.That(empty.Success, Is.True);
            Assert.That(empty.Empty, Is.True);
            Assert.That(empty.Renderable, Is.False);
            Assert.That(zero.Success, Is.True);
            Assert.That(zero.Empty, Is.False);
            Assert.That(zero.Renderable, Is.False);
            Assert.That(negative.Success, Is.False);
            Assert.That(negative.Diagnostics.Any(item => item.Code == SvgDiagnosticCode.InvalidValue), Is.True);
            Assert.That(negative.Renderable, Is.False);
        });
    }

    [Test]
    public void ClipPathConvertsObjectBoundingBoxCoordinatesAndResolvesNestedClip()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><clipPath id='base'><rect/></clipPath><clipPath id='outer' clipPathUnits='objectBoundingBox' style='transform:translate(10%,20%);clip-path:url(#base)'><circle/></clipPath></svg>");
        SvgEffectResolveOptions options = BoundsOptions(10, 20, 200, 100);

        SvgEffectResult result = new SvgEffectResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[1], options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Kind, Is.EqualTo(SvgEffectKind.ClipPath));
            Assert.That(result.Units, Is.EqualTo(SvgPaintServerUnits.ObjectBoundingBox));
            Assert.That(result.ExplicitTransform.E, Is.EqualTo(0.1).Within(1e-12));
            Assert.That(result.ExplicitTransform.F, Is.EqualTo(0.2).Within(1e-12));
            Assert.That(result.Transform.A, Is.EqualTo(200));
            Assert.That(result.Transform.D, Is.EqualTo(100));
            Assert.That(result.Transform.E, Is.EqualTo(30).Within(1e-12));
            Assert.That(result.Transform.F, Is.EqualTo(40).Within(1e-12));
            Assert.That(result.References, Has.Count.EqualTo(1));
            Assert.That(result.References[0].Effect.Node, Is.SameAs(document.DocumentElement.Children[0]));
            Assert.That(result.References[0].Success, Is.True);
            Assert.That(result.Renderable, Is.True);
        });
    }

    [Test]
    public void ClipPathNestedReferencesHonorExternalBaseUriResolution()
    {
        SvgDocument external = Load("<svg xmlns='http://www.w3.org/2000/svg'><clipPath id='remote'><rect/></clipPath></svg>", "https://example.test/assets/effects.svg");
        SvgDocument source = Load("<svg xmlns='http://www.w3.org/2000/svg'><clipPath clip-path='url(assets/effects.svg#remote)'><circle/></clipPath></svg>", "https://example.test/root.svg");
        SvgEffectResolveOptions options = UserOptions(100, 100);
        options.ReferenceOptions.ExternalDocument = external;

        SvgEffectResult result = new SvgEffectResolver().ResolveWithNodeAndOptions(source.DocumentElement.Children[0], options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.References.Single().Reference.Status, Is.EqualTo(SvgReferenceStatus.Resolved));
            Assert.That(result.References.Single().Reference.ResolvedUri, Is.EqualTo("https://example.test/assets/effects.svg#remote"));
            Assert.That(result.References.Single().Effect.Node, Is.SameAs(external.DocumentElement.Children[0]));
        });
    }

    [Test]
    public void ClipPathUseMustDirectlyReferenceClipGeometry()
    {
        SvgDocument validDocument = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect id='shape'/><clipPath><use href='#shape'/></clipPath></svg>");
        SvgDocument invalidDocument = Load("<svg xmlns='http://www.w3.org/2000/svg'><g id='group'><rect/></g><clipPath><use href='#group'/></clipPath></svg>");
        SvgEffectResolveOptions options = UserOptions(100, 100);

        SvgEffectResult valid = new SvgEffectResolver().ResolveWithNodeAndOptions(validDocument.DocumentElement.Children[1], options);
        SvgEffectResult invalid = new SvgEffectResolver().ResolveWithNodeAndOptions(invalidDocument.DocumentElement.Children[1], options);

        Assert.Multiple(() =>
        {
            Assert.That(valid.Success, Is.True);
            Assert.That(valid.References.Single().Use.Success, Is.True);
            Assert.That(valid.Renderable, Is.True);
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.Diagnostics.Any(item => item.Message.Contains("shape or text")), Is.True);
            Assert.That(invalid.Renderable, Is.False);
        });
    }

    [Test]
    public void ClipPathCyclesAndDepthLimitsProduceStableErrors()
    {
        SvgDocument cycleDocument = Load("<svg xmlns='http://www.w3.org/2000/svg'><clipPath id='a' clip-path='url(#b)'><rect/></clipPath><clipPath id='b' clip-path='url(#a)'><rect/></clipPath></svg>");
        SvgDocument depthDocument = Load("<svg xmlns='http://www.w3.org/2000/svg'><clipPath id='a' clip-path='url(#b)'><rect/></clipPath><clipPath id='b' clip-path='url(#c)'><rect/></clipPath><clipPath id='c'><rect/></clipPath></svg>");
        SvgEffectResolveOptions cycleOptions = UserOptions(100, 100);
        SvgEffectResolveOptions depthOptions = UserOptions(100, 100);
        depthOptions.Limits.MaxEffectDepth = 1;
        SvgEffectResolveOptions countOptions = UserOptions(100, 100);
        countOptions.Limits.MaxCount = 1;

        SvgEffectResult cycle = new SvgEffectResolver().ResolveWithNodeAndOptions(cycleDocument.DocumentElement.Children[0], cycleOptions);
        SvgEffectResult depth = new SvgEffectResolver().ResolveWithNodeAndOptions(depthDocument.DocumentElement.Children[0], depthOptions);
        SvgEffectResult count = new SvgEffectResolver().ResolveWithNodeAndOptions(depthDocument.DocumentElement.Children[0], countOptions);

        Assert.Multiple(() =>
        {
            Assert.That(cycle.Success, Is.False);
            Assert.That(cycle.Diagnostics.Any(item => item.Code == SvgDiagnosticCode.ReferenceCycle), Is.True);
            Assert.That(cycle.References[0].Effect.References[0].Cycle, Is.True);
            Assert.That(depth.Success, Is.False);
            Assert.That(depth.Diagnostics.Any(item => item.Code == SvgDiagnosticCode.InputLimitExceeded), Is.True);
            Assert.That(count.Success, Is.False);
            Assert.That(count.Diagnostics.Any(item => item.Message.Contains("MaxCount")), Is.True);
        });
    }

    [Test]
    public void EmptyClipAndDegenerateObjectBoundingBoxClipEverything()
    {
        SvgNode emptyNode = Load("<svg xmlns='http://www.w3.org/2000/svg'><clipPath><g/></clipPath></svg>").DocumentElement.Children[0];
        SvgNode boundedNode = Load("<svg xmlns='http://www.w3.org/2000/svg'><clipPath clipPathUnits='objectBoundingBox'><rect/></clipPath></svg>").DocumentElement.Children[0];
        SvgNode emptyMaskNode = Load("<svg xmlns='http://www.w3.org/2000/svg'><mask><g/></mask></svg>").DocumentElement.Children[0];

        SvgEffectResult empty = new SvgEffectResolver().ResolveWithNodeAndOptions(emptyNode, UserOptions(100, 100));
        SvgEffectResult degenerate = new SvgEffectResolver().ResolveWithNodeAndOptions(boundedNode, BoundsOptions(5, 6, 0, 20));
        SvgEffectResult emptyMask = new SvgEffectResolver().ResolveWithNodeAndOptions(emptyMaskNode, BoundsOptions(0, 0, 100, 100));

        Assert.Multiple(() =>
        {
            Assert.That(empty.Success, Is.True);
            Assert.That(empty.Empty, Is.True);
            Assert.That(empty.Renderable, Is.False);
            Assert.That(degenerate.Success, Is.True);
            Assert.That(degenerate.Renderable, Is.False);
            Assert.That(emptyMask.Success, Is.True);
            Assert.That(emptyMask.Empty, Is.True);
            Assert.That(emptyMask.Renderable, Is.False);
        });
    }

    [Test]
    public void MaskDefaultsConvertRegionAndObjectBoundingBoxContent()
    {
        SvgNode mask = Load("<svg xmlns='http://www.w3.org/2000/svg'><mask maskContentUnits='objectBoundingBox' mask-type='alpha'><rect/></mask></svg>").DocumentElement.Children[0];

        SvgEffectResult result = new SvgEffectResolver().ResolveWithNodeAndOptions(mask, BoundsOptions(100, 50, 200, 100));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Units, Is.EqualTo(SvgPaintServerUnits.ObjectBoundingBox));
            Assert.That(result.ContentUnits, Is.EqualTo(SvgPaintServerUnits.ObjectBoundingBox));
            Assert.That(result.Region.X, Is.EqualTo(-0.1));
            Assert.That(result.Region.Y, Is.EqualTo(-0.1));
            Assert.That(result.Region.Width, Is.EqualTo(1.2));
            Assert.That(result.Region.Height, Is.EqualTo(1.2));
            Assert.That(result.UserRegion.X, Is.EqualTo(80).Within(1e-12));
            Assert.That(result.UserRegion.Y, Is.EqualTo(40).Within(1e-12));
            Assert.That(result.UserRegion.Width, Is.EqualTo(240).Within(1e-12));
            Assert.That(result.UserRegion.Height, Is.EqualTo(120).Within(1e-12));
            Assert.That(result.ContentTransform.A, Is.EqualTo(200));
            Assert.That(result.ContentTransform.D, Is.EqualTo(100));
            Assert.That(result.ContentTransform.E, Is.EqualTo(100));
            Assert.That(result.ContentTransform.F, Is.EqualTo(50));
            Assert.That(result.MaskType, Is.EqualTo(SvgMaskType.Alpha));
            Assert.That(result.Renderable, Is.True);
        });
    }

    [Test]
    public void UserSpaceMaskRegionAndNonpositiveDimensionsHaveDefinedBehavior()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><mask maskUnits='userSpaceOnUse' x='10%' y='20%' width='30%' height='40%'><rect/></mask><mask width='0'><rect/></mask><mask height='-1'><rect/></mask></svg>");
        SvgEffectResolveOptions options = UserOptions(200, 100);

        SvgEffectResult region = new SvgEffectResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[0], options);
        SvgEffectResult zero = new SvgEffectResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[1], BoundsOptions(0, 0, 100, 100));
        SvgEffectResult negative = new SvgEffectResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[2], BoundsOptions(0, 0, 100, 100));

        Assert.Multiple(() =>
        {
            Assert.That(region.Success, Is.True);
            Assert.That(region.UserRegion.X, Is.EqualTo(20));
            Assert.That(region.UserRegion.Y, Is.EqualTo(20));
            Assert.That(region.UserRegion.Width, Is.EqualTo(60));
            Assert.That(region.UserRegion.Height, Is.EqualTo(40));
            Assert.That(region.MaskType, Is.EqualTo(SvgMaskType.Luminance));
            Assert.That(zero.Success, Is.True);
            Assert.That(zero.Renderable, Is.False);
            Assert.That(negative.Success, Is.True);
            Assert.That(negative.Region, Is.Null);
            Assert.That(negative.Renderable, Is.False);
        });
    }

    [Test]
    public void NestedMaskReferencesResolveAndDetectCycles()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><mask id='a' mask='url(#b)'><rect/></mask><mask id='b' mask='url(#a)'><rect/></mask></svg>");

        SvgEffectResult result = new SvgEffectResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[0], BoundsOptions(0, 0, 100, 100));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.References[0].Effect.Kind, Is.EqualTo(SvgEffectKind.Mask));
            Assert.That(result.References[0].Effect.References[0].Cycle, Is.True);
            Assert.That(result.Diagnostics.Any(item => item.Code == SvgDiagnosticCode.ReferenceCycle), Is.True);
        });
    }

    [Test]
    public void ContentAndNestedMutationsInvalidateResolvedSnapshot()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><clipPath id='base'><rect/></clipPath><clipPath id='outer' clip-path='url(#base)'><circle/></clipPath></svg>");
        SvgEffectResult contentResult = new SvgEffectResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[0], UserOptions(100, 100));
        SvgEffectResult nestedResult = new SvgEffectResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[1], UserOptions(100, 100));

        document.DocumentElement.Children[0].Children[0].MarkDirty();

        Assert.Multiple(() =>
        {
            Assert.That(contentResult.IsCurrent(), Is.False);
            Assert.That(nestedResult.IsCurrent(), Is.False);
        });
    }

    static SvgEffectResolveOptions BoundsOptions(double x, double y, double width, double height)
    {
        SvgEffectResolveOptions options = UserOptions(width, height);
        options.TargetBounds = SvgRect.Create(x, y, width, height, width == 0 || height == 0);
        return options;
    }

    static SvgEffectResolveOptions UserOptions(double width, double height)
    {
        SvgEffectResolveOptions options = new SvgEffectResolveOptions();
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
}
