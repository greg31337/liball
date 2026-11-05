using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class ObjectIdGeneratorTests
{
    [Test]
    public void FromParts_ComposesTimestampProcessUniqueBytesAndCounter()
    {
        byte[] processUnique = new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55 };

        ObjectIdValue value = ObjectIdGenerator.FromParts(1600000000, processUnique, 66051);

        Assert.That(value.ToBytes(), Is.EqualTo(new byte[] { 0x5F, 0x5E, 0x10, 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x01, 0x02, 0x03 }));
        Assert.That(value.ToHexString(), Is.EqualTo("5f5e10001122334455010203"));
        Assert.That(value.UnixSeconds(), Is.EqualTo(1600000000));
    }

    [Test]
    public void Generator_UsesStableProcessUniqueBytesAndIncrementsCounter()
    {
        byte[] processUnique = new byte[] { 1, 2, 3, 4, 5 };
        ObjectIdGenerator generator = ObjectIdGenerator.Create(processUnique, 7);

        ObjectIdValue first = generator.NextAtUnixSeconds(1600000000);
        ObjectIdValue second = generator.NextAtUnixSeconds(1600000000);

        Assert.That(first.UnixSeconds(), Is.EqualTo(1600000000));
        Assert.That(second.UnixSeconds(), Is.EqualTo(1600000000));
        Assert.That(first.ToBytes(), Is.EqualTo(new byte[] { 0x5F, 0x5E, 0x10, 0x00, 1, 2, 3, 4, 5, 0, 0, 7 }));
        Assert.That(second.ToBytes(), Is.EqualTo(new byte[] { 0x5F, 0x5E, 0x10, 0x00, 1, 2, 3, 4, 5, 0, 0, 8 }));
        Assert.That(second.ToHexString(), Is.Not.EqualTo(first.ToHexString()));
    }

    [Test]
    public void Generator_RollsCounterOverAfterMaximumUInt24Value()
    {
        byte[] processUnique = new byte[] { 1, 2, 3, 4, 5 };
        ObjectIdGenerator generator = ObjectIdGenerator.Create(processUnique, ObjectIdLimits.MaxCounter - 1);

        ObjectIdValue beforeMax = generator.NextAtUnixSeconds(1600000000);
        ObjectIdValue max = generator.NextAtUnixSeconds(1600000000);
        ObjectIdValue rolled = generator.NextAtUnixSeconds(1600000000);

        Assert.That(beforeMax.ToHexString(), Does.EndWith("fffffe"));
        Assert.That(max.ToHexString(), Does.EndWith("ffffff"));
        Assert.That(rolled.ToHexString(), Does.EndWith("000000"));
        Assert.That(rolled.UnixSeconds(), Is.EqualTo(1600000000));
    }

    [Test]
    public void Generator_ContinuesIncrementingAfterCounterRollover()
    {
        byte[] processUnique = new byte[] { 1, 2, 3, 4, 5 };
        ObjectIdGenerator generator = ObjectIdGenerator.Create(processUnique, ObjectIdLimits.MaxCounter);

        ObjectIdValue max = generator.NextAtUnixSeconds(1600000000);
        ObjectIdValue rolled = generator.NextAtUnixSeconds(1600000001);
        ObjectIdValue afterRolled = generator.NextAtUnixSeconds(1600000001);

        Assert.That(max.ToHexString(), Does.EndWith("ffffff"));
        Assert.That(rolled.ToHexString(), Does.EndWith("000000"));
        Assert.That(afterRolled.ToHexString(), Does.EndWith("000001"));
        Assert.That(rolled.UnixSeconds(), Is.EqualTo(1600000001));
        Assert.That(afterRolled.UnixSeconds(), Is.EqualTo(1600000001));
    }

    [Test]
    public void NextAt_UsesDateTimeOffsetTimestamp()
    {
        byte[] processUnique = new byte[] { 1, 2, 3, 4, 5 };
        ObjectIdGenerator generator = ObjectIdGenerator.Create(processUnique, 7);
        DateTimeOffset time = DateTimeOffset.FromUnixTimeSeconds(1600000000);

        ObjectIdValue value = generator.NextAt(time);

        Assert.That(value.UnixSeconds(), Is.EqualTo(1600000000));
        Assert.That(value.Time(), Is.EqualTo(time));
        Assert.That(value.ToBytes(), Is.EqualTo(new byte[] { 0x5F, 0x5E, 0x10, 0x00, 1, 2, 3, 4, 5, 0, 0, 7 }));
    }

    [Test]
    public void ObjectIdFacade_GeneratesParsableObjectIds()
    {
        ObjectIdValue value = new ObjectId().NewAtUnixSeconds(1600000000);
        string text = new ObjectId().Format(value);
        ObjectIdParseResult parsed = new ObjectId().Parse(text);

        Assert.That(value.UnixSeconds(), Is.EqualTo(1600000000));
        Assert.That(text.Length, Is.EqualTo(24));
        Assert.That(parsed.Ok, Is.True);
        Assert.That(parsed.Value, Is.Not.Null);
        Assert.That(parsed.Value.EqualsValue(value), Is.True);
    }

    [Test]
    public void RandomGenerator_GeneratesTwelveByteValueWithRequestedTimestamp()
    {
        ObjectIdGenerator generator = ObjectIdGenerator.Create();

        ObjectIdValue value = generator.NextAtUnixSeconds(1600000000);

        Assert.That(value.ToBytes().Length, Is.EqualTo(12));
        Assert.That(value.UnixSeconds(), Is.EqualTo(1600000000));
    }

    [Test]
    public void Generator_RejectsInvalidParts()
    {
        byte[] processUnique = new byte[] { 1, 2, 3, 4, 5 };

        Assert.That(() => ObjectIdGenerator.Create(null, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => ObjectIdGenerator.Create(new byte[4], 0), Throws.ArgumentException);
        Assert.That(() => ObjectIdGenerator.Create(processUnique, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => ObjectIdGenerator.Create(processUnique, ObjectIdLimits.MaxCounter + 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => ObjectIdGenerator.FromParts(-1, processUnique, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => ObjectIdGenerator.FromParts(ObjectIdLimits.MaxUnixSeconds + 1, processUnique, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => ObjectIdGenerator.FromParts(0, new byte[4], 0), Throws.ArgumentException);
        Assert.That(() => ObjectIdGenerator.FromParts(0, processUnique, ObjectIdLimits.MaxCounter + 1), Throws.TypeOf<ArgumentOutOfRangeException>());
    }
}
