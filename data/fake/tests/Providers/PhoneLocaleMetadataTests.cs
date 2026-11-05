using fake;

namespace fake.Tests;

[TestFixture]
public class PhoneLocaleMetadataTests
{
    [Test]
    public void DefaultCatalogExposesNanpCallingAreaAndExtensionRules()
    {
        PhoneProvider phone = Fake.Create(9001).Phone();
        PhoneLocaleMetadata metadata = phone.Metadata();

        Assert.Multiple(() =>
        {
            Assert.That(metadata.RegionCode, Is.EqualTo("US"));
            Assert.That(metadata.CallingCode, Is.EqualTo("+1"));
            Assert.That(metadata.InternationalDialingPrefix, Is.EqualTo("011"));
            Assert.That(metadata.NationalPrefix, Is.EqualTo("1"));
            Assert.That(metadata.AreaCodeMask, Is.EqualTo("N##"));
            Assert.That(metadata.SubscriberMask, Is.EqualTo("N##-####"));
            Assert.That(metadata.ExtensionPrefix, Is.EqualTo(" ext. "));
            Assert.That(metadata.MinimumExtensionDigits, Is.EqualTo(1));
            Assert.That(metadata.MaximumExtensionDigits, Is.EqualTo(6));
            Assert.That(phone.CallingCode(), Is.EqualTo(metadata.CallingCode));
            Assert.That(phone.CountryCode(), Is.EqualTo(metadata.CallingCode));
            Assert.That(phone.InternationalDialingPrefix(), Is.EqualTo(metadata.InternationalDialingPrefix));
            Assert.That(phone.NationalPrefix(), Is.EqualTo(metadata.NationalPrefix));
            Assert.That(phone.AreaCodeMask(), Is.EqualTo(metadata.AreaCodeMask));
            Assert.That(phone.SubscriberMask(), Is.EqualTo(metadata.SubscriberMask));
            Assert.That(phone.ExtensionPrefix(), Is.EqualTo(metadata.ExtensionPrefix));
        });
    }

    [Test]
    public void EveryPhoneCategoryHasValidatedCorrelatedFormatRules()
    {
        PhoneProvider phone = Fake.Create(9002).Phone();
        PhoneNumberCategory[] categories = phone.Categories();

        Assert.That(categories, Is.EqualTo(new PhoneNumberCategory[]
        {
            PhoneNumberCategory.General,
            PhoneNumberCategory.Landline,
            PhoneNumberCategory.Mobile,
            PhoneNumberCategory.TollFree,
            PhoneNumberCategory.Premium
        }));

        foreach (PhoneNumberCategory category in categories)
        {
            PhoneFormatRule rule = phone.Format(category);
            Assert.Multiple(() =>
            {
                Assert.That(rule.Category, Is.EqualTo(category));
                Assert.That(rule.AreaCodeMask, Is.Not.Empty);
                Assert.That(rule.SubscriberMask, Is.Not.Empty);
                Assert.That(rule.NationalMask, Does.Match("^[0-9N# ().-]+$"));
                Assert.That(rule.InternationalMask, Does.StartWith("+1 "));
                Assert.That(rule.E164Mask, Does.Match("^\\+1[0-9N#]+$"));
                Assert.That(rule.NationalMask.Any(value => value == 'N' || value == '#'), Is.True);
                Assert.That(rule.E164Mask.Substring(2), Has.Length.EqualTo(10));
                Assert.That(rule.AllowsExtension, Is.True);
            });
        }

        Assert.That(phone.Format(PhoneNumberCategory.Landline).DistinguishableByNumber, Is.False);
        Assert.That(phone.Format(PhoneNumberCategory.Mobile).DistinguishableByNumber, Is.False);
        Assert.That(phone.Format(PhoneNumberCategory.TollFree).AreaCodes,
            Is.EqualTo(new string[] { "800", "833", "844", "855", "866", "877", "888" }));
        Assert.That(phone.Format(PhoneNumberCategory.TollFree).DistinguishableByNumber, Is.True);
        Assert.That(phone.Format(PhoneNumberCategory.Premium).AreaCodes, Is.EqualTo(new string[] { "900" }));
        Assert.That(phone.Format(PhoneNumberCategory.Premium).DistinguishableByNumber, Is.True);
    }

