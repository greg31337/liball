namespace sdp.Tests.Unit.Validation;

public class SdpValidationSeverityPolicyTests
{
    [Test]
    public void FatalStructuralIssues_CannotBeSuppressedByDiagnosticCode()
    {
        SdpValidationResult result = ResultWithSuppressedFatalCode();
        SdpValidationIssueLimiter limiter = new SdpValidationIssueLimiter(result.Issues, 4);
        SdpSourceLocation attributeLocation = new SdpSourceLocation { LineNumber = 7, ColumnNumber = 1 };
        SdpSourceLocation fieldLocation = new SdpSourceLocation { LineNumber = 3, ColumnNumber = 1 };

        SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ParseNulCharacterNotAllowed, SdpValidationSeverity.Fatal, SdpScope.Media, SdpAttributeNames.Candidate, 0, 0, "A fatal attribute structure failure.", attributeLocation);
        SdpValidationCore.AddFieldValidationIssue(result, limiter, SdpDiagnosticCodes.ParseNulCharacterNotAllowed, SdpValidationSeverity.Fatal, SdpScope.Session, 'o', -1, 0, "A fatal field structure failure.", fieldLocation);
        SdpValidationCore.PopulateValidationCounts(result);

        Assert.Multiple(() =>
        {
            Assert.That(result.Issues, Has.Count.EqualTo(2));
            Assert.That(result.Issues, Has.All.Matches<SdpValidationIssue>(issue => issue.Severity == SdpValidationSeverity.Fatal));
            Assert.That(result.Issues, Has.All.Matches<SdpValidationIssue>(issue => issue.Code == SdpDiagnosticCodes.ParseNulCharacterNotAllowed));
            Assert.That(result.Issues.Select(issue => issue.FieldName), Is.EqualTo(new[] { 'a', 'o' }));
            Assert.That(result.ErrorCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void NonFatalIssues_StillHonorStableCodeSuppression()
    {
        SdpValidationResult result = ResultWithSuppressedFatalCode();
        SdpValidationIssueLimiter limiter = new SdpValidationIssueLimiter(result.Issues, 4);
        SdpSourceLocation location = new SdpSourceLocation();

        SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ParseNulCharacterNotAllowed, SdpValidationSeverity.Error, SdpScope.Session, string.Empty, -1, 0, "A suppressible non-fatal issue.", location);
        SdpValidationCore.AddFieldValidationIssue(result, limiter, SdpDiagnosticCodes.ParseNulCharacterNotAllowed, SdpValidationSeverity.Warning, SdpScope.Session, 'v', -1, 0, "A suppressible warning.", location);

        Assert.That(result.Issues, Is.Empty);
    }

    private static SdpValidationResult ResultWithSuppressedFatalCode()
    {
        SdpValidationResult result = new SdpValidationResult();
        result.AppliedOptions.SuppressedDiagnosticCodes.Add(SdpDiagnosticCodes.ParseNulCharacterNotAllowed);
        return result;
    }
}
