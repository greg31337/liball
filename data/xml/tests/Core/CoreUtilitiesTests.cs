using System.Collections;
using xml;

namespace xml.Tests;

[TestFixture]
public class CoreUtilitiesTests
{
    [Test]
    public void GuardAcceptsValidValues()
    {
        Assert.DoesNotThrow(() => Guard.NotNull(new object(), "value"));
        Assert.DoesNotThrow(() => Guard.NotNullOrEmpty("x", "value"));
        Assert.DoesNotThrow(() => Guard.NonNegative(0, "value"));
        Assert.DoesNotThrow(() => Guard.NonNegativeInt(0, "value"));
        Assert.DoesNotThrow(() => Guard.Positive(1, "value"));
    }

    [Test]
    public void GuardRejectsInvalidValuesWithParameterNames()
    {
        Assert.That(Assert.Throws<ArgumentNullException>(() => Guard.NotNull(null, "item"))!.ParamName, Is.EqualTo("item"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => Guard.NotNullOrEmpty(null, "text"))!.ParamName, Is.EqualTo("text"));
        Assert.That(Assert.Throws<ArgumentException>(() => Guard.NotNullOrEmpty(string.Empty, "text"))!.ParamName, Is.EqualTo("text"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => Guard.NonNegative(-1, "count"))!.ParamName, Is.EqualTo("count"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => Guard.NonNegativeInt(-1, "count"))!.ParamName, Is.EqualTo("count"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => Guard.Positive(0, "count"))!.ParamName, Is.EqualTo("count"));
    }

    [Test]
    public void LimitsTracksConsumptionReleaseAndReset()
    {
        Limits limits = new Limits(10);

        limits.Consume(6);
        Assert.That(limits.Used, Is.EqualTo(6));
        Assert.That(limits.Remaining(), Is.EqualTo(4));
        limits.Release(2);
        Assert.That(limits.Remaining(), Is.EqualTo(6));
        limits.Reset();
        Assert.That(limits.Used, Is.Zero);
        Assert.That(limits.Remaining(), Is.EqualTo(10));
    }

    [Test]
    public void LimitsRejectsOverflowWithoutChangingUsage()
    {
        Limits limits = new Limits(long.MaxValue);
        limits.Consume(long.MaxValue - 1);

        Assert.That(limits.TryConsume(2), Is.False);
        Assert.That(limits.Used, Is.EqualTo(long.MaxValue - 1));
        Assert.Throws<InvalidOperationException>(() => limits.Consume(2));
        Assert.That(limits.Used, Is.EqualTo(long.MaxValue - 1));
    }

