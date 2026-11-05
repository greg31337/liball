using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class MustacheCompatibilityTests
{
    [Test]
    public void RenderMustacheToString_RendersVariablesRawOutputCommentsPartialsAndDelimiterChanges()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterPartial("card", "Card {{name}};");
        registry.RegisterPartial("row", "Row {{name}};");
        TemplateRenderer renderer = TemplateRenderer.Create(registry);

        string template = "Hello {{name}} {{! hidden }}{{html}} {{{html}}} {{& html}} {{> card}} {{>*partialName}} {{=<% %>=}}<%name%>";
        string result = renderer.RenderMustacheToString(template, CreateRoot());

        Assert.That(result, Is.EqualTo("Hello Ada &lt;b&gt;Ada&lt;/b&gt; <b>Ada</b> <b>Ada</b> Card Ada; Row Ada; Ada"));
    }

    [Test]
    public void RenderMustacheToString_RendersSectionsInvertedSectionsCurrentValuesAndLambdas()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string template = "{{#items}}[{{name}}]{{/items}}{{^empty}}empty{{/empty}} {{#tags}}{{.}},{{/tags}} {{#wrap}}{{name}}{{/wrap}} {{lambda}}";
        string result = renderer.RenderMustacheToString(template, CreateRoot());

        Assert.That(result, Is.EqualTo("[One][Two]empty red,blue, <Ada> Hi"));
    }

    [Test]
    public void RenderToString_WithMustacheProfile_ConvertsMustacheSyntaxAndTracksParents()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterTemplate("layout", "H {{$body}}Base {{name}}{{/body}} F");
        TemplateRenderer renderer = TemplateRenderer.Create(MustacheProfile.CreateOptions(), registry);

        string result = renderer.RenderToString("ignored {{<layout}}{{$body}}Child {{name}}{{/body}}{{/layout}}", CreateRoot());

        Assert.That(result, Is.EqualTo("ignored H Child Ada F"));
        AssertDependency(renderer.GetLastDependencies()[0], TemplateDependencyKind.Template, "layout");
    }

    [Test]
    public void RenderMustacheToString_ConformsToOfficialLambdaExpansionAndDelimiterSemantics()
    {
        TemplateValueFactory factory = new TemplateValueFactory();
        Dictionary<string, TemplateValue> fields = new Dictionary<string, TemplateValue>();
        fields["planet"] = factory.CreateString("Earth");
        fields["x"] = factory.CreateString("Error!");
        fields["interpolation"] = factory.CreateFunction((List<TemplateValue> arguments) => factory.CreateString("{{planet}}"));
        fields["escape"] = factory.CreateFunction((List<TemplateValue> arguments) => factory.CreateString(">"));
        fields["rawCheck"] = factory.CreateFunction((List<TemplateValue> arguments) => factory.CreateString(arguments[0].StringValue == "{{x}}" ? "yes" : "no"));
        fields["expand"] = factory.CreateFunction((List<TemplateValue> arguments) => factory.CreateString(arguments[0].StringValue + "{{planet}}" + arguments[0].StringValue));
        fields["alternate"] = factory.CreateFunction((List<TemplateValue> arguments) => factory.CreateString(arguments[0].StringValue + "{{planet}} => |planet|" + arguments[0].StringValue));
        fields["alternateInterpolation"] = factory.CreateFunction((List<TemplateValue> arguments) => factory.CreateString("|planet| => {{planet}}"));
        fields["falseLambda"] = factory.CreateFunction((List<TemplateValue> arguments) => factory.CreateBoolean(false));
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderMustacheToString(
            "{{interpolation}}|{{escape}}|{{{escape}}}|{{#rawCheck}}{{x}}{{/rawCheck}}|{{#expand}}-{{/expand}}|{{= | | =}}<|#alternate|-|/alternate|>",
            factory.CreateMap(fields));

        Assert.That(result, Is.EqualTo("Earth|&gt;|>|yes|-Earth-|<-{{planet}} => Earth->"));
        Assert.That(
            renderer.RenderMustacheToString("{{= | | =}}\nHello, (|&alternateInterpolation|)!<|^falseLambda|wrong|/falseLambda|>", factory.CreateMap(fields)),
            Is.EqualTo("Hello, (|planet| => Earth)!<>"));

        int calls = 0;
        fields["counter"] = factory.CreateFunction((List<TemplateValue> arguments) => factory.CreateNumber(++calls));
        Assert.That(renderer.RenderMustacheToString("{{counter}} == {{{counter}}} == {{counter}}", factory.CreateMap(fields)), Is.EqualTo("1 == 2 == 3"));
    }

    [Test]
    public void RenderMustacheToString_RejectsMalformedSectionsAndDelimiterChanges()
    {
        TemplateValue model = new TemplateValueFactory().CreateMap(new Dictionary<string, TemplateValue>());
        TemplateRenderer renderer = new TemplateRenderer();

        Assert.That(() => renderer.RenderMustacheToString("{{#value}}x{{/other}}", model), Throws.InvalidOperationException);
        Assert.That(() => renderer.RenderMustacheToString("{{#value}}x", model), Throws.InvalidOperationException);
        Assert.That(() => renderer.RenderMustacheToString("{{/value}}", model), Throws.InvalidOperationException);
        Assert.That(() => renderer.RenderMustacheToString("{{= broken =}}", model), Throws.InvalidOperationException);
    }

    static void AssertDependency(TemplateDependency dependency, TemplateDependencyKind kind, string name)
    {
        Assert.That(dependency.Kind, Is.EqualTo(kind));
        Assert.That(dependency.Name, Is.EqualTo(name));
    }

    static TemplateValue CreateRoot()
    {
        Dictionary<string, TemplateValue> root = new Dictionary<string, TemplateValue>();
        root["name"] = TemplateValue.FromString("Ada");
        root["html"] = TemplateValue.FromString("<b>Ada</b>");
        root["partialName"] = TemplateValue.FromString("row");
        root["empty"] = TemplateValue.FromList(new List<TemplateValue>());
        root["items"] = TemplateValue.FromList(CreateItemList());
        root["tags"] = TemplateValue.FromList(CreateTagList());
        root["wrap"] = TemplateValue.FromFunction(WrapLambda);
        root["lambda"] = TemplateValue.FromFunction(TextLambda);
        return TemplateValue.FromMap(root);
    }

    static List<TemplateValue> CreateItemList()
    {
        List<TemplateValue> items = new List<TemplateValue>();
        items.Add(CreateNamedItem("One"));
        items.Add(CreateNamedItem("Two"));
        return items;
    }

    static TemplateValue CreateNamedItem(string name)
    {
        Dictionary<string, TemplateValue> item = new Dictionary<string, TemplateValue>();
        item["name"] = TemplateValue.FromString(name);
        return TemplateValue.FromMap(item);
    }

    static List<TemplateValue> CreateTagList()
    {
        List<TemplateValue> tags = new List<TemplateValue>();
        tags.Add(TemplateValue.FromString("red"));
        tags.Add(TemplateValue.FromString("blue"));
        return tags;
    }

    static TemplateValue WrapLambda(List<TemplateValue> arguments)
    {
        return TemplateValue.FromString("<" + TemplateOutputEncoder.ConvertValueToText(arguments[0]) + ">");
    }

    static TemplateValue TextLambda(List<TemplateValue> arguments)
    {
        return TemplateValue.FromString("Hi");
    }
}
