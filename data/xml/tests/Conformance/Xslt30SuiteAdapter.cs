using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace xml.Tests;

internal sealed class Xslt30CatalogNode
{
    internal string Kind = "";
    internal string NamespaceUri = "";
    internal string Value = "";
    internal string? FilePath;
    internal string? NameNamespaceUri;
    internal Dictionary<string, string> Attributes = new Dictionary<string, string>(StringComparer.Ordinal);
    internal Dictionary<string, string> Namespaces = new Dictionary<string, string>(StringComparer.Ordinal);
    internal Xslt30CatalogNode[] Children = [];

    internal Xslt30CatalogNode[] Find(string kind)
    {
        return Children.Where(child => child.Kind == kind).ToArray();
    }
}

internal sealed class Xslt30Dependency
{
    internal string Kind = "";
    internal string Value = "";
    internal bool Satisfied = true;
    internal string Range = "unbounded";
    internal Dictionary<string, string> Attributes = new Dictionary<string, string>(StringComparer.Ordinal);
}

internal sealed class Xslt30Environment
{
    internal string Name = "";
    internal string Reference = "";
    internal string CatalogPath = "";
    internal Xslt30CatalogNode[] Nodes = [];

    internal Xslt30CatalogNode[] Find(string kind)
    {
        return Nodes.Where(node => node.Kind == kind).ToArray();
    }
}

internal sealed class Xslt30Assertion
{
    internal string Kind = "";
    internal string NamespaceUri = "";
    internal string Value = "";
    internal string? FilePath;
    internal Dictionary<string, string> Attributes = new Dictionary<string, string>(StringComparer.Ordinal);
    internal Dictionary<string, string> Namespaces = new Dictionary<string, string>(StringComparer.Ordinal);
    internal Xslt30Assertion[] Children = [];
}

internal sealed class Xslt30Invocation
{
    internal Xslt30CatalogNode[] Packages = [];
    internal Xslt30CatalogNode[] Stylesheets = [];
    internal Xslt30CatalogNode[] Parameters = [];
    internal Xslt30CatalogNode? InitialTemplate;
    internal Xslt30CatalogNode? InitialMode;
    internal Xslt30CatalogNode? InitialFunction;
    internal Xslt30CatalogNode? PostureAndSweep;
    internal Xslt30CatalogNode? Output;
    internal Xslt30CatalogNode[] Nodes = [];
}

internal sealed class Xslt30TestCase
{
    internal string Name = "";
    internal string TestSetName = "";
    internal string TestSetPath = "";
    internal string Description = "";
    internal string Keywords = "";
    internal Xslt30Environment Environment = new Xslt30Environment();
    internal Xslt30Dependency[] Dependencies = [];
    internal Xslt30Invocation Invocation = new Xslt30Invocation();
    internal Xslt30Assertion Assertion = new Xslt30Assertion();
}

internal sealed class Xslt30TestSet
{
    internal string Name = "";
    internal string CatalogPath = "";
    internal Xslt30Environment[] Environments = [];
    internal Xslt30Dependency[] Dependencies = [];
    internal Xslt30TestCase[] TestCases = [];
}

internal sealed class Xslt30SuiteAdapter : IDisposable
{
    internal const string ArchiveBaseUri = "https://xslt30.test/";
    internal const string CatalogNamespace = "http://www.w3.org/2012/10/xslt-test-catalog";
    internal static readonly string[] AssertionKinds =
    [
        "all-of", "any-of", "not", "assert", "assert-message", "assert-result-document", "assert-xml",
        "assert-warning", "assert-serialization", "serialization-matches", "assert-serialization-error",
        "assert-type", "assert-eq", "assert-count", "assert-deep-eq", "assert-permutation", "assert-empty",
        "assert-true", "assert-false", "assert-string-value", "error", "assert-posture-and-sweep"
    ];

    internal string ArchivePath;
    internal string RootPath;
    internal string CatalogPath;
    internal Dictionary<string, string> ResourceEntries;
    internal Dictionary<string, Xslt30Environment> GlobalEnvironments;
    internal Xslt30Dependency[] CatalogDependencies;
    internal Xslt30TestSet[] TestSets;
    internal Xslt30TestCase[] TestCases;
    internal FileStream ResourceStream;
    internal ZipArchive ResourceArchive;
    internal object ResourceLock;

