using languagecodes;

namespace languagecodes.Tests;

public class LanguageValidationDataTests
{
    [Test]
    public void ValidationOptionsStoreAssignmentAndAcceptancePolicy()
    {
        LanguageValidationOptions options = new LanguageValidationOptions
        {
            RequiredSet = Iso639Set.Set3,
            RequiredForm = LanguageCodeForm.Set3,
            AllowedStatuses = new[] { LanguageCodeStatus.Active, LanguageCodeStatus.Retired },
            AllowedScopes = new[] { LanguageScope.Individual, LanguageScope.Macrolanguage },
            AllowedTypes = new[] { LanguageType.Living, LanguageType.Historical },
            AllowSpecialCodes = true,
            AllowGroupCodes = true,
            AllowMacrolanguages = true,
            AllowPrivateUseCodes = true,
            RequireCanonical = true
        };

        Assert.Multiple(() =>
        {
            Assert.That(options.RequiredSet, Is.EqualTo(Iso639Set.Set3));
            Assert.That(options.RequiredForm, Is.EqualTo(LanguageCodeForm.Set3));
            Assert.That(
                options.AllowedStatuses,
                Is.EqualTo(new[] { LanguageCodeStatus.Active, LanguageCodeStatus.Retired }));
            Assert.That(
                options.AllowedScopes,
                Is.EqualTo(new[] { LanguageScope.Individual, LanguageScope.Macrolanguage }));
            Assert.That(
                options.AllowedTypes,
                Is.EqualTo(new[] { LanguageType.Living, LanguageType.Historical }));
            Assert.That(options.AllowSpecialCodes, Is.True);
            Assert.That(options.AllowGroupCodes, Is.True);
            Assert.That(options.AllowMacrolanguages, Is.True);
            Assert.That(options.AllowPrivateUseCodes, Is.True);
            Assert.That(options.RequireCanonical, Is.True);
        });
    }

    [Test]
    public void ValidationResultRepresentsAValidCanonicalAssignment()
    {
        LanguageCodeEntry english = new LanguageCodeEntry
        {
            Set3Code = "eng",
            InSet3 = true,
            Status = LanguageCodeStatus.Active,
            ReferenceName = "English"
        };
        LanguageValidationResult result = new LanguageValidationResult
        {
            IsWellFormed = true,
            IsAssigned = true,
            IsValid = true,
            IsCanonical = true,
            Entry = english,
            CanonicalValue = "eng"
        };

        Assert.Multiple(() =>
        {
            Assert.That(result.IsWellFormed, Is.True);
            Assert.That(result.IsAssigned, Is.True);
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IsCanonical, Is.True);
            Assert.That(result.Entry, Is.SameAs(english));
            Assert.That(result.CanonicalValue, Is.EqualTo("eng"));
            Assert.That(result.Issues, Is.Empty);
        });
    }

    [Test]
    public void ValidationResultRepresentsAValidNoncanonicalWarning()
    {
        LanguageValidationIssue issue = new LanguageValidationIssue
        {
            Severity = LanguageValidationSeverity.Warning,
            Error = LanguageValidationError.NonCanonical,
            Component = "fre",
            Message = "The terminology code fra is preferred."
        };
        LanguageValidationResult result = new LanguageValidationResult
        {
            IsWellFormed = true,
            IsAssigned = true,
            IsValid = true,
            IsCanonical = false,
            CanonicalValue = "fra",
            Issues = new[] { issue }
        };

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IsCanonical, Is.False);
            Assert.That(result.CanonicalValue, Is.EqualTo("fra"));
            Assert.That(result.Issues[0].Severity, Is.EqualTo(LanguageValidationSeverity.Warning));
            Assert.That(result.Issues[0].Error, Is.EqualTo(LanguageValidationError.NonCanonical));
            Assert.That(result.Issues[0].Component, Is.EqualTo("fre"));
            Assert.That(result.Issues[0].Message, Is.EqualTo("The terminology code fra is preferred."));
        });
    }

    [Test]
    public void ValidationResultDistinguishesMalformedFromUnassignedInput()
    {
        LanguageValidationResult malformed = new LanguageValidationResult
        {
            IsWellFormed = false,
            IsAssigned = false,
            IsValid = false,
            IsCanonical = false,
            Issues = new[]
            {
                new LanguageValidationIssue
                {
                    Severity = LanguageValidationSeverity.Error,
                    Error = LanguageValidationError.InvalidCharacter,
                    Component = "e1g",
                    Message = "Language codes contain ASCII letters only."
                }
            }
        };
        LanguageValidationResult unassigned = new LanguageValidationResult
        {
            IsWellFormed = true,
            IsAssigned = false,
            IsValid = false,
            IsCanonical = false,
            Issues = new[]
            {
                new LanguageValidationIssue
                {
                    Severity = LanguageValidationSeverity.Error,
                    Error = LanguageValidationError.UnassignedCode,
                    Component = "zzz",
                    Message = "The code is not assigned."
                }
            }
        };

        Assert.Multiple(() =>
        {
            Assert.That(malformed.IsWellFormed, Is.False);
            Assert.That(malformed.Issues[0].Error, Is.EqualTo(LanguageValidationError.InvalidCharacter));
            Assert.That(unassigned.IsWellFormed, Is.True);
            Assert.That(unassigned.IsAssigned, Is.False);
            Assert.That(unassigned.Issues[0].Error, Is.EqualTo(LanguageValidationError.UnassignedCode));
        });
    }

    [Test]
    public void ValidationOptionsAndResultsDoNotShareArrays()
    {
        LanguageValidationOptions firstOptions = new LanguageValidationOptions();
        LanguageValidationOptions secondOptions = new LanguageValidationOptions();
        LanguageValidationResult firstResult = new LanguageValidationResult();
        LanguageValidationResult secondResult = new LanguageValidationResult();

        Assert.Multiple(() =>
        {
            Assert.That(firstOptions.AllowedStatuses, Is.Not.SameAs(secondOptions.AllowedStatuses));
            Assert.That(firstOptions.AllowedScopes, Is.Not.SameAs(secondOptions.AllowedScopes));
            Assert.That(firstOptions.AllowedTypes, Is.Not.SameAs(secondOptions.AllowedTypes));
            Assert.That(firstResult.Issues, Is.Not.SameAs(secondResult.Issues));
        });
    }

    [Test]
    public void ValidationOperationRowKeepsOptionsAndResultTogether()
    {
        LanguageValidationOptions options = new LanguageValidationOptions
        {
            RequiredSet = Iso639Set.Set1
        };
        LanguageValidationResult result = new LanguageValidationResult
        {
            IsWellFormed = true,
            IsAssigned = true,
            IsValid = true
        };
        ValidationOperationRow operation = new ValidationOperationRow(options, result);

        Assert.Multiple(() =>
        {
            Assert.That(operation.Options, Is.SameAs(options));
            Assert.That(operation.Result, Is.SameAs(result));
        });
    }
}
