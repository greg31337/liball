using System.Reflection;
using icc;

namespace icc.Tests;

public class BinaryCursorTests
{
    [Test]
    public void BigEndianReaderDecodesSignedAndUnsignedIntegers()
    {
        byte[] encoded =
        {
            0x7f,
            0x80,
            0x12, 0x34,
            0xff, 0xfe,
            0x89, 0xab, 0xcd, 0xef,
            0xff, 0xff, 0xff, 0xfd,
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xfc
        };
        object reader = CreateInternal("icc.BigEndianReader", new ReadOnlyMemory<byte>(encoded), 100L);

        byte unsigned8 = Method<Func<byte>>(reader, "ReadUInt8")();
        sbyte signed8 = Method<Func<sbyte>>(reader, "ReadInt8")();
        ushort unsigned16 = Method<Func<ushort>>(reader, "ReadUInt16")();
        short signed16 = Method<Func<short>>(reader, "ReadInt16")();
        uint unsigned32 = Method<Func<uint>>(reader, "ReadUInt32")();
        int signed32 = Method<Func<int>>(reader, "ReadInt32")();
        ulong unsigned64 = Method<Func<ulong>>(reader, "ReadUInt64")();
        long signed64 = Method<Func<long>>(reader, "ReadInt64")();

        Assert.Multiple(() =>
        {
            Assert.That(unsigned8, Is.EqualTo(0x7f));
            Assert.That(signed8, Is.EqualTo(-128));
            Assert.That(unsigned16, Is.EqualTo(0x1234));
            Assert.That(signed16, Is.EqualTo(-2));
            Assert.That(unsigned32, Is.EqualTo(0x89abcdef));
            Assert.That(signed32, Is.EqualTo(-3));
            Assert.That(unsigned64, Is.EqualTo(0x0102030405060708));
            Assert.That(signed64, Is.EqualTo(-4));
            Assert.That(Method<Func<int>>(reader, "GetReadPosition")(), Is.EqualTo(30));
            Assert.That(Method<Func<long>>(reader, "GetAbsoluteOffset")(), Is.EqualTo(130));
            Assert.That(Method<Func<int>>(reader, "GetRemainingCount")(), Is.Zero);
        });
    }

    [Test]
    public void BigEndianWriterProducesExactBytesAndTracksOffsets()
    {
        byte[] destination = new byte[30];
        object writer = CreateInternal("icc.BigEndianWriter", new Memory<byte>(destination), 100L);

        Method<Action<byte>>(writer, "WriteUInt8")(0x7f);
        Method<Action<sbyte>>(writer, "WriteInt8")(-128);
        Method<Action<ushort>>(writer, "WriteUInt16")(0x1234);
        Method<Action<short>>(writer, "WriteInt16")(-2);
        Method<Action<uint>>(writer, "WriteUInt32")(0x89abcdef);
        Method<Action<int>>(writer, "WriteInt32")(-3);
        Method<Action<ulong>>(writer, "WriteUInt64")(0x0102030405060708);
        Method<Action<long>>(writer, "WriteInt64")(-4);

        byte[] expected =
        {
            0x7f,
            0x80,
            0x12, 0x34,
            0xff, 0xfe,
            0x89, 0xab, 0xcd, 0xef,
            0xff, 0xff, 0xff, 0xfd,
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xfc
        };

        Assert.Multiple(() =>
        {
            Assert.That(destination, Is.EqualTo(expected));
            Assert.That(Method<Func<int>>(writer, "GetWritePosition")(), Is.EqualTo(30));
            Assert.That(Method<Func<long>>(writer, "GetAbsoluteOffset")(), Is.EqualTo(130));
            Assert.That(Method<Func<int>>(writer, "GetRemainingCount")(), Is.Zero);
        });
    }

    [Test]
    public void MemorySlicesSkipsAndAlignmentRemainBounded()
    {
        byte[] source = { 1, 2, 3, 4, 5, 6, 7, 8 };
        object reader = CreateInternal("icc.BigEndianReader", new ReadOnlyMemory<byte>(source), 500L);
        Method<Action<int>>(reader, "SkipExactBytes")(2);
        ReadOnlyMemory<byte> slice = Method<Func<int, ReadOnlyMemory<byte>>>(reader, "ReadExactSlice")(2);
        Method<Action<int>>(reader, "SkipExactBytes")(1);
        Method<Action<int>>(reader, "AlignReadPosition")(4);

        byte[] destination = { 9, 9, 9, 9, 9, 9, 9, 9 };
        object writer = CreateInternal("icc.BigEndianWriter", new Memory<byte>(destination), 700L);
        Method<Action<byte[]>>(writer, "WriteExactBytes")(new byte[] { 1, 2, 3 });
        Method<Action<int>>(writer, "AlignWritePosition")(4);
        Method<Action<byte>>(writer, "WriteUInt8")(4);

        Assert.Multiple(() =>
        {
            Assert.That(slice.ToArray(), Is.EqualTo(new byte[] { 3, 4 }));
            Assert.That(Method<Func<int>>(reader, "GetReadPosition")(), Is.EqualTo(8));
            Assert.That(Method<Func<long>>(reader, "GetAbsoluteOffset")(), Is.EqualTo(508));
            Assert.That(destination, Is.EqualTo(new byte[] { 1, 2, 3, 0, 4, 9, 9, 9 }));
            Assert.That(Method<Func<long>>(writer, "GetAbsoluteOffset")(), Is.EqualTo(705));
        });
    }

    [Test]
    public void FailedMemoryReadsAndWritesDoNotAdvanceTheirCursors()
    {
        object reader = CreateInternal("icc.BigEndianReader", new ReadOnlyMemory<byte>(new byte[3]), 0L);
        object writer = CreateInternal("icc.BigEndianWriter", new Memory<byte>(new byte[6]), 0L);
        Func<uint> readUInt32 = Method<Func<uint>>(reader, "ReadUInt32");
        Action<ulong> writeUInt64 = Method<Action<ulong>>(writer, "WriteUInt64");

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileFormatException>(() => readUInt32());
            Assert.Throws<IccProfileWriteException>(() => writeUInt64(1));
            Assert.That(Method<Func<int>>(reader, "GetReadPosition")(), Is.Zero);
            Assert.That(Method<Func<int>>(writer, "GetWritePosition")(), Is.Zero);
        });
    }

    private static object CreateInternal(string typeName, params object[] arguments)
    {
        Type type = typeof(IccProfile).Assembly.GetType(typeName)
            ?? throw new InvalidOperationException($"{typeName} was not found in the production assembly.");
        return Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.NonPublic, null, arguments, null)
            ?? throw new InvalidOperationException($"{typeName} could not be created.");
    }

    private static MethodInfo InternalMethod(object instance, string name)
    {
        return instance.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"{instance.GetType().Name}.{name} was not found.");
    }

    private static T Method<T>(object instance, string name) where T : Delegate
    {
        return InternalMethod(instance, name).CreateDelegate<T>(instance);
    }
}
