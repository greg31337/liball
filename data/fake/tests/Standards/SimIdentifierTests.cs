using fake;

namespace fake.Tests;

[TestFixture]
public class SimIdentifierTests
{
    [Test]
    public void RandomIccidsAreDeterministicNineteenDigitLuhnShapes()
    {
        PhoneProvider first = Fake.Create(9501).Phone();
        PhoneProvider second = Fake.Create(9501).Phone();
        LuhnChecksum luhn = Fake.Create(9502).Checksum().Luhn();

        int index = 0;
        while (index < 512)
        {
            string value = first.Iccid();

            Assert.Multiple(() =>
            {
                Assert.That(value, Is.EqualTo(second.Iccid()));
                Assert.That(value, Does.Match("^89[0-9]{17}$"));
                Assert.That(first.ValidateIccid(value), Is.True);
                Assert.That(luhn.Validate(value), Is.True);
                Assert.That(first.IccidPayload(value), Is.EqualTo(value.Substring(0, 18)));
                Assert.That(first.ComposeIccid(first.IccidPayload(value)), Is.EqualTo(value));
            });
            index++;
        }

        Assert.That(first.ItuE118Version(), Is.EqualTo("09/2025"));
    }

    [Test]
    public void IccidCompositionSupportsTheDocumentedMaximumWithoutInventingAllocation()
    {
        PhoneProvider phone = Fake.Create(9503).Phone();
        LuhnChecksum luhn = Fake.Create(9504).Checksum().Luhn();
        string maximumPayload = "890123456789012345";
        string maximum = phone.ComposeIccid(maximumPayload);
        string shortValue = phone.ComposeIccid("89012");

        Assert.Multiple(() =>
        {
            Assert.That(maximum, Does.StartWith(maximumPayload));
            Assert.That(maximum, Has.Length.EqualTo(19));
            Assert.That(phone.ValidateIccid(maximum), Is.True);
            Assert.That(luhn.Validate(maximum), Is.True);
            Assert.That(shortValue, Has.Length.EqualTo(6));
            Assert.That(phone.ValidateIccid(shortValue), Is.True);
            Assert.That(phone.IccidPayload(shortValue), Is.EqualTo("89012"));
        });
    }

    [Test]
    public void IccidValidationRejectsBadChecksLengthsPrefixesAndCharacters()
    {
        PhoneProvider phone = Fake.Create(9505).Phone();
        string valid = phone.Iccid();

        Assert.Multiple(() =>
        {
            Assert.That(phone.ValidateIccid(ChangeLast(valid)), Is.False);
            Assert.That(phone.ValidateIccid("79" + valid.Substring(2)), Is.False);
            Assert.That(phone.ValidateIccid(valid.Substring(0, 18)), Is.False);
            Assert.That(phone.ValidateIccid(valid + "0"), Is.False);
            Assert.That(phone.ValidateIccid("８９" + valid.Substring(2)), Is.False);
            Assert.That(phone.ValidateIccid(valid.Substring(0, 10) + "-" + valid.Substring(11)), Is.False);
        });
    }

