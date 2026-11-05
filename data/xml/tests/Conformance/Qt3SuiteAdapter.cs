using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using xml;

namespace xml.Tests;

internal enum Qt3LanguageApplicability
{
    XPath31,
    XQueryOnly,
    OtherXPathVersion
}

internal sealed class Qt3Dependency
{
    internal string Type = "";
    internal string Value = "";
    internal bool Satisfied = true;
}

internal sealed class Qt3EnvironmentNode
{
    internal string Kind = "";
    internal string Value = "";
    internal string? FilePath;
    internal string? NameNamespaceUri;
    internal Dictionary<string, string> Attributes = new Dictionary<string, string>(StringComparer.Ordinal);
    internal Qt3EnvironmentNode[] Children = [];
}

internal sealed class Qt3Environment
{
    internal string Name = "";
    internal string Reference = "";
    internal string CatalogPath = "";
    internal Qt3EnvironmentNode[] Nodes = [];

    internal Qt3EnvironmentNode[] Find(string kind)
    {
        return Nodes.Where(node => node.Kind == kind).ToArray();
    }
}

internal sealed class Qt3Assertion
{
    internal string Kind = "";
    internal string Value = "";
    internal string? FilePath;
    internal Dictionary<string, string> Attributes = new Dictionary<string, string>(StringComparer.Ordinal);
    internal Qt3Assertion[] Children = [];
}

internal sealed class Qt3TestCase
{
    internal string Name = "";
    internal string TestSetName = "";
    internal string TestSetPath = "";
    internal string Description = "";
    internal string Expression = "";
    internal string? ExpressionPath;
    internal Qt3Environment Environment = new Qt3Environment();
    internal Qt3Dependency[] Dependencies = [];
    internal Qt3Assertion Assertion = new Qt3Assertion();
    internal Qt3LanguageApplicability Applicability;
    internal string ApplicabilityReason = "";
}

internal sealed class Qt3SuiteAdapter
{
    internal const string CatalogNamespace = "http://www.w3.org/2010/09/qt-fots-catalog";
    internal const string ArchiveBaseUri = "https://qt3.test/";
    internal const string XQueryOnlyReason = "The effective specification dependency permits XQuery but not XPath 3.1.";
    internal const string OtherXPathVersionReason = "The effective specification dependency does not permit XPath 3.1.";
    internal static readonly string[] AssertionKinds =
    [
        "all-of", "any-of", "not", "assert", "assert-count", "assert-deep-eq", "assert-empty", "assert-eq",
        "assert-false", "assert-permutation", "assert-serialization", "assert-serialization-error",
        "assert-string-value", "assert-true", "assert-type", "assert-xml", "error", "serialization-matches"
    ];

    internal Dictionary<string, byte[]> Resources;
    internal string RootPath;
    internal string CatalogPath;
    internal Dictionary<string, Qt3Environment> GlobalEnvironments;
    internal Qt3Dependency[] CatalogDependencies;
    internal Qt3TestCase[] TestCases;

    internal Qt3SuiteAdapter(string archivePath)
    {
        Resources = ReadArchive(archivePath);
        CatalogPath = FindCatalogPath();
        RootPath = CatalogPath.Substring(0, CatalogPath.Length - "catalog.xml".Length);
        GlobalEnvironments = new Dictionary<string, Qt3Environment>(StringComparer.Ordinal);
        CatalogDependencies = [];
        TestCases = [];
        LoadCatalog();
    }

    internal Dictionary<string, byte[]> ReadArchive(string archivePath)
    {
        Dictionary<string, byte[]> resources = new Dictionary<string, byte[]>(StringComparer.Ordinal);
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
                resources[NormalizePath(entry.FullName)] = content.ToArray();
            }

