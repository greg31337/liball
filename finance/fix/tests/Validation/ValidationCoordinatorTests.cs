using NUnit.Framework;

namespace fix.Tests;

public class ValidationCoordinatorTests
{
    [TestCase(ValidationLayer.Framing)]
    [TestCase(ValidationLayer.Envelope)]
    [TestCase(ValidationLayer.SessionHeader)]
    [TestCase(ValidationLayer.DictionaryGrammar)]
    [TestCase(ValidationLayer.Datatype)]
    [TestCase(ValidationLayer.CodeSet)]
    [TestCase(ValidationLayer.Group)]
    [TestCase(ValidationLayer.ConditionalRule)]
    [TestCase(ValidationLayer.ApplicationSemantic)]
    public void EveryValidationDomainCanBeSelectedIndependently(ValidationLayer selectedLayer)
    {
        List<ValidationIssue> candidates = [];
        ValidationLayer[] layers =
        [
            ValidationLayer.Framing,
            ValidationLayer.Envelope,
            ValidationLayer.SessionHeader,
            ValidationLayer.DictionaryGrammar,
            ValidationLayer.Datatype,
            ValidationLayer.CodeSet,
            ValidationLayer.Group,
            ValidationLayer.ConditionalRule,
            ValidationLayer.ApplicationSemantic
        ];
        for (int index = 0; index < layers.Length; index++)
        {
            candidates.Add(Issue(layers[index], ValidationSeverity.Warning, layers[index].ToString()));
        }
        ValidationOptions options = new ValidationOptions { Layers = selectedLayer };

        ValidationResult result = ValidationCoordinator.CoordinateValidation(candidates, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Issues, Has.Count.EqualTo(1));
            Assert.That(result.Issues[0].Layer, Is.EqualTo(selectedLayer));
            Assert.That(result.Issues[0].Code, Is.EqualTo(selectedLayer.ToString()));
        });
    }

    [Test]
    public void CoordinatorFiltersOptionsAndOrdersIssuesByLayerThenEmissionOrder()
    {
        ValidationIssue datatypeFirst = Issue(ValidationLayer.Datatype, ValidationSeverity.Error, "datatype-first");
        ValidationIssue conditional = Issue(ValidationLayer.ConditionalRule, ValidationSeverity.Warning, "conditional");
        ValidationIssue envelope = Issue(ValidationLayer.Envelope, ValidationSeverity.Error, "envelope");
        ValidationIssue group = Issue(ValidationLayer.Group, ValidationSeverity.Fatal, "group");
        ValidationIssue datatypeSecond = Issue(ValidationLayer.Datatype, ValidationSeverity.Information, "datatype-second");
        ValidationIssue codeSet = Issue(ValidationLayer.CodeSet, ValidationSeverity.Error, "code-set");
        List<ValidationIssue> candidates = [datatypeFirst, conditional, envelope, group, datatypeSecond, codeSet];
        ValidationOptions options = new ValidationOptions
        {
            Layers = ValidationLayer.Envelope | ValidationLayer.Datatype | ValidationLayer.CodeSet | ValidationLayer.Group | ValidationLayer.ConditionalRule,
            CheckGroups = false
        };

        ValidationResult result = ValidationCoordinator.CoordinateValidation(candidates, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.IssueLimitReached, Is.False);
            Assert.That(result.Issues.ConvertAll(issue => issue.Code), Is.EqualTo(new[]
            {
                "envelope",
                "datatype-first",
                "datatype-second",
                "code-set",
                "conditional"
            }));
            Assert.That(result.Issues[1], Is.SameAs(datatypeFirst));
            Assert.That(result.Issues[2], Is.SameAs(datatypeSecond));
            Assert.That(candidates.ConvertAll(issue => issue.Code), Is.EqualTo(new[]
            {
                "datatype-first",
                "conditional",
                "envelope",
                "group",
                "datatype-second",
                "code-set"
            }));
        });
    }

    [Test]
    public void CoordinatorRetainsTheConfiguredLimitButEvaluatesAllSelectedSeverities()
    {
        ValidationIssue warning = Issue(ValidationLayer.Envelope, ValidationSeverity.Warning, "warning");
        ValidationIssue information = Issue(ValidationLayer.DictionaryGrammar, ValidationSeverity.Information, "information");
        ValidationIssue fatalBeyondLimit = Issue(ValidationLayer.Datatype, ValidationSeverity.Fatal, "fatal");
        ValidationOptions options = new ValidationOptions { MaxIssues = 2 };

        ValidationResult result = ValidationCoordinator.CoordinateValidation([fatalBeyondLimit, information, warning], options);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.IssueLimitReached, Is.True);
            Assert.That(result.Issues.ConvertAll(issue => issue.Code), Is.EqualTo(new[] { "warning", "information" }));
            Assert.That(options.MaxIssues, Is.EqualTo(2));
        });
    }

    [Test]
    public void DisabledErrorsDoNotInvalidateAndWarningsDoNotFailValidation()
    {
        ValidationOptions options = new ValidationOptions
        {
            Layers = ValidationLayer.Envelope | ValidationLayer.Group | ValidationLayer.ConditionalRule,
            CheckGroups = false,
            CheckConditionalRules = false
        };
        List<ValidationIssue> candidates =
        [
            Issue(ValidationLayer.Envelope, ValidationSeverity.Warning, "envelope-warning"),
            Issue(ValidationLayer.Datatype, ValidationSeverity.Fatal, "disabled-layer"),
            Issue(ValidationLayer.Group, ValidationSeverity.Error, "disabled-group"),
            Issue(ValidationLayer.ConditionalRule, ValidationSeverity.Error, "disabled-rule")
        ];

        ValidationResult result = ValidationCoordinator.CoordinateValidation(candidates, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IssueLimitReached, Is.False);
            Assert.That(result.Issues.ConvertAll(issue => issue.Code), Is.EqualTo(new[] { "envelope-warning" }));
            Assert.That(ValidationCoordinator.IsLayerEnabled(options, ValidationLayer.Envelope), Is.True);
            Assert.That(ValidationCoordinator.IsLayerEnabled(options, ValidationLayer.Datatype), Is.False);
            Assert.That(ValidationCoordinator.IsLayerEnabled(options, ValidationLayer.Group), Is.False);
            Assert.That(ValidationCoordinator.IsLayerEnabled(options, ValidationLayer.ConditionalRule), Is.False);
            Assert.That(ValidationCoordinator.IsLayerEnabled(options, ValidationLayer.None), Is.False);
        });
    }

    [TestCase(-1)]
    [TestCase(0)]
    public void NonPositiveLimitsRetainNoIssuesAndStillCalculateValidity(int configuredLimit)
    {
        ValidationOptions options = new ValidationOptions { Layers = ValidationLayer.Envelope, MaxIssues = configuredLimit };

        ValidationResult result = ValidationCoordinator.CoordinateValidation(
            [Issue(ValidationLayer.Envelope, ValidationSeverity.Error, "not-retained")],
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.IssueLimitReached, Is.True);
            Assert.That(result.Issues, Is.Empty);
            Assert.That(ValidationCoordinator.ResolveIssueLimit(options), Is.Zero);
        });
    }

    [Test]
    public void IssueLimitCannotExceedTheLibraryHardLimit()
    {
        ValidationOptions options = new ValidationOptions { MaxIssues = int.MaxValue };
        List<ValidationIssue> candidates = [];
        for (int index = 0; index < FixDefaults.MaxDiagnostics + 1; index++)
        {
            candidates.Add(Issue(ValidationLayer.Envelope, ValidationSeverity.Warning, index.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }

        ValidationResult result = ValidationCoordinator.CoordinateValidation(candidates, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IssueLimitReached, Is.True);
            Assert.That(result.Issues, Has.Count.EqualTo(FixDefaults.MaxDiagnostics));
            Assert.That(result.Issues[0].Code, Is.EqualTo("0"));
            Assert.That(result.Issues[^1].Code, Is.EqualTo((FixDefaults.MaxDiagnostics - 1).ToString(System.Globalization.CultureInfo.InvariantCulture)));
        });
    }

    private static ValidationIssue Issue(ValidationLayer layer, ValidationSeverity severity, string code)
    {
        return new ValidationIssue { Layer = layer, Severity = severity, Code = code };
    }
}
