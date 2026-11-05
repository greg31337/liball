using fake;

namespace fake.Tests;

[TestFixture]
public class PhoneCompletionTests
{
    [Test]
    public void ExplicitReservedFictionIgnoresTheConfiguredPlausibleMode()
    {
        const long seed = 9240;
        string national = PlausiblePhone(seed).ReservedFiction();
        string international = PlausiblePhone(seed).ReservedFictionFromFormat(
            PhoneNumberFormat.International);
        string e164 = PlausiblePhone(seed).ReservedFictionFromFormat(
            PhoneNumberFormat.E164);
        PhoneRecord record = PlausiblePhone(seed).ReservedFictionRecord();

        Assert.Multiple(() =>
        {
            Assert.That(national, Does.Match("^\\([2-9][0-9]{2}\\) 555-01[0-9]{2}$"));
            Assert.That(Digits(international).Substring(1), Is.EqualTo(Digits(national)));
            Assert.That(Digits(e164).Substring(1), Is.EqualTo(Digits(national)));
            Assert.That(record.Number, Is.EqualTo(national));
            Assert.That(record.SafetyMetadata.RequestedMode, Is.EqualTo(SafetyMode.Reserved));
            Assert.That(record.SafetyMetadata.AppliedMode, Is.EqualTo(SafetyMode.Reserved));
            Assert.That(record.SafetyMetadata.UsesAuthoritativeSafeRange, Is.True);
        });
    }

    [Test]
    public void ExplicitReservedFictionFailsClosedWithoutAnAuthoritativeRange()
    {
        PhoneProvider phone = BritishFake(9241).Phone();

        Assert.That(
            () => phone.ReservedFiction(),
            Throws.TypeOf<FakeException>().With.Message.Contains(
                "authoritative safe-fiction range"));
    }

    [Test]
    public void PhoneWordsDecodeToTheSameReservedNumberInSupportedFormats()
    {
        const long seed = 9242;
        PhoneProvider phone = Fake.Create(seed).Phone();
        string national = phone.PhoneWord();
        string expectedNational = Fake.Create(seed).Phone().ReservedFiction();
        string international = Fake.Create(seed).Phone().PhoneWordFromFormat(
            PhoneNumberFormat.International);
        string expectedInternational = Fake.Create(seed).Phone().ReservedFictionFromFormat(
            PhoneNumberFormat.International);

        Assert.Multiple(() =>
        {
            Assert.That(phone.SupportsPhoneWords(), Is.True);
            Assert.That(phone.Metadata().SupportsPhoneWords, Is.True);
            Assert.That(national, Does.Match("^\\([2-9][0-9]{2}\\) JJJ-01[A-Z0-9]{2}$"));
            Assert.That(KeypadDigits(national), Is.EqualTo(expectedNational));
            Assert.That(KeypadDigits(international), Is.EqualTo(expectedInternational));
            Assert.That(
                () => Fake.Create(seed).Phone().PhoneWordFromFormat(PhoneNumberFormat.E164),
                Throws.TypeOf<ArgumentException>().With.Property("ParamName").EqualTo("format"));
            Assert.That(
                () => Fake.Create(seed).Phone().PhoneWordFromFormat((PhoneNumberFormat)999),
                Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("format"));
        });
    }

    [Test]
    public void PhoneWordsFailBeforeGenerationWhenTheLocaleDoesNotSupportThem()
    {
        PhoneProvider phone = BritishFake(9243).Phone();

        Assert.Multiple(() =>
        {
            Assert.That(phone.SupportsPhoneWords(), Is.False);
            Assert.That(phone.Metadata().SupportsPhoneWords, Is.False);
            Assert.That(
                () => phone.PhoneWord(),
                Throws.TypeOf<FakeException>().With.Message.Contains("unsupported"));
        });
    }