    internal Xslt30SuiteAdapter(string archivePath)
    {
        ArchivePath = archivePath;
        ResourceEntries = new Dictionary<string, string>(StringComparer.Ordinal);
        GlobalEnvironments = new Dictionary<string, Xslt30Environment>(StringComparer.Ordinal);
        CatalogDependencies = [];
        TestSets = [];
        TestCases = [];
        ResourceStream = File.OpenRead(archivePath);
        ResourceArchive = new ZipArchive(ResourceStream, ZipArchiveMode.Read, false);
        ResourceLock = new object();
        IndexArchive(ResourceArchive);
        CatalogPath = FindCatalogPath();
        RootPath = CatalogPath.Substring(0, CatalogPath.Length - "catalog.xml".Length);
        LoadCatalog(ResourceArchive);
    }

    internal void IndexArchive(ZipArchive archive)
    {
        int index = 0;
        while (index < archive.Entries.Count)
        {
            ZipArchiveEntry entry = archive.Entries[index];
            if (entry.Name.Length != 0)
            {
                string path = NormalizePath(entry.FullName);
                if (ResourceEntries.ContainsKey(path))
                {
                    throw new InvalidDataException("The XSLT 3.0 archive contains duplicate path '" + path + "'.");
                }

                ResourceEntries[path] = entry.FullName;
            }

            index = index + 1;
        }
    }

    internal string FindCatalogPath()
    {
        string[] matches = ResourceEntries.Keys.Where(path => path.EndsWith("/catalog.xml", StringComparison.Ordinal) && path.Count(character => character == '/') == 1).ToArray();
        if (matches.Length != 1)
        {
            throw new InvalidDataException("The XSLT 3.0 archive must contain exactly one root catalog.");
        }

        return matches[0];
    }

    internal void LoadCatalog(ZipArchive archive)
    {
        XDocument catalog = ReadXml(archive, CatalogPath);
        XNamespace ns = CatalogNamespace;
        XElement root = catalog.Root ?? throw new InvalidDataException("The XSLT 3.0 catalog has no document element.");
        if (root.Name != ns + "catalog")
        {
            throw new InvalidDataException("The XSLT 3.0 catalog document element is not catalog.");
        }

        XElement[] globalEnvironmentElements = root.Elements(ns + "environment").ToArray();
        int environmentIndex = 0;
        while (environmentIndex < globalEnvironmentElements.Length)
        {
            Xslt30Environment environment = ParseEnvironment(globalEnvironmentElements[environmentIndex], CatalogPath);
            if (environment.Name.Length != 0)
            {
                GlobalEnvironments[environment.Name] = environment;
            }

            environmentIndex = environmentIndex + 1;
        }

        CatalogDependencies = ParseDependencies(root.Element(ns + "dependencies"));
        List<Xslt30TestSet> sets = new List<Xslt30TestSet>();
        List<Xslt30TestCase> cases = new List<Xslt30TestCase>();
        XElement[] references = root.Elements(ns + "test-set").ToArray();
        int referenceIndex = 0;
        while (referenceIndex < references.Length)
        {
            string file = RequiredAttribute(references[referenceIndex], "file");
            string path = ResolvePath(DirectoryPath(CatalogPath), file);
            Xslt30TestSet set = LoadTestSet(archive, path);
            string catalogName = RequiredAttribute(references[referenceIndex], "name");
            if (set.Name != catalogName)
            {
                throw new InvalidDataException("The XSLT test-set name '" + set.Name + "' does not match catalog name '" + catalogName + "'.");
            }

            sets.Add(set);
            cases.AddRange(set.TestCases);
            referenceIndex = referenceIndex + 1;
        }

        TestSets = sets.ToArray();
        TestCases = cases.ToArray();
    }

