namespace svg.Tests;

public class SvgTypedAttributeTests
{
    [Test]
    public void AccessIsLazyPreservesAuthoredTextAndCachesByNodeVersion()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect x='1&#48;px'/></svg>");
        SvgNode rect = document.DocumentElement.Children[0];
        SvgTypedAttributeAccessor accessor = new SvgTypedAttributeAccessor();

        Assert.That(accessor.GetStatistics().ParseCount, Is.Zero);

        SvgTypedAttributeValue first = accessor.Get(rect, "x");
        SvgTypedAttributeValue cached = accessor.Get(rect, "x");
        SvgLength firstLength = (SvgLength)first.Value;
        SvgAttribute replacement = new SvgAttribute("x", "", "x", "", " 20% ");
        SvgMutationResult mutation = new SvgNodeApi().ReplaceAttribute(rect, rect.Attributes.Single(), replacement);
        SvgTypedAttributeValue changed = accessor.Get(rect, "x");
        SvgLength changedLength = (SvgLength)changed.Value;
        SvgTypedAttributeStatistics statistics = accessor.GetStatistics();

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(first.ValueKind, Is.EqualTo(SvgTypedAttributeKind.Length));
            Assert.That(first.OriginalText, Is.EqualTo("1&#48;px"));
            Assert.That(first.EffectiveText, Is.EqualTo("10px"));
            Assert.That(firstLength.Value, Is.EqualTo(10));
            Assert.That(firstLength.Unit, Is.EqualTo(SvgLengthUnit.Px));
            Assert.That(cached, Is.SameAs(first));
            Assert.That(mutation.Success, Is.True);
            Assert.That(changed, Is.Not.SameAs(first));
            Assert.That(changed.OriginalText, Is.EqualTo(" 20% "));
            Assert.That(changedLength.Value, Is.EqualTo(20));
            Assert.That(changedLength.Unit, Is.EqualTo(SvgLengthUnit.Percentage));
            Assert.That(changed.NodeVersion, Is.EqualTo(rect.Version));
            Assert.That(statistics.CacheHits, Is.EqualTo(1));
            Assert.That(statistics.CacheMisses, Is.EqualTo(2));
            Assert.That(statistics.ParseCount, Is.EqualTo(2));
            Assert.That(statistics.EntryCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void CatalogDefaultsIncludeLiteralRelatedAndContextDependentOrigins()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><radialGradient cx='3&#48;%'/><rect/></svg>");
        SvgNode root = document.DocumentElement;
        SvgNode gradient = root.Children[0];
        SvgNode rect = root.Children[1];
        SvgTypedAttributeAccessor accessor = new SvgTypedAttributeAccessor();

        SvgTypedAttributeValue fx = accessor.Get(gradient, "fx");
        SvgTypedAttributeValue fy = accessor.Get(gradient, "fy");
        SvgTypedAttributeValue width = accessor.Get(rect, "width");
        SvgTypedAttributeValue viewportWidth = accessor.Get(root, "width");

        Assert.Multiple(() =>
        {
            Assert.That(fx.Success, Is.True);
            Assert.That(fx.Origin, Is.EqualTo(SvgTypedAttributeOrigin.OtherAttributeDefault));
            Assert.That(fx.Attribute, Is.Null);
            Assert.That(fx.SourceAttribute, Is.SameAs(gradient.Attributes.Single()));
            Assert.That(fx.OriginalText, Is.Empty);
            Assert.That(fx.EffectiveText, Is.EqualTo("30%"));
            Assert.That(((SvgLength)fx.Value).Value, Is.EqualTo(30));
            Assert.That(((SvgLength)fx.Value).Unit, Is.EqualTo(SvgLengthUnit.Percentage));
            Assert.That(fy.Origin, Is.EqualTo(SvgTypedAttributeOrigin.OtherAttributeDefault));
            Assert.That(fy.SourceAttribute, Is.Null);
            Assert.That(fy.EffectiveText, Is.EqualTo("50%"));
            Assert.That(width.Origin, Is.EqualTo(SvgTypedAttributeOrigin.LiteralDefault));
            Assert.That(width.ValueKind, Is.EqualTo(SvgTypedAttributeKind.Keyword));
            Assert.That(width.Value, Is.EqualTo("auto"));
            Assert.That(viewportWidth.Success, Is.True);
            Assert.That(viewportWidth.Origin, Is.EqualTo(SvgTypedAttributeOrigin.ContextDependentDefault));
            Assert.That(viewportWidth.ValueKind, Is.EqualTo(SvgTypedAttributeKind.ContextDependent));
            Assert.That(viewportWidth.HasValue, Is.False);
        });
    }

    [Test]
    public void CentralTaggedAccessCoversGeometryReferenceAndTextValueFamilies()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 1 20 30' preserveAspectRatio='xMaxYMin slice'><g transform='translate(2 3)'><path d='M0 0L4 5' pathLength='9'/><polygon points='0,0 4,0 4,3'/><text x='1 2%' rotate='10 20'>x</text><image href='#asset'/></g></svg>");
        SvgNode root = document.DocumentElement;
        SvgNode group = root.Children[0];
        SvgNode path = group.Children[0];
        SvgNode polygon = group.Children[1];
        SvgNode text = group.Children[2];
        SvgNode image = group.Children[3];
        SvgTypedAttributeAccessor accessor = new SvgTypedAttributeAccessor();

