using System.Numerics;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class GenericModelCoverageTests
{
    [Test]
    public void GenericModelObjectsStartWithNeutralPayloads()
    {
        CborValue value = new CborValue();
        CborMapEntry entry = new CborMapEntry();
        CborFloatValue floatValue = new CborFloatValue();
        CborRepresentation representation = new CborRepresentation();
        CborToken token = new CborToken();

        Assert.Multiple(() =>
        {
            Assert.That(value.Kind, Is.EqualTo(CborValueKind.None));
            Assert.That(value.IntegerValue, Is.EqualTo(BigInteger.Zero));
            Assert.That(value.ByteStringValue, Is.Null);
            Assert.That(value.TextStringValue, Is.Null);
            Assert.That(value.ArrayItems, Is.Null);
            Assert.That(value.MapEntries, Is.Null);
            Assert.That(value.TagNumber, Is.Zero);
            Assert.That(value.TaggedValue, Is.Null);
            Assert.That(value.FloatValue, Is.Null);
            Assert.That(value.FloatWidth, Is.EqualTo(CborFloatWidth.None));
            Assert.That(value.SimpleValue, Is.Zero);
            Assert.That(value.LengthKind, Is.EqualTo(CborLengthKind.None));
            Assert.That(value.Representation, Is.Null);
            Assert.That(value.IsSharedValue, Is.False);
            Assert.That(value.SharedValueIndex, Is.EqualTo(-1));
            Assert.That(value.ReferencedValue, Is.Null);
            Assert.That(entry.Key, Is.Null);
            Assert.That(entry.Value, Is.Null);
            Assert.That(floatValue.Value, Is.Zero);
            Assert.That(floatValue.Width, Is.EqualTo(CborFloatWidth.None));
            Assert.That(floatValue.RawBits, Is.Zero);
            Assert.That(floatValue.IsNaN, Is.False);
            Assert.That(floatValue.IsSignalingNaN, Is.False);
            Assert.That(representation.HeadArgumentWidth, Is.Zero);
            Assert.That(representation.LengthKind, Is.EqualTo(CborLengthKind.None));
            Assert.That(representation.ChunkByteLengths, Is.Null);
            Assert.That(representation.HasFloatBits, Is.False);
            Assert.That(representation.HasSourceRange, Is.False);
            Assert.That(token.Kind, Is.EqualTo(CborTokenKind.None));
            Assert.That(token.FloatWidth, Is.EqualTo(CborFloatWidth.None));
            Assert.That(token.LengthKind, Is.EqualTo(CborLengthKind.None));
            Assert.That(token.IntegerValue, Is.EqualTo(BigInteger.Zero));
            Assert.That(token.ByteStringValue, Is.Null);
            Assert.That(token.TextStringValue, Is.Null);
            Assert.That(token.Length, Is.Zero);
            Assert.That(token.TagNumber, Is.Zero);
            Assert.That(token.SimpleValue, Is.Zero);
            Assert.That(token.FloatValue, Is.Null);
            Assert.That(token.Representation, Is.Null);
        });
    }

    [Test]
    public void RepresentationMetadataRetainsEveryIndependentDimension()
    {
        List<int> chunks = [0, 23, 256];
        CborRepresentation representation = new CborRepresentation
        {
            HeadArgumentWidth = 8,
            LengthKind = CborLengthKind.Indefinite,
            ChunkByteLengths = chunks,
            FloatWidth = CborFloatWidth.Double,
            FloatBits = 0x8000000000000000,
            HasFloatBits = true,
            HasSourceRange = true,
            SourceOffset = 4294967296,
            SourceLength = 9
        };
        CborValue value = new CborValue
        {
            Kind = CborValueKind.Float,
            FloatValue = CborHalfPrecision.CreateDoubleFloat(0x8000000000000000),
            FloatWidth = CborFloatWidth.Double,
            LengthKind = CborLengthKind.Indefinite,
            Representation = representation
        };

        Assert.Multiple(() =>
        {
            Assert.That(representation.HeadArgumentWidth, Is.EqualTo(8));
            Assert.That(representation.LengthKind, Is.EqualTo(CborLengthKind.Indefinite));
            Assert.That(representation.ChunkByteLengths, Is.SameAs(chunks));
            Assert.That(representation.ChunkByteLengths, Is.EqualTo(new[] { 0, 23, 256 }));
            Assert.That(representation.FloatWidth, Is.EqualTo(CborFloatWidth.Double));
            Assert.That(representation.FloatBits, Is.EqualTo(0x8000000000000000UL));
            Assert.That(representation.HasFloatBits, Is.True);
            Assert.That(representation.HasSourceRange, Is.True);
            Assert.That(representation.SourceOffset, Is.EqualTo(4294967296));
            Assert.That(representation.SourceLength, Is.EqualTo(9));
            Assert.That(value.Representation, Is.SameAs(representation));
        });
    }

    [Test]
    public void KeyEquivalenceKeepsGenericDataModelGroupsDistinct()
    {
        CborValue bytes = new CborValue { Kind = CborValueKind.ByteString, ByteStringValue = [0x32] };
        CborValue matchingBytes = new CborValue { Kind = CborValueKind.ByteString, ByteStringValue = [0x32] };
        CborValue text = new CborValue { Kind = CborValueKind.TextString, TextStringValue = "2" };
        CborValue integer = new CborValue
        {
            Kind = CborValueKind.UnsignedInteger,
            IntegerValue = new BigInteger(2)
        };
        CborValue simple = new CborValue { Kind = CborValueKind.SimpleValue, SimpleValue = 2 };
        CborValue tagged = new CborValue
        {
            Kind = CborValueKind.Tag,
            TagNumber = 2,
            TaggedValue = bytes
        };
        CborValue orderedArray = new CborValue
        {
            Kind = CborValueKind.Array,
            ArrayItems = [integer, simple]
        };
        CborValue reversedArray = new CborValue
        {
            Kind = CborValueKind.Array,
            ArrayItems = [simple, integer]
        };

        Assert.Multiple(() =>
        {
            Assert.That(CborKeyComparer.AreEquivalent(bytes, matchingBytes), Is.True);
            Assert.That(CborKeyComparer.GetEquivalentHash(bytes),
                Is.EqualTo(CborKeyComparer.GetEquivalentHash(matchingBytes)));
            Assert.That(CborKeyComparer.AreEquivalent(bytes, text), Is.False);
            Assert.That(CborKeyComparer.AreEquivalent(integer, simple), Is.False);
            Assert.That(CborKeyComparer.AreEquivalent(tagged, bytes), Is.False);
            Assert.That(CborKeyComparer.AreEquivalent(orderedArray, reversedArray), Is.False);
            Assert.That(CborKeyComparer.AreEquivalent(null, null), Is.True);
            Assert.That(CborKeyComparer.AreEquivalent(null, integer), Is.False);
        });
    }

    [Test]
    public void NanSignsAreIgnoredButSignificandsRemainDistinct()
    {
        CborValue positiveNan = WrapFloat(CborHalfPrecision.CreateHalfFloat(0x7e01));
        CborValue negativeNan = WrapFloat(CborHalfPrecision.CreateHalfFloat(0xfe01));
        CborValue differentNan = WrapFloat(CborHalfPrecision.CreateHalfFloat(0x7e02));

        Assert.Multiple(() =>
        {
            Assert.That(CborKeyComparer.AreEquivalent(positiveNan, negativeNan), Is.True);
            Assert.That(CborKeyComparer.GetEquivalentHash(positiveNan),
                Is.EqualTo(CborKeyComparer.GetEquivalentHash(negativeNan)));
            Assert.That(CborKeyComparer.AreEquivalent(positiveNan, differentNan), Is.False);
        });
    }

    [Test]
    public void ResolvedSharedReferenceCyclesCompareByGraphStructure()
    {
        CborValue leftShared = new CborValue
        {
            Kind = CborValueKind.Array,
            ArrayItems = [],
            IsSharedValue = true,
            SharedValueIndex = 0
        };
        CborValue leftReference = CreateReference(leftShared, 0);
        leftShared.ArrayItems.Add(leftReference);

        CborValue rightShared = new CborValue
        {
            Kind = CborValueKind.Array,
            ArrayItems = [],
            IsSharedValue = true,
            SharedValueIndex = 10
        };
        CborValue rightReference = CreateReference(rightShared, 0);
        rightReference.SharedValueIndex = 10;
        rightShared.ArrayItems.Add(rightReference);

        CborValue differentShared = new CborValue
        {
            Kind = CborValueKind.Array,
            ArrayItems = []
        };
        CborValue selfReferencingNode = CreateReference(differentShared, 0);
        selfReferencingNode.ReferencedValue = selfReferencingNode;
        differentShared.ArrayItems.Add(selfReferencingNode);

        Assert.Multiple(() =>
        {
            Assert.That(CborValueComparer.AreEqual(leftShared, rightShared), Is.True);
            Assert.That(CborKeyComparer.GetEquivalentHash(leftShared),
                Is.EqualTo(CborKeyComparer.GetEquivalentHash(rightShared)));
            Assert.That(CborValueComparer.AreEqual(leftShared, differentShared), Is.False);
        });
    }

    [Test]
    public void PopulatedResultsRetainSuccessProgressAndFailureMetadata()
    {
        CborValue value = new CborValue { Kind = CborValueKind.SimpleValue, SimpleValue = 16 };
        CborToken token = new CborToken { Kind = CborTokenKind.SimpleValue };
        CborReadResult read = new CborReadResult
        {
            Status = CborReadStatus.Done,
            Value = value,
            Consumed = 3,
            Remaining = 2
        };
        CborTokenReadResult tokenRead = new CborTokenReadResult
        {
            Status = CborReadStatus.NeedMoreData,
            Token = token,
            Consumed = 1,
            Remaining = 0
        };
        CborWriteResult write = new CborWriteResult
        {
            Status = CborWriteStatus.DestinationTooSmall,
            Written = 0,
            Required = 9,
            ErrorPath = "$[1]"
        };
        CborReadResult failure = new CborReadResult
        {
            Status = CborReadStatus.Error,
            ErrorCode = "CBOR_TEST_ERROR",
            ErrorMessage = "failure",
            ErrorOffset = 7,
            ErrorPath = "$.key"
        };
        CborValidationIssue issue = new CborValidationIssue
        {
            Severity = CborIssueSeverity.Warning,
            Code = "CBOR_TEST_WARNING",
            Message = "warning",
            ByteOffset = 4,
            Path = "$[0]"
        };
        CborValidationResult validation = new CborValidationResult
        {
            IsValid = true,
            Issues = [issue]
        };

        Assert.Multiple(() =>
        {
            Assert.That(read.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(read.Value, Is.SameAs(value));
            Assert.That(read.Consumed, Is.EqualTo(3));
            Assert.That(read.Remaining, Is.EqualTo(2));
            Assert.That(tokenRead.Status, Is.EqualTo(CborReadStatus.NeedMoreData));
            Assert.That(tokenRead.Token, Is.SameAs(token));
            Assert.That(tokenRead.Consumed, Is.EqualTo(1));
            Assert.That(write.Status, Is.EqualTo(CborWriteStatus.DestinationTooSmall));
            Assert.That(write.Required, Is.EqualTo(9));
            Assert.That(write.ErrorPath, Is.EqualTo("$[1]"));
            Assert.That(failure.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(failure.ErrorCode, Is.EqualTo("CBOR_TEST_ERROR"));
            Assert.That(failure.ErrorMessage, Is.EqualTo("failure"));
            Assert.That(failure.ErrorOffset, Is.EqualTo(7));
            Assert.That(failure.ErrorPath, Is.EqualTo("$.key"));
            Assert.That(validation.IsValid, Is.True);
            Assert.That(validation.Issues, Has.Length.EqualTo(1));
            Assert.That(validation.Issues[0], Is.SameAs(issue));
        });
    }

    private static CborValue WrapFloat(CborFloatValue value)
    {
        return new CborValue
        {
            Kind = CborValueKind.Float,
            FloatValue = value,
            FloatWidth = value.Width
        };
    }

    private static CborValue CreateReference(CborValue target, int index)
    {
        return new CborValue
        {
            Kind = CborValueKind.Tag,
            TagNumber = CborTagNumbers.SharedValueReference,
            TaggedValue = new CborValue
            {
                Kind = CborValueKind.UnsignedInteger,
                IntegerValue = new BigInteger(index)
            },
            SharedValueIndex = index,
            ReferencedValue = target
        };
    }
}
