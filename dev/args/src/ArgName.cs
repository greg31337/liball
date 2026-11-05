namespace args;

internal class ArgName
{
    internal static void ValidateCommandName(string name)
    {
        string error = CommandNameError(name);
        if (error.Length > 0)
        {
            throw new System.ArgumentException(error, "name");
        }
    }

    internal static void ValidateOptionAlias(string alias)
    {
        string error = OptionAliasError(alias);
        if (error.Length > 0)
        {
            throw new System.ArgumentException(error, "alias");
        }
    }

    internal static void ValidateArgumentName(string name)
    {
        string error = ArgumentNameError(name);
        if (error.Length > 0)
        {
            throw new System.ArgumentException(error, "name");
        }
    }

    internal static string CommandNameError(string name)
    {
        if (name == null)
        {
            return "Command name cannot be null.";
        }

        if (name.Length == 0)
        {
            return "Command name cannot be empty.";
        }

        if (HasWhiteSpace(name))
        {
            return "Command name cannot contain whitespace.";
        }

        if (name[0] == '-' || name[0] == '/')
        {
            return "Command name cannot start with '-' or '/'.";
        }

        if (ContainsCharacter(name, '='))
        {
            return "Command name cannot contain '='.";
        }

        return "";
    }

    internal static string OptionAliasError(string alias)
    {
        if (alias == null)
        {
            return "Option alias cannot be null.";
        }

        if (alias.Length == 0)
        {
            return "Option alias cannot be empty.";
        }

        if (HasWhiteSpace(alias))
        {
            return "Option alias cannot contain whitespace.";
        }

        if (ContainsCharacter(alias, '='))
        {
            return "Option alias cannot contain '='.";
        }

        if (IsLongOptionAlias(alias))
        {
            return "";
        }

        if (IsShortOptionAlias(alias))
        {
            return "";
        }

        if (IsSlashOptionAlias(alias))
        {
            return "";
        }

        return "Option alias must use '--name', '-n', or '/name' form.";
    }

    internal static string ArgumentNameError(string name)
    {
        if (name == null)
        {
            return "Argument name cannot be null.";
        }

        if (name.Length == 0)
        {
            return "Argument name cannot be empty.";
        }

        if (HasWhiteSpace(name))
        {
            return "Argument name cannot contain whitespace.";
        }

        if (name[0] == '-' || name[0] == '/')
        {
            return "Argument name cannot start with '-' or '/'.";
        }

        if (ContainsCharacter(name, '='))
        {
            return "Argument name cannot contain '='.";
        }

        return "";
    }

    internal static bool IsLongOptionAlias(string alias)
    {
        if (alias == null)
        {
            return false;
        }

        if (alias.Length <= 2)
        {
            return false;
        }

        if (alias[0] != '-' || alias[1] != '-')
        {
            return false;
        }

        if (alias[2] == '-')
        {
            return false;
        }

        return true;
    }

    internal static bool IsShortOptionAlias(string alias)
    {
        if (alias == null)
        {
            return false;
        }

        if (alias.Length != 2)
        {
            return false;
        }

        if (alias[0] != '-')
        {
            return false;
        }

        if (alias[1] == '-')
        {
            return false;
        }

        return true;
    }

    internal static bool IsSlashOptionAlias(string alias)
    {
        if (alias == null)
        {
            return false;
        }

        if (alias.Length <= 1)
        {
            return false;
        }

        if (alias[0] != '/')
        {
            return false;
        }

        if (alias[1] == '/')
        {
            return false;
        }

        if (ContainsCharacter(alias, ':'))
        {
            return false;
        }

        return true;
    }

    internal static bool HasWhiteSpace(string text)
    {
        int index = 0;
        while (index < text.Length)
        {
            if (char.IsWhiteSpace(text[index]))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool ContainsCharacter(string text, char value)
    {
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] == value)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
