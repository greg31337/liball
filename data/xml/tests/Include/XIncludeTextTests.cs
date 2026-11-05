using System.Text;
using xml;

namespace xml.Tests;

[TestFixture]
public class XIncludeTextTests
{
    private const string Xi = "http://www.w3.org/2001/XInclude";

    [Test]
    public void IncludesLiteralTextAndNormalizesXml10LineEndings()
    {
        TextIncludeResolver resolver = new TextIncludeResolver();
        resolver.AddCharacters("literal.txt", "<tag>&value\r\nline\rend");
        XmlDocument document = Parse("<root xmlns:xi='" + Xi + "'><before/><xi:include href='literal.txt' parse='text' accept='text/plain' accept-language='en-US'/><after/></root>", XmlVersion.Xml10);

        new XmlIncludeProcessor().ProcessXmlIncludes(document, Options(), resolver);

        XmlNode[] children = document.DocumentElement!.Children!;
        Assert.Multiple(() =>
        {
            Assert.That(children, Has.Length.EqualTo(3));
            Assert.That(children[1].Kind, Is.EqualTo(XmlNodeKind.Text));
            Assert.That(children[1].Value, Is.EqualTo("<tag>&value\nline\nend"));
            Assert.That(children[1].Parent, Is.SameAs(document.DocumentElement));
            Assert.That(children[1].OwnerDocument, Is.SameAs(document));
            Assert.That(resolver.Requests, Has.Count.EqualTo(1));
            Assert.That(resolver.Requests[0].Kind, Is.EqualTo(XmlResourceKind.XIncludeText));
            Assert.That(resolver.Requests[0].BaseUri, Is.EqualTo("https://example.test/source.xml"));
            Assert.That(resolver.Requests[0].Accept, Is.EqualTo("text/plain"));
            Assert.That(resolver.Requests[0].AcceptLanguage, Is.EqualTo("en-US"));
        });
    }

    [Test]
    public void UsesExternalEncodingBeforeTheIncludeEncodingAttribute()
    {
        TextIncludeResolver resolver = new TextIncludeResolver();
        resolver.AddBytes("latin.txt", [0x63, 0x61, 0x66, 0xE9], "iso-8859-1", "text/plain");
        resolver.AddBytes("charset.txt", [0x63, 0x61, 0x66, 0xE9], null, "text/plain; charset='iso-8859-1'");
        string source = "<root xmlns:xi='" + Xi + "'>" +
            "<xi:include href='latin.txt' parse='text' encoding='utf-8'/>" +
            "<xi:include href='charset.txt' parse='text' encoding='utf-8'/>" +
            "</root>";
        XmlDocument document = Parse(source, XmlVersion.Xml10);

        new XmlIncludeProcessor().ProcessXmlIncludes(document, Options(), resolver);

        Assert.Multiple(() =>
        {
            Assert.That(document.DocumentElement!.Children![0].Value, Is.EqualTo("café"));
            Assert.That(document.DocumentElement.Children[1].Value, Is.EqualTo("café"));
        });
    }

    [Test]
    public void UsesXmlMediaEncodingRulesBeforeTheIncludeEncodingAttribute()
    {
        TextIncludeResolver resolver = new TextIncludeResolver();
        byte[] prefix = Encoding.ASCII.GetBytes("<?xml version='1.0' encoding='iso-8859-1'?>caf");
        byte[] bytes = new byte[prefix.Length + 1];
        Array.Copy(prefix, bytes, prefix.Length);
        bytes[bytes.Length - 1] = 0xE9;
        resolver.AddBytes("xml.txt", bytes, null, "application/example+xml");
        XmlDocument document = Parse("<root xmlns:xi='" + Xi + "'><xi:include href='xml.txt' parse='text' encoding='utf-8'/></root>", XmlVersion.Xml10);

        new XmlIncludeProcessor().ProcessXmlIncludes(document, Options(), resolver);

        Assert.That(document.DocumentElement!.Children![0].Value, Is.EqualTo("<?xml version='1.0' encoding='iso-8859-1'?>café"));
    }

