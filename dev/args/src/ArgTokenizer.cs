using System.Collections.Generic;

namespace args;

internal class ArgTokenizer
{
    internal ArgParserSettings Settings;

    internal ArgTokenizer()
    {
        Settings = new ArgParserSettings();
    }

    internal static ArgTokenizer WithSettings(ArgParserSettings settings)
    {
        if (settings == null)
        {
            throw new System.ArgumentNullException("settings");
        }

        ArgTokenizer tokenizer = new ArgTokenizer();
        tokenizer.Settings = settings;
        return tokenizer;
    }

    internal List<ArgToken> Tokenize(string[] argv)
    {
        if (argv == null)
        {
            throw new System.ArgumentNullException("argv");
        }

        List<ArgToken> tokens = new List<ArgToken>();
        bool afterEndOfOptions = false;
        int index = 0;
        while (index < argv.Length)
        {
            string rawText = argv[index];
            ArgTokenKind kind = Classify(rawText, afterEndOfOptions);
            tokens.Add(new ArgToken(rawText, kind, index));
            if (kind == ArgTokenKind.EndOfOptions)
            {
                afterEndOfOptions = true;
            }

            index = index + 1;
        }

        return tokens;
    }

    internal ArgTokenKind Classify(string rawText, bool afterEndOfOptions)
    {
        if (rawText == null)
        {
            throw new System.ArgumentNullException("rawText");
        }

        if (afterEndOfOptions)
        {
            return ArgTokenKind.Positional;
        }

        if (rawText == Settings.EndOfOptionsDelimiter)
        {
            return ArgTokenKind.EndOfOptions;
        }

        if (rawText.Length == 0)
        {
            return ArgTokenKind.Positional;
        }

        if (rawText == "-")
        {
            return ArgTokenKind.Positional;
        }

        if (IsLongOptionToken(rawText))
        {
            return ArgTokenKind.LongOption;
        }

        if (IsShortOptionToken(rawText))
        {
            return ArgTokenKind.ShortOption;
        }

        if (Settings.SlashOptionsEnabled() && IsSlashOptionToken(rawText))
        {
            return ArgTokenKind.SlashOption;
        }

        return ArgTokenKind.Positional;
    }

    internal bool IsLongOptionToken(string rawText)
    {
        if (rawText.Length <= 2)
        {
            return false;
        }

        if (rawText[0] == '-' && rawText[1] == '-')
        {
            return true;
        }

        return false;
    }

    internal bool IsShortOptionToken(string rawText)
    {
        if (rawText.Length <= 1)
        {
            return false;
        }

        if (rawText[0] == '-' && rawText[1] != '-')
        {
            return true;
        }

        return false;
    }

    internal bool IsSlashOptionToken(string rawText)
    {
        if (rawText.Length <= 1)
        {
            return false;
        }

        if (rawText[0] == '/')
        {
            return true;
        }

        return false;
    }
}
