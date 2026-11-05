namespace svg.Tests;

public class SvgFilterBuilderTests
{
    [Test]
    public void AddsEveryStandardPrimitiveAndItsStructuredChildren()
    {
        SvgBuilder document = TargetDocument();
        SvgNode target = document.Current;
        SvgFilterBuilder filter = new SvgFilterBuilder(document);
        double[] identity =
        {
            1, 0, 0, 0, 0,
            0, 1, 0, 0, 0,
            0, 0, 1, 0, 0,
            0, 0, 0, 1, 0
        };

        filter.Begin()
            .Flood("red", .5)
            .Blend("SourceGraphic", "multiply")
            .ColorMatrix("matrix", identity)
            .ComponentTransfer()
            .IdentityFunction(SvgElementKind.FeFuncR)
            .LinearFunction(SvgElementKind.FeFuncG, 1, 0)
            .TableFunction(SvgElementKind.FeFuncB, new double[] { 0, 1 })
            .GammaFunction(SvgElementKind.FeFuncA, 1, 1, 0)
            .Composite("SourceGraphic", "over")
            .ConvolveMatrix(1, 1, new double[] { 1 })
            .DiffuseLighting(1, 1).DistantLight(30, 45)
            .DisplacementMap("SourceGraphic", 2, "R", "G")
            .DropShadow(2, 3, 1, "black", .75)
            .GaussianBlurAxes(2, 3)
            .Image("#source-image");
        string imageResult = filter.LastResult;
        filter.Merge(new[] { "SourceGraphic", imageResult })
            .MorphologyAxes("dilate", 1, 2)
            .Offset(4, 5)
            .SpecularLighting(1, 2, 8).PointLight(1, 2, 3)
            .Tile()
            .Turbulence(.1, 3, 7, "fractalNoise", true);

        SvgNode built = filter.Build();
        List<SvgNode> primitives = built.Children.Where(item => item.Kind == SvgNodeKind.Element).ToList();
        SvgNode transfer = primitives.Single(item => item.ElementKind == SvgElementKind.FeComponentTransfer);
        SvgNode merge = primitives.Single(item => item.ElementKind == SvgElementKind.FeMerge);
        SvgNode diffuse = primitives.Single(item => item.ElementKind == SvgElementKind.FeDiffuseLighting);
        SvgNode specular = primitives.Single(item => item.ElementKind == SvgElementKind.FeSpecularLighting);

        Assert.Multiple(() =>
        {
            Assert.That(filter.Succeeded(), Is.True, filter.Message);
            Assert.That(filter.GetPrimitiveCount(), Is.EqualTo(17));
            Assert.That(primitives.Select(item => item.ElementKind), Is.EqualTo(new[]
            {
                SvgElementKind.FeFlood,
                SvgElementKind.FeBlend,
                SvgElementKind.FeColorMatrix,
                SvgElementKind.FeComponentTransfer,
                SvgElementKind.FeComposite,
                SvgElementKind.FeConvolveMatrix,
                SvgElementKind.FeDiffuseLighting,
                SvgElementKind.FeDisplacementMap,
                SvgElementKind.FeDropShadow,
                SvgElementKind.FeGaussianBlur,
                SvgElementKind.FeImage,
                SvgElementKind.FeMerge,
                SvgElementKind.FeMorphology,
                SvgElementKind.FeOffset,
                SvgElementKind.FeSpecularLighting,
                SvgElementKind.FeTile,
                SvgElementKind.FeTurbulence
            }));
            Assert.That(primitives.Select(item => Attribute(item, "result")), Is.EqualTo(Enumerable.Range(1, 17).Select(index => "svg-filter-result-" + index)));
            Assert.That(Attribute(primitives[1], "in"), Is.EqualTo("svg-filter-result-1"));
            Assert.That(Attribute(primitives[1], "in2"), Is.EqualTo("SourceGraphic"));
            Assert.That(Attribute(primitives[10], "in"), Is.Null);
            Assert.That(transfer.Children.Select(item => item.ElementKind), Is.EqualTo(new[] { SvgElementKind.FeFuncR, SvgElementKind.FeFuncG, SvgElementKind.FeFuncB, SvgElementKind.FeFuncA }));
            Assert.That(merge.Children.Select(item => Attribute(item, "in")), Is.EqualTo(new[] { "SourceGraphic", imageResult }));
            Assert.That(diffuse.Children.Single().ElementKind, Is.EqualTo(SvgElementKind.FeDistantLight));
            Assert.That(specular.Children.Single().ElementKind, Is.EqualTo(SvgElementKind.FePointLight));
            Assert.That(Attribute(target, "filter"), Is.EqualTo("url(#svg-filter-1)"));
            Assert.That(document.Document.Index.GetById("svg-filter-1"), Is.SameAs(built));
        });
    }

