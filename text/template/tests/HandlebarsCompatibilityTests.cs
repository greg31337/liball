using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class HandlebarsCompatibilityTests
{
    [Test]
    public void ConvertToNative_NormalizesHelpersHashArgumentsBlocksAndPartials()
    {
        string converted = HandlebarsTemplateConverter.ConvertToNative("{{format (concat first last) suffix=mark}}{{#each people as |person n|}}{{person.name}}{{/each}}{{#> layout}}Body{{/layout}}{{#*inline \"badge\"}}B{{/inline}}{{> (partialName)}}");

        Assert.That(converted, Is.EqualTo("{{ format((concat first last), {\"suffix\": mark}) }}{% handlebarsEach people \"person\" \"n\" %}{{ person.name }}{% endhandlebarsEach %}{% handlebarsPartialBlock \"layout\" %}Body{% endhandlebarsPartialBlock %}{% handlebarsInlinePartial \"badge\" %}B{% endhandlebarsInlinePartial %}{% partial partialName %}"));
    }

    [Test]
    public void RenderToString_WithHandlebarsProfile_RendersHelpersHashSubexpressionsAndEscaping()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterHelper("concat", ConcatHelper);
        registry.RegisterHelper("format", FormatHelper);
        TemplateRenderer renderer = TemplateRenderer.Create(HandlebarsProfile.CreateOptions(), registry);

        string result = renderer.RenderToString("{{format (concat first last) prefix=\"<\" suffix=mark}} {{{format first prefix=\"<\" suffix=mark}}}", CreateRoot());

        Assert.That(result, Is.EqualTo("&lt;Ada Lovelace! <Ada!"));
    }

    [Test]
    public void RenderToString_WithHandlebarsProfile_RendersEachBlockParametersAndDataVariables()
    {
        TemplateRenderer renderer = TemplateRenderer.Create(HandlebarsProfile.CreateOptions());

        string result = renderer.RenderToString("{{#each people as |person n|}}{{n}}:{{@index}}:{{person.name}};{{else}}none{{/each}} {{#each empty}}{{this}}{{else}}empty{{/each}}", CreateRoot());

        Assert.That(result, Is.EqualTo("0:0:One;1:1:Two; empty"));
    }

    [Test]
    public void RenderToString_WithHandlebarsProfile_DispatchesBlockHelpersWithBodyElseAndHashArguments()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterBlockHelper("panel", PanelBlockHelper);
        TemplateRenderer renderer = TemplateRenderer.Create(HandlebarsProfile.CreateOptions(), registry);

        string result = renderer.RenderToString("{{#panel name tone=\"quiet\"}}{{name}}{{else}}fallback{{/panel}}", CreateRoot());

        Assert.That(result, Is.EqualTo("panel:Ada:quiet:1:2"));
    }

    [Test]
    public void RenderToString_WithHandlebarsProfile_RendersPartialBlocksInlinePartialsAndDynamicPartials()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterPartial("layout", "L[{{> @partial-block}}]");
        registry.RegisterPartial("row", "Row {{name}}");
        TemplateRenderer renderer = TemplateRenderer.Create(HandlebarsProfile.CreateOptions(), registry);

        string template = "{{#> layout}}Body {{name}}{{/layout}} {{#*inline \"badge\"}}Inline {{name}}{{/inline}}{{> badge}} {{> (partialName)}}";
        string result = renderer.RenderToString(template, CreateRoot());

        Assert.That(result, Is.EqualTo("L[Body Ada] Inline Ada Row Ada"));
    }

    [Test]
    public void RenderToString_WithHandlebarsProfile_ConformsToPathsScopeDataAndBlockParameters()
    {
        TemplateValueFactory factory = new TemplateValueFactory();
        Dictionary<string, object> model = new Dictionary<string, object>();
        model["title"] = "Root";
        model["person"] = new Dictionary<string, object> { ["name"] = "Ada" };
        model["groups"] = new List<object>
        {
            new Dictionary<string, object>
            {
                ["name"] = "G",
                ["people"] = new List<object> { new Dictionary<string, object> { ["name"] = "One" }, new Dictionary<string, object> { ["name"] = "Two" } }
            }
        };
        TemplateRenderer renderer = TemplateRenderer.Create(new TemplateProfileCatalog().CreateHandlebarsOptions());

        string result = renderer.RenderToString(
            "{{#with person as |p|}}{{name}}/{{this/name}}/{{p.name}}/{{title}}/{{../title}}/{{@root.title}}{{else}}missing{{/with}}|" +
            "{{#each groups as |group gi|}}{{#each group.people as |person pi|}}{{@../index}}.{{@index}}:{{person.name}}:{{../name}}:{{@first}}:{{@last}};{{/each}}{{/each}}",
            factory.Convert(model));

        Assert.That(result, Is.EqualTo("Ada/Ada/Ada//Root/Root|0.0:One:G:true:false;0.1:Two:G:false:true;"));
    }

    [Test]
    public void RenderToString_WithHandlebarsProfile_ConformsToRawBlocksInversesAndInverseChains()
    {
        TemplateValueFactory factory = new TemplateValueFactory();
        Dictionary<string, object> model = new Dictionary<string, object>();
        model["name"] = "Ada";
        model["first"] = false;
        model["second"] = true;
        model["zero"] = 0;
        TemplateRenderer renderer = TemplateRenderer.Create(new TemplateProfileCatalog().CreateHandlebarsOptions());

        string result = renderer.RenderToString(
            "{{{{raw}}}}{{name}}{{{{/raw}}}}|{{^missing}}inverse{{else}}wrong{{/missing}}|" +
            "{{#if first}}A{{else if second}}B{{else}}C{{/if}}|{{#if zero includeZero=true}}zero{{else}}false{{/if}}",
            factory.Convert(model));

        Assert.That(result, Is.EqualTo("{{name}}|inverse|B|zero"));
    }

    [Test]
    public void RenderToString_WithHandlebarsProfile_ConformsToHelperPrecedenceLiteralsAndEscaping()
    {
        TemplateValueFactory factory = new TemplateValueFactory();
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterHelper("value", (List<TemplateValue> arguments) => factory.CreateString("helper"));
        registry.RegisterHelper("choose", (List<TemplateValue> arguments) => arguments[0]);
        TemplateRenderer renderer = TemplateRenderer.Create(new TemplateProfileCatalog().CreateHandlebarsOptions(), registry);
        Dictionary<string, object> model = new Dictionary<string, object>();
        model["value"] = "field";
        model["html"] = "&<>'\"`=";

        string result = renderer.RenderToString("{{value}}|{{choose true}}|{{choose null}}|{{choose undefined}}|{{html}}|{{{html}}}|\\{{value}}", factory.Convert(model));

        Assert.That(result, Is.EqualTo("helper|true|||&amp;&lt;&gt;&#x27;&quot;&#x60;&#x3D;|&<>'\"`=|{{value}}"));
    }

    [Test]
    public void RenderToString_WithHandlebarsProfile_ConformsToPartialContextsHashDynamicNamesAndInlineSource()
    {
        TemplateValueFactory factory = new TemplateValueFactory();
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterPartial("card", "{{prefix}}{{name}}{{suffix}}");
        registry.RegisterPartial("layout", "[{{name}}:{{> @partial-block}}]");
        TemplateRenderer renderer = TemplateRenderer.Create(new TemplateProfileCatalog().CreateHandlebarsOptions(), registry);
        Dictionary<string, object> model = new Dictionary<string, object>();
        model["name"] = "Root";
        model["partial"] = "card";
        model["person"] = new Dictionary<string, object> { ["name"] = "Ada" };
        model["other"] = new Dictionary<string, object> { ["name"] = "Grace" };

        string result = renderer.RenderToString(
            "{{> card person prefix=\"<\" suffix=\">\"}}|{{> (partial) person prefix=\"[\" suffix=\"]\"}}|" +
            "{{#> layout person}}Caller {{../name}}{{/layout}}|{{#*inline \"local\"}}{{name}}{{/inline}}{{> local other}}|" +
            "{{#with person}}{{#*inline \"local\"}}Inner {{name}}{{/inline}}{{> local}}{{/with}}/{{> local other}}",
            factory.Convert(model));

        Assert.That(result, Is.EqualTo("&lt;Ada&gt;|[Ada]|[Ada:Caller Root]|Grace|Inner Ada/Grace"));
    }

    [Test]
    public void RenderToString_WithHandlebarsProfile_AppliesWhitespaceControlAndRejectsMalformedInput()
    {
        TemplateValue model = new TemplateValueFactory().Convert(new Dictionary<string, object> { ["ok"] = true });
        TemplateRenderer renderer = TemplateRenderer.Create(new TemplateProfileCatalog().CreateHandlebarsOptions());

        Assert.That(renderer.RenderToString("A  {{~#if ok~}}  B  {{~/if~}}  C", model), Is.EqualTo("ABC"));
        Assert.That(() => renderer.RenderToString("{{#if ok}}x{{/each}}", model), Throws.InvalidOperationException);
        Assert.That(() => renderer.RenderToString("{{#if ok}}x", model), Throws.InvalidOperationException);
        Assert.That(() => renderer.RenderToString("{{name", model), Throws.InvalidOperationException);
        Assert.That(() => renderer.RenderToString("{{{{raw}}}}x", model), Throws.InvalidOperationException);
    }

    [Test]
    public void RenderToString_WithHandlebarsProfile_ConformsToCommentsLiteralsScopedBlocksAndStandalonePartials()
    {
        TemplateValueFactory factory = new TemplateValueFactory();
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterHelper("show", (List<TemplateValue> arguments) => arguments[0]);
        registry.RegisterPartial("lines", "{{name}}\n{{name}}\n");
        TemplateRenderer renderer = TemplateRenderer.Create(new TemplateProfileCatalog().CreateHandlebarsOptions(), registry);
        Dictionary<string, object> model = new Dictionary<string, object>();
        model["person"] = new Dictionary<string, object> { ["name"] = "Ada" };
        model["people"] = new List<object> { new Dictionary<string, object> { ["name"] = "One" }, new Dictionary<string, object> { ["name"] = "Two" } };
        model["empty"] = new List<object>();
        model["emptyObject"] = new Dictionary<string, object>();

        string result = renderer.RenderToString(
            "A{{! short }}B{{!-- long --}}C|{{show -12.5}}|{{show 'text'}}|" +
            "{{#person}}{{name}}{{else}}missing{{/person}}|{{#people}}{{name}},{{else}}none{{/people}}|" +
            "{{#empty}}wrong{{else}}empty{{/empty}}|{{#emptyObject}}object{{else}}wrong{{/emptyObject}}\n  {{> lines person}}Z",
            factory.Convert(model));

        Assert.That(result, Is.EqualTo("ABC|-12.5|text|Ada|One,Two,|empty|object\n  Ada\n  Ada\nZ"));
    }

    [Test]
    public async Task RenderToStringAsync_WithHandlebarsProfile_PreservesHelpersWithElseAndInlinePartials()
    {
        TemplateValueFactory factory = new TemplateValueFactory();
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterAsyncHelper("asyncValue", (List<TemplateValue> arguments, CancellationToken cancellationToken) => Task.FromResult(factory.CreateString("async")));
        TemplateRenderer renderer = TemplateRenderer.Create(new TemplateProfileCatalog().CreateHandlebarsOptions(), registry);
        Dictionary<string, object> model = new Dictionary<string, object>();
        model["missing"] = null!;
        model["person"] = new Dictionary<string, object> { ["name"] = "Ada" };

        string result = await renderer.RenderToStringAsync(
            "{{asyncValue}}|{{#with missing}}wrong{{else}}empty{{/with}}|{{#*inline \"name\"}}{{name}}{{/inline}}{{> name person}}",
            factory.Convert(model));

        Assert.That(result, Is.EqualTo("async|empty|Ada"));
    }

    static TemplateValue ConcatHelper(List<TemplateValue> arguments)
    {
        return TemplateValue.FromString(TemplateOutputEncoder.ConvertValueToText(arguments[0]) + " " + TemplateOutputEncoder.ConvertValueToText(arguments[1]));
    }

    static TemplateValue FormatHelper(List<TemplateValue> arguments)
    {
        TemplateValue hash = arguments[arguments.Count - 1];
        string prefix = TemplateOutputEncoder.ConvertValueToText(hash.MapValue["prefix"]);
        string suffix = TemplateOutputEncoder.ConvertValueToText(hash.MapValue["suffix"]);
        return TemplateValue.FromString(prefix + TemplateOutputEncoder.ConvertValueToText(arguments[0]) + suffix);
    }

    static TemplateValue PanelBlockHelper(TemplateBlockHelperContext context)
    {
        TemplateValue hash = context.Arguments[1];
        string tone = TemplateOutputEncoder.ConvertValueToText(hash.MapValue["tone"]);
        string name = TemplateOutputEncoder.ConvertValueToText(context.Arguments[0]);
        return TemplateValue.FromString("panel:" + name + ":" + tone + ":" + context.Body.Count + ":" + context.ElseBody.Count);
    }

    static TemplateValue CreateRoot()
    {
        Dictionary<string, TemplateValue> root = new Dictionary<string, TemplateValue>();
        root["first"] = TemplateValue.FromString("Ada");
        root["last"] = TemplateValue.FromString("Lovelace");
        root["name"] = TemplateValue.FromString("Ada");
        root["mark"] = TemplateValue.FromString("!");
        root["partialName"] = TemplateValue.FromString("row");
        root["people"] = TemplateValue.FromList(CreatePeople());
        root["empty"] = TemplateValue.FromList(new List<TemplateValue>());
        return TemplateValue.FromMap(root);
    }

    static List<TemplateValue> CreatePeople()
    {
        List<TemplateValue> people = new List<TemplateValue>();
        people.Add(CreatePerson("One"));
        people.Add(CreatePerson("Two"));
        return people;
    }

    static TemplateValue CreatePerson(string name)
    {
        Dictionary<string, TemplateValue> person = new Dictionary<string, TemplateValue>();
        person["name"] = TemplateValue.FromString(name);
        return TemplateValue.FromMap(person);
    }
}
