using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class TemplateValueTests
{
    [Test]
    public void MissingAndNull_StoreKindsAndPredicates()
    {
        TemplateValue missing = TemplateValue.Missing();
        TemplateValue nullValue = TemplateValue.Null();

        Assert.That(missing.Kind, Is.EqualTo(TemplateValueKind.Missing));
        Assert.That(missing.IsMissing(), Is.True);
        Assert.That(missing.IsNull(), Is.False);
        Assert.That(nullValue.Kind, Is.EqualTo(TemplateValueKind.Null));
        Assert.That(nullValue.IsNull(), Is.True);
        Assert.That(nullValue.IsMissing(), Is.False);
    }

    [Test]
    public void ScalarFactories_StoreBooleanNumberStringAndSafeString()
    {
        TemplateValue boolean = TemplateValue.FromBoolean(true);
        TemplateValue number = TemplateValue.FromNumber(12.5m);
        TemplateValue text = TemplateValue.FromString("hello");
        TemplateValue safe = TemplateValue.FromSafeString("<b>safe</b>");

        Assert.That(boolean.Kind, Is.EqualTo(TemplateValueKind.Boolean));
        Assert.That(boolean.BooleanValue, Is.True);
        Assert.That(number.Kind, Is.EqualTo(TemplateValueKind.Number));
        Assert.That(number.NumberValue, Is.EqualTo(12.5m));
        Assert.That(text.Kind, Is.EqualTo(TemplateValueKind.String));
        Assert.That(text.StringValue, Is.EqualTo("hello"));
        Assert.That(text.IsStringLike(), Is.True);
        Assert.That(text.IsSafeString(), Is.False);
        Assert.That(safe.Kind, Is.EqualTo(TemplateValueKind.SafeString));
        Assert.That(safe.StringValue, Is.EqualTo("<b>safe</b>"));
        Assert.That(safe.IsStringLike(), Is.True);
        Assert.That(safe.IsSafeString(), Is.True);
    }

    [Test]
    public void ListFactory_CopiesValuesAndReturnsMissingForInvalidAccess()
    {
        List<TemplateValue> values = new List<TemplateValue>();
        values.Add(TemplateValue.FromString("first"));
        TemplateValue list = TemplateValue.FromList(values);
        values.Add(TemplateValue.FromString("second"));

        Assert.That(list.Kind, Is.EqualTo(TemplateValueKind.List));
        Assert.That(list.IsCollection(), Is.True);
        Assert.That(list.ListValue.Count, Is.EqualTo(1));
        Assert.That(list.GetListItem(0).StringValue, Is.EqualTo("first"));
        Assert.That(list.GetListItem(-1).IsMissing(), Is.True);
        Assert.That(list.GetListItem(1).IsMissing(), Is.True);
        Assert.That(TemplateValue.FromString("x").GetListItem(0).IsMissing(), Is.True);
    }

    [Test]
    public void MapFactory_CopiesValuesAndReturnsMissingForUnknownKeys()
    {
        Dictionary<string, TemplateValue> values = new Dictionary<string, TemplateValue>();
        values["name"] = TemplateValue.FromString("Ada");
        TemplateValue map = TemplateValue.FromMap(values);
        values["name"] = TemplateValue.FromString("Grace");

        Assert.That(map.Kind, Is.EqualTo(TemplateValueKind.Map));
        Assert.That(map.IsCollection(), Is.True);
        Assert.That(map.MapValue.Count, Is.EqualTo(1));
        Assert.That(map.GetMapItem("name").StringValue, Is.EqualTo("Ada"));
        Assert.That(map.GetMapItem("missing").IsMissing(), Is.True);
        Assert.That(TemplateValue.FromString("x").GetMapItem("name").IsMissing(), Is.True);
    }

    [Test]
    public void ObjectFunctionAndFragmentFactories_StoreRuntimePayloads()
    {
        object model = new object();
        TemplateValue objectValue = TemplateValue.FromObject(model);
        TemplateValue function = TemplateValue.FromFunction(delegate(List<TemplateValue> arguments)
        {
            return TemplateValue.FromNumber(arguments.Count);
        });

        TemplateSource source = TemplateSource.FromString("x");
        TemplateTextSyntax text = new TemplateTextSyntax(source.CreateSpan(0, 1));
        List<TemplateSyntaxNode> nodes = new List<TemplateSyntaxNode>();
        nodes.Add(TemplateSyntaxNode.FromText(text));
        TemplateValue fragment = TemplateValue.FromFragment(nodes);
        nodes.Clear();

        List<TemplateValue> callArguments = new List<TemplateValue>();
        callArguments.Add(TemplateValue.FromString("a"));
        callArguments.Add(TemplateValue.FromString("b"));

        Assert.That(objectValue.Kind, Is.EqualTo(TemplateValueKind.Object));
        Assert.That(objectValue.ObjectValue, Is.SameAs(model));
        Assert.That(function.Kind, Is.EqualTo(TemplateValueKind.Function));
        Assert.That(function.Invoke(callArguments).NumberValue, Is.EqualTo(2m));
        Assert.That(fragment.Kind, Is.EqualTo(TemplateValueKind.Fragment));
        Assert.That(fragment.IsCollection(), Is.True);
        Assert.That(fragment.FragmentValue.Count, Is.EqualTo(1));
        Assert.That(fragment.FragmentValue[0].Text, Is.SameAs(text));
    }

    [Test]
    public void FactoriesAndAccessors_RejectNullRequiredValues()
    {
        Assert.Throws<ArgumentNullException>(() => TemplateValue.FromString(null!));
        Assert.Throws<ArgumentNullException>(() => TemplateValue.FromSafeString(null!));
        Assert.Throws<ArgumentNullException>(() => TemplateValue.FromList(null!));
        Assert.Throws<ArgumentNullException>(() => TemplateValue.FromMap(null!));
        Assert.Throws<ArgumentNullException>(() => TemplateValue.FromObject(null!));
        Assert.Throws<ArgumentNullException>(() => TemplateValue.FromFunction(null!));
        Assert.Throws<ArgumentNullException>(() => TemplateValue.FromFragment(null!));
        Assert.Throws<ArgumentNullException>(() => TemplateValue.FromMap(new Dictionary<string, TemplateValue>()).GetMapItem(null!));
        Assert.Throws<ArgumentNullException>(() => TemplateValue.FromFunction(delegate(List<TemplateValue> arguments)
        {
            return TemplateValue.Null();
        }).Invoke(null!));
    }

    [Test]
    public void Factories_RejectNullCollectionItems()
    {
        List<TemplateValue> values = new List<TemplateValue>();
        values.Add(null!);
        Dictionary<string, TemplateValue> map = new Dictionary<string, TemplateValue>();
        map["x"] = null!;
        List<TemplateSyntaxNode> nodes = new List<TemplateSyntaxNode>();
        nodes.Add(null!);

        Assert.Throws<ArgumentException>(() => TemplateValue.FromList(values));
        Assert.Throws<ArgumentException>(() => TemplateValue.FromMap(map));
        Assert.Throws<ArgumentException>(() => TemplateValue.FromFragment(nodes));
    }

    [Test]
    public void Invoke_RejectsNonFunctionValuesAndNullResults()
    {
        TemplateValue nonFunction = TemplateValue.FromString("x");
        TemplateValue nullReturningFunction = TemplateValue.FromFunction(delegate(List<TemplateValue> arguments)
        {
            return null!;
        });

        Assert.Throws<InvalidOperationException>(() => nonFunction.Invoke(new List<TemplateValue>()));
        Assert.Throws<InvalidOperationException>(() => nullReturningFunction.Invoke(new List<TemplateValue>()));
    }
}
