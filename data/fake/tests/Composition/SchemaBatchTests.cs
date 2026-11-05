using System.Collections;
using fake;

namespace fake.Tests;

[TestFixture]
public class SchemaBatchTests
{
    [Test]
    public void ZeroAndMaximumCountsRespectConfiguredBounds()
    {
        FakeOptions options = FakeOptions.Create(14205);
        options.MaximumCount = 3;
        ISchemaGenerator schema = Fake.Create(options).Schema();
        int calls = 0;
        FakeSchema definition = schema.Schema(
            schema.ScalarFromGenerate(
                () =>
                {
                    calls++;
                    return calls;
                }));
        object[] emptyArray = Array.Empty<object>();
        ArrayList emptyList = new ArrayList();

        object[] emptyMany = schema.Many(definition, 0);
        schema.Deliver(definition, 0, value => calls++);
        Array returnedArray = schema.Fill(emptyArray, definition);
        IList returnedList = schema.FillFromListAndSchema(
            emptyList,
            definition);
        object[] emptyPage = schema.Page(
            definition,
            long.MaxValue,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(emptyMany, Is.Empty);
            Assert.That(emptyPage, Is.Empty);
            Assert.That(returnedArray, Is.SameAs(emptyArray));
            Assert.That(returnedList, Is.SameAs(emptyList));
            Assert.That(calls, Is.Zero);
        });

        object[] maximum = schema.Many(definition, 3);

