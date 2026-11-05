using System.Text;
using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlProcessorPublicContractTests
{
    [Test]
    public void FactoryAndConfiguration_CreateIndependentProcessorOptionGraphs()
    {
        global::yaml.YamlOptions supplied = new global::yaml.YamlOptions();
        supplied.Schema.Schema = global::yaml.YamlSchemaKind.Json;
        supplied.Emitter.WriteDocumentStart = true;
        supplied.Object.RequiredMembers.Add("FirstName");
        global::yaml.YamlScalarConverterRegistration registration = new global::yaml.YamlScalarConverterRegistration();
        registration.Tag = "!original";
        registration.ClrType = typeof(ProcessorContractToken);
        supplied.Object.ScalarConverters.Registrations.Add(registration);

        global::yaml.IYamlProcessorFactory sharedFactory = global::yaml.YamlApi.Processors;
        global::yaml.YamlProcessorFactory concreteFactory = new global::yaml.YamlProcessorFactory();
        global::yaml.IYamlProcessor first = sharedFactory.CreateProcessor(supplied);
        global::yaml.YamlProcessor second = concreteFactory.CreateProcessor(supplied);

        supplied.Schema.Schema = global::yaml.YamlSchemaKind.Failsafe;
        supplied.Emitter.WriteDocumentStart = false;
        supplied.Object.RequiredMembers.Clear();
        registration.Tag = "!mutated";

        global::yaml.YamlOptions firstSnapshot = first.GetOptions();
        global::yaml.YamlOptions secondSnapshot = second.GetOptions();
        Assert.That(firstSnapshot.Schema.Schema, Is.EqualTo(global::yaml.YamlSchemaKind.Json));
        Assert.That(firstSnapshot.Emitter.WriteDocumentStart, Is.True);
        Assert.That(firstSnapshot.Object.RequiredMembers, Is.EqualTo(new string[] { "FirstName" }));
        Assert.That(firstSnapshot.Object.ScalarConverters.Registrations[0].Tag, Is.EqualTo("!original"));
        Assert.That(secondSnapshot.Schema.Schema, Is.EqualTo(global::yaml.YamlSchemaKind.Json));

        firstSnapshot.Schema.Schema = global::yaml.YamlSchemaKind.Yaml11;
        firstSnapshot.Object.RequiredMembers.Clear();
        Assert.That(first.GetOptions().Schema.Schema, Is.EqualTo(global::yaml.YamlSchemaKind.Json));
        Assert.That(first.GetOptions().Object.RequiredMembers, Is.EqualTo(new string[] { "FirstName" }));

        global::yaml.YamlOptions replacement = new global::yaml.YamlOptions();
        replacement.Schema.Schema = global::yaml.YamlSchemaKind.Core;
        replacement.Emitter.SortStringKeys = true;
        first.Configure(replacement);
        replacement.Emitter.SortStringKeys = false;

        Assert.That(first.GetOptions().Emitter.SortStringKeys, Is.True);
        Assert.That(second.GetOptions().Emitter.SortStringKeys, Is.False);

        first.Configure(null);
        Assert.That(first.GetOptions().Schema.Schema, Is.EqualTo(global::yaml.YamlSchemaKind.Core));
        Assert.That(first.GetOptions().Emitter.WriteDocumentStart, Is.False);
    }

    [Test]
    public void ParseOperations_ProcessFirstAndAllTextAndByteDocuments()
    {
        global::yaml.IYamlProcessor processor = new global::yaml.YamlProcessor();
        global::yaml.YamlProcessor concrete = new global::yaml.YamlProcessor();
        byte[] singleBytes = Encoding.UTF8.GetBytes("name: Ada\n");
        string streamText = "---\nname: Ada\n---\nname: Grace\n";
        byte[] streamBytes = Encoding.UTF8.GetBytes(streamText);

        global::yaml.YamlDocument parsed = processor.Parse("name: Ada\n");
        global::yaml.YamlDocument parsedBytes = concrete.ParseBytes(singleBytes);
        global::yaml.YamlStream parsedAll = processor.ParseAll(streamText);
        global::yaml.YamlStream parsedAllBytes = concrete.ParseAllBytes(streamBytes);

        Assert.That(parsed.Root!.FindMappingValue("name")!.Value, Is.EqualTo("Ada"));
        Assert.That(parsedBytes.Root!.FindMappingValue("name")!.Value, Is.EqualTo("Ada"));
        Assert.That(parsedAll.Documents.Count, Is.EqualTo(2));
        Assert.That(parsedAll.Documents[1].Root!.FindMappingValue("name")!.Value, Is.EqualTo("Grace"));
        Assert.That(parsedAllBytes.Documents.Count, Is.EqualTo(2));
        Assert.That(parsedAllBytes.Documents[0].Root!.FindMappingValue("name")!.Value, Is.EqualTo("Ada"));
    }

    [Test]
    public void TryParseOperations_ResetResultsAndReportTextByteAndStreamFailures()
    {
        global::yaml.IYamlProcessor processor = global::yaml.YamlApi.Processors.CreateProcessor(null);
        global::yaml.YamlParseResult text = new global::yaml.YamlParseResult();
        global::yaml.YamlParseResult bytes = new global::yaml.YamlParseResult();
        global::yaml.YamlParseResult allText = new global::yaml.YamlParseResult();
        global::yaml.YamlParseResult allBytes = new global::yaml.YamlParseResult();

        Assert.That(processor.TryParse("value: 1\n", text), Is.True);
        Assert.That(processor.TryParseBytes(Encoding.UTF8.GetBytes("value: 2\n"), bytes), Is.True);
        Assert.That(processor.TryParseAll("---\n1\n---\n2\n", allText), Is.True);
        Assert.That(processor.TryParseAllBytes(Encoding.UTF8.GetBytes("---\n3\n---\n4\n"), allBytes), Is.True);
        Assert.That(text.Value, Is.TypeOf<global::yaml.YamlDocument>());
        Assert.That(bytes.Value, Is.TypeOf<global::yaml.YamlDocument>());
        Assert.That(((global::yaml.YamlStream)allText.Value!).Documents.Count, Is.EqualTo(2));
        Assert.That(((global::yaml.YamlStream)allBytes.Value!).Documents.Count, Is.EqualTo(2));

        Assert.That(processor.TryParse("\u0001\n", text), Is.False);
        Assert.That(text.Success, Is.False);
        Assert.That(text.Value, Is.Null);
        Assert.That(text.Diagnostics, Is.Not.Empty);
        Assert.That(processor.TryParseBytes(new byte[] { 255 }, bytes), Is.False);
        Assert.That(bytes.Diagnostics, Is.Not.Empty);
        Assert.That(processor.TryParseAll("\u0001\n", allText), Is.False);
        Assert.That(processor.TryParseAllBytes(new byte[] { 255 }, allBytes), Is.False);
        Assert.That(processor.TryParse("value\n", null), Is.False);
        Assert.That(processor.TryParseBytes(new byte[0], null), Is.False);
        Assert.That(processor.TryParseAll("value\n", null), Is.False);
        Assert.That(processor.TryParseAllBytes(new byte[0], null), Is.False);
    }

    [Test]
    public void LoadOperations_ProcessFirstAndAllTextAndByteValues()
    {
        global::yaml.IYamlProcessor processor = new global::yaml.YamlProcessor();

        object? text = processor.Load("answer: 42\n");
        object? bytes = processor.LoadBytes(Encoding.UTF8.GetBytes("ready: true\n"));
        List<object?> allText = processor.LoadAll("---\n1\n---\ntwo\n");
        List<object?> allBytes = processor.LoadAllBytes(Encoding.UTF8.GetBytes("---\nfalse\n---\n3.5\n"));

        Assert.That(((Dictionary<string, object?>)text!)["answer"], Is.EqualTo(42L));
        Assert.That(((Dictionary<string, object?>)bytes!)["ready"], Is.EqualTo(true));
        Assert.That(allText, Is.EqualTo(new object?[] { 1L, "two" }));
        Assert.That(allBytes, Is.EqualTo(new object?[] { false, 3.5d }));
    }

    [Test]
    public void TryLoadOperations_ReturnValuesOrDiagnosticsWithoutThrowing()
    {
        global::yaml.IYamlProcessor processor = new global::yaml.YamlProcessor();
        global::yaml.YamlParseResult text = new global::yaml.YamlParseResult();
        global::yaml.YamlParseResult bytes = new global::yaml.YamlParseResult();

        Assert.That(processor.TryLoad("count: 7\n", text), Is.True);
        Assert.That(((Dictionary<string, object?>)text.Value!)["count"], Is.EqualTo(7L));
        Assert.That(processor.TryLoadBytes(Encoding.UTF8.GetBytes("count: 8\n"), bytes), Is.True);
        Assert.That(((Dictionary<string, object?>)bytes.Value!)["count"], Is.EqualTo(8L));

        Assert.That(processor.TryLoad("\u0001\n", text), Is.False);
        Assert.That(text.Value, Is.Null);
        Assert.That(text.Diagnostics, Is.Not.Empty);
        Assert.That(processor.TryLoadBytes(new byte[] { 255 }, bytes), Is.False);
        Assert.That(bytes.Value, Is.Null);
        Assert.That(processor.TryLoad("value\n", null), Is.False);
        Assert.That(processor.TryLoadBytes(new byte[0], null), Is.False);
    }

    [Test]
    public void DumpSerializeAndTryDump_UseTheSameConfiguredConversionPipeline()
    {
        global::yaml.IYamlProcessor processor = new global::yaml.YamlProcessor();
        Dictionary<string, object?> value = new Dictionary<string, object?>();
        value["name"] = "Ada";
        value["active"] = true;
        global::yaml.YamlParseResult result = new global::yaml.YamlParseResult();

        string dumped = processor.Dump(value);
        string serialized = processor.Serialize(value);
        bool success = processor.TryDump(value, result);

        Assert.That(dumped, Is.EqualTo("name: Ada\nactive: true\n"));
        Assert.That(serialized, Is.EqualTo(dumped));
        Assert.That(success, Is.True);
        Assert.That(result.Success, Is.True);
        Assert.That(result.Value, Is.EqualTo(dumped));
        Assert.That(processor.TryDump(value, null), Is.False);
    }

    [Test]
    public void BuiltInClrValues_RoundTripThroughProcessorSerialization()
    {
        global::yaml.IYamlProcessor processor = new global::yaml.YamlProcessor();
        object[] values = new object[]
        {
            "text",
            true,
            (byte)7,
            (sbyte)-7,
            (short)-12,
            (ushort)12,
            -37,
            (uint)37,
            -922L,
            (ulong)922,
            1.5f,
            1.5d,
            1.5m,
            new DateTime(2026, 8, 1, 12, 30, 0, DateTimeKind.Utc),
            new DateTimeOffset(2026, 8, 1, 12, 30, 0, TimeSpan.Zero),
            new Guid("7b7478e4-6c56-4e20-a81a-6b863f29c0f4"),
            new byte[] { 1, 2, 3, 4 },
            ProcessorContractRole.Admin
        };

        foreach (object value in values)
        {
            string text = processor.Serialize(value);
            object? converted = processor.Deserialize(text, value.GetType());
            Assert.That(converted, Is.EqualTo(value), "Failed to round-trip " + value.GetType().FullName);
        }

        Assert.That(processor.Deserialize(processor.Serialize(null), typeof(object)), Is.Null);
        Assert.That(processor.DeserializeBytes(Encoding.UTF8.GetBytes("42\n"), typeof(int)), Is.EqualTo(42));
        Assert.That((int[])processor.Deserialize("[1, 2, 3]\n", typeof(int[]))!, Is.EqualTo(new int[] { 1, 2, 3 }));
        Assert.That((List<string>)processor.Deserialize("[Ada, Grace]\n", typeof(List<string>))!, Is.EqualTo(new string[] { "Ada", "Grace" }));
        Dictionary<string, int> mapping = (Dictionary<string, int>)processor.Deserialize("one: 1\ntwo: 2\n", typeof(Dictionary<string, int>))!;
        Assert.That(mapping, Is.EqualTo(new Dictionary<string, int> { ["one"] = 1, ["two"] = 2 }));
    }

    [TestCase(global::yaml.YamlSchemaKind.Failsafe, "true\n", typeof(string), "true")]
    [TestCase(global::yaml.YamlSchemaKind.Json, "true\n", typeof(bool), "True")]
    [TestCase(global::yaml.YamlSchemaKind.Core, "true\n", typeof(bool), "True")]
    [TestCase(global::yaml.YamlSchemaKind.Yaml11, "YES\n", typeof(bool), "True")]
    public void SchemaChoices_ControlProcessorScalarResolution(
        global::yaml.YamlSchemaKind schema,
        string text,
        Type expectedType,
        string expectedText)
    {
        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        options.Schema.Schema = schema;
        global::yaml.IYamlProcessor processor = global::yaml.YamlApi.Processors.CreateProcessor(options);

        object? value = processor.Load(text);

        Assert.That(value, Is.TypeOf(expectedType));
        Assert.That(value!.ToString(), Is.EqualTo(expectedText));
    }

    [TestCase(global::yaml.YamlDuplicateKeyPolicy.Error, false, 0, "", 1)]
    [TestCase(global::yaml.YamlDuplicateKeyPolicy.Warn, true, 2, "first", 1)]
    [TestCase(global::yaml.YamlDuplicateKeyPolicy.FirstWins, true, 1, "first", 0)]
    [TestCase(global::yaml.YamlDuplicateKeyPolicy.LastWins, true, 1, "last", 0)]
    [TestCase(global::yaml.YamlDuplicateKeyPolicy.PreserveAll, true, 2, "first", 0)]
    public void DuplicateKeyPolicies_ControlProcessorComposition(
        global::yaml.YamlDuplicateKeyPolicy policy,
        bool expectedSuccess,
        int expectedPairs,
        string expectedValue,
        int expectedDiagnostics)
    {
        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        options.Composer.DuplicateKeyPolicy = policy;
        global::yaml.IYamlProcessor processor = global::yaml.YamlApi.Processors.CreateProcessor(options);
        global::yaml.YamlParseResult result = new global::yaml.YamlParseResult();

        bool success = processor.TryParse("key: first\nkey: last\n", result);

        Assert.That(success, Is.EqualTo(expectedSuccess));
        Assert.That(result.Diagnostics.Count, Is.EqualTo(expectedDiagnostics));
        if (!expectedSuccess)
        {
            Assert.That(result.Value, Is.Null);
            return;
        }

        global::yaml.YamlDocument document = (global::yaml.YamlDocument)result.Value!;
        Assert.That(document.Root!.Pairs.Count, Is.EqualTo(expectedPairs));
        Assert.That(document.Root.FindMappingValue("key")!.Value, Is.EqualTo(expectedValue));
    }

    [TestCase(global::yaml.YamlUnknownTagPolicy.Preserve, true, 0)]
    [TestCase(global::yaml.YamlUnknownTagPolicy.Warn, true, 1)]
    [TestCase(global::yaml.YamlUnknownTagPolicy.Error, false, 1)]
    public void UnknownTagPolicies_ControlProcessorDiagnostics(
        global::yaml.YamlUnknownTagPolicy policy,
        bool expectedSuccess,
        int expectedDiagnostics)
    {
        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        options.Schema.UnknownTagPolicy = policy;
        global::yaml.IYamlProcessor processor = global::yaml.YamlApi.Processors.CreateProcessor(options);
        global::yaml.YamlParseResult result = new global::yaml.YamlParseResult();

        bool success = processor.TryParse("!mystery value\n", result);

        Assert.That(success, Is.EqualTo(expectedSuccess));
        Assert.That(result.Diagnostics.Count, Is.EqualTo(expectedDiagnostics));
        if (expectedDiagnostics > 0)
        {
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo("YAMLS006"));
        }
    }

    [TestCase(global::yaml.YamlInvalidValuePolicy.Error, false)]
    [TestCase(global::yaml.YamlInvalidValuePolicy.QuoteAsString, true)]
    public void InvalidValuePolicies_ControlProcessorDumpFailures(
        global::yaml.YamlInvalidValuePolicy policy,
        bool expectedSuccess)
    {
        global::yaml.YamlScalarConverterRegistration registration = new global::yaml.YamlScalarConverterRegistration();
        registration.Tag = "!unsafe";
        registration.ClrType = typeof(ProcessorContractToken);
        registration.PreferredStyle = global::yaml.YamlScalarStyle.Plain;
        registration.WriteScalar = WriteUnsafeToken;
        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        options.Emitter.InvalidValuePolicy = policy;
        options.Object.ScalarConverters.Registrations.Add(registration);
        global::yaml.IYamlProcessor processor = global::yaml.YamlApi.Processors.CreateProcessor(options);
        global::yaml.YamlParseResult result = new global::yaml.YamlParseResult();

        bool success = processor.TryDump(new ProcessorContractToken("value"), result);

        Assert.That(success, Is.EqualTo(expectedSuccess));
        if (expectedSuccess)
        {
            Assert.That(result.Value, Is.EqualTo("!unsafe 'a: b'\n"));
        }
        else
        {
            Assert.That(result.Value, Is.Null);
            Assert.That(result.Diagnostics.Single().Code, Is.EqualTo("YAMLO002"));
        }
    }

    [TestCase(global::yaml.YamlNamingConvention.Exact, "FirstName")]
    [TestCase(global::yaml.YamlNamingConvention.CamelCase, "firstName")]
    [TestCase(global::yaml.YamlNamingConvention.PascalCase, "FirstName")]
    [TestCase(global::yaml.YamlNamingConvention.SnakeCase, "first_name")]
    [TestCase(global::yaml.YamlNamingConvention.KebabCase, "first-name")]
    public void NamingChoices_RoundTripReflectionBasedObjects(
        global::yaml.YamlNamingConvention naming,
        string key)
    {
        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        options.Object.NamingConvention = naming;
        global::yaml.IYamlProcessor processor = global::yaml.YamlApi.Processors.CreateProcessor(options);
        ProcessorContractPerson source = new ProcessorContractPerson();
        source.FirstName = "Ada";
        source.Count = 37;

        string text = processor.Serialize(source);
        ProcessorContractPerson converted = (ProcessorContractPerson)processor.Deserialize(text, typeof(ProcessorContractPerson))!;

        Assert.That(text, Does.Contain(key + ": Ada\n"));
        Assert.That(converted.FirstName, Is.EqualTo("Ada"));
        Assert.That(converted.Count, Is.EqualTo(37));
    }

    [Test]
    public void RequiredAndUnknownMemberPolicies_AreEnforcedByProcessorDeserialization()
    {
        global::yaml.YamlOptions strict = new global::yaml.YamlOptions();
        strict.Object.NamingConvention = global::yaml.YamlNamingConvention.SnakeCase;
        strict.Object.RequireConfiguredMembers = true;
        strict.Object.RequiredMembers.Add("FirstName");
        strict.Object.ErrorOnUnknownMembers = true;
        global::yaml.IYamlProcessor processor = global::yaml.YamlApi.Processors.CreateProcessor(strict);

        ProcessorContractPerson valid = (ProcessorContractPerson)processor.Deserialize(
            "first_name: Ada\ncount: 37\n",
            typeof(ProcessorContractPerson))!;

        Assert.That(valid.FirstName, Is.EqualTo("Ada"));
        Assert.That(
            () => processor.Deserialize("count: 1\n", typeof(ProcessorContractPerson)),
            Throws.TypeOf<global::yaml.YamlException>());
        Assert.That(
            () => processor.Deserialize("first_name: Ada\nextra: value\n", typeof(ProcessorContractPerson)),
            Throws.TypeOf<global::yaml.YamlException>());

        strict.Object.ErrorOnUnknownMembers = false;
        processor.Configure(strict);
        ProcessorContractPerson lenient = (ProcessorContractPerson)processor.Deserialize(
            "first_name: Grace\nextra: ignored\n",
            typeof(ProcessorContractPerson))!;
        Assert.That(lenient.FirstName, Is.EqualTo("Grace"));
    }

    [Test]
    public void ObjectInclusionPolicies_ControlReflectionSerializationAndDeserialization()
    {
        ProcessorContractOptional source = new ProcessorContractOptional();
        source.Name = null;
        source.Count = 0;
        source.Note = "present";
        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        options.Object.IgnoreNulls = true;
        options.Object.IgnoreDefaults = true;
        global::yaml.IYamlProcessor processor = global::yaml.YamlApi.Processors.CreateProcessor(options);

        string text = processor.Serialize(source);
        ProcessorContractOptional converted = (ProcessorContractOptional)processor.Deserialize(
            "Name: null\nCount: 0\nNote: changed\n",
            typeof(ProcessorContractOptional))!;

        Assert.That(text, Is.EqualTo("Note: present\n"));
        Assert.That(converted.Name, Is.EqualTo("existing"));
        Assert.That(converted.Count, Is.EqualTo(7));
        Assert.That(converted.Note, Is.EqualTo("changed"));
    }

    [Test]
    public void TypeTags_AreAppliedAndInferredOnlyWhenConfigured()
    {
        global::yaml.YamlTagRegistration tag = new global::yaml.YamlTagRegistration();
        tag.Tag = "!person";
        tag.NodeKind = global::yaml.YamlNodeKind.Mapping;
        tag.ClrType = typeof(ProcessorContractPerson);
        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        options.Object.UseTypeTags = true;
        options.Schema.UnknownTagPolicy = global::yaml.YamlUnknownTagPolicy.Error;
        options.Schema.CustomTags.Registrations.Add(tag);
        global::yaml.IYamlProcessor processor = global::yaml.YamlApi.Processors.CreateProcessor(options);
        ProcessorContractPerson source = new ProcessorContractPerson();
        source.FirstName = "Ada";
        source.Count = 37;

        string tagged = processor.Serialize(source);
        object? inferred = processor.Load(tagged);

        Assert.That(tagged, Does.StartWith("!person\n"));
        Assert.That(inferred, Is.TypeOf<ProcessorContractPerson>());
        Assert.That(((ProcessorContractPerson)inferred!).FirstName, Is.EqualTo("Ada"));

        options.Object.UseTypeTags = false;
        processor.Configure(options);
        string untagged = processor.Serialize(source);
        object? generic = processor.Load("!person { FirstName: Ada, Count: 37 }\n");
        Assert.That(untagged, Does.Not.StartWith("!person"));
        Assert.That(generic, Is.TypeOf<Dictionary<string, object?>>());
    }

    [Test]
    public void CustomScalarAndTypeConverters_ApplyInBothDirections()
    {
        global::yaml.YamlScalarConverterRegistration scalar = new global::yaml.YamlScalarConverterRegistration();
        scalar.Tag = "!token";
        scalar.ClrType = typeof(ProcessorContractToken);
        scalar.PreferredStyle = global::yaml.YamlScalarStyle.SingleQuoted;
        scalar.ReadScalar = ReadToken;
        scalar.WriteScalar = WriteToken;
        global::yaml.YamlTypeConverterRegistration type = new global::yaml.YamlTypeConverterRegistration();
        type.ClrType = typeof(ProcessorContractPoint);
        type.ReadValue = ReadPoint;
        type.WriteValue = WritePoint;
        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        options.Object.ScalarConverters.Registrations.Add(scalar);
        options.Object.TypeConverters.Registrations.Add(type);
        global::yaml.IYamlProcessor processor = global::yaml.YamlApi.Processors.CreateProcessor(options);

        string tokenText = processor.Serialize(new ProcessorContractToken("Ada"));
        ProcessorContractToken token = (ProcessorContractToken)processor.Deserialize(tokenText, typeof(ProcessorContractToken))!;
        string pointText = processor.Serialize(new ProcessorContractPoint(3, 5));
        ProcessorContractPoint point = (ProcessorContractPoint)processor.Deserialize(pointText, typeof(ProcessorContractPoint))!;

        Assert.That(tokenText, Is.EqualTo("!token 'write:Ada'\n"));
        Assert.That(token.Text, Is.EqualTo("read:write:Ada"));
        Assert.That(point.X, Is.EqualTo(3));
        Assert.That(point.Y, Is.EqualTo(5));
    }

    public static string WriteUnsafeToken(object value)
    {
        return "a: b";
    }

    public static object ReadToken(string value)
    {
        return new ProcessorContractToken("read:" + value);
    }

    public static string WriteToken(object value)
    {
        return "write:" + ((ProcessorContractToken)value).Text;
    }

    public static object ReadPoint(object value)
    {
        string[] parts = value.ToString()!.Split(',');
        return new ProcessorContractPoint(int.Parse(parts[0]), int.Parse(parts[1]));
    }

    public static object WritePoint(object value)
    {
        ProcessorContractPoint point = (ProcessorContractPoint)value;
        return point.X.ToString() + "," + point.Y.ToString();
    }
}

public class ProcessorContractPerson
{
    public string FirstName;
    public int Count;

    public ProcessorContractPerson()
    {
        FirstName = "";
        Count = 0;
    }
}

public class ProcessorContractOptional
{
    public string? Name;
    public int Count;
    public string Note;

    public ProcessorContractOptional()
    {
        Name = "existing";
        Count = 7;
        Note = "";
    }
}

public class ProcessorContractToken
{
    public string Text;

    public ProcessorContractToken(string text)
    {
        Text = text;
    }
}

public class ProcessorContractPoint
{
    public int X;
    public int Y;

    public ProcessorContractPoint(int x, int y)
    {
        X = x;
        Y = y;
    }
}

public enum ProcessorContractRole
{
    Unknown = 0,
    Admin = 1
}
