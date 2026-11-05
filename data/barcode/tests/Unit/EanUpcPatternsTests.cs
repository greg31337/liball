namespace barcode.Tests;

public class EanUpcPatternsTests
{
    const string ExpectedLModules =
        "0001101 0011001 0010011 0111101 0100011 0110001 0101111 0111011 0110111 0001011";

    const string ExpectedGModules =
        "0100111 0110011 0011011 0100001 0011101 0111001 0000101 0010001 0001001 0010111";

    const string ExpectedRModules =
        "1110010 1100110 1101100 1000010 1011100 1001110 1010000 1000100 1001000 1110100";

    const string ExpectedLWidths = "3211 2221 2122 1411 1132 1231 1114 1312 1213 3112";
    const string ExpectedGWidths = "1123 1222 2212 1141 2311 1321 4111 2131 3121 2113";
    const string ExpectedRWidths = "3211 2221 2122 1411 1132 1231 1114 1312 1213 3112";

    [Test]
    public void CompleteDigitTablesMatchIndependentModuleOracle()
    {
        VerifyModuleTable(EanUpcDigitSet.L, ExpectedLModules);
        VerifyModuleTable(EanUpcDigitSet.G, ExpectedGModules);
        VerifyModuleTable(EanUpcDigitSet.R, ExpectedRModules);
    }

    [Test]
    public void CompleteDigitTablesMatchIndependentElementWidthOracle()
    {
        VerifyWidthTable(EanUpcDigitSet.L, ExpectedLWidths);
        VerifyWidthTable(EanUpcDigitSet.G, ExpectedGWidths);
        VerifyWidthTable(EanUpcDigitSet.R, ExpectedRWidths);
    }

    [Test]
    public void DigitTablesHaveRequiredParityOrientationAndRelationships()
    {
        HashSet<string> lPatterns = new HashSet<string>();
        HashSet<string> gPatterns = new HashSet<string>();
        HashSet<string> rPatterns = new HashSet<string>();
        int digit = 0;
        while (digit < EanUpcPatterns.DigitCount)
        {
            byte[] l = EanUpcPatterns.GetDigitModules(digit, EanUpcDigitSet.L);
            byte[] g = EanUpcPatterns.GetDigitModules(digit, EanUpcDigitSet.G);
            byte[] r = EanUpcPatterns.GetDigitModules(digit, EanUpcDigitSet.R);
            string lText = ToModuleString(l);
            string gText = ToModuleString(g);
            string rText = ToModuleString(r);

            Assert.Multiple(() =>
            {
                Assert.That(l, Has.Length.EqualTo(EanUpcPatterns.DigitModuleCount));
                Assert.That(g, Has.Length.EqualTo(EanUpcPatterns.DigitModuleCount));
                Assert.That(r, Has.Length.EqualTo(EanUpcPatterns.DigitModuleCount));
                Assert.That(CountDarkModules(l) % 2, Is.EqualTo(1), $"L digit {digit}");
                Assert.That(CountDarkModules(g) % 2, Is.Zero, $"G digit {digit}");
                Assert.That(CountDarkModules(r) % 2, Is.Zero, $"R digit {digit}");
                Assert.That(l[0], Is.Zero);
                Assert.That(g[0], Is.Zero);
                Assert.That(r[0], Is.EqualTo(1));
                Assert.That(Complement(lText), Is.EqualTo(rText));
                Assert.That(Reverse(gText), Is.EqualTo(rText));
                Assert.That(lPatterns.Add(lText), Is.True);
                Assert.That(gPatterns.Add(gText), Is.True);
                Assert.That(rPatterns.Add(rText), Is.True);
            });
            digit = digit + 1;
        }
    }

    [TestCase(0, "101")]
    [TestCase(1, "01010")]
    [TestCase(2, "010101")]
    [TestCase(3, "1011")]
    [TestCase(4, "01")]
    public void GuardTableMatchesIndependentModuleOracle(int guardValue, string expected)
    {
        EanUpcGuardPattern guardPattern = (EanUpcGuardPattern)guardValue;
        byte[] modules = EanUpcPatterns.GetGuardModules(guardPattern);

        Assert.That(ToModuleString(modules), Is.EqualTo(expected));
    }

    [Test]
    public void Ean13AndUpcEParityTablesMatchIndependentOracle()
    {
        string[] expectedEan13 =
            "LLLLLL LLGLGG LLGGLG LLGGGL LGLLGG LGGLLG LGGGLL LGLGLG LGLGGL LGGLGL".Split(' ');
        string[] expectedUpcEZero =
            "GGGLLL GGLGLL GGLLGL GGLLLG GLGGLL GLLGGL GLLLGG GLGLGL GLGLLG GLLGLG".Split(' ');
        string[] expectedUpcEOne =
            "LLLGGG LLGLGG LLGGLG LLGGGL LGLLGG LGGLLG LGGGLL LGLGLG LGLGGL LGGLGL".Split(' ');

        int digit = 0;
        while (digit < EanUpcPatterns.DigitCount)
        {
            Assert.Multiple(() =>
            {
                Assert.That(ToParityString(EanUpcPatterns.GetEan13Parity(digit)), Is.EqualTo(expectedEan13[digit]), $"EAN-13 leading digit {digit}");
                Assert.That(ToParityString(EanUpcPatterns.GetUpcEParity(0, digit)), Is.EqualTo(expectedUpcEZero[digit]), $"UPC-E number system 0 check digit {digit}");
                Assert.That(ToParityString(EanUpcPatterns.GetUpcEParity(1, digit)), Is.EqualTo(expectedUpcEOne[digit]), $"UPC-E number system 1 check digit {digit}");
            });
            digit = digit + 1;
        }
    }

