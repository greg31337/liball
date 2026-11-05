namespace json.Tests;

public class JsonStringWriterTests
{
    [Test]
    public void MinimalModeUsesRequiredEscapesForEveryControlCharacter()
    {
        char[] characters = new char[0x20];
        int index = 0;
        while (index < characters.Length)
        {
            characters[index] = (char)index;
            index++;
        }

        WriterFixture fixture = new WriterFixture(
            JsonStringEscapeMode.Minimal,
            JsonUnicodePolicy.UnicodeScalar,
            false,
            false);
        AssertSuccess(fixture.Writer.WriteString(new string(characters)));
        AssertSuccess(fixture.Writer.CompleteWriter());

        Assert.That(
            fixture.Text(),
            Is.EqualTo(
                "\"\\u0000\\u0001\\u0002\\u0003\\u0004\\u0005" +
                "\\u0006\\u0007\\b\\t\\n\\u000b\\f\\r\\u000e\\u000f" +
                "\\u0010\\u0011\\u0012\\u0013\\u0014\\u0015\\u0016" +
                "\\u0017\\u0018\\u0019\\u001a\\u001b\\u001c\\u001d" +
                "\\u001e\\u001f\""));
    }

    [Test]
    public void MinimalModeEscapesJsonSyntaxAndPreservesAsciiBoundaries()
    {
        WriterFixture fixture = new WriterFixture(
            JsonStringEscapeMode.Minimal,
            JsonUnicodePolicy.UnicodeScalar,
            false,
            false);
        string value =
            "\u001f !\"#[\\]^_`{|}~\u007f\u0080é€\u2028\u2029";

        AssertSuccess(fixture.Writer.WriteString(value));
        AssertSuccess(fixture.Writer.CompleteWriter());

        Assert.That(
            fixture.Text(),
            Is.EqualTo(
                "\"\\u001f !\\\"#[\\\\]^_`{|}~" +
                "\u007f\u0080é€\u2028\u2029\""));
    }

    [Test]
    public void EveryEscapeModeAppliesEquallyToNamesAndValues()
    {
        WriterFixture minimal = new WriterFixture(
            JsonStringEscapeMode.Minimal,
            JsonUnicodePolicy.UnicodeScalar,
            false,
            false);
        WriteSingleMember(minimal, "<é>");

        WriterFixture ascii = new WriterFixture(
            JsonStringEscapeMode.AsciiOnly,
            JsonUnicodePolicy.UnicodeScalar,
            false,
            false);
        WriteSingleMember(ascii, "<é>");

        WriterFixture html = new WriterFixture(
            JsonStringEscapeMode.HtmlSafe,
            JsonUnicodePolicy.UnicodeScalar,
            false,
            false);
        WriteSingleMember(html, "<é>");

        Assert.Multiple(() =>
        {
            Assert.That(minimal.Text(), Is.EqualTo("{\"<é>\":\"<é>\"}"));
            Assert.That(
                ascii.Text(),
                Is.EqualTo("{\"<\\u00e9>\":\"<\\u00e9>\"}"));
            Assert.That(
                html.Text(),
                Is.EqualTo(
                    "{\"\\u003cé\\u003e\":" +
                    "\"\\u003cé\\u003e\"}"));
        });
    }

    [Test]
    public void AsciiOnlyModeEscapesBmpAndSupplementaryScalarBoundaries()
    {
        WriterFixture fixture = new WriterFixture(
            JsonStringEscapeMode.AsciiOnly,
            JsonUnicodePolicy.UnicodeScalar,
            false,
            false);
        string value =
            "~\u007f\u0080\u07ff\u0800\uffff\U00010000😀\U0010ffff";

        AssertSuccess(fixture.Writer.WriteString(value));
        AssertSuccess(fixture.Writer.CompleteWriter());

        Assert.That(
            fixture.Text(),
            Is.EqualTo(
                "\"~\u007f\\u0080\\u07ff\\u0800\\uffff" +
                "\\ud800\\udc00\\ud83d\\ude00\\udbff\\udfff\""));
    }

