namespace args;

internal class ArgMessageLookup
{
    internal static readonly ArgEnglishMessageProvider English = new ArgEnglishMessageProvider();

    internal static string Get(ArgParserSettings settings, string key, string[] values)
    {
        string? message = null;
        if (settings.MessageProvider != null)
        {
            message = settings.MessageProvider.GetMessage(key, values);
        }

        if (message != null)
        {
            return message;
        }

        message = English.GetMessage(key, values);
        if (message != null)
        {
            return message;
        }

        return key;
    }
}
