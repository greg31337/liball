namespace odf;

internal static class OdfVersionRules
{
    internal const string StandardProfile = "standard";
    internal const string GenericExtendedProfile = "extended";
    internal const string LibreOfficeExtendedProfile = "libreoffice-extended";
    internal const string OpenOfficeExtendedProfile = "openoffice-extended";
    internal const string MicrosoftOfficeExtendedProfile = "microsoft-office-extended";
    internal const string CalligraExtendedProfile = "calligra-extended";

    internal static OdfVersionInfo ParseVersion(string? lexicalValue)
    {
        OdfVersionInfo result = new OdfVersionInfo();
        if (lexicalValue == null)
        {
            return result;
        }
        result.LexicalValue = lexicalValue;
        if (lexicalValue.Length < 3)
        {
            return result;
        }

        int separator = lexicalValue.IndexOf('.');
        if (separator <= 0 || separator == lexicalValue.Length - 1 || separator != lexicalValue.LastIndexOf('.'))
        {
            return result;
        }
        if (separator > 1 && lexicalValue[0] == '0')
        {
            return result;
        }
        if (lexicalValue.Length - separator - 1 > 1 && lexicalValue[separator + 1] == '0')
        {
            return result;
        }

        int major = ParseVersionNumber(lexicalValue, 0, separator);
        int minor = ParseVersionNumber(lexicalValue, separator + 1, lexicalValue.Length);
        if (major < 0 || minor < 0)
        {
            return result;
        }

        result.Major = major;
        result.Minor = minor;
        result.IsValid = true;
        if (major == 1)
        {
            if (minor == 0)
            {
                result.Version = OdfVersion.Version10;
            }
            else if (minor == 1)
            {
                result.Version = OdfVersion.Version11;
            }
            else if (minor == 2)
            {
                result.Version = OdfVersion.Version12;
            }
            else if (minor == 3)
            {
                result.Version = OdfVersion.Version13;
            }
            else if (minor == 4)
            {
                result.Version = OdfVersion.Version14;
            }
        }

        if (major > 1 || major == 1 && minor > 4)
        {
            result.Version = OdfVersion.Future;
            result.IsFuture = true;
        }
        return result;
    }

    internal static string FormatVersion(OdfVersionInfo? value)
    {
        if (value == null)
        {
            return "";
        }
        if (value.Version == OdfVersion.Version10)
        {
            return "1.0";
        }
        if (value.Version == OdfVersion.Version11)
        {
            return "1.1";
        }
        if (value.Version == OdfVersion.Version12)
        {
            return "1.2";
        }
        if (value.Version == OdfVersion.Version13)
        {
            return "1.3";
        }
        if (value.Version == OdfVersion.Version14)
        {
            return "1.4";
        }
        if (value.LexicalValue.Length > 0)
        {
            return value.LexicalValue;
        }
        if (value.IsValid)
        {
            return $"{value.Major}.{value.Minor}";
        }
        return "";
    }

    internal static bool ApplyConformanceClass(OdfVersionInfo? value, string? lexicalValue)
    {
        if (value == null)
        {
            return false;
        }
        value.ConformanceLexicalValue = lexicalValue ?? "";
        value.ConformanceClass = OdfConformanceClass.Unknown;
        if (lexicalValue == "strict")
        {
            value.ConformanceClass = OdfConformanceClass.Strict;
            return true;
        }
        if (lexicalValue == "conforming")
        {
            value.ConformanceClass = OdfConformanceClass.Conforming;
            return true;
        }
        if (lexicalValue == "extended" || lexicalValue == "extended-conforming")
        {
            value.ConformanceClass = OdfConformanceClass.Extended;
            return true;
        }
        return false;
    }

    internal static string FormatConformanceClass(OdfConformanceClass value)
    {
        if (value == OdfConformanceClass.Strict)
        {
            return "strict";
        }
        if (value == OdfConformanceClass.Conforming)
        {
            return "conforming";
        }
        if (value == OdfConformanceClass.Extended)
        {
            return "extended";
        }
        return "";
    }

    internal static void ApplyProducerProfile(OdfVersionInfo? value, string? producer)
    {
        if (value == null)
        {
            return;
        }
        value.Producer = producer ?? "";
        if (value.ConformanceClass != OdfConformanceClass.Extended)
        {
            value.ProducerProfile = StandardProfile;
            return;
        }
        if (producer == null || producer.Length == 0)
        {
            value.ProducerProfile = GenericExtendedProfile;
            return;
        }
        if (ContainsOrdinalIgnoreCase(producer, "LibreOffice") || ContainsOrdinalIgnoreCase(producer, "Collabora"))
        {
            value.ProducerProfile = LibreOfficeExtendedProfile;
            return;
        }
        if (ContainsOrdinalIgnoreCase(producer, "OpenOffice") || ContainsOrdinalIgnoreCase(producer, "StarOffice"))
        {
            value.ProducerProfile = OpenOfficeExtendedProfile;
            return;
        }
        if (ContainsOrdinalIgnoreCase(producer, "Microsoft"))
        {
            value.ProducerProfile = MicrosoftOfficeExtendedProfile;
            return;
        }
        if (ContainsOrdinalIgnoreCase(producer, "Calligra") || ContainsOrdinalIgnoreCase(producer, "KOffice"))
        {
            value.ProducerProfile = CalligraExtendedProfile;
            return;
        }
        value.ProducerProfile = GenericExtendedProfile;
    }

    internal static int CompareVersions(OdfVersionInfo? left, OdfVersionInfo? right)
    {
        if (left == null)
        {
            if (right == null)
            {
                return 0;
            }
            return -1;
        }
        if (right == null)
        {
            return 1;
        }
        if (left.Major < right.Major)
        {
            return -1;
        }
        if (left.Major > right.Major)
        {
            return 1;
        }
        if (left.Minor < right.Minor)
        {
            return -1;
        }
        if (left.Minor > right.Minor)
        {
            return 1;
        }
        return 0;
    }

    internal static bool IsAtLeastVersion(OdfVersionInfo? value, OdfVersion minimum)
    {
        if (value == null || !value.IsValid)
        {
            return false;
        }
        int requiredMinor = KnownMinorVersion(minimum);
        if (requiredMinor < 0)
        {
            return false;
        }
        if (value.Major > 1)
        {
            return true;
        }
        return value.Major == 1 && value.Minor >= requiredMinor;
    }

    static int ParseVersionNumber(string value, int start, int end)
    {
        int result = 0;
        int index = start;
        while (index < end)
        {
            int digit = value[index] - '0';
            if (digit < 0 || digit > 9)
            {
                return -1;
            }
            if (result > (int.MaxValue - digit) / 10)
            {
                return -1;
            }
            result = result * 10 + digit;
            index = index + 1;
        }
        return result;
    }

    static bool ContainsOrdinalIgnoreCase(string value, string candidate)
    {
        return value.IndexOf(candidate, global::System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static int KnownMinorVersion(OdfVersion value)
    {
        if (value == OdfVersion.Version10)
        {
            return 0;
        }
        if (value == OdfVersion.Version11)
        {
            return 1;
        }
        if (value == OdfVersion.Version12)
        {
            return 2;
        }
        if (value == OdfVersion.Version13)
        {
            return 3;
        }
        if (value == OdfVersion.Version14)
        {
            return 4;
        }
        return -1;
    }
}