    internal Xslt30TestSet LoadTestSet(ZipArchive archive, string path)
    {
        XDocument document = ReadXml(archive, path);
        XNamespace ns = CatalogNamespace;
        XElement root = document.Root ?? throw new InvalidDataException("An XSLT test set has no document element.");
        if (root.Name != ns + "test-set")
        {
            throw new InvalidDataException("The XSLT test-set document element is not test-set.");
        }

        Xslt30TestSet set = new Xslt30TestSet();
        set.Name = RequiredAttribute(root, "name");
        set.CatalogPath = path;
        XElement[] environmentElements = root.Elements(ns + "environment").ToArray();
        List<Xslt30Environment> environments = new List<Xslt30Environment>();
        Dictionary<string, Xslt30Environment> namedEnvironments = new Dictionary<string, Xslt30Environment>(StringComparer.Ordinal);
        int environmentIndex = 0;
        while (environmentIndex < environmentElements.Length)
        {
            Xslt30Environment environment = ParseEnvironment(environmentElements[environmentIndex], path);
            environments.Add(environment);
            if (environment.Name.Length != 0)
            {
                namedEnvironments[environment.Name] = environment;
            }

            environmentIndex = environmentIndex + 1;
        }

        set.Environments = environments.ToArray();
        set.Dependencies = CatalogDependencies.Concat(ParseDependencies(root.Element(ns + "dependencies"))).ToArray();
        XElement[] caseElements = root.Elements(ns + "test-case").ToArray();
        Xslt30TestCase[] testCases = new Xslt30TestCase[caseElements.Length];
        int caseIndex = 0;
        while (caseIndex < caseElements.Length)
        {
            testCases[caseIndex] = ParseTestCase(caseElements[caseIndex], set, namedEnvironments);
            caseIndex = caseIndex + 1;
        }

        set.TestCases = testCases;
        return set;
    }

    internal Xslt30TestCase ParseTestCase(XElement element, Xslt30TestSet set, Dictionary<string, Xslt30Environment> namedEnvironments)
    {
        XNamespace ns = CatalogNamespace;
        Xslt30TestCase test = new Xslt30TestCase();
        test.Name = RequiredAttribute(element, "name");
        test.TestSetName = set.Name;
        test.TestSetPath = set.CatalogPath;
        test.Description = OptionalElementValue(element, ns + "description");
        test.Keywords = OptionalElementValue(element, ns + "keywords");
        test.Environment = ResolveEnvironment(element.Element(ns + "environment"), set.CatalogPath, namedEnvironments);
        test.Dependencies = set.Dependencies.Concat(ParseDependencies(element.Element(ns + "dependencies"))).ToArray();
        XElement invocation = element.Element(ns + "test") ?? throw new InvalidDataException("XSLT test '" + test.Name + "' has no test invocation.");
        test.Invocation = ParseInvocation(invocation, set.CatalogPath);
        XElement result = element.Element(ns + "result") ?? throw new InvalidDataException("XSLT test '" + test.Name + "' has no result.");
        XElement[] assertions = result.Elements().ToArray();
        if (assertions.Length != 1)
        {
            throw new InvalidDataException("XSLT test '" + test.Name + "' must contain exactly one result assertion.");
        }

        test.Assertion = ParseAssertion(assertions[0], set.CatalogPath);
        return test;
    }

    internal Xslt30Environment ResolveEnvironment(XElement? element, string catalogPath, Dictionary<string, Xslt30Environment> namedEnvironments)
    {
        if (element == null)
        {
            return new Xslt30Environment { CatalogPath = catalogPath };
        }

        string? reference = OptionalAttribute(element, "ref");
        if (reference == null)
        {
            return ParseEnvironment(element, catalogPath);
        }

        if (namedEnvironments.TryGetValue(reference, out Xslt30Environment? local))
        {
            return local;
        }

        if (GlobalEnvironments.TryGetValue(reference, out Xslt30Environment? global))
        {
            return global;
        }

        throw new InvalidDataException("XSLT environment '" + reference + "' is not defined.");
    }

    internal Xslt30Environment ParseEnvironment(XElement element, string catalogPath)
    {
        Xslt30Environment environment = new Xslt30Environment();
        environment.Name = OptionalAttribute(element, "name") ?? "";
        environment.Reference = OptionalAttribute(element, "ref") ?? "";
        environment.CatalogPath = catalogPath;
        environment.Nodes = element.Elements().Select(child => ParseCatalogNode(child, catalogPath)).ToArray();
        return environment;
    }

