namespace barcode.Tests;

public class Pdf417TextCompactionTests
{
    [Test]
    public void AlphaAndMixedReferenceVectorPacksPairsInBaseThirty()
    {
        Pdf417TextEncodingResult encoded = Pdf417TextCompaction.EncodeText(
            "PDF417",
            Pdf417TextSubmode.Alpha);
        Pdf417TextDecodingResult decoded = Pdf417TextCompaction.DecodeCodewords(
            encoded.Codewords,
            Pdf417TextSubmode.Alpha);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Values, Is.EqualTo(new int[] { 15, 3, 5, 28, 4, 1, 7 }));
            Assert.That(encoded.Codewords, Is.EqualTo(new int[] { 453, 178, 121, 239 }));
            Assert.That(encoded.InitialSubmode, Is.EqualTo(Pdf417TextSubmode.Alpha));
            Assert.That(encoded.FinalSubmode, Is.EqualTo(Pdf417TextSubmode.Mixed));
            Assert.That(decoded.Text, Is.EqualTo("PDF417"));
            Assert.That(decoded.Values, Is.EqualTo(new int[] { 15, 3, 5, 28, 4, 1, 7, 29 }));
            Assert.That(decoded.FinalSubmode, Is.EqualTo(Pdf417TextSubmode.Mixed));
        });
    }

    [Test]
    public void LatchesAndShiftsTraverseEverySubmodeWithStableValues()
    {
        const string text = "ABCabc123;<>XYZ";
        int[] expectedValues = new int[]
        {
            0, 1, 2, 27, 0, 1, 2, 28, 1, 2, 3, 25, 0, 1, 2, 29, 23, 24, 25
        };
        int[] expectedCodewords = new int[]
        {
            1, 87, 1, 88, 32, 115, 1, 89, 714, 779
        };

        Pdf417TextEncodingResult encoded = Pdf417TextCompaction.EncodeText(
            text,
            Pdf417TextSubmode.Alpha);
        Pdf417TextDecodingResult decodedValues = Pdf417TextCompaction.DecodeValues(
            expectedValues,
            Pdf417TextSubmode.Alpha);
        Pdf417TextDecodingResult decodedCodewords = Pdf417TextCompaction.DecodeCodewords(
            expectedCodewords,
            Pdf417TextSubmode.Alpha);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Values, Is.EqualTo(expectedValues));
            Assert.That(encoded.Codewords, Is.EqualTo(expectedCodewords));
            Assert.That(encoded.FinalSubmode, Is.EqualTo(Pdf417TextSubmode.Alpha));
            Assert.That(decodedValues.Text, Is.EqualTo(text));
            Assert.That(decodedValues.FinalSubmode, Is.EqualTo(Pdf417TextSubmode.Alpha));
            Assert.That(decodedCodewords.Text, Is.EqualTo(text));
            Assert.That(decodedCodewords.FinalSubmode, Is.EqualTo(Pdf417TextSubmode.Alpha));
        });
    }

    [Test]
    public void LowerAlphaShiftAndSinglePunctuationShiftRestoreTheLatchedSubmode()
    {
        Pdf417TextEncodingResult lower = Pdf417TextCompaction.EncodeText(
            "aBc",
            Pdf417TextSubmode.Alpha);
        Pdf417TextEncodingResult punctuation = Pdf417TextCompaction.EncodeText(
            "A;B",
            Pdf417TextSubmode.Alpha);
        Pdf417TextDecodingResult lowerDecoded = Pdf417TextCompaction.DecodeCodewords(
            lower.Codewords,
            Pdf417TextSubmode.Alpha);
        Pdf417TextDecodingResult punctuationDecoded = Pdf417TextCompaction.DecodeCodewords(
            punctuation.Codewords,
            Pdf417TextSubmode.Alpha);

        Assert.Multiple(() =>
        {
            Assert.That(lower.Values, Is.EqualTo(new int[] { 27, 0, 27, 1, 2 }));
            Assert.That(lower.Codewords, Is.EqualTo(new int[] { 810, 811, 89 }));
            Assert.That(lower.FinalSubmode, Is.EqualTo(Pdf417TextSubmode.Lower));
            Assert.That(lowerDecoded.Text, Is.EqualTo("aBc"));
            Assert.That(lowerDecoded.FinalSubmode, Is.EqualTo(Pdf417TextSubmode.Lower));
            Assert.That(punctuation.Values, Is.EqualTo(new int[] { 0, 29, 0, 1 }));
            Assert.That(punctuation.Codewords, Is.EqualTo(new int[] { 29, 1 }));
            Assert.That(punctuationDecoded.Text, Is.EqualTo("A;B"));
            Assert.That(punctuationDecoded.FinalSubmode, Is.EqualTo(Pdf417TextSubmode.Alpha));
        });
    }

    [Test]
    public void ExplicitInitialSubmodesPreserveTheirDefinedTransitions()
    {
        Pdf417TextEncodingResult lower = Pdf417TextCompaction.EncodeText(
            "aA b",
            Pdf417TextSubmode.Lower);
        Pdf417TextEncodingResult mixed = Pdf417TextCompaction.EncodeText(
            "12 A",
            Pdf417TextSubmode.Mixed);
        Pdf417TextEncodingResult punctuation = Pdf417TextCompaction.EncodeText(
            ";A",
            Pdf417TextSubmode.Punctuation);

        Assert.Multiple(() =>
        {
            Assert.That(lower.Values, Is.EqualTo(new int[] { 0, 27, 0, 26, 1 }));
            Assert.That(lower.FinalSubmode, Is.EqualTo(Pdf417TextSubmode.Lower));
            Assert.That(Decode(lower), Is.EqualTo("aA b"));
            Assert.That(mixed.Values, Is.EqualTo(new int[] { 1, 2, 26, 28, 0 }));
            Assert.That(mixed.FinalSubmode, Is.EqualTo(Pdf417TextSubmode.Alpha));
            Assert.That(Decode(mixed), Is.EqualTo("12 A"));
            Assert.That(punctuation.Values, Is.EqualTo(new int[] { 0, 29, 0 }));
            Assert.That(punctuation.FinalSubmode, Is.EqualTo(Pdf417TextSubmode.Alpha));
            Assert.That(Decode(punctuation), Is.EqualTo(";A"));
        });
    }

    [Test]
    public void EverySupportedTextCharacterPairRoundTripsFromEveryInitialSubmode()
    {
        string supported = CreateSupportedTextCharacters();
        Pdf417TextSubmode[] submodes = new Pdf417TextSubmode[]
        {
            Pdf417TextSubmode.Alpha,
            Pdf417TextSubmode.Lower,
            Pdf417TextSubmode.Mixed,
            Pdf417TextSubmode.Punctuation
        };
        int submodeIndex = 0;
        while (submodeIndex < submodes.Length)
        {
            int first = 0;
            while (first < supported.Length)
            {
                int second = 0;
                while (second < supported.Length)
                {
                    string text = new string(new char[] { supported[first], supported[second] });
                    Pdf417TextEncodingResult encoded = Pdf417TextCompaction.EncodeText(
                        text,
                        submodes[submodeIndex]);
                    Pdf417TextDecodingResult decoded = Pdf417TextCompaction.DecodeCodewords(
                        encoded.Codewords,
                        submodes[submodeIndex]);
                    Assert.That(decoded.Text, Is.EqualTo(text));
                    second++;
                }

                first++;
            }

            submodeIndex++;
        }
    }

    [Test]
    public void CharacterTablesContainTheExactMixedAndPunctuationAlphabets()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Pdf417TextCompaction.MixedCharacters, Is.EqualTo("0123456789&\r\t,:#-.$/+%*=^"));
            Assert.That(Pdf417TextCompaction.MixedCharacters, Has.Length.EqualTo(25));
            Assert.That(Pdf417TextCompaction.PunctuationCharacters, Is.EqualTo(";<>@[\\]_`~!\r\t,:\n-.$/\"|*()?{}'"));
            Assert.That(Pdf417TextCompaction.PunctuationCharacters, Has.Length.EqualTo(29));
            Assert.That(Pdf417TextCompaction.TextLatchCodeword, Is.EqualTo(900));
            Assert.That(Pdf417TextCompaction.ValueBase, Is.EqualTo(30));
            Assert.That(Pdf417TextCompaction.PaddingValue, Is.EqualTo(29));
        });
    }

    [Test]
    public void ResultsOwnBuffersAndEmptyTextProducesNoCodewords()
    {
        Pdf417TextEncodingResult encoded = Pdf417TextCompaction.EncodeText(
            "AB",
            Pdf417TextSubmode.Alpha);
        Pdf417TextDecodingResult decoded = Pdf417TextCompaction.DecodeValues(
            encoded.Values,
            Pdf417TextSubmode.Alpha);
        Pdf417TextEncodingResult empty = Pdf417TextCompaction.EncodeText(
            "",
            Pdf417TextSubmode.Mixed);
        encoded.Values[0] = 29;
        decoded.Values[0] = 29;

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Codewords, Is.EqualTo(new int[] { 1 }));
            Assert.That(decoded.Text, Is.EqualTo("AB"));
            Assert.That(empty.Values, Is.Empty);
            Assert.That(empty.Codewords, Is.Empty);
            Assert.That(empty.FinalSubmode, Is.EqualTo(Pdf417TextSubmode.Mixed));
            Assert.That(Pdf417TextCompaction.DecodeCodewords(empty.Codewords, Pdf417TextSubmode.Mixed).Text, Is.Empty);
        });
    }

    [Test]
    public void InvalidTextCodewordsValuesAndSubmodesAreRejected()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => Pdf417TextCompaction.EncodeText(null!, Pdf417TextSubmode.Alpha), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Pdf417TextCompaction.EncodeText("A\0B", Pdf417TextSubmode.Alpha), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Pdf417TextCompaction.EncodeText("caf\u00e9", Pdf417TextSubmode.Alpha), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Pdf417TextCompaction.EncodeText("A", (Pdf417TextSubmode)99), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Pdf417TextCompaction.DecodeCodewords(null!, Pdf417TextSubmode.Alpha), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => Pdf417TextCompaction.DecodeCodewords(new int[] { -1 }, Pdf417TextSubmode.Alpha), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => Pdf417TextCompaction.DecodeCodewords(new int[] { 900 }, Pdf417TextSubmode.Alpha), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => Pdf417TextCompaction.DecodeValues(null!, Pdf417TextSubmode.Alpha), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => Pdf417TextCompaction.DecodeValues(new int[] { -1 }, Pdf417TextSubmode.Alpha), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => Pdf417TextCompaction.DecodeValues(new int[] { 30 }, Pdf417TextSubmode.Alpha), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => Pdf417TextCompaction.DecodeValues(new int[0], (Pdf417TextSubmode)99), Throws.TypeOf<BarcodeDecodingException>());
        });
    }

    static string Decode(Pdf417TextEncodingResult encoded)
    {
        return Pdf417TextCompaction.DecodeCodewords(
            encoded.Codewords,
            encoded.InitialSubmode).Text;
    }

    static string CreateSupportedTextCharacters()
    {
        char[] characters = new char[98];
        characters[0] = '\t';
        characters[1] = '\n';
        characters[2] = '\r';
        int value = 32;
        while (value <= 126)
        {
            characters[value - 29] = (char)value;
            value++;
        }

        return new string(characters);
    }
}
