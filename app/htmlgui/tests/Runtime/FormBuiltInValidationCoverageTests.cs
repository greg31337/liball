using System;
using System.Globalization;
using htmlgui;

namespace htmlgui.Tests;

public class FormBuiltInValidationCoverageTests
{
    [Test]
    public void RequiredAndLengthRulesUsePortableValueAndUnicodeScalarBoundaries()
    {
        FormValidationCore validation = new FormValidationCore(new FormCore());

        Assert.Multiple(() =>
        {
            Assert.That(validation.IsRequiredValidationValue(new UiValue()), Is.False);
            Assert.That(validation.IsRequiredValidationValue(new UiValue { Kind = UiValueKind.Boolean }), Is.False);
            Assert.That(validation.IsRequiredValidationValue(new UiValue { Kind = UiValueKind.Boolean, BooleanValue = true }), Is.True);
            Assert.That(validation.IsRequiredValidationValue(Text(string.Empty)), Is.False);
            Assert.That(validation.IsRequiredValidationValue(Text(" ")), Is.True);
            Assert.That(validation.IsRequiredValidationValue(Binary()), Is.False);
            Assert.That(validation.IsRequiredValidationValue(Binary(1)), Is.True);
            Assert.That(validation.IsRequiredValidationValue(ArrayValue()), Is.False);
            Assert.That(validation.IsRequiredValidationValue(ArrayValue(Text("item"))), Is.True);
            Assert.That(validation.IsRequiredValidationValue(ObjectValue()), Is.False);
            Assert.That(validation.IsRequiredValidationValue(ObjectValue(Entry("key", Text("value")))), Is.True);
            Assert.That(validation.CountUnicodeScalars("😀"), Is.EqualTo(1));
            Assert.That(validation.CountUnicodeScalars("e\u0301"), Is.EqualTo(2));
            Assert.That(validation.CountUnicodeScalars("👩‍👩‍👧‍👦"), Is.EqualTo(7));
            Assert.That(validation.CountUnicodeScalars("\ud800"), Is.EqualTo(1));
        });
    }

    [Test]
    public void NumericRangesAndStepsStayExactAndIndependentOfCurrentCulture()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
            FormCore forms = new FormCore();
            UiField integerBoundary = Field("integer", Integer(long.MaxValue),
                Rule("integer-range", "range", Entry("maximum", Integer(long.MaxValue - 1))));
            UiField decimalBoundary = Field("decimal", Decimal(decimal.MaxValue),
                Rule("decimal-range", "range", Entry("maximum", Decimal(decimal.MaxValue - 1m))));
            UiField exactStep = Field("step", Decimal(1.3m),
                Rule("step-rule", "step", Entry("minimum", Decimal(0.1m)), Entry("step", Decimal(0.2m))),
                Rule("exact-range", "range", Entry("minimum", Decimal(1.3m)), Entry("maximum", Decimal(1.3m))));
            forms.RegisterForm(new UiForm { Key = "numbers", Fields = new UiField[] { integerBoundary, decimalBoundary, exactStep } });
            FormValidationCore validation = new FormValidationCore(forms);

            ValidationRuleResult integerOutside = validation.EvaluateBuiltInRule("numbers", "integer", "integer-range");
            ValidationRuleResult decimalOutside = validation.EvaluateBuiltInRule("numbers", "decimal", "decimal-range");
            ValidationRuleResult aligned = validation.EvaluateBuiltInRule("numbers", "step", "step-rule");
            ValidationRuleResult inclusive = validation.EvaluateBuiltInRule("numbers", "step", "exact-range");
            forms.UpdateFormFieldValue("numbers", "step", Decimal(1.3000000000000000000000000001m));
            ValidationRuleResult adjacentDecimal = validation.EvaluateBuiltInRule("numbers", "step", "exact-range");

