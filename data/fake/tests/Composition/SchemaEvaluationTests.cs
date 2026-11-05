using fake;

namespace fake.Tests;

[TestFixture]
public class SchemaEvaluationTests
{
    public class ReferenceRecord
    {
        public string Code;
    }

    [Test]
    public void GenerateEvaluatesObjectsWithCorrelationAndReferencesInOrder()
    {
        Fake fake = Fake.Create(13900);
        SchemaGenerator schema = fake.Schema();
        Fake observed = null;
        ReferenceRecord record = new ReferenceRecord();
        record.Code = "record-code";
        SchemaNode nested = schema.Object(
            new SchemaField[]
            {
                schema.Field("local", schema.Constant("inside")),
                schema.Field("localCopy", schema.Reference("local")),
                schema.Field("rootCopy", schema.Reference("identifier"))
            });
        SchemaNode root = schema.Object(
            new SchemaField[]
            {
                schema.Field(
                    "identifier",
                    schema.Scalar(
                        value =>
                        {
                            observed = value;
                            return "id-139";
                        })),
                schema.Field("copy", schema.Reference("identifier")),
                schema.Field(
                    "derived",
                    schema.Derived(
                        context =>
                            ((Dictionary<string, object>)context)["identifier"] +
                            "-derived")),
                schema.Field(
                    "conditional",
                    schema.Condition(
                        context =>
                            ((Dictionary<string, object>)context).ContainsKey(
                                "copy"),
                        schema.Constant("ready"),
                        schema.Constant("missing"))),
                schema.Field("nested", nested),
                schema.Field("record", schema.Constant(record)),
                schema.Field("recordCode", schema.Reference("record.Code"))
            });

        Dictionary<string, object> result =
            (Dictionary<string, object>)schema.Generate(schema.Schema(root));
        Dictionary<string, object> nestedResult =
            (Dictionary<string, object>)result["nested"];

        Assert.Multiple(() =>
        {
            Assert.That(observed, Is.SameAs(fake));
            Assert.That(result.Keys, Is.EqualTo(
                new string[]
                {
                    "identifier",
                    "copy",
                    "derived",
                    "conditional",
                    "nested",
                    "record",
                    "recordCode"
                }));
            Assert.That(result["identifier"], Is.EqualTo("id-139"));
            Assert.That(result["copy"], Is.EqualTo("id-139"));
            Assert.That(result["derived"], Is.EqualTo("id-139-derived"));
            Assert.That(result["conditional"], Is.EqualTo("ready"));
            Assert.That(nestedResult["localCopy"], Is.EqualTo("inside"));
            Assert.That(nestedResult["rootCopy"], Is.EqualTo("id-139"));
            Assert.That(result["recordCode"], Is.EqualTo("record-code"));
        });
    }

    [Test]
    public void SeededArrayAndVariantEvaluationIsReproducible()
    {
        Fake first = Fake.Create(13901);
        Fake second = Fake.Create(13901);
        SchemaGenerator firstSchema = first.Schema();
        SchemaGenerator secondSchema = second.Schema();
        SchemaNode firstVariant = firstSchema.Variant(
            new SchemaVariant[]
            {
                firstSchema.Choice(firstSchema.Constant("low"), 1.0),
                firstSchema.Choice(firstSchema.Constant("high"), 3.0)
            });
        SchemaNode secondVariant = secondSchema.Variant(
            new SchemaVariant[]
            {
                secondSchema.Choice(secondSchema.Constant("low"), 1.0),
                secondSchema.Choice(secondSchema.Constant("high"), 3.0)
            });
        SchemaNode firstRoot = firstSchema.ArrayFromItemAndMinimumCountAndMaximumCount(firstVariant, 3, 8);
        SchemaNode secondRoot = secondSchema.ArrayFromItemAndMinimumCountAndMaximumCount(secondVariant, 3, 8);

        object[] firstResult = (object[])firstSchema.EvaluateFromRoot(firstRoot);
        object[] secondResult = (object[])secondSchema.EvaluateFromRoot(secondRoot);

        Assert.Multiple(() =>
        {
            Assert.That(firstResult.Length, Is.InRange(3, 8));
            Assert.That(firstResult, Is.EqualTo(secondResult));
            Assert.That(firstResult,
                Has.All.Matches<object>(value =>
                    (string)value == "low" || (string)value == "high"));
        });
    }

