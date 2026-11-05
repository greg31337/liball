namespace diameter;

/// <summary>
/// Provides a composed Diameter dictionary initialized with the built-in RFC modules.
/// </summary>
public class DiameterDictionary : IDiameterDictionary
{
    internal DictionaryBuilder Builder = new DictionaryBuilder();

    /// <summary>Initializes a dictionary and registers all built-in RFC modules.</summary>
    public DiameterDictionary()
    {
        RegisterBuiltInModules();
    }

    /// <summary>Registers one dictionary module transactionally.</summary>
    /// <param name="module">The module to register.</param>
    /// <returns>The registration outcome and active snapshot.</returns>
    public DiameterDictionaryRegistrationResult RegisterDictionaryModule(
        DiameterDictionaryModule? module)
    {
        return CreatePublicRegistrationResult(Builder.RegisterDictionaryModule(module));
    }

    /// <summary>Reads an independent snapshot of the composed dictionary.</summary>
    /// <returns>The current dictionary snapshot.</returns>
    public DiameterDictionarySnapshot ReadDictionarySnapshot()
    {
        return Builder.ReadDictionarySnapshot();
    }

    /// <summary>Finds a module by canonical name.</summary>
    /// <param name="name">The canonical module name.</param>
    /// <returns>An independent definition, or <c>null</c> when absent.</returns>
    public DiameterDictionaryModule? FindDictionaryModuleByName(string? name)
    {
        DiameterDictionarySnapshot snapshot = Builder.ReadDictionarySnapshot();
        return new DictionaryIndexes(snapshot).FindModuleByName(name);
    }

    /// <summary>Finds a vendor by Vendor-ID.</summary>
    /// <param name="vendorId">The Vendor-ID.</param>
    /// <returns>An independent definition, or <c>null</c> when absent.</returns>
    public DiameterVendorDefinition? FindDictionaryVendorById(uint vendorId)
    {
        DiameterDictionarySnapshot snapshot = Builder.ReadDictionarySnapshot();
        return new DictionaryIndexes(snapshot).FindVendorById(vendorId);
    }

    /// <summary>Finds a vendor by canonical name.</summary>
    /// <param name="name">The canonical vendor name.</param>
    /// <returns>An independent definition, or <c>null</c> when absent.</returns>
    public DiameterVendorDefinition? FindDictionaryVendorByName(string? name)
    {
        DiameterDictionarySnapshot snapshot = Builder.ReadDictionarySnapshot();
        return new DictionaryIndexes(snapshot).FindVendorByName(name);
    }

    /// <summary>Finds an application by vendor and application identifiers.</summary>
    /// <param name="vendorId">The defining Vendor-ID.</param>
    /// <param name="applicationId">The application identifier.</param>
    /// <returns>An independent definition, or <c>null</c> when absent.</returns>
    public DiameterApplicationDefinition? FindDictionaryApplicationById(
        uint vendorId,
        uint applicationId)
    {
        DiameterDictionarySnapshot snapshot = Builder.ReadDictionarySnapshot();
        return new DictionaryIndexes(snapshot).FindApplicationById(vendorId, applicationId);
    }

    /// <summary>Finds an application by vendor and canonical name.</summary>
    /// <param name="vendorId">The defining Vendor-ID.</param>
    /// <param name="name">The canonical application name.</param>
    /// <returns>An independent definition, or <c>null</c> when absent.</returns>
    public DiameterApplicationDefinition? FindDictionaryApplicationByName(
        uint vendorId,
        string? name)
    {
        DiameterDictionarySnapshot snapshot = Builder.ReadDictionarySnapshot();
        return new DictionaryIndexes(snapshot).FindApplicationByName(vendorId, name);
    }

    /// <summary>Finds a value type by vendor and canonical name.</summary>
    /// <param name="vendorId">The defining Vendor-ID.</param>
    /// <param name="name">The canonical type name.</param>
    /// <returns>An independent definition, or <c>null</c> when absent.</returns>
    public DiameterTypeDefinition? FindDictionaryTypeByName(uint vendorId, string? name)
    {
        DiameterDictionarySnapshot snapshot = Builder.ReadDictionarySnapshot();
        return new DictionaryIndexes(snapshot).FindTypeByName(vendorId, name);
    }

