using fake;

namespace fake.Tests;

[TestFixture]
public class SchemaNodeTests
{
    [Test]
    public void ScalarConstantReferenceDerivedAndConditionNodesRetainBehavior()
    {
        SchemaGenerator schema = Fake.Create(13800).Schema();
        Fake observed = null;
        int calls = 0;
        SchemaNode scalar = schema.Scalar(
            value =>
            {
                observed = value;
                return value.Identifier().ShortId();
            });
        SchemaNode parameterless = schema.ScalarFromGenerate(
            () =>
            {
                calls++;
                return "callback";
            });
        SchemaNode constant = schema.Constant(null);
        SchemaNode reference = schema.Reference("customer.identifier");
        SchemaNode derived = schema.Derived(
            context => ((Dictionary<string, object>)context)["first"] +
                " derived");
        SchemaNode whenTrue = schema.Constant("yes");
        SchemaNode whenFalse = schema.Constant("no");
        SchemaNode conditional = schema.Conditional(
            context => (bool)((Dictionary<string, object>)context)["flag"],
            whenTrue,
            whenFalse);
        SchemaNode conditionAlias = schema.Condition(
            context => true,
            whenTrue,
            whenFalse);
        Fake callbackFake = Fake.Create(13801);
        Dictionary<string, object> values = new Dictionary<string, object>();
        values["first"] = "base";
        values["flag"] = true;

        object scalarValue = scalar.ScalarFactory(callbackFake);
        object parameterlessValue = parameterless.ScalarFactory(callbackFake);
        object derivedValue = derived.Deriver(values);
        bool predicateValue = conditional.Predicate(values);

        Assert.Multiple(() =>
        {
            Assert.That(scalar.Kind, Is.EqualTo(SchemaNodeKind.Scalar));
            Assert.That(scalarValue, Is.TypeOf<string>());
            Assert.That(observed, Is.SameAs(callbackFake));
            Assert.That(parameterless.Kind,
                Is.EqualTo(SchemaNodeKind.Scalar));
            Assert.That(parameterlessValue, Is.EqualTo("callback"));
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(constant.Kind,
                Is.EqualTo(SchemaNodeKind.Constant));
            Assert.That(constant.ConstantValue, Is.Null);
            Assert.That(reference.Kind,
                Is.EqualTo(SchemaNodeKind.Reference));
            Assert.That(reference.ReferencePath,
                Is.EqualTo("customer.identifier"));
            Assert.That(derived.Kind,
                Is.EqualTo(SchemaNodeKind.Derived));
            Assert.That(derivedValue, Is.EqualTo("base derived"));
            Assert.That(conditional.Kind,
                Is.EqualTo(SchemaNodeKind.Condition));
            Assert.That(predicateValue, Is.True);
            Assert.That(conditional.WhenTrue, Is.SameAs(whenTrue));
            Assert.That(conditional.WhenFalse, Is.SameAs(whenFalse));
            Assert.That(conditionAlias.Kind,
                Is.EqualTo(SchemaNodeKind.Condition));
        });
    }

    [Test]
    public void ObjectAndArrayNodesSnapshotDeterministicStructure()
    {
        SchemaGenerator schema = Fake.Create(13802).Schema();
        SchemaNode firstValue = schema.Constant("first");
        SchemaNode secondValue = schema.Constant("second");
        SchemaField first = schema.Field("value", firstValue);
        SchemaField second = schema.Field("value", secondValue);
        SchemaField[] supplied = new SchemaField[] { first, second };

        SchemaNode objectNode = schema.Object(supplied);
        supplied[0] = schema.Field("replacement", secondValue);
        SchemaNode fixedArray = schema.Array(objectNode, 3);
        SchemaNode boundedArray = schema.ArrayFromItemAndMinimumCountAndMaximumCount(objectNode, 1, 4);
        FakeSchema wrapped = schema.Schema(boundedArray);

        Assert.Multiple(() =>
        {
            Assert.That(objectNode.Kind,
                Is.EqualTo(SchemaNodeKind.Object));
            Assert.That(objectNode.Fields,
                Is.EqualTo(new SchemaField[] { first, second }));
            Assert.That(objectNode.Fields[0].Name, Is.EqualTo("value"));
            Assert.That(objectNode.Fields[0].Node,
                Is.SameAs(firstValue));
            Assert.That(objectNode.Fields[1].Node,
                Is.SameAs(secondValue));
            Assert.That(fixedArray.Kind,
                Is.EqualTo(SchemaNodeKind.Array));
            Assert.That(fixedArray.Item, Is.SameAs(objectNode));
            Assert.That(fixedArray.MinimumCount, Is.EqualTo(3));
            Assert.That(fixedArray.MaximumCount, Is.EqualTo(3));
            Assert.That(boundedArray.MinimumCount, Is.EqualTo(1));
            Assert.That(boundedArray.MaximumCount, Is.EqualTo(4));
            Assert.That(wrapped.Root, Is.SameAs(boundedArray));
        });
    }

    [Test]
    public void VariantNodesSnapshotChoicesAndWeights()
    {
        SchemaGenerator schema = Fake.Create(13803).Schema();
        SchemaNode low = schema.Constant("low");
        SchemaNode high = schema.Constant("high");
        SchemaVariant first = schema.Choice(low, 1.5);
        SchemaVariant second = schema.Choice(high, 3.0);
        SchemaVariant[] supplied = new SchemaVariant[] { first, second };

        SchemaNode variant = schema.Variant(supplied);
        supplied[0] = schema.Choice(high, 10.0);

        Assert.Multiple(() =>
        {
            Assert.That(variant.Kind,
                Is.EqualTo(SchemaNodeKind.Variant));
            Assert.That(variant.Variants,
                Is.EqualTo(new SchemaVariant[] { first, second }));
            Assert.That(variant.Variants[0].Node, Is.SameAs(low));
            Assert.That(variant.Variants[0].Weight, Is.EqualTo(1.5));
            Assert.That(variant.Variants[1].Node, Is.SameAs(high));
            Assert.That(variant.Variants[1].Weight, Is.EqualTo(3.0));
        });
    }