    internal Xslt30Invocation ParseInvocation(XElement element, string catalogPath)
    {
        Xslt30CatalogNode[] nodes = element.Elements().Select(child => ParseCatalogNode(child, catalogPath)).ToArray();
        Xslt30Invocation invocation = new Xslt30Invocation();
        invocation.Nodes = nodes;
        invocation.Packages = nodes.Where(node => node.Kind == "package").ToArray();
        invocation.Stylesheets = nodes.Where(node => node.Kind == "stylesheet").ToArray();
        invocation.Parameters = nodes.Where(node => node.Kind == "param").ToArray();
        invocation.InitialTemplate = nodes.SingleOrDefault(node => node.Kind == "initial-template");
        invocation.InitialMode = nodes.SingleOrDefault(node => node.Kind == "initial-mode");
        invocation.InitialFunction = nodes.SingleOrDefault(node => node.Kind == "initial-function");
        invocation.PostureAndSweep = nodes.SingleOrDefault(node => node.Kind == "posture-and-sweep");
        invocation.Output = nodes.SingleOrDefault(node => node.Kind == "output");
        return invocation;
    }

    internal Xslt30CatalogNode ParseCatalogNode(XElement element, string catalogPath)
    {
        Xslt30CatalogNode node = new Xslt30CatalogNode();
        node.Kind = element.Name.LocalName;
        node.NamespaceUri = element.Name.NamespaceName;
        node.Value = element.HasElements ? "" : element.Value;
        node.Attributes = ReadAttributes(element);
        node.Namespaces = ReadNamespaces(element);
        string? file = OptionalAttribute(element, "file");
        if (file != null && file != "#absent")
        {
            node.FilePath = ResolvePath(DirectoryPath(catalogPath), file);
        }

        string? name = OptionalAttribute(element, "name");
        if (name != null)
        {
            node.NameNamespaceUri = ResolveQNameNamespace(element, name);
        }

        node.Children = element.Elements().Select(child => ParseCatalogNode(child, catalogPath)).ToArray();
        return node;
    }

    internal Xslt30Dependency[] ParseDependencies(XElement? dependencies)
    {
        if (dependencies == null)
        {
            return [];
        }

        return dependencies.Elements().Select(element =>
        {
            Xslt30Dependency dependency = new Xslt30Dependency();
            dependency.Kind = element.Name.LocalName;
            dependency.Value = OptionalAttribute(element, "value") ?? "";
            string satisfied = OptionalAttribute(element, "satisfied") ?? "true";
            dependency.Satisfied = satisfied != "false" && satisfied != "0";
            dependency.Range = OptionalAttribute(element, "range") ?? "unbounded";
            dependency.Attributes = ReadAttributes(element);
            return dependency;
        }).ToArray();
    }

    internal Xslt30Assertion ParseAssertion(XElement element, string catalogPath)
    {
        Xslt30Assertion assertion = new Xslt30Assertion();
        assertion.Kind = element.Name.LocalName;
        assertion.NamespaceUri = element.Name.NamespaceName;
        assertion.Value = element.HasElements ? "" : element.Value;
        assertion.Attributes = ReadAttributes(element);
        assertion.Namespaces = ReadNamespaces(element);
        string? file = OptionalAttribute(element, "file");
        if (file != null)
        {
            assertion.FilePath = ResolvePath(DirectoryPath(catalogPath), file);
        }

        assertion.Children = element.Elements().Select(child => ParseAssertion(child, catalogPath)).ToArray();
        return assertion;
    }

    internal Dictionary<string, string> ReadAttributes(XElement element)
    {
        Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.Ordinal);
        XAttribute[] attributes = element.Attributes().Where(attribute => !attribute.IsNamespaceDeclaration).ToArray();
        int index = 0;
        while (index < attributes.Length)
        {
            XAttribute attribute = attributes[index];
            string key = attribute.Name.NamespaceName.Length == 0 ? attribute.Name.LocalName : "Q{" + attribute.Name.NamespaceName + "}" + attribute.Name.LocalName;
            result[key] = attribute.Value;
            index = index + 1;
        }

