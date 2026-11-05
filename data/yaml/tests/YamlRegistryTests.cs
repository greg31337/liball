using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlRegistryTests
{
    [Test]
    public void TagRegistry_RegisterFindRemoveAndCopy_WorkIndependently()
    {
        global::yaml.YamlTagRegistry registry = new global::yaml.YamlTagRegistry();
        global::yaml.YamlTagRegistration tag = registry.Register("!point", global::yaml.YamlNodeKind.Mapping);
        global::yaml.YamlTagRegistration typeTag = registry.RegisterType("!guid", typeof(global::System.Guid), global::yaml.YamlNodeKind.Scalar);

        Assert.That(registry.Count(), Is.EqualTo(2));
        Assert.That(registry.FindByTag("!point"), Is.SameAs(tag));
        Assert.That(registry.FindByType(typeof(global::System.Guid)), Is.SameAs(typeTag));
        Assert.That(registry.ContainsTag("!guid"), Is.True);

        global::yaml.YamlTagRegistry copy = registry.Copy();
        registry.RemoveTag("!point");

        Assert.That(registry.Count(), Is.EqualTo(1));
        Assert.That(copy.Count(), Is.EqualTo(2));
        Assert.That(copy.FindByTag("!point")!.NodeKind, Is.EqualTo(global::yaml.YamlNodeKind.Mapping));
    }

    [Test]
    public void TagRegistry_ScalarResolvers_ReturnFirstMatchingTag()
    {
        global::yaml.YamlTagRegistry registry = new global::yaml.YamlTagRegistry();
        registry.RegisterScalarResolver("!lower", (value, style) => style == global::yaml.YamlScalarStyle.Plain && value == "match");
        registry.RegisterScalarResolver("!second", (value, style) => value == "match");

        Assert.That(registry.ResolveScalarTag("match", global::yaml.YamlScalarStyle.Plain), Is.EqualTo("!lower"));
        Assert.That(registry.ResolveScalarTag("match", global::yaml.YamlScalarStyle.SingleQuoted), Is.EqualTo("!second"));
        Assert.That(registry.ResolveScalarTag("miss", global::yaml.YamlScalarStyle.Plain), Is.EqualTo(""));
    }

    [Test]
    public void ScalarConverterRegistry_RegisterFindInvokeRemoveAndCopy_WorkIndependently()
    {
        global::yaml.YamlScalarConverterRegistry registry = new global::yaml.YamlScalarConverterRegistry();
        global::yaml.YamlScalarConverterRegistration converter = registry.RegisterConverter(
            "!upper",
            typeof(string),
            value => value.ToUpperInvariant(),
            value => value == null ? "" : value.ToString()!.ToLowerInvariant());
        converter.PreferredStyle = global::yaml.YamlScalarStyle.DoubleQuoted;

        Assert.That(registry.Count(), Is.EqualTo(1));
        Assert.That(registry.FindByTag("!upper"), Is.SameAs(converter));
        Assert.That(registry.FindByType(typeof(string)), Is.SameAs(converter));
        Assert.That(converter.CanRead(), Is.True);
        Assert.That(converter.CanWrite(), Is.True);
        Assert.That(converter.Read("abc"), Is.EqualTo("ABC"));
        Assert.That(converter.Write("ABC"), Is.EqualTo("abc"));

        global::yaml.YamlScalarConverterRegistry copy = registry.Copy();
        registry.RemoveTag("!upper");

        Assert.That(registry.Count(), Is.EqualTo(0));
        Assert.That(copy.Count(), Is.EqualTo(1));
        Assert.That(copy.FindByTag("!upper")!.PreferredStyle, Is.EqualTo(global::yaml.YamlScalarStyle.DoubleQuoted));
    }

    [Test]
    public void ScalarConverterRegistration_EmptyCallbacks_AreNoOps()
    {
        global::yaml.YamlScalarConverterRegistration converter = global::yaml.YamlScalarConverterRegistration.Create("!noop", typeof(string));

        Assert.That(converter.CanRead(), Is.False);
        Assert.That(converter.CanWrite(), Is.False);
        Assert.That(converter.Read("abc"), Is.Null);
        Assert.That(converter.Write("abc"), Is.EqualTo(""));
    }

    [Test]
    public void TagRegistry_InterfaceResolver_CanHoldStateAndIsCopied()
    {
        CountingScalarResolver resolver = new CountingScalarResolver();
        global::yaml.YamlTagRegistry registry = new global::yaml.YamlTagRegistry();
        registry.RegisterScalarResolverInstance("!match", resolver);

        Assert.That(registry.ResolveScalarTag("match", global::yaml.YamlScalarStyle.Plain), Is.EqualTo("!match"));
        Assert.That(registry.ResolveScalarTag("miss", global::yaml.YamlScalarStyle.Plain), Is.EqualTo(""));

        global::yaml.YamlTagRegistry copy = registry.Copy();
        Assert.That(copy.ResolveScalarTag("match", global::yaml.YamlScalarStyle.Plain), Is.EqualTo("!match"));
        Assert.That(resolver.MatchCount, Is.EqualTo(3));
    }

    [Test]
    public void ScalarConverterRegistry_InterfaceConverter_ReadsWritesAndIsCopied()
    {
        PrefixScalarConverter converter = new PrefixScalarConverter();
        global::yaml.YamlScalarConverterRegistry registry = new global::yaml.YamlScalarConverterRegistry();
        global::yaml.YamlScalarConverterRegistration registration = registry.RegisterConverterInstance("!prefix", typeof(string), converter);

        Assert.That(registration.CanRead(), Is.True);
        Assert.That(registration.CanWrite(), Is.True);
        Assert.That(registration.Read("value"), Is.EqualTo("read:value"));
        Assert.That(registration.Write("value"), Is.EqualTo("write:value"));

        global::yaml.YamlScalarConverterRegistration copied = registry.Copy().FindByTag("!prefix")!;
        Assert.That(copied.Converter, Is.SameAs(converter));
    }
}

public class CountingScalarResolver : global::yaml.IYamlScalarResolver
{
    public int MatchCount;

    public CountingScalarResolver()
    {
        MatchCount = 0;
    }

    public bool Matches(string value, global::yaml.YamlScalarStyle style)
    {
        MatchCount = MatchCount + 1;
        return value == "match";
    }
}

public class PrefixScalarConverter : global::yaml.IYamlScalarConverter
{
    public PrefixScalarConverter()
    {
    }

    public object Read(string value)
    {
        return "read:" + value;
    }

    public string Write(object value)
    {
        return "write:" + value;
    }
}
