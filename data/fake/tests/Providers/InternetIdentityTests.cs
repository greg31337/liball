using System.Globalization;
using fake;

namespace fake.Tests;

[TestFixture]
public class InternetIdentityTests
{
    [Test]
    public void UsernamesAreDeterministicLowercaseAsciiFixtures()
    {
        InternetProvider first = Fake.Create(8601).Internet();
        InternetProvider second = Fake.Create(8601).Internet();

        int index = 0;
        while (index < 512)
        {
            string value = first.Username();

            Assert.That(value, Is.EqualTo(second.Username()));
            Assert.That(value, Does.Match(@"^[a-z]+\.[a-z]+\.[0-9]{4}$"));
            Assert.That(value.Length, Is.InRange(15, 21));
            Assert.That(value.All(character => character <= 127), Is.True);
            index++;
        }
    }

    [Test]
    public void SocialHandlesAndHashtagsUseBoundedPlatformSafeShapes()
    {
        InternetProvider first = Fake.Create(8602).Internet();
        InternetProvider second = Fake.Create(8602).Internet();

        int index = 0;
        while (index < 512)
        {
            string handle = first.SocialHandle();
            string hashtag = first.Hashtag();

            Assert.That(handle, Is.EqualTo(second.SocialHandle()));
            Assert.That(hashtag, Is.EqualTo(second.Hashtag()));
            Assert.That(handle, Does.Match("^@[a-z]+_[a-z]+[0-9]{3}$"));
            Assert.That(hashtag, Does.Match("^#[a-z]+[a-z]+[0-9]{3}$"));
            Assert.That(handle.Length, Is.LessThanOrEqualTo(16));
            Assert.That(hashtag.Length, Is.LessThanOrEqualTo(15));
            index++;
        }
    }

    [Test]
    public void EmojiCatalogIncludesSequencesAndGenerationReturnsOneTextElement()
    {
        InternetProvider first = Fake.Create(8603).Internet();
        InternetProvider second = Fake.Create(8603).Internet();
        string[] catalog = first.Emojis();

        Assert.That(catalog, Does.Contain("😀"));
        Assert.That(catalog, Does.Contain("❤️"));
        Assert.That(catalog, Does.Contain("👩‍💻"));
        Assert.That(catalog, Does.Contain("🏳️‍🌈"));
        catalog[0] = "changed";
        Assert.That(first.Emojis()[0], Is.EqualTo("😀"));

        int index = 0;
        while (index < 256)
        {
            string value = first.Emoji();

            Assert.That(value, Is.EqualTo(second.Emoji()));
            Assert.That(first.Emojis(), Does.Contain(value));
            Assert.That(new StringInfo(value).LengthInTextElements, Is.EqualTo(1));
            index++;
        }
    }

    [Test]
    public void PasswordBridgePreservesLengthDeterminismAndRequiredClasses()
    {
        InternetProvider first = Fake.Create(8604).Internet();
        InternetProvider second = Fake.Create(8604).Internet();

        int index = 0;
        while (index < 256)
        {
            string value = first.Password();

            Assert.That(value, Is.EqualTo(second.Password()));
            Assert.That(value, Has.Length.EqualTo(16));
            Assert.That(value.Any(char.IsAsciiLetterLower), Is.True);
            Assert.That(value.Any(char.IsAsciiLetterUpper), Is.True);
            Assert.That(value.Any(char.IsAsciiDigit), Is.True);
            Assert.That(value.Any(character => !char.IsAsciiLetterOrDigit(character)), Is.True);
            index++;
        }
    }

