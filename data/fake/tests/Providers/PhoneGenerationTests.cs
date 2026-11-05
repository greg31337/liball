using fake;

namespace fake.Tests;

[TestFixture]
public class PhoneGenerationTests
{
    [Test]
    public void GeneralFormatsAreDeterministicAliasesWithCorrelatedDigits()
    {
        const long seed = 9101;
        string number = Fake.Create(seed).Phone().Number();
        string national = Fake.Create(seed).Phone().National();
        string formattedNational = Fake.Create(seed).Phone().Formatted(PhoneNumberFormat.National);
        string international = Fake.Create(seed).Phone().International();
        string formattedInternational = Fake.Create(seed).Phone().Formatted(PhoneNumberFormat.International);
        string e164 = Fake.Create(seed).Phone().E164();
        string formattedE164 = Fake.Create(seed).Phone().Formatted(PhoneNumberFormat.E164);
        string nationalDigits = Digits(national);

        Assert.Multiple(() =>
        {
            Assert.That(number, Is.EqualTo(national));
            Assert.That(formattedNational, Is.EqualTo(national));
            Assert.That(formattedInternational, Is.EqualTo(international));
            Assert.That(formattedE164, Is.EqualTo(e164));
            Assert.That(national, Does.Match("^\\([2-9][0-9]{2}\\) [2-9][0-9]{2}-[0-9]{4}$"));
            Assert.That(international, Does.Match("^\\+1 [2-9][0-9]{2} [2-9][0-9]{2} [0-9]{4}$"));
            Assert.That(e164, Does.Match("^\\+1[2-9][0-9]{2}[2-9][0-9]{6}$"));
            Assert.That(Digits(international).Substring(1), Is.EqualTo(nationalDigits));
            Assert.That(Digits(e164).Substring(1), Is.EqualTo(nationalDigits));
            Assert.That(e164.Length, Is.LessThanOrEqualTo(16));
        });
    }

    [Test]
    public void NamedCategoryApisApplyTheirCategoryRules()
    {
        PhoneProvider mobile = PlausiblePhone(9102);
        PhoneProvider landline = PlausiblePhone(9102);
        PhoneProvider tollFree = PlausiblePhone(9103);
        PhoneProvider tollFreeFormatted = PlausiblePhone(9103);
        PhoneProvider premium = PlausiblePhone(9104);
        PhoneProvider premiumFormatted = PlausiblePhone(9104);

        string mobileValue = mobile.Mobile();
        string landlineValue = landline.Landline();
        string tollFreeValue = tollFree.TollFree();
        string premiumValue = premium.Premium();

        Assert.Multiple(() =>
        {
            Assert.That(mobileValue, Is.EqualTo(landlineValue));
            Assert.That(mobileValue, Does.Match("^\\([2-9][0-9]{2}\\) [2-9][0-9]{2}-[0-9]{4}$"));
            Assert.That(tollFreeValue,
                Is.EqualTo(tollFreeFormatted.FormattedFromCategoryAndFormat(PhoneNumberCategory.TollFree, PhoneNumberFormat.National)));
            Assert.That(tollFreeValue, Does.Match("^\\((800|833|844|855|866|877|888)\\) [2-9][0-9]{2}-[0-9]{4}$"));
            Assert.That(premiumValue,
                Is.EqualTo(premiumFormatted.FormattedFromCategoryAndFormat(PhoneNumberCategory.Premium, PhoneNumberFormat.National)));
            Assert.That(premiumValue, Does.Match("^\\(900\\) [2-9][0-9]{2}-[0-9]{4}$"));
            Assert.That(PlausiblePhone(9103).TollFreeFromFormat(PhoneNumberFormat.E164),
                Does.Match("^\\+1(800|833|844|855|866|877|888)[2-9][0-9]{6}$"));
            Assert.That(PlausiblePhone(9104).PremiumFromFormat(PhoneNumberFormat.International),
                Does.Match("^\\+1 900 [2-9][0-9]{2} [0-9]{4}$"));
        });
    }

    [Test]
    public void AreaCodeApisMatchTheSameSeedCategoryNumber()
    {
        foreach (PhoneNumberCategory category in Enum.GetValues<PhoneNumberCategory>())
        {
            long seed = 9110 + (int)category;
            string areaCode = PlausiblePhone(seed).AreaCodeFromCategory(category);
            string national = PlausiblePhone(seed).NationalFromCategory(category);
            string nationalAreaCode = Digits(national).Substring(0, areaCode.Length);

            Assert.That(areaCode, Does.Match("^[0-9]+$"));
            Assert.That(areaCode, Is.EqualTo(nationalAreaCode), category.ToString());
        }
    }

