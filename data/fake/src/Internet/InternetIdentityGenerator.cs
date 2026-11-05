using System;
using System.Globalization;

namespace fake;

internal class UserAgentFixture
{
    internal string Browser = string.Empty;
    internal string Version = string.Empty;
    internal string OperatingSystem = string.Empty;
    internal string Device = string.Empty;
    internal string Text = string.Empty;
    internal bool IsBot;

    internal UserAgentFixture(
        string browser,
        string version,
        string operatingSystem,
        string device,
        string text,
        bool isBot)
    {
        Browser = browser;
        Version = version;
        OperatingSystem = operatingSystem;
        Device = device;
        Text = text;
        IsBot = isBot;
    }
}

internal static class InternetIdentityGenerator
{
    static readonly string[] UsernameAdjectives = new string[]
    {
        "bright", "calm", "clever", "cosmic", "gentle",
        "lucky", "mellow", "quiet", "rapid", "vivid"
    };

    static readonly string[] UsernameNouns = new string[]
    {
        "badger", "falcon", "garden", "harbor", "lantern",
        "meadow", "otter", "planet", "river", "willow"
    };

    static readonly string[] HandleAdjectives = new string[]
    {
        "calm", "kind", "lunar", "merry", "nimble", "quiet"
    };

    static readonly string[] HandleNouns = new string[]
    {
        "fox", "owl", "panda", "river", "star"
    };

    static readonly string[] EmojiValues = new string[]
    {
        "😀", "🌟", "🚀", "🧪", "📚", "🌈", "👍", "❤️", "👩‍💻", "🏳️‍🌈"
    };

    static readonly string[] AppNames = new string[]
    {
        "Demo Calendar", "Example Notes", "Fixture Chat", "Sample Player"
    };