        Assert.Multiple(() =>
        {
            Assert.That(maximum, Is.EqualTo(new object[] { 1, 2, 3 }));
            Assert.That(calls, Is.EqualTo(3));
            Assert.That(
                () => schema.Many(definition, 4),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With.Property("ParamName").EqualTo("count"));
            Assert.That(calls, Is.EqualTo(3));
        });
    }

    [Test]
    public void ManyUsesFreshCorrelationAndContinuousSequenceState()
    {
        Fake fake = Fake.Create(14200);
        ISchemaGenerator schema = fake.Schema();
        SchemaNode root = schema.Object(
            new SchemaField[]
            {
                schema.Field("id", schema.Sequence("batch-records")),
                schema.Field("copy", schema.Reference("id")),
                schema.Field(
                    "derived",
                    schema.Derived(
                        value =>
                            ((Dictionary<string, object>)value)["id"] +
                            "-derived"))
            });
        FakeSchema definition = schema.Schema(root);

        object[] values = schema.Many(definition, 3);
        Dictionary<string, object> first =
            (Dictionary<string, object>)values[0];
        Dictionary<string, object> second =
            (Dictionary<string, object>)values[1];
        Dictionary<string, object> next =
            (Dictionary<string, object>)schema.Generate(definition);

        Assert.Multiple(() =>
        {
            Assert.That(first["id"], Is.EqualTo(0L));
            Assert.That(first["copy"], Is.EqualTo(0L));
            Assert.That(first["derived"], Is.EqualTo("0-derived"));
            Assert.That(second["id"], Is.EqualTo(1L));
            Assert.That(second["copy"], Is.EqualTo(1L));
            Assert.That(second["derived"], Is.EqualTo("1-derived"));
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(next["id"], Is.EqualTo(3L));
        });
    }

    [Test]
    public void DeliveryAndExistingDestinationsContinueOneSequence()
    {
        ISchemaGenerator schema = Fake.Create(14201).Schema();
        FakeSchema definition = schema.Schema(
            schema.SequenceFromNameAndStartAndIncrement(
                "destination-records",
                10L,
                1L));
        ArrayList delivered = new ArrayList();
        object[] array = new object[2];
        ArrayList list = new ArrayList(
            new object[] { "unchanged", "unchanged" });

        schema.Deliver(definition, 2, value => delivered.Add(value));
        Array returnedArray = schema.Fill(array, definition);
        IList returnedList = schema.FillFromListAndSchema(list, definition);

        Assert.Multiple(() =>
        {
            Assert.That(delivered, Is.EqualTo(new object[] { 10L, 11L }));
            Assert.That(array, Is.EqualTo(new object[] { 12L, 13L }));
            Assert.That(list, Is.EqualTo(new object[] { 14L, 15L }));
            Assert.That(returnedArray, Is.SameAs(array));
            Assert.That(returnedList, Is.SameAs(list));
        });
    }

    [Test]
    public void DeterministicPagesReplayWithoutAdvancingActiveState()
    {
        ISchemaGenerator schema = Fake.Create(14202).Schema();
        FakeSchema definition = schema.Schema(
            schema.Sequence("page-records"));

        object[] zeroth = schema.Page(definition, 0L, 3);
        object[] first = schema.Page(definition, 1L, 3);
        object[] repeated = schema.Page(definition, 1L, 3);
        object active = schema.Generate(definition);

        Assert.Multiple(() =>
        {
            Assert.That(zeroth, Is.EqualTo(
                new object[] { 0L, 1L, 2L }));
            Assert.That(first, Is.EqualTo(new object[] { 3L, 4L, 5L }));
            Assert.That(repeated, Is.EqualTo(first));
            Assert.That(active, Is.EqualTo(0L));
        });
    }

    [Test]
    public void CallbackFailureStopsOrderedDeliveryAfterCompletedPrefix()
    {
        ISchemaGenerator schema = Fake.Create(14206).Schema();
        FakeSchema definition = schema.Schema(
            schema.Sequence("callback-failure-records"));
        ArrayList delivered = new ArrayList();

        Assert.That(
            () => schema.Deliver(
                definition,
                5,
                value =>
                {
                    if ((long)value == 2L)
                    {
                        throw new InvalidOperationException(
                            "delivery failed");
                    }
                    delivered.Add(value);
                }),
            Throws.TypeOf<InvalidOperationException>()
                .With.Message.EqualTo("delivery failed"));

        Assert.Multiple(() =>
        {
            Assert.That(delivered, Is.EqualTo(new object[] { 0L, 1L }));
            Assert.That(schema.Generate(definition), Is.EqualTo(3L));
        });
    }

    [Test]
    public void CancellationStopsDeliveryAndKeepsCompletedPrefix()
    {
        ISchemaGenerator schema = Fake.Create(14203).Schema();
        FakeSchema definition = schema.Schema(
            schema.Sequence("cancel-records"));
        CancellationTokenSource cancellation =
            new CancellationTokenSource();
        ArrayList delivered = new ArrayList();

        Assert.That(
            () => schema.DeliverFromSchemaAndCountAndCallbackAndCancellationToken(
                definition,
                5,
                value =>
                {
                    delivered.Add(value);
                    if (delivered.Count == 2)
                        cancellation.Cancel();
                },
                cancellation.Token),
            Throws.TypeOf<OperationCanceledException>());

        Assert.Multiple(() =>
        {
            Assert.That(delivered, Is.EqualTo(new object[] { 0L, 1L }));
            Assert.That(schema.Generate(definition), Is.EqualTo(2L));
        });
    }

    [Test]
    public void PreCancellationLeavesStateAndDestinationsUntouched()
    {
        ISchemaGenerator schema = Fake.Create(14207).Schema();
        FakeSchema definition = schema.Schema(
            schema.Sequence("pre-cancel-records"));
        CancellationTokenSource cancellation =
            new CancellationTokenSource();
        cancellation.Cancel();
        object[] array = new object[] { "array" };
        ArrayList list = new ArrayList(new object[] { "list" });

        Assert.Multiple(() =>
        {
            Assert.That(
                () => schema.GenerateFromSchemaAndCancellationToken(
                    definition,
                    cancellation.Token),
                Throws.TypeOf<OperationCanceledException>());
            Assert.That(
                () => schema.ManyFromSchemaAndCountAndCancellationToken(
                    definition,
                    1,
                    cancellation.Token),
                Throws.TypeOf<OperationCanceledException>());
            Assert.That(
                () => schema.FillFromArrayAndSchemaAndCancellationToken(
                    array,
                    definition,
                    cancellation.Token),
                Throws.TypeOf<OperationCanceledException>());
            Assert.That(
                () => schema.FillFromListAndSchemaAndCancellationToken(
                    list,
                    definition,
                    cancellation.Token),
                Throws.TypeOf<OperationCanceledException>());
            Assert.That(
                () => schema.PageFromSchemaAndPageIndexAndPageSizeAndCancellationToken(
                    definition,
                    0L,
                    1,
                    cancellation.Token),
                Throws.TypeOf<OperationCanceledException>());
        });

        Assert.Multiple(() =>
        {
            Assert.That(array, Is.EqualTo(new object[] { "array" }));
            Assert.That(list, Is.EqualTo(new object[] { "list" }));
            Assert.That(schema.Generate(definition), Is.EqualTo(0L));
        });
    }

    [Test]
    public void CancellationDuringGenerationKeepsCompletedFillPrefix()
    {
        ISchemaGenerator schema = Fake.Create(14208).Schema();
        CancellationTokenSource cancellation =
            new CancellationTokenSource();
        int calls = 0;
        FakeSchema definition = schema.Schema(
            schema.ScalarFromGenerate(
                () =>
                {
                    calls++;
                    if (calls == 2)
                        cancellation.Cancel();
                    return "generated-" + calls;
                }));
        object[] values = new object[]
        {
            "original-1",
            "original-2",
            "original-3"
        };

        Assert.That(
            () => schema.FillFromArrayAndSchemaAndCancellationToken(
                values,
                definition,
                cancellation.Token),
            Throws.TypeOf<OperationCanceledException>());

        Assert.Multiple(() =>
        {
            Assert.That(values, Is.EqualTo(
                new object[]
                {
                    "generated-1",
                    "original-2",
                    "original-3"
                }));
            Assert.That(calls, Is.EqualTo(2));
        });
    }

    [Test]
    public void UniquenessStateContinuesAcrossBulkAndSingleGeneration()
    {
        ISchemaGenerator schema = Fake.Create(14204).Schema();
        object[] candidates = new object[] { "first", "second", "first" };
        int calls = 0;
        SchemaNode source = schema.ScalarFromGenerate(
            () =>
            {
                object result = candidates[calls];
                calls++;
                return result;
            });
        FakeSchema definition = schema.Schema(
            schema.UniqueFromNameAndSourceAndRetryLimit(
                "bulk-unique",
                source,
                1));

        object[] values = schema.Many(definition, 2);

        Assert.Multiple(() =>
        {
            Assert.That(values, Is.EqualTo(
                new object[] { "first", "second" }));
            Assert.That(
                () => schema.Generate(definition),
                Throws.TypeOf<FakeException>());
            Assert.That(calls, Is.EqualTo(3));
        });
    }

    [Test]
    public void CyclicSchemasFailBeforeDeliveryOrDestinationMutation()
    {
        ISchemaGenerator schema = Fake.Create(14209).Schema();
        SchemaNode cycle = schema.Array(schema.Constant("value"), 1);
        cycle.Item = cycle;
        FakeSchema definition = schema.Schema(cycle);
        object[] values = new object[] { "first", "second" };
        int deliveries = 0;

        Assert.Multiple(() =>
        {
            Assert.That(
                () => schema.Deliver(
                    definition,
                    2,
                    value => deliveries++),
                Throws.TypeOf<FakeException>()
                    .With.Message.Contains("cycle"));
            Assert.That(
                () => schema.Fill(values, definition),
                Throws.TypeOf<FakeException>()
                    .With.Message.Contains("cycle"));
        });

        Assert.Multiple(() =>
        {
            Assert.That(deliveries, Is.Zero);
            Assert.That(values, Is.EqualTo(
                new object[] { "first", "second" }));
        });
    }

    [Test]
    public void OutputSizeFailureStopsBeforeDeliveringOversizedResult()
    {
        FakeOptions options = FakeOptions.Create(14210);
        options.MaximumOutputSize = 4;
        ISchemaGenerator schema = Fake.Create(options).Schema();
        int calls = 0;
        FakeSchema definition = schema.Schema(
            schema.ScalarFromGenerate(
                () =>
                {
                    calls++;
                    if (calls == 1)
                        return "ok";
                    return "oversized";
                }));
        ArrayList delivered = new ArrayList();

        Assert.That(
            () => schema.Deliver(
                definition,
                3,
                value => delivered.Add(value)),
            Throws.TypeOf<ArgumentOutOfRangeException>()
                .With.Message.Contains("configured maximum"));

        Assert.Multiple(() =>
        {
            Assert.That(delivered, Is.EqualTo(new object[] { "ok" }));
            Assert.That(calls, Is.EqualTo(2));
        });
    }
}
