using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlTypeConverterTests
{
    [Test]
    public void Registry_RegistersFindsCopiesAndRemovesTypeConverters()
    {
        global::yaml.YamlTypeConverterRegistry registry = new global::yaml.YamlTypeConverterRegistry();
        global::yaml.YamlTypeConverterRegistration registration = registry.RegisterConverter(typeof(CustomPoint), ReadPoint, WritePoint);

        Assert.That(registry.Count(), Is.EqualTo(1));
        Assert.That(registration.CanRead(), Is.True);
        Assert.That(registration.CanWrite(), Is.True);
        Assert.That(registry.FindByType(typeof(CustomPoint)), Is.SameAs(registration));

        global::yaml.YamlTypeConverterRegistry copy = registry.Copy();

        Assert.That(copy, Is.Not.SameAs(registry));
        Assert.That(copy.Count(), Is.EqualTo(1));
        Assert.That(copy.FindByType(typeof(CustomPoint)), Is.Not.SameAs(registration));
        Assert.That(registry.RemoveType(typeof(CustomPoint)), Is.True);
        Assert.That(registry.Count(), Is.EqualTo(0));
    }

    [Test]
    public void Serialize_UsesCustomTypeConverterForRootValue()
    {
        global::yaml.YamlOptions options = OptionsWithPointConverter();
        CustomPoint point = new CustomPoint();
        point.X = 2;
        point.Y = 3;

        string text = global::yaml.Yaml.Serialize(point, options);

        Assert.That(text, Is.EqualTo("'2,3'\n"));
    }

    [Test]
    public void Serialize_UsesCustomTypeConverterForNestedPublicField()
    {
        global::yaml.YamlOptions options = OptionsWithPointConverter();
        ObjectWithCustomPoint value = new ObjectWithCustomPoint();
        value.Location.X = 5;
        value.Location.Y = 8;

        string text = global::yaml.Yaml.Serialize(value, options);

        Assert.That(text, Is.EqualTo("Location: '5,8'\n"));
    }

    [Test]
    public void Deserialize_UsesCustomTypeConverterForRootValue()
    {
        global::yaml.YamlOptions options = OptionsWithPointConverter();

        CustomPoint point = (CustomPoint)global::yaml.Yaml.Deserialize("'13,21'", typeof(CustomPoint), options)!;

        Assert.That(point.X, Is.EqualTo(13));
        Assert.That(point.Y, Is.EqualTo(21));
    }

    [Test]
    public void Deserialize_UsesCustomTypeConverterForNestedPublicField()
    {
        global::yaml.YamlOptions options = OptionsWithPointConverter();

        ObjectWithCustomPoint value = (ObjectWithCustomPoint)global::yaml.Yaml.Deserialize("Location: '34,55'\n", typeof(ObjectWithCustomPoint), options)!;

        Assert.That(value.Location.X, Is.EqualTo(34));
        Assert.That(value.Location.Y, Is.EqualTo(55));
    }

    [Test]
    public void SerializeAndDeserialize_UseInterfaceTypeConverter()
    {
        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        PointInterfaceConverter converter = new PointInterfaceConverter();
        options.Object.TypeConverters.RegisterConverterInstance(typeof(CustomPoint), converter);
        CustomPoint point = new CustomPoint();
        point.X = 8;
        point.Y = 13;

        string text = global::yaml.Yaml.Serialize(point, options);
        CustomPoint copy = (CustomPoint)global::yaml.Yaml.Deserialize(text, typeof(CustomPoint), options)!;

        Assert.That(text, Is.EqualTo("'8,13'\n"));
        Assert.That(copy.X, Is.EqualTo(8));
        Assert.That(copy.Y, Is.EqualTo(13));
        Assert.That(converter.ReadCount, Is.EqualTo(1));
        Assert.That(converter.WriteCount, Is.EqualTo(1));
    }

    public static global::yaml.YamlOptions OptionsWithPointConverter()
    {
        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        options.Object.TypeConverters.RegisterConverter(typeof(CustomPoint), ReadPoint, WritePoint);
        return options;
    }

    public static object ReadPoint(object value)
    {
        string text = global::yaml.Yaml.TextValue(value);
        string[] parts = text.Split(',');
        CustomPoint point = new CustomPoint();
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
        CustomPoint? point = value as CustomPoint;
        if (point == null)
        {
            return "";
        }

        return point.X.ToString() + "," + point.Y.ToString();
    }
}

public class CustomPoint
{
    public int X;
    public int Y;

    public CustomPoint()
    {
        X = 0;
        Y = 0;
    }
}

public class ObjectWithCustomPoint
{
    public CustomPoint Location;

    public ObjectWithCustomPoint()
    {
        Location = new CustomPoint();
    }
}

public class PointInterfaceConverter : global::yaml.IYamlTypeConverter
{
    public int ReadCount;
    public int WriteCount;

    public PointInterfaceConverter()
    {
        ReadCount = 0;
        WriteCount = 0;
    }

    public object Read(object value)
    {
        ReadCount = ReadCount + 1;
        return YamlTypeConverterTests.ReadPoint(value);
    }

    public object Write(object value)
    {
        WriteCount = WriteCount + 1;
        return YamlTypeConverterTests.WritePoint(value);
    }
}