            Assert.Multiple(() =>
            {
                Assert.That(integerOutside.IsValid, Is.False);
                Assert.That(decimalOutside.IsValid, Is.False);
                Assert.That(aligned.IsValid, Is.True);
                Assert.That(inclusive.IsValid, Is.True);
                Assert.That(adjacentDecimal.IsValid, Is.False);
            });
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Test]
    public void PatternsMatchWholeValuesAndRejectUnsupportedOrOversizedExpressions()
    {
        FormCore forms = new FormCore();
        UiField exact = Field("exact", Text("XAB12Y"), Rule("format", "pattern", Entry("pattern", Text("[A-Z]{2}\\d{2}"))));
        UiField alternative = Field("alternative", Text("dog"), Rule("alternative-rule", "pattern", Entry("pattern", Text("(cat|dog)"))));
        UiField backReference = Field("back-reference", Text("aa"), Rule("back-reference-rule", "pattern", Entry("pattern", Text("(a)\\1"))));
        UiField lookAhead = Field("look-ahead", Text("a"), Rule("look-ahead-rule", "pattern", Entry("pattern", Text("(?=a)a"))));
        UiField oversized = Field("oversized", Text("a"), Rule("oversized-rule", "pattern", Entry("pattern", Text(new string('a', FormValidationCore.MaximumPatternLength + 1)))));
        forms.RegisterForm(new UiForm { Key = "patterns", Fields = new UiField[] { exact, alternative, backReference, lookAhead, oversized } });
        FormValidationCore validation = new FormValidationCore(forms);

        ValidationRuleResult surroundingText = validation.EvaluateBuiltInRule("patterns", "exact", "format");
        forms.UpdateFormFieldValue("patterns", "exact", Text("AB12"));
        ValidationRuleResult exactValue = validation.EvaluateBuiltInRule("patterns", "exact", "format");
        ValidationRuleResult alternativeValue = validation.EvaluateBuiltInRule("patterns", "alternative", "alternative-rule");
        ValidationRuleResult rejectedBackReference = validation.EvaluateBuiltInRule("patterns", "back-reference", "back-reference-rule");
        ValidationRuleResult rejectedLookAhead = validation.EvaluateBuiltInRule("patterns", "look-ahead", "look-ahead-rule");
        ValidationRuleResult rejectedOversized = validation.EvaluateBuiltInRule("patterns", "oversized", "oversized-rule");

        Assert.Multiple(() =>
        {
            Assert.That(surroundingText.IsValid, Is.False);
            Assert.That(exactValue.IsValid, Is.True);
            Assert.That(alternativeValue.IsValid, Is.True);
            Assert.That(rejectedBackReference.Code, Is.EqualTo("validation.pattern-parameter"));
            Assert.That(rejectedLookAhead.Code, Is.EqualTo("validation.pattern-parameter"));
            Assert.That(rejectedOversized.Code, Is.EqualTo("validation.pattern-parameter"));
        });
    }

    [Test]
    public void CrossFieldComparisonSupportsEveryOperatorWithoutLosingIntegerPrecision()
    {
        FormCore forms = new FormCore();
        UiField reference = Field("reference", Integer(long.MaxValue - 1));
        UiField candidate = Field("candidate", Integer(long.MaxValue),
            ComparisonRule("equal", "equal"),
            ComparisonRule("not-equal", "not-equal"),
            ComparisonRule("less", "less-than"),
            ComparisonRule("less-equal", "less-than-or-equal"),
            ComparisonRule("greater", "greater-than"),
            ComparisonRule("greater-equal", "greater-than-or-equal"));
        forms.RegisterForm(new UiForm { Key = "comparison", Fields = new UiField[] { reference, candidate } });
        FormValidationCore validation = new FormValidationCore(forms);

        ValidationRuleResult unequal = validation.EvaluateBuiltInRule("comparison", "candidate", "equal");
        ValidationRuleResult notEqual = validation.EvaluateBuiltInRule("comparison", "candidate", "not-equal");
        ValidationRuleResult less = validation.EvaluateBuiltInRule("comparison", "candidate", "less");
        ValidationRuleResult lessEqual = validation.EvaluateBuiltInRule("comparison", "candidate", "less-equal");
        ValidationRuleResult greater = validation.EvaluateBuiltInRule("comparison", "candidate", "greater");
        ValidationRuleResult greaterEqual = validation.EvaluateBuiltInRule("comparison", "candidate", "greater-equal");
        forms.UpdateFormFieldValue("comparison", "candidate", Integer(long.MaxValue - 1));
        ValidationRuleResult equal = validation.EvaluateBuiltInRule("comparison", "candidate", "equal");
        ValidationRuleResult equalLess = validation.EvaluateBuiltInRule("comparison", "candidate", "less-equal");
        ValidationRuleResult equalGreater = validation.EvaluateBuiltInRule("comparison", "candidate", "greater-equal");

        Assert.Multiple(() =>
        {
            Assert.That(unequal.IsValid, Is.False);
            Assert.That(notEqual.IsValid, Is.True);
            Assert.That(less.IsValid, Is.False);
            Assert.That(lessEqual.IsValid, Is.False);
            Assert.That(greater.IsValid, Is.True);
            Assert.That(greaterEqual.IsValid, Is.True);
            Assert.That(equal.IsValid, Is.True);
            Assert.That(equalLess.IsValid, Is.True);
            Assert.That(equalGreater.IsValid, Is.True);
        });
    }

    [Test]
    public void SelectionCountIncludesBothBoundsAndRejectsWrongValueShapes()
    {
        FormCore forms = new FormCore();
        UiField choices = Field("choices", ArrayValue(), Rule("count", "selection-count", Entry("minimum", Integer(0)), Entry("maximum", Integer(2))));
        UiField scalar = Field("scalar", Text("one"), Rule("scalar-count", "selection-count", Entry("maximum", Integer(1))));
        UiField inverted = Field("inverted", ArrayValue(Text("one")), Rule("inverted-count", "selection-count", Entry("minimum", Integer(2)), Entry("maximum", Integer(1))));
        forms.RegisterForm(new UiForm { Key = "selection", Fields = new UiField[] { choices, scalar, inverted } });
        FormValidationCore validation = new FormValidationCore(forms);

        ValidationRuleResult empty = validation.EvaluateBuiltInRule("selection", "choices", "count");
        forms.UpdateFormFieldValue("selection", "choices", ArrayValue(Text("one"), Text("two")));
        ValidationRuleResult upperBoundary = validation.EvaluateBuiltInRule("selection", "choices", "count");
        forms.UpdateFormFieldValue("selection", "choices", ArrayValue(Text("one"), Text("two"), Text("three")));
        ValidationRuleResult aboveMaximum = validation.EvaluateBuiltInRule("selection", "choices", "count");
        ValidationRuleResult wrongShape = validation.EvaluateBuiltInRule("selection", "scalar", "scalar-count");
        ValidationRuleResult malformed = validation.EvaluateBuiltInRule("selection", "inverted", "inverted-count");

        Assert.Multiple(() =>
        {
            Assert.That(empty.IsValid, Is.True);
            Assert.That(upperBoundary.IsValid, Is.True);
            Assert.That(aboveMaximum.IsValid, Is.False);
            Assert.That(wrongShape.Code, Is.EqualTo("validation.selection-count-value"));
            Assert.That(malformed.Code, Is.EqualTo("validation.selection-count-parameter"));
        });
    }

    private static UiValidationRule ComparisonRule(string key, string operation)
    {
        return Rule(key, "comparison", Entry("field", Text("reference")), Entry("operator", Text(operation)));
    }

    private static UiField Field(string key, UiValue value, params UiValidationRule[] rules)
    {
        return new UiField { Key = key, Name = key, Value = value, Rules = rules };
    }

    private static UiValidationRule Rule(string key, string kind, params UiValueEntry[] parameters)
    {
        return new UiValidationRule { Key = key, Kind = kind, Message = key, Parameters = parameters };
    }

    private static UiValueEntry Entry(string key, UiValue value)
    {
        return new UiValueEntry { Key = key, Value = value };
    }

    private static UiValue Text(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }

    private static UiValue Integer(long value)
    {
        return new UiValue { Kind = UiValueKind.Integer, IntegerValue = value };
    }

    private static UiValue Decimal(decimal value)
    {
        return new UiValue { Kind = UiValueKind.Decimal, DecimalValue = value };
    }

    private static UiValue Binary(params byte[] values)
    {
        return new UiValue { Kind = UiValueKind.Binary, BinaryValue = values };
    }

    private static UiValue ArrayValue(params UiValue[] values)
    {
        return new UiValue { Kind = UiValueKind.Array, ArrayValue = values };
    }

    private static UiValue ObjectValue(params UiValueEntry[] values)
    {
        return new UiValue { Kind = UiValueKind.Object, ObjectValue = values };
    }
}
