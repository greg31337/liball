using System.Reflection;
using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CacheEntryTests
{
    [Test]
    public void CacheEntry_IsAssemblyInternalMetadataType()
    {
        Type type = EntryType();

        Assert.That(type.IsPublic, Is.False);
        Assert.That(type.IsClass, Is.True);
    }

    [Test]
    public void Constructor_DefaultsToEmptyLiveEntryMetadata()
    {
        object entry = NewEntry();

        Assert.That(Field(entry, "Key").GetValue(entry), Is.Null);
        Assert.That(Field(entry, "Value").GetValue(entry), Is.Null);
        Assert.That(Field(entry, "CreatedUtcTicks").GetValue(entry), Is.EqualTo(0L));
        Assert.That(Field(entry, "LastAccessUtcTicks").GetValue(entry), Is.EqualTo(0L));
        Assert.That(Field(entry, "LastWriteUtcTicks").GetValue(entry), Is.EqualTo(0L));
        Assert.That(Field(entry, "HasAbsoluteExpiration").GetValue(entry), Is.EqualTo(false));
        Assert.That(Field(entry, "AbsoluteExpirationUtcTicks").GetValue(entry), Is.EqualTo(0L));
        Assert.That(Field(entry, "HasSlidingExpiration").GetValue(entry), Is.EqualTo(false));
        Assert.That(Field(entry, "SlidingExpirationTicks").GetValue(entry), Is.EqualTo(0L));
        Assert.That(Field(entry, "Weight").GetValue(entry), Is.EqualTo(1L));
        Assert.That(Field(entry, "Priority").GetValue(entry), Is.EqualTo(CacheEntryPriority.Normal));
        Assert.That(Field(entry, "Version").GetValue(entry), Is.EqualTo(0L));
        Assert.That(Field(entry, "RemovalReason").GetValue(entry), Is.EqualTo(CacheRemovalReason.Unknown));
        Assert.That(Field(entry, "AccessCount").GetValue(entry), Is.EqualTo(0L));
    }

    [Test]
    public void InternalFields_CanRepresentStoredEntryMetadata()
    {
        object entry = NewEntry();

        Field(entry, "Key").SetValue(entry, "a");
        Field(entry, "Value").SetValue(entry, "b");
        Field(entry, "CreatedUtcTicks").SetValue(entry, 10L);
        Field(entry, "LastAccessUtcTicks").SetValue(entry, 20L);
        Field(entry, "LastWriteUtcTicks").SetValue(entry, 30L);
        Field(entry, "HasAbsoluteExpiration").SetValue(entry, true);
        Field(entry, "AbsoluteExpirationUtcTicks").SetValue(entry, 40L);
        Field(entry, "HasSlidingExpiration").SetValue(entry, true);
        Field(entry, "SlidingExpirationTicks").SetValue(entry, 50L);
        Field(entry, "Weight").SetValue(entry, 60L);
        Field(entry, "Priority").SetValue(entry, CacheEntryPriority.High);
        Field(entry, "Version").SetValue(entry, 70L);
        Field(entry, "RemovalReason").SetValue(entry, CacheRemovalReason.Replace);
        Field(entry, "AccessCount").SetValue(entry, 80L);

        Assert.That(Field(entry, "Key").GetValue(entry), Is.EqualTo("a"));
        Assert.That(Field(entry, "Value").GetValue(entry), Is.EqualTo("b"));
        Assert.That(Field(entry, "CreatedUtcTicks").GetValue(entry), Is.EqualTo(10L));
        Assert.That(Field(entry, "LastAccessUtcTicks").GetValue(entry), Is.EqualTo(20L));
        Assert.That(Field(entry, "LastWriteUtcTicks").GetValue(entry), Is.EqualTo(30L));
        Assert.That(Field(entry, "HasAbsoluteExpiration").GetValue(entry), Is.EqualTo(true));
        Assert.That(Field(entry, "AbsoluteExpirationUtcTicks").GetValue(entry), Is.EqualTo(40L));
        Assert.That(Field(entry, "HasSlidingExpiration").GetValue(entry), Is.EqualTo(true));
        Assert.That(Field(entry, "SlidingExpirationTicks").GetValue(entry), Is.EqualTo(50L));
        Assert.That(Field(entry, "Weight").GetValue(entry), Is.EqualTo(60L));
        Assert.That(Field(entry, "Priority").GetValue(entry), Is.EqualTo(CacheEntryPriority.High));
        Assert.That(Field(entry, "Version").GetValue(entry), Is.EqualTo(70L));
        Assert.That(Field(entry, "RemovalReason").GetValue(entry), Is.EqualTo(CacheRemovalReason.Replace));
        Assert.That(Field(entry, "AccessCount").GetValue(entry), Is.EqualTo(80L));
    }

    static Type EntryType()
    {
        Type? type = typeof(CacheOptions).Assembly.GetType("cache.CacheEntry");
        Assert.That(type, Is.Not.Null);
        return type!;
    }

    static object NewEntry()
    {
        object? entry = Activator.CreateInstance(EntryType(), true);
        Assert.That(entry, Is.Not.Null);
        return entry!;
    }

    static FieldInfo Field(object entry, string name)
    {
        FieldInfo? field = entry.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        Assert.That(field!.IsAssembly, Is.True);
        return field!;
    }
}
