using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborTagRegistryTests
{
    [Test]
    public void RegistryStoresAndReturnsRegistrationSnapshots()
    {
        CborTagRegistry registry = new CborTagRegistry();
        ValidTagHandler handler = new ValidTagHandler();
        CborTagRegistration registration = new CborTagRegistration
        {
            TagNumber = 100,
            Name = "original",
            Handler = handler,
            ValidateContent = true
        };

        registry.AddTag(registration);
        registration.Name = "mutated";
        registration.ValidateContent = false;
        CborTagRegistration? first = registry.FindTag(100);
        first!.Name = "returned mutation";
        CborTagRegistration? second = registry.FindTag(100);

        Assert.Multiple(() =>
        {
            Assert.That(first.TagNumber, Is.EqualTo(100));
            Assert.That(first.Handler, Is.SameAs(handler));
            Assert.That(second, Is.Not.Null);
            Assert.That(second!.Name, Is.EqualTo("original"));
            Assert.That(second.ValidateContent, Is.True);
            Assert.That(registry.FindTag(101), Is.Null);
        });
    }

    [Test]
    public void RegistryRejectsMissingAndDuplicateRegistrations()
    {
        CborTagRegistry registry = new CborTagRegistry();
        registry.AddTag(new CborTagRegistration { TagNumber = 100 });

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => registry.AddTag(null!));
            Assert.Throws<ArgumentException>(() => registry.AddTag(
                new CborTagRegistration { TagNumber = 100 }));
        });
    }

    [Test]
    public void UnknownTagsRemainGenericUnlessPolicyRejectsThem()
    {
        byte[] source = Convert.FromHexString("D86400");
        CborDecoder decoder = new CborDecoder();
        CborReadResult preserved = decoder.Decode(
            source,
            new CborDecodeOptions { TagPolicy = CborTagPolicy.Preserve });
        CborReadResult validateKnown = decoder.Decode(
            source,
            new CborDecodeOptions { TagPolicy = CborTagPolicy.ValidateKnown });
        CborReadResult rejected = decoder.Decode(
            source,
            new CborDecodeOptions { TagPolicy = CborTagPolicy.RejectUnknown });
        CborReadResult forbidden = decoder.Decode(
            source,
            new CborDecodeOptions { TagPolicy = CborTagPolicy.Forbid });

        Assert.Multiple(() =>
        {
            AssertGenericTag(preserved, 100);
            AssertGenericTag(validateKnown, 100);
            Assert.That(rejected.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(rejected.ErrorCode, Is.EqualTo("CBOR_UNKNOWN_TAG"));
            Assert.That(forbidden.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(forbidden.ErrorCode, Is.EqualTo("CBOR_TAG_FORBIDDEN"));
        });
    }

    [Test]
    public void RegisteredHandlersValidateDecodedGenericTags()
    {
        InvalidTagHandler handler = new InvalidTagHandler();
        CborDecodeOptions validating = new CborDecodeOptions
        {
            TagPolicy = CborTagPolicy.ValidateKnown,
            TagRegistrations =
            [
                new CborTagRegistration
                {
                    TagNumber = 100,
                    Handler = handler,
                    ValidateContent = true
                }
            ]
        };
        CborReadResult invalid = new CborDecoder().Decode(
            Convert.FromHexString("D86400"),
            validating);
        validating.TagPolicy = CborTagPolicy.Preserve;
        CborReadResult preserved = new CborDecoder().Decode(
            Convert.FromHexString("D86400"),
            validating);

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(invalid.ErrorCode, Is.EqualTo("CBOR_TEST_TAG_INVALID"));
            Assert.That(handler.ValidationCalls, Is.EqualTo(1));
            AssertGenericTag(preserved, 100);
        });
    }

    [Test]
    public void RegistrationLookupIsImmutableDuringOneOperation()
    {
        CborDecodeOptions options = new CborDecodeOptions
        {
            TagPolicy = CborTagPolicy.RejectUnknown
        };
        MutatingTagHandler outer = new MutatingTagHandler();
        outer.Options = options;
        options.TagRegistrations.Add(new CborTagRegistration
        {
            TagNumber = 100,
            Handler = outer
        });
        byte[] source = Convert.FromHexString("D864D86500");
        CborDecoder decoder = new CborDecoder();

        CborReadResult first = decoder.Decode(source, options);
        CborReadResult second = decoder.Decode(source, options);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(first.ErrorCode, Is.EqualTo("CBOR_UNKNOWN_TAG"));
            Assert.That(options.TagRegistrations, Has.Count.EqualTo(2));
            Assert.That(second.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(outer.ValidationCalls, Is.EqualTo(2));
            Assert.That(outer.Inner.ValidationCalls, Is.EqualTo(1));
        });
    }

    [Test]
    public void DuplicateOperationRegistrationsFailConsistently()
    {
        CborTagRegistration first = new CborTagRegistration { TagNumber = 100 };
        CborTagRegistration second = new CborTagRegistration { TagNumber = 100 };
        CborDecodeOptions decodeOptions = new CborDecodeOptions
        {
            TagRegistrations = [first, second]
        };
        CborEncodeOptions encodeOptions = new CborEncodeOptions
        {
            TagRegistrations = [first, second]
        };
        CborReadResult decoded = new CborDecoder().Decode([0x00], decodeOptions);
        CborValidationResult validated = new CborValidator().ValidateEncodingWithOptions(
            [0x00],
            decodeOptions);
        CborEncodeException? encoded = Assert.Throws<CborEncodeException>(() =>
            new CborEncoder().Measure(Integer(0), encodeOptions));

        Assert.Multiple(() =>
        {
            Assert.That(decoded.ErrorCode,
                Is.EqualTo("CBOR_TAG_REGISTRATION_DUPLICATE"));
            Assert.That(validated.Issues[0].Code,
                Is.EqualTo("CBOR_TAG_REGISTRATION_DUPLICATE"));
            Assert.That(encoded?.ErrorCode,
                Is.EqualTo("CBOR_TAG_REGISTRATION_DUPLICATE"));
        });
    }

    private static CborValue Integer(int value)
    {
        return new CborValue
        {
            Kind = CborValueKind.UnsignedInteger,
            IntegerValue = new System.Numerics.BigInteger(value)
        };
    }

    private static void AssertGenericTag(CborReadResult result, ulong tagNumber)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(result.Value?.Kind, Is.EqualTo(CborValueKind.Tag));
            Assert.That(result.Value?.TagNumber, Is.EqualTo(tagNumber));
            Assert.That(result.Value?.TaggedValue?.Kind,
                Is.EqualTo(CborValueKind.UnsignedInteger));
        });
    }

    private class ValidTagHandler : ICborTagHandler
    {
        public int ValidationCalls;

        public CborTagResult ValidateTag(ulong tagNumber, CborValue content)
        {
            ValidationCalls = ValidationCalls + 1;
            return new CborTagResult { Handled = true, IsValid = true };
        }

        public CborTagResult DecodeTag(ulong tagNumber, CborValue content)
        {
            return new CborTagResult();
        }

        public CborTagResult EncodeTag(ulong tagNumber, object nativeValue)
        {
            return new CborTagResult();
        }
    }

    private class InvalidTagHandler : ICborTagHandler
    {
        public int ValidationCalls;

        public CborTagResult ValidateTag(ulong tagNumber, CborValue content)
        {
            ValidationCalls = ValidationCalls + 1;
            return new CborTagResult
            {
                Handled = true,
                IsValid = false,
                ErrorCode = "CBOR_TEST_TAG_INVALID"
            };
        }

        public CborTagResult DecodeTag(ulong tagNumber, CborValue content)
        {
            return new CborTagResult();
        }

        public CborTagResult EncodeTag(ulong tagNumber, object nativeValue)
        {
            return new CborTagResult();
        }
    }

    private class MutatingTagHandler : ICborTagHandler
    {
        public CborDecodeOptions? Options;
        public ValidTagHandler Inner = new ValidTagHandler();
        public int ValidationCalls;
        public bool Registered;

        public CborTagResult ValidateTag(ulong tagNumber, CborValue content)
        {
            ValidationCalls = ValidationCalls + 1;
            if (!Registered)
            {
                Options!.TagRegistrations.Add(new CborTagRegistration
                {
                    TagNumber = 101,
                    Handler = Inner
                });
                Registered = true;
            }
            return new CborTagResult { Handled = true, IsValid = true };
        }

        public CborTagResult DecodeTag(ulong tagNumber, CborValue content)
        {
            return new CborTagResult();
        }

        public CborTagResult EncodeTag(ulong tagNumber, object nativeValue)
        {
            return new CborTagResult();
        }
    }
}
