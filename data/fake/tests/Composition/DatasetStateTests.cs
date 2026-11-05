using fake;

namespace fake.Tests;

[TestFixture]
public class DatasetStateTests
{
    public class CustomProfile
    {
        public string Code;
    }

    [Test]
    public void NamedSequenceIsSharedAcrossRandomSchemaAndProfileGeneration()
    {
        Fake fake = Fake.Create(14000);
        FakeRandom random = fake.Random();
        SchemaGenerator schema = fake.Schema();
        ProfileProvider profile = fake.Profile();
        SequenceState randomState = random.SequenceFromNameAndStartAndIncrement("orders", 100L, 5L);
        SequenceState profileState = profile.SequenceFromNameAndStartAndIncrement(
            "orders",
            100L,
            5L);
        SchemaNode schemaValue = schema.SequenceFromNameAndStartAndIncrement("orders", 100L, 5L);

        long first = randomState.Next();
        object second = schema.GenerateFromRoot(schemaValue);
        long third = profileState.Next();
        schema.ResetSequence("orders");
        long reset = random.SequenceFromNameAndStartAndIncrement("orders", 100L, 5L).Next();

        Assert.Multiple(() =>
        {
            Assert.That(profileState, Is.SameAs(randomState));
            Assert.That(first, Is.EqualTo(100L));
            Assert.That(second, Is.EqualTo(105L));
            Assert.That(third, Is.EqualTo(110L));
            Assert.That(reset, Is.EqualTo(100L));
            Assert.That(
                () => profile.SequenceFromNameAndStartAndIncrement("orders", 0L, 1L),
                Throws.TypeOf<FakeException>()
                    .With.Message.Contains("incompatible"));
        });
    }

    [Test]
    public void NamedSequenceScopesAreIsolatedResettableAndDeterministic()
    {
        Fake first = Fake.Create(14001);
        Fake second = Fake.Create(14001);
        SequenceState firstAlpha = first.Random().SequenceFromName("alpha");
        SequenceState firstBeta = first.Profile().SequenceFromNameAndStartAndIncrement("beta", 10L, 2L);
        SequenceState secondAlpha = second.Random().SequenceFromName("alpha");
        SequenceState secondBeta = second.Profile().SequenceFromNameAndStartAndIncrement(
            "beta",
            10L,
            2L);

        Assert.That(firstAlpha.Repeat(3),
            Is.EqualTo(secondAlpha.Repeat(3)));
        Assert.That(firstBeta.Repeat(2),
            Is.EqualTo(secondBeta.Repeat(2)));

        first.Profile().ResetSequence("alpha");
        Assert.That(firstAlpha.Next(), Is.EqualTo(0L));
        Assert.That(firstBeta.Next(), Is.EqualTo(14L));

        first.Random().ResetSequenceFromNoArguments();
        Assert.That(firstAlpha.Next(), Is.EqualTo(0L));
        Assert.That(firstBeta.Next(), Is.EqualTo(10L));
        Assert.That(secondAlpha.Next(), Is.EqualTo(3L));
        Assert.That(secondBeta.Next(), Is.EqualTo(14L));
    }

    [Test]
    public void ProfileGenerationAndSchemaShareSimpleUniqueness()
    {
        Fake fake = Fake.Create(14002);
        FakeRuleSet rules = new FakeRuleSet();
        rules.Generate(
            "Code",
            value => value.Random().UniqueFromNameAndGenerate("profile-codes", () => "shared"));

        CustomProfile profile = (CustomProfile)fake.Profile().Custom(
            () => new CustomProfile(),
            rules);
        SchemaGenerator schema = fake.Schema();
        SchemaNode unique = schema.UniqueFromNameAndSourceAndRetryLimit(
            "profile-codes",
            schema.Constant("shared"),
            2);

        Assert.Multiple(() =>
        {
            Assert.That(profile.Code, Is.EqualTo("shared"));
            Assert.That(fake.Profile().Unique("profile-codes").Count(),
                Is.EqualTo(1));
            Assert.That(() => schema.GenerateFromRoot(unique),
                Throws.TypeOf<FakeException>()
                    .With.Message.Contains("2 attempts"));
        });

        schema.ResetUnique("profile-codes");

        Assert.That(schema.GenerateFromRoot(unique), Is.EqualTo("shared"));
        Assert.That(fake.Random().Unique("profile-codes").Count(),
            Is.EqualTo(1));
    }

    [Test]
    public void CompositeSchemaUniquenessSharesOrderedKeysWithProfiles()
    {
        Fake fake = Fake.Create(14003);
        object accepted = new string[] { "north", "1" };
        fake.Profile().UniqueFromNameAndGenerateAndCompositeKey(
            "locations",
            () => accepted,
            value => (string[])value);
        object duplicate = new string[] { "north", "1" };
        object fresh = new string[] { "north", "2" };
        object[] candidates = new object[] { duplicate, fresh };
        int calls = 0;
        SchemaGenerator schema = fake.Schema();
        SchemaNode source = schema.ScalarFromGenerate(
            () =>
            {
                object value = candidates[calls];
                calls++;
                return value;
            });
        SchemaNode unique = schema.UniqueFromNameAndSourceAndCompositeKeyAndRetryLimit(
            "locations",
            source,
            value => (string[])value,
            3);

        object result = schema.GenerateFromRoot(unique);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.SameAs(fresh));
            Assert.That(calls, Is.EqualTo(2));
            Assert.That(fake.Random().Unique("locations").Count(),
                Is.EqualTo(2));
            Assert.That(unique.UniqueKeySelector,
                Is.Not.Null);
            Assert.That(unique.RetryLimit, Is.EqualTo(3));
        });
    }

    [Test]
    public void NamedResetsAffectOnlyTheirScopeAndAllResetsClearEveryScope()
    {
        Fake fake = Fake.Create(14004);
        UniqueScope first = fake.Profile().Unique("first");
        UniqueScope second = fake.Random().Unique("second");
        first.Next(() => "a");
        second.Next(() => "b");

        fake.Schema().ResetUnique("first");

        Assert.That(first.Count(), Is.Zero);
        Assert.That(second.Count(), Is.EqualTo(1));

        fake.Profile().ResetUniqueFromNoArguments();

        Assert.That(first.Count(), Is.Zero);
        Assert.That(second.Count(), Is.Zero);
    }

    [Test]
    public void DatasetScopeArgumentsAndCompositeSelectorsAreValidated()
    {
        Fake fake = Fake.Create(14005);
        SchemaGenerator schema = fake.Schema();
        SchemaNode source = schema.Constant("value");

        Assert.Multiple(() =>
        {
            Assert.That(() => fake.Random().SequenceFromName((string)null),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => fake.Profile().Sequence(" "),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => fake.Random().SequenceFromNameAndStartAndIncrement("bad", 0L, 0L),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => fake.Schema().ResetSequence(null),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => schema.UniqueFromNameAndSourceAndCompositeKey(
                    "composite",
                    source,
                    (Func<object, string[]>)null),
                Throws.TypeOf<ArgumentNullException>());
        });

        SchemaNode invalidKey = schema.UniqueFromNameAndSourceAndCompositeKey(
            "composite",
            source,
            value => null);

        Assert.That(() => schema.GenerateFromRoot(invalidKey),
            Throws.TypeOf<FakeException>()
                .With.Message.Contains("composite key callback returned null"));
    }
}
