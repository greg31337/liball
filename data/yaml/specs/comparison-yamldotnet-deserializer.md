# Object deserialization from YAML

As [discussed](Serialization), deserialization of objects to YAML is performed by the `Deserializer` class.

For simple use cases, it is enough to just create an instance and call the `Deserialize()` method:

```c#
var deserializer = new YamlDotNet.Serialization.Deserializer();
var dict = deserializer.Deserialize<Dictionary<string, string>>("hello: world");
Console.WriteLine(dict["hello"]);
```

This will produce the following output:

```text output
world
```

While the default behaviour is enough for demonstration purposes, in many cases it will be necessary to customize it. For example, one may want to use a different naming convention.

## Configuring the deserializer

The `Deserializer` itself is immutable thus its behaviour cannot be altered after its construction. Instead, the configuration should be performed on an instance of `DeserializerBuilder`, which will then take care of creating a `Deserializer` according to its configuration.

The `DeserializerBuilder` provides a series of methods that configure specific aspects of the deserializer. These methods are described below.

Many of these methods register a component that is inserted into a chain of components, and offer various overloads that allow to configure the location of the registered component in the chain. This is discussed in detail in the [dedicated documentation page](Serialization.ComponentChainRegistration).

### `IgnoreFields()`

Instructs the deserializer to ignore public fields. The default behaviour is to assign public fields and public properties.

### `WithNamingConvention(INamingConvention)`

Specifies the [naming convention](Serialization.NamingConventions) that will be used. The naming convention specifies how .NET member names are converted to YAML keys.

```c#
var deserializer = new DeserializerBuilder()
    .WithNamingConvention(CamelCaseNamingConvention.Instance)
    .Build();
```

### `WithTypeResolver(ITypeResolver)`

Specifies the [type resolver](Serialization.TypeResolver) that will be used to determine which type should be considered when entering an object's member. By default, `DynamicTypeResolver` is used, which uses the actual type of the object. The other implementation that is included in the library is `StaticTypeResolver`, which uses the declaring type of the member. See the corresponding [documentation on the `SerializerBuilder`](Serialization.Serializer) for an example.

TODO: Is there a meaningful example that can be included here ?

### `WithTagMapping(string, Type)`