    /// <summary>Finds an enumerated definition by type and numeric value.</summary>
    /// <param name="typeVendorId">The type Vendor-ID.</param>
    /// <param name="typeName">The canonical type name.</param>
    /// <param name="value">The enumerated value.</param>
    /// <returns>An independent definition, or <c>null</c> when absent.</returns>
    public DiameterEnumValueDefinition? FindDictionaryEnumValueByTypeNumber(
        uint typeVendorId,
        string? typeName,
        int value)
    {
        DiameterDictionarySnapshot snapshot = Builder.ReadDictionarySnapshot();
        return new DictionaryIndexes(snapshot).FindEnumValueByTypeValue(
            typeVendorId,
            typeName,
            value);
    }

    /// <summary>Finds an enumerated definition by type and value name.</summary>
    /// <param name="typeVendorId">The type Vendor-ID.</param>
    /// <param name="typeName">The canonical type name.</param>
    /// <param name="valueName">The canonical enumerated value name.</param>
    /// <returns>An independent definition, or <c>null</c> when absent.</returns>
    public DiameterEnumValueDefinition? FindDictionaryEnumValueByTypeName(
        uint typeVendorId,
        string? typeName,
        string? valueName)
    {
        DiameterDictionarySnapshot snapshot = Builder.ReadDictionarySnapshot();
        return new DictionaryIndexes(snapshot).FindEnumValueByTypeName(
            typeVendorId,
            typeName,
            valueName);
    }

    /// <summary>Finds an enumerated definition by AVP and numeric value.</summary>
    /// <param name="avpVendorId">The AVP Vendor-ID.</param>
    /// <param name="avpCode">The AVP code.</param>
    /// <param name="value">The enumerated value.</param>
    /// <returns>An independent definition, or <c>null</c> when absent.</returns>
    public DiameterEnumValueDefinition? FindDictionaryEnumValueByAvpNumber(
        uint avpVendorId,
        uint avpCode,
        int value)
    {
        DiameterDictionarySnapshot snapshot = Builder.ReadDictionarySnapshot();
        return new DictionaryIndexes(snapshot).FindEnumValueByAvpValue(
            avpVendorId,
            avpCode,
            value);
    }

    /// <summary>Finds an enumerated definition by AVP and value name.</summary>
    /// <param name="avpVendorId">The AVP Vendor-ID.</param>
    /// <param name="avpCode">The AVP code.</param>
    /// <param name="valueName">The canonical enumerated value name.</param>
    /// <returns>An independent definition, or <c>null</c> when absent.</returns>
    public DiameterEnumValueDefinition? FindDictionaryEnumValueByAvpName(
        uint avpVendorId,
        uint avpCode,
        string? valueName)
    {
        DiameterDictionarySnapshot snapshot = Builder.ReadDictionarySnapshot();
        return new DictionaryIndexes(snapshot).FindEnumValueByAvpName(
            avpVendorId,
            avpCode,
            valueName);
    }

    /// <summary>Finds an AVP by vendor and code.</summary>
    /// <param name="vendorId">The AVP Vendor-ID.</param>
    /// <param name="avpCode">The AVP code.</param>
    /// <returns>An independent definition, or <c>null</c> when absent.</returns>
    public DiameterAvpDefinition? FindDictionaryAvpByCode(uint vendorId, uint avpCode)
    {
        DiameterDictionarySnapshot snapshot = Builder.ReadDictionarySnapshot();
        return new DictionaryIndexes(snapshot).FindAvpByCode(vendorId, avpCode);
    }

