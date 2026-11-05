using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlParserSpecExamplesTests
{
    [Test]
    public void Parse_SpecExampleSequenceOfScalars_EmitsBlockSequence()
    {
        string text = "- Mark McGwire\n- Sammy Sosa\n- Ken Griffey\n";

        global::yaml.YamlParserResult result = Parse(text);

        Assert.That(result.Success, Is.True);
        Assert.That(EventKinds(result), Is.EqualTo(new global::yaml.YamlEventKind[]
        {
            global::yaml.YamlEventKind.StreamStart,
            global::yaml.YamlEventKind.DocumentStart,
            global::yaml.YamlEventKind.SequenceStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.SequenceEnd,
            global::yaml.YamlEventKind.DocumentEnd,
            global::yaml.YamlEventKind.StreamEnd
        }));
        Assert.That(ScalarValues(result), Is.EqualTo(new string[]
        {
            "Mark McGwire",
            "Sammy Sosa",
            "Ken Griffey"
        }));
        Assert.That(result.Events[2].CollectionStyle, Is.EqualTo(global::yaml.YamlCollectionStyle.Block));
    }

    [Test]
    public void Parse_SpecExampleMappingScalarsToScalars_EmitsBlockMapping()
    {
        string text = "hr:  65    # Home runs\navg: 0.278 # Batting average\nrbi: 147   # Runs Batted In\n";

        global::yaml.YamlParserResult result = Parse(text);

        Assert.That(result.Success, Is.True);
        Assert.That(EventKinds(result), Is.EqualTo(new global::yaml.YamlEventKind[]
        {
            global::yaml.YamlEventKind.StreamStart,
            global::yaml.YamlEventKind.DocumentStart,
            global::yaml.YamlEventKind.MappingStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Comment,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Comment,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Comment,
            global::yaml.YamlEventKind.MappingEnd,
            global::yaml.YamlEventKind.DocumentEnd,
            global::yaml.YamlEventKind.StreamEnd
        }));
        Assert.That(ScalarValues(result), Is.EqualTo(new string[]
        {
            "hr",
            "65",
            "avg",
            "0.278",
            "rbi",
            "147"
        }));
        Assert.That(CommentValues(result), Is.EqualTo(new string[]
        {
            "# Home runs",
            "# Batting average",
            "# Runs Batted In"
        }));
    }

    [Test]
    public void Parse_SpecExampleSequenceOfSequences_EmitsNestedFlowSequences()
    {
        string text = "- [name        , hr, avg  ]\n- [Mark McGwire, 65, 0.278]\n- [Sammy Sosa  , 63, 0.288]\n";

        global::yaml.YamlParserResult result = Parse(text);

        Assert.That(result.Success, Is.True);
        Assert.That(EventKinds(result), Is.EqualTo(new global::yaml.YamlEventKind[]
        {
            global::yaml.YamlEventKind.StreamStart,
            global::yaml.YamlEventKind.DocumentStart,
            global::yaml.YamlEventKind.SequenceStart,
            global::yaml.YamlEventKind.SequenceStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.SequenceEnd,
            global::yaml.YamlEventKind.SequenceStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.SequenceEnd,
            global::yaml.YamlEventKind.SequenceStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.SequenceEnd,
            global::yaml.YamlEventKind.SequenceEnd,
            global::yaml.YamlEventKind.DocumentEnd,
            global::yaml.YamlEventKind.StreamEnd
        }));
        Assert.That(result.Events.Where(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.SequenceStart).Select(yamlEvent => yamlEvent.CollectionStyle), Is.EqualTo(new global::yaml.YamlCollectionStyle[]
        {
            global::yaml.YamlCollectionStyle.Block,
            global::yaml.YamlCollectionStyle.Flow,
            global::yaml.YamlCollectionStyle.Flow,
            global::yaml.YamlCollectionStyle.Flow
        }));
    }

    [Test]
    public void Parse_SpecExampleMappingOfMappings_EmitsFlowMappingValues()
    {
        string text = "Mark McGwire: {hr: 65, avg: 0.278}\nSammy Sosa: {hr: 63, avg: 0.288}\n";

        global::yaml.YamlParserResult result = Parse(text);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Events.Where(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.MappingStart).Select(yamlEvent => yamlEvent.CollectionStyle), Is.EqualTo(new global::yaml.YamlCollectionStyle[]
        {
            global::yaml.YamlCollectionStyle.Block,
            global::yaml.YamlCollectionStyle.Flow,
            global::yaml.YamlCollectionStyle.Flow
        }));
        Assert.That(ScalarValues(result), Is.EqualTo(new string[]
        {
            "Mark McGwire",
            "hr",
            "65",
            "avg",
            "0.278",
            "Sammy Sosa",
            "hr",
            "63",
            "avg",
            "0.288"
        }));
    }

    [Test]
    public void Parse_SpecExampleCompactNestedMapping_EmitsSequenceOfMappings()
    {
        string text = "---\n- item    : Super Hoop\n  quantity: 1\n- item    : Basketball\n  quantity: 4\n- item    : Big Shoes\n  quantity: 1\n";

        global::yaml.YamlParserResult result = Parse(text);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Events.Count(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.MappingStart), Is.EqualTo(3));
        Assert.That(ScalarValues(result), Is.EqualTo(new string[]
        {
            "item",
            "Super Hoop",
            "quantity",
            "1",
            "item",
            "Basketball",
            "quantity",
            "4",
            "item",
            "Big Shoes",
            "quantity",
            "1"
        }));
    }

    [Test]
    public void Parse_SpecExampleNodePropertyIndicators_EmitsTagAnchorAndAlias()
    {
        string text = "anchored: !local &anchor value\nalias: *anchor\n";

        global::yaml.YamlParserResult result = Parse(text);

        Assert.That(result.Success, Is.True);
        global::yaml.YamlEvent anchored = result.Events.Single(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Scalar && yamlEvent.Value == "value");
        global::yaml.YamlEvent alias = result.Events.Single(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Alias);
        Assert.That(anchored.Tag, Is.EqualTo("!local"));
        Assert.That(anchored.Anchor, Is.EqualTo("anchor"));
        Assert.That(alias.AliasName, Is.EqualTo("anchor"));
    }

    [Test]
    public void Parse_SpecDirectiveAndDocumentMarkers_EmitsDocumentContent()
    {
        string text = "%YAML 1.2\n---\n- Mark McGwire\n- Sammy Sosa\n...\n";

        global::yaml.YamlParserResult result = Parse(text);

        Assert.That(result.Success, Is.True);
        Assert.That(EventKinds(result), Is.EqualTo(new global::yaml.YamlEventKind[]
        {
            global::yaml.YamlEventKind.StreamStart,
            global::yaml.YamlEventKind.YamlVersionDirective,
            global::yaml.YamlEventKind.DocumentStart,
            global::yaml.YamlEventKind.SequenceStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.SequenceEnd,
            global::yaml.YamlEventKind.DocumentEnd,
            global::yaml.YamlEventKind.StreamEnd
        }));
    }

    [Test]
    public void Parse_SpecExampleTwoDocumentsInStream_EmitsTwoDocumentSequences()
    {
        string text = "# Ranking of 1998 home runs\n---\n- Mark McGwire\n- Sammy Sosa\n- Ken Griffey\n# Team ranking\n---\n- Chicago Cubs\n- St Louis Cardinals\n";

        global::yaml.YamlParserResult result = Parse(text);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Events.Count(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.DocumentStart), Is.EqualTo(2));
        Assert.That(result.Events.Count(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.DocumentEnd), Is.EqualTo(2));
        Assert.That(result.Events.Count(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.SequenceStart), Is.EqualTo(2));
        Assert.That(ScalarValues(result), Is.EqualTo(new string[]
        {
            "Mark McGwire",
            "Sammy Sosa",
            "Ken Griffey",
            "Chicago Cubs",
            "St Louis Cardinals"
        }));
    }

    static global::yaml.YamlParserResult Parse(string text)
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan(text, new global::yaml.YamlParserOptions());
        return global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());
    }

    static global::yaml.YamlEventKind[] EventKinds(global::yaml.YamlParserResult result)
    {
        return result.Events.Select(yamlEvent => yamlEvent.Kind).ToArray();
    }

    static string[] ScalarValues(global::yaml.YamlParserResult result)
    {
        return result.Events.Where(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Scalar).Select(yamlEvent => yamlEvent.Value).ToArray();
    }

    static string[] CommentValues(global::yaml.YamlParserResult result)
    {
        return result.Events.Where(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Comment).Select(yamlEvent => yamlEvent.Value).ToArray();
    }
}
