namespace asn1;

internal class XsdSchemaImporterImplementation : IXsdSchemaImporter
{
    internal XsdDocumentLoadResult LastLoadResult =
        new XsdDocumentLoadResult();
    internal XsdSchemaGraphResult LastGraphResult =
        new XsdSchemaGraphResult();
    internal XsdMappingValidationResult LastMappingValidationResult =
        new XsdMappingValidationResult();
    internal XsdSimpleTypeMappingResult LastSimpleTypeResult =
        new XsdSimpleTypeMappingResult();
    internal XsdDeclarationMappingResult LastDeclarationResult =
        new XsdDeclarationMappingResult();
    internal XsdComplexTypeMappingResult LastComplexTypeResult =
        new XsdComplexTypeMappingResult();
    internal XsdAdvancedMappingResult LastAdvancedMappingResult =
        new XsdAdvancedMappingResult();
    internal XsdModuleGenerationResult LastModuleGenerationResult =
        new XsdModuleGenerationResult();
    internal XsdBuiltInTypeDefinition[] LastBuiltInTypes = [];
    internal XsdV2SupportModule LastSupportModule =
        new XsdV2SupportModule();

    public AsnXsdImportResult ImportSchemas(
        System.ReadOnlySpan<AsnXsdInput> inputs,
        AsnXsdImportOptions options)
    {
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }
        this.LastBuiltInTypes = XsdBuiltInTypeCatalog.GetTypes();
        this.LastSupportModule = XsdV2SupportModuleCatalog.GetModule();
        this.LastLoadResult = new XsdDocumentLoader().LoadDocuments(
            inputs.ToArray(),
            options);
        this.LastGraphResult = this.LastLoadResult.Success
            ? new XsdSchemaGraphResolver().ResolveGraph(
                this.LastLoadResult,
                options)
            : new XsdSchemaGraphResult();
        this.LastMappingValidationResult = this.LastGraphResult.Success
            ? new XsdMappingValidator().ValidateGraph(
                this.LastGraphResult,
                options)
            : new XsdMappingValidationResult();
        this.LastSimpleTypeResult = this.LastGraphResult.Success
            ? new XsdSimpleTypeMapper().MapSimpleTypes(
                this.LastGraphResult,
                options)
            : new XsdSimpleTypeMappingResult();
        this.LastDeclarationResult = this.LastSimpleTypeResult.Success
            ? new XsdDeclarationMapper().MapDeclarations(
                this.LastGraphResult,
                this.LastSimpleTypeResult,
                options)
            : new XsdDeclarationMappingResult();
        this.LastComplexTypeResult = this.LastDeclarationResult.Success
            ? new XsdComplexTypeMapper().MapComplexTypes(
                this.LastGraphResult,
                this.LastSimpleTypeResult,
                this.LastDeclarationResult,
                options)
            : new XsdComplexTypeMappingResult();
        this.LastAdvancedMappingResult = this.LastComplexTypeResult.Success
            ? new XsdAdvancedMapper().MapAdvancedFeatures(
                this.LastGraphResult,
                this.LastSimpleTypeResult,
                this.LastDeclarationResult,
                this.LastComplexTypeResult,
                options)
            : new XsdAdvancedMappingResult();
        this.LastModuleGenerationResult = this.LastAdvancedMappingResult.Success
                && this.LastMappingValidationResult.Success
            ? new XsdModuleGenerator().GenerateModules(
                this.LastGraphResult,
                this.LastSimpleTypeResult,
                this.LastDeclarationResult,
                this.LastComplexTypeResult,
                this.LastAdvancedMappingResult,
                this.LastSupportModule,
                options)
            : new XsdModuleGenerationResult();
        AsnDiagnostic[] diagnostics = this.MergeDiagnostics(
            this.LastLoadResult.Diagnostics,
            this.MergeDiagnostics(
                this.LastGraphResult.Diagnostics,
                this.MergeDiagnostics(
                    this.LastMappingValidationResult.Diagnostics,
                    this.MergeDiagnostics(
                        this.LastSimpleTypeResult.Diagnostics,
                        this.MergeDiagnostics(
                            this.LastDeclarationResult.Diagnostics,
                            this.MergeDiagnostics(
                                this.LastComplexTypeResult.Diagnostics,
                                this.MergeDiagnostics(
                                    this.LastAdvancedMappingResult.Diagnostics,
                                    this.LastModuleGenerationResult.Diagnostics)))))));
        return new AsnXsdImportResult
        {
            Success = this.LastLoadResult.Success
                && this.LastGraphResult.Success
                && this.LastMappingValidationResult.Success
                && this.LastSimpleTypeResult.Success
                && this.LastDeclarationResult.Success
                && this.LastComplexTypeResult.Success
                && this.LastAdvancedMappingResult.Success
                && this.LastModuleGenerationResult.Success,
            SourceUnits = this.LastModuleGenerationResult.SourceUnits,
            Diagnostics = diagnostics
        };
    }

    internal AsnDiagnostic[] MergeDiagnostics(
        AsnDiagnostic[] first,
        AsnDiagnostic[] second)
    {
        AsnDiagnostic[] result =
            new AsnDiagnostic[first.Length + second.Length];
        int index = 0;
        while (index < first.Length)
        {
            result[index] = first[index];
            index = index + 1;
        }
        int secondIndex = 0;
        while (secondIndex < second.Length)
        {
            result[index] = second[secondIndex];
            index = index + 1;
            secondIndex = secondIndex + 1;
        }
        return result;
    }
}
