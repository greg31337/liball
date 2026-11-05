using System;
using System.Globalization;
using System.Text;

namespace email;
/// <summary>
/// Represents a maildir name.
/// </summary>
public class EmailMaildirName : IEmailMaildirNameContract
{
    const int AllStandardFlags = 63;
    string fileName;
    string uniqueName;
    bool hasInfo;
    int infoVersion;
    string? infoText;
    string flagText;
    string unknownFlagText;
    EmailMaildirFlags flags;
    bool infoWellFormed;
    bool canonical;
    EmailMaildirName(string fileName, string uniqueName, bool hasInfo, int infoVersion, string? infoText, string flagText, string unknownFlagText, EmailMaildirFlags flags, bool infoWellFormed, bool canonical)
    {
        this.fileName = fileName;
        this.uniqueName = uniqueName;
        this.hasInfo = hasInfo;
        this.infoVersion = infoVersion;
        this.infoText = infoText;
        this.flagText = flagText;
        this.unknownFlagText = unknownFlagText;
        this.flags = flags;
        this.infoWellFormed = infoWellFormed;
        this.canonical = canonical;
    }

    internal static EmailMaildirName ParseImplementation(string fileName, EmailMaildirFolder folder)
    {
        ValidateFolder(folder);
        if (fileName == null)
            throw new ArgumentNullException("fileName");
        if (!IsSafeFileNameImplementation(fileName, folder))
            throw new ArgumentException("A Maildir filename must be one safe path segment with a valid unique-name portion.", "fileName");
        int separator = fileName.IndexOf(':');
        string unique = separator < 0 ? fileName : fileName.Substring(0, separator);
        if (separator < 0)
        {
            return new EmailMaildirName(fileName, unique, false, -1, null, string.Empty, string.Empty, EmailMaildirFlags.None, folder != EmailMaildirFolder.Current, folder != EmailMaildirFolder.Current);
        }

        string info = fileName.Substring(separator + 1);
        int comma = info.IndexOf(',');
        int version = comma > 0 ? ParseDecimal(info, comma) : -1;
        bool wellFormed = version >= 0 && comma > 0;
        string flagValue = wellFormed && version == 2 ? info.Substring(comma + 1) : string.Empty;
        EmailMaildirFlags parsedFlags = EmailMaildirFlags.None;
        StringBuilder unknown = new StringBuilder();
        bool ordered = true;
        char previous = (char)0;
        int index = 0;
        while (index < flagValue.Length)
        {
            char flag = flagValue[index];
            if (index > 0 && flag <= previous)
                ordered = false;
            previous = flag;
            EmailMaildirFlags standard = ParseFlag(flag);
            if (standard == EmailMaildirFlags.None)
                unknown.Append(flag);
            else if (((int)parsedFlags & (int)standard) != 0)
                ordered = false;
            else
                parsedFlags = (EmailMaildirFlags)((int)parsedFlags | (int)standard);
            index++;
        }

        bool isCanonical = wellFormed && version == 2 && ordered;
        return new EmailMaildirName(fileName, unique, true, version, info, flagValue, unknown.ToString(), parsedFlags, wellFormed, isCanonical);
    }

    internal static bool IsSafeUniqueNameImplementation(string value)
    {
        if (value == null || value.Length == 0 || value[0] == '.')
            return false;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == 0 || character == ':' || character == '/' || character == '\\' || char.IsControl(character))
                return false;
            index++;
        }

        return value != "." && value != "..";
    }

    internal static bool IsSafeFileNameImplementation(string? value, EmailMaildirFolder folder)
    {
        if (folder < EmailMaildirFolder.New || folder > EmailMaildirFolder.Temporary)
            return false;
        if (value == null || value.Length == 0 || value[0] == '.')
            return false;
        int separator = value.IndexOf(':');
        if (folder != EmailMaildirFolder.Current && separator >= 0)
            return false;
        string unique = separator < 0 ? value : value.Substring(0, separator);
        if (!IsSafeUniqueNameImplementation(unique))
            return false;
        int index = separator < 0 ? unique.Length : separator + 1;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == 0 || character == '/' || character == '\\' || char.IsControl(character))
                return false;
            index++;
        }

        return true;
    }

    internal static string CreateUniqueNameImplementationWith()
    {
        return CreateUniqueNameImplementationWithHostName(Environment.MachineName);
    }

    internal static string CreateUniqueNameImplementationWithHostName(string hostName)
    {
        if (hostName == null)
            throw new ArgumentNullException("hostName");
        DateTimeOffset now = DateTimeOffset.UtcNow;
        long microseconds = now.Ticks % TimeSpan.TicksPerSecond / 10;
        string safeHost = SanitizeHostName(hostName);
        return now.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture) + ".M" + microseconds.ToString("D6", CultureInfo.InvariantCulture) + "P" + Environment.ProcessId.ToString(CultureInfo.InvariantCulture) + "R" + Guid.NewGuid().ToString("N") + "." + safeHost;
    }

    internal static string CreateNewFileNameImplementation(string uniqueName)
    {
        ValidateUniqueNameImplementation(uniqueName, "uniqueName");
        return uniqueName;
    }

    internal static string CreateCurrentFileNameImplementation(string uniqueName, EmailMaildirFlags flags)
    {
        return CreateCurrentFileNamePreservingUnknownFlagsImplementation(uniqueName, flags, string.Empty);
    }

    internal static string CreateCurrentFileNamePreservingUnknownFlagsImplementation(string uniqueName, EmailMaildirFlags flags, string unknownFlagText)
    {
        ValidateUniqueNameImplementation(uniqueName, "uniqueName");
        ValidateFlags(flags);
        if (unknownFlagText == null)
            throw new ArgumentNullException("unknownFlagText");
        bool[] characters = new bool[127];
        AddFlag(characters, flags, EmailMaildirFlags.Draft, 'D');
        AddFlag(characters, flags, EmailMaildirFlags.Flagged, 'F');
        AddFlag(characters, flags, EmailMaildirFlags.Passed, 'P');
        AddFlag(characters, flags, EmailMaildirFlags.Replied, 'R');
        AddFlag(characters, flags, EmailMaildirFlags.Seen, 'S');
        AddFlag(characters, flags, EmailMaildirFlags.Trashed, 'T');
        int unknownIndex = 0;
        while (unknownIndex < unknownFlagText.Length)
        {
            char unknown = unknownFlagText[unknownIndex];
            if (!IsSupportedUnknownFlagCharacterImplementation(unknown) || ParseFlag(unknown) != EmailMaildirFlags.None)
                throw new ArgumentException("Unknown Maildir flags must be safe printable nonstandard ASCII characters.", "unknownFlagText");
            characters[(int)unknown] = true;
            unknownIndex++;
        }

        StringBuilder value = new StringBuilder(uniqueName);
        value.Append(":2,");
        int characterIndex = 33;
        while (characterIndex < characters.Length)
        {
            if (characters[characterIndex])
                value.Append((char)characterIndex);
            characterIndex++;
        }

        return value.ToString();
    }

    internal static bool IsSupportedUnknownFlagCharacterImplementation(char value)
    {
        return value >= (char)33 && value <= (char)126 && value != ':' && value != '/' && value != '\\' && value != ',';
    }

    /// <summary>
    /// Gets the file name.
    /// </summary>
    /// <returns>The current file name.</returns>
    public string GetFileName()
    {
        return fileName;
    }

    /// <summary>
    /// Gets the unique name.
    /// </summary>
    /// <returns>The current unique name.</returns>
    public string GetUniqueName()
    {
        return uniqueName;
    }

    /// <summary>
    /// Determines whether the info is present or available.
    /// </summary>
    /// <returns><c>true</c> when the info is available; otherwise, <c>false</c>.</returns>
    public bool HasInfo()
    {
        return hasInfo;
    }

    /// <summary>
    /// Gets the info version.
    /// </summary>
    /// <returns>The current info version.</returns>
    public int GetInfoVersion()
    {
        return infoVersion;
    }

    /// <summary>
    /// Gets the info text.
    /// </summary>
    /// <returns>The current info text.</returns>
    public string? GetInfoText()
    {
        return infoText;
    }

    /// <summary>
    /// Gets the flag text.
    /// </summary>
    /// <returns>The current flag text.</returns>
    public string GetFlagText()
    {
        return flagText;
    }

    /// <summary>
    /// Gets the unknown flag text.
    /// </summary>
    /// <returns>The current unknown flag text.</returns>
    public string GetUnknownFlagText()
    {
        return unknownFlagText;
    }

    /// <summary>
    /// Gets the flags.
    /// </summary>
    /// <returns>The current flags.</returns>
    public EmailMaildirFlags GetFlags()
    {
        return flags;
    }

    /// <summary>
    /// Determines whether the current value or state is info well formed.
    /// </summary>
    /// <returns><c>true</c> when the value or state is info well formed; otherwise, <c>false</c>.</returns>
    public bool IsInfoWellFormed()
    {
        return infoWellFormed;
    }

    /// <summary>
    /// Determines whether the current value or state is canonical.
    /// </summary>
    /// <returns><c>true</c> when the value or state is canonical; otherwise, <c>false</c>.</returns>
    public bool IsCanonical()
    {
        return canonical;
    }

    /// <summary>
    /// Determines whether the flag is present or available.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the flag is available; otherwise, <c>false</c>.</returns>
    public bool HasFlag(EmailMaildirFlags value)
    {
        ValidateFlags(value);
        if (value == EmailMaildirFlags.None)
            return flags == EmailMaildirFlags.None;
        return ((int)flags & (int)value) == (int)value;
    }

    internal static void ValidateUniqueNameImplementation(string value, string parameterName)
    {
        if (value == null)
            throw new ArgumentNullException(parameterName);
        if (!IsSafeUniqueNameImplementation(value))
            throw new ArgumentException("A Maildir unique name must be non-empty, may not begin with a dot, and may not contain a colon, path separator, NUL, or control character.", parameterName);
    }

    static void ValidateFolder(EmailMaildirFolder value)
    {
        if (value < EmailMaildirFolder.New || value > EmailMaildirFolder.Temporary)
            throw new ArgumentOutOfRangeException("folder");
    }

    static void ValidateFlags(EmailMaildirFlags value)
    {
        int numeric = (int)value;
        if (numeric < 0 || (numeric & ~AllStandardFlags) != 0)
            throw new ArgumentOutOfRangeException("flags");
    }

    static int ParseDecimal(string value, int length)
    {
        int parsed = 0;
        int index = 0;
        while (index < length)
        {
            char character = value[index];
            if (character < '0' || character > '9')
                return -1;
            int digit = character - '0';
            if (parsed > (int.MaxValue - digit) / 10)
                return -1;
            parsed = parsed * 10 + digit;
            index++;
        }

        return parsed;
    }

    static EmailMaildirFlags ParseFlag(char value)
    {
        if (value == 'D')
            return EmailMaildirFlags.Draft;
        if (value == 'F')
            return EmailMaildirFlags.Flagged;
        if (value == 'P')
            return EmailMaildirFlags.Passed;
        if (value == 'R')
            return EmailMaildirFlags.Replied;
        if (value == 'S')
            return EmailMaildirFlags.Seen;
        if (value == 'T')
            return EmailMaildirFlags.Trashed;
        return EmailMaildirFlags.None;
    }

    static bool Has(EmailMaildirFlags values, EmailMaildirFlags value)
    {
        return ((int)values & (int)value) != 0;
    }

    static void AddFlag(bool[] characters, EmailMaildirFlags values, EmailMaildirFlags flag, char character)
    {
        if (Has(values, flag))
            characters[(int)character] = true;
    }

    static string SanitizeHostName(string value)
    {
        StringBuilder safe = new StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            bool accepted = character >= 'a' && character <= 'z' || character >= 'A' && character <= 'Z' || character >= '0' && character <= '9' || character == '-' || character == '_' || character == '.';
            safe.Append(accepted ? character : '_');
            index++;
        }

        return safe.Length == 0 ? "localhost" : safe.ToString();
    }
}
