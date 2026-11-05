namespace diameter.Tests;

public class ValidationDataTests
{
    [Test]
    public void ValidationDataDefaultsRepresentAValidOutcomeWithoutAnErrorAnswer()
    {
        DiameterValidationIssue issue = new DiameterValidationIssue();
        DiameterErrorAnswerRequest error = new DiameterErrorAnswerRequest();
        DiameterValidationResult result = new DiameterValidationResult();

        Assert.Multiple(() =>
        {
            Assert.That(issue.Severity, Is.EqualTo(DiameterIssueSeverity.Information));
            Assert.That(issue.Code, Is.Empty);
            Assert.That(issue.Message, Is.Empty);
            Assert.That(issue.ResultCode, Is.Zero);
            Assert.That(issue.AvpCode, Is.Zero);
            Assert.That(issue.AvpVendorId, Is.Zero);
            Assert.That(issue.ParentAvpCode, Is.Zero);
            Assert.That(issue.ParentAvpVendorId, Is.Zero);
            Assert.That(issue.AvpIndex, Is.EqualTo(-1));
            Assert.That(issue.Depth, Is.Zero);
            Assert.That(issue.ExpectedMaximum, Is.EqualTo(-1));
            Assert.That(issue.ExpectedLength, Is.EqualTo(-1));
            Assert.That(issue.ActualLength, Is.EqualTo(-1));
            Assert.That(issue.ExpectedPosition, Is.EqualTo(-1));
            Assert.That(issue.OffendingAvp, Is.Null);
            Assert.That(issue.FailedAvp, Is.Null);
            Assert.That(error.Request, Is.Null);
            Assert.That(error.Issue, Is.Null);
            Assert.That(error.ResultCode, Is.Zero);
            Assert.That(error.ExperimentalResult, Is.Null);
            Assert.That(error.ErrorMessage, Is.Empty);
            Assert.That(error.ErrorReportingHost, Is.Null);
            Assert.That(error.FailedAvp, Is.Null);
            Assert.That(result.Status, Is.EqualTo(DiameterValidationStatus.Valid));
            Assert.That(result.Issues, Is.Empty);
            Assert.That(result.ErrorAnswer, Is.Null);
        });
    }

    [Test]
    public void ValidationDataRetainsIssueAndErrorAnswerReferences()
    {
        DiameterMessage request = new DiameterMessage();
        DiameterAvp offending = new DiameterAvp();
        DiameterAvp failed = new DiameterAvp();
        DiameterValidationIssue issue = new DiameterValidationIssue();
        issue.Severity = DiameterIssueSeverity.Error;
        issue.Code = "invalid-avp-size";
        issue.Message = "invalid";
        issue.ResultCode = DiameterResultCodes.InvalidAvpLength;
        issue.OffendingAvp = offending;
        issue.FailedAvp = failed;
        DiameterErrorAnswerRequest error = new DiameterErrorAnswerRequest();
        error.Request = request;
        error.Issue = issue;
        error.ResultCode = issue.ResultCode;
        error.ErrorMessage = issue.Message;
        error.FailedAvp = failed;
        DiameterValidationResult result = new DiameterValidationResult();
        result.Status = DiameterValidationStatus.Invalid;
        result.Issues.Add(issue);
        result.ErrorAnswer = error;

        Assert.Multiple(() =>
        {
            Assert.That(result.Issues, Is.EqualTo(new[] { issue }));
            Assert.That(result.ErrorAnswer, Is.SameAs(error));
            Assert.That(error.Request, Is.SameAs(request));
            Assert.That(error.Issue, Is.SameAs(issue));
            Assert.That(error.FailedAvp, Is.SameAs(failed));
            Assert.That(issue.OffendingAvp, Is.SameAs(offending));
            Assert.That(issue.FailedAvp, Is.SameAs(failed));
        });
    }
}
