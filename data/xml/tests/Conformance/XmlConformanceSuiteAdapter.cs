using System.IO.Compression;
using System.Net;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using xml;

namespace xml.Tests;

internal enum XmlConformanceClassification
{
    Valid,
    Invalid,
    NotWellFormed,
    Error
}

internal enum XmlConformanceExpectation
{
    Accept,
    RejectValidity,
    RejectWellFormedness,
    OptionalError
}

internal sealed class XmlConformanceTestCase
{
    internal string Id = "";
    internal string Profile = "";
    internal string Recommendation = "";
    internal string Sections = "";
    internal string Description = "";
    internal string ExpectedDiagnosticText = "";
    internal string EntityUsage = "";
    internal string InputPath = "";
    internal string InputUri = "";
    internal string? OutputPath;
    internal string? ValidatingOutputPath;
    internal XmlConformanceClassification Classification;
    internal XmlVersion Version;
    internal XmlNamespaceHandling NamespaceHandling;
    internal int[] Editions = [];

    internal XmlConformanceExpectation ExpectedOutcome(bool validating)
    {
        if (Classification == XmlConformanceClassification.Valid)
        {
            return XmlConformanceExpectation.Accept;
        }

        if (Classification == XmlConformanceClassification.Invalid)
        {
            return validating ? XmlConformanceExpectation.RejectValidity : XmlConformanceExpectation.Accept;
        }

        return Classification == XmlConformanceClassification.NotWellFormed ? XmlConformanceExpectation.RejectWellFormedness : XmlConformanceExpectation.OptionalError;
    }

    internal XmlErrorCategory[] ExpectedDiagnosticCategories(bool validating)
    {
        XmlConformanceExpectation expectation = ExpectedOutcome(validating);
        if (expectation == XmlConformanceExpectation.Accept)
        {
            return [];
        }

        if (expectation == XmlConformanceExpectation.RejectValidity)
        {
            return [XmlErrorCategory.Validation];
        }

        if (Recommendation == "NS1.0" || Recommendation == "NS1.1" || Recommendation == "NS1.0-errata1e")
        {
            return [XmlErrorCategory.Namespace, XmlErrorCategory.Xml];
        }

        return [XmlErrorCategory.Xml, XmlErrorCategory.Dtd];
    }

    internal bool AppliesToEdition(int edition)
    {
        return Editions.Length == 0 || Editions.Contains(edition);
    }
}

internal sealed class XmlConformanceSuiteAdapter
{
    internal const string ArchiveBaseUri = "https://xmlts.test/";
    internal const string RootCatalogPath = "xmlconf/xmlconf.xml";
    internal Dictionary<string, byte[]> Resources;
    internal XmlConformanceTestCase[] TestCases;

    internal XmlConformanceSuiteAdapter(string archivePath)
    {
        Resources = ReadArchive(archivePath);
        TestCases = ReadCatalog();
    }

    internal XmlInput CreateInput(XmlConformanceTestCase test)
    {
        return new XmlInput
        {
            Kind = XmlInputKind.ByteMemory,
            Bytes = ReadResource(test.InputPath),
            BaseUri = test.InputUri
        };
    }

    internal byte[] ReadResource(string archivePath)
    {
        if (!Resources.TryGetValue(NormalizeArchivePath(archivePath), out byte[]? value))
        {
            throw new FileNotFoundException("The XML conformance resource was not found in the suite archive.", archivePath);
        }

        return value.ToArray();
    }

    internal IXmlResourceResolver CreateResourceResolver()
    {
        return new XmlConformanceResourceResolver(Resources);
    }

    internal XmlParserOptions CreateParserOptions(XmlConformanceTestCase test, bool validating)
    {
        return new XmlParserOptions
        {
            Version = test.Version,
            NamespaceHandling = test.NamespaceHandling,
            DtdProcessing = XmlDtdProcessing.Parse,
            EntityHandling = XmlEntityHandling.Expand,
            ValidationMode = validating ? XmlValidationMode.Dtd : XmlValidationMode.None,
            AccumulateValidationDiagnostics = true,
            ResolverPolicy = new XmlResolverPolicy
            {
                Access = XmlResourceAccess.Allowed,
                AllowedSchemes = ["https"],
                MaximumDepth = 64,
                MaximumResourceBytes = 16777216,
                MaximumWorkUnits = 268435456
            },
            SecurityLimits = new XmlSecurityLimits
            {
                MaximumDepth = 1024,
                MaximumNodeCount = 2000000,
                MaximumAttributesPerElement = 100000,
                MaximumNameLength = 1048576,
                MaximumTextLength = 67108864,
                MaximumTokenLength = 67108864,
                MaximumInputCharacters = 134217728,
                MaximumInputBytes = 134217728,
                MaximumDtdCharacters = 67108864,
                MaximumEntityDepth = 64,
                MaximumEntityCount = 100000,
                MaximumEntityCharacters = 134217728,
                MaximumEntityAmplification = 100000,
                MaximumExternalResources = 10000,
                MaximumResourceDepth = 64,
                MaximumResourceBytes = 16777216,
                CancellationPollingInterval = 1024
            }
        };
    }

