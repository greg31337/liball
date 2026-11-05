using System.Reflection;
using iso20022;

namespace iso20022.Tests;

public class PublicApiContractInventoryTests
{
    [Test]
    public void EveryPublicContractMethodAndImplementationHasBehavioralCoverage()
    {
        List<ContractCoverage> inventory = CreateInventory();
        Assembly apiAssembly = typeof(IMessageProcessor).Assembly;
        Type[] contracts = apiAssembly.GetExportedTypes()
            .Where(type => type.IsInterface && type.Namespace == "iso20022")
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
        List<string> expectedMethods = new List<string>();
        foreach (Type contract in contracts)
        {
            foreach (MethodInfo method in contract.GetMethods()
                .OrderBy(method => method.Name, StringComparer.Ordinal))
            {
                expectedMethods.Add(MethodKey(contract, method.Name));
            }
        }

        string[] mappedMethods = inventory
            .Select(item => MethodKey(item.Contract, item.MethodName))
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        Assert.That(mappedMethods, Is.Unique);
        Assert.That(mappedMethods, Is.EqualTo(expectedMethods.OrderBy(
            value => value,
            StringComparer.Ordinal)));

        Type[] expectedImplementations = apiAssembly.GetExportedTypes()
            .Where(type => type.IsClass && !type.IsAbstract &&
                type.GetInterfaces().Any(contract =>
                    contract.Assembly == apiAssembly && contract.IsPublic))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
        Type[] mappedImplementations = inventory
            .Where(item => item.Implementation != null)
            .Select(item => item.Implementation!)
            .Distinct()
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
        Assert.That(mappedImplementations, Is.EqualTo(expectedImplementations));

        foreach (ContractCoverage item in inventory)
        {
            MethodInfo? contractMethod = item.Contract.GetMethod(item.MethodName);
            MethodInfo? testMethod = item.TestFixture.GetMethod(item.TestMethod);
            Assert.That(contractMethod, Is.Not.Null,
                MethodKey(item.Contract, item.MethodName));
            Assert.That(testMethod, Is.Not.Null,
                item.TestFixture.Name + "." + item.TestMethod);
            Assert.That(testMethod?.GetCustomAttributes(true).Any(attribute =>
                attribute is TestAttribute || attribute is TestCaseAttribute),
                Is.True,
                item.TestFixture.Name + "." + item.TestMethod +
                " must remain an executable behavioral test.");
            if (item.Implementation != null)
            {
                Assert.That(item.Contract.IsAssignableFrom(item.Implementation),
                    Is.True,
                    item.Implementation.Name + " must implement " +
                    item.Contract.Name + ".");
            }
        }
    }

