namespace diameter;

/// <summary>
/// Registers, composes, snapshots, and inspects Diameter dictionary definitions.
/// </summary>
public interface IDiameterDictionary
{
    /// <summary>Registers one dictionary module transactionally.</summary>
    /// <param name="module">The module to register.</param>
    /// <returns>The registration outcome and active snapshot.</returns>
    DiameterDictionaryRegistrationResult RegisterDictionaryModule(
        DiameterDictionaryModule? module);

    /// <summary>Reads an independent snapshot of the composed dictionary.</summary>
    /// <returns>The current dictionary snapshot.</returns>
    DiameterDictionarySnapshot ReadDictionarySnapshot();

    /// <summary>Finds a module by canonical name.</summary>
    /// <param name="name">The canonical module name.</param>
    /// <returns>An independent definition, or <c>null</c> when absent.</returns>
    DiameterDictionaryModule? FindDictionaryModuleByName(string? name);

    /// <summary>Finds a vendor by Vendor-ID.</summary>
    /// <param name="vendorId">The Vendor-ID.</param>
    /// <returns>An independent definition, or <c>null</c> when absent.</returns>
    DiameterVendorDefinition? FindDictionaryVendorById(uint vendorId);

    /// <summary>Finds a vendor by canonical name.</summary>
    /// <param name="name">The canonical vendor name.</param>
    /// <returns>An independent definition, or <c>null</c> when absent.</returns>
    DiameterVendorDefinition? FindDictionaryVendorByName(string? name);

    /// <summary>Finds an application by vendor and application identifiers.</summary>
    /// <param name="vendorId">The defining Vendor-ID.</param>
    /// <param name="applicationId">The application identifier.</param>
    /// <returns>An independent definition, or <c>null</c> when absent.</returns>
    DiameterApplicationDefinition? FindDictionaryApplicationById(
        uint vendorId,
        uint applicationId);

    /// <summary>Finds an application by vendor and canonical name.</summary>
    /// <param name="vendorId">The defining Vendor-ID.</param>
    /// <param name="name">The canonical application name.</param>
    /// <returns>An independent definition, or <c>null</c> when absent.</returns>
    DiameterApplicationDefinition? FindDictionaryApplicationByName(
        uint vendorId,
        string? name);

    /// <summary>Finds a value type by vendor and canonical name.</summary>
    /// <param name="vendorId">The defining Vendor-ID.</param>
    /// <param name="name">The canonical type name.</param>
    /// <returns>An independent definition, or <c>null</c> when absent.</returns>
    DiameterTypeDefinition? FindDictionaryTypeByName(uint vendorId, string? name);

    /// <summary>Finds an enumerated definition by type and numeric value.</summary>
    /// <param name="typeVendorId">The type Vendor-ID.</param>
    /// <param name="typeName">The canonical type name.</param>
    /// <param name="value">The enumerated value.</param>
    /// <returns>An independent definition, or <c>null</c> when absent.</returns>
    DiameterEnumValueDefinition? FindDictionaryEnumValueByTypeNumber(
        uint typeVendorId,
        string? typeName,
        int value);

    /// <summary>Finds an enumerated definition by type and value name.</summary>
    /// <param name="typeVendorId">The type Vendor-ID.</param>
    /// <param name="typeName">The canonical type name.</param>
    /// <param name="valueName">The canonical enumerated value name.</param>
    /// <returns>An independent definition, or <c>null</c> when absent.</returns>
    DiameterEnumValueDefinition? FindDictionaryEnumValueByTypeName(
        uint typeVendorId,
        string? typeName,
        string? valueName);

    /// <summary>Finds an enumerated definition by AVP and numeric value.</summary>
    /// <param name="avpVendorId">The AVP Vendor-ID.</param>
    /// <param name="avpCode">The AVP code.</param>
    /// <param name="value">The enumerated value.</param>
    /// <returns>An independent definition, or <c>null</c> when absent.</returns>
    DiameterEnumValueDefinition? FindDictionaryEnumValueByAvpNumber(
        uint avpVendorId,
        uint avpCode,
        int value);

