using System.Reflection;
using System.Text.Json;
using geo;

namespace geo.Tests;

public class PublicApiContractCompletenessTests
{
    [Test]
    public void EveryPublicInterfaceMethodHasAConcreteDispatchTarget()
    {
        Assembly assembly = typeof(Geometry).Assembly;
        Type[] interfaces = assembly.GetTypes()
            .Where(type => type.IsPublic && type.IsInterface &&
                type.Namespace == "geo")
            .OrderBy(type => type.Name)
            .ToArray();
        Type[] implementations = assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract)
            .Append(typeof(GeometryDataProviderFixture))
            .ToArray();
        int methodCount = 0;

        Assert.That(interfaces, Has.Length.EqualTo(24));
        foreach (Type contract in interfaces)
        {
            Type[] targets = implementations
                .Where(contract.IsAssignableFrom)
                .ToArray();
            Assert.That(targets, Is.Not.Empty,
                contract.Name + " has no concrete dispatch target");

            MethodInfo[] methods = contract.GetMethods();
            Assert.That(methods, Is.Not.Empty,
                contract.Name + " has no operations");
            foreach (MethodInfo method in methods)
            {
                bool mapped = targets.Any(target =>
                    MapsMethod(target, contract, method));
                Assert.That(mapped, Is.True,
                    contract.Name + "." + method.Name +
                    " has no concrete dispatch target");
                methodCount++;
            }
        }