    [Test]
    public void UsesIncludeAndConfiguredEncodingFallbacks()
    {
        TextIncludeResolver resolver = new TextIncludeResolver();
        resolver.AddBytes("attribute.txt", Encoding.BigEndianUnicode.GetBytes("αβ"), null, "text/plain");
        resolver.AddBytes("option.txt", [0x63, 0x61, 0x66, 0xE9], null, "text/plain");
        string source = "<root xmlns:xi='" + Xi + "'>" +
            "<xi:include href='attribute.txt' parse='text' encoding='utf-16be'/>" +
            "<xi:include href='option.txt' parse='text'/>" +
            "</root>";
        XmlDocument document = Parse(source, XmlVersion.Xml10);
        XmlIncludeOptions options = Options();
        options.TextEncoding = "iso-8859-1";

        new XmlIncludeProcessor().ProcessXmlIncludes(document, options, resolver);

        Assert.Multiple(() =>
        {
            Assert.That(document.DocumentElement!.Children![0].Value, Is.EqualTo("αβ"));
            Assert.That(document.DocumentElement.Children[1].Value, Is.EqualTo("café"));
        });
    }

    [Test]
    public void AppliesGenericAndFixedEndianByteOrderMarkRules()
    {
        TextIncludeResolver resolver = new TextIncludeResolver();
        resolver.AddBytes("utf8.txt", [0xEF, 0xBB, 0xBF, 0x6F, 0x6B], null, "text/plain");
        resolver.AddBytes("fixed.txt", [0xFF, 0xFE, 0x6F, 0x00, 0x6B, 0x00], null, "text/plain");
        string source = "<root xmlns:xi='" + Xi + "'>" +
            "<xi:include href='utf8.txt' parse='text'/>" +
            "<xi:include href='fixed.txt' parse='text' encoding='utf-16le'/>" +
            "</root>";
        XmlDocument document = Parse(source, XmlVersion.Xml10);

        new XmlIncludeProcessor().ProcessXmlIncludes(document, Options(), resolver);

        Assert.Multiple(() =>
        {
            Assert.That(document.DocumentElement!.Children![0].Value, Is.EqualTo("ok"));
            Assert.That(document.DocumentElement.Children[1].Value, Is.EqualTo("﻿ok"));
        });
    }

    [Test]
    public void NormalizesXml11NelAndLineSeparatorCharacters()
    {
        TextIncludeResolver resolver = new TextIncludeResolver();
        resolver.AddCharacters("lines.txt", "one\u0085two\u2028three\r\u0085four");
        XmlDocument document = Parse("<?xml version='1.1'?><root xmlns:xi='" + Xi + "'><xi:include href='lines.txt' parse='text'/></root>", XmlVersion.Xml11);

        new XmlIncludeProcessor().ProcessXmlIncludes(document, Options(), resolver);

        Assert.That(document.DocumentElement!.Children![0].Value, Is.EqualTo("one\ntwo\nthree\nfour"));
    }

