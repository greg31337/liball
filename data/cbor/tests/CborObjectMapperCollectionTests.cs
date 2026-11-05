using System.Numerics;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborObjectMapperCollectionTests
{
    public class Location
    {
        public string City = "";
        public int PostalCode { get; set; }
    }

    public class Person
    {
        public int Age;
        public Location Home { get; set; } = new Location();
        public string Name { get; set; } = "";
    }

    public class ByteHolder
    {
        public byte Value;
    }

    public class NoDefaultConstructor
    {
        public int Value;

        public NoDefaultConstructor(int value)
        {
            Value = value;
        }
    }

    [Test]
    public void RoundTripsArraysAndBuiltInListInterfaces()
    {
        CborObjectMapper mapper = new CborObjectMapper();
        CborMappingOptions options = new CborMappingOptions();
        int[] array = [1, 2, 3];
        IReadOnlyList<string> list = new List<string> { "a", "b" };

        CborValue encodedArray = mapper.MapToValue(array, typeof(int[]), options);
        CborValue encodedList = mapper.MapToValue(
            list,
            typeof(IReadOnlyList<string>),
            options);
        int[] decodedArray = (int[])mapper.MapFromValue(
            encodedArray,
            typeof(int[]),
            options)!;
        IReadOnlyList<string> decodedList = (IReadOnlyList<string>)mapper.MapFromValue(
            encodedList,
            typeof(IReadOnlyList<string>),
            options)!;

        Assert.Multiple(() =>
        {
            Assert.That(encodedArray.Kind, Is.EqualTo(CborValueKind.Array));
            Assert.That(encodedArray.ArrayItems, Has.Count.EqualTo(3));
            Assert.That(decodedArray, Is.EqualTo(array));
            Assert.That(encodedList.ArrayItems, Has.Count.EqualTo(2));
            Assert.That(decodedList, Is.EqualTo(list));
            Assert.That(decodedList, Is.TypeOf<List<string>>());
        });
    }

    [Test]
    public void RecursivelyMapsNestedCollections()
    {
        CborObjectMapper mapper = new CborObjectMapper();
        CborMappingOptions options = new CborMappingOptions();
        List<int[]> source = new List<int[]> { new int[] { 1, 2 }, new int[] { 3 } };

        CborValue encoded = mapper.MapToValue(
            source,
            typeof(List<int[]>),
            options);
        List<int[]> decoded = (List<int[]>)mapper.MapFromValue(
            encoded,
            typeof(List<int[]>),
            options)!;

        Assert.Multiple(() =>
        {
            Assert.That(encoded.ArrayItems, Has.Count.EqualTo(2));
            Assert.That(encoded.ArrayItems?[0].ArrayItems, Has.Count.EqualTo(2));
            Assert.That(decoded, Has.Count.EqualTo(2));
            Assert.That(decoded[0], Is.EqualTo(new int[] { 1, 2 }));
            Assert.That(decoded[1], Is.EqualTo(new int[] { 3 }));
        });
    }

    [Test]
    public void RoundTripsDictionariesWithTextAndArbitraryKeys()
    {
        CborObjectMapper mapper = new CborObjectMapper();
        CborMappingOptions options = new CborMappingOptions();
        IReadOnlyDictionary<string, int> text = new Dictionary<string, int>
        {
            ["one"] = 1,
            ["two"] = 2
        };
        Dictionary<int, string> arbitrary = new Dictionary<int, string>
        {
            [7] = "seven"
        };

        CborValue encodedText = mapper.MapToValue(
            text,
            typeof(IReadOnlyDictionary<string, int>),
            options);
        CborValue encodedArbitrary = mapper.MapToValue(
            arbitrary,
            typeof(Dictionary<int, string>),
            options);
        IReadOnlyDictionary<string, int> decodedText =
            (IReadOnlyDictionary<string, int>)mapper.MapFromValue(
                encodedText,
                typeof(IReadOnlyDictionary<string, int>),
                options)!;
        Dictionary<int, string> decodedArbitrary =
            (Dictionary<int, string>)mapper.MapFromValue(
                encodedArbitrary,
                typeof(Dictionary<int, string>),
                options)!;

        Assert.Multiple(() =>
        {
            Assert.That(encodedText.Kind, Is.EqualTo(CborValueKind.Map));
            Assert.That(encodedText.MapEntries, Has.Count.EqualTo(2));
            Assert.That(decodedText, Is.TypeOf<Dictionary<string, int>>());
            Assert.That(decodedText["one"], Is.EqualTo(1));
            Assert.That(decodedText["two"], Is.EqualTo(2));
            Assert.That(encodedArbitrary.MapEntries?[0].Key?.IntegerValue,
                Is.EqualTo(new BigInteger(7)));
            Assert.That(decodedArbitrary[7], Is.EqualTo("seven"));
        });
    }

    [Test]
    public void TextOnlyDictionaryPolicyRejectsNonTextKeysInBothDirections()
    {
        CborObjectMapper mapper = new CborObjectMapper();
        CborMappingOptions options = new CborMappingOptions
        {
            DictionaryKeyPolicy = CborMappingKeyPolicy.TextOnly
        };
        Dictionary<int, string> native = new Dictionary<int, string> { [1] = "one" };
        CborValue encoded = Map(Integer(1), Text("one"));

        CborMappingException encodingError = Assert.Throws<CborMappingException>(() =>
            mapper.MapToValue(native, typeof(Dictionary<int, string>), options))!;
        CborMappingException decodingError = Assert.Throws<CborMappingException>(() =>
            mapper.MapFromValue(encoded, typeof(Dictionary<int, string>), options))!;

        Assert.Multiple(() =>
        {
            Assert.That(encodingError.ErrorCode,
                Is.EqualTo("CBOR_MAPPING_DICTIONARY_KEY_NOT_TEXT"));
            Assert.That(encodingError.ErrorPath, Is.EqualTo("$[0].key"));
            Assert.That(decodingError.ErrorCode,
                Is.EqualTo("CBOR_MAPPING_DICTIONARY_KEY_NOT_TEXT"));
            Assert.That(decodingError.ErrorPath, Is.EqualTo("$[0].key"));
        });
    }

    [Test]
    public void MapsReflectedFieldsAndPropertiesAsNamedMaps()
    {
        CborObjectMapper mapper = new CborObjectMapper();
        CborMappingOptions options = new CborMappingOptions();
        Person person = SamplePerson();

        CborValue encoded = mapper.MapToValue(person, typeof(Person), options);
        Person decoded = (Person)mapper.MapFromValue(
            encoded,
            typeof(Person),
            options)!;

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Kind, Is.EqualTo(CborValueKind.Map));
            Assert.That(encoded.MapEntries, Has.Count.EqualTo(3));
            Assert.That(encoded.MapEntries?[0].Key?.TextStringValue, Is.EqualTo("Age"));
            Assert.That(encoded.MapEntries?[1].Key?.TextStringValue, Is.EqualTo("Home"));
            Assert.That(encoded.MapEntries?[2].Key?.TextStringValue, Is.EqualTo("Name"));
            Assert.That(decoded.Age, Is.EqualTo(37));
            Assert.That(decoded.Name, Is.EqualTo("Ada"));
            Assert.That(decoded.Home.City, Is.EqualTo("London"));
            Assert.That(decoded.Home.PostalCode, Is.EqualTo(12345));
        });
    }

    [Test]
    public void AppliesCamelCaseNamesAndUsesStablePositionalOrder()
    {
        CborObjectMapper mapper = new CborObjectMapper();
        Person person = SamplePerson();
        CborMappingOptions camel = new CborMappingOptions
        {
            MemberNamingPolicy = CborMappingNamingPolicy.CamelCase
        };
        CborMappingOptions positional = new CborMappingOptions
        {
            MapObjectsAsArrays = true
        };

        CborValue named = mapper.MapToValue(person, typeof(Person), camel);
        Person namedRoundTrip = (Person)mapper.MapFromValue(
            named,
            typeof(Person),
            camel)!;
        CborValue array = mapper.MapToValue(person, typeof(Person), positional);
        Person arrayRoundTrip = (Person)mapper.MapFromValue(
            array,
            typeof(Person),
            positional)!;

        Assert.Multiple(() =>
        {
            Assert.That(named.MapEntries?[0].Key?.TextStringValue, Is.EqualTo("age"));
            Assert.That(named.MapEntries?[1].Key?.TextStringValue, Is.EqualTo("home"));
            Assert.That(named.MapEntries?[2].Key?.TextStringValue, Is.EqualTo("name"));
            Assert.That(namedRoundTrip.Home.City, Is.EqualTo("London"));
            Assert.That(array.Kind, Is.EqualTo(CborValueKind.Array));
            Assert.That(array.ArrayItems?[0].IntegerValue,
                Is.EqualTo(new BigInteger(37)));
            Assert.That(array.ArrayItems?[2].TextStringValue, Is.EqualTo("Ada"));
            Assert.That(arrayRoundTrip.Name, Is.EqualTo("Ada"));
        });
    }

    [Test]
    public void EnforcesRequiredUnknownAndDuplicateMemberPolicies()
    {
        CborObjectMapper mapper = new CborObjectMapper();
        CborValue missing = Map(
            Text("Age"), Integer(10),
            Text("Name"), Text("Lin"));
        CborValue unknown = Map(
            Text("Age"), Integer(10),
            Text("Home"), LocationValue(),
            Text("Name"), Text("Lin"),
            Text("Extra"), Integer(1));
        CborValue duplicate = Map(
            Text("Age"), Integer(10),
            Text("Age"), Integer(11),
            Text("Home"), LocationValue(),
            Text("Name"), Text("Lin"));
        CborMappingOptions strict = new CborMappingOptions();
        CborMappingOptions lenient = new CborMappingOptions
        {
            RequireAllMembers = false,
            IgnoreUnknownMembers = true
        };

        CborMappingException missingError = Assert.Throws<CborMappingException>(() =>
            mapper.MapFromValue(missing, typeof(Person), strict))!;
        CborMappingException unknownError = Assert.Throws<CborMappingException>(() =>
            mapper.MapFromValue(unknown, typeof(Person), strict))!;
        CborMappingException duplicateError = Assert.Throws<CborMappingException>(() =>
            mapper.MapFromValue(duplicate, typeof(Person), strict))!;
        Person missingAccepted = (Person)mapper.MapFromValue(
            missing,
            typeof(Person),
            lenient)!;
        Person unknownAccepted = (Person)mapper.MapFromValue(
            unknown,
            typeof(Person),
            lenient)!;

        Assert.Multiple(() =>
        {
            Assert.That(missingError.ErrorCode,
                Is.EqualTo("CBOR_MAPPING_REQUIRED_MEMBER_MISSING"));
            Assert.That(missingError.ErrorPath, Is.EqualTo("$.Home"));
            Assert.That(unknownError.ErrorCode, Is.EqualTo("CBOR_MAPPING_UNKNOWN_MEMBER"));
            Assert.That(unknownError.ErrorPath, Is.EqualTo("$.Extra"));
            Assert.That(duplicateError.ErrorCode,
                Is.EqualTo("CBOR_MAPPING_DUPLICATE_MEMBER"));
            Assert.That(duplicateError.ErrorPath, Is.EqualTo("$.Age"));
            Assert.That(missingAccepted.Name, Is.EqualTo("Lin"));
            Assert.That(missingAccepted.Home, Is.Not.Null);
            Assert.That(unknownAccepted.Name, Is.EqualTo("Lin"));
        });
    }

    [Test]
    public void ReportsNestedIntegerOverflowAtTheMemberPath()
    {
        CborObjectMapper mapper = new CborObjectMapper();
        CborMappingOptions options = new CborMappingOptions();
        CborValue source = Map(Text("Value"), Integer(256));

        CborMappingException error = Assert.Throws<CborMappingException>(() =>
            mapper.MapFromValue(source, typeof(ByteHolder), options))!;

        Assert.Multiple(() =>
        {
            Assert.That(error.ErrorCode, Is.EqualTo("CBOR_MAPPING_INTEGER_OVERFLOW"));
            Assert.That(error.ErrorPath, Is.EqualTo("$.Value"));
        });
    }

    [Test]
    public void EnforcesCollectionDepthAndTotalItemLimits()
    {
        CborObjectMapper mapper = new CborObjectMapper();
        List<int> three = new List<int> { 1, 2, 3 };
        List<int[]> nested = new List<int[]> { new int[] { 1 } };
        CborMappingOptions collectionLimit = new CborMappingOptions
        {
            MaxCollectionItems = 2
        };
        CborMappingOptions depthLimit = new CborMappingOptions
        {
            MaxNestingDepth = 0
        };
        CborMappingOptions totalLimit = new CborMappingOptions
        {
            MaxTotalItems = 2
        };

        CborMappingException collectionError = Assert.Throws<CborMappingException>(() =>
            mapper.MapToValue(three, typeof(List<int>), collectionLimit))!;
        CborMappingException depthError = Assert.Throws<CborMappingException>(() =>
            mapper.MapToValue(nested, typeof(List<int[]>), depthLimit))!;
        CborMappingException totalError = Assert.Throws<CborMappingException>(() =>
            mapper.MapToValue(three, typeof(List<int>), totalLimit))!;

        Assert.Multiple(() =>
        {
            Assert.That(collectionError.ErrorCode,
                Is.EqualTo("CBOR_MAPPING_MAX_COLLECTION_ITEMS"));
            Assert.That(depthError.ErrorCode,
                Is.EqualTo("CBOR_MAPPING_MAX_NESTING_DEPTH"));
            Assert.That(depthError.ErrorPath, Is.EqualTo("$[0]"));
            Assert.That(totalError.ErrorCode,
                Is.EqualTo("CBOR_MAPPING_MAX_TOTAL_ITEMS"));
            Assert.That(totalError.ErrorPath, Is.EqualTo("$[1]"));
        });
    }

    [Test]
    public void RejectsUnsupportedArrayRanksAndTargetsWithoutConstructors()
    {
        CborObjectMapper mapper = new CborObjectMapper();
        CborMappingOptions options = new CborMappingOptions();
        int[,] matrix = new int[1, 1];
        CborValue source = Map(Text("Value"), Integer(1));

        CborMappingException rankError = Assert.Throws<CborMappingException>(() =>
            mapper.MapToValue(matrix, typeof(int[,]), options))!;
        CborMappingException constructorError = Assert.Throws<CborMappingException>(() =>
            mapper.MapFromValue(source, typeof(NoDefaultConstructor), options))!;

        Assert.Multiple(() =>
        {
            Assert.That(rankError.ErrorCode,
                Is.EqualTo("CBOR_MAPPING_ARRAY_RANK_UNSUPPORTED"));
            Assert.That(constructorError.ErrorCode,
                Is.EqualTo("CBOR_MAPPING_CONSTRUCTOR_REQUIRED"));
        });
    }

    private static Person SamplePerson()
    {
        return new Person
        {
            Age = 37,
            Name = "Ada",
            Home = new Location
            {
                City = "London",
                PostalCode = 12345
            }
        };
    }

    private static CborValue LocationValue()
    {
        return Map(
            Text("City"), Text("London"),
            Text("PostalCode"), Integer(12345));
    }

    private static CborValue Map(params CborValue[] values)
    {
        CborValue result = new CborValue
        {
            Kind = CborValueKind.Map,
            LengthKind = CborLengthKind.Definite,
            MapEntries = new List<CborMapEntry>()
        };
        int index = 0;
        while (index < values.Length)
        {
            result.MapEntries.Add(new CborMapEntry
            {
                Key = values[index],
                Value = values[index + 1]
            });
            index += 2;
        }
        return result;
    }

    private static CborValue Integer(int value)
    {
        return new CborValue
        {
            Kind = value < 0
                ? CborValueKind.NegativeInteger
                : CborValueKind.UnsignedInteger,
            IntegerValue = value
        };
    }

    private static CborValue Text(string value)
    {
        return new CborValue
        {
            Kind = CborValueKind.TextString,
            LengthKind = CborLengthKind.Definite,
            TextStringValue = value
        };
    }
}
