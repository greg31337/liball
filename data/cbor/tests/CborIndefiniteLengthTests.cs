using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborIndefiniteLengthTests
{
    [Test]
    public void IndefiniteContainersInteractWithNestedDefiniteContainers()
    {
        CborTokenReader reader = CreateReader();

        CborTokenReadResult arrayStart = reader.ReadToken([0x9f]);
        CborTokenReadResult firstItem = reader.ReadToken([0x01]);
        CborTokenReadResult mapStart = reader.ReadToken([0xbf]);
        CborTokenReadResult mapKey = reader.ReadToken([0x02]);
        CborTokenReadResult mapValue = reader.ReadToken([0x03]);
        CborTokenReadResult mapEnd = reader.ReadToken([0xff]);
        CborTokenReadResult nestedStart = reader.ReadToken([0x81]);
        CborTokenReadResult nestedItem = reader.ReadToken([0x04]);
        CborTokenReadResult nestedEnd = reader.ReadToken([0xff]);
        CborTokenReadResult arrayEnd = reader.ReadToken([0xff, 0x05]);
        CborTokenReadResult trailing = reader.ReadToken([0x05]);

        Assert.Multiple(() =>
        {
            Assert.That(arrayStart.Token?.Kind, Is.EqualTo(CborTokenKind.StartArray));
            Assert.That(arrayStart.Token?.LengthKind, Is.EqualTo(CborLengthKind.Indefinite));
            Assert.That(firstItem.Token?.Kind, Is.EqualTo(CborTokenKind.UnsignedInteger));
            Assert.That(mapStart.Token?.Kind, Is.EqualTo(CborTokenKind.StartMap));
            Assert.That(mapStart.Token?.LengthKind, Is.EqualTo(CborLengthKind.Indefinite));
            Assert.That(mapKey.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(mapValue.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(mapEnd.Token?.Kind, Is.EqualTo(CborTokenKind.EndMap));
            Assert.That(mapEnd.Token?.LengthKind, Is.EqualTo(CborLengthKind.Indefinite));
            Assert.That(mapEnd.Consumed, Is.EqualTo(1));
            Assert.That(nestedStart.Token?.Kind, Is.EqualTo(CborTokenKind.StartArray));
            Assert.That(nestedStart.Token?.LengthKind, Is.EqualTo(CborLengthKind.Definite));
            Assert.That(nestedItem.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(nestedEnd.Token?.Kind, Is.EqualTo(CborTokenKind.EndArray));
            Assert.That(nestedEnd.Token?.LengthKind, Is.EqualTo(CborLengthKind.Definite));
            Assert.That(nestedEnd.Consumed, Is.Zero);
            Assert.That(nestedEnd.Remaining, Is.EqualTo(1));
            Assert.That(arrayEnd.Token?.Kind, Is.EqualTo(CborTokenKind.EndArray));
            Assert.That(arrayEnd.Token?.LengthKind, Is.EqualTo(CborLengthKind.Indefinite));
            Assert.That(arrayEnd.Consumed, Is.EqualTo(1));
            Assert.That(arrayEnd.Remaining, Is.EqualTo(1));
            Assert.That(trailing.Token?.Kind, Is.EqualTo(CborTokenKind.UnsignedInteger));
        });
    }

    [Test]
    public void IndefiniteByteStringProducesChunksAndRetainsTheirLengths()
    {
        CborTokenReader reader = CreateReader();

        CborTokenReadResult start = reader.ReadToken([0x5f]);
        CborTokenReadResult first = reader.ReadToken([0x42, 0x01, 0x02]);
        CborTokenReadResult empty = reader.ReadToken([0x40]);
        CborTokenReadResult end = reader.ReadToken([0xff, 0x07]);
        CborTokenReadResult trailing = reader.ReadToken([0x07]);

        Assert.Multiple(() =>
        {
            Assert.That(start.Token?.Kind, Is.EqualTo(CborTokenKind.StartByteString));
            Assert.That(start.Token?.LengthKind, Is.EqualTo(CborLengthKind.Indefinite));
            Assert.That(start.Token?.Representation?.ChunkByteLengths, Is.EqualTo(new[] { 2, 0 }));
            Assert.That(start.Token?.Representation?.SourceOffset, Is.Zero);
            Assert.That(start.Token?.Representation?.SourceLength, Is.EqualTo(1));
            Assert.That(first.Token?.Kind, Is.EqualTo(CborTokenKind.ByteStringChunk));
            Assert.That(first.Token?.LengthKind, Is.EqualTo(CborLengthKind.Definite));
            Assert.That(first.Token?.ByteStringValue, Is.EqualTo(new byte[] { 0x01, 0x02 }));
            Assert.That(first.Token?.Length, Is.EqualTo(2));
            Assert.That(empty.Token?.Kind, Is.EqualTo(CborTokenKind.ByteStringChunk));
            Assert.That(empty.Token?.ByteStringValue, Is.Empty);
            Assert.That(end.Token?.Kind, Is.EqualTo(CborTokenKind.EndByteString));
            Assert.That(end.Token?.LengthKind, Is.EqualTo(CborLengthKind.Indefinite));
            Assert.That(end.Token?.Representation?.SourceOffset, Is.EqualTo(5));
            Assert.That(end.Token?.Representation?.SourceLength, Is.EqualTo(1));
            Assert.That(end.Consumed, Is.EqualTo(1));
            Assert.That(end.Remaining, Is.EqualTo(1));
            Assert.That(trailing.Token?.Kind, Is.EqualTo(CborTokenKind.UnsignedInteger));
        });
    }

    [Test]
    public void IndefiniteTextStringValidatesEachDefiniteChunk()
    {
        CborTokenReader reader = CreateReader();

        CborTokenReadResult start = reader.ReadToken([0x7f]);
        CborTokenReadResult ascii = reader.ReadToken([0x61, 0x61]);
        CborTokenReadResult unicode = reader.ReadToken([0x63, 0xe2, 0x82, 0xac]);
        CborTokenReadResult end = reader.ReadToken([0xff]);

        Assert.Multiple(() =>
        {
            Assert.That(start.Token?.Kind, Is.EqualTo(CborTokenKind.StartTextString));
            Assert.That(start.Token?.Representation?.ChunkByteLengths, Is.EqualTo(new[] { 1, 3 }));
            Assert.That(ascii.Token?.Kind, Is.EqualTo(CborTokenKind.TextStringChunk));
            Assert.That(ascii.Token?.TextStringValue, Is.EqualTo("a"));
            Assert.That(unicode.Token?.Kind, Is.EqualTo(CborTokenKind.TextStringChunk));
            Assert.That(unicode.Token?.TextStringValue, Is.EqualTo("€"));
            Assert.That(end.Token?.Kind, Is.EqualTo(CborTokenKind.EndTextString));
            Assert.That(end.Consumed, Is.EqualTo(1));
        });
    }

    [Test]
    public void IndefiniteStringsRejectWrongAndNestedChunkTypesWithoutLosingState()
    {
        CborTokenReader byteReader = CreateReader();
        byteReader.ReadToken([0x5f]);
        CborTokenReadResult textChunk = byteReader.ReadToken([0x61, 0x61]);
        CborTokenReadResult nestedBytes = byteReader.ReadToken([0x5f]);
        CborTokenReadResult validBytes = byteReader.ReadToken([0x41, 0xaa]);
        CborTokenReadResult byteEnd = byteReader.ReadToken([0xff]);

        CborTokenReader textReader = CreateReader();
        textReader.ReadToken([0x7f]);
        CborTokenReadResult byteChunk = textReader.ReadToken([0x41, 0x61]);
        CborTokenReadResult nestedText = textReader.ReadToken([0x7f]);
        CborTokenReadResult incompleteUtf8 = textReader.ReadToken([0x62, 0xe2, 0x82]);
        CborTokenReadResult validText = textReader.ReadToken([0x61, 0x61]);
        CborTokenReadResult textEnd = textReader.ReadToken([0xff]);

        Assert.Multiple(() =>
        {
            Assert.That(textChunk.ErrorCode, Is.EqualTo("CBOR_INVALID_BYTE_STRING_CHUNK"));
            Assert.That(nestedBytes.ErrorCode, Is.EqualTo("CBOR_NESTED_INDEFINITE_BYTE_STRING"));
            Assert.That(validBytes.Token?.Kind, Is.EqualTo(CborTokenKind.ByteStringChunk));
            Assert.That(byteEnd.Token?.Kind, Is.EqualTo(CborTokenKind.EndByteString));
            Assert.That(byteChunk.ErrorCode, Is.EqualTo("CBOR_INVALID_TEXT_STRING_CHUNK"));
            Assert.That(nestedText.ErrorCode, Is.EqualTo("CBOR_NESTED_INDEFINITE_TEXT_STRING"));
            Assert.That(incompleteUtf8.ErrorCode, Is.EqualTo("CBOR_UTF8_INCOMPLETE_SEQUENCE"));
            Assert.That(incompleteUtf8.ErrorOffset, Is.EqualTo(2));
            Assert.That(validText.Token?.Kind, Is.EqualTo(CborTokenKind.TextStringChunk));
            Assert.That(textEnd.Token?.Kind, Is.EqualTo(CborTokenKind.EndTextString));
        });
    }

    [Test]
    public void BreakRulesRejectStrayDefiniteOddMapAndTagBreaks()
    {
        CborTokenReadResult stray = CreateReader().ReadToken([0xff]);

        CborTokenReader definiteReader = CreateReader();
        definiteReader.ReadToken([0x81]);
        CborTokenReadResult definiteBreak = definiteReader.ReadToken([0xff]);

        CborTokenReader mapReader = CreateReader();
        mapReader.ReadToken([0xbf]);
        mapReader.ReadToken([0x01]);
        CborTokenReadResult oddBreak = mapReader.ReadToken([0xff]);
        CborTokenReadResult recoveredValue = mapReader.ReadToken([0x02]);
        CborTokenReadResult recoveredEnd = mapReader.ReadToken([0xff]);

        CborTokenReader tagReader = CreateReader();
        tagReader.ReadToken([0x9f]);
        tagReader.ReadToken([0xc0]);
        CborTokenReadResult tagBreak = tagReader.ReadToken([0xff]);

        Assert.Multiple(() =>
        {
            Assert.That(stray.ErrorCode, Is.EqualTo("CBOR_UNEXPECTED_BREAK"));
            Assert.That(stray.Consumed, Is.Zero);
            Assert.That(definiteBreak.ErrorCode, Is.EqualTo("CBOR_BREAK_IN_DEFINITE_CONTAINER"));
            Assert.That(definiteBreak.Consumed, Is.Zero);
            Assert.That(oddBreak.ErrorCode, Is.EqualTo("CBOR_ODD_INDEFINITE_MAP"));
            Assert.That(oddBreak.ErrorOffset, Is.EqualTo(2));
            Assert.That(recoveredValue.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(recoveredEnd.Token?.Kind, Is.EqualTo(CborTokenKind.EndMap));
            Assert.That(tagBreak.ErrorCode, Is.EqualTo("CBOR_BREAK_AFTER_TAG"));
        });
    }

    [Test]
    public void IndefiniteLengthsCanBeDisabledForEveryLegalMajorType()
    {
        byte[] initialBytes = [0x5f, 0x7f, 0x9f, 0xbf];

        foreach (byte initialByte in initialBytes)
        {
            CborDecodeOptions options = new CborDecodeOptions
            {
                AllowIndefiniteLength = false
            };
            CborTokenReadResult result = new CborTokenReader(options).ReadToken([initialByte]);
            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(CborReadStatus.Error));
                Assert.That(result.ErrorCode, Is.EqualTo("CBOR_INDEFINITE_LENGTH_DISABLED"));
                Assert.That(result.Consumed, Is.Zero);
                Assert.That(result.Remaining, Is.EqualTo(1));
            });
        }
    }

    [Test]
    public void IllegalIndefiniteIntegerAndTagHeadsAreRejected()
    {
        CborTokenReadResult unsigned = CreateReader().ReadToken([0x1f]);
        CborTokenReadResult negative = CreateReader().ReadToken([0x3f]);
        CborTokenReadResult tag = CreateReader().ReadToken([0xdf]);

        Assert.Multiple(() =>
        {
            Assert.That(unsigned.ErrorCode, Is.EqualTo("CBOR_ILLEGAL_INDEFINITE_HEAD"));
            Assert.That(negative.ErrorCode, Is.EqualTo("CBOR_ILLEGAL_INDEFINITE_HEAD"));
            Assert.That(tag.ErrorCode, Is.EqualTo("CBOR_ILLEGAL_INDEFINITE_HEAD"));
        });
    }

    private static CborTokenReader CreateReader()
    {
        CborDecodeOptions options = new CborDecodeOptions
        {
            RetainRepresentation = true,
            RetainSourceRanges = true
        };
        return new CborTokenReader(options);
    }
}
