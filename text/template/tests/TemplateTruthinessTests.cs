using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class TemplateTruthinessTests
{
    [Test]
    public void NativeTruthiness_TreatsZeroAndEmptyValuesAsFalsy()
    {
        AssertFalsy(TemplateValue.Missing(), TemplateTruthinessMode.Native);
        AssertFalsy(TemplateValue.Null(), TemplateTruthinessMode.Native);
        AssertFalsy(TemplateValue.FromBoolean(false), TemplateTruthinessMode.Native);
        AssertFalsy(TemplateValue.FromNumber(0m), TemplateTruthinessMode.Native);
        AssertFalsy(TemplateValue.FromString(""), TemplateTruthinessMode.Native);
        AssertFalsy(TemplateValue.FromSafeString(""), TemplateTruthinessMode.Native);
        AssertFalsy(TemplateValue.FromList(new List<TemplateValue>()), TemplateTruthinessMode.Native);
        AssertFalsy(TemplateValue.FromMap(new Dictionary<string, TemplateValue>()), TemplateTruthinessMode.Native);
        AssertFalsy(TemplateValue.FromFragment(new List<TemplateSyntaxNode>()), TemplateTruthinessMode.Native);

        AssertTruthy(TemplateValue.FromBoolean(true), TemplateTruthinessMode.Native);
        AssertTruthy(TemplateValue.FromNumber(1m), TemplateTruthinessMode.Native);
        AssertTruthy(TemplateValue.FromString("x"), TemplateTruthinessMode.Native);
        AssertTruthy(TemplateValue.FromObject(new object()), TemplateTruthinessMode.Native);
    }

    [Test]
    public void MustacheTruthiness_TreatsNumbersAndMapsAsTruthyButEmptyListsAsFalsy()
    {
        AssertFalsy(TemplateValue.Missing(), TemplateTruthinessMode.Mustache);
        AssertFalsy(TemplateValue.Null(), TemplateTruthinessMode.Mustache);
        AssertFalsy(TemplateValue.FromBoolean(false), TemplateTruthinessMode.Mustache);
        AssertFalsy(TemplateValue.FromString(""), TemplateTruthinessMode.Mustache);
        AssertFalsy(TemplateValue.FromList(new List<TemplateValue>()), TemplateTruthinessMode.Mustache);

        AssertTruthy(TemplateValue.FromNumber(0m), TemplateTruthinessMode.Mustache);
        AssertTruthy(TemplateValue.FromMap(new Dictionary<string, TemplateValue>()), TemplateTruthinessMode.Mustache);
    }

    [Test]
    public void HandlebarsTruthiness_TreatsZeroAndEmptyListsAsFalsyButMapsAsTruthy()
    {
        AssertFalsy(TemplateValue.FromNumber(0m), TemplateTruthinessMode.Handlebars);
        AssertFalsy(TemplateValue.FromString(""), TemplateTruthinessMode.Handlebars);
        AssertFalsy(TemplateValue.FromList(new List<TemplateValue>()), TemplateTruthinessMode.Handlebars);
        AssertTruthy(TemplateValue.FromMap(new Dictionary<string, TemplateValue>()), TemplateTruthinessMode.Handlebars);

        AssertTruthy(TemplateValue.FromNumber(2m), TemplateTruthinessMode.Handlebars);
        AssertTruthy(TemplateValue.FromString("x"), TemplateTruthinessMode.Handlebars);
    }

    [Test]
    public void LiquidTruthiness_OnlyTreatsMissingNullAndFalseAsFalsy()
    {
        AssertFalsy(TemplateValue.Missing(), TemplateTruthinessMode.Liquid);
        AssertFalsy(TemplateValue.Null(), TemplateTruthinessMode.Liquid);
        AssertFalsy(TemplateValue.FromBoolean(false), TemplateTruthinessMode.Liquid);

        AssertTruthy(TemplateValue.FromNumber(0m), TemplateTruthinessMode.Liquid);
        AssertTruthy(TemplateValue.FromString(""), TemplateTruthinessMode.Liquid);
        AssertTruthy(TemplateValue.FromList(new List<TemplateValue>()), TemplateTruthinessMode.Liquid);
        AssertTruthy(TemplateValue.FromMap(new Dictionary<string, TemplateValue>()), TemplateTruthinessMode.Liquid);
    }

    [Test]
    public void JinjaTruthiness_FollowsPythonLikeEmptyValueRules()
    {
        AssertFalsy(TemplateValue.FromNumber(0m), TemplateTruthinessMode.Jinja);
        AssertFalsy(TemplateValue.FromString(""), TemplateTruthinessMode.Jinja);
        AssertFalsy(TemplateValue.FromList(new List<TemplateValue>()), TemplateTruthinessMode.Jinja);
        AssertFalsy(TemplateValue.FromMap(new Dictionary<string, TemplateValue>()), TemplateTruthinessMode.Jinja);

        AssertTruthy(TemplateValue.FromNumber(1m), TemplateTruthinessMode.Jinja);
        AssertTruthy(TemplateValue.FromString("x"), TemplateTruthinessMode.Jinja);
    }

    [Test]
    public void OptionsResolveProfileDefaultAndAllowExplicitOverride()
    {
        TemplateOptions liquid = new TemplateOptions();
        liquid.SyntaxProfile = "liquid";

        TemplateOptions explicitNative = new TemplateOptions();
        explicitNative.SyntaxProfile = "liquid";
        explicitNative.TruthinessMode = TemplateTruthinessMode.Native;

        TemplateOptions django = new TemplateOptions();
        django.SyntaxProfile = "django";

        Assert.That(TemplateTruthiness.ResolveMode(liquid), Is.EqualTo(TemplateTruthinessMode.Liquid));
        AssertTruthy(TemplateValue.FromNumber(0m), liquid);
        Assert.That(TemplateTruthiness.ResolveMode(explicitNative), Is.EqualTo(TemplateTruthinessMode.Native));
        AssertFalsy(TemplateValue.FromNumber(0m), explicitNative);
        Assert.That(TemplateTruthiness.ResolveMode(django), Is.EqualTo(TemplateTruthinessMode.Jinja));
    }

    [Test]
    public void NullInputs_AreRejected()
    {
        TemplateValue value = TemplateValue.FromBoolean(true);
        TemplateOptions options = new TemplateOptions();

        Assert.Throws<ArgumentNullException>(() => TemplateTruthiness.IsTruthy(null!, options));
        Assert.Throws<ArgumentNullException>(() => TemplateTruthiness.IsTruthy(value, (TemplateOptions)null!));
        Assert.Throws<ArgumentNullException>(() => TemplateTruthiness.IsTruthy(null!, TemplateTruthinessMode.Native));
        Assert.Throws<ArgumentNullException>(() => TemplateTruthiness.ResolveMode(null!));
    }

    static void AssertTruthy(TemplateValue value, TemplateTruthinessMode mode)
    {
        Assert.That(TemplateTruthiness.IsTruthy(value, mode), Is.True);
        Assert.That(TemplateTruthiness.IsFalsy(value, mode), Is.False);
    }

    static void AssertFalsy(TemplateValue value, TemplateTruthinessMode mode)
    {
        Assert.That(TemplateTruthiness.IsTruthy(value, mode), Is.False);
        Assert.That(TemplateTruthiness.IsFalsy(value, mode), Is.True);
    }

    static void AssertTruthy(TemplateValue value, TemplateOptions options)
    {
        Assert.That(TemplateTruthiness.IsTruthy(value, options), Is.True);
        Assert.That(TemplateTruthiness.IsFalsy(value, options), Is.False);
    }

    static void AssertFalsy(TemplateValue value, TemplateOptions options)
    {
        Assert.That(TemplateTruthiness.IsTruthy(value, options), Is.False);
        Assert.That(TemplateTruthiness.IsFalsy(value, options), Is.True);
    }
}
