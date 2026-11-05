namespace pop3;

internal class Pop3LanguageSession
{
    internal Pop3Language[] GetValidatedLanguages(
        Pop3ServerOptions options)
    {
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }
        IPop3LanguageProvider? provider = options.LanguageProvider;
        if (provider == null)
        {
            return new Pop3Language[0];
        }

        Pop3Language[] languages = provider.GetLanguages();
        if (languages == null)
        {
            throw new System.IO.InvalidDataException(
                "The POP3 language provider returned a null language listing.");
        }

        byte[][] lines = new byte[languages.Length][];
        System.Text.UTF8Encoding encoding =
            new System.Text.UTF8Encoding(false, true);
        int index = 0;
        while (index < languages.Length)
        {
            Pop3Language language = languages[index];
            if (language == null || language.Tag == null ||
                language.Description == null)
            {
                throw new System.IO.InvalidDataException(
                    "The POP3 language provider returned an invalid language listing.");
            }
            string line = language.Tag;
            if (language.Description.Length > 0)
            {
                line += " " + language.Description;
            }
            lines[index] = encoding.GetBytes(line);
            index++;
        }
        return new Pop3LanguageParser().ParseLanguages(lines);
    }

    internal string ResolveResponseText(
        Pop3ServerOptions options,
        Pop3ServerStateMachine stateMachine,
        string responseTextKey,
        string fallbackText)
    {
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }
        if (stateMachine == null)
        {
            throw new System.ArgumentNullException("stateMachine");
        }
        if (responseTextKey == null || responseTextKey.Length == 0)
        {
            throw new System.ArgumentException(
                "A POP3 response-text key is required.",
                "responseTextKey");
        }
        if (fallbackText == null)
        {
            throw new System.ArgumentNullException("fallbackText");
        }

        IPop3LanguageProvider? provider = options.LanguageProvider;
        if (provider == null)
        {
            return fallbackText;
        }

        string? selectedLanguage = stateMachine.GetSelectedLanguage();
        if (selectedLanguage == null)
        {
            selectedLanguage = options.DefaultLanguage;
        }
        if (selectedLanguage == null &&
            !stateMachine.GetIsAuthenticated())
        {
            return fallbackText;
        }

        Pop3LanguageTextRequest request = new Pop3LanguageTextRequest();
        request.SelectedLanguage = selectedLanguage;
        request.ResponseTextKey = responseTextKey;
        string? responseText = provider.GetResponseText(request);
        if (responseText == null)
        {
            throw new System.IO.InvalidDataException(
                "The POP3 language provider returned null response text.");
        }
        int index = 0;
        while (index < responseText.Length)
        {
            if (responseText[index] < 32 || responseText[index] == 127)
            {
                throw new System.IO.InvalidDataException(
                    "The POP3 language provider returned control characters.");
            }
            index++;
        }
        return responseText;
    }
}
