namespace json;

internal static class JsonUtf16Input
{
    internal static byte[] EncodeJsonStringInput(string text)
    {
        if (text == null)
        {
            throw new global::System.ArgumentNullException("text");
        }

        int index = 0;
        while (index < text.Length)
        {
            char value = text[index];
            if (global::System.Char.IsHighSurrogate(value))
            {
                if (index + 1 >= text.Length ||
                    !global::System.Char.IsLowSurrogate(text[index + 1]))
                {
                    throw CreateSurrogateFailure("A high surrogate in string input requires a following low surrogate.");
                }

                index += 2;
                continue;
            }

            if (global::System.Char.IsLowSurrogate(value))
            {
                throw CreateSurrogateFailure("A low surrogate in string input requires a preceding high surrogate.");
            }

            index++;
        }

        return global::System.Text.Encoding.UTF8.GetBytes(text);
    }

    static JsonParseException CreateSurrogateFailure(string message)
    {
        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = JsonDiagnosticCode.InvalidSurrogatePair;
        diagnostic.Message = message;
        return new JsonParseException(diagnostic);
    }
}
