using System.Numerics;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborObjectMapperComprehensiveTests
{
    public class SelectedMembers
    {
        public static int StaticField = 10;
        public readonly int ReadOnlyField = 11;
        public int Field;
        public int WritableProperty { get; set; }
        public int PrivateSetter { get; private set; } = 12;
        public int ReadOnlyProperty => 13;

        public int this[int index]
        {
            get => index;
            set => Field = value;
        }
    }

    public class PositionalValue
    {
        public int Count;
        public string Name = "default";
    }

    public class CollidingMembers
    {
        public int Name;
        public int name;
    }

    public class ReferenceBox
    {
        public int Value;
    }

    public class ReferenceMismatch
    {
        public ReferenceBox First = new ReferenceBox();
        public string Second = "";
    }

    public class Animal
    {
        public string Name = "";
    }

    public class Cat : Animal
    {
        public int Lives;
    }

    public class AdapterToken
    {
        public string Value = "";
    }

    public struct AdapterNumber
    {
        public int Value;
    }

    private sealed class AnimalAdapter : ICborTypeAdapter
    {
        public bool SupportsType(Type runtimeType)
        {
            return runtimeType == typeof(Cat);
        }

        public CborValue EncodeObject(object value, CborMappingContext context)
        {
            Cat cat = (Cat)value;
            return Text(cat.Name + ":" + cat.Lives);
        }

        public object? DecodeObject(
            CborValue value,
            Type targetType,
            CborMappingContext context)
        {
            string[] parts = (value.TextStringValue ?? "").Split(':');
            return new Cat
            {
                Name = parts[0],
                Lives = int.Parse(parts[1])
            };
        }
    }

    private sealed class NullValueAdapter : ICborTypeAdapter
    {
        public bool SupportsType(Type runtimeType)
        {
            return runtimeType == typeof(AdapterToken);
        }

        public CborValue EncodeObject(object value, CborMappingContext context)
        {
            return null!;
        }

        public object? DecodeObject(
            CborValue value,
            Type targetType,
            CborMappingContext context)
        {
            return new AdapterToken();
        }
    }

    private sealed class NullResultAdapter : ICborTypeAdapter
    {
        public bool SupportsType(Type runtimeType)
        {
            return runtimeType == typeof(AdapterNumber);
        }

        public CborValue EncodeObject(object value, CborMappingContext context)
        {
            return Integer(((AdapterNumber)value).Value);
        }

        public object? DecodeObject(
            CborValue value,
            Type targetType,
            CborMappingContext context)
        {
            return null;
        }
    }

    private sealed class WrongResultAdapter : ICborTypeAdapter
    {
        public bool SupportsType(Type runtimeType)
        {
            return runtimeType == typeof(AdapterToken);
        }

        public CborValue EncodeObject(object value, CborMappingContext context)
        {
            return Text("token");
        }

        public object? DecodeObject(
            CborValue value,
            Type targetType,
            CborMappingContext context)
        {
            return "wrong type";
        }
    }

    [Test]
    public void RoundTripsMutableCollectionInterfacesIncludingEmptyAndNullValues()
    {
        CborObjectMapper mapper = new CborObjectMapper();
        CborMappingOptions options = new CborMappingOptions();
        IList<string> list = new List<string> { "first", null!, "third" };
        IDictionary<string, List<int>> dictionary =
            new Dictionary<string, List<int>>
            {
                ["empty"] = new List<int>(),
                ["values"] = new List<int> { 1, 2 }
            };

        CborValue encodedList = mapper.MapToValue(list, typeof(IList<string>), options);
        CborValue encodedDictionary = mapper.MapToValue(
            dictionary,
            typeof(IDictionary<string, List<int>>),
            options);
        IList<string> decodedList = (IList<string>)mapper.MapFromValue(
            encodedList,
            typeof(IList<string>),
            options)!;
        IDictionary<string, List<int>> decodedDictionary =
            (IDictionary<string, List<int>>)mapper.MapFromValue(
                encodedDictionary,
                typeof(IDictionary<string, List<int>>),
                options)!;

        Assert.Multiple(() =>
        {
            Assert.That(decodedList, Is.TypeOf<List<string>>());
            Assert.That(decodedList, Is.EqualTo(list));
            Assert.That(encodedList.ArrayItems?[1].SimpleValue,
                Is.EqualTo(CborSimpleValues.Null));
            Assert.That(decodedDictionary, Is.TypeOf<Dictionary<string, List<int>>>());
            Assert.That(decodedDictionary["empty"], Is.Empty);
            Assert.That(decodedDictionary["values"], Is.EqualTo(new int[] { 1, 2 }));
        });
    }

    [Test]
    public void CopiesByteStorageWhenDecodingEveryByteContainer()
    {
        CborObjectMapper mapper = new CborObjectMapper();
        CborMappingOptions options = new CborMappingOptions();
        byte[] storage = [1, 2, 3];
        CborValue source = Bytes(storage);

        byte[] array = (byte[])mapper.MapFromValue(source, typeof(byte[]), options)!;
        Memory<byte> memory = (Memory<byte>)mapper.MapFromValue(
            source,
            typeof(Memory<byte>),
            options)!;
        ReadOnlyMemory<byte> readOnly = (ReadOnlyMemory<byte>)mapper.MapFromValue(
            source,
            typeof(ReadOnlyMemory<byte>),
            options)!;
        storage[0] = 9;

        Assert.Multiple(() =>
        {
            Assert.That(array, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(memory.ToArray(), Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(readOnly.ToArray(), Is.EqualTo(new byte[] { 1, 2, 3 }));
        });
    }

    [Test]
    public void ReflectionMapsOnlyWritablePublicInstanceFieldsAndProperties()
    {
        CborObjectMapper mapper = new CborObjectMapper();
        CborMappingOptions options = new CborMappingOptions();
        SelectedMembers source = new SelectedMembers
        {
            Field = 3,
            WritableProperty = 4
        };

        CborValue encoded = mapper.MapToValue(source, typeof(SelectedMembers), options);
        SelectedMembers decoded = (SelectedMembers)mapper.MapFromValue(
            encoded,
            typeof(SelectedMembers),
            options)!;

        Assert.Multiple(() =>
        {
            Assert.That(encoded.MapEntries, Has.Count.EqualTo(2));
            Assert.That(encoded.MapEntries?[0].Key?.TextStringValue, Is.EqualTo("Field"));
            Assert.That(encoded.MapEntries?[1].Key?.TextStringValue,
                Is.EqualTo("WritableProperty"));
            Assert.That(decoded.Field, Is.EqualTo(3));
            Assert.That(decoded.WritableProperty, Is.EqualTo(4));
            Assert.That(decoded.ReadOnlyField, Is.EqualTo(11));
            Assert.That(decoded.PrivateSetter, Is.EqualTo(12));
        });
    }

    [Test]
    public void PositionalLayoutsEnforceMissingExtraAndRepresentationPolicies()
    {
        CborObjectMapper mapper = new CborObjectMapper();
        CborMappingOptions strict = new CborMappingOptions
        {
            MapObjectsAsArrays = true
        };
        CborMappingOptions lenient = new CborMappingOptions
        {
            MapObjectsAsArrays = true,
            RequireAllMembers = false,
            IgnoreUnknownMembers = true
        };
        CborValue missing = Array(Integer(5));
        CborValue extra = Array(Integer(5), Text("five"), Text("ignored"));

        CborMappingException missingError = Assert.Throws<CborMappingException>(() =>
            mapper.MapFromValue(missing, typeof(PositionalValue), strict))!;
        CborMappingException extraError = Assert.Throws<CborMappingException>(() =>
            mapper.MapFromValue(extra, typeof(PositionalValue), strict))!;
        CborMappingException arrayRequired = Assert.Throws<CborMappingException>(() =>
            mapper.MapFromValue(Map(), typeof(PositionalValue), strict))!;
        CborMappingException mapRequired = Assert.Throws<CborMappingException>(() =>
            mapper.MapFromValue(Array(), typeof(PositionalValue), new CborMappingOptions()))!;
        PositionalValue missingAccepted = (PositionalValue)mapper.MapFromValue(
            missing,
            typeof(PositionalValue),
            lenient)!;
        PositionalValue extraAccepted = (PositionalValue)mapper.MapFromValue(
            extra,
            typeof(PositionalValue),
            lenient)!;

        Assert.Multiple(() =>
        {
            AssertCode(missingError, "CBOR_MAPPING_REQUIRED_MEMBER_MISSING");
            Assert.That(missingError.ErrorPath, Is.EqualTo("$.Name"));
            AssertCode(extraError, "CBOR_MAPPING_UNKNOWN_MEMBER");
            Assert.That(extraError.ErrorPath, Is.EqualTo("$[2]"));
            AssertCode(arrayRequired, "CBOR_MAPPING_OBJECT_ARRAY_REQUIRED");
            AssertCode(mapRequired, "CBOR_MAPPING_OBJECT_MAP_REQUIRED");
            Assert.That(missingAccepted.Count, Is.EqualTo(5));
            Assert.That(missingAccepted.Name, Is.EqualTo("default"));
            Assert.That(extraAccepted.Name, Is.EqualTo("five"));
        });
    }

    [Test]
    public void NamedLayoutsRejectMalformedKeysEntriesAndCamelCaseCollisions()
    {
        CborObjectMapper mapper = new CborObjectMapper();
        CborValue nonText = Map(Integer(1), Integer(2));
        CborValue incomplete = new CborValue
        {
            Kind = CborValueKind.Map,
            MapEntries = new List<CborMapEntry> { new CborMapEntry() }
        };
        CborMappingOptions optional = new CborMappingOptions
        {
            RequireAllMembers = false
        };
        CborMappingOptions ignore = new CborMappingOptions
        {
            RequireAllMembers = false,
            IgnoreUnknownMembers = true
        };
        CborMappingOptions camel = new CborMappingOptions
        {
            MemberNamingPolicy = CborMappingNamingPolicy.CamelCase
        };

        CborMappingException keyError = Assert.Throws<CborMappingException>(() =>
            mapper.MapFromValue(nonText, typeof(PositionalValue), optional))!;
        CborMappingException entryError = Assert.Throws<CborMappingException>(() =>
            mapper.MapFromValue(incomplete, typeof(PositionalValue), optional))!;
        CborMappingException collision = Assert.Throws<CborMappingException>(() =>
            mapper.MapToValue(new CollidingMembers(), typeof(CollidingMembers), camel))!;
        PositionalValue ignored = (PositionalValue)mapper.MapFromValue(
            nonText,
            typeof(PositionalValue),
            ignore)!;

        Assert.Multiple(() =>
        {
            AssertCode(keyError, "CBOR_MAPPING_OBJECT_KEY_NOT_TEXT");
            Assert.That(keyError.ErrorPath, Is.EqualTo("$[0].key"));
            AssertCode(entryError, "CBOR_MAPPING_MAP_ENTRY_MISSING");
            AssertCode(collision, "CBOR_MAPPING_MEMBER_NAME_COLLISION");
            Assert.That(ignored.Name, Is.EqualTo("default"));
        });
    }

    [Test]
    public void DictionaryDecodingRejectsMalformedNullAndDuplicateKeys()
    {
        CborObjectMapper mapper = new CborObjectMapper();
        CborMappingOptions options = new CborMappingOptions();
        CborValue incomplete = new CborValue
        {
            Kind = CborValueKind.Map,
            MapEntries = new List<CborMapEntry> { new CborMapEntry() }
        };
        CborValue nullKey = Map(Null(), Text("value"));
        CborValue duplicate = Map(
            Integer(1), Text("one"),
            Integer(1), Text("again"));

        CborMappingException arrayRequired = Assert.Throws<CborMappingException>(() =>
            mapper.MapFromValue(Text("no"), typeof(List<int>), options))!;
        CborMappingException mapRequired = Assert.Throws<CborMappingException>(() =>
            mapper.MapFromValue(Text("no"), typeof(Dictionary<int, string>), options))!;
        CborMappingException entryError = Assert.Throws<CborMappingException>(() =>
            mapper.MapFromValue(incomplete, typeof(Dictionary<int, string>), options))!;
        CborMappingException nullError = Assert.Throws<CborMappingException>(() =>
            mapper.MapFromValue(nullKey, typeof(Dictionary<string, string>), options))!;
        CborMappingException duplicateError = Assert.Throws<CborMappingException>(() =>
            mapper.MapFromValue(duplicate, typeof(Dictionary<int, string>), options))!;

        Assert.Multiple(() =>
        {
            AssertCode(arrayRequired, "CBOR_MAPPING_ARRAY_REQUIRED");
            AssertCode(mapRequired, "CBOR_MAPPING_MAP_REQUIRED");
            AssertCode(entryError, "CBOR_MAPPING_MAP_ENTRY_MISSING");
            AssertCode(nullError, "CBOR_MAPPING_DICTIONARY_KEY_NULL");
            AssertCode(duplicateError, "CBOR_MAPPING_DUPLICATE_DICTIONARY_KEY");
            Assert.That(duplicateError.ErrorPath, Is.EqualTo("$[1].key"));
        });
    }

    [Test]
    public void BaseTypeAdapterHandlesDerivedValuesAndContractFailuresAreReported()
    {
        CborObjectMapper mapper = new CborObjectMapper();
        CborMappingOptions hierarchy = Registered(typeof(Animal), new AnimalAdapter());
        Cat source = new Cat { Name = "Mog", Lives = 8 };

        CborValue encoded = mapper.MapToValue(source, typeof(Cat), hierarchy);
        Cat decoded = (Cat)mapper.MapFromValue(encoded, typeof(Cat), hierarchy)!;
        CborMappingException missingValue = Assert.Throws<CborMappingException>(() =>
            mapper.MapToValue(
                new AdapterToken(),
                typeof(AdapterToken),
                Registered(typeof(AdapterToken), new NullValueAdapter())))!;
        CborMappingException nullResult = Assert.Throws<CborMappingException>(() =>
            mapper.MapFromValue(
                Integer(1),
                typeof(AdapterNumber),
                Registered(typeof(AdapterNumber), new NullResultAdapter())))!;
        CborMappingException wrongResult = Assert.Throws<CborMappingException>(() =>
            mapper.MapFromValue(
                Text("token"),
                typeof(AdapterToken),
                Registered(typeof(AdapterToken), new WrongResultAdapter())))!;

        Assert.Multiple(() =>
        {
            Assert.That(encoded.TextStringValue, Is.EqualTo("Mog:8"));
            Assert.That(decoded.Name, Is.EqualTo("Mog"));
            Assert.That(decoded.Lives, Is.EqualTo(8));
            AssertCode(missingValue, "CBOR_MAPPING_ADAPTER_VALUE_MISSING");
            AssertCode(nullResult, "CBOR_MAPPING_ADAPTER_RESULT_NULL");
            AssertCode(wrongResult, "CBOR_MAPPING_RUNTIME_TYPE_MISMATCH");
        });
    }

    [Test]
    public void PreservesCyclesInArrayListAndDictionaryContainers()
    {
        CborObjectMapper mapper = new CborObjectMapper();
        CborMappingOptions options = new CborMappingOptions
        {
            PreserveObjectReferences = true
        };
        object[] array = new object[1];
        array[0] = array;
        List<object> list = new List<object>();
        list.Add(list);
        Dictionary<string, object> dictionary = new Dictionary<string, object>();
        dictionary["self"] = dictionary;

        CborValue arrayValue = mapper.MapToValue(array, typeof(object[]), options);
        CborValue listValue = mapper.MapToValue(list, typeof(List<object>), options);
        CborValue dictionaryValue = mapper.MapToValue(
            dictionary,
            typeof(Dictionary<string, object>),
            options);
        object[] decodedArray = (object[])mapper.MapFromValue(
            arrayValue,
            typeof(object[]),
            options)!;
        List<object> decodedList = (List<object>)mapper.MapFromValue(
            listValue,
            typeof(List<object>),
            options)!;
        Dictionary<string, object> decodedDictionary =
            (Dictionary<string, object>)mapper.MapFromValue(
                dictionaryValue,
                typeof(Dictionary<string, object>),
                options)!;

        Assert.Multiple(() =>
        {
            Assert.That(arrayValue.ArrayItems?[0], Is.SameAs(arrayValue));
            Assert.That(listValue.ArrayItems?[0], Is.SameAs(listValue));
            Assert.That(dictionaryValue.MapEntries?[0].Value,
                Is.SameAs(dictionaryValue));
            Assert.That(decodedArray[0], Is.SameAs(decodedArray));
            Assert.That(decodedList[0], Is.SameAs(decodedList));
            Assert.That(decodedDictionary["self"], Is.SameAs(decodedDictionary));
        });
    }

    [Test]
    public void RejectsUnresolvedMissingAndTypeMismatchedReferences()
    {
        CborObjectMapper mapper = new CborObjectMapper();
        CborMappingOptions options = new CborMappingOptions
        {
            PreserveObjectReferences = true
        };
        CborValue orphan = Text("orphan");
        CborValue dangling = Text("reference");
        dangling.ReferencedValue = orphan;
        CborValue unresolved = new CborValue
        {
            Kind = CborValueKind.Tag,
            TagNumber = CborTagNumbers.SharedValueReference,
            TaggedValue = Integer(0)
        };
        CborValue missing = new CborValue
        {
            Kind = CborValueKind.Tag,
            TagNumber = CborTagNumbers.ShareableValue
        };
        CborValue box = Map(Text("Value"), Integer(1));
        CborValue boxReference = new CborValue { ReferencedValue = box };
        CborValue mismatch = Map(
            Text("First"), box,
            Text("Second"), boxReference);

        CborMappingException danglingError = Assert.Throws<CborMappingException>(() =>
            mapper.MapFromValue(dangling, typeof(string), options))!;
        CborMappingException unresolvedError = Assert.Throws<CborMappingException>(() =>
            mapper.MapFromValue(unresolved, typeof(string), options))!;
        CborMappingException missingError = Assert.Throws<CborMappingException>(() =>
            mapper.MapFromValue(missing, typeof(string), options))!;
        CborMappingException mismatchError = Assert.Throws<CborMappingException>(() =>
            mapper.MapFromValue(mismatch, typeof(ReferenceMismatch), options))!;

        Assert.Multiple(() =>
        {
            AssertCode(danglingError, "CBOR_MAPPING_REFERENCE_NOT_FOUND");
            AssertCode(unresolvedError, "CBOR_MAPPING_REFERENCE_NOT_RESOLVED");
            AssertCode(missingError, "CBOR_MAPPING_SHARED_VALUE_MISSING");
            AssertCode(mismatchError, "CBOR_MAPPING_REFERENCE_TYPE_MISMATCH");
            Assert.That(mismatchError.ErrorPath, Is.EqualTo("$.Second"));
        });
    }

    [Test]
    public void RejectsMissingArgumentsAndEveryInvalidOptionShape()
    {
        CborObjectMapper mapper = new CborObjectMapper();
        CborValue one = Integer(1);
        CborMappingOptions invalidNaming = new CborMappingOptions
        {
            MemberNamingPolicy = (CborMappingNamingPolicy)99
        };
        CborMappingOptions invalidKeys = new CborMappingOptions
        {
            DictionaryKeyPolicy = (CborMappingKeyPolicy)99
        };
        CborMappingOptions invalidCollection = new CborMappingOptions
        {
            MaxCollectionItems = -1
        };
        CborMappingOptions invalidTotal = new CborMappingOptions
        {
            MaxTotalItems = -1
        };
        CborMappingOptions missingRegistrations = new CborMappingOptions();
        missingRegistrations.TypeRegistrations = null!;
        CborMappingOptions missingRegistration = new CborMappingOptions();
        missingRegistration.TypeRegistrations.Add(null!);
        CborMappingOptions missingType = new CborMappingOptions();
        missingType.TypeRegistrations.Add(new CborTypeRegistration
        {
            Adapter = new NullValueAdapter()
        });

        Assert.Multiple(() =>
        {
            AssertFailure(() => mapper.MapToValue(1, null!, new CborMappingOptions()),
                "CBOR_MAPPING_RUNTIME_TYPE_MISSING");
            AssertFailure(() => mapper.MapFromValue(null!, typeof(int), new CborMappingOptions()),
                "CBOR_MAPPING_VALUE_MISSING");
            AssertFailure(() => mapper.MapFromValue(one, null!, new CborMappingOptions()),
                "CBOR_MAPPING_TARGET_TYPE_MISSING");
            AssertFailure(() => mapper.MapToValue(1, typeof(int), invalidNaming),
                "CBOR_MAPPING_NAMING_POLICY_INVALID");
            AssertFailure(() => mapper.MapToValue(1, typeof(int), invalidKeys),
                "CBOR_MAPPING_KEY_POLICY_INVALID");
            AssertFailure(() => mapper.MapToValue(1, typeof(int), invalidCollection),
                "CBOR_MAPPING_MAX_COLLECTION_ITEMS_INVALID");
            AssertFailure(() => mapper.MapToValue(1, typeof(int), invalidTotal),
                "CBOR_MAPPING_MAX_TOTAL_ITEMS_INVALID");
            AssertFailure(() => mapper.MapToValue(1, typeof(int), missingRegistrations),
                "CBOR_MAPPING_TYPE_REGISTRATIONS_MISSING");
            AssertFailure(() => mapper.MapToValue(1, typeof(int), missingRegistration),
                "CBOR_MAPPING_TYPE_REGISTRATION_MISSING");
            AssertFailure(() => mapper.MapToValue(1, typeof(int), missingType),
                "CBOR_MAPPING_REGISTERED_TYPE_MISSING");
        });
    }

    [Test]
    public void ReportsWrongScalarRepresentationsAndNumericOverflow()
    {
        CborObjectMapper mapper = new CborObjectMapper();
        CborMappingOptions options = new CborMappingOptions();
        CborValue text = Text("wrong");
        CborValue hugeFloat = new CborValue
        {
            Kind = CborValueKind.Float,
            FloatValue = new CborFloatValue
            {
                Value = double.MaxValue,
                Width = CborFloatWidth.Double
            },
            FloatWidth = CborFloatWidth.Double
        };
        CborValue invalidSign = Integer(1);
        invalidSign.IntegerValue = -BigInteger.One;

        Assert.Multiple(() =>
        {
            AssertFailure(() => mapper.MapFromValue(text, typeof(bool), options),
                "CBOR_MAPPING_BOOLEAN_REQUIRED");
            AssertFailure(() => mapper.MapFromValue(text, typeof(string[]), options),
                "CBOR_MAPPING_ARRAY_REQUIRED");
            AssertFailure(() => mapper.MapFromValue(text, typeof(byte[]), options),
                "CBOR_MAPPING_BYTE_STRING_REQUIRED");
            AssertFailure(() => mapper.MapFromValue(text, typeof(decimal), options),
                "CBOR_MAPPING_DECIMAL_REQUIRED");
            AssertFailure(() => mapper.MapFromValue(text, typeof(DateTimeOffset), options),
                "CBOR_MAPPING_DATE_TIME_REQUIRED");
            AssertFailure(() => mapper.MapFromValue(text, typeof(Uri), options),
                "CBOR_MAPPING_URI_REQUIRED");
            AssertFailure(() => mapper.MapFromValue(hugeFloat, typeof(Half), options),
                "CBOR_MAPPING_FLOAT_OVERFLOW");
            AssertFailure(() => mapper.MapFromValue(invalidSign, typeof(int), options),
                "CBOR_MAPPING_INTEGER_SIGN_INVALID");
        });
    }

    private static CborMappingOptions Registered(Type type, ICborTypeAdapter adapter)
    {
        CborMappingOptions options = new CborMappingOptions();
        options.TypeRegistrations.Add(new CborTypeRegistration
        {
            RuntimeType = type,
            Adapter = adapter
        });
        return options;
    }

    private static void AssertFailure(TestDelegate action, string code)
    {
        CborMappingException error = Assert.Throws<CborMappingException>(action)!;
        AssertCode(error, code);
    }

    private static void AssertCode(CborMappingException error, string code)
    {
        Assert.That(error.ErrorCode, Is.EqualTo(code));
    }

    private static CborValue Array(params CborValue[] items)
    {
        return new CborValue
        {
            Kind = CborValueKind.Array,
            LengthKind = CborLengthKind.Definite,
            ArrayItems = new List<CborValue>(items)
        };
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

    private static CborValue Bytes(byte[] value)
    {
        return new CborValue
        {
            Kind = CborValueKind.ByteString,
            LengthKind = CborLengthKind.Definite,
            ByteStringValue = value
        };
    }

    private static CborValue Null()
    {
        return new CborValue
        {
            Kind = CborValueKind.SimpleValue,
            SimpleValue = CborSimpleValues.Null
        };
    }
}
