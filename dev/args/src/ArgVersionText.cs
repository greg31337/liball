namespace args;

internal class ArgVersionText
{
    internal static string ResolveCommand(ArgCommand command)
    {
        ArgCommand root = CommandRoot(command);
        if (root.VersionProviderFunc == null)
        {
            return root.VersionText;
        }

        string? text = root.VersionProviderFunc();
        if (text == null)
        {
            throw new System.InvalidOperationException("The version text provider returned null.");
        }

        return text;
    }

    internal static ArgCommand CommandRoot(ArgCommand command)
    {
        ArgCommand current = command;
        while (current.Parent != null)
        {
            current = current.Parent;
        }

        return current;
    }
}