    private static List<ContractCoverage> CreateInventory()
    {
        List<ContractCoverage> inventory = new List<ContractCoverage>();
        Add(inventory, typeof(IArtifactStore), typeof(FileArtifactStore),
            typeof(FileArtifactStoreTests),
            nameof(FileArtifactStoreTests.StoreAndOpenRoundTripContentManifestAndEncodedCoordinate),
            "StoreArtifactStream", "OpenArtifact");
        Add(inventory, typeof(ICSharpModelGenerator), typeof(CSharpModelGenerator),
            typeof(CSharpModelGeneratorTests),
            nameof(CSharpModelGeneratorTests.ManifestRecordsExactMappingsSettingsSourcesAndDeterministicHashes),
            "GenerateCSharpModels");
        Add(inventory, typeof(ICatalogueClient), typeof(CatalogueClient),
            typeof(CatalogueClientTests),
            nameof(CatalogueClientTests.DownloadCarriesConditionsAndBuildsVerifiedProvenance),
            "DownloadCatalogue");
        Add(inventory, typeof(ICodeSetProvider), typeof(CodeSetProvider),
            typeof(CodeSetProviderTests),
            nameof(CodeSetProviderTests.InternalLookupsSelectPinnedOrLatestEditionByIdentifierOrNameWithProvenance),
            "LookupCode");
        Add(inventory, typeof(IExternalCodeSetImporter),
            typeof(ExternalCodeSetImporter), typeof(ExternalCodeSetJsonImporterTests),
            nameof(ExternalCodeSetJsonImporterTests.RawJsonNormalizesSchemaFacetsDefinitionsEditionAndAbsentLifecycle),
            "ImportJsonStream");
        Add(inventory, typeof(IExternalCodeSetImporter),
            typeof(ExternalCodeSetImporter), typeof(ExternalCodeSetXsdImporterTests),
            nameof(ExternalCodeSetXsdImporterTests.RawXsdNormalizesFacetsDocumentationAndPublishedLifecycleMetadata),
            "ImportXsdStream");
        Add(inventory, typeof(IExternalCodeSetImporter),
            typeof(ExternalCodeSetImporter), typeof(ExternalCodeSetXlsxImporterTests),
            nameof(ExternalCodeSetXlsxImporterTests.RawWorkbookNormalizesSharedInlineNumericDateMappingRemovalAndBlankCells),
            "ImportXlsxStream");
        Add(inventory, typeof(IExternalCodeSetImporter),
            typeof(ExternalCodeSetImporter), typeof(ExternalCodeCatalogComparisonTests),
            nameof(ExternalCodeCatalogComparisonTests.ComparisonReportsPresenceMultiplicityFacetAndFieldDisagreementsPerFormat),
            "CompareCatalogs");

        AddFoundationCoverage(inventory);

        Add(inventory, typeof(IJsonSchemaGenerator), typeof(JsonSchemaGenerator),
            typeof(JsonSchemaGeneratorTests),
            nameof(JsonSchemaGeneratorTests.RepositoryModelGeneratesIsoDraft202012Schema),
            "GenerateJsonSchema");
        Add(inventory, typeof(IMessageBuilder), typeof(MessageBuilder),
            typeof(MessageBuilderTests),
            nameof(MessageBuilderTests.BuildsEveryGeneratedSemanticTemplateAndRoundTripsExactIdentity),
            "BuildMessage");
        Add(inventory, typeof(IMessageChecksumCalculator), typeof(MessageChecksum),
            typeof(MessageChecksumTests),
            nameof(MessageChecksumTests.ByteChecksumsPreserveExactInputAndVerifyExpectedValues),
            "CalculateChecksum");
        Add(inventory, typeof(IMessageDetector), typeof(MessageDetector),
            typeof(MessageDetectorTests),
            nameof(MessageDetectorTests.NullInputsAndOptionsReturnStableFailures),
            "DetectBytes", "DetectStream", "DetectText");
        Add(inventory, typeof(IMessageIdentifierParser),
            typeof(MessageIdentifierParser), typeof(IdentityNamespaceServiceTests),
            nameof(IdentityNamespaceServiceTests.IdentifierParserReturnsExactComponents),
            "ParseIdentifier");
        Add(inventory, typeof(IMessageJsonConverter), typeof(MessageJsonConverter),
            typeof(MessageJsonConverterTests),
            nameof(MessageJsonConverterTests.GenericConversionCarriesIdentityFormsChoicesArraysAndAmountAttributes),
            "WriteGenericDocument", "ReadGenericText");
        Add(inventory, typeof(IMessageMapper), typeof(MessageMapper),
            typeof(MessageMapperTests),
            nameof(MessageMapperTests.MapsMessageWithInspectablePathsConstantsCodesAndDrops),
            "MapMessage");
        Add(inventory, typeof(IMessageMigrator), typeof(MessageMigrator),
            typeof(MessageMigratorTests),
            nameof(MessageMigratorTests.ExecutesEveryBuiltInActionAndMaterializesTargetVersion),
            "MigrateMessage");
        Add(inventory, typeof(IMessageProcessor), typeof(MessageProcessor),
            typeof(MessageProcessorTests),
            nameof(MessageProcessorTests.CompletesEveryNonEmptyStageCombinationInCanonicalOrder),
            "ProcessMessage");
        Add(inventory, typeof(IMessageProjector), typeof(MessageProjector),
            typeof(MessageProjectorTests),
            nameof(MessageProjectorTests.ProjectsEveryStableTaskViewFromExactRawMessage),
            "ProjectMessage");
        Add(inventory, typeof(IMessageStreamReader), typeof(MessageStreamReader),
            typeof(MessageStreamBatchApiTests),
            nameof(MessageStreamBatchApiTests.EmptyAndSingleItemBatchesHaveStablePullBoundaries),
            "ReadNextMessage");
        Add(inventory, typeof(IMessageStreamWriter), typeof(MessageStreamWriter),
            typeof(MessageStreamBatchApiTests),
            nameof(MessageStreamBatchApiTests.WriterFinalizesEmptyAndSingleItemBatchesExactlyOnce),
            "WriteNextMessage", "CompleteWriting");
        Add(inventory, typeof(IMessageValidator), typeof(MessageValidator),
            typeof(MessageValidatorTests),
            nameof(MessageValidatorTests.DefaultPipelineRunsEveryStageInDeterministicOrderAndResolvesIdentity),
            "ValidateMessage");
        Add(inventory, typeof(IMessageVersionComparer),
            typeof(MessageVersionComparer), typeof(MessageVersionComparerTests),
            nameof(MessageVersionComparerTests.ExactMessageDiffClassifiesAllStructuralChangeKinds),
            "CompareMessageVersions");
        Add(inventory, typeof(IMetadataExtractor), typeof(MetadataExtractor),
            typeof(MetadataExtractorTests),
            nameof(MetadataExtractorTests.ExtractsHeaderIdentityPaymentValuesAndOrderedReferences),
            "ExtractMetadata");
        Add(inventory, typeof(IMigrationPlanner), typeof(MigrationPlanner),
            typeof(MigrationPlannerTests),
            nameof(MigrationPlannerTests.ExplicitRulesProduceEveryActionInDeterministicSequence),
            "PlanMigration");
        Add(inventory, typeof(INamespaceService), typeof(NamespaceService),
            typeof(IdentityNamespaceServiceTests),
            nameof(IdentityNamespaceServiceTests.StrictNamespaceParsingClassifiesIdentifierFamilies),
            "ParseNamespace");
        Add(inventory, typeof(INamespaceService), typeof(NamespaceService),
            typeof(IdentityNamespaceServiceTests),
            nameof(IdentityNamespaceServiceTests.NamespaceBuilderCreatesCanonicalNamespaceAndClassification),
            "BuildNamespaceFromIdentifier");
        Add(inventory, typeof(IProfileProvider), typeof(ProfileProvider),
            typeof(ProfileProviderTests),
            nameof(ProfileProviderTests.ProviderFindsExactAndLatestDottedVersionFromCopiedRegistrationList),
            "FindProfile");
        Add(inventory, typeof(IRepositoryComparer), typeof(RepositoryComparer),
            typeof(RepositoryComparerTests),
            nameof(RepositoryComparerTests.GraphDiffClassifiesEveryRepositoryChangeCategory),
            "CompareRepositories");
        Add(inventory, typeof(IRepositoryImporter), typeof(RepositoryImporter),
            typeof(RepositoryImporterQueryTests),
            nameof(RepositoryImporterQueryTests.ImporterReadsRawAndZipStreamsWithProvenanceAndIntegrityCounts),
            "ImportRawStream", "ImportZipStream");
        Add(inventory, typeof(IRepositoryImporter), typeof(RepositoryImporter),
            typeof(RepositoryImporterQueryTests),
            nameof(RepositoryImporterQueryTests.ImporterReadsRawAndZipFilesAndRejectsMismatchedForms),
            "ImportRawFile", "ImportZipFile");
        Add(inventory, typeof(IRepositoryQuery), typeof(RepositoryQuery),
            typeof(RepositoryQueryApiTests),
            nameof(RepositoryQueryApiTests.QueryCombinesCriteriaAndArtifactEditionConjunctively),
            "QueryRepository");
        Add(inventory, typeof(ISchemaImporter), typeof(SchemaImporter),
            typeof(SchemaImporterTests),
            nameof(SchemaImporterTests.StreamImportNormalizesNamespaceTypesElementsFacetsAndProvenance),
            "ImportSchemaStream");
        Add(inventory, typeof(ISchemaImporter), typeof(SchemaImporter),
            typeof(SchemaImporterTests),
            nameof(SchemaImporterTests.ImportReportsPolicyFailuresAndUsesSecureBoundedFileParsing),
            "ImportSchemaFile");
        AddSchemaRegistryCoverage(inventory);
        Add(inventory, typeof(ISemanticRule), null,
            typeof(MessageValidatorTests),
            nameof(MessageValidatorTests.SemanticRulesUseDeterministicApplicabilityPathsAndCallerContext),
            "GetSemanticRuleDescriptor", "ValidateSemanticRule");
        Add(inventory, typeof(ISupplementaryDataValidator),
            typeof(SupplementaryDataValidator),
            typeof(SupplementaryDataValidatorTests),
            nameof(SupplementaryDataValidatorTests.RawDiscoveryPreservesGlobalLocalRepeatingNamespaceCompletePayloads),
            "ValidateSupplementaryData");
        AddXmlReaderCoverage(inventory);
        Add(inventory, typeof(IXmlMessageSigner), typeof(XmlMessageSigner),
            typeof(XmlMessageSignerTests),
            nameof(XmlMessageSignerTests.SignsWholeDocumentDeterministicallyAndVerifiesGeneratedKeyValue),
            "SignMessage");
        Add(inventory, typeof(IXmlMessageWriter), typeof(XmlMessageWriter),
            typeof(XmlMessageWriterTests),
            nameof(XmlMessageWriterTests.StreamTextWriterBufferAndStringEntryPointsProduceEquivalentCompactXml),
            "WriteGenericStream", "WriteGenericTextWriter", "WriteGenericBuffer",
            "WriteGenericString");
        Add(inventory, typeof(IXmlSignatureValidator),
            typeof(XmlSignatureValidator), typeof(XmlSignatureValidatorTests),
            nameof(XmlSignatureValidatorTests.ValidatesContainedEnvelopedSignatureAndTrustedCertificate),
            "ValidateSignatures");
        Add(inventory, typeof(IXsdGenerator), typeof(XsdGenerator),
            typeof(XsdGeneratorTests),
            nameof(XsdGeneratorTests.RepositoryModelGeneratesCompilableSchemaForAllLogicalConstructs),
            "GenerateXsd");
        Add(inventory, typeof(IXsdValidator), typeof(XsdValidator),
            typeof(XsdValidatorTests),
            nameof(XsdValidatorTests.RawXmlValidationReportsStableLinePathAndSchemaProvenance),
            "ValidateXmlStream");
        Add(inventory, typeof(IXsdValidator), typeof(XsdValidator),
            typeof(XsdValidatorTests),
            nameof(XsdValidatorTests.MessageDocumentValidationUsesOriginalNodeLocations),
            "ValidateMessageDocument");
        return inventory;
    }

