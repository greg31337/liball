using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class PublicSemVerOperationsCompletenessTests
{
    [TestCase("0.0.0", "0.0.0")]
    [TestCase("1.0.0-alpha", "1.0.0-alpha")]
    [TestCase("1.0.0-alpha.beta", "1.0.0-alpha.beta")]
    [TestCase("1.0.0-alpha.beta.1", "1.0.0-alpha.beta.1")]
    [TestCase("1.0.0-0.3.7", "1.0.0-0.3.7")]
    [TestCase("1.0.0-x.7.z.92", "1.0.0-x.7.z.92")]
    [TestCase("1.0.0-x-y-z.--", "1.0.0-x-y-z.--")]
    [TestCase("1.0.0+20130313144700", "1.0.0+20130313144700")]
    [TestCase("1.0.0-beta+exp.sha.5114f85", "1.0.0-beta+exp.sha.5114f85")]
    [TestCase("1.0.0+21AF26D3----117B344092BD", "1.0.0+21AF26D3----117B344092BD")]
    [TestCase("1.0.0+001", "1.0.0+1")]
    public void StrictParsing_AcceptsSpecificationGrammarExamples(string text, string canonical)
    {
        ISemVerOperations operations = new SemVerOperations();

        VersionParseResult result = operations.TryParseStrict(text);
        UniversalVersion version = RequireVersion(result, text);

        Assert.That(result.sourceText, Is.EqualTo(text));
        Assert.That(result.dialect, Is.EqualTo(VersionDialect.SemVer));
        Assert.That(result.diagnostics, Is.Empty);
        Assert.That(version.originalText, Is.EqualTo(text));
        Assert.That(version.dialect, Is.EqualTo(VersionDialect.SemVer));
        Assert.That(version.numericComponents, Has.Length.EqualTo(3));
        Assert.That(operations.FormatCanonical(version), Is.EqualTo(canonical));
    }

    [Test]
    public void StrictParsing_ExposesCorePrereleaseAndBuildStructure()
    {
        ISemVerOperations operations = new SemVerOperations();
        UniversalVersion version = operations.ParseStrict("12.34.56-alpha.7.x+build.001.sha");

        Assert.That(version.numericComponents, Is.EqualTo(new string[] { "12", "34", "56" }));
        Assert.That(version.prereleaseIdentifiers, Has.Length.EqualTo(3));
        Assert.That(version.prereleaseIdentifiers[0].text, Is.EqualTo("alpha"));
        Assert.That(version.prereleaseIdentifiers[0].numeric, Is.False);
        Assert.That(version.prereleaseIdentifiers[1].text, Is.EqualTo("7"));
        Assert.That(version.prereleaseIdentifiers[1].numeric, Is.True);
        Assert.That(version.prereleaseIdentifiers[2].text, Is.EqualTo("x"));
        Assert.That(version.buildIdentifiers, Has.Length.EqualTo(3));
        Assert.That(version.buildIdentifiers[0].text, Is.EqualTo("build"));
        Assert.That(version.buildIdentifiers[1].text, Is.EqualTo("001"));
        Assert.That(version.buildIdentifiers[1].numeric, Is.True);
        Assert.That(version.buildIdentifiers[2].text, Is.EqualTo("sha"));
        Assert.That(version.missingComponentCount, Is.Zero);
        Assert.That(version.leadingVPrefix, Is.False);
    }

    [TestCase("01.2.3", SemVerDiagnosticCodes.NumericComponentLeadingZero, 0)]
    [TestCase("1.02.3", SemVerDiagnosticCodes.NumericComponentLeadingZero, 2)]
    [TestCase("1.2.03", SemVerDiagnosticCodes.NumericComponentLeadingZero, 4)]
    [TestCase("1.2.3-01", SemVerDiagnosticCodes.PrereleaseLeadingZero, 6)]
    [TestCase("1.2.3-alpha.01", SemVerDiagnosticCodes.PrereleaseLeadingZero, 12)]
    public void StrictParsing_RejectsForbiddenLeadingZeroes(string text, string code, int index)
    {
        ISemVerOperations operations = new SemVerOperations();

        AssertDiagnostic(operations.TryParseStrict(text), text, code, index);
        Assert.That(() => operations.ParseStrict(text), Throws.TypeOf<System.ArgumentException>());
    }

    [TestCase("", SemVerDiagnosticCodes.MissingMajor, 0)]
    [TestCase("1", SemVerDiagnosticCodes.MissingMinor, 1)]
    [TestCase("1.2", SemVerDiagnosticCodes.MissingPatch, 3)]
    [TestCase("1x.2.3", SemVerDiagnosticCodes.ExpectedDot, 1)]
    [TestCase("v1.2.3", SemVerDiagnosticCodes.IllegalCharacter, 0)]
    [TestCase("1.2.3.4", SemVerDiagnosticCodes.TrailingContent, 5)]
    [TestCase("1.2.3-", SemVerDiagnosticCodes.MalformedPrerelease, 6)]
    [TestCase("1.2.3+", SemVerDiagnosticCodes.MalformedBuild, 6)]
    [TestCase("1.2.3-alpha..1", SemVerDiagnosticCodes.EmptyPrereleaseIdentifier, 12)]
    [TestCase("1.2.3+build..1", SemVerDiagnosticCodes.EmptyBuildIdentifier, 12)]
    [TestCase("1.2.3-alpha_1", SemVerDiagnosticCodes.IllegalCharacter, 11)]
    [TestCase("1.2.3 build", SemVerDiagnosticCodes.TrailingContent, 5)]
    [TestCase("1.2.3\nextra", SemVerDiagnosticCodes.TrailingContent, 5)]
    public void StrictParsing_ReportsMalformedInputDiagnostics(string text, string code, int index)
    {
        ISemVerOperations operations = new SemVerOperations();

        AssertDiagnostic(operations.TryParseStrict(text), text, code, index);
    }

    [Test]
    public void Precedence_OrdersEveryPairInTheSpecificationChain()
    {
        ISemVerOperations operations = new SemVerOperations();
        string[] versions = new string[]
        {
            "1.0.0-alpha",
            "1.0.0-alpha.1",
            "1.0.0-alpha.beta",
            "1.0.0-beta",
            "1.0.0-beta.2",
            "1.0.0-beta.11",
            "1.0.0-rc.1",
            "1.0.0"
        };

        AssertAscendingPrecedence(operations, versions);
    }

    [Test]
    public void Precedence_OrdersCoreComponentsAndIdentifierKinds()
    {
        ISemVerOperations operations = new SemVerOperations();

        AssertAscendingPrecedence(
            operations,
            new string[] { "1.0.0", "2.0.0", "2.1.0", "2.1.1" });
        Assert.That(operations.ComparePrecedence("1.0.0-1", "1.0.0-alpha"), Is.LessThan(0));
        Assert.That(operations.ComparePrecedence("1.0.0-alpha", "1.0.0-alpha.1"), Is.LessThan(0));
        Assert.That(operations.ComparePrecedence("1.0.0-Alpha", "1.0.0-alpha"), Is.LessThan(0));
    }

    [Test]
    public void BuildMetadata_AffectsIdentityButNotPrecedenceOrNormalizedPackageIdentity()
    {
        ISemVerOperations operations = new SemVerOperations();
        string[] buildVariants = new string[]
        {
            "1.0.0",
            "1.0.0+001",
            "1.0.0+20130313144700",
            "1.0.0+exp.sha.5114f85",
            "1.0.0+EXP.sha.5114f85"
        };

        int leftIndex = 0;
        while (leftIndex < buildVariants.Length)
        {
            int rightIndex = leftIndex + 1;
            while (rightIndex < buildVariants.Length)
            {
                string left = buildVariants[leftIndex];
                string right = buildVariants[rightIndex];
                int identity = operations.CompareIdentity(left, right);

                Assert.That(operations.ComparePrecedence(left, right), Is.Zero, left + " / " + right);
                Assert.That(identity, Is.Not.Zero, left + " / " + right);
                Assert.That(operations.CompareIdentity(right, left), Is.EqualTo(0 - identity));
                Assert.That(operations.EqualsNormalizedPackageIdentity(left, right), Is.True);

                rightIndex = rightIndex + 1;
            }

            Assert.That(operations.CompareIdentity(buildVariants[leftIndex], buildVariants[leftIndex]), Is.Zero);
            leftIndex = leftIndex + 1;
        }

        Assert.That(
            operations.EqualsNormalizedPackageIdentity("1.0.0-alpha+one", "1.0.0-beta+one"),
            Is.False);
    }

    [TestCase("v1.2.3", "v1.2.3", true)]
    [TestCase("V1.2.3", "v1.2.3", true)]
    [TestCase("=1.2.3", "1.2.3", false)]
    [TestCase("= v1.2.3", "v1.2.3", true)]
    [TestCase(" 1.2.3 ", "1.2.3", false)]
    [TestCase(" \t= v01.002.0003-alpha.01+build.0007 \r\n", "v1.2.3-alpha.1+build.7", true)]
    public void LooseParsing_AcceptsCompatibilityBoundaries(string text, string canonical, bool leadingVPrefix)
    {
        ISemVerOperations operations = new SemVerOperations();

        VersionParseResult result = operations.TryParseLoose(text);
        UniversalVersion version = RequireVersion(result, text);

        Assert.That(result.sourceText, Is.EqualTo(text));
        Assert.That(result.dialect, Is.EqualTo(VersionDialect.NodeSemVer));
        Assert.That(version.leadingVPrefix, Is.EqualTo(leadingVPrefix));
        Assert.That(operations.FormatCanonical(version), Is.EqualTo(canonical));
    }

    [TestCase("1.2")]
    [TestCase("version 1.2.3")]
    [TestCase("1.2.3-alpha_1")]
    [TestCase("v1.2.3 trailing")]
    public void LooseParsing_DoesNotCoercePartialEmbeddedOrMalformedInput(string text)
    {
        ISemVerOperations operations = new SemVerOperations();
        VersionParseResult result = operations.TryParseLoose(text);

        Assert.That(result.success, Is.False);
        Assert.That(result.versionPresent, Is.False);
        Assert.That(result.diagnostics, Has.Length.EqualTo(1));
        Assert.That(() => operations.ParseLoose(text), Throws.TypeOf<System.ArgumentException>());
    }

    [Test]
    public void LooseParsing_MapsDiagnosticLocationsToTheOriginalInput()
    {
        ISemVerOperations operations = new SemVerOperations();
        string text = "  = v1.2";

        VersionParseResult result = operations.TryParseLoose(text);

        AssertDiagnostic(result, text, SemVerDiagnosticCodes.MissingPatch, text.Length);
        Assert.That(result.diagnostics[0].lineNumber, Is.EqualTo(1));
        Assert.That(result.diagnostics[0].columnNumber, Is.EqualTo(text.Length + 1));
    }

    [TestCase("42", "42.0.0", 2, false)]
    [TestCase("42.6", "42.6.0", 1, false)]
    [TestCase("42.6.7", "42.6.7", 0, false)]
    [TestCase("build 42.6.7.9", "42.6.7", 0, false)]
    [TestCase("build 42.6.x", "42.6.0", 1, false)]
    [TestCase("build 42.6.7-rc.1+meta", "42.6.7", 0, false)]
    [TestCase("first 1.2 then 9.9.9", "1.2.0", 1, false)]
    [TestCase("release v0001.0002.0003", "v1.2.3", 0, true)]
    [TestCase("-1.2.3", "1.2.3", 0, false)]
    public void Coercion_StopsAtTheFirstNumericTupleBoundary(
        string text,
        string canonical,
        int missingComponentCount,
        bool leadingVPrefix)
    {
        ISemVerOperations operations = new SemVerOperations();

        VersionParseResult result = operations.TryCoerce(text);
        UniversalVersion version = RequireVersion(result, text);

        Assert.That(result.dialect, Is.EqualTo(VersionDialect.NodeSemVer));
        Assert.That(version.missingComponentCount, Is.EqualTo(missingComponentCount));
        Assert.That(version.leadingVPrefix, Is.EqualTo(leadingVPrefix));
        Assert.That(version.prereleaseIdentifiers, Is.Empty);
        Assert.That(version.buildIdentifiers, Is.Empty);
        Assert.That(operations.FormatCanonical(version), Is.EqualTo(canonical));
    }

    [TestCase("")]
    [TestCase("no version here")]
    [TestCase("---")]
    public void Coercion_ReportsNoCoercibleVersionAtEndOfInput(string text)
    {
        ISemVerOperations operations = new SemVerOperations();

        VersionParseResult result = operations.TryCoerce(text);

        AssertDiagnostic(result, text, SemVerDiagnosticCodes.NoCoercibleVersion, text.Length);
        Assert.That(result.diagnostics[0].lineNumber, Is.EqualTo(1));
        Assert.That(result.diagnostics[0].columnNumber, Is.EqualTo(text.Length + 1));
        Assert.That(() => operations.Coerce(text), Throws.TypeOf<System.ArgumentException>());
    }

    [Test]
    public void CanonicalStrictText_RoundTripsThroughThePublicOperations()
    {
        ISemVerOperations operations = new SemVerOperations();
        string[] inputs = new string[]
        {
            "0.0.0",
            "1.2.3-alpha.9+build.0007",
            "1.0.0-x-y-z.--",
            "999999999999999999999999999999.0.0"
        };

        int index = 0;
        while (index < inputs.Length)
        {
            UniversalVersion original = operations.ParseStrict(inputs[index]);
            string canonical = operations.FormatCanonical(original);
            VersionParseResult reparsedResult = operations.TryParseStrict(canonical);
            UniversalVersion reparsed = RequireVersion(reparsedResult, canonical);

            Assert.That(operations.FormatCanonical(reparsed), Is.EqualTo(canonical));
            Assert.That(operations.ComparePrecedence(inputs[index], canonical), Is.Zero);
            Assert.That(operations.EqualsNormalizedPackageIdentity(inputs[index], canonical), Is.True);

            index = index + 1;
        }
    }

    [Test]
    public void CanonicalLooseAndCoercedText_RoundTripsThroughLooseParsing()
    {
        ISemVerOperations operations = new SemVerOperations();
        UniversalVersion loose = operations.ParseLoose("= v01.002.0003-alpha.01+build.0007");
        UniversalVersion coerced = operations.Coerce("release v0004.05");
        string[] canonicalTexts = new string[]
        {
            operations.FormatCanonical(loose),
            operations.FormatCanonical(coerced)
        };

        int index = 0;
        while (index < canonicalTexts.Length)
        {
            UniversalVersion reparsed = operations.ParseLoose(canonicalTexts[index]);
            Assert.That(operations.FormatCanonical(reparsed), Is.EqualTo(canonicalTexts[index]));
            index = index + 1;
        }

        Assert.That(canonicalTexts, Is.EqualTo(new string[]
        {
            "v1.2.3-alpha.1+build.7",
            "v4.5.0"
        }));
    }

    [Test]
    public void OversizedNumericComponentsAndIdentifiers_RemainUnboundedAndComparable()
    {
        ISemVerOperations operations = new SemVerOperations();
        string lower = new string('9', 400);
        string higher = "1" + new string('0', 400);
        string longBuild = new string('7', 600);
        string lowerText = lower + ".0.0-" + lower + "+" + longBuild;
        string higherCoreText = higher + ".0.0";
        string higherPrereleaseText = lower + ".0.0-" + higher;

        UniversalVersion version = operations.ParseStrict(lowerText);

        Assert.That(version.numericComponents[0], Is.EqualTo(lower));
        Assert.That(version.prereleaseIdentifiers[0].text, Is.EqualTo(lower));
        Assert.That(version.prereleaseIdentifiers[0].numeric, Is.True);
        Assert.That(version.buildIdentifiers[0].text, Is.EqualTo(longBuild));
        Assert.That(operations.FormatCanonical(version), Is.EqualTo(lowerText));
        Assert.That(operations.ComparePrecedence(lowerText, higherCoreText), Is.LessThan(0));
        Assert.That(
            operations.ComparePrecedence(lower + ".0.0-" + lower, higherPrereleaseText),
            Is.LessThan(0));
    }

    static UniversalVersion RequireVersion(VersionParseResult result, string sourceText)
    {
        Assert.That(result.success, Is.True, sourceText);
        Assert.That(result.versionPresent, Is.True, sourceText);
        Assert.That(result.version, Is.Not.Null, sourceText);
        return result.version!;
    }

    static void AssertDiagnostic(
        VersionParseResult result,
        string sourceText,
        string expectedCode,
        int expectedIndex)
    {
        Assert.That(result.success, Is.False, sourceText);
        Assert.That(result.versionPresent, Is.False, sourceText);
        Assert.That(result.version, Is.Null, sourceText);
        Assert.That(result.sourceText, Is.EqualTo(sourceText));
        Assert.That(result.diagnostics, Has.Length.EqualTo(1), sourceText);
        Assert.That(result.diagnostics[0].code, Is.EqualTo(expectedCode), sourceText);
        Assert.That(result.diagnostics[0].sourceText, Is.EqualTo(sourceText));
        Assert.That(result.diagnostics[0].index, Is.EqualTo(expectedIndex), sourceText);
    }

    static void AssertAscendingPrecedence(ISemVerOperations operations, string[] versions)
    {
        int leftIndex = 0;
        while (leftIndex < versions.Length)
        {
            Assert.That(operations.ComparePrecedence(versions[leftIndex], versions[leftIndex]), Is.Zero);

            int rightIndex = leftIndex + 1;
            while (rightIndex < versions.Length)
            {
                Assert.That(
                    operations.ComparePrecedence(versions[leftIndex], versions[rightIndex]),
                    Is.LessThan(0),
                    versions[leftIndex] + " / " + versions[rightIndex]);
                Assert.That(
                    operations.ComparePrecedence(versions[rightIndex], versions[leftIndex]),
                    Is.GreaterThan(0),
                    versions[rightIndex] + " / " + versions[leftIndex]);

                rightIndex = rightIndex + 1;
            }

            leftIndex = leftIndex + 1;
        }
    }
}
