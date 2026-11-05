using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class EnumTests
{
    [Test]
    public void IdentifierKind_DefinesExpectedStableValues()
    {
        Assert.That((int)IdentifierKind.Unknown, Is.EqualTo(0));
        Assert.That((int)IdentifierKind.Uuid, Is.EqualTo(1));
        Assert.That((int)IdentifierKind.Ulid, Is.EqualTo(2));
        Assert.That((int)IdentifierKind.Ksuid, Is.EqualTo(3));
        Assert.That((int)IdentifierKind.Snowflake, Is.EqualTo(4));
        Assert.That((int)IdentifierKind.ObjectId, Is.EqualTo(5));
        Assert.That((int)IdentifierKind.NanoId, Is.EqualTo(6));
    }

    [Test]
    public void UuidVersion_DefinesKnownRfc9562Versions()
    {
        Assert.That((int)UuidVersion.Unknown, Is.EqualTo(0));
        Assert.That((int)UuidVersion.Version1, Is.EqualTo(1));
        Assert.That((int)UuidVersion.Version3, Is.EqualTo(3));
        Assert.That((int)UuidVersion.Version4, Is.EqualTo(4));
        Assert.That((int)UuidVersion.Version5, Is.EqualTo(5));
        Assert.That((int)UuidVersion.Version6, Is.EqualTo(6));
        Assert.That((int)UuidVersion.Version7, Is.EqualTo(7));
        Assert.That((int)UuidVersion.Version8, Is.EqualTo(8));
    }

    [Test]
    public void UuidVariant_DefinesExpectedCategories()
    {
        Assert.That((int)UuidVariant.Unknown, Is.EqualTo(0));
        Assert.That((int)UuidVariant.Ncs, Is.EqualTo(1));
        Assert.That((int)UuidVariant.Rfc9562, Is.EqualTo(2));
        Assert.That((int)UuidVariant.Microsoft, Is.EqualTo(3));
        Assert.That((int)UuidVariant.Future, Is.EqualTo(4));
    }

    [Test]
    public void SnowflakeClockRollbackBehavior_DefinesAvailableBehaviors()
    {
        Assert.That((int)SnowflakeClockRollbackBehavior.FailFast, Is.EqualTo(0));
        Assert.That((int)SnowflakeClockRollbackBehavior.Wait, Is.EqualTo(1));
    }
}