    [Test]
    public void ValidationDoesNotConsumeEntropyAndAcceptsSharedSubtrees()
    {
        Fake inspected = Fake.Create(13902);
        Fake untouched = Fake.Create(13902);
        SchemaGenerator schema = inspected.Schema();
        SchemaNode shared = schema.Constant("shared");
        SchemaNode root = schema.Object(
            new SchemaField[]
            {
                schema.Field("first", shared),
                schema.Field("second", shared)
            });

        schema.ValidateFromRoot(root);

        Assert.That(inspected.Schema().RandomValue.Long(),
            Is.EqualTo(untouched.Schema().RandomValue.Long()));
    }

    [Test]
    public void ValidationRejectsCyclesDepthNodeLimitsAndMutableInvalidNodes()
    {
        SchemaGenerator schema = Fake.Create(13903).Schema();
        SchemaNode cycle = schema.Array(schema.Constant("item"), 1);
        cycle.Item = cycle;
        SchemaNode deep = schema.Constant("end");
        int depth = 0;
        while (depth < 130)
        {
            deep = schema.Array(deep, 0);
            depth++;
        }
        SchemaNode duplicate = schema.Object(
            new SchemaField[]
            {
                schema.Field("same", schema.Constant(1)),
                schema.Field("same", schema.Constant(2))
            });
        SchemaNode badReference = schema.Reference("valid");
        badReference.ReferencePath = "bad..path";
        SchemaNode badKind = schema.Constant("value");
        badKind.Kind = (SchemaNodeKind)999;

        FakeOptions limitedOptions = FakeOptions.Create(13904);
        limitedOptions.MaximumCount = 2;
        SchemaGenerator limited = Fake.Create(limitedOptions).Schema();
        SchemaNode overNodes = limited.Array(
            limited.Array(limited.Constant("value"), 0),
            0);

        Assert.Multiple(() =>
        {
            Assert.That(() => schema.ValidateFromRoot(cycle),
                Throws.TypeOf<FakeException>()
                    .With.Message.Contains("cycle"));
            Assert.That(() => schema.ValidateFromRoot(deep),
                Throws.TypeOf<FakeException>()
                    .With.Message.Contains("depth"));
            Assert.That(() => schema.ValidateFromRoot(duplicate),
                Throws.TypeOf<FakeException>()
                    .With.Message.Contains("duplicates"));
            Assert.That(() => schema.ValidateFromRoot(badReference),
                Throws.TypeOf<FakeException>()
                    .With.Message.Contains("reference path"));
            Assert.That(() => schema.ValidateFromRoot(badKind),
                Throws.TypeOf<FakeException>()
                    .With.Message.Contains("unsupported"));
            Assert.That(() => limited.ValidateFromRoot(overNodes),
                Throws.TypeOf<FakeException>()
                    .With.Message.Contains("node limit"));
        });
    }

    [Test]
    public void ForwardAndMissingReferencesFailBeforeLaterFieldsCanSatisfyThem()
    {
        SchemaGenerator schema = Fake.Create(13905).Schema();
        SchemaNode root = schema.Object(
            new SchemaField[]
            {
                schema.Field("copy", schema.Reference("later")),
                schema.Field("later", schema.Constant("value"))
            });

        Assert.That(() => schema.GenerateFromRoot(root),
            Throws.TypeOf<FakeException>()
                .With.Message.Contains("later")
                .And.Message.Contains("previously evaluated"));
    }

    [Test]
    public void SequenceAndUniqueNodesMaintainDeterministicGeneratorState()
    {
        SchemaGenerator schema = Fake.Create(13906).Schema();
        SchemaNode sequence = schema.Array(
            schema.SequenceFromNameAndStartAndIncrement("orders", 10L, 2L),
            3);

        object[] firstSequence = (object[])schema.GenerateFromRoot(sequence);
        object[] secondSequence = (object[])schema.GenerateFromRoot(sequence);

        int calls = 0;
        SchemaNode unique = schema.UniqueFromNameAndSourceAndRetryLimit(
            "codes",
            schema.ScalarFromGenerate(
                () =>
                {
                    calls++;
                    if (calls < 3)
                        return "same";
                    return "next";
                }),
            2);
        object firstUnique = schema.GenerateFromRoot(unique);
        object secondUnique = schema.GenerateFromRoot(unique);
        SchemaNode exhausted = schema.UniqueFromNameAndSourceAndRetryLimit(
            "constant",
            schema.Constant("only"),
            2);
        schema.GenerateFromRoot(exhausted);

        Assert.Multiple(() =>
        {
            Assert.That(firstSequence,
                Is.EqualTo(new object[] { 10L, 12L, 14L }));
            Assert.That(secondSequence,
                Is.EqualTo(new object[] { 16L, 18L, 20L }));
            Assert.That(firstUnique, Is.EqualTo("same"));
            Assert.That(secondUnique, Is.EqualTo("next"));
            Assert.That(calls, Is.EqualTo(3));
            Assert.That(() => schema.GenerateFromRoot(exhausted),
                Throws.TypeOf<FakeException>()
                    .With.Message.Contains("constant")
                    .And.Message.Contains("2 attempts"));
        });
    }

