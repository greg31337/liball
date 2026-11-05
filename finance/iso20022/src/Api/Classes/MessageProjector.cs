using System;
using System.Collections.Generic;
using System.Text;

namespace iso20022;

/// <summary>Projects exact ISO 20022 messages into stable version-neutral task views.</summary>
public class MessageProjector : IMessageProjector
{
    /// <summary>Initializes a deterministic task-oriented message projector.</summary>
    public MessageProjector()
    {
    }

    /// <summary>Projects one raw, generic, or generated source message.</summary>
    /// <param name="request">The source, exact identity, projection, and validation policy.</param>
    /// <returns>The projected view with source paths, unmapped data, and validation.</returns>
    public ProjectionResult ProjectMessage(ProjectionRequest? request)
    {
        if (request == null)
        {
            return CreateProjectionFailure("projection.request_missing", "A projection request is required.", string.Empty);
        }
        if (request.CancellationToken.IsCancellationRequested)
        {
            return CreateProjectionFailure("projection.cancelled", "Projection was cancelled.", request.ProjectionIdentifier);
        }
        if (request.Limits == null || !MessageValidator.LimitsArePositive(request.Limits) ||
            request.MetadataOptions == null || request.MetadataOptions.AdditionalReferencePaths == null)
        {
            return CreateProjectionFailure("projection.configuration_invalid", "Projection limits and metadata options are required.", request.ProjectionIdentifier);
        }

        SemanticTemplateDefinition? definition = SemanticTemplateRegistry.ResolveIdentifier(request.ProjectionIdentifier);
        if (definition == null)
        {
            return CreateProjectionFailure("projection.identifier_unknown", "The task-oriented projection identifier is not supported.", request.ProjectionIdentifier);
        }

        ProjectionExecutionState state = new ProjectionExecutionState();
        state.Request = request;
        state.Definition = definition;
        state.Result.ProjectionIdentifier = definition.Identifier;
        if (!ValidateProjectionInput(state) || !ReadProjectionSource(state))
        {
            return state.Result;
        }
        if (request.CancellationToken.IsCancellationRequested)
        {
            return FailProjection(state, "projection.cancelled", "Projection was cancelled.");
        }

        ResolveProjectionIdentity(state);
        if (state.Result.ErrorCode.Length > 0 || !ValidateProjectionDefinition(state))
        {
            return state.Result;
        }
        if (!ValidateProjectionSource(state))
        {
            return state.Result;
        }

        IndexProjectionTree(state, state.Tree, new XmlPathData());
        state.Result.ProjectionDefinition = request.ProjectionDefinition ??
            CreateGeneratedProjectionDefinition(state);
        state.Result.View = CreateProjectedView(state);
        if (state.Result.ErrorCode.Length > 0)
        {
            return state.Result;
        }
        if (state.Result.SourcePaths.Count == 0 || state.Result.View == null)
        {
            return FailProjection(state, "projection.content_unrecognized", "The source does not contain content recognized by the selected projection.");
        }

        CollectUnmappedProjectionContent(state, state.Tree);
        if (state.Result.ErrorCode.Length > 0)
        {
            return state.Result;
        }
        state.Result.Success = true;
        return state.Result;
    }

    internal static bool ValidateProjectionInput(ProjectionExecutionState state)
    {
        ProjectionRequest request = state.Request;
        int count = 0;
        if (request.Xml.Length > 0)
        {
            count++;
            if (request.Xml.LongLength > request.Limits.MaxTotalBytes)
            {
                FailProjection(state, "projection.input_limit", "The raw source exceeds the configured total-byte limit.");
                return false;
            }
        }
        if (request.Document != null)
        {
            count++;
        }
        if (request.Model != null)
        {
            count++;
        }
        if (count != 1)
        {
            FailProjection(state, "projection.input_invalid", "Exactly one raw, generic, or generated-model source is required.");
            return false;
        }
        return true;
    }

    internal static bool ReadProjectionSource(ProjectionExecutionState state)
    {
        ProjectionRequest request = state.Request;
        MessageReadOptions readOptions = CreateProjectionReadOptions(request.Limits);
        if (request.Xml.Length > 0)
        {
            MessageReadResult read = new XmlMessageReader().ReadAutoBytes(request.Xml, readOptions);
            if (!read.Success || read.Document == null)
            {
                FailProjection(state, read.ErrorCode.Length == 0 ? "projection.read_failed" : read.ErrorCode, read.ErrorMessage);
                return false;
            }
            state.Document = read.Document;
        }
        else if (request.Document != null)
        {
            state.Document = request.Document;
        }
        else
        {
            MessageDocument? modelDocument = request.Model as MessageDocument;
            if (modelDocument != null)
            {
                state.Document = modelDocument;
            }
            else
            {
                DynamicXmlNode? modelTree = request.Model as DynamicXmlNode;
                if (modelTree == null)
                {
                    return ReadGeneratedProjectionModel(state, request.Model, readOptions);
                }
                DynamicXmlNode documentTree = CanonicalXmlNodeNormalizer.CloneNode(modelTree);
                if (documentTree.Kind != XmlNodeKind.Document)
                {
                    DynamicXmlNode wrapper = new DynamicXmlNode();
                    wrapper.Kind = XmlNodeKind.Document;
                    wrapper.Children.Add(documentTree);
                    documentTree = wrapper;
                }
                MessageReadResult materialized = XmlMessageReader.BuildGenericResult(documentTree, 0, readOptions);
                if (!materialized.Success || materialized.Document == null)
                {
                    FailProjection(state, "projection.model_invalid", "The generated-model XML tree could not be materialized.");
                    return false;
                }
                state.Document = materialized.Document;
            }
        }

        DynamicXmlNode? tree = XmlMessageWriter.BuildDocumentTree(
            state.Document,
            MigrationExecutor.CreateWriteOptions(request.Limits));
        if (tree == null)
        {
            FailProjection(state, "projection.content_missing", "The source document does not contain projectable XML content.");
            return false;
        }
        state.Tree = CanonicalXmlNodeNormalizer.CloneNode(tree);
        return true;
    }

