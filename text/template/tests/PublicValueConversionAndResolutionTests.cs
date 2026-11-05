using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace template.Tests;

[TestFixture]
public class PublicValueConversionAndResolutionTests
{
    [Test]
    public void ValueFactory_ConvertsBuiltInScalarsTemporalValuesAndSequences()
    {
        TemplateValueFactory factory = new TemplateValueFactory();
        object[] numbers = new object[]
        {
            (sbyte)-1,
            (byte)2,
            (short)-3,
            (ushort)4,
            -5,
            (uint)6,
            (long)-7,
            (ulong)8,
            9.5f,
            10.25d,
            11.75m
        };

        int numberIndex = 0;
        while (numberIndex < numbers.Length)
        {
            Assert.That(factory.Convert(numbers[numberIndex]).Kind, Is.EqualTo(TemplateValueKind.Number));
            numberIndex = numberIndex + 1;
        }

        Assert.That(factory.Convert(true).Kind, Is.EqualTo(TemplateValueKind.Boolean));
        Assert.That(factory.Convert('x').StringValue, Is.EqualTo("x"));
        Assert.That(factory.Convert("text").Kind, Is.EqualTo(TemplateValueKind.String));
        Assert.That(factory.Convert(new DateTime(2026, 8, 26)).Kind, Is.EqualTo(TemplateValueKind.Object));
        Assert.That(factory.Convert(new DateTimeOffset(2026, 8, 26, 1, 2, 3, TimeSpan.Zero)).Kind, Is.EqualTo(TemplateValueKind.Object));
        Assert.That(factory.Convert(TimeSpan.FromMinutes(5)).Kind, Is.EqualTo(TemplateValueKind.Object));
        Assert.That(factory.Convert(new int[] { 1, 2 }).Kind, Is.EqualTo(TemplateValueKind.List));
        Assert.That(factory.Convert(new List<string> { "a", "b" }).ListValue.Count, Is.EqualTo(2));
        Assert.That(factory.Convert(new PublicEnumerableOnly(new object[] { "a", 2 })).ListValue[1].NumberValue, Is.EqualTo(2m));
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.Convert(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.Convert(double.PositiveInfinity));
    }

    [Test]
    public void ValueFactory_ConvertsMutableAndReadOnlyDictionariesAsMaps()
    {
        TemplateValueFactory factory = new TemplateValueFactory();
        Hashtable mutable = new Hashtable();
        mutable["name"] = "Ada";
        PublicReadOnlyDictionary readOnly = new PublicReadOnlyDictionary();
        readOnly.Add("name", "Grace");
        readOnly.Add("items", new int[] { 1, 2 });

        TemplateValue mutableValue = factory.Convert(mutable);
        TemplateValue readOnlyValue = factory.Convert(readOnly);

        Assert.That(mutableValue.Kind, Is.EqualTo(TemplateValueKind.Map));
        Assert.That(mutableValue.MapValue["name"].StringValue, Is.EqualTo("Ada"));
        Assert.That(readOnlyValue.Kind, Is.EqualTo(TemplateValueKind.Map));
        Assert.That(readOnlyValue.MapValue["name"].StringValue, Is.EqualTo("Grace"));
        Assert.That(readOnlyValue.MapValue["items"].ListValue[1].NumberValue, Is.EqualTo(2m));
    }

    [Test]
    public void ValueConversionPolicy_UsesCultureAndCustomConvertersRecursively()
    {
        TemplateValueConversionPolicy policy = new TemplateValueConversionPolicy();
        policy.Culture = CultureInfo.GetCultureInfo("fr-FR");
        policy.Converters.Add(new PublicMoneyConverter());
        TemplateValueFactory factory = new TemplateValueFactory();
        factory.Configure(policy);
        Hashtable model = new Hashtable();
        model[1.5m] = new PublicMoney(12.5m);
        model["items"] = new object[] { new PublicMoney(3m) };

        TemplateValue converted = factory.Convert(model);

        Assert.That(converted.MapValue["1,5"].NumberValue, Is.EqualTo(12.5m));
        Assert.That(converted.MapValue["items"].ListValue[0].NumberValue, Is.EqualTo(3m));
        Assert.Throws<ArgumentNullException>(() => factory.Configure(null!));
    }

