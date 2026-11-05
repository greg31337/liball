namespace svg.Tests;

public class SvgFilterGraphTests
{
    [Test]
    public void PlansPropagatedRegionsColorConversionsAndSafeLifetimeReuse()
    {
        SvgSceneResource filter = Filter(SvgRect.Create(0, 0, 8, 6));
        SvgFilterPrimitive[] references = References(5);
        SvgSceneFilterPrimitiveData first = Primitive(0, SvgElementKind.FeFlood, SvgFilterColorSpace.Srgb, SvgRect.Create(1, 1, 2, 2));
        first.ResultName = "kept";
        SvgSceneFilterPrimitiveData converted = Primitive(1, SvgElementKind.FeOffset, SvgFilterColorSpace.LinearRgb, null);
        converted.Inputs.Add(Input(SvgFilterInputKind.Previous, references[0]));
        SvgSceneFilterPrimitiveData independent = Primitive(2, SvgElementKind.FeFlood, SvgFilterColorSpace.Srgb, SvgRect.Create(0, 0, 8, 6));
        SvgSceneFilterPrimitiveData combined = Primitive(3, SvgElementKind.FeBlend, SvgFilterColorSpace.Srgb, SvgRect.Create(0, 0, 8, 6));
        combined.Inputs.Add(Input(SvgFilterInputKind.NamedResult, references[0]));
        combined.Inputs.Add(Input(SvgFilterInputKind.Previous, references[2]));
        SvgSceneFilterPrimitiveData final = Primitive(4, SvgElementKind.FeOffset, SvgFilterColorSpace.Srgb, SvgRect.Create(0, 0, 8, 6));
        final.Inputs.Add(Input(SvgFilterInputKind.Previous, references[3]));
        filter.Filter.Primitives.Add(first);
        filter.Filter.Primitives.Add(converted);
        filter.Filter.Primitives.Add(independent);
        filter.Filter.Primitives.Add(combined);
        filter.Filter.Primitives.Add(final);

        SvgFilterGraphPlan plan = new SvgFilterGraphPlanner().Plan(filter, new SvgFilterGraphOptions());

        Assert.Multiple(() =>
        {
            Assert.That(plan.Success, Is.True);
            Assert.That(plan.Steps, Has.Count.EqualTo(5));
            Assert.That(plan.Surfaces, Has.Count.EqualTo(4));
            Assert.That(plan.Steps[0].LastUseStep, Is.EqualTo(3));
            Assert.That(plan.Steps[1].Bounds.X, Is.EqualTo(1));
            Assert.That(plan.Steps[1].Bounds.Y, Is.EqualTo(1));
            Assert.That(plan.Steps[1].Bounds.Width, Is.EqualTo(2));
            Assert.That(plan.Steps[1].Bounds.Height, Is.EqualTo(2));
            Assert.That(plan.Steps[1].Inputs[0].RequiresColorConversion, Is.True);
            Assert.That(plan.Steps[3].OutputSurfaceIndex, Is.Not.EqualTo(plan.Steps[3].Inputs[0].SurfaceIndex));
            Assert.That(plan.Steps[3].OutputSurfaceIndex, Is.Not.EqualTo(plan.Steps[3].Inputs[1].SurfaceIndex));
            Assert.That(plan.Steps[4].OutputSurfaceIndex, Is.EqualTo(plan.Steps[0].OutputSurfaceIndex));
            Assert.That(plan.Steps[4].ReusesSurface, Is.True);
            Assert.That(plan.ReusedSurfaceCount, Is.EqualTo(1));
            Assert.That(plan.FinalSurfaceIndex, Is.EqualTo(plan.Steps[4].OutputSurfaceIndex));
            Assert.That(plan.Surfaces[plan.FinalSurfaceIndex].Uses.Select(item => item.PrimitiveIndex), Is.EqualTo(new[] { 0, 4 }));
        });
    }

