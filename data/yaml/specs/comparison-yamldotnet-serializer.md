# Object serialization to YAML

As [discussed](Serialization), serialization of objects to YAML is performed by the `Serializer` class.

For simple use cases, it is enough to just create an instance and call the `Serialize()` method:

```c#
var serializer = new YamlDotNet.Serialization.Serializer();
serializer.Serialize(Console.Out, new {
    Hello = "world"
});
```

This will produce the following output:

```yaml output
Hello: world
```

While the default behaviour is enough for demonstration purposes, in many cases it will be necessary to customize it. For example, one may want to use a different naming convention so that the `Hello` key is converted to camelCase and become `hello`.

## Configuring the serializer

The `Serializer` itself is immutable thus its behaviour cannot be altered after its construction. Instead, the configuration should be performed on an instance of `SerializerBuilder`, which will then take care of creating a `Serializer` according to its configuration.

The `SerializerBuilder` provides a series of methods that configure specific aspects of the serializer. These methods are described below.

Many of these methods register a component that is inserted into a chain of components, and offer various overloads that allow to configure the location of the registered component in the chain. This is discussed in detail in the [dedicated documentation page](Serialization.ComponentChainRegistration).

### `IgnoreFields()`

Instructs the serializer to ignore public fields. The default behaviour is to emit public fields and public properties.

### `WithNamingConvention(INamingConvention)`

Specifies the [naming convention](Serialization.NamingConventions) that will be used. The naming convention specifies how .NET member names are converted to YAML keys.

```c#
var serializer = new SerializerBuilder()
    .WithNamingConvention(CamelCaseNamingConvention.Instance)
    .Build();
```

### `WithTypeResolver(ITypeResolver)`

Specifies the [type resolver](Serialization.TypeResolver) that will be used to determine which type should be considered when entering an object's member. By default, `DynamicTypeResolver` is used, which uses the actual type of the object. The other implementation that is included in the library is `StaticTypeResolver`, which uses the declaring type of the member. The difference is better illustrated with an example:

Consider the following class:

```c# id=contacts
interface IContact
{
    string Name { get; }
    string PhoneNumber { get; }
}

class CompanyContact : IContact
{
    public string Name { get; set; }
    public string PhoneNumber { get; set; }
    public string RepresentativeName { get; set; }
}

IContact[] contacts = new IContact[]
{
    new CompanyContact
    {
        Name = "Oz-Ware",
        PhoneNumber = "123456789", 
        RepresentativeName = "John Smith"
    }
};
```

When serializing a CompanyContact as IContact, by default all members of CompanyContact will be emitted because of the `DynamicTypeResolver`:

```c# include=contacts
var serializer = new SerializerBuilder()
    .Build();

serializer.Serialize(Console.Out, contacts);
```

outputs:

```yaml output
- Name: Oz-Ware
  PhoneNumber: 123456789
  RepresentativeName: John Smith
```

If instead we configure the serializer with `StaticTypeResolver`, only the members defined in the interface will be emitted:

```c# include=contacts
var serializer = new SerializerBuilder()
    .WithTypeResolver(new StaticTypeResolver())
    .Build();

serializer.Serialize(Console.Out, contacts);
```

outputs:

```yaml output
- Name: Oz-Ware
  PhoneNumber: 123456789
```

User-defined implementations of the `ITypeResolver` can be used for more advanced scenarios.

### `WithTagMapping(string, Type)`

