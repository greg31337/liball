namespace gltf.Tests.Internal.Validation;

[TestFixture]
public class ValidationIssueAggregatorTests
{
    [Test]
    public void AggregateReports_AppliesSuppressionOverridesAndMinimumSeverity()
    {
        GltfValidationReport source = new GltfValidationReport();
        GltfValidationIssue suppressed = CreateIssue(
            "SUPPRESSED_CODE",
            GltfValidationSeverity.Error,
            "/suppressed");
        GltfValidationIssue promoted = CreateIssue(
            "PROMOTED_CODE",
            GltfValidationSeverity.Warning,
            "/promoted");
        source.Issues.Add(suppressed);
        source.Issues.Add(promoted);
        source.Issues.Add(CreateIssue(
            "LOW_CODE",
            GltfValidationSeverity.Information,
            "/low"));
        GltfValidationOptions options = new GltfValidationOptions();
        options.MinimumSeverity = GltfValidationSeverity.Warning;
        options.SuppressedIssueCodes.Add("SUPPRESSED_CODE");
        options.SeverityOverrides["PROMOTED_CODE"] = GltfValidationSeverity.Error;

        GltfValidationReport report = new ValidationIssueAggregator().AggregateReports(
            new List<GltfValidationReport> { source },
            options);

        Assert.Multiple(() =>
        {
            Assert.That(report.Issues, Has.Count.EqualTo(1));
            Assert.That(report.Issues[0].Code, Is.EqualTo("PROMOTED_CODE"));
            Assert.That(report.Issues[0].Severity, Is.EqualTo(GltfValidationSeverity.Error));
            Assert.That(report.IsValid, Is.False);
            Assert.That(promoted.Severity, Is.EqualTo(GltfValidationSeverity.Warning));
            Assert.That(suppressed.Severity, Is.EqualTo(GltfValidationSeverity.Error));
        });
    }

    [Test]
    public void AggregateReports_FailFastKeepsTheFirstReportableIssue()
    {
        GltfValidationReport first = new GltfValidationReport();
        first.Issues.Add(CreateIssue(
            "FILTERED",
            GltfValidationSeverity.Information,
            "/filtered"));
        first.Issues.Add(CreateIssue(
            "FIRST",
            GltfValidationSeverity.Warning,
            "/z"));
        first.Issues.Add(CreateIssue(
            "SECOND",
            GltfValidationSeverity.Error,
            "/a"));
        GltfValidationReport second = new GltfValidationReport();
        second.Issues.Add(CreateIssue(
            "THIRD",
            GltfValidationSeverity.Error,
            "/third"));
        GltfValidationOptions options = new GltfValidationOptions();
        options.FailFast = true;
        options.MinimumSeverity = GltfValidationSeverity.Warning;

        GltfValidationReport report = new ValidationIssueAggregator().AggregateReports(
            new List<GltfValidationReport> { first, second },
            options);

        Assert.Multiple(() =>
        {
            Assert.That(report.Issues, Has.Count.EqualTo(1));
            Assert.That(report.Issues[0].Code, Is.EqualTo("FIRST"));
            Assert.That(report.IsValid, Is.True);
            Assert.That(report.IssueLimitReached, Is.False);
        });
    }

    [Test]
    public void AggregateReports_StopsAtTheConfiguredIssueCap()
    {
        GltfValidationReport source = new GltfValidationReport();
        source.Issues.Add(CreateIssue("ONE", GltfValidationSeverity.Warning, "/3"));
        source.Issues.Add(CreateIssue("TWO", GltfValidationSeverity.Warning, "/2"));
        source.Issues.Add(CreateIssue("THREE", GltfValidationSeverity.Error, "/1"));
        GltfValidationOptions options = new GltfValidationOptions();
        options.MaximumIssueCount = 2;

        GltfValidationReport report = new ValidationIssueAggregator().AggregateReports(
            new List<GltfValidationReport> { source },
            options);

        Assert.Multiple(() =>
        {
            Assert.That(report.Issues, Has.Count.EqualTo(2));
            Assert.That(report.Issues[0].Code, Is.EqualTo("TWO"));
            Assert.That(report.Issues[1].Code, Is.EqualTo("ONE"));
            Assert.That(report.IssueLimitReached, Is.True);
            Assert.That(report.IsValid, Is.True);
        });
    }

