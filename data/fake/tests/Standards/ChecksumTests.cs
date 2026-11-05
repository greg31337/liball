using fake;

namespace fake.Tests;

[TestFixture]
public class ChecksumTests
{
    [Test]
    public void ProviderReturnsCachedStandardEngines()
    {
        ChecksumProvider checksum = Fake.Create(4301).Checksum();
        LuhnChecksum luhn = checksum.Luhn();
        VerhoeffChecksum verhoeff = checksum.Verhoeff();
        DammChecksum damm = checksum.Damm();
        Modulo10Checksum modulo10 = checksum.Modulo10();
        Modulo11Checksum modulo11 = checksum.Modulo11();
        Mod97Checksum mod97 = checksum.Mod97();

        Assert.Multiple(() =>
        {
            Assert.That(checksum.Luhn(), Is.SameAs(luhn));
            Assert.That(checksum.Verhoeff(), Is.SameAs(verhoeff));
            Assert.That(checksum.Damm(), Is.SameAs(damm));
            Assert.That(checksum.Modulo10(), Is.SameAs(modulo10));
            Assert.That(checksum.Modulo11(), Is.SameAs(modulo11));
            Assert.That(checksum.Mod97(), Is.SameAs(mod97));
        });
    }

    [TestCase("7992739871", 3, "79927398713")]
    [TestCase("123456781234567", 0, "1234567812345670")]
    [TestCase("", 0, "0")]
    public void LuhnMatchesReferenceVectors(string payload, int check, string complete)
    {
        LuhnChecksum luhn = Fake.Create(4302).Checksum().Luhn();

        Assert.Multiple(() =>
        {
            Assert.That(luhn.Calculate(payload), Is.EqualTo(check));
            Assert.That(luhn.Append(payload), Is.EqualTo(complete));
            Assert.That(luhn.Validate(complete), Is.True);
            Assert.That(luhn.Validate(ChangeLast(complete)), Is.False);
        });
    }

    [TestCase("236", 3, "2363")]
    [TestCase("942", 7, "9427")]
    [TestCase("", 0, "0")]
    public void VerhoeffMatchesPublishedExamples(string payload, int check, string complete)
    {
        VerhoeffChecksum verhoeff = Fake.Create(4303).Checksum().Verhoeff();

        Assert.Multiple(() =>
        {
            Assert.That(verhoeff.Calculate(payload), Is.EqualTo(check));
            Assert.That(verhoeff.Append(payload), Is.EqualTo(complete));
            Assert.That(verhoeff.Validate(complete), Is.True);
            Assert.That(verhoeff.Validate(ChangeLast(complete)), Is.False);
        });
    }

    [TestCase("572", 4, "5724")]
    [TestCase("43881234567", 9, "438812345679")]
    [TestCase("", 0, "0")]
    public void DammMatchesQuasigroupReferenceVectors(string payload, int check, string complete)
    {
        DammChecksum damm = Fake.Create(4304).Checksum().Damm();

        Assert.Multiple(() =>
        {
            Assert.That(damm.Calculate(payload), Is.EqualTo(check));
            Assert.That(damm.Append(payload), Is.EqualTo(complete));
            Assert.That(damm.Validate(complete), Is.True);
            Assert.That(damm.Validate(ChangeLast(complete)), Is.False);
        });
    }

    [Test]
    public void DefaultModulo10MatchesGtinWeighting()
    {
        Modulo10Checksum modulo = Fake.Create(4305).Checksum().Modulo10();

        Assert.Multiple(() =>
        {
            Assert.That(modulo.Calculate("400638133393"), Is.EqualTo(1));
            Assert.That(modulo.Append("400638133393"), Is.EqualTo("4006381333931"));
            Assert.That(modulo.Validate("4006381333931"), Is.True);
            Assert.That(modulo.Validate("4006381333932"), Is.False);
        });
    }

    [Test]
    public void ExplicitModulo11WeightsSupportDigitAndXChecks()
    {
        Modulo11Checksum isbn = Fake.Create(4306).Checksum().Modulo11FromWeights(
            new int[] { 2, 3, 4, 5, 6, 7, 8, 9, 10 });

        Assert.Multiple(() =>
        {
            Assert.That(isbn.Calculate("030640615"), Is.EqualTo(2));
            Assert.That(isbn.Append("030640615"), Is.EqualTo("0306406152"));
            Assert.That(isbn.Validate("0306406152"), Is.True);
            Assert.That(isbn.Calculate("097522980"), Is.EqualTo(10));
            Assert.That(isbn.Append("097522980"), Is.EqualTo("097522980X"));
            Assert.That(isbn.Validate("097522980X"), Is.True);
            Assert.That(isbn.Validate("097522980x"), Is.False);
        });
    }