    internal Dictionary<string, byte[]> ReadArchive(string archivePath)
    {
        Dictionary<string, byte[]> result = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        using FileStream stream = File.OpenRead(archivePath);
        using ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read, false);
        int index = 0;
        while (index < archive.Entries.Count)
        {
            ZipArchiveEntry entry = archive.Entries[index];
            if (entry.Name.Length != 0)
            {
                using Stream input = entry.Open();
                using MemoryStream content = new MemoryStream();
                input.CopyTo(content);
                result.Add(NormalizeArchivePath(entry.FullName), content.ToArray());
            }

            index = index + 1;
        }

        return result;
    }

    internal XmlConformanceTestCase[] ReadCatalog()
    {
        XmlConformanceArchiveResolver resolver = new XmlConformanceArchiveResolver(Resources);
        System.Xml.XmlReaderSettings settings = new System.Xml.XmlReaderSettings
        {
            DtdProcessing = System.Xml.DtdProcessing.Parse,
            XmlResolver = resolver,
            MaxCharactersFromEntities = 67108864,
            MaxCharactersInDocument = 134217728
        };
        Uri catalogUri = new Uri(new Uri(ArchiveBaseUri), RootCatalogPath);
        using Stream stream = resolver.OpenArchiveUri(catalogUri);
        using System.Xml.XmlReader reader = System.Xml.XmlReader.Create(stream, settings, catalogUri.AbsoluteUri);
        XDocument catalog = XDocument.Load(reader, LoadOptions.SetBaseUri);
        return catalog.Descendants("TEST").Select(element => CreateTestCase(element, resolver)).ToArray();
    }

    internal XmlConformanceTestCase CreateTestCase(XElement element, XmlConformanceArchiveResolver resolver)
    {
        string recommendation = ReadAttribute(element, "RECOMMENDATION", "XML1.0");
        string inputUri = ResolveTestUri(element, ReadRequiredAttribute(element, "URI"));
        XmlConformanceTestCase result = new XmlConformanceTestCase();
        result.Id = ReadRequiredAttribute(element, "ID");
        result.Profile = ReadProfile(element);
        result.Recommendation = recommendation;
        result.Sections = ReadRequiredAttribute(element, "SECTIONS");
        result.Description = Regex.Replace(element.Value, "\\s+", " ").Trim();
        result.ExpectedDiagnosticText = result.Description;
        result.EntityUsage = ReadAttribute(element, "ENTITIES", "none");
        result.InputUri = inputUri;
        result.InputPath = resolver.GetArchivePath(new Uri(inputUri));
        result.OutputPath = ResolveOptionalTestPath(element, resolver, "OUTPUT");
        result.ValidatingOutputPath = ResolveOptionalTestPath(element, resolver, "OUTPUT3");
        result.Classification = ReadClassification(ReadRequiredAttribute(element, "TYPE"));
        result.Version = ReadVersion(element, recommendation);
        result.NamespaceHandling = ReadNamespaceHandling(element, recommendation);
        result.Editions = ReadEditions(element);
        return result;
    }

    internal string ResolveTestUri(XElement element, string relative)
    {
        string basis = element.BaseUri.Length == 0 ? new Uri(new Uri(ArchiveBaseUri), RootCatalogPath).AbsoluteUri : element.BaseUri;
        return new Uri(new Uri(basis), relative).AbsoluteUri;
    }

    internal string? ResolveOptionalTestPath(XElement element, XmlConformanceArchiveResolver resolver, string name)
    {
        string? relative = (string?)element.Attribute(name);
        return relative == null ? null : resolver.GetArchivePath(new Uri(ResolveTestUri(element, relative)));
    }

    internal string ReadProfile(XElement element)
    {
        XElement? current = element.Parent;
        while (current != null)
        {
            string? profile = (string?)current.Attribute("PROFILE");
            if (profile != null && profile.Length != 0)
            {
                return profile;
            }

            current = current.Parent;
        }

        return "Unprofiled XML conformance tests";
    }

    internal XmlConformanceClassification ReadClassification(string value)
    {
        if (value == "valid")
        {
            return XmlConformanceClassification.Valid;
        }

        if (value == "invalid")
        {
            return XmlConformanceClassification.Invalid;
        }

        if (value == "not-wf")
        {
            return XmlConformanceClassification.NotWellFormed;
        }

        if (value == "error")
        {
            return XmlConformanceClassification.Error;
        }

        throw new InvalidDataException("Unknown XML conformance classification: " + value);
    }

    internal XmlVersion ReadVersion(XElement element, string recommendation)
    {
        string? version = (string?)element.Attribute("VERSION");
        if (version != null)
        {
            string[] values = version.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (values.Length == 1)
            {
                return values[0] == "1.1" ? XmlVersion.Xml11 : XmlVersion.Xml10;
            }
        }

        return recommendation == "XML1.1" || recommendation == "NS1.1" ? XmlVersion.Xml11 : XmlVersion.Xml10;
    }

    internal XmlNamespaceHandling ReadNamespaceHandling(XElement element, string recommendation)
    {
        if (ReadAttribute(element, "NAMESPACE", "yes") == "no")
        {
            return XmlNamespaceHandling.Disabled;
        }

        return recommendation == "NS1.1" ? XmlNamespaceHandling.Namespaces11 : XmlNamespaceHandling.Namespaces10;
    }

    internal int[] ReadEditions(XElement element)
    {
        string? editions = (string?)element.Attribute("EDITION");
        return editions == null
            ? []
            : editions.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Select(value => int.Parse(value, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
    }

    internal string ReadRequiredAttribute(XElement element, string name)
    {
        string? value = (string?)element.Attribute(name);
        return value == null || value.Length == 0 ? throw new InvalidDataException("The XML conformance TEST is missing " + name + ".") : value;
    }

    internal string ReadAttribute(XElement element, string name, string fallback)
    {
        string? value = (string?)element.Attribute(name);
        return value == null ? fallback : value;
    }

    internal static string NormalizeArchivePath(string path)
    {
        return path.Replace('\\', '/').TrimStart('/');
    }
}

internal sealed class XmlConformanceArchiveResolver : System.Xml.XmlResolver
{
    internal Dictionary<string, byte[]> Resources;

    internal XmlConformanceArchiveResolver(Dictionary<string, byte[]> resources)
    {
        Resources = resources;
    }

    public override ICredentials? Credentials
    {
        set
        {
        }
    }

    public override object GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn)
    {
        return OpenArchiveUri(absoluteUri);
    }

    internal Stream OpenArchiveUri(Uri absoluteUri)
    {
        string path = GetArchivePath(absoluteUri);
        if (!Resources.TryGetValue(path, out byte[]? bytes))
        {
            throw new FileNotFoundException("The XML conformance catalog resource was not found.", path);
        }

        return new MemoryStream(bytes, false);
    }

    internal string GetArchivePath(Uri absoluteUri)
    {
        if (absoluteUri.Scheme != "https" || absoluteUri.Host != "xmlts.test")
        {
            throw new InvalidOperationException("The XML conformance adapter cannot resolve outside its archive URI space.");
        }

        return XmlConformanceSuiteAdapter.NormalizeArchivePath(absoluteUri.GetComponents(UriComponents.Path, UriFormat.Unescaped));
    }
}