    [Test]
    public void SequenceAndUniqueNodesCaptureNamedStateConfiguration()
    {
        FakeOptions options = FakeOptions.Create(13804);
        options.UniquenessRetryLimit = 7;
        SchemaGenerator schema = Fake.Create(options).Schema();
        SchemaNode source = schema.Scalar(
            value => value.Identifier().ShortId());
        SchemaNode defaultSequence = schema.Sequence("orders");
        SchemaNode configuredSequence = schema.SequenceFromNameAndStartAndIncrement(
            "descending",
            100L,
            -5L);
        SchemaNode defaultUnique = schema.Unique("identifiers", source);
        SchemaNode configuredUnique = schema.UniqueFromNameAndSourceAndRetryLimit("emails", source, 3);

        Assert.Multiple(() =>
        {
            Assert.That(defaultSequence.Kind,
                Is.EqualTo(SchemaNodeKind.Sequence));
            Assert.That(defaultSequence.StateName, Is.EqualTo("orders"));
            Assert.That(defaultSequence.SequenceStart, Is.EqualTo(0L));
            Assert.That(defaultSequence.SequenceIncrement, Is.EqualTo(1L));
            Assert.That(configuredSequence.StateName,
                Is.EqualTo("descending"));
            Assert.That(configuredSequence.SequenceStart, Is.EqualTo(100L));
            Assert.That(configuredSequence.SequenceIncrement,
                Is.EqualTo(-5L));
            Assert.That(defaultUnique.Kind,
                Is.EqualTo(SchemaNodeKind.Unique));
            Assert.That(defaultUnique.StateName,
                Is.EqualTo("identifiers"));
            Assert.That(defaultUnique.Source, Is.SameAs(source));
            Assert.That(defaultUnique.RetryLimit, Is.EqualTo(7));
            Assert.That(configuredUnique.StateName, Is.EqualTo("emails"));
            Assert.That(configuredUnique.RetryLimit, Is.EqualTo(3));
        });
    }

    [Test]
    public void BuildingSchemaNodesDoesNotConsumeSchemaEntropy()
    {
        Fake inspected = Fake.Create(13805);
        Fake untouched = Fake.Create(13805);
        SchemaGenerator schema = inspected.Schema();
        SchemaNode value = schema.Constant("value");

        schema.Scalar(fake => fake.Identifier().ShortId());
        schema.Object(new SchemaField[] { schema.Field("value", value) });
        schema.Array(value, 2);
        schema.Reference("value");
        schema.Derived(context => context);
        schema.Conditional(context => true, value, value);
        schema.Variant(new SchemaVariant[] { schema.Choice(value, 1.0) });
        schema.Sequence("sequence");
        schema.Unique("unique", value);

        Assert.That(inspected.Schema().RandomValue.Long(),
            Is.EqualTo(untouched.Schema().RandomValue.Long()));
    }

    [Test]
    public void BuildersRejectInvalidLocalArguments()
    {
        FakeOptions options = FakeOptions.Create(13806);
        options.MaximumCount = 1;
        SchemaGenerator schema = Fake.Create(options).Schema();
        SchemaNode value = schema.Constant("value");
        SchemaVariant choice = schema.Choice(value, 1.0);

        Assert.Multiple(() =>
        {
            Assert.That(() => new FakeSchema(null),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => new SchemaField(null, value),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => new SchemaField(" ", value),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => new SchemaField("name", null),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => new SchemaVariant(null, 1.0),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => new SchemaVariant(value, -1.0),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new SchemaVariant(value, double.NaN),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => schema.ScalarFromGenerate((Func<object>)null),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => schema.Scalar(
                    (Func<Fake, object>)null),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => schema.Object(null),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => schema.Object(
                    new SchemaField[] { null }),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => schema.Object(
                    new SchemaField[]
                    {
                        schema.Field("one", value),
                        schema.Field("two", value)
                    }),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => schema.Array(null, 1),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => schema.Array(value, -1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => schema.ArrayFromItemAndMinimumCountAndMaximumCount(value, 0, 2),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => schema.ArrayFromItemAndMinimumCountAndMaximumCount(value, 1, 0),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => schema.Reference(null),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => schema.Reference("bad..path"),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => schema.Derived(null),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => schema.Conditional(null, value, value),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => schema.Conditional(context => true, null, value),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => schema.Conditional(context => true, value, null),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => schema.Choice(value, double.PositiveInfinity),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => schema.Variant(null),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => schema.Variant(new SchemaVariant[0]),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => schema.Variant(
                    new SchemaVariant[] { null }),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => schema.Variant(
                    new SchemaVariant[]
                    {
                        schema.Choice(value, 1.0),
                        schema.Choice(value, 1.0)
                    }),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => schema.Variant(
                    new SchemaVariant[]
                    {
                        schema.Choice(value, 0.0)
                    }),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => schema.Sequence(" "),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => schema.SequenceFromNameAndStartAndIncrement("sequence", 0L, 0L),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => schema.Unique("unique", null),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => schema.UniqueFromNameAndSourceAndRetryLimit("unique", value, 0),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }
}
