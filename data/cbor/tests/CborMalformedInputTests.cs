using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborMalformedInputTests
{
    [Test]
    public void EveryReservedAdditionalInformationValueIsRejected()
    {
        for (int majorType = 0; majorType <= 7; majorType++)
        {
            for (int additionalInformation = 28; additionalInformation <= 30; additionalInformation++)
            {
                byte initialByte = (byte)((majorType << 5) | additionalInformation);
                CborReadResult result = Skip([initialByte]);
                Assert.Multiple(() =>
                {
                    Assert.That(result.Status, Is.EqualTo(CborReadStatus.Error));
                    Assert.That(result.ErrorCode, Is.EqualTo("CBOR_RESERVED_ADDITIONAL_INFORMATION"));
                    Assert.That(result.ErrorOffset, Is.Zero);
                    Assert.That(result.Consumed, Is.Zero);
                    Assert.That(result.Remaining, Is.EqualTo(1));
                });
            }
        }
    }

    [Test]
    public void EveryReservedTwoByteSimpleEncodingIsRejected()
    {
        byte[][] encodings =
        [
            [0xf8, 0x00],
            [0xf8, 0x01],
            [0xf8, 0x18],
            [0xf8, 0x1f]
        ];

        foreach (byte[] encoding in encodings)
        {
            CborReadResult result = Skip(encoding);
            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(CborReadStatus.Error));
                Assert.That(result.ErrorCode, Is.EqualTo("CBOR_INVALID_SIMPLE_VALUE_ENCODING"));
                Assert.That(result.ErrorOffset, Is.EqualTo(1));
                Assert.That(result.Consumed, Is.Zero);
                Assert.That(result.Remaining, Is.EqualTo(2));
            });
        }
    }

    [Test]
    public void EveryAppendixFWrongIndefiniteStringChunkClassIsRejected()
    {
        string[] byteStringCases =
        [
            "5f00ff",
            "5f21ff",
            "5f6100ff",
            "5f80ff",
            "5fa0ff",
            "5fc000ff",
            "5fe0ff"
        ];

        foreach (string encoding in byteStringCases)
        {
            CborReadResult result = SkipHex(encoding);
            Assert.Multiple(() =>
            {
                Assert.That(result.ErrorCode, Is.EqualTo("CBOR_INVALID_BYTE_STRING_CHUNK"));
                Assert.That(result.ErrorOffset, Is.EqualTo(1));
                Assert.That(result.Consumed, Is.EqualTo(1));
            });
        }

        CborReadResult textStringCase = SkipHex("7f4100ff");
        CborReadResult nestedBytes = SkipHex("5f5f4100ffff");
        CborReadResult nestedText = SkipHex("7f7f6100ffff");
        Assert.Multiple(() =>
        {
            Assert.That(textStringCase.ErrorCode, Is.EqualTo("CBOR_INVALID_TEXT_STRING_CHUNK"));
            Assert.That(textStringCase.ErrorOffset, Is.EqualTo(1));
            Assert.That(nestedBytes.ErrorCode, Is.EqualTo("CBOR_NESTED_INDEFINITE_BYTE_STRING"));
            Assert.That(nestedBytes.ErrorOffset, Is.EqualTo(1));
            Assert.That(nestedText.ErrorCode, Is.EqualTo("CBOR_NESTED_INDEFINITE_TEXT_STRING"));
            Assert.That(nestedText.ErrorOffset, Is.EqualTo(1));
        });
    }

    [Test]
    public void AppendixFBreakPlacementErrorsAreRejected()
    {
        string[] definiteContainerCases =
        [
            "81ff",
            "8200ff",
            "a1ff",
            "a1ff00",
            "a100ff",
            "a20000ff",
            "9f81ff",
            "9f829f819f9fffffffff"
        ];

        foreach (string encoding in definiteContainerCases)
        {
            CborReadResult result = SkipHex(encoding);
            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(CborReadStatus.Error));
                Assert.That(result.ErrorCode, Is.EqualTo("CBOR_BREAK_IN_DEFINITE_CONTAINER"));
                Assert.That(result.Consumed, Is.GreaterThanOrEqualTo(1));
            });
        }

        CborReadResult stray = SkipHex("ff");
        CborReadResult tag = SkipHex("c0ff");
        CborReadResult oddOne = SkipHex("bf00ff");
        CborReadResult oddTwo = SkipHex("bf000000ff");
        Assert.Multiple(() =>
        {
            Assert.That(stray.ErrorCode, Is.EqualTo("CBOR_UNEXPECTED_BREAK"));
            Assert.That(stray.ErrorOffset, Is.Zero);
            Assert.That(tag.ErrorCode, Is.EqualTo("CBOR_BREAK_AFTER_TAG"));
            Assert.That(tag.ErrorOffset, Is.EqualTo(1));
            Assert.That(oddOne.ErrorCode, Is.EqualTo("CBOR_ODD_INDEFINITE_MAP"));
            Assert.That(oddOne.ErrorOffset, Is.EqualTo(2));
            Assert.That(oddTwo.ErrorCode, Is.EqualTo("CBOR_ODD_INDEFINITE_MAP"));
            Assert.That(oddTwo.ErrorOffset, Is.EqualTo(4));
        });
    }

    [Test]
    public void IllegalIndefiniteIntegerAndTagHeadsAreAlwaysSyntaxErrors()
    {
        byte[] initialBytes = [0x1f, 0x3f, 0xdf];

        foreach (byte initialByte in initialBytes)
        {
            CborDecodeOptions options = new CborDecodeOptions
            {
                AllowIndefiniteLength = false,
                MaxTotalItems = 0
            };
            CborReadResult result = new CborTokenReader(options).SkipValue([initialByte]);
            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(CborReadStatus.Error));
                Assert.That(result.ErrorCode, Is.EqualTo("CBOR_ILLEGAL_INDEFINITE_HEAD"));
                Assert.That(result.ErrorOffset, Is.Zero);
                Assert.That(result.Consumed, Is.Zero);
            });
        }
    }

    [Test]
    public void SyntaxErrorsTakePrecedenceOverActiveSkipLimits()
    {
        CborDecodeOptions arrayOptions = new CborDecodeOptions { MaxArrayItems = 0 };
        CborTokenReader arrayReader = new CborTokenReader(arrayOptions);
        CborReadResult arrayStart = arrayReader.SkipValue([0x9f]);
        CborReadResult reserved = arrayReader.SkipValue([0x1c]);

        CborDecodeOptions stringOptions = new CborDecodeOptions { MaxStringBytes = 0 };
        CborTokenReader stringReader = new CborTokenReader(stringOptions);
        CborReadResult stringStart = stringReader.SkipValue([0x5f]);
        CborReadResult wrongChunk = stringReader.SkipValue([0x61, 0x61]);

        CborDecodeOptions itemOptions = new CborDecodeOptions { MaxTotalItems = 0 };
        CborReadResult invalidSimple = new CborTokenReader(itemOptions).SkipValue([0xf8, 0x00]);

        Assert.Multiple(() =>
        {
            Assert.That(arrayStart.Status, Is.EqualTo(CborReadStatus.NeedMoreData));
            Assert.That(reserved.ErrorCode, Is.EqualTo("CBOR_RESERVED_ADDITIONAL_INFORMATION"));
            Assert.That(stringStart.Status, Is.EqualTo(CborReadStatus.NeedMoreData));
            Assert.That(wrongChunk.ErrorCode, Is.EqualTo("CBOR_INVALID_BYTE_STRING_CHUNK"));
            Assert.That(invalidSimple.ErrorCode, Is.EqualTo("CBOR_INVALID_SIMPLE_VALUE_ENCODING"));
        });
    }

    [Test]
    public void EveryAppendixFTruncatedHeadReturnsNeedMoreData()
    {
        string[] encodings =
        [
            "18",
            "19",
            "1a",
            "1b",
            "1901",
            "1a0102",
            "1b01020304050607",
            "38",
            "58",
            "78",
            "98",
            "9a01ff00",
            "b8",
            "d8",
            "f8",
            "f900",
            "fa0000",
            "fb000000"
        ];

        AssertTruncatedCases(encodings);
    }

    [Test]
    public void EveryAppendixFTruncatedContainerOrTagReturnsNeedMoreData()
    {
        string[] encodings =
        [
            "81",
            "818181818181818181",
            "8200",
            "a1",
            "a20102",
            "a100",
            "a2000000",
            "c0",
            "5f4100",
            "7f6100",
            "9f",
            "9f0102",
            "bf",
            "bf01020102",
            "819f",
            "9f8000",
            "9f9f9f9f9fffffffff",
            "9f819f819f9fffffff"
        ];

        AssertTruncatedCases(encodings);
    }

    [Test]
    public void ShortOrdinaryStringsReturnNeedMoreButLengthBombsAreRejectedEarly()
    {
        CborReadResult shortBytes = SkipHex("41");
        CborReadResult shortText = SkipHex("61");
        CborReadResult largeBytes32 = SkipHex("5affffffff00");
        CborReadResult largeBytes64 = SkipHex("5bffffffffffffffff010203");
        CborReadResult largeText32 = SkipHex("7affffffff00");
        CborReadResult largeText64 = SkipHex("7b7fffffffffffffff010203");

        Assert.Multiple(() =>
        {
            Assert.That(shortBytes.Status, Is.EqualTo(CborReadStatus.NeedMoreData));
            Assert.That(shortBytes.Consumed, Is.EqualTo(1));
            Assert.That(shortText.Status, Is.EqualTo(CborReadStatus.NeedMoreData));
            Assert.That(shortText.Consumed, Is.EqualTo(1));
            Assert.That(largeBytes32.ErrorCode, Is.EqualTo("CBOR_MAX_STRING_BYTES"));
            Assert.That(largeBytes64.ErrorCode, Is.EqualTo("CBOR_MAX_STRING_BYTES"));
            Assert.That(largeText32.ErrorCode, Is.EqualTo("CBOR_MAX_STRING_BYTES"));
            Assert.That(largeText64.ErrorCode, Is.EqualTo("CBOR_MAX_STRING_BYTES"));
        });
    }

    [Test]
    public void CompleteFirstItemReportsTooMuchDataAsRemainingInput()
    {
        CborReadResult result = SkipHex("0001");

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(result.Consumed, Is.EqualTo(1));
            Assert.That(result.Remaining, Is.EqualTo(1));
            Assert.That(result.ErrorCode, Is.Null);
        });
    }

    private static void AssertTruncatedCases(string[] encodings)
    {
        foreach (string encoding in encodings)
        {
            byte[] bytes = DecodeHex(encoding);
            CborReadResult result = Skip(bytes);
            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(CborReadStatus.NeedMoreData), encoding);
                Assert.That(result.Consumed, Is.EqualTo(bytes.Length), encoding);
                Assert.That(result.Remaining, Is.Zero, encoding);
                Assert.That(result.ErrorCode, Is.Null, encoding);
                Assert.That(result.ErrorOffset, Is.EqualTo(-1), encoding);
            });
        }
    }

    private static CborReadResult SkipHex(string encoding)
    {
        return Skip(DecodeHex(encoding));
    }

    private static CborReadResult Skip(byte[] encoding)
    {
        return new CborTokenReader(new CborDecodeOptions()).SkipValue(encoding);
    }

    private static byte[] DecodeHex(string encoding)
    {
        return Convert.FromHexString(encoding);
    }
}