    internal static bool ReadGeneratedProjectionModel(ProjectionExecutionState state, object? model, MessageReadOptions readOptions)
    {
        MessageIdentifier? identifier = NormalizeProjectionIdentifier(state.Request.ExpectedSourceMessageIdentifier);
        if (identifier == null)
        {
            FailProjection(state, "projection.generated_identity_missing", "An exact expected source identifier is required for a generated model.");
            return false;
        }
        string namespaceUri = state.Request.ExpectedSourceNamespaceUri;
        if (namespaceUri.Length == 0)
        {
            namespaceUri = Iso20022Namespaces.SchemaUrnPrefix + identifier.Value;
        }
        GeneratedModelWriteResult generated = GeneratedModelXmlSerializer.WriteModel(model, identifier, namespaceUri);
        if (!generated.Success)
        {
            FailProjection(state, "projection." + generated.ErrorCode, generated.ErrorMessage);
            return false;
        }
        MessageReadResult read = new XmlMessageReader().ReadAutoText(generated.Xml, readOptions);
        if (!read.Success || read.Document == null)
        {
            FailProjection(state, read.ErrorCode.Length == 0 ? "projection.generated_read_failed" : read.ErrorCode, read.ErrorMessage);
            return false;
        }
        state.Document = read.Document;
        DynamicXmlNode? tree = XmlMessageWriter.BuildDocumentTree(
            state.Document,
            MigrationExecutor.CreateWriteOptions(state.Request.Limits));
        if (tree == null)
        {
            FailProjection(state, "projection.content_missing", "The generated source does not contain projectable XML content.");
            return false;
        }
        state.Tree = CanonicalXmlNodeNormalizer.CloneNode(tree);
        return true;
    }

    internal static MessageReadOptions CreateProjectionReadOptions(ResourceLimits limits)
    {
        MessageReadOptions options = new MessageReadOptions();
        options.Limits = limits;
        options.UnknownContentMode = UnknownContentMode.Preserve;
        options.PreserveLexicalValues = true;
        options.PreserveWhitespace = true;
        return options;
    }

    internal static void ResolveProjectionIdentity(ProjectionExecutionState state)
    {
        MetadataExtractionOptions options = CopyProjectionMetadataOptions(state.Request.MetadataOptions, state.Request.Limits);
        state.Result.Metadata = new MetadataExtractor().ExtractMetadata(state.Document, options);
        MessageIdentifier? actualIdentifier = state.Result.Metadata.MessageIdentifier;
        string actualNamespace = state.Result.Metadata.NamespaceUri;
        MessageIdentifier? expectedIdentifier = NormalizeProjectionIdentifier(state.Request.ExpectedSourceMessageIdentifier);
        if (state.Request.ExpectedSourceMessageIdentifier != null && expectedIdentifier == null)
        {
            FailProjection(state, "projection.expected_identifier_invalid", "The expected source message identifier is invalid.");
            return;
        }
        if (actualIdentifier == null && expectedIdentifier != null)
        {
            actualIdentifier = expectedIdentifier;
            state.Result.Metadata.MessageIdentifier = expectedIdentifier;
        }
        if (actualNamespace.Length == 0 && state.Request.ExpectedSourceNamespaceUri.Length > 0)
        {
            actualNamespace = state.Request.ExpectedSourceNamespaceUri;
            state.Result.Metadata.NamespaceUri = actualNamespace;
        }
        if (actualIdentifier == null && actualNamespace.Length > 0)
        {
            NamespaceServiceResult parsed = new NamespaceService().ParseNamespace(actualNamespace, NamespaceMode.Strict);
            if (parsed.Success && parsed.Descriptor != null)
            {
                actualIdentifier = parsed.Descriptor.MessageIdentifier;
                state.Result.Metadata.MessageIdentifier = actualIdentifier;
            }
        }
        if (actualNamespace.Length == 0 && actualIdentifier != null)
        {
            NamespaceServiceResult built = new NamespaceService().BuildNamespaceFromIdentifier(actualIdentifier);
            if (built.Success && built.Descriptor != null)
            {
                actualNamespace = built.Descriptor.Uri;
                state.Result.Metadata.NamespaceUri = actualNamespace;
            }
        }
        if (actualIdentifier == null || actualNamespace.Length == 0)
        {
            FailProjection(state, "projection.source_identity_missing", "An exact source message identifier and namespace are required for projection.");
            return;
        }

        string actualText = MessageBuilder.GetExactIdentifierValue(actualIdentifier);
        string expectedText = expectedIdentifier == null ? string.Empty : expectedIdentifier.Value;
        if (expectedText.Length > 0 && actualText != expectedText)
        {
            FailProjection(state, "projection.source_identifier_mismatch", "The source message identifier does not match the expected exact identifier.");
            return;
        }
        if (state.Request.ExpectedSourceNamespaceUri.Length > 0 &&
            actualNamespace != state.Request.ExpectedSourceNamespaceUri)
        {
            FailProjection(state, "projection.source_namespace_mismatch", "The source namespace does not match the expected exact namespace.");
            return;
        }
        if (!SemanticTemplateRegistry.SupportsTarget(state.Definition, actualIdentifier))
        {
            FailProjection(state, "projection.source_family_mismatch", "The selected projection does not support the exact source message family.");
            return;
        }

        state.Result.SourceMessageIdentifier = actualIdentifier;
        state.Result.SourceNamespaceUri = actualNamespace;
    }

    internal static MessageIdentifier? NormalizeProjectionIdentifier(MessageIdentifier? identifier)
    {
        if (identifier == null)
        {
            return null;
        }
        string value = MessageBuilder.GetExactIdentifierValue(identifier);
        if (value.Length == 0)
        {
            return null;
        }
        return MessageIdentifierLexical.ParseMessageIdentifier(value.AsSpan());
    }

