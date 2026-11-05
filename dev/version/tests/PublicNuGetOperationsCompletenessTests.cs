using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class PublicNuGetOperationsCompletenessTests
{
    [TestCase("1", 1, 2, false, "", "1.0.0")]
    [TestCase("01.002", 2, 1, false, "", "1.2.0")]
    [TestCase("01.002.0003", 3, 0, false, "", "1.2.3")]
    [TestCase("01.002.0003.0004", 3, 0, true, "0004", "1.2.3.4")]
    public void ParseVersion_CoversOneToFourPartsAndRevisionNormalization(
        string text,
        int componentCount,
        int missingComponentCount,
        bool revisionPresent,
        string revision,
        string normalized)
    {
        INuGetResolution operations = new NuGetOperations();

        VersionParseResult result = operations.ParseVersion(text);

        Assert.That(result.success, Is.True, text);
        Assert.That(result.version, Is.Not.Null, text);
        Assert.That(result.version!.numericComponents, Has.Length.EqualTo(componentCount), text);
        Assert.That(result.version.missingComponentCount, Is.EqualTo(missingComponentCount), text);
        Assert.That(result.version.revisionComponentPresent, Is.EqualTo(revisionPresent), text);
        Assert.That(result.version.revisionComponent, Is.EqualTo(revision), text);
        Assert.That(operations.NormalizeVersion(text), Is.EqualTo(normalized), text);
    }

    [Test]
    public void VersionIdentity_CoversSemVer2LabelsBuildMetadataAndCaseInsensitivePrecedence()
    {
        INuGetResolution operations = new NuGetOperations();

        VersionParseResult result = operations.ParseVersion("1.2.3-alpha.1+Meta.0007");

        Assert.That(result.success, Is.True);
        Assert.That(result.version, Is.Not.Null);
        Assert.That(result.version!.prereleaseIdentifiers, Has.Length.EqualTo(2));
        Assert.That(result.version.prereleaseIdentifiers[0].text, Is.EqualTo("alpha"));
        Assert.That(result.version.prereleaseIdentifiers[1].text, Is.EqualTo("1"));
        Assert.That(result.version.buildIdentifiers, Has.Length.EqualTo(2));
        Assert.That(operations.NormalizeVersion(result.sourceText), Is.EqualTo("1.2.3-alpha.1"));
        Assert.That(operations.CompareVersions("1.2.3-RC.1+one", "1.2.3-rc.1+two"), Is.Zero);
        Assert.That(operations.CompareVersions("1.2.3-alpha.2", "1.2.3-alpha.10"), Is.LessThan(0));
        Assert.That(operations.CompareVersions("1.2.3.1", "1.2.3"), Is.GreaterThan(0));
    }

    [TestCase("[1.0]", 1, RangeOperator.Equal)]
    [TestCase("1.0", 1, RangeOperator.GreaterThanOrEqual)]
    [TestCase("[1.0,)", 1, RangeOperator.GreaterThanOrEqual)]
    [TestCase("(,2.0]", 1, RangeOperator.LessThanOrEqual)]
    [TestCase("[1.0,2.0)", 2, RangeOperator.GreaterThanOrEqual)]
    public void ParseRange_CoversExactBareUnboundedAndBoundedIntervals(
        string text,
        int comparatorCount,
        RangeOperator firstOperator)
    {
        INuGetResolution operations = new NuGetOperations();

        VersionRangeParseResult result = operations.ParseRange(text);

        Assert.That(result.success, Is.True, text);
        Assert.That(result.range, Is.Not.Null, text);
        Assert.That(result.range!.sets, Has.Length.EqualTo(1), text);
        Assert.That(result.range.sets[0].comparators, Has.Length.EqualTo(comparatorCount), text);
        Assert.That(result.range.sets[0].comparators[0].rangeOperator, Is.EqualTo(firstOperator), text);

        if (comparatorCount == 2)
        {
            Assert.That(result.range.sets[0].comparators[1].rangeOperator, Is.EqualTo(RangeOperator.LessThan), text);
        }
    }

    [TestCase("*", 0, false, false, 0)]
    [TestCase("6.*", 1, false, false, 0)]
    [TestCase("6.0.*", 2, false, false, 0)]
    [TestCase("*-*", 0, true, true, 0)]
    [TestCase("1.1.*-*", 2, true, true, 0)]
    [TestCase("1.2.0-rc.*", 3, true, false, 1)]
    public void ParseFloatingVersion_CoversStableAndPrereleasePatterns(
        string text,
        int precision,
        bool includePrerelease,
        bool prereleaseAny,
        int prefixCount)
    {
        INuGetResolution operations = new NuGetOperations();

        NuGetFloatingVersion floating = operations.ParseFloatingVersion(text);
        VersionRangeParseResult range = operations.ParseRange(text);

        Assert.That(floating.valid, Is.True, text);
        Assert.That(floating.numericPrecision, Is.EqualTo(precision), text);
        Assert.That(floating.includePrerelease, Is.EqualTo(includePrerelease), text);
        Assert.That(floating.prereleaseAny, Is.EqualTo(prereleaseAny), text);
        Assert.That(floating.prereleasePrefix, Has.Length.EqualTo(prefixCount), text);
        Assert.That(range.success, Is.True, text);
        Assert.That(range.range, Is.Not.Null, text);
        Assert.That(range.range!.nuGetFloatingVersion, Is.Not.Null, text);
    }

    [Test]
    public void LowestSelection_CoversSourceOrderingDisabledSourcesAndPrereleaseOptions()
    {
        INuGetResolution operations = new NuGetOperations();
        NuGetSourceIdentity primary = Source("primary");
        NuGetSourceIdentity secondary = Source("secondary");
        NuGetSourceIdentity disabled = Source("disabled");
        NuGetSourceIdentity unconfigured = Source("unconfigured");
        NuGetPackageSourcePriority[] priorities = new NuGetPackageSourcePriority[]
        {
            new NuGetPackageSourcePriority(primary, 0, true),
            new NuGetPackageSourcePriority(secondary, 1, true),
            new NuGetPackageSourcePriority(disabled, 2, false)
        };
        NuGetSourcePackageMetadata secondaryTie = AvailablePackage(
            operations,
            "Example",
            "1.0.0",
            secondary,
            new NuGetDependencyGroup[0]);
        NuGetSourcePackageMetadata primaryTie = AvailablePackage(
            operations,
            "Example",
            "1.0.0",
            primary,
            new NuGetDependencyGroup[0]);
        NuGetSourcePackageMetadata disabledLower = AvailablePackage(
            operations,
            "Example",
            "0.8.0",
            disabled,
            new NuGetDependencyGroup[0]);
        NuGetSourcePackageMetadata unconfiguredLower = AvailablePackage(
            operations,
            "Example",
            "0.9.0",
            unconfigured,
            new NuGetDependencyGroup[0]);
        NuGetSourcePackageMetadata prerelease = AvailablePackage(
            operations,
            "Preview",
            "2.0.0-beta.1",
            primary,
            new NuGetDependencyGroup[0]);

        NuGetSourcePackageMetadata? selected = operations.ResolveLowestApplicableVersion(
            Dependency(operations, "Example", "[0.0.0,)"),
            new NuGetSourcePackageMetadata[]
            {
                secondaryTie,
                disabledLower,
                unconfiguredLower,
                primaryTie
            },
            priorities);
        VersionSelectionOptions include = new VersionSelectionOptions();
        include.prereleasePolicy = PrereleasePolicy.Include;
        NuGetSourcePackageMetadata? selectedPrerelease = operations.ResolveLowestApplicableVersionWithOptions(
            Dependency(operations, "Preview", "[1.0.0,3.0.0)"),
            new NuGetSourcePackageMetadata[] { prerelease },
            priorities,
            include);
        VersionSelectionOptions automatic = new VersionSelectionOptions();
        automatic.prereleasePolicy = PrereleasePolicy.AutoIfNoStableCandidate;
        NuGetSourcePackageMetadata? automaticPrerelease = operations.ResolveLowestApplicableVersionWithOptions(
            Dependency(operations, "Preview", "[1.0.0,3.0.0)"),
            new NuGetSourcePackageMetadata[] { prerelease },
            priorities,
            automatic);

        Assert.That(selected, Is.SameAs(primaryTie));
        Assert.That(selectedPrerelease, Is.SameAs(prerelease));
        Assert.That(automaticPrerelease, Is.SameAs(prerelease));
    }

    [Test]
    public void RejectionPolicies_CoverUnavailableUnlistedDeprecatedAndVulnerablePackages()
    {
        INuGetResolution operations = new NuGetOperations();
        NuGetSourceIdentity source = Source("feed");
        NuGetPackageSourcePriority[] priorities = Priorities(source);
        NuGetSourcePackageMetadata unavailable = Package(
            operations,
            "Example",
            "0.9.0",
            source,
            NuGetListedState.Listed,
            NuGetPackageAvailability.Unavailable,
            NotDeprecated(),
            new NuGetPackageVulnerabilityMetadata[0],
            new NuGetDependencyGroup[0]);
        NuGetSourcePackageMetadata unlisted = Package(
            operations,
            "Example",
            "1.0.0",
            source,
            NuGetListedState.Unlisted,
            NuGetPackageAvailability.Available,
            NotDeprecated(),
            new NuGetPackageVulnerabilityMetadata[0],
            new NuGetDependencyGroup[0]);
        NuGetSourcePackageMetadata deprecated = Package(
            operations,
            "Example",
            "1.1.0",
            source,
            NuGetListedState.Listed,
            NuGetPackageAvailability.Available,
            new NuGetPackageDeprecationMetadata(
                true,
                new NuGetPackageDeprecationReason[] { NuGetPackageDeprecationReason.Legacy },
                "Use the replacement.",
                null),
            new NuGetPackageVulnerabilityMetadata[0],
            new NuGetDependencyGroup[0]);
        NuGetSourcePackageMetadata vulnerable = Package(
            operations,
            "Example",
            "1.2.0",
            source,
            NuGetListedState.Listed,
            NuGetPackageAvailability.Available,
            NotDeprecated(),
            new NuGetPackageVulnerabilityMetadata[]
            {
                new NuGetPackageVulnerabilityMetadata(
                    NuGetPackageVulnerabilitySeverity.High,
                    "https://advisories.example.test/high")
            },
            new NuGetDependencyGroup[0]);
        NuGetSourcePackageMetadata safe = AvailablePackage(
            operations,
            "Example",
            "1.3.0",
            source,
            new NuGetDependencyGroup[0]);
        NuGetSourcePackageMetadata[] packages = new NuGetSourcePackageMetadata[]
        {
            unavailable,
            unlisted,
            deprecated,
            vulnerable,
            safe
        };
        NuGetPackageRejectionPolicy strict = new NuGetPackageRejectionPolicy();
        strict.rejectUnlistedPackages = true;
        strict.rejectDeprecatedPackages = true;
        strict.rejectVulnerablePackages = true;
        strict.minimumRejectedVulnerabilitySeverity = NuGetPackageVulnerabilitySeverity.Moderate;

        NuGetSourcePackageMetadata? defaultLowest = operations.ResolveLowestApplicableVersion(
            Dependency(operations, "Example", "[0.0.0,)"),
            packages,
            priorities);
        NuGetSourcePackageMetadata? strictLowest = operations.ResolveLowestApplicableVersionWithPolicies(
            Dependency(operations, "Example", "[0.0.0,)"),
            packages,
            priorities,
            strict,
            new VersionSelectionOptions());
        NuGetSourcePackageMetadata? strictHighest = operations.ResolveHighestFloatingVersionWithPolicy(
            "example",
            "1.*",
            packages,
            priorities,
            strict);

        Assert.That(defaultLowest, Is.SameAs(unlisted));
        Assert.That(strictLowest, Is.SameAs(safe));
        Assert.That(strictHighest, Is.SameAs(safe));
    }

    [Test]
    public void HighestFloatingSelection_CoversStablePrereleaseAndSourceTieBreaking()
    {
        INuGetResolution operations = new NuGetOperations();
        NuGetSourceIdentity primary = Source("primary");
        NuGetSourceIdentity secondary = Source("secondary");
        NuGetSourcePackageMetadata stable = AvailablePackage(
            operations,
            "Example",
            "1.9.0",
            secondary,
            new NuGetDependencyGroup[0]);
        NuGetSourcePackageMetadata secondaryPreview = AvailablePackage(
            operations,
            "Example",
            "1.10.0-beta.2",
            secondary,
            new NuGetDependencyGroup[0]);
        NuGetSourcePackageMetadata primaryPreview = AvailablePackage(
            operations,
            "Example",
            "1.10.0-beta.2",
            primary,
            new NuGetDependencyGroup[0]);

        NuGetSourcePackageMetadata? stableSelected = operations.ResolveHighestFloatingVersion(
            "EXAMPLE",
            "1.*",
            new NuGetSourcePackageMetadata[] { stable, secondaryPreview, primaryPreview },
            Priorities(primary, secondary));
        NuGetSourcePackageMetadata? previewSelected = operations.ResolveHighestFloatingVersion(
            "EXAMPLE",
            "1.*-*",
            new NuGetSourcePackageMetadata[] { stable, secondaryPreview, primaryPreview },
            Priorities(primary, secondary));

        Assert.That(stableSelected, Is.SameAs(stable));
        Assert.That(previewSelected, Is.SameAs(primaryPreview));
    }

    [Test]
    public void PackageMetadata_CoversIdentityNormalizationDirectAndTargetedDependencies()
    {
        INuGetResolution operations = new NuGetOperations();
        string nuspec = @"<package>
  <metadata minClientVersion=""6.0"">
    <id>Example.Package</id>
    <version>01.002.0000+build.9</version>
    <title>Example</title>
    <authors>Alice;Bob</authors>
    <owners>Owners</owners>
    <description>Description</description>
    <tags>one two</tags>
    <requireLicenseAcceptance>true</requireLicenseAcceptance>
    <developmentDependency>true</developmentDependency>
    <dependencies>
      <dependency id=""Direct.Dependency"" version=""[1.0,2.0)"" />
      <group targetFramework=""net8.0"">
        <dependency id=""Framework.Dependency"" version=""2.*"" />
      </group>
    </dependencies>
  </metadata>
</package>";

        NuGetPackageMetadata metadata = operations.ParsePackageMetadata(nuspec);
        NuGetDependencyGroup[] groups = operations.ParseDependencyGroups(nuspec);

        Assert.That(metadata.identity.packageId.value, Is.EqualTo("Example.Package"));
        Assert.That(metadata.identity.packageId.normalizedValue, Is.EqualTo("example.package"));
        Assert.That(metadata.identity.normalizedVersionText, Is.EqualTo("1.2.0"));
        Assert.That(metadata.minClientVersion, Is.EqualTo("6.0"));
        Assert.That(metadata.requireLicenseAcceptance, Is.True);
        Assert.That(metadata.developmentDependency, Is.True);
        Assert.That(groups, Has.Length.EqualTo(2));
        Assert.That(groups[0].targetFrameworkSpecified, Is.False);
        Assert.That(groups[0].dependencies[0].packageId.normalizedValue, Is.EqualTo("direct.dependency"));
        Assert.That(groups[1].targetFramework, Is.EqualTo("net8.0"));
        Assert.That(groups[1].dependencies[0].versionRange.nuGetFloatingVersion, Is.Not.Null);
    }

    [TestCase("net8.0", "net6.0", true)]
    [TestCase("net8.0", "netstandard2.1", true)]
    [TestCase("net472", "netstandard2.0", true)]
    [TestCase("net45", "netstandard2.0", false)]
    [TestCase("net8.0-windows10.0.19041", "net8.0-windows10.0", true)]
    [TestCase("net8.0", "net8.0-windows", false)]
    public void TargetFrameworkCompatibility_CoversFrameworkFamiliesAndPlatforms(
        string projectText,
        string candidateText,
        bool expected)
    {
        INuGetResolution operations = new NuGetOperations();

        Assert.That(
            operations.IsTargetFrameworkCompatible(projectText, candidateText),
            Is.EqualTo(expected));
    }

    [Test]
    public void TargetFrameworkOperations_CoverParsedFallbackAndEveryDependencyGroupPath()
    {
        INuGetResolution operations = new NuGetOperations();
        NuGetTargetFrameworkParseResult project = operations.ParseTargetFramework("NET8.0");
        NuGetTargetFrameworkParseResult candidate = operations.ParseTargetFramework("net48");
        NuGetTargetFrameworkFallbackParseResult fallback = operations.ParseTargetFrameworkFallbackList(
            "net48;netstandard2.0");
        NuGetDependencyGroup any = new NuGetDependencyGroup(null, new NuGetDependency[0]);
        NuGetDependencyGroup compatible = new NuGetDependencyGroup(
            "netstandard2.0",
            new NuGetDependency[0]);
        NuGetDependencyGroup exact = new NuGetDependencyGroup("net8.0", new NuGetDependency[0]);
        NuGetDependencyGroup[] groups = new NuGetDependencyGroup[] { any, compatible, exact };
        NuGetPackageMetadata metadata = Metadata(
            operations,
            "Group.Package",
            "1.0.0",
            groups);

        Assert.That(project.success, Is.True);
        Assert.That(project.framework, Is.Not.Null);
        Assert.That(project.framework!.normalizedText, Is.EqualTo("net8.0"));
        Assert.That(fallback.success, Is.True);
        Assert.That(fallback.fallbackList, Is.Not.Null);
        Assert.That(fallback.fallbackList!.frameworks, Has.Length.EqualTo(2));
        Assert.That(
            operations.AreTargetFrameworksCompatibleWithFallback(
                project.framework,
                candidate.framework!,
                "net48"),
            Is.True);
        Assert.That(operations.SelectBestDependencyGroup("net8.0", groups), Is.SameAs(exact));
        Assert.That(operations.SelectBestPackageDependencyGroup("net8.0", metadata), Is.SameAs(exact));
        Assert.That(
            operations.SelectBestParsedDependencyGroup(project.framework, groups),
            Is.SameAs(exact));
        Assert.That(
            operations.SelectBestParsedPackageDependencyGroup(project.framework, metadata),
            Is.SameAs(exact));
    }

    [Test]
    public void DependencyResolution_CoversMissingAmbiguousAndCyclicGraphConflicts()
    {
        INuGetResolution operations = new NuGetOperations();
        NuGetSourceIdentity first = Source("first");
        NuGetSourceIdentity second = Source("second");
        NuGetDependency rootA = Dependency(operations, "A", "[1.0.0]");
        NuGetDependency rootMissing = Dependency(operations, "Missing", "[1.0.0]");
        NuGetSourcePackageMetadata firstA = AvailablePackage(
            operations,
            "A",
            "1.0.0",
            first,
            new NuGetDependencyGroup[0]);
        NuGetSourcePackageMetadata secondA = AvailablePackage(
            operations,
            "A",
            "1.0.0",
            second,
            new NuGetDependencyGroup[0]);
        NuGetDependency dependencyB = Dependency(operations, "B", "[1.0.0]");
        NuGetDependency dependencyA = Dependency(operations, "A", "[1.0.0]");
        NuGetSourcePackageMetadata cyclicA = AvailablePackage(
            operations,
            "A",
            "1.0.0",
            first,
            new NuGetDependencyGroup[]
            {
                new NuGetDependencyGroup(null, new NuGetDependency[] { dependencyB })
            });
        NuGetSourcePackageMetadata cyclicB = AvailablePackage(
            operations,
            "B",
            "1.0.0",
            first,
            new NuGetDependencyGroup[]
            {
                new NuGetDependencyGroup(null, new NuGetDependency[] { dependencyA })
            });

        NuGetDependencyResolutionResult missing = operations.ResolveDependencies(
            new NuGetDependency[] { rootMissing },
            new NuGetSourcePackageMetadata[0],
            new NuGetPackageSourcePriority[0]);
        NuGetDependencyResolutionResult ambiguous = operations.ResolveDependencies(
            new NuGetDependency[] { rootA },
            new NuGetSourcePackageMetadata[] { firstA, secondA },
            new NuGetPackageSourcePriority[0]);
        NuGetDependencyResolutionResult cyclic = operations.ResolveDependencies(
            new NuGetDependency[] { rootA },
            new NuGetSourcePackageMetadata[] { cyclicA, cyclicB },
            Priorities(first));

        AssertConflict(missing, NuGetResolverDiagnosticCodes.MissingPackageMetadata);
        AssertConflict(ambiguous, NuGetResolverDiagnosticCodes.AmbiguousSourceMatches);
        AssertConflict(cyclic, NuGetResolverDiagnosticCodes.CyclicDependency);
    }

    [Test]
    public void DependencyResolutionWithPolicy_CanAdmitUnavailablePackages()
    {
        INuGetResolution operations = new NuGetOperations();
        NuGetSourceIdentity source = Source("feed");
        NuGetDependency root = Dependency(operations, "A", "[1.0.0]");
        NuGetSourcePackageMetadata unavailable = Package(
            operations,
            "A",
            "1.0.0",
            source,
            NuGetListedState.Listed,
            NuGetPackageAvailability.Unavailable,
            NotDeprecated(),
            new NuGetPackageVulnerabilityMetadata[0],
            new NuGetDependencyGroup[0]);
        NuGetPackageRejectionPolicy allowUnavailable = new NuGetPackageRejectionPolicy();
        allowUnavailable.rejectUnavailablePackages = false;

        NuGetDependencyResolutionResult rejected = operations.ResolveDependencies(
            new NuGetDependency[] { root },
            new NuGetSourcePackageMetadata[] { unavailable },
            Priorities(source));
        NuGetDependencyResolutionResult accepted = operations.ResolveDependenciesWithPolicy(
            new NuGetDependency[] { root },
            new NuGetSourcePackageMetadata[] { unavailable },
            Priorities(source),
            allowUnavailable);

        AssertConflict(rejected, NuGetResolverDiagnosticCodes.UnsatisfiedConstraints);
        Assert.That(accepted.conflicts, Is.Empty);
        Assert.That(accepted.nodes, Has.Length.EqualTo(1));
        Assert.That(accepted.nodes[0].packageMetadata, Is.SameAs(unavailable));
    }

    [Test]
    public void FrameworkDependencyResolution_CoversTextParsedAndPolicyEntryPoints()
    {
        INuGetResolution operations = new NuGetOperations();
        NuGetSourceIdentity source = Source("feed");
        NuGetDependency root = Dependency(operations, "Root", "[1.0.0]");
        NuGetDependency netDependency = Dependency(operations, "Net.Dependency", "[1.0.0]");
        NuGetDependency standardDependency = Dependency(
            operations,
            "Standard.Dependency",
            "[1.0.0]");
        NuGetSourcePackageMetadata rootPackage = AvailablePackage(
            operations,
            "Root",
            "1.0.0",
            source,
            new NuGetDependencyGroup[]
            {
                new NuGetDependencyGroup("netstandard2.0", new NuGetDependency[] { standardDependency }),
                new NuGetDependencyGroup("net8.0", new NuGetDependency[] { netDependency })
            });
        NuGetSourcePackageMetadata netPackage = AvailablePackage(
            operations,
            "Net.Dependency",
            "1.0.0",
            source,
            new NuGetDependencyGroup[0]);
        NuGetSourcePackageMetadata standardPackage = AvailablePackage(
            operations,
            "Standard.Dependency",
            "1.0.0",
            source,
            new NuGetDependencyGroup[0]);
        NuGetSourcePackageMetadata[] packages = new NuGetSourcePackageMetadata[]
        {
            rootPackage,
            netPackage,
            standardPackage
        };
        NuGetPackageSourcePriority[] priorities = Priorities(source);
        NuGetTargetFramework parsedFramework = operations.ParseTargetFramework("net8.0").framework!;
        NuGetPackageRejectionPolicy policy = new NuGetPackageRejectionPolicy();

        NuGetDependencyResolutionResult text = operations.ResolveDependenciesForFramework(
            new NuGetDependency[] { root }, packages, priorities, "net8.0");
        NuGetDependencyResolutionResult textPolicy = operations.ResolveDependenciesForFrameworkWithPolicy(
            new NuGetDependency[] { root }, packages, priorities, "net8.0", policy);
        NuGetDependencyResolutionResult parsed = operations.ResolveDependenciesForParsedFramework(
            new NuGetDependency[] { root }, packages, priorities, parsedFramework);
        NuGetDependencyResolutionResult parsedPolicy =
            operations.ResolveDependenciesForParsedFrameworkWithPolicy(
                new NuGetDependency[] { root },
                packages,
                priorities,
                parsedFramework,
                policy);

        AssertNetFrameworkResolution(text);
        AssertNetFrameworkResolution(textPolicy);
        AssertNetFrameworkResolution(parsed);
        AssertNetFrameworkResolution(parsedPolicy);
    }

    [Test]
    public void DiagnosticFailures_AreExposedThroughResultsAndExceptionsAsDocumented()
    {
        INuGetResolution operations = new NuGetOperations();

        VersionParseResult emptyVersion = operations.ParseVersion("  ");
        VersionParseResult malformedVersion = operations.ParseVersion("1.2.3.4.5");
        VersionRangeParseResult malformedRange = operations.ParseRange("[2.0,1.0]");
        NuGetFloatingVersion malformedFloating = operations.ParseFloatingVersion("1.*+build");
        NuGetTargetFrameworkParseResult nullFramework = operations.ParseTargetFramework(null);
        NuGetTargetFrameworkFallbackParseResult malformedFallback =
            operations.ParseTargetFrameworkFallbackList("net8.0;;netstandard2.0");

        Assert.That(emptyVersion.success, Is.False);
        Assert.That(emptyVersion.diagnostics, Has.Length.EqualTo(1));
        Assert.That(malformedVersion.success, Is.False);
        Assert.That(malformedVersion.diagnostics, Has.Length.EqualTo(1));
        Assert.That(malformedRange.success, Is.False);
        Assert.That(malformedRange.diagnostics, Has.Length.EqualTo(1));
        Assert.That(malformedFloating.valid, Is.False);
        Assert.That(nullFramework.success, Is.False);
        Assert.That(nullFramework.diagnostics[0].code, Is.EqualTo(NuGetTargetFrameworkDiagnosticCodes.NullInput));
        Assert.That(malformedFallback.success, Is.False);
        Assert.That(
            malformedFallback.diagnostics[0].code,
            Is.EqualTo(NuGetTargetFrameworkDiagnosticCodes.InvalidFallbackList));

        Assert.That(
            delegate { operations.ParseVersion(null!); },
            Throws.TypeOf<System.ArgumentNullException>());
        Assert.That(
            delegate { operations.ParseRange(null!); },
            Throws.TypeOf<System.ArgumentNullException>());
        Assert.That(operations.CompareVersions("1.0.0", "invalid"), Is.GreaterThan(0));
        Assert.That(operations.CompareVersions("invalid", "also-invalid"), Is.Zero);
        Assert.That(
            delegate
            {
                operations.ResolveHighestFloatingVersion(
                    "A",
                    "invalid",
                    new NuGetSourcePackageMetadata[0],
                    new NuGetPackageSourcePriority[0]);
            },
            Throws.TypeOf<System.ArgumentException>());
        Assert.That(
            delegate
            {
                operations.ParsePackageMetadata(
                    "<package><metadata><version>1.0.0</version></metadata></package>");
            },
            Throws.TypeOf<System.ArgumentException>());
        Assert.That(
            delegate { operations.ParsePackageMetadata("<package><metadata></package>"); },
            Throws.TypeOf<System.Xml.XmlException>());
        Assert.That(
            delegate
            {
                operations.ResolveDependencies(
                    new NuGetDependency[] { null! },
                    new NuGetSourcePackageMetadata[0],
                    new NuGetPackageSourcePriority[0]);
            },
            Throws.TypeOf<System.ArgumentException>());
    }

    static NuGetDependency Dependency(
        INuGetResolution operations,
        string packageId,
        string rangeText)
    {
        VersionRangeParseResult range = operations.ParseRange(rangeText);
        Assert.That(range.success, Is.True, rangeText);
        Assert.That(range.range, Is.Not.Null, rangeText);
        return new NuGetDependency(new NuGetPackageId(packageId), range.range!, rangeText);
    }

    static NuGetPackageMetadata Metadata(
        INuGetResolution operations,
        string packageId,
        string versionText,
        NuGetDependencyGroup[] groups)
    {
        VersionParseResult version = operations.ParseVersion(versionText);
        Assert.That(version.success, Is.True, versionText);
        Assert.That(version.version, Is.Not.Null, versionText);

        return new NuGetPackageMetadata(
            new NuGetPackageVersionIdentity(new NuGetPackageId(packageId), version.version!),
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            false,
            false,
            groups);
    }

    static NuGetSourcePackageMetadata AvailablePackage(
        INuGetResolution operations,
        string packageId,
        string versionText,
        NuGetSourceIdentity source,
        NuGetDependencyGroup[] groups)
    {
        return Package(
            operations,
            packageId,
            versionText,
            source,
            NuGetListedState.Listed,
            NuGetPackageAvailability.Available,
            NotDeprecated(),
            new NuGetPackageVulnerabilityMetadata[0],
            groups);
    }

    static NuGetSourcePackageMetadata Package(
        INuGetResolution operations,
        string packageId,
        string versionText,
        NuGetSourceIdentity source,
        NuGetListedState listedState,
        NuGetPackageAvailability availability,
        NuGetPackageDeprecationMetadata deprecation,
        NuGetPackageVulnerabilityMetadata[] vulnerabilities,
        NuGetDependencyGroup[] groups)
    {
        return new NuGetSourcePackageMetadata(
            source,
            Metadata(operations, packageId, versionText, groups),
            listedState,
            availability,
            deprecation,
            vulnerabilities);
    }

    static NuGetPackageDeprecationMetadata NotDeprecated()
    {
        return new NuGetPackageDeprecationMetadata(
            false,
            new NuGetPackageDeprecationReason[0],
            "",
            null);
    }

    static NuGetSourceIdentity Source(string name)
    {
        return new NuGetSourceIdentity(name, "https://" + name + ".example.test/v3/index.json");
    }

    static NuGetPackageSourcePriority[] Priorities(NuGetSourceIdentity source)
    {
        return new NuGetPackageSourcePriority[]
        {
            new NuGetPackageSourcePriority(source, 0, true)
        };
    }

    static NuGetPackageSourcePriority[] Priorities(
        NuGetSourceIdentity primary,
        NuGetSourceIdentity secondary)
    {
        return new NuGetPackageSourcePriority[]
        {
            new NuGetPackageSourcePriority(primary, 0, true),
            new NuGetPackageSourcePriority(secondary, 1, true)
        };
    }

    static void AssertConflict(NuGetDependencyResolutionResult result, string code)
    {
        Assert.That(result.conflicts, Has.Length.EqualTo(1));
        Assert.That(result.conflicts[0].diagnosticCode, Is.EqualTo(code));
        Assert.That(result.conflicts[0].message, Is.Not.Empty);
    }

    static void AssertNetFrameworkResolution(NuGetDependencyResolutionResult result)
    {
        Assert.That(result.conflicts, Is.Empty);
        Assert.That(result.nodes, Has.Length.EqualTo(2));
        Assert.That(FindNode(result, "Root"), Is.Not.Null);
        Assert.That(FindNode(result, "Net.Dependency"), Is.Not.Null);
        Assert.That(FindNode(result, "Standard.Dependency"), Is.Null);
    }

    static NuGetDependencyGraphNode? FindNode(
        NuGetDependencyResolutionResult result,
        string packageId)
    {
        int index = 0;

        while (index < result.nodes.Length)
        {
            if (result.nodes[index].packageMetadata.packageMetadata.identity.packageId.normalizedValue
                == packageId.ToLowerInvariant())
            {
                return result.nodes[index];
            }

            index = index + 1;
        }

        return null;
    }
}
