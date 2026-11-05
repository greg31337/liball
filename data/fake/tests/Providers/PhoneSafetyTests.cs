using fake;

namespace fake.Tests;

[TestFixture]
public class PhoneSafetyTests
{
    [Test]
    public void DefaultGeneralFormatsUseTheAuthoritativeNanpaFictionRange()
    {
        const long seed = 9201;
        string national = Fake.Create(seed).Phone().National();
        string international = Fake.Create(seed).Phone().International();
        string e164 = Fake.Create(seed).Phone().E164();

        Assert.Multiple(() =>
        {
            Assert.That(national, Does.Match("^\\([2-9][0-9]{2}\\) 555-01[0-9]{2}$"));
            Assert.That(international, Does.Match("^\\+1 [2-9][0-9]{2} 555 01[0-9]{2}$"));
            Assert.That(e164, Does.Match("^\\+1[2-9][0-9]{2}55501[0-9]{2}$"));
            Assert.That(Digits(international).Substring(1), Is.EqualTo(Digits(national)));
            Assert.That(Digits(e164).Substring(1), Is.EqualTo(Digits(national)));
        });
    }

    [Test]
    public void EveryDefaultGeneralValueStaysInsideTheInclusiveNanpaBoundary()
    {
        PhoneProvider phone = Fake.Create(9202).Phone();

        int index = 0;
        while (index < 512)
        {
            string digits = Digits(phone.National());
            int subscriber = int.Parse(digits.Substring(6, 4));

            Assert.That(digits.Substring(3, 3), Is.EqualTo("555"));
            Assert.That(subscriber, Is.InRange(100, 199));
            index++;
        }
    }

    [Test]
    public void IndistinguishableCategoriesUseTheSafeRangeAndDistinctServicesFailClosed()
    {
        PhoneProvider phone = Fake.Create(9203).Phone();
        string landline = phone.Landline();
        string mobile = phone.Mobile();
        PhoneSafetyMetadata landlineSafety = phone.SafetyMetadataFromCategory(PhoneNumberCategory.Landline);
        PhoneSafetyMetadata mobileSafety = phone.SafetyMetadataFromCategory(PhoneNumberCategory.Mobile);

        Assert.Multiple(() =>
        {
            Assert.That(landline, Does.Match("^\\([2-9][0-9]{2}\\) 555-01[0-9]{2}$"));
            Assert.That(mobile, Does.Match("^\\([2-9][0-9]{2}\\) 555-01[0-9]{2}$"));
            Assert.That(landlineSafety.UsesAuthoritativeSafeRange, Is.True);
            Assert.That(mobileSafety.UsesAuthoritativeSafeRange, Is.True);
            Assert.That(() => phone.TollFree(), Throws.TypeOf<FakeException>().With.Message.Contains("requires an authoritative safe-fiction range"));
            Assert.That(() => phone.Premium(), Throws.TypeOf<FakeException>().With.Message.Contains("requires an authoritative safe-fiction range"));
            Assert.That(() => phone.SafetyMetadataFromCategory(PhoneNumberCategory.TollFree), Throws.TypeOf<FakeException>());
            Assert.That(() => phone.SafetyMetadataFromCategory(PhoneNumberCategory.Premium), Throws.TypeOf<FakeException>());
        });
    }

    [Test]
    public void SafetyMetadataExposesTheAppliedAuthoritativeRange()
    {
        PhoneSafetyMetadata safety = Fake.Create(9204).Phone().SafetyMetadata();

        Assert.Multiple(() =>
        {
            Assert.That(safety.RequestedMode, Is.EqualTo(SafetyMode.Reserved));
            Assert.That(safety.AppliedMode, Is.EqualTo(SafetyMode.Reserved));
            Assert.That(safety.Category, Is.EqualTo(PhoneNumberCategory.General));
            Assert.That(safety.RegionCode, Is.EqualTo("US"));
            Assert.That(safety.UsesAuthoritativeSafeRange, Is.True);
            Assert.That(safety.UsedFallback, Is.False);
            Assert.That(safety.FallbackReason, Is.Empty);
            Assert.That(safety.SafeRange, Is.Not.Null);
            Assert.That(safety.SafeRange.FirstSubscriber, Is.EqualTo("0100"));
            Assert.That(safety.SafeRange.LastSubscriber, Is.EqualTo("0199"));
            Assert.That(safety.SafeRange.Authority, Is.EqualTo("NANPA 555 line-number reservation"));
            Assert.That(safety.SafeRange.SourceVersion, Is.EqualTo("NANPA page retrieved 2026-08-14"));
        });
    }

