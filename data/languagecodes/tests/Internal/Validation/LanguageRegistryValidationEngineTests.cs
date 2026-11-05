using languagecodes;

namespace languagecodes.Tests;

public class LanguageRegistryValidationEngineTests
{
    [TestCase("en", "eng")]
    [TestCase("eng", "eng")]
    [TestCase("fre", "fra")]
    [TestCase("aaa", "aaa")]
    [TestCase("gmw", null)]
    public void ActiveCodesAndAliasesResolveAsAssigned(
        string code,
        string? expectedSet3Code)
    {
        LanguageValidationResult result =
            LanguageValidationEngine.ValidateCodeRegistry(code);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsWellFormed, Is.True);
            Assert.That(result.IsAssigned, Is.True);
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IsCanonical, Is.True);
            Assert.That(result.Entry, Is.Not.Null);
            Assert.That(result.Entry?.Status, Is.EqualTo(LanguageCodeStatus.Active));
            Assert.That(result.Entry?.Set3Code, Is.EqualTo(expectedSet3Code));
            Assert.That(result.CanonicalValue, Is.Null);
            Assert.That(result.Issues, Is.Empty);
        });
    }

    [TestCase("eng", LanguageScope.Individual, LanguageType.Living)]
    [TestCase("ara", LanguageScope.Macrolanguage, LanguageType.Living)]
    [TestCase("afa", LanguageScope.Collection, LanguageType.Unknown)]
    [TestCase("mis", LanguageScope.Special, LanguageType.Special)]
    public void AssignedEntriesPreserveTheirEntityClassification(
        string code,
        LanguageScope expectedScope,
        LanguageType expectedType)
    {
        LanguageValidationResult result =
            LanguageValidationEngine.ValidateCodeRegistry(code);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsAssigned, Is.True);
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Entry?.Scope, Is.EqualTo(expectedScope));
            Assert.That(result.Entry?.LanguageType, Is.EqualTo(expectedType));
            Assert.That(result.Entry?.Status, Is.EqualTo(LanguageCodeStatus.Active));
        });
    }

    [TestCase("iw", "heb")]
    [TestCase("in", "ind")]
    [TestCase("asd", "snz")]
    public void DeprecatedRegistryCodesRemainRecognizedWithAWarning(
        string code,
        string expectedSet3Code)
    {
        LanguageValidationResult result =
            LanguageValidationEngine.ValidateCodeRegistry(code);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsWellFormed, Is.True);
            Assert.That(result.IsAssigned, Is.True);
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IsCanonical, Is.True);
            Assert.That(result.Entry?.Set3Code, Is.EqualTo(expectedSet3Code));
            Assert.That(result.Entry?.Status, Is.EqualTo(LanguageCodeStatus.Active));
            Assert.That(result.CanonicalValue, Is.Null);
            Assert.That(result.Issues, Has.Length.EqualTo(1));
            Assert.That(result.Issues[0].Severity, Is.EqualTo(
                LanguageValidationSeverity.Warning));
            Assert.That(result.Issues[0].Error, Is.EqualTo(
                LanguageValidationError.DeprecatedCode));
            Assert.That(result.Issues[0].Component, Is.EqualTo(code));
            Assert.That(result.Issues[0].Message, Is.EqualTo(
                "The code is deprecated by its authoritative registry."));
        });
    }

    [TestCase("fri", "fry")]
    [TestCase("aay", null)]
    [TestCase("agp", null)]
    public void RetiredCodesRemainRecognizedWithTheirHistoricalEntry(
        string code,
        string? singularReplacement)
    {
        LanguageValidationResult result =
            LanguageValidationEngine.ValidateCodeRegistry(code);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsWellFormed, Is.True);
            Assert.That(result.IsAssigned, Is.True);
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IsCanonical, Is.True);
            Assert.That(result.Entry?.Status, Is.EqualTo(LanguageCodeStatus.Retired));
            Assert.That(result.Entry?.Retirement?.RetiredCode, Is.EqualTo(code));
            if (singularReplacement == null)
            {
                Assert.That(
                    result.Entry?.Retirement?.ReplacementCodes.Length,
                    Is.Not.EqualTo(1));
            }
            else
            {
                Assert.That(result.Entry?.Retirement?.ReplacementCodes, Has.Length.EqualTo(1));
                Assert.That(result.Entry?.Retirement?.ReplacementCodes[0], Is.EqualTo(
                    singularReplacement));
            }

            Assert.That(result.Issues, Has.Length.EqualTo(1));
            Assert.That(result.Issues[0].Severity, Is.EqualTo(
                LanguageValidationSeverity.Warning));
            Assert.That(result.Issues[0].Error, Is.EqualTo(
                LanguageValidationError.RetiredCode));
            Assert.That(result.Issues[0].Message, Is.EqualTo(
                "The code is retired but remains an authoritative historical identifier."));
        });
    }

    [TestCase("qaa")]
    [TestCase("qaz")]
    [TestCase("qtz")]
    public void LocalUseRangeIsReservedRatherThanAssigned(string code)
    {
        LanguageValidationResult result =
            LanguageValidationEngine.ValidateCodeRegistry(code);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsWellFormed, Is.True);
            Assert.That(result.IsAssigned, Is.False);
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.IsCanonical, Is.False);
            Assert.That(result.Entry, Is.Null);
            Assert.That(result.CanonicalValue, Is.Null);
            Assert.That(result.Issues, Has.Length.EqualTo(1));
            Assert.That(result.Issues[0].Severity, Is.EqualTo(
                LanguageValidationSeverity.Error));
            Assert.That(result.Issues[0].Error, Is.EqualTo(
                LanguageValidationError.ReservedCode));
            Assert.That(result.Issues[0].Component, Is.EqualTo(code));
        });
    }

    [TestCase("zz")]
    [TestCase("zzz")]
    [TestCase("qzz")]
    public void WellFormedUnknownValuesAreUnassigned(string code)
    {
        LanguageValidationResult result =
            LanguageValidationEngine.ValidateCodeRegistry(code);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsWellFormed, Is.True);
            Assert.That(result.IsAssigned, Is.False);
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.IsCanonical, Is.False);
            Assert.That(result.Entry, Is.Null);
            Assert.That(result.Issues, Has.Length.EqualTo(1));
            Assert.That(result.Issues[0].Error, Is.EqualTo(
                LanguageValidationError.UnassignedCode));
            Assert.That(result.Issues[0].Message, Is.EqualTo(
                "The well-formed code is not assigned."));
        });
    }

    [Test]
    public void RegistryDiagnosticsPrecedeCanonicalCasingWarnings()
    {
        LanguageValidationResult deprecated =
            LanguageValidationEngine.ValidateCodeRegistry("IW");
        LanguageValidationResult unassigned =
            LanguageValidationEngine.ValidateCodeRegistry("ZZZ");

        Assert.Multiple(() =>
        {
            Assert.That(deprecated.IsAssigned, Is.True);
            Assert.That(deprecated.IsValid, Is.True);
            Assert.That(deprecated.IsCanonical, Is.False);
            Assert.That(deprecated.CanonicalValue, Is.EqualTo("iw"));
            Assert.That(
                deprecated.Issues.Select(issue => issue.Error),
                Is.EqualTo(new[]
                {
                    LanguageValidationError.DeprecatedCode,
                    LanguageValidationError.NonCanonical
                }));
            Assert.That(unassigned.IsAssigned, Is.False);
            Assert.That(unassigned.IsValid, Is.False);
            Assert.That(unassigned.CanonicalValue, Is.EqualTo("zzz"));
            Assert.That(
                unassigned.Issues.Select(issue => issue.Error),
                Is.EqualTo(new[]
                {
                    LanguageValidationError.UnassignedCode,
                    LanguageValidationError.NonCanonical
                }));
        });
    }

    [Test]
    public void MalformedInputStopsBeforeRegistryResolution()
    {
        LanguageValidationResult result =
            LanguageValidationEngine.ValidateCodeRegistry("e1g");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsWellFormed, Is.False);
            Assert.That(result.IsAssigned, Is.False);
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Entry, Is.Null);
            Assert.That(result.Issues.Select(issue => issue.Error), Is.EqualTo(
                new[] { LanguageValidationError.InvalidCharacter }));
            Assert.That(result.Issues.Any(issue =>
                issue.Error == LanguageValidationError.UnassignedCode), Is.False);
        });
    }

    [Test]
    public void RegistryResultsAreRecursiveDefensiveSnapshots()
    {
        LanguageValidationResult first =
            LanguageValidationEngine.ValidateCodeRegistry("fri");
        LanguageValidationResult second =
            LanguageValidationEngine.ValidateCodeRegistry("fri");
        string originalName = second.Entry!.Names[0].Text!;

        first.Entry!.Set3Code = "changed";
        first.Entry.Names[0].Text = "changed";
        first.Entry.Retirement!.ReplacementCodes[0] = "changed";
        first.Issues[0].Message = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(second.Entry?.Set3Code, Is.EqualTo("fri"));
            Assert.That(second.Entry?.Names[0].Text, Is.EqualTo(originalName));
            Assert.That(second.Entry?.Retirement?.ReplacementCodes[0], Is.EqualTo("fry"));
            Assert.That(second.Issues[0].Message, Is.EqualTo(
                "The code is retired but remains an authoritative historical identifier."));
            Assert.That(LanguageValidationEngine.ValidateCodeRegistry("fri")
                .Entry?.Retirement?.ReplacementCodes[0], Is.EqualTo("fry"));
        });
    }
}
