using System.Text;
using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlByteInputTests
{
    [Test]
    public void PipelineScanBytes_DecodesUtf8AndBomMarkedUtf16WithCharacterPositions()
    {
        global::yaml.IYamlPipeline pipeline = global::yaml.YamlApi.Pipeline;
        byte[][] inputs = new byte[][]
        {
            Encoding.UTF8.GetBytes("name: café\n"),
            Combine(Encoding.UTF8.GetPreamble(), Encoding.UTF8.GetBytes("name: café\n")),
            Combine(Encoding.Unicode.GetPreamble(), Encoding.Unicode.GetBytes("name: café\n")),
            Combine(Encoding.BigEndianUnicode.GetPreamble(), Encoding.BigEndianUnicode.GetBytes("name: café\n"))
        };

        int index = 0;
        while (index < inputs.Length)
        {
            global::yaml.YamlScannerResult result = pipeline.ScanBytes(inputs[index], null, null);
            global::yaml.YamlToken value = result.Tokens.Single(token => token.Kind == global::yaml.YamlTokenKind.PlainScalar && token.Value == "café");

            Assert.That(result.Success, Is.True, "encoding index " + index.ToString());
            Assert.That(value.Span.StartIndex, Is.EqualTo(6), "encoding index " + index.ToString());
            Assert.That(value.Span.StartLine, Is.EqualTo(1), "encoding index " + index.ToString());
            Assert.That(value.Span.StartColumn, Is.EqualTo(7), "encoding index " + index.ToString());
            index = index + 1;
        }
    }

    [Test]
    public void PipelineParseBytes_ReturnsParserEvents()
    {
        global::yaml.IYamlPipeline pipeline = new global::yaml.YamlPipeline();
        byte[] bytes = Encoding.UTF8.GetBytes("[one, two]\n");

        global::yaml.YamlParserResult result = pipeline.ParseBytes(bytes, null, null);

        Assert.That(result.Success, Is.True);
        Assert.That(
            result.Events.Where(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Scalar).Select(yamlEvent => yamlEvent.Value),
            Is.EqualTo(new string[] { "one", "two" }));
    }

    [TestCase("long", "scalar", "YAMLS002")]
    [TestCase("[[value]]", "depth", "YAMLS003")]
    [TestCase("[one, two]", "collection", "YAMLS005")]
    [TestCase("[&a one, *a, *a]", "alias", "YAMLS004")]
    public void PipelineParseBytes_EnforcesParserStageSecurityLimits(string text, string limit, string code)
    {
        global::yaml.IYamlPipeline pipeline = new global::yaml.YamlPipeline();
        global::yaml.YamlSecurityOptions security = new global::yaml.YamlSecurityOptions();
        if (limit == "scalar")
        {
            security.MaxScalarLength = 3;
        }
        else if (limit == "depth")
        {
            security.MaxDepth = 2;
        }
        else if (limit == "collection")
        {
            security.MaxCollectionSize = 1;
        }
        else
        {
            security.MaxAliasCount = 1;
        }

        global::yaml.YamlParserResult result = pipeline.ParseBytes(Encoding.UTF8.GetBytes(text + "\ntrailing\n"), null, security);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain(code));
        global::yaml.YamlDiagnostic diagnostic = result.Diagnostics.First(item => item.Code == code);
        Assert.That(diagnostic.Span.StartLine, Is.EqualTo(1));
        Assert.That(diagnostic.Span.StartColumn, Is.GreaterThan(0));
        Assert.That(result.Events.Any(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Scalar && yamlEvent.Value == "trailing"), Is.False);
        Assert.That(result.Events.Last().Kind, Is.EqualTo(global::yaml.YamlEventKind.StreamEnd));
    }

    [Test]
    public void ProcessorByteMethods_ParseAndLoadAllSupportedEncodings()
    {
        global::yaml.IYamlProcessor processor = global::yaml.YamlApi.Processors.CreateProcessor(null);
        byte[] utf8 = Combine(Encoding.UTF8.GetPreamble(), Encoding.UTF8.GetBytes("count: 3\n"));
        byte[] utf16Little = Combine(Encoding.Unicode.GetPreamble(), Encoding.Unicode.GetBytes("---\none\n---\n2\n"));
        byte[] utf16Big = Combine(Encoding.BigEndianUnicode.GetPreamble(), Encoding.BigEndianUnicode.GetBytes("37\n"));

        global::yaml.YamlDocument document = processor.ParseBytes(utf8);
        List<object?> values = processor.LoadAllBytes(utf16Little);
        object? converted = processor.DeserializeBytes(utf16Big, typeof(int));

        Assert.That(document.Root!.Pairs[0].Value!.Value, Is.EqualTo("3"));
        Assert.That(values, Is.EqualTo(new object?[] { "one", 2L }));
        Assert.That(converted, Is.EqualTo(37));
    }

    [Test]
    public void ProcessorTryParseAllBytes_ReturnsEveryDocument()
    {
        global::yaml.IYamlProcessor processor = new global::yaml.YamlProcessor();
        global::yaml.YamlParseResult result = new global::yaml.YamlParseResult();
        byte[] bytes = Encoding.UTF8.GetBytes("---\none\n---\ntwo\n");

        bool success = processor.TryParseAllBytes(bytes, result);

        global::yaml.YamlStream stream = (global::yaml.YamlStream)result.Value!;
        Assert.That(success, Is.True);
        Assert.That(stream.Documents.Count, Is.EqualTo(2));
    }

    [Test]
    public void ProcessorTryLoadBytes_PopulatesLoadedValue()
    {
        global::yaml.IYamlProcessor processor = new global::yaml.YamlProcessor();
        global::yaml.YamlParseResult result = new global::yaml.YamlParseResult();
        byte[] bytes = Encoding.UTF8.GetBytes("enabled: true\n");

        bool success = processor.TryLoadBytes(bytes, result);

        Dictionary<string, object> mapping = (Dictionary<string, object>)result.Value!;
        Assert.That(success, Is.True);
        Assert.That(mapping["enabled"], Is.EqualTo(true));
    }

    [Test]
    public void InvalidUtf8_ProducesEncodingDiagnosticAtDecodedSourcePosition()
    {
        global::yaml.IYamlPipeline pipeline = new global::yaml.YamlPipeline();
        byte[] bytes = new byte[] { 195, 169, 13, 10, 195 };

        global::yaml.YamlScannerResult result = pipeline.ScanBytes(bytes, null, null);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Single().Code, Is.EqualTo("YAMLE001"));
        Assert.That(result.Diagnostics[0].Message, Does.Contain("byte offset 4"));
        Assert.That(result.Diagnostics[0].Span.StartIndex, Is.EqualTo(2));
        Assert.That(result.Diagnostics[0].Span.StartLine, Is.EqualTo(2));
        Assert.That(result.Diagnostics[0].Span.StartColumn, Is.EqualTo(1));
    }

    [Test]
    public void InvalidUtf16_ProducesEncodingDiagnosticWithoutDecoderReplacement()
    {
        global::yaml.IYamlProcessor processor = new global::yaml.YamlProcessor();
        global::yaml.YamlParseResult result = new global::yaml.YamlParseResult();
        byte[] bytes = new byte[] { 255, 254, 97, 0, 10, 0, 255 };

        bool success = processor.TryParseBytes(bytes, result);

        Assert.That(success, Is.False);
        Assert.That(result.Diagnostics.Single().Code, Is.EqualTo("YAMLE001"));
        Assert.That(result.Diagnostics[0].Span.StartIndex, Is.EqualTo(2));
        Assert.That(result.Diagnostics[0].Span.StartLine, Is.EqualTo(2));
        Assert.That(result.Diagnostics[0].Span.StartColumn, Is.EqualTo(1));
        Assert.That(result.Value, Is.Null);
    }

    [Test]
    public void UnsupportedUtf32Bom_ProducesEncodingDiagnostic()
    {
        global::yaml.IYamlPipeline pipeline = new global::yaml.YamlPipeline();
        byte[] bytes = new byte[] { 255, 254, 0, 0, 97, 0, 0, 0 };

        global::yaml.YamlScannerResult result = pipeline.ScanBytes(bytes, null, null);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Single().Code, Is.EqualTo("YAMLE001"));
        Assert.That(result.Diagnostics[0].Message, Does.Contain("UTF-32"));
    }

    [Test]
    public void ByteInputLimit_UsesDecodedUtf16LengthAndExcludesBom()
    {
        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        options.Security.MaxInputLength = 1;
        global::yaml.IYamlProcessor processor = global::yaml.YamlApi.Processors.CreateProcessor(options);
        global::yaml.YamlParseResult accepted = new global::yaml.YamlParseResult();
        global::yaml.YamlParseResult rejected = new global::yaml.YamlParseResult();
        byte[] oneCharacter = Combine(Encoding.UTF8.GetPreamble(), Encoding.UTF8.GetBytes("é"));
        byte[] twoUtf16Characters = Encoding.UTF8.GetBytes("😀");

        bool acceptedSuccess = processor.TryParseBytes(oneCharacter, accepted);
        bool rejectedSuccess = processor.TryParseBytes(twoUtf16Characters, rejected);

        Assert.That(acceptedSuccess, Is.True);
        Assert.That(rejectedSuccess, Is.False);
        Assert.That(rejected.Diagnostics.Single().Code, Is.EqualTo("YAMLS001"));
    }

    [Test]
    public void ParseBytes_ThrowsYamlExceptionForInvalidEncoding()
    {
        global::yaml.IYamlProcessor processor = new global::yaml.YamlProcessor();
        byte[] bytes = new byte[] { 192, 175 };

        global::yaml.YamlException? exception = Assert.Throws<global::yaml.YamlException>(() => processor.ParseBytes(bytes));

        Assert.That(exception!.Message, Does.Contain("YAMLE001"));
    }

    public static byte[] Combine(byte[] first, byte[] second)
    {
        byte[] result = new byte[first.Length + second.Length];
        Array.Copy(first, 0, result, 0, first.Length);
        Array.Copy(second, 0, result, first.Length, second.Length);
        return result;
    }
}