Associates a YAML tag to a .NET type. When serializing derived classes or interface implementations, it may be necessary to emit a [tag](https://yaml.org/spec/1.2/spec.html#id2764295) that indicates the type of the data.

Building up on the previous example, when we register a tag mapping for the CompanyContact type, we can see that the tag is emitted along with the contact:

```c# include=contacts
var serializer = new SerializerBuilder()
    .WithTagMapping("!company", typeof(CompanyContact))
    .Build();

serializer.Serialize(Console.Out, contacts);
```

outputs:

```yaml output
- !company
  Name: Oz-Ware
  PhoneNumber: 123456789
  RepresentativeName: John Smith
```

This would allow us to distinguish between different implementations of the IContact interface. Note that the tag will only be emitted if the type could be different. If we were serializing an array of CompanyContact instead, no tag would have been emitted.

### `WithAttributeOverride()`

Associates a [custom attribute](https://docs.microsoft.com/en-us/dotnet/api/system.attribute?view=netstandard-2.0) to a class member. This allows to apply the `YamlIgnore` and `YamlMember` attributes to classes without modifying them:

```c# include=contacts
var serializer = new SerializerBuilder()
    .WithAttributeOverride<CompanyContact>(
        c => c.RepresentativeName,
        new YamlMemberAttribute
        {
            ScalarStyle = ScalarStyle.DoubleQuoted
        }
    )
    .Build();

serializer.Serialize(Console.Out, contacts);
```

outputs:

```yaml output
- Name: Oz-Ware
  PhoneNumber: 123456789
  RepresentativeName: "John Smith"
```

### `WithTypeConverter(IYamlTypeConverter)`

Registers a [type converter](Serialization.TypeConverters) that can completely take control of the serialization of specific types.

Please refer to the dedicated [documentation page](Serialization.TypeConverters) for more details and usage examples.

### `WithoutTypeConverter(Type)`

Removes a previously registered [type converter](Serialization.TypeConverters). This is mostly useful to disable one of the built-in type converters:

- `GuidConverter`
- `SystemTypeConverter`

### `WithTypeInspector()`

Registers a [type inspector](Serialization.TypeInspectors) that provides an abstraction over the .NET reflection API. This is most useful to enrich the built-in type inspectors with additional behaviour. For example, the previously discussed `WithAttributeOverride` method uses a type inspector to enrich the information that is collected through the reflection API.

Please refer to the dedicated [documentation page](Serialization.TypeInspectors) for more details and usage examples.

### `WithoutTypeInspector(Type)`

Removes a previously registered [type inspector](Serialization.TypeInspectors). This method is provided for completeness, but be advised doing so will probably break some functionality.

### `WithMaximumRecursion(int)`

Defines the maximum allowed depth in the serialized object graphs.

### `WithEventEmitter()`

Registers an `IEventEmitter` that can intercept and modify the YAML events before they are written to the `IEmitter`. The following example shows the registration of a custom `IEventEmitter` that forces the flow style for sequences of integers.

```c# id=FlowStyleIntegerSequences
class FlowStyleIntegerSequences : ChainedEventEmitter
{
    public FlowStyleIntegerSequences(IEventEmitter nextEmitter)
        : base(nextEmitter) {}

    public override void Emit(SequenceStartEventInfo eventInfo, IEmitter emitter)
    {
        if (typeof(IEnumerable<int>).IsAssignableFrom(eventInfo.Source.Type))
        {
            eventInfo = new SequenceStartEventInfo(eventInfo.Source)
            {
                Style = SequenceStyle.Flow
            };
        }

        nextEmitter.Emit(eventInfo, emitter);
    }
}
```

```c# include=FlowStyleIntegerSequences
var serializer = new SerializerBuilder()
    .WithEventEmitter(next => new FlowStyleIntegerSequences(next))
    .Build();

serializer.Serialize(Console.Out, new {
    strings = new[] { "one", "two", "three" },
    ints = new[] { 1, 2, 3 }
});
```

outputs:

```yaml output
strings:
- one
- two
- three
ints: [1, 2, 3]
```

### `WithIndentedSequences()`

The default behaviour of the serializer is to put list items on the same indentation level as the parent item. So by default, lists are indented as follows:

```yaml output
list:
- item1
- item2
```

By using this option, the emitter will indent list items (just like other items), resulting in the following output:

```yaml output
list:
  - item1
  - item2
```

### `EnsureRoundtrip()`

Ensures that it will be possible to deserialize the serialized objects.
This option will force the emission of tags and emit only properties with setters.

### `DisableAliases()`

When the same object appears multiple times in the serialized graph, the default behaviour is to emit it only once, assigning it an anchor, and use aliases to reference it on its subsequent occurrences. `DisableAliases` prevents this and causes the object to be emitted multiple times.

Assigning anchors has a performance cost because the object graph must be walked twice, so if you know that there won't be duplicate references, or you don't mind the duplication, it may be benefical to use `DisableAliases`.

The following example illustrates the difference between the two behaviours:

```c#
var address = new
{
    street = "123 Tornado Alley, Suite 16",
    city = "East Westville",
    state = "KS"
};

var receipt = new
{
    bill_to = address,
    ship_to = address,
};

Console.WriteLine("# First, with aliases enabled (default)");
new SerializerBuilder()
    .Build()
    .Serialize(Console.Out, receipt);

Console.WriteLine("# Then, with aliases disabled");
new SerializerBuilder()
    .DisableAliases()
    .Build()
    .Serialize(Console.Out, receipt);
```

outputs:

```yaml output
# First, with aliases enabled (default)
bill_to: &o0
  street: 123 Tornado Alley, Suite 16
  city: East Westville
  state: KS
ship_to: *o0
# Then, with aliases disabled
bill_to:
  street: 123 Tornado Alley, Suite 16
  city: East Westville
  state: KS
ship_to:
  street: 123 Tornado Alley, Suite 16
  city: East Westville
  state: KS
```

### `ConfigureDefaultValuesHandling(DefaultValuesHandling)`

The default behaviour of the serializer is to emit all properties regardless of their value. The behaviour can be modified if one prefers not to see null or default values. The `configuration` parameter is used to specify how properties with null or default values should be handled:

| Value | Behavior |
|-|-|
| Preserve | Specifies that all properties are to be emitted regardless of their value. This is the default behavior. |
| OmitNull | Specifies that properties that contain null references or a null `Nullable<T>` are to be omitted. |
| OmitDefaults | Specifies that properties that that contain their default value, either `default(T)` or the value specified in [`DefaultValueAttribute`](https://docs.microsoft.com/en-us/dotnet/api/system.componentmodel.defaultvalueattribute) are to be omitted. |

The following example illustrates the difference between the behaviours. Consider the following class:

```c# id=nullsanddefaults
class Model
{
    public string NullString => null;
    [DefaultValue("some-default")] public string NullStringWithDefault => null;
    public int ZeroInteger => 0;
    public int? NullableInteger => null;
    [DefaultValue(42)] public int DefaultInteger => 42;
}
```

Compare the outputs depending on the `DefaultValuesHandling` configuration:

```c# include=nullsanddefaults
var model = new Model();

Console.WriteLine("# DefaultValuesHandling.Preserve (default)");
new SerializerBuilder()
    .ConfigureDefaultValuesHandling(DefaultValuesHandling.Preserve)
    .Build()
    .Serialize(Console.Out, model);

Console.WriteLine("# DefaultValuesHandling.OmitNull");
new SerializerBuilder()
    .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
    .Build()
    .Serialize(Console.Out, model);

Console.WriteLine("# DefaultValuesHandling.OmitDefaults");
new SerializerBuilder()
    .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
    .Build()
    .Serialize(Console.Out, model);
```

```yaml output
# DefaultValuesHandling.Preserve (default)
NullString: 
NullStringWithDefault: 
ZeroInteger: 0
NullableInteger: 
DefaultInteger: 42
# DefaultValuesHandling.OmitNull
ZeroInteger: 0
DefaultInteger: 42
# DefaultValuesHandling.OmitDefaults
NullStringWithDefault: 
```

### `JsonCompatible()`

YAML aims to be a superset of JSON. This implies that, in theory, any valid JSON document should also be a valid YAML document. Calling `JsonCompatible` instructs the serializer to emit YAML that uses only constructs that also exist in JSON.

The following example illustrates this:

```c# include=contacts
var serializer = new SerializerBuilder()
    .JsonCompatible()
    .Build();

serializer.Serialize(Console.Out, contacts);
```

outputs:

```json output
[{"Name": "Oz-Ware", "PhoneNumber": "123456789", "RepresentativeName": "John Smith"}]
```

## Extending the serializer

The serializer offers extension points that offer greater control over the serialization process.

Using these requires a deeper understanding of the internals of the `Serializer`.

TODO:

- `WithPreProcessingPhaseObjectGraphVisitor`
- `WithEmissionPhaseObjectGraphVisitor`
- `WithObjectGraphTraversalStrategyFactory`