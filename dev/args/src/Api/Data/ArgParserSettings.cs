using System.Collections.Generic;

namespace args;

/// <summary>
/// Controls token recognition, name matching, unmatched-input behavior, and help formatting.
/// </summary>
public class ArgParserSettings
{
    /// <summary>
    /// The parser convention preset. Non-custom modes override the related recognition, matching, and option-ordering fields.
    /// </summary>
    public ArgParserMode Mode;
    /// <summary>
    /// Whether compact short options such as -abc are expanded into individual aliases in custom mode.
    /// </summary>
    public bool EnablePosixShortClusters;
    /// <summary>
    /// Whether a short option may take an attached value such as -n5 in custom mode.
    /// </summary>
    public bool EnableShortOptionAttachedValues;
    /// <summary>
    /// Whether slash-prefixed options such as /name or /name:value are recognized in custom mode.
    /// </summary>
    public bool EnableSlashOptions;
    /// <summary>
    /// Whether option aliases are compared without regard to case in custom mode.
    /// </summary>
    public bool OptionsCaseInsensitive;
    /// <summary>
    /// Whether command names and aliases are compared without regard to case in custom mode.
    /// </summary>
    public bool CommandsCaseInsensitive;
    /// <summary>
    /// Whether unknown tokens are retained instead of immediately reported as errors.
    /// </summary>
    public bool CollectUnmatched;
    /// <summary>
    /// Whether parsing stops option recognition at the first positional token in custom mode.
    /// </summary>
    public bool StopAtFirstPositional;
    /// <summary>
    /// Whether parsing stops at the first unmatched token and retains the remaining input.
    /// </summary>
    public bool StopAtFirstUnmatched;
    /// <summary>
    /// The token that ends option recognition, normally --.
    /// </summary>
    public string EndOfOptionsDelimiter;
    /// <summary>
    /// The target line width used when wrapping generated help.
    /// </summary>
    public int HelpWidth;
    /// <summary>
    /// Whether generated help includes commands and options marked as hidden.
    /// </summary>
    public bool IncludeHiddenInHelp;
    /// <summary>
    /// Whether generated help uses ANSI styling for usage and section headings.
    /// </summary>
    public bool EnableAnsiHelpStyling;
    /// <summary>
    /// The aliases recognized as the automatic help option.
    /// </summary>
    public List<string> HelpAliases;
    /// <summary>
    /// The source used to read environment-variable fallback values.
    /// </summary>
    public IArgEnvironment Environment;
    /// <summary>
    /// The provider used to resolve parser, validation, invocation, help, and completion messages.
    /// </summary>
    public IArgMessageProvider MessageProvider;
    /// <summary>
    /// The culture used by culture-sensitive built-in value conversions.
    /// </summary>
    public System.Globalization.CultureInfo ConversionCulture;

    /// <summary>
    /// Creates conservative GNU/POSIX-oriented settings with help aliases <c>--help</c> and <c>-h</c> and invariant-culture conversion.
    /// </summary>
    public ArgParserSettings()
    {
        Mode = ArgParserMode.Custom;
        EnablePosixShortClusters = true;
        EnableShortOptionAttachedValues = false;
        EnableSlashOptions = false;
        OptionsCaseInsensitive = false;
        CommandsCaseInsensitive = false;
        CollectUnmatched = false;
        StopAtFirstPositional = false;
        StopAtFirstUnmatched = false;
        EndOfOptionsDelimiter = "--";
        HelpWidth = 80;
        IncludeHiddenInHelp = false;
        EnableAnsiHelpStyling = false;
        HelpAliases = new List<string>();
        HelpAliases.Add("--help");
        HelpAliases.Add("-h");
        Environment = new ArgSystemEnvironment();
        MessageProvider = new ArgEnglishMessageProvider();
        ConversionCulture = System.Globalization.CultureInfo.InvariantCulture;
    }

    internal bool ShortClustersEnabled()
    {
        if (Mode == ArgParserMode.Posix || Mode == ArgParserMode.GnuFriendly)
        {
            return true;
        }

        if (Mode == ArgParserMode.WindowsFriendly)
        {
            return false;
        }

        return EnablePosixShortClusters;
    }

    internal bool AttachedShortValuesEnabled()
    {
        if (Mode == ArgParserMode.Posix || Mode == ArgParserMode.GnuFriendly)
        {
            return true;
        }

        if (Mode == ArgParserMode.WindowsFriendly)
        {
            return false;
        }

        return EnableShortOptionAttachedValues;
    }

    internal bool SlashOptionsEnabled()
    {
        if (Mode == ArgParserMode.WindowsFriendly)
        {
            return true;
        }

        if (Mode == ArgParserMode.Posix || Mode == ArgParserMode.GnuFriendly)
        {
            return false;
        }

        return EnableSlashOptions;
    }

    internal bool OptionMatchingCaseInsensitive()
    {
        if (Mode == ArgParserMode.WindowsFriendly)
        {
            return true;
        }

        if (Mode == ArgParserMode.Posix || Mode == ArgParserMode.GnuFriendly)
        {
            return false;
        }

        return OptionsCaseInsensitive;
    }

    internal bool CommandMatchingCaseInsensitive()
    {
        if (Mode == ArgParserMode.WindowsFriendly)
        {
            return true;
        }

        if (Mode == ArgParserMode.Posix || Mode == ArgParserMode.GnuFriendly)
        {
            return false;
        }

        return CommandsCaseInsensitive;
    }

    internal bool OptionsStopAtFirstPositional()
    {
        if (Mode == ArgParserMode.Posix)
        {
            return true;
        }

        if (Mode == ArgParserMode.GnuFriendly || Mode == ArgParserMode.WindowsFriendly)
        {
            return false;
        }

        return StopAtFirstPositional;
    }
}