    internal static MetadataExtractionOptions CopyProjectionMetadataOptions(MetadataExtractionOptions source, ResourceLimits limits)
    {
        MetadataExtractionOptions copy = new MetadataExtractionOptions();
        copy.IncludeHeader = source.IncludeHeader;
        copy.IncludePayload = source.IncludePayload;
        copy.RedactSensitiveValues = source.RedactSensitiveValues;
        copy.Limits = limits;
        int index = 0;
        while (index < source.AdditionalReferencePaths.Count)
        {
            copy.AdditionalReferencePaths.Add(source.AdditionalReferencePaths[index]);
            index++;
        }
        return copy;
    }

    internal static bool ValidateProjectionDefinition(ProjectionExecutionState state)
    {
        MappingDefinition? mapping = state.Request.ProjectionDefinition;
        if (mapping == null)
        {
            return true;
        }
        if (mapping.Rules.Count > state.Request.Limits.MaxCollectionCount)
        {
            FailProjection(state, "projection.rule_limit", "The explicit projection definition exceeds the configured collection limit.");
            return false;
        }
        string expectedModelName = GetProjectionModelName(state.Definition);
        if (mapping.TargetModelName.Length > 0 && mapping.TargetModelName != expectedModelName)
        {
            FailProjection(state, "projection.definition_target_mismatch", "The explicit projection definition targets a different task-oriented view.");
            return false;
        }
        int index = 0;
        while (index < mapping.Rules.Count)
        {
            MappingRuleDefinition rule = mapping.Rules[index];
            if (rule.Sequence != index || rule.TargetMember.Length == 0 || rule.SourcePaths.Count == 0 ||
                rule.SourcePaths.Count > state.Request.Limits.MaxCollectionCount ||
                !ProjectionTargetMemberIsSupported(state.Definition, rule.TargetMember) ||
                (rule.Action != MappingActionKind.Copy && rule.Action != MappingActionKind.Convert))
            {
                FailProjection(state, "projection.rule_invalid", "Projection rules must be ordered copy or conversion rules with source paths and target members.");
                return false;
            }
            index++;
        }
        if (mapping.SourceMessageIdentifier != null && state.Result.SourceMessageIdentifier != null &&
            MessageBuilder.GetExactIdentifierValue(mapping.SourceMessageIdentifier) !=
            MessageBuilder.GetExactIdentifierValue(state.Result.SourceMessageIdentifier))
        {
            FailProjection(state, "projection.definition_source_mismatch", "The explicit projection definition targets a different source message identifier.");
            return false;
        }
        return true;
    }

    internal static bool ProjectionTargetMemberIsSupported(SemanticTemplateDefinition definition, string member)
    {
        switch (definition.Kind)
        {
            case SemanticTemplateRegistry.PaymentInitiation:
                return member == "MessageIdentifier" || member == "PaymentInformationIdentifier" ||
                    member == "EndToEndIdentifier" || member == "Debtor" || member == "DebtorAccount" ||
                    member == "Creditor" || member == "CreditorAccount" || member == "Amount" ||
                    member == "RequestedExecutionDate" || member == "RemittanceInformation";
            case SemanticTemplateRegistry.PaymentStatus:
                return member == "MessageIdentifier" || member == "OriginalMessageIdentifier" ||
                    member == "OriginalEndToEndIdentifier" || member == "Status" || member == "StatusReason";
            case SemanticTemplateRegistry.InterbankTransfer:
                return member == "MessageIdentifier" || member == "InstructionIdentifier" ||
                    member == "EndToEndIdentifier" || member == "TransactionIdentifier" || member == "Uetr" ||
                    member == "Debtor" || member == "DebtorAccount" || member == "Creditor" ||
                    member == "CreditorAccount" || member == "SettlementAmount" ||
                    member == "SettlementDate" || member == "RemittanceInformation";
            case SemanticTemplateRegistry.CashManagement:
                return member == "MessageIdentifier" || member == "ReportIdentifier" ||
                    member == "AccountIdentifier" || member == "Owner" || member == "BalanceAmount" ||
                    member == "BalanceDate";
            case SemanticTemplateRegistry.Securities:
                return member == "MessageIdentifier" || member == "TransactionIdentifier" ||
                    member == "SecurityIdentifier" || member == "AccountIdentifier" || member == "Quantity" ||
                    member == "SettlementAmount" || member == "SettlementDate" ||
                    member == "DeliveringParty" || member == "ReceivingParty";
            case SemanticTemplateRegistry.Card:
                return member == "MessageIdentifier" || member == "TransactionIdentifier" ||
                    member == "AcquirerIdentifier" || member == "MerchantIdentifier" ||
                    member == "MaskedCardIdentifier" || member == "Amount" || member == "TransactionDateTime";
            case SemanticTemplateRegistry.ForeignExchange:
                return member == "MessageIdentifier" || member == "TradeIdentifier" ||
                    member == "Counterparty" || member == "BoughtAmount" || member == "SoldAmount" ||
                    member == "ExchangeRate" || member == "TradeDate" || member == "SettlementDate";
            case SemanticTemplateRegistry.Trade:
                return member == "MessageIdentifier" || member == "TradeIdentifier" ||
                    member == "BaselineIdentifier" || member == "Buyer" || member == "Seller" ||
                    member == "Amount" || member == "DueDate" || member == "GoodsDescription";
            default:
                return false;
        }
    }

