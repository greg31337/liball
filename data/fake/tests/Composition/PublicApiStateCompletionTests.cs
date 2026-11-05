using fake;
using System.Collections;

namespace fake.Tests;

[TestFixture]
public class PublicApiStateCompletionTests
{
    class CompletionTarget
    {
        public string Value = string.Empty;
    }

    [Test]
    public void PublicRootAndExceptionConstructorsPreserveSuppliedState()
    {
        FakeOptions options = new FakeOptions();
        options.Seed = 15119;
        options.Locale = "fr-CA";
        Fake value = new Fake(options);
        options.Locale = "de-DE";
        InvalidOperationException inner = new InvalidOperationException(
            "inner");
        FakeException exception = new FakeException("outer", inner);

        Assert.Multiple(() =>
        {
            Assert.That(value.OptionsSnapshot().Locale, Is.EqualTo("fr-CA"));
            Assert.That(value.OptionsSnapshot().Seed, Is.EqualTo(15119));
            Assert.That(exception.Message, Is.EqualTo("outer"));
            Assert.That(exception.InnerException, Is.SameAs(inner));
        });
    }

    [Test]
    public void ObjectChoicesAreDeterministicAndUseOnlySourcePositions()
    {
        object[] source = new object[] { "first", 2, "third" };
        FakeRandom first = Fake.Create(15120).Random();
        FakeRandom second = Fake.Create(15120).Random();

        object[] actual = first.ChoicesFromValuesAndCount(source, 12);
        object[] expected = second.ChoicesFromValuesAndCount(source, 12);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actual, Has.Length.EqualTo(12));
            int index = 0;
            while (index < actual.Length)
            {
                Assert.That(source, Does.Contain(actual[index]));
                index++;
            }
            Assert.That(
                () => first.ChoicesFromValuesAndCount(
                    Array.Empty<object>(),
                    1),
                Throws.TypeOf<ArgumentException>());
        });
    }

    [Test]
    public void RandomNamedSequenceResetRestoresOnlyTheRequestedSequence()
    {
        FakeRandom random = Fake.Create(15121).Random();
        SequenceState reset = random.SequenceFromName("random-reset");
        SequenceState untouched = random.SequenceFromName("random-untouched");

        Assert.That(reset.Next(), Is.EqualTo(0L));
        Assert.That(reset.Next(), Is.EqualTo(1L));
        Assert.That(untouched.Next(), Is.EqualTo(0L));

        random.ResetSequence("random-reset");

        Assert.Multiple(() =>
        {
            Assert.That(reset.Next(), Is.EqualTo(0L));
            Assert.That(untouched.Next(), Is.EqualTo(1L));
            Assert.That(
                () => random.ResetSequence(" "),
                Throws.TypeOf<ArgumentException>());
        });
    }

    [Test]
    public void ProfileResetAliasesRestoreSharedSequenceAndUniqueState()
    {
        ProfileProvider profile = Fake.Create(15122).Profile();
        SequenceState first = profile.Sequence("profile-first");
        SequenceState second = profile.Sequence("profile-second");
        UniqueScope unique = profile.Unique("profile-unique");

        Assert.That(first.Next(), Is.EqualTo(0L));
        Assert.That(first.Next(), Is.EqualTo(1L));
        Assert.That(second.Next(), Is.EqualTo(0L));
        Assert.That(unique.Next(() => "same"), Is.EqualTo("same"));

        profile.ResetSequenceFromNoArguments();
        profile.ResetUnique("profile-unique");

        Assert.Multiple(() =>
        {
            Assert.That(first.Next(), Is.EqualTo(0L));
            Assert.That(second.Next(), Is.EqualTo(0L));
            Assert.That(profile.Unique("profile-unique").Next(() => "same"),
                Is.EqualTo("same"));
        });
    }

    [Test]
    public void FakeAwareProfileFactoryReceivesTheOwnerAndHonorsRules()
    {
        Fake value = Fake.Create(15123);
        Fake supplied = null;
        FakeRuleSet rules = new FakeRuleSet();
        rules.Constant("Value", "populated");

        CompletionTarget result = (CompletionTarget)value.Profile()
            .CustomFromFactoryAndRulesAndCancellationTokenUsingFuncOfFakeAndObjectAndFakeRuleSetAndCancellationToken(
                generated =>
                {
                    supplied = generated;
                    return new CompletionTarget();
                },
                rules,
                CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(supplied, Is.SameAs(value));
            Assert.That(result.Value, Is.EqualTo("populated"));
        });
    }

    [Test]
    public void FakeAwareObjectFillerOperationsPopulateCreatedAndExistingValues()
    {
        Fake value = Fake.Create(15125);
        ObjectFiller filler = new ObjectFiller(value);
        FakeRuleSet rules = new FakeRuleSet();
        rules.Constant("Value", "filled");
        int ownerCalls = 0;
        Func<Fake, object> factory = owner =>
        {
            Assert.That(owner, Is.SameAs(value));
            ownerCalls++;
            return new CompletionTarget();
        };

        CompletionTarget created = (CompletionTarget)filler
            .CreateFromFactoryAndRulesAndCancellationTokenUsingFuncOfFakeAndObjectAndFakeRuleSetAndCancellationToken(
                factory,
                rules,
                CancellationToken.None);
        object[] many = filler
            .ManyFromFactoryAndRulesAndCountAndCancellationTokenUsingFuncOfFakeAndObjectAndFakeRuleSetAndInt32AndCancellationToken(
                factory,
                rules,
                2,
                CancellationToken.None);
        ArrayList existing = new ArrayList();
        existing.Add(new CompletionTarget());
        existing.Add(new CompletionTarget());
        IList returned = filler
            .FillFromValuesAndRulesAndCancellationTokenUsingIListAndFakeRuleSetAndCancellationToken(
                existing,
                rules,
                CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(created.Value, Is.EqualTo("filled"));
            Assert.That(many, Has.Length.EqualTo(2));
            Assert.That(((CompletionTarget)many[0]).Value,
                Is.EqualTo("filled"));
            Assert.That(((CompletionTarget)many[1]).Value,
                Is.EqualTo("filled"));
            Assert.That(ownerCalls, Is.EqualTo(3));
            Assert.That(returned, Is.SameAs(existing));
            Assert.That(((CompletionTarget)existing[0]).Value,
                Is.EqualTo("filled"));
            Assert.That(((CompletionTarget)existing[1]).Value,
                Is.EqualTo("filled"));
        });
    }

    [Test]
    public void SchemaEvaluateAndResetAliasesRestoreSharedState()
    {
        SchemaGenerator schema = Fake.Create(15124).Schema();
        FakeSchema sequence = schema.Schema(schema.Sequence("schema-reset"));
        FakeSchema unique = schema.Schema(
            schema.UniqueFromNameAndSourceAndRetryLimit(
                "schema-unique",
                schema.Constant("only"),
                1));

        Assert.That(schema.Evaluate(sequence), Is.EqualTo(0L));
        Assert.That(schema.Evaluate(sequence), Is.EqualTo(1L));
        Assert.That(schema.Evaluate(unique), Is.EqualTo("only"));

        schema.ResetSequenceFromNoArguments();
        schema.ResetUniqueFromNoArguments();

        Assert.Multiple(() =>
        {
            Assert.That(schema.Evaluate(sequence), Is.EqualTo(0L));
            Assert.That(schema.Evaluate(unique), Is.EqualTo("only"));
            Assert.That(
                () => schema.Evaluate((FakeSchema)null),
                Throws.TypeOf<ArgumentNullException>());
        });
    }
}