    [Test]
    public void HtmlSafeModeEscapesOnlyEmbeddingSensitiveCharacters()
    {
        WriterFixture fixture = new WriterFixture(
            JsonStringEscapeMode.HtmlSafe,
            JsonUnicodePolicy.UnicodeScalar,
            false,
            false);
        string value = "%&'()*+,-./:;<= >?@é\u2027\u2028\u2029\u202a";

        AssertSuccess(fixture.Writer.WriteString(value));
        AssertSuccess(fixture.Writer.CompleteWriter());

        Assert.That(
            fixture.Text(),
            Is.EqualTo(
                "\"%\\u0026\\u0027()*+,-./:;\\u003c\\u003d " +
                "\\u003e?@é\u2027\\u2028\\u2029\u202a\""));
    }

    [Test]
    public void SolidusEscapingIsIndependentOfTheEscapeMode()
    {
        JsonStringEscapeMode[] modes = new JsonStringEscapeMode[]
        {
            JsonStringEscapeMode.Minimal,
            JsonStringEscapeMode.AsciiOnly,
            JsonStringEscapeMode.HtmlSafe
        };

        foreach (JsonStringEscapeMode mode in modes)
        {
            WriterFixture raw = new WriterFixture(
                mode,
                JsonUnicodePolicy.UnicodeScalar,
                false,
                false);
            AssertSuccess(raw.Writer.WriteString("</script>/"));
            AssertSuccess(raw.Writer.CompleteWriter());

            WriterFixture escaped = new WriterFixture(
                mode,
                JsonUnicodePolicy.UnicodeScalar,
                true,
                false);
            AssertSuccess(escaped.Writer.WriteString("</script>/"));
            AssertSuccess(escaped.Writer.CompleteWriter());

            string expectedRaw = mode == JsonStringEscapeMode.HtmlSafe
                ? "\"\\u003c/script\\u003e/\""
                : "\"</script>/\"";
            string expectedEscaped = mode == JsonStringEscapeMode.HtmlSafe
                ? "\"\\u003c\\/script\\u003e\\/\""
                : "\"<\\/script>\\/\"";

            Assert.Multiple(() =>
            {
                Assert.That(raw.Text(), Is.EqualTo(expectedRaw));
                Assert.That(escaped.Text(), Is.EqualTo(expectedEscaped));
            });
        }
    }

    [Test]
    public void HexadecimalCaseAppliesToControlsBmpScalarsAndSurrogates()
    {
        WriterFixture lowercase = new WriterFixture(
            JsonStringEscapeMode.AsciiOnly,
            JsonUnicodePolicy.SyntaxCompatible,
            false,
            false);
        WriterFixture uppercase = new WriterFixture(
            JsonStringEscapeMode.AsciiOnly,
            JsonUnicodePolicy.SyntaxCompatible,
            false,
            true);
        string value = new string(new char[]
        {
            '\u001A',
            '\u00AF',
            '\uD83D',
            '\uDE0A',
            '\uDABC'
        });

        AssertSuccess(lowercase.Writer.WriteString(value));
        AssertSuccess(lowercase.Writer.CompleteWriter());
        AssertSuccess(uppercase.Writer.WriteString(value));
        AssertSuccess(uppercase.Writer.CompleteWriter());

        Assert.Multiple(() =>
        {
            Assert.That(
                lowercase.Text(),
                Is.EqualTo(
                    "\"\\u001a\\u00af\\ud83d\\ude0a\\udabc\""));
            Assert.That(
                uppercase.Text(),
                Is.EqualTo(
                    "\"\\u001A\\u00AF\\uD83D\\uDE0A\\uDABC\""));
        });
    }

    [Test]
    public void SupplementaryScalarsRemainUtf8OutsideAsciiOnlyMode()
    {
        string value = "\U00010000😀\U0010ffff";
        WriterFixture minimal = new WriterFixture(
            JsonStringEscapeMode.Minimal,
            JsonUnicodePolicy.UnicodeScalar,
            false,
            false);
        WriterFixture html = new WriterFixture(
            JsonStringEscapeMode.HtmlSafe,
            JsonUnicodePolicy.UnicodeScalar,
            false,
            false);

        AssertSuccess(minimal.Writer.WriteString(value));
        AssertSuccess(minimal.Writer.CompleteWriter());
        AssertSuccess(html.Writer.WriteString(value));
        AssertSuccess(html.Writer.CompleteWriter());

        Assert.Multiple(() =>
        {
            Assert.That(minimal.Text(), Is.EqualTo("\"" + value + "\""));
            Assert.That(html.Text(), Is.EqualTo("\"" + value + "\""));
        });
    }

