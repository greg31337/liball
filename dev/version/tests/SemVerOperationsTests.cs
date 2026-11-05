using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class SemVerOperationsTests
{
    [Test]
    public void StrictParsing_ExposesFailFastAndDiagnosticPaths()
    {
        ISemVerOperations operations = new SemVerOperations();

        VersionParseResult success = operations.TryParseStrict("1.2.3-alpha.1+build.5");
        VersionParseResult failure = operations.TryParseStrict("01.2.3");
        UniversalVersion version = operations.ParseStrict("1.2.3");

        Assert.That(success.success, Is.True);
        Assert.That(success.versionPresent, Is.True);
        Assert.That(success.version, Is.Not.Null);
        Assert.That(success.dialect, Is.EqualTo(VersionDialect.SemVer));
        Assert.That(success.diagnostics, Is.Empty);
        Assert.That(operations.FormatCanonical(success.version!), Is.EqualTo("1.2.3-alpha.1+build.5"));
        Assert.That(operations.FormatCanonical(version), Is.EqualTo("1.2.3"));

        Assert.That(failure.success, Is.False);
        Assert.That(failure.versionPresent, Is.False);
        Assert.That(failure.version, Is.Null);
        Assert.That(failure.sourceText, Is.EqualTo("01.2.3"));
        Assert.That(failure.diagnostics, Has.Length.EqualTo(1));
        Assert.That(failure.diagnostics[0].code, Is.EqualTo(SemVerDiagnosticCodes.NumericComponentLeadingZero));
        Assert.That(failure.diagnostics[0].index, Is.EqualTo(0));
        Assert.That(() => operations.ParseStrict("01.2.3"), Throws.TypeOf<System.ArgumentException>());
    }

    [Test]
    public void LooseParsing_AcceptsAndCanonicalizesLooseSemVerInput()
    {
        ISemVerOperations operations = new SemVerOperations();
        string text = " = v01.002.0003-alpha.01+build.0005 ";

        VersionParseResult result = operations.TryParseLoose(text);
        UniversalVersion version = operations.ParseLoose(text);

        Assert.That(result.success, Is.True);
        Assert.That(result.sourceText, Is.EqualTo(text));
        Assert.That(result.dialect, Is.EqualTo(VersionDialect.NodeSemVer));
        Assert.That(operations.FormatCanonical(version), Is.EqualTo("v1.2.3-alpha.1+build.5"));
        Assert.That(operations.TryParseLoose("1.2").success, Is.False);
        Assert.That(() => operations.ParseLoose("1.2"), Throws.TypeOf<System.ArgumentException>());
    }

    [Test]
    public void Coercion_ExposesProducedVersionAndFailureDiagnostic()
    {
        ISemVerOperations operations = new SemVerOperations();

        VersionParseResult success = operations.TryCoerce("release-v0002.03");
        VersionParseResult failure = operations.TryCoerce("release-current");
        UniversalVersion version = operations.Coerce("release-v0002.03");

        Assert.That(success.success, Is.True);
        Assert.That(success.dialect, Is.EqualTo(VersionDialect.NodeSemVer));
        Assert.That(operations.FormatCanonical(version), Is.EqualTo("v2.3.0"));
        Assert.That(failure.success, Is.False);
        Assert.That(failure.diagnostics, Has.Length.EqualTo(1));
        Assert.That(failure.diagnostics[0].code, Is.EqualTo(SemVerDiagnosticCodes.NoCoercibleVersion));
        Assert.That(failure.diagnostics[0].index, Is.EqualTo("release-current".Length));
        Assert.That(() => operations.Coerce("release-current"), Throws.TypeOf<System.ArgumentException>());
    }

    [Test]
    public void Comparisons_DistinguishPrecedenceFromBuildIdentity()
    {
        ISemVerOperations operations = new SemVerOperations();

        Assert.That(operations.ComparePrecedence("1.0.0-alpha.2", "1.0.0-alpha.10"), Is.LessThan(0));
        Assert.That(operations.ComparePrecedence("1.0.0+build.1", "1.0.0+build.2"), Is.Zero);
        Assert.That(operations.CompareIdentity("1.0.0+build.1", "1.0.0+build.2"), Is.LessThan(0));
        Assert.That(operations.CompareIdentity("2.0.0", "1.9.9"), Is.GreaterThan(0));
        Assert.That(
            () => operations.ComparePrecedence("invalid", "1.0.0"),
            Throws.TypeOf<System.ArgumentException>());
    }

    [Test]
    public void NormalizedPackageIdentity_IgnoresBuildMetadataOnly()
    {
        ISemVerOperations operations = new SemVerOperations();

        Assert.That(
            operations.EqualsNormalizedPackageIdentity("1.2.3-alpha+linux", "1.2.3-alpha+windows"),
            Is.True);
        Assert.That(
            operations.EqualsNormalizedPackageIdentity("1.2.3-alpha+linux", "1.2.3-beta+linux"),
            Is.False);
    }

    [Test]
    public void CanonicalFormatting_RejectsNonSemVerDialectsAndNull()
    {
        ISemVerOperations operations = new SemVerOperations();
        VersionParseOptions mavenOptions = new VersionParseOptions(
            VersionDialect.Maven,
            VersionParseMode.DialectSpecific,
            4096,
            true,
            true,
            PrereleasePolicy.Default);
        UniversalVersion mavenVersion = new VersionService().Parse("1.0", mavenOptions);

        Assert.That(() => operations.FormatCanonical(mavenVersion), Throws.TypeOf<System.ArgumentException>());
        Assert.That(() => operations.FormatCanonical(null!), Throws.TypeOf<System.ArgumentNullException>());
        Assert.That(() => operations.TryParseStrict(null!), Throws.TypeOf<System.ArgumentNullException>());
        Assert.That(() => operations.TryParseLoose(null!), Throws.TypeOf<System.ArgumentNullException>());
        Assert.That(() => operations.TryCoerce(null!), Throws.TypeOf<System.ArgumentNullException>());
    }
}
