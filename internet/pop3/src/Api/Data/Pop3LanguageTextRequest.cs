namespace pop3;

/// <summary>Describes a request for localized POP3 response text.</summary>
public class Pop3LanguageTextRequest
{
    /// <summary>The selected language tag, or <c>null</c> to use the server default.</summary>
    public string? SelectedLanguage;

    /// <summary>The stable key identifying the response text to resolve.</summary>
    public string ResponseTextKey = "";
}