    [Test]
    public void NanpaSafeRangeRetainsTheExactAuthoritativeFictionBoundary()
    {
        PhoneLocaleMetadata metadata = Fake.Create(9003).Phone().Metadata();
        PhoneSafeRange range = metadata.SafeRanges.Single();

        Assert.Multiple(() =>
        {
            Assert.That(metadata.HasAuthoritativeSafeRange, Is.True);
            Assert.That(range.Category, Is.EqualTo(PhoneNumberCategory.General));
            Assert.That(range.AreaCodeMask, Is.EqualTo("N##"));
            Assert.That(range.Exchange, Is.EqualTo("555"));
            Assert.That(range.FirstSubscriber, Is.EqualTo("0100"));
            Assert.That(range.LastSubscriber, Is.EqualTo("0199"));
            Assert.That(range.NationalMask, Is.EqualTo("(N##) 555-01##"));
            Assert.That(range.InternationalMask, Is.EqualTo("+1 N## 555 01##"));
            Assert.That(range.E164Mask, Is.EqualTo("+1N##55501##"));
            Assert.That(range.Authority, Is.EqualTo("NANPA 555 line-number reservation"));
            Assert.That(range.SourceVersion, Is.EqualTo("NANPA page retrieved 2026-08-14"));
            Assert.That(range.IsAuthoritative, Is.True);
        });
    }

    [Test]
    public void MetadataResultsAndTheirNestedArraysAreIsolated()
    {
        PhoneProvider phone = Fake.Create(9004).Phone();
        PhoneLocaleMetadata first = phone.Metadata();
        PhoneLocaleMetadata second = phone.Metadata();
        PhoneNumberCategory[] firstCategories = phone.Categories();
        PhoneNumberCategory[] secondCategories = phone.Categories();

        first.RegionCode = "XX";
        first.Formats[0].NationalMask = "changed";
        first.Formats[3].AreaCodes[0] = "000";
        first.SafeRanges[0].Exchange = "000";
        firstCategories[0] = PhoneNumberCategory.Premium;

        Assert.Multiple(() =>
        {
            Assert.That(second.RegionCode, Is.EqualTo("US"));
            Assert.That(second.Formats[0].NationalMask, Is.EqualTo("(N##) N##-####"));
            Assert.That(second.Formats[3].AreaCodes[0], Is.EqualTo("800"));
            Assert.That(second.SafeRanges[0].Exchange, Is.EqualTo("555"));
            Assert.That(secondCategories[0], Is.EqualTo(PhoneNumberCategory.General));
        });
    }

    [Test]
    public void CallerLocaleCatalogCanDefineDifferentTrunkMasksAndNoSafeRange()
    {
        Fake fake = Fake.Create(FakeOptions.Create("en-GB", 9005));
        fake.Locale().RegisterFromLocaleAndFields("en-GB", new Dictionary<string, object>
        {
            ["phone.catalog"] = BritishCatalog()
        });

        PhoneLocaleMetadata metadata = fake.Phone().Metadata();

        Assert.Multiple(() =>
        {
            Assert.That(metadata.RegionCode, Is.EqualTo("GB"));
            Assert.That(metadata.CallingCode, Is.EqualTo("+44"));
            Assert.That(metadata.InternationalDialingPrefix, Is.EqualTo("00"));
            Assert.That(metadata.NationalPrefix, Is.EqualTo("0"));
            Assert.That(metadata.AreaCodeMask, Is.EqualTo("####"));
            Assert.That(metadata.SubscriberMask, Is.EqualTo("######"));
            Assert.That(metadata.ExtensionPrefix, Is.EqualTo(" x"));
            Assert.That(metadata.MinimumExtensionDigits, Is.EqualTo(2));
            Assert.That(metadata.MaximumExtensionDigits, Is.EqualTo(5));
            Assert.That(metadata.Formats, Has.Length.EqualTo(1));
            Assert.That(metadata.Formats[0].E164Mask, Is.EqualTo("+44##########"));
            Assert.That(metadata.SafeRanges, Is.Empty);
            Assert.That(metadata.HasAuthoritativeSafeRange, Is.False);
        });
    }

    [Test]
    public void InvalidLocalePhoneRulesFailWithExplicitDiagnostics()
    {
        Fake fake = Fake.Create(FakeOptions.Create("en-GB", 9006));
        Dictionary<string, object> catalog = BritishCatalog();
        catalog["callingCode"] = "44";
        fake.Locale().RegisterFromLocaleAndFields("en-GB", new Dictionary<string, object>
        {
            ["phone.catalog"] = catalog
        });

        Assert.That(
            () => fake.Phone().Metadata(),
            Throws.TypeOf<FakeException>().With.Message.Contains("phone.catalog.callingCode"));
        Assert.That(
            () => Fake.Create(9006).Phone().Format((PhoneNumberCategory)999),
            Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("category"));
    }

    [Test]
    public void PhoneMetadataHonorsConfiguredCollectionAndOutputLimits()
    {
        FakeOptions countOptions = FakeOptions.Create(9007);
        countOptions.MaximumCount = 4;
        FakeOptions outputOptions = FakeOptions.Create(9007);
        outputOptions.MaximumOutputSize = 2;

        Assert.That(() => Fake.Create(countOptions).Phone().Metadata(), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => Fake.Create(outputOptions).Phone().Metadata(), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    static Dictionary<string, object> BritishCatalog()
    {
        return new Dictionary<string, object>
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
        };
    }
}