            index = index + 1;
        }

        return resources;
    }

    internal string FindCatalogPath()
    {
        string[] matches = Resources.Keys.Where(path => path.EndsWith("/catalog.xml", StringComparison.Ordinal) && path.Count(character => character == '/') == 1).ToArray();
        if (matches.Length != 1)
        {
            throw new InvalidDataException("The QT3 archive must contain exactly one root catalog.");
        }

        return matches[0];
    }

    internal void LoadCatalog()
    {
        XDocument catalog = ReadXml(CatalogPath);
        XNamespace ns = CatalogNamespace;
        XElement root = catalog.Root ?? throw new InvalidDataException("The QT3 catalog has no document element.");
        string catalogDirectory = DirectoryPath(CatalogPath);
        XElement[] environmentElements = root.Elements(ns + "environment").ToArray();
        int environmentIndex = 0;
        while (environmentIndex < environmentElements.Length)
        {
            Qt3Environment environment = ParseEnvironment(environmentElements[environmentIndex], CatalogPath);
            if (environment.Name.Length != 0)
            {
                GlobalEnvironments[environment.Name] = environment;
            }

            environmentIndex = environmentIndex + 1;
        }

        CatalogDependencies = root.Elements(ns + "dependency").Select(ParseDependency).ToArray();
        List<Qt3TestCase> tests = new List<Qt3TestCase>();
        XElement[] setReferences = root.Elements(ns + "test-set").ToArray();
        int setIndex = 0;
        while (setIndex < setReferences.Length)
        {
            string file = RequiredAttribute(setReferences[setIndex], "file");
            string setPath = ResolvePath(catalogDirectory, file);
            LoadTestSet(setPath, tests);
            setIndex = setIndex + 1;
        }

        TestCases = tests.ToArray();
    }

    internal void LoadTestSet(string setPath, List<Qt3TestCase> tests)
    {
        XDocument document = ReadXml(setPath);
        XNamespace ns = CatalogNamespace;
        XElement root = document.Root ?? throw new InvalidDataException("A QT3 test set has no document element.");
        string setName = RequiredAttribute(root, "name");
        Dictionary<string, Qt3Environment> localEnvironments = new Dictionary<string, Qt3Environment>(StringComparer.Ordinal);
        XElement[] environmentElements = root.Elements(ns + "environment").ToArray();
        int environmentIndex = 0;
        while (environmentIndex < environmentElements.Length)
        {
            Qt3Environment environment = ParseEnvironment(environmentElements[environmentIndex], setPath);
            if (environment.Name.Length != 0)
            {
                localEnvironments[environment.Name] = environment;
            }

            environmentIndex = environmentIndex + 1;
        }

        Qt3Dependency[] setDependencies = CatalogDependencies.Concat(root.Elements(ns + "dependency").Select(ParseDependency)).ToArray();
        XElement[] caseElements = root.Elements(ns + "test-case").ToArray();
        int caseIndex = 0;
        while (caseIndex < caseElements.Length)
        {
            tests.Add(ParseTestCase(caseElements[caseIndex], setName, setPath, localEnvironments, setDependencies));
            caseIndex = caseIndex + 1;
        }
    }

    internal Qt3TestCase ParseTestCase(XElement element, string setName, string setPath, Dictionary<string, Qt3Environment> localEnvironments, Qt3Dependency[] setDependencies)
    {
        XNamespace ns = CatalogNamespace;
        Qt3TestCase test = new Qt3TestCase();
        test.Name = RequiredAttribute(element, "name");
        test.TestSetName = setName;
        test.TestSetPath = setPath;
        test.Description = element.Element(ns + "description") == null ? "" : element.Element(ns + "description")!.Value.Trim();
        XElement testElement = element.Element(ns + "test") ?? throw new InvalidDataException("QT3 test '" + test.Name + "' has no test expression.");
        string? expressionFile = OptionalAttribute(testElement, "file");
        if (expressionFile == null)
        {
            test.Expression = testElement.Value;
        }
        else
        {
            test.ExpressionPath = ResolvePath(DirectoryPath(setPath), expressionFile);
            test.Expression = ReadText(test.ExpressionPath);
        }

        XElement? environmentElement = element.Element(ns + "environment");
        test.Environment = ResolveEnvironment(environmentElement, setPath, localEnvironments);
        test.Dependencies = setDependencies.Concat(element.Elements(ns + "dependency").Select(ParseDependency)).ToArray();
        XElement result = element.Element(ns + "result") ?? throw new InvalidDataException("QT3 test '" + test.Name + "' has no result assertion.");
        XElement assertionElement = result.Elements().Single();
        test.Assertion = ParseAssertion(assertionElement, setPath);
        test.Applicability = ClassifyApplicability(test.Dependencies);
        test.ApplicabilityReason = test.Applicability == Qt3LanguageApplicability.XQueryOnly ? XQueryOnlyReason : test.Applicability == Qt3LanguageApplicability.OtherXPathVersion ? OtherXPathVersionReason : "";
        return test;
    }

    internal Qt3Environment ResolveEnvironment(XElement? element, string setPath, Dictionary<string, Qt3Environment> localEnvironments)
    {
        if (element == null)
        {
            return new Qt3Environment { CatalogPath = setPath };
        }

        string? reference = OptionalAttribute(element, "ref");
        if (reference == null)
        {
            return ParseEnvironment(element, setPath);
        }

        if (localEnvironments.TryGetValue(reference, out Qt3Environment? local))
        {
            return local;
        }

        if (GlobalEnvironments.TryGetValue(reference, out Qt3Environment? global))
        {
            return global;
        }

        throw new InvalidDataException("QT3 environment '" + reference + "' is not defined.");
    }

    internal Qt3Environment ParseEnvironment(XElement element, string catalogPath)
    {
        Qt3Environment environment = new Qt3Environment();
        environment.Name = OptionalAttribute(element, "name") ?? "";
        environment.Reference = OptionalAttribute(element, "ref") ?? "";
        environment.CatalogPath = catalogPath;
        environment.Nodes = element.Elements().Select(child => ParseEnvironmentNode(child, catalogPath)).ToArray();
        return environment;
    }

    internal Qt3EnvironmentNode ParseEnvironmentNode(XElement element, string catalogPath)
    {
        Qt3EnvironmentNode node = new Qt3EnvironmentNode();
        node.Kind = element.Name.LocalName;
        node.Value = element.Value;
        node.Attributes = element.Attributes().Where(attribute => !attribute.IsNamespaceDeclaration).ToDictionary(attribute => attribute.Name.LocalName, attribute => attribute.Value, StringComparer.Ordinal);
        string? declaredName = OptionalAttribute(element, "name");
        int nameColon = declaredName == null ? -1 : declaredName.IndexOf(':');
        node.NameNamespaceUri = nameColon < 0 || declaredName == null ? null : element.GetNamespaceOfPrefix(declaredName.Substring(0, nameColon))?.NamespaceName;
        string? file = OptionalAttribute(element, "file");
        node.FilePath = file == null ? null : ResolvePath(DirectoryPath(catalogPath), file);
        node.Children = element.Elements().Select(child => ParseEnvironmentNode(child, catalogPath)).ToArray();
        return node;
    }

    internal Qt3Dependency ParseDependency(XElement element)
    {
        Qt3Dependency dependency = new Qt3Dependency();
        dependency.Type = RequiredAttribute(element, "type");
        dependency.Value = RequiredAttribute(element, "value");
        dependency.Satisfied = OptionalAttribute(element, "satisfied") != "false";
        return dependency;
    }

    internal Qt3Assertion ParseAssertion(XElement element, string setPath)
    {
        Qt3Assertion assertion = new Qt3Assertion();
        assertion.Kind = element.Name.LocalName;
        if (!SupportsAssertion(assertion.Kind))
        {
            throw new InvalidDataException("QT3 assertion '" + assertion.Kind + "' is not supported by the adapter.");
        }

        assertion.Value = element.Value;
        assertion.Attributes = element.Attributes().Where(attribute => !attribute.IsNamespaceDeclaration).ToDictionary(attribute => attribute.Name.LocalName, attribute => attribute.Value, StringComparer.Ordinal);
        string? file = OptionalAttribute(element, "file");
        assertion.FilePath = file == null ? null : ResolvePath(DirectoryPath(setPath), file);
        assertion.Children = element.Elements().Select(child => ParseAssertion(child, setPath)).ToArray();
        return assertion;
    }

    internal bool SupportsAssertion(string kind)
    {
        return AssertionKinds.Contains(kind);
    }

    internal Qt3LanguageApplicability ClassifyApplicability(Qt3Dependency[] dependencies)
    {
        Qt3Dependency[] specifications = dependencies.Where(dependency => dependency.Type == "spec").ToArray();
        if (specifications.All(DependencyAllowsXPath31))
        {
            return Qt3LanguageApplicability.XPath31;
        }

        bool explicitlyXQuery = specifications.Any(dependency => dependency.Satisfied && HasXQueryToken(dependency.Value) && !HasCompatibleXPath31Token(dependency.Value));
        return explicitlyXQuery ? Qt3LanguageApplicability.XQueryOnly : Qt3LanguageApplicability.OtherXPathVersion;
    }

    internal bool DependencyAllowsXPath31(Qt3Dependency dependency)
    {
        bool listed = HasCompatibleXPath31Token(dependency.Value);
        return dependency.Satisfied ? listed : !listed;
    }

    internal bool HasCompatibleXPath31Token(string value)
    {
        string[] tokens = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return tokens.Any(token => token == "XP31" || token == "XP31+" || token == "XP30+" || token == "XP20+");
    }

    internal bool HasXQueryToken(string value)
    {
        return value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(token => token.StartsWith("XQ", StringComparison.Ordinal));
    }

    internal XDocument ReadXml(string path)
    {
        return XDocument.Parse(ReadText(path), LoadOptions.PreserveWhitespace);
    }

    internal string ReadText(string path)
    {
        if (!Resources.TryGetValue(NormalizePath(path), out byte[]? bytes))
        {
            throw new FileNotFoundException("The QT3 archive resource was not found.", path);
        }

        string text = Encoding.UTF8.GetString(bytes);
        return text.Length != 0 && text[0] == '\uFEFF' ? text.Substring(1) : text;
    }

    internal string ResourceUri(string path)
    {
        return new Uri(new Uri(ArchiveBaseUri), NormalizePath(path)).AbsoluteUri;
    }

    internal string ResolvePath(string directory, string relative)
    {
        Uri baseUri = new Uri(new Uri(ArchiveBaseUri), directory);
        string absolutePath = new Uri(baseUri, relative).AbsolutePath.TrimStart('/');
        return NormalizePath(Uri.UnescapeDataString(absolutePath));
    }

    internal string DirectoryPath(string path)
    {
        int slash = NormalizePath(path).LastIndexOf('/');
        return slash < 0 ? "" : NormalizePath(path).Substring(0, slash + 1);
    }

    internal string NormalizePath(string path)
    {
        return path.Replace('\\', '/').TrimStart('/');
    }

    internal string RequiredAttribute(XElement element, string name)
    {
        return OptionalAttribute(element, name) ?? throw new InvalidDataException("QT3 element '" + element.Name.LocalName + "' requires attribute '" + name + "'.");
    }

    internal string? OptionalAttribute(XElement element, string name)
    {
        XAttribute? attribute = element.Attributes().FirstOrDefault(candidate => candidate.Name.LocalName == name);
        return attribute == null ? null : attribute.Value;
    }
}
