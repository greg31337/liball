using fake;

namespace fake.Tests;

[TestFixture]
public class ObjectFillerTests
{
    [Test]
    public void PopulateAppliesOrderedRulesToFieldsPropertiesAndNestedPaths()
    {
        FakeRuleSet nested = new FakeRuleSet();
        nested.Constant("City", "Example City");
        nested.Generate(
            "Code",
            value => value.Identifier().ShortId());
        FakeRuleSet rules = new FakeRuleSet();
        rules.Constant("FieldValue", "constant");
        rules.Constant("Number", 7);
        rules.Nested("NestedValue", nested);
        rules.Constant("ExistingNested.City", "Updated City");
        rules.Skip("SkippedValue");
        rules.Derived(
            "DisplayValue",
            target =>
            {
                PopulateTarget value = (PopulateTarget)target;
                return value.FieldValue + "|" + value.Number + "|" +
                    value.NestedValue.City + "|" +
                    value.NestedValue.Code;
            });
        PopulateTarget first = NewTarget();
        PopulateTarget second = NewTarget();
        ObjectFiller firstFiller = new ObjectFiller(Fake.Create(13600));
        ObjectFiller secondFiller = new ObjectFiller(Fake.Create(13600));

        object returned = firstFiller.Populate(first, rules);
        secondFiller.Populate(second, rules);

        Assert.Multiple(() =>
        {
            Assert.That(returned, Is.SameAs(first));
            Assert.That(first.FieldValue, Is.EqualTo("constant"));
            Assert.That(first.Number, Is.EqualTo(7));
            Assert.That(first.NestedValue, Is.Not.Null);
            Assert.That(first.NestedValue.City,
                Is.EqualTo("Example City"));
            Assert.That(first.NestedValue.Code,
                Is.EqualTo(second.NestedValue.Code));
            Assert.That(first.ExistingNested.City,
                Is.EqualTo("Updated City"));
            Assert.That(first.SkippedValue, Is.EqualTo("preserved"));
            Assert.That(first.DisplayValue, Is.EqualTo(
                "constant|7|Example City|" + first.NestedValue.Code));
            Assert.That(second.DisplayValue,
                Is.EqualTo(first.DisplayValue));
        });
    }

    [Test]
    public void NonStrictRulesSkipUnavailableMembersWithoutCallbacksOrMutation()
    {
        int calls = 0;
        FakeRuleSet rules = new FakeRuleSet();
        rules.GenerateFromMemberPathAndGenerate(
            "Missing",
            () =>
            {
                calls++;
                return "missing";
            });
        rules.GenerateFromMemberPathAndGenerate(
            "ReadOnlyValue",
            () =>
            {
                calls++;
                return "changed";
            });
        rules.GenerateFromMemberPathAndGenerate(
            "PrivateSetterValue",
            () =>
            {
                calls++;
                return "changed";
            });
        rules.Constant("NoDefault.Value", "changed");
        rules.Constant("WriteOnlyNested.City", "changed");
        rules.Constant("NestedValue.Missing", "changed");
        PopulateTarget target = NewTarget();

        new ObjectFiller(Fake.Create(13601)).Populate(target, rules);

        Assert.Multiple(() =>
        {
            Assert.That(rules.IsStrict(), Is.False);
            Assert.That(calls, Is.EqualTo(0));
            Assert.That(target.ReadOnlyValue, Is.EqualTo("read-only"));
            Assert.That(target.PrivateSetterValue,
                Is.EqualTo("private"));
            Assert.That(target.NoDefault, Is.Null);
            Assert.That(target.NestedValue, Is.Null);
            Assert.That(target.WriteOnlyNestedBacking, Is.Null);
        });
    }

    [Test]
    public void StrictRulesReportMissingAndReadOnlyPaths()
    {
        PopulateTarget target = NewTarget();

        FakeException missing = PopulateFailure(
            target,
            "Missing",
            "value");
        FakeException readOnly = PopulateFailure(
            target,
            "ReadOnlyValue",
            "value");
        FakeException privateSetter = PopulateFailure(
            target,
            "PrivateSetterValue",
            "value");
        FakeException noConstructor = PopulateFailure(
            target,
            "NoDefault.Value",
            "value");
        FakeException writeOnly = PopulateFailure(
            target,
            "WriteOnlyNested.City",
            "value");

        Assert.Multiple(() =>
        {
            Assert.That(missing.Message,
                Does.Contain("Member path 'Missing'")
                    .And.Contain("does not contain public instance member"));
            Assert.That(readOnly.Message,
                Does.Contain("Member path 'ReadOnlyValue'")
                    .And.Contain("read-only"));
            Assert.That(privateSetter.Message,
                Does.Contain("Member path 'PrivateSetterValue'")
                    .And.Contain("read-only"));
            Assert.That(noConstructor.Message,
                Does.Contain("Member path 'NoDefault.Value'")
                    .And.Contain("no public parameterless constructor"));
            Assert.That(writeOnly.Message,
                Does.Contain("Member path 'WriteOnlyNested.City'")
                    .And.Contain("cannot read intermediate member"));
        });
    }

    [Test]
    public void StrictModeCanBeEnabledAndDisabledDeterministically()
    {
        FakeRuleSet rules = new FakeRuleSet();

        FakeRuleSet enabled = rules.Strict();
        bool first = rules.IsStrict();
        FakeRuleSet disabled = rules.StrictFromEnabled(false);
        bool second = rules.IsStrict();
        rules.StrictFromEnabled(true);

        Assert.Multiple(() =>
        {
            Assert.That(enabled, Is.SameAs(rules));
            Assert.That(disabled, Is.SameAs(rules));
            Assert.That(first, Is.True);
            Assert.That(second, Is.False);
            Assert.That(rules.IsStrict(), Is.True);
        });
    }