    internal static bool ValidateProjectionSource(ProjectionExecutionState state)
    {
        if (!state.Request.ValidateSource)
        {
            return true;
        }
        if (state.Request.ValidationPolicy == null || state.Request.ValidationContext == null)
        {
            FailProjection(state, "projection.validation_configuration_invalid", "Validation policy and context are required when source validation is selected.");
            return false;
        }
        IMessageValidator validator = state.Request.Validator ?? new MessageValidator();
        ValidationRequest request = new ValidationRequest();
        request.Document = state.Document;
        request.ExpectedIdentifier = state.Result.SourceMessageIdentifier;
        request.ExpectedNamespaceUri = state.Result.SourceNamespaceUri;
        request.Policy = state.Request.ValidationPolicy;
        request.Context = state.Request.ValidationContext;
        state.Result.SourceValidation = validator.ValidateMessage(request);
        CopyProjectionDiagnostics(state, state.Result.SourceValidation);
        if (!state.Result.SourceValidation.Success || !state.Result.SourceValidation.Valid)
        {
            FailProjection(state, "projection.source_validation_failed", "The source did not pass the requested validation.");
            return false;
        }
        return true;
    }

    internal static MappingDefinition CreateGeneratedProjectionDefinition(ProjectionExecutionState state)
    {
        MappingDefinition definition = new MappingDefinition();
        definition.Identifier = "iso20022.projection." + state.Definition.Identifier;
        definition.Version = "1";
        definition.Definition = "Generated version-neutral task-oriented projection.";
        definition.SourceMessageIdentifier = state.Result.SourceMessageIdentifier;
        definition.TargetModelName = GetProjectionModelName(state.Definition);
        definition.AllowUnmappedSource = true;
        definition.RequireAllTargets = false;
        return definition;
    }

    internal static string GetProjectionModelName(SemanticTemplateDefinition definition)
    {
        switch (definition.Kind)
        {
            case SemanticTemplateRegistry.PaymentInitiation:
                return "PaymentInitiationTemplate";
            case SemanticTemplateRegistry.PaymentStatus:
                return "PaymentStatusTemplate";
            case SemanticTemplateRegistry.InterbankTransfer:
                return "InterbankTransferTemplate";
            case SemanticTemplateRegistry.CashManagement:
                return "CashManagementTemplate";
            case SemanticTemplateRegistry.Securities:
                return "SecuritiesTemplate";
            case SemanticTemplateRegistry.Card:
                return "CardTemplate";
            case SemanticTemplateRegistry.ForeignExchange:
                return "ForeignExchangeTemplate";
            case SemanticTemplateRegistry.Trade:
                return "TradeTemplate";
            default:
                return string.Empty;
        }
    }

    internal static void IndexProjectionTree(ProjectionExecutionState state, DynamicXmlNode node, XmlPathData parentPath)
    {
        XmlPathData currentPath = parentPath;
        if (node.Kind == XmlNodeKind.Element)
        {
            currentPath = CopyProjectionPath(parentPath);
            QualifiedNameData segment = new QualifiedNameData();
            segment.LocalName = node.Name.LocalName;
            segment.NamespaceUri = node.Name.NamespaceUri;
            segment.Prefix = node.Name.Prefix;
            currentPath.Segments.Add(segment);
            currentPath.Value = parentPath.Value + "/" + node.Name.LocalName;
            ProjectionNodeMatch match = new ProjectionNodeMatch();
            match.Node = node;
            match.Path = currentPath;
            state.Matches.Add(match);
        }
        int index = 0;
        while (index < node.Children.Count)
        {
            IndexProjectionTree(state, node.Children[index], currentPath);
            index++;
        }
    }

    internal static XmlPathData CopyProjectionPath(XmlPathData source)
    {
        XmlPathData copy = new XmlPathData();
        copy.Value = source.Value;
        int index = 0;
        while (index < source.Segments.Count)
        {
            QualifiedNameData sourceSegment = source.Segments[index];
            QualifiedNameData segment = new QualifiedNameData();
            segment.LocalName = sourceSegment.LocalName;
            segment.NamespaceUri = sourceSegment.NamespaceUri;
            segment.Prefix = sourceSegment.Prefix;
            copy.Segments.Add(segment);
            index++;
        }
        return copy;
    }

    internal static object? CreateProjectedView(ProjectionExecutionState state)
    {
        switch (state.Definition.Kind)
        {
            case SemanticTemplateRegistry.PaymentInitiation:
                return ProjectPaymentInitiation(state);
            case SemanticTemplateRegistry.PaymentStatus:
                return ProjectPaymentStatus(state);
            case SemanticTemplateRegistry.InterbankTransfer:
                return ProjectInterbankTransfer(state);
            case SemanticTemplateRegistry.CashManagement:
                return ProjectCashManagement(state);
            case SemanticTemplateRegistry.Securities:
                return ProjectSecurities(state);
            case SemanticTemplateRegistry.Card:
                return ProjectCard(state);
            case SemanticTemplateRegistry.ForeignExchange:
                return ProjectForeignExchange(state);
            case SemanticTemplateRegistry.Trade:
                return ProjectTrade(state);
            default:
                return null;
        }
    }

    internal static PaymentInitiationTemplate ProjectPaymentInitiation(ProjectionExecutionState state)
    {
        PaymentInitiationTemplate view = new PaymentInitiationTemplate();
        view.MessageIdentifier = ProjectTextValue(state, "MessageIdentifier", "/GrpHdr/MsgId|/MsgId");
        view.PaymentInformationIdentifier = ProjectTextValue(state, "PaymentInformationIdentifier", "/PmtInf/PmtInfId|/PmtInfId");
        view.EndToEndIdentifier = ProjectTextValue(state, "EndToEndIdentifier", "/PmtId/EndToEndId|/EndToEndId");
        view.Debtor = ProjectTextValue(state, "Debtor", "/Dbtr/Nm");
        view.DebtorAccount = ProjectTextValue(state, "DebtorAccount", "/DbtrAcct/Id/IBAN|/DbtrAcct/Id/Othr/Id|/DbtrAcct/Id");
        view.Creditor = ProjectTextValue(state, "Creditor", "/Cdtr/Nm");
        view.CreditorAccount = ProjectTextValue(state, "CreditorAccount", "/CdtrAcct/Id/IBAN|/CdtrAcct/Id/Othr/Id|/CdtrAcct/Id");
        IsoAmountValue? amount = ProjectAmountValue(state, "Amount", "/Amt/InstdAmt|/InstdAmt");
        if (amount != null)
        {
            view.Amount = amount;
        }
        IsoDateValue? date = ProjectDateValue(state, "RequestedExecutionDate", "/PmtInf/ReqdExctnDt/Dt|/PmtInf/ReqdExctnDt|/ReqdExctnDt");
        if (date != null)
        {
            view.RequestedExecutionDate = date;
        }
        view.RemittanceInformation = ProjectTextValue(state, "RemittanceInformation", "/RmtInf/Ustrd|/Ustrd");
        return view;
    }

