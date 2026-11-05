# yaml

A layered YAML library for .NET with scanning, parsing, composition, a mutable node model, schema resolution, emission, JSON-compatible conversion, and public-field object conversion.

The library targets YAML 1.2-style behavior by default, uses only built-in .NET libraries, and organizes its public surface under `src/Api`. Behavior is exposed through interfaces and instance classes; mutable data objects expose fields and keep their helper methods internal.

## Runtime dependencies

The `yaml` runtime package has no third-party package dependencies. Scanning, parsing, composition, schema resolution, object conversion, and emission are implemented by this project using the .NET runtime libraries. Packages referenced by the test project are development-only and are not dependencies of the shipped library.

Historical library comparisons and the pinned YAML Test Suite remain under `specs/` as research and conformance inputs; they are not linked into the runtime package.

## High-level processing

Create an independent processor through `YamlApi.Processors`. Supplied options are copied so later mutations of the caller's options do not affect the processor.

```csharp
using yaml;

YamlOptions options = new YamlOptions();
options.Emitter.WriteDocumentStart = true;

IYamlProcessor processor = YamlApi.Processors.CreateProcessor(options);
YamlDocument document = processor.Parse("name: Ada\n");
string text = processor.Serialize(new Dictionary<string, object>
{
    ["name"] = "Ada"
});
```

`YamlProcessor` can also be constructed directly and configured later.

```csharp
IYamlProcessor processor = new YamlProcessor();
processor.Configure(options);
YamlOptions copiedOptions = processor.GetOptions();
```

Applications can depend on `IYamlProcessor` and supply a fake in tests.

## Parsing and querying

```csharp
IYamlProcessor processor = YamlApi.Processors.CreateProcessor(null);
YamlDocument document = processor.Parse("name: Ada\nadmin: true\n");

YamlNode? name = YamlApi.Model.FindPath(document.Root, "name");
YamlNode? admin = YamlApi.Model.FindPath(document.Root, "admin");
```

Use the nonthrowing methods when YAML diagnostics should be returned instead of raised as `YamlException`.

```csharp
YamlParseResult result = new YamlParseResult();
bool ok = processor.TryParse("name: Ada\n", result);

if (!ok)
{
    foreach (YamlDiagnostic diagnostic in result.Diagnostics)
    {
        Console.WriteLine(diagnostic.Message);
    }
}
```

## Editing the model

Data objects are intentionally field-based. Use their lists directly for simple edits and `YamlApi.Model` for construction, paths, cloning, merge keys, and conversions.

```csharp
YamlDocument document = processor.Parse("name: Ada\nroles:\n  - user\n");
YamlNode root = document.Root;
YamlNode? roles = YamlApi.Model.FindPath(root, "roles");

if (roles != null)
{
    roles.Children.Add(YamlApi.Model.CreateScalarNode(
        "admin",
        YamlScalarStyle.Plain,
        null));
}

YamlNode key = YamlApi.Model.CreateScalarNode("enabled", YamlScalarStyle.Plain, null);
YamlNode value = YamlApi.Model.CreateScalarNode("true", YamlScalarStyle.Plain, null);
value.Tag = YamlTags.Bool;
root.Pairs.Add(YamlApi.Model.CreatePair(key, value, null));
```

## Low-level pipeline

`YamlApi.Pipeline` exposes each processing stage without public static methods.

```csharp
IYamlPipeline pipeline = YamlApi.Pipeline;
YamlParserOptions parserOptions = new YamlParserOptions();
YamlScannerResult scanner = pipeline.Scan("items: [one, two]\n", parserOptions);
YamlParserResult parser = pipeline.ParseScannerResult(scanner, parserOptions);
YamlComposerResult composer = pipeline.ComposeParserResult(parser, new YamlComposerOptions());

pipeline.ResolveStream(composer.Stream, new YamlSchemaOptions());
string emitted = pipeline.EmitStream(composer.Stream, new YamlEmitterOptions());
```

## Building and emitting nodes

```csharp
YamlNode root = YamlApi.Model.CreateMappingNode(YamlCollectionStyle.Block, null);
YamlNode key = YamlApi.Model.CreateScalarNode("name", YamlScalarStyle.Plain, null);
YamlNode value = YamlApi.Model.CreateScalarNode("Ada", YamlScalarStyle.SingleQuoted, null);
root.Pairs.Add(YamlApi.Model.CreatePair(key, value, null));

YamlDocument document = new YamlDocument();
document.Root = root;

YamlEmitterOptions emitterOptions = new YamlEmitterOptions();
emitterOptions.WriteDocumentStart = true;
string text = YamlApi.Pipeline.EmitDocument(document, emitterOptions);
```

## Loading and dumping

`Load` returns dictionaries, lists, scalar CLR values, or null. `Dump` converts supported CLR values back to YAML.

```csharp
object? loaded = processor.Load("name: Ada\ncount: 37\n");
Dictionary<string, object> mapping = (Dictionary<string, object>)loaded;

string text = processor.Dump(mapping);
```

Supported high-level values include dictionaries, lists, arrays, primitive values, dates, GUIDs, byte arrays, enums, and ordinary objects with public fields.

## Object conversion

```csharp
YamlOptions options = new YamlOptions();
options.Object.NamingConvention = YamlNamingConvention.KebabCase;
options.Object.ErrorOnUnknownMembers = true;
options.Object.RequiredMembers.Add("DisplayName");

IYamlProcessor processor = YamlApi.Processors.CreateProcessor(options);
string text = processor.Serialize(config);
UserConfig copy = (UserConfig)processor.Deserialize(text, typeof(UserConfig));
```

Custom tag and converter registrations are data objects stored in the corresponding option lists. Interface-based resolvers and converters remain available through `IYamlScalarResolver`, `IYamlScalarConverter`, and `IYamlTypeConverter`.

## Safe untrusted input

Security limits are enabled by default and can be tightened before creating a processor.

```csharp
YamlOptions options = new YamlOptions();
options.Security.MaxInputLength = 1024 * 1024;
options.Security.MaxScalarLength = 64 * 1024;
options.Security.MaxDepth = 50;
options.Security.MaxAliasCount = 25;
options.Security.MaxCollectionSize = 10000;
options.Composer.DuplicateKeyPolicy = YamlDuplicateKeyPolicy.Error;
options.Schema.UnknownTagPolicy = YamlUnknownTagPolicy.Error;

IYamlProcessor processor = YamlApi.Processors.CreateProcessor(options);
YamlParseResult result = new YamlParseResult();
bool ok = processor.TryParse(untrustedText, result);
```

## Public API layout

- `src/Api/Contracts`: behavior contracts.
- `src/Api/Classes`: interface implementations.
- `src/Api/Data`: field-based mutable data and option types.
- `src/Api/Static`: method-free shared service fields and constants.
- `src/Api/Enums`: public enumerations.
- `src/Api/Exceptions`: YAML-specific exceptions.

Scanner, parser, composer, emitter, schema, reflection, security, and compatibility helpers outside `src/Api` are internal implementation details.