    [Test]
    public void ExtensionsAreDeterministicDecimalValuesWithinLocaleBounds()
    {
        PhoneProvider first = Fake.Create(9120).Phone();
        PhoneProvider second = Fake.Create(9120).Phone();

        int index = 0;
        while (index < 256)
        {
            string value = first.Extension();
            Assert.That(value, Is.EqualTo(second.Extension()));
            Assert.That(value, Does.Match("^[0-9]{1,6}$"));
            index++;
        }

        Assert.That(Fake.Create(9121).Phone().ExtensionFromLength(1), Does.Match("^[0-9]$"));
        Assert.That(Fake.Create(9121).Phone().ExtensionFromLength(6), Does.Match("^[0-9]{6}$"));
        Assert.That(() => Fake.Create(9121).Phone().ExtensionFromLength(0),
            Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("length"));
        Assert.That(() => Fake.Create(9121).Phone().ExtensionFromLength(7),
            Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("length"));
    }

    [Test]
    public void ExtensionFormattingPreservesTheBaseNumberAndLocalePrefix()
    {
        const long exactSeed = 9130;
        const long randomSeed = 9131;
        string exact = Fake.Create(exactSeed).Phone().NumberWithExtensionFromExtensionLength(4);
        string exactBase = Fake.Create(exactSeed).Phone().National();
        string random = Fake.Create(randomSeed).Phone().NumberWithExtension();
        string randomBase = Fake.Create(randomSeed).Phone().National();
        string international = PlausiblePhone(9132).FormattedWithExtension(
            PhoneNumberCategory.TollFree,
            PhoneNumberFormat.International,
            3);

        Assert.Multiple(() =>
        {
            Assert.That(exact, Does.StartWith(exactBase + " ext. "));
            Assert.That(exact.Substring((exactBase + " ext. ").Length), Does.Match("^[0-9]{4}$"));
            Assert.That(random, Does.StartWith(randomBase + " ext. "));
            Assert.That(random.Substring((randomBase + " ext. ").Length), Does.Match("^[0-9]{1,6}$"));
            Assert.That(international,
                Does.Match("^\\+1 (800|833|844|855|866|877|888) [2-9][0-9]{2} [0-9]{4} ext\\. [0-9]{3}$"));
        });

        Assert.That(
            () => Fake.Create(9133).Phone().FormattedWithExtension(
                PhoneNumberCategory.General,
                PhoneNumberFormat.E164,
                4),
            Throws.TypeOf<ArgumentException>().With.Property("ParamName").EqualTo("format"));
    }

    [Test]
    public void CallerLocaleMasksDriveEveryGeneratedPresentation()
    {
        const long seed = 9140;
        Fake nationalFake = BritishFake(seed);
        Fake internationalFake = BritishFake(seed);
        Fake e164Fake = BritishFake(seed);
        string national = nationalFake.Phone().National();
        string international = internationalFake.Phone().International();
        string e164 = e164Fake.Phone().E164();

        Assert.Multiple(() =>
        {
            Assert.That(national, Does.Match("^0[0-9]{4} [0-9]{6}$"));
            Assert.That(international, Does.Match("^\\+44 [0-9]{4} [0-9]{6}$"));
            Assert.That(e164, Does.Match("^\\+44[0-9]{10}$"));
            Assert.That(Digits(national).Substring(1), Is.EqualTo(Digits(international).Substring(2)));
            Assert.That(Digits(national).Substring(1), Is.EqualTo(Digits(e164).Substring(2)));
        });
    }

    [Test]
    public void PhoneGenerationRejectsUnsupportedEnumsAndHonorsOutputLimits()
    {
        PhoneProvider phone = Fake.Create(9150).Phone();
        string baseline = Fake.Create(9151).Phone().NumberWithExtensionFromExtensionLength(6);
        FakeOptions exactOptions = FakeOptions.Create(9151);
        exactOptions.MaximumOutputSize = baseline.Length;
        FakeOptions shortOptions = FakeOptions.Create(9151);
        shortOptions.MaximumOutputSize = baseline.Length - 1;

        Assert.That(() => phone.NumberFromCategory((PhoneNumberCategory)999),
            Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("category"));
        Assert.That(() => phone.Formatted((PhoneNumberFormat)999),
            Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("format"));
        Assert.That(Fake.Create(exactOptions).Phone().NumberWithExtensionFromExtensionLength(6), Is.EqualTo(baseline));
        Assert.That(() => Fake.Create(shortOptions).Phone().NumberWithExtensionFromExtensionLength(6),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    static string Digits(string value)
    {
        return new string(value.Where(char.IsAsciiDigit).ToArray());
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

    static PhoneProvider PlausiblePhone(long seed)
    {
        FakeOptions options = FakeOptions.Create(seed);
        options.SafetyMode = SafetyMode.SyntheticPlausible;
        return Fake.Create(options).Phone();
    }
}