    [Test]
    public void CapturesOnlyReferencedStandardInputsAsIndependentClippedSnapshots()
    {
        SvgSceneResource filter = Filter(SvgRect.Create(-1, 0, 4, 2));
        SvgSceneFilterPrimitiveData blend = Primitive(0, SvgElementKind.FeBlend, SvgFilterColorSpace.LinearRgb, SvgRect.Create(-5, -5, 20, 20));
        blend.Inputs.Add(Standard(SvgFilterStandardInput.SourceGraphic));
        blend.Inputs.Add(Standard(SvgFilterStandardInput.SourceAlpha));
        filter.Filter.Primitives.Add(blend);
        SvgPixelBuffer source = Surface(3, 2, SvgPixelColorSpace.Srgb);
        source.Write(0, 0, new SvgPremultipliedRgba8(10, 20, 30, 40));
        source.Write(1, 0, new SvgPremultipliedRgba8(50, 60, 70, 80));
        SvgFilterGraphOptions options = new SvgFilterGraphOptions();
        options.SourceGraphic = source;

        SvgFilterGraphPlan plan = new SvgFilterGraphPlanner().Plan(filter, options);
        source.Write(0, 0, new SvgPremultipliedRgba8(0, 0, 0, 0));
        SvgFilterSurface graphic = plan.Surfaces[plan.Steps[0].Inputs[0].SurfaceIndex];
        SvgFilterSurface alpha = plan.Surfaces[plan.Steps[0].Inputs[1].SurfaceIndex];

        Assert.Multiple(() =>
        {
            Assert.That(plan.Success, Is.True);
            Assert.That(plan.FilterBounds.X, Is.EqualTo(-1));
            Assert.That(plan.FilterBounds.Width, Is.EqualTo(4));
            Assert.That(plan.Steps[0].Bounds.X, Is.EqualTo(-1));
            Assert.That(plan.Steps[0].Bounds.Width, Is.EqualTo(4));
            Assert.That(plan.SnapshotCount, Is.EqualTo(2));
            Assert.That(plan.Surfaces, Has.Count.EqualTo(3));
            Assert.That(graphic.Kind, Is.EqualTo(SvgFilterSurfaceKind.StandardInput));
            Assert.That(graphic.Buffer.Read(0, 0).Pixel.Alpha, Is.Zero);
            Assert.That(graphic.Buffer.Read(1, 0).Pixel.Red, Is.EqualTo(10));
            Assert.That(graphic.Buffer.Read(1, 0).Pixel.Green, Is.EqualTo(20));
            Assert.That(graphic.Buffer.Read(1, 0).Pixel.Blue, Is.EqualTo(30));
            Assert.That(graphic.Buffer.Read(1, 0).Pixel.Alpha, Is.EqualTo(40));
            Assert.That(alpha.Buffer.Read(1, 0).Pixel.Red, Is.Zero);
            Assert.That(alpha.Buffer.Read(1, 0).Pixel.Green, Is.Zero);
            Assert.That(alpha.Buffer.Read(1, 0).Pixel.Blue, Is.Zero);
            Assert.That(alpha.Buffer.Read(1, 0).Pixel.Alpha, Is.EqualTo(40));
            Assert.That(plan.Steps[0].Inputs.All(item => item.RequiresColorConversion), Is.True);
        });
    }

    [Test]
    public void EmptyGraphReturnsAnOwnedSourceGraphicSnapshot()
    {
        SvgSceneResource filter = Filter(SvgRect.Create(0, 0, 2, 1));
        SvgPixelBuffer source = Surface(2, 1, SvgPixelColorSpace.Srgb);
        source.Write(1, 0, new SvgPremultipliedRgba8(8, 9, 10, 20));
        SvgFilterGraphOptions options = new SvgFilterGraphOptions();
        options.SourceGraphic = source;

        SvgFilterGraphPlan plan = new SvgFilterGraphPlanner().Plan(filter, options);

        Assert.Multiple(() =>
        {
            Assert.That(plan.Success, Is.True);
            Assert.That(plan.Steps, Is.Empty);
            Assert.That(plan.SnapshotCount, Is.EqualTo(1));
            Assert.That(plan.FinalSurfaceIndex, Is.EqualTo(0));
            Assert.That(plan.FinalBounds.Width, Is.EqualTo(2));
            Assert.That(plan.Surfaces[0].StandardInput, Is.EqualTo(SvgFilterStandardInput.SourceGraphic));
            Assert.That(plan.Surfaces[0].Buffer.Read(1, 0).Pixel.Blue, Is.EqualTo(10));
        });
    }

