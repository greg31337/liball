using fake;

namespace fake.Tests;

[TestFixture]
public class SimIdentifierConformanceTests
{
    [Test]
    public void E118MaximumIccidUsesTelecommunicationsMiiAndLuhnCheckDigit()
    {
        PhoneProvider phone = Fake.Create(9520).Phone();
        const string payload = "890123456789012345";
        const string expected = "8901234567890123455";
        string value = phone.ComposeIccid(payload);
        IccidResult result = phone.IccidResultFromValue(value);

        Assert.Multiple(() =>
        {
            Assert.That(phone.ItuE118Version(), Is.EqualTo("09/2025"));
            Assert.That(value, Is.EqualTo(expected));
            Assert.That(value, Has.Length.EqualTo(19));
            Assert.That(value, Does.StartWith("89"));
            Assert.That(IndependentLuhn(value), Is.True);
            Assert.That(result.Value, Is.EqualTo(value));
            Assert.That(result.Payload, Is.EqualTo(payload));
            Assert.That(result.MajorIndustryIdentifier, Is.EqualTo("89"));
            Assert.That(result.IssuerAndAccountIdentifier, Is.EqualTo(payload.Substring(2)));
            Assert.That(result.CheckDigit, Is.EqualTo("5"));
            Assert.That(result.StructuralValidity, Is.EqualTo(ValidityMode.Valid));
            Assert.That(result.ChecksumValidity, Is.EqualTo(ValidityMode.Valid));
            Assert.That(result.IsSynthetic, Is.False);
            Assert.That(result.RepresentsAssignment, Is.False);
            Assert.That(result.AllocationStatus, Is.EqualTo(AllocationStatus.Unknown));
        });
    }

    [Test]
    public void GeneratedE118IccidsConformWithoutClaimingIssuerAllocation()
    {
        PhoneProvider first = Fake.Create(9521).Phone();
        PhoneProvider second = Fake.Create(9521).Phone();

        int index = 0;
        while (index < 256)
        {
            IccidResult result = first.IccidResult();

            Assert.Multiple(() =>
            {
                Assert.That(result.Value, Is.EqualTo(second.IccidResult().Value));
                Assert.That(result.Value, Does.Match("^89[0-9]{17}$"));
                Assert.That(IndependentLuhn(result.Value), Is.True);
                Assert.That(result.Payload + result.CheckDigit, Is.EqualTo(result.Value));
                Assert.That(result.IsSynthetic, Is.True);
                Assert.That(result.RepresentsAssignment, Is.False);
                Assert.That(result.AllocationStatus, Is.EqualTo(AllocationStatus.Unknown));
            });
            index++;
        }
    }

    [Test]
    public void E118MutationsHaveIndependentStructureAndChecksumFailures()
    {
        PhoneProvider phone = Fake.Create(9522).Phone();
        const string valid = "8901234567890123455";
        string badCheckDigit = ChangeLast(valid);
        string badMajorIndustryIdentifier = "79" + valid.Substring(2);
        string badCharacter = valid.Substring(0, 9) + "A" + valid.Substring(10);
        string overlong = valid + "0";
        IccidResult checksumFailure = phone.IccidResultFromValue(badCheckDigit);
        IccidResult prefixFailure = phone.IccidResultFromValue(badMajorIndustryIdentifier);
        IccidResult characterFailure = phone.IccidResultFromValue(badCharacter);
        IccidResult lengthFailure = phone.IccidResultFromValue(overlong);

        Assert.Multiple(() =>
        {
            Assert.That(phone.ValidateIccid(valid), Is.True);
            Assert.That(phone.ValidateIccid(badCheckDigit), Is.False);
            Assert.That(checksumFailure.StructuralValidity, Is.EqualTo(ValidityMode.Valid));
            Assert.That(checksumFailure.ChecksumValidity, Is.EqualTo(ValidityMode.Invalid));
            Assert.That(checksumFailure.Payload, Is.Not.Empty);
            Assert.That(phone.ValidateIccid(badMajorIndustryIdentifier), Is.False);
            Assert.That(prefixFailure.StructuralValidity, Is.EqualTo(ValidityMode.Invalid));
            Assert.That(prefixFailure.ChecksumValidity, Is.EqualTo(ValidityMode.Unknown));
            Assert.That(prefixFailure.Payload, Is.Empty);
            Assert.That(phone.ValidateIccid(badCharacter), Is.False);
            Assert.That(characterFailure.StructuralValidity, Is.EqualTo(ValidityMode.Invalid));
            Assert.That(characterFailure.ChecksumValidity, Is.EqualTo(ValidityMode.Unknown));
            Assert.That(phone.ValidateIccid(overlong), Is.False);
            Assert.That(lengthFailure.StructuralValidity, Is.EqualTo(ValidityMode.Invalid));
            Assert.That(lengthFailure.ChecksumValidity, Is.EqualTo(ValidityMode.Unknown));
        });
    }

