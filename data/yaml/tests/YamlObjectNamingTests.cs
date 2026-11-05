using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlObjectNamingTests
{
    [Test]
    public void FieldNaming_AppliesSupportedConventions()
    {
        Assert.That(global::yaml.YamlFieldNaming.Apply("HTTPServerPort", global::yaml.YamlNamingConvention.Exact), Is.EqualTo("HTTPServerPort"));
        Assert.That(global::yaml.YamlFieldNaming.Apply("HTTPServerPort", global::yaml.YamlNamingConvention.CamelCase), Is.EqualTo("httpServerPort"));
        Assert.That(global::yaml.YamlFieldNaming.Apply("http_server_port", global::yaml.YamlNamingConvention.PascalCase), Is.EqualTo("HttpServerPort"));
        Assert.That(global::yaml.YamlFieldNaming.Apply("HTTPServerPort", global::yaml.YamlNamingConvention.SnakeCase), Is.EqualTo("http_server_port"));
        Assert.That(global::yaml.YamlFieldNaming.Apply("HTTPServerPort", global::yaml.YamlNamingConvention.KebabCase), Is.EqualTo("http-server-port"));
    }

    [Test]
    public void Serialize_UsesConfiguredPublicFieldNamingConvention()
    {
        ObjectNamingSample sample = new ObjectNamingSample();
        sample.FirstName = "Ada";
        sample.HTTPServerPort = 443;
        sample.IsAdmin = true;

        AssertSerializedNames(sample, global::yaml.YamlNamingConvention.Exact, "FirstName: Ada\nHTTPServerPort: 443\nIsAdmin: true\n");
        AssertSerializedNames(sample, global::yaml.YamlNamingConvention.CamelCase, "firstName: Ada\nhttpServerPort: 443\nisAdmin: true\n");
        AssertSerializedNames(sample, global::yaml.YamlNamingConvention.PascalCase, "FirstName: Ada\nHttpServerPort: 443\nIsAdmin: true\n");
        AssertSerializedNames(sample, global::yaml.YamlNamingConvention.SnakeCase, "first_name: Ada\nhttp_server_port: 443\nis_admin: true\n");
        AssertSerializedNames(sample, global::yaml.YamlNamingConvention.KebabCase, "first-name: Ada\nhttp-server-port: 443\nis-admin: true\n");
    }

    [Test]
    public void Deserialize_UsesConfiguredPublicFieldNamingConvention()
    {
        AssertDeserializedNames(global::yaml.YamlNamingConvention.Exact, "FirstName: Ada\nHTTPServerPort: 443\nIsAdmin: true\n");
        AssertDeserializedNames(global::yaml.YamlNamingConvention.CamelCase, "firstName: Ada\nhttpServerPort: 443\nisAdmin: true\n");
        AssertDeserializedNames(global::yaml.YamlNamingConvention.PascalCase, "FirstName: Ada\nHttpServerPort: 443\nIsAdmin: true\n");
        AssertDeserializedNames(global::yaml.YamlNamingConvention.SnakeCase, "first_name: Ada\nhttp_server_port: 443\nis_admin: true\n");
        AssertDeserializedNames(global::yaml.YamlNamingConvention.KebabCase, "first-name: Ada\nhttp-server-port: 443\nis-admin: true\n");
    }

    public void AssertSerializedNames(ObjectNamingSample sample, global::yaml.YamlNamingConvention convention, string expected)
    {
        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        options.Object.NamingConvention = convention;

        string text = global::yaml.Yaml.Serialize(sample, options);

        Assert.That(text, Is.EqualTo(expected));
    }

    public void AssertDeserializedNames(global::yaml.YamlNamingConvention convention, string text)
    {
        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        options.Object.NamingConvention = convention;

        ObjectNamingSample sample = (ObjectNamingSample)global::yaml.Yaml.Deserialize(text, typeof(ObjectNamingSample), options)!;

        Assert.That(sample.FirstName, Is.EqualTo("Ada"));
        Assert.That(sample.HTTPServerPort, Is.EqualTo(443));
        Assert.That(sample.IsAdmin, Is.EqualTo(true));
    }
}

public class ObjectNamingSample
{
    public string FirstName;
    public int HTTPServerPort;
    public bool IsAdmin;

    public ObjectNamingSample()
    {
        FirstName = "";
        HTTPServerPort = 0;
        IsAdmin = false;
    }
}