    [Test]
    public void RejectsSurfaceLimitsAndForwardGraphEdgesWithoutPartialPlans()
    {
        SvgSceneResource limitedFilter = Filter(SvgRect.Create(0, 0, 2, 2));
        SvgSceneFilterPrimitiveData blend = Primitive(0, SvgElementKind.FeBlend, SvgFilterColorSpace.Srgb, SvgRect.Create(0, 0, 2, 2));
        blend.Inputs.Add(Standard(SvgFilterStandardInput.SourceGraphic));
        blend.Inputs.Add(Standard(SvgFilterStandardInput.SourceAlpha));
        limitedFilter.Filter.Primitives.Add(blend);
        SvgFilterGraphOptions limitedOptions = new SvgFilterGraphOptions();
        limitedOptions.FilterLimits.MaxOffscreenSurfaces = 2;

        SvgFilterGraphPlan limited = new SvgFilterGraphPlanner().Plan(limitedFilter, limitedOptions);

        SvgSceneResource invalidFilter = Filter(SvgRect.Create(0, 0, 2, 2));
        SvgFilterPrimitive[] references = References(2);
        SvgSceneFilterPrimitiveData first = Primitive(0, SvgElementKind.FeOffset, SvgFilterColorSpace.Srgb, SvgRect.Create(0, 0, 2, 2));
        first.Inputs.Add(Input(SvgFilterInputKind.NamedResult, references[1]));
        invalidFilter.Filter.Primitives.Add(first);
        invalidFilter.Filter.Primitives.Add(Primitive(1, SvgElementKind.FeFlood, SvgFilterColorSpace.Srgb, SvgRect.Create(0, 0, 2, 2)));

        SvgFilterGraphPlan invalid = new SvgFilterGraphPlanner().Plan(invalidFilter, new SvgFilterGraphOptions());

        Assert.Multiple(() =>
        {
            Assert.That(limited.Success, Is.False);
            Assert.That(limited.Error, Is.EqualTo(SvgFilterGraphError.SurfaceLimitExceeded));
            Assert.That(limited.Steps, Is.Empty);
            Assert.That(limited.Surfaces, Is.Empty);
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.Error, Is.EqualTo(SvgFilterGraphError.InvalidGraph));
            Assert.That(invalid.Steps, Is.Empty);
            Assert.That(invalid.Surfaces, Is.Empty);
        });
    }

    static SvgSceneResource Filter(SvgRect region)
    {
        SvgSceneResource resource = new SvgSceneResource("filter", SvgSceneResourceKind.Filter);
        resource.Filter.Region = region;
        return resource;
    }

    static SvgSceneFilterPrimitiveData Primitive(int index, SvgElementKind kind, SvgFilterColorSpace colorSpace, SvgRect region)
    {
        SvgSceneFilterPrimitiveData primitive = new SvgSceneFilterPrimitiveData();
        primitive.Index = index;
        primitive.ElementKind = kind;
        primitive.ColorSpace = colorSpace;
        primitive.Region = region;
        return primitive;
    }

    static SvgFilterPrimitive[] References(int count)
    {
        SvgFilterPrimitive[] references = new SvgFilterPrimitive[count];
        int index = 0;
        while (index < count)
        {
            references[index] = new SvgFilterPrimitive(null, index);
            index = index + 1;
        }
        return references;
    }

    static SvgFilterInput Input(SvgFilterInputKind kind, SvgFilterPrimitive primitive)
    {
        SvgFilterInput input = new SvgFilterInput("in");
        input.Kind = kind;
        input.Primitive = primitive;
        return input;
    }

    static SvgFilterInput Standard(SvgFilterStandardInput standard)
    {
        SvgFilterInput input = new SvgFilterInput("in");
        input.Kind = SvgFilterInputKind.Standard;
        input.StandardInput = standard;
        return input;
    }

    static SvgPixelBuffer Surface(int width, int height, SvgPixelColorSpace colorSpace)
    {
        SvgPixelBufferResult result = SvgPixelBuffer.Create(width, height, colorSpace);
        Assert.That(result.Success, Is.True);
        return result.Buffer;
    }
}