    private static void AddFoundationCoverage(List<ContractCoverage> inventory)
    {
        Add(inventory, typeof(IFoundationDataConsumer), null,
            typeof(FoundationDataTests),
            nameof(FoundationDataTests.PrimitiveModelsRetainValueAndLexicalState),
            "ConsumeAmountValue", "ConsumeBooleanValue", "ConsumeCodeValue",
            "ConsumeTextValue");
        Add(inventory, typeof(IFoundationDataConsumer), null,
            typeof(FoundationDataTests),
            nameof(FoundationDataTests.RemainingPrimitiveModelsRetainTheirDistinctState),
            "ConsumeDurationValue");
        Add(inventory, typeof(IFoundationDataConsumer), null,
            typeof(FoundationDataTests),
            nameof(FoundationDataTests.ArtifactSelectorAndSourceLocationRetainCallerData),
            "ConsumeArtifactManifest", "ConsumeSourceLocation");
        Add(inventory, typeof(IFoundationDataConsumer), null,
            typeof(FoundationDataTests),
            nameof(FoundationDataTests.IdentityModelsPreserveLeadingZeroesAndQualifiedNames),
            "ConsumeNamespaceDescriptor", "ConsumeVersionSelector",
            "ConsumeXmlPath");
        Add(inventory, typeof(IFoundationDataConsumer), null,
            typeof(GenericMessageDataTests),
            nameof(GenericMessageDataTests.GenericXmlTreePreservesQualifiedNamesMixedContentAndRawNamespaceClosure),
            "ConsumeNamespaceDeclaration");
        Add(inventory, typeof(IFoundationDataConsumer), null,
            typeof(ResourceLimitsTests),
            nameof(ResourceLimitsTests.NewLimitsUseThePublishedConservativeDefaults),
            "ConsumeResourceLimits");
        Add(inventory, typeof(IFoundationDataConsumer), null,
            typeof(GenericMessageDataTests),
            nameof(GenericMessageDataTests.MessageDocumentComposesHeaderPayloadEnvelopeAndMetadataWithoutLosingRepresentations),
            "ConsumeMessageDocument");
        Add(inventory, typeof(IFoundationDataConsumer), null,
            typeof(GenericMessageDataTests),
            nameof(GenericMessageDataTests.ReadWriteAndMetadataOptionsRetainExplicitPolicies),
            "ConsumeMessageReadOptions", "ConsumeMessageWriteOptions",
            "ConsumeMetadataExtractionOptions");
        Add(inventory, typeof(IFoundationDataConsumer), null,
            typeof(GenericMessageDataTests),
            nameof(GenericMessageDataTests.ResultsRepairsAndStreamOptionsRetainOutcomeAndControlState),
            "ConsumeMessageWriteResult", "ConsumeMessageStreamItem",
            "ConsumeStreamReadOptions", "ConsumeStreamWriteOptions");
        Add(inventory, typeof(IFoundationDataConsumer), null,
            typeof(RepositoryFoundationDataTests),
            nameof(RepositoryFoundationDataTests.SnapshotRetainsExactArtifactAndMetamodelProvenance),
            "ConsumeRepositorySnapshot");
        Add(inventory, typeof(IFoundationDataConsumer), null,
            typeof(ValidationApiDataTests),
            nameof(ValidationApiDataTests.ValidationRequestCarriesRawGenericTypedIdentityProfileAndCallerContext),
            "ConsumeValidationRequest");
        Add(inventory, typeof(IFoundationDataConsumer), null,
            typeof(ValidationApiDataTests),
            nameof(ValidationApiDataTests.ValidationReportRetainsOrderedLocatedRedactedDiagnosticsAndProvenance),
            "ConsumeValidationReport");
    }

