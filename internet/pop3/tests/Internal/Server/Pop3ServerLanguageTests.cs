using System.Text;

namespace pop3.Tests;

public class Pop3ServerLanguageTests
{
    [Test]
    public void AuthorizationListsValidatedLanguagesWithUtf8Descriptions()
    {
        LanguageProvider provider = new LanguageProvider();
        provider.Languages = new[]
        {
            CreateLanguage("en", "English"),
            CreateLanguage("fr-CA", "Français canadien")
        };
        Fixture fixture = Start("LANG\r\n", provider, null, false);

        Pop3Language[] languages = fixture.Engine.HandleLangCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(languages.Length, Is.EqualTo(2));
        Assert.That(languages[0].Tag, Is.EqualTo("en"));
        Assert.That(languages[1].Description,
            Is.EqualTo("Français canadien"));
        Assert.That(provider.TextRequests, Is.Empty);
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n" +
            "+OK Language listing follows.\r\n" +
            "en English\r\n" +
            "fr-CA Français canadien\r\n" +
            ".\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void AuthorizationSelectsLanguageAndPreservesItAfterRejection()
    {
        LanguageProvider provider = new LanguageProvider();
        provider.SupportedLanguages = new[] { "fr" };
        Fixture fixture = Start(
            "LANG fr\r\nLANG zz\r\n",
            provider,
            null,
            false);

        fixture.Engine.HandleLangCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        fixture.Engine.HandleLangCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(fixture.Engine.GetStateMachine().GetSelectedLanguage(),
            Is.EqualTo("fr"));
        Assert.That(provider.TextRequests.Count, Is.EqualTo(2));
        Assert.That(provider.TextRequests[0].SelectedLanguage,
            Is.EqualTo("fr"));
        Assert.That(provider.TextRequests[0].ResponseTextKey,
            Is.EqualTo("lang-select-success"));
        Assert.That(provider.TextRequests[1].SelectedLanguage,
            Is.EqualTo("fr"));
        Assert.That(provider.TextRequests[1].ResponseTextKey,
            Is.EqualTo("lang-language-unavailable"));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n" +
            "+OK fr langue choisie\r\n" +
            "-ERR langue indisponible\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void TransactionResetUsesConfiguredDefaultWithoutStoringIt()
    {
        LanguageProvider provider = new LanguageProvider();
        provider.SupportedLanguages = new[] { "en", "fr" };
        Fixture fixture = Start("LANG *\r\n", provider, "en", true);
        fixture.Engine.GetStateMachine().SelectLanguage("fr");

        fixture.Engine.HandleLangCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(fixture.Engine.GetStateMachine().GetCurrentState(),
            Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(fixture.Engine.GetStateMachine().GetSelectedLanguage(),
            Is.Null);
        Assert.That(provider.TextRequests.Count, Is.EqualTo(1));
        Assert.That(provider.TextRequests[0].SelectedLanguage,
            Is.EqualTo("en"));
        Assert.That(provider.TextRequests[0].ResponseTextKey,
            Is.EqualTo("lang-reset-success"));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n+OK en default restored\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void TransactionResetCanUseAuthenticatedProviderPolicy()
    {
        LanguageProvider provider = new LanguageProvider();
        Fixture fixture = Start("LANG *\r\n", provider, null, true);
        fixture.Engine.GetStateMachine().SelectLanguage("fr");

        fixture.Engine.HandleLangCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(provider.TextRequests.Count, Is.EqualTo(1));
        Assert.That(provider.TextRequests[0].SelectedLanguage, Is.Null);
        Assert.That(provider.TextRequests[0].ResponseTextKey,
            Is.EqualTo("lang-reset-success"));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n+OK default restored\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void AnonymousResetDoesNotConsultUserAwareProviderPolicy()
    {
        LanguageProvider provider = new LanguageProvider();
        Fixture fixture = Start("LANG *\r\n", provider, null, false);

        fixture.Engine.HandleLangCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(provider.TextRequests, Is.Empty);
        Assert.That(fixture.Engine.GetStateMachine().GetSelectedLanguage(),
            Is.Null);
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n+OK Server default language selected.\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void MissingProviderRejectsLangWithoutChangingState()
    {
        Fixture fixture = Start("LANG fr\r\n", null, null, false);

        Pop3Language[] languages = fixture.Engine.HandleLangCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(languages, Is.Empty);
        Assert.That(fixture.Engine.GetStateMachine().GetSelectedLanguage(),
            Is.Null);
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n-ERR Language support is not available.\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void InvalidProviderListingFailsBeforeMultilineResponseStarts()
    {
        LanguageProvider provider = new LanguageProvider();
        provider.Languages = new[]
        {
            CreateLanguage("en", "English"),
            CreateLanguage("EN", "duplicate")
        };
        Fixture fixture = Start("LANG\r\n", provider, null, false);
        Pop3Command command =
            fixture.Engine.ReadNextCommand(CancellationToken.None);

        Assert.Throws<InvalidDataException>(() =>
            fixture.Engine.HandleLangCommand(
                command,
                CancellationToken.None));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo("+OK ready\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void LanguageSelectionStopsFurtherPipelineIntakeUntilItsResponse()
    {
        LanguageProvider provider = new LanguageProvider();
        provider.SupportedLanguages = new[] { "fr" };
        Fixture fixture = Start(
            "LANG fr\r\nCAPA\r\n",
            provider,
            null,
            false);
        Pop3Command language =
            fixture.Engine.ReadNextCommand(CancellationToken.None);

        Assert.Throws<Pop3StateException>(() =>
            fixture.Engine.ReadNextCommand(CancellationToken.None));
        fixture.Engine.HandleLangCommand(language, CancellationToken.None);
        Pop3Command capability =
            fixture.Engine.ReadNextCommand(CancellationToken.None);

        Assert.That(capability.Keyword, Is.EqualTo(Pop3Commands.Capa));
        fixture.Engine.WriteCapabilitiesResponse(CancellationToken.None);
        fixture.Dispose();
    }

    [Test]
    public void LanguageSessionValidatesConfigurationAndProviderResults()
    {
        Pop3LanguageSession session = new Pop3LanguageSession();
        Pop3ServerOptions options = new Pop3ServerOptions();
        LanguageProvider provider = new LanguageProvider();

        Assert.Throws<ArgumentNullException>(() =>
            session.GetValidatedLanguages(null!));
        Assert.That(session.GetValidatedLanguages(options), Is.Empty);

        options.LanguageProvider = provider;
        provider.Languages = null!;
        Assert.Throws<InvalidDataException>(() =>
            session.GetValidatedLanguages(options));
        provider.Languages = new Pop3Language[] { null! };
        Assert.Throws<InvalidDataException>(() =>
            session.GetValidatedLanguages(options));
        Pop3Language missingTag = CreateLanguage("en", "English");
        missingTag.Tag = null!;
        provider.Languages = new[] { missingTag };
        Assert.Throws<InvalidDataException>(() =>
            session.GetValidatedLanguages(options));
        Pop3Language missingDescription = CreateLanguage("en", "English");
        missingDescription.Description = null!;
        provider.Languages = new[] { missingDescription };
        Assert.Throws<InvalidDataException>(() =>
            session.GetValidatedLanguages(options));
    }

    [Test]
    public void LanguageSessionValidatesResponseLookupInputsAndText()
    {
        Pop3LanguageSession session = new Pop3LanguageSession();
        Pop3ServerOptions options = new Pop3ServerOptions();
        Pop3ServerStateMachine state = new Pop3ServerStateMachine();

        Assert.Throws<ArgumentNullException>(() => session.ResolveResponseText(
            null!, state, "key", "fallback"));
        Assert.Throws<ArgumentNullException>(() => session.ResolveResponseText(
            options, null!, "key", "fallback"));
        Assert.Throws<ArgumentException>(() => session.ResolveResponseText(
            options, state, null!, "fallback"));
        Assert.Throws<ArgumentException>(() => session.ResolveResponseText(
            options, state, "", "fallback"));
        Assert.Throws<ArgumentNullException>(() => session.ResolveResponseText(
            options, state, "key", null!));
        Assert.That(
            session.ResolveResponseText(options, state, "key", "fallback"),
            Is.EqualTo("fallback"));

        LanguageProvider provider = new LanguageProvider();
        options.LanguageProvider = provider;
        options.DefaultLanguage = "en";
        provider.ResponseText = null!;
        Assert.Throws<InvalidDataException>(() => session.ResolveResponseText(
            options, state, "key", "fallback"));
        provider.ResponseText = "bad\ntext";
        Assert.Throws<InvalidDataException>(() => session.ResolveResponseText(
            options, state, "key", "fallback"));
        provider.ResponseText = "bad\u007ftext";
        Assert.Throws<InvalidDataException>(() => session.ResolveResponseText(
            options, state, "key", "fallback"));
    }

    static Pop3Language CreateLanguage(string tag, string description)
    {
        Pop3Language language = new Pop3Language();
        language.Tag = tag;
        language.Description = description;
        return language;
    }

    static Fixture Start(
        string input,
        IPop3LanguageProvider? provider,
        string? defaultLanguage,
        bool transaction)
    {
        DuplexStream stream = new DuplexStream(input);
        Pop3ServerOptions options = new Pop3ServerOptions();
        options.GreetingText = "ready";
        options.SecurityMode = Pop3SecurityMode.Plaintext;
        options.EnablePipelining = true;
        options.LanguageProvider = provider;
        options.DefaultLanguage = defaultLanguage;
        options.BufferSize = 64;
        Pop3ServerConnection connection = new Pop3ServerConnection();
        connection.Stream = stream;
        connection.LeaveOpen = true;
        Pop3ServerEngine engine = new Pop3ServerEngine();
        engine.StartSession(connection, options, CancellationToken.None);
        if (transaction)
        {
            engine.GetStateMachine().EnterTransaction();
        }
        return new Fixture(engine, stream);
    }

    sealed class Fixture
    {
        internal Pop3ServerEngine Engine;
        internal DuplexStream Stream;

        internal Fixture(Pop3ServerEngine engine, DuplexStream stream)
        {
            Engine = engine;
            Stream = stream;
        }

        internal void Dispose()
        {
            Engine.CloseSession();
            Stream.Dispose();
        }
    }

    sealed class LanguageProvider : IPop3LanguageProvider
    {
        internal Pop3Language[] Languages = Array.Empty<Pop3Language>();
        internal string[] SupportedLanguages = Array.Empty<string>();
        internal List<Pop3LanguageTextRequest> TextRequests =
            new List<Pop3LanguageTextRequest>();
        internal string ResponseText = "localized";

        public Pop3Language[] GetLanguages()
        {
            return Languages;
        }

        public bool SupportsLanguage(string languageTag)
        {
            int index = 0;
            while (index < SupportedLanguages.Length)
            {
                if (string.Equals(
                    SupportedLanguages[index],
                    languageTag,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                index++;
            }
            return false;
        }

        public string GetResponseText(Pop3LanguageTextRequest request)
        {
            TextRequests.Add(request);
            if (request.ResponseTextKey == "lang-select-success")
            {
                return "langue choisie";
            }
            if (request.ResponseTextKey == "lang-language-unavailable")
            {
                return "langue indisponible";
            }
            if (request.ResponseTextKey == "lang-reset-success")
            {
                return "default restored";
            }
            return ResponseText;
        }
    }

    sealed class DuplexStream : Stream
    {
        byte[] Input;
        int InputOffset;
        MemoryStream Output = new MemoryStream();

        internal DuplexStream(string input)
        {
            Input = Encoding.ASCII.GetBytes(input);
        }

        internal string WrittenText()
        {
            return Encoding.UTF8.GetString(Output.ToArray());
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int available = Input.Length - InputOffset;
            int copied = Math.Min(count, available);
            if (copied > 0)
            {
                Array.Copy(Input, InputOffset, buffer, offset, copied);
                InputOffset += copied;
            }
            return copied;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Output.Write(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }
    }
}
