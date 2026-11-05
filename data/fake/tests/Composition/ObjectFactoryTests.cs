using System.Collections;
using fake;

namespace fake.Tests;

[TestFixture]
public class ObjectFactoryTests
{
    [Test]
    public void CreateSupportsParameterlessAndFakeAwareFactories()
    {
        FakeRuleSet rules = Rules();
        Fake firstFake = Fake.Create(13700);
        Fake secondFake = Fake.Create(13700);
        Fake observed = null;
        int calls = 0;
        ObjectFiller firstFiller = new ObjectFiller(firstFake);
        ObjectFiller secondFiller = new ObjectFiller(secondFake);

        FactoryTarget first = (FactoryTarget)firstFiller.Create(
            () =>
            {
                calls++;
                FactoryTarget target = new FactoryTarget();
                target.Index = 7;
                return target;
            },
            rules);
        FactoryTarget same = (FactoryTarget)secondFiller.CreateFromFactoryAndRules(
            value =>
            {
                observed = value;
                FactoryTarget target = new FactoryTarget();
                target.Index = 7;
                return target;
            },
            rules);

        Assert.Multiple(() =>
        {
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(observed, Is.SameAs(secondFake));
            Assert.That(first.Generated, Is.EqualTo(same.Generated));
            Assert.That(first.Display, Is.EqualTo(same.Display));
            Assert.That(first.Display,
                Is.EqualTo("7|" + first.Generated));
        });
    }