    [Test]
    public void PasswordBridgeSupportsExplicitCharacterClassesAndValidation()
    {
        InternetProvider internet = Fake.Create(8605).Internet();
        string digits = internet.PasswordFromLengthAndLowercaseAndUppercaseAndDigitsAndSymbols(24, false, false, true, false);
        string mixed = internet.PasswordFromLengthAndLowercaseAndUppercaseAndDigitsAndSymbols(32, true, true, false, false);

        Assert.That(digits, Does.Match("^[0-9]{24}$"));
        Assert.That(mixed, Does.Match("^[A-Za-z]{32}$"));
        Assert.That(mixed.Any(char.IsAsciiLetterLower), Is.True);
        Assert.That(mixed.Any(char.IsAsciiLetterUpper), Is.True);
        Assert.That(
            () => internet.PasswordFromLength(3),
            Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("length"));
        Assert.That(
            () => internet.PasswordFromLengthAndLowercaseAndUppercaseAndDigitsAndSymbols(8, false, false, false, false),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void PassphraseBridgePreservesSharedWordAndSeparatorRules()
    {
        InternetProvider first = Fake.Create(8606).Internet();
        InternetProvider second = Fake.Create(8606).Internet();

        string standard = first.Passphrase();
        string custom = first.PassphraseFromWordCountAndSeparator(3, "🙂");

        Assert.That(standard, Is.EqualTo(second.Passphrase()));
        Assert.That(custom, Is.EqualTo(second.PassphraseFromWordCountAndSeparator(3, "🙂")));
        Assert.That(standard, Does.Match("^[a-z]{5}(-[a-z]{5}){3}$"));
        Assert.That(custom.Split("🙂"), Has.Length.EqualTo(3));
        Assert.That(custom.Replace("🙂", ""), Does.Match("^[a-z]{15}$"));
        Assert.That(first.PassphraseFromWordCount(0), Is.Empty);
        Assert.That(() => first.PassphraseFromWordCountAndSeparator(2, null), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void BrowserOperatingSystemAndDeviceValuesAreDeterministicFixtures()
    {
        InternetProvider first = Fake.Create(8607).Internet();
        InternetProvider second = Fake.Create(8607).Internet();

        int index = 0;
        while (index < 512)
        {
            string browser = first.Browser();
            string operatingSystem = first.OperatingSystem();
            string device = first.Device();

            Assert.That(browser, Is.EqualTo(second.Browser()));
            Assert.That(operatingSystem, Is.EqualTo(second.OperatingSystem()));
            Assert.That(device, Is.EqualTo(second.Device()));
            Assert.That(browser, Is.AnyOf("Chrome", "Firefox", "Safari"));
            Assert.That(operatingSystem, Is.AnyOf(
                "Windows NT 10.0", "Ubuntu", "Mac OS X 14_4",
                "iPhone OS 17_4", "Android 14"));
            Assert.That(device, Is.AnyOf(
                "Win64", "Linux x86_64", "Macintosh", "iPhone", "Pixel 8"));
            index++;
        }
    }

    [Test]
    public void AppOutputCorrelatesNameAndSemanticVersion()
    {
        InternetProvider combined = Fake.Create(8608).Internet();
        InternetProvider parts = Fake.Create(8608).Internet();

        int index = 0;
        while (index < 512)
        {
            string app = combined.App();
            string name = parts.AppName();
            string version = parts.AppVersion();

            Assert.That(app, Is.EqualTo(name + "/" + version));
            Assert.That(name, Is.AnyOf(
                "Demo Calendar", "Example Notes", "Fixture Chat", "Sample Player"));
            Assert.That(version, Does.Match(@"^[1-9]\.[0-9]\.[0-9]{1,2}$"));
            index++;
        }
    }

    [Test]
    public void BrowserUserAgentTextAndRecordsRemainCorrelated()
    {
        InternetProvider textInternet = Fake.Create(8609).Internet();
        InternetProvider recordInternet = Fake.Create(8609).Internet();
        InternetProvider duplicateInternet = Fake.Create(8609).Internet();

        int index = 0;
        while (index < 512)
        {
            string text = textInternet.UserAgent();
            UserAgentRecord record = recordInternet.UserAgentRecord();
            UserAgentRecord duplicate = duplicateInternet.UserAgentRecord();

            Assert.Multiple(() =>
            {
                Assert.That(record.UserAgent, Is.EqualTo(text));
                Assert.That(record.UserAgent, Is.EqualTo(duplicate.UserAgent));
                Assert.That(record.Browser, Is.EqualTo(duplicate.Browser));
                Assert.That(record.BrowserVersion, Is.EqualTo(duplicate.BrowserVersion));
                Assert.That(record.IsBot, Is.False);
                Assert.That(text, Does.Contain(record.Browser));
                Assert.That(text, Does.Contain(record.BrowserVersion));
                Assert.That(text, Does.Contain(record.OperatingSystem));
                Assert.That(text, Does.Contain(record.Device));
            });
            index++;
        }
    }

    [Test]
    public void BotUserAgentsAreExplicitAndUseReservedContactUrls()
    {
        InternetProvider textInternet = Fake.Create(8610).Internet();
        InternetProvider recordInternet = Fake.Create(8610).Internet();

        int index = 0;
        while (index < 256)
        {
            string text = textInternet.BotUserAgent();
            UserAgentRecord record = recordInternet.UserAgentRecordFromBot(true);

            Assert.That(record.UserAgent, Is.EqualTo(text));
            Assert.That(record.IsBot, Is.True);
            Assert.That(text, Does.Contain(record.Browser));
            Assert.That(text, Does.Contain(record.BrowserVersion));
            Assert.That(text, Does.Contain("Synthetic"));
            Assert.That(text, Does.Contain("Crawler"));
            Assert.That(text, Does.Contain("https://example.com/"));
            index++;
        }
    }

    [Test]
    public void IdentityOutputsHonorConfiguredCollectionAndOutputLimits()
    {
        FakeOptions options = FakeOptions.Create(8611);
        options.MaximumCount = 3;
        options.MaximumOutputSize = 10;
        InternetProvider internet = Fake.Create(options).Internet();

        Assert.That(() => internet.Username(), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => internet.SocialHandle(), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => internet.Hashtag(), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => internet.Emojis(), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => internet.Password(), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => internet.Passphrase(), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => internet.App(), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => internet.UserAgent(), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => internet.UserAgentRecord(), Throws.TypeOf<ArgumentOutOfRangeException>());
    }
}
