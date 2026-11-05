using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborLimitTests
{
    [Test]
    public void OrdinaryTokenReadsEnforceTotalItemBudget()
    {
        CborDecodeOptions options = new CborDecodeOptions { MaxTotalItems = 2 };
        CborTokenReader reader = new CborTokenReader(options);

        CborTokenReadResult first = reader.ReadToken([0x00]);
        CborTokenReadResult second = reader.ReadToken([0x01]);
        CborTokenReadResult third = reader.ReadToken([0x02]);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(second.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(third.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(third.ErrorCode, Is.EqualTo("CBOR_MAX_TOTAL_ITEMS"));
            Assert.That(third.Consumed, Is.Zero);
            Assert.That(third.Remaining, Is.EqualTo(1));
            Assert.That(reader.Core.Limits.TotalItems, Is.EqualTo(2));
        });
    }

    [Test]
    public void SyntheticContainerEndsDoNotConsumeItemOrByteBudgets()
    {
        CborDecodeOptions options = new CborDecodeOptions
        {
            MaxTotalItems = 2,
            MaxTotalBytes = 2
        };
        CborTokenReader reader = new CborTokenReader(options);

        CborTokenReadResult start = reader.ReadToken([0x81]);
        CborTokenReadResult item = reader.ReadToken([0x00]);
        CborTokenReadResult end = reader.ReadToken([]);

        Assert.Multiple(() =>
        {
            Assert.That(start.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(item.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(end.Token?.Kind, Is.EqualTo(CborTokenKind.EndArray));
            Assert.That(end.Consumed, Is.Zero);
            Assert.That(reader.Core.Limits.TotalItems, Is.EqualTo(2));
            Assert.That(reader.Core.Limits.TotalBytes, Is.EqualTo(2));
        });
    }

    [Test]
    public void PendingFragmentsCannotGrowPastTotalByteBudget()
    {
        CborDecodeOptions options = new CborDecodeOptions { MaxTotalBytes = 4 };
        CborTokenReader reader = new CborTokenReader(options);
        CborTokenReader trailingReader = new CborTokenReader(
            new CborDecodeOptions { MaxTotalBytes = 2 });

        CborTokenReadResult first = reader.ReadToken([0x1a, 0x00]);
        CborTokenReadResult second = reader.ReadToken([0x00, 0x01]);
        CborTokenReadResult overLimit = reader.ReadToken([0x00]);
        CborTokenReadResult trailingHead = trailingReader.ReadToken([0x18]);
        CborTokenReadResult trailingValue = trailingReader.ReadToken([0x18, 0x00, 0x01]);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(CborReadStatus.NeedMoreData));
            Assert.That(first.Consumed, Is.EqualTo(2));
            Assert.That(second.Status, Is.EqualTo(CborReadStatus.NeedMoreData));
            Assert.That(second.Consumed, Is.EqualTo(2));
            Assert.That(overLimit.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(overLimit.ErrorCode, Is.EqualTo("CBOR_MAX_TOTAL_BYTES"));
            Assert.That(overLimit.Consumed, Is.Zero);
            Assert.That(overLimit.Remaining, Is.EqualTo(1));
            Assert.That(reader.Core.PendingBytes.Length, Is.EqualTo(4));
            Assert.That(reader.Core.Limits.TotalBytes, Is.Zero);
            Assert.That(trailingHead.Status, Is.EqualTo(CborReadStatus.NeedMoreData));
            Assert.That(trailingValue.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(trailingValue.Consumed, Is.EqualTo(1));
            Assert.That(trailingValue.Remaining, Is.EqualTo(2));
            Assert.That(trailingReader.Core.Limits.TotalBytes, Is.EqualTo(2));
        });
    }

    [Test]
    public void DeclaredStringLengthIsRejectedBeforePayloadAllocationOrBuffering()
    {
        CborDecodeOptions options = new CborDecodeOptions { MaxStringBytes = 2 };
        CborTokenReader byteReader = new CborTokenReader(options);
        CborTokenReader textReader = new CborTokenReader(options);

        CborTokenReadResult bytes = byteReader.ReadToken([0x5a, 0xff, 0xff, 0xff, 0xff]);
        CborTokenReadResult text = textReader.ReadToken([0x63]);

        Assert.Multiple(() =>
        {
            Assert.That(bytes.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(bytes.ErrorCode, Is.EqualTo("CBOR_MAX_STRING_BYTES"));
            Assert.That(bytes.Consumed, Is.Zero);
            Assert.That(bytes.Token, Is.Null);
            Assert.That(byteReader.Core.PendingBytes, Is.Empty);
            Assert.That(byteReader.Core.Limits.TotalBytes, Is.Zero);
            Assert.That(text.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(text.ErrorCode, Is.EqualTo("CBOR_MAX_STRING_BYTES"));
            Assert.That(text.Consumed, Is.Zero);
            Assert.That(textReader.Core.PendingBytes, Is.Empty);
        });
    }

    [Test]
    public void DeclaredArrayMapAndNestingLimitsApplyToOrdinaryReads()
    {
        CborTokenReadResult array = new CborTokenReader(
            new CborDecodeOptions { MaxArrayItems = 1 }).ReadToken([0x82]);
        CborTokenReadResult map = new CborTokenReader(
            new CborDecodeOptions { MaxMapPairs = 1 }).ReadToken([0xa2]);

        CborDecodeOptions depthOptions = new CborDecodeOptions { MaxNestingDepth = 2 };
        CborTokenReader depthReader = new CborTokenReader(depthOptions);
        CborTokenReadResult outer = depthReader.ReadToken([0x81]);
        CborTokenReadResult tag = depthReader.ReadToken([0xc0]);
        CborTokenReadResult tooDeep = depthReader.ReadToken([0x81]);

        Assert.Multiple(() =>
        {
            Assert.That(array.ErrorCode, Is.EqualTo("CBOR_MAX_ARRAY_ITEMS"));
            Assert.That(array.Consumed, Is.Zero);
            Assert.That(map.ErrorCode, Is.EqualTo("CBOR_MAX_MAP_PAIRS"));
            Assert.That(map.Consumed, Is.Zero);
            Assert.That(outer.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(tag.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(tooDeep.ErrorCode, Is.EqualTo("CBOR_MAX_NESTING_DEPTH"));
            Assert.That(tooDeep.Consumed, Is.Zero);
            Assert.That(depthReader.Core.Frames.Count, Is.EqualTo(2));
        });
    }

    [Test]
    public void ObservedIndefiniteArrayAndMapLimitsAllowClosingBreak()
    {
        CborTokenReader arrayReader = new CborTokenReader(
            new CborDecodeOptions { MaxArrayItems = 1 });
        CborTokenReadResult arrayStart = arrayReader.ReadToken([0x9f]);
        CborTokenReadResult arrayItem = arrayReader.ReadToken([0x00]);
        CborTokenReadResult extraItem = arrayReader.ReadToken([0x01]);
        CborTokenReadResult arrayEnd = arrayReader.ReadToken([0xff]);

        CborTokenReader mapReader = new CborTokenReader(
            new CborDecodeOptions { MaxMapPairs = 1 });
        CborTokenReadResult mapStart = mapReader.ReadToken([0xbf]);
        CborTokenReadResult key = mapReader.ReadToken([0x00]);
        CborTokenReadResult value = mapReader.ReadToken([0x01]);
        CborTokenReadResult extraKey = mapReader.ReadToken([0x02]);
        CborTokenReadResult mapEnd = mapReader.ReadToken([0xff]);

        Assert.Multiple(() =>
        {
            Assert.That(arrayStart.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(arrayItem.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(extraItem.ErrorCode, Is.EqualTo("CBOR_MAX_ARRAY_ITEMS"));
            Assert.That(arrayEnd.Token?.Kind, Is.EqualTo(CborTokenKind.EndArray));
            Assert.That(mapStart.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(key.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(value.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(extraKey.ErrorCode, Is.EqualTo("CBOR_MAX_MAP_PAIRS"));
            Assert.That(mapEnd.Token?.Kind, Is.EqualTo(CborTokenKind.EndMap));
        });
    }

    [Test]
    public void IndefiniteStringChunksUseOneCumulativeStringBudget()
    {
        CborTokenReader reader = new CborTokenReader(
            new CborDecodeOptions { MaxStringBytes = 3 });
        CborTokenReadResult start = reader.ReadToken([0x5f]);
        CborTokenReadResult first = reader.ReadToken([0x42, 0x01, 0x02]);
        CborTokenReadResult tooLarge = reader.ReadToken([0x42, 0x03, 0x04]);
        CborTokenReadResult end = reader.ReadToken([0xff]);

        Assert.Multiple(() =>
        {
            Assert.That(start.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(first.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(tooLarge.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(tooLarge.ErrorCode, Is.EqualTo("CBOR_MAX_STRING_BYTES"));
            Assert.That(tooLarge.Consumed, Is.Zero);
            Assert.That(end.Token?.Kind, Is.EqualTo(CborTokenKind.EndByteString));
        });
    }

    [Test]
    public void RetainedRepresentationBudgetIsCheckedBeforeMetadataAllocation()
    {
        CborDecodeOptions retainedOptions = new CborDecodeOptions
        {
            RetainRepresentation = true,
            MaxRetainedSourceBytes = 2
        };
        CborTokenReader retainedReader = new CborTokenReader(retainedOptions);
        CborTokenReadResult first = retainedReader.ReadToken([0x18, 0x18]);
        CborTokenReadResult overLimit = retainedReader.ReadToken([0x00]);

        CborDecodeOptions unretainedOptions = new CborDecodeOptions
        {
            MaxRetainedSourceBytes = 0
        };
        CborTokenReadResult unretained = new CborTokenReader(unretainedOptions).ReadToken([0x00]);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(first.Token?.Representation, Is.Not.Null);
            Assert.That(retainedReader.Core.Limits.RetainedBytes, Is.EqualTo(2));
            Assert.That(overLimit.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(overLimit.ErrorCode, Is.EqualTo("CBOR_MAX_RETAINED_SOURCE_BYTES"));
            Assert.That(overLimit.Consumed, Is.Zero);
            Assert.That(overLimit.Token, Is.Null);
            Assert.That(retainedReader.Core.Limits.RetainedBytes, Is.EqualTo(2));
            Assert.That(unretained.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(unretained.Token?.Representation, Is.Null);
        });
    }

    [Test]
    public void ResetRestoresAllOperationBudgets()
    {
        CborDecodeOptions options = new CborDecodeOptions
        {
            MaxTotalBytes = 1,
            MaxTotalItems = 1,
            MaxRetainedSourceBytes = 1,
            RetainSourceRanges = true
        };
        CborTokenReader reader = new CborTokenReader(options);
        CborTokenReadResult first = reader.ReadToken([0x00]);

        reader.ResetReader();
        CborTokenReadResult afterReset = reader.ReadToken([0x01]);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(afterReset.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(afterReset.Token?.Representation?.SourceOffset, Is.Zero);
            Assert.That(reader.Core.Limits.TotalBytes, Is.EqualTo(1));
            Assert.That(reader.Core.Limits.TotalItems, Is.EqualTo(1));
            Assert.That(reader.Core.Limits.RetainedBytes, Is.EqualTo(1));
        });
    }

    [Test]
    public void EmbeddedTokenizerCoresShareBudgetsAndCheckDepthBeforeAllocation()
    {
        CborDecodeOptions options = new CborDecodeOptions { MaxEmbeddedDepth = 1 };
        CborTokenizerCore root = new CborTokenizerCore(options, null);

        CborTokenizerCore? child = root.CreateEmbeddedCore();
        root.ExitEmbeddedCore();
        CborTokenizerCore? rejectedGrandchild = child?.CreateEmbeddedCore();
        CborTokenReadResult childToken = child!.ReadToken([0x00]);
        child.ExitEmbeddedCore();
        child.ExitEmbeddedCore();
        CborTokenizerCore? allowedAfterExit = root.CreateEmbeddedCore();
        allowedAfterExit!.ExitEmbeddedCore();

        Assert.Multiple(() =>
        {
            Assert.That(child, Is.Not.Null);
            Assert.That(rejectedGrandchild, Is.Null);
            Assert.That(childToken.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(root.Limits.TotalBytes, Is.EqualTo(1));
            Assert.That(root.Limits.TotalItems, Is.EqualTo(1));
            Assert.That(allowedAfterExit, Is.Not.Null);
            Assert.That(root.Limits.EmbeddedDepth, Is.Zero);
        });
    }
}