    internal static PaymentStatusTemplate ProjectPaymentStatus(ProjectionExecutionState state)
    {
        PaymentStatusTemplate view = new PaymentStatusTemplate();
        view.MessageIdentifier = ProjectTextValue(state, "MessageIdentifier", "/GrpHdr/MsgId|/MsgId");
        view.OriginalMessageIdentifier = ProjectTextValue(state, "OriginalMessageIdentifier", "/OrgnlGrpInfAndSts/OrgnlMsgId|/OrgnlMsgId");
        view.OriginalEndToEndIdentifier = ProjectTextValue(state, "OriginalEndToEndIdentifier", "/TxInfAndSts/OrgnlEndToEndId|/OrgnlEndToEndId");
        view.Status = ProjectTextValue(state, "Status", "/TxInfAndSts/TxSts|/OrgnlGrpInfAndSts/GrpSts|/TxSts|/GrpSts");
        view.StatusReason = ProjectTextValue(state, "StatusReason", "/StsRsnInf/AddtlInf|/StsRsn/Rsn/Cd|/AddtlInf");
        return view;
    }

    internal static InterbankTransferTemplate ProjectInterbankTransfer(ProjectionExecutionState state)
    {
        InterbankTransferTemplate view = new InterbankTransferTemplate();
        view.MessageIdentifier = ProjectTextValue(state, "MessageIdentifier", "/GrpHdr/MsgId|/MsgId");
        view.InstructionIdentifier = ProjectTextValue(state, "InstructionIdentifier", "/PmtId/InstrId|/InstrId");
        view.EndToEndIdentifier = ProjectTextValue(state, "EndToEndIdentifier", "/PmtId/EndToEndId|/EndToEndId");
        view.TransactionIdentifier = ProjectTextValue(state, "TransactionIdentifier", "/PmtId/TxId|/TxId");
        view.Uetr = ProjectTextValue(state, "Uetr", "/PmtId/UETR|/UETR");
        view.Debtor = ProjectTextValue(state, "Debtor", "/Dbtr/Nm");
        view.DebtorAccount = ProjectTextValue(state, "DebtorAccount", "/DbtrAcct/Id/IBAN|/DbtrAcct/Id/Othr/Id|/DbtrAcct/Id");
        view.Creditor = ProjectTextValue(state, "Creditor", "/Cdtr/Nm");
        view.CreditorAccount = ProjectTextValue(state, "CreditorAccount", "/CdtrAcct/Id/IBAN|/CdtrAcct/Id/Othr/Id|/CdtrAcct/Id");
        IsoAmountValue? amount = ProjectAmountValue(state, "SettlementAmount", "/CdtTrfTxInf/IntrBkSttlmAmt|/IntrBkSttlmAmt|/Amt/InstdAmt");
        if (amount != null)
        {
            view.SettlementAmount = amount;
        }
        IsoDateValue? date = ProjectDateValue(state, "SettlementDate", "/GrpHdr/IntrBkSttlmDt|/IntrBkSttlmDt|/SttlmDt");
        if (date != null)
        {
            view.SettlementDate = date;
        }
        view.RemittanceInformation = ProjectTextValue(state, "RemittanceInformation", "/RmtInf/Ustrd|/Ustrd");
        return view;
    }

    internal static CashManagementTemplate ProjectCashManagement(ProjectionExecutionState state)
    {
        CashManagementTemplate view = new CashManagementTemplate();
        view.MessageIdentifier = ProjectTextValue(state, "MessageIdentifier", "/GrpHdr/MsgId|/MsgId");
        view.ReportIdentifier = ProjectTextValue(state, "ReportIdentifier", "/Stmt/Id|/Rpt/Id|/Ntfctn/Id");
        view.AccountIdentifier = ProjectTextValue(state, "AccountIdentifier", "/Acct/Id/IBAN|/Acct/Id/Othr/Id|/Acct/Id");
        view.Owner = ProjectTextValue(state, "Owner", "/Acct/Ownr/Nm|/Ownr/Nm");
        IsoAmountValue? amount = ProjectAmountValue(state, "BalanceAmount", "/Bal/Amt");
        if (amount != null)
        {
            view.BalanceAmount = amount;
        }
        IsoDateValue? date = ProjectDateValue(state, "BalanceDate", "/Bal/Dt/Dt|/Bal/Dt|/ValDt/Dt|/ValDt");
        if (date != null)
        {
            view.BalanceDate = date;
        }
        return view;
    }

    internal static SecuritiesTemplate ProjectSecurities(ProjectionExecutionState state)
    {
        SecuritiesTemplate view = new SecuritiesTemplate();
        view.MessageIdentifier = ProjectTextValue(state, "MessageIdentifier", "/MsgId|/Ref");
        view.TransactionIdentifier = ProjectTextValue(state, "TransactionIdentifier", "/Tx/TxId|/TxId|/TradId");
        view.SecurityIdentifier = ProjectTextValue(state, "SecurityIdentifier", "/FinInstrmId/ISIN|/ISIN|/OthrId/Id");
        view.AccountIdentifier = ProjectTextValue(state, "AccountIdentifier", "/SfkpgAcct|/Acct/Id|/SctiesAcct/Id");
        IsoDecimalValue? quantity = ProjectDecimalValue(state, "Quantity", "/Tx/Qty|/Qty|/SttlmQty");
        if (quantity != null)
        {
            view.Quantity = quantity;
        }
        IsoAmountValue? amount = ProjectAmountValue(state, "SettlementAmount", "/Tx/SttlmAmt|/SttlmAmt|/Amt");
        if (amount != null)
        {
            view.SettlementAmount = amount;
        }
        IsoDateValue? date = ProjectDateValue(state, "SettlementDate", "/Tx/SttlmDt|/SttlmDt|/TradDt");
        if (date != null)
        {
            view.SettlementDate = date;
        }
        view.DeliveringParty = ProjectTextValue(state, "DeliveringParty", "/DlvrgPty/Nm|/DlvrgPty");
        view.ReceivingParty = ProjectTextValue(state, "ReceivingParty", "/RcvgPty/Nm|/RcvgPty");
        return view;
    }

