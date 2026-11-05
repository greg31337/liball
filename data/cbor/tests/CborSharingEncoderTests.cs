using System.Numerics;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborSharingEncoderTests
{
    [Test]
    public void ResolvePolicyEncodesDirectSharedCyclesAndMeasuresThem()
    {
        CborValue shared = new CborValue
        {
            Kind = CborValueKind.Array,
            ArrayItems = [],
            IsSharedValue = true,
            SharedValueIndex = 0
        };
        shared.ArrayItems.Add(shared);
        CborEncodeOptions options = new CborEncodeOptions
        {
            SharedValuePolicy = CborSharedValuePolicy.Resolve
        };
        CborEncoder encoder = new CborEncoder();

        byte[] encoded = encoder.Encode(shared, options);
        CborReadResult decoded = new CborDecoder().Decode(
            encoded,
            new CborDecodeOptions
            {
                SharedValuePolicy = CborSharedValuePolicy.Resolve
            });

        Assert.Multiple(() =>
        {
            Assert.That(encoded, Is.EqualTo(Convert.FromHexString("D81C81D81D00")));
            Assert.That(encoder.Measure(shared, options), Is.EqualTo(encoded.Length));
            Assert.That(decoded.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(
                decoded.Value?.TaggedValue?.ArrayItems?[0].ReferencedValue,
                Is.SameAs(decoded.Value?.TaggedValue));
        });
    }

    [Test]
    public void ResolvePolicyUsesEncounterOrderForRepeatedSharedNodes()
    {
        CborValue first = Text("first");
        first.IsSharedValue = true;
        first.SharedValueIndex = 0;
        CborValue second = Integer(2);
        second.IsSharedValue = true;
        second.SharedValueIndex = 1;
        CborValue root = new CborValue
        {
            Kind = CborValueKind.Array,
            ArrayItems = [first, second, first, second]
        };
        CborEncodeOptions options = new CborEncodeOptions
        {
            SharedValuePolicy = CborSharedValuePolicy.Resolve
        };

        byte[] encoded = new CborEncoder().Encode(root, options);

        Assert.That(
            encoded,
            Is.EqualTo(Convert.FromHexString(
                "84D81C656669727374D81C02D81D00D81D01")));
    }

    [Test]
    public void ResolvedSharedAndStringReferenceGraphsRoundTripExactly()
    {
        byte[] source = Convert.FromHexString(
            "82D81C81D81D00D901008463666F6FD81900626E6F4401020304");
        CborReadResult decoded = new CborDecoder().Decode(
            source,
            new CborDecodeOptions
            {
                SharedValuePolicy = CborSharedValuePolicy.Resolve
            });
        CborEncodeOptions options = new CborEncodeOptions
        {
            SharedValuePolicy = CborSharedValuePolicy.Resolve
        };
        CborEncoder encoder = new CborEncoder();

        byte[] encoded = encoder.Encode(decoded.Value!, options);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(encoded, Is.EqualTo(source));
            Assert.That(encoder.Measure(decoded.Value!, options), Is.EqualTo(source.Length));
        });
    }

    [Test]
    public void PreservePolicyValidatesAndEmitsExplicitSharingTags()
    {
        CborValue shared = Integer(1);
        CborValue root = new CborValue
        {
            Kind = CborValueKind.Array,
            ArrayItems =
            [
                Tag(CborTagNumbers.ShareableValue, shared),
                Tag(CborTagNumbers.SharedValueReference, Integer(0))
            ]
        };
        CborEncodeOptions options = new CborEncodeOptions
        {
            SharedValuePolicy = CborSharedValuePolicy.Preserve
        };

        byte[] encoded = new CborEncoder().Encode(root, options);

        Assert.That(encoded, Is.EqualTo(Convert.FromHexString("82D81C01D81D00")));
    }

    [Test]
    public void StringReferenceThresholdRejectsAnUnregisteredShortString()
    {
        CborValue value = Tag(
            CborTagNumbers.StringReferenceNamespace,
            new CborValue
            {
                Kind = CborValueKind.Array,
                ArrayItems =
                [
                    Text("no"),
                    Tag(CborTagNumbers.StringReference, Integer(0))
                ]
            });
        CborEncodeOptions options = new CborEncodeOptions
        {
            SharedValuePolicy = CborSharedValuePolicy.Preserve
        };

        CborEncodeException? failure = Assert.Throws<CborEncodeException>(
            () => new CborEncoder().Measure(value, options));

        Assert.That(failure?.ErrorCode, Is.EqualTo("CBOR_STRING_REFERENCE_NOT_FOUND"));
    }

    [Test]
    public void StaleAndForwardReferencesFailAtomically()
    {
        CborValue target = Integer(1);
        CborValue reference = Tag(CborTagNumbers.SharedValueReference, Integer(1));
        reference.SharedValueIndex = 0;
        reference.ReferencedValue = target;
        CborValue value = new CborValue
        {
            Kind = CborValueKind.Array,
            ArrayItems =
            [
                Tag(CborTagNumbers.ShareableValue, target),
                reference
            ]
        };
        CborEncodeOptions options = new CborEncodeOptions
        {
            SharedValuePolicy = CborSharedValuePolicy.Resolve
        };
        byte[] destination = [0xa5, 0xa5, 0xa5, 0xa5, 0xa5, 0xa5, 0xa5];

        CborWriteResult result = new CborEncoder().EncodeInto(
            value,
            destination,
            options);
        CborValue forward = new CborValue
        {
            Kind = CborValueKind.Array,
            ArrayItems =
            [
                Tag(CborTagNumbers.SharedValueReference, Integer(0)),
                Tag(CborTagNumbers.ShareableValue, Integer(1))
            ]
        };
        CborEncodeException? forwardFailure = Assert.Throws<CborEncodeException>(
            () => new CborEncoder().Measure(
                forward,
                new CborEncodeOptions
                {
                    SharedValuePolicy = CborSharedValuePolicy.Preserve
                }));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CborWriteStatus.Error));
            Assert.That(result.ErrorCode, Is.EqualTo("CBOR_REFERENCE_INDEX_MISMATCH"));
            Assert.That(destination, Is.All.EqualTo(0xa5));
            Assert.That(
                forwardFailure?.ErrorCode,
                Is.EqualTo("CBOR_SHARED_REFERENCE_NOT_FOUND"));
        });
    }

    [Test]
    public void SharedValueLimitAndDisabledPolicyAreEnforced()
    {
        CborValue shared = Integer(1);
        shared.IsSharedValue = true;
        CborEncoder encoder = new CborEncoder();
        CborEncodeException? limitFailure = Assert.Throws<CborEncodeException>(
            () => encoder.Measure(
                shared,
                new CborEncodeOptions
                {
                    SharedValuePolicy = CborSharedValuePolicy.Resolve,
                    MaxSharedValues = 0
                }));
        CborEncodeException? disabledFailure = Assert.Throws<CborEncodeException>(
            () => encoder.Measure(
                Tag(CborTagNumbers.ShareableValue, Integer(0)),
                new CborEncodeOptions()));

        Assert.Multiple(() =>
        {
            Assert.That(limitFailure?.ErrorCode, Is.EqualTo("CBOR_MAX_SHARED_VALUES"));
            Assert.That(disabledFailure?.ErrorCode, Is.EqualTo("CBOR_REFERENCE_TAG_DISABLED"));
        });
    }

    [TestCase(CborEncodingProfile.CoreDeterministic)]
    [TestCase(CborEncodingProfile.LengthFirstCoreDeterministic)]
    public void DeterministicProfilesRejectEnabledReferenceProcessing(
        CborEncodingProfile profile)
    {
        CborEncodeOptions options = new CborEncodeOptions
        {
            EncodingProfile = profile,
            SharedValuePolicy = CborSharedValuePolicy.Preserve
        };

        CborEncodeException? failure = Assert.Throws<CborEncodeException>(
            () => new CborEncoder().Measure(Integer(0), options));

        Assert.That(
            failure?.ErrorCode,
            Is.EqualTo("CBOR_DETERMINISTIC_REFERENCES_UNSUPPORTED"));
    }

    private static CborValue Integer(int value)
    {
        return new CborValue
        {
            Kind = CborValueKind.UnsignedInteger,
            IntegerValue = new BigInteger(value)
        };
    }

    private static CborValue Text(string value)
    {
        return new CborValue
        {
            Kind = CborValueKind.TextString,
            TextStringValue = value
        };
    }

    private static CborValue Tag(ulong tagNumber, CborValue content)
    {
        return new CborValue
        {
            Kind = CborValueKind.Tag,
            TagNumber = tagNumber,
            TaggedValue = content
        };
    }
}