    private static void AddSchemaRegistryCoverage(
        List<ContractCoverage> inventory)
    {
        Add(inventory, typeof(ISchemaRegistry), typeof(SchemaRegistry),
            typeof(SchemaFamilyRegistrationTests),
            nameof(SchemaFamilyRegistrationTests.CallerSuppliedFamiliesUseTheSameStreamRegistryPath),
            "ImportRegisterSchemaStream");
        Add(inventory, typeof(ISchemaRegistry), typeof(SchemaRegistry),
            typeof(SchemaFamilyRegistrationTests),
            nameof(SchemaFamilyRegistrationTests.FileRegistrationImportsBeforeApplyingRegistryPolicy),
            "ImportRegisterSchemaFile");
        Add(inventory, typeof(ISchemaRegistry), typeof(SchemaRegistry),
            typeof(SchemaFamilyRegistrationTests),
            nameof(SchemaFamilyRegistrationTests.ArchiveRegistrationRequiresAnExactSafeXsdEntry),
            "ImportRegisterSchemaArchive");
        Add(inventory, typeof(ISchemaRegistry), typeof(SchemaRegistry),
            typeof(SchemaRegistryTests),
            nameof(SchemaRegistryTests.RegistrationAndLookupUseExactKeysAndPreserveArtifactProvenance),
            "RegisterSchema", "LookupSchema");
        Add(inventory, typeof(ISchemaRegistry), typeof(SchemaRegistry),
            typeof(SchemaRegistryTests),
            nameof(SchemaRegistryTests.DependencyReplacementInvalidatesCompiledRootClosure),
            "CompileSchema");
    }