    [Test]
    public void AggregateReports_ProducesOwnedDeterministicIssuesAndRelatedLocations()
    {
        GltfValidationIssue second = CreateIssue(
            "Z_CODE",
            GltfValidationSeverity.Warning,
            "/objects/2");
        second.ResourceUri = "resource.bin";
        second.HasByteOffset = true;
        second.ByteOffset = 8;
        second.RelatedJsonPointers.Add("/objects/4");
        second.RelatedJsonPointers.Add("/objects/1");
        second.RelatedJsonPointers.Add("/objects/4");
        GltfValidationIssue first = CreateIssue(
            "A_CODE",
            GltfValidationSeverity.Error,
            "/objects/1");
        GltfValidationIssue sameLocation = CreateIssue(
            "B_CODE",
            GltfValidationSeverity.Information,
            "/objects/1");
        GltfValidationReport source = new GltfValidationReport();
        source.Issues.Add(second);
        source.Issues.Add(sameLocation);
        source.Issues.Add(first);

        GltfValidationReport report = new ValidationIssueAggregator().AggregateReports(
            new List<GltfValidationReport> { source },
            new GltfValidationOptions());
        second.RelatedJsonPointers[0] = "/changed";
        second.Message = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(
                IssueKeys(report),
                Is.EqualTo(new[]
                {
                    "/objects/1|A_CODE",
                    "/objects/1|B_CODE",
                    "/objects/2|Z_CODE"
                }));
            Assert.That(report.Issues[2].Message, Is.EqualTo("message Z_CODE"));
            Assert.That(
                report.Issues[2].RelatedJsonPointers,
                Is.EqualTo(new[] { "/objects/1", "/objects/4" }));
            Assert.That(
                report.Issues[2].RelatedJsonPointers,
                Is.Not.SameAs(second.RelatedJsonPointers));
            Assert.That(report.Issues[2].ResourceUri, Is.EqualTo("resource.bin"));
            Assert.That(report.Issues[2].ByteOffset, Is.EqualTo(8));
        });
    }

    [Test]
    public void IncrementalAggregation_StopsBeforeLaterReportsAfterFailFast()
    {
        ValidationIssueAggregator aggregator = new ValidationIssueAggregator();
        GltfValidationOptions options = new GltfValidationOptions();
        options.FailFast = true;
        ValidationIssueAggregation aggregation = aggregator.CreateAggregation(options);
        GltfValidationReport first = new GltfValidationReport();
        first.Issues.Add(CreateIssue(
            "STABLE_CUSTOM_CODE",
            GltfValidationSeverity.Error,
            "/first"));
        GltfValidationReport later = new GltfValidationReport();
        later.Issues.Add(CreateIssue(
            "LATER",
            GltfValidationSeverity.Error,
            "/later"));

        aggregator.AppendReport(aggregation, first);
        aggregator.AppendReport(aggregation, later);
        GltfValidationReport report = aggregator.CompleteAggregation(aggregation);

        Assert.Multiple(() =>
        {
            Assert.That(aggregation.Stopped, Is.True);
            Assert.That(report.Issues, Has.Count.EqualTo(1));
            Assert.That(report.Issues[0].Code, Is.EqualTo("STABLE_CUSTOM_CODE"));
        });
    }

    [Test]
    public void AggregateReports_ZeroCapRecordsThatAReportableIssueWasDropped()
    {
        GltfValidationReport source = new GltfValidationReport();
        source.Issues.Add(CreateIssue(
            "DROPPED",
            GltfValidationSeverity.Error,
            "/dropped"));
        GltfValidationOptions options = new GltfValidationOptions();
        options.MaximumIssueCount = 0;

        GltfValidationReport report = new ValidationIssueAggregator().AggregateReports(
            new List<GltfValidationReport> { source },
            options);

        Assert.Multiple(() =>
        {
            Assert.That(report.Issues, Is.Empty);
            Assert.That(report.IssueLimitReached, Is.True);
            Assert.That(report.IsValid, Is.True);
        });
    }

    [Test]
    public void AggregateReports_RejectsInvalidConfiguration()
    {
        ValidationIssueAggregator aggregator = new ValidationIssueAggregator();
        GltfValidationOptions negativeCap = new GltfValidationOptions();
        negativeCap.MaximumIssueCount = -1;
        GltfValidationOptions invalidSeverity = new GltfValidationOptions();
        invalidSeverity.MinimumSeverity = (GltfValidationSeverity)99;
        GltfValidationOptions invalidOverride = new GltfValidationOptions();
        invalidOverride.SeverityOverrides["CODE"] = (GltfValidationSeverity)99;

        Assert.Multiple(() =>
        {
            Assert.That(
                () => aggregator.AggregateReports(
                    new List<GltfValidationReport>(),
                    negativeCap),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => aggregator.AggregateReports(
                    new List<GltfValidationReport>(),
                    invalidSeverity),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => aggregator.AggregateReports(
                    new List<GltfValidationReport>(),
                    invalidOverride),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => aggregator.AggregateReports(null!, new GltfValidationOptions()),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("reports"));
        });
    }

    [Test]
    public void IncrementalAggregation_PropagatesSourceCapsAndRejectsUnstableCodes()
    {
        ValidationIssueAggregator aggregator = new ValidationIssueAggregator();
        ValidationIssueAggregation aggregation = aggregator.CreateAggregation(
            new GltfValidationOptions());
        GltfValidationReport capped = new GltfValidationReport();
        capped.IssueLimitReached = true;
        aggregator.AppendReport(aggregation, capped);
        GltfValidationIssue invalid = CreateIssue(
            "",
            GltfValidationSeverity.Error,
            "/invalid");

        Assert.Multiple(() =>
        {
            Assert.That(aggregation.Report.IssueLimitReached, Is.True);
            Assert.That(
                () => aggregator.AppendIssue(aggregation, invalid),
                Throws.TypeOf<ArgumentException>()
                    .With.Property("ParamName").EqualTo("source"));
        });
    }

    private static GltfValidationIssue CreateIssue(
        string code,
        GltfValidationSeverity severity,
        string pointer)
    {
        GltfValidationIssue result = new GltfValidationIssue();
        result.Code = code;
        result.Severity = severity;
        result.Message = $"message {code}";
        result.JsonPointer = pointer;
        return result;
    }

    private static string[] IssueKeys(GltfValidationReport report)
    {
        string[] result = new string[report.Issues.Count];
        int index = 0;
        while (index < report.Issues.Count)
        {
            result[index] = report.Issues[index].JsonPointer
                + "|"
                + report.Issues[index].Code;
            index++;
        }

        return result;
    }
}