    internal static CardTemplate ProjectCard(ProjectionExecutionState state)
    {
        CardTemplate view = new CardTemplate();
        view.MessageIdentifier = ProjectTextValue(state, "MessageIdentifier", "/MsgId");
        view.TransactionIdentifier = ProjectTextValue(state, "TransactionIdentifier", "/Tx/TxId|/TxId|/TxId/Id");
        view.AcquirerIdentifier = ProjectTextValue(state, "AcquirerIdentifier", "/AcqrrId|/Acqrr/Id");
        view.MerchantIdentifier = ProjectTextValue(state, "MerchantIdentifier", "/MrchntId|/Mrchnt/Id");
        view.MaskedCardIdentifier = ProjectTextValue(state, "MaskedCardIdentifier", "/MskdPAN|/CardData/PAN|/PAN");
        IsoAmountValue? amount = ProjectAmountValue(state, "Amount", "/Tx/TxAmt|/TxAmt|/TxDtls/Amt|/Amt");
        if (amount != null)
        {
            view.Amount = amount;
        }
        IsoDateTimeValue? dateTime = ProjectDateTimeValue(state, "TransactionDateTime", "/Tx/TxDtTm|/TxDtTm|/TxDtls/TxDtTm");
        if (dateTime != null)
        {
            view.TransactionDateTime = dateTime;
        }
        return view;
    }

    internal static ForeignExchangeTemplate ProjectForeignExchange(ProjectionExecutionState state)
    {
        ForeignExchangeTemplate view = new ForeignExchangeTemplate();
        view.MessageIdentifier = ProjectTextValue(state, "MessageIdentifier", "/MsgId|/TradInf/MsgId");
        view.TradeIdentifier = ProjectTextValue(state, "TradeIdentifier", "/Trad/TradId|/TradId|/TradRef");
        view.Counterparty = ProjectTextValue(state, "Counterparty", "/Trad/CtrPty|/CtrPty/Nm|/CtrPty");
        IsoAmountValue? bought = ProjectAmountValue(state, "BoughtAmount", "/Trad/BuyAmt|/BuyAmt|/TradAmt/BghtAmt");
        if (bought != null)
        {
            view.BoughtAmount = bought;
        }
        IsoAmountValue? sold = ProjectAmountValue(state, "SoldAmount", "/Trad/SellAmt|/SellAmt|/TradAmt/SldAmt");
        if (sold != null)
        {
            view.SoldAmount = sold;
        }
        IsoDecimalValue? rate = ProjectDecimalValue(state, "ExchangeRate", "/Trad/XchgRate|/XchgRate|/Rate");
        if (rate != null)
        {
            view.ExchangeRate = rate;
        }
        IsoDateValue? tradeDate = ProjectDateValue(state, "TradeDate", "/Trad/TradDt|/TradDt");
        if (tradeDate != null)
        {
            view.TradeDate = tradeDate;
        }
        IsoDateValue? settlementDate = ProjectDateValue(state, "SettlementDate", "/Trad/SttlmDt|/SttlmDt");
        if (settlementDate != null)
        {
            view.SettlementDate = settlementDate;
        }
        return view;
    }

    internal static TradeTemplate ProjectTrade(ProjectionExecutionState state)
    {
        TradeTemplate view = new TradeTemplate();
        view.MessageIdentifier = ProjectTextValue(state, "MessageIdentifier", "/MsgId|/AckId/Id");
        view.TradeIdentifier = ProjectTextValue(state, "TradeIdentifier", "/Trad/TradId|/TradId|/TxId");
        view.BaselineIdentifier = ProjectTextValue(state, "BaselineIdentifier", "/Trad/BaselnId|/BaselnId/Id|/BaselnId");
        view.Buyer = ProjectTextValue(state, "Buyer", "/Trad/Buyr/Nm|/Buyr/Nm|/Buyr");
        view.Seller = ProjectTextValue(state, "Seller", "/Trad/Sellr/Nm|/Sellr/Nm|/Sellr");
        IsoAmountValue? amount = ProjectAmountValue(state, "Amount", "/Trad/Amt|/BuyrBkToSellrBkInf/Amt|/Amt");
        if (amount != null)
        {
            view.Amount = amount;
        }
        IsoDateValue? date = ProjectDateValue(state, "DueDate", "/Trad/DueDt|/DueDt");
        if (date != null)
        {
            view.DueDate = date;
        }
        view.GoodsDescription = ProjectTextValue(state, "GoodsDescription", "/Trad/GoodsDesc|/Goods/Desc|/GoodsDesc");
        return view;
    }

    internal static string ProjectTextValue(ProjectionExecutionState state, string targetMember, string aliases)
    {
        ProjectionNodeMatch? match = FindProjectedNode(state, targetMember, aliases);
        if (match == null)
        {
            return string.Empty;
        }
        return GetProjectionElementText(match.Node);
    }

