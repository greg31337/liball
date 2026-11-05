using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class FormValidationCoreTests
{
    [Test]
    public void ValidatesRequiredAndUnicodeScalarLengthBoundaries()
    {
        FormCore forms = new FormCore();
        UiField field = Field("name", Text(string.Empty), UiInputKind.Text,
            Rule("required", "required", "A name is required"),
            Rule("length", "length", "Use two to four characters", Entry("minimum", Integer(2)), Entry("maximum", Integer(4))));
        forms.RegisterForm(new UiForm { Key = "profile", Fields = new UiField[] { field } });
        FormValidationCore validation = new FormValidationCore(forms);

        ValidationRuleResult missing = validation.EvaluateBuiltInRule("profile", "name", "required");
        ValidationRuleResult shortValue = validation.EvaluateBuiltInRule("profile", "name", "length");
        forms.UpdateFormFieldValue("profile", "name", Text("😀a"));
        ValidationRuleResult present = validation.EvaluateBuiltInRule("profile", "name", "required");
        ValidationRuleResult validLength = validation.EvaluateBuiltInRule("profile", "name", "length");
        forms.UpdateFormFieldValue("profile", "name", Text("😀abcd"));
        ValidationRuleResult longValue = validation.EvaluateBuiltInRule("profile", "name", "length");

        Assert.Multiple(() =>
        {
            Assert.That(missing.Success, Is.True);
            Assert.That(missing.IsValid, Is.False);
            Assert.That(missing.Issue!.Code, Is.EqualTo("validation.required"));
            Assert.That(missing.Issue.Message, Is.EqualTo("A name is required"));
            Assert.That(missing.Issue.ValueVersion, Is.EqualTo(0));
            Assert.That(shortValue.IsValid, Is.False);
            Assert.That(present.IsValid, Is.True);
            Assert.That(present.Issue, Is.Null);
            Assert.That(validLength.IsValid, Is.True);
            Assert.That(longValue.IsValid, Is.False);
            Assert.That(longValue.Issue!.Code, Is.EqualTo("validation.length"));
            Assert.That(longValue.Issue.ValueVersion, Is.EqualTo(2));
        });
    }

    [Test]
    public void ValidatesInclusiveNumericRangesAndExactSteps()
    {
        FormCore forms = new FormCore();
        UiField field = Field("score", Decimal(4.5m), UiInputKind.Number,
            Rule("range", "range", "Score must be in range", Entry("minimum", Integer(0)), Entry("maximum", Decimal(10m))),
            Rule("step", "step", "Score must use half steps", Entry("step", Decimal(0.5m)), Entry("minimum", Integer(0))));
        forms.RegisterForm(new UiForm { Key = "rating", Fields = new UiField[] { field } });
        FormValidationCore validation = new FormValidationCore(forms);

        ValidationRuleResult range = validation.EvaluateBuiltInRule("rating", "score", "range");
        ValidationRuleResult aligned = validation.EvaluateBuiltInRule("rating", "score", "step");
        forms.UpdateFormFieldValue("rating", "score", Decimal(4.6m));
        ValidationRuleResult misaligned = validation.EvaluateBuiltInRule("rating", "score", "step");
        forms.UpdateFormFieldValue("rating", "score", Integer(10));
        ValidationRuleResult upperBoundary = validation.EvaluateBuiltInRule("rating", "score", "range");
        forms.UpdateFormFieldValue("rating", "score", Integer(11));
        ValidationRuleResult outside = validation.EvaluateBuiltInRule("rating", "score", "range");
        UiField malformed = Field("malformed", Integer(5), UiInputKind.Number,
            Rule("inverted", "range", "Bad range", Entry("minimum", Integer(10)), Entry("maximum", Integer(1))));
        forms.RegisterFormField("rating", malformed);
        ValidationRuleResult inverted = validation.EvaluateBuiltInRule("rating", "malformed", "inverted");

        Assert.Multiple(() =>
        {
            Assert.That(range.IsValid, Is.True);
            Assert.That(aligned.IsValid, Is.True);
            Assert.That(misaligned.IsValid, Is.False);
            Assert.That(misaligned.Issue!.Code, Is.EqualTo("validation.step"));
            Assert.That(upperBoundary.IsValid, Is.True);
            Assert.That(outside.IsValid, Is.False);
            Assert.That(outside.Issue!.Code, Is.EqualTo("validation.range"));
            Assert.That(inverted.Success, Is.False);
            Assert.That(inverted.Code, Is.EqualTo("validation.range-parameter"));
        });
    }

    [Test]
    public void ValidatesPatternsComparisonsAndSelectionCounts()
    {
        FormCore forms = new FormCore();
        UiField password = Field("password", Text("AB12"), UiInputKind.Password,
            Rule("pattern", "pattern", "Use the required format", Entry("pattern", Text("[A-Z]{2}\\d{2}"))));
        UiField confirmation = Field("confirmation", Text("AB12"), UiInputKind.Password,
            Rule("matches", "comparison", "Values must match", Entry("field", Text("password")), Entry("operator", Text("equal"))));
        UiField tags = Field("tags", ArrayValue(Text("one"), Text("two")), UiInputKind.ReadOnly,
            Rule("count", "selection-count", "Choose one or two", Entry("minimum", Integer(1)), Entry("maximum", Integer(2))));
        forms.RegisterForm(new UiForm { Key = "account", Fields = new UiField[] { password, confirmation, tags } });
        FormValidationCore validation = new FormValidationCore(forms);

        ValidationRuleResult pattern = validation.EvaluateBuiltInRule("account", "password", "pattern");
        ValidationRuleResult comparison = validation.EvaluateBuiltInRule("account", "confirmation", "matches");
        ValidationRuleResult count = validation.EvaluateBuiltInRule("account", "tags", "count");
        forms.UpdateFormFieldValue("account", "confirmation", Text("different"));
        ValidationRuleResult mismatch = validation.EvaluateBuiltInRule("account", "confirmation", "matches");
        forms.UpdateFormFieldValue("account", "tags", ArrayValue(Text("one"), Text("two"), Text("three")));
        ValidationRuleResult excessive = validation.EvaluateBuiltInRule("account", "tags", "count");
        UiField malformed = Field("malformed", Text("value"), UiInputKind.Text,
            Rule("bad-pattern", "pattern", "Bad", Entry("pattern", Text("["))));
        forms.RegisterFormField("account", malformed);
        ValidationRuleResult rejected = validation.EvaluateBuiltInRule("account", "malformed", "bad-pattern");

        Assert.Multiple(() =>
        {
            Assert.That(pattern.IsValid, Is.True);
            Assert.That(comparison.IsValid, Is.True);
            Assert.That(count.IsValid, Is.True);
            Assert.That(mismatch.IsValid, Is.False);
            Assert.That(mismatch.Issue!.Code, Is.EqualTo("validation.comparison"));
            Assert.That(excessive.IsValid, Is.False);
            Assert.That(excessive.Issue!.Code, Is.EqualTo("validation.selection-count"));
            Assert.That(rejected.Success, Is.False);
            Assert.That(rejected.Code, Is.EqualTo("validation.pattern-parameter"));
        });
    }

    [Test]
    public void ProjectsEquivalentRulesIntoNativeInputConstraintsAndRendering()
    {
        FormCore forms = new FormCore();
        UiField text = Field("code", Text("AB12"), UiInputKind.Text,
            Rule("required", "required", "Required"),
            Rule("length", "length", "Length", Entry("minimum", Integer(2)), Entry("maximum", Integer(8))),
            Rule("pattern", "pattern", "Pattern", Entry("pattern", Text("[A-Z]{2}\\d{2}"))));
        UiField number = Field("quantity", Integer(5), UiInputKind.Number,
            Rule("range", "range", "Range", Entry("minimum", Integer(1)), Entry("maximum", Integer(9))),
            Rule("step", "step", "Step", Entry("step", Integer(2)), Entry("minimum", Integer(1))));
        forms.RegisterForm(new UiForm { Key = "order", Fields = new UiField[] { text, number } });
        FormValidationCore validation = new FormValidationCore(forms);

        FormOperationResult textConstraints = validation.ApplyNativeFormConstraints("order", "code");
        FormOperationResult numberConstraints = validation.ApplyNativeFormConstraints("order", "quantity");
        DomNode rendered = new SemanticExpander().ExpandNode(Node(textConstraints.Field!.Input!));

        Assert.Multiple(() =>
        {
            Assert.That(textConstraints.Success, Is.True);
            Assert.That(textConstraints.Field!.Input!.IsRequired, Is.True);
            Assert.That(textConstraints.Field.Input.HasMinimumLength, Is.True);
            Assert.That(textConstraints.Field.Input.MinimumLength, Is.EqualTo(2));
            Assert.That(textConstraints.Field.Input.HasMaximumLength, Is.True);
            Assert.That(textConstraints.Field.Input.MaximumLength, Is.EqualTo(8));
            Assert.That(textConstraints.Field.Input.Pattern, Is.EqualTo("[A-Z]{2}\\d{2}"));
            Assert.That(numberConstraints.Field!.Input!.Minimum!.IntegerValue, Is.EqualTo(1));
            Assert.That(numberConstraints.Field.Input.Maximum!.IntegerValue, Is.EqualTo(9));
            Assert.That(numberConstraints.Field.Input.Step!.IntegerValue, Is.EqualTo(2));
            Assert.That(Attribute(rendered, HtmlAttributes.MinimumLength), Is.EqualTo("2"));
            Assert.That(Attribute(rendered, HtmlAttributes.MaximumLength), Is.EqualTo("8"));
            Assert.That(Attribute(rendered, HtmlAttributes.Pattern), Is.EqualTo("[A-Z]{2}\\d{2}"));
            Assert.That(rendered.Attributes.Single(item => item.Name == HtmlAttributes.Required).IsBoolean, Is.True);
        });
    }

    private static UiField Field(string key, UiValue value, UiInputKind kind, params UiValidationRule[] rules)
    {
        return new UiField
        {
            Key = key,
            Name = key,
            Label = key,
            Value = value,
            Input = new UiInput { Kind = kind, Name = key, Value = value },
            Rules = rules
        };
    }

    private static UiValidationRule Rule(string key, string kind, string message, params UiValueEntry[] parameters)
    {
        return new UiValidationRule { Key = key, Kind = kind, Message = message, Parameters = parameters };
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

    private static UiValue ArrayValue(params UiValue[] values)
    {
        return new UiValue { Kind = UiValueKind.Array, ArrayValue = values };
    }

    private static UiNode Node(UiInput input)
    {
        return new UiNode
        {
            Kind = UiNodeKind.Control,
            Identity = new UiNodeIdentity { AuthorKey = "field", SiblingIdentity = "field", StableId = "field-id" },
            Input = input
        };
    }

    private static string Attribute(DomNode node, string name)
    {
        return node.Attributes.Single(item => item.Name == name).Value;
    }
}
