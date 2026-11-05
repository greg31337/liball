using xml;

namespace xml.Tests;

[TestFixture]
public class XsltPackageCompilerTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";

    [Test]
    public void ParsesOrdersAndMatchesPackageVersionsAndRanges()
    {
        XsltPackageVersionRules rules = new XsltPackageVersionRules();
        XsltPackageVersion releaseCandidate = rules.ParseXsltPackageVersion("1.0.3-rc1")!;
        XsltPackageVersion release = rules.ParseXsltPackageVersion("1.0.3")!;
        XsltPackageVersion trailingZero = rules.ParseXsltPackageVersion("1.0")!;
        XsltPackageVersion one = rules.ParseXsltPackageVersion("1")!;

        Assert.Multiple(() =>
        {
            Assert.That(rules.CompareXsltPackageVersions(releaseCandidate, release), Is.LessThan(0));
            Assert.That(rules.CompareXsltPackageVersions(trailingZero, one), Is.EqualTo(0));
            Assert.That(rules.MatchesXsltPackageVersionRange("1.3-beta", "1.3.*"), Is.True);
            Assert.That(rules.MatchesXsltPackageVersionRange("1.9", "1.3+"), Is.True);
            Assert.That(rules.MatchesXsltPackageVersionRange("4.0-beta", "to 4.0"), Is.True);
            Assert.That(rules.MatchesXsltPackageVersionRange("5.7.2", "1 to 5.*"), Is.True);
            Assert.That(rules.MatchesXsltPackageVersionRange("6.0-beta", "1 to 5.*"), Is.False);
            Assert.That(rules.IsValidXsltPackageVersionRange("9.5.0.8, 9.6+"), Is.True);
            Assert.That(rules.IsValidXsltPackageVersionRange("1..2"), Is.False);
        });
    }

    [Test]
    public void CompilesPackageMetadataComponentsAndExposure()
    {
        string source = "<xsl:package xmlns:xsl='" + Xsl + "' xmlns:f='urn:functions' name='urn:main' package-version='2.4-alpha' version='3.0'>" +
            "<xsl:expose component='function' names='f:calculate#1' visibility='final'/>" +
            "<xsl:expose component='template' names='*' visibility='public'/>" +
            "<xsl:function name='f:calculate' visibility='public'><xsl:param name='value'/></xsl:function>" +
            "<xsl:template name='entry'/><xsl:variable name='state'/>" +
            "</xsl:package>";

        XsltExecutable executable = Compile(source, new PackageResolver(), null, false);
        XsltPackagePlan package = PackagePlan(executable);

        Assert.Multiple(() =>
        {
            Assert.That(executable.PackageName!.LocalName, Is.EqualTo("urn:main"));
            Assert.That(executable.PackageVersion, Is.EqualTo("2.4-alpha"));
            Assert.That(package.Name, Is.EqualTo("urn:main"));
            Assert.That(package.Version, Is.EqualTo("2.4-alpha"));
            Assert.That(package.IsExplicit, Is.True);
            Assert.That(Component(package, XsltComponentKind.Function, "Q{urn:functions}calculate", 1).Visibility, Is.EqualTo(XsltComponentVisibility.Final));
            Assert.That(Component(package, XsltComponentKind.Template, "Q{}entry", -1).Visibility, Is.EqualTo(XsltComponentVisibility.Public));
            Assert.That(Component(package, XsltComponentKind.Variable, "Q{}state", -1).Visibility, Is.EqualTo(XsltComponentVisibility.Private));
            Assert.That(Component(package, XsltComponentKind.Mode, "#unnamed", -1).Visibility, Is.EqualTo(XsltComponentVisibility.Private));
            Assert.That(package.IsExecutable, Is.True);
        });
    }

    [Test]
    public void DeclaredVisibilityTakesPriorityOverWildcardExposure()
    {
        string source = Library("urn:main", "1", "<xsl:expose component='template' names='*' visibility='private'/><xsl:template name='entry' visibility='public'/>", "");

        XsltPackagePlan package = PackagePlan(CompileLibrary(source, new PackageResolver(), null));

        Assert.That(Component(package, XsltComponentKind.Template, "Q{}entry", -1).Visibility, Is.EqualTo(XsltComponentVisibility.Public));
    }

    [Test]
    public void KeepsStaticParametersPrivateAndHidesThemFromUsingPackages()
    {
        string parameter = "<xsl:param name='setting' static='yes' select='1'/>";
        XsltPackagePlan library = PackagePlan(CompileLibrary(Library("urn:library", "1", parameter, ""), new PackageResolver(), null));
        PackageResolver resolver = new PackageResolver();
        resolver.Resources.Add("https://example.test/library.xsl", Library("urn:library", "1", parameter, ""));
        string source = Library("urn:main", "1", "<xsl:use-package name='urn:library'/>", "");

        XsltPackagePlan package = PackagePlan(Compile(source, resolver, new[] { Reference("urn:library", "1", "library.xsl") }, false));
        XsltComponent imported = package.Components.Single(component => component.Name == "Q{}setting");

        Assert.Multiple(() =>
        {
            Assert.That(library.Components.Single(component => component.Name == "Q{}setting").Visibility, Is.EqualTo(XsltComponentVisibility.Private));
            Assert.That(imported.Visibility, Is.EqualTo(XsltComponentVisibility.Hidden));
        });
    }

    [TestCase("<xsl:mode visibility='public'/>")]
    [TestCase("<xsl:mode name='named' visibility='abstract'/>")]
    [TestCase("<xsl:param name='value' visibility='private'/>")]
    public void RejectsInvalidComponentVisibilityDeclarations(string component)
    {
        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(Library("urn:main", "1", component, ""), new PackageResolver(), null, false))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE0020));
    }

    [Test]
    public void SelectsTheHighestMatchingPackageAndRetainsAReusableExecutable()
    {
        PackageResolver resolver = new PackageResolver();
        resolver.Resources.Add("https://example.test/library-1.xsl", Library("urn:library", "1.5", "<xsl:template name='public' visibility='public'/><xsl:template name='old' visibility='public'/>", ""));
        resolver.Resources.Add("https://example.test/library-2.xsl", Library("urn:library", "2.7", "<xsl:template name='public' visibility='public'/>", ""));
        XsltPackageReference first = Reference("urn:library", "1.5", "library-1.xsl");
        XsltPackageReference second = Reference("urn:library", "2.7", "library-2.xsl");
        string source = Stylesheet("<xsl:use-package name='urn:library' package-version='1 to 2.*'><xsl:accept component='template' names='public' visibility='public'/></xsl:use-package>");

        XsltPackagePlan package = PackagePlan(Compile(source, resolver, new[] { first, second }, true));
        XsltPackageUse use = package.Uses.Single();

        Assert.Multiple(() =>
        {
            Assert.That(use.Target.Version, Is.EqualTo("2.7"));
            Assert.That(use.SeparateExecutable, Is.Not.Null);
            Assert.That(use.Target.IsReusable, Is.True);
            Assert.That(Component(package, XsltComponentKind.Template, "Q{}public", -1).Visibility, Is.EqualTo(XsltComponentVisibility.Public));
            Assert.That(resolver.RequestedUris, Is.EqualTo(new[] { "library-2.xsl" }));
            Assert.That(resolver.Kinds, Is.EqualTo(new[] { XmlResourceKind.Package }));
        });
    }

    [Test]
    public void ReusesSeparatelyCompiledPackagesAcrossHiddenImports()
    {
        PackageResolver resolver = new PackageResolver();
        resolver.Resources.Add("https://example.test/library.xsl", Library("urn:library", "1", "<xsl:template name='shared' visibility='public'/>", ""));
        XsltPackageReference reference = Reference("urn:library", "1", "library.xsl");
        string use = "<xsl:use-package name='urn:library'><xsl:accept component='template' names='*' visibility='hidden'/></xsl:use-package>";
        string source = Library("urn:main", "1", use + use, "");

        XsltPackagePlan package = PackagePlan(Compile(source, resolver, new[] { reference }, true));

        Assert.Multiple(() =>
        {
            Assert.That(package.Uses, Has.Length.EqualTo(2));
            Assert.That(package.Uses[0].SeparateExecutable, Is.SameAs(package.Uses[1].SeparateExecutable));
            Assert.That(resolver.RequestedUris, Is.EqualTo(new[] { "library.xsl" }));
        });
    }

    [Test]
    public void OverridesAbstractComponentsAndRebindsUsedComponentReferences()
    {
        PackageResolver resolver = new PackageResolver();
        string libraryContent = "<xsl:template name='hook' visibility='abstract'/><xsl:template name='entry' visibility='public'><xsl:call-template name='hook'/></xsl:template>";
        resolver.Resources.Add("https://example.test/library.xsl", Library("urn:library", "1", libraryContent, ""));
        XsltPackageReference reference = Reference("urn:library", "1", "library.xsl");
        string source = "<xsl:package xmlns:xsl='" + Xsl + "' name='urn:main' version='3.0'>" +
            "<xsl:use-package name='urn:library'><xsl:override><xsl:template name='hook'/></xsl:override></xsl:use-package>" +
            "</xsl:package>";

        XsltPackagePlan package = PackagePlan(Compile(source, resolver, new[] { reference }, false));
        XsltComponent entry = Component(package, XsltComponentKind.Template, "Q{}entry", -1);
        XsltComponent overriding = package.Components.Single(component => component.IsOverride);
        XsltComponent original = package.Components.Single(component => component.SymbolicIdentifier == overriding.SymbolicIdentifier && component.Visibility == XsltComponentVisibility.Hidden);

        Assert.Multiple(() =>
        {
            Assert.That(overriding.Visibility, Is.EqualTo(XsltComponentVisibility.Private));
            Assert.That(overriding.OriginalComponent, Is.SameAs(original));
            Assert.That(entry.References.Single().Target, Is.SameAs(overriding));
            Assert.That(package.IsExecutable, Is.True);
        });
    }

    [Test]
    public void BindsLocalTemplateModeAndAttributeSetReferences()
    {
        string source = "<xsl:package xmlns:xsl='" + Xsl + "' name='urn:main' version='3.0'>" +
            "<xsl:attribute-set name='style'/><xsl:mode name='render'/>" +
            "<xsl:template name='called'/><xsl:template name='entry'>" +
            "<xsl:call-template name='called'/><xsl:apply-templates mode='render'/><xsl:copy use-attribute-sets='style'/>" +
            "</xsl:template></xsl:package>";

        XsltPackagePlan package = PackagePlan(Compile(source, new PackageResolver(), null, false));
        XsltComponent entry = Component(package, XsltComponentKind.Template, "Q{}entry", -1);

        Assert.That(entry.References.Select(reference => reference.Target!.Name), Is.EquivalentTo(new[] { "Q{}called", "Q{}render", "Q{}style" }));
    }

    [Test]
    public void PreservesBindingsToHiddenComponentsFromAUsedPackage()
    {
        PackageResolver resolver = new PackageResolver();
        string content = "<xsl:template name='helper'/><xsl:template name='entry' visibility='public'><xsl:call-template name='helper'/></xsl:template>";
        resolver.Resources.Add("https://example.test/library.xsl", Library("urn:library", "1", content, ""));
        string source = Library("urn:main", "1", "<xsl:use-package name='urn:library'/>", "");

        XsltPackagePlan package = PackagePlan(Compile(source, resolver, new[] { Reference("urn:library", "1", "library.xsl") }, false));
        XsltComponent entry = Component(package, XsltComponentKind.Template, "Q{}entry", -1);

        Assert.Multiple(() =>
        {
            Assert.That(entry.References.Single().Target, Is.Not.Null);
            Assert.That(entry.References.Single().Target!.Name, Is.EqualTo("Q{}helper"));
            Assert.That(entry.References.Single().Target!.Visibility, Is.EqualTo(XsltComponentVisibility.Hidden));
        });
    }

    [Test]
    public void PreservesOriginalBindingsAcrossTransitivePackageReuse()
    {
        PackageResolver resolver = new PackageResolver();
        resolver.Resources.Add("https://example.test/base.xsl", Library("urn:base", "1", "<xsl:template name='hook' visibility='public'/>", ""));
        string middleContent = "<xsl:use-package name='urn:base'><xsl:override><xsl:template name='hook' visibility='public'><xsl:call-template name='xsl:original'/></xsl:template></xsl:override></xsl:use-package>";
        resolver.Resources.Add("https://example.test/middle.xsl", Library("urn:middle", "1", middleContent, ""));
        XsltPackageReference[] references =
        [
            Reference("urn:base", "1", "base.xsl"),
            Reference("urn:middle", "1", "middle.xsl")
        ];
        string source = Library("urn:main", "1", "<xsl:use-package name='urn:middle'/>", "");

        XsltPackagePlan package = PackagePlan(Compile(source, resolver, references, true));
        XsltComponent hook = Component(package, XsltComponentKind.Template, "Q{}hook", -1);

        Assert.Multiple(() =>
        {
            Assert.That(hook.References.Single().Target, Is.Not.Null);
            Assert.That(hook.References.Single().Target!.Visibility, Is.EqualTo(XsltComponentVisibility.Hidden));
            Assert.That(hook.References.Single().Name, Is.EqualTo("Q{" + Xsl + "}original"));
        });
    }

    [TestCase("<xsl:expose component='template' names='missing' visibility='public'/><xsl:template name='present'/>", XsltErrorCodes.XTSE3020)]
    [TestCase("<xsl:expose component='*' names='present' visibility='public'/><xsl:template name='present'/>", XsltErrorCodes.XTSE3022)]
    [TestCase("<xsl:expose component='template' names='present' visibility='public'/><xsl:template name='present' visibility='private'/>", XsltErrorCodes.XTSE3010)]
    [TestCase("<xsl:expose component='template' names='present' visibility='abstract'/><xsl:template name='present'/>", XsltErrorCodes.XTSE3025)]
    public void EnforcesExposeConstraints(string content, string expectedCode)
    {
        string source = Library("urn:main", "1", content, "");

        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(source, new PackageResolver(), null, true))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(expectedCode));
    }

    [TestCase("<xsl:accept component='template' names='missing' visibility='public'/>", "<xsl:template name='present' visibility='public'/>", XsltErrorCodes.XTSE3030)]
    [TestCase("<xsl:accept component='*' names='present' visibility='public'/>", "<xsl:template name='present' visibility='public'/>", XsltErrorCodes.XTSE3032)]
    [TestCase("<xsl:accept component='template' names='present' visibility='public'/>", "<xsl:template name='present' visibility='private'/>", XsltErrorCodes.XTSE3040)]
    public void EnforcesAcceptConstraints(string accept, string libraryComponent, string expectedCode)
    {
        PackageResolver resolver = new PackageResolver();
        resolver.Resources.Add("https://example.test/library.xsl", Library("urn:library", "1", libraryComponent, ""));
        XsltPackageReference reference = Reference("urn:library", "1", "library.xsl");
        string source = Library("urn:main", "1", "<xsl:use-package name='urn:library'>" + accept + "</xsl:use-package>", "");

        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(source, resolver, new[] { reference }, true))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(expectedCode));
    }

    [Test]
    public void RejectsAbstractComponentsInTheTopLevelPackageButAllowsThemInLibraries()
    {
        string source = Library("urn:main", "1", "<xsl:template name='required' visibility='abstract'/>", "");

        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(source, new PackageResolver(), null, false))!;
        XsltPackagePlan library = PackagePlan(CompileLibrary(source, new PackageResolver(), null));

        Assert.Multiple(() =>
        {
            Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE3080));
            Assert.That(library.IsExecutable, Is.False);
            Assert.That(library.IsLibrary, Is.True);
        });
    }

    [TestCase("<xsl:template name='fixed' visibility='final'/>", "<xsl:template name='fixed'/>", XsltErrorCodes.XTSE3060)]
    [TestCase("<xsl:template name='open' visibility='public' as='item()'/>", "<xsl:template name='open' as='node()'/>", XsltErrorCodes.XTSE3070)]
    [TestCase("<xsl:template name='open' visibility='public'/>", "<xsl:template name='other'/>", XsltErrorCodes.XTSE3058)]
    public void EnforcesOverrideTargetAndSignatureConstraints(string libraryComponent, string overridingComponent, string expectedCode)
    {
        PackageResolver resolver = new PackageResolver();
        resolver.Resources.Add("https://example.test/library.xsl", Library("urn:library", "1", libraryComponent, ""));
        XsltPackageReference reference = Reference("urn:library", "1", "library.xsl");
        string source = Library("urn:main", "1", "<xsl:use-package name='urn:library'><xsl:override>" + overridingComponent + "</xsl:override></xsl:use-package>", "");

        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(source, resolver, new[] { reference }, true))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(expectedCode));
    }

    [Test]
    public void AcceptsCompatibleOverrideSignatures()
    {
        PackageResolver resolver = new PackageResolver();
        string libraryContent =
            "<xsl:template name='open' visibility='public'><xsl:param name='value' required='yes'/></xsl:template>" +
            "<xsl:function name='f:run' visibility='public'><xsl:param name='old'/></xsl:function>" +
            "<xsl:attribute-set name='style' visibility='public'/>";
        resolver.Resources.Add("https://example.test/library.xsl", Library("urn:library", "1", libraryContent, "xmlns:f='urn:functions'"));
        string overrideContent =
            "<xsl:template name='open'><xsl:param name='value' required='yes'/><xsl:param name='optional'/></xsl:template>" +
            "<xsl:function name='f:run' streamable='yes'><xsl:param name='renamed'/></xsl:function>" +
            "<xsl:attribute-set name='style' streamable='yes'/>";
        string source = Library("urn:main", "1", "<xsl:use-package name='urn:library'><xsl:override>" + overrideContent + "</xsl:override></xsl:use-package>", "xmlns:f='urn:functions'");

        XsltPackagePlan package = PackagePlan(Compile(source, resolver, new[] { Reference("urn:library", "1", "library.xsl") }, false));

        Assert.That(package.Components.Count(component => component.IsOverride), Is.EqualTo(3));
    }

    [Test]
    public void RejectsComponentsThatAreBothAcceptedAndOverridden()
    {
        PackageResolver resolver = new PackageResolver();
        resolver.Resources.Add("https://example.test/library.xsl", Library("urn:library", "1", "<xsl:template name='open' visibility='public'/>", ""));
        string use = "<xsl:use-package name='urn:library'><xsl:accept component='template' names='open' visibility='private'/><xsl:override><xsl:template name='open'/></xsl:override></xsl:use-package>";

        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(Library("urn:main", "1", use, ""), resolver, new[] { Reference("urn:library", "1", "library.xsl") }, false))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE3051));
    }

    [Test]
    public void RejectsVisibleComponentConflictsFromUsedPackages()
    {
        PackageResolver resolver = new PackageResolver();
        resolver.Resources.Add("https://example.test/first.xsl", Library("urn:first", "1", "<xsl:template name='shared' visibility='public'/>", ""));
        resolver.Resources.Add("https://example.test/second.xsl", Library("urn:second", "1", "<xsl:template name='shared' visibility='public'/>", ""));
        XsltPackageReference[] references =
        [
            Reference("urn:first", "1", "first.xsl"),
            Reference("urn:second", "1", "second.xsl")
        ];
        string source = Library("urn:main", "1", "<xsl:use-package name='urn:first'/><xsl:use-package name='urn:second'/>", "");

        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(source, resolver, references, false))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE3050));
    }

    [Test]
    public void RejectsOriginalReferencesToAbstractComponents()
    {
        PackageResolver resolver = new PackageResolver();
        resolver.Resources.Add("https://example.test/library.xsl", Library("urn:library", "1", "<xsl:template name='hook' visibility='abstract'/>", ""));
        XsltPackageReference reference = Reference("urn:library", "1", "library.xsl");
        string source = Library("urn:main", "1", "<xsl:use-package name='urn:library'><xsl:override><xsl:template name='hook'><xsl:call-template name='xsl:original'/></xsl:template></xsl:override></xsl:use-package>", "");

        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(source, resolver, new[] { reference }, true))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE3075));
    }

    [Test]
    public void DoesNotBindOriginalNamesInsideXPathStringLiterals()
    {
        string content =
            "<xsl:variable name='value' select=\"'$xsl:original'\"/>" +
            "<xsl:function xmlns:f='urn:functions' name='f:value' select=\"'xsl:original()'\"/>";

        XsltPackagePlan package = PackagePlan(Compile(Library("urn:main", "1", content, ""), new PackageResolver(), null, false));

        Assert.That(package.Components.SelectMany(component => component.References), Is.Empty);
    }

    [Test]
    public void DetectsPackageDependencyCycles()
    {
        PackageResolver resolver = new PackageResolver();
        resolver.Resources.Add("https://example.test/library.xsl", Library("urn:library", "1", "<xsl:use-package name='urn:main'/>", ""));
        resolver.Resources.Add("https://example.test/main-copy.xsl", Library("urn:main", "1.0", "", ""));
        XsltPackageReference library = Reference("urn:library", "1", "library.xsl");
        XsltPackageReference main = Reference("urn:main", "1.0", "main-copy.xsl");
        string source = Library("urn:main", "1", "<xsl:use-package name='urn:library'/>", "");

        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(source, resolver, new[] { library, main }, false))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE3005));
    }

    [Test]
    public void AllowsDependenciesOnADifferentVersionOfTheSamePackageName()
    {
        PackageResolver resolver = new PackageResolver();
        resolver.Resources.Add("https://example.test/previous.xsl", Library("urn:main", "1", "", ""));
        string source = Library("urn:main", "2", "<xsl:use-package name='urn:main' package-version='1'/>", "");

        XsltPackagePlan package = PackagePlan(Compile(source, resolver, new[] { Reference("urn:main", "1", "previous.xsl") }, false));

        Assert.That(package.Uses.Single().Target.Version, Is.EqualTo("1"));
    }

    [Test]
    public void RejectsUsePackageFromAnImportedStylesheetLevel()
    {
        PackageResolver resolver = new PackageResolver();
        resolver.Resources.Add("https://example.test/imported.xsl", Stylesheet("<xsl:use-package name='urn:library'/>"));
        string source = Stylesheet("<xsl:import href='imported.xsl'/>");

        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(source, resolver, null, false))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE3008));
    }

    [Test]
    public void ReportsMissingAndMismatchedPackagesAtTheUseDeclaration()
    {
        string source = "<xsl:package xmlns:xsl='" + Xsl + "' name='urn:main' version='3.0'>\n  <xsl:use-package name='urn:missing' package-version='2+'/>\n</xsl:package>";

        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(source, new PackageResolver(), null, false))!;

        Assert.Multiple(() =>
        {
            Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE3000));
            Assert.That(error.Diagnostic.Range!.Start!.Line, Is.EqualTo(2));
            Assert.That(error.Diagnostic.Range.Start.SourceUri, Is.EqualTo("https://example.test/main.xsl"));
        });
    }

    [Test]
    public void RejectsAResolvedPackageWhoseManifestDoesNotMatchTheReference()
    {
        PackageResolver resolver = new PackageResolver();
        resolver.Resources.Add("https://example.test/library.xsl", Library("urn:other", "1", "", ""));
        string source = Library("urn:main", "1", "<xsl:use-package name='urn:library'/>", "");

        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(source, resolver, new[] { Reference("urn:library", "1", "library.xsl") }, false))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE3000));
    }

    [Test]
    public void PublicPackageResolverLoadsPackagesAndModulesThroughTheDefaultResolver()
    {
        string directory = Path.Combine(Path.GetTempPath(), "xml-xslt-package-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string packagePath = Path.Combine(directory, "package.xsl");
        string modulePath = Path.Combine(directory, "module.xsl");
        File.WriteAllText(packagePath, Library("urn:public", "3.1", "<xsl:template name='entry' visibility='public'/>", ""));
        File.WriteAllText(modulePath, Stylesheet(""));
        try
        {
            XmlResolverPolicy policy = new XmlResolverPolicy
            {
                Access = XmlResourceAccess.Allowed,
                AllowedSchemes = ["file"],
                AllowedRoots = [directory],
                MaximumDepth = 16,
                MaximumResourceBytes = 1048576,
                MaximumWorkUnits = 1048576
            };
            XsltCompileOptions options = new XsltCompileOptions { ResolverPolicy = policy };
            XsltPackageReference reference = Reference("urn:public", "3.1", new Uri(packagePath).AbsoluteUri);
            XsltPackageResolver packageResolver = new XsltPackageResolver();

            XsltExecutable executable = packageResolver.ResolveXsltPackage(reference, options);
            XmlResolvedResource module = packageResolver.ResolveXsltModule("module.xsl", new Uri(directory + Path.DirectorySeparatorChar).AbsoluteUri, policy);

            Assert.Multiple(() =>
            {
                Assert.That(executable.PackageName!.LocalName, Is.EqualTo("urn:public"));
                Assert.That(executable.PackageVersion, Is.EqualTo("3.1"));
                Assert.That(PackagePlan(executable).IsLibrary, Is.True);
                Assert.That(module.Kind, Is.EqualTo(XmlResourceKind.Stylesheet));
                Assert.That(module.AbsoluteUri, Is.EqualTo(new Uri(modulePath).AbsoluteUri));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static XsltPackageReference Reference(string name, string version, string location)
    {
        return new XsltPackageReference
        {
            Name = new XmlName { LocalName = name },
            Version = version,
            LocationUri = location
        };
    }

    private static XsltPackagePlan PackagePlan(XsltExecutable executable)
    {
        return ((XsltStylesheetPlan)executable.Plan!).Package!;
    }

    private static XsltComponent Component(XsltPackagePlan package, XsltComponentKind kind, string name, int arity)
    {
        return package.Components.Single(component => component.Kind == kind && component.Name == name && component.Arity == arity && component.Visibility != XsltComponentVisibility.Hidden);
    }

    private static string Stylesheet(string content)
    {
        return "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0'>" + content + "</xsl:stylesheet>";
    }

    private static string Library(string name, string version, string content, string namespaces)
    {
        return "<xsl:package xmlns:xsl='" + Xsl + "' " + namespaces + " name='" + name + "' package-version='" + version + "' version='3.0'>" + content + "</xsl:package>";
    }

    private static XsltExecutable Compile(string source, PackageResolver resolver, XsltPackageReference[]? packages, bool separately)
    {
        XsltCompileOptions options = Options(packages, separately);
        return new XsltCompiler().CompileXslt(new XmlInput { Text = source }, options, resolver);
    }

    private static XsltExecutable CompileLibrary(string source, PackageResolver resolver, XsltPackageReference[]? packages)
    {
        XsltCompileOptions options = Options(packages, true);
        return new XsltStylesheetLoader(options, resolver, new XsltPackageCompilationContext(), true).CompileXsltStylesheet(new XmlInput { Text = source, BaseUri = "https://example.test/main.xsl" });
    }

    private static XsltCompileOptions Options(XsltPackageReference[]? packages, bool separately)
    {
        return new XsltCompileOptions
        {
            BaseUri = "https://example.test/main.xsl",
            Packages = packages,
            CompilePackagesSeparately = separately,
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

    private sealed class PackageResolver : IXmlResourceResolver
    {
        internal Dictionary<string, string> Resources = new Dictionary<string, string>(StringComparer.Ordinal);
        internal List<string> RequestedUris = new List<string>();
        internal List<XmlResourceKind> Kinds = new List<XmlResourceKind>();

        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            string requested = systemIdentifier ?? "";
            RequestedUris.Add(requested);
            Kinds.Add(kind);
            string absolute = new Uri(new Uri(baseUri!), requested).AbsoluteUri;
            return new XmlResolvedResource
            {
                Kind = kind,
                Access = XmlResourceAccess.Allowed,
                Input = new XmlInput { Text = Resources[absolute] },
                RequestedUri = requested,
                AbsoluteUri = absolute
            };
        }
    }
}