        SvgTypedAttributeValue viewBox = accessor.Get(root, "viewBox");
        SvgTypedAttributeValue aspect = accessor.Get(root, "preserveAspectRatio");
        SvgTypedAttributeValue transform = accessor.Get(group, "transform");
        SvgTypedAttributeValue data = accessor.Get(path, "d");
        SvgTypedAttributeValue pathLength = accessor.Get(path, "pathLength");
        SvgTypedAttributeValue points = accessor.Get(polygon, "points");
        SvgTypedAttributeValue positions = accessor.Get(text, "x");
        SvgTypedAttributeValue rotations = accessor.Get(text, "rotate");
        SvgTypedAttributeValue href = accessor.Get(image, "href");

        Assert.Multiple(() =>
        {
            Assert.That(viewBox.ValueKind, Is.EqualTo(SvgTypedAttributeKind.ViewBox));
            Assert.That(((SvgViewBox)viewBox.Value).Height, Is.EqualTo(30));
            Assert.That(aspect.ValueKind, Is.EqualTo(SvgTypedAttributeKind.AspectRatio));
            Assert.That(transform.ValueKind, Is.EqualTo(SvgTypedAttributeKind.TransformList));
            Assert.That(((SvgTransformList)transform.Value).Items, Has.Count.EqualTo(1));
            Assert.That(data.ValueKind, Is.EqualTo(SvgTypedAttributeKind.PathData));
            Assert.That(((SvgPathData)data.Value).Commands, Has.Count.EqualTo(2));
            Assert.That(pathLength.ValueKind, Is.EqualTo(SvgTypedAttributeKind.PathLength));
            Assert.That(((SvgPathLength)pathLength.Value).Value, Is.EqualTo(9));
            Assert.That(points.ValueKind, Is.EqualTo(SvgTypedAttributeKind.Points));
            Assert.That((double[])points.Value, Is.EqualTo(new[] { 0.0, 0.0, 4.0, 0.0, 4.0, 3.0 }));
            Assert.That(positions.ValueKind, Is.EqualTo(SvgTypedAttributeKind.LengthList));
            Assert.That(((SvgLengthListValue)positions.Value).Items, Has.Count.EqualTo(2));
            Assert.That(rotations.ValueKind, Is.EqualTo(SvgTypedAttributeKind.AngleList));
            Assert.That((List<SvgRotateItem>)rotations.Value, Has.Count.EqualTo(2));
            Assert.That(href.ValueKind, Is.EqualTo(SvgTypedAttributeKind.Iri));
            Assert.That(((SvgIriReference)href.Value).Fragment, Is.EqualTo("asset"));
            Assert.That(new[] { viewBox, aspect, transform, data, pathLength, points, positions, rotations, href }.All(value => value.Success), Is.True);
        });
    }

    [Test]
    public void InvalidAndMissingRequiredValuesProduceLocatedDiagnostics()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect x='oops'/><image/></svg>");
        SvgNode rect = document.DocumentElement.Children[0];
        SvgNode image = document.DocumentElement.Children[1];
        SvgTypedAttributeAccessor accessor = new SvgTypedAttributeAccessor();

        SvgTypedAttributeValue invalid = accessor.Get(rect, "x");
        SvgTypedAttributeValue missing = accessor.Get(image, "href");

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.ErrorOffset, Is.Zero);
            Assert.That(invalid.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(invalid.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.InvalidValue));
            Assert.That(invalid.Diagnostics[0].Source, Is.Not.Null);
            Assert.That(invalid.Diagnostics[0].Source.Source, Is.SameAs(rect.Attributes[0].SourceSpan.Source));
            Assert.That(invalid.Diagnostics[0].Source.StartOffset, Is.GreaterThan(rect.Attributes[0].SourceSpan.StartOffset));
            Assert.That(invalid.Diagnostics[0].Source.EndOffset - invalid.Diagnostics[0].Source.StartOffset, Is.EqualTo(1));
            Assert.That(missing.Success, Is.False);
            Assert.That(missing.HasValue, Is.False);
            Assert.That(missing.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(missing.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.InvalidValue));
            Assert.That(missing.Diagnostics[0].Source, Is.SameAs(image.SourceSpan));
        });
    }

    [Test]
    public void RelatedDefaultsInvalidateWhenTheirSourceAttributeChanges()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><radialGradient cx='10%'/></svg>");
        SvgNode gradient = document.DocumentElement.Children[0];
        SvgTypedAttributeAccessor accessor = new SvgTypedAttributeAccessor();
        SvgTypedAttributeValue first = accessor.Get(gradient, "fx");

        SvgAttribute replacement = new SvgAttribute("cx", "", "cx", "", "75%");
        new SvgNodeApi().ReplaceAttribute(gradient, gradient.Attributes[0], replacement);
        SvgTypedAttributeValue changed = accessor.Get(gradient, "fx");

        Assert.Multiple(() =>
        {
            Assert.That(changed, Is.Not.SameAs(first));
            Assert.That(((SvgLength)first.Value).Value, Is.EqualTo(10));
            Assert.That(((SvgLength)changed.Value).Value, Is.EqualTo(75));
            Assert.That(changed.SourceAttribute, Is.SameAs(replacement));
            Assert.That(changed.NodeVersion, Is.EqualTo(gradient.Version));
        });
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True);
        return result.Document;
    }
}
