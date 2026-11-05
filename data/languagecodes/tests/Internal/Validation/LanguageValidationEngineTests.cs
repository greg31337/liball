using languagecodes;

namespace languagecodes.Tests;

public class LanguageValidationEngineTests
{
    [Test]
    public void NullAndEmptyInputsHaveDistinctDiagnostics()
    {
        LanguageValidationResult nullResult =
            LanguageValidationEngine.ValidateCodeLexically(null);
        LanguageValidationResult emptyResult =
            LanguageValidationEngine.ValidateCodeLexically(string.Empty);

        Assert.Multiple(() =>
        {
            AssertMalformedResult(nullResult, LanguageValidationError.NullInput, null);
            Assert.That(nullResult.Issues[0].Message, Is.EqualTo(
                "A language code is required."));
            AssertMalformedResult(emptyResult, LanguageValidationError.EmptyInput, string.Empty);
            Assert.That(emptyResult.Issues[0].Message, Is.EqualTo(
                "A language code cannot be empty."));
        });
    }

    [TestCase("en")]
    [TestCase("eng")]
    [TestCase("zz")]
    [TestCase("zzz")]
    public void LowercaseAsciiCodeShapesAreLexicallyValidRegardlessOfAssignment(string code)
    {
        LanguageValidationResult result =
            LanguageValidationEngine.ValidateCodeLexically(code);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsWellFormed, Is.True);
            Assert.That(result.IsAssigned, Is.False);
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IsCanonical, Is.True);
            Assert.That(result.Entry, Is.Null);
            Assert.That(result.CanonicalValue, Is.Null);
            Assert.That(result.Issues, Is.Empty);
        });
    }

    [TestCase("EN", "en")]
    [TestCase("eNg", "eng")]
    [TestCase("ZZZ", "zzz")]
    public void UppercaseIsWellFormedWithALowercaseCanonicalSuggestion(
        string code,
        string expectedCanonical)
    {
        LanguageValidationResult result =
            LanguageValidationEngine.ValidateCodeLexically(code);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsWellFormed, Is.True);
            Assert.That(result.IsAssigned, Is.False);
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IsCanonical, Is.False);
            Assert.That(result.CanonicalValue, Is.EqualTo(expectedCanonical));
            Assert.That(result.Issues, Has.Length.EqualTo(1));
            Assert.That(result.Issues[0].Severity, Is.EqualTo(
                LanguageValidationSeverity.Warning));
            Assert.That(result.Issues[0].Error, Is.EqualTo(
                LanguageValidationError.NonCanonical));
            Assert.That(result.Issues[0].Component, Is.EqualTo(code));
            Assert.That(result.Issues[0].Message, Is.EqualTo(
                "Language codes use lowercase ASCII letters."));
        });
    }

    [TestCase("e")]
    [TestCase("engl")]
    [TestCase("abcde")]
    public void WrongLengthIsReportedWithoutConflatingAssignment(string code)
    {
        LanguageValidationResult result =
            LanguageValidationEngine.ValidateCodeLexically(code);

        Assert.Multiple(() =>
        {
            AssertMalformedResult(result, LanguageValidationError.InvalidLength, code);
            Assert.That(result.Issues[0].Message, Is.EqualTo(
                "Language codes must contain two or three letters."));
        });
    }

    [TestCase("\u00e9n")]
    [TestCase("e\u00f1g")]
    [TestCase("\ud83d\ude00")]
    public void NonAsciiInputHasItsOwnRepertoireDiagnostic(string code)
    {
        LanguageValidationResult result =
            LanguageValidationEngine.ValidateCodeLexically(code);

        Assert.Multiple(() =>
        {
            AssertMalformedResult(result, LanguageValidationError.NonAscii, code);
            Assert.That(result.Issues[0].Message, Is.EqualTo(
                "Language codes contain ASCII letters only."));
        });
    }

    [TestCase("en-")]
    [TestCase("e_n")]
    public void SeparatorsAreDistinguishedFromOtherInvalidCharacters(string code)
    {
        LanguageValidationResult result =
            LanguageValidationEngine.ValidateCodeLexically(code);

        Assert.Multiple(() =>
        {
            AssertMalformedResult(result, LanguageValidationError.InvalidSeparator, code);
            Assert.That(result.Issues[0].Message, Is.EqualTo(
                "Language codes cannot contain separators."));
        });
    }

    [TestCase("e1")]
    [TestCase("e.g")]
    [TestCase("e g")]
    public void DigitsWhitespaceAndPunctuationUseInvalidCharacter(string code)
    {
        LanguageValidationResult result =
            LanguageValidationEngine.ValidateCodeLexically(code);

        Assert.Multiple(() =>
        {
            AssertMalformedResult(result, LanguageValidationError.InvalidCharacter, code);
            Assert.That(result.Issues[0].Message, Is.EqualTo(
                "Language codes cannot contain digits, whitespace, or punctuation."));
        });
    }

    [Test]
    public void IndependentLexicalProblemsAreReturnedInStableEnumOrder()
    {
        string code = "\u00e91-x";

        LanguageValidationResult result =
            LanguageValidationEngine.ValidateCodeLexically(code);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsWellFormed, Is.False);
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.IsCanonical, Is.False);
            Assert.That(result.CanonicalValue, Is.Null);
            Assert.That(
                result.Issues.Select(issue => issue.Error),
                Is.EqualTo(new[]
                {
                    LanguageValidationError.InvalidLength,
                    LanguageValidationError.NonAscii,
                    LanguageValidationError.InvalidCharacter,
                    LanguageValidationError.InvalidSeparator
                }));
            Assert.That(result.Issues.All(issue =>
                issue.Severity == LanguageValidationSeverity.Error), Is.True);
            Assert.That(result.Issues.All(issue => issue.Component == code), Is.True);
        });
    }

    [Test]
    public void MalformedUppercaseInputDoesNotReceiveACanonicalSuggestion()
    {
        LanguageValidationResult result =
            LanguageValidationEngine.ValidateCodeLexically(" EN ");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsWellFormed, Is.False);
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.IsCanonical, Is.False);
            Assert.That(result.CanonicalValue, Is.Null);
            Assert.That(result.Issues.Select(issue => issue.Error), Is.EqualTo(new[]
            {
                LanguageValidationError.InvalidLength,
                LanguageValidationError.InvalidCharacter
            }));
            Assert.That(result.Issues.Any(issue =>
                issue.Error == LanguageValidationError.NonCanonical), Is.False);
        });
    }

    [Test]
    public void ValidationResultsReturnIndependentIssueSnapshots()
    {
        LanguageValidationResult first =
            LanguageValidationEngine.ValidateCodeLexically("EN");
        LanguageValidationResult second =
            LanguageValidationEngine.ValidateCodeLexically("EN");

        first.Issues[0].Component = "changed";
        first.Issues[0].Message = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.Issues, Is.Not.SameAs(second.Issues));
            Assert.That(first.Issues[0], Is.Not.SameAs(second.Issues[0]));
            Assert.That(second.Issues[0].Component, Is.EqualTo("EN"));
            Assert.That(second.Issues[0].Message, Is.EqualTo(
                "Language codes use lowercase ASCII letters."));
            Assert.That(
                LanguageValidationEngine.ValidateCodeLexically("EN").CanonicalValue,
                Is.EqualTo("en"));
        });
    }

    private static void AssertMalformedResult(
        LanguageValidationResult result,
        LanguageValidationError expectedError,
        string? expectedComponent)
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
            Assert.That(result.Issues[0].Error, Is.EqualTo(expectedError));
            Assert.That(result.Issues[0].Component, Is.EqualTo(expectedComponent));
        });
    }
}