    [Test]
    public void DefaultImsiUsesThePrivateNetworkTestCodesAndRetainsLeadingZeroes()
    {
        PhoneProvider first = Fake.Create(9506).Phone();
        PhoneProvider second = Fake.Create(9506).Phone();
        bool observedLeadingZero = false;

        int index = 0;
        while (index < 512)
        {
            string value = first.Imsi();
            string subscription = first.ImsiMobileSubscriptionIdentificationNumber(value, 2);

            Assert.Multiple(() =>
            {
                Assert.That(value, Is.EqualTo(second.Imsi()));
                Assert.That(value, Does.Match("^99999[0-9]{10}$"));
                Assert.That(first.ValidateImsi(value), Is.True);
                Assert.That(first.ValidateImsiFromValueAndMobileNetworkCodeLength(value, 2), Is.True);
                Assert.That(first.ImsiMobileCountryCode(value, 2), Is.EqualTo("999"));
                Assert.That(first.ImsiMobileNetworkCode(value, 2), Is.EqualTo("99"));
                Assert.That(subscription, Has.Length.EqualTo(10));
            });
            if (subscription[0] == '0')
            {
                observedLeadingZero = true;
            }
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(observedLeadingZero, Is.True);
            Assert.That(first.ItuE212Version(), Is.EqualTo("06/2024"));
        });
    }

    [Test]
    public void PrivateNetworkImsiSupportsBothDocumentedMncLengths()
    {
        const long seed = 9507;
        string twoDigit = Fake.Create(seed).Phone().ImsiFromMobileNetworkCodeLength(2);
        string threeDigit = Fake.Create(seed).Phone().ImsiFromMobileNetworkCodeLength(3);
        PhoneProvider phone = Fake.Create(9508).Phone();

        Assert.Multiple(() =>
        {
            Assert.That(twoDigit, Does.Match("^99999[0-9]{10}$"));
            Assert.That(threeDigit, Does.Match("^999999[0-9]{9}$"));
            Assert.That(phone.ValidateImsiFromValueAndMobileNetworkCodeLength(twoDigit, 2), Is.True);
            Assert.That(phone.ValidateImsiFromValueAndMobileNetworkCodeLength(threeDigit, 3), Is.True);
            Assert.That(phone.ImsiMobileNetworkCode(twoDigit, 2), Is.EqualTo("99"));
            Assert.That(phone.ImsiMobileNetworkCode(threeDigit, 3), Is.EqualTo("999"));
            Assert.That(phone.ImsiMobileSubscriptionIdentificationNumber(twoDigit, 2), Has.Length.EqualTo(10));
            Assert.That(phone.ImsiMobileSubscriptionIdentificationNumber(threeDigit, 3), Has.Length.EqualTo(9));
        });
    }

    [Test]
    public void CallerSuppliedImsiFieldsPreserveCodesAndLeadingZeroes()
    {
        const long seed = 9509;
        PhoneProvider first = Fake.Create(seed).Phone();
        PhoneProvider second = Fake.Create(seed).Phone();
        string generated = first.ImsiFromMobileCountryCodeAndMobileNetworkCode("001", "02");
        string twoDigit = first.ComposeImsi("001", "02", "0000000001");
        string threeDigit = first.ComposeImsi("001", "002", "000000001");

        Assert.Multiple(() =>
        {
            Assert.That(generated, Is.EqualTo(second.ImsiFromMobileCountryCodeAndMobileNetworkCode("001", "02")));
            Assert.That(generated, Does.Match("^00102[0-9]{10}$"));
            Assert.That(twoDigit, Is.EqualTo("001020000000001"));
            Assert.That(threeDigit, Is.EqualTo("001002000000001"));
            Assert.That(first.ValidateImsiFromValueAndMobileNetworkCodeLength(twoDigit, 2), Is.True);
            Assert.That(first.ValidateImsiFromValueAndMobileNetworkCodeLength(threeDigit, 3), Is.True);
            Assert.That(first.ImsiMobileCountryCode(threeDigit, 3), Is.EqualTo("001"));
            Assert.That(first.ImsiMobileNetworkCode(threeDigit, 3), Is.EqualTo("002"));
            Assert.That(first.ImsiMobileSubscriptionIdentificationNumber(threeDigit, 3), Is.EqualTo("000000001"));
        });
    }

    [Test]
    public void ImsiValidationEnforcesAsciiAndLengthButDoesNotInventAChecksum()
    {
        PhoneProvider phone = Fake.Create(9510).Phone();
        string value = phone.Imsi();

        Assert.Multiple(() =>
        {
            Assert.That(phone.ValidateImsi(ChangeLast(value)), Is.True);
            Assert.That(phone.ValidateImsi("12345"), Is.False);
            Assert.That(phone.ValidateImsi("123456"), Is.True);
            Assert.That(phone.ValidateImsi("123456789012345"), Is.True);
            Assert.That(phone.ValidateImsi("1234567890123456"), Is.False);
            Assert.That(phone.ValidateImsi("１２３４５６"), Is.False);
            Assert.That(phone.ValidateImsi("12345A"), Is.False);
            Assert.That(phone.ValidateImsiFromValueAndMobileNetworkCodeLength("123456", 2), Is.True);
            Assert.That(phone.ValidateImsiFromValueAndMobileNetworkCodeLength("123456", 3), Is.False);
        });
    }

    [Test]
    public void InvalidSimIdentifierFieldsUseExplicitParameters()
    {
        PhoneProvider phone = Fake.Create(9511).Phone();

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentNullException>(() => phone.ComposeIccid(null)).ParamName, Is.EqualTo("payload"));
            Assert.That(Assert.Throws<ArgumentException>(() => phone.ComposeIccid("79")).ParamName, Is.EqualTo("payload"));
            Assert.That(Assert.Throws<ArgumentException>(() => phone.ComposeIccid("89A")).ParamName, Is.EqualTo("payload"));
            Assert.That(Assert.Throws<ArgumentException>(() => phone.ComposeIccid("8901234567890123456")).ParamName, Is.EqualTo("payload"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => phone.ValidateIccid((string)null)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => phone.ImsiFromMobileNetworkCodeLength(1)).ParamName, Is.EqualTo("mobileNetworkCodeLength"));
            Assert.That(Assert.Throws<ArgumentException>(() => phone.ImsiFromMobileCountryCodeAndMobileNetworkCode("01", "99")).ParamName, Is.EqualTo("mobileCountryCode"));
            Assert.That(Assert.Throws<ArgumentException>(() => phone.ImsiFromMobileCountryCodeAndMobileNetworkCode("999", "9")).ParamName, Is.EqualTo("mobileNetworkCode"));
            Assert.That(Assert.Throws<ArgumentException>(() => phone.ComposeImsi("999", "999", "1234567890")).ParamName, Is.EqualTo("mobileSubscriptionIdentificationNumber"));
            Assert.That(Assert.Throws<ArgumentException>(() => phone.ComposeImsi("999", "99", "")).ParamName, Is.EqualTo("mobileSubscriptionIdentificationNumber"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => phone.ValidateImsi((string)null)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => phone.ValidateImsiFromValueAndMobileNetworkCodeLength("999991", 4)).ParamName, Is.EqualTo("mobileNetworkCodeLength"));
            Assert.That(Assert.Throws<ArgumentException>(() => phone.ImsiMobileNetworkCode("12345A", 2)).ParamName, Is.EqualTo("value"));
        });
    }

    [Test]
    public void SimIdentifierLimitsAreCheckedBeforeRandomDigitsAreConsumed()
    {
        FakeOptions outputOptions = FakeOptions.Create(9512);
        outputOptions.MaximumOutputSize = 18;
        PhoneProvider outputActual = Fake.Create(outputOptions).Phone();
        PhoneProvider outputExpected = Fake.Create(outputOptions).Phone();

        Assert.Throws<ArgumentOutOfRangeException>(() => outputActual.Iccid());
        Assert.That(outputActual.Imsi(), Is.EqualTo(outputExpected.Imsi()));

        FakeOptions countOptions = FakeOptions.Create(9513);
        countOptions.MaximumCount = 17;
        PhoneProvider countActual = Fake.Create(countOptions).Phone();
        PhoneProvider countExpected = Fake.Create(countOptions).Phone();

        Assert.Throws<ArgumentOutOfRangeException>(() => countActual.Iccid());
        Assert.That(countActual.Imsi(), Is.EqualTo(countExpected.Imsi()));
    }

    [Test]
    public void CryptographicModeProducesStructurallyValidSimIdentifiers()
    {
        FakeOptions options = new FakeOptions();
        options.RandomMode = RandomMode.Cryptographic;
        PhoneProvider phone = Fake.Create(options).Phone();

        int index = 0;
        while (index < 128)
        {
            Assert.That(phone.ValidateIccid(phone.Iccid()), Is.True);
            Assert.That(phone.ValidateImsi(phone.Imsi()), Is.True);
            index++;
        }
    }

    static string ChangeLast(string value)
    {
        char[] characters = value.ToCharArray();
        characters[characters.Length - 1] = characters[characters.Length - 1] == '0' ? '1' : '0';
        return new string(characters);
    }
}
