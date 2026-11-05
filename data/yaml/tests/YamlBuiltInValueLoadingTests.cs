using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlBuiltInValueLoadingTests
{
    [Test]
    public void Deserialize_LoadsPrimitiveScalarTypes()
    {
        Assert.That(global::yaml.Yaml.Deserialize("'text'", typeof(string)), Is.EqualTo("text"));
        Assert.That(global::yaml.Yaml.Deserialize("true", typeof(bool)), Is.EqualTo(true));
        Assert.That(global::yaml.Yaml.Deserialize("7", typeof(byte)), Is.EqualTo((byte)7));
        Assert.That(global::yaml.Yaml.Deserialize("-7", typeof(sbyte)), Is.EqualTo((sbyte)-7));
        Assert.That(global::yaml.Yaml.Deserialize("-12", typeof(short)), Is.EqualTo((short)-12));
        Assert.That(global::yaml.Yaml.Deserialize("12", typeof(ushort)), Is.EqualTo((ushort)12));
        Assert.That(global::yaml.Yaml.Deserialize("-37", typeof(int)), Is.EqualTo(-37));
        Assert.That(global::yaml.Yaml.Deserialize("37", typeof(uint)), Is.EqualTo((uint)37));
        Assert.That(global::yaml.Yaml.Deserialize("-922", typeof(long)), Is.EqualTo(-922L));
        Assert.That(global::yaml.Yaml.Deserialize("922", typeof(ulong)), Is.EqualTo((ulong)922));
        Assert.That(global::yaml.Yaml.Deserialize("1.5", typeof(float)), Is.EqualTo(1.5f));
        Assert.That(global::yaml.Yaml.Deserialize("1.5", typeof(double)), Is.EqualTo(1.5d));
        Assert.That(global::yaml.Yaml.Deserialize("1.5", typeof(decimal)), Is.EqualTo(1.5m));
        Assert.That(global::yaml.Yaml.Deserialize("null", typeof(object)), Is.Null);
    }

    [Test]
    public void Deserialize_LoadsNullableValues()
    {
        object? missing = global::yaml.Yaml.Deserialize("null", typeof(string));
        object? present = global::yaml.Yaml.Deserialize("42", typeof(int));

        Assert.That(missing, Is.Null);
        Assert.That(present, Is.EqualTo(42));
    }

    [Test]
    public void Deserialize_LoadsArraysAndLists()
    {
        int[] array = (int[])global::yaml.Yaml.Deserialize("[1, 2, 3]", typeof(int[]))!;
        List<string> list = (List<string>)global::yaml.Yaml.Deserialize("- Ada\n- Grace\n", typeof(List<string>))!;

        Assert.That(array, Is.EqualTo(new int[] { 1, 2, 3 }));
        Assert.That(list, Is.EqualTo(new List<string> { "Ada", "Grace" }));
    }

    [Test]
    public void Deserialize_LoadsDictionaries()
    {
        Dictionary<string, int> value = (Dictionary<string, int>)global::yaml.Yaml.Deserialize("alpha: 1\nbravo: 2\n", typeof(Dictionary<string, int>))!;

        Assert.That(value["alpha"], Is.EqualTo(1));
        Assert.That(value["bravo"], Is.EqualTo(2));
    }

    [Test]
    public void Deserialize_LoadsDatesGuidsBytesAndEnums()
    {
        DateTime date = (DateTime)global::yaml.Yaml.Deserialize("'2026-08-01T12:30:00.0000000Z'", typeof(DateTime))!;
        DateTimeOffset offset = (DateTimeOffset)global::yaml.Yaml.Deserialize("'2026-08-01T12:30:00.0000000+00:00'", typeof(DateTimeOffset))!;
        Guid guid = (Guid)global::yaml.Yaml.Deserialize("7b7478e4-6c56-4e20-a81a-6b863f29c0f4", typeof(Guid))!;
        byte[] bytes = (byte[])global::yaml.Yaml.Deserialize("SGVsbG8=", typeof(byte[]))!;
        BuiltInRole role = (BuiltInRole)global::yaml.Yaml.Deserialize("Admin", typeof(BuiltInRole))!;

        Assert.That(date, Is.EqualTo(DateTime.Parse("2026-08-01T12:30:00.0000000Z", null, System.Globalization.DateTimeStyles.RoundtripKind)));
        Assert.That(offset, Is.EqualTo(DateTimeOffset.Parse("2026-08-01T12:30:00.0000000+00:00")));
        Assert.That(guid, Is.EqualTo(new Guid("7b7478e4-6c56-4e20-a81a-6b863f29c0f4")));
        Assert.That(bytes, Is.EqualTo(new byte[] { 72, 101, 108, 108, 111 }));
        Assert.That(role, Is.EqualTo(BuiltInRole.Admin));
    }

    [Test]
    public void Dump_EmitsBuiltInScalarValues()
    {
        string date = global::yaml.Yaml.Dump(new DateTime(2026, 8, 1, 12, 30, 0, DateTimeKind.Utc));
        string guid = global::yaml.Yaml.Dump(new Guid("7b7478e4-6c56-4e20-a81a-6b863f29c0f4"));
        string bytes = global::yaml.Yaml.Dump(new byte[] { 72, 101, 108, 108, 111 });
        string role = global::yaml.Yaml.Dump(BuiltInRole.Admin);
        string unsigned = global::yaml.Yaml.Dump(ulong.MaxValue);

        Assert.That(date, Is.EqualTo("2026-08-01T12:30:00.0000000Z\n"));
        Assert.That(guid, Is.EqualTo("7b7478e4-6c56-4e20-a81a-6b863f29c0f4\n"));
        Assert.That(bytes, Is.EqualTo("SGVsbG8=\n"));
        Assert.That(role, Is.EqualTo("Admin\n"));
        Assert.That(unsigned, Is.EqualTo("18446744073709551615\n"));
    }
}

public enum BuiltInRole
{
    Unknown = 0,
    Admin = 1,
    Reader = 2
}