    internal static IsoAmountValue? ProjectAmountValue(ProjectionExecutionState state, string targetMember, string aliases)
    {
        ProjectionNodeMatch? match = FindProjectedNode(state, targetMember, aliases);
        if (match == null)
        {
            return null;
        }
        string text = GetProjectionElementText(match.Node);
        IsoDecimalValue? decimalValue = DecimalLexical.ParseDecimal(text.AsSpan());
        string currency = GetProjectionAttribute(match.Node, "Ccy");
        if (decimalValue == null || currency.Length == 0)
        {
            FailProjection(state, "projection.amount_invalid", "A projected amount must contain a valid decimal and currency attribute.");
            return null;
        }
        IsoAmountValue amount = new IsoAmountValue();
        amount.Amount = decimalValue;
        amount.Currency = currency;
        return amount;
    }

    internal static IsoDecimalValue? ProjectDecimalValue(ProjectionExecutionState state, string targetMember, string aliases)
    {
        ProjectionNodeMatch? match = FindProjectedNode(state, targetMember, aliases);
        if (match == null)
        {
            return null;
        }
        IsoDecimalValue? value = DecimalLexical.ParseDecimal(GetProjectionElementText(match.Node).AsSpan());
        if (value == null)
        {
            FailProjection(state, "projection.decimal_invalid", "A projected decimal value has an invalid lexical form.");
        }
        return value;
    }

    internal static IsoDateValue? ProjectDateValue(ProjectionExecutionState state, string targetMember, string aliases)
    {
        ProjectionNodeMatch? match = FindProjectedNode(state, targetMember, aliases);
        if (match == null)
        {
            return null;
        }
        IsoDateValue? value = DateTimeLexical.ParseDate(GetProjectionElementText(match.Node).AsSpan());
        if (value == null)
        {
            FailProjection(state, "projection.date_invalid", "A projected date value has an invalid ISO lexical form.");
        }
        return value;
    }

    internal static IsoDateTimeValue? ProjectDateTimeValue(ProjectionExecutionState state, string targetMember, string aliases)
    {
        ProjectionNodeMatch? match = FindProjectedNode(state, targetMember, aliases);
        if (match == null)
        {
            return null;
        }
        IsoDateTimeValue? value = DateTimeLexical.ParseDateTime(GetProjectionElementText(match.Node).AsSpan());
        if (value == null)
        {
            FailProjection(state, "projection.datetime_invalid", "A projected date-time value has an invalid ISO lexical form.");
        }
        return value;
    }

    internal static ProjectionNodeMatch? FindProjectedNode(ProjectionExecutionState state, string targetMember, string aliases)
    {
        MappingDefinition? explicitDefinition = state.Request.ProjectionDefinition;
        if (explicitDefinition != null)
        {
            int ruleIndex = 0;
            while (ruleIndex < explicitDefinition.Rules.Count)
            {
                MappingRuleDefinition rule = explicitDefinition.Rules[ruleIndex];
                if (rule.TargetMember == targetMember)
                {
                    int pathIndex = 0;
                    while (pathIndex < rule.SourcePaths.Count)
                    {
                        ProjectionNodeMatch? explicitMatch = FindProjectionPath(state, rule.SourcePaths[pathIndex].Value, true);
                        if (explicitMatch != null)
                        {
                            RecordProjectionMatch(state, explicitMatch, targetMember, rule);
                            return explicitMatch;
                        }
                        pathIndex++;
                    }
                    if (rule.Required && state.Result.ErrorCode.Length == 0)
                    {
                        FailProjection(state, "projection.required_source_missing", "A required explicit projection source path is absent.");
                    }
                    return null;
                }
                ruleIndex++;
            }
            return null;
        }

        string[] candidates = aliases.Split('|');
        int candidateIndex = 0;
        while (candidateIndex < candidates.Length)
        {
            ProjectionNodeMatch? match = FindProjectionPath(state, candidates[candidateIndex], false);
            if (match != null)
            {
                RecordProjectionMatch(state, match, targetMember, null);
                return match;
            }
            candidateIndex++;
        }
        return null;
    }

    internal static ProjectionNodeMatch? FindProjectionPath(ProjectionExecutionState state, string path, bool exact)
    {
        int index = 0;
        while (index < state.Matches.Count)
        {
            ProjectionNodeMatch match = state.Matches[index];
            bool selected = exact ? match.Path.Value == path :
                match.Path.Value.EndsWith(path, StringComparison.Ordinal);
            if (selected && !state.ConsumedNodes.Contains(match.Node))
            {
                return match;
            }
            index++;
        }
        return null;
    }

    internal static void RecordProjectionMatch(ProjectionExecutionState state, ProjectionNodeMatch match, string targetMember, MappingRuleDefinition? explicitRule)
    {
        state.ConsumedNodes.Add(match.Node);
        AddProjectionSourcePath(state.Result, match.Path);
        MappingRuleDefinition rule;
        if (explicitRule != null)
        {
            rule = explicitRule;
        }
        else
        {
            rule = new MappingRuleDefinition();
            rule.Identifier = state.Result.ProjectionIdentifier + "." + targetMember;
            MappingDefinition? generated = state.Result.ProjectionDefinition;
            rule.Sequence = generated == null ? 0 : generated.Rules.Count;
            rule.Action = MappingActionKind.Copy;
            rule.TargetMember = targetMember;
            rule.SourcePaths.Add(CopyProjectionPath(match.Path));
            if (generated != null)
            {
                generated.Rules.Add(rule);
            }
        }
        if (!state.Result.AppliedRules.Contains(rule))
        {
            state.Result.AppliedRules.Add(rule);
        }
    }

    internal static void AddProjectionSourcePath(ProjectionResult result, XmlPathData path)
    {
        int index = 0;
        while (index < result.SourcePaths.Count)
        {
            if (result.SourcePaths[index].Value == path.Value)
            {
                return;
            }
            index++;
        }
        result.SourcePaths.Add(CopyProjectionPath(path));
    }

    internal static string GetProjectionElementText(DynamicXmlNode node)
    {
        string value = string.Empty;
        int index = 0;
        while (index < node.Children.Count)
        {
            DynamicXmlNode child = node.Children[index];
            if (child.Kind == XmlNodeKind.Text || child.Kind == XmlNodeKind.CData)
            {
                value += child.Value;
            }
            index++;
        }
        return value;
    }

