using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlObjectConversionTests
{
    [Test]
    public void ObjectConversion_RoundTripsPrimitiveCollectionAndBuiltInFields()
    {
        ObjectConversionRoot source = BuildRoot();

        string text = global::yaml.Yaml.Serialize(source);
        ObjectConversionRoot roundTrip = (ObjectConversionRoot)global::yaml.Yaml.Deserialize(text, typeof(ObjectConversionRoot))!;

        AssertRoot(roundTrip);
    }

    [Test]
    public void ObjectConversion_DeserializesStructuredYamlIntoPublicFields()
    {
        string text =
            "Name: Ada\n" +
            "Count: 37\n" +
            "Active: true\n" +
            "Ratio: 1.25\n" +
            "Tags:\n" +
            "  - compiler\n" +
            "  - math\n" +
            "Scores:\n" +
            "  - 3\n" +
            "  - 5\n" +
            "Limits:\n" +
            "  low: 1\n" +
            "  high: 9\n" +
            "Child:\n" +
            "  Label: nested\n" +
            "  Depth: 2\n" +
            "Role: Admin\n" +
            "CreatedAt: '2026-08-01T12:30:00.0000000Z'\n" +
            "UpdatedAt: '2026-08-01T12:30:00.0000000+00:00'\n" +
            "Id: '7b7478e4-6c56-4e20-a81a-6b863f29c0f4'\n" +
            "Data: 'AQIDBA=='\n";

        ObjectConversionRoot value = (ObjectConversionRoot)global::yaml.Yaml.Deserialize(text, typeof(ObjectConversionRoot))!;

        AssertRoot(value);
    }

    [Test]
    public void ObjectConversion_AppliesNamingConventionToPublicFields()
    {
        ObjectConversionNaming source = new ObjectConversionNaming();
        source.FirstName = "Ada";
        source.LastLoginCount = 3;
        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        options.Object.NamingConvention = global::yaml.YamlNamingConvention.KebabCase;

        string text = global::yaml.Yaml.Serialize(source, options);
        ObjectConversionNaming roundTrip = (ObjectConversionNaming)global::yaml.Yaml.Deserialize(text, typeof(ObjectConversionNaming), options)!;

        Assert.That(text, Is.EqualTo("first-name: Ada\nlast-login-count: 3\n"));
        Assert.That(roundTrip.FirstName, Is.EqualTo("Ada"));
        Assert.That(roundTrip.LastLoginCount, Is.EqualTo(3));
    }

    [Test]
    public void ObjectConversion_AppliesCustomConvertersToPublicFields()
    {
        ObjectConversionWithPoint source = new ObjectConversionWithPoint();
        source.Name = "origin";
        source.Point.X = 8;
        source.Point.Y = 13;
        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        options.Object.TypeConverters.RegisterConverter(typeof(ObjectConversionPoint), ReadPoint, WritePoint);

        string text = global::yaml.Yaml.Serialize(source, options);
        ObjectConversionWithPoint roundTrip = (ObjectConversionWithPoint)global::yaml.Yaml.Deserialize(text, typeof(ObjectConversionWithPoint), options)!;

        Assert.That(text, Is.EqualTo("Name: origin\nPoint: '8,13'\n"));
        Assert.That(roundTrip.Name, Is.EqualTo("origin"));
        Assert.That(roundTrip.Point.X, Is.EqualTo(8));
        Assert.That(roundTrip.Point.Y, Is.EqualTo(13));
    }

    public static ObjectConversionRoot BuildRoot()
    {
        ObjectConversionRoot value = new ObjectConversionRoot();
        value.Name = "Ada";
        value.Count = 37;
        value.Active = true;
        value.Ratio = 1.25d;
        value.Tags.Add("compiler");
        value.Tags.Add("math");
        value.Scores = new int[] { 3, 5 };
        value.Limits["low"] = 1;
        value.Limits["high"] = 9;
        value.Child.Label = "nested";
        value.Child.Depth = 2;
        value.Role = ObjectConversionRole.Admin;
        value.CreatedAt = new DateTime(2026, 8, 1, 12, 30, 0, DateTimeKind.Utc);
        value.UpdatedAt = new DateTimeOffset(2026, 8, 1, 12, 30, 0, TimeSpan.Zero);
        value.Id = new Guid("7b7478e4-6c56-4e20-a81a-6b863f29c0f4");
        value.Data = new byte[] { 1, 2, 3, 4 };
        return value;
    }

    public static void AssertRoot(ObjectConversionRoot value)
    {
        Assert.That(value.Name, Is.EqualTo("Ada"));
        Assert.That(value.Count, Is.EqualTo(37));
        Assert.That(value.Active, Is.EqualTo(true));
        Assert.That(value.Ratio, Is.EqualTo(1.25d));
        Assert.That(value.Tags, Is.EqualTo(new List<string> { "compiler", "math" }));
        Assert.That(value.Scores, Is.EqualTo(new int[] { 3, 5 }));
        Assert.That(value.Limits["low"], Is.EqualTo(1));
        Assert.That(value.Limits["high"], Is.EqualTo(9));
        Assert.That(value.Child.Label, Is.EqualTo("nested"));
        Assert.That(value.Child.Depth, Is.EqualTo(2));
        Assert.That(value.Role, Is.EqualTo(ObjectConversionRole.Admin));
        Assert.That(value.CreatedAt, Is.EqualTo(new DateTime(2026, 8, 1, 12, 30, 0, DateTimeKind.Utc)));
        Assert.That(value.UpdatedAt, Is.EqualTo(new DateTimeOffset(2026, 8, 1, 12, 30, 0, TimeSpan.Zero)));
        Assert.That(value.Id, Is.EqualTo(new Guid("7b7478e4-6c56-4e20-a81a-6b863f29c0f4")));
        Assert.That(value.Data, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
    }

    public static object ReadPoint(object value)
    {
        string[] parts = global::yaml.Yaml.TextValue(value).Split(',');
        ObjectConversionPoint point = new ObjectConversionPoint();
        if (parts.Length > 0)
        {
            point.X = int.Parse(parts[0]);
        }

        if (parts.Length > 1)
        {
            point.Y = int.Parse(parts[1]);
        }

        return point;
    }

    public static object WritePoint(object value)
    {
        ObjectConversionPoint? point = value as ObjectConversionPoint;
        if (point == null)
        {
            return "";
        }

        return point.X.ToString() + "," + point.Y.ToString();
    }
}

public class ObjectConversionRoot
{
    public string Name;
    public int Count;
    public bool Active;
    public double Ratio;
    public List<string> Tags;
    public int[] Scores;
    public Dictionary<string, int> Limits;
    public ObjectConversionChild Child;
    public ObjectConversionRole Role;
    public DateTime CreatedAt;
    public DateTimeOffset UpdatedAt;
    public Guid Id;
    public byte[] Data;

    public ObjectConversionRoot()
    {
        Name = "";
        Count = 0;
        Active = false;
        Ratio = 0.0d;
        Tags = new List<string>();
        Scores = new int[0];
        Limits = new Dictionary<string, int>();
        Child = new ObjectConversionChild();
        Role = ObjectConversionRole.Unknown;
        CreatedAt = DateTime.MinValue;
        UpdatedAt = DateTimeOffset.MinValue;
        Id = Guid.Empty;
        Data = new byte[0];
    }
}

public class ObjectConversionChild
{
    public string Label;
    public int Depth;

    public ObjectConversionChild()
    {
        Label = "";
        Depth = 0;
    }
}

public enum ObjectConversionRole
{
    Unknown = 0,
    Admin = 1
}

public class ObjectConversionNaming
{
    public string FirstName;
    public int LastLoginCount;

    public ObjectConversionNaming()
    {
        FirstName = "";
        LastLoginCount = 0;
    }
}

public class ObjectConversionPoint
{
    public int X;
    public int Y;

    public ObjectConversionPoint()
    {
        X = 0;
        Y = 0;
    }
}

public class ObjectConversionWithPoint
{
    public string Name;
    public ObjectConversionPoint Point;

    public ObjectConversionWithPoint()
    {
        Name = "";
        Point = new ObjectConversionPoint();
    }
}
