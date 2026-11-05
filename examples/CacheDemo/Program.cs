using System.Globalization;
using cache;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
var store = new CacheBuilder()
    .WithMaximumEntries(2)
    .WithEvictionPolicy(CacheEvictionPolicy.Lru)
    .WithStats()
    .Build();
Console.WriteLine("Two-entry least-recently-used cache");
if (!store.Put("tea", 2.25m).Written || !store.Put("coffee", 3.50m).Written)
{
    throw new InvalidOperationException("Could not populate the cache.");
}

// Access tea so coffee becomes the eviction candidate.
var tea = store.Get("tea");
Console.WriteLine($"Read tea: {tea.Value} (found={tea.Found})");
if (!store.Put("cocoa", 4.00m).Written)
{
    throw new InvalidOperationException("Could not cache cocoa.");
}

Console.WriteLine("Added cocoa; the capacity stays at two.");
Console.WriteLine($"Tea retained:    {store.Peek("tea").Found}");
Console.WriteLine($"Coffee evicted:  {!store.Get("coffee").Found}");
Console.WriteLine($"Cocoa retained:  {store.Peek("cocoa").Found}");
var stats = store.Stats();
Console.WriteLine(
    $"Entries={stats.CurrentSize}, hits={stats.Hits}, misses={stats.Misses}, evictions={stats.Evictions}");
store.Stop();

// A loader fills a missing entry once; subsequent reads reuse the cached value.
int loads = 0;
var loading = new CacheBuilder().WithLoader(key =>
{
    loads++;
    return $"Product {key}";
}).Build();
Console.WriteLine();
Console.WriteLine($"First load:  {loading.GetOrLoad("A42").Value}");
Console.WriteLine($"Cached load: {loading.GetOrLoad("A42").Value}");
Console.WriteLine($"Loader calls: {loads}");
loading.Stop();