    internal static string GetProjectionAttribute(DynamicXmlNode node, string localName)
    {
        int index = 0;
        while (index < node.Attributes.Count)
        {
            if (node.Attributes[index].Name.LocalName == localName &&
                node.Attributes[index].Name.NamespaceUri.Length == 0)
            {
                return node.Attributes[index].Value;
            }
            index++;
        }
        return string.Empty;
    }

    internal static void CollectUnmappedProjectionContent(ProjectionExecutionState state, DynamicXmlNode node)
    {
        if (node.Kind == XmlNodeKind.Document)
        {
            int documentIndex = 0;
            while (documentIndex < node.Children.Count)
            {
                CollectUnmappedProjectionContent(state, node.Children[documentIndex]);
                documentIndex++;
            }
            return;
        }
        if (node.Kind != XmlNodeKind.Element || state.ConsumedNodes.Contains(node))
        {
            return;
        }
        if (!HasConsumedProjectionDescendant(state, node))
        {
            CaptureUnmappedProjectionNode(state, node);
            return;
        }
        int index = 0;
        while (index < node.Children.Count)
        {
            CollectUnmappedProjectionContent(state, node.Children[index]);
            if (state.Result.ErrorCode.Length > 0)
            {
                return;
            }
            index++;
        }
    }

    internal static bool HasConsumedProjectionDescendant(ProjectionExecutionState state, DynamicXmlNode node)
    {
        int index = 0;
        while (index < node.Children.Count)
        {
            DynamicXmlNode child = node.Children[index];
            if (state.ConsumedNodes.Contains(child) || HasConsumedProjectionDescendant(state, child))
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static void CaptureUnmappedProjectionNode(ProjectionExecutionState state, DynamicXmlNode node)
    {
        if (state.Result.UnmappedContent.Count >= state.Request.Limits.MaxCollectionCount ||
            state.Result.UnmappedSourcePaths.Count >= state.Request.Limits.MaxCollectionCount)
        {
            FailProjection(state, "projection.unmapped_limit", "Unmapped source content exceeds the configured collection limit.");
            return;
        }
        ProjectionNodeMatch? match = FindProjectionNodeReference(state, node);
        if (match == null)
        {
            return;
        }
        DynamicXmlNode copy = CanonicalXmlNodeNormalizer.CloneNode(node);
        MessageWriteOptions options = MigrationExecutor.CreateWriteOptions(state.Request.Limits);
        options.WriteXmlDeclaration = false;
        string xml = new GenericXmlNodeWriter(options).WriteText(copy);
        RawXmlFragment fragment = new RawXmlFragment();
        fragment.Xml = xml;
        fragment.Utf8Bytes = Encoding.UTF8.GetBytes(xml);
        fragment.RootName = CopyProjectionName(node.Name);
        fragment.Location = node.Location;
        int declarationIndex = 0;
        while (declarationIndex < node.NamespaceDeclarations.Count)
        {
            NamespaceDeclarationData sourceDeclaration = node.NamespaceDeclarations[declarationIndex];
            NamespaceDeclarationData declaration = new NamespaceDeclarationData();
            declaration.Prefix = sourceDeclaration.Prefix;
            declaration.NamespaceUri = sourceDeclaration.NamespaceUri;
            fragment.NamespaceDeclarations.Add(declaration);
            declarationIndex++;
        }
        state.Result.UnmappedSourcePaths.Add(CopyProjectionPath(match.Path));
        state.Result.UnmappedContent.Add(fragment);
    }

    internal static ProjectionNodeMatch? FindProjectionNodeReference(ProjectionExecutionState state, DynamicXmlNode node)
    {
        int index = 0;
        while (index < state.Matches.Count)
        {
            if (state.Matches[index].Node == node)
            {
                return state.Matches[index];
            }
            index++;
        }
        return null;
    }

    internal static QualifiedNameData CopyProjectionName(QualifiedNameData source)
    {
        QualifiedNameData copy = new QualifiedNameData();
        copy.LocalName = source.LocalName;
        copy.NamespaceUri = source.NamespaceUri;
        copy.Prefix = source.Prefix;
        return copy;
    }

    internal static ProjectionResult CreateProjectionFailure(string code, string message, string projectionIdentifier)
    {
        ProjectionResult result = new ProjectionResult();
        result.ProjectionIdentifier = projectionIdentifier;
        result.ErrorCode = code;
        result.ErrorMessage = message;
        AddProjectionDiagnostic(result, code, message);
        return result;
    }

    internal static ProjectionResult FailProjection(ProjectionExecutionState state, string code, string message)
    {
        state.Result.Success = false;
        state.Result.ErrorCode = code;
        state.Result.ErrorMessage = message;
        AddProjectionDiagnostic(state.Result, code, message);
        return state.Result;
    }

    internal static void AddProjectionDiagnostic(ProjectionResult result, string code, string message)
    {
        if (result.Diagnostics.Count > 0 && result.Diagnostics[result.Diagnostics.Count - 1].Code == code)
        {
            return;
        }
        ValidationDiagnostic diagnostic = new ValidationDiagnostic();
        diagnostic.Code = code;
        diagnostic.RuleIdentifier = code;
        diagnostic.Severity = ValidationSeverity.Error;
        diagnostic.Stage = ValidationStage.Semantic;
        diagnostic.Kind = DiagnosticKind.Configuration;
        diagnostic.Message = message;
        result.Diagnostics.Add(diagnostic);
    }

    internal static void CopyProjectionDiagnostics(ProjectionExecutionState state, ValidationReport report)
    {
        int index = 0;
        while (index < report.Diagnostics.Count && state.Result.Diagnostics.Count < state.Request.Limits.MaxDiagnosticCount)
        {
            state.Result.Diagnostics.Add(report.Diagnostics[index]);
            index++;
        }
    }
}
