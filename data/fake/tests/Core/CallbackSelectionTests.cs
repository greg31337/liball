using fake;

namespace fake.Tests;

[TestFixture]
public class CallbackSelectionTests
{
    enum SampleValue
    {
        First = -9,
        Second = 4,
        Third = 200
    }

    enum EmptyValue
    {
    }

    [Test]
    public void RepeatInvokesCallbackInOrderThroughConfiguredMaximum()
    {
        FakeOptions options = new FakeOptions();
        options.MaximumCount = 3;
        FakeRandom random = Fake.Create(options).Random();
        int next = 0;

        object[] empty = random.Repeat(0, () =>
        {
            next++;
            return next;
        });
        object[] maximum = random.Repeat(3, () =>
        {
            next++;
            return next;
        });

        Assert.That(empty, Is.Empty);
        Assert.That(maximum, Is.EqualTo(new object[] { 1, 2, 3 }));
        Assert.That(next, Is.EqualTo(3));
    }

    [Test]
    public void DestinationFillWritesInOrderAndSupportsEmptyArrays()
    {
        FakeOptions options = new FakeOptions();
        options.MaximumCount = 3;
        FakeRandom random = Fake.Create(options).Random();
        object[] empty = Array.Empty<object>();
        object[] destination = new object[3];
        int next = 0;

        random.FillFromDestinationAndGenerate(empty, () =>
        {
            next++;
            return next;
        });
        random.FillFromDestinationAndGenerate(destination, () =>
        {
            next++;
            return next;
        });

        Assert.That(destination, Is.EqualTo(new object[] { 1, 2, 3 }));
        Assert.That(next, Is.EqualTo(3));
    }

    [Test]
    public void CallbackFailurePropagatesAndFillKeepsCompletedPrefix()
    {
        FakeRandom random = Fake.Create(4401).Random();
        object untouched = new object();
        object[] destination = new object[] { untouched, untouched, untouched };
        int calls = 0;
        Func<object> generate = () =>
        {
            calls++;
            if (calls == 2)
            {
                throw new InvalidOperationException("callback failure");
            }
            return calls;
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => random.FillFromDestinationAndGenerate(destination, generate));

        Assert.That(exception.Message, Is.EqualTo("callback failure"));
        Assert.That(destination[0], Is.EqualTo(1));
        Assert.That(destination[1], Is.SameAs(untouched));
        Assert.That(destination[2], Is.SameAs(untouched));
        Assert.That(calls, Is.EqualTo(2));
    }

    [Test]
    public void RepeatPropagatesCallbackFailureAndStopsInvoking()
    {
        FakeRandom random = Fake.Create(4403).Random();
        int calls = 0;

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => random.Repeat(3, () =>
        {
            calls++;
            if (calls == 2)
            {
                throw new InvalidOperationException("repeat failure");
            }
            return calls;
        }));

        Assert.That(exception.Message, Is.EqualTo("repeat failure"));
        Assert.That(calls, Is.EqualTo(2));
    }

    [Test]
    public void RepeatAndDestinationFillReplaySeededCallbackGenerationThroughInterface()
    {
        IFakeRandom repeatedRandom = Fake.Create(4404).Random();
        IFakeRandom filledRandom = Fake.Create(4404).Random();
        object[] destination = new object[64];

        object[] repeated = repeatedRandom.Repeat(64, () => repeatedRandom.Int());
        filledRandom.FillFromDestinationAndGenerate(destination, () => filledRandom.Int());

        Assert.That(destination, Is.EqualTo(repeated));
    }

    [Test]
    public void CallbackOperationsRejectNullAndOutOfRangeInputsBeforeInvocation()
    {
        FakeOptions options = new FakeOptions();
        options.MaximumCount = 2;
        FakeRandom random = Fake.Create(options).Random();
        int calls = 0;
        Func<object> generate = () =>
        {
            calls++;
            return new object();
        };

        Assert.That(Assert.Throws<ArgumentNullException>(() => random.Repeat(1, (Func<object>)null)).ParamName, Is.EqualTo("generate"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => random.Repeat(-1, generate)).ParamName, Is.EqualTo("count"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => random.Repeat(3, generate)).ParamName, Is.EqualTo("count"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => random.FillFromDestinationAndGenerate(null, generate)).ParamName, Is.EqualTo("destination"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => random.FillFromDestinationAndGenerate(Array.Empty<object>(), null)).ParamName, Is.EqualTo("generate"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => random.FillFromDestinationAndGenerate(new object[3], generate)).ParamName, Is.EqualTo("destination"));
        Assert.That(calls, Is.Zero);
    }

    [Test]
    public void EnumSelectionIsSeededAndCanReachEveryDeclaredValue()
    {
        FakeRandom first = Fake.Create(4402).Random();
        FakeRandom second = Fake.Create(4402).Random();
        HashSet<SampleValue> seen = new HashSet<SampleValue>();

        for (int index = 0; index < 100; index++)
        {
            object expected = first.EnumValue(typeof(SampleValue));
            object actual = second.EnumValue(typeof(SampleValue));
            Assert.That(actual, Is.EqualTo(expected));
            seen.Add((SampleValue)actual);
        }

        Assert.That(seen, Is.EquivalentTo(Enum.GetValues<SampleValue>()));
    }

    [Test]
    public void EnumSelectionRejectsInvalidTypesLimitsAndExhaustionWithoutAdvancing()
    {
        FakeOptions options = new FakeOptions();
        options.MaximumCount = 2;
        FakeRandom random = Fake.Create(options).Random();
        FakeState before = random.Snapshot();

        Assert.That(Assert.Throws<ArgumentNullException>(() => random.EnumValue(null)).ParamName, Is.EqualTo("enumType"));
        Assert.That(Assert.Throws<ArgumentException>(() => random.EnumValue(typeof(string))).ParamName, Is.EqualTo("enumType"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => random.EnumValue(typeof(SampleValue))).ParamName, Is.EqualTo("enumType"));
        Assert.Throws<FakeException>(() => random.EnumValue(typeof(EmptyValue)));
        AssertStateEqual(random.Snapshot(), before);
    }

    static void AssertStateEqual(FakeState actual, FakeState expected)
    {
        Assert.That(actual.Algorithm, Is.EqualTo(expected.Algorithm));
        Assert.That(actual.InitialSeed, Is.EqualTo(expected.InitialSeed));
        Assert.That(actual.State0, Is.EqualTo(expected.State0));
        Assert.That(actual.State1, Is.EqualTo(expected.State1));
        Assert.That(actual.State2, Is.EqualTo(expected.State2));
        Assert.That(actual.State3, Is.EqualTo(expected.State3));
    }
}