Associates a YAML tag to a .NET type. When serializing derived classes or interface implementations, it may be necessary to emit a [tag](https://yaml.org/spec/1.2/spec.html#id2764295) that indicates the type of the data.

Consider the following YAML document:

```yaml input
- Name: Oz-Ware
  PhoneNumber: 123456789
```

and a corresponding C# class:

```c# id=contacts
class Contact
{
    public string Name { get; set; }
    public string PhoneNumber { get; set; }

    public override string ToString() => $"name={Name}, tel={PhoneNumber}";
}
```

If we know beforehand the structure of the YAML document, we can parse it easily:

```c# include=contacts
var deserializer = new DeserializerBuilder()
    .Build();

var contacts = deserializer.Deserialize<List<Contact>>(yamlInput);
Console.WriteLine(contacts[0]);
```

outputs:

```text output
name=Oz-Ware, tel=123456789
```

But when the concrete structure is not known in advance, the deserializer has no way of determining that each item of the sequence should be deserialized as a Contact:

```c# include=contacts
var deserializer = new DeserializerBuilder()
    .Build();

var contacts = deserializer.Deserialize<object>(yamlInput);
foreach (var contact in (IEnumerable)contacts)
{
    Console.WriteLine(contact.GetType().FullName);
}
```

outputs:

```text output
System.Collections.Generic.Dictionary`2[[System.Object, System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Object, System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
```

Notice how each contact was deserialized as a `Dictionary<object, object>`. This is because neither the type information that we supplied to the `Deserialize` method (`object`) nor the YAML document contained any type information for the contacts. This can be solved by adding a [tag](https://yaml.org/spec/1.2/spec.html#id2764295) to each contact:

```yaml input
- !contact
  Name: Oz-Ware
  PhoneNumber: 123456789
```

The tag can have any value, but needs to be registered as follows:

```c# include=contacts
var deserializer = new DeserializerBuilder()
    .WithTagMapping("!contact", typeof(Contact))
    .Build();

var contacts = deserializer.Deserialize<object>(yamlInput);
foreach (var contact in (IEnumerable)contacts)
{
    Console.WriteLine(contact.GetType().Name);
}
```

outputs:

```text output
Contact
```

### `WithAttributeOverride()`

Associates a [custom attribute](https://docs.microsoft.com/en-us/dotnet/api/system.attribute?view=netstandard-2.0) to a class member. This allows to apply the `YamlIgnore` and `YamlMember` attributes to classes without modifying them:

```yaml input
full_name: Oz-Ware
PhoneNumber: 123456789
```

```c# include=contacts
var deserializer = new DeserializerBuilder()
    .WithAttributeOverride<Contact>(
        c => c.Name,
        new YamlMemberAttribute
        {
            Alias = "full_name"
        }
    )
    .Build();

var contact = deserializer.Deserialize<Contact>(yamlInput);
Console.WriteLine(contact);
```

outputs:

```text output
name=Oz-Ware, tel=123456789
```

### `WithTypeConverter(IYamlTypeConverter)`

Registers a [type converter](Serialization.TypeConverters) that can completely take control of the deserialization of specific types.

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

### `WithObjectFactory(IObjectFactory)`

Replaces the `DefaultObjectFactory` that is used to create instances of types as needed during deserialization. One situation where this may be handy is when using the deserializer to construct business objects that depend on services, such as in the following example:

```c# id=orderItem
interface IDiscountCalculationPolicy
{
    decimal Apply(decimal basePrice);
}

class TenPercentDiscountCalculationPolicy : IDiscountCalculationPolicy
{
    public decimal Apply(decimal basePrice)
    {
        return basePrice - basePrice / 10m;
    }
}

class OrderItem
{
    private readonly IDiscountCalculationPolicy discountPolicy;

    public OrderItem(IDiscountCalculationPolicy discountPolicy)
    {
        this.discountPolicy = discountPolicy;
    }

    public string Description { get; set; }
    public decimal BasePrice { get; set; }

    public decimal FinalPrice => discountPolicy.Apply(BasePrice);
}
```

Because the Contact class now requires a constructor argument, the default object factory will not be able to instantiate it. Therefore, we need to create a custom object factory that implements `IObjectFactory`. The recommended way to implement the interface is to handle the specific types that require special logic, and to delegate the handling of all other types to `DefaultObjectFactory`:

```c# id=orderItemFactory
class OrderItemFactory : IObjectFactory
{
    private readonly IObjectFactory fallback;

    public OrderItemFactory(IObjectFactory fallback)
    {
        this.fallback = fallback;
    }

    public object Create(Type type)
    {
        if (type == typeof(OrderItem))
        {
            return new OrderItem(new TenPercentDiscountCalculationPolicy());
        }
        else
        {
            return fallback.Create(type);
        }
    }
}
```

```yaml input
Description: High Heeled "Ruby" Slippers
BasePrice: 100.27
```

```c# include=orderItem include=orderItemFactory
var deserializer = new DeserializerBuilder()
    .WithObjectFactory(new OrderItemFactory(new DefaultObjectFactory()))
    .Build();

var orderItem = deserializer.Deserialize<OrderItem>(yamlInput);
Console.WriteLine($"Final price: {orderItem.FinalPrice}");
```

outputs:

```text output
Final price: 90.243
```

### `WithNodeDeserializer`

Registers an additional `INodeDeserializer`. Implementations of `INodeDeserializer` are responsible for deserializing a single object from an `IParser`. This interface can also be used to enrich the behaviour of built-in node deserializers. The following example shows how we can take advantage of this to add validation support to the deserializer.

First, we'll implement a new `INodeDeserializer` that will decorate another INodeDeserializer with validation:

```c# id=validatingNodeDeserializer
public class ValidatingNodeDeserializer : INodeDeserializer
{
    private readonly INodeDeserializer inner;

    public ValidatingNodeDeserializer(INodeDeserializer inner)
    {
        this.inner = inner;
    }

    public bool Deserialize(IParser parser, Type expectedType, Func<IParser, Type, object> nestedObjectDeserializer, out object value)
    {
        if (inner.Deserialize(parser, expectedType, nestedObjectDeserializer, out value))
        {
            var context = new ValidationContext(value, null, null);
            Validator.ValidateObject(value, context, true);
            return true;
        }
        return false;
    }
}
```

We'll also add validation attributes to our Contact class:

```c# id=validatedContact
class Contact
{
    [Required]
    public string Name { get; set; }

    [Required]
    public string PhoneNumber { get; set; }
}
```

We will attempt to deserialize the following document, that is missing the phone number, this will not pass the validation:

```yaml input
Name: John Smith
```

Then we can use this deserializer to decorate the built-in `ObjectNodeDeserializer`:

```c# include=validatingNodeDeserializer include=validatedContact
var deserializer = new DeserializerBuilder()
    .WithNodeDeserializer(
        inner => new ValidatingNodeDeserializer(inner),
        s => s.InsteadOf<ObjectNodeDeserializer>()
    )
    .Build();

try
{
    deserializer.Deserialize<Contact>(yamlInput);
}
catch(YamlException ex)
{
    Console.WriteLine(ex.Message);
    Console.WriteLine(ex.InnerException.Message);
}
```

outputs:

```text output
(Line: 1, Col: 1, Idx: 0) - (Line: 1, Col: 1, Idx: 0): Exception during deserialization
The PhoneNumber field is required.
```

### `WithNodeTypeResolver`

Registers an `INodeTypeResolver`. Implementing this interface allows to extend the node type resolution, which is the process of dermining the .NET type that corresponds to a YAML node. This process can be used to resolve tags to types, and also to specify an implementation when the type to deserialize is an interface.

#### Example 1: mapping a set of tags to a set of types

Let's define an `INodeTypeResolver` that will resolve any tag in the format `!clr:<type name>` to the .NET type with the same name.

```c#
public class SystemTypeFromTagNodeTypeResolver : INodeTypeResolver
{
    public bool Resolve(NodeEvent nodeEvent, ref Type currentType)
    {
        if (nodeEvent.Tag.StartsWith("!clr:"))
        {
            var netTypeName = nodeEvent.Tag.Substring(5);
            var type = Type.GetType(netTypeName);
            if (type != null)
            {
                currentType = type;
                return true;
            }
        }

        return false;
    }
}
```


### `IgnoreUnmatchedProperties`

If the property corresponding to the YAML key does not exist at the deserialization destination, instruct the deserializer to ignore it and continue processing. The default behavior is to throw an exception if the property corresponding to the YAML key does not exist in the deserialized destination. If this instruction is given, the exception will not be thrown and the key will be ignored and processing will continue.

```c#
var deserializer = new DeserializerBuilder()
	.IgnoreUnmatchedProperties()
 	.Build();
```

// contacts



<!--

## Extending the deserializer

The deserializer offers extension points that offer greater control over the serialization process.

Using these requires a deeper understanding of the internals of the `Serializer`.

TODO:

- `WithPreProcessingPhaseObjectGraphVisitor`
- `WithEmissionPhaseObjectGraphVisitor`
- `WithObjectGraphTraversalStrategyFactory` -->