    private static void AddXmlReaderCoverage(List<ContractCoverage> inventory)
    {
        Add(inventory, typeof(IXmlMessageReader), typeof(XmlMessageReader),
            typeof(XmlMessageReaderTests),
            nameof(XmlMessageReaderTests.GenericEntryPointsPreserveUnknownXmlWithoutRequiringDetection),
            "ReadGenericBytes", "ReadGenericStream", "ReadGenericText");
        Add(inventory, typeof(IXmlMessageReader), typeof(XmlMessageReader),
            typeof(XmlMessageReaderTests),
            nameof(XmlMessageReaderTests.AutoEntryPointsMaterializeDetectedHeaderPayloadAndMetadataConsistently),
            "ReadAutoBytes", "ReadAutoStream", "ReadAutoText");
        Add(inventory, typeof(IXmlMessageReader), typeof(XmlMessageReader),
            typeof(XmlMessageReaderTests),
            nameof(XmlMessageReaderTests.ExactVersionEntryPointsAcceptValueOnlyOrComponentIdentifiersAndRejectMismatches),
            "ReadExactBytes", "ReadExactStream", "ReadExactText");
        Add(inventory, typeof(IXmlMessageReader), typeof(XmlMessageReader),
            typeof(XmlMessageReaderTests),
            nameof(XmlMessageReaderTests.HeaderOnlyEntryPointsRetainHeaderAndMetadataButDoNotExposePayloadTrees),
            "ReadHeaderBytes", "ReadHeaderStream", "ReadHeaderText");
        Add(inventory, typeof(IXmlMessageReader), typeof(XmlMessageReader),
            typeof(XmlMessageReaderTests),
            nameof(XmlMessageReaderTests.MetadataOnlyEntryPointsReturnBusinessValuesWithoutHeaderPayloadOrEnvelopeTrees),
            "ReadMetadataBytes", "ReadMetadataStream", "ReadMetadataText");
    }

    private static void Add(
        List<ContractCoverage> inventory,
        Type contract,
        Type? implementation,
        Type testFixture,
        string testMethod,
        params string[] methods)
    {
        foreach (string method in methods)
        {
            inventory.Add(new ContractCoverage(
                contract,
                method,
                implementation,
                testFixture,
                testMethod));
        }
    }

    private static string MethodKey(Type contract, string method)
    {
        return contract.FullName + "." + method;
    }

    private sealed class ContractCoverage
    {
        internal ContractCoverage(
            Type contract,
            string methodName,
            Type? implementation,
            Type testFixture,
            string testMethod)
        {
            Contract = contract;
            MethodName = methodName;
            Implementation = implementation;
            TestFixture = testFixture;
            TestMethod = testMethod;
        }

        internal Type Contract;
        internal string MethodName;
        internal Type? Implementation;
        internal Type TestFixture;
        internal string TestMethod;
    }
}
