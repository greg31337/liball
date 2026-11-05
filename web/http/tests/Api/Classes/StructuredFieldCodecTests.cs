using System.Text;

namespace http.Tests;

public class StructuredFieldCodecTests
{
    [Test]
    public void ItemParserSupportsEveryBareItemType()
    {
        StructuredFieldCodec codec = new StructuredFieldCodec();
        StructuredFieldMember integer = codec.ParseStructuredItem("-42"u8.ToArray());
        StructuredFieldMember decimalValue = codec.ParseStructuredItem("12.340"u8.ToArray());
        StructuredFieldMember text = codec.ParseStructuredItem("\"hello \\\"you\\\" \\\\\""u8.ToArray());
        StructuredFieldMember token = codec.ParseStructuredItem("gzip/1.0:fast"u8.ToArray());
        StructuredFieldMember bytes = codec.ParseStructuredItem(":AAEC/w==:"u8.ToArray());
        StructuredFieldMember boolean = codec.ParseStructuredItem("?0"u8.ToArray());
        StructuredFieldMember date = codec.ParseStructuredItem("@1659578233"u8.ToArray());
        StructuredFieldMember display = codec.ParseStructuredItem(
            "%\"This is for %c3%bcsers.%22\""u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(integer.ValueKind, Is.EqualTo(HttpFieldValueKind.Integer));
            Assert.That(integer.IntegerValue, Is.EqualTo(-42));
            Assert.That(decimalValue.ValueKind, Is.EqualTo(HttpFieldValueKind.Decimal));
            Assert.That(decimalValue.DecimalThousandths, Is.EqualTo(12340));
            Assert.That(text.TextValue, Is.EqualTo("hello \"you\" \\"));
            Assert.That(token.TextValue, Is.EqualTo("gzip/1.0:fast"));
            Assert.That(bytes.ByteSequenceValue, Is.EqualTo(new byte[] { 0, 1, 2, 255 }));
            Assert.That(boolean.BooleanValue, Is.False);
            Assert.That(date.DateSeconds, Is.EqualTo(1659578233));
            Assert.That(display.ValueKind, Is.EqualTo(HttpFieldValueKind.DisplayString));
            Assert.That(display.TextValue, Is.EqualTo("This is for üsers.\""));
        });
    }

    [Test]
    public void NumericParserNormalizesZerosAndAcceptsExactBounds()
    {
        StructuredFieldCodec codec = new StructuredFieldCodec();
        StructuredFieldMember maximum = codec.ParseStructuredItem("999999999999999"u8.ToArray());
        StructuredFieldMember minimum = codec.ParseStructuredItem("-999999999999999"u8.ToArray());
        StructuredFieldMember decimalMaximum = codec.ParseStructuredItem(
            "999999999999.999"u8.ToArray());
        StructuredFieldMember signedZero = codec.ParseStructuredItem("-0.000"u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(maximum.IntegerValue, Is.EqualTo(999999999999999));
            Assert.That(minimum.IntegerValue, Is.EqualTo(-999999999999999));
            Assert.That(decimalMaximum.DecimalThousandths, Is.EqualTo(999999999999999));
            Assert.That(signedZero.DecimalThousandths, Is.Zero);
        });
    }

    [Test]
    public void NumericParserRejectsOutOfRangeAndMalformedNumbers()
    {
        StructuredFieldCodec codec = new StructuredFieldCodec();
        string[] malformed =
        [
            "1000000000000000",
            "-1000000000000000",
            "1000000000000.0",
            "1.0000",
            "1.",
            "-",
            "+1",
            "@1.5"
        ];

        foreach (string value in malformed)
        {
            Assert.Throws<HttpParseException>(() =>
                codec.ParseStructuredItem(Encoding.ASCII.GetBytes(value)), value);
        }
    }

    [Test]
    public void StringTokenBooleanAndDateParsingIsStrict()
    {
        StructuredFieldCodec codec = new StructuredFieldCodec();
        byte[][] malformed =
        [
            "\"bad\\nescape\""u8.ToArray(),
            new byte[] { 0x22, 0x61, 0x7f, 0x22 },
            "1token"u8.ToArray(),
            "?2"u8.ToArray(),
            "?10"u8.ToArray(),
            "@"u8.ToArray(),
            "@-"u8.ToArray(),
            " token\t"u8.ToArray()
        ];

        foreach (byte[] value in malformed)
        {
            HttpParseException error = Assert.Throws<HttpParseException>(() =>
                codec.ParseStructuredItem(value));
            Assert.That(error.DiagnosticCode, Is.EqualTo("invalid-structured-field"));
        }
    }

    [Test]
    public void ByteSequenceParserAcceptsMissingPaddingAndRejectsInvalidBase64()
    {
        StructuredFieldCodec codec = new StructuredFieldCodec();
        StructuredFieldMember noPadding = codec.ParseStructuredItem(":Zm8:"u8.ToArray());
        StructuredFieldMember partialPadding = codec.ParseStructuredItem(":Zg=:"u8.ToArray());
        StructuredFieldMember empty = codec.ParseStructuredItem("::"u8.ToArray());

        string[] malformed = [":A:", ":Z===:", ":Zm-8:", ":Z=g:", ":Zm8"];
        foreach (string value in malformed)
        {
            Assert.Throws<HttpParseException>(() =>
                codec.ParseStructuredItem(Encoding.ASCII.GetBytes(value)), value);
        }

        Assert.Multiple(() =>
        {
            Assert.That(noPadding.ByteSequenceValue, Is.EqualTo("fo"u8.ToArray()));
            Assert.That(partialPadding.ByteSequenceValue, Is.EqualTo("f"u8.ToArray()));
            Assert.That(empty.ByteSequenceValue, Is.Empty);
        });
    }

    [Test]
    public void DisplayStringsRequireLowercaseEscapesAndScalarUtf8()
    {
        StructuredFieldCodec codec = new StructuredFieldCodec();
        StructuredFieldMember emoji = codec.ParseStructuredItem("%\"ok %f0%9f%98%80\""u8.ToArray());
        byte[][] malformed =
        [
            "%\"%C3%BC\""u8.ToArray(),
            "%\"%zz\""u8.ToArray(),
            "%\"%c0%80\""u8.ToArray(),
            "%\"%ed%a0%80\""u8.ToArray(),
            "%\"unterminated"u8.ToArray()
        ];

        foreach (byte[] value in malformed)
        {
            Assert.Throws<HttpParseException>(() => codec.ParseStructuredItem(value));
        }

        Assert.That(emoji.TextValue, Is.EqualTo("ok 😀"));
    }

    [Test]
    public void ParametersAreOrderedAndDuplicateKeysUseTheLastValueInPlace()
    {
        StructuredFieldCodec codec = new StructuredFieldCodec();
        StructuredFieldMember item = codec.ParseStructuredItem(
            "token;a=1;b;a=2;c=?1"u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(item.Parameters, Has.Count.EqualTo(3));
            Assert.That(item.Parameters[0].Name, Is.EqualTo("a"));
            Assert.That(item.Parameters[0].BareItem!.IntegerValue, Is.EqualTo(2));
            Assert.That(item.Parameters[1].Name, Is.EqualTo("b"));
            Assert.That(item.Parameters[1].BareItem!.BooleanValue, Is.True);
            Assert.That(item.Parameters[2].Name, Is.EqualTo("c"));
            Assert.That(item.Parameters[2].BareItem!.BooleanValue, Is.True);
        });
    }

    [Test]
    public void ListParserHandlesItemsInnerListsAndTheirParameters()
    {
        StructuredFieldCodec codec = new StructuredFieldCodec();
        StructuredFieldValue value = codec.ParseStructuredList(
            "  \"foo\";a=1, (\"bar\";b=?0 baz);lvl=5, ()\t"u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(value.IsList, Is.True);
            Assert.That(value.Members, Has.Count.EqualTo(3));
            Assert.That(value.Members[0].TextValue, Is.EqualTo("foo"));
            Assert.That(value.Members[0].Parameters[0].BareItem!.IntegerValue, Is.EqualTo(1));
            Assert.That(value.Members[1].IsInnerList, Is.True);
            Assert.That(value.Members[1].InnerList, Has.Count.EqualTo(2));
            Assert.That(value.Members[1].InnerList[0].Parameters[0].BareItem!.BooleanValue,
                Is.False);
            Assert.That(value.Members[1].Parameters[0].BareItem!.IntegerValue, Is.EqualTo(5));
            Assert.That(value.Members[2].IsInnerList, Is.True);
            Assert.That(value.Members[2].InnerList, Is.Empty);
        });
    }

    [Test]
    public void ListParserRejectsEmptyMembersBadWhitespaceAndNestedLists()
    {
        StructuredFieldCodec codec = new StructuredFieldCodec();
        string[] malformed =
        [
            "token,",
            ",token",
            "token,,other",
            "(one\ttwo)",
            "((one))",
            "(one,two)",
            "(one"
        ];

        foreach (string value in malformed)
        {
            Assert.Throws<HttpParseException>(() =>
                codec.ParseStructuredList(Encoding.ASCII.GetBytes(value)), value);
        }
    }

    [Test]
    public void DictionaryParserHandlesImplicitTrueInnerListsAndDuplicateReplacement()
    {
        StructuredFieldCodec codec = new StructuredFieldCodec();
        StructuredFieldValue value = codec.ParseStructuredDictionary(
            "a=?0, b, c;foo=bar, feelings=(joy sadness), a=42"u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(value.IsDictionary, Is.True);
            Assert.That(value.Members, Has.Count.EqualTo(4));
            Assert.That(value.Members[0].DictionaryKey, Is.EqualTo("a"));
            Assert.That(value.Members[0].IntegerValue, Is.EqualTo(42));
            Assert.That(value.Members[0].HasExplicitDictionaryValue, Is.True);
            Assert.That(value.Members[1].DictionaryKey, Is.EqualTo("b"));
            Assert.That(value.Members[1].BooleanValue, Is.True);
            Assert.That(value.Members[1].HasExplicitDictionaryValue, Is.False);
            Assert.That(value.Members[2].Parameters[0].BareItem!.TextValue, Is.EqualTo("bar"));
            Assert.That(value.Members[3].InnerList, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void CanonicalItemSerializationNormalizesNumbersEscapesAndBinaryData()
    {
        StructuredFieldCodec codec = new StructuredFieldCodec();
        string[] inputs =
        [
            "00042",
            "-0",
            "4.500",
            "-0.010",
            "\"a\\\"b\\\\c\"",
            ":Zm8:",
            "?1",
            "@00042",
            "%\"100%25 %c3%bcsers\""
        ];
        string[] expected =
        [
            "42",
            "0",
            "4.5",
            "-0.01",
            "\"a\\\"b\\\\c\"",
            ":Zm8=:",
            "?1",
            "@42",
            "%\"100%25 %c3%bcsers\""
        ];

        int index = 0;
        while (index < inputs.Length)
        {
            StructuredFieldMember member = codec.ParseStructuredItem(
                Encoding.ASCII.GetBytes(inputs[index]));
            StructuredFieldValue value = ItemValue(member);
            byte[] serialized = codec.SerializeStructuredValue(value);
            Assert.That(Encoding.ASCII.GetString(serialized), Is.EqualTo(expected[index]));
            index = index + 1;
        }
    }

    [Test]
    public void CanonicalCollectionSerializationOmitsTrueAndUsesRequiredSpacing()
    {
        StructuredFieldCodec codec = new StructuredFieldCodec();
        StructuredFieldValue list = codec.ParseStructuredList(
            "one;a=?1,(two   three);ok=?1"u8.ToArray());
        StructuredFieldValue dictionary = codec.ParseStructuredDictionary(
            "a=?1, b=?0, c;foo=?1, d=(1 2);valid=?1"u8.ToArray());

        byte[] listBytes = codec.SerializeStructuredValue(list);
        byte[] dictionaryBytes = codec.SerializeStructuredValue(dictionary);

        Assert.Multiple(() =>
        {
            Assert.That(Encoding.ASCII.GetString(listBytes),
                Is.EqualTo("one;a, (two three);ok"));
            Assert.That(Encoding.ASCII.GetString(dictionaryBytes),
                Is.EqualTo("a, b=?0, c;foo, d=(1 2);valid"));
        });
    }

    [Test]
    public void SerializerRejectsInvalidUnionKeysDuplicatesAndBareParameterShapes()
    {
        StructuredFieldCodec codec = new StructuredFieldCodec();
        StructuredFieldValue noKind = new StructuredFieldValue();
        StructuredFieldValue duplicateDictionary = codec.ParseStructuredDictionary("a=1,b=2"u8.ToArray());
        duplicateDictionary.Members[1].DictionaryKey = "a";
        StructuredFieldMember parameterized = codec.ParseStructuredItem("one;a"u8.ToArray());
        parameterized.Parameters.Add(new StructuredFieldParameter
        {
            Name = "A",
            BareItem = new StructuredFieldMember
            {
                ValueKind = HttpFieldValueKind.Boolean,
                BooleanValue = true
            }
        });

        Assert.Multiple(() =>
        {
            Assert.Throws<HttpFieldException>(() => codec.SerializeStructuredValue(noKind));
            Assert.Throws<HttpFieldException>(() =>
                codec.SerializeStructuredValue(duplicateDictionary));
            Assert.Throws<HttpFieldException>(() =>
                codec.SerializeStructuredValue(ItemValue(parameterized)));
        });
    }

    [Test]
    public void DisplayStringSerializerRejectsUnpairedSurrogates()
    {
        StructuredFieldCodec codec = new StructuredFieldCodec();
        StructuredFieldMember member = new StructuredFieldMember();
        member.ValueKind = HttpFieldValueKind.DisplayString;
        member.TextValue = "bad\ud800value";

        Assert.Throws<HttpFieldException>(() =>
            codec.SerializeStructuredValue(ItemValue(member)));
    }

    [Test]
    public void ParserSupportsRequiredTopLevelCardinalityAndLimitsTheNextMember()
    {
        StructuredFieldCodec codec = new StructuredFieldCodec();
        string supported = string.Join(",", Enumerable.Repeat("a", 1024));
        string excessive = supported + ",a";

        StructuredFieldValue parsed = codec.ParseStructuredList(Encoding.ASCII.GetBytes(supported));
        HttpLimitExceededException error = Assert.Throws<HttpLimitExceededException>(() =>
            codec.ParseStructuredList(Encoding.ASCII.GetBytes(excessive)));

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Members, Has.Count.EqualTo(1024));
            Assert.That(error.LimitName, Is.EqualTo("structured-list-members"));
            Assert.That(error.Limit, Is.EqualTo(1024));
            Assert.That(error.Actual, Is.EqualTo(1025));
        });
    }

    [Test]
    public void EmptyListsAndDictionariesRoundTripAsEmptyFieldValues()
    {
        IStructuredFieldCodec codec = new StructuredFieldCodec();
        StructuredFieldValue list = codec.ParseStructuredList(Array.Empty<byte>());
        StructuredFieldValue dictionary = codec.ParseStructuredDictionary("   "u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(list.Members, Is.Empty);
            Assert.That(dictionary.Members, Is.Empty);
            Assert.That(codec.SerializeStructuredValue(list), Is.Empty);
            Assert.That(codec.SerializeStructuredValue(dictionary), Is.Empty);
        });
    }

    [Test]
    public void Rfc9651ItemAndListExamplesRoundTripCanonically()
    {
        IStructuredFieldCodec codec = new StructuredFieldCodec();
        StructuredFieldMember item = codec.ParseStructuredItem(
            "2; foourl=\"https://foo.example.com/\""u8.ToArray());
        StructuredFieldValue tokens = codec.ParseStructuredList(
            "sugar, tea, rum"u8.ToArray());
        StructuredFieldValue strings = codec.ParseStructuredList(
            "(\"foo\" \"bar\"), (\"baz\"), (\"bat\" \"one\"), ()"u8.ToArray());
        StructuredFieldValue parameters = codec.ParseStructuredList(
            "abc;a=1;b=2; cde_456, (ghi;jk=4 l);q=\"9\";r=w"u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(Text(codec.SerializeStructuredValue(ItemValue(item))),
                Is.EqualTo("2;foourl=\"https://foo.example.com/\""));
            Assert.That(Text(codec.SerializeStructuredValue(tokens)),
                Is.EqualTo("sugar, tea, rum"));
            Assert.That(Text(codec.SerializeStructuredValue(strings)),
                Is.EqualTo("(\"foo\" \"bar\"), (\"baz\"), (\"bat\" \"one\"), ()"));
            Assert.That(Text(codec.SerializeStructuredValue(parameters)),
                Is.EqualTo("abc;a=1;b=2;cde_456, (ghi;jk=4 l);q=\"9\";r=w"));
        });
    }

    [Test]
    public void Rfc9651DictionaryExamplesRoundTripCanonically()
    {
        IStructuredFieldCodec codec = new StructuredFieldCodec();
        string[] inputs =
        [
            "en=\"Applepie\", da=:w4ZibGV0w6ZydGU=:",
            "a=?0, b, c; foo=bar",
            "rating=1.5, feelings=(joy sadness)",
            "a=(1 2), b=3, c=4;aa=bb, d=(5 6);valid"
        ];
        string[] expected =
        [
            "en=\"Applepie\", da=:w4ZibGV0w6ZydGU=:",
            "a=?0, b, c;foo=bar",
            "rating=1.5, feelings=(joy sadness)",
            "a=(1 2), b=3, c=4;aa=bb, d=(5 6);valid"
        ];

        int index = 0;
        while (index < inputs.Length)
        {
            StructuredFieldValue value = codec.ParseStructuredDictionary(
                Encoding.ASCII.GetBytes(inputs[index]));
            Assert.That(Text(codec.SerializeStructuredValue(value)),
                Is.EqualTo(expected[index]), inputs[index]);
            index = index + 1;
        }
    }

    [Test]
    public void RequiredScalarAndKeySizesParseAndSerializeWithoutTruncation()
    {
        IStructuredFieldCodec codec = new StructuredFieldCodec();
        string stringContent = new string('s', 1024);
        string tokenContent = "t" + new string('x', 511);
        byte[] binaryContent = new byte[16384];
        int byteIndex = 0;
        while (byteIndex < binaryContent.Length)
        {
            binaryContent[byteIndex] = (byte)(byteIndex % 251);
            byteIndex = byteIndex + 1;
        }
        string key = "k" + new string('x', 63);
        string binaryWire = ":" + Convert.ToBase64String(binaryContent) + ":";

        StructuredFieldMember text = codec.ParseStructuredItem(
            Encoding.ASCII.GetBytes("\"" + stringContent + "\""));
        StructuredFieldMember token = codec.ParseStructuredItem(
            Encoding.ASCII.GetBytes(tokenContent));
        StructuredFieldMember bytes = codec.ParseStructuredItem(
            Encoding.ASCII.GetBytes(binaryWire));
        StructuredFieldValue dictionary = codec.ParseStructuredDictionary(
            Encoding.ASCII.GetBytes(key + "=1"));
        StructuredFieldMember parameter = codec.ParseStructuredItem(
            Encoding.ASCII.GetBytes("1;" + key));

        Assert.Multiple(() =>
        {
            Assert.That(text.TextValue, Has.Length.EqualTo(1024));
            Assert.That(token.TextValue, Has.Length.EqualTo(512));
            Assert.That(bytes.ByteSequenceValue, Is.EqualTo(binaryContent));
            Assert.That(dictionary.Members[0].DictionaryKey, Is.EqualTo(key));
            Assert.That(parameter.Parameters[0].Name, Is.EqualTo(key));
            Assert.That(Text(codec.SerializeStructuredValue(ItemValue(text))),
                Is.EqualTo("\"" + stringContent + "\""));
            Assert.That(Text(codec.SerializeStructuredValue(ItemValue(token))),
                Is.EqualTo(tokenContent));
            Assert.That(Text(codec.SerializeStructuredValue(ItemValue(bytes))),
                Is.EqualTo(binaryWire));
        });
    }

    [Test]
    public void RequiredDateRangeAndSignedNumericBoundariesRoundTrip()
    {
        IStructuredFieldCodec codec = new StructuredFieldCodec();
        string[] inputs =
        [
            "-999999999999999",
            "999999999999999",
            "-999999999999.999",
            "999999999999.999",
            "@-62135596800",
            "@253402214400"
        ];

        foreach (string input in inputs)
        {
            StructuredFieldMember item = codec.ParseStructuredItem(
                Encoding.ASCII.GetBytes(input));
            Assert.That(Text(codec.SerializeStructuredValue(ItemValue(item))),
                Is.EqualTo(input), input);
        }
    }

    [Test]
    public void ParserSupportsRequiredInnerListAndParameterCardinality()
    {
        IStructuredFieldCodec codec = new StructuredFieldCodec();
        string innerSupported = "(" + string.Join(" ", Enumerable.Repeat("1", 256)) + ")";
        string innerExcessive = "(" + string.Join(" ", Enumerable.Repeat("1", 257)) + ")";
        StringBuilder parameterSupported = new StringBuilder("1");
        int index = 0;
        while (index < 256)
        {
            parameterSupported.Append(";p");
            parameterSupported.Append(index);
            index = index + 1;
        }

        string parameterExcessive = parameterSupported + ";overflow";
        StructuredFieldValue parsedInner = codec.ParseStructuredList(
            Encoding.ASCII.GetBytes(innerSupported));
        StructuredFieldMember parsedParameters = codec.ParseStructuredItem(
            Encoding.ASCII.GetBytes(parameterSupported.ToString()));
        HttpLimitExceededException innerError = Assert.Throws<HttpLimitExceededException>(() =>
            codec.ParseStructuredList(Encoding.ASCII.GetBytes(innerExcessive)))!;
        HttpLimitExceededException parameterError = Assert.Throws<HttpLimitExceededException>(() =>
            codec.ParseStructuredItem(Encoding.ASCII.GetBytes(parameterExcessive)))!;

        Assert.Multiple(() =>
        {
            Assert.That(parsedInner.Members[0].InnerList, Has.Count.EqualTo(256));
            Assert.That(parsedParameters.Parameters, Has.Count.EqualTo(256));
            Assert.That(innerError.LimitName, Is.EqualTo("structured-inner-list-members"));
            Assert.That(innerError.Actual, Is.EqualTo(257));
            Assert.That(parameterError.LimitName, Is.EqualTo("structured-parameters"));
            Assert.That(parameterError.Actual, Is.EqualTo(257));
        });
    }

    [Test]
    public void ParserSupportsRequiredDictionaryCardinalityAndRejectsTheNextMember()
    {
        IStructuredFieldCodec codec = new StructuredFieldCodec();
        string supported = string.Join(",", Enumerable.Range(0, 1024).Select(
            index => "k" + index));
        string excessive = supported + ",overflow";

        StructuredFieldValue parsed = codec.ParseStructuredDictionary(
            Encoding.ASCII.GetBytes(supported));
        HttpLimitExceededException error = Assert.Throws<HttpLimitExceededException>(() =>
            codec.ParseStructuredDictionary(Encoding.ASCII.GetBytes(excessive)))!;

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Members, Has.Count.EqualTo(1024));
            Assert.That(error.LimitName, Is.EqualTo("structured-dictionary-members"));
            Assert.That(error.Limit, Is.EqualTo(1024));
            Assert.That(error.Actual, Is.EqualTo(1025));
        });
    }

    [Test]
    public void RfcParsingAlgorithmsRejectMalformedTopLevelAndBareItemForms()
    {
        IStructuredFieldCodec codec = new StructuredFieldCodec();
        byte[][] itemValues =
        [
            [],
            "(one)"u8.ToArray(),
            "42 extra"u8.ToArray(),
            "'single quotes'"u8.ToArray(),
            "\"bad\\escape\""u8.ToArray(),
            new byte[] { 0x22, 0x7f, 0x22 },
            "9token"u8.ToArray(),
            ":abc\n:"u8.ToArray(),
            "??"u8.ToArray(),
            "@1.0"u8.ToArray(),
            "%\"%C3%BC\""u8.ToArray()
        ];
        string[] listValues =
        [
            "one,",
            ",one",
            "one,,two",
            "(one\ttwo)",
            "((one))",
            "(one,two)"
        ];
        string[] dictionaryValues =
        [
            "Upper=1",
            "a=",
            "a =1",
            "a=1 b=2",
            "a=(one,two)",
            "a=1,"
        ];

        foreach (byte[] value in itemValues)
        {
            Assert.Throws<HttpParseException>(() => codec.ParseStructuredItem(value));
        }

        foreach (string value in listValues)
        {
            Assert.Throws<HttpParseException>(() =>
                codec.ParseStructuredList(Encoding.ASCII.GetBytes(value)), value);
        }

        foreach (string value in dictionaryValues)
        {
            Assert.Throws<HttpParseException>(() =>
                codec.ParseStructuredDictionary(Encoding.ASCII.GetBytes(value)), value);
        }
    }

    [Test]
    public void SerializerRejectsInvalidStateForEveryBareItemKind()
    {
        IStructuredFieldCodec codec = new StructuredFieldCodec();
        StructuredFieldMember integer = new StructuredFieldMember();
        integer.ValueKind = HttpFieldValueKind.Integer;
        integer.IntegerValue = 1000000000000000;
        StructuredFieldMember decimalValue = new StructuredFieldMember();
        decimalValue.ValueKind = HttpFieldValueKind.Decimal;
        decimalValue.DecimalThousandths = 1000000000000000;
        StructuredFieldMember text = new StructuredFieldMember();
        text.ValueKind = HttpFieldValueKind.String;
        text.TextValue = "bad\ntext";
        StructuredFieldMember token = new StructuredFieldMember();
        token.ValueKind = HttpFieldValueKind.Token;
        token.TextValue = "9token";
        StructuredFieldMember bytes = new StructuredFieldMember();
        bytes.ValueKind = HttpFieldValueKind.ByteSequence;
        bytes.ByteSequenceValue = null!;
        StructuredFieldMember date = new StructuredFieldMember();
        date.ValueKind = HttpFieldValueKind.Date;
        date.DateSeconds = 1000000000000000;
        StructuredFieldMember display = new StructuredFieldMember();
        display.ValueKind = HttpFieldValueKind.DisplayString;
        display.TextValue = "bad\udffftext";

        Assert.Multiple(() =>
        {
            Assert.Throws<HttpFieldException>(() =>
                codec.SerializeStructuredValue(ItemValue(integer)));
            Assert.Throws<HttpFieldException>(() =>
                codec.SerializeStructuredValue(ItemValue(decimalValue)));
            Assert.Throws<HttpFieldException>(() =>
                codec.SerializeStructuredValue(ItemValue(text)));
            Assert.Throws<HttpFieldException>(() =>
                codec.SerializeStructuredValue(ItemValue(token)));
            Assert.Throws<HttpFieldException>(() =>
                codec.SerializeStructuredValue(ItemValue(bytes)));
            Assert.Throws<HttpFieldException>(() =>
                codec.SerializeStructuredValue(ItemValue(date)));
            Assert.Throws<HttpFieldException>(() =>
                codec.SerializeStructuredValue(ItemValue(display)));
        });
    }

    [Test]
    public void NullInputsFailThroughTheDocumentedPublicExceptions()
    {
        IStructuredFieldCodec codec = new StructuredFieldCodec();

        Assert.Multiple(() =>
        {
            Assert.Throws<HttpParseException>(() => codec.ParseStructuredItem(null!));
            Assert.Throws<HttpParseException>(() => codec.ParseStructuredList(null!));
            Assert.Throws<HttpParseException>(() => codec.ParseStructuredDictionary(null!));
            Assert.Throws<HttpFieldException>(() => codec.SerializeStructuredValue(null!));
        });
    }

    static StructuredFieldValue ItemValue(StructuredFieldMember member)
    {
        StructuredFieldValue value = new StructuredFieldValue();
        value.IsItem = true;
        value.Item = member;
        return value;
    }

    static string Text(byte[] value)
    {
        return Encoding.ASCII.GetString(value);
    }
}
