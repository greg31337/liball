using System;
using htmlgui;

namespace htmlgui.Tests;

public class InputTemplateExpanderTests
{
    [TestCase(UiInputKind.Text, HtmlInputTypes.Text, "text")]
    [TestCase(UiInputKind.Password, HtmlInputTypes.Password, "password")]
    [TestCase(UiInputKind.Search, HtmlInputTypes.Search, "search")]
    [TestCase(UiInputKind.Email, HtmlInputTypes.Email, "email")]
    [TestCase(UiInputKind.Url, HtmlInputTypes.Url, "url")]
    [TestCase(UiInputKind.Telephone, HtmlInputTypes.Telephone, "telephone")]
    [TestCase(UiInputKind.Number, HtmlInputTypes.Number, "number")]
    [TestCase(UiInputKind.Range, HtmlInputTypes.Range, "range")]
    [TestCase(UiInputKind.Date, HtmlInputTypes.Date, "date")]
    [TestCase(UiInputKind.Time, HtmlInputTypes.Time, "time")]
    [TestCase(UiInputKind.Month, HtmlInputTypes.Month, "month")]
    [TestCase(UiInputKind.Week, HtmlInputTypes.Week, "week")]
    [TestCase(UiInputKind.LocalDateTime, HtmlInputTypes.LocalDateTime, "local-date-time")]
    [TestCase(UiInputKind.Color, HtmlInputTypes.Color, "color")]
    [TestCase(UiInputKind.Hidden, HtmlInputTypes.Hidden, "hidden")]
    public void MapsScalarInputKindsToNativeInputTypes(UiInputKind kind, string type, string kindToken)
    {
        UiNode node = InputControl(kind, "field");
        node.State.Version = 8;

        DomNode result = new SemanticExpander().ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(result.Kind, Is.EqualTo(DomNodeKind.Element));
            Assert.That(result.Key, Is.EqualTo("input"));
            Assert.That(result.Id, Is.EqualTo("input-id"));
            Assert.That(result.Name, Is.EqualTo(HtmlElements.Input));
            Assert.That(AttributeValue(result, HtmlAttributes.Class), Is.EqualTo("htmlgui-input htmlgui-input-" + kindToken));
            Assert.That(AttributeValue(result, HtmlAttributes.Name), Is.EqualTo("field"));
            Assert.That(AttributeValue(result, HtmlAttributes.Type), Is.EqualTo(type));
            Assert.That(AttributeValue(result, HtmlAttributes.Value), Is.Empty);
            Assert.That(result.Properties, Has.Length.EqualTo(1));
            Assert.That(result.Properties[0].Name, Is.EqualTo(HtmlAttributes.Value));
            Assert.That(result.Properties[0].Value.Kind, Is.EqualTo(UiValueKind.String));
            Assert.That(result.Properties[0].Value.StringValue, Is.Empty);
            Assert.That(result.Children, Is.Empty);
            Assert.That(result.Version, Is.EqualTo(8));
        });
    }

    [Test]
    public void FormatsNumericTemporalAndHiddenScalarValuesInvariantly()
    {
        UiNode integer = InputControl(UiInputKind.Number, "integer");
        integer.Input!.Value = new UiValue { Kind = UiValueKind.Integer, IntegerValue = -42 };
        UiNode floating = InputControl(UiInputKind.Range, "floating");
        floating.Input!.Value = new UiValue { Kind = UiValueKind.FloatingPoint, FloatingPointValue = 1.25 };
        UiNode decimalValue = InputControl(UiInputKind.Number, "decimal");
        decimalValue.Input!.Value = new UiValue { Kind = UiValueKind.Decimal, DecimalValue = 9.75m };
        UiNode temporal = InputControl(UiInputKind.LocalDateTime, "moment");
        temporal.Input!.Value = new UiValue { Kind = UiValueKind.DateTime, DateTimeValue = "2026-09-14T12:30" };
        UiNode hidden = InputControl(UiInputKind.Hidden, "flag");
        hidden.Input!.Value = new UiValue { Kind = UiValueKind.Boolean, BooleanValue = true };
        UiNode binary = InputControl(UiInputKind.Hidden, "payload");
        binary.Input!.Value = new UiValue { Kind = UiValueKind.Binary, BinaryValue = new byte[] { 1, 2, 3 } };
        SemanticExpander expander = new SemanticExpander();

        DomNode integerResult = expander.ExpandNode(integer);
        DomNode floatingResult = expander.ExpandNode(floating);
        DomNode decimalResult = expander.ExpandNode(decimalValue);
        DomNode temporalResult = expander.ExpandNode(temporal);
        DomNode hiddenResult = expander.ExpandNode(hidden);
        DomNode binaryResult = expander.ExpandNode(binary);

        Assert.Multiple(() =>
        {
            AssertValue(integerResult, "-42");
            AssertValue(floatingResult, "1.25");
            AssertValue(decimalResult, "9.75");
            AssertValue(temporalResult, "2026-09-14T12:30");
            AssertValue(hiddenResult, "true");
            AssertValue(binaryResult, "AQID");
        });
    }

    [Test]
    public void ProjectsEveryPortableScalarAsMatchingInitialAndLiveValues()
    {
        UiValue[] values = new UiValue[]
        {
            new UiValue { Kind = UiValueKind.Null },
            new UiValue { Kind = UiValueKind.Boolean, BooleanValue = false },
            new UiValue { Kind = UiValueKind.Integer, IntegerValue = 123 },
            new UiValue { Kind = UiValueKind.FloatingPoint, FloatingPointValue = -4.5 },
            new UiValue { Kind = UiValueKind.Decimal, DecimalValue = 6.75m },
            new UiValue { Kind = UiValueKind.String, StringValue = "text" },
            new UiValue { Kind = UiValueKind.DateTime, DateTimeValue = "2026-09-14T09:30:00Z" },
            new UiValue { Kind = UiValueKind.Binary, BinaryValue = new byte[] { 0, 255 } }
        };
        string[] expected = new string[] { string.Empty, "false", "123", "-4.5", "6.75", "text", "2026-09-14T09:30:00Z", "AP8=" };
        SemanticExpander expander = new SemanticExpander();
        int index = 0;

        while (index < values.Length)
        {
            UiNode node = InputControl(UiInputKind.Hidden, "scalar-" + index);
            node.Input!.Value = values[index];

            DomNode result = expander.ExpandNode(node);

            AssertValue(result, expected[index]);
            index++;
        }
    }

    [Test]
    public void MapsMultilineInputToTextAreaWithNormalizedInitialAndLiveValues()
    {
        UiNode node = InputControl(UiInputKind.Multiline, "notes");
        node.Input!.Value = new UiValue { Kind = UiValueKind.String, StringValue = "first\r\nsecond\rthird" };
        node.State.Version = 3;
        SemanticExpander expander = new SemanticExpander();

        DomNode first = expander.ExpandNode(node);
        DomNode second = expander.ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(first.Name, Is.EqualTo(HtmlElements.TextArea));
            Assert.That(AttributeValue(first, HtmlAttributes.Class), Is.EqualTo("htmlgui-input htmlgui-input-multiline"));
            Assert.That(AttributeValue(first, HtmlAttributes.Name), Is.EqualTo("notes"));
            Assert.That(HasAttribute(first, HtmlAttributes.Type), Is.False);
            Assert.That(HasAttribute(first, HtmlAttributes.Value), Is.False);
            Assert.That(first.Properties[0].Value.StringValue, Is.EqualTo("first\nsecond\nthird"));
            Assert.That(first.Children, Has.Length.EqualTo(1));
            Assert.That(first.Children[0].Kind, Is.EqualTo(DomNodeKind.Text));
            Assert.That(first.Children[0].Text, Is.EqualTo("first\nsecond\nthird"));
            Assert.That(first.Children[0].Version, Is.EqualTo(3));
            Assert.That(first.Children[0].Id, Is.EqualTo(second.Children[0].Id));
        });
    }

    [Test]
    public void MapsReadOnlyInputToOutputWithTextAndLiveValue()
    {
        UiNode node = InputControl(UiInputKind.ReadOnly, "total");
        node.Input!.Value = new UiValue { Kind = UiValueKind.Integer, IntegerValue = 1250 };
        node.Input.IsDisabled = true;

        DomNode result = new SemanticExpander().ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo(HtmlElements.Output));
            Assert.That(AttributeValue(result, HtmlAttributes.Class), Is.EqualTo("htmlgui-input htmlgui-input-read-only"));
            Assert.That(AttributeValue(result, HtmlAttributes.Name), Is.EqualTo("total"));
            Assert.That(AttributeValue(result, AriaAttributes.Disabled), Is.EqualTo("true"));
            Assert.That(HasAttribute(result, HtmlAttributes.Disabled), Is.False);
            Assert.That(HasAttribute(result, HtmlAttributes.Type), Is.False);
            Assert.That(HasAttribute(result, HtmlAttributes.Value), Is.False);
            Assert.That(result.Properties[0].Value.StringValue, Is.EqualTo("1250"));
            Assert.That(result.Children[0].Text, Is.EqualTo("1250"));
        });
    }

    [Test]
    public void ProjectsDisabledVisibilityBusyAndTabIndexState()
    {
        UiNode inputDisabled = InputControl(UiInputKind.Text, "first");
        inputDisabled.Input!.IsDisabled = true;
        UiNode nodeDisabled = InputControl(UiInputKind.Email, "second");
        nodeDisabled.State.IsEnabled = false;
        nodeDisabled.State.IsVisible = false;
        nodeDisabled.State.IsBusy = true;
        nodeDisabled.State.HasTabIndex = true;
        nodeDisabled.State.TabIndex = -1;
        SemanticExpander expander = new SemanticExpander();

        DomNode inputResult = expander.ExpandNode(inputDisabled);
        DomNode nodeResult = expander.ExpandNode(nodeDisabled);

        Assert.Multiple(() =>
        {
            Assert.That(FindAttribute(inputResult, HtmlAttributes.Disabled).IsBoolean, Is.True);
            Assert.That(FindAttribute(nodeResult, HtmlAttributes.Disabled).IsBoolean, Is.True);
            Assert.That(FindAttribute(nodeResult, HtmlAttributes.Hidden).IsBoolean, Is.True);
            Assert.That(AttributeValue(nodeResult, AriaAttributes.Busy), Is.EqualTo("true"));
            Assert.That(AttributeValue(nodeResult, HtmlAttributes.TabIndex), Is.EqualTo("-1"));
        });
    }

    [Test]
    public void FileInputsNeverProjectAValue()
    {
        UiNode nullValue = InputControl(UiInputKind.File, "upload");
        UiNode emptyValue = InputControl(UiInputKind.File, "upload-empty");
        emptyValue.Input!.Value = new UiValue { Kind = UiValueKind.String, StringValue = string.Empty };
        SemanticExpander expander = new SemanticExpander();

        DomNode nullResult = expander.ExpandNode(nullValue);
        DomNode emptyResult = expander.ExpandNode(emptyValue);

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(nullResult, HtmlAttributes.Type), Is.EqualTo(HtmlInputTypes.File));
            Assert.That(HasAttribute(nullResult, HtmlAttributes.Value), Is.False);
            Assert.That(nullResult.Properties, Is.Empty);
            Assert.That(HasAttribute(emptyResult, HtmlAttributes.Value), Is.False);
            Assert.That(emptyResult.Properties, Is.Empty);
        });
    }

    [Test]
    public void RejectsMissingPayloadNameValueIncompatibleValuesAndUndefinedKinds()
    {
        UiNode missingPayload = new UiNode
        {
            Kind = UiNodeKind.Control,
            Name = "input",
            Identity = new UiNodeIdentity { AuthorKey = "missing", StableId = "missing-id" }
        };
        UiNode missingName = InputControl(UiInputKind.Text, " ");
        UiNode missingValue = InputControl(UiInputKind.Text, "missing-value");
        missingValue.Input!.Value = null!;
        UiNode wrongText = InputControl(UiInputKind.Text, "text");
        wrongText.Input!.Value = new UiValue { Kind = UiValueKind.Boolean, BooleanValue = true };
        UiNode wrongNumber = InputControl(UiInputKind.Number, "number");
        wrongNumber.Input!.Value = new UiValue { Kind = UiValueKind.String, StringValue = "12" };
        UiNode wrongTemporal = InputControl(UiInputKind.Date, "date");
        wrongTemporal.Input!.Value = new UiValue { Kind = UiValueKind.Integer, IntegerValue = 1 };
        UiNode aggregate = InputControl(UiInputKind.Hidden, "aggregate");
        aggregate.Input!.Value = new UiValue { Kind = UiValueKind.Array, ArrayValue = Array.Empty<UiValue>() };
        UiNode populatedFile = InputControl(UiInputKind.File, "file");
        populatedFile.Input!.Value = new UiValue { Kind = UiValueKind.String, StringValue = "/private/file.txt" };
        UiNode undefined = InputControl((UiInputKind)999, "undefined");
        SemanticExpander expander = new SemanticExpander();

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<UiRenderException>(() => expander.ExpandNode(missingPayload))!.NodeId, Is.EqualTo("missing-id"));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(missingName));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(missingValue));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(wrongText));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(wrongNumber));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(wrongTemporal));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(aggregate));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(populatedFile));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => expander.ExpandNode(undefined))!.ParamName, Is.EqualTo("kind"));
        });
    }

    [Test]
    public void RepeatedExpansionIsFreshStableAndDoesNotMutateInputModels()
    {
        UiNode node = InputControl(UiInputKind.Multiline, "notes");
        node.Input!.Value = new UiValue { Kind = UiValueKind.String, StringValue = "original\r\ntext" };
        UiInput sourceInput = node.Input;
        UiValue sourceValue = sourceInput.Value;
        SemanticExpander expander = new SemanticExpander();

        DomNode first = expander.ExpandNode(node);
        DomNode second = expander.ExpandNode(node);
        first.Properties[0].Value.StringValue = "changed";
        first.Children[0].Text = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.Attributes, Is.Not.SameAs(second.Attributes));
            Assert.That(first.Properties, Is.Not.SameAs(second.Properties));
            Assert.That(first.Properties[0], Is.Not.SameAs(second.Properties[0]));
            Assert.That(first.Properties[0].Value, Is.Not.SameAs(second.Properties[0].Value));
            Assert.That(first.Children, Is.Not.SameAs(second.Children));
            Assert.That(first.Children[0], Is.Not.SameAs(second.Children[0]));
            Assert.That(first.Children[0].Id, Is.EqualTo(second.Children[0].Id));
            Assert.That(sourceInput.Name, Is.EqualTo("notes"));
            Assert.That(sourceValue.StringValue, Is.EqualTo("original\r\ntext"));
            Assert.That(second.Properties[0].Value.StringValue, Is.EqualTo("original\ntext"));
            Assert.That(second.Children[0].Text, Is.EqualTo("original\ntext"));
        });
    }

    private static UiNode InputControl(UiInputKind kind, string name)
    {
        return new UiNode
        {
            Kind = UiNodeKind.Control,
            Identity = new UiNodeIdentity { AuthorKey = "input", SiblingIdentity = "input", StableId = "input-id" },
            Input = new UiInput { Kind = kind, Name = name }
        };
    }

    private static void AssertValue(DomNode node, string expected)
    {
        Assert.That(AttributeValue(node, HtmlAttributes.Value), Is.EqualTo(expected));
        Assert.That(node.Properties[0].Value.StringValue, Is.EqualTo(expected));
    }

    private static bool HasAttribute(DomNode node, string name)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            if (node.Attributes[index].Name == name)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    private static DomAttribute FindAttribute(DomNode node, string name)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            if (node.Attributes[index].Name == name)
            {
                return node.Attributes[index];
            }

            index++;
        }

        throw new AssertionException("Attribute was not found: " + name);
    }

    private static string AttributeValue(DomNode node, string name)
    {
        return FindAttribute(node, name).Value;
    }
}
