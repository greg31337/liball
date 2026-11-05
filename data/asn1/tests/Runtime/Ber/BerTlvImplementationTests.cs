using asn1;

namespace asn1.Tests;

public class BerTlvImplementationTests
{
    [Test]
    public void ReaderBuildsNestedSiblingTreeEventsAndExactSlices()
    {
        byte[] encoded =
        [
            0xff, 0xee,
            0x30, 0x0a,
            0x02, 0x01, 0x05,
            0xa0, 0x05,
            0x04, 0x03, 0xaa, 0xbb, 0xcc,
            0xdd
        ];
        BerReadResult result = new BerTlvReaderImplementation(
            new AsnResourceLimits()).Read(new BerReadRequest
            {
                Data = encoded,
                Offset = 2,
                HasCount = true,
                Count = 12,
                EncodingRules = AsnEncodingRules.Ber,
                PreserveEncodedData = true
            });
        BerTlv root = result.Tlv ?? throw new InvalidOperationException();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(result.ConsumedCount, Is.EqualTo(12));
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(root.Tag.Number,
                Is.EqualTo(new System.Numerics.BigInteger(16)));
            Assert.That(root.Constructed, Is.True);
            Assert.That(root.Children, Has.Length.EqualTo(2));
            Assert.That(root.Children[0].Content, Is.EqualTo(new byte[] { 5 }));
            Assert.That(root.Children[1].Children[0].Content,
                Is.EqualTo(new byte[] { 0xaa, 0xbb, 0xcc }));
            Assert.That(root.EncodedData,
                Is.EqualTo(encoded.Skip(2).Take(12).ToArray()));
            Assert.That(root.Children[0].EncodedData,
                Is.EqualTo(new byte[] { 0x02, 0x01, 0x05 }));
            Assert.That(root.Children[1].EncodedData,
                Is.EqualTo(new byte[]
                {
                    0xa0, 0x05, 0x04, 0x03, 0xaa, 0xbb, 0xcc
                }));
            Assert.That(result.Events.Select(item => item.Kind),
                Is.EqualTo(new[]
                {
                    AsnTlvEventKind.StartConstructed,
                    AsnTlvEventKind.Primitive,
                    AsnTlvEventKind.StartConstructed,
                    AsnTlvEventKind.Primitive,
                    AsnTlvEventKind.EndConstructed,
                    AsnTlvEventKind.EndConstructed,
                    AsnTlvEventKind.EndOfInput
                }));
            Assert.That(result.Events.Select(item => item.Offset),
                Is.EqualTo(new[] { 2, 4, 7, 9, 14, 14, 14 }));
        });
    }

    [Test]
    public void ReaderHandlesIndefiniteNestingAndRejectsIllegalUses()
    {
        byte[] encoded =
        [
            0x30, 0x80,
            0x02, 0x01, 0x01,
            0xa0, 0x80,
            0x04, 0x00,
            0x00, 0x00,
            0x00, 0x00
        ];
        BerTlvReaderImplementation reader = new BerTlvReaderImplementation(
            new AsnResourceLimits());

        BerReadResult ber = reader.Read(new BerReadRequest
        {
            Data = encoded,
            EncodingRules = AsnEncodingRules.Ber,
            PreserveEncodedData = true
        });
        BerReadResult der = reader.ReadSpan(encoded, AsnEncodingRules.Der);
        BerReadResult primitive = reader.ReadSpan(
            [0x04, 0x80],
            AsnEncodingRules.Ber);

        Assert.Multiple(() =>
        {
            Assert.That(ber.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(ber.ConsumedCount, Is.EqualTo(encoded.Length));
            Assert.That(ber.Tlv?.IndefiniteLength, Is.True);
            Assert.That(ber.Tlv?.Children[1].IndefiniteLength, Is.True);
            Assert.That(ber.Tlv?.EncodedData, Is.EqualTo(encoded));
            Assert.That(ber.Events.Count(item =>
                item.Kind == AsnTlvEventKind.EndConstructed), Is.EqualTo(2));
            Assert.That(der.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(der.Diagnostics.Single().Code,
                Is.EqualTo("ASND0019"));
            Assert.That(primitive.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(primitive.Diagnostics.Single().Code,
                Is.EqualTo("ASND0018"));
        });
    }

    [Test]
    public void ReaderStopsAfterOneRootAndNavigatorSafelyTraversesValues()
    {
        byte[] siblings = [0x02, 0x01, 0x01, 0x04, 0x01, 0xff];
        BerTlvReaderImplementation reader = new BerTlvReaderImplementation(
            new AsnResourceLimits());
        BerReadResult first = reader.ReadSpan(siblings, AsnEncodingRules.Ber);
        BerReadResult second = reader.Read(new BerReadRequest
        {
            Data = siblings,
            Offset = first.ConsumedCount,
            EncodingRules = AsnEncodingRules.Ber
        });
        BerTlvNavigator navigator = new BerTlvNavigator(reader);
        BerTlvSkipResult skipped = navigator.SkipValue(
            siblings,
            AsnEncodingRules.Ber);
        BerTlv parent = new BerTlv
        {
            Constructed = true,
            Children =
            [
                first.Tlv ?? throw new InvalidOperationException(),
                second.Tlv ?? throw new InvalidOperationException()
            ]
        };
        BerTlvSelectionResult selected = navigator.SelectChild(parent, 0);
        BerTlvSelectionResult next = navigator.SelectNextSibling(
            parent.Children,
            0);
        BerTlvSelectionResult missing = navigator.SelectChild(parent, 2);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(first.ConsumedCount, Is.EqualTo(3));
            Assert.That(first.Tlv?.Tag.Number,
                Is.EqualTo(new System.Numerics.BigInteger(2)));
            Assert.That(second.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(second.ConsumedCount, Is.EqualTo(3));
            Assert.That(second.Events[0].Offset, Is.EqualTo(3));
            Assert.That(second.Tlv?.Tag.Number,
                Is.EqualTo(new System.Numerics.BigInteger(4)));
            Assert.That(skipped.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(skipped.SkippedCount, Is.EqualTo(3));
            Assert.That(selected.Value, Is.SameAs(parent.Children[0]));
            Assert.That(next.Value, Is.SameAs(parent.Children[1]));
            Assert.That(missing.Success, Is.False);
            Assert.That(missing.ErrorCode, Is.EqualTo("ASND0011"));
        });
    }

    [Test]
    public void ReaderPreservesCompletedPartialsAndDiagnosesBoundaryErrors()
    {
        BerTlvReaderImplementation reader = new BerTlvReaderImplementation(
            new AsnResourceLimits());

        BerReadResult truncated = reader.ReadSpan(
            [0x30, 0x05, 0x02, 0x01, 0x01],
            AsnEncodingRules.Ber);
        BerReadResult boundary = reader.ReadSpan(
            [0x30, 0x02, 0x02, 0x01, 0x01],
            AsnEncodingRules.Ber);
        BerReadResult strayEoc = reader.ReadSpan(
            [0x00, 0x00],
            AsnEncodingRules.Ber);
        BerReadResult crossedIdentifier = reader.ReadSpan(
            [0x30, 0x01, 0x1f],
            AsnEncodingRules.Ber);
        BerReadResult missingNestedEoc = reader.ReadSpan(
            [0x30, 0x02, 0xa0, 0x80],
            AsnEncodingRules.Ber);

        Assert.Multiple(() =>
        {
            Assert.That(truncated.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            Assert.That(truncated.ConsumedCount, Is.EqualTo(5));
            Assert.That(truncated.Tlv?.Children, Has.Length.EqualTo(1));
            Assert.That(truncated.Events.Select(item => item.Kind),
                Is.EqualTo(new[]
                {
                    AsnTlvEventKind.StartConstructed,
                    AsnTlvEventKind.Primitive
                }));
            Assert.That(boundary.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(boundary.Diagnostics.Single().Code,
                Is.EqualTo("ASND0020"));
            Assert.That(strayEoc.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(strayEoc.Diagnostics.Single().Code,
                Is.EqualTo("ASND0017"));
            Assert.That(crossedIdentifier.Status,
                Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(crossedIdentifier.Diagnostics.Single().Code,
                Is.EqualTo("ASND0015"));
            Assert.That(missingNestedEoc.Status,
                Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(missingNestedEoc.Diagnostics.Single().Code,
                Is.EqualTo("ASND0020"));
        });
    }

    [Test]
    public void WriterEmitsDefiniteAndIndefiniteTreesThatRoundTrip()
    {
        BerTlv integer = new BerTlv
        {
            Tag = new AsnTag
            {
                Class = AsnTagClass.Universal,
                Number = 2
            },
            Content = [0x05]
        };
        BerTlv octets = new BerTlv
        {
            Tag = new AsnTag
            {
                Class = AsnTagClass.Universal,
                Number = 4
            },
            Content = [0xaa, 0xbb]
        };
        BerTlv inner = new BerTlv
        {
            Tag = new AsnTag
            {
                Class = AsnTagClass.ContextSpecific,
                Number = 0
            },
            Constructed = true,
            IndefiniteLength = true,
            Children = [octets]
        };
        BerTlv root = new BerTlv
        {
            Tag = new AsnTag
            {
                Class = AsnTagClass.Universal,
                Number = 16
            },
            Constructed = true,
            Children = [integer, inner]
        };
        BerTlvWriterImplementation writer = new BerTlvWriterImplementation(
            new AsnResourceLimits());

        BerWriteResult written = writer.Write(new BerWriteRequest
        {
            Tlv = root,
            EncodingRules = AsnEncodingRules.Ber
        });
        BerReadResult read = new BerTlvReaderImplementation(
            new AsnResourceLimits()).ReadSpan(
                written.Data,
                AsnEncodingRules.Ber);
        BerWriteResult der = writer.Write(new BerWriteRequest
        {
            Tlv = root,
            EncodingRules = AsnEncodingRules.Der
        });

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True,
                Format(written.Diagnostics));
            Assert.That(written.Data, Is.EqualTo(new byte[]
            {
                0x30, 0x0b,
                0x02, 0x01, 0x05,
                0xa0, 0x80,
                0x04, 0x02, 0xaa, 0xbb,
                0x00, 0x00
            }));
            Assert.That(read.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(read.Tlv?.Children[1].Children[0].Content,
                Is.EqualTo(new byte[] { 0xaa, 0xbb }));
            Assert.That(der.Success, Is.False);
            Assert.That(der.Diagnostics.Single().Code,
                Is.EqualTo("ASNE0020"));
        });
    }

    [Test]
    public void WriterReconstructsEventsAndValidatesDeclaredLengths()
    {
        AsnTag sequence = new AsnTag
        {
            Class = AsnTagClass.Universal,
            Number = 16
        };
        AsnTag integer = new AsnTag
        {
            Class = AsnTagClass.Universal,
            Number = 2
        };
        BerTlvEvent[] events =
        [
            new BerTlvEvent
            {
                Kind = AsnTlvEventKind.StartConstructed,
                Tag = sequence,
                HasLength = true,
                Length = 3
            },
            new BerTlvEvent
            {
                Kind = AsnTlvEventKind.Primitive,
                Tag = integer,
                HasLength = true,
                Length = 1,
                Data = [0x07]
            },
            new BerTlvEvent
            {
                Kind = AsnTlvEventKind.EndConstructed,
                Tag = sequence
            },
            new BerTlvEvent { Kind = AsnTlvEventKind.EndOfInput }
        ];
        BerTlvWriterImplementation writer = new BerTlvWriterImplementation(
            new AsnResourceLimits());

        BerWriteResult valid = writer.Write(new BerWriteRequest
        {
            Events = events,
            EncodingRules = AsnEncodingRules.Ber
        });
        events[0].Length = 4;
        BerWriteResult mismatch = writer.Write(new BerWriteRequest
        {
            Events = events,
            EncodingRules = AsnEncodingRules.Ber
        });

        Assert.Multiple(() =>
        {
            Assert.That(valid.Success, Is.True);
            Assert.That(valid.Data,
                Is.EqualTo(new byte[] { 0x30, 0x03, 0x02, 0x01, 0x07 }));
            Assert.That(mismatch.Success, Is.False);
            Assert.That(mismatch.Diagnostics.Single().Code,
                Is.EqualTo("ASNE0022"));
        });
    }

    [Test]
    public void SpanAndStreamOperationsAreCompleteAndNonDestructive()
    {
        BerTlv value = new BerTlv
        {
            Tag = new AsnTag
            {
                Class = AsnTagClass.Universal,
                Number = 4
            },
            Content = [1, 2, 3]
        };
        BerWriteRequest request = new BerWriteRequest
        {
            Tlv = value,
            EncodingRules = AsnEncodingRules.Ber
        };
        BerTlvWriterImplementation writer = new BerTlvWriterImplementation(
            new AsnResourceLimits());
        byte[] tooSmall = [0xcc, 0xcc, 0xcc, 0xcc];
        BerWriteResult spanFailure = writer.WriteSpan(request, tooSmall);
        MemoryStream output = new MemoryStream();
        BerWriteResult streamWrite = writer.WriteStream(request, output);
        output.Position = 0;
        BerReadResult streamRead = new BerTlvReaderImplementation(
            new AsnResourceLimits()).ReadStream(
                output,
                AsnEncodingRules.Ber);

        Assert.Multiple(() =>
        {
            Assert.That(spanFailure.Success, Is.False);
            Assert.That(spanFailure.Diagnostics.Single().Code,
                Is.EqualTo("ASNE0006"));
            Assert.That(tooSmall,
                Is.EqualTo(new byte[] { 0xcc, 0xcc, 0xcc, 0xcc }));
            Assert.That(streamWrite.Success, Is.True);
            Assert.That(output.ToArray(),
                Is.EqualTo(new byte[] { 0x04, 0x03, 0x01, 0x02, 0x03 }));
            Assert.That(streamRead.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(streamRead.Tlv?.Content,
                Is.EqualTo(new byte[] { 1, 2, 3 }));
        });
    }

    [Test]
    public void ReaderAndWriterEnforceNestingCollectionAndCycleLimits()
    {
        AsnResourceLimits shallow = new AsnResourceLimits
        {
            MaximumNestingDepth = 1
        };
        BerReadResult deepRead = new BerTlvReaderImplementation(shallow)
            .ReadSpan(
                [0x30, 0x04, 0x30, 0x02, 0x04, 0x00],
                AsnEncodingRules.Ber);

        BerTlv cycle = new BerTlv
        {
            Tag = new AsnTag { Number = 16 },
            Constructed = true
        };
        cycle.Children = [cycle];
        BerWriteResult cycleWrite = new BerTlvWriterImplementation(
            new AsnResourceLimits()).Write(new BerWriteRequest
            {
                Tlv = cycle,
                EncodingRules = AsnEncodingRules.Ber
            });

        AsnResourceLimits oneChild = new AsnResourceLimits
        {
            MaximumCollectionElements = 1
        };
        BerReadResult children = new BerTlvReaderImplementation(oneChild)
            .ReadSpan(
                [0x30, 0x06, 0x04, 0x01, 0x01, 0x04, 0x01, 0x02],
                AsnEncodingRules.Ber);

        Assert.Multiple(() =>
        {
            Assert.That(deepRead.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(deepRead.Diagnostics.Single().Code,
                Is.EqualTo("ASNR0004"));
            Assert.That(cycleWrite.Success, Is.False);
            Assert.That(cycleWrite.Diagnostics.Single().Code,
                Is.EqualTo("ASNE0021"));
            Assert.That(children.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(children.Diagnostics.Single().Code,
                Is.EqualTo("ASNR0007"));
        });
    }

    private static string Format(AsnDiagnostic[] diagnostics)
    {
        return string.Join(" | ", diagnostics.Select(item =>
            item.Code + ":" + item.Message));
    }
}
