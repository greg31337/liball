namespace barcode.Tests;

public class Code128PatternsTests
{
    const string ExpectedModulePatterns =
        "11011001100 11001101100 11001100110 10010011000 10010001100 10001001100 " +
        "10011001000 10011000100 10001100100 11001001000 11001000100 11000100100 " +
        "10110011100 10011011100 10011001110 10111001100 10011101100 10011100110 " +
        "11001110010 11001011100 11001001110 11011100100 11001110100 11101101110 " +
        "11101001100 11100101100 11100100110 11101100100 11100110100 11100110010 " +
        "11011011000 11011000110 11000110110 10100011000 10001011000 10001000110 " +
        "10110001000 10001101000 10001100010 11010001000 11000101000 11000100010 " +
        "10110111000 10110001110 10001101110 10111011000 10111000110 10001110110 " +
        "11101110110 11010001110 11000101110 11011101000 11011100010 11011101110 " +
        "11101011000 11101000110 11100010110 11101101000 11101100010 11100011010 " +
        "11101111010 11001000010 11110001010 10100110000 10100001100 10010110000 " +
        "10010000110 10000101100 10000100110 10110010000 10110000100 10011010000 " +
        "10011000010 10000110100 10000110010 11000010010 11001010000 11110111010 " +
        "11000010100 10001111010 10100111100 10010111100 10010011110 10111100100 " +
        "10011110100 10011110010 11110100100 11110010100 11110010010 11011011110 " +
        "11011110110 11110110110 10101111000 10100011110 10001011110 10111101000 " +
        "10111100010 11110101000 11110100010 10111011110 10111101110 11101011110 " +
        "11110101110 11010000100 11010010000 11010011100 1100011101011";

    [Test]
    public void CompletePatternTableMatchesIndependentModuleOracle()
    {
        string[] expected = ExpectedModulePatterns.Split(' ');

        Assert.That(expected, Has.Length.EqualTo(Code128Patterns.SymbolCharacterCount));
        int codeword = 0;
        while (codeword < expected.Length)
        {
            int[] widths = Code128Patterns.GetPatternWidths(codeword);
            string actual = ExpandWidths(widths);
            Assert.That(actual, Is.EqualTo(expected[codeword]), $"Codeword {codeword}");
            codeword = codeword + 1;
        }
    }

    [Test]
    public void EveryPatternHasStandardStructureParityAndUniqueEncoding()
    {
        HashSet<string> observedPatterns = new HashSet<string>();
        int codeword = 0;
        while (codeword < Code128Patterns.SymbolCharacterCount)
        {
            int[] widths = Code128Patterns.GetPatternWidths(codeword);
            int barModules = 0;
            int spaceModules = 0;
            int index = 0;
            while (index < widths.Length)
            {
                Assert.That(widths[index], Is.InRange(1, 4), $"Codeword {codeword}, element {index}");
                if (index % 2 == 0)
                {
                    barModules = barModules + widths[index];
                }
                else
                {
                    spaceModules = spaceModules + widths[index];
                }

                index = index + 1;
            }

            Assert.Multiple(() =>
            {
                Assert.That(widths, Has.Length.EqualTo(Code128Patterns.GetElementCount(codeword)));
                Assert.That(barModules + spaceModules, Is.EqualTo(Code128Patterns.GetModuleCount(codeword)));
                Assert.That(barModules % 2, Is.Zero, $"Codeword {codeword} bar parity");
                Assert.That(spaceModules % 2, Is.EqualTo(1), $"Codeword {codeword} space parity");
                Assert.That(observedPatterns.Add(ExpandWidths(widths)), Is.True, $"Duplicate codeword {codeword}");
            });
            codeword = codeword + 1;
        }
    }