    [Test]
    public void E212FieldLengthsReachTheirPermittedBoundaries()
    {
        PhoneProvider phone = Fake.Create(9523).Phone();
        string minimumTwoDigitMnc = phone.ComposeImsi("001", "02", "0");
        string maximumTwoDigitMnc = phone.ComposeImsi("001", "02", "0123456789");
        string minimumThreeDigitMnc = phone.ComposeImsi("001", "002", "0");
        string maximumThreeDigitMnc = phone.ComposeImsi("001", "002", "012345678");

        Assert.Multiple(() =>
        {
            Assert.That(phone.ItuE212Version(), Is.EqualTo("06/2024"));
            Assert.That(minimumTwoDigitMnc, Is.EqualTo("001020"));
            Assert.That(minimumTwoDigitMnc, Has.Length.EqualTo(6));
            Assert.That(maximumTwoDigitMnc, Is.EqualTo("001020123456789"));
            Assert.That(maximumTwoDigitMnc, Has.Length.EqualTo(15));
            Assert.That(minimumThreeDigitMnc, Is.EqualTo("0010020"));
            Assert.That(minimumThreeDigitMnc, Has.Length.EqualTo(7));
            Assert.That(maximumThreeDigitMnc, Is.EqualTo("001002012345678"));
            Assert.That(maximumThreeDigitMnc, Has.Length.EqualTo(15));
            Assert.That(phone.ValidateImsiFromValueAndMobileNetworkCodeLength(
                minimumTwoDigitMnc,
                2), Is.True);
            Assert.That(phone.ValidateImsiFromValueAndMobileNetworkCodeLength(
                maximumTwoDigitMnc,
                2), Is.True);
            Assert.That(phone.ValidateImsiFromValueAndMobileNetworkCodeLength(
                minimumThreeDigitMnc,
                3), Is.True);
            Assert.That(phone.ValidateImsiFromValueAndMobileNetworkCodeLength(
                maximumThreeDigitMnc,
                3), Is.True);
            Assert.That(
                () => phone.ComposeImsi("001", "002", "0123456789"),
                Throws.TypeOf<ArgumentException>()
                    .With.Property("ParamName")
                    .EqualTo("mobileSubscriptionIdentificationNumber"));
        });
    }

    [Test]
    public void E212ExplicitMncLengthResolvesTheOtherwiseAmbiguousBoundary()
    {
        PhoneProvider phone = Fake.Create(9524).Phone();
        const string value = "310260123456789";
        ImsiResult twoDigit = phone.ImsiResultFromValueAndMobileNetworkCodeLength(
            value,
            2);
        ImsiResult threeDigit = phone.ImsiResultFromValueAndMobileNetworkCodeLength(
            value,
            3);

        Assert.Multiple(() =>
        {
            Assert.That(twoDigit.StructuralValidity, Is.EqualTo(ValidityMode.Valid));
            Assert.That(twoDigit.MobileCountryCode, Is.EqualTo("310"));
            Assert.That(twoDigit.MobileNetworkCode, Is.EqualTo("26"));
            Assert.That(twoDigit.MobileSubscriptionIdentificationNumber,
                Is.EqualTo("0123456789"));
            Assert.That(twoDigit.MobileNetworkCodeLength, Is.EqualTo(2));
            Assert.That(threeDigit.StructuralValidity, Is.EqualTo(ValidityMode.Valid));
            Assert.That(threeDigit.MobileCountryCode, Is.EqualTo("310"));
            Assert.That(threeDigit.MobileNetworkCode, Is.EqualTo("260"));
            Assert.That(threeDigit.MobileSubscriptionIdentificationNumber,
                Is.EqualTo("123456789"));
            Assert.That(threeDigit.MobileNetworkCodeLength, Is.EqualTo(3));
        });
    }

