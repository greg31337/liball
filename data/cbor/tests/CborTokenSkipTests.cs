using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborTokenSkipTests
{
    [Test]
    public void EveryScalarFormSkipsExactlyOneValueAndLeavesTrailingData()
    {
        byte[][] encodings =
        [
            [0x00],
            [0x3a, 0x00, 0x01, 0x00, 0x00],
            [0x43, 0x01, 0x02, 0x03],
            [0x63, 0xe2, 0x82, 0xac],
            [0xf4],
            [0xf8, 0x20],
            [0xf9, 0x3e, 0x00],
            [0xfa, 0x3f, 0xc0, 0x00, 0x00],
            [0xfb, 0x3f, 0xf8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]
        ];

        foreach (byte[] encoding in encodings)
        {
            byte[] source = new byte[encoding.Length + 1];
            encoding.CopyTo(source, 0);
            source[source.Length - 1] = 0x07;
            CborTokenReader reader = CreateReader(new CborDecodeOptions());

            CborReadResult skipped = reader.SkipValue(source);
            CborTokenReadResult trailing = reader.ReadToken([0x07]);

            Assert.Multiple(() =>
            {
                Assert.That(skipped.Status, Is.EqualTo(CborReadStatus.Done));
                Assert.That(skipped.Value, Is.Null);
                Assert.That(skipped.Consumed, Is.EqualTo(encoding.Length));
                Assert.That(skipped.Remaining, Is.EqualTo(1));
                Assert.That(skipped.ErrorCode, Is.Null);
                Assert.That(trailing.Status, Is.EqualTo(CborReadStatus.Done));
                Assert.That(trailing.Token?.Representation?.SourceOffset, Is.EqualTo(encoding.Length));
            });
        }
    }

    [Test]
    public void NestedDefiniteContainersAndTagChainsSkipAsOneValue()
    {
        byte[] source = [0x83, 0x01, 0xa1, 0x02, 0x82, 0x03, 0x04, 0xc0, 0x05, 0x09];
        CborTokenReader reader = CreateReader(new CborDecodeOptions());

        CborReadResult result = reader.SkipValue(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(result.Consumed, Is.EqualTo(9));
            Assert.That(result.Remaining, Is.EqualTo(1));
            Assert.That(reader.Core.Frames, Is.Empty);
            Assert.That(reader.Core.PendingBytes, Is.Empty);
        });
    }

    [Test]
    public void NestedIndefiniteContainersStringsAndTagsSkipAsOneValue()
    {
        byte[] source =
        [
            0x9f,
            0x5f, 0x42, 0xaa, 0xbb, 0x40, 0xff,
            0xbf, 0x01, 0x7f, 0x61, 0x61, 0xff, 0xff,
            0xc0, 0x9f, 0xff,
            0xff,
            0x07
        ];
        CborTokenReader reader = CreateReader(new CborDecodeOptions());

        CborReadResult result = reader.SkipValue(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(result.Consumed, Is.EqualTo(source.Length - 1));
            Assert.That(result.Remaining, Is.EqualTo(1));
            Assert.That(reader.Core.Frames, Is.Empty);
            Assert.That(reader.Core.SkipState.Active, Is.False);
        });
    }

    [Test]
    public void SkipResumesAtEveryFragmentBoundaryWithoutRereading()
    {
        byte[] encoded = [0x83, 0x01, 0xa1, 0x02, 0x82, 0x03, 0x04, 0xc0, 0x05];

        for (int split = 0; split < encoded.Length; split++)
        {
            CborTokenReader reader = CreateReader(new CborDecodeOptions());
            CborReadResult first = reader.SkipValue(encoded.AsSpan(0, split).ToArray());
            byte[] remainder = new byte[encoded.Length - split + 1];
            encoded.AsSpan(split).CopyTo(remainder);
            remainder[remainder.Length - 1] = 0x07;
            CborReadResult completed = reader.SkipValue(remainder);

            Assert.Multiple(() =>
            {
                Assert.That(first.Status, Is.EqualTo(CborReadStatus.NeedMoreData));
                Assert.That(first.Consumed, Is.EqualTo(split));
                Assert.That(first.Remaining, Is.Zero);
                Assert.That(completed.Status, Is.EqualTo(CborReadStatus.Done));
                Assert.That(completed.Consumed, Is.EqualTo(encoded.Length - split));
                Assert.That(completed.Remaining, Is.EqualTo(1));
            });
        }
    }

    [Test]
    public void IndefiniteSkipResumesAtEveryContainerAndChunkBoundary()
    {
        byte[] encoded = [0x9f, 0x5f, 0x41, 0xaa, 0xff, 0xbf, 0x01, 0x02, 0xff, 0xff];

        for (int split = 0; split < encoded.Length; split++)
        {
            CborTokenReader reader = CreateReader(new CborDecodeOptions());
            CborReadResult first = reader.SkipValue(encoded.AsSpan(0, split).ToArray());
            byte[] remainder = new byte[encoded.Length - split + 1];
            encoded.AsSpan(split).CopyTo(remainder);
            remainder[remainder.Length - 1] = 0x07;
            CborReadResult completed = reader.SkipValue(remainder);

            Assert.Multiple(() =>
            {
                Assert.That(first.Status, Is.EqualTo(CborReadStatus.NeedMoreData));
                Assert.That(first.Consumed, Is.EqualTo(split));
                Assert.That(completed.Status, Is.EqualTo(CborReadStatus.Done));
                Assert.That(completed.Consumed, Is.EqualTo(encoded.Length - split));
                Assert.That(completed.Remaining, Is.EqualTo(1));
            });
        }
    }

    [Test]
    public void EmptyDefiniteContainerSkipsWithoutConsumingTrailingData()
    {
        CborTokenReader reader = CreateReader(new CborDecodeOptions());

        CborReadResult result = reader.SkipValue([0x80, 0x07]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(result.Consumed, Is.EqualTo(1));
            Assert.That(result.Remaining, Is.EqualTo(1));
        });
    }

    [Test]
    public void SkipCanOperateOnTheNextItemInsideAnOpenContainer()
    {
        CborTokenReader reader = CreateReader(new CborDecodeOptions());
        CborTokenReadResult start = reader.ReadToken([0x82]);

        CborReadResult first = reader.SkipValue([0xc0, 0x01, 0x02]);
        CborReadResult second = reader.SkipValue([0x02]);
        CborTokenReadResult end = reader.ReadToken([]);

        Assert.Multiple(() =>
        {
            Assert.That(start.Token?.Kind, Is.EqualTo(CborTokenKind.StartArray));
            Assert.That(first.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(first.Consumed, Is.EqualTo(2));
            Assert.That(first.Remaining, Is.EqualTo(1));
            Assert.That(second.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(second.Consumed, Is.EqualTo(1));
            Assert.That(end.Token?.Kind, Is.EqualTo(CborTokenKind.EndArray));
            Assert.That(end.Consumed, Is.Zero);
        });
    }

    [Test]
    public void ResetDiscardsAnIncrementalSkipAndItsBufferedToken()
    {
        CborTokenReader reader = CreateReader(new CborDecodeOptions());
        CborReadResult partial = reader.SkipValue([0x1a, 0x00]);

        reader.ResetReader();
        CborReadResult afterReset = reader.SkipValue([0x01, 0x07]);

        Assert.Multiple(() =>
        {
            Assert.That(partial.Status, Is.EqualTo(CborReadStatus.NeedMoreData));
            Assert.That(partial.Consumed, Is.EqualTo(2));
            Assert.That(afterReset.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(afterReset.Consumed, Is.EqualTo(1));
            Assert.That(afterReset.Remaining, Is.EqualTo(1));
        });
    }

    [Test]
    public void MalformedInputErrorsKeepExactSkipConsumptionAndOffsets()
    {
        CborTokenReader reader = CreateReader(new CborDecodeOptions());

        CborReadResult result = reader.SkipValue([0xbf, 0x01, 0xff, 0x07]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(result.ErrorCode, Is.EqualTo("CBOR_ODD_INDEFINITE_MAP"));
            Assert.That(result.ErrorOffset, Is.EqualTo(2));
            Assert.That(result.Consumed, Is.EqualTo(2));
            Assert.That(result.Remaining, Is.EqualTo(2));
            Assert.That(result.ErrorPath, Is.EqualTo("$"));
        });
    }

    [Test]
    public void DeclaredContainerAndStringLimitsRejectBeforeTheirContents()
    {
        CborDecodeOptions arrayOptions = new CborDecodeOptions { MaxArrayItems = 1 };
        CborDecodeOptions mapOptions = new CborDecodeOptions { MaxMapPairs = 0 };
        CborDecodeOptions stringOptions = new CborDecodeOptions { MaxStringBytes = 2 };

        CborReadResult array = CreateReader(arrayOptions).SkipValue([0x82, 0x00, 0x01]);
        CborReadResult map = CreateReader(mapOptions).SkipValue([0xa1, 0x00, 0x01]);
        CborReadResult bytes = CreateReader(stringOptions).SkipValue([0x43, 0x01, 0x02, 0x03]);
        CborReadResult text = CreateReader(stringOptions).SkipValue([0x63, 0x61, 0x62, 0x63]);

        Assert.Multiple(() =>
        {
            Assert.That(array.ErrorCode, Is.EqualTo("CBOR_MAX_ARRAY_ITEMS"));
            Assert.That(array.Consumed, Is.Zero);
            Assert.That(array.Remaining, Is.EqualTo(3));
            Assert.That(map.ErrorCode, Is.EqualTo("CBOR_MAX_MAP_PAIRS"));
            Assert.That(map.Consumed, Is.Zero);
            Assert.That(map.Remaining, Is.EqualTo(3));
            Assert.That(bytes.ErrorCode, Is.EqualTo("CBOR_MAX_STRING_BYTES"));
            Assert.That(bytes.Consumed, Is.Zero);
            Assert.That(bytes.Remaining, Is.EqualTo(4));
            Assert.That(text.ErrorCode, Is.EqualTo("CBOR_MAX_STRING_BYTES"));
            Assert.That(text.Consumed, Is.Zero);
            Assert.That(text.Remaining, Is.EqualTo(4));
        });
    }

    [Test]
    public void ObservedIndefiniteContainerAndStringLimitsAreEnforced()
    {
        CborDecodeOptions arrayOptions = new CborDecodeOptions { MaxArrayItems = 1 };
        CborDecodeOptions mapOptions = new CborDecodeOptions { MaxMapPairs = 1 };
        CborDecodeOptions stringOptions = new CborDecodeOptions { MaxStringBytes = 3 };

        CborReadResult array = CreateReader(arrayOptions).SkipValue([0x9f, 0x00, 0x01, 0xff]);
        CborReadResult map = CreateReader(mapOptions).SkipValue([0xbf, 0x00, 0x01, 0x02, 0x03, 0xff]);
        CborReadResult bytes = CreateReader(stringOptions).SkipValue(
            [0x5f, 0x42, 0x01, 0x02, 0x42, 0x03, 0x04, 0xff]);

        Assert.Multiple(() =>
        {
            Assert.That(array.ErrorCode, Is.EqualTo("CBOR_MAX_ARRAY_ITEMS"));
            Assert.That(array.Consumed, Is.EqualTo(2));
            Assert.That(array.Remaining, Is.EqualTo(2));
            Assert.That(map.ErrorCode, Is.EqualTo("CBOR_MAX_MAP_PAIRS"));
            Assert.That(map.Consumed, Is.EqualTo(3));
            Assert.That(map.Remaining, Is.EqualTo(3));
            Assert.That(bytes.ErrorCode, Is.EqualTo("CBOR_MAX_STRING_BYTES"));
            Assert.That(bytes.Consumed, Is.EqualTo(4));
            Assert.That(bytes.Remaining, Is.EqualTo(4));
        });
    }

    [Test]
    public void DepthItemAndEncodedByteBudgetsAreEnforced()
    {
        CborDecodeOptions depthOptions = new CborDecodeOptions { MaxNestingDepth = 2 };
        CborDecodeOptions itemOptions = new CborDecodeOptions { MaxTotalItems = 2 };
        CborDecodeOptions byteOptions = new CborDecodeOptions { MaxTotalBytes = 2 };

        CborReadResult depth = CreateReader(depthOptions).SkipValue([0x81, 0x81, 0x81, 0x00]);
        CborReadResult items = CreateReader(itemOptions).SkipValue([0x82, 0x00, 0x01]);
        CborReadResult bytes = CreateReader(byteOptions).SkipValue([0xf9, 0x3c, 0x00]);

        Assert.Multiple(() =>
        {
            Assert.That(depth.ErrorCode, Is.EqualTo("CBOR_MAX_NESTING_DEPTH"));
            Assert.That(depth.Consumed, Is.EqualTo(2));
            Assert.That(depth.Remaining, Is.EqualTo(2));
            Assert.That(items.ErrorCode, Is.EqualTo("CBOR_MAX_TOTAL_ITEMS"));
            Assert.That(items.Consumed, Is.EqualTo(2));
            Assert.That(items.Remaining, Is.EqualTo(1));
            Assert.That(bytes.ErrorCode, Is.EqualTo("CBOR_MAX_TOTAL_BYTES"));
            Assert.That(bytes.Consumed, Is.Zero);
            Assert.That(bytes.Remaining, Is.EqualTo(3));
        });
    }

    private static CborTokenReader CreateReader(CborDecodeOptions options)
    {
        options.RetainRepresentation = true;
        options.RetainSourceRanges = true;
        return new CborTokenReader(options);
    }
}