    /// <summary>Finds an AVP by vendor and canonical name.</summary>
    /// <param name="vendorId">The AVP Vendor-ID.</param>
    /// <param name="name">The canonical AVP name.</param>
    /// <returns>An independent definition, or <c>null</c> when absent.</returns>
    public DiameterAvpDefinition? FindDictionaryAvpByName(uint vendorId, string? name)
    {
        DiameterDictionarySnapshot snapshot = Builder.ReadDictionarySnapshot();
        return new DictionaryIndexes(snapshot).FindAvpByName(vendorId, name);
    }

    /// <summary>Finds a command by application, code, and direction.</summary>
    /// <param name="applicationId">The application identifier.</param>
    /// <param name="commandCode">The command code.</param>
    /// <param name="request">Indicates the request form.</param>
    /// <returns>An independent definition, or <c>null</c> when absent.</returns>
    public DiameterCommandDefinition? FindDictionaryCommandByCode(
        uint applicationId,
        uint commandCode,
        bool request)
    {
        DiameterDictionarySnapshot snapshot = Builder.ReadDictionarySnapshot();
        return new DictionaryIndexes(snapshot).FindCommandByCode(
            applicationId,
            commandCode,
            request);
    }

    /// <summary>Finds a command by application, canonical name, and direction.</summary>
    /// <param name="applicationId">The application identifier.</param>
    /// <param name="name">The canonical command name.</param>
    /// <param name="request">Indicates the request form.</param>
    /// <returns>An independent definition, or <c>null</c> when absent.</returns>
    public DiameterCommandDefinition? FindDictionaryCommandByName(
        uint applicationId,
        string? name,
        bool request)
    {
        DiameterDictionarySnapshot snapshot = Builder.ReadDictionarySnapshot();
        return new DictionaryIndexes(snapshot).FindCommandByName(applicationId, name, request);
    }

    /// <summary>Finds a Grouped AVP occurrence rule by parent and child codes.</summary>
    /// <param name="parentVendorId">The parent AVP Vendor-ID.</param>
    /// <param name="parentAvpCode">The parent AVP code.</param>
    /// <param name="childVendorId">The child AVP Vendor-ID.</param>
    /// <param name="childAvpCode">The child AVP code.</param>
    /// <returns>An independent definition, or <c>null</c> when absent.</returns>
    public DiameterAvpRuleDefinition? FindDictionaryGroupedRuleByCode(
        uint parentVendorId,
        uint parentAvpCode,
        uint childVendorId,
        uint childAvpCode)
    {
        DiameterDictionarySnapshot snapshot = Builder.ReadDictionarySnapshot();
        return new DictionaryIndexes(snapshot).FindGroupedRuleByCode(
            parentVendorId,
            parentAvpCode,
            childVendorId,
            childAvpCode);
    }

    /// <summary>Finds a Grouped AVP occurrence rule by parent and child names.</summary>
    /// <param name="parentVendorId">The parent AVP Vendor-ID.</param>
    /// <param name="parentName">The canonical parent AVP name.</param>
    /// <param name="childVendorId">The child AVP Vendor-ID.</param>
    /// <param name="childName">The canonical child AVP name.</param>
    /// <returns>An independent definition, or <c>null</c> when absent.</returns>
    public DiameterAvpRuleDefinition? FindDictionaryGroupedRuleByName(
        uint parentVendorId,
        string? parentName,
        uint childVendorId,
        string? childName)
    {
        DiameterDictionarySnapshot snapshot = Builder.ReadDictionarySnapshot();
        DictionaryIndexes indexes = new DictionaryIndexes(snapshot);
        DiameterAvpDefinition? parent = indexes.FindAvpByName(parentVendorId, parentName);
        if (parent == null)
        {
            return null;
        }

        DiameterAvpRuleDefinition? result = indexes.FindGroupedRuleByName(
            parentVendorId,
            parent.Code,
            childName);
        if (result != null && result.AvpVendorId != childVendorId)
        {
            return null;
        }

        return result;
    }