    [TestCase(SafetyMode.SyntheticPlausible)]
    [TestCase(SafetyMode.UnrestrictedShape)]
    public void ExplicitNonReservedModesRetainPlausibleGenerationWithoutFallback(
        SafetyMode mode)
    {
        FakeOptions options = FakeOptions.Create(9205);
        options.SafetyMode = mode;
        PhoneProvider phone = Fake.Create(options).Phone();
        PhoneSafetyMetadata safety = phone.SafetyMetadata();
        bool foundOutsideReservedRange = false;

        int index = 0;
        while (index < 64)
        {
            string value = phone.National();
            Assert.That(value, Does.Match("^\\([2-9][0-9]{2}\\) [2-9][0-9]{2}-[0-9]{4}$"));
            if (!value.Contains(" 555-01", StringComparison.Ordinal))
            {
                foundOutsideReservedRange = true;
            }
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(foundOutsideReservedRange, Is.True);
            Assert.That(safety.RequestedMode, Is.EqualTo(mode));
            Assert.That(safety.AppliedMode, Is.EqualTo(mode));
            Assert.That(safety.UsesAuthoritativeSafeRange, Is.False);
            Assert.That(safety.UsedFallback, Is.False);
            Assert.That(safety.FallbackReason, Is.Empty);
            Assert.That(safety.SafeRange, Is.Null);
        });
    }

    [Test]
    public void UnsupportedRegionRequiresExplicitPlausibleMode()
    {
        Fake reserved = BritishFake(9206, SafetyMode.Reserved);
        Fake plausible = BritishFake(9206, SafetyMode.SyntheticPlausible);
        PhoneSafetyMetadata safety = plausible.Phone().SafetyMetadata();
        string national = plausible.Phone().National();

        Assert.Multiple(() =>
        {
            Assert.That(() => reserved.Phone().SafetyMetadata(), Throws.TypeOf<FakeException>());
            Assert.That(() => reserved.Phone().National(), Throws.TypeOf<FakeException>());
            Assert.That(national, Does.Match("^0[0-9]{4} [0-9]{6}$"));
            Assert.That(safety.RequestedMode, Is.EqualTo(SafetyMode.SyntheticPlausible));
            Assert.That(safety.AppliedMode, Is.EqualTo(SafetyMode.SyntheticPlausible));
            Assert.That(safety.RegionCode, Is.EqualTo("GB"));
            Assert.That(safety.UsesAuthoritativeSafeRange, Is.False);
            Assert.That(safety.UsedFallback, Is.False);
            Assert.That(safety.FallbackReason, Is.Empty);
            Assert.That(safety.SafeRange, Is.Null);
        });
    }

    [Test]
    public void ReadingSafetyMetadataDoesNotConsumePhoneEntropy()
    {
        PhoneProvider inspected = Fake.Create(9207).Phone();
        PhoneProvider untouched = Fake.Create(9207).Phone();

        inspected.SafetyMetadata();

        Assert.That(inspected.Number(), Is.EqualTo(untouched.Number()));
    }

    [Test]
    public void SafetyMetadataRejectsUnsupportedCategoriesAndHonorsOutputLimits()
    {
        FakeOptions options = FakeOptions.Create(9208);
        options.MaximumOutputSize = 10;

        Assert.That(
            () => Fake.Create(9208).Phone().SafetyMetadataFromCategory((PhoneNumberCategory)999),
            Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("category"));
        Assert.That(
            () => Fake.Create(options).Phone().SafetyMetadata(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    static string Digits(string value)
    {
        return new string(value.Where(char.IsAsciiDigit).ToArray());
    }

    static Fake BritishFake(long seed, SafetyMode safetyMode)
    {
        FakeOptions options = FakeOptions.Create("en-GB", seed);
        options.SafetyMode = safetyMode;
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
