using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class TemplateMemberResolverTests
{
    [Test]
    public void ResolveMember_TemplateMaps_UseKeysBeforeIntrinsicCount()
    {
        TemplateMemberResolver resolver = new TemplateMemberResolver();
        Dictionary<string, TemplateValue> values = new Dictionary<string, TemplateValue>();
        values["name"] = TemplateValue.FromString("Ada");
        values["count"] = TemplateValue.FromString("stored");
        TemplateValue map = TemplateValue.FromMap(values);

        AssertString(resolver.ResolveMember(map, "name"), "Ada");
        AssertString(resolver.ResolveMember(map, "count"), "stored");
        AssertNumber(resolver.ResolveMember(map, "length"), 2m);
        Assert.That(resolver.ResolveMember(map, "missing").IsMissing(), Is.True);
    }

    [Test]
    public void ResolveMember_CaseInsensitiveOptions_ApplyToMapAndReflectionNames()
    {
        TemplateMemberResolverOptions options = new TemplateMemberResolverOptions();
        options.CaseSensitive = false;
        TemplateMemberResolver resolver = TemplateMemberResolver.Create(options);
        Dictionary<string, TemplateValue> values = new Dictionary<string, TemplateValue>();
        values["DisplayName"] = TemplateValue.FromString("Ada");

        AssertString(resolver.ResolveMember(TemplateValue.FromMap(values), "displayname"), "Ada");
        AssertString(resolver.ResolveMember(TemplateValue.FromObject(new ReflectionSample()), "nickname"), "Countess");
    }

    [Test]
    public void ResolveMember_NamingPolicies_DefaultToExactOnly()
    {
        TemplateMemberResolver resolver = new TemplateMemberResolver();
        Dictionary<string, TemplateValue> values = new Dictionary<string, TemplateValue>();
        values["DisplayName"] = TemplateValue.FromString("Ada");
        TemplateValue map = TemplateValue.FromMap(values);

        AssertString(resolver.ResolveMember(map, "DisplayName"), "Ada");
        Assert.That(resolver.ResolveMember(map, "displayName").IsMissing(), Is.True);
        Assert.That(resolver.ResolveMember(map, "display_name").IsMissing(), Is.True);
        Assert.That(resolver.ResolveMember(map, "display-name").IsMissing(), Is.True);
    }

    [Test]
    public void ResolveMember_NamingPolicies_EnableCamelSnakeAndKebabAccess()
    {
        TemplateMemberResolverOptions options = new TemplateMemberResolverOptions();
        options.AddMemberNamingPolicy(TemplateMemberNamingPolicy.CamelCase);
        options.AddMemberNamingPolicy(TemplateMemberNamingPolicy.SnakeCase);
        options.AddMemberNamingPolicy(TemplateMemberNamingPolicy.KebabCase);
        TemplateMemberResolver resolver = TemplateMemberResolver.Create(options);
        Dictionary<string, TemplateValue> values = new Dictionary<string, TemplateValue>();
        values["DisplayName"] = TemplateValue.FromString("Ada");
        values["URLValue"] = TemplateValue.FromString("https://example.test");
        TemplateValue map = TemplateValue.FromMap(values);
        TemplateValue sample = TemplateValue.FromObject(new ReflectionSample());

        AssertString(resolver.ResolveMember(map, "displayName"), "Ada");
        AssertString(resolver.ResolveMember(map, "display_name"), "Ada");
        AssertString(resolver.ResolveMember(map, "display-name"), "Ada");
        AssertString(resolver.ResolveMember(map, "url_value"), "https://example.test");
        AssertString(resolver.ResolveMember(sample, "nickName"), "Countess");
        AssertString(resolver.ResolveMember(sample, "nick_name"), "Countess");
        AssertString(resolver.ResolveMember(sample, "nick-name"), "Countess");
    }

    [Test]
    public void ResolveMember_NamingPolicies_CanDisableExactAccess()
    {
        TemplateMemberResolverOptions options = new TemplateMemberResolverOptions();
        options.MemberNamingPolicies.Clear();
        options.AddMemberNamingPolicy(TemplateMemberNamingPolicy.SnakeCase);
        TemplateMemberResolver resolver = TemplateMemberResolver.Create(options);
        Dictionary<string, TemplateValue> values = new Dictionary<string, TemplateValue>();
        values["DisplayName"] = TemplateValue.FromString("Ada");
        TemplateValue map = TemplateValue.FromMap(values);

        Assert.That(resolver.ResolveMember(map, "DisplayName").IsMissing(), Is.True);
        AssertString(resolver.ResolveMember(map, "display_name"), "Ada");
    }

    [Test]
    public void ResolveIndex_TemplateListsAndStrings_SupportNumericIndexes()
    {
        TemplateMemberResolver resolver = new TemplateMemberResolver();
        List<TemplateValue> values = new List<TemplateValue>();
        values.Add(TemplateValue.FromString("zero"));
        values.Add(TemplateValue.FromNumber(1m));
        TemplateValue list = TemplateValue.FromList(values);

        AssertNumber(resolver.ResolveMember(list, "length"), 2m);
        AssertString(resolver.ResolveMember(list, "0"), "zero");
        AssertNumber(resolver.ResolveIndex(list, TemplateValue.FromNumber(1m)), 1m);
        Assert.That(resolver.ResolveIndex(list, TemplateValue.FromNumber(1.5m)).IsMissing(), Is.True);
        AssertString(resolver.ResolveIndex(TemplateValue.FromString("abc"), TemplateValue.FromNumber(2m)), "c");
        AssertNumber(resolver.ResolveMember(TemplateValue.FromString("abc"), "size"), 3m);
    }

    [Test]
    public void ResolveMember_RawDictionariesAndCollections_AreConvertedAtAccessTime()
    {
        TemplateMemberResolver resolver = new TemplateMemberResolver();
        Hashtable table = new Hashtable();
        table["name"] = "Ada";
        table[1] = 42;
        object[] array = new object[] { "zero", 1 };

        AssertString(resolver.ResolveMember(TemplateValue.FromObject(table), "name"), "Ada");
        AssertNumber(resolver.ResolveMember(TemplateValue.FromObject(table), "1"), 42m);
        AssertNumber(resolver.ResolveMember(TemplateValue.FromObject(table), "count"), 2m);
        AssertString(resolver.ResolveIndex(TemplateValue.FromObject(array), TemplateValue.FromNumber(0m)), "zero");
        AssertNumber(resolver.ResolveMember(TemplateValue.FromObject(array), "length"), 2m);
    }

    [Test]
    public void ResolveIndex_EnumerableOnlyObjects_WalkSequenceForIndexAndCount()
    {
        TemplateMemberResolver resolver = new TemplateMemberResolver();
        EnumerableOnly values = new EnumerableOnly(new object[] { "first", "second", "third" });
        TemplateValue target = TemplateValue.FromObject(values);

        AssertString(resolver.ResolveIndex(target, TemplateValue.FromNumber(1m)), "second");
        AssertNumber(resolver.ResolveMember(target, "count"), 3m);
        Assert.That(resolver.ResolveIndex(target, TemplateValue.FromNumber(3m)).IsMissing(), Is.True);
    }

    [Test]
    public void ResolveMember_PublicFieldsAndProperties_CanBeDisabledSeparately()
    {
        ReflectionSample sample = new ReflectionSample();
        TemplateMemberResolver resolver = new TemplateMemberResolver();

        AssertString(resolver.ResolveMember(TemplateValue.FromObject(sample), "Name"), "Ada");
        AssertNumber(resolver.ResolveMember(TemplateValue.FromObject(sample), "Age"), 37m);

        TemplateMemberResolverOptions options = new TemplateMemberResolverOptions();
        options.AllowPublicFields = false;
        options.AllowPublicProperties = false;
        TemplateMemberResolver disabledResolver = TemplateMemberResolver.Create(options);

        Assert.That(disabledResolver.ResolveMember(TemplateValue.FromObject(sample), "Name").IsMissing(), Is.True);
        Assert.That(disabledResolver.ResolveMember(TemplateValue.FromObject(sample), "Age").IsMissing(), Is.True);
    }

    [Test]
    public void ResolveMember_SafeMethods_AreExposedOnlyWhenAllowlisted()
    {
        ReflectionSample sample = new ReflectionSample();
        TemplateValue target = TemplateValue.FromObject(sample);
        TemplateMemberResolver defaultResolver = new TemplateMemberResolver();

        Assert.That(defaultResolver.ResolveMember(target, "Display").IsMissing(), Is.True);

        TemplateMemberResolverOptions options = new TemplateMemberResolverOptions();
        options.AllowPublicMethods = true;
        options.AddSafeMethod("Display");
        options.AddSafeMethod("WithArgument");
        TemplateMemberResolver resolver = TemplateMemberResolver.Create(options);

        TemplateValue display = resolver.ResolveMember(target, "Display");

        Assert.That(display.Kind, Is.EqualTo(TemplateValueKind.Function));
        AssertString(display.Invoke(new List<TemplateValue>()), "Ada:37");
        Assert.Throws<InvalidOperationException>(() => display.Invoke(CreateOneArgument()));
        Assert.That(resolver.ResolveMember(target, "WithArgument").IsMissing(), Is.True);
        Assert.That(resolver.ResolveMember(target, "Secret").IsMissing(), Is.True);
    }

    [Test]
    public void ResolveMember_NamingPolicies_ApplyToSafeMethodExposure()
    {
        TemplateMemberResolverOptions options = new TemplateMemberResolverOptions();
        options.AddMemberNamingPolicy(TemplateMemberNamingPolicy.CamelCase);
        options.AddMemberNamingPolicy(TemplateMemberNamingPolicy.KebabCase);
        options.AllowPublicMethods = true;
        options.AddSafeMethod("DisplayName");
        TemplateMemberResolver resolver = TemplateMemberResolver.Create(options);
        TemplateValue target = TemplateValue.FromObject(new ReflectionSample());

        TemplateValue camel = resolver.ResolveMember(target, "displayName");
        TemplateValue kebab = resolver.ResolveMember(target, "display-name");

        Assert.That(camel.Kind, Is.EqualTo(TemplateValueKind.Function));
        AssertString(camel.Invoke(new List<TemplateValue>()), "Ada Lovelace");
        Assert.That(kebab.Kind, Is.EqualTo(TemplateValueKind.Function));
        AssertString(kebab.Invoke(new List<TemplateValue>()), "Ada Lovelace");
    }

    [Test]
    public void ConstructorsAndAccessors_RejectNullRequiredValues()
    {
        TemplateMemberResolverOptions options = new TemplateMemberResolverOptions();
        TemplateMemberResolver resolver = TemplateMemberResolver.Create(options);

        Assert.Throws<ArgumentNullException>(() => TemplateMemberResolver.Create(null!));
        Assert.Throws<ArgumentNullException>(() => options.AddSafeMethod(null!));
        Assert.Throws<ArgumentNullException>(() => options.IsSafeMethod(null!));
        Assert.Throws<ArgumentNullException>(() => options.NamesEqual(null!, "x"));
        Assert.Throws<ArgumentNullException>(() => options.NamesEqual("x", null!));
        Assert.Throws<ArgumentNullException>(() => options.MemberNamesEqual(null!, "x"));
        Assert.Throws<ArgumentNullException>(() => options.MemberNamesEqual("x", null!));
        Assert.Throws<ArgumentNullException>(() => resolver.ResolveMember(null!, "x"));
        Assert.Throws<ArgumentNullException>(() => resolver.ResolveMember(TemplateValue.Null(), null!));
        Assert.Throws<ArgumentNullException>(() => resolver.ResolveIndex(null!, TemplateValue.FromNumber(0m)));
        Assert.Throws<ArgumentNullException>(() => resolver.ResolveIndex(TemplateValue.FromString("x"), null!));
    }

    static List<TemplateValue> CreateOneArgument()
    {
        List<TemplateValue> arguments = new List<TemplateValue>();
        arguments.Add(TemplateValue.FromString("x"));
        return arguments;
    }

    static void AssertNumber(TemplateValue value, decimal expected)
    {
        Assert.That(value.Kind, Is.EqualTo(TemplateValueKind.Number));
        Assert.That(value.NumberValue, Is.EqualTo(expected));
    }

    static void AssertString(TemplateValue value, string expected)
    {
        Assert.That(value.Kind, Is.EqualTo(TemplateValueKind.String));
        Assert.That(value.StringValue, Is.EqualTo(expected));
    }
}

public class ReflectionSample
{
    public string Name = "Ada";

    public int Age
    {
        get
        {
            return 37;
        }
    }

    public string NickName
    {
        get
        {
            return "Countess";
        }
    }

    public string Display()
    {
        return Name + ":37";
    }

    public string DisplayName()
    {
        return Name + " Lovelace";
    }

    public string WithArgument(string suffix)
    {
        return Name + suffix;
    }

    public string Secret()
    {
        return "hidden";
    }
}

public class EnumerableOnly : IEnumerable
{
    object[] Values;

    public EnumerableOnly(object[] values)
    {
        Values = values;
    }

    public IEnumerator GetEnumerator()
    {
        return Values.GetEnumerator();
    }
}