        return result;
    }

    internal Dictionary<string, string> ReadNamespaces(XElement element)
    {
        Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.Ordinal);
        XElement[] scope = element.AncestorsAndSelf().Reverse().ToArray();
        int elementIndex = 0;
        while (elementIndex < scope.Length)
        {
            XAttribute[] declarations = scope[elementIndex].Attributes().Where(attribute => attribute.IsNamespaceDeclaration).ToArray();
            int declarationIndex = 0;
            while (declarationIndex < declarations.Length)
            {
                XAttribute declaration = declarations[declarationIndex];
                string prefix = declaration.Name.LocalName == "xmlns" ? "" : declaration.Name.LocalName;
                result[prefix] = declaration.Value;
                declarationIndex = declarationIndex + 1;
            }

            elementIndex = elementIndex + 1;
        }

        return result;
    }

    internal string? ResolveQNameNamespace(XElement element, string lexical)
    {
        if (lexical.StartsWith("#", StringComparison.Ordinal))
        {
            return null;
        }

        if (lexical.StartsWith("Q{", StringComparison.Ordinal))
        {
            int closing = lexical.IndexOf('}');
            return closing < 0 ? null : lexical.Substring(2, closing - 2);
        }

        int colon = lexical.IndexOf(':');
        if (colon < 0)
        {
            return "";
        }

        XNamespace? resolved = element.GetNamespaceOfPrefix(lexical.Substring(0, colon));
        return resolved == null ? null : resolved.NamespaceName;
    }

    internal bool ContainsResource(string path)
    {
        return ResourceEntries.ContainsKey(NormalizePath(path));
    }

    internal byte[] ReadBytes(string path)
    {
        string normalized = NormalizePath(path);
        if (!ResourceEntries.TryGetValue(normalized, out string? entryName))
        {
            throw new FileNotFoundException("The XSLT suite resource was not found.", normalized);
        }

        lock (ResourceLock)
        {
            ZipArchiveEntry entry = ResourceArchive.GetEntry(entryName) ?? throw new FileNotFoundException("The XSLT suite archive entry was not found.", normalized);
            using Stream input = entry.Open();
            using MemoryStream result = new MemoryStream();
            input.CopyTo(result);
            return result.ToArray();
        }
    }

    internal string ReadText(string path)
    {
        byte[] bytes = ReadBytes(path);
        using MemoryStream stream = new MemoryStream(bytes, false);
        using StreamReader reader = new StreamReader(stream, Encoding.UTF8, true);
        return reader.ReadToEnd();
    }

    internal string ResourceUri(string path)
    {
        return new Uri(new Uri(ArchiveBaseUri), NormalizePath(path)).AbsoluteUri;
    }

    public void Dispose()
    {
        ResourceArchive.Dispose();
        ResourceStream.Dispose();
    }

    internal XDocument ReadXml(ZipArchive archive, string path)
    {
        string normalized = NormalizePath(path);
        if (!ResourceEntries.TryGetValue(normalized, out string? entryName))
        {
            throw new FileNotFoundException("The XSLT suite catalog resource was not found.", normalized);
        }

        ZipArchiveEntry entry = archive.GetEntry(entryName) ?? throw new FileNotFoundException("The XSLT suite archive entry was not found.", normalized);
        using Stream stream = entry.Open();
        return XDocument.Load(stream, LoadOptions.PreserveWhitespace);
    }

    internal string ResolvePath(string directory, string relative)
    {
        if (relative.Contains("://", StringComparison.Ordinal))
        {
            return relative;
        }

        return NormalizePath(directory + relative);
    }

    internal string DirectoryPath(string path)
    {
        int slash = path.LastIndexOf('/');
        return slash < 0 ? "" : path.Substring(0, slash + 1);
    }

    internal string NormalizePath(string path)
    {
        string[] parts = path.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        List<string> normalized = new List<string>();
        int index = 0;
        while (index < parts.Length)
        {
            if (parts[index] == ".")
            {
                index = index + 1;
                continue;
            }

            if (parts[index] == "..")
            {
                if (normalized.Count == 0)
                {
                    throw new InvalidDataException("An XSLT suite path escapes the archive root.");
                }

                normalized.RemoveAt(normalized.Count - 1);
            }
            else
            {
                normalized.Add(parts[index]);
            }

            index = index + 1;
        }

        return string.Join('/', normalized);
    }

    internal string RequiredAttribute(XElement element, string name)
    {
        return OptionalAttribute(element, name) ?? throw new InvalidDataException("Element '" + element.Name.LocalName + "' requires attribute '" + name + "'.");
    }

    internal string? OptionalAttribute(XElement element, string name)
    {
        XAttribute? attribute = element.Attribute(name);
        return attribute == null ? null : attribute.Value;
    }

    internal string OptionalElementValue(XElement element, XName name)
    {
        XElement? child = element.Element(name);
        return child == null ? "" : child.Value.Trim();
    }
}