    [TestCase(0, Code128CodeSet.A, 0)]
    [TestCase(95, Code128CodeSet.A, 0)]
    [TestCase(96, Code128CodeSet.A, 3)]
    [TestCase(97, Code128CodeSet.A, 2)]
    [TestCase(98, Code128CodeSet.A, 5)]
    [TestCase(99, Code128CodeSet.A, 8)]
    [TestCase(100, Code128CodeSet.A, 7)]
    [TestCase(101, Code128CodeSet.A, 4)]
    [TestCase(102, Code128CodeSet.A, 1)]
    [TestCase(0, Code128CodeSet.B, 0)]
    [TestCase(95, Code128CodeSet.B, 0)]
    [TestCase(96, Code128CodeSet.B, 3)]
    [TestCase(97, Code128CodeSet.B, 2)]
    [TestCase(98, Code128CodeSet.B, 5)]
    [TestCase(99, Code128CodeSet.B, 8)]
    [TestCase(100, Code128CodeSet.B, 4)]
    [TestCase(101, Code128CodeSet.B, 6)]
    [TestCase(102, Code128CodeSet.B, 1)]
    [TestCase(0, Code128CodeSet.C, 0)]
    [TestCase(95, Code128CodeSet.C, 0)]
    [TestCase(96, Code128CodeSet.C, 0)]
    [TestCase(97, Code128CodeSet.C, 0)]
    [TestCase(98, Code128CodeSet.C, 0)]
    [TestCase(99, Code128CodeSet.C, 0)]
    [TestCase(100, Code128CodeSet.C, 7)]
    [TestCase(101, Code128CodeSet.C, 6)]
    [TestCase(102, Code128CodeSet.C, 1)]
    public void FunctionTableUsesActiveCodeSet(int codeword, Code128CodeSet codeSet, int expected)
    {
        Assert.That((int)Code128Patterns.GetCodewordRole(codeword, codeSet), Is.EqualTo(expected));
    }

    [TestCase(103, 9)]
    [TestCase(104, 10)]
    [TestCase(105, 11)]
    [TestCase(106, 12)]
    public void StartAndStopRolesDoNotDependOnAnActiveCodeSet(int codeword, int expected)
    {
        Assert.That((int)Code128Patterns.GetCodewordRole(codeword, Code128CodeSet.Automatic), Is.EqualTo(expected));
    }

    [Test]
    public void StartCodewordMappingsAreBidirectional()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Code128Patterns.GetStartCodeword(Code128CodeSet.A), Is.EqualTo(103));
            Assert.That(Code128Patterns.GetStartCodeword(Code128CodeSet.B), Is.EqualTo(104));
            Assert.That(Code128Patterns.GetStartCodeword(Code128CodeSet.C), Is.EqualTo(105));
            Assert.That(Code128Patterns.GetCodeSetForStart(103), Is.EqualTo(Code128CodeSet.A));
            Assert.That(Code128Patterns.GetCodeSetForStart(104), Is.EqualTo(Code128CodeSet.B));
            Assert.That(Code128Patterns.GetCodeSetForStart(105), Is.EqualTo(Code128CodeSet.C));
        });
    }

    [Test]
    public void ContextDependentFunctionConstantsMatchTheCharacterTable()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Code128Patterns.Function3Codeword, Is.EqualTo(96));
            Assert.That(Code128Patterns.Function2Codeword, Is.EqualTo(97));
            Assert.That(Code128Patterns.ShiftCodeword, Is.EqualTo(98));
            Assert.That(Code128Patterns.CodeCCodeword, Is.EqualTo(99));
            Assert.That(Code128Patterns.CodeBCodeword, Is.EqualTo(100));
            Assert.That(Code128Patterns.Function4InCodeSetBCodeword, Is.EqualTo(100));
            Assert.That(Code128Patterns.CodeACodeword, Is.EqualTo(101));
            Assert.That(Code128Patterns.Function4InCodeSetACodeword, Is.EqualTo(101));
            Assert.That(Code128Patterns.Function1Codeword, Is.EqualTo(102));
        });
    }

    [Test]
    public void ReturnedPatternsCannotMutateTheCanonicalTable()
    {
        int[] first = Code128Patterns.GetPatternWidths(35);
        first[0] = 4;

        Assert.That(Code128Patterns.GetPatternWidths(35), Is.EqualTo(new int[] { 1, 3, 1, 3, 2, 1 }));
    }

    [Test]
    public void TableRejectsInvalidCodewordsAndMissingCodeSetContext()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => Code128Patterns.GetPatternWidths(-1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128Patterns.GetPatternWidths(107), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128Patterns.GetElementCount(107), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128Patterns.GetModuleCount(-1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128Patterns.GetCodewordRole(0, Code128CodeSet.Automatic), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128Patterns.GetCodewordRole(0, (Code128CodeSet)99), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128Patterns.GetStartCodeword(Code128CodeSet.Automatic), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128Patterns.GetCodeSetForStart(102), Throws.TypeOf<BarcodeValidationException>());
        });
    }

    static string ExpandWidths(int[] widths)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        bool dark = true;
        int index = 0;
        while (index < widths.Length)
        {
            int module = 0;
            while (module < widths[index])
            {
                if (dark)
                {
                    builder.Append('1');
                }
                else
                {
                    builder.Append('0');
                }

                module = module + 1;
            }

            dark = !dark;
            index = index + 1;
        }

        return builder.ToString();
    }
}