        Assert.That(methodCount, Is.EqualTo(310),
            "The enumerated public operation contract changed");
    }

    [Test]
    public void ConsumerImplementedDataProviderExercisesEveryGetter()
    {
        IGeometryDataProvider provider =
            new GeometryDataProviderFixture();
        object[] first = ReadAllProviderValues(provider);
        object[] second = ReadAllProviderValues(provider);
        Type[] expectedTypes = ProviderValueTypes();

        Assert.That(first, Has.Length.EqualTo(29));
        Assert.That(second, Has.Length.EqualTo(first.Length));
        Assert.That(expectedTypes, Has.Length.EqualTo(first.Length));
        int index = 0;
        while (index < first.Length)
        {
            Assert.Multiple(() =>
            {
                Assert.That(first[index], Is.TypeOf(expectedTypes[index]),
                    "provider value " + index);
                Assert.That(second[index], Is.TypeOf(expectedTypes[index]),
                    "provider value " + index);
                Assert.That(second[index], Is.Not.SameAs(first[index]),
                    "provider values must be owning values at " + index);
            });
            index++;
        }
    }

    [Test]
    public void EveryPublicDataFieldStoresItsDeclaredValue()
    {
        Assembly assembly = typeof(Geometry).Assembly;
        Type[] dataTypes = assembly.GetTypes()
            .Where(type => type.IsPublic && type.IsClass &&
                type.Namespace == "geo" &&
                PublicInstanceFields(type).Length > 0)
            .OrderBy(type => type.Name)
            .ToArray();
        int fieldCount = 0;

        Assert.That(dataTypes, Has.Length.EqualTo(50));
        foreach (Type dataType in dataTypes)
        {
            object instance = Activator.CreateInstance(dataType)!;
            FieldInfo[] fields = PublicInstanceFields(dataType);
            foreach (FieldInfo field in fields)
            {
                object value = SampleValue(field.FieldType);
                field.SetValue(instance, value);
                object? stored = field.GetValue(instance);
                if (field.FieldType.IsValueType ||
                    field.FieldType == typeof(string))
                {
                    Assert.That(stored, Is.EqualTo(value),
                        dataType.Name + "." + field.Name);
                }
                else
                {
                    Assert.That(stored, Is.SameAs(value),
                        dataType.Name + "." + field.Name);
                }
                fieldCount++;
            }
        }

        Assert.That(fieldCount, Is.EqualTo(194),
            "The enumerated public data contract changed");
    }

    [Test]
    public void EveryPublicEnumValueRoundTripsByItsContractName()
    {
        Type[] enumTypes = typeof(Geometry).Assembly.GetTypes()
            .Where(type => type.IsPublic && type.IsEnum &&
                type.Namespace == "geo")
            .OrderBy(type => type.Name)
            .ToArray();
        int valueCount = 0;

        Assert.That(enumTypes, Has.Length.EqualTo(21));
        foreach (Type enumType in enumTypes)
        {
            Array values = Enum.GetValues(enumType);
            Assert.That(values.Length, Is.GreaterThan(0), enumType.Name);
            foreach (object value in values)
            {
                string name = Enum.GetName(enumType, value)!;
                object parsed = Enum.Parse(enumType, name);
                Assert.That(parsed, Is.EqualTo(value),
                    enumType.Name + "." + name);
                valueCount++;
            }
        }

        Assert.That(valueCount, Is.EqualTo(122),
            "The enumerated public enum contract changed");
    }

    [Test]
    public void EveryPublicStaticDataFieldIsReadable()
    {
        Type[] staticTypes = typeof(Geometry).Assembly.GetTypes()
            .Where(type => type.IsPublic && type.IsClass &&
                type.IsAbstract && type.IsSealed &&
                type.Namespace == "geo")
            .OrderBy(type => type.Name)
            .ToArray();
        int fieldCount = 0;

        Assert.That(staticTypes.Select(type => type.Name), Is.EqualTo(
            new[]
            {
                nameof(De9ImPatterns),
                nameof(GeoJsonNames),
                nameof(GeometryConstants),
                nameof(WkbTypeCodes)
            }));
        foreach (Type staticType in staticTypes)
        {
            FieldInfo[] fields = staticType.GetFields(
                BindingFlags.Public | BindingFlags.Static |
                BindingFlags.DeclaredOnly);
            Assert.That(fields, Is.Not.Empty, staticType.Name);
            foreach (FieldInfo field in fields)
            {
                object? value = field.GetValue(null);
                Assert.That(value, Is.Not.Null,
                    staticType.Name + "." + field.Name);
                fieldCount++;
            }
        }

        Assert.That(fieldCount, Is.EqualTo(79),
            "The enumerated public static-data contract changed");
    }

    [Test]
    public void PublicFailuresUseTheirSpecificDomainExceptions()
    {
        IWktCodec wkt = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        IGeoJsonCodec geoJson = new GeoJsonCodec(
            new GeoJsonReadOptions(), new GeoJsonWriteOptions());
        IGeometryFactory factory = new GeometryFactory(
            new GeometryFactoryOptions());

        Assert.Multiple(() =>
        {
            Assert.Throws<GeometryParseException>(() =>
                wkt.ReadWkt("POINT ("));
            Assert.Throws<InvalidGeometryException>(() =>
                factory.CreateLineString(
                    [new Coordinate { X = 0.0, Y = 0.0 }]));
            Assert.Throws<PrecisionException>(() =>
                new GeometryFactory(new GeometryFactoryOptions
                {
                    PrecisionModelType = PrecisionModelType.Fixed,
                    PrecisionScale = 0.0
                }));
            Assert.Throws<GeoJsonException>(() =>
                geoJson.ReadGeoJson("{}"));
            Assert.Throws<SpatialIndexException>(() =>
                new RTreeIndex(1));
            Assert.Throws<GeometryException>(() =>
                throw new GeometryException("geometry contract failure"));
            Assert.Throws<TopologyException>(() =>
                throw new TopologyException("topology contract failure"));
        });
    }

    private static bool MapsMethod(
        Type target, Type contract, MethodInfo method)
    {
        InterfaceMapping mapping = target.GetInterfaceMap(contract);
        int index = 0;
        while (index < mapping.InterfaceMethods.Length)
        {
            if (mapping.InterfaceMethods[index] == method &&
                mapping.TargetMethods[index].IsPublic) return true;
            index++;
        }
        return false;
    }

    private static FieldInfo[] PublicInstanceFields(Type type)
    {
        return type.GetFields(
            BindingFlags.Public | BindingFlags.Instance |
            BindingFlags.DeclaredOnly);
    }

    private static object SampleValue(Type type)
    {
        if (type == typeof(bool)) return true;
        if (type == typeof(int)) return 37;
        if (type == typeof(long)) return 41L;
        if (type == typeof(double)) return 12.5;
        if (type == typeof(string)) return "contract-value";
        if (type == typeof(object)) return new object();
        if (type == typeof(JsonElement))
        {
            using JsonDocument document = JsonDocument.Parse("37");
            return document.RootElement.Clone();
        }
        if (type.IsEnum)
        {
            Array values = Enum.GetValues(type);
            return values.GetValue(values.Length - 1)!;
        }
        if (type.IsArray)
        {
            Type elementType = type.GetElementType()!;
            Array array = Array.CreateInstance(elementType, 1);
            array.SetValue(SampleValue(elementType), 0);
            return array;
        }
        return Activator.CreateInstance(type)!;
    }

    private static object[] ReadAllProviderValues(
        IGeometryDataProvider provider)
    {
        return
        [
            provider.GetCoordinate(),
            provider.GetEnvelope(),
            provider.GetPrecisionModel(),
            provider.GetAffineTransform(),
            provider.GetFactoryOptions(),
            provider.GetWkbWriteOptions(),
            provider.GetWktReadOptions(),
            provider.GetWktWriteOptions(),
            provider.GetWkbReadOptions(),
            provider.GetGeoJsonReadOptions(),
            provider.GetGeoJsonWriteOptions(),
            provider.GetGeometryReadResult(),
            provider.GetGeoJsonReadResult(),
            provider.GetWriteResult(),
            provider.GetRelationMatrix(),
            provider.GetSegmentIntersectionResult(),
            provider.GetDistanceResult(),
            provider.GetBufferParameters(),
            provider.GetSimplifyParameters(),
            provider.GetRepairParameters(),
            provider.GetValidationResult(),
            provider.GetCoverageValidationResult(),
            provider.GetLinearLocation(),
            provider.GetSpatialIndexItem(),
            provider.GetSpatialIndexQuery(),
            provider.GetGeoJsonDocument(),
            provider.GetParseError(),
            provider.GetCircleResult(),
            provider.GetCurveLinearizationParameters()
        ];
    }

    private static Type[] ProviderValueTypes()
    {
        return
        [
            typeof(Coordinate),
            typeof(Envelope),
            typeof(PrecisionModel),
            typeof(AffineTransform),
            typeof(GeometryFactoryOptions),
            typeof(WkbWriteOptions),
            typeof(WktReadOptions),
            typeof(WktWriteOptions),
            typeof(WkbReadOptions),
            typeof(GeoJsonReadOptions),
            typeof(GeoJsonWriteOptions),
            typeof(GeometryReadResult),
            typeof(GeoJsonReadResult),
            typeof(WriteResult),
            typeof(RelationMatrix),
            typeof(SegmentIntersectionResult),
            typeof(DistanceResult),
            typeof(BufferParameters),
            typeof(SimplifyParameters),
            typeof(RepairParameters),
            typeof(ValidationResult),
            typeof(CoverageValidationResult),
            typeof(LinearLocation),
            typeof(SpatialIndexItem),
            typeof(SpatialIndexQuery),
            typeof(GeoJsonDocument),
            typeof(ParseError),
            typeof(CircleResult),
            typeof(CurveLinearizationParameters)
        ];
    }
}

