using System;
using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class TemplateDependencyGraphTests
{
    [Test]
    public void SyntaxDumper_DumpsParsedNativeTemplate()
    {
        TemplateSyntaxDumper dumper = new TemplateSyntaxDumper();

        string dump = dumper.Dump("Hi {{ name }}");

        Assert.That(dump, Is.EqualTo(
            "Document [0..13) source=\"(anonymous)\"\n" +
            "  Text [0..3) text=\"Hi \"\n" +
            "  Output [3..13) raw=false escape=true\n" +
            "    Expression [6..10) kind=Variable path=Variable text=\"name\"\n" +
            "      PathSegment Name text=\"name\"\n"));
    }

    [Test]
    public void DependencyGraphBuilder_BuildsRecursiveGraphForLiteralDependencies()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterTemplate("layout", "{% partial \"head\" %}{% include \"leaf\" %}");
        registry.RegisterTemplate("leaf", "Leaf");
        registry.RegisterPartial("head", "Head");
        TemplateDependencyGraphBuilder builder = TemplateDependencyGraphBuilder.Create(registry);

        TemplateDependencyGraph graph = builder.Build("{% extends \"layout\" %}{% include \"leaf\" %}");

        Assert.That(graph.Nodes.Count, Is.EqualTo(4));
        Assert.That(graph.Edges.Count, Is.EqualTo(4));
        Assert.That(graph.HasDiagnostics(), Is.False);
        Assert.That(graph.Format(), Is.EqualTo(
            "DependencyGraph\n" +
            "  Node root:(anonymous) kind=Root name=\"(anonymous)\"\n" +
            "  Node template:layout kind=Template name=\"layout\"\n" +
            "  Node partial:head kind=Partial name=\"head\"\n" +
            "  Node template:leaf kind=Template name=\"leaf\"\n" +
            "  Edge root:(anonymous) -> template:layout kind=Template name=\"layout\" resolved=true\n" +
            "  Edge template:layout -> partial:head kind=Partial name=\"head\" resolved=true\n" +
            "  Edge template:layout -> template:leaf kind=Template name=\"leaf\" resolved=true\n" +
            "  Edge root:(anonymous) -> template:leaf kind=Template name=\"leaf\" resolved=true\n"));
    }

    [Test]
    public void DependencyGraphBuilder_ReportsDynamicAndMissingDependencies()
    {
        TemplateRegistry registry = new TemplateRegistry();
        TemplateDependencyGraphBuilder builder = TemplateDependencyGraphBuilder.Create(registry);

        TemplateDependencyGraph graph = builder.Build("{% include target %}{% partial \"missing\" %}");

        Assert.That(graph.Nodes.Count, Is.EqualTo(1));
        Assert.That(graph.Edges.Count, Is.EqualTo(1));
        Assert.That(graph.HasDiagnostics(), Is.True);
        Assert.That(graph.Diagnostics.Count, Is.EqualTo(2));
        Assert.That(graph.Diagnostics[0].Code, Is.EqualTo("GRAPH001"));
        Assert.That(graph.Diagnostics[1].Code, Is.EqualTo("GRAPH002"));
        Assert.That(graph.Format(), Does.Contain("Edge root:(anonymous) -> partial:missing kind=Partial name=\"missing\" resolved=false"));
    }

    [Test]
    public void DependencyGraphBuilder_UsesProfileConversionForCompatibilityTemplates()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterTemplate("layout", "{{> card }}");
        registry.RegisterPartial("card", "Card");
        TemplateOptions options = new TemplateOptions();
        options.SyntaxProfile = "handlebars";
        TemplateDependencyGraphBuilder builder = TemplateDependencyGraphBuilder.Create(options, registry);

        TemplateDependencyGraph graph = builder.Build("{% include \"layout\" %}");

        Assert.That(graph.Nodes.Count, Is.EqualTo(3));
        Assert.That(graph.Edges.Count, Is.EqualTo(2));
        Assert.That(graph.Edges[0].Name, Is.EqualTo("layout"));
        Assert.That(graph.Edges[1].Name, Is.EqualTo("card"));
        Assert.That(graph.Edges[1].Kind, Is.EqualTo(TemplateDependencyKind.Partial));
    }

    [Test]
    public void Tooling_RejectsNullArguments()
    {
        TemplateDependencyGraph graph = new TemplateDependencyGraph();
        TemplateDependencyGraphBuilder builder = new TemplateDependencyGraphBuilder();
        TemplateSyntaxDumper dumper = new TemplateSyntaxDumper();
        TemplateDocumentSyntax document = TemplateParser.ParseNative("x").Document;

        Assert.Throws<ArgumentNullException>(() => new TemplateDependencyGraphNode(null!, TemplateDependencyGraphNodeKind.Root, "root", null));
        Assert.Throws<ArgumentNullException>(() => new TemplateDependencyGraphNode("root", TemplateDependencyGraphNodeKind.Root, null!, null));
        Assert.Throws<ArgumentNullException>(() => new TemplateDependencyGraphEdge(null!, "to", TemplateDependencyKind.Template, "name", true, null));
        Assert.Throws<ArgumentNullException>(() => new TemplateDependencyGraphEdge("from", null!, TemplateDependencyKind.Template, "name", true, null));
        Assert.Throws<ArgumentNullException>(() => new TemplateDependencyGraphEdge("from", "to", TemplateDependencyKind.Template, null!, true, null));
        Assert.Throws<ArgumentNullException>(() => graph.FindNode(null!));
        Assert.Throws<ArgumentNullException>(() => TemplateDependencyGraphBuilder.Create(null!));
        Assert.Throws<ArgumentNullException>(() => TemplateDependencyGraphBuilder.Create(null!, new TemplateRegistry()));
        Assert.Throws<ArgumentNullException>(() => TemplateDependencyGraphBuilder.Create(new TemplateOptions(), null!));
        Assert.Throws<ArgumentNullException>(() => builder.Build((string)null!));
        Assert.Throws<ArgumentNullException>(() => builder.BuildDocument((TemplateDocumentSyntax)null!));
        Assert.Throws<ArgumentNullException>(() => dumper.Dump((string)null!));
        Assert.Throws<ArgumentNullException>(() => dumper.DumpDocument((TemplateDocumentSyntax)null!));
        Assert.Throws<ArgumentNullException>(() => dumper.DumpNode((TemplateSyntaxNode)null!));
        Assert.Throws<ArgumentNullException>(() => dumper.DumpExpression(null!));
        Assert.That(document.Children.Count, Is.EqualTo(1));
    }
}
