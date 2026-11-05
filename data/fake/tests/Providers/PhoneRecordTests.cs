using fake;

namespace fake.Tests;

[TestFixture]
public class PhoneRecordTests
{
    [Test]
    public void DefaultRecordPreservesGeneratedValueComponentsAndContext()
    {
        const long seed = 9301;
        PhoneRecord record = Fake.Create(seed).Phone().Record();
        string expected = Fake.Create(seed).Phone().National();
        string categoryAlias = Fake.Create(seed).Phone().RecordFromCategory(PhoneNumberCategory.General).Number;
        string formatAlias = Fake.Create(seed).Phone().RecordFromFormat(PhoneNumberFormat.National).Number;
        string digits = Digits(record.Number);

        Assert.Multiple(() =>
        {
            Assert.That(record.Number, Is.EqualTo(expected));
            Assert.That(categoryAlias, Is.EqualTo(expected));
            Assert.That(formatAlias, Is.EqualTo(expected));
            Assert.That(record.AreaCode, Is.EqualTo(digits.Substring(0, 3)));
            Assert.That(record.SubscriberNumber, Is.EqualTo(digits.Substring(3)));
            Assert.That(record.NationalSignificantNumber,
                Is.EqualTo(record.AreaCode + record.SubscriberNumber));
            Assert.That(record.NationalSignificantNumber, Is.EqualTo(digits));
            Assert.That(record.Extension, Is.Empty);
            Assert.That(record.HasExtension, Is.False);
            Assert.That(record.ParsingMetadata.RegionCode, Is.EqualTo("US"));
            Assert.That(record.ParsingMetadata.CallingCode, Is.EqualTo("+1"));
            Assert.That(record.ParsingMetadata.NationalPrefix, Is.EqualTo("1"));
            Assert.That(record.ParsingMetadata.Category, Is.EqualTo(PhoneNumberCategory.General));
            Assert.That(record.ParsingMetadata.Format, Is.EqualTo(PhoneNumberFormat.National));
            Assert.That(record.ParsingMetadata.FormatMask, Is.EqualTo("(N##) 555-01##"));
            Assert.That(record.ParsingMetadata.AreaCodeMask, Is.EqualTo("N##"));
            Assert.That(record.ParsingMetadata.SubscriberMask, Is.EqualTo("N##-####"));
            Assert.That(record.ParsingMetadata.ExtensionPrefix, Is.EqualTo(" ext. "));
            Assert.That(record.ParsingMetadata.CategoryDistinguishableByNumber, Is.False);
            Assert.That(record.ParsingMetadata.AllowsExtension, Is.True);
            Assert.That(record.SafetyMetadata.AppliedMode, Is.EqualTo(SafetyMode.Reserved));
            Assert.That(record.SafetyMetadata.UsesAuthoritativeSafeRange, Is.True);
        });
    }

    [Test]
    public void EveryRecordPresentationMatchesItsDirectGenerationApi()
    {
        foreach (PhoneNumberCategory category in Enum.GetValues<PhoneNumberCategory>())
        {
            foreach (PhoneNumberFormat format in Enum.GetValues<PhoneNumberFormat>())
            {
                long seed = 9310 + (long)category * 10 + (long)format;
                PhoneRecord record = PlausiblePhone(seed).RecordFromCategoryAndFormat(category, format);
                string expected = PlausiblePhone(seed).FormattedFromCategoryAndFormat(category, format);
                string digits = Digits(record.Number);
                int callingCodeDigits = format == PhoneNumberFormat.National ? 0 : 1;

                Assert.Multiple(() =>
                {
                    Assert.That(record.Number, Is.EqualTo(expected));
                    Assert.That(record.ParsingMetadata.Category, Is.EqualTo(category));
                    Assert.That(record.ParsingMetadata.Format, Is.EqualTo(format));
                    Assert.That(record.NationalSignificantNumber,
                        Is.EqualTo(digits.Substring(callingCodeDigits)));
                    Assert.That(record.NationalSignificantNumber,
                        Is.EqualTo(record.AreaCode + record.SubscriberNumber));
                });
            }
        }
    }

    [Test]
    public void AmbiguousNumberShapesRetainTheirRequestedCategoryMetadata()
    {
        const long seed = 9320;
        PhoneRecord landline = PlausiblePhone(seed).RecordFromCategoryAndFormat(
            PhoneNumberCategory.Landline,
            PhoneNumberFormat.National);
        PhoneRecord mobile = PlausiblePhone(seed).RecordFromCategoryAndFormat(
            PhoneNumberCategory.Mobile,
            PhoneNumberFormat.National);
        PhoneRecord tollFree = PlausiblePhone(seed).RecordFromCategoryAndFormat(
            PhoneNumberCategory.TollFree,
            PhoneNumberFormat.National);

        Assert.Multiple(() =>
        {
            Assert.That(landline.Number, Is.EqualTo(mobile.Number));
            Assert.That(landline.ParsingMetadata.Category, Is.EqualTo(PhoneNumberCategory.Landline));
            Assert.That(mobile.ParsingMetadata.Category, Is.EqualTo(PhoneNumberCategory.Mobile));
            Assert.That(landline.ParsingMetadata.CategoryDistinguishableByNumber, Is.False);
            Assert.That(mobile.ParsingMetadata.CategoryDistinguishableByNumber, Is.False);
            Assert.That(tollFree.ParsingMetadata.CategoryDistinguishableByNumber, Is.True);
        });
    }