    [Test]
    public void MemberResolver_AppliesCaseCustomAliasesAndNestedConversionPolicy()
    {
        TemplateMemberResolverOptions resolverOptions = new TemplateMemberResolverOptions();
        resolverOptions.CaseSensitive = false;
        resolverOptions.MemberNameMappers.Add(new PublicMemberAliasMapper());
        resolverOptions.ValueConversionPolicy.Converters.Add(new PublicMoneyConverter());
        TemplateMemberResolver resolver = new TemplateMemberResolver();
        resolver.Configure(resolverOptions);
        TemplateRenderer renderer = CreateRenderer(new TemplateOptions(), resolver);
        PublicResolutionModel model = new PublicResolutionModel();

        string output = renderer.RenderToString("{{ displayname }}|{{ total }}", TemplateValue.FromObject(model));

        Assert.That(output, Is.EqualTo("Ada|19.5"));
    }

    [Test]
    public void MemberResolver_HonorsDictionaryFirstAndMemberFirstLookup()
    {
        PublicDictionaryCollision model = new PublicDictionaryCollision();
        model["Value"] = "dictionary";
        TemplateMemberResolverOptions dictionaryOptions = new TemplateMemberResolverOptions();
        TemplateMemberResolver dictionaryResolver = new TemplateMemberResolver();
        dictionaryResolver.Configure(dictionaryOptions);
        TemplateRenderer dictionaryRenderer = CreateRenderer(new TemplateOptions(), dictionaryResolver);
        TemplateMemberResolverOptions memberOptions = new TemplateMemberResolverOptions();
        memberOptions.DictionaryFirst = false;
        TemplateMemberResolver memberResolver = new TemplateMemberResolver();
        memberResolver.Configure(memberOptions);
        TemplateRenderer memberRenderer = CreateRenderer(new TemplateOptions(), memberResolver);

        Assert.That(dictionaryRenderer.RenderToString("{{ Value }}", TemplateValue.FromObject(model)), Is.EqualTo("dictionary"));
        Assert.That(memberRenderer.RenderToString("{{ Value }}", TemplateValue.FromObject(model)), Is.EqualTo("member"));
    }

    [Test]
    public void NameLookupOrder_SelectsLocalCurrentOrRootBeforeOtherScopes()
    {
        TemplateValue model = CreateScopeModel();
        string source = "{% set name \"local\" %}{% with child %}{{ name }}|{{ $.name }}|{{ this.name }}{% endwith %}";

        TemplateOptions localOptions = new TemplateOptions();
        TemplateOptions currentOptions = new TemplateOptions();
        currentOptions.NameLookupOrder = TemplateNameLookupOrder.CurrentLocalRoot;
        TemplateOptions rootOptions = new TemplateOptions();
        rootOptions.NameLookupOrder = TemplateNameLookupOrder.RootLocalCurrent;

        Assert.That(CreateRenderer(localOptions, new TemplateMemberResolver()).RenderToString(source, model), Is.EqualTo("local|root|current"));
        Assert.That(CreateRenderer(currentOptions, new TemplateMemberResolver()).RenderToString(source, model), Is.EqualTo("current|root|current"));
        Assert.That(CreateRenderer(rootOptions, new TemplateMemberResolver()).RenderToString(source, model), Is.EqualTo("root|root|current"));
    }

