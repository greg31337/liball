using languagecodes;

namespace languagecodes.Tests;

public class LanguageTagValidationEngineTests
{
    [TestCase("en")]
    [TestCase("zh-cmn-Hans-CN-rozaj-1994-u-ca-gregory-t-en-x-private")]
    [TestCase("x-company-internal")]
    [TestCase("i-klingon")]
    [TestCase("sgn-US")]
    [TestCase("abcd")]
    [TestCase("abcdefgh-419")]
    public void AcceptsEveryRfc5646TagProductionAsWellFormed(string value)
    {
        LanguageValidationResult result = LanguageValidationEngine.ValidateTag(
            value,
            new LanguageValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsWellFormed, Is.True);
        });
    }

    [TestCase("en-Latn-US-fonipa-u-ca-gregory")]
    [TestCase("cmn")]
    [TestCase("i-default")]
    public void AcceptsTagsWhoseRegistryControlledComponentsAreAssigned(string value)
    {
        LanguageValidationResult result = LanguageValidationEngine.ValidateTag(
            value,
            new LanguageValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsWellFormed, Is.True);
            Assert.That(result.IsAssigned, Is.True);
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Issues, Is.Empty);
        });
    }

    [TestCase("abcd", "abcd")]
    [TestCase("en-zzz", "zzz")]
    [TestCase("en-Abcd", "Abcd")]
    [TestCase("en-AB", "AB")]
    [TestCase("en-abcde", "abcde")]
    public void RejectsUnassignedRegistryControlledSubtags(
        string value,
        string component)
    {
        LanguageValidationResult result = LanguageValidationEngine.ValidateTag(
            value,
            new LanguageValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsWellFormed, Is.True);
            Assert.That(result.IsAssigned, Is.False);
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues, Has.Length.EqualTo(1));
            Assert.That(result.Issues[0].Severity, Is.EqualTo(
                LanguageValidationSeverity.Error));
            Assert.That(result.Issues[0].Error, Is.EqualTo(
                LanguageValidationError.UnassignedCode));
            Assert.That(result.Issues[0].Component, Is.EqualTo(component));
        });
    }

    [TestCase("ar-aao")]
    [TestCase("de-CH-1901")]
    [TestCase("sl-rozaj-biske-1994")]
    [TestCase("sl-IT-rozaj-biske-1994")]
    public void AcceptsRegisteredExtlangAndVariantPrefixes(string value)
    {
        LanguageValidationResult result = LanguageValidationEngine.ValidateTag(
            value,
            new LanguageValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsAssigned, Is.True);
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Issues.All(issue =>
                issue.Error != LanguageValidationError.PrefixMismatch), Is.True);
        });
    }

    [TestCase("en-aao", "aao")]
    [TestCase("en-1994", "1994")]
    [TestCase("zh-cmn-yue", "yue")]
    public void RejectsAssignedSubtagsUsedOutsideTheirRegisteredPrefixes(
        string value,
        string component)
    {
        LanguageValidationResult result = LanguageValidationEngine.ValidateTag(
            value,
            new LanguageValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsWellFormed, Is.True);
            Assert.That(result.IsAssigned, Is.True);
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues, Has.Length.EqualTo(1));
            Assert.That(result.Issues[0].Error, Is.EqualTo(
                LanguageValidationError.PrefixMismatch));
            Assert.That(result.Issues[0].Component, Is.EqualTo(component));
        });
    }

    [TestCase("x-company-internal", new[] { "x-company-internal" })]
    [TestCase("en-x-company", new[] { "x" })]
    [TestCase("qaa-Qaaa-QM-x-local", new[] { "qaa", "Qaaa", "QM", "x" })]
    public void RejectsPrivateUseComponentsByDefault(
        string value,
        string[] components)
    {
        LanguageValidationResult result = LanguageValidationEngine.ValidateTag(
            value,
            new LanguageValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsWellFormed, Is.True);
            Assert.That(result.IsAssigned, Is.False);
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues.Select(issue => issue.Error), Is.EqualTo(
                components.Select(component => LanguageValidationError.PrivateUseNotAllowed)));
            Assert.That(result.Issues.Select(issue => issue.Component), Is.EqualTo(components));
        });
    }

    [TestCase("x-company-internal")]
    [TestCase("en-x-company")]
    [TestCase("qaa-Qaaa-QM-x-local")]
    public void AcceptsPrivateUseComponentsAsExplicitlyUnassigned(string value)
    {
        LanguageValidationOptions options = new LanguageValidationOptions
        {
            AllowPrivateUseCodes = true
        };
        LanguageValidationResult result = LanguageValidationEngine.ValidateTag(value, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsWellFormed, Is.True);
            Assert.That(result.IsAssigned, Is.False);
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Issues, Is.Not.Empty);
            Assert.That(result.Issues.All(issue =>
                issue.Severity == LanguageValidationSeverity.Warning), Is.True);
            Assert.That(result.Issues.All(issue =>
                issue.Error == LanguageValidationError.ReservedCode), Is.True);
        });
    }

    [TestCase("qaa", "qaa")]
    [TestCase("qtz", "qtz")]
    [TestCase("en-Qaaa", "Qaaa")]
    [TestCase("en-Qabx", "Qabx")]
    [TestCase("en-AA", "AA")]
    [TestCase("en-QM", "QM")]
    [TestCase("en-QZ", "QZ")]
    [TestCase("en-XA", "XA")]
    [TestCase("en-XZ", "XZ")]
    [TestCase("en-ZZ", "ZZ")]
    public void RecognizesEveryPrivateUseRegistryRangeBoundary(
        string value,
        string component)
    {
        LanguageValidationOptions options = new LanguageValidationOptions
        {
            AllowPrivateUseCodes = true
        };
        LanguageValidationResult result = LanguageValidationEngine.ValidateTag(value, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsWellFormed, Is.True);
            Assert.That(result.IsAssigned, Is.False);
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Issues, Has.Length.EqualTo(1));
            Assert.That(result.Issues[0].Severity, Is.EqualTo(
                LanguageValidationSeverity.Warning));
            Assert.That(result.Issues[0].Error, Is.EqualTo(
                LanguageValidationError.ReservedCode));
            Assert.That(result.Issues[0].Component, Is.EqualTo(component));
        });
    }

    [TestCase("EN-us", "en-US")]
    [TestCase("iw-BU", "he-MM")]
    [TestCase("i-klingon", "tlh")]
    [TestCase("zh-cmn", "cmn")]
    public void ReportsCanonicalIanaAlternativesWithoutRejectingThemByDefault(
        string value,
        string canonicalValue)
    {
        LanguageValidationResult result = LanguageValidationEngine.ValidateTag(
            value,
            new LanguageValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsWellFormed, Is.True);
            Assert.That(result.IsAssigned, Is.True);
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IsCanonical, Is.False);
            Assert.That(result.CanonicalValue, Is.EqualTo(canonicalValue));
            Assert.That(result.Issues, Has.Length.EqualTo(1));
            Assert.That(result.Issues[0].Severity, Is.EqualTo(
                LanguageValidationSeverity.Warning));
            Assert.That(result.Issues[0].Error, Is.EqualTo(
                LanguageValidationError.NonCanonical));
        });
    }

    [Test]
    public void StrictCanonicalPolicyRejectsNoncanonicalTags()
    {
        LanguageValidationResult result = LanguageValidationEngine.ValidateTag(
            "EN-us",
            new LanguageValidationOptions
            {
                RequireCanonical = true
            });

        Assert.Multiple(() =>
        {
            Assert.That(result.IsWellFormed, Is.True);
            Assert.That(result.IsAssigned, Is.True);
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.IsCanonical, Is.False);
            Assert.That(result.CanonicalValue, Is.EqualTo("en-US"));
            Assert.That(result.Issues, Has.Length.EqualTo(1));
            Assert.That(result.Issues[0].Severity, Is.EqualTo(
                LanguageValidationSeverity.Error));
            Assert.That(result.Issues[0].Error, Is.EqualTo(
                LanguageValidationError.NonCanonical));
        });
    }

    [Test]
    public void ReportsCanonicalTagsWithoutAReplacement()
    {
        LanguageValidationResult result = LanguageValidationEngine.ValidateTag(
            "en-Latn-US",
            new LanguageValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IsCanonical, Is.True);
            Assert.That(result.CanonicalValue, Is.Null);
            Assert.That(result.Issues, Is.Empty);
        });
    }

    [Test]
    public void DistinguishesNullAndEmptyInputs()
    {
        LanguageValidationResult nullResult = LanguageValidationEngine.ValidateTag(
            null,
            new LanguageValidationOptions());
        LanguageValidationResult emptyResult = LanguageValidationEngine.ValidateTag(
            string.Empty,
            new LanguageValidationOptions());

        Assert.Multiple(() =>
        {
            AssertMalformed(nullResult, LanguageValidationError.NullInput, null);
            AssertMalformed(emptyResult, LanguageValidationError.EmptyInput, string.Empty);
        });
    }

    [Test]
    public void ReportsIndependentLexicalFailuresInStableOrder()
    {
        const string value = "en_é !";
        LanguageValidationResult result = LanguageValidationEngine.ValidateTag(
            value,
            new LanguageValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsWellFormed, Is.False);
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues.Select(issue => issue.Error), Is.EqualTo(new[]
            {
                LanguageValidationError.InvalidSeparator,
                LanguageValidationError.NonAscii,
                LanguageValidationError.InvalidCharacter
            }));
            Assert.That(result.Issues.All(issue =>
                issue.Severity == LanguageValidationSeverity.Error), Is.True);
            Assert.That(result.Issues.All(issue => issue.Component == value), Is.True);
        });
    }

    [TestCase("en--US")]
    [TestCase("en-abcdefghi")]
    [TestCase("-en")]
    [TestCase("en-")]
    public void RejectsEmptyOrOversizedSubtagsAsInvalidLengths(string value)
    {
        LanguageValidationResult result = LanguageValidationEngine.ValidateTag(
            value,
            new LanguageValidationOptions());

        AssertMalformed(result, LanguageValidationError.InvalidLength, value);
    }

    [TestCase("e", "e", LanguageValidationError.InvalidLength)]
    [TestCase("1n-US", "1n", LanguageValidationError.InvalidTag)]
    [TestCase("en-12", "12", LanguageValidationError.InvalidTag)]
    [TestCase("en-US-Latn", "Latn", LanguageValidationError.InvalidTag)]
    [TestCase("en-cmn-yue-gan-abc", "abc", LanguageValidationError.InvalidTag)]
    public void RejectsInvalidShapesAndOutOfOrderComponents(
        string value,
        string component,
        LanguageValidationError error)
    {
        LanguageValidationResult result = LanguageValidationEngine.ValidateTag(
            value,
            new LanguageValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsWellFormed, Is.False);
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues, Has.Length.EqualTo(1));
            Assert.That(result.Issues[0].Error, Is.EqualTo(error));
            Assert.That(result.Issues[0].Component, Is.EqualTo(component));
        });
    }

    [TestCase("en-u", "u")]
    [TestCase("en-u-t-abc", "u")]
    [TestCase("en-u-x-private", "u")]
    public void ExtensionSingletonsRequirePayloads(string value, string component)
    {
        LanguageValidationResult result = LanguageValidationEngine.ValidateTag(
            value,
            new LanguageValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsWellFormed, Is.False);
            Assert.That(result.Issues, Has.Length.EqualTo(1));
            Assert.That(result.Issues[0].Error, Is.EqualTo(
                LanguageValidationError.InvalidTag));
            Assert.That(result.Issues[0].Component, Is.EqualTo(component));
            Assert.That(result.Issues[0].Message, Does.Contain("extension singleton"));
        });
    }

    [TestCase("x")]
    [TestCase("en-x")]
    public void PrivateUseSingletonsRequirePayloads(string value)
    {
        LanguageValidationResult result = LanguageValidationEngine.ValidateTag(
            value,
            new LanguageValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsWellFormed, Is.False);
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues, Has.Length.EqualTo(1));
            Assert.That(result.Issues[0].Error, Is.EqualTo(
                LanguageValidationError.InvalidTag));
            Assert.That(result.Issues[0].Message, Does.Contain("private-use singleton"));
        });
    }

    [Test]
    public void DuplicateVariantsAndExtensionSingletonsAreCaseInsensitiveValidityErrors()
    {
        const string value = "sl-rozaj-ROZAJ-u-ca-U-nu-latn";
        LanguageValidationResult result = LanguageValidationEngine.ValidateTag(
            value,
            new LanguageValidationOptions());
        LanguageTag parsed = LanguageTagParser.ParseTag(value);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsWellFormed, Is.True);
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues.Select(issue => issue.Error), Is.EqualTo(new[]
            {
                LanguageValidationError.DuplicateSubtag,
                LanguageValidationError.DuplicateSubtag
            }));
            Assert.That(result.Issues.Select(issue => issue.Component), Is.EqualTo(
                new[] { "ROZAJ", "U" }));
            Assert.That(parsed.Variants, Is.EqualTo(new[] { "rozaj", "rozaj" }));
            Assert.That(parsed.Extensions, Is.EqualTo(
                new[] { "u-ca", "u-nu-latn" }));
        });
    }

    [Test]
    public void RepeatedPrivateUsePayloadsArePermitted()
    {
        LanguageValidationOptions options = new LanguageValidationOptions
        {
            AllowPrivateUseCodes = true
        };
        LanguageValidationResult result = LanguageValidationEngine.ValidateTag(
            "en-x-example-example",
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsWellFormed, Is.True);
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Issues, Has.Length.EqualTo(1));
            Assert.That(result.Issues[0].Error, Is.EqualTo(
                LanguageValidationError.ReservedCode));
        });
    }

    [Test]
    public void ValidationIssuesAreIndependentSnapshots()
    {
        LanguageValidationResult first = LanguageValidationEngine.ValidateTag(
            "en-u",
            new LanguageValidationOptions());
        LanguageValidationResult second = LanguageValidationEngine.ValidateTag(
            "en-u",
            new LanguageValidationOptions());

        first.Issues[0].Message = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.Issues, Is.Not.SameAs(second.Issues));
            Assert.That(first.Issues[0], Is.Not.SameAs(second.Issues[0]));
            Assert.That(second.Issues[0].Message, Does.Contain("extension singleton"));
        });
    }

    private static void AssertMalformed(
        LanguageValidationResult result,
        LanguageValidationError error,
        string? component)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.IsWellFormed, Is.False);
            Assert.That(result.IsAssigned, Is.False);
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.IsCanonical, Is.False);
            Assert.That(result.Entry, Is.Null);
            Assert.That(result.CanonicalValue, Is.Null);
            Assert.That(result.Issues, Has.Length.EqualTo(1));
            Assert.That(result.Issues[0].Severity, Is.EqualTo(
                LanguageValidationSeverity.Error));
            Assert.That(result.Issues[0].Error, Is.EqualTo(error));
            Assert.That(result.Issues[0].Component, Is.EqualTo(component));
        });
    }
}
