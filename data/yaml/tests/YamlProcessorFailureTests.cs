using System.Text;
using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlProcessorFailureTests
{
    [Test]
    public void TryParsePaths_DoNotReturnRecoveredValuesWhenDiagnosticsContainErrors()
    {
        global::yaml.IYamlProcessor processor = new global::yaml.YamlProcessor();
        global::yaml.YamlParseResult first = new global::yaml.YamlParseResult();
        global::yaml.YamlParseResult all = new global::yaml.YamlParseResult();

        bool firstSuccess = processor.TryParse("\u0001\n", first);
        bool allSuccess = processor.TryParseAll("\u0001\n", all);

        Assert.That(firstSuccess, Is.False);
        Assert.That(first.Success, Is.False);
        Assert.That(first.Diagnostics, Is.Not.Empty);
        Assert.That(first.Value, Is.Null);
        Assert.That(allSuccess, Is.False);
        Assert.That(all.Success, Is.False);
        Assert.That(all.Diagnostics, Is.Not.Empty);
        Assert.That(all.Value, Is.Null);
    }

    [Test]
    public void TryLoadPaths_ReturnConversionDiagnosticsAndNoPartialValues()
    {
        global::yaml.IYamlProcessor processor = global::yaml.YamlApi.Processors.CreateProcessor(ThrowingConverterOptions());
        global::yaml.YamlParseResult textResult = new global::yaml.YamlParseResult();
        global::yaml.YamlParseResult byteResult = new global::yaml.YamlParseResult();

        bool textSuccess = processor.TryLoad("!failure value\n", textResult);
        bool byteSuccess = processor.TryLoadBytes(Encoding.UTF8.GetBytes("!failure value\n"), byteResult);

        Assert.That(textSuccess, Is.False);
        Assert.That(textResult.Success, Is.False);
        Assert.That(textResult.Diagnostics.Single().Code, Is.EqualTo("YAMLO001"));
        Assert.That(textResult.Diagnostics[0].Message, Does.Contain("read failure"));
        Assert.That(textResult.Value, Is.Null);
        Assert.That(byteSuccess, Is.False);
        Assert.That(byteResult.Success, Is.False);
        Assert.That(byteResult.Diagnostics.Single().Code, Is.EqualTo("YAMLO001"));
        Assert.That(byteResult.Value, Is.Null);
    }

    [Test]
    public void TryLoad_DoesNotConvertARecoveredDocumentAfterParsingFails()
    {
        global::yaml.IYamlProcessor processor = new global::yaml.YamlProcessor();
        global::yaml.YamlParseResult result = new global::yaml.YamlParseResult();

        bool success = processor.TryLoad("name: Ada\n\u0001\n", result);

        Assert.That(success, Is.False);
        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics, Is.Not.Empty);
        Assert.That(result.Value, Is.Null);
    }

    [Test]
    public void TryDump_ReturnsConversionDiagnosticsInsteadOfThrowing()
    {
        global::yaml.IYamlProcessor processor = global::yaml.YamlApi.Processors.CreateProcessor(ThrowingConverterOptions());
        global::yaml.YamlParseResult result = new global::yaml.YamlParseResult();

        bool success = processor.TryDump(new ProcessorFailureValue(), result);

        Assert.That(success, Is.False);
        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Single().Code, Is.EqualTo("YAMLO002"));
        Assert.That(result.Diagnostics[0].Message, Does.Contain("write failure"));
        Assert.That(result.Value, Is.Null);
    }

    [Test]
    public void TryDump_ReturnsDiagnosticsForDirectObjectCycles()
    {
        global::yaml.IYamlProcessor processor = new global::yaml.YamlProcessor();
        global::yaml.YamlParseResult result = new global::yaml.YamlParseResult();
        List<object> value = new List<object>();
        value.Add(value);

        bool success = processor.TryDump(value, result);

        Assert.That(success, Is.False);
        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Single().Code, Is.EqualTo("YAMLO002"));
        Assert.That(result.Diagnostics[0].Message, Does.Contain("cycle"));
        Assert.That(result.Value, Is.Null);
    }

    [Test]
    public void ThrowingProcessorOperations_UseYamlExceptionForProcessingAndConversionFailures()
    {
        global::yaml.IYamlProcessor defaultProcessor = new global::yaml.YamlProcessor();
        global::yaml.IYamlProcessor converterProcessor = global::yaml.YamlApi.Processors.CreateProcessor(ThrowingConverterOptions());

        Assert.That(() => defaultProcessor.Parse("\u0001\n"), Throws.TypeOf<global::yaml.YamlException>());
        Assert.That(() => defaultProcessor.ParseAll("\u0001\n"), Throws.TypeOf<global::yaml.YamlException>());
        Assert.That(() => defaultProcessor.Load("\u0001\n"), Throws.TypeOf<global::yaml.YamlException>());
        Assert.That(() => defaultProcessor.LoadAll("\u0001\n"), Throws.TypeOf<global::yaml.YamlException>());
        Assert.That(() => defaultProcessor.Deserialize("not-an-integer\n", typeof(int)), Throws.TypeOf<global::yaml.YamlException>());
        Assert.That(
            () => defaultProcessor.DeserializeBytes(Encoding.UTF8.GetBytes("not-an-integer\n"), typeof(int)),
            Throws.TypeOf<global::yaml.YamlException>());
        Assert.That(() => converterProcessor.Dump(new ProcessorFailureValue()), Throws.TypeOf<global::yaml.YamlException>());
        Assert.That(() => converterProcessor.Serialize(new ProcessorFailureValue()), Throws.TypeOf<global::yaml.YamlException>());
    }

    [Test]
    public void CustomSchemaFailures_AreDiagnosticsForTryAndYamlExceptionForThrowingParse()
    {
        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        global::yaml.YamlTagRegistration registration = new global::yaml.YamlTagRegistration();
        registration.Tag = "!resolver";
        registration.NodeKind = global::yaml.YamlNodeKind.Scalar;
        registration.ScalarMatcher = ThrowingMatcher;
        options.Schema.CustomTags.Registrations.Add(registration);
        global::yaml.IYamlProcessor processor = global::yaml.YamlApi.Processors.CreateProcessor(options);
        global::yaml.YamlParseResult result = new global::yaml.YamlParseResult();

        bool success = processor.TryParse("plain\n", result);
        global::yaml.YamlException? exception = Assert.Throws<global::yaml.YamlException>(() => processor.Parse("plain\n"));

        Assert.That(success, Is.False);
        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Single().Code, Is.EqualTo("YAMLPROC001"));
        Assert.That(result.Diagnostics[0].Message, Does.Contain("resolver failure"));
        Assert.That(result.Value, Is.Null);
        Assert.That(exception!.InnerException, Is.TypeOf<InvalidOperationException>());
        Assert.That(exception.Message, Does.Contain("YAMLPROC001"));
    }

    public static global::yaml.YamlOptions ThrowingConverterOptions()
    {
        global::yaml.YamlScalarConverterRegistration registration = new global::yaml.YamlScalarConverterRegistration();
        registration.Tag = "!failure";
        registration.ClrType = typeof(ProcessorFailureValue);
        registration.ReadScalar = ThrowingReader;
        registration.WriteScalar = ThrowingWriter;
        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        options.Object.ScalarConverters.Registrations.Add(registration);
        return options;
    }

    public static object ThrowingReader(string value)
    {
        throw new InvalidOperationException("read failure");
    }

    public static string ThrowingWriter(object value)
    {
        throw new InvalidOperationException("write failure");
    }

    public static bool ThrowingMatcher(string value, global::yaml.YamlScalarStyle style)
    {
        throw new InvalidOperationException("resolver failure");
    }
}

public class ProcessorFailureValue
{
    public ProcessorFailureValue()
    {
    }
}
