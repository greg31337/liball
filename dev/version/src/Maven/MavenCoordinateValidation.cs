namespace version;

internal static class MavenCoordinateValidation
{
    internal static void ValidateIdentifier(string text, string message)
    {
        ValidateToken(text, message);

        int index = 0;
        while (index < text.Length)
        {
            if (text[index] == '/' || text[index] == '\\')
            {
                throw new System.ArgumentException(message);
            }

            index = index + 1;
        }
    }

    internal static void ValidateToken(string text, string message)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        if (text.Length == 0)
        {
            throw new System.ArgumentException(message);
        }

        int index = 0;
        while (index < text.Length)
        {
            if (VersionChar.IsWhitespace(text[index]) || text[index] == ':')
            {
                throw new System.ArgumentException(message);
            }

            index = index + 1;
        }
    }
}
