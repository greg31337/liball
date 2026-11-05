using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CacheEnumTests
{
    [Test]
    public void EvictionPolicy_ExposesPlannedPolicies()
    {
        Assert.That(Enum.IsDefined(typeof(CacheEvictionPolicy), CacheEvictionPolicy.Lru), Is.True);
        Assert.That(Enum.IsDefined(typeof(CacheEvictionPolicy), CacheEvictionPolicy.Fifo), Is.True);
        Assert.That(Enum.IsDefined(typeof(CacheEvictionPolicy), CacheEvictionPolicy.Lfu), Is.True);
        Assert.That(Enum.IsDefined(typeof(CacheEvictionPolicy), CacheEvictionPolicy.Random), Is.True);
        Assert.That(Enum.IsDefined(typeof(CacheEvictionPolicy), CacheEvictionPolicy.Segmented), Is.True);
    }

    [Test]
    public void EntryPriority_OrdersEvictionPreference()
    {
        Assert.That((int)CacheEntryPriority.Low, Is.LessThan((int)CacheEntryPriority.Normal));
        Assert.That((int)CacheEntryPriority.Normal, Is.LessThan((int)CacheEntryPriority.High));
    }

    [Test]
    public void RemovalReason_ExposesPlannedReasons()
    {
        Assert.That(Enum.IsDefined(typeof(CacheRemovalReason), CacheRemovalReason.Unknown), Is.True);
        Assert.That(Enum.IsDefined(typeof(CacheRemovalReason), CacheRemovalReason.ExplicitRemove), Is.True);
        Assert.That(Enum.IsDefined(typeof(CacheRemovalReason), CacheRemovalReason.Replace), Is.True);
        Assert.That(Enum.IsDefined(typeof(CacheRemovalReason), CacheRemovalReason.Expired), Is.True);
        Assert.That(Enum.IsDefined(typeof(CacheRemovalReason), CacheRemovalReason.Capacity), Is.True);
        Assert.That(Enum.IsDefined(typeof(CacheRemovalReason), CacheRemovalReason.Clear), Is.True);
        Assert.That(Enum.IsDefined(typeof(CacheRemovalReason), CacheRemovalReason.Rejected), Is.True);
    }

    [Test]
    public void ResultStatus_ExposesCommonOperationOutcomes()
    {
        Assert.That(Enum.IsDefined(typeof(CacheResultStatus), CacheResultStatus.Hit), Is.True);
        Assert.That(Enum.IsDefined(typeof(CacheResultStatus), CacheResultStatus.Miss), Is.True);
        Assert.That(Enum.IsDefined(typeof(CacheResultStatus), CacheResultStatus.Expired), Is.True);
        Assert.That(Enum.IsDefined(typeof(CacheResultStatus), CacheResultStatus.Loaded), Is.True);
        Assert.That(Enum.IsDefined(typeof(CacheResultStatus), CacheResultStatus.Written), Is.True);
        Assert.That(Enum.IsDefined(typeof(CacheResultStatus), CacheResultStatus.Replaced), Is.True);
        Assert.That(Enum.IsDefined(typeof(CacheResultStatus), CacheResultStatus.Removed), Is.True);
        Assert.That(Enum.IsDefined(typeof(CacheResultStatus), CacheResultStatus.Rejected), Is.True);
        Assert.That(Enum.IsDefined(typeof(CacheResultStatus), CacheResultStatus.Cleared), Is.True);
    }
}