    /// <summary>Finds an enumerated definition by AVP and value name.</summary>
    /// <param name="avpVendorId">The AVP Vendor-ID.</param>
    /// <param name="avpCode">The AVP code.</param>
    /// <param name="valueName">The canonical enumerated value name.</param>
    /// <returns>An independent definition, or <c>null</c> when absent.</returns>
    DiameterEnumValueDefinition? FindDictionaryEnumValueByAvpName(
        uint avpVendorId,
        uint avpCode,
        string? valueName);

    /// <summary>Finds an AVP by vendor and code.</summary>
    /// <param name="vendorId">The AVP Vendor-ID.</param>
    /// <param name="avpCode">The AVP code.</param>
    /// <returns>An independent definition, or <c>null</c> when absent.</returns>
    DiameterAvpDefinition? FindDictionaryAvpByCode(uint vendorId, uint avpCode);

    /// <summary>Finds an AVP by vendor and canonical name.</summary>
    /// <param name="vendorId">The AVP Vendor-ID.</param>
    /// <param name="name">The canonical AVP name.</param>
    /// <returns>An independent definition, or <c>null</c> when absent.</returns>
    DiameterAvpDefinition? FindDictionaryAvpByName(uint vendorId, string? name);

    /// <summary>Finds a command by application, code, and direction.</summary>
    /// <param name="applicationId">The application identifier.</param>
    /// <param name="commandCode">The command code.</param>
    /// <param name="request">Indicates the request form.</param>
    /// <returns>An independent definition, or <c>null</c> when absent.</returns>
    DiameterCommandDefinition? FindDictionaryCommandByCode(
        uint applicationId,
        uint commandCode,
        bool request);

    /// <summary>Finds a command by application, canonical name, and direction.</summary>
    /// <param name="applicationId">The application identifier.</param>
    /// <param name="name">The canonical command name.</param>
    /// <param name="request">Indicates the request form.</param>
    /// <returns>An independent definition, or <c>null</c> when absent.</returns>
    DiameterCommandDefinition? FindDictionaryCommandByName(
        uint applicationId,
        string? name,
        bool request);

    /// <summary>Finds a Grouped AVP occurrence rule by parent and child codes.</summary>
    /// <param name="parentVendorId">The parent AVP Vendor-ID.</param>
    /// <param name="parentAvpCode">The parent AVP code.</param>
    /// <param name="childVendorId">The child AVP Vendor-ID.</param>
    /// <param name="childAvpCode">The child AVP code.</param>
    /// <returns>An independent definition, or <c>null</c> when absent.</returns>
    DiameterAvpRuleDefinition? FindDictionaryGroupedRuleByCode(
        uint parentVendorId,
        uint parentAvpCode,
        uint childVendorId,
        uint childAvpCode);

    /// <summary>Finds a Grouped AVP occurrence rule by parent and child names.</summary>
    /// <param name="parentVendorId">The parent AVP Vendor-ID.</param>
    /// <param name="parentName">The canonical parent AVP name.</param>
    /// <param name="childVendorId">The child AVP Vendor-ID.</param>
    /// <param name="childName">The canonical child AVP name.</param>
    /// <returns>An independent definition, or <c>null</c> when absent.</returns>
    DiameterAvpRuleDefinition? FindDictionaryGroupedRuleByName(
        uint parentVendorId,
        string? parentName,
        uint childVendorId,
        string? childName);

    /// <summary>Finds a command occurrence rule by command and AVP codes.</summary>
    /// <param name="applicationId">The command application identifier.</param>
    /// <param name="commandCode">The command code.</param>
    /// <param name="request">Indicates the request form.</param>
    /// <param name="avpVendorId">The rule AVP Vendor-ID.</param>
    /// <param name="avpCode">The rule AVP code.</param>
    /// <returns>An independent definition, or <c>null</c> when absent.</returns>
    DiameterAvpRuleDefinition? FindDictionaryCommandRuleByCode(
        uint applicationId,
        uint commandCode,
        bool request,
        uint avpVendorId,
        uint avpCode);

