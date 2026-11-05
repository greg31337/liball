namespace svg.Tests;

public class SvgEmbeddedContentResolverTests
{
    [Test]
    public void ImageAutoDimensionsUseIntrinsicSizeAndPercentagePosition()
    {
        SvgNode image = Load("<svg xmlns='http://www.w3.org/2000/svg'><image href='photo.png' x='10%' y='25%'/></svg>").DocumentElement.Children[0];
        SvgEmbeddedContentResolveOptions options = ViewportOptions(500, 200);
        options.IntrinsicSize = SvgIntrinsicSize.Create(640, 480);
        options.ImageContentAvailable = true;

        SvgEmbeddedContentResult result = new SvgEmbeddedContentResolver().ResolveWithNodeAndOptions(image, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Kind, Is.EqualTo(SvgEmbeddedContentKind.Image));
            Assert.That(result.X, Is.EqualTo(50));
            Assert.That(result.Y, Is.EqualTo(50));
            Assert.That(result.Width, Is.EqualTo(640));
            Assert.That(result.Height, Is.EqualTo(480));
            Assert.That(result.WidthAuto, Is.True);
            Assert.That(result.HeightAuto, Is.True);
            Assert.That(result.UsedDefaultObjectSize, Is.False);
            Assert.That(result.HasIntrinsicAspectRatio, Is.True);
            Assert.That(result.Renderable, Is.True);
            Assert.That(result.HostContent.Bounds, Is.SameAs(result.Bounds));
        });
    }

    [Test]
    public void AutoSizingUsesIntrinsicRatioAndDefaultObjectPlaceholder()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><image href='a' width='200'/><image href='b'/><foreignObject/></svg>");
        SvgIntrinsicSize ratioOnly = new SvgIntrinsicSize();
        ratioOnly.HasAspectRatio = true;
        ratioOnly.AspectRatio = 2;
        SvgEmbeddedContentResolveOptions ratioOptions = new SvgEmbeddedContentResolveOptions();
        ratioOptions.IntrinsicSize = ratioOnly;

        SvgEmbeddedContentResult oneAuto = new SvgEmbeddedContentResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[0], ratioOptions);
        SvgEmbeddedContentResult imageDefault = new SvgEmbeddedContentResolver().ResolveWithNode(document.DocumentElement.Children[1]);
        SvgEmbeddedContentResult foreignDefault = new SvgEmbeddedContentResolver().ResolveWithNode(document.DocumentElement.Children[2]);

        Assert.Multiple(() =>
        {
            Assert.That(oneAuto.Success, Is.True);
            Assert.That(oneAuto.Width, Is.EqualTo(200));
            Assert.That(oneAuto.Height, Is.EqualTo(100));
            Assert.That(oneAuto.UsedDefaultObjectSize, Is.False);
            Assert.That(imageDefault.Success, Is.True);
            Assert.That(imageDefault.Width, Is.EqualTo(300));
            Assert.That(imageDefault.Height, Is.EqualTo(150));
            Assert.That(imageDefault.UsedDefaultObjectSize, Is.True);
            Assert.That(imageDefault.ContentAvailable, Is.False);
            Assert.That(imageDefault.Renderable, Is.False);
            Assert.That(foreignDefault.Success, Is.True);
            Assert.That(foreignDefault.Width, Is.EqualTo(300));
            Assert.That(foreignDefault.Height, Is.EqualTo(150));
            Assert.That(foreignDefault.UsedDefaultObjectSize, Is.True);
        });
    }

    [Test]
    public void PreserveAspectRatioMeetAndSliceProduceDeterministicContentBounds()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><image href='a' x='10' y='20' width='200' height='200'/><image href='b' x='10' y='20' width='200' height='200' preserveAspectRatio='xMidYMid slice'/></svg>");
        SvgEmbeddedContentResolveOptions options = new SvgEmbeddedContentResolveOptions();
        options.IntrinsicSize = SvgIntrinsicSize.Create(400, 200);
        options.ImageContentAvailable = true;

        SvgEmbeddedContentResult meet = new SvgEmbeddedContentResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[0], options);
        SvgEmbeddedContentResult slice = new SvgEmbeddedContentResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[1], options);

        Assert.Multiple(() =>
        {
            Assert.That(meet.Success, Is.True);
            Assert.That(meet.AspectRatioApplied, Is.True);
            Assert.That(meet.ContentBounds.X, Is.EqualTo(10));
            Assert.That(meet.ContentBounds.Y, Is.EqualTo(70));
            Assert.That(meet.ContentBounds.Width, Is.EqualTo(200));
            Assert.That(meet.ContentBounds.Height, Is.EqualTo(100));
            Assert.That(meet.ContentTransform.A, Is.EqualTo(0.5));
            Assert.That(meet.ClipsOverflow, Is.True);
            Assert.That(meet.ClipBounds, Is.SameAs(meet.Bounds));
            Assert.That(slice.Success, Is.True);
            Assert.That(slice.ContentBounds.X, Is.EqualTo(-90));
            Assert.That(slice.ContentBounds.Y, Is.EqualTo(20));
            Assert.That(slice.ContentBounds.Width, Is.EqualTo(400));
            Assert.That(slice.ContentBounds.Height, Is.EqualTo(200));
            Assert.That(slice.ContentTransform.A, Is.EqualTo(1));
        });
    }

    [Test]
    public void ImageWithoutIntrinsicRatioFillsItsPositioningRectangle()
    {
        SvgNode image = Load("<svg xmlns='http://www.w3.org/2000/svg'><image href='photo.png' x='4' y='5' width='80' height='40' preserveAspectRatio='xMaxYMax slice'/></svg>").DocumentElement.Children[0];
        SvgEmbeddedContentResolveOptions options = new SvgEmbeddedContentResolveOptions();
        options.ImageContentAvailable = true;

        SvgEmbeddedContentResult result = new SvgEmbeddedContentResolver().ResolveWithNodeAndOptions(image, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.AspectRatio.Alignment, Is.EqualTo(SvgAspectRatioAlignment.XMaxYMax));
            Assert.That(result.AspectRatioApplied, Is.False);
            Assert.That(result.ContentBounds.X, Is.EqualTo(4));
            Assert.That(result.ContentBounds.Y, Is.EqualTo(5));
            Assert.That(result.ContentBounds.Width, Is.EqualTo(80));
            Assert.That(result.ContentBounds.Height, Is.EqualTo(40));
            Assert.That(result.Renderable, Is.True);
        });
    }

    [Test]
    public void ExplicitIntrinsicRatioControlsFittingWhenDimensionsHaveAnotherRatio()
    {
        SvgNode image = Load("<svg xmlns='http://www.w3.org/2000/svg'><image href='nested.svg' width='200' height='200'/></svg>").DocumentElement.Children[0];
        SvgIntrinsicSize intrinsic = SvgIntrinsicSize.Create(100, 100);
        intrinsic.AspectRatio = 2;
        SvgEmbeddedContentResolveOptions options = new SvgEmbeddedContentResolveOptions();
        options.IntrinsicSize = intrinsic;
        options.ImageContentAvailable = true;

        SvgEmbeddedContentResult result = new SvgEmbeddedContentResolver().ResolveWithNodeAndOptions(image, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.IntrinsicAspectRatio, Is.EqualTo(2));
            Assert.That(result.ContentBounds.X, Is.EqualTo(0));
            Assert.That(result.ContentBounds.Y, Is.EqualTo(50));
            Assert.That(result.ContentBounds.Width, Is.EqualTo(200));
            Assert.That(result.ContentBounds.Height, Is.EqualTo(100));
            Assert.That(result.ContentTransform.A, Is.EqualTo(2));
            Assert.That(result.ContentTransform.D, Is.EqualTo(1));
        });
    }

    [Test]
    public void ImageRetainsBothReferenceFormsAndCrossOriginMode()
    {
        SvgNode image = Load("<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink'><image href='new.png#part' xlink:href='old.png' crossorigin='use-credentials' width='10' height='20'/></svg>").DocumentElement.Children[0];
        SvgEmbeddedContentResolveOptions options = new SvgEmbeddedContentResolveOptions();
        options.ImageContentAvailable = true;

        SvgEmbeddedContentResult result = new SvgEmbeddedContentResolver().ResolveWithNodeAndOptions(image, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Href.Value, Is.EqualTo("new.png#part"));
            Assert.That(result.Href.Fragment, Is.EqualTo("part"));
            Assert.That(result.XLinkHref.Value, Is.EqualTo("old.png"));
            Assert.That(result.CrossOrigin, Is.EqualTo(SvgCrossOriginMode.UseCredentials));
            Assert.That(result.HostContent.Href, Is.SameAs(result.Href));
            Assert.That(result.HostContent.XLinkHref, Is.SameAs(result.XLinkHref));
            Assert.That(result.HostContent.CrossOrigin, Is.EqualTo(SvgCrossOriginMode.UseCredentials));
            Assert.That(result.Reference.AttributeKind, Is.EqualTo(SvgReferenceAttributeKind.Href));
            Assert.That(result.HostContent.Reference, Is.SameAs(result.Reference));
        });
    }

    [Test]
    public void ImageContentEligibilityUsesModernHrefPrecedence()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink'><image href='bad%2G' xlink:href='legacy.png' width='10' height='10'/><image xlink:href='legacy.png' width='10' height='10'/><image href='#missing' width='10' height='10'/></svg>");
        SvgEmbeddedContentResolveOptions options = new SvgEmbeddedContentResolveOptions();
        options.ImageContentAvailable = true;

        SvgEmbeddedContentResult invalidModern = new SvgEmbeddedContentResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[0], options);
        SvgEmbeddedContentResult legacyOnly = new SvgEmbeddedContentResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[1], options);
        SvgEmbeddedContentResult missingTarget = new SvgEmbeddedContentResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[2], options);

        Assert.Multiple(() =>
        {
            Assert.That(invalidModern.Success, Is.False);
            Assert.That(invalidModern.Reference.AttributeKind, Is.EqualTo(SvgReferenceAttributeKind.Href));
            Assert.That(invalidModern.Reference.XLinkHrefIgnored, Is.True);
            Assert.That(invalidModern.Href, Is.Null);
            Assert.That(invalidModern.XLinkHref.Value, Is.EqualTo("legacy.png"));
            Assert.That(invalidModern.HasContent, Is.False);
            Assert.That(invalidModern.Renderable, Is.False);
            Assert.That(legacyOnly.Success, Is.True);
            Assert.That(legacyOnly.Reference.AttributeKind, Is.EqualTo(SvgReferenceAttributeKind.XLinkHref));
            Assert.That(legacyOnly.HasContent, Is.True);
            Assert.That(legacyOnly.Renderable, Is.True);
            Assert.That(missingTarget.Success, Is.False);
            Assert.That(missingTarget.Reference.Status, Is.EqualTo(SvgReferenceStatus.TargetNotFound));
            Assert.That(missingTarget.HasContent, Is.False);
            Assert.That(missingTarget.Renderable, Is.False);
        });
    }

    [Test]
    public void ForeignObjectCreatesBoundedHostHandoffAndNeedsCallback()
    {
        SvgNode foreign = Load("<svg xmlns='http://www.w3.org/2000/svg'><foreignObject x='5' y='6' width='100' height='50'><div xmlns='http://www.w3.org/1999/xhtml'>Hello</div></foreignObject></svg>").DocumentElement.Children[0];

        SvgEmbeddedContentResult pending = new SvgEmbeddedContentResolver().ResolveWithNode(foreign);
        SvgEmbeddedContentResolveOptions readyOptions = new SvgEmbeddedContentResolveOptions();
        readyOptions.ForeignContentCallbackAvailable = true;
        SvgEmbeddedContentResult ready = new SvgEmbeddedContentResolver().ResolveWithNodeAndOptions(foreign, readyOptions);

        Assert.Multiple(() =>
        {
            Assert.That(pending.Success, Is.True);
            Assert.That(pending.HasContent, Is.True);
            Assert.That(pending.ContentAvailable, Is.False);
            Assert.That(pending.Renderable, Is.False);
            Assert.That(ready.Success, Is.True);
            Assert.That(ready.Renderable, Is.True);
            Assert.That(ready.ContentBounds, Is.SameAs(ready.Bounds));
            Assert.That(ready.ContentTransform.E, Is.EqualTo(5));
            Assert.That(ready.ContentTransform.F, Is.EqualTo(6));
            Assert.That(ready.ClipsOverflow, Is.False);
            Assert.That(ready.HostContent.RequiresHostProcessing, Is.True);
            Assert.That(ready.HostContent.Children, Has.Count.EqualTo(1));
            Assert.That(ready.HostContent.Children[0], Is.SameAs(foreign.Children[0]));
        });
    }

    [Test]
    public void DisplayVisibilityConditionsAndZeroGeometrySuppressRendering()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><image href='a' width='10' height='10' style='display:none'/><image href='b' width='10' height='10' visibility='hidden'/><image href='c' width='0' height='10'/><image href='d' width='10' height='10'/></svg>");
        SvgEmbeddedContentResolveOptions displayOptions = new SvgEmbeddedContentResolveOptions();
        displayOptions.ImageContentAvailable = true;
        displayOptions.ComputedStyle = new SvgStyleResolver().Resolve(document.DocumentElement.Children[0], new List<SvgStyleSheet>());
        SvgEmbeddedContentResolveOptions available = new SvgEmbeddedContentResolveOptions();
        available.ImageContentAvailable = true;
        SvgEmbeddedContentResolveOptions conditionOptions = new SvgEmbeddedContentResolveOptions();
        conditionOptions.ImageContentAvailable = true;
        conditionOptions.ConditionsSatisfied = false;

        SvgEmbeddedContentResult display = new SvgEmbeddedContentResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[0], displayOptions);
        SvgEmbeddedContentResult visibility = new SvgEmbeddedContentResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[1], available);
        SvgEmbeddedContentResult zero = new SvgEmbeddedContentResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[2], available);
        SvgEmbeddedContentResult condition = new SvgEmbeddedContentResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[3], conditionOptions);

        Assert.Multiple(() =>
        {
            Assert.That(display.Success, Is.True);
            Assert.That(display.UsesComputedGeometry, Is.True);
            Assert.That(display.Displayed, Is.False);
            Assert.That(display.Renderable, Is.False);
            Assert.That(visibility.Visible, Is.False);
            Assert.That(visibility.Renderable, Is.False);
            Assert.That(zero.Success, Is.True);
            Assert.That(zero.HasGeometry, Is.False);
            Assert.That(zero.Renderable, Is.False);
            Assert.That(condition.ConditionsSatisfied, Is.False);
            Assert.That(condition.HostContent.ConditionsSatisfied, Is.False);
            Assert.That(condition.Renderable, Is.False);
        });
    }

    [Test]
    public void InvalidDimensionsReferencesAndHostBoundsProduceLocatedFailures()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><image href='a' width='-1' height='10'/><image width='10' height='10'/><foreignObject width='10' height='10'><a/><b/></foreignObject></svg>");
        SvgEmbeddedContentResolveOptions limited = new SvgEmbeddedContentResolveOptions();
        limited.MaxHostContentNodes = 1;

        SvgEmbeddedContentResult negative = new SvgEmbeddedContentResolver().ResolveWithNode(document.DocumentElement.Children[0]);
        SvgEmbeddedContentResult missing = new SvgEmbeddedContentResolver().ResolveWithNode(document.DocumentElement.Children[1]);
        SvgEmbeddedContentResult bounded = new SvgEmbeddedContentResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[2], limited);

        Assert.Multiple(() =>
        {
            Assert.That(negative.Success, Is.False);
            Assert.That(negative.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InvalidValue));
            Assert.That(negative.Diagnostics.Single().Source, Is.SameAs(document.DocumentElement.Children[0].Attributes[1].SourceSpan));
            Assert.That(missing.Success, Is.False);
            Assert.That(missing.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InvalidReference));
            Assert.That(bounded.Success, Is.False);
            Assert.That(bounded.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
            Assert.That(bounded.HostContent.Children, Has.Count.EqualTo(1));
        });
    }

    static SvgEmbeddedContentResolveOptions ViewportOptions(double width, double height)
    {
        SvgEmbeddedContentResolveOptions options = new SvgEmbeddedContentResolveOptions();
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
