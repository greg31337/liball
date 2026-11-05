namespace barcode.Tests;

public class Code128EncoderTests
{
    [Test]
    public void EncodeBytesAssemblesStartDataChecksumAndStop()
    {
        Code128EncodedData encoded = Code128Encoder.EncodeBytes(new byte[] { 65, 66, 67 }, null);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.StartCodeSet, Is.EqualTo(Code128CodeSet.B));
            Assert.That(encoded.Codewords, Is.EqualTo(new int[] { 104, 33, 34, 35, 1, 106 }));
            Assert.That(encoded.ChecksumValue, Is.EqualTo(1));
            Assert.That(encoded.ElementWidths, Has.Length.EqualTo(37));
            Assert.That(encoded.Modules, Has.Length.EqualTo(68));
            Assert.That(ToModuleString(encoded.Modules), Is.EqualTo(
                "11010010000" +
                "10100011000" +
                "10001011000" +
                "10001000110" +
                "11001101100" +
                "1100011101011"));
        });
    }

    [Test]
    public void AssemblePlanMatchesKnownWeightedChecksumVector()
    {
        Code128Plan plan = CreatePlan(
            Code128CodeSet.B,
            new int[] { 48, 42, 42, 17, 18, 19, 35 });

        Code128EncodedData encoded = Code128Encoder.AssemblePlan(plan);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Codewords, Is.EqualTo(new int[] { 104, 48, 42, 42, 17, 18, 19, 35, 55, 106 }));
            Assert.That(encoded.ChecksumValue, Is.EqualTo(55));
            Assert.That(encoded.ElementWidths[..6], Is.EqualTo(new int[] { 2, 1, 1, 2, 1, 4 }));
            Assert.That(encoded.ElementWidths[^7..], Is.EqualTo(new int[] { 2, 3, 3, 1, 1, 1, 2 }));
            Assert.That(ToModuleString(encoded.Modules[^13..]), Is.EqualTo("1100011101011"));
            Assert.That(encoded.Modules[^1], Is.EqualTo(1));
        });
    }

    [Test]
    public void EncodeTokensPreservesFunctionCodewordAndNumericCompaction()
    {
        Code128Options options = new Code128Options();
        options.AllowFnc1 = true;
        int[] tokens = new int[] { Code128Planner.Function1Token, '1', '2' };

        Code128EncodedData encoded = Code128Encoder.EncodeTokens(tokens, options);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.StartCodeSet, Is.EqualTo(Code128CodeSet.C));
            Assert.That(encoded.Codewords, Is.EqualTo(new int[] { 105, 102, 12, 25, 106 }));
            Assert.That(encoded.ChecksumValue, Is.EqualTo(25));
            Assert.That(encoded.Modules, Has.Length.EqualTo(57));
        });
    }

    [Test]
    public void CapacityAcceptsMaximumPlanAndRejectsOneMoreCodeword()
    {
        int[] maximumCodewords = new int[Code128Encoder.MaximumPlannedCodewordCount];
        Code128Plan maximum = CreatePlan(Code128CodeSet.B, maximumCodewords);
        Code128Plan excessive = CreatePlan(
            Code128CodeSet.B,
            new int[Code128Encoder.MaximumPlannedCodewordCount + 1]);

        Code128EncodedData encoded = Code128Encoder.AssemblePlan(maximum);

        Assert.Multiple(() =>
        {
            Assert.That(Code128Encoder.MaximumPreCheckCodewordCount, Is.EqualTo(102));
            Assert.That(encoded.Codewords, Has.Length.EqualTo(104));
            Assert.That(encoded.ElementWidths, Has.Length.EqualTo(625));
            Assert.That(encoded.Modules, Has.Length.EqualTo(1146));
            Assert.That(
                () => Code128Encoder.AssemblePlan(excessive),
                Throws.TypeOf<BarcodeEncodingException>()
                    .With.Message.EqualTo("Code 128 plan exceeds the maximum symbol-character capacity."));
        });
    }

    [Test]
    public void AssemblyRejectsMalformedPlans()
    {
        Code128Plan automatic = CreatePlan(Code128CodeSet.Automatic, new int[] { 1 });
        Code128Plan invalidSet = CreatePlan((Code128CodeSet)99, new int[] { 1 });
        Code128Plan empty = CreatePlan(Code128CodeSet.B, new int[0]);
        Code128Plan negative = CreatePlan(Code128CodeSet.B, new int[] { -1 });
        Code128Plan startInData = CreatePlan(Code128CodeSet.B, new int[] { 103 });
        Code128Plan missingCodewords = new Code128Plan();
        missingCodewords.StartCodeSet = Code128CodeSet.B;
        missingCodewords.Codewords = null!;

        Assert.Multiple(() =>
        {
            Assert.That(() => Code128Encoder.AssemblePlan(null!), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128Encoder.AssemblePlan(automatic), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128Encoder.AssemblePlan(invalidSet), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128Encoder.AssemblePlan(empty), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128Encoder.AssemblePlan(negative), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128Encoder.AssemblePlan(startInData), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128Encoder.AssemblePlan(missingCodewords), Throws.TypeOf<BarcodeValidationException>());
        });
    }

    [Test]
    public void AssemblyDoesNotAliasThePlannedCodewords()
    {
        Code128Plan plan = CreatePlan(Code128CodeSet.C, new int[] { 12, 34, 56 });

        Code128EncodedData encoded = Code128Encoder.AssemblePlan(plan);
        plan.Codewords[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Codewords, Is.EqualTo(new int[] { 105, 12, 34, 56, 44, 106 }));
            Assert.That(encoded.ChecksumValue, Is.EqualTo(44));
            Assert.That(encoded.Codewords, Is.Not.SameAs(plan.Codewords));
        });
    }

    static Code128Plan CreatePlan(Code128CodeSet codeSet, int[] codewords)
    {
        Code128Plan plan = new Code128Plan();
        plan.StartCodeSet = codeSet;
        plan.Codewords = codewords;
        return plan;
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
}
