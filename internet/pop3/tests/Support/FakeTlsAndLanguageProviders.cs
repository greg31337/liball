namespace pop3.Tests;

internal sealed class FakeTlsProvider : IPop3ClientTlsProvider, IPop3ServerTlsProvider
{
    internal Func<Stream, Pop3TlsClientOptions, CancellationToken, Stream>? ClientHandler = null;
    internal Func<Stream, Pop3TlsServerOptions, CancellationToken, Stream>? ServerHandler = null;
    internal Stream? ClientResultStream = null;
    internal Stream? ServerResultStream = null;
    internal readonly List<string> Calls = new List<string>();
    internal int ClientCallCount;
    internal int ServerCallCount;
    internal Stream? LastClientInputStream;
    internal Stream? LastServerInputStream;
    internal Stream? LastClientResultStream;
    internal Stream? LastServerResultStream;
    internal Pop3TlsClientOptions? LastClientOptions;
    internal Pop3TlsServerOptions? LastServerOptions;
    internal CancellationToken LastClientCancellationToken;
    internal CancellationToken LastServerCancellationToken;

    public Stream SecureClientStream(
        Stream stream,
        Pop3TlsClientOptions options,
        CancellationToken cancellationToken)
    {
        ClientCallCount++;
        Calls.Add("client");
        LastClientInputStream = stream;
        LastClientOptions = options;
        LastClientCancellationToken = cancellationToken;
        if (ClientHandler != null)
        {
            LastClientResultStream = ClientHandler(stream, options, cancellationToken);
        }
        else
        {
            LastClientResultStream = ClientResultStream ?? stream;
        }
        return LastClientResultStream;
    }

    public Stream SecureServerStream(
        Stream stream,
        Pop3TlsServerOptions options,
        CancellationToken cancellationToken)
    {
        ServerCallCount++;
        Calls.Add("server");
        LastServerInputStream = stream;
        LastServerOptions = options;
        LastServerCancellationToken = cancellationToken;
        if (ServerHandler != null)
        {
            LastServerResultStream = ServerHandler(stream, options, cancellationToken);
        }
        else
        {
            LastServerResultStream = ServerResultStream ?? stream;
        }
        return LastServerResultStream;
    }
}

internal sealed class FakeLanguageProvider : IPop3LanguageProvider
{
    internal Pop3Language[] Languages = Array.Empty<Pop3Language>();
    internal readonly HashSet<string> SupportedLanguages =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    internal readonly Dictionary<string, string> ResponseTexts =
        new Dictionary<string, string>(StringComparer.Ordinal);
    internal Func<Pop3LanguageTextRequest, string>? ResponseTextHandler = null;
    internal readonly List<string> SupportedLanguageRequests = new List<string>();
    internal readonly List<Pop3LanguageTextRequest> TextRequests =
        new List<Pop3LanguageTextRequest>();
    internal int GetLanguagesCallCount;
    internal int SupportsLanguageCallCount;
    internal int GetResponseTextCallCount;

    public Pop3Language[] GetLanguages()
    {
        GetLanguagesCallCount++;
        return Languages;
    }

    public bool SupportsLanguage(string languageTag)
    {
        SupportsLanguageCallCount++;
        SupportedLanguageRequests.Add(languageTag);
        return SupportedLanguages.Contains(languageTag);
    }

    public string GetResponseText(Pop3LanguageTextRequest request)
    {
        GetResponseTextCallCount++;
        TextRequests.Add(request);
        if (ResponseTextHandler != null)
        {
            return ResponseTextHandler(request);
        }
        if (ResponseTexts.TryGetValue(request.ResponseTextKey, out string? text))
        {
            return text;
        }
        return request.ResponseTextKey;
    }
}
