using System.Text;
using iso20022;

namespace iso20022.Tests;

public class MessageMigratorTests
{
    [Test]
    public void ExecutesEveryBuiltInActionAndMaterializesTargetVersion()
    {
        MigrationRequest request = Request(
            "<Document xmlns=\"urn:iso:std:iso:20022:tech:xsd:pacs.008.001.10\">" +
            "<A>one</A><B>two</B><C> mixed </C><E>split</E><F>x</F><G>y</G>" +
            "<Code>A</Code><Gone>secret</Gone><Keep>copy</Keep></Document>");
        request.Plan = Plan(
            Step(0, MigrationActionKind.Rename, "/Document/A", "/Document/AR"),
            Step(1, MigrationActionKind.Move, "/Document/B", "/Document/Group/B"),
            ConversionStep(2, "/Document/C", "/Document/C2", "uppercase"),
            DefaultStep(3, "/Document/D", "default"),
            SplitStep(4, "/Document/E", "/Document/E1", "/Document/E2"),
            MergeStep(5, "/Document/F", "/Document/G", "/Document/FG"),
            CodeStep(6, "/Document/Code", "/Document/Mapped", "A", "X"),
            Step(7, MigrationActionKind.Drop, "/Document/Gone", string.Empty),
            Step(8, MigrationActionKind.Copy, "/Document/Keep", "/Document/KeepCopy"));

        MigrationResult result = new MessageMigrator().MigrateMessage(request);
        string xml = Encoding.UTF8.GetString(result.Xml);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.AppliedSteps, Has.Count.EqualTo(9));
            Assert.That(result.FailedSteps, Is.Empty);
            Assert.That(xml, Does.Contain("pacs.008.001.11"));
            Assert.That(xml, Does.Contain("<AR>one</AR>"));
            Assert.That(xml, Does.Contain("<Group><B>two</B></Group>"));
            Assert.That(xml, Does.Contain("<C2> MIXED </C2>"));
            Assert.That(xml, Does.Contain("<D>default</D>"));
            Assert.That(xml, Does.Contain("<E1>split</E1>"));
            Assert.That(xml, Does.Contain("<E2>split</E2>"));
            Assert.That(xml, Does.Contain("<FG>xy</FG>"));
            Assert.That(xml, Does.Contain("<Mapped>X</Mapped>"));
            Assert.That(xml, Does.Contain("<KeepCopy>copy</KeepCopy>"));
            Assert.That(xml, Does.Not.Contain("<Gone>"));
            Assert.That(result.UnmappedSourcePaths.Select(path => path.Value),
                Is.EqualTo(new[] { "/Document/Gone" }));
            Assert.That(result.UnmappedContent.Single().Xml, Does.Contain("secret"));
            Assert.That(result.DataLoss, Is.True);
            Assert.That(result.Document?.Metadata.MessageIdentifier?.Version, Is.EqualTo("11"));
        });
    }

    [Test]
    public void SupplementaryFallbackRequiresRequestAndPlanAuthorization()
    {
        MigrationRequest denied = Request(XmlWith("<Old>value</Old>"));
        denied.AllowSupplementaryFallback = true;
        denied.Plan = Plan(Step(0, MigrationActionKind.Drop, "/Document/Old", string.Empty));

        MigrationResult deniedResult = new MessageMigrator().MigrateMessage(denied);

        MigrationRequest authorized = Request(XmlWith("<Old>value</Old>"));
        authorized.AllowSupplementaryFallback = true;
        authorized.Plan = Plan(Step(0, MigrationActionKind.Drop, "/Document/Old", string.Empty));
        authorized.Plan.SupplementaryFallbackAuthorized = true;

        MigrationResult authorizedResult = new MessageMigrator().MigrateMessage(authorized);
        string authorizedXml = Encoding.UTF8.GetString(authorizedResult.Xml);

        Assert.Multiple(() =>
        {
            Assert.That(deniedResult.Success, Is.True);
            Assert.That(deniedResult.SupplementaryFallbackUsed, Is.False);
            Assert.That(Encoding.UTF8.GetString(deniedResult.Xml), Does.Not.Contain("SplmtryData"));
            Assert.That(authorizedResult.Success, Is.True);
            Assert.That(authorizedResult.SupplementaryFallbackUsed, Is.True);
            Assert.That(authorizedXml, Does.Contain("<SplmtryData><Envlp><Old>value</Old></Envlp></SplmtryData>"));
            Assert.That(authorizedResult.UnmappedContent, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void RequiredConversionFailureIsReportedAndCanUseAuthorizedFallback()
    {
        MigrationStep custom = Step(0, MigrationActionKind.Custom, "/Document/Old", "/Document/New");
        custom.Required = true;
        custom.ConverterIdentifier = "caller.rule";
        MigrationRequest failed = Request(XmlWith("<Old>value</Old>"));
        failed.Plan = Plan(custom);

        MigrationResult failedResult = new MessageMigrator().MigrateMessage(failed);

        MigrationRequest retained = Request(XmlWith("<Old>value</Old>"));
        retained.AllowSupplementaryFallback = true;
        retained.Plan = Plan(custom);
        retained.Plan.SupplementaryFallbackAuthorized = true;

        MigrationResult retainedResult = new MessageMigrator().MigrateMessage(retained);

        Assert.Multiple(() =>
        {
            Assert.That(failedResult.Success, Is.False);
            Assert.That(failedResult.ErrorCode, Is.EqualTo("migration.required_step_failed"));
            Assert.That(failedResult.FailedSteps, Has.Count.EqualTo(1));
            Assert.That(failedResult.Diagnostics.Single().Code, Is.EqualTo("migration.step_failed"));
            Assert.That(retainedResult.Success, Is.True);
            Assert.That(retainedResult.SupplementaryFallbackUsed, Is.True);
            Assert.That(retainedResult.FailedSteps, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void RunsTargetValidationAndPropagatesInvalidOutcome()
    {
        StubValidator validator = new StubValidator
        {
            Report = new ValidationReport { Success = true, Valid = false }
        };
        validator.Report.Diagnostics.Add(new ValidationDiagnostic
        {
            Code = "target.problem",
            Severity = ValidationSeverity.Error
        });
        MigrationRequest request = Request(XmlWith("<Value>ok</Value>"));
        request.ValidateTarget = true;
        request.Validator = validator;
        request.Plan = Plan();

        MigrationResult result = new MessageMigrator().MigrateMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("migration.target_invalid"));
            Assert.That(result.TargetValidation, Is.SameAs(validator.Report));
            Assert.That(result.Diagnostics.Single().Code, Is.EqualTo("target.problem"));
            Assert.That(validator.Request?.ExpectedIdentifier?.Version, Is.EqualTo("11"));
            Assert.That(validator.Request?.Document, Is.SameAs(result.Document));
        });
    }

    [Test]
    public void RejectsMissingInputInvalidSequenceAndMissingValidator()
    {
        MessageMigrator migrator = new MessageMigrator();
        MigrationRequest missing = Request(string.Empty);
        missing.Plan = Plan();
        MigrationRequest sequence = Request(XmlWith("<Value>ok</Value>"));
        MigrationStep nonContiguous = Step(2, MigrationActionKind.Copy, "/Document/Value", "/Document/Value");
        sequence.Plan = Plan(nonContiguous);
        MigrationRequest validator = Request(XmlWith("<Value>ok</Value>"));
        validator.ValidateTarget = true;
        validator.Plan = Plan();

        MigrationResult missingResult = migrator.MigrateMessage(missing);
        MigrationResult sequenceResult = migrator.MigrateMessage(sequence);
        MigrationResult validatorResult = migrator.MigrateMessage(validator);

        Assert.Multiple(() =>
        {
            Assert.That(migrator.MigrateMessage(null).ErrorCode, Is.EqualTo("migration.request_missing"));
            Assert.That(missingResult.ErrorCode, Is.EqualTo("migration.input_invalid"));
            Assert.That(sequenceResult.ErrorCode, Is.EqualTo("migration.step_sequence_invalid"));
            Assert.That(validatorResult.ErrorCode, Is.EqualTo("migration.validator_required"));
        });
    }

    private static MigrationRequest Request(string xml)
    {
        return new MigrationRequest
        {
            Xml = Encoding.UTF8.GetBytes(xml),
            SourceMessageIdentifier = Identifier("10"),
            TargetMessageIdentifier = Identifier("11"),
            ValidateTarget = false
        };
    }

    private static string XmlWith(string content)
    {
        return "<Document xmlns=\"urn:iso:std:iso:20022:tech:xsd:pacs.008.001.10\">" + content + "</Document>";
    }

    private static MessageIdentifier Identifier(string version)
    {
        return new MessageIdentifier
        {
            BusinessArea = "pacs",
            MessageFunctionality = "008",
            Variant = "001",
            Version = version,
            Value = "pacs.008.001." + version
        };
    }

    private static MigrationPlan Plan(params MigrationStep[] steps)
    {
        MigrationPlan plan = new MigrationPlan
        {
            Success = true,
            SourceMessageIdentifier = Identifier("10"),
            TargetMessageIdentifier = Identifier("11")
        };
        plan.Steps.AddRange(steps);
        return plan;
    }

    private static MigrationStep Step(int sequence, MigrationActionKind action, string source, string target)
    {
        MigrationStep step = new MigrationStep { Sequence = sequence, Action = action };
        if (source.Length > 0)
        {
            step.SourcePaths.Add(new XmlPathData { Value = source });
        }

        if (target.Length > 0)
        {
            step.TargetPaths.Add(new XmlPathData { Value = target });
        }

        return step;
    }

    private static MigrationStep ConversionStep(int sequence, string source, string target, string converter)
    {
        MigrationStep step = Step(sequence, MigrationActionKind.Convert, source, target);
        step.ConverterIdentifier = converter;
        return step;
    }

    private static MigrationStep DefaultStep(int sequence, string target, string value)
    {
        MigrationStep step = Step(sequence, MigrationActionKind.DefaultValue, string.Empty, target);
        step.DefaultValue = value;
        return step;
    }

    private static MigrationStep SplitStep(int sequence, string source, string first, string second)
    {
        MigrationStep step = Step(sequence, MigrationActionKind.Split, source, first);
        step.TargetPaths.Add(new XmlPathData { Value = second });
        return step;
    }

    private static MigrationStep MergeStep(int sequence, string first, string second, string target)
    {
        MigrationStep step = Step(sequence, MigrationActionKind.Merge, first, target);
        step.SourcePaths.Add(new XmlPathData { Value = second });
        return step;
    }

    private static MigrationStep CodeStep(int sequence, string source, string target, string from, string to)
    {
        MigrationStep step = Step(sequence, MigrationActionKind.CodeMap, source, target);
        step.SourceCodeValues.Add(from);
        step.TargetCodeValues.Add(to);
        return step;
    }

    private class StubValidator : IMessageValidator
    {
        public ValidationReport Report = new ValidationReport();
        public ValidationRequest? Request;

        public ValidationReport ValidateMessage(ValidationRequest? request)
        {
            Request = request;
            return Report;
        }
    }
}
