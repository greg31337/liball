using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class TemplateValueConverterTests
{
    [Test]
    public void Convert_NullDbNullAndExistingTemplateValue_ReturnExpectedValues()
    {
        TemplateValue existing = TemplateValue.FromString("ready");

        TemplateValue nullValue = TemplateValueConverter.Convert(null);
        TemplateValue dbNull = TemplateValueConverter.Convert(DBNull.Value);
        TemplateValue same = TemplateValueConverter.Convert(existing);

        Assert.That(nullValue.Kind, Is.EqualTo(TemplateValueKind.Null));
        Assert.That(dbNull.Kind, Is.EqualTo(TemplateValueKind.Null));
        Assert.That(same, Is.SameAs(existing));
    }

    [Test]
    public void Convert_PrimitiveBuiltIns_ReturnScalarTemplateValues()
    {
        Assert.That(TemplateValueConverter.Convert(true).BooleanValue, Is.True);
        AssertNumber(TemplateValueConverter.Convert((byte)2), 2m);
        AssertNumber(TemplateValueConverter.Convert((short)3), 3m);
        AssertNumber(TemplateValueConverter.Convert(4), 4m);
        AssertNumber(TemplateValueConverter.Convert(5L), 5m);
        AssertNumber(TemplateValueConverter.Convert(6.5f), 6.5m);
        AssertNumber(TemplateValueConverter.Convert(7.25d), 7.25m);
        AssertNumber(TemplateValueConverter.Convert(8.75m), 8.75m);
        AssertString(TemplateValueConverter.Convert("hello"), "hello");
        AssertString(TemplateValueConverter.Convert('x'), "x");
    }

    [Test]
    public void Convert_EnumerableValues_RecursivelyConvertsItems()
    {
        object?[] values = new object?[]
        {
            1,
            "two",
            null,
            new object[] { true }
        };

        TemplateValue result = TemplateValueConverter.Convert(values);

        Assert.That(result.Kind, Is.EqualTo(TemplateValueKind.List));
        Assert.That(result.ListValue.Count, Is.EqualTo(4));
        AssertNumber(result.ListValue[0], 1m);
        AssertString(result.ListValue[1], "two");
        Assert.That(result.ListValue[2].Kind, Is.EqualTo(TemplateValueKind.Null));
        Assert.That(result.ListValue[3].Kind, Is.EqualTo(TemplateValueKind.List));
        Assert.That(result.ListValue[3].ListValue[0].BooleanValue, Is.True);
    }

    [Test]
    public void Convert_DictionaryValues_RecursivelyConvertsEntriesAndKeys()
    {
        Hashtable table = new Hashtable();
        table["name"] = "Ada";
        table[1] = 42;
        table["items"] = new object[] { "x", 2 };

        TemplateValue result = TemplateValueConverter.Convert(table);

        Assert.That(result.Kind, Is.EqualTo(TemplateValueKind.Map));
        AssertString(result.GetMapItem("name"), "Ada");
        AssertNumber(result.GetMapItem("1"), 42m);
        Assert.That(result.GetMapItem("items").Kind, Is.EqualTo(TemplateValueKind.List));
        AssertString(result.GetMapItem("items").ListValue[0], "x");
        AssertNumber(result.GetMapItem("items").ListValue[1], 2m);
    }

    [Test]
    public void Convert_FunctionFragmentAndObject_ReturnExpectedRuntimeValues()
    {
        Func<List<TemplateValue>, TemplateValue> function = delegate (List<TemplateValue> arguments)
        {
            return TemplateValue.FromNumber(arguments.Count);
        };

        TemplateSource source = TemplateSource.FromString("x");
        List<TemplateSyntaxNode> fragmentNodes = new List<TemplateSyntaxNode>();
        fragmentNodes.Add(TemplateSyntaxNode.FromText(new TemplateTextSyntax(source.CreateSpan(0, 1))));
        DateTime objectValue = new DateTime(2026, 1, 2);

        TemplateValue functionValue = TemplateValueConverter.Convert(function);
        TemplateValue fragmentValue = TemplateValueConverter.Convert(fragmentNodes);
        TemplateValue fallbackObject = TemplateValueConverter.Convert(objectValue);

        Assert.That(functionValue.Kind, Is.EqualTo(TemplateValueKind.Function));
        AssertNumber(functionValue.Invoke(new List<TemplateValue>()), 0m);
        Assert.That(fragmentValue.Kind, Is.EqualTo(TemplateValueKind.Fragment));
        Assert.That(fragmentValue.FragmentValue.Count, Is.EqualTo(1));
        Assert.That(fallbackObject.Kind, Is.EqualTo(TemplateValueKind.Object));
        Assert.That(fallbackObject.ObjectValue, Is.EqualTo(objectValue));
    }

    [Test]
    public void ExplicitEnumerableAndDictionaryMethods_RejectNullInputs()
    {
        Assert.Throws<ArgumentNullException>(() => TemplateValueConverter.FromEnumerable(null!));
        Assert.Throws<ArgumentNullException>(() => TemplateValueConverter.FromDictionary(null!));
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
