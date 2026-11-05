using System.Collections.Concurrent;
using fake;

namespace fake.Tests;

[TestFixture]
public class UniqueScopeTests
{
    [Test]
    public void StringNextRetriesDuplicatesAndTracksBudget()
    {
        FakeOptions options = new FakeOptions();
        options.UniquenessRetryLimit = 3;
        UniqueScope scope = Fake.Create(options).Random().Unique("letters");
        string[] candidates = new string[] { "a", "a", "b" };
        int index = 0;

        Assert.That(scope.Next(() => candidates[index++]), Is.EqualTo("a"));
        Assert.That(scope.Count(), Is.EqualTo(1));
        Assert.That(scope.RemainingAttempts(), Is.EqualTo(2));
        Assert.That(scope.Next(() => candidates[index++]), Is.EqualTo("b"));
        Assert.That(scope.Count(), Is.EqualTo(2));
        Assert.That(scope.RemainingAttempts(), Is.EqualTo(1));
    }

    [Test]
    public void ExhaustionUsesExactRetryLimitAndStableDiagnostics()
    {
        FakeOptions options = new FakeOptions();
        options.UniquenessRetryLimit = 3;
        UniqueScope scope = Fake.Create(options).Random().Unique("emails");
        scope.Next(() => "same@example.test");
        int calls = 0;

        FakeException exception = Assert.Throws<FakeException>(() => scope.Next(() =>
        {
            calls++;
            return "same@example.test";
        }));

        Assert.That(calls, Is.EqualTo(3));
        Assert.That(scope.Count(), Is.EqualTo(1));
        Assert.That(scope.RemainingAttempts(), Is.Zero);
        Assert.That(
            exception.Message,
            Is.EqualTo("Unique scope 'emails' exhausted its retry limit after 3 attempts with 1 accepted keys."));
    }

    [Test]
    public void ANewOperationReceivesAFreshBudgetAfterExhaustion()
    {
        FakeOptions options = new FakeOptions();
        options.UniquenessRetryLimit = 2;
        UniqueScope scope = Fake.Create(options).Random().Unique("values");
        scope.Next(() => "a");
        Assert.Throws<FakeException>(() => scope.Next(() => "a"));

        Assert.That(scope.Next(() => "b"), Is.EqualTo("b"));
        Assert.That(scope.RemainingAttempts(), Is.EqualTo(1));
        Assert.That(scope.Count(), Is.EqualTo(2));
    }

    [Test]
    public void CompositeKeysUseOrderedCollisionFreePartBoundaries()
    {
        UniqueScope scope = Fake.Create().Random().Unique("composite");
        object first = new string[] { "a", "b:c" };
        object second = new string[] { "a:b", "c" };
        object duplicate = new string[] { "a", "b:c" };
        object third = new string[] { "a", "b", ":c" };
        object[] candidates = new object[] { first, second, duplicate, third };
        int index = 0;

        Assert.That(scope.NextFromGenerateAndCompositeKey(() => candidates[index++], value => (string[])value), Is.SameAs(first));
        Assert.That(scope.NextFromGenerateAndCompositeKey(() => candidates[index++], value => (string[])value), Is.SameAs(second));
        Assert.That(scope.NextFromGenerateAndCompositeKey(() => candidates[index++], value => (string[])value), Is.SameAs(third));
        Assert.That(scope.Count(), Is.EqualTo(3));
    }

    [Test]
    public void CompositeKeysDistinguishNullEmptyAndPartCount()
    {
        UniqueScope scope = Fake.Create().Random().Unique("edge-parts");
        object nullPart = new string[] { null };
        object emptyPart = new string[] { "" };
        object noParts = Array.Empty<string>();
        object twoParts = new string[] { "", "" };

        Assert.That(scope.NextFromGenerateAndCompositeKey(() => nullPart, value => (string[])value), Is.SameAs(nullPart));
        Assert.That(scope.NextFromGenerateAndCompositeKey(() => emptyPart, value => (string[])value), Is.SameAs(emptyPart));
        Assert.That(scope.NextFromGenerateAndCompositeKey(() => noParts, value => (string[])value), Is.SameAs(noParts));
        Assert.That(scope.NextFromGenerateAndCompositeKey(() => twoParts, value => (string[])value), Is.SameAs(twoParts));
        Assert.That(scope.Count(), Is.EqualTo(4));
    }

    [Test]
    public void SimpleAndCompositeKeyDomainsDoNotCollide()
    {
        UniqueScope scope = Fake.Create().Random().Unique("mixed");
        object composite = new string[] { "a" };

        Assert.That(scope.Next(() => "1:a"), Is.EqualTo("1:a"));
        Assert.That(scope.NextFromGenerateAndCompositeKey(() => composite, value => (string[])value), Is.SameAs(composite));
        Assert.That(scope.Count(), Is.EqualTo(2));
    }

    [Test]
    public void NullAndEmptyStringsAreDistinctUniqueValues()
    {
        UniqueScope scope = Fake.Create().Random().Unique("nullable-strings");

        Assert.That(scope.Next(() => null), Is.Null);
        Assert.That(scope.Next(() => ""), Is.Empty);
        Assert.That(scope.Count(), Is.EqualTo(2));
    }

    [Test]
    public void NamedScopesAreCachedCaseSensitiveAndIsolated()
    {
        FakeRandom random = Fake.Create().Random();
        UniqueScope lower = random.Unique("users");
        UniqueScope same = random.Unique("users");
        UniqueScope upper = random.Unique("Users");

        lower.Next(() => "a");

        Assert.That(same, Is.SameAs(lower));
        Assert.That(same.Count(), Is.EqualTo(1));
        Assert.That(upper, Is.Not.SameAs(lower));
        Assert.That(upper.Count(), Is.Zero);
        Assert.That(upper.Next(() => "a"), Is.EqualTo("a"));
    }

