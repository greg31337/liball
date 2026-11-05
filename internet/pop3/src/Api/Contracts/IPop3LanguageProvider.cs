namespace pop3;

/// <summary>Provides available languages and localized POP3 response text.</summary>
public interface IPop3LanguageProvider
{
    /// <summary>Gets the languages available to the current server.</summary>
    /// <returns>The available languages in advertisement order.</returns>
    Pop3Language[] GetLanguages();

    /// <summary>Checks whether a language tag can be selected.</summary>
    /// <param name="languageTag">The requested language tag.</param>
    /// <returns><c>true</c> when the language is available; otherwise <c>false</c>.</returns>
    bool SupportsLanguage(string languageTag);

    /// <summary>Resolves one stable response-text key.</summary>
    /// <param name="request">The language and response-text key to resolve.</param>
    /// <returns>The localized response text.</returns>
    string GetResponseText(Pop3LanguageTextRequest request);
}