internal class GeometryDataProviderFixture : IGeometryDataProvider
{
    public Coordinate GetCoordinate()
    {
        return new Coordinate();
    }

    public Envelope GetEnvelope()
    {
        return new Envelope();
    }

    public PrecisionModel GetPrecisionModel()
    {
        return new PrecisionModel();
    }

    public AffineTransform GetAffineTransform()
    {
        return new AffineTransform();
    }

    public GeometryFactoryOptions GetFactoryOptions()
    {
        return new GeometryFactoryOptions();
    }

    public WkbWriteOptions GetWkbWriteOptions()
    {
        return new WkbWriteOptions();
    }

    public WktReadOptions GetWktReadOptions()
    {
        return new WktReadOptions();
    }

    public WktWriteOptions GetWktWriteOptions()
    {
        return new WktWriteOptions();
    }

    public WkbReadOptions GetWkbReadOptions()
    {
        return new WkbReadOptions();
    }

    public GeoJsonReadOptions GetGeoJsonReadOptions()
    {
        return new GeoJsonReadOptions();
    }

    public GeoJsonWriteOptions GetGeoJsonWriteOptions()
    {
        return new GeoJsonWriteOptions();
    }

    public GeometryReadResult GetGeometryReadResult()
    {
        return new GeometryReadResult();
    }

    public GeoJsonReadResult GetGeoJsonReadResult()
    {
        return new GeoJsonReadResult();
    }

    public WriteResult GetWriteResult()
    {
        return new WriteResult();
    }

    public RelationMatrix GetRelationMatrix()
    {
        return new RelationMatrix();
    }

    public SegmentIntersectionResult GetSegmentIntersectionResult()
    {
        return new SegmentIntersectionResult();
    }

    public DistanceResult GetDistanceResult()
    {
        return new DistanceResult();
    }

    public BufferParameters GetBufferParameters()
    {
        return new BufferParameters();
    }

    public SimplifyParameters GetSimplifyParameters()
    {
        return new SimplifyParameters();
    }

    public RepairParameters GetRepairParameters()
    {
        return new RepairParameters();
    }

    public ValidationResult GetValidationResult()
    {
        return new ValidationResult();
    }

    public CoverageValidationResult GetCoverageValidationResult()
    {
        return new CoverageValidationResult();
    }

    public LinearLocation GetLinearLocation()
    {
        return new LinearLocation();
    }

    public SpatialIndexItem GetSpatialIndexItem()
    {
        return new SpatialIndexItem();
    }

    public SpatialIndexQuery GetSpatialIndexQuery()
    {
        return new SpatialIndexQuery();
    }

    public GeoJsonDocument GetGeoJsonDocument()
    {
        return new GeoJsonDocument();
    }

    public ParseError GetParseError()
    {
        return new ParseError();
    }

    public CircleResult GetCircleResult()
    {
        return new CircleResult();
    }

    public CurveLinearizationParameters GetCurveLinearizationParameters()
    {
        return new CurveLinearizationParameters();
    }
}