    [Test]
    public void Resolution_HandlesParentRootMissingAndDeniedMembers()
    {
        TemplateRenderer renderer = new TemplateRenderer();
        string pathOutput = renderer.RenderToString("{% with child %}{% with grandchild %}{{ ../name }}|{{ $.name }}|{{ missing }}{% endwith %}{% endwith %}", CreateScopeModel());
        Assert.That(pathOutput, Is.EqualTo("current|root|"));

        PublicResolutionModel model = new PublicResolutionModel();
        Assert.That(renderer.RenderToString("{{ GetType }}", TemplateValue.FromObject(model)), Is.EqualTo(""));

        TemplateOptions strictMissing = new TemplateOptions();
        strictMissing.StrictMissingValues = true;
        TemplateRenderer missingRenderer = CreateRenderer(strictMissing, new TemplateMemberResolver());
        Assert.Throws<InvalidOperationException>(() => missingRenderer.RenderToString("{{ missing }}", TemplateValue.FromObject(model)));

        TemplateOptions strictUnsafe = new TemplateOptions();
        strictUnsafe.StrictUnsafeMembers = true;
        TemplateRenderer unsafeRenderer = CreateRenderer(strictUnsafe, new TemplateMemberResolver());
        Assert.Throws<InvalidOperationException>(() => unsafeRenderer.RenderToString("{{ GetType }}", TemplateValue.FromObject(model)));
    }

    static TemplateRenderer CreateRenderer(TemplateOptions options, TemplateMemberResolver resolver)
    {
        TemplateRenderer renderer = new TemplateRenderer();
        renderer.Configure(options, new TemplateRegistry(), resolver);
        return renderer;
    }

    static TemplateValue CreateScopeModel()
    {
        TemplateValueFactory factory = new TemplateValueFactory();
        Hashtable grandchild = new Hashtable();
        grandchild["name"] = "grandchild";
        Hashtable child = new Hashtable();
        child["name"] = "current";
        child["grandchild"] = grandchild;
        Hashtable root = new Hashtable();
        root["name"] = "root";
        root["child"] = child;
        return factory.Convert(root);
    }
}

public class PublicMoney
{
    public decimal Amount;

    public PublicMoney(decimal amount)
    {
        Amount = amount;
    }
}

public class PublicMoneyConverter : ICustomTemplateValueConverter
{
    public TemplateValue? Convert(object value)
    {
        PublicMoney? money = value as PublicMoney;
        if (money == null)
        {
            return null;
        }

        return TemplateValue.FromNumber(money.Amount);
    }
}

public class PublicMemberAliasMapper : ITemplateMemberNameMapper
{
    public string? MapName(string memberName)
    {
        if (memberName == "Balance")
        {
            return "total";
        }

        return null;
    }
}

public class PublicResolutionModel
{
    public string DisplayName = "Ada";
    public PublicMoney Balance = new PublicMoney(19.5m);
}

public class PublicDictionaryCollision : Hashtable
{
    public string Value
    {
        get
        {
            return "member";
        }
    }
}

public class PublicEnumerableOnly : IEnumerable
{
    object[] Values;

    public PublicEnumerableOnly(object[] values)
    {
        Values = values;
    }

    public IEnumerator GetEnumerator()
    {
        return Values.GetEnumerator();
    }
}

public class PublicReadOnlyDictionary : IReadOnlyDictionary<string, object?>
{
    Dictionary<string, object?> Values;

    public PublicReadOnlyDictionary()
    {
        Values = new Dictionary<string, object?>();
    }

    public object? this[string key]
    {
        get
        {
            return Values[key];
        }
    }

    public IEnumerable<string> Keys
    {
        get
        {
            return Values.Keys;
        }
    }

    public IEnumerable<object?> ValuesCollection
    {
        get
        {
            return Values.Values;
        }
    }

    IEnumerable<object?> IReadOnlyDictionary<string, object?>.Values
    {
        get
        {
            return Values.Values;
        }
    }

    public int Count
    {
        get
        {
            return Values.Count;
        }
    }

    public void Add(string key, object? value)
    {
        Values.Add(key, value);
    }

    public bool ContainsKey(string key)
    {
        return Values.ContainsKey(key);
    }

    public bool TryGetValue(string key, out object? value)
    {
        return Values.TryGetValue(key, out value);
    }

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
    {
        return Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return Values.GetEnumerator();
    }
}
