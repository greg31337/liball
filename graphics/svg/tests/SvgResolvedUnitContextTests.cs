namespace svg.Tests;

public class SvgResolvedUnitContextTests
{
    [Test]
    public void ComputedFontContextsDriveEmExChRemAndDoNotRecomputeInheritedPercentages()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg' style='font-size:2em' width='10em' height='10rem'><g style='font-size:150%'><rect x='1em' y='1ex' width='2ch' height='1rem'/></g></svg>");
        SvgResolveOptions options = new SvgResolveOptions();
        options.StyleOptions.StyleEnvironment.RootFontSize = 10;

        SvgResolvedDocument result = new SvgDocumentResolver().ResolveWithOptions(document, options);
        SvgResolvedNode group = result.FindSource(document.DocumentElement.Children[0]);
        SvgResolvedNode rectangle = result.FindSource(document.DocumentElement.Children[0].Children[0]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Root.Viewport.Width, Is.EqualTo(200));
            Assert.That(result.Root.Viewport.Height, Is.EqualTo(200));
            Assert.That(result.Root.LengthContext.FontSize, Is.EqualTo(20));
            Assert.That(result.Root.LengthContext.RootFontSize, Is.EqualTo(20));
            Assert.That(group.LengthContext.FontSize, Is.EqualTo(30));
            Assert.That(group.LengthContext.XHeight, Is.EqualTo(15));
            Assert.That(group.LengthContext.ZeroAdvance, Is.EqualTo(15));
            Assert.That(rectangle.LengthContext.FontSize, Is.EqualTo(30));
            Assert.That(rectangle.Geometry.X, Is.EqualTo(30));
            Assert.That(rectangle.Geometry.Y, Is.EqualTo(15));
            Assert.That(rectangle.Geometry.Width, Is.EqualTo(30));
            Assert.That(rectangle.Geometry.Height, Is.EqualTo(20));
        });
    }

    [Test]
    public void NestedViewportContextUsesViewBoxUserSpaceAndElementFontSize()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg' width='400' height='200'><svg x='10%' y='10%' width='50%' height='50%' viewBox='0 0 50 25' style='font-size:20px'><rect x='10%' y='20%' width='50%' height='40%'/></svg></svg>");

        SvgResolvedDocument result = new SvgDocumentResolver().Resolve(document);
        SvgResolvedNode nested = result.FindSource(document.DocumentElement.Children[0]);
        SvgResolvedNode rectangle = result.FindSource(document.DocumentElement.Children[0].Children[0]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(nested.Viewport.X, Is.EqualTo(40));
            Assert.That(nested.Viewport.Y, Is.EqualTo(20));
            Assert.That(nested.Viewport.Width, Is.EqualTo(200));
            Assert.That(nested.Viewport.Height, Is.EqualTo(100));
            Assert.That(nested.LengthContext.ViewportWidth, Is.EqualTo(50));
            Assert.That(nested.LengthContext.ViewportHeight, Is.EqualTo(25));
            Assert.That(nested.LengthContext.FontSize, Is.EqualTo(20));
            Assert.That(rectangle.LengthContext.ViewportWidth, Is.EqualTo(50));
            Assert.That(rectangle.LengthContext.ViewportHeight, Is.EqualTo(25));
            Assert.That(rectangle.Geometry.X, Is.EqualTo(5));
            Assert.That(rectangle.Geometry.Y, Is.EqualTo(5));
            Assert.That(rectangle.Geometry.Width, Is.EqualTo(25));
            Assert.That(rectangle.Geometry.Height, Is.EqualTo(10));
            Assert.That(rectangle.LengthContext, Is.Not.SameAs(nested.LengthContext));
        });
    }

    [Test]
    public void EveryResolvedNodeGetsAnIndependentContextAndGeometryGetsObjectBounds()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg' width='100' height='80'><path d='M5 5L25 10L10 30Z'/><text>hello</text></svg>");

        SvgResolvedDocument result = new SvgDocumentResolver().Resolve(document);
        SvgResolvedNode path = result.FindSource(document.DocumentElement.Children[0]);
        SvgResolvedNode text = result.FindSource(document.DocumentElement.Children[1]);
        SvgResolvedNode textNode = result.FindSource(document.DocumentElement.Children[1].Children[0]);
        SvgLengthEvaluationResult halfWidth = new SvgLength(50, SvgLengthUnit.Percentage).Evaluate(path.LengthContext, SvgLengthPercentageBasis.ObjectBoundingBoxWidth);
        SvgLengthEvaluationResult halfHeight = new SvgLength(0.5, SvgLengthUnit.Number).Evaluate(path.LengthContext, SvgLengthPercentageBasis.ObjectBoundingBoxHeight);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Nodes.All(node => node.LengthContext != null), Is.True);
            Assert.That(path.Bounds.X, Is.EqualTo(5));
            Assert.That(path.Bounds.Y, Is.EqualTo(5));
            Assert.That(path.Bounds.Width, Is.EqualTo(20));
            Assert.That(path.Bounds.Height, Is.EqualTo(25));
            Assert.That(path.LengthContext.HasObjectBoundingBox, Is.True);
            Assert.That(halfWidth.Value, Is.EqualTo(10));
            Assert.That(halfHeight.Value, Is.EqualTo(12.5));
            Assert.That(text.LengthContext.HasObjectBoundingBox, Is.False);
            Assert.That(textNode.LengthContext.HasObjectBoundingBox, Is.False);
            Assert.That(textNode.LengthContext, Is.Not.SameAs(text.LengthContext));
            Assert.That(textNode.LengthContext.ViewportWidth, Is.EqualTo(100));
            Assert.That(textNode.LengthContext.ViewportHeight, Is.EqualTo(80));
        });
    }

    [Test]
    public void UseInstancesAndResourceNodesReceiveComputedContexts()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100' style='font-size:10px'><defs><linearGradient id='paint' gradientUnits='userSpaceOnUse' style='font-size:150%'/><g id='art' style='font-size:2em'><rect width='1em' height='1rem'/></g></defs><use href='#art'/></svg>");

        SvgResolvedDocument result = new SvgDocumentResolver().Resolve(document);
        SvgResolvedNode gradient = result.FindSource(document.DocumentElement.Children[0].Children[0]);
        SvgResolvedNode instanceGroup = result.FindInstance("0/1/instance/0");
        SvgResolvedNode instanceRectangle = result.FindInstance("0/1/instance/0/0");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(gradient.LengthContext.FontSize, Is.EqualTo(15));
            Assert.That(gradient.LengthContext.ViewportWidth, Is.EqualTo(100));
            Assert.That(gradient.LengthContext.HasObjectBoundingBox, Is.False);
            Assert.That(instanceGroup.LengthContext.FontSize, Is.EqualTo(20));
            Assert.That(instanceRectangle.LengthContext.FontSize, Is.EqualTo(20));
            Assert.That(instanceRectangle.Geometry.Width, Is.EqualTo(20));
            Assert.That(instanceRectangle.Geometry.Height, Is.EqualTo(10));
            Assert.That(instanceRectangle.LengthContext.HasObjectBoundingBox, Is.True);
        });
    }

    [Test]
    public void ContextCopiesPreserveMetricsButNeverLeakObjectBoundsAcrossInheritance()
    {
        SvgLengthContext source = new SvgLengthContext();
        source.HasViewport = true;
        source.ViewportWidth = 120;
        source.ViewportHeight = 80;
        source.HasFontSize = true;
        source.FontSize = 20;
        source.HasXHeight = true;
        source.XHeight = 8;
        source.HasZeroAdvance = true;
        source.ZeroAdvance = 12;
        source.HasRootFontSize = true;
        source.RootFontSize = 16;
        source.HasObjectBoundingBox = true;
        source.ObjectBoundingBoxWidth = 30;
        source.ObjectBoundingBoxHeight = 40;

        SvgLengthContext inherited = new SvgUnitContextResolver().Inherit(source);
        SvgLengthContext bounded = new SvgUnitContextResolver().WithObjectBoundingBox(inherited, SvgRect.Create(2, 3, 50, 60));
        SvgLengthContext empty = new SvgUnitContextResolver().WithObjectBoundingBox(bounded, SvgRect.Create(0, 0, 0, 0, true));
        inherited.FontSize = 99;

        Assert.Multiple(() =>
        {
            Assert.That(source.FontSize, Is.EqualTo(20));
            Assert.That(source.HasObjectBoundingBox, Is.True);
            Assert.That(source.ObjectBoundingBoxWidth, Is.EqualTo(30));
            Assert.That(inherited.HasViewport, Is.True);
            Assert.That(inherited.ViewportWidth, Is.EqualTo(120));
            Assert.That(inherited.XHeight, Is.EqualTo(8));
            Assert.That(inherited.ZeroAdvance, Is.EqualTo(12));
            Assert.That(inherited.RootFontSize, Is.EqualTo(16));
            Assert.That(inherited.HasObjectBoundingBox, Is.False);
            Assert.That(bounded.HasObjectBoundingBox, Is.True);
            Assert.That(bounded.ObjectBoundingBoxWidth, Is.EqualTo(50));
            Assert.That(bounded.ObjectBoundingBoxHeight, Is.EqualTo(60));
            Assert.That(bounded.FontSize, Is.EqualTo(20));
            Assert.That(empty.HasObjectBoundingBox, Is.False);
            Assert.That(empty.ObjectBoundingBoxWidth, Is.Zero);
            Assert.That(empty.ObjectBoundingBoxHeight, Is.Zero);
        });
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True);
        return result.Document;
    }
}
