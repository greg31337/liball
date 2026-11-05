using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborObjectMapperAdapterReferenceTests
{
    public class CustomIdentifier
    {
        public string Value = "";
    }

    public class CustomPair
    {
        public CustomIdentifier Left = new CustomIdentifier();
        public CustomIdentifier Right = new CustomIdentifier();
    }

    public class Link
    {
        public string Name = "";
        public Link? Next;
    }

    public class LinkPair
    {
        public Link Left = new Link();
        public Link Right = new Link();
    }

    private sealed class IdentifierAdapter : ICborTypeAdapter
    {
        public int EncodeCalls;
        public int DecodeCalls;
        public int LastDepth;
        public int LastItemCount;
        public CborMappingOptions? LastOptions;

        public bool SupportsType(Type runtimeType)
        {
            return runtimeType == typeof(CustomIdentifier);
        }

        public CborValue EncodeObject(object value, CborMappingContext context)
        {
            EncodeCalls++;
            LastDepth = context.Depth;
            LastItemCount = context.ItemCount;
            LastOptions = context.Options;
            CustomIdentifier identifier = (CustomIdentifier)value;
            return new CborValue
            {
                Kind = CborValueKind.TextString,
                LengthKind = CborLengthKind.Definite,
                TextStringValue = "id:" + identifier.Value
            };
        }

        public object? DecodeObject(
            CborValue value,
            Type targetType,
            CborMappingContext context)
        {
            DecodeCalls++;
            LastDepth = context.Depth;
            LastItemCount = context.ItemCount;
            LastOptions = context.Options;
            string text = value.TextStringValue ?? "";
            return new CustomIdentifier
            {
                Value = text.StartsWith("id:", StringComparison.Ordinal)
                    ? text.Substring(3)
                    : text
            };
        }
    }

    private sealed class RejectingAdapter : ICborTypeAdapter
    {
        public bool SupportsType(Type runtimeType)
        {
            return false;
        }

        public CborValue EncodeObject(object value, CborMappingContext context)
        {
            return new CborValue();
        }

        public object? DecodeObject(
            CborValue value,
            Type targetType,
            CborMappingContext context)
        {
            return null;
        }
    }

    [Test]
    public void RegisteredAdapterOverridesReflectionAndReceivesMappingState()
    {
        IdentifierAdapter adapter = new IdentifierAdapter();
        CborMappingOptions options = OptionsWith(adapter);
        CborObjectMapper mapper = new CborObjectMapper();
        CustomIdentifier source = new CustomIdentifier { Value = "abc" };

        CborValue encoded = mapper.MapToValue(
            source,
            typeof(CustomIdentifier),
            options);
        CustomIdentifier decoded = (CustomIdentifier)mapper.MapFromValue(
            encoded,
            typeof(CustomIdentifier),
            options)!;

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Kind, Is.EqualTo(CborValueKind.TextString));
            Assert.That(encoded.TextStringValue, Is.EqualTo("id:abc"));
            Assert.That(decoded.Value, Is.EqualTo("abc"));
            Assert.That(adapter.EncodeCalls, Is.EqualTo(1));
            Assert.That(adapter.DecodeCalls, Is.EqualTo(1));
            Assert.That(adapter.LastDepth, Is.EqualTo(0));
            Assert.That(adapter.LastItemCount, Is.EqualTo(1));
            Assert.That(adapter.LastOptions, Is.SameAs(options));
        });
    }

    [Test]
    public void RegistrationValidationRejectsDuplicatesMissingAdaptersAndUnsupportedTypes()
    {
        CborObjectMapper mapper = new CborObjectMapper();
        IdentifierAdapter adapter = new IdentifierAdapter();
        CborTypeRegistration registration = new CborTypeRegistration
        {
            RuntimeType = typeof(CustomIdentifier),
            Adapter = adapter
        };
        CborMappingOptions duplicate = new CborMappingOptions();
        duplicate.TypeRegistrations.Add(registration);
        duplicate.TypeRegistrations.Add(registration);
        CborMappingOptions missing = new CborMappingOptions();
        missing.TypeRegistrations.Add(new CborTypeRegistration
        {
            RuntimeType = typeof(CustomIdentifier)
        });
        CborMappingOptions unsupported = new CborMappingOptions();
        unsupported.TypeRegistrations.Add(new CborTypeRegistration
        {
            RuntimeType = typeof(CustomIdentifier),
            Adapter = new RejectingAdapter()
        });
        CustomIdentifier value = new CustomIdentifier();

        CborMappingException duplicateError = Assert.Throws<CborMappingException>(() =>
            mapper.MapToValue(value, typeof(CustomIdentifier), duplicate))!;
        CborMappingException missingError = Assert.Throws<CborMappingException>(() =>
            mapper.MapToValue(value, typeof(CustomIdentifier), missing))!;
        CborMappingException unsupportedError = Assert.Throws<CborMappingException>(() =>
            mapper.MapToValue(value, typeof(CustomIdentifier), unsupported))!;

        Assert.Multiple(() =>
        {
            Assert.That(duplicateError.ErrorCode,
                Is.EqualTo("CBOR_MAPPING_TYPE_REGISTRATION_DUPLICATE"));
            Assert.That(missingError.ErrorCode,
                Is.EqualTo("CBOR_MAPPING_TYPE_ADAPTER_MISSING"));
            Assert.That(unsupportedError.ErrorCode,
                Is.EqualTo("CBOR_MAPPING_ADAPTER_TYPE_UNSUPPORTED"));
        });
    }

    [Test]
    public void PreservesSelfReferencingObjectsAsCborIdentityCycles()
    {
        Link source = new Link { Name = "root" };
        source.Next = source;
        CborMappingOptions options = new CborMappingOptions
        {
            PreserveObjectReferences = true
        };
        CborObjectMapper mapper = new CborObjectMapper();

        CborValue encoded = mapper.MapToValue(source, typeof(Link), options);
        Link decoded = (Link)mapper.MapFromValue(encoded, typeof(Link), options)!;

        Assert.Multiple(() =>
        {
            Assert.That(encoded.IsSharedValue, Is.True);
            Assert.That(encoded.MapEntries?[1].Value, Is.SameAs(encoded));
            Assert.That(decoded.Name, Is.EqualTo("root"));
            Assert.That(decoded.Next, Is.SameAs(decoded));
        });
    }

    [Test]
    public void PreservesRepeatedBuiltInAndAdapterMappedObjects()
    {
        Link sharedLink = new Link { Name = "shared" };
        LinkPair links = new LinkPair { Left = sharedLink, Right = sharedLink };
        CustomIdentifier sharedIdentifier = new CustomIdentifier { Value = "same" };
        CustomPair identifiers = new CustomPair
        {
            Left = sharedIdentifier,
            Right = sharedIdentifier
        };
        IdentifierAdapter adapter = new IdentifierAdapter();
        CborMappingOptions options = OptionsWith(adapter);
        options.PreserveObjectReferences = true;
        CborObjectMapper mapper = new CborObjectMapper();

        CborValue encodedLinks = mapper.MapToValue(links, typeof(LinkPair), options);
        LinkPair decodedLinks = (LinkPair)mapper.MapFromValue(
            encodedLinks,
            typeof(LinkPair),
            options)!;
        CborValue encodedIdentifiers = mapper.MapToValue(
            identifiers,
            typeof(CustomPair),
            options);
        CustomPair decodedIdentifiers = (CustomPair)mapper.MapFromValue(
            encodedIdentifiers,
            typeof(CustomPair),
            options)!;

        Assert.Multiple(() =>
        {
            Assert.That(encodedLinks.MapEntries?[0].Value,
                Is.SameAs(encodedLinks.MapEntries?[1].Value));
            Assert.That(encodedLinks.MapEntries?[0].Value?.IsSharedValue, Is.True);
            Assert.That(decodedLinks.Left, Is.SameAs(decodedLinks.Right));
            Assert.That(encodedIdentifiers.MapEntries?[0].Value,
                Is.SameAs(encodedIdentifiers.MapEntries?[1].Value));
            Assert.That(decodedIdentifiers.Left, Is.SameAs(decodedIdentifiers.Right));
            Assert.That(adapter.EncodeCalls, Is.EqualTo(1));
            Assert.That(adapter.DecodeCalls, Is.EqualTo(1));
        });
    }

    [Test]
    public void SharedGraphSurvivesBinaryResolveRoundTrip()
    {
        Link source = new Link { Name = "binary" };
        source.Next = source;
        CborMappingOptions mappingOptions = new CborMappingOptions
        {
            PreserveObjectReferences = true
        };
        CborObjectMapper mapper = new CborObjectMapper();
        CborValue mapped = mapper.MapToValue(source, typeof(Link), mappingOptions);
        byte[] bytes = new CborEncoder().Encode(
            mapped,
            new CborEncodeOptions
            {
                SharedValuePolicy = CborSharedValuePolicy.Resolve
            });
        CborReadResult read = new CborDecoder().Decode(
            bytes,
            new CborDecodeOptions
            {
                SharedValuePolicy = CborSharedValuePolicy.Resolve
            });

        Link decoded = (Link)mapper.MapFromValue(
            read.Value!,
            typeof(Link),
            mappingOptions)!;

        Assert.Multiple(() =>
        {
            Assert.That(read.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(decoded.Name, Is.EqualTo("binary"));
            Assert.That(decoded.Next, Is.SameAs(decoded));
        });
    }

    [Test]
    public void CyclesRemainRejectedWhenReferencePreservationIsDisabled()
    {
        Link source = new Link { Name = "cycle" };
        source.Next = source;
        CborObjectMapper mapper = new CborObjectMapper();

        CborMappingException error = Assert.Throws<CborMappingException>(() =>
            mapper.MapToValue(source, typeof(Link), new CborMappingOptions()))!;

        Assert.Multiple(() =>
        {
            Assert.That(error.ErrorCode, Is.EqualTo("CBOR_MAPPING_CYCLE"));
            Assert.That(error.ErrorPath, Is.EqualTo("$.Next"));
        });
    }

    private static CborMappingOptions OptionsWith(ICborTypeAdapter adapter)
    {
        CborMappingOptions options = new CborMappingOptions();
        options.TypeRegistrations.Add(new CborTypeRegistration
        {
            RuntimeType = typeof(CustomIdentifier),
            Adapter = adapter
        });
        return options;
    }
}