    [Test]
    public void ResolvesAutomaticAndExplicitNamedFilterGraphWiring()
    {
        SvgBuilder document = TargetDocument();
        SvgNode target = document.Current;
        SvgFilterBuilder filter = new SvgFilterBuilder(document);
        filter.Begin()
            .GaussianBlur(2).Result("blurred")
            .Offset(3, 4).Result("moved")
            .Flood("blue", .5).Result("paint")
            .Blend("moved", "screen");

        SvgFilterResolveOptions options = new SvgFilterResolveOptions();
        options.TargetBounds = SvgRect.Create(0, 0, 20, 10);
        SvgFilterResult resolved = new SvgFilterResolver().ResolveWithNodeAndOptions(filter.Filter, options);

        Assert.Multiple(() =>
        {
            Assert.That(filter.Succeeded(), Is.True, filter.Message);
            Assert.That(resolved.Success, Is.True, Diagnostics(resolved.Diagnostics));
            Assert.That(resolved.Primitives, Has.Count.EqualTo(4));
            Assert.That(resolved.Primitives.Select(item => item.ResultName), Is.EqualTo(new[] { "blurred", "moved", "paint", "svg-filter-result-4" }));
            Assert.That(resolved.Primitives[0].Inputs.Single().Name, Is.EqualTo("SourceGraphic"));
            Assert.That(resolved.Primitives[1].Inputs.Single().Name, Is.EqualTo("blurred"));
            Assert.That(resolved.Primitives[2].Inputs, Is.Empty);
            Assert.That(resolved.Primitives[3].Inputs.Select(item => item.Name), Is.EqualTo(new[] { "paint", "moved" }));
            Assert.That(resolved.Primitives[3].Inputs.All(item => item.Available), Is.True);
            Assert.That(Attribute(target, "filter"), Is.EqualTo("url(#svg-filter-1)"));
            Assert.That(resolved.Renderable, Is.True);
        });
    }

    [Test]
    public void AvoidsFilterIdCollisionsAndKeepsLimitAndInputFailuresAtomic()
    {
        SvgBuilder document = new SvgBuilder();
        document.BeginElement(SvgElementKind.Defs)
            .BeginElement(SvgElementKind.Filter).SetAttribute("id", "svg-filter-1").EndElement()
            .EndElement()
            .BeginElement(SvgElementKind.Rect).SetAttribute("width", "10").SetAttribute("height", "10");
        SvgFilterBuilder filter = new SvgFilterBuilder(document);
        filter.MaximumPrimitives = 1;
        filter.Begin().Flood("red", 1).GaussianBlur(1);

        Assert.Multiple(() =>
        {
            Assert.That(filter.Id, Is.EqualTo("svg-filter-2"));
            Assert.That(filter.Error, Is.EqualTo(SvgFilterBuilderError.PrimitiveLimitExceeded));
            Assert.That(filter.GetPrimitiveCount(), Is.EqualTo(1));
            Assert.That(filter.Filter.Children, Has.Count.EqualTo(1));
        });

        filter.ClearError();
        filter.MaximumPrimitives = 2;
        filter.Blend("missing-result", "screen");
        Assert.Multiple(() =>
        {
            Assert.That(filter.Error, Is.EqualTo(SvgFilterBuilderError.InvalidArgument));
            Assert.That(filter.GetPrimitiveCount(), Is.EqualTo(1));
            Assert.That(filter.Filter.Children, Has.Count.EqualTo(1));
        });

        filter.ClearError().Primitive(SvgElementKind.Rect);
        Assert.That(filter.Error, Is.EqualTo(SvgFilterBuilderError.InvalidArgument));

        SvgFilterBuilder missing = new SvgFilterBuilder(null);
        Assert.Multiple(() =>
        {
            Assert.That(missing.Error, Is.EqualTo(SvgFilterBuilderError.InvalidArgument));
            Assert.That(missing.Build(), Is.Null);
        });

        SvgBuilder resultDocument = TargetDocument();
        SvgFilterBuilder resultFilter = new SvgFilterBuilder(resultDocument);
        resultFilter.Begin().Flood("red", 1).Result("svg-filter-result-2").Offset(1, 1);
        Assert.Multiple(() =>
        {
            Assert.That(resultFilter.Succeeded(), Is.True, resultFilter.Message);
            Assert.That(Attribute(resultFilter.Filter.Children[0], "result"), Is.EqualTo("svg-filter-result-2"));
            Assert.That(Attribute(resultFilter.Filter.Children[1], "result"), Is.EqualTo("svg-filter-result-3"));
            Assert.That(Attribute(resultFilter.Filter.Children[1], "in"), Is.EqualTo("svg-filter-result-2"));
        });
    }

    static SvgBuilder TargetDocument()
    {
        SvgBuilder document = new SvgBuilder();
        document.BeginElement(SvgElementKind.Rect)
            .SetAttribute("id", "target")
            .SetAttribute("width", "20")
            .SetAttribute("height", "10");
        return document;
    }

    static string Attribute(SvgNode element, string name)
    {
        SvgAttribute attribute = element.Attributes.SingleOrDefault(item => item.NamespaceUri.Length == 0 && item.LocalName == name);
        return attribute == null ? null : attribute.Value;
    }

    static string Diagnostics(List<SvgDiagnostic> diagnostics)
    {
        return string.Join(" | ", diagnostics.Select(item => item.Message));
    }
}
