using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class ObjectIdSpecificationTests
{
    [Test]
    public void MongoSpecificationTimestampVectors_AreUnsignedUInt32Seconds()
    {
        AssertTimestampVector(new byte[] { 0x00, 0x00, 0x00, 0x00 }, 0);
        AssertTimestampVector(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF }, 2147483647);
        AssertTimestampVector(new byte[] { 0x80, 0x00, 0x00, 0x00 }, 2147483648);
        AssertTimestampVector(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, 4294967295);
    }

    [Test]
    public void MongoSpecificationCounter_RollsOverFromUInt24MaxToZero()
    {
        byte[] processUnique = new byte[] { 1, 2, 3, 4, 5 };
        ObjectIdGenerator generator = ObjectIdGenerator.Create(processUnique, ObjectIdLimits.MaxCounter);

        ObjectIdValue max = generator.NextAtUnixSeconds(1600000000);
        ObjectIdValue rolled = generator.NextAtUnixSeconds(1600000000);

        Assert.That(max.ToHexString(), Is.EqualTo("5f5e10000102030405ffffff"));
        Assert.That(rolled.ToHexString(), Is.EqualTo("5f5e10000102030405000000"));
    }

    [Test]
    public void MongoSpecificationTimestampAndCounter_UseBigEndianByteOrder()
    {
        byte[] processUnique = new byte[] { 5, 6, 7, 8, 9 };

        ObjectIdValue value = ObjectIdGenerator.FromParts(16909060, processUnique, 658188);

        Assert.That(value.ToBytes(), Is.EqualTo(new byte[] { 0x01, 0x02, 0x03, 0x04, 5, 6, 7, 8, 9, 0x0A, 0x0B, 0x0C }));
        Assert.That(value.ToHexString(), Is.EqualTo("0102030405060708090a0b0c"));
    }

    [Test]
    public void MongoSpecificationBigEndianLayout_PreservesBinaryTimestampOrdering()
    {
        byte[] processUnique = new byte[] { 0, 0, 0, 0, 0 };
        ObjectIdValue low = ObjectIdGenerator.FromParts(1, processUnique, 0);
        ObjectIdValue high = ObjectIdGenerator.FromParts(256, processUnique, 0);

        Assert.That(low.CompareTo(high), Is.LessThan(0));
        Assert.That(low.ToHexString(), Is.EqualTo("000000010000000000000000"));
        Assert.That(high.ToHexString(), Is.EqualTo("000001000000000000000000"));
    }

    static void AssertTimestampVector(byte[] timestampBytes, long expectedUnixSeconds)
    {
        byte[] bytes = new byte[ObjectIdLimits.ByteLength];
        int index = 0;
        while (index < timestampBytes.Length)
        {
            bytes[index] = timestampBytes[index];
            index = index + 1;
        }

        ObjectIdValue value = new ObjectIdValue(bytes);

        Assert.That(value.UnixSeconds(), Is.EqualTo(expectedUnixSeconds));
        Assert.That(value.Time(), Is.EqualTo(DateTimeOffset.FromUnixTimeSeconds(expectedUnixSeconds)));
    }
}
