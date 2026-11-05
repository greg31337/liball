using System.Text;
using iso20022;

namespace iso20022.Tests;

public class MigrationMappingApiExhaustiveTests
{
    [Test]
    public void MappingExecutesEveryBuiltInActionAndReportsDataLoss()
    {
        MappingDefinition mapping = Mapping();
        mapping.Rules.Add(Rule(0, MappingActionKind.Copy, "/Document/A", "/Document/ACopy"));
        mapping.Rules.Add(Rule(1, MappingActionKind.Rename, "/Document/B", "/Document/BRenamed"));
        mapping.Rules.Add(Rule(2, MappingActionKind.Move, "/Document/C", "/Document/Group/C"));
        MappingRuleDefinition convert = Rule(3, MappingActionKind.Convert, "/Document/D", "/Document/DConverted");
        convert.ConverterIdentifier = "uppercase";
        mapping.Rules.Add(convert);
        MappingRuleDefinition constant = Rule(4, MappingActionKind.Constant, string.Empty, "/Document/Constant");
        constant.ConstantValue = "fixed";
        mapping.Rules.Add(constant);
        MappingRuleDefinition split = Rule(5, MappingActionKind.Split, "/Document/E", "/Document/E1");
        split.TargetPaths.Add(new XmlPathData { Value = "/Document/E2" });
        mapping.Rules.Add(split);
        MappingRuleDefinition merge = Rule(6, MappingActionKind.Merge, "/Document/F", "/Document/FG");
        merge.SourcePaths.Add(new XmlPathData { Value = "/Document/G" });
        mapping.Rules.Add(merge);
        MappingRuleDefinition codeMap = Rule(7, MappingActionKind.CodeMap, "/Document/H", "/Document/HMapped");
        codeMap.SourceCodeValues.Add("OLD");
        codeMap.TargetCodeValues.Add("NEW");
        mapping.Rules.Add(codeMap);
        mapping.Rules.Add(Rule(8, MappingActionKind.Drop, "/Document/I", string.Empty));
        MappingRequest request = MappingRequestFor(
            "<A>a</A><B>b</B><C>c</C><D> mixed </D><E>e</E>" +
            "<F>f</F><G>g</G><H>OLD</H><I>discarded</I>",
            mapping);

        MappingResult result = new MessageMapper().MapMessage(request);
        string xml = Encoding.UTF8.GetString(result.Xml);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.AppliedRules, Has.Count.EqualTo(9));
            Assert.That(result.AppliedRules.Select(rule => rule.Action), Is.EqualTo(new[]
            {
                MappingActionKind.Copy,
                MappingActionKind.Rename,
                MappingActionKind.Move,
                MappingActionKind.Convert,
                MappingActionKind.Constant,
                MappingActionKind.Split,
                MappingActionKind.Merge,
                MappingActionKind.CodeMap,
                MappingActionKind.Drop
            }));
            Assert.That(result.FailedRules, Is.Empty);
            Assert.That(result.UnmappedSourcePaths, Is.Empty);
            Assert.That(result.DataLoss, Is.True);
            Assert.That(xml, Does.Contain("<ACopy>a</ACopy>"));
            Assert.That(xml, Does.Contain("<BRenamed>b</BRenamed>"));
            Assert.That(xml, Does.Contain("<Group><C>c</C></Group>"));
            Assert.That(xml, Does.Contain("<DConverted> MIXED </DConverted>"));
            Assert.That(xml, Does.Contain("<Constant>fixed</Constant>"));
            Assert.That(xml, Does.Contain("<E1>e</E1>"));
            Assert.That(xml, Does.Contain("<E2>e</E2>"));
            Assert.That(xml, Does.Contain("<FG>fg</FG>"));
            Assert.That(xml, Does.Contain("<HMapped>NEW</HMapped>"));
            Assert.That(xml, Does.Not.Contain("discarded"));
        });
    }

    [Test]
    public void NamedConversionAndCustomRuleFailuresAreExplicit()
    {
        MappingDefinition conversionMapping = Mapping();
        conversionMapping.AllowUnmappedSource = true;
        MappingRuleDefinition conversion = Rule(
            0,
            MappingActionKind.Convert,
            "/Document/Value",
            "/Document/Converted");
        conversion.ConverterIdentifier = "caller.unknown-converter";
        conversion.Required = true;
        conversionMapping.Rules.Add(conversion);
        MappingResult conversionResult = new MessageMapper().MapMessage(
            MappingRequestFor("<Value>x</Value>", conversionMapping));

        MappingDefinition customMapping = Mapping();
        customMapping.AllowUnmappedSource = true;
        MappingRuleDefinition custom = Rule(
            0,
            MappingActionKind.Custom,
            "/Document/Value",
            "/Document/Custom");
        custom.Identifier = "caller.custom-rule";
        custom.Required = true;
        customMapping.Rules.Add(custom);
        MappingResult customResult = new MessageMapper().MapMessage(
            MappingRequestFor("<Value>x</Value>", customMapping));

        Assert.Multiple(() =>
        {
            Assert.That(conversionResult.Success, Is.False);
            Assert.That(conversionResult.ErrorCode,
                Is.EqualTo("mapping.required_rule_failed"));
            Assert.That(conversionResult.FailedRules.Single(), Is.SameAs(conversion));
            Assert.That(conversionResult.MissingTargetPaths.Single().Value,
                Is.EqualTo("/Document/Converted"));
            Assert.That(conversionResult.UnmappedSourcePaths.Single().Value,
                Is.EqualTo("/Document/Value"));
            Assert.That(customResult.Success, Is.False);
            Assert.That(customResult.ErrorCode,
                Is.EqualTo("mapping.required_rule_failed"));
            Assert.That(customResult.FailedRules.Single(), Is.SameAs(custom));
            Assert.That(customResult.Diagnostics.Single().Kind,
                Is.EqualTo(DiagnosticKind.UnsupportedConstraint));
        });
    }

    [Test]
    public void FailedMigrationConverterUsesOnlyDualAuthorizedFallback()
    {
        MigrationStep failedStep = new MigrationStep
        {
            Sequence = 0,
            Action = MigrationActionKind.Convert,
            ConverterIdentifier = "caller.unknown-converter",
            Required = true
        };
        failedStep.SourcePaths.Add(new XmlPathData { Value = "/Document/Value" });
        failedStep.TargetPaths.Add(new XmlPathData { Value = "/Document/Converted" });
        MigrationRequest denied = MigrationRequestFor("<Value>x</Value>");
        denied.AllowSupplementaryFallback = true;
        denied.Plan = Plan(failedStep);

        MigrationResult deniedResult = new MessageMigrator().MigrateMessage(denied);

        MigrationRequest authorized = MigrationRequestFor("<Value>x</Value>");
        authorized.AllowSupplementaryFallback = true;
        authorized.Plan = Plan(failedStep);
        authorized.Plan.SupplementaryFallbackAuthorized = true;
        MigrationResult authorizedResult = new MessageMigrator().MigrateMessage(authorized);

        Assert.Multiple(() =>
        {
            Assert.That(deniedResult.Success, Is.False);
            Assert.That(deniedResult.ErrorCode,
                Is.EqualTo("migration.required_step_failed"));
            Assert.That(deniedResult.SupplementaryFallbackUsed, Is.False);
            Assert.That(deniedResult.UnmappedContent.Single().Xml,
                Does.Contain(">x</Value>"));
            Assert.That(authorizedResult.Success, Is.True);
            Assert.That(authorizedResult.SupplementaryFallbackUsed, Is.True);
            Assert.That(Encoding.UTF8.GetString(authorizedResult.Xml),
                Does.Contain("<SplmtryData><Envlp><Value>x</Value></Envlp></SplmtryData>"));
        });
    }

    [Test]
    public void MigrationReturnsSuccessfulAndOperationFailedValidationResults()
    {
        StubValidator validValidator = new StubValidator
        {
            Report = new ValidationReport { Success = true, Valid = true }
        };
        MigrationRequest validRequest = MigrationRequestFor("<Value>x</Value>");
        validRequest.Plan = Plan();
        validRequest.ValidateTarget = true;
        validRequest.Validator = validValidator;
        MigrationResult valid = new MessageMigrator().MigrateMessage(validRequest);

        StubValidator failedValidator = new StubValidator
        {
            Report = new ValidationReport
            {
                Success = false,
                Valid = false,
                ErrorCode = "validation.configuration"
            }
        };
        MigrationRequest failedRequest = MigrationRequestFor("<Value>x</Value>");
        failedRequest.Plan = Plan();
        failedRequest.ValidateTarget = true;
        failedRequest.Validator = failedValidator;
        MigrationResult failed = new MessageMigrator().MigrateMessage(failedRequest);

        Assert.Multiple(() =>
        {
            Assert.That(valid.Success, Is.True);
            Assert.That(valid.TargetValidation, Is.SameAs(validValidator.Report));
            Assert.That(failed.Success, Is.False);
            Assert.That(failed.ErrorCode,
                Is.EqualTo("migration.target_validation_failed"));
            Assert.That(failed.TargetValidation, Is.SameAs(failedValidator.Report));
        });
    }

    [Test]
    public void MappingReturnsSuccessfulAndOperationFailedValidationResults()
    {
        MappingDefinition mapping = Mapping();
        mapping.Rules.Add(Rule(0, MappingActionKind.Copy, "/Document/Value", "/Document/Value"));
        StubValidator validValidator = new StubValidator
        {
            Report = new ValidationReport { Success = true, Valid = true }
        };
        MappingRequest validRequest = MappingRequestFor("<Value>x</Value>", mapping);
        validRequest.ValidateTarget = true;
        validRequest.Validator = validValidator;
        MappingResult valid = new MessageMapper().MapMessage(validRequest);

        StubValidator failedValidator = new StubValidator
        {
            Report = new ValidationReport
            {
                Success = false,
                Valid = false,
                ErrorCode = "validation.configuration"
            }
        };
        MappingRequest failedRequest = MappingRequestFor("<Value>x</Value>", mapping);
        failedRequest.ValidateTarget = true;
        failedRequest.Validator = failedValidator;
        MappingResult failed = new MessageMapper().MapMessage(failedRequest);

        Assert.Multiple(() =>
        {
            Assert.That(valid.Success, Is.True);
            Assert.That(valid.TargetValidation, Is.SameAs(validValidator.Report));
            Assert.That(failed.Success, Is.False);
            Assert.That(failed.ErrorCode,
                Is.EqualTo("mapping.target_validation_failed"));
            Assert.That(failed.TargetValidation, Is.SameAs(failedValidator.Report));
        });
    }

    private static MappingDefinition Mapping()
    {
        return new MappingDefinition
        {
            Identifier = "exhaustive-mapping",
            SourceMessageIdentifier = Identifier("10"),
            TargetMessageIdentifier = Identifier("11")
        };
    }

    private static MappingRequest MappingRequestFor(
        string content,
        MappingDefinition mapping)
    {
        return new MappingRequest
        {
            Xml = Encoding.UTF8.GetBytes(MessageXml(content)),
            Mapping = mapping,
            ValidateTarget = false
        };
    }

    private static MappingRuleDefinition Rule(
        int sequence,
        MappingActionKind action,
        string source,
        string target)
    {
        MappingRuleDefinition rule = new MappingRuleDefinition
        {
            Identifier = "rule-" + sequence,
            Sequence = sequence,
            Action = action
        };
        if (source.Length > 0)
        {
            rule.SourcePaths.Add(new XmlPathData { Value = source });
        }

        if (target.Length > 0)
        {
            rule.TargetPaths.Add(new XmlPathData { Value = target });
        }

        return rule;
    }

    private static MigrationRequest MigrationRequestFor(string content)
    {
        return new MigrationRequest
        {
            Xml = Encoding.UTF8.GetBytes(MessageXml(content)),
            SourceMessageIdentifier = Identifier("10"),
            TargetMessageIdentifier = Identifier("11"),
            ValidateTarget = false
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

    private static string MessageXml(string content)
    {
        return "<Document xmlns=\"urn:iso:std:iso:20022:tech:xsd:pacs.008.001.10\">" +
            content + "</Document>";
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

    private class StubValidator : IMessageValidator
    {
        public ValidationReport Report = new ValidationReport();

        public ValidationReport ValidateMessage(ValidationRequest? request)
        {
            return Report;
        }
    }
}
