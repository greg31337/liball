using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborSharingTests
{
    [Test]
    public void DisabledPolicyRejectsEveryReferenceRelatedTag()
    {
        byte[][] encodings =
        [
            [0xd8, 0x19, 0x00],
            [0xd9, 0x01, 0x00, 0x00],
            [0xd8, 0x1c, 0x00],
            [0xd8, 0x1d, 0x00]
        ];

        foreach (byte[] encoding in encodings)
        {
            CborReadResult result = new CborDecoder().Decode(
                encoding,
                new CborDecodeOptions());
            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(CborReadStatus.Error));
                Assert.That(result.ErrorCode, Is.EqualTo("CBOR_REFERENCE_TAG_DISABLED"));
                Assert.That(result.Value, Is.Null);
                Assert.That(result.Consumed, Is.Zero);
                Assert.That(result.Remaining, Is.EqualTo(encoding.Length));
            });
        }
    }

    [Test]
    public void PreservePolicyKeepsSharingTagsWithoutGraphAnnotations()
    {
        CborReadResult result = Decode(
            [0x82, 0xd8, 0x1c, 0x61, 0x78, 0xd8, 0x1d, 0x00],
            CborSharedValuePolicy.Preserve);
        CborValue shareableTag = result.Value!.ArrayItems![0];
        CborValue referenceTag = result.Value.ArrayItems[1];

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(shareableTag.Kind, Is.EqualTo(CborValueKind.Tag));
            Assert.That(shareableTag.TagNumber, Is.EqualTo(CborTagNumbers.ShareableValue));
            Assert.That(shareableTag.TaggedValue?.TextStringValue, Is.EqualTo("x"));
            Assert.That(shareableTag.TaggedValue?.IsSharedValue, Is.False);
            Assert.That(shareableTag.TaggedValue?.SharedValueIndex, Is.EqualTo(-1));
            Assert.That(referenceTag.TagNumber, Is.EqualTo(CborTagNumbers.SharedValueReference));
            Assert.That(referenceTag.SharedValueIndex, Is.EqualTo(-1));
            Assert.That(referenceTag.ReferencedValue, Is.Null);
        });
    }

    [Test]
    public void ResolvePolicyCreatesSelfReferentialSharedValueGraph()
    {
        CborReadResult result = Decode(
            [0xd8, 0x1c, 0x81, 0xd8, 0x1d, 0x00],
            CborSharedValuePolicy.Resolve);
        CborValue shareableTag = result.Value!;
        CborValue sharedArray = shareableTag.TaggedValue!;
        CborValue reference = sharedArray.ArrayItems![0];

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(shareableTag.TagNumber, Is.EqualTo(CborTagNumbers.ShareableValue));
            Assert.That(sharedArray.IsSharedValue, Is.True);
            Assert.That(sharedArray.SharedValueIndex, Is.Zero);
            Assert.That(reference.TagNumber, Is.EqualTo(CborTagNumbers.SharedValueReference));
            Assert.That(reference.SharedValueIndex, Is.Zero);
            Assert.That(reference.ReferencedValue, Is.SameAs(sharedArray));
        });
    }

    [Test]
    public void SharedReferencesRejectForwardMissingWrongTypeAndLargeIndexesAtomically()
    {
        CborDecodeOptions options = new CborDecodeOptions
        {
            SharedValuePolicy = CborSharedValuePolicy.Resolve,
            RetainSourceRanges = true
        };
        CborReadResult forward = new CborDecoder().Decode(
            [0x82, 0xd8, 0x1d, 0x00, 0xd8, 0x1c, 0x00],
            options);
        CborReadResult missing = new CborDecoder().Decode(
            [0x82, 0xd8, 0x1c, 0x00, 0xd8, 0x1d, 0x01],
            options);
        CborReadResult wrongType = new CborDecoder().Decode(
            [0xd8, 0x1d, 0x20],
            options);
        CborReadResult tooLarge = new CborDecoder().Decode(
            [0xd8, 0x1d, 0x1b, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff],
            options);

        Assert.Multiple(() =>
        {
            AssertFailure(forward, "CBOR_SHARED_REFERENCE_NOT_FOUND", 1, 7);
            AssertFailure(missing, "CBOR_SHARED_REFERENCE_NOT_FOUND", 4, 7);
            AssertFailure(wrongType, "CBOR_SHARED_REFERENCE_TYPE", 0, 3);
            AssertFailure(tooLarge, "CBOR_SHARED_REFERENCE_INDEX_OUT_OF_RANGE", 0, 11);
        });
    }

    [Test]
    public void ResolvePolicyRegistersTextAndByteStringsInsideNamespace()
    {
        byte[] encoding =
        [
            0xd9, 0x01, 0x00,
            0x84,
            0x63, 0x66, 0x6f, 0x6f,
            0x43, 0x01, 0x02, 0x03,
            0xd8, 0x19, 0x00,
            0xd8, 0x19, 0x01
        ];
        CborReadResult result = Decode(encoding, CborSharedValuePolicy.Resolve);
        CborReadResult preserved = Decode(encoding, CborSharedValuePolicy.Preserve);
        CborValue namespaceTag = result.Value!;
        List<CborValue> items = namespaceTag.TaggedValue!.ArrayItems!;
        List<CborValue> preservedItems = preserved.Value!.TaggedValue!.ArrayItems!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(namespaceTag.TagNumber, Is.EqualTo(CborTagNumbers.StringReferenceNamespace));
            Assert.That(items[2].TagNumber, Is.EqualTo(CborTagNumbers.StringReference));
            Assert.That(items[2].SharedValueIndex, Is.Zero);
            Assert.That(items[2].ReferencedValue, Is.SameAs(items[0]));
            Assert.That(items[3].SharedValueIndex, Is.EqualTo(1));
            Assert.That(items[3].ReferencedValue, Is.SameAs(items[1]));
            Assert.That(items[3].ReferencedValue?.ByteStringValue,
                Is.EqualTo(new byte[] { 0x01, 0x02, 0x03 }));
            Assert.That(preserved.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(preservedItems[2].TagNumber, Is.EqualTo(CborTagNumbers.StringReference));
            Assert.That(preservedItems[2].SharedValueIndex, Is.EqualTo(-1));
            Assert.That(preservedItems[2].ReferencedValue, Is.Null);
        });
    }

    [Test]
    public void NestedStringNamespacesRestoreTheOuterEncounterTable()
    {
        byte[] encoding =
        [
            0xd9, 0x01, 0x00,
            0x83,
            0x63, 0x6f, 0x6e, 0x65,
            0xd9, 0x01, 0x00,
            0x82,
            0x63, 0x74, 0x77, 0x6f,
            0xd8, 0x19, 0x00,
            0xd8, 0x19, 0x00
        ];
        CborReadResult result = Decode(encoding, CborSharedValuePolicy.Resolve);
        List<CborValue> outerItems = result.Value!.TaggedValue!.ArrayItems!;
        List<CborValue> innerItems = outerItems[1].TaggedValue!.ArrayItems!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(innerItems[1].ReferencedValue, Is.SameAs(innerItems[0]));
            Assert.That(innerItems[1].ReferencedValue?.TextStringValue, Is.EqualTo("two"));
            Assert.That(outerItems[2].ReferencedValue, Is.SameAs(outerItems[0]));
            Assert.That(outerItems[2].ReferencedValue?.TextStringValue, Is.EqualTo("one"));
        });
    }

    [Test]
    public void StringReferencesRejectInvalidScopeTypeIndexAndUnregisteredStrings()
    {
        CborReadResult outside = Decode(
            [0xd8, 0x19, 0x00],
            CborSharedValuePolicy.Preserve);
        CborReadResult wrongType = Decode(
            [0xd9, 0x01, 0x00, 0xd8, 0x19, 0x20],
            CborSharedValuePolicy.Preserve);
        CborReadResult shortString = Decode(
            [0xd9, 0x01, 0x00, 0x82, 0x62, 0x68, 0x69, 0xd8, 0x19, 0x00],
            CborSharedValuePolicy.Preserve);
        CborReadResult indefiniteString = Decode(
            [
                0xd9, 0x01, 0x00, 0x82,
                0x7f, 0x63, 0x66, 0x6f, 0x6f, 0xff,
                0xd8, 0x19, 0x00
            ],
            CborSharedValuePolicy.Preserve);

        Assert.Multiple(() =>
        {
            Assert.That(outside.ErrorCode, Is.EqualTo("CBOR_STRING_REFERENCE_OUTSIDE_NAMESPACE"));
            Assert.That(wrongType.ErrorCode, Is.EqualTo("CBOR_STRING_REFERENCE_TYPE"));
            Assert.That(shortString.ErrorCode, Is.EqualTo("CBOR_STRING_REFERENCE_NOT_FOUND"));
            Assert.That(indefiniteString.ErrorCode, Is.EqualTo("CBOR_STRING_REFERENCE_NOT_FOUND"));
            Assert.That(outside.Value, Is.Null);
            Assert.That(wrongType.Value, Is.Null);
            Assert.That(shortString.Value, Is.Null);
            Assert.That(indefiniteString.Value, Is.Null);
        });
    }

    [Test]
    public void ReusedDecoderDoesNotLeakSharedValuesAcrossOperations()
    {
        CborDecoder decoder = new CborDecoder();
        CborDecodeOptions options = new CborDecodeOptions
        {
            SharedValuePolicy = CborSharedValuePolicy.Resolve
        };
        CborReadResult marked = decoder.Decode([0xd8, 0x1c, 0x66, 0x73, 0x65, 0x63, 0x72, 0x65, 0x74], options);
        CborReadResult leakedReference = decoder.Decode([0xd8, 0x1d, 0x00], options);
        CborReadResult clean = decoder.Decode([0x00], options);

        Assert.Multiple(() =>
        {
            Assert.That(marked.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(leakedReference.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(leakedReference.ErrorCode, Is.EqualTo("CBOR_SHARED_REFERENCE_NOT_FOUND"));
            Assert.That(leakedReference.Value, Is.Null);
            Assert.That(clean.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(clean.Value?.IntegerValue, Is.EqualTo(System.Numerics.BigInteger.Zero));
            Assert.That(decoder.Core.SharingDecoder?.SharedValues, Is.Empty);
        });
    }

    private static CborReadResult Decode(byte[] encoding, CborSharedValuePolicy policy)
    {
        return new CborDecoder().Decode(
            encoding,
            new CborDecodeOptions { SharedValuePolicy = policy });
    }

    private static void AssertFailure(
        CborReadResult result,
        string errorCode,
        long errorOffset,
        int sourceLength)
    {
        Assert.That(result.Status, Is.EqualTo(CborReadStatus.Error));
        Assert.That(result.ErrorCode, Is.EqualTo(errorCode));
        Assert.That(result.ErrorOffset, Is.EqualTo(errorOffset));
        Assert.That(result.Value, Is.Null);
        Assert.That(result.Consumed, Is.Zero);
        Assert.That(result.Remaining, Is.EqualTo(sourceLength));
    }
}