    /// <summary>Finds a command occurrence rule by command and AVP names.</summary>
    /// <param name="applicationId">The command application identifier.</param>
    /// <param name="commandName">The canonical command name.</param>
    /// <param name="request">Indicates the request form.</param>
    /// <param name="avpVendorId">The rule AVP Vendor-ID.</param>
    /// <param name="avpName">The canonical rule AVP name.</param>
    /// <returns>An independent definition, or <c>null</c> when absent.</returns>
    DiameterAvpRuleDefinition? FindDictionaryCommandRuleByName(
        uint applicationId,
        string? commandName,
        bool request,
        uint avpVendorId,
        string? avpName);
}

/// <summary>
/// Loads versioned Diameter dictionary modules from JSON.
/// </summary>
public interface IDiameterDictionaryLoader
{
    /// <summary>Loads and validates one JSON dictionary module.</summary>
    /// <param name="json">The versioned dictionary JSON document.</param>
    /// <returns>The loaded dictionary module.</returns>
    /// <exception cref="DiameterDictionaryException">The document is invalid.</exception>
    DiameterDictionaryModule LoadDictionaryModule(string? json);
}

/// <summary>
/// Validates Diameter messages and AVPs and constructs validation error answers.
/// </summary>
public interface IDiameterValidator
{
    /// <summary>Classifies a Diameter Result-Code by its thousands digit.</summary>
    /// <param name="resultCode">The Result-Code to classify.</param>
    /// <returns>The RFC 6733 result class, or permanent failure for an unknown class.</returns>
    DiameterResultClass ClassifyResultCode(uint resultCode);

    /// <summary>Validates a message against a dictionary snapshot and limits.</summary>
    /// <param name="message">The message to validate.</param>
    /// <param name="snapshot">The dictionary snapshot.</param>
    /// <param name="limits">The validation limits.</param>
    /// <returns>The structured validation outcome.</returns>
    DiameterValidationResult ValidateMessage(
        DiameterMessage? message,
        DiameterDictionarySnapshot? snapshot,
        DiameterLimitConfiguration? limits);

    /// <summary>Validates one AVP, including its Grouped children when applicable.</summary>
    /// <param name="avp">The AVP to validate.</param>
    /// <param name="snapshot">The dictionary snapshot.</param>
    /// <param name="limits">The validation limits.</param>
    /// <returns>The structured validation outcome.</returns>
    DiameterValidationResult ValidateGroupedAvp(
        DiameterAvp? avp,
        DiameterDictionarySnapshot? snapshot,
        DiameterLimitConfiguration? limits);

    /// <summary>Builds a Diameter answer for a structured validation failure.</summary>
    /// <param name="request">The validation error-answer input.</param>
    /// <param name="originHost">The answering node identity.</param>
    /// <param name="originRealm">The answering node realm.</param>
    /// <returns>The protocol error answer.</returns>
    /// <exception cref="DiameterValidationException">Required input is absent or invalid.</exception>
    DiameterMessage BuildValidationErrorAnswer(
        DiameterErrorAnswerRequest? request,
        DiameterIdentity? originHost,
        DiameterIdentity? originRealm);

    /// <summary>Builds a Diameter answer carrying a protocol, transient, or permanent failure.</summary>
    /// <param name="request">The failed request and error values.</param>
    /// <param name="originHost">The answering node identity.</param>
    /// <param name="originRealm">The answering node realm.</param>
    /// <returns>The error answer.</returns>
    /// <exception cref="DiameterValidationException">Required input is absent or invalid.</exception>
    DiameterMessage BuildResultErrorAnswer(
        DiameterErrorAnswerRequest? request,
        DiameterIdentity? originHost,
        DiameterIdentity? originRealm);
}
