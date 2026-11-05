using iso20022;

namespace iso20022.Tests;

public class MessageProjectorTests
{
    [TestCase("payment-initiation", "pain.001.001.11", "PAY-001")]
    [TestCase("payment-status", "pacs.002.001.16", "ACCP")]
    [TestCase("interbank-credit-transfer", "pacs.008.001.10", "E2E-001")]
    [TestCase("cash-statement", "camt.053.001.13", "ACCOUNT-001")]
    [TestCase("securities", "semt.001.001.04", "US0378331005")]
    [TestCase("cards", "caaa.001.001.13", "MERCHANT-001")]
    [TestCase("foreign-exchange", "fxtr.008.001.08", "FX-TRADE-001")]
    [TestCase("trade", "tsmt.001.001.03", "BASELINE-001")]
    public void ProjectsEveryStableTaskViewFromExactRawMessage(
        string projectionIdentifier,
        string messageIdentifier,
        string expectedValue)
    {
        BuilderRequest builderRequest = CreateBuilderRequest(
            projectionIdentifier,
            messageIdentifier);
        BuilderResult built = new MessageBuilder().BuildMessage(builderRequest);
        ProjectionRequest request = new ProjectionRequest
        {
            Xml = built.Xml,
            ProjectionIdentifier = projectionIdentifier,
            ExpectedSourceMessageIdentifier = Identifier(messageIdentifier),
            ExpectedSourceNamespaceUri = Namespace(messageIdentifier)
        };
        IMessageProjector projector = new MessageProjector();

        ProjectionResult result = projector.ProjectMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(built.Success, Is.True, built.ErrorMessage);
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.SourceMessageIdentifier?.Value,
                Is.EqualTo(messageIdentifier));
            Assert.That(result.SourceNamespaceUri,
                Is.EqualTo(Namespace(messageIdentifier)));
            Assert.That(result.ProjectionDefinition, Is.Not.Null);
            Assert.That(result.ProjectionDefinition?.Rules, Is.Not.Empty);
            Assert.That(result.AppliedRules,
                Is.EqualTo(result.ProjectionDefinition?.Rules));
            Assert.That(result.SourcePaths, Is.Not.Empty);
            Assert.That(result.SourcePaths,
                Has.All.Matches<XmlPathData>(path =>
                    path.Value.StartsWith("/Document/") &&
                    path.Segments.All(segment =>
                        segment.NamespaceUri == Namespace(messageIdentifier))));
            AssertProjectedValue(result.View, projectionIdentifier, expectedValue);
            AssertRoundTripView(builderRequest, result.View);
        });
    }

    [TestCase("payment-initiation", "pain.001.001.03")]
    [TestCase("payment-status", "pain.002.001.12")]
    [TestCase("interbank-credit-transfer", "pacs.009.001.11")]
    [TestCase("cash-management", "camt.054.001.13")]
    [TestCase("securities", "sese.023.001.12")]
    [TestCase("cards", "catm.001.001.13")]
    [TestCase("foreign-exchange", "fxtr.014.001.05")]
    [TestCase("trade", "tsmt.019.001.04")]
    public void ProjectsAlternateSupportedExactVersions(
        string projectionIdentifier,
        string messageIdentifier)
    {
        BuilderRequest builderRequest = CreateBuilderRequest(
            projectionIdentifier,
            messageIdentifier);
        BuilderResult built = new MessageBuilder().BuildMessage(builderRequest);
        ProjectionRequest request = new ProjectionRequest
        {
            Xml = built.Xml,
            ProjectionIdentifier = projectionIdentifier,
            ExpectedSourceMessageIdentifier = Identifier(messageIdentifier),
            ExpectedSourceNamespaceUri = Namespace(messageIdentifier)
        };

        ProjectionResult result = new MessageProjector().ProjectMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(built.Success, Is.True, built.ErrorMessage);
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.SourceMessageIdentifier?.Value,
                Is.EqualTo(messageIdentifier));
            Assert.That(result.SourceNamespaceUri,
                Is.EqualTo(Namespace(messageIdentifier)));
            AssertRoundTripView(builderRequest, result.View);
        });
    }

    [Test]
    public void PreservesPrecisionQualifiedPathsMetadataAndUnmappedSubtree()
    {
        BuilderResult built = new MessageBuilder().BuildMessage(
            CreateBuilderRequest("cash-statement", "camt.053.001.13"));
        DynamicXmlNode payload = built.Document!.Payloads[0].GenericContent!;
        DynamicXmlNode message = payload.Children[0];
        DynamicXmlNode extension = Element("Extension", "urn:example:extension");
        DynamicXmlNode secret = Element("Secret", "urn:example:extension");
        secret.Children.Add(Text("kept-verbatim"));
        extension.Children.Add(secret);
        message.Children.Add(extension);
        ProjectionRequest request = new ProjectionRequest
        {
            Document = built.Document,
            ProjectionIdentifier = "cash-statement",
            ExpectedSourceMessageIdentifier = Identifier("camt.053.001.13"),
            ExpectedSourceNamespaceUri = Namespace("camt.053.001.13")
        };

        ProjectionResult result = new MessageProjector().ProjectMessage(request);
        CashManagementTemplate? view = result.View as CashManagementTemplate;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(view, Is.Not.Null);
            Assert.That(view?.BalanceAmount.Amount.Value, Is.EqualTo(1000.00m));
            Assert.That(view?.BalanceAmount.Amount.Scale, Is.EqualTo(2));
            Assert.That(view?.BalanceAmount.Amount.LexicalValue,
                Is.EqualTo("1000.00"));
            Assert.That(view?.BalanceDate.LexicalValue,
                Is.EqualTo("2026-09-17"));
            Assert.That(result.Metadata.PrimaryAmount?.Amount.Scale,
                Is.EqualTo(2));
            Assert.That(result.SourcePaths.Single(path =>
                    path.Value.EndsWith("/Bal/Amt"))
                .Segments.Last().NamespaceUri,
                Is.EqualTo(Namespace("camt.053.001.13")));
            Assert.That(result.UnmappedSourcePaths,
                Has.One.Matches<XmlPathData>(path =>
                    path.Value.EndsWith("/Extension")));
            Assert.That(result.UnmappedContent,
                Has.One.Matches<RawXmlFragment>(fragment =>
                    fragment.RootName?.NamespaceUri == "urn:example:extension" &&
                    fragment.Xml.Contains("kept-verbatim") &&
                    fragment.Utf8Bytes.Length > 0));
        });
    }

    [Test]
    public void AppliesCallerInspectableProjectionPathsWithoutImplicitReflection()
    {
        const string identifier = "pain.002.001.12";
        string namespaceUri = Namespace(identifier);
        string xml = "<Document xmlns=\"" + namespaceUri + "\">" +
            "<CstmrPmtStsRpt><Custom><Id>CUSTOM-MSG</Id>" +
            "<Code>RJCT</Code><Extra>preserved</Extra></Custom>" +
            "</CstmrPmtStsRpt></Document>";
        MappingRuleDefinition messageRule = Rule(
            0,
            "custom.message",
            "/Document/CstmrPmtStsRpt/Custom/Id",
            "MessageIdentifier");
        MappingRuleDefinition statusRule = Rule(
            1,
            "custom.status",
            "/Document/CstmrPmtStsRpt/Custom/Code",
            "Status");
        MappingDefinition mapping = new MappingDefinition
        {
            Identifier = "caller.payment-status",
            Version = "7",
            SourceMessageIdentifier = Identifier(identifier),
            TargetModelName = "PaymentStatusTemplate",
            AllowUnmappedSource = true,
            RequireAllTargets = false
        };
        mapping.Rules.Add(messageRule);
        mapping.Rules.Add(statusRule);
        ProjectionRequest request = new ProjectionRequest
        {
            Xml = System.Text.Encoding.UTF8.GetBytes(xml),
            ProjectionIdentifier = "payment-status",
            ProjectionDefinition = mapping,
            ExpectedSourceMessageIdentifier = Identifier(identifier),
            ExpectedSourceNamespaceUri = namespaceUri
        };

        ProjectionResult result = new MessageProjector().ProjectMessage(request);
        PaymentStatusTemplate? view = result.View as PaymentStatusTemplate;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.ProjectionDefinition, Is.SameAs(mapping));
            Assert.That(result.AppliedRules, Is.EqualTo(new[] { messageRule, statusRule }));
            Assert.That(view?.MessageIdentifier, Is.EqualTo("CUSTOM-MSG"));
            Assert.That(view?.Status, Is.EqualTo("RJCT"));
            Assert.That(result.SourcePaths.Select(path => path.Value),
                Is.EqualTo(new[]
                {
                    "/Document/CstmrPmtStsRpt/Custom/Id",
                    "/Document/CstmrPmtStsRpt/Custom/Code"
                }));
            Assert.That(result.UnmappedContent.Single().Xml,
                Does.Contain("preserved"));
        });
    }

    [Test]
    public void ProjectsDynamicModelTreeAndUsesExpectedExactIdentity()
    {
        BuilderResult built = new MessageBuilder().BuildMessage(
            CreateBuilderRequest("interbank-credit-transfer", "pacs.008.001.10"));
        DynamicXmlNode model = built.Document!.Payloads[0].GenericContent!;
        ProjectionRequest request = new ProjectionRequest
        {
            Model = model,
            ProjectionIdentifier = "interbank-credit-transfer",
            ExpectedSourceMessageIdentifier = Identifier("pacs.008.001.10"),
            ExpectedSourceNamespaceUri = Namespace("pacs.008.001.10")
        };

        ProjectionResult result = new MessageProjector().ProjectMessage(request);
        InterbankTransferTemplate? view = result.View as InterbankTransferTemplate;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(view?.EndToEndIdentifier, Is.EqualTo("E2E-001"));
            Assert.That(result.SourceMessageIdentifier?.Value,
                Is.EqualTo("pacs.008.001.10"));
            Assert.That(result.Metadata.MessageIdentifier?.Value,
                Is.EqualTo("pacs.008.001.10"));
        });
    }

    [Test]
    public void ProjectsGeneratedModelThroughItsExplicitCodec()
    {
        ProjectionGeneratedTestModel model = new ProjectionGeneratedTestModel
        {
            MessageIdentifier = "GENERATED-MSG",
            PaymentIdentifier = "GENERATED-PAY"
        };
        ProjectionRequest request = new ProjectionRequest
        {
            Model = model,
            ProjectionIdentifier = "payment-initiation",
            ExpectedSourceMessageIdentifier = Identifier("pain.001.001.11"),
            ExpectedSourceNamespaceUri = Namespace("pain.001.001.11")
        };

        ProjectionResult result = new MessageProjector().ProjectMessage(request);
        PaymentInitiationTemplate? view = result.View as PaymentInitiationTemplate;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(view?.MessageIdentifier, Is.EqualTo("GENERATED-MSG"));
            Assert.That(view?.PaymentInformationIdentifier,
                Is.EqualTo("GENERATED-PAY"));
            Assert.That(result.SourcePaths.Select(path => path.Value),
                Does.Contain("/Document/CstmrCdtTrfInitn/PmtInf/PmtInfId"));
        });
    }

    [Test]
    public void ExposesSourceValidationFailureWithoutDiscardingExactIdentity()
    {
        BuilderResult built = new MessageBuilder().BuildMessage(
            CreateBuilderRequest("payment-status", "pacs.002.001.16"));
        ValidationDiagnostic diagnostic = new ValidationDiagnostic
        {
            Code = "profile.status_forbidden",
            Severity = ValidationSeverity.Error,
            Stage = ValidationStage.Profile
        };
        ValidationReport report = new ValidationReport
        {
            Success = true,
            Valid = false
        };
        report.Diagnostics.Add(diagnostic);
        RecordingValidator validator = new RecordingValidator(report);
        ProjectionRequest request = new ProjectionRequest
        {
            Xml = built.Xml,
            ProjectionIdentifier = "payment-status",
            ExpectedSourceMessageIdentifier = Identifier("pacs.002.001.16"),
            ExpectedSourceNamespaceUri = Namespace("pacs.002.001.16"),
            ValidateSource = true,
            Validator = validator
        };

        ProjectionResult result = new MessageProjector().ProjectMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode,
                Is.EqualTo("projection.source_validation_failed"));
            Assert.That(result.SourceValidation, Is.SameAs(report));
            Assert.That(result.SourceMessageIdentifier?.Value,
                Is.EqualTo("pacs.002.001.16"));
            Assert.That(result.Diagnostics.First(), Is.SameAs(diagnostic));
            Assert.That(validator.Request?.Document, Is.Not.Null);
            Assert.That(validator.Request?.ExpectedNamespaceUri,
                Is.EqualTo(Namespace("pacs.002.001.16")));
        });
    }

    [Test]
    public void ReturnsSuccessfulSourceValidationOutcome()
    {
        BuilderResult built = new MessageBuilder().BuildMessage(
            CreateBuilderRequest("trade", "tsmt.001.001.03"));
        ValidationReport report = new ValidationReport
        {
            Success = true,
            Valid = true
        };
        RecordingValidator validator = new RecordingValidator(report);
        ProjectionRequest request = new ProjectionRequest
        {
            Xml = built.Xml,
            ProjectionIdentifier = "trade",
            ValidateSource = true,
            Validator = validator
        };

        ProjectionResult result = new MessageProjector().ProjectMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.SourceValidation, Is.SameAs(report));
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.View, Is.TypeOf<TradeTemplate>());
            Assert.That(validator.Request?.ExpectedIdentifier?.Value,
                Is.EqualTo("tsmt.001.001.03"));
            Assert.That(validator.Request?.ExpectedNamespaceUri,
                Is.EqualTo(Namespace("tsmt.001.001.03")));
        });
    }

    [Test]
    public void RejectsAmbiguousInputWrongFamilyAndMissingRequiredRuleSource()
    {
        ProjectionRequest ambiguous = new ProjectionRequest
        {
            Xml = new byte[] { 1 },
            Document = new MessageDocument(),
            ProjectionIdentifier = "payment-status"
        };
        BuilderResult payment = new MessageBuilder().BuildMessage(
            CreateBuilderRequest("payment-initiation", "pain.001.001.11"));
        ProjectionRequest wrongFamily = new ProjectionRequest
        {
            Xml = payment.Xml,
            ProjectionIdentifier = "trade"
        };
        MappingRuleDefinition required = Rule(
            0,
            "missing.required",
            "/Document/Absent",
            "Status");
        required.Required = true;
        MappingDefinition definition = new MappingDefinition
        {
            Identifier = "missing",
            TargetModelName = "PaymentStatusTemplate"
        };
        definition.Rules.Add(required);
        BuilderResult status = new MessageBuilder().BuildMessage(
            CreateBuilderRequest("payment-status", "pacs.002.001.16"));
        ProjectionRequest missing = new ProjectionRequest
        {
            Xml = status.Xml,
            ProjectionIdentifier = "payment-status",
            ProjectionDefinition = definition
        };

        ProjectionResult ambiguousResult = new MessageProjector().ProjectMessage(ambiguous);
        ProjectionResult wrongFamilyResult = new MessageProjector().ProjectMessage(wrongFamily);
        ProjectionResult missingResult = new MessageProjector().ProjectMessage(missing);

        Assert.Multiple(() =>
        {
            Assert.That(ambiguousResult.ErrorCode,
                Is.EqualTo("projection.input_invalid"));
            Assert.That(wrongFamilyResult.ErrorCode,
                Is.EqualTo("projection.source_family_mismatch"));
            Assert.That(missingResult.ErrorCode,
                Is.EqualTo("projection.required_source_missing"));
        });
    }

    [Test]
    public void ProjectionConfigurationAndSupportedMemberMatrixCoverBoundaries()
    {
        MessageProjector projector = new MessageProjector();
        ProjectionRequest cancelled = new ProjectionRequest
        {
            ProjectionIdentifier = "payment-initiation",
            CancellationToken = new CancellationToken(true)
        };
        ProjectionRequest noLimits = new ProjectionRequest
        {
            ProjectionIdentifier = "payment-initiation",
            Limits = null!
        };
        ProjectionRequest noMetadataPaths = new ProjectionRequest
        {
            ProjectionIdentifier = "payment-initiation",
            MetadataOptions = new MetadataExtractionOptions
                { AdditionalReferencePaths = null! }
        };
        ProjectionRequest unknown = new ProjectionRequest
            { ProjectionIdentifier = "unknown" };
        ProjectionRequest noInput = new ProjectionRequest
            { ProjectionIdentifier = "payment-initiation" };
        ProjectionRequest tooLarge = new ProjectionRequest
        {
            ProjectionIdentifier = "payment-initiation",
            Xml = "<x/>"u8.ToArray(),
            Limits = new ResourceLimits { MaxTotalBytes = 1 }
        };
        ProjectionRequest ambiguous = new ProjectionRequest
        {
            ProjectionIdentifier = "payment-initiation",
            Xml = "<x/>"u8.ToArray(),
            Document = new MessageDocument()
        };
        ProjectionRequest emptyDocument = new ProjectionRequest
        {
            ProjectionIdentifier = "payment-initiation",
            Document = new MessageDocument()
        };
        ProjectionRequest generatedNoIdentity = new ProjectionRequest
        {
            ProjectionIdentifier = "payment-initiation",
            Model = new PaymentInitiationTemplate()
        };

        Assert.Multiple(() =>
        {
            Assert.That(projector.ProjectMessage(cancelled).ErrorCode,
                Is.EqualTo("projection.cancelled"));
            Assert.That(projector.ProjectMessage(noLimits).ErrorCode,
                Is.EqualTo("projection.configuration_invalid"));
            Assert.That(projector.ProjectMessage(noMetadataPaths).ErrorCode,
                Is.EqualTo("projection.configuration_invalid"));
            Assert.That(projector.ProjectMessage(unknown).ErrorCode,
                Is.EqualTo("projection.identifier_unknown"));
            Assert.That(projector.ProjectMessage(noInput).ErrorCode,
                Is.EqualTo("projection.input_invalid"));
            Assert.That(projector.ProjectMessage(tooLarge).ErrorCode,
                Is.EqualTo("projection.input_limit"));
            Assert.That(projector.ProjectMessage(ambiguous).ErrorCode,
                Is.EqualTo("projection.input_invalid"));
            Assert.That(projector.ProjectMessage(emptyDocument).ErrorCode,
                Is.EqualTo("projection.content_missing"));
            Assert.That(projector.ProjectMessage(generatedNoIdentity).ErrorCode,
                Is.EqualTo("projection.generated_identity_missing"));
        });

        string[] identifiers =
        [
            "payment-initiation", "payment-status", "interbank-credit-transfer",
            "cash-management", "securities", "cards", "foreign-exchange", "trade"
        ];
        Type[] viewTypes =
        [
            typeof(PaymentInitiationTemplate), typeof(PaymentStatusTemplate),
            typeof(InterbankTransferTemplate), typeof(CashManagementTemplate),
            typeof(SecuritiesTemplate), typeof(CardTemplate),
            typeof(ForeignExchangeTemplate), typeof(TradeTemplate)
        ];
        int index = 0;
        while (index < identifiers.Length)
        {
            SemanticTemplateDefinition definition =
                SemanticTemplateRegistry.ResolveIdentifier(identifiers[index]) ??
                throw new AssertionException("Missing semantic template.");
            foreach (System.Reflection.FieldInfo field in viewTypes[index].GetFields())
            {
                Assert.That(MessageProjector.ProjectionTargetMemberIsSupported(
                    definition, field.Name), Is.True,
                    identifiers[index] + "." + field.Name);
            }
            Assert.That(MessageProjector.ProjectionTargetMemberIsSupported(
                definition, "Unsupported"), Is.False);
            index++;
        }

        SemanticTemplateDefinition invalidDefinition = new SemanticTemplateDefinition
            { Kind = 99 };
        Assert.That(MessageProjector.ProjectionTargetMemberIsSupported(
            invalidDefinition, "MessageIdentifier"), Is.False);

        ProjectionResult result = new ProjectionResult();
        MessageProjector.AddProjectionDiagnostic(result, "code", "message");
        MessageProjector.AddProjectionDiagnostic(result, "code", "message");
        Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
    }

    private static BuilderRequest CreateBuilderRequest(
        string projectionIdentifier,
        string messageIdentifier)
    {
        string builderIdentifier = projectionIdentifier == "cash-statement" ?
            "cash-management" : projectionIdentifier;
        BuilderRequest request = new BuilderRequest
        {
            TemplateIdentifier = builderIdentifier,
            TargetMessageIdentifier = Identifier(messageIdentifier),
            TargetNamespaceUri = Namespace(messageIdentifier),
            ValidateResult = false,
            WriteOptions = new MessageWriteOptions { WriteXmlDeclaration = false }
        };
        switch (builderIdentifier)
        {
            case "payment-initiation":
                request.PaymentInitiation = new PaymentInitiationTemplate
                {
                    MessageIdentifier = "MSG-001",
                    PaymentInformationIdentifier = "PAY-001",
                    EndToEndIdentifier = "E2E-001",
                    Debtor = "Debtor Ltd",
                    DebtorAccount = "DEBTOR-001",
                    Creditor = "Creditor Ltd",
                    CreditorAccount = "CREDITOR-001",
                    Amount = Amount("EUR", 125.2500m, 4),
                    RequestedExecutionDate = Date(2026, 9, 15),
                    RemittanceInformation = "Invoice 42"
                };
                break;
            case "payment-status":
                request.PaymentStatus = new PaymentStatusTemplate
                {
                    MessageIdentifier = "STATUS-001",
                    OriginalMessageIdentifier = "MSG-001",
                    OriginalEndToEndIdentifier = "E2E-001",
                    Status = "ACCP",
                    StatusReason = "Accepted"
                };
                break;
            case "interbank-credit-transfer":
                request.InterbankTransfer = new InterbankTransferTemplate
                {
                    MessageIdentifier = "MSG-001",
                    InstructionIdentifier = "INSTR-001",
                    EndToEndIdentifier = "E2E-001",
                    TransactionIdentifier = "TX-001",
                    Uetr = "123e4567-e89b-42d3-a456-426614174000",
                    Debtor = "Debtor",
                    DebtorAccount = "DEBTOR-ACCOUNT-001",
                    Creditor = "Creditor",
                    CreditorAccount = "CREDITOR-ACCOUNT-001",
                    SettlementAmount = Amount("USD", 99.9900m, 4),
                    SettlementDate = Date(2026, 9, 16),
                    RemittanceInformation = "Invoice 43"
                };
                break;
            case "cash-management":
                request.CashManagement = new CashManagementTemplate
                {
                    MessageIdentifier = "CASH-001",
                    ReportIdentifier = "REPORT-001",
                    AccountIdentifier = "ACCOUNT-001",
                    Owner = "Owner",
                    BalanceAmount = Amount("GBP", 1000.00m, 2),
                    BalanceDate = Date(2026, 9, 17)
                };
                break;
            case "securities":
                request.Securities = new SecuritiesTemplate
                {
                    MessageIdentifier = "SEC-001",
                    TransactionIdentifier = "SEC-TX-001",
                    SecurityIdentifier = "US0378331005",
                    AccountIdentifier = "SECURITIES-ACCOUNT-001",
                    Quantity = Decimal(25.000m, 3),
                    SettlementAmount = Amount("USD", 5000.00m, 2),
                    SettlementDate = Date(2026, 9, 18),
                    DeliveringParty = "Delivering Bank",
                    ReceivingParty = "Receiving Bank"
                };
                break;
            case "cards":
                request.Card = new CardTemplate
                {
                    MessageIdentifier = "CARD-001",
                    TransactionIdentifier = "CARD-TX-001",
                    AcquirerIdentifier = "ACQUIRER-001",
                    MerchantIdentifier = "MERCHANT-001",
                    MaskedCardIdentifier = "411111******1111",
                    Amount = Amount("USD", 12.340m, 3),
                    TransactionDateTime = DateTimeValue(2026, 9, 19, 14, 30, 15)
                };
                break;
            case "foreign-exchange":
                request.ForeignExchange = new ForeignExchangeTemplate
                {
                    MessageIdentifier = "FX-001",
                    TradeIdentifier = "FX-TRADE-001",
                    Counterparty = "Counterparty",
                    BoughtAmount = Amount("EUR", 100.00m, 2),
                    SoldAmount = Amount("USD", 123.45m, 2),
                    ExchangeRate = Decimal(1.234500m, 6),
                    TradeDate = Date(2026, 9, 20),
                    SettlementDate = Date(2026, 9, 22)
                };
                break;
            case "trade":
                request.Trade = new TradeTemplate
                {
                    MessageIdentifier = "TRADE-001",
                    TradeIdentifier = "TRADE-TX-001",
                    BaselineIdentifier = "BASELINE-001",
                    Buyer = "Buyer",
                    Seller = "Seller",
                    Amount = Amount("EUR", 750.00m, 2),
                    DueDate = Date(2026, 10, 1),
                    GoodsDescription = "Parts"
                };
                break;
        }
        return request;
    }

    private static void AssertProjectedValue(
        object? view,
        string projectionIdentifier,
        string expectedValue)
    {
        switch (projectionIdentifier)
        {
            case "payment-initiation":
                Assert.That((view as PaymentInitiationTemplate)?.PaymentInformationIdentifier,
                    Is.EqualTo(expectedValue));
                break;
            case "payment-status":
                Assert.That((view as PaymentStatusTemplate)?.Status,
                    Is.EqualTo(expectedValue));
                break;
            case "interbank-credit-transfer":
                Assert.That((view as InterbankTransferTemplate)?.EndToEndIdentifier,
                    Is.EqualTo(expectedValue));
                break;
            case "cash-statement":
                Assert.That((view as CashManagementTemplate)?.AccountIdentifier,
                    Is.EqualTo(expectedValue));
                break;
            case "securities":
                Assert.That((view as SecuritiesTemplate)?.SecurityIdentifier,
                    Is.EqualTo(expectedValue));
                break;
            case "cards":
                Assert.That((view as CardTemplate)?.MerchantIdentifier,
                    Is.EqualTo(expectedValue));
                break;
            case "foreign-exchange":
                Assert.That((view as ForeignExchangeTemplate)?.TradeIdentifier,
                    Is.EqualTo(expectedValue));
                break;
            case "trade":
                Assert.That((view as TradeTemplate)?.BaselineIdentifier,
                    Is.EqualTo(expectedValue));
                break;
        }
    }

    private static void AssertRoundTripView(
        BuilderRequest request,
        object? actual)
    {
        if (request.PaymentInitiation != null)
        {
            PaymentInitiationTemplate expected = request.PaymentInitiation;
            PaymentInitiationTemplate? view = actual as PaymentInitiationTemplate;
            Assert.That(view, Is.Not.Null);
            Assert.That(view?.MessageIdentifier, Is.EqualTo(expected.MessageIdentifier));
            Assert.That(view?.PaymentInformationIdentifier,
                Is.EqualTo(expected.PaymentInformationIdentifier));
            Assert.That(view?.EndToEndIdentifier,
                Is.EqualTo(expected.EndToEndIdentifier));
            Assert.That(view?.Debtor, Is.EqualTo(expected.Debtor));
            Assert.That(view?.DebtorAccount, Is.EqualTo(expected.DebtorAccount));
            Assert.That(view?.Creditor, Is.EqualTo(expected.Creditor));
            Assert.That(view?.CreditorAccount, Is.EqualTo(expected.CreditorAccount));
            AssertAmount(view?.Amount, expected.Amount);
            AssertDate(view?.RequestedExecutionDate, expected.RequestedExecutionDate);
            Assert.That(view?.RemittanceInformation,
                Is.EqualTo(expected.RemittanceInformation));
            return;
        }
        if (request.PaymentStatus != null)
        {
            PaymentStatusTemplate expected = request.PaymentStatus;
            PaymentStatusTemplate? view = actual as PaymentStatusTemplate;
            Assert.That(view, Is.Not.Null);
            Assert.That(view?.MessageIdentifier, Is.EqualTo(expected.MessageIdentifier));
            Assert.That(view?.OriginalMessageIdentifier,
                Is.EqualTo(expected.OriginalMessageIdentifier));
            Assert.That(view?.OriginalEndToEndIdentifier,
                Is.EqualTo(expected.OriginalEndToEndIdentifier));
            Assert.That(view?.Status, Is.EqualTo(expected.Status));
            Assert.That(view?.StatusReason, Is.EqualTo(expected.StatusReason));
            return;
        }
        if (request.InterbankTransfer != null)
        {
            InterbankTransferTemplate expected = request.InterbankTransfer;
            InterbankTransferTemplate? view = actual as InterbankTransferTemplate;
            Assert.That(view, Is.Not.Null);
            Assert.That(view?.MessageIdentifier, Is.EqualTo(expected.MessageIdentifier));
            Assert.That(view?.InstructionIdentifier,
                Is.EqualTo(expected.InstructionIdentifier));
            Assert.That(view?.EndToEndIdentifier,
                Is.EqualTo(expected.EndToEndIdentifier));
            Assert.That(view?.TransactionIdentifier,
                Is.EqualTo(expected.TransactionIdentifier));
            Assert.That(view?.Uetr, Is.EqualTo(expected.Uetr));
            Assert.That(view?.Debtor, Is.EqualTo(expected.Debtor));
            Assert.That(view?.DebtorAccount, Is.EqualTo(expected.DebtorAccount));
            Assert.That(view?.Creditor, Is.EqualTo(expected.Creditor));
            Assert.That(view?.CreditorAccount, Is.EqualTo(expected.CreditorAccount));
            AssertAmount(view?.SettlementAmount, expected.SettlementAmount);
            AssertDate(view?.SettlementDate, expected.SettlementDate);
            Assert.That(view?.RemittanceInformation,
                Is.EqualTo(expected.RemittanceInformation));
            return;
        }
        if (request.CashManagement != null)
        {
            CashManagementTemplate expected = request.CashManagement;
            CashManagementTemplate? view = actual as CashManagementTemplate;
            Assert.That(view, Is.Not.Null);
            Assert.That(view?.MessageIdentifier, Is.EqualTo(expected.MessageIdentifier));
            Assert.That(view?.ReportIdentifier, Is.EqualTo(expected.ReportIdentifier));
            Assert.That(view?.AccountIdentifier,
                Is.EqualTo(expected.AccountIdentifier));
            Assert.That(view?.Owner, Is.EqualTo(expected.Owner));
            AssertAmount(view?.BalanceAmount, expected.BalanceAmount);
            AssertDate(view?.BalanceDate, expected.BalanceDate);
            return;
        }
        if (request.Securities != null)
        {
            SecuritiesTemplate expected = request.Securities;
            SecuritiesTemplate? view = actual as SecuritiesTemplate;
            Assert.That(view, Is.Not.Null);
            Assert.That(view?.MessageIdentifier, Is.EqualTo(expected.MessageIdentifier));
            Assert.That(view?.TransactionIdentifier,
                Is.EqualTo(expected.TransactionIdentifier));
            Assert.That(view?.SecurityIdentifier,
                Is.EqualTo(expected.SecurityIdentifier));
            Assert.That(view?.AccountIdentifier,
                Is.EqualTo(expected.AccountIdentifier));
            AssertDecimal(view?.Quantity, expected.Quantity);
            AssertAmount(view?.SettlementAmount, expected.SettlementAmount);
            AssertDate(view?.SettlementDate, expected.SettlementDate);
            Assert.That(view?.DeliveringParty,
                Is.EqualTo(expected.DeliveringParty));
            Assert.That(view?.ReceivingParty,
                Is.EqualTo(expected.ReceivingParty));
            return;
        }
        if (request.Card != null)
        {
            CardTemplate expected = request.Card;
            CardTemplate? view = actual as CardTemplate;
            Assert.That(view, Is.Not.Null);
            Assert.That(view?.MessageIdentifier, Is.EqualTo(expected.MessageIdentifier));
            Assert.That(view?.TransactionIdentifier,
                Is.EqualTo(expected.TransactionIdentifier));
            Assert.That(view?.AcquirerIdentifier,
                Is.EqualTo(expected.AcquirerIdentifier));
            Assert.That(view?.MerchantIdentifier,
                Is.EqualTo(expected.MerchantIdentifier));
            Assert.That(view?.MaskedCardIdentifier,
                Is.EqualTo(expected.MaskedCardIdentifier));
            AssertAmount(view?.Amount, expected.Amount);
            AssertDateTime(view?.TransactionDateTime, expected.TransactionDateTime);
            return;
        }
        if (request.ForeignExchange != null)
        {
            ForeignExchangeTemplate expected = request.ForeignExchange;
            ForeignExchangeTemplate? view = actual as ForeignExchangeTemplate;
            Assert.That(view, Is.Not.Null);
            Assert.That(view?.MessageIdentifier, Is.EqualTo(expected.MessageIdentifier));
            Assert.That(view?.TradeIdentifier, Is.EqualTo(expected.TradeIdentifier));
            Assert.That(view?.Counterparty, Is.EqualTo(expected.Counterparty));
            AssertAmount(view?.BoughtAmount, expected.BoughtAmount);
            AssertAmount(view?.SoldAmount, expected.SoldAmount);
            AssertDecimal(view?.ExchangeRate, expected.ExchangeRate);
            AssertDate(view?.TradeDate, expected.TradeDate);
            AssertDate(view?.SettlementDate, expected.SettlementDate);
            return;
        }
        if (request.Trade != null)
        {
            TradeTemplate expected = request.Trade;
            TradeTemplate? view = actual as TradeTemplate;
            Assert.That(view, Is.Not.Null);
            Assert.That(view?.MessageIdentifier, Is.EqualTo(expected.MessageIdentifier));
            Assert.That(view?.TradeIdentifier, Is.EqualTo(expected.TradeIdentifier));
            Assert.That(view?.BaselineIdentifier,
                Is.EqualTo(expected.BaselineIdentifier));
            Assert.That(view?.Buyer, Is.EqualTo(expected.Buyer));
            Assert.That(view?.Seller, Is.EqualTo(expected.Seller));
            AssertAmount(view?.Amount, expected.Amount);
            AssertDate(view?.DueDate, expected.DueDate);
            Assert.That(view?.GoodsDescription,
                Is.EqualTo(expected.GoodsDescription));
            return;
        }
        Assert.Fail("Expected a generated semantic template input.");
    }

    private static void AssertAmount(
        IsoAmountValue? actual,
        IsoAmountValue expected)
    {
        Assert.That(actual, Is.Not.Null);
        Assert.That(actual?.Currency, Is.EqualTo(expected.Currency));
        AssertDecimal(actual?.Amount, expected.Amount);
    }

    private static void AssertDecimal(
        IsoDecimalValue? actual,
        IsoDecimalValue expected)
    {
        Assert.That(actual, Is.Not.Null);
        Assert.That(actual?.Value, Is.EqualTo(expected.Value));
        Assert.That(actual?.Scale, Is.EqualTo(expected.Scale));
        Assert.That(actual?.IsNegativeZero, Is.EqualTo(expected.IsNegativeZero));
    }

    private static void AssertDate(
        IsoDateValue? actual,
        IsoDateValue expected)
    {
        Assert.That(actual, Is.Not.Null);
        Assert.That(actual?.Year, Is.EqualTo(expected.Year));
        Assert.That(actual?.Month, Is.EqualTo(expected.Month));
        Assert.That(actual?.Day, Is.EqualTo(expected.Day));
        Assert.That(actual?.HasTimezone, Is.EqualTo(expected.HasTimezone));
        Assert.That(actual?.TimezoneOffsetMinutes,
            Is.EqualTo(expected.TimezoneOffsetMinutes));
    }

    private static void AssertDateTime(
        IsoDateTimeValue? actual,
        IsoDateTimeValue expected)
    {
        Assert.That(actual, Is.Not.Null);
        AssertDate(actual?.Date, expected.Date);
        Assert.That(actual?.Time.Hour, Is.EqualTo(expected.Time.Hour));
        Assert.That(actual?.Time.Minute, Is.EqualTo(expected.Time.Minute));
        Assert.That(actual?.Time.Second, Is.EqualTo(expected.Time.Second));
        Assert.That(actual?.Time.FractionalSecondDigits,
            Is.EqualTo(expected.Time.FractionalSecondDigits));
    }

    private static MappingRuleDefinition Rule(
        int sequence,
        string identifier,
        string sourcePath,
        string targetMember)
    {
        MappingRuleDefinition rule = new MappingRuleDefinition
        {
            Sequence = sequence,
            Identifier = identifier,
            Action = MappingActionKind.Copy,
            TargetMember = targetMember
        };
        rule.SourcePaths.Add(new XmlPathData { Value = sourcePath });
        return rule;
    }

    private static DynamicXmlNode Element(string localName, string namespaceUri)
    {
        return new DynamicXmlNode
        {
            Kind = XmlNodeKind.Element,
            Name = new QualifiedNameData
            {
                LocalName = localName,
                NamespaceUri = namespaceUri
            }
        };
    }

    private static DynamicXmlNode Text(string value)
    {
        return new DynamicXmlNode
        {
            Kind = XmlNodeKind.Text,
            Value = value
        };
    }

    private static IsoAmountValue Amount(string currency, decimal value, int scale)
    {
        return new IsoAmountValue
        {
            Currency = currency,
            Amount = Decimal(value, scale)
        };
    }

    private static IsoDecimalValue Decimal(decimal value, int scale)
    {
        return new IsoDecimalValue
        {
            Value = value,
            Scale = scale
        };
    }

    private static IsoDateValue Date(int year, int month, int day)
    {
        return new IsoDateValue
        {
            Year = year,
            Month = month,
            Day = day
        };
    }

    private static IsoDateTimeValue DateTimeValue(
        int year,
        int month,
        int day,
        int hour,
        int minute,
        int second)
    {
        return new IsoDateTimeValue
        {
            Date = Date(year, month, day),
            Time = new IsoTimeValue
            {
                Hour = hour,
                Minute = minute,
                Second = second
            }
        };
    }

    private static MessageIdentifier Identifier(string value)
    {
        string[] parts = value.Split('.');
        return new MessageIdentifier
        {
            BusinessArea = parts[0],
            MessageFunctionality = parts[1],
            Variant = parts[2],
            Version = parts[3],
            Value = value
        };
    }

    private static string Namespace(string identifier)
    {
        return Iso20022Namespaces.SchemaUrnPrefix + identifier;
    }

    private sealed class RecordingValidator : IMessageValidator
    {
        private readonly ValidationReport report;

        public RecordingValidator(ValidationReport report)
        {
            this.report = report;
        }

        public ValidationRequest? Request;

        public ValidationReport ValidateMessage(ValidationRequest? request)
        {
            Request = request;
            return report;
        }
    }
}

public class ProjectionGeneratedTestModel
{
    public string MessageIdentifier = string.Empty;
    public string PaymentIdentifier = string.Empty;
}

internal static class ProjectionGeneratedTestModelCodec
{
    internal static void WriteDocument(
        System.Xml.XmlWriter writer,
        ProjectionGeneratedTestModel model,
        string messageIdentifier,
        string namespaceUri)
    {
        writer.WriteStartElement("Document", namespaceUri);
        writer.WriteStartElement("CstmrCdtTrfInitn", namespaceUri);
        writer.WriteStartElement("GrpHdr", namespaceUri);
        writer.WriteElementString("MsgId", namespaceUri, model.MessageIdentifier);
        writer.WriteEndElement();
        writer.WriteStartElement("PmtInf", namespaceUri);
        writer.WriteElementString("PmtInfId", namespaceUri, model.PaymentIdentifier);
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndElement();
    }
}
