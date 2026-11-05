using System;

namespace diameter.Tests;

public class NetworkByteOrderTests
{
    [Test]
    public void UnsignedValuesUseNetworkByteOrder()
    {
        byte[] value24 = new byte[] { 170, 170, 170, 170 };
        byte[] value32 = new byte[4];
        byte[] value64 = new byte[8];

        NetworkByteOrder.WriteUInt24(value24, 1193046U);
        NetworkByteOrder.WriteUInt32(value32, 2309737967U);
        NetworkByteOrder.WriteUInt64(value64, 81985529216486895UL);

        Assert.Multiple(() =>
        {
            Assert.That(value24, Is.EqualTo(new byte[] { 18, 52, 86, 170 }));
            Assert.That(value32, Is.EqualTo(new byte[] { 137, 171, 205, 239 }));
            Assert.That(value64, Is.EqualTo(new byte[] { 1, 35, 69, 103, 137, 171, 205, 239 }));
            Assert.That(NetworkByteOrder.ReadUInt24(value24), Is.EqualTo(1193046U));
            Assert.That(NetworkByteOrder.ReadUInt32(value32), Is.EqualTo(2309737967U));
            Assert.That(NetworkByteOrder.ReadUInt64(value64), Is.EqualTo(81985529216486895UL));
        });
    }

    [Test]
    public void SignedValuesUseTwosComplementNetworkByteOrder()
    {
        byte[] value24 = new byte[3];
        byte[] value32 = new byte[4];
        byte[] value64 = new byte[8];

        NetworkByteOrder.WriteInt24(value24, -2);
        NetworkByteOrder.WriteInt32(value32, -2);
        NetworkByteOrder.WriteInt64(value64, long.MinValue);

        Assert.Multiple(() =>
        {
            Assert.That(value24, Is.EqualTo(new byte[] { 255, 255, 254 }));
            Assert.That(value32, Is.EqualTo(new byte[] { 255, 255, 255, 254 }));
            Assert.That(value64, Is.EqualTo(new byte[] { 128, 0, 0, 0, 0, 0, 0, 0 }));
            Assert.That(NetworkByteOrder.ReadInt24(value24), Is.EqualTo(-2));
            Assert.That(NetworkByteOrder.ReadInt32(value32), Is.EqualTo(-2));
            Assert.That(NetworkByteOrder.ReadInt64(value64), Is.EqualTo(long.MinValue));
        });
    }

    [Test]
    public void EveryWidthRoundTripsBoundaryValues()
    {
        byte[] value24 = new byte[3];
        byte[] value32 = new byte[4];
        byte[] value64 = new byte[8];

        NetworkByteOrder.WriteUInt24(value24, 16777215U);
        Assert.That(NetworkByteOrder.ReadUInt24(value24), Is.EqualTo(16777215U));
        NetworkByteOrder.WriteInt24(value24, -8388608);
        Assert.That(NetworkByteOrder.ReadInt24(value24), Is.EqualTo(-8388608));
        NetworkByteOrder.WriteInt24(value24, 8388607);
        Assert.That(NetworkByteOrder.ReadInt24(value24), Is.EqualTo(8388607));

        NetworkByteOrder.WriteUInt32(value32, uint.MaxValue);
        Assert.That(NetworkByteOrder.ReadUInt32(value32), Is.EqualTo(uint.MaxValue));
        NetworkByteOrder.WriteInt32(value32, int.MinValue);
        Assert.That(NetworkByteOrder.ReadInt32(value32), Is.EqualTo(int.MinValue));
        NetworkByteOrder.WriteInt32(value32, int.MaxValue);
        Assert.That(NetworkByteOrder.ReadInt32(value32), Is.EqualTo(int.MaxValue));

        NetworkByteOrder.WriteUInt64(value64, ulong.MaxValue);
        Assert.That(NetworkByteOrder.ReadUInt64(value64), Is.EqualTo(ulong.MaxValue));
        NetworkByteOrder.WriteInt64(value64, long.MinValue);
        Assert.That(NetworkByteOrder.ReadInt64(value64), Is.EqualTo(long.MinValue));
        NetworkByteOrder.WriteInt64(value64, long.MaxValue);
        Assert.That(NetworkByteOrder.ReadInt64(value64), Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void WritesRejectValuesOutsideSignedAndUnsigned24BitRanges()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                NetworkByteOrder.WriteUInt24(new byte[3], 16777216U));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                NetworkByteOrder.WriteInt24(new byte[3], -8388609));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                NetworkByteOrder.WriteInt24(new byte[3], 8388608));
        });
    }

    [Test]
    public void ReadsAndWritesRejectShortBuffers()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => NetworkByteOrder.ReadUInt24(new byte[2]));
            Assert.Throws<ArgumentException>(() => NetworkByteOrder.ReadInt24(new byte[2]));
            Assert.Throws<ArgumentException>(() => NetworkByteOrder.ReadUInt32(new byte[3]));
            Assert.Throws<ArgumentException>(() => NetworkByteOrder.ReadInt32(new byte[3]));
            Assert.Throws<ArgumentException>(() => NetworkByteOrder.ReadUInt64(new byte[7]));
            Assert.Throws<ArgumentException>(() => NetworkByteOrder.ReadInt64(new byte[7]));
            Assert.Throws<ArgumentException>(() => NetworkByteOrder.WriteUInt24(new byte[2], 0U));
            Assert.Throws<ArgumentException>(() => NetworkByteOrder.WriteInt24(new byte[2], 0));
            Assert.Throws<ArgumentException>(() => NetworkByteOrder.WriteUInt32(new byte[3], 0U));
            Assert.Throws<ArgumentException>(() => NetworkByteOrder.WriteInt32(new byte[3], 0));
            Assert.Throws<ArgumentException>(() => NetworkByteOrder.WriteUInt64(new byte[7], 0UL));
            Assert.Throws<ArgumentException>(() => NetworkByteOrder.WriteInt64(new byte[7], 0L));
        });
    }
}