    [Test]
    public void AddonParityTablesMatchIndependentOracle()
    {
        string[] expectedAddon2 = "LL LG GL GG".Split(' ');
        string[] expectedAddon5 = "GGLLL GLGLL GLLGL GLLLG LGGLL LLGGL LLLGG LGLGL LGLLG LLGLG".Split(' ');

        int remainder = 0;
        while (remainder < expectedAddon2.Length)
        {
            Assert.That(
                ToParityString(EanUpcPatterns.GetAddon2Parity(remainder)),
                Is.EqualTo(expectedAddon2[remainder]),
                $"Two-digit add-on remainder {remainder}");
            remainder = remainder + 1;
        }

        int checksum = 0;
        while (checksum < expectedAddon5.Length)
        {
            Assert.That(
                ToParityString(EanUpcPatterns.GetAddon5Parity(checksum)),
                Is.EqualTo(expectedAddon5[checksum]),
                $"Five-digit add-on checksum {checksum}");
            checksum = checksum + 1;
        }
    }

    [Test]
    public void ReturnedTablesAreIndependentAndInvalidIndexesAreRejected()
    {
        byte[] digit = EanUpcPatterns.GetDigitModules(0, EanUpcDigitSet.L);
        byte[] guard = EanUpcPatterns.GetGuardModules(EanUpcGuardPattern.Normal);
        EanUpcDigitSet[] parity = EanUpcPatterns.GetEan13Parity(0);
        digit[0] = 1;
        guard[0] = 0;
        parity[0] = EanUpcDigitSet.G;

        Assert.Multiple(() =>
        {
            Assert.That(ToModuleString(EanUpcPatterns.GetDigitModules(0, EanUpcDigitSet.L)), Is.EqualTo("0001101"));
            Assert.That(ToModuleString(EanUpcPatterns.GetGuardModules(EanUpcGuardPattern.Normal)), Is.EqualTo("101"));
            Assert.That(ToParityString(EanUpcPatterns.GetEan13Parity(0)), Is.EqualTo("LLLLLL"));
            Assert.That(() => EanUpcPatterns.GetDigitModules(-1, EanUpcDigitSet.L), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => EanUpcPatterns.GetDigitModules(10, EanUpcDigitSet.L), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => EanUpcPatterns.GetDigitModules(0, (EanUpcDigitSet)99), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => EanUpcPatterns.GetDigitWidths(0, (EanUpcDigitSet)99), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => EanUpcPatterns.GetGuardModules((EanUpcGuardPattern)99), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => EanUpcPatterns.GetEan13Parity(10), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => EanUpcPatterns.GetUpcEParity(-1, 0), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => EanUpcPatterns.GetUpcEParity(2, 0), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => EanUpcPatterns.GetUpcEParity(0, -1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => EanUpcPatterns.GetAddon2Parity(-1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => EanUpcPatterns.GetAddon2Parity(4), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => EanUpcPatterns.GetAddon5Parity(10), Throws.TypeOf<BarcodeValidationException>());
        });
    }

    static void VerifyModuleTable(EanUpcDigitSet digitSet, string oracle)
    {
        string[] expected = oracle.Split(' ');
        Assert.That(expected, Has.Length.EqualTo(EanUpcPatterns.DigitCount));
        int digit = 0;
        while (digit < expected.Length)
        {
            Assert.That(
                ToModuleString(EanUpcPatterns.GetDigitModules(digit, digitSet)),
                Is.EqualTo(expected[digit]),
                $"Set {digitSet}, digit {digit}");
            digit = digit + 1;
        }
    }

    static void VerifyWidthTable(EanUpcDigitSet digitSet, string oracle)
    {
        string[] expected = oracle.Split(' ');
        Assert.That(expected, Has.Length.EqualTo(EanUpcPatterns.DigitCount));
        int digit = 0;
        while (digit < expected.Length)
        {
            Assert.That(
                ToWidthString(EanUpcPatterns.GetDigitWidths(digit, digitSet)),
                Is.EqualTo(expected[digit]),
                $"Set {digitSet}, digit {digit}");
            digit = digit + 1;
        }
    }

    static string ToModuleString(byte[] modules)
    {
        char[] characters = new char[modules.Length];
        int index = 0;
        while (index < modules.Length)
        {
            characters[index] = modules[index] == 0 ? '0' : '1';
            index = index + 1;
        }

        return new string(characters);
    }

    static string ToWidthString(int[] widths)
    {
        char[] characters = new char[widths.Length];
        int index = 0;
        while (index < widths.Length)
        {
            characters[index] = (char)('0' + widths[index]);
            index = index + 1;
        }

        return new string(characters);
    }

    static string ToParityString(EanUpcDigitSet[] parity)
    {
        char[] characters = new char[parity.Length];
        int index = 0;
        while (index < parity.Length)
        {
            characters[index] = parity[index] == EanUpcDigitSet.L ? 'L' : 'G';
            index = index + 1;
        }

        return new string(characters);
    }

    static int CountDarkModules(byte[] modules)
    {
        int count = 0;
        int index = 0;
        while (index < modules.Length)
        {
            count = count + modules[index];
            index = index + 1;
        }

        return count;
    }

    static string Complement(string value)
    {
        char[] characters = new char[value.Length];
        int index = 0;
        while (index < value.Length)
        {
            characters[index] = value[index] == '0' ? '1' : '0';
            index = index + 1;
        }

        return new string(characters);
    }

    static string Reverse(string value)
    {
        char[] characters = new char[value.Length];
        int index = 0;
        while (index < value.Length)
        {
            characters[index] = value[value.Length - index - 1];
            index = index + 1;
        }

        return new string(characters);
    }
}