    static readonly UserAgentFixture[] HumanAgents = new UserAgentFixture[]
    {
        new UserAgentFixture(
            "Chrome",
            "124.0.0.0",
            "Windows NT 10.0",
            "Win64",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36",
            false),
        new UserAgentFixture(
            "Firefox",
            "125.0",
            "Ubuntu",
            "Linux x86_64",
            "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:125.0) Gecko/20100101 Firefox/125.0",
            false),
        new UserAgentFixture(
            "Safari",
            "17.4",
            "Mac OS X 14_4",
            "Macintosh",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 14_4) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.4 Safari/605.1.15",
            false),
        new UserAgentFixture(
            "Safari",
            "17.4",
            "iPhone OS 17_4",
            "iPhone",
            "Mozilla/5.0 (iPhone; CPU iPhone OS 17_4 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.4 Mobile/15E148 Safari/604.1",
            false),
        new UserAgentFixture(
            "Chrome",
            "124.0.0.0",
            "Android 14",
            "Pixel 8",
            "Mozilla/5.0 (Linux; Android 14; Pixel 8) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Mobile Safari/537.36",
            false)
    };

    static readonly UserAgentFixture[] BotAgents = new UserAgentFixture[]
    {
        new UserAgentFixture(
            "ExampleBot",
            "1.0",
            "Synthetic",
            "Crawler",
            "ExampleBot/1.0 (Synthetic; Crawler; +https://example.com/bot)",
            true),
        new UserAgentFixture(
            "FixtureCrawler",
            "2.1",
            "Synthetic",
            "Crawler",
            "FixtureCrawler/2.1 (Synthetic; Crawler; +https://example.com/crawler)",
            true)
    };

    internal static string Username(FakeRandom random, int maximumOutputSize)
    {
        string result = UsernameAdjectives[random.IntFromMaximumExclusive(UsernameAdjectives.Length)] +
            "." +
            UsernameNouns[random.IntFromMaximumExclusive(UsernameNouns.Length)] +
            "." +
            random.IntFromMaximumExclusive(10000).ToString("D4", CultureInfo.InvariantCulture);
        Guard.OutputSize(result.Length, maximumOutputSize, "username");
        return result;
    }

    internal static string SocialHandle(FakeRandom random, int maximumOutputSize)
    {
        string result = "@" +
            HandleAdjectives[random.IntFromMaximumExclusive(HandleAdjectives.Length)] +
            "_" +
            HandleNouns[random.IntFromMaximumExclusive(HandleNouns.Length)] +
            random.IntFromMaximumExclusive(1000).ToString("D3", CultureInfo.InvariantCulture);
        Guard.OutputSize(result.Length, maximumOutputSize, "socialHandle");
        return result;
    }

    internal static string Hashtag(FakeRandom random, int maximumOutputSize)
    {
        string result = "#" +
            HandleAdjectives[random.IntFromMaximumExclusive(HandleAdjectives.Length)] +
            HandleNouns[random.IntFromMaximumExclusive(HandleNouns.Length)] +
            random.IntFromMaximumExclusive(1000).ToString("D3", CultureInfo.InvariantCulture);
        Guard.OutputSize(result.Length, maximumOutputSize, "hashtag");
        return result;
    }

    internal static string Emoji(FakeRandom random, int maximumOutputSize)
    {
        string result = EmojiValues[random.IntFromMaximumExclusive(EmojiValues.Length)];
        Guard.OutputSize(result.Length, maximumOutputSize, "emoji");
        return result;
    }

    internal static string[] Emojis(int maximumCount, int maximumOutputSize)
    {
        Guard.Count(EmojiValues.Length, maximumCount, "emojis");
        string[] result = new string[EmojiValues.Length];
        Array.Copy(EmojiValues, result, result.Length);
        int total = 0;
        int index = 0;
        while (index < result.Length)
        {
            total = total + result[index].Length;
            index = index + 1;
        }
        Guard.OutputSize(total, maximumOutputSize, "emojis");
        return result;
    }

    internal static string Browser(FakeRandom random, int maximumOutputSize)
    {
        string result = HumanFixture(random).Browser;
        Guard.OutputSize(result.Length, maximumOutputSize, "browser");
        return result;
    }

    internal static string OperatingSystem(FakeRandom random, int maximumOutputSize)
    {
        string result = HumanFixture(random).OperatingSystem;
        Guard.OutputSize(result.Length, maximumOutputSize, "operatingSystem");
        return result;
    }

    internal static string Device(FakeRandom random, int maximumOutputSize)
    {
        string result = HumanFixture(random).Device;
        Guard.OutputSize(result.Length, maximumOutputSize, "device");
        return result;
    }

    internal static string AppName(FakeRandom random, int maximumOutputSize)
    {
        string result = AppNames[random.IntFromMaximumExclusive(AppNames.Length)];
        Guard.OutputSize(result.Length, maximumOutputSize, "appName");
        return result;
    }

    internal static string AppVersion(FakeRandom random, int maximumOutputSize)
    {
        string result = random.IntFromMinimumAndMaximum(1, 10).ToString(CultureInfo.InvariantCulture) +
            "." +
            random.IntFromMaximumExclusive(10).ToString(CultureInfo.InvariantCulture) +
            "." +
            random.IntFromMaximumExclusive(20).ToString(CultureInfo.InvariantCulture);
        Guard.OutputSize(result.Length, maximumOutputSize, "appVersion");
        return result;
    }

    internal static string App(FakeRandom random, int maximumOutputSize)
    {
        string name = AppName(random, maximumOutputSize);
        string version = AppVersion(random, maximumOutputSize);
        string result = name + "/" + version;
        Guard.OutputSize(result.Length, maximumOutputSize, "app");
        return result;
    }

    internal static string UserAgent(
        FakeRandom random,
        bool bot,
        int maximumOutputSize)
    {
        UserAgentFixture fixture = Fixture(random, bot);
        Guard.OutputSize(fixture.Text.Length, maximumOutputSize, "userAgent");
        return fixture.Text;
    }

    internal static UserAgentRecord UserAgentRecord(
        FakeRandom random,
        bool bot,
        int maximumOutputSize)
    {
        UserAgentFixture fixture = Fixture(random, bot);
        Guard.OutputSize(fixture.Browser.Length, maximumOutputSize, "userAgent");
        Guard.OutputSize(fixture.Version.Length, maximumOutputSize, "userAgent");
        Guard.OutputSize(fixture.OperatingSystem.Length, maximumOutputSize, "userAgent");
        Guard.OutputSize(fixture.Device.Length, maximumOutputSize, "userAgent");
        Guard.OutputSize(fixture.Text.Length, maximumOutputSize, "userAgent");
        UserAgentRecord result = new UserAgentRecord();
        result.Browser = fixture.Browser;
        result.BrowserVersion = fixture.Version;
        result.OperatingSystem = fixture.OperatingSystem;
        result.Device = fixture.Device;
        result.UserAgent = fixture.Text;
        result.IsBot = fixture.IsBot;
        return result;
    }

    static UserAgentFixture HumanFixture(FakeRandom random)
    {
        return HumanAgents[random.IntFromMaximumExclusive(HumanAgents.Length)];
    }

    static UserAgentFixture Fixture(FakeRandom random, bool bot)
    {
        if (bot)
        {
            return BotAgents[random.IntFromMaximumExclusive(BotAgents.Length)];
        }
        return HumanFixture(random);
    }
}
