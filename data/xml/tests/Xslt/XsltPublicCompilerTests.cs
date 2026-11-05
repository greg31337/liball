using xml;

namespace xml.Tests;

[TestFixture]
public class XsltPublicCompilerTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";

    [Test]
    public void ReusesOneCompilerConcurrentlyAndPublishesThreadSafeExecutables()
    {
        string source = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0'><xsl:template name='entry' match='item'/></xsl:stylesheet>";
        XsltCompileOptions options = Options();
        XsltCompiler compiler = new XsltCompiler();
        Task<XsltExecutable>[] compilations = Enumerable.Range(0, 24)
            .Select(index => Task.Factory.StartNew(() => compiler.CompileXslt(new XmlInput { Text = source, BaseUri = "https://example.test/" + index + ".xsl" }, options, new XmlDefaultResolver()), TaskCreationOptions.LongRunning))
            .ToArray();

        XsltExecutable[] executables = Task.WhenAll(compilations).GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(executables.All(item => item.IsImmutable), Is.True);
            Assert.That(executables.All(item => item.IsThreadSafe), Is.True);
            Assert.That(executables.Select(item => item.Plan).Distinct().Count(), Is.EqualTo(executables.Length));
            Assert.That(executables.Select(item => ((XsltStylesheetPlan)item.Plan!).PrincipalModule.BaseUri),
                Is.EquivalentTo(Enumerable.Range(0, 24).Select(index => "https://example.test/" + index + ".xsl")));
        });
    }

    [Test]
    public void SupportsConcurrentReadOnlySelectionFromOneExecutable()
    {
        string source =
            "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0'>" +
            "<xsl:template name='wildcard' match='*'/>" +
            "<xsl:template name='item' match='item' priority='2'/>" +
            "</xsl:stylesheet>";
        XsltExecutable executable = new XsltCompiler().CompileXslt(new XmlInput { Text = source }, Options(), new XmlDefaultResolver());
        XsltStylesheetPlan plan = (XsltStylesheetPlan)executable.Plan!;
        Task<XsltTemplateSelectionResult>[] selections = Enumerable.Range(0, 64).Select(index => Task.Factory.StartNew(() =>
        {
            XmlNode node = Element(index % 2 == 0 ? "item" : "other");
            XdmSequence candidate = new XdmSequenceOperations().CreateXdmNodeSequence(node);
            return new XsltTemplateSelector().SelectXsltTemplateRule(plan, "#unnamed", candidate, new XPathDynamicContext());
        }, TaskCreationOptions.LongRunning)).ToArray();

        XsltTemplateSelectionResult[] results = Task.WhenAll(selections).GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(executable.IsThreadSafe, Is.True);
            Assert.That(results.Where((item, index) => index % 2 == 0).All(item => item.Template!.Name == "Q{}item"), Is.True);
            Assert.That(results.Where((item, index) => index % 2 != 0).All(item => item.Template!.Name == "Q{}wildcard"), Is.True);
            Assert.That(results.All(item => item.Diagnostics.Length == 0), Is.True);
        });
    }

    [Test]
    public void DeeplySnapshotsMutableCompilationOptions()
    {
        XmlName packageName = new XmlName { LocalName = "urn:requested" };
        XsltPackageReference package = new XsltPackageReference
        {
            Name = new XmlName { LocalName = "urn:dependency" },
            Version = "1.0",
            LocationUri = "dependency.xsl"
        };
        XdmSequence value = new XdmSequenceOperations().CreateXdmAtomicSequence(new XdmFactory().CreateXdmStringAtomicValue("original", XdmAtomicType.String));
        XsltParameter parameter = new XsltParameter { Name = new XmlName { LocalName = "setting" }, Value = value, IsStatic = true };
        XsltExtensionBinding extension = new XsltExtensionBinding { Name = new XmlName { LocalName = "extension", NamespaceUri = "urn:extensions" }, BindingKey = "binding", IsFunction = true };
        XmlCatalogEntry child = new XmlCatalogEntry { Kind = XmlCatalogEntryKind.Uri, Uri = "urn:source", TargetUri = "https://example.test/original.xml" };
        XmlCatalogEntry catalog = new XmlCatalogEntry { Kind = XmlCatalogEntryKind.Group, Entries = [child] };
        XmlSchemaSet schemas = new XmlSchemaSet { SchemaDocuments = [], TargetNamespaces = ["urn:schema"], CompiledSet = new object(), Diagnostics = [], IsImmutable = true };
        XsltCompileOptions options = Options();
        options.PackageName = packageName;
        options.Packages = [package];
        options.StaticParameters = [parameter];
        options.Extensions = [extension];
        options.SchemaSet = schemas;
        options.ResolverPolicy!.CatalogEntries = [catalog];

        XsltExecutable executable = new XsltCompiler().CompileXslt(
            new XmlInput { Text = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0'/>" }, options, new XmlDefaultResolver());
        XsltCompileOptions snapshot = executable.CompileOptions!;
        packageName.LocalName = "urn:changed";
        package.Name!.LocalName = "urn:changed-dependency";
        parameter.Name!.LocalName = "changed";
        value.AtomicValues = [];
        extension.Name!.LocalName = "changed";
        child.TargetUri = "https://changed.test/";
        schemas.TargetNamespaces![0] = "urn:changed-schema";

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.PackageName, Is.Not.SameAs(packageName));
            Assert.That(snapshot.PackageName!.LocalName, Is.EqualTo("urn:requested"));
            Assert.That(snapshot.Packages![0], Is.Not.SameAs(package));
            Assert.That(snapshot.Packages[0].Name, Is.Not.SameAs(package.Name));
            Assert.That(snapshot.Packages[0].Name!.LocalName, Is.EqualTo("urn:dependency"));
            Assert.That(snapshot.StaticParameters![0], Is.Not.SameAs(parameter));
            Assert.That(snapshot.StaticParameters[0].Name!.LocalName, Is.EqualTo("setting"));
            Assert.That(snapshot.StaticParameters[0].Value, Is.Not.SameAs(value));
            Assert.That(snapshot.StaticParameters[0].Value!.Count, Is.EqualTo(1));
            Assert.That(snapshot.StaticParameters[0].Value!.IsImmutable, Is.True);
            Assert.That(snapshot.Extensions![0], Is.Not.SameAs(extension));
            Assert.That(snapshot.Extensions[0].Name!.LocalName, Is.EqualTo("extension"));
            Assert.That(snapshot.ResolverPolicy!.CatalogEntries![0], Is.Not.SameAs(catalog));
            Assert.That(snapshot.ResolverPolicy.CatalogEntries[0].Entries![0], Is.Not.SameAs(child));
            Assert.That(snapshot.ResolverPolicy.CatalogEntries[0].Entries![0].TargetUri, Is.EqualTo("https://example.test/original.xml"));
            Assert.That(snapshot.SchemaSet, Is.Not.SameAs(schemas));
            Assert.That(snapshot.SchemaSet!.TargetNamespaces, Is.EqualTo(new[] { "urn:schema" }));
            Assert.That(snapshot.SchemaSet.CompiledSet, Is.SameAs(schemas.CompiledSet));
        });
    }

    [Test]
    public void ValidatesCompilerOptionsAndPackageRequests()
    {
        string source = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0'/>";
        XsltCompileOptions conformance = Options();
        conformance.ConformanceLevel = (XsltConformanceLevel)99;
        XsltCompileOptions recovery = Options();
        recovery.RecoveryPolicy = (XsltRecoveryPolicy)99;
        XsltCompileOptions schemas = Options();
        schemas.ConformanceLevel = XsltConformanceLevel.SchemaAware;
        schemas.SchemaSet = new XmlSchemaSet();
        XsltPackageResolver resolver = new XsltPackageResolver();

        Assert.Multiple(() =>
        {
            Assert.That(() => new XsltCompiler().CompileXslt(new XmlInput { Text = source }, conformance, new XmlDefaultResolver()), Throws.ArgumentException.With.Property("ParamName").EqualTo("options"));
            Assert.That(() => new XsltCompiler().CompileXslt(new XmlInput { Text = source }, recovery, new XmlDefaultResolver()), Throws.ArgumentException.With.Property("ParamName").EqualTo("options"));
            Assert.That(() => new XsltCompiler().CompileXslt(new XmlInput { Text = source }, schemas, new XmlDefaultResolver()), Throws.ArgumentException.With.Property("ParamName").EqualTo("options"));
            Assert.That(() => resolver.ResolveXsltPackage(new XsltPackageReference(), Options()), Throws.ArgumentException.With.Property("ParamName").EqualTo("package"));
            Assert.That(() => resolver.ResolveXsltPackage(new XsltPackageReference { Name = new XmlName { LocalName = "relative" }, LocationUri = "package.xsl" }, Options()), Throws.ArgumentException.With.Property("ParamName").EqualTo("package"));
            Assert.That(() => resolver.ResolveXsltPackage(new XsltPackageReference { Name = new XmlName { LocalName = "urn:package" } }, Options()), Throws.ArgumentException.With.Property("ParamName").EqualTo("package"));
            Assert.That(() => resolver.ResolveXsltPackage(new XsltPackageReference { Name = new XmlName { LocalName = "urn:package" }, Version = "1..2", LocationUri = "package.xsl" }, Options()), Throws.ArgumentException.With.Property("ParamName").EqualTo("package"));
            Assert.That(() => resolver.ResolveXsltModule("   ", null, new XmlResolverPolicy()), Throws.ArgumentException.With.Property("ParamName").EqualTo("uri"));
        });
    }

    [Test]
    public void ReusesOnePackageResolverConcurrently()
    {
        string directory = Path.Combine(Path.GetTempPath(), "xml-xslt-public-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string packagePath = Path.Combine(directory, "package.xsl");
        string modulePath = Path.Combine(directory, "module.xsl");
        string stylesheetPath = Path.Combine(directory, "not-package.xsl");
        File.WriteAllText(packagePath,
            "<xsl:package xmlns:xsl='" + Xsl + "' name='urn:public' package-version='3.1' version='3.0'><xsl:template name='entry' visibility='public'/></xsl:package>");
        File.WriteAllText(modulePath, "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0'/>");
        File.WriteAllText(stylesheetPath, "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0'/>");
        try
        {
            XsltCompileOptions options = Options();
            options.ResolverPolicy!.AllowedSchemes = ["file"];
            options.ResolverPolicy.AllowedRoots = [directory];
            XsltPackageReference reference = new XsltPackageReference
            {
                Name = new XmlName { LocalName = "urn:public" },
                Version = "3.*",
                LocationUri = new Uri(packagePath).AbsoluteUri
            };
            XsltPackageResolver resolver = new XsltPackageResolver();
            Task<XsltExecutable>[] resolutions = Enumerable.Range(0, 12)
                .Select(index => Task.Factory.StartNew(() => resolver.ResolveXsltPackage(reference, options), TaskCreationOptions.LongRunning))
                .ToArray();

            XsltExecutable[] executables = Task.WhenAll(resolutions).GetAwaiter().GetResult();
            options.ResolverPolicy.CatalogEntries = [new XmlCatalogEntry
            {
                Kind = XmlCatalogEntryKind.Uri,
                Uri = "urn:module",
                TargetUri = new Uri(modulePath).AbsoluteUri
            }];
            XmlResolvedResource module = resolver.ResolveXsltModule(" urn:module ", null, options.ResolverPolicy);
            XsltPackageReference invalid = new XsltPackageReference
            {
                Name = new XmlName { LocalName = "urn:public" },
                Version = "3.1",
                LocationUri = new Uri(stylesheetPath).AbsoluteUri
            };
            XsltStaticException error = Assert.Throws<XsltStaticException>(() => resolver.ResolveXsltPackage(invalid, options))!;

            Assert.Multiple(() =>
            {
                Assert.That(executables.All(item => item.IsImmutable && item.IsThreadSafe), Is.True);
                Assert.That(executables.All(item => item.PackageName!.LocalName == "urn:public" && item.PackageVersion == "3.1"), Is.True);
                Assert.That(executables.All(item => ((XsltStylesheetPlan)item.Plan!).Package!.IsLibrary), Is.True);
                Assert.That(module.AbsoluteUri, Is.EqualTo(new Uri(modulePath).AbsoluteUri));
                Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE3000));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static XsltCompileOptions Options()
    {
        return new XsltCompileOptions
        {
            BaseUri = "https://example.test/main.xsl",
            ResolverPolicy = new XmlResolverPolicy
            {
                Access = XmlResourceAccess.Allowed,
                AllowedSchemes = ["https"],
                MaximumDepth = 16,
                MaximumResourceBytes = 1048576,
                MaximumWorkUnits = 1048576
            }
        };
    }

    private static XmlNode Element(string name)
    {
        XmlDocument document = new XmlParser().ParseXmlDocument(new XmlInput { Text = "<" + name + "/>" }, new XmlParserOptions(), null, null, null);
        return document.DocumentElement!;
    }
}