    [Test]
    public void ExtensionRecordsPreserveBaseComponentsAndExtensionMetadata()
    {
        const long seed = 9330;
        PhoneRecord record = PlausiblePhone(seed).RecordWithExtensionFromCategoryAndFormatAndExtensionLength(
            PhoneNumberCategory.TollFree,
            PhoneNumberFormat.International,
            4);
        string expected = PlausiblePhone(seed).FormattedWithExtension(
            PhoneNumberCategory.TollFree,
            PhoneNumberFormat.International,
            4);

        Assert.Multiple(() =>
        {
            Assert.That(record.Number, Is.EqualTo(expected));
            Assert.That(record.Number, Does.EndWith(" ext. " + record.Extension));
            Assert.That(record.Extension, Does.Match("^[0-9]{4}$"));
            Assert.That(record.HasExtension, Is.True);
            Assert.That(record.NationalSignificantNumber,
                Is.EqualTo(record.AreaCode + record.SubscriberNumber));
            Assert.That(record.ParsingMetadata.Category, Is.EqualTo(PhoneNumberCategory.TollFree));
            Assert.That(record.ParsingMetadata.Format, Is.EqualTo(PhoneNumberFormat.International));
        });

        Assert.That(
            () => Fake.Create(9331).Phone().RecordWithExtensionFromCategoryAndFormatAndExtensionLength(
                PhoneNumberCategory.General,
                PhoneNumberFormat.E164,
                4),
            Throws.TypeOf<ArgumentException>().With.Property("ParamName").EqualTo("format"));
    }

    [Test]
    public void CallerLocaleRecordPreservesCountryDialingAndLeadingTrunkContext()
    {
        const long seed = 9340;
        PhoneRecord record = BritishFake(seed).Phone().RecordFromCategoryAndFormat(
            PhoneNumberCategory.General,
            PhoneNumberFormat.National);
        string digits = Digits(record.Number);

        Assert.Multiple(() =>
        {
            Assert.That(record.Number, Does.Match("^0[0-9]{4} [0-9]{6}$"));
            Assert.That(record.ParsingMetadata.RegionCode, Is.EqualTo("GB"));
            Assert.That(record.ParsingMetadata.CallingCode, Is.EqualTo("+44"));
            Assert.That(record.ParsingMetadata.NationalPrefix, Is.EqualTo("0"));
            Assert.That(record.ParsingMetadata.FormatMask, Is.EqualTo("0#### ######"));
            Assert.That(record.AreaCode, Is.EqualTo(digits.Substring(1, 4)));
            Assert.That(record.SubscriberNumber, Is.EqualTo(digits.Substring(5)));
            Assert.That(record.NationalSignificantNumber, Is.EqualTo(digits.Substring(1)));
            Assert.That(record.SafetyMetadata.RequestedMode, Is.EqualTo(SafetyMode.SyntheticPlausible));
            Assert.That(record.SafetyMetadata.AppliedMode, Is.EqualTo(SafetyMode.SyntheticPlausible));
            Assert.That(record.SafetyMetadata.UsedFallback, Is.False);
        });
    }

    [Test]
    public void PhoneRecordsAreDeterministicAndMetadataResultsAreIsolated()
    {
        PhoneProvider first = Fake.Create(9350).Phone();
        PhoneProvider second = Fake.Create(9350).Phone();
        PhoneRecord firstRecord = first.Record();
        PhoneRecord matching = second.Record();

        firstRecord.ParsingMetadata.RegionCode = "XX";
        firstRecord.SafetyMetadata.FallbackReason = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(firstRecord.Number, Is.EqualTo(matching.Number));
            Assert.That(firstRecord.AreaCode, Is.EqualTo(matching.AreaCode));
            Assert.That(firstRecord.SubscriberNumber, Is.EqualTo(matching.SubscriberNumber));
            Assert.That(matching.ParsingMetadata.RegionCode, Is.EqualTo("US"));
            Assert.That(matching.SafetyMetadata.FallbackReason, Is.Empty);
        });
    }

    [Test]
    public void PhoneRecordApisRejectInvalidInputsAndHonorOutputLimits()
    {
        FakeOptions options = FakeOptions.Create(9360);
        options.MaximumOutputSize = 10;
        PhoneProvider phone = Fake.Create(9361).Phone();

        Assert.That(
            () => phone.RecordFromCategoryAndFormat((PhoneNumberCategory)999, PhoneNumberFormat.National),
            Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("category"));
        Assert.That(
            () => phone.RecordFromCategoryAndFormat(PhoneNumberCategory.General, (PhoneNumberFormat)999),
            Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("format"));
        Assert.That(
            () => phone.RecordWithExtension(0),
            Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("length"));
        Assert.That(
            () => Fake.Create(options).Phone().Record(),
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
