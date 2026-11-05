using System.Text;
using iso20022;

namespace iso20022.Tests;

public class MessageMapperTests
{
    [Test]
    public void MapsMessageWithInspectablePathsConstantsCodesAndDrops()
    {
        MappingDefinition mapping = MessageMapping();
        mapping.Rules.Add(Rule(0, MappingActionKind.Rename, "/Document/Amount", "/Document/Value"));
        mapping.Rules.Add(ConstantRule(1, "/Document/Currency", "USD"));
        mapping.Rules.Add(CodeRule(2, "/Document/Status", "/Document/State", "OLD", "NEW"));
        mapping.Rules.Add(Rule(3, MappingActionKind.Drop, "/Document/Discard", string.Empty));
        MappingRequest request = Request(
            "<Amount>10.50</Amount><Status>OLD</Status><Discard>private</Discard>",
            mapping);

        MappingResult result = new MessageMapper().MapMessage(request);
        string xml = Encoding.UTF8.GetString(result.Xml);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Mapping, Is.SameAs(mapping));
            Assert.That(result.AppliedRules, Is.EqualTo(mapping.Rules));
            Assert.That(result.FailedRules, Is.Empty);
            Assert.That(result.UnmappedSourcePaths, Is.Empty);
            Assert.That(result.DataLoss, Is.True);
            Assert.That(xml, Does.Contain("pacs.008.001.11"));
            Assert.That(xml, Does.Contain("<Value>10.50</Value>"));
            Assert.That(xml, Does.Contain("<Currency>USD</Currency>"));
            Assert.That(xml, Does.Contain("<State>NEW</State>"));
            Assert.That(xml, Does.Not.Contain("private"));
            Assert.That(result.Document?.Metadata.MessageIdentifier?.Version, Is.EqualTo("11"));
        });
    }

    [Test]
    public void MapsMessageToExplicitCanonicalNodeMembers()
    {
        MappingDefinition mapping = new MappingDefinition
        {
            Identifier = "message-to-canonical",
            SourceMessageIdentifier = Identifier("10"),
            TargetModelName = "CanonicalPayment"
        };
        mapping.Rules.Add(new MappingRuleDefinition
        {
            Identifier = "amount",
            Sequence = 0,
            Action = MappingActionKind.Copy,
            SourcePaths = new List<XmlPathData> { new XmlPathData { Value = "/Document/Amount" } },
            TargetMember = "Amount",
            Required = true
        });
        MappingRequest request = Request("<Amount>42</Amount>", mapping);

        MappingResult result = new MessageMapper().MapMessage(request);
        DynamicXmlNode? model = result.Model as DynamicXmlNode;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Document, Is.Null);
            Assert.That(model?.Name.LocalName, Is.EqualTo("CanonicalPayment"));
            Assert.That(model?.Children.Single().Name.LocalName, Is.EqualTo("Amount"));
            Assert.That(model?.Children.Single().Children.Single().Value, Is.EqualTo("42"));
            Assert.That(Encoding.UTF8.GetString(result.Xml),
                Does.Contain("<CanonicalPayment><Amount>42</Amount></CanonicalPayment>"));
        });
    }

    [Test]
    public void MapsCanonicalNodeMembersToMessageWithoutReflection()
    {
        DynamicXmlNode canonical = Element("CanonicalPayment");
        DynamicXmlNode amount = Element("Amount");
        amount.Children.Add(Text("25"));
        canonical.Children.Add(amount);
        MappingDefinition mapping = new MappingDefinition
        {
            Identifier = "canonical-to-message",
            SourceModelName = "CanonicalPayment",
            TargetMessageIdentifier = Identifier("11")
        };
        mapping.Rules.Add(new MappingRuleDefinition
        {
            Identifier = "amount",
            Sequence = 0,
            Action = MappingActionKind.Rename,
            SourceMember = "Amount",
            TargetPaths = new List<XmlPathData> { new XmlPathData { Value = "/Document/IntrBkSttlmAmt" } },
            Required = true
        });
        MappingRequest request = new MappingRequest
        {
            Model = canonical,
            Mapping = mapping,
            ValidateTarget = false
        };

        MappingResult result = new MessageMapper().MapMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Model, Is.Null);
            Assert.That(result.Document, Is.Not.Null);
            Assert.That(Encoding.UTF8.GetString(result.Xml),
                Does.Contain("<IntrBkSttlmAmt>25</IntrBkSttlmAmt>"));
            Assert.That(result.UnmappedSourcePaths, Is.Empty);
        });
    }

    [Test]
    public void ReportsUnmappedSourceAndMissingRequiredTargets()
    {
        MappingDefinition unmappedMapping = MessageMapping();
        unmappedMapping.Rules.Add(Rule(0, MappingActionKind.Copy, "/Document/Amount", "/Document/Amount"));
        MappingRequest unmappedRequest = Request("<Amount>10</Amount><Extra>x</Extra>", unmappedMapping);

        MappingResult unmapped = new MessageMapper().MapMessage(unmappedRequest);

        MappingDefinition requiredMapping = MessageMapping();
        MappingRuleDefinition required = Rule(0, MappingActionKind.Copy, "/Document/Absent", "/Document/Required");
        required.Required = true;
        requiredMapping.Rules.Add(required);
        requiredMapping.AllowUnmappedSource = true;
        MappingResult missing = new MessageMapper().MapMessage(
            Request("<Amount>10</Amount>", requiredMapping));

        Assert.Multiple(() =>
        {
            Assert.That(unmapped.Success, Is.False);
            Assert.That(unmapped.ErrorCode, Is.EqualTo("mapping.source_unmapped"));
            Assert.That(unmapped.UnmappedSourcePaths.Select(path => path.Value),
                Is.EqualTo(new[] { "/Document/Extra" }));
            Assert.That(missing.Success, Is.False);
            Assert.That(missing.ErrorCode, Is.EqualTo("mapping.required_rule_failed"));
            Assert.That(missing.FailedRules.Single(), Is.SameAs(required));
            Assert.That(missing.MissingTargetPaths.Single().Value,
                Is.EqualTo("/Document/Required"));
            Assert.That(missing.Diagnostics.Single().Code, Is.EqualTo("mapping.rule_failed"));
        });
    }

    [Test]
    public void RejectsArbitraryClrModelsInsteadOfReflectingOverMembers()
    {
        MappingDefinition mapping = new MappingDefinition
        {
            SourceModelName = "SecretModel",
            TargetModelName = "TargetModel",
            AllowUnmappedSource = true
        };
        mapping.Rules.Add(new MappingRuleDefinition
        {
            Sequence = 0,
            Action = MappingActionKind.Copy,
            SourceMember = "Value",
            TargetMember = "Value"
        });
        MappingRequest request = new MappingRequest
        {
            Model = new SecretModel { Value = "must-not-be-read" },
            Mapping = mapping,
            ValidateTarget = false
        };

        MappingResult result = new MessageMapper().MapMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("mapping.model_unsupported"));
            Assert.That(result.Xml, Is.Empty);
        });
    }

    [Test]
    public void ValidatesMappedTargetAndPropagatesDiagnostics()
    {
        MappingDefinition mapping = MessageMapping();
        mapping.Rules.Add(Rule(0, MappingActionKind.Copy, "/Document/Amount", "/Document/Amount"));
        StubValidator validator = new StubValidator
        {
            Report = new ValidationReport { Success = true, Valid = false }
        };
        validator.Report.Diagnostics.Add(new ValidationDiagnostic { Code = "target.invalid" });
        MappingRequest request = Request("<Amount>10</Amount>", mapping);
        request.ValidateTarget = true;
        request.Validator = validator;

        MappingResult result = new MessageMapper().MapMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("mapping.target_invalid"));
            Assert.That(result.TargetValidation, Is.SameAs(validator.Report));
            Assert.That(result.Diagnostics.Single().Code, Is.EqualTo("target.invalid"));
            Assert.That(validator.Request?.ExpectedIdentifier?.Version, Is.EqualTo("11"));
            Assert.That(validator.Request?.Document, Is.SameAs(result.Document));
        });
    }

    [Test]
    public void RejectsMissingDefinitionsInvalidSequencesAndMissingValidators()
    {
        MessageMapper mapper = new MessageMapper();
        MappingRequest missing = new MappingRequest
        {
            Xml = Encoding.UTF8.GetBytes(MessageXml("<Amount>10</Amount>")),
            ValidateTarget = false
        };
        MappingDefinition badSequence = MessageMapping();
        badSequence.Rules.Add(Rule(1, MappingActionKind.Copy, "/Document/Amount", "/Document/Amount"));
        MappingRequest sequenceRequest = Request("<Amount>10</Amount>", badSequence);
        MappingDefinition valid = MessageMapping();
        valid.Rules.Add(Rule(0, MappingActionKind.Copy, "/Document/Amount", "/Document/Amount"));
        MappingRequest validatorRequest = Request("<Amount>10</Amount>", valid);
        validatorRequest.ValidateTarget = true;

        Assert.Multiple(() =>
        {
            Assert.That(mapper.MapMessage(null).ErrorCode, Is.EqualTo("mapping.request_missing"));
            Assert.That(mapper.MapMessage(missing).ErrorCode, Is.EqualTo("mapping.definition_missing"));
            Assert.That(mapper.MapMessage(sequenceRequest).ErrorCode,
                Is.EqualTo("mapping.rule_sequence_invalid"));
            Assert.That(mapper.MapMessage(validatorRequest).ErrorCode,
                Is.EqualTo("mapping.validator_required"));
        });
    }

    private static MappingDefinition MessageMapping()
    {
        return new MappingDefinition
        {
            Identifier = "message-map",
            SourceMessageIdentifier = Identifier("10"),
            TargetMessageIdentifier = Identifier("11")
        };
    }

    private static MappingRequest Request(string content, MappingDefinition mapping)
    {
        return new MappingRequest
        {
            Xml = Encoding.UTF8.GetBytes(MessageXml(content)),
            Mapping = mapping,
            ValidateTarget = false
        };
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

    private static MappingRuleDefinition ConstantRule(int sequence, string target, string value)
    {
        MappingRuleDefinition rule = Rule(sequence, MappingActionKind.Constant, string.Empty, target);
        rule.ConstantValue = value;
        return rule;
    }

    private static MappingRuleDefinition CodeRule(
        int sequence,
        string source,
        string target,
        string sourceCode,
        string targetCode)
    {
        MappingRuleDefinition rule = Rule(sequence, MappingActionKind.CodeMap, source, target);
        rule.SourceCodeValues.Add(sourceCode);
        rule.TargetCodeValues.Add(targetCode);
        return rule;
    }

    private static DynamicXmlNode Element(string name)
    {
        return new DynamicXmlNode
        {
            Kind = XmlNodeKind.Element,
            Name = new QualifiedNameData { LocalName = name }
        };
    }

    private static DynamicXmlNode Text(string value)
    {
        return new DynamicXmlNode { Kind = XmlNodeKind.Text, Value = value };
    }

    private class SecretModel
    {
        public string Value = string.Empty;
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
