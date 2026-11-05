using fake;

namespace fake.Tests;

[TestFixture]
public class RuleTests
{
    [Test]
    public void RulesRetainKindsAndDeterministicInsertionOrder()
    {
        FakeRuleSet rules = new FakeRuleSet();
        FakeRule constant = rules.Constant("Name", "Example");
        FakeRule generated = rules.Generate(
            "Identifier",
            value => value.Identifier().ShortId());
        FakeRule parameterless = rules.GenerateFromMemberPathAndGenerate(
            "Status",
            () => "ready");
        FakeRule skipped = rules.Skip("Existing");
        FakeRule derived = rules.Derived(
            "DisplayName",
            target => ((PersonProfile)target).FirstName + " " +
                ((PersonProfile)target).LastName);
        FakeRule duplicate = rules.Constant("Name", "Replacement");
        FakeRule[] ordered = rules.Rules();

        Assert.Multiple(() =>
        {
            Assert.That(rules.Count(), Is.EqualTo(6));
            Assert.That(ordered, Is.EqualTo(new FakeRule[]
            {
                constant,
                generated,
                parameterless,
                skipped,
                derived,
                duplicate
            }));
            Assert.That(ordered.Select(rule => rule.Order),
                Is.EqualTo(new int[] { 0, 1, 2, 3, 4, 5 }));
            Assert.That(ordered.Select(rule => rule.Kind), Is.EqualTo(
                new FakeRuleKind[]
                {
                    FakeRuleKind.Constant,
                    FakeRuleKind.Generated,
                    FakeRuleKind.Generated,
                    FakeRuleKind.Skipped,
                    FakeRuleKind.Derived,
                    FakeRuleKind.Constant
                }));
            Assert.That(ordered.Select(rule => rule.MemberPath),
                Is.EqualTo(new string[]
                {
                    "Name", "Identifier", "Status", "Existing",
                    "DisplayName", "Name"
                }));
        });
    }

    [Test]
    public void ConstantGeneratedAndDerivedRulesEvaluateTheirValues()
    {
        int calls = 0;
        FakeRuleSet rules = new FakeRuleSet();
        rules.Constant("Locale", "en-US");
        rules.Generate(
            "Generated",
            value => value.Identifier().ShortId());
        rules.GenerateFromMemberPathAndGenerate(
            "Callback",
            () =>
            {
                calls++;
                return "callback-" + calls;
            });
        rules.Derived(
            "FullName",
            target => ((PersonProfile)target).FirstName + " " +
                ((PersonProfile)target).LastName);
        FakeRule[] ordered = rules.Rules();
        PersonProfile target = new PersonProfile();
        target.FirstName = "Avery";
        target.LastName = "Example";
        Fake first = Fake.Create(13500);
        Fake second = Fake.Create(13500);

        object constant = ordered[0].Evaluate(first, target);
        object generated = ordered[1].Evaluate(first, target);
        object sameGenerated = ordered[1].Evaluate(second, target);
        object callback = ordered[2].Evaluate(first, target);
        object derived = ordered[3].Evaluate(first, target);

        Assert.Multiple(() =>
        {
            Assert.That(constant, Is.EqualTo("en-US"));
            Assert.That(generated, Is.EqualTo(sameGenerated));
            Assert.That(generated, Is.TypeOf<string>());
            Assert.That(callback, Is.EqualTo("callback-1"));
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(derived, Is.EqualTo("Avery Example"));
        });
    }

    [Test]
    public void NestedRulesFlattenPathsInSnapshotOrder()
    {
        FakeRuleSet coordinates = new FakeRuleSet();
        coordinates.Constant("Latitude", 12.5m);
        coordinates.Constant("Longitude", -45.25m);
        FakeRuleSet address = new FakeRuleSet();
        address.Constant("City", "Example City");
        address.Nested("Coordinate", coordinates);
        address.Skip("PostalCode");
        FakeRuleSet root = new FakeRuleSet();
        root.Constant("Name", "Example Person");

        FakeRule[] added = root.Nested("Address", address);
        address.Constant("Country", "Exampleland");
        FakeRule[] ordered = root.Rules();

        Assert.Multiple(() =>
        {
            Assert.That(added, Has.Length.EqualTo(4));
            Assert.That(root.Count(), Is.EqualTo(5));
            Assert.That(ordered.Select(rule => rule.MemberPath),
                Is.EqualTo(new string[]
                {
                    "Name",
                    "Address.City",
                    "Address.Coordinate.Latitude",
                    "Address.Coordinate.Longitude",
                    "Address.PostalCode"
                }));
            Assert.That(ordered.Select(rule => rule.Order),
                Is.EqualTo(new int[] { 0, 1, 2, 3, 4 }));
            Assert.That(ordered[1].ConstantValue,
                Is.EqualTo("Example City"));
            Assert.That(ordered[2].ConstantValue, Is.EqualTo(12.5m));
            Assert.That(ordered[4].Kind,
                Is.EqualTo(FakeRuleKind.Skipped));
        });

        ordered[0] = null;
        Assert.That(root.Rules()[0].MemberPath, Is.EqualTo("Name"));
    }

    [Test]
    public void InvalidPathsAndCallbacksDoNotChangeTheRuleSet()
    {
        FakeRuleSet rules = new FakeRuleSet();

        Assert.Multiple(() =>
        {
            Assert.That(
                Assert.Throws<ArgumentNullException>(
                    () => rules.Constant(null, 1)).ParamName,
                Is.EqualTo("memberPath"));
            Assert.That(() => rules.Constant("", 1),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => rules.Constant(" ", 1),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => rules.Constant(".Name", 1),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => rules.Constant("Name.", 1),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => rules.Constant("Name..First", 1),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => rules.Constant("Name First", 1),
                Throws.TypeOf<ArgumentException>());
            Assert.That(
                Assert.Throws<ArgumentNullException>(
                    () => rules.Generate(
                        "Name",
                        (Func<Fake, object>)null)).ParamName,
                Is.EqualTo("generate"));
            Assert.That(
                Assert.Throws<ArgumentNullException>(
                    () => rules.GenerateFromMemberPathAndGenerate(
                        "Name",
                        (Func<object>)null)).ParamName,
                Is.EqualTo("generate"));
            Assert.That(
                Assert.Throws<ArgumentNullException>(
                    () => rules.Derived(
                        "Name",
                        (Func<object, object>)null)).ParamName,
                Is.EqualTo("derive"));
            Assert.That(
                Assert.Throws<ArgumentNullException>(
                    () => rules.Nested("Address", null)).ParamName,
                Is.EqualTo("rules"));
            Assert.That(rules.Count(), Is.EqualTo(0));
            Assert.That(rules.Rules(), Is.Empty);
        });
    }

    [Test]
    public void RulesRejectEvaluationContextsThatCannotProduceValues()
    {
        FakeRuleSet rules = new FakeRuleSet();
        FakeRule generated = rules.Generate(
            "Value",
            value => value.Random().Int());
        FakeRule derived = rules.Derived("Other", target => target);
        FakeRule skipped = rules.Skip("Ignored");

        Assert.Multiple(() =>
        {
            Assert.That(
                Assert.Throws<ArgumentNullException>(
                    () => generated.Evaluate(null, new object())).ParamName,
                Is.EqualTo("value"));
            Assert.That(
                Assert.Throws<ArgumentNullException>(
                    () => derived.Evaluate(Fake.Create(1), null)).ParamName,
                Is.EqualTo("target"));
            Assert.That(() => skipped.Evaluate(Fake.Create(1), new object()),
                Throws.TypeOf<InvalidOperationException>());
        });
    }
}