internal sealed class XmlConformanceResourceResolver : IXmlResourceResolver
{
    internal Dictionary<string, byte[]> Resources;

    internal XmlConformanceResourceResolver(Dictionary<string, byte[]> resources)
    {
        Resources = resources;
    }

    public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
    {
        string requested = systemIdentifier == null ? "" : systemIdentifier;
        Uri? absolute = ResolveUri(requested, baseUri);
        string? path = absolute == null ? null : XmlConformanceSuiteAdapter.NormalizeArchivePath(absolute.GetComponents(UriComponents.Path, UriFormat.Unescaped));
        bool available = absolute != null && absolute.Scheme == "https" && absolute.Host == "xmlts.test" && path != null && Resources.ContainsKey(path);
        XmlResolvedResource result = new XmlResolvedResource();
        result.Kind = kind;
        result.Access = available ? XmlResourceAccess.Allowed : XmlResourceAccess.Denied;
        result.RequestedUri = requested;
        result.AbsoluteUri = absolute == null ? null : absolute.AbsoluteUri;
        result.PublicIdentifier = publicIdentifier;
        result.SystemIdentifier = systemIdentifier;
        if (available && path != null)
        {
            result.Input = new XmlInput { Kind = XmlInputKind.ByteMemory, Bytes = Resources[path].ToArray(), BaseUri = absolute!.AbsoluteUri };
        }

        return result;
    }

    internal Uri? ResolveUri(string requested, string? baseUri)
    {
        if (Uri.TryCreate(requested, UriKind.Absolute, out Uri? absolute))
        {
            return absolute;
        }

        return baseUri == null || !Uri.TryCreate(baseUri, UriKind.Absolute, out Uri? basis) ? null : new Uri(basis, requested);
    }
}