    [Test]
    public void RandomForksShareScopesWhileFakeForksAreIndependent()
    {
        Fake fake = Fake.Create(601);
        FakeRandom root = fake.Random();
        FakeRandom randomFork = root.Fork("child");
        Fake fakeFork = fake.Fork("child");

        root.Unique("ids").Next(() => "a");

        Assert.That(randomFork.Unique("ids"), Is.SameAs(root.Unique("ids")));
        Assert.That(randomFork.Unique("ids").Count(), Is.EqualTo(1));
        Assert.That(fakeFork.Random().Unique("ids").Count(), Is.Zero);
    }

    [Test]
    public void ScopeAndProviderResetOperationsClearExpectedKeys()
    {
        FakeRandom random = Fake.Create().Random();
        UniqueScope first = random.Unique("first");
        UniqueScope second = random.Unique("second");
        first.Next(() => "a");
        second.Next(() => "b");

        random.ResetUnique("first");

        Assert.That(first.Count(), Is.Zero);
        Assert.That(first.RemainingAttempts(), Is.EqualTo(100));
        Assert.That(second.Count(), Is.EqualTo(1));

        random.ResetUniqueFromNoArguments();

        Assert.That(second.Count(), Is.Zero);
        Assert.That(second.RemainingAttempts(), Is.EqualTo(100));
    }

    [Test]
    public void DirectUniqueOverloadsUseNamedScopes()
    {
        FakeRandom random = Fake.Create().Random();
        object first = new string[] { "north", "1" };

        Assert.That(random.UniqueFromNameAndGenerate("strings", () => "a"), Is.EqualTo("a"));
        Assert.That(
            random.UniqueFromNameAndGenerateAndCompositeKey("objects", () => first, value => (string[])value),
            Is.SameAs(first));
        Assert.That(random.Unique("strings").Count(), Is.EqualTo(1));
        Assert.That(random.Unique("objects").Count(), Is.EqualTo(1));
    }

    [Test]
    public void SeededCandidateGenerationIsDeterministic()
    {
        FakeRandom first = Fake.Create(602).Random();
        FakeRandom second = Fake.Create(602).Random();
        string[] values = new string[] { "a", "b", "c", "d", "e" };
        string[] firstValues = new string[5];
        string[] secondValues = new string[5];

        for (int index = 0; index < firstValues.Length; index++)
        {
            firstValues[index] = first.UniqueFromNameAndGenerate("letters", () => first.Choice(values));
            secondValues[index] = second.UniqueFromNameAndGenerate("letters", () => second.Choice(values));
        }

        Assert.That(secondValues, Is.EqualTo(firstValues));
        Assert.That(firstValues, Is.Unique);
    }

    [Test]
    public void ScopeCapturesRetryAndKeyLimitsFromOptions()
    {
        FakeOptions options = new FakeOptions();
        options.UniquenessRetryLimit = 2;
        options.MaximumCount = 1;
        options.MaximumOutputSize = 4;
        Fake fake = Fake.Create(options);
        UniqueScope scope = fake.Random().Unique("limits");
        options.UniquenessRetryLimit = 100;
        options.MaximumCount = 100;
        options.MaximumOutputSize = 100;
        scope.Next(() => "a");

        Assert.That(Assert.Throws<FakeException>(() => scope.Next(() => "a")).Message, Does.Contain("after 2 attempts"));
        Assert.That(
            Assert.Throws<ArgumentOutOfRangeException>(() => scope.Next(() => "ab")).ParamName,
            Is.EqualTo("generate"));
        Assert.That(
            Assert.Throws<ArgumentOutOfRangeException>(() => scope.NextFromGenerateAndCompositeKey(
                () => new object(),
                value => new string[] { "a", "b" })).ParamName,
            Is.EqualTo("compositeKey"));
    }

    [Test]
    public void InvalidNamesCallbacksAndCompositeResultsAreRejected()
    {
        FakeRandom random = Fake.Create().Random();
        UniqueScope scope = random.Unique("valid");

        Assert.That(Assert.Throws<ArgumentNullException>(() => random.Unique(null)).ParamName, Is.EqualTo("name"));
        Assert.That(Assert.Throws<ArgumentException>(() => random.Unique(" ")).ParamName, Is.EqualTo("name"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => scope.Next((Func<string>)null)).ParamName, Is.EqualTo("generate"));
        Assert.That(
            Assert.Throws<ArgumentNullException>(() => scope.NextFromGenerateAndCompositeKey((Func<object>)null, value => new string[] { "a" })).ParamName,
            Is.EqualTo("generate"));
        Assert.That(
            Assert.Throws<ArgumentNullException>(() => scope.NextFromGenerateAndCompositeKey(() => new object(), (Func<object, string[]>)null)).ParamName,
            Is.EqualTo("compositeKey"));
        Assert.That(
            Assert.Throws<FakeException>(() => scope.NextFromGenerateAndCompositeKey(() => new object(), value => null)).Message,
            Is.EqualTo("Unique scope 'valid' composite key callback returned null."));
        Assert.That(scope.Count(), Is.Zero);
    }

    [Test]
    public void ConcurrentUniqueCallsAcceptEveryDistinctCandidateOnce()
    {
        UniqueScope scope = Fake.Create().Random().Unique("concurrent");
        ConcurrentBag<string> values = new ConcurrentBag<string>();

        Parallel.For(0, 1000, index => values.Add(scope.Next(() => index.ToString())));

        Assert.That(scope.Count(), Is.EqualTo(1000));
        Assert.That(values, Has.Count.EqualTo(1000));
        Assert.That(values.Distinct().Count(), Is.EqualTo(1000));
    }
}
