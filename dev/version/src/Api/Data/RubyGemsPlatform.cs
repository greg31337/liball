namespace version;

/// <summary>
/// Represents a rubygems platform.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class RubyGemsPlatform
{
    /// <summary>
    /// Stores the <c>value</c> value.
    /// </summary>
    public string value;
    /// <summary>
    /// Stores the <c>cpu</c> value.
    /// </summary>
    public string cpu;
    /// <summary>
    /// Stores the <c>os</c> value.
    /// </summary>
    public string os;
    /// <summary>
    /// Stores the <c>platformVersion</c> value.
    /// </summary>
    public string platformVersion;
    /// <summary>
    /// Stores the <c>ruby</c> value.
    /// </summary>
    public bool ruby;
    /// <summary>
    /// Stores the <c>universal</c> value.
    /// </summary>
    public bool universal;

    /// <summary>
    /// Initializes a new RubyGems Platform instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="valueText">The value text to process.</param>
    public RubyGemsPlatform(string valueText)
    {
        ValidatePlatformText(valueText, "RubyGems platform cannot be empty or contain invalid characters.");

        value = valueText;
        ruby = valueText == "ruby";
        universal = StartsWithText(valueText, "universal-") || valueText == "universal";
        cpu = "";
        os = "";
        platformVersion = "";

        ParsePlatformParts(valueText);
    }

    /// <summary>
    /// Creates a ruby.
    /// </summary>
    /// <returns>The resulting rubygems platform.</returns>
    internal static RubyGemsPlatform CreateRuby()
    {
        return new RubyGemsPlatform("ruby");
    }

    /// <summary>
    /// Creates an universal.
    /// </summary>
    /// <param name="osText">The os text to process.</param>
    /// <returns>The resulting rubygems platform.</returns>
    internal static RubyGemsPlatform CreateUniversal(string osText)
    {
        ValidatePlatformText(osText, "RubyGems universal platform OS cannot contain invalid characters.");

        return new RubyGemsPlatform("universal-" + osText);
    }

    /// <summary>
    /// Gets a value.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetValue()
    {
        return value;
    }

    /// <summary>
    /// Gets a cpu.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetCpu()
    {
        return cpu;
    }

    /// <summary>
    /// Reports whether this value has cpu.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasCpu()
    {
        return cpu.Length > 0;
    }

    /// <summary>
    /// Gets an os.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetOs()
    {
        return os;
    }

    /// <summary>
    /// Reports whether this value has version.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasVersion()
    {
        return platformVersion.Length > 0;
    }

    /// <summary>
    /// Gets a version.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetVersion()
    {
        return platformVersion;
    }

    /// <summary>
    /// Reports whether ruby.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsRuby()
    {
        return ruby;
    }

    /// <summary>
    /// Reports whether universal.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsUniversal()
    {
        return universal;
    }

    /// <summary>
    /// Reports whether this value matches the supplied text under its ecosystem comparison rules.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal bool MatchesText(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        return value == text;
    }

    /// <summary>
    /// Reports whether two RubyGems platforms have the same normalized identity.
    /// </summary>
    /// <param name="other">The other used by the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal bool EqualsPlatform(RubyGemsPlatform other)
    {
        System.ArgumentNullException.ThrowIfNull(other);

        return cpu == other.GetCpu()
            && os == other.GetOs()
            && platformVersion == other.GetVersion();
    }

    /// <summary>
    /// Reports whether this RubyGems platform is compatible with the supplied platform.
    /// </summary>
    /// <param name="localPlatform">The local platform used by the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal bool MatchesPlatform(RubyGemsPlatform localPlatform)
    {
        System.ArgumentNullException.ThrowIfNull(localPlatform);

        if (IsRuby() || localPlatform.IsRuby())
        {
            return IsRuby() && localPlatform.IsRuby();
        }

        if (!CpuMatches(localPlatform))
        {
            return false;
        }

        if (!OsMatches(localPlatform))
        {
            return false;
        }

        return VersionMatches(localPlatform);
    }

    internal void ParsePlatformParts(string text)
    {
        if (ruby)
        {
            os = "ruby";
            return;
        }

        string[] parts = Split(text, '-');
        if (parts.Length == 1)
        {
            if (universal)
            {
                cpu = "universal";
            }
            else
            {
                os = parts[0];
            }

            return;
        }

        cpu = parts[0];
        os = parts[1];

        if (parts.Length > 2)
        {
            platformVersion = JoinTail(parts, 2);
        }
    }

    internal bool CpuMatches(RubyGemsPlatform localPlatform)
    {
        if (cpu.Length == 0 || cpu == "universal")
        {
            return true;
        }

        if (localPlatform.GetCpu().Length == 0 || localPlatform.GetCpu() == "universal")
        {
            return true;
        }

        if (cpu == localPlatform.GetCpu())
        {
            return true;
        }

        if (cpu == "arm" && StartsWithText(localPlatform.GetCpu(), "armv"))
        {
            return true;
        }

        return false;
    }

    internal bool OsMatches(RubyGemsPlatform localPlatform)
    {
        if ((cpu == "universal" || localPlatform.GetCpu() == "universal")
            && StartsWithText(os, "mingw")
            && StartsWithText(localPlatform.GetOs(), "mingw"))
        {
            return true;
        }

        return os == localPlatform.GetOs();
    }

    internal bool VersionMatches(RubyGemsPlatform localPlatform)
    {
        if (os == "linux")
        {
            return LinuxVersionMatches(localPlatform);
        }

        return platformVersion.Length == 0
            || localPlatform.GetVersion().Length == 0
            || platformVersion == localPlatform.GetVersion();
    }

    internal bool LinuxVersionMatches(RubyGemsPlatform localPlatform)
    {
        string localVersion = NormalizeLinuxVersion(localPlatform.GetVersion());
        string gemVersion = NormalizeLinuxVersion(platformVersion);

        if (gemVersion == localVersion)
        {
            return true;
        }

        if (platformVersion.Length > 0)
        {
            if (localPlatform.GetVersion() == "musl" + platformVersion)
            {
                return true;
            }

            if (localPlatform.GetVersion() == "musleabi" + platformVersion)
            {
                return true;
            }

            if (localPlatform.GetVersion() == "musleabihf" + platformVersion)
            {
                return true;
            }
        }

        return false;
    }

    internal static string NormalizeLinuxVersion(string valueText)
    {
        if (valueText.Length == 0)
        {
            return "gnu";
        }

        return valueText;
    }

    internal static string[] Split(string text, char delimiter)
    {
        System.Collections.Generic.List<string> parts = new System.Collections.Generic.List<string>();
        int startIndex = 0;
        int index = 0;

        while (index < text.Length)
        {
            if (text[index] == delimiter)
            {
                parts.Add(text.Substring(startIndex, index - startIndex));
                startIndex = index + 1;
            }

            index = index + 1;
        }

        parts.Add(text.Substring(startIndex));
        return parts.ToArray();
    }

    internal static string JoinTail(string[] parts, int startIndex)
    {
        string valueText = "";
        int index = startIndex;

        while (index < parts.Length)
        {
            if (valueText.Length > 0)
            {
                valueText = valueText + "-";
            }

            valueText = valueText + parts[index];
            index = index + 1;
        }

        return valueText;
    }

    internal static bool StartsWithText(string text, string valueText)
    {
        if (text.Length < valueText.Length)
        {
            return false;
        }

        return text.Substring(0, valueText.Length) == valueText;
    }

    /// <summary>
    /// Validates a platform text.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="message">The message used by the operation.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentException">An argument is malformed or inconsistent with the requested operation.</exception>
    internal static void ValidatePlatformText(string text, string message)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        if (text.Length == 0)
        {
            throw new System.ArgumentException(message);
        }

        int index = 0;
        while (index < text.Length)
        {
            if (!VersionChar.IsAlphaNumeric(text[index])
                && text[index] != '-'
                && text[index] != '_'
                && text[index] != '.')
            {
                throw new System.ArgumentException(message);
            }

            index = index + 1;
        }
    }
}