    [Test]
    public void ManyCreatesIndependentObjectsInDeterministicOrder()
    {
        FakeRuleSet rules = Rules();
        int firstIndex = 0;
        int secondIndex = 0;
        ObjectFiller firstFiller = new ObjectFiller(Fake.Create(13701));
        ObjectFiller secondFiller = new ObjectFiller(Fake.Create(13701));

        object[] first = firstFiller.Many(
            () =>
            {
                FactoryTarget target = new FactoryTarget();
                target.Index = firstIndex;
                firstIndex++;
                return target;
            },
            rules,
            5);
        object[] second = secondFiller.ManyFromFactoryAndRulesAndCount(
            value =>
            {
                FactoryTarget target = new FactoryTarget();
                target.Index = secondIndex;
                secondIndex++;
                return target;
            },
            rules,
            5);
        object[] empty = firstFiller.Many(
            () => new FactoryTarget(),
            rules,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(first, Has.Length.EqualTo(5));
            Assert.That(second, Has.Length.EqualTo(5));
            Assert.That(empty, Is.Empty);
            Assert.That(firstIndex, Is.EqualTo(5));
            Assert.That(secondIndex, Is.EqualTo(5));
        });
        int index = 0;
        while (index < first.Length)
        {
            FactoryTarget firstValue = (FactoryTarget)first[index];
            FactoryTarget secondValue = (FactoryTarget)second[index];
            Assert.Multiple(() =>
            {
                Assert.That(firstValue, Is.Not.SameAs(secondValue));
                Assert.That(firstValue.Index, Is.EqualTo(index));
                Assert.That(secondValue.Index, Is.EqualTo(index));
                Assert.That(firstValue.Generated,
                    Is.EqualTo(secondValue.Generated));
                Assert.That(firstValue.Display,
                    Is.EqualTo(secondValue.Display));
            });
            index++;
        }
    }

    [Test]
    public void FillPopulatesExistingArraysAndListsInPlace()
    {
        FakeRuleSet rules = Rules();
        FactoryTarget first = Target(1);
        FactoryTarget second = Target(2);
        FactoryTarget[] array = new FactoryTarget[] { first, second };
        ObjectFiller arrayFiller = new ObjectFiller(Fake.Create(13702));

        Array returnedArray = arrayFiller.Fill(array, rules);

        FactoryTarget third = Target(3);
        FactoryTarget fourth = Target(4);
        ArrayList list = new ArrayList();
        list.Add(third);
        list.Add(fourth);
        ObjectFiller listFiller = new ObjectFiller(Fake.Create(13703));
        IList returnedList = listFiller.FillFromValuesAndRules((IList)list, rules);

        Array bounded = Array.CreateInstance(
            typeof(FactoryTarget),
            new int[] { 2 },
            new int[] { 3 });
        FactoryTarget fifth = Target(5);
        FactoryTarget sixth = Target(6);
        bounded.SetValue(fifth, 3);
        bounded.SetValue(sixth, 4);
        new ObjectFiller(Fake.Create(13704)).Fill(bounded, rules);

        Assert.Multiple(() =>
        {
            Assert.That(returnedArray, Is.SameAs(array));
            Assert.That(array[0], Is.SameAs(first));
            Assert.That(array[1], Is.SameAs(second));
            Assert.That(first.Display,
                Is.EqualTo("1|" + first.Generated));
            Assert.That(second.Display,
                Is.EqualTo("2|" + second.Generated));
            Assert.That(returnedList, Is.SameAs(list));
            Assert.That(list[0], Is.SameAs(third));
            Assert.That(list[1], Is.SameAs(fourth));
            Assert.That(third.Display,
                Is.EqualTo("3|" + third.Generated));
            Assert.That(fourth.Display,
                Is.EqualTo("4|" + fourth.Generated));
            Assert.That(fifth.Display,
                Is.EqualTo("5|" + fifth.Generated));
            Assert.That(sixth.Display,
                Is.EqualTo("6|" + sixth.Generated));
        });
    }

    [Test]
    public void FactoriesAndCollectionsAreValidatedBeforeMutation()
    {
        FakeOptions options = FakeOptions.Create(13705);
        options.MaximumCount = 1;
        ObjectFiller limited = new ObjectFiller(Fake.Create(options));
        ObjectFiller filler = new ObjectFiller(Fake.Create(13705));
        FakeRuleSet rules = Rules();
        FactoryTarget preserved = Target(9);
        preserved.Generated = "preserved";
        object[] containsNull = new object[] { preserved, null };
        ArrayList list = new ArrayList();
        list.Add(Target(1));
        list.Add(Target(2));

        Assert.Multiple(() =>
        {
            Assert.That(
                Assert.Throws<ArgumentNullException>(() => filler.Many(
                    (Func<object>)null,
                    rules,
                    1)).ParamName,
                Is.EqualTo("factory"));
            Assert.That(
                Assert.Throws<ArgumentNullException>(() => filler.Many(
                    () => new FactoryTarget(),
                    null,
                    1)).ParamName,
                Is.EqualTo("rules"));
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(() => limited.Many(
                    () => new FactoryTarget(),
                    rules,
                    -1)).ParamName,
                Is.EqualTo("count"));
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(() => limited.Many(
                    () => new FactoryTarget(),
                    rules,
                    2)).ParamName,
                Is.EqualTo("count"));
            Assert.That(() => filler.Create(() => null, rules),
                Throws.TypeOf<FakeException>()
                    .With.Message.EqualTo("The object factory returned null."));
            Assert.That(
                Assert.Throws<ArgumentException>(() => filler.Fill(
                    containsNull,
                    rules)).ParamName,
                Is.EqualTo("values"));
            Assert.That(preserved.Generated, Is.EqualTo("preserved"));
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(() => limited.FillFromValuesAndRules(
                    (IList)list,
                    rules)).ParamName,
                Is.EqualTo("values"));
            Assert.That(
                Assert.Throws<ArgumentException>(() => filler.Fill(
                    new FactoryTarget[1, 1],
                    rules)).ParamName,
                Is.EqualTo("values"));
            Assert.That(
                Assert.Throws<ArgumentNullException>(() => filler.Fill(
                    (Array)null,
                    rules)).ParamName,
                Is.EqualTo("values"));
            Assert.That(
                Assert.Throws<ArgumentNullException>(() => filler.FillFromValuesAndRules(
                    (IList)null,
                    rules)).ParamName,
                Is.EqualTo("values"));
        });
    }

    [Test]
    public void CancellationStopsFactoriesAndCollectionPopulation()
    {
        FakeRuleSet rules = Rules();
        ObjectFiller filler = new ObjectFiller(Fake.Create(13706));
        CancellationTokenSource before = new CancellationTokenSource();
        before.Cancel();
        int beforeCalls = 0;

        Assert.Throws<OperationCanceledException>(() => filler.CreateFromFactoryAndRulesAndCancellationToken(
            () =>
            {
                beforeCalls++;
                return new FactoryTarget();
            },
            rules,
            before.Token));

        CancellationTokenSource duringMany = new CancellationTokenSource();
        int manyCalls = 0;
        Assert.Throws<OperationCanceledException>(() => filler.ManyFromFactoryAndRulesAndCountAndCancellationToken(
            () =>
            {
                manyCalls++;
                if (manyCalls == 2)
                    duringMany.Cancel();
                return Target(manyCalls);
            },
            rules,
            5,
            duringMany.Token));

        CancellationTokenSource duringFill = new CancellationTokenSource();
        FactoryTarget first = Target(1);
        FactoryTarget second = Target(2);
        FakeRuleSet cancelRules = new FakeRuleSet();
        cancelRules.GenerateFromMemberPathAndGenerate(
            "Generated",
            () =>
            {
                duringFill.Cancel();
                return "first";
            });

        Assert.Throws<OperationCanceledException>(() => filler.FillFromValuesAndRulesAndCancellationToken(
            new FactoryTarget[] { first, second },
            cancelRules,
            duringFill.Token));

        Assert.Multiple(() =>
        {
            Assert.That(beforeCalls, Is.EqualTo(0));
            Assert.That(manyCalls, Is.EqualTo(2));
            Assert.That(first.Generated, Is.EqualTo("first"));
            Assert.That(second.Generated, Is.Null);
        });
    }

    [Test]
    public void ProfileCustomUsesTheOwningFakeAndSupportsCancellation()
    {
        FakeRuleSet rules = Rules();
        Fake firstFake = Fake.Create(13707);
        Fake secondFake = Fake.Create(13707);
        Fake observed = null;

        FactoryTarget first = (FactoryTarget)firstFake.Profile().CustomFromFactoryAndRules(
            value =>
            {
                observed = value;
                return Target(11);
            },
            rules);
        FactoryTarget second = (FactoryTarget)secondFake.Profile().Custom(
            () => Target(11),
            rules);
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        int calls = 0;

        Assert.Throws<OperationCanceledException>(() => firstFake.Profile()
            .CustomFromFactoryAndRulesAndCancellationToken(
                () =>
                {
                    calls++;
                    return Target(12);
                },
                rules,
                cancellation.Token));

        Assert.Multiple(() =>
        {
            Assert.That(observed, Is.SameAs(firstFake));
            Assert.That(first.Generated, Is.EqualTo(second.Generated));
            Assert.That(first.Display, Is.EqualTo(second.Display));
            Assert.That(calls, Is.EqualTo(0));
        });
    }

    static FakeRuleSet Rules()
    {
        FakeRuleSet rules = new FakeRuleSet();
        rules.Generate(
            "Generated",
            value => value.Identifier().ShortId());
        rules.Derived(
            "Display",
            target =>
            {
                FactoryTarget value = (FactoryTarget)target;
                return value.Index + "|" + value.Generated;
            });
        return rules;
    }

    static FactoryTarget Target(int index)
    {
        FactoryTarget result = new FactoryTarget();
        result.Index = index;
        return result;
    }
}

public class FactoryTarget
{
    public int Index;

    public string Generated;

    public string Display;
}