    [Test]
    public void GeneratedIccidResultExposesCorrelatedComponentsAndValidity()
    {
        const long seed = 9244;
        IccidResult result = Fake.Create(seed).Phone().IccidResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.EqualTo(Fake.Create(seed).Phone().Iccid()));
            Assert.That(result.Payload, Is.EqualTo(result.Value.Substring(0, result.Value.Length - 1)));
            Assert.That(result.MajorIndustryIdentifier, Is.EqualTo("89"));
            Assert.That(result.IssuerAndAccountIdentifier, Is.EqualTo(result.Payload.Substring(2)));
            Assert.That(result.CheckDigit, Is.EqualTo(result.Value.Substring(result.Value.Length - 1)));
            Assert.That(result.StructuralValidity, Is.EqualTo(ValidityMode.Valid));
            Assert.That(result.ChecksumValidity, Is.EqualTo(ValidityMode.Valid));
            Assert.That(result.IsSynthetic, Is.True);
            Assert.That(result.RepresentsAssignment, Is.False);
            Assert.That(result.AllocationStatus, Is.EqualTo(AllocationStatus.Unknown));
        });
    }

    [Test]
    public void IccidInspectionSeparatesStructureFromChecksumValidity()
    {
        PhoneProvider phone = Fake.Create(9245).Phone();
        string valid = phone.Iccid();
        char replacement = valid[valid.Length - 1] == '0' ? '1' : '0';
        string badChecksum = valid.Substring(0, valid.Length - 1) + replacement;
        IccidResult inspected = phone.IccidResultFromValue(valid);
        IccidResult checksumFailure = phone.IccidResultFromValue(badChecksum);
        IccidResult structuralFailure = phone.IccidResultFromValue("791234");

        Assert.Multiple(() =>
        {
            Assert.That(inspected.StructuralValidity, Is.EqualTo(ValidityMode.Valid));
            Assert.That(inspected.ChecksumValidity, Is.EqualTo(ValidityMode.Valid));
            Assert.That(inspected.IsSynthetic, Is.False);
            Assert.That(checksumFailure.StructuralValidity, Is.EqualTo(ValidityMode.Valid));
            Assert.That(checksumFailure.ChecksumValidity, Is.EqualTo(ValidityMode.Invalid));
            Assert.That(checksumFailure.Payload, Is.Not.Empty);
            Assert.That(structuralFailure.StructuralValidity, Is.EqualTo(ValidityMode.Invalid));
            Assert.That(structuralFailure.ChecksumValidity, Is.EqualTo(ValidityMode.Unknown));
            Assert.That(structuralFailure.Payload, Is.Empty);
            Assert.That(structuralFailure.AllocationStatus, Is.EqualTo(AllocationStatus.Unknown));
            Assert.That(
                () => phone.IccidResultFromValue(null),
                Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("value"));
        });
    }

    [TestCase(2, "99")]
    [TestCase(3, "999")]
    public void GeneratedImsiResultExposesTheRequestedFieldBoundary(
        int mobileNetworkCodeLength,
        string expectedMobileNetworkCode)
    {
        const long seed = 9246;
        ImsiResult result = Fake.Create(seed).Phone()
            .ImsiResultFromMobileNetworkCodeLength(mobileNetworkCodeLength);
        string expected = Fake.Create(seed).Phone()
            .ImsiFromMobileNetworkCodeLength(mobileNetworkCodeLength);

        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.EqualTo(expected));
            Assert.That(result.MobileCountryCode, Is.EqualTo("999"));
            Assert.That(result.MobileNetworkCode, Is.EqualTo(expectedMobileNetworkCode));
            Assert.That(result.MobileSubscriptionIdentificationNumber,
                Is.EqualTo(expected.Substring(3 + mobileNetworkCodeLength)));
            Assert.That(result.MobileNetworkCodeLength, Is.EqualTo(mobileNetworkCodeLength));
            Assert.That(result.StructuralValidity, Is.EqualTo(ValidityMode.Valid));
            Assert.That(result.ChecksumValidity, Is.EqualTo(ValidityMode.NotApplicable));
            Assert.That(result.IsSynthetic, Is.True);
            Assert.That(result.RepresentsAssignment, Is.False);
            Assert.That(result.AllocationStatus, Is.EqualTo(AllocationStatus.Unknown));
        });
    }

    [Test]
    public void ImsiInspectionReportsInvalidStructureWithoutInventingComponents()
    {
        PhoneProvider phone = Fake.Create(9247).Phone();
        string valid = phone.ImsiFromMobileNetworkCodeLength(3);
        ImsiResult inspected = phone.ImsiResultFromValueAndMobileNetworkCodeLength(valid, 3);
        ImsiResult invalid = phone.ImsiResultFromValueAndMobileNetworkCodeLength("99999A", 2);

        Assert.Multiple(() =>
        {
            Assert.That(inspected.MobileCountryCode, Is.EqualTo("999"));
            Assert.That(inspected.MobileNetworkCode, Is.EqualTo("999"));
            Assert.That(inspected.IsSynthetic, Is.False);
            Assert.That(inspected.StructuralValidity, Is.EqualTo(ValidityMode.Valid));
            Assert.That(invalid.StructuralValidity, Is.EqualTo(ValidityMode.Invalid));
            Assert.That(invalid.ChecksumValidity, Is.EqualTo(ValidityMode.NotApplicable));
            Assert.That(invalid.MobileCountryCode, Is.Empty);
            Assert.That(invalid.MobileNetworkCode, Is.Empty);
            Assert.That(invalid.MobileSubscriptionIdentificationNumber, Is.Empty);
            Assert.That(invalid.AllocationStatus, Is.EqualTo(AllocationStatus.Unknown));
            Assert.That(
                () => phone.ImsiResultFromValueAndMobileNetworkCodeLength(valid, 4),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With.Property("ParamName").EqualTo("mobileNetworkCodeLength"));
        });
    }

    static string Digits(string value)
    {
        return new string(value.Where(char.IsAsciiDigit).ToArray());
    }

    static string KeypadDigits(string value)
    {
        char[] result = value.ToCharArray();
        int index = 0;
        while (index < result.Length)
        {
            char character = result[index];
            if (character >= 'A' && character <= 'C')
            {
                result[index] = '2';
            }
            else if (character >= 'D' && character <= 'F')
            {
                result[index] = '3';
            }
            else if (character >= 'G' && character <= 'I')
            {
                result[index] = '4';
            }
            else if (character >= 'J' && character <= 'L')
            {
                result[index] = '5';
            }
            else if (character >= 'M' && character <= 'O')
            {
                result[index] = '6';
            }
            else if (character >= 'P' && character <= 'S')
            {
                result[index] = '7';
            }
            else if (character >= 'T' && character <= 'V')
            {
                result[index] = '8';
            }
            else if (character >= 'W' && character <= 'Z')
            {
                result[index] = '9';
            }
            index++;
        }
        return new string(result);
    }

    static PhoneProvider PlausiblePhone(long seed)
    {
        FakeOptions options = FakeOptions.Create(seed);
        options.SafetyMode = SafetyMode.SyntheticPlausible;
        return Fake.Create(options).Phone();
    }

    static Fake BritishFake(long seed)
    {
        FakeOptions options = FakeOptions.Create("en-GB", seed);
        options.SafetyMode = SafetyMode.SyntheticPlausible;
        Fake fake = Fake.Create(options);
        fake.Locale().RegisterFromLocaleAndFields("en-GB", new Dictionary<string, object>
        {
            ["phone.catalog"] = new Dictionary<string, object>
            {
                ["regionCode"] = "GB",
                ["callingCode"] = "+44",
                ["internationalDialingPrefix"] = "00",
                ["nationalPrefix"] = "0",
                ["areaCodeMask"] = "####",
                ["subscriberMask"] = "######",
                ["extensionPrefix"] = " x",
                ["minimumExtensionDigits"] = 2,
                ["maximumExtensionDigits"] = 5,
                ["supportsPhoneWords"] = false,
                ["safeRangeField"] = "",
                ["formats"] = new object[]
                {
                    new Dictionary<string, object>
                    {
                        ["category"] = "general",
                        ["areaCodeMask"] = "####",
                        ["subscriberMask"] = "######",
                        ["nationalMask"] = "0#### ######",
                        ["internationalMask"] = "+44 #### ######",
                        ["e164Mask"] = "+44##########",
                        ["areaCodes"] = Array.Empty<string>(),
                        ["allowsExtension"] = true,
                        ["distinguishableByNumber"] = false
                    }
                }
            }
        });
        return fake;
    }
}
