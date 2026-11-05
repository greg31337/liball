using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborAdversarialDecodeTests
{
    [TestCase("5bffffffffffffffff", "CBOR_MAX_STRING_BYTES")]
    [TestCase("7bffffffffffffffff", "CBOR_MAX_STRING_BYTES")]
    [TestCase("9bffffffffffffffff", "CBOR_MAX_ARRAY_ITEMS")]
    [TestCase("bbffffffffffffffff", "CBOR_MAX_MAP_PAIRS")]
    public void DeclaredLengthBombsFailBeforePayloadOrContainerAllocation(
        string hex,
        string errorCode)
    {
        byte[] source = Convert.FromHexString(hex);
        CborDecoder decoder = new CborDecoder();

        CborReadResult result = decoder.Decode(source, new CborDecodeOptions());

        AssertAtomicFailure(result, errorCode, 0, source.Length);
        Assert.Multiple(() =>
        {
            Assert.That(decoder.Core.Builder.Value, Is.Null);
            Assert.That(decoder.Core.Builder.Frames, Is.Empty);
            Assert.That(decoder.Core.Tokenizer?.PendingBytes, Is.Empty);
            Assert.That(decoder.Core.Tokenizer?.Limits.TotalBytes, Is.Zero);
            Assert.That(decoder.Core.Tokenizer?.Limits.TotalItems, Is.Zero);
        });
    }

    [TestCase(0x81)]
    [TestCase(0xc0)]
    public void DeepContainerAndTagChainsStopAtTheConfiguredNestingBoundary(byte head)
    {
        const int maximumDepth = 8;
        byte[] source = new byte[maximumDepth + 2];
        for (int index = 0; index < maximumDepth + 1; index++)
        {
            source[index] = head;
        }
        source[source.Length - 1] = 0x00;

        CborReadResult result = new CborDecoder().Decode(
            source,
            new CborDecodeOptions { MaxNestingDepth = maximumDepth });

        AssertAtomicFailure(result, "CBOR_MAX_NESTING_DEPTH", maximumDepth, source.Length);
    }

    [Test]
    public void IndefiniteItemFloodStopsAtTheTotalItemBudget()
    {
        byte[] source = [0x9f, 0x00, 0x01, 0x02, 0x03, 0x04, 0xff];
        CborDecodeOptions options = new CborDecodeOptions
        {
            MaxTotalItems = 5,
            MaxArrayItems = 100
        };

        CborReadResult result = new CborDecoder().Decode(source, options);

        AssertAtomicFailure(result, "CBOR_MAX_TOTAL_ITEMS", 5, source.Length);
    }

    [Test]
    public void ValidAndMalformedUtf8KeepStableResultsAtEveryTokenFragmentBoundary()
    {
        byte[] valid = [0x6a, 0x61, 0xc2, 0xa2, 0xe2, 0x82, 0xac, 0xf0, 0x90, 0x85, 0x91];
        byte[] malformed = [0x64, 0x61, 0xe2, 0x28, 0xa1];

        for (int split = 0; split < valid.Length; split++)
        {
            CborTokenReader reader = new CborTokenReader(new CborDecodeOptions());
            CborTokenReadResult first = reader.ReadToken(valid.AsSpan(0, split).ToArray());
            CborTokenReadResult completed = reader.ReadToken(valid.AsSpan(split).ToArray());
            CborDomBuilder builder = new CborDomBuilder();
            bool added = builder.AddToken(completed.Token!);

            Assert.Multiple(() =>
            {
                Assert.That(first.Status, Is.EqualTo(CborReadStatus.NeedMoreData));
                Assert.That(first.Consumed, Is.EqualTo(split));
                Assert.That(completed.Status, Is.EqualTo(CborReadStatus.Done));
                Assert.That(completed.Consumed, Is.EqualTo(valid.Length - split));
                Assert.That(completed.Token?.TextStringValue, Is.EqualTo("a¢€𐅑"));
                Assert.That(added, Is.True);
                Assert.That(builder.Completed, Is.True);
                Assert.That(builder.Value?.TextStringValue, Is.EqualTo("a¢€𐅑"));
            });
        }

        for (int split = 0; split < malformed.Length; split++)
        {
            CborTokenReader reader = new CborTokenReader(new CborDecodeOptions());
            CborTokenReadResult first = reader.ReadToken(malformed.AsSpan(0, split).ToArray());
            CborTokenReadResult failure = reader.ReadToken(malformed.AsSpan(split).ToArray());

            Assert.Multiple(() =>
            {
                Assert.That(first.Status, Is.EqualTo(CborReadStatus.NeedMoreData));
                Assert.That(first.Consumed, Is.EqualTo(split));
                Assert.That(failure.Status, Is.EqualTo(CborReadStatus.Error));
                Assert.That(failure.ErrorCode, Is.EqualTo("CBOR_UTF8_INVALID_CONTINUATION"));
                Assert.That(failure.ErrorOffset, Is.EqualTo(3));
                Assert.That(failure.Consumed, Is.Zero);
                Assert.That(failure.Remaining, Is.EqualTo(malformed.Length - split));
            });
        }
    }

    [Test]
    public void ResolvedSharingCanBuildCrossLinkedCyclesWithoutRecursiveProcessing()
    {
        byte[] source =
        [
            0xd8, 0x1c,
            0x82,
            0xd8, 0x1c,
            0x81,
            0xd8, 0x1d, 0x00,
            0xd8, 0x1d, 0x01
        ];
        CborReadResult result = new CborDecoder().Decode(
            source,
            new CborDecodeOptions { SharedValuePolicy = CborSharedValuePolicy.Resolve });
        CborValue outerTag = result.Value!;
        CborValue outerArray = outerTag.TaggedValue!;
        CborValue innerTag = outerArray.ArrayItems![0];
        CborValue innerArray = innerTag.TaggedValue!;
        CborValue outerReference = innerArray.ArrayItems![0];
        CborValue innerReference = outerArray.ArrayItems[1];

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(outerArray.IsSharedValue, Is.True);
            Assert.That(outerArray.SharedValueIndex, Is.Zero);
            Assert.That(innerArray.IsSharedValue, Is.True);
            Assert.That(innerArray.SharedValueIndex, Is.EqualTo(1));
            Assert.That(outerReference.SharedValueIndex, Is.Zero);
            Assert.That(outerReference.ReferencedValue, Is.SameAs(outerArray));
            Assert.That(innerReference.SharedValueIndex, Is.EqualTo(1));
            Assert.That(innerReference.ReferencedValue, Is.SameAs(innerArray));
        });
    }

    [Test]
    public void ReferenceIndexesAtAndBeyondTheSupportedBoundaryFailPrecisely()
    {
        CborDecodeOptions options = new CborDecodeOptions
        {
            SharedValuePolicy = CborSharedValuePolicy.Resolve,
            RetainSourceRanges = true
        };
        CborReadResult sharedMissing = new CborDecoder().Decode(
            [0xd8, 0x1d, 0x1a, 0x7f, 0xff, 0xff, 0xff],
            options);
        CborReadResult sharedOutOfRange = new CborDecoder().Decode(
            [0xd8, 0x1d, 0x1a, 0x80, 0x00, 0x00, 0x00],
            options);
        CborReadResult stringMissing = new CborDecoder().Decode(
            [
                0xd9, 0x01, 0x00, 0x82,
                0x63, 0x66, 0x6f, 0x6f,
                0xd8, 0x19, 0x1a, 0x7f, 0xff, 0xff, 0xff
            ],
            options);
        CborReadResult stringOutOfRange = new CborDecoder().Decode(
            [
                0xd9, 0x01, 0x00, 0x82,
                0x63, 0x66, 0x6f, 0x6f,
                0xd8, 0x19, 0x1a, 0x80, 0x00, 0x00, 0x00
            ],
            options);

        Assert.Multiple(() =>
        {
            AssertAtomicFailure(sharedMissing, "CBOR_SHARED_REFERENCE_NOT_FOUND", 0, 7);
            AssertAtomicFailure(
                sharedOutOfRange,
                "CBOR_SHARED_REFERENCE_INDEX_OUT_OF_RANGE",
                0,
                7);
            AssertAtomicFailure(stringMissing, "CBOR_STRING_REFERENCE_NOT_FOUND", 8, 15);
            AssertAtomicFailure(
                stringOutOfRange,
                "CBOR_STRING_REFERENCE_INDEX_OUT_OF_RANGE",
                8,
                15);
        });
    }

    [Test]
    public void EmbeddedTokenizerCoresPropagateDepthAndAllSharedOperationBudgets()
    {
        CborDecodeOptions options = new CborDecodeOptions
        {
            MaxEmbeddedDepth = 2,
            MaxTotalBytes = 4,
            MaxTotalItems = 3,
            MaxRetainedSourceBytes = 4,
            RetainSourceRanges = true
        };
        CborTokenizerCore root = new CborTokenizerCore(options, null);
        CborTokenReadResult rootValue = root.ReadToken([0x00]);
        CborTokenizerCore child = root.CreateEmbeddedCore()!;
        CborTokenReadResult childValue = child.ReadToken([0x18, 0x18]);
        CborTokenizerCore grandchild = child.CreateEmbeddedCore()!;
        CborTokenizerCore? rejectedDepth = grandchild.CreateEmbeddedCore();
        CborTokenReadResult rejectedBytes = grandchild.ReadToken([0x1a, 0x00, 0x00, 0x00, 0x00]);
        CborTokenReadResult grandchildValue = grandchild.ReadToken([0x01]);
        CborTokenReadResult rejectedItems = root.ReadToken([0x02]);

        grandchild.ExitEmbeddedCore();
        child.ExitEmbeddedCore();

        Assert.Multiple(() =>
        {
            Assert.That(rootValue.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(childValue.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(rejectedDepth, Is.Null);
            Assert.That(rejectedBytes.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(rejectedBytes.ErrorCode, Is.EqualTo("CBOR_MAX_TOTAL_BYTES"));
            Assert.That(rejectedBytes.Consumed, Is.Zero);
            Assert.That(grandchildValue.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(rejectedItems.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(rejectedItems.ErrorCode, Is.EqualTo("CBOR_MAX_TOTAL_ITEMS"));
            Assert.That(root.Limits.TotalBytes, Is.EqualTo(4));
            Assert.That(root.Limits.TotalItems, Is.EqualTo(3));
            Assert.That(root.Limits.RetainedBytes, Is.EqualTo(4));
            Assert.That(root.Limits.EmbeddedDepth, Is.Zero);
            Assert.That(child.Limits, Is.SameAs(root.Limits));
            Assert.That(grandchild.Limits, Is.SameAs(root.Limits));
        });
    }

    private static void AssertAtomicFailure(
        CborReadResult result,
        string errorCode,
        long errorOffset,
        int sourceLength)
    {
        Assert.That(result.Status, Is.EqualTo(CborReadStatus.Error));
        Assert.That(result.ErrorCode, Is.EqualTo(errorCode));
        Assert.That(result.ErrorOffset, Is.EqualTo(errorOffset));
        Assert.That(result.ErrorPath, Is.EqualTo("$"));
        Assert.That(result.Value, Is.Null);
        Assert.That(result.Consumed, Is.Zero);
        Assert.That(result.Remaining, Is.EqualTo(sourceLength));
    }
}