    [Test]
    public void CrossKindCyclesReportTheTraversalPath()
    {
        SchemaGenerator schema = Fake.Create(14520).Schema();
        SchemaNode condition = schema.Condition(
            context => true,
            schema.Constant("yes"),
            schema.Constant("no"));
        SchemaNode unique = schema.UniqueFromNameAndSourceAndRetryLimit(
            "cycle-scope",
            condition,
            2);
        condition.WhenTrue = unique;

        FakeException validation = Assert.Throws<FakeException>(
            () => schema.ValidateFromRoot(unique));
        FakeException generation = Assert.Throws<FakeException>(
            () => schema.GenerateFromRoot(unique));

        Assert.Multiple(() =>
        {
            Assert.That(validation.Message,
                Does.Contain("$.source.whenTrue")
                    .And.Contain("schema node cycle"));
            Assert.That(generation.Message, Is.EqualTo(validation.Message));
        });
    }

    [Test]
    public void CompositeUniquenessExhaustionUsesExactlyTheRetryBudget()
    {
        Fake fake = Fake.Create(14521);
        SchemaGenerator schema = fake.Schema();
        int calls = 0;
        SchemaNode source = schema.ScalarFromGenerate(
            () =>
            {
                calls++;
                return "candidate-" + calls;
            });
        SchemaNode unique = schema.UniqueFromNameAndSourceAndCompositeKeyAndRetryLimit(
            "composite-exhaustion",
            source,
            value => new string[] { "same", "key" },
            3);

        object accepted = schema.GenerateFromRoot(unique);
        FakeException exhausted = Assert.Throws<FakeException>(
            () => schema.GenerateFromRoot(unique));

        Assert.Multiple(() =>
        {
            Assert.That(accepted, Is.EqualTo("candidate-1"));
            Assert.That(calls, Is.EqualTo(4));
            Assert.That(exhausted.Message,
                Does.Contain("composite-exhaustion")
                    .And.Contain("3 attempts"));
            Assert.That(fake.Random().Unique("composite-exhaustion").Count(),
                Is.EqualTo(1));
        });

        schema.ResetUnique("composite-exhaustion");

        Assert.That(schema.GenerateFromRoot(unique), Is.EqualTo("candidate-5"));
    }

    [Test]
    public void OutputSizeBoundsCumulativeGeneratedValuesAndCallbackArrays()
    {
        FakeOptions options = FakeOptions.Create(13907);
        options.MaximumOutputSize = 8;
        SchemaGenerator schema = Fake.Create(options).Schema();
        SchemaNode objectValue = schema.Object(
            new SchemaField[]
            {
                schema.Field("name", schema.Constant("four"))
            });
        object[] cyclic = new object[1];
        cyclic[0] = cyclic;
        SchemaNode cyclicValue = schema.ScalarFromGenerate(() => cyclic);

        Assert.Multiple(() =>
        {
            Assert.That(() => schema.GenerateFromRoot(objectValue),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With.Message.Contains("configured maximum"));
            Assert.That(() => schema.GenerateFromRoot(cyclicValue),
                Throws.TypeOf<FakeException>()
                    .With.Message.Contains("array cycle"));
        });
    }

    [Test]
    public void NullEntryPointsAreRejected()
    {
        SchemaGenerator schema = Fake.Create(13908).Schema();

        Assert.Multiple(() =>
        {
            Assert.That(() => schema.Validate((FakeSchema)null),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => schema.ValidateFromRoot((SchemaNode)null),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => schema.Generate((FakeSchema)null),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => schema.GenerateFromRoot((SchemaNode)null),
                Throws.TypeOf<ArgumentNullException>());
        });
    }
}