    [Test]
    public void SyntaxCompatibleModeEscapesEveryLoneSurrogatePosition()
    {
        WriterFixture fixture = new WriterFixture(
            JsonStringEscapeMode.Minimal,
            JsonUnicodePolicy.SyntaxCompatible,
            false,
            false);
        string value = new string(new char[]
        {
            '\uD800',
            'a',
            '\uDC00',
            '\uDC01',
            '\uD801',
            '\uD83D',
            '\uDE00',
            '\uDBFF'
        });

        AssertSuccess(fixture.Writer.WriteString(value));
        AssertSuccess(fixture.Writer.CompleteWriter());

        Assert.That(
            fixture.Text(),
            Is.EqualTo(
                "\"\\ud800a\\udc00\\udc01\\ud801😀\\udbff\""));
    }

    [Test]
    public void UnicodeScalarModeRejectsLoneSurrogatesWithoutChangingState()
    {
        WriterFixture valueFixture = new WriterFixture(
            JsonStringEscapeMode.Minimal,
            JsonUnicodePolicy.UnicodeScalar,
            false,
            false);
        string loneHigh = new string(new char[] { '\uD800' });
        string loneLow = new string(new char[] { '\uDC00' });

        JsonOperationResult highValue =
            valueFixture.Writer.WriteString(loneHigh);
        JsonOperationResult lowValue =
            valueFixture.Writer.WriteString(loneLow);
        AssertStringFailure(highValue);
        AssertStringFailure(lowValue);
        Assert.That(valueFixture.Writer.GetBytesPending(), Is.Zero);
        AssertSuccess(valueFixture.Writer.WriteString("valid"));
        AssertSuccess(valueFixture.Writer.CompleteWriter());

        WriterFixture nameFixture = new WriterFixture(
            JsonStringEscapeMode.Minimal,
            JsonUnicodePolicy.UnicodeScalar,
            false,
            false);
        AssertSuccess(nameFixture.Writer.WriteStartObject());
        JsonOperationResult highName =
            nameFixture.Writer.WritePropertyName(loneHigh);
        JsonOperationResult lowName =
            nameFixture.Writer.WritePropertyName(loneLow);
        AssertStringFailure(highName);
        AssertStringFailure(lowName);
        Assert.That(nameFixture.Writer.GetBytesPending(), Is.EqualTo(1));
        AssertSuccess(nameFixture.Writer.WriteEndObject());
        AssertSuccess(nameFixture.Writer.CompleteWriter());

        Assert.Multiple(() =>
        {
            Assert.That(valueFixture.Text(), Is.EqualTo("\"valid\""));
            Assert.That(nameFixture.Text(), Is.EqualTo("{}"));
        });
    }

    static void WriteSingleMember(WriterFixture fixture, string value)
    {
        AssertSuccess(fixture.Writer.WriteStartObject());
        AssertSuccess(fixture.Writer.WritePropertyName(value));
        AssertSuccess(fixture.Writer.WriteString(value));
        AssertSuccess(fixture.Writer.WriteEndObject());
        AssertSuccess(fixture.Writer.CompleteWriter());
    }

    static void AssertStringFailure(JsonOperationResult result)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(
                result.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidSurrogatePair));
            Assert.That(result.BytesWritten, Is.Zero);
        });
    }

    static void AssertSuccess(JsonOperationResult result)
    {
        Assert.That(result.Success, Is.True, result.Diagnostic?.Message);
    }

    sealed class WriterFixture
    {
        internal System.Buffers.ArrayBufferWriter<byte> Destination;
        internal IJsonTokenWriter Writer;

        internal WriterFixture(
            JsonStringEscapeMode escapeMode,
            JsonUnicodePolicy unicodePolicy,
            bool escapeSolidus,
            bool uppercaseHexadecimal)
        {
            Destination = new System.Buffers.ArrayBufferWriter<byte>();
            JsonWriterOptions options = new JsonWriterOptions();
            options.StringEscapeMode = escapeMode;
            options.UnicodePolicy = unicodePolicy;
            options.EscapeSolidus = escapeSolidus;
            options.UseUppercaseHexadecimalEscapes = uppercaseHexadecimal;
            JsonWriterTarget target = new JsonWriterTarget();
            target.BufferWriter = Destination;
            Writer = new JsonUtf8Writer(options, target);
        }

        internal string Text()
        {
            return System.Text.Encoding.UTF8.GetString(
                Destination.WrittenSpan);
        }
    }
}
