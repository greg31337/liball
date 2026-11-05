using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlObjectHandlingOptionsTests
{
    [Test]
    public void Serialize_SkipsNullAndDefaultPublicFieldsWhenConfigured()
    {
        ObjectHandlingSample sample = new ObjectHandlingSample();
        sample.Name = null;
        sample.Count = 0;
        sample.Enabled = false;
        sample.Note = "present";
        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        options.Object.IgnoreNulls = true;
        options.Object.IgnoreDefaults = true;

        string text = global::yaml.Yaml.Serialize(sample, options);

        Assert.That(text, Is.EqualTo("Note: present\n"));
    }

    [Test]
    public void Deserialize_ThrowsForUnknownMembersByDefault()
    {
        Assert.Throws<global::yaml.YamlException>(() => global::yaml.Yaml.Deserialize("Name: Ada\nExtra: value\n", typeof(ObjectHandlingSample)));
    }

    [Test]
    public void Deserialize_IgnoresUnknownMembersWhenConfigured()
    {
        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        options.Object.ErrorOnUnknownMembers = false;

        ObjectHandlingSample sample = (ObjectHandlingSample)global::yaml.Yaml.Deserialize("Name: Ada\nExtra: value\n", typeof(ObjectHandlingSample), options)!;

        Assert.That(sample.Name, Is.EqualTo("Ada"));
        Assert.That(sample.Count, Is.EqualTo(7));
        Assert.That(sample.Enabled, Is.EqualTo(true));
    }

    [Test]
    public void Deserialize_RequiresConfiguredMembersUsingNamingConvention()
    {
        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        options.Object.NamingConvention = global::yaml.YamlNamingConvention.SnakeCase;
        options.Object.RequireConfiguredMembers = true;
        options.Object.AddRequiredMember("FirstName");

        RequiredObjectSample present = (RequiredObjectSample)global::yaml.Yaml.Deserialize("first_name: Ada\ncount: 2\n", typeof(RequiredObjectSample), options)!;

        Assert.That(present.FirstName, Is.EqualTo("Ada"));
        Assert.That(present.Count, Is.EqualTo(2));
        Assert.Throws<global::yaml.YamlException>(() => global::yaml.Yaml.Deserialize("count: 2\n", typeof(RequiredObjectSample), options));
    }

    [Test]
    public void Deserialize_SkipsNullAndDefaultValuesWhenConfigured()
    {
        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        options.Object.IgnoreNulls = true;
        options.Object.IgnoreDefaults = true;

        ObjectHandlingSample sample = (ObjectHandlingSample)global::yaml.Yaml.Deserialize("Name: null\nCount: 0\nEnabled: false\nNote: changed\n", typeof(ObjectHandlingSample), options)!;

        Assert.That(sample.Name, Is.EqualTo("existing"));
        Assert.That(sample.Count, Is.EqualTo(7));
        Assert.That(sample.Enabled, Is.EqualTo(true));
        Assert.That(sample.Note, Is.EqualTo("changed"));
    }
}

public class ObjectHandlingSample
{
    public string? Name;
    public int Count;
    public bool Enabled;
    public string Note;

    public ObjectHandlingSample()
    {
        Name = "existing";
        Count = 7;
        Enabled = true;
        Note = "";
    }
}

public class RequiredObjectSample
{
    public string FirstName;
    public int Count;

    public RequiredObjectSample()
    {
        FirstName = "";
        Count = 0;
    }
}