    [Test]
    public void IncompatibleRuleValuesFailBeforeAssignment()
    {
        PopulateTarget target = NewTarget();
        target.Number = 19;
        FakeRuleSet wrongTypeRules = new FakeRuleSet();
        wrongTypeRules.Constant("Number", "19");
        FakeRuleSet nullRules = new FakeRuleSet();
        nullRules.Constant("Number", null);
        FakeRuleSet nullableReferenceRules = new FakeRuleSet();
        nullableReferenceRules.Constant("FieldValue", null);
        ObjectFiller filler = new ObjectFiller(Fake.Create(13602));

        FakeException wrongType = Assert.Throws<FakeException>(
            () => filler.Populate(target, wrongTypeRules));
        FakeException nullValue = Assert.Throws<FakeException>(
            () => filler.Populate(target, nullRules));
        filler.Populate(target, nullableReferenceRules);

        Assert.Multiple(() =>
        {
            Assert.That(wrongType.Message,
                Does.Contain("member path 'Number'")
                    .And.Contain("System.String")
                    .And.Contain("System.Int32"));
            Assert.That(nullValue.Message,
                Does.Contain("produced null")
                    .And.Contain("System.Int32"));
            Assert.That(target.Number, Is.EqualTo(19));
            Assert.That(target.FieldValue, Is.Null);
        });
    }

    [Test]
    public void PopulateValidatesItsArgumentsAndMutatedRulePaths()
    {
        Fake value = Fake.Create(13603);
        ObjectFiller filler = new ObjectFiller(value);
        FakeRuleSet rules = new FakeRuleSet();
        FakeRule rule = rules.Constant("FieldValue", "value");
        rule.MemberPath = "Bad..Path";

        Assert.Multiple(() =>
        {
            Assert.That(
                Assert.Throws<ArgumentNullException>(
                    () => new ObjectFiller(null)).ParamName,
                Is.EqualTo("value"));
            Assert.That(
                Assert.Throws<ArgumentNullException>(
                    () => filler.Populate(null, rules)).ParamName,
                Is.EqualTo("target"));
            Assert.That(
                Assert.Throws<ArgumentNullException>(
                    () => filler.Populate(NewTarget(), null)).ParamName,
                Is.EqualTo("rules"));
            Assert.That(() => filler.Populate(NewTarget(), rules),
                Throws.TypeOf<ArgumentException>());
        });
    }

    [Test]
    public void StrictDiagnosticsIdentifyNullReadOnlyStaticAndIndexedPaths()
    {
        DiagnosticTarget target = new DiagnosticTarget();

        FakeException readOnlyNull = PopulateFailure(
            target,
            "ReadOnlyNull.City",
            "value");
        FakeException staticMember = PopulateFailure(
            target,
            "StaticValue",
            "value");
        FakeException indexedMember = PopulateFailure(
            target,
            "Item",
            "value");

        Assert.Multiple(() =>
        {
            Assert.That(readOnlyNull.Message,
                Does.Contain("Member path 'ReadOnlyNull.City'")
                    .And.Contain(
                        "cannot initialize read-only intermediate member " +
                        "'ReadOnlyNull'"));
            Assert.That(staticMember.Message,
                Does.Contain("Member path 'StaticValue'")
                    .And.Contain("does not contain public instance member"));
            Assert.That(indexedMember.Message,
                Does.Contain("Member path 'Item'")
                    .And.Contain("does not contain public instance member"));
            Assert.That(target.ReadOnlyNull, Is.Null);
        });
    }

    static PopulateTarget NewTarget()
    {
        PopulateTarget result = new PopulateTarget();
        result.SkippedValue = "preserved";
        return result;
    }

    static FakeException PopulateFailure(
        object target,
        string memberPath,
        object value)
    {
        FakeRuleSet rules = new FakeRuleSet();
        rules.Strict();
        rules.Constant(memberPath, value);
        return Assert.Throws<FakeException>(
            () => new ObjectFiller(Fake.Create(13699)).Populate(target, rules));
    }
}

public class PopulateTarget
{
    public string FieldValue;

    public int Number { get; set; }

    public PopulateNestedTarget NestedValue;

    public PopulateNestedTarget ExistingNested { get; }

    public string SkippedValue;

    public string DisplayValue { get; set; }

    public string ReadOnlyValue { get; }

    public string PrivateSetterValue { get; private set; }

    public PopulateNoDefaultTarget NoDefault;

    public PopulateNestedTarget WriteOnlyNestedBacking;

    public PopulateNestedTarget WriteOnlyNested
    {
        set { WriteOnlyNestedBacking = value; }
    }

    public PopulateTarget()
    {
        ExistingNested = new PopulateNestedTarget();
        ReadOnlyValue = "read-only";
        PrivateSetterValue = "private";
    }
}

public class PopulateNestedTarget
{
    public string City;

    public string Code { get; set; }
}

public class PopulateNoDefaultTarget
{
    public string Value;

    public PopulateNoDefaultTarget(string value)
    {
        Value = value;
    }
}

public class DiagnosticTarget
{
    public static string StaticValue;

    public PopulateNestedTarget ReadOnlyNull { get; }

    public string this[int index]
    {
        get { return index.ToString(); }
        set { }
    }
}
