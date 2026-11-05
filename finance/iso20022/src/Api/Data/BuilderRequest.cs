using System.Threading;

namespace iso20022;

/// <summary>Requests construction of one exact ISO 20022 message from a semantic template input.</summary>
public class BuilderRequest
{
    /// <summary>The stable semantic-template identifier selected by the caller.</summary>
    public string TemplateIdentifier = string.Empty;
    /// <summary>The semantic template input model.</summary>
    public object? SemanticInput;
    /// <summary>The typed payment-initiation input, when selected without <see cref="SemanticInput"/>.</summary>
    public PaymentInitiationTemplate? PaymentInitiation;
    /// <summary>The typed payment-status input, when selected without <see cref="SemanticInput"/>.</summary>
    public PaymentStatusTemplate? PaymentStatus;
    /// <summary>The typed interbank-transfer input, when selected without <see cref="SemanticInput"/>.</summary>
    public InterbankTransferTemplate? InterbankTransfer;
    /// <summary>The typed cash-management input, when selected without <see cref="SemanticInput"/>.</summary>
    public CashManagementTemplate? CashManagement;
    /// <summary>The typed securities input, when selected without <see cref="SemanticInput"/>.</summary>
    public SecuritiesTemplate? Securities;
    /// <summary>The typed card input, when selected without <see cref="SemanticInput"/>.</summary>
    public CardTemplate? Card;
    /// <summary>The typed foreign-exchange input, when selected without <see cref="SemanticInput"/>.</summary>
    public ForeignExchangeTemplate? ForeignExchange;
    /// <summary>The typed trade-services input, when selected without <see cref="SemanticInput"/>.</summary>
    public TradeTemplate? Trade;
    /// <summary>The repository snapshot that supplies the exact target definition when required.</summary>
    public RepositorySnapshot? Repository;
    /// <summary>The exact target message identifier.</summary>
    public MessageIdentifier? TargetMessageIdentifier;
    /// <summary>The exact target message namespace URI.</summary>
    public string TargetNamespaceUri = string.Empty;
    /// <summary>The optional business application header to attach.</summary>
    public MessageHeader? Header;
    /// <summary>The requested physical message layout.</summary>
    public EnvelopeKind Layout = EnvelopeKind.StandaloneDocument;
    /// <summary>Whether the constructed target must be validated before success is returned.</summary>
    public bool ValidateResult = true;
    /// <summary>The validator used for optional target validation.</summary>
    public IMessageValidator? Validator;
    /// <summary>The exact target validation stage and policy selection.</summary>
    public ValidationPolicy ValidationPolicy = new ValidationPolicy();
    /// <summary>The caller context supplied to target validation.</summary>
    public ValidationContext ValidationContext = new ValidationContext();
    /// <summary>The XML writing policy used when target bytes are requested.</summary>
    public MessageWriteOptions WriteOptions = new MessageWriteOptions();
    /// <summary>The resource bounds applied during construction, validation, and writing.</summary>
    public ResourceLimits Limits = new ResourceLimits();
    /// <summary>The cancellation request observed between bounded builder stages.</summary>
    public CancellationToken CancellationToken;
}