    /// <summary>Finds a command occurrence rule by command and AVP codes.</summary>
    /// <param name="applicationId">The command application identifier.</param>
    /// <param name="commandCode">The command code.</param>
    /// <param name="request">Indicates the request form.</param>
    /// <param name="avpVendorId">The rule AVP Vendor-ID.</param>
    /// <param name="avpCode">The rule AVP code.</param>
    /// <returns>An independent definition, or <c>null</c> when absent.</returns>
    public DiameterAvpRuleDefinition? FindDictionaryCommandRuleByCode(
        uint applicationId,
        uint commandCode,
        bool request,
        uint avpVendorId,
        uint avpCode)
    {
        DiameterDictionarySnapshot snapshot = Builder.ReadDictionarySnapshot();
        return new DictionaryIndexes(snapshot).FindCommandRuleByCode(
            applicationId,
            commandCode,
            request,
            avpVendorId,
            avpCode);
    }

    /// <summary>Finds a command occurrence rule by command and AVP names.</summary>
    /// <param name="applicationId">The command application identifier.</param>
    /// <param name="commandName">The canonical command name.</param>
    /// <param name="request">Indicates the request form.</param>
    /// <param name="avpVendorId">The rule AVP Vendor-ID.</param>
    /// <param name="avpName">The canonical rule AVP name.</param>
    /// <returns>An independent definition, or <c>null</c> when absent.</returns>
    public DiameterAvpRuleDefinition? FindDictionaryCommandRuleByName(
        uint applicationId,
        string? commandName,
        bool request,
        uint avpVendorId,
        string? avpName)
    {
        DiameterDictionarySnapshot snapshot = Builder.ReadDictionarySnapshot();
        DictionaryIndexes indexes = new DictionaryIndexes(snapshot);
        DiameterCommandDefinition? command = indexes.FindCommandByName(
            applicationId,
            commandName,
            request);
        if (command == null)
        {
            return null;
        }

        DiameterAvpRuleDefinition? result = indexes.FindCommandRuleByName(
            applicationId,
            command.Code,
            request,
            avpName);
        if (result != null && result.AvpVendorId != avpVendorId)
        {
            return null;
        }

        return result;
    }

    internal void RegisterBuiltInModules()
    {
        Builder.RegisterDictionaryModule(Rfc6733Dictionary.CreateModule());
        Builder.RegisterDictionaryModule(Rfc8506Dictionary.CreateModule());
        Builder.RegisterDictionaryModule(Rfc6737Dictionary.CreateModule());
        Builder.RegisterDictionaryModule(Rfc7075Dictionary.CreateModule());
        Builder.RegisterDictionaryModule(Rfc7683Dictionary.CreateModule());
        Builder.RegisterDictionaryModule(Rfc7944Dictionary.CreateModule());
        Builder.RegisterDictionaryModule(Rfc8581Dictionary.CreateModule());
        Builder.RegisterDictionaryModule(Rfc8582Dictionary.CreateModule());
        Builder.RegisterDictionaryModule(Rfc8583Dictionary.CreateModule());
    }

    internal DiameterDictionaryRegistrationResult CreatePublicRegistrationResult(
        DictionaryRegistrationResult source)
    {
        DiameterDictionaryRegistrationResult result =
            new DiameterDictionaryRegistrationResult();
        result.Status = ConvertRegistrationStatus(source.Status);
        result.Code = source.Code;
        result.Message = source.Message;
        result.Snapshot = source.Snapshot;
        return result;
    }

    internal DiameterDictionaryRegistrationStatus ConvertRegistrationStatus(
        DictionaryRegistrationStatus status)
    {
        if (status == DictionaryRegistrationStatus.Duplicate)
        {
            return DiameterDictionaryRegistrationStatus.Duplicate;
        }

        if (status == DictionaryRegistrationStatus.Invalid)
        {
            return DiameterDictionaryRegistrationStatus.Invalid;
        }

        if (status == DictionaryRegistrationStatus.Conflict)
        {
            return DiameterDictionaryRegistrationStatus.Conflict;
        }

        return DiameterDictionaryRegistrationStatus.Success;
    }
}