    [Test]
    public void LimitsRejectsNegativeAndExcessReleaseAmounts()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Limits(-1));
        Limits limits = new Limits(3);
        Assert.Throws<ArgumentOutOfRangeException>(() => limits.TryConsume(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => limits.Release(1));
    }

    [Test]
    public void StringTableReturnsTheFirstOrdinalInstance()
    {
        StringTable table = new StringTable();
        string first = new string('x', 3);
        string equal = new string('x', 3);

        string stored = table.GetOrAdd(first);
        string repeated = table.GetOrAdd(equal);

        Assert.That(object.ReferenceEquals(stored, repeated), Is.True);
        Assert.That(table.EntryCount(), Is.EqualTo(1));
        Assert.That(table.ContainsText("xxx"), Is.True);
        Assert.That(table.ContainsText("XXX"), Is.False);
    }

    [Test]
    public void StringTableClearsInternedEntries()
    {
        StringTable table = new StringTable();
        table.GetOrAdd("value");

        table.ClearEntries();

        Assert.That(table.EntryCount(), Is.Zero);
        Assert.That(table.ContainsText("value"), Is.False);
    }

    [Test]
    public void NameTableInternsExpandedNamesAndTheirStrings()
    {
        StringTable strings = new StringTable();
        NameTable names = new NameTable(strings);
        string namespaceOne = new string("urn:test".ToCharArray());
        string namespaceTwo = new string("urn:test".ToCharArray());
        string localOne = new string("item".ToCharArray());
        string localTwo = new string("item".ToCharArray());

        NameEntry first = names.GetOrAddName(namespaceOne, localOne);
        NameEntry repeated = names.GetOrAddName(namespaceTwo, localTwo);
        NameEntry other = names.GetOrAddName(namespaceTwo, "other");

        Assert.That(object.ReferenceEquals(first, repeated), Is.True);
        Assert.That(object.ReferenceEquals(first.NamespaceUri, strings.GetOrAdd(namespaceTwo)), Is.True);
        Assert.That(object.ReferenceEquals(first.LocalName, strings.GetOrAdd(localTwo)), Is.True);
        Assert.That(object.ReferenceEquals(first, other), Is.False);
        Assert.That(names.NameCount(), Is.EqualTo(2));
    }

    [Test]
    public void NameTableDistinguishesNamespacesAndCanBeCleared()
    {
        NameTable names = new NameTable(new StringTable());
        NameEntry first = names.GetOrAddName("urn:one", "item");
        NameEntry second = names.GetOrAddName("urn:two", "item");
        Assert.That(object.ReferenceEquals(first, second), Is.False);

        names.ClearNames();

        Assert.That(names.NameCount(), Is.Zero);
        Assert.That(object.ReferenceEquals(first, names.GetOrAddName("urn:one", "item")), Is.False);
    }

    [Test]
    public void StableSortOrdersValuesAndPreservesEqualInputOrder()
    {
        StableItem[] items =
        [
            new StableItem(2, "a"),
            new StableItem(1, "b"),
            new StableItem(2, "c"),
            new StableItem(1, "d"),
            new StableItem(2, "e")
        ];
        object[] values = items;

        StableSort.SortObjects(values, new StableItemComparer());

        Assert.That(((StableItem)values[0]).Label, Is.EqualTo("b"));
        Assert.That(((StableItem)values[1]).Label, Is.EqualTo("d"));
        Assert.That(((StableItem)values[2]).Label, Is.EqualTo("a"));
        Assert.That(((StableItem)values[3]).Label, Is.EqualTo("c"));
        Assert.That(((StableItem)values[4]).Label, Is.EqualTo("e"));
    }

    [Test]
    public void StableSortHandlesEmptyAndSingleElementInputs()
    {
        StableItemComparer comparer = new StableItemComparer();
        object[] empty = [];
        object[] single = [new StableItem(1, "only")];

        StableSort.SortObjects(empty, comparer);
        StableSort.SortObjects(single, comparer);

        Assert.That(empty, Is.Empty);
        Assert.That(((StableItem)single[0]).Label, Is.EqualTo("only"));
    }

    [Test]
    public void UriToolsCreatesAndResolvesAbsoluteUris()
    {
        Uri baseUri = UriTools.CreateAbsolute("https://example.com/a/b/document.xml");

        Uri resolved = UriTools.ResolveReference(baseUri, "../image.svg?size=2#view");

        Assert.That(resolved.AbsoluteUri, Is.EqualTo("https://example.com/a/image.svg?size=2#view"));
        Assert.Throws<UriFormatException>(() => UriTools.CreateAbsolute("relative/path"));
        Assert.Throws<ArgumentException>(() => UriTools.ResolveReference(new Uri("relative", UriKind.Relative), "child"));
    }

    [Test]
    public void UriToolsHandlesFragmentsAndCanonicalResourceKeys()
    {
        const string value = "https://EXAMPLE.com:443/a/../b?q=1#fragment";

        Assert.That(UriTools.HasFragment(value), Is.True);
        Assert.That(UriTools.RemoveFragment(value), Is.EqualTo("https://EXAMPLE.com:443/a/../b?q=1"));
        Assert.That(UriTools.RemoveFragment("urn:test"), Is.EqualTo("urn:test"));
        Assert.That(UriTools.ResourceKey(UriTools.CreateAbsolute(value)), Is.EqualTo("https://example.com/b?q=1"));
    }

    [Test]
    public void ObjectPoolReusesReturnedItemsAndHonorsCapacity()
    {
        CountingFactory factory = new CountingFactory();
        ObjectPool pool = new ObjectPool(factory, 1);
        object first = pool.RentItem();
        object second = pool.RentItem();

        Assert.That(factory.Created, Is.EqualTo(2));
        Assert.That(pool.ReturnItem(first), Is.True);
        Assert.That(pool.ReturnItem(first), Is.False);
        Assert.That(pool.ReturnItem(second), Is.False);
        Assert.That(object.ReferenceEquals(pool.RentItem(), first), Is.True);
        Assert.That(pool.PoolCount(), Is.Zero);
    }

    [Test]
    public void ObjectPoolCanBeClearedAndCanDisableRetention()
    {
        CountingFactory factory = new CountingFactory();
        ObjectPool pool = new ObjectPool(factory, 1);
        object item = pool.RentItem();
        pool.ReturnItem(item);

        pool.ClearPool();

        Assert.That(pool.PoolCount(), Is.Zero);
        ObjectPool noRetention = new ObjectPool(factory, 0);
        Assert.That(noRetention.ReturnItem(new object()), Is.False);
    }

    [Test]
    public void ObjectPoolRejectsInvalidFactoriesAndFactoryResults()
    {
        Assert.Throws<ArgumentNullException>(() => new ObjectPool(null!, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ObjectPool(new CountingFactory(), -1));
        ObjectPool pool = new ObjectPool(new NullFactory(), 1);
        Assert.Throws<InvalidOperationException>(() => pool.RentItem());
    }

    private sealed class StableItem
    {
        internal int Key { get; }
        internal string Label { get; }

        internal StableItem(int key, string label)
        {
            Key = key;
            Label = label;
        }
    }

    private sealed class StableItemComparer : IComparer
    {
        public int Compare(object? left, object? right)
        {
            return ((StableItem)left!).Key.CompareTo(((StableItem)right!).Key);
        }
    }

    private sealed class CountingFactory : IObjectPoolFactory
    {
        internal int Created;

        public object CreatePoolItem()
        {
            Created++;
            return new object();
        }
    }

    private sealed class NullFactory : IObjectPoolFactory
    {
        public object CreatePoolItem()
        {
            return null!;
        }
    }
}