    [TestCase("", 98, "98")]
    [TestCase("5100075470611114", 62, "510007547061111462")]
    [TestCase("32142829123456987654321611", 82, "3214282912345698765432161182")]
    public void Mod97MatchesIsoStyleRemainderVectors(string payload, int check, string complete)
    {
        Mod97Checksum mod97 = Fake.Create(4307).Checksum().Mod97();

        Assert.Multiple(() =>
        {
            Assert.That(mod97.Calculate(payload), Is.EqualTo(check));
            Assert.That(mod97.Append(payload), Is.EqualTo(complete));
            Assert.That(mod97.Validate(complete), Is.True);
            Assert.That(mod97.Validate(ChangeLast(complete)), Is.False);
        });
    }

    [Test]
    public void SpanOverloadsUseSlicesAndLeaveDestinationSuffixUntouched()
    {
        ChecksumProvider checksum = Fake.Create(4308).Checksum();
        char[] source = "!7992739871?".ToCharArray();
        char[] luhn = Enumerable.Repeat('#', 14).ToArray();
        char[] verhoeff = Enumerable.Repeat('#', 14).ToArray();
        char[] damm = Enumerable.Repeat('#', 14).ToArray();
        char[] modulo10 = Enumerable.Repeat('#', 14).ToArray();
        char[] modulo11 = Enumerable.Repeat('#', 14).ToArray();
        char[] mod97 = Enumerable.Repeat('#', 15).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(checksum.Luhn().AppendFromValueAndDestination(source.AsSpan(1, 10), luhn.AsSpan()), Is.EqualTo(11));
            Assert.That(checksum.Verhoeff().AppendFromValueAndDestination(source.AsSpan(1, 10), verhoeff.AsSpan()), Is.EqualTo(11));
            Assert.That(checksum.Damm().AppendFromValueAndDestination(source.AsSpan(1, 10), damm.AsSpan()), Is.EqualTo(11));
            Assert.That(checksum.Modulo10().AppendFromValueAndDestination(source.AsSpan(1, 10), modulo10.AsSpan()), Is.EqualTo(11));
            Assert.That(checksum.Modulo11().AppendFromValueAndDestination(source.AsSpan(1, 10), modulo11.AsSpan()), Is.EqualTo(11));
            Assert.That(checksum.Mod97().AppendFromValueAndDestination(source.AsSpan(1, 10), mod97.AsSpan()), Is.EqualTo(12));
        });

        Assert.Multiple(() =>
        {
            Assert.That(new string(luhn), Is.EqualTo(checksum.Luhn().Append("7992739871") + "###"));
            Assert.That(new string(verhoeff), Is.EqualTo(checksum.Verhoeff().Append("7992739871") + "###"));
            Assert.That(new string(damm), Is.EqualTo(checksum.Damm().Append("7992739871") + "###"));
            Assert.That(new string(modulo10), Is.EqualTo(checksum.Modulo10().Append("7992739871") + "###"));
            Assert.That(new string(modulo11), Is.EqualTo(checksum.Modulo11().Append("7992739871") + "###"));
            Assert.That(new string(mod97), Is.EqualTo(checksum.Mod97().Append("7992739871") + "###"));
            Assert.That(checksum.Luhn().ValidateFromValue(luhn.AsSpan(0, 11)), Is.True);
            Assert.That(checksum.Mod97().ValidateFromValue(mod97.AsSpan(0, 12)), Is.True);
        });
    }

    [Test]
    public void TooSmallDestinationsFailWithoutModification()
    {
        ChecksumProvider checksum = Fake.Create(4309).Checksum();
        char[] single = Enumerable.Repeat('#', 3).ToArray();
        char[] doubleCheck = Enumerable.Repeat('#', 4).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentException>(() => checksum.Luhn().AppendFromValueAndDestination("123".AsSpan(), single.AsSpan())).ParamName, Is.EqualTo("destination"));
            Assert.That(Assert.Throws<ArgumentException>(() => checksum.Verhoeff().AppendFromValueAndDestination("123".AsSpan(), single.AsSpan())).ParamName, Is.EqualTo("destination"));
            Assert.That(Assert.Throws<ArgumentException>(() => checksum.Damm().AppendFromValueAndDestination("123".AsSpan(), single.AsSpan())).ParamName, Is.EqualTo("destination"));
            Assert.That(Assert.Throws<ArgumentException>(() => checksum.Modulo10().AppendFromValueAndDestination("123".AsSpan(), single.AsSpan())).ParamName, Is.EqualTo("destination"));
            Assert.That(Assert.Throws<ArgumentException>(() => checksum.Modulo11().AppendFromValueAndDestination("123".AsSpan(), single.AsSpan())).ParamName, Is.EqualTo("destination"));
            Assert.That(Assert.Throws<ArgumentException>(() => checksum.Mod97().AppendFromValueAndDestination("123".AsSpan(), doubleCheck.AsSpan())).ParamName, Is.EqualTo("destination"));
        });

        Assert.That(single, Has.All.EqualTo('#'));
        Assert.That(doubleCheck, Has.All.EqualTo('#'));
    }

    [Test]
    public void ValidationReturnsFalseForMalformedOrIncompleteValues()
    {
        ChecksumProvider checksum = Fake.Create(4310).Checksum();

        Assert.Multiple(() =>
        {
            Assert.That(checksum.Luhn().Validate(""), Is.False);
            Assert.That(checksum.Verhoeff().Validate("12 3"), Is.False);
            Assert.That(checksum.Damm().Validate("１２３"), Is.False);
            Assert.That(checksum.Modulo10().Validate("12-3"), Is.False);
            Assert.That(checksum.Modulo11().Validate("X"), Is.False);
            Assert.That(checksum.Mod97().Validate("1"), Is.False);
            Assert.That(checksum.Mod97().Validate("12A"), Is.False);
        });
    }

    [Test]
    public void CalculationRejectsNullAndNonAsciiDecimalPayloads()
    {
        ChecksumProvider checksum = Fake.Create(4311).Checksum();

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentNullException>(() => checksum.Luhn().Calculate((string)null)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentException>(() => checksum.Verhoeff().Calculate("12 3")).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentException>(() => checksum.Damm().Append("１２３")).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentException>(() => checksum.Modulo10().Calculate("12-3")).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentException>(() => checksum.Modulo11().Calculate("12X")).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentException>(() => checksum.Mod97().Calculate("12A")).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => checksum.Mod97().Validate((string)null)).ParamName, Is.EqualTo("value"));
        });
    }

    [Test]
    public void WeightConfigurationIsValidatedAndCopied()
    {
        ChecksumProvider checksum = Fake.Create(4312).Checksum();
        int[] weights = new int[] { 3, 1 };
        Modulo10Checksum configured = checksum.Modulo10FromWeights(weights);
        int before = configured.Calculate("400638133393");
        weights[0] = 9;

        Assert.Multiple(() =>
        {
            Assert.That(configured.Calculate("400638133393"), Is.EqualTo(before));
            Assert.That(Assert.Throws<ArgumentNullException>(() => checksum.Modulo10FromWeights((int[])null)).ParamName, Is.EqualTo("weights"));
            Assert.That(Assert.Throws<ArgumentException>(() => checksum.Modulo10FromWeights(Array.Empty<int>())).ParamName, Is.EqualTo("weights"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => checksum.Modulo11FromWeights(new int[] { 2, 0, 3 })).ParamName, Is.EqualTo("weights"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => checksum.Modulo10FromWeights(new int[] { -1 })).ParamName, Is.EqualTo("weights"));
        });
    }

    [Test]
    public void ConfiguredCountAndOutputLimitsAreEnforced()
    {
        FakeOptions options = FakeOptions.Create(4313L);
        options.MaximumCount = 3;
        options.MaximumOutputSize = 4;
        ChecksumProvider checksum = Fake.Create(options).Checksum();

        Assert.Multiple(() =>
        {
            string complete = checksum.Luhn().Append("123");
            Assert.That(complete, Has.Length.EqualTo(4));
            Assert.That(checksum.Luhn().Validate(complete), Is.True);
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => checksum.Luhn().Calculate("1234")).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => checksum.Mod97().Append("123")).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => checksum.Modulo10FromWeights(new int[] { 1, 2, 3, 4 })).ParamName, Is.EqualTo("weights"));
        });
    }

    [Test]
    public void ChecksumOperationsConsumeNoRandomEntropy()
    {
        ChecksumProvider actual = Fake.Create(4314).Checksum();
        ChecksumProvider expectedPosition = Fake.Create(4314).Checksum();
        char[] destination = new char[64];

        actual.Luhn().Calculate("7992739871");
        actual.Verhoeff().Append("236");
        actual.Damm().Validate("5724");
        actual.Modulo10().AppendFromValueAndDestination("400638133393".AsSpan(), destination.AsSpan());
        actual.Modulo11().Validate("12345678903");
        actual.Mod97().CalculateFromValue("5100075470611114".AsSpan());

        Assert.That(actual.RandomValue.Int(), Is.EqualTo(expectedPosition.RandomValue.Int()));
    }

    static string ChangeLast(string value)
    {
        char[] characters = value.ToCharArray();
        char last = characters[characters.Length - 1];
        characters[characters.Length - 1] = last == '0' ? '1' : '0';
        return new string(characters);
    }
}