    [TestCase(2, "99", 10)]
    [TestCase(3, "999", 9)]
    public void E212PrivateNetworkTestResultsUseMcc999AndRecommendedMnc(
        int mobileNetworkCodeLength,
        string expectedMobileNetworkCode,
        int expectedSubscriptionLength)
    {
        ImsiResult result = Fake.Create(9525).Phone()
            .ImsiResultFromMobileNetworkCodeLength(mobileNetworkCodeLength);

        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Has.Length.EqualTo(15));
            Assert.That(result.MobileCountryCode, Is.EqualTo("999"));
            Assert.That(result.MobileNetworkCode, Is.EqualTo(expectedMobileNetworkCode));
            Assert.That(result.MobileSubscriptionIdentificationNumber,
                Has.Length.EqualTo(expectedSubscriptionLength));
            Assert.That(result.StructuralValidity, Is.EqualTo(ValidityMode.Valid));
            Assert.That(result.ChecksumValidity, Is.EqualTo(ValidityMode.NotApplicable));
            Assert.That(result.IsSynthetic, Is.True);
            Assert.That(result.RepresentsAssignment, Is.False);
            Assert.That(result.AllocationStatus, Is.EqualTo(AllocationStatus.Unknown));
        });
    }

    [Test]
    public void E212DigitMutationIsNotAChecksumFailureButMalformedStructureIsRejected()
    {
        PhoneProvider phone = Fake.Create(9526).Phone();
        string valid = phone.ComposeImsi("999", "99", "0123456789");
        string digitMutation = ChangeLast(valid);
        string characterMutation = valid.Substring(0, 8) + "A" + valid.Substring(9);
        string lengthMutation = valid + "0";
        ImsiResult digitResult = phone.ImsiResultFromValueAndMobileNetworkCodeLength(
            digitMutation,
            2);
        ImsiResult characterResult = phone.ImsiResultFromValueAndMobileNetworkCodeLength(
            characterMutation,
            2);
        ImsiResult lengthResult = phone.ImsiResultFromValueAndMobileNetworkCodeLength(
            lengthMutation,
            2);

        Assert.Multiple(() =>
        {
            Assert.That(phone.ValidateImsi(digitMutation), Is.True);
            Assert.That(digitResult.StructuralValidity, Is.EqualTo(ValidityMode.Valid));
            Assert.That(digitResult.ChecksumValidity, Is.EqualTo(ValidityMode.NotApplicable));
            Assert.That(phone.ValidateImsi(characterMutation), Is.False);
            Assert.That(characterResult.StructuralValidity, Is.EqualTo(ValidityMode.Invalid));
            Assert.That(characterResult.ChecksumValidity, Is.EqualTo(ValidityMode.NotApplicable));
            Assert.That(characterResult.MobileCountryCode, Is.Empty);
            Assert.That(phone.ValidateImsi(lengthMutation), Is.False);
            Assert.That(lengthResult.StructuralValidity, Is.EqualTo(ValidityMode.Invalid));
            Assert.That(lengthResult.MobileNetworkCode, Is.Empty);
        });
    }

    [Test]
    public void SimStandardsRemainLocaleIndependentWhilePhoneCategoriesFailClosed()
    {
        FakeOptions options = FakeOptions.Create("de-DE", 9527);
        options.StrictnessMode = StrictnessMode.Strict;
        PhoneProvider phone = Fake.Create(options).Phone();
        IccidResult iccid = phone.IccidResult();
        ImsiResult imsi = phone.ImsiResult();

        Assert.Multiple(() =>
        {
            Assert.That(iccid.StructuralValidity, Is.EqualTo(ValidityMode.Valid));
            Assert.That(iccid.ChecksumValidity, Is.EqualTo(ValidityMode.Valid));
            Assert.That(imsi.StructuralValidity, Is.EqualTo(ValidityMode.Valid));
            Assert.That(imsi.ChecksumValidity, Is.EqualTo(ValidityMode.NotApplicable));
            Assert.That(
                () => phone.ReservedFiction(),
                Throws.TypeOf<FakeException>().With.Message.Contains("phone.catalog"));
            Assert.That(
                () => Fake.Create(9528).Phone().TollFree(),
                Throws.TypeOf<FakeException>().With.Message.Contains(
                    "authoritative safe-fiction range"));
            Assert.That(
                () => Fake.Create(9528).Phone().RecordFromCategory(
                    (PhoneNumberCategory)999),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With.Property("ParamName")
                    .EqualTo("category"));
        });
    }

    static bool IndependentLuhn(string value)
    {
        int sum = 0;
        bool doubleDigit = false;
        int index = value.Length - 1;
        while (index >= 0)
        {
            int digit = value[index] - '0';
            if (doubleDigit)
            {
                digit = digit * 2;
                if (digit > 9)
                {
                    digit = digit - 9;
                }
            }
            sum = sum + digit;
            doubleDigit = !doubleDigit;
            index--;
        }
        return sum % 10 == 0;
    }

    static string ChangeLast(string value)
    {
        char[] characters = value.ToCharArray();
        characters[characters.Length - 1] =
            characters[characters.Length - 1] == '0' ? '1' : '0';
        return new string(characters);
    }
}