    [Test]
    public void UsesFallbackForUnsupportedEncodingsAndEmptyLocations()
    {
        TextIncludeResolver resolver = new TextIncludeResolver();
        resolver.AddBytes("unsupported.txt", [0x61], null, "text/plain");
        string source = "<root xmlns:xi='" + Xi + "'>" +
            "<xi:include href='unsupported.txt' parse='text' encoding='x-unknown'><xi:fallback><encoding-fallback/></xi:fallback></xi:include>" +
            "<xi:include href='' parse='text'><xi:fallback><location-fallback/></xi:fallback></xi:include>" +
            "</root>";
        XmlDocument document = Parse(source, XmlVersion.Xml10);

        new XmlIncludeProcessor().ProcessXmlIncludes(document, Options(), resolver);

        XmlNode[] children = document.DocumentElement!.Children!;
        Assert.Multiple(() =>
        {
            Assert.That(children[0].Name!.LocalName, Is.EqualTo("encoding-fallback"));
            Assert.That(children[1].Name!.LocalName, Is.EqualTo("location-fallback"));
            Assert.That(resolver.Requests, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void RejectsInvalidBytesAndIllegalXmlCharactersWithoutFallback()
    {
        TextIncludeResolver invalidBytesResolver = new TextIncludeResolver();
        invalidBytesResolver.AddBytes("bytes.txt", [0xC3, 0x28], null, "text/plain");
        XmlDocument invalidBytes = Parse("<root xmlns:xi='" + Xi + "'><xi:include href='bytes.txt' parse='text'><xi:fallback><unused/></xi:fallback></xi:include></root>", XmlVersion.Xml10);

        XmlResourceException byteFailure = Assert.Throws<XmlResourceException>(() => new XmlIncludeProcessor().ProcessXmlIncludes(invalidBytes, Options(), invalidBytesResolver))!;
        Assert.That(byteFailure.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.IncludeFailure));

        TextIncludeResolver invalidCharacterResolver = new TextIncludeResolver();
        invalidCharacterResolver.AddCharacters("characters.txt", "valid\0invalid");
        XmlDocument invalidCharacters = Parse("<root xmlns:xi='" + Xi + "'><xi:include href='characters.txt' parse='text'><xi:fallback><unused/></xi:fallback></xi:include></root>", XmlVersion.Xml10);

        XmlResourceException characterFailure = Assert.Throws<XmlResourceException>(() => new XmlIncludeProcessor().ProcessXmlIncludes(invalidCharacters, Options(), invalidCharacterResolver))!;
        Assert.That(characterFailure.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.IncludeFailure));
    }

    [Test]
    public void RemovesAnIncludeForAnEmptyTextResourceAndClosesOwnedStreams()
    {
        TextIncludeResolver resolver = new TextIncludeResolver();
        TrackingMemoryStream stream = new TrackingMemoryStream([]);
        resolver.AddStream("empty.txt", stream, true);
        XmlDocument document = Parse("<root xmlns:xi='" + Xi + "'><before/><xi:include href='empty.txt' parse='text'/><after/></root>", XmlVersion.Xml10);

        new XmlIncludeProcessor().ProcessXmlIncludes(document, Options(), resolver);

        Assert.Multiple(() =>
        {
            Assert.That(document.DocumentElement!.Children, Has.Length.EqualTo(2));
            Assert.That(document.DocumentElement.Children![0].Name!.LocalName, Is.EqualTo("before"));
            Assert.That(document.DocumentElement.Children[1].Name!.LocalName, Is.EqualTo("after"));
            Assert.That(stream.WasDisposed, Is.True);
        });
    }

    [Test]
    public void RejectsXPointerTextIncludesInvalidAcceptMetadataAndDocumentLevelText()
    {
        TextIncludeResolver resolver = new TextIncludeResolver();
        resolver.AddCharacters("text.txt", "text");
        string[] sources =
        [
            "<root xmlns:xi='" + Xi + "'><xi:include href='text.txt' parse='text' xpointer='element(/1)'/></root>",
            "<root xmlns:xi='" + Xi + "'><xi:include href='text.txt' parse='text' accept='text/é'/></root>",
            "<xi:include xmlns:xi='" + Xi + "' href='text.txt' parse='text'/>"
        ];

        foreach (string source in sources)
        {
            XmlDocument document = Parse(source, XmlVersion.Xml10);
            XmlResourceException failure = Assert.Throws<XmlResourceException>(() => new XmlIncludeProcessor().ProcessXmlIncludes(document, Options(), resolver))!;
            Assert.That(failure.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.IncludeFailure));
        }

        XmlDocument optionMetadata = Parse("<root xmlns:xi='" + Xi + "'><xi:include href='text.txt' parse='text'/></root>", XmlVersion.Xml10);
        XmlIncludeOptions options = Options();
        options.AcceptLanguage = "en\nUS";
        Assert.Throws<XmlResourceException>(() => new XmlIncludeProcessor().ProcessXmlIncludes(optionMetadata, options, resolver));
    }

    private static XmlDocument Parse(string text, XmlVersion version)
    {
        XmlInput input = new XmlInput { Kind = XmlInputKind.String, Text = text, BaseUri = "https://example.test/source.xml" };
        XmlParserOptions options = new XmlParserOptions
        {
            Version = version,
            NamespaceHandling = version == XmlVersion.Xml11 ? XmlNamespaceHandling.Namespaces11 : XmlNamespaceHandling.Namespaces10,
            DtdProcessing = XmlDtdProcessing.Prohibit,
            EntityHandling = XmlEntityHandling.Expand,
            ValidationMode = XmlValidationMode.None,
            PreservePrefixes = true,
            PreserveSourceLocations = true,
            RetainNamespaceDeclarations = true
        };
        return new XmlParser().ParseXmlDocument(input, options, null, null, null);
    }

    private static XmlIncludeOptions Options()
    {
        return new XmlIncludeOptions
        {
            ResolverPolicy = new XmlResolverPolicy
            {
                Access = XmlResourceAccess.Allowed,
                MaximumDepth = 32,
                MaximumResourceBytes = 1048576,
                MaximumWorkUnits = 1048576
            }
        };
    }

    private sealed class TextIncludeResolver : IXmlResourceResolver
    {
        private readonly Dictionary<string, TextResource> resources = [];
        public readonly List<TextRequest> Requests = [];

        public void AddCharacters(string reference, string text)
        {
            resources[reference] = new TextResource
            {
                Input = new XmlInput { Kind = XmlInputKind.String, Text = text },
                AbsoluteUri = "https://example.test/" + reference
            };
        }

        public void AddBytes(string reference, byte[] bytes, string? encodingName, string? mediaType)
        {
            resources[reference] = new TextResource
            {
                Input = new XmlInput { Kind = XmlInputKind.ByteMemory, Bytes = bytes },
                AbsoluteUri = "https://example.test/" + reference,
                EncodingName = encodingName,
                MediaType = mediaType
            };
        }

        public void AddStream(string reference, Stream stream, bool ownsResource)
        {
            resources[reference] = new TextResource
            {
                Input = new XmlInput { Kind = XmlInputKind.Stream, ByteStream = stream },
                AbsoluteUri = "https://example.test/" + reference,
                OwnsResource = ownsResource
            };
        }

        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            Requests.Add(new TextRequest { Kind = kind, BaseUri = baseUri, Accept = policy.Accept, AcceptLanguage = policy.AcceptLanguage });
            if (systemIdentifier == null || !resources.TryGetValue(systemIdentifier, out TextResource? resource))
            {
                throw new InvalidOperationException("No text resource was registered for " + systemIdentifier + ".");
            }

            return new XmlResolvedResource
            {
                Kind = kind,
                Access = XmlResourceAccess.Allowed,
                Input = resource.Input,
                RequestedUri = systemIdentifier,
                AbsoluteUri = resource.AbsoluteUri,
                EncodingName = resource.EncodingName,
                MediaType = resource.MediaType,
                OwnsResource = resource.OwnsResource
            };
        }
    }

    private sealed class TextResource
    {
        public XmlInput? Input;
        public string? AbsoluteUri;
        public string? EncodingName;
        public string? MediaType;
        public bool OwnsResource;
    }

    private sealed class TextRequest
    {
        public XmlResourceKind Kind;
        public string? BaseUri;
        public string? Accept;
        public string? AcceptLanguage;
    }

    private sealed class TrackingMemoryStream : MemoryStream
    {
        public bool WasDisposed;

        public TrackingMemoryStream(byte[] bytes) : base(bytes)
        {
        }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }
}
