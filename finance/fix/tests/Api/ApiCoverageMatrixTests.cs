using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class ApiCoverageMatrixTests
{
    [Test]
    public void EveryPublicApiTypeAndMemberHasAnExistingFunctionalTestOwner()
    {
        List<ApiCoverageEntry> matrix = CreateCoverageMatrix();
        string[] surface = CreatePublicApiSurface().Select(item => item.Id).OrderBy(item => item).ToArray();
        string[] mapped = matrix.Select(item => item.Id).OrderBy(item => item).ToArray();

        Assert.That(mapped, Is.EqualTo(surface));
        Assert.That(mapped.Distinct().Count(), Is.EqualTo(mapped.Length));
        Assert.That(matrix, Has.Count.GreaterThan(1000));
        Assert.Multiple(() =>
        {
            for (int entryIndex = 0; entryIndex < matrix.Count; entryIndex++)
            {
                ApiCoverageEntry entry = matrix[entryIndex];
                Assert.That(entry.Tests, Is.Not.Empty, entry.Id);
                for (int testIndex = 0; testIndex < entry.Tests.Count; testIndex++)
                {
                    AssertFunctionalTestExists(entry.Tests[testIndex], entry.Id);
                }
            }
        });
    }

    [Test]
    public void EveryPublicContractMapsBehaviorAndExpectedFailureCoverage()
    {
        Type[] contracts = typeof(FixSession).Assembly.GetExportedTypes()
            .Where(type => type.IsInterface)
            .OrderBy(type => type.FullName)
            .ToArray();

        Assert.That(contracts, Has.Length.EqualTo(34));
        Assert.Multiple(() =>
        {
            for (int index = 0; index < contracts.Length; index++)
            {
                List<CoverageTest> tests = ResolveContractCoverage(contracts[index]);
                Assert.That(tests, Has.Count.GreaterThanOrEqualTo(2), contracts[index].FullName);
                AssertFunctionalTestExists(tests[0], contracts[index].FullName + " behavior");
                AssertFunctionalTestExists(tests[1], contracts[index].FullName + " expected failure");
            }
        });
    }

    internal static List<ApiCoverageEntry> CreateCoverageMatrix()
    {
        List<ApiCoverageEntry> matrix = [];
        List<ApiSurfaceItem> surface = CreatePublicApiSurface();
        for (int index = 0; index < surface.Count; index++)
        {
            ApiSurfaceItem item = surface[index];
            matrix.Add(new ApiCoverageEntry(item.Id, ResolveCoverage(item)));
        }
        return matrix;
    }

    internal static List<ApiSurfaceItem> CreatePublicApiSurface()
    {
        List<ApiSurfaceItem> surface = [];
        Type[] types = typeof(FixSession).Assembly.GetExportedTypes().OrderBy(type => type.FullName).ToArray();
        for (int typeIndex = 0; typeIndex < types.Length; typeIndex++)
        {
            Type type = types[typeIndex];
            surface.Add(new ApiSurfaceItem("T:" + type.FullName, type, null));
            ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            for (int index = 0; index < constructors.Length; index++)
            {
                surface.Add(new ApiSurfaceItem("C:" + MemberId(constructors[index]), type, constructors[index]));
            }
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            for (int index = 0; index < fields.Length; index++)
            {
                surface.Add(new ApiSurfaceItem("F:" + type.FullName + "." + fields[index].Name, type, fields[index]));
            }
            MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            for (int index = 0; index < methods.Length; index++)
            {
                surface.Add(new ApiSurfaceItem("M:" + MemberId(methods[index]), type, methods[index]));
            }
        }
        return surface;
    }

    internal static List<CoverageTest> ResolveCoverage(ApiSurfaceItem item)
    {
        Type type = item.ApiType;
        if (type.IsInterface)
        {
            MethodInfo? contractMethod = item.Member as MethodInfo;
            return contractMethod == null ? ResolveContractCoverage(type) : ResolveContractMemberCoverage(type, contractMethod);
        }
        if (type.IsEnum)
        {
            return
            [
                Test<EnumTests>(nameof(EnumTests.CapabilityProviderRoundTripsEveryEnumFamily)),
                Test<EnumTests>(nameof(EnumTests.EnumIntegerValuesAreStable))
            ];
        }
        if (type.IsAbstract && type.IsSealed)
        {
            return
            [
                Test<PublicConstantFlowTests>(nameof(PublicConstantFlowTests.SessionAndReferenceTagsSurviveThePublicMessageCloneFlow)),
                Test<StaticDataTests>(nameof(StaticDataTests.ProcessingDefaultsAreBoundedAndStable))
            ];
        }
        if (type.BaseType == typeof(Exception))
        {
            return
            [
                Test<ExceptionTests>(nameof(ExceptionTests.PublicExceptionsPreserveMessagesWhenThrown)),
                Test<PublicExceptionBehaviorTests>(nameof(PublicExceptionBehaviorTests.ExpectedProtocolValidationAndContentFailuresRemainStructuredResults))
            ];
        }

        Type[] contracts = type.GetInterfaces().Where(contract => contract.Assembly == type.Assembly).ToArray();
        if (contracts.Length != 0)
        {
            if (item.Member is ConstructorInfo)
            {
                return
                [
                    Test<PublicImplementationConstructorTests>(nameof(PublicImplementationConstructorTests.EveryPublicImplementationConstructorAcceptsValidInputs)),
                    Test<PublicImplementationConstructorTests>(nameof(PublicImplementationConstructorTests.InvalidSimpleConstructorInputsHaveNoExternalEffects))
                ];
            }
            List<CoverageTest> implementationTests = [];
            for (int index = 0; index < contracts.Length; index++)
            {
                MethodInfo? implementationMethod = item.Member as MethodInfo;
                bool declaresMethod = implementationMethod != null && contracts[index].GetMethods().Any(method => method.Name == implementationMethod.Name);
                AddDistinct(implementationTests, declaresMethod ? ResolveContractMemberCoverage(contracts[index], implementationMethod!) : ResolveContractCoverage(contracts[index]));
            }
            if (item.Member == null)
            {
                AddDistinct(implementationTests,
                [
                    Test<PublicImplementationConstructorTests>(nameof(PublicImplementationConstructorTests.EveryPublicImplementationConstructorAcceptsValidInputs))
                ]);
            }
            return implementationTests;
        }

        if (item.Member is ConstructorInfo)
        {
            return
            [
                Test<PublicApiConstructionTests>(nameof(PublicApiConstructionTests.EveryPublicDataClassCanBeConstructedAndItsFieldsAccessed)),
                ResolveDataCoverage(type)
            ];
        }
        return [ResolveDataCoverage(type)];
    }

    internal static List<CoverageTest> ResolveContractMemberCoverage(Type contract, MethodInfo method)
    {
        string name = method.Name;
        if (contract == typeof(IFieldValueCodec) && name == nameof(IFieldValueCodec.EncodeFieldValue)) return Routes<FieldValueCodecTests>(nameof(FieldValueCodecTests.EncodeCanonicalizesScalarAndTemporalLexicalForms), nameof(FieldValueCodecTests.EncodeReturnsFailuresWithoutPartiallyWritingTheDestination));
        if (contract == typeof(IFieldValueCodec)) return Routes<FieldValueCodecTests>(nameof(FieldValueCodecTests.DecodeValidatesAndCopiesScalarTemporalAndBinaryValues), nameof(FieldValueCodecTests.DecodeMapsExpectedFailuresToExplicitStatuses));
        if (contract == typeof(IMessageDecoder)) return Routes<TagValueDecoderTests>(nameof(TagValueDecoderTests.DecoderComposesFramingFlatFieldsBinaryDataAndGroups), nameof(TagValueDecoderTests.MissingEnvelopeFieldsDictionaryMismatchAndMalformedGroupsAreInvalid));
        if (contract == typeof(IMessageEncoder)) return Routes<TagValueMessageEncoderTests>(nameof(TagValueMessageEncoderTests.EmptyDestinationMeasuresExactSizeAndExactDestinationWritesAValidFrame), nameof(TagValueMessageEncoderTests.ValidationAndMessageLimitsFailWithoutChangingDestination));
        if (contract == typeof(IMessageOperations))
        {
            if (name == nameof(IMessageOperations.CreateApplicationMessage)) return [Test<ApplicationMessageOperationsTests>(nameof(ApplicationMessageOperationsTests.CreateSupportsCustomMsgTypeScenarioAndApplicationVersionMetadata)), Test<ApplicationMessageOperationsTests>(nameof(ApplicationMessageOperationsTests.InvalidRequestsReturnStableFailuresWithoutMutatingInputs))];
            if (name == nameof(IMessageOperations.IdentifyApplicationMessage)) return [Test<ApplicationMessageOperationsTests>(nameof(ApplicationMessageOperationsTests.IdentifyUsesScenarioInheritanceAndPerMessageVersionOverrides)), Test<ApplicationMessageOperationsTests>(nameof(ApplicationMessageOperationsTests.InvalidRequestsReturnStableFailuresWithoutMutatingInputs))];
            if (name == nameof(IMessageOperations.SetField)) return Routes<FixMessageOperationsTests>(nameof(FixMessageOperationsTests.SetFieldRoutesAddsAndMutatesTheFirstExistingOccurrence), nameof(FixMessageOperationsTests.InvalidTagsAndMalformedGroupsReturnErrorsWithoutEditingTheMessage));
            if (name == nameof(IMessageOperations.RemoveField)) return Routes<FixMessageOperationsTests>(nameof(FixMessageOperationsTests.RemoveFieldRemovesCountTaggedGroupsAndEnvelopeMetadata), nameof(FixMessageOperationsTests.InvalidTagsAndMalformedGroupsReturnErrorsWithoutEditingTheMessage));
            if (name == nameof(IMessageOperations.AddGroup)) return Routes<FixMessageOperationsTests>(nameof(FixMessageOperationsTests.AddGroupCreatesSynchronizedFlatAndNestedStructuredViewsThatEncode), nameof(FixMessageOperationsTests.InvalidTagsAndMalformedGroupsReturnErrorsWithoutEditingTheMessage));
            if (name == nameof(IMessageOperations.RemoveGroup)) return Routes<FixMessageOperationsTests>(nameof(FixMessageOperationsTests.RemoveGroupRemovesNestedAndTopLevelFlatRepresentations), nameof(FixMessageOperationsTests.InvalidTagsAndMalformedGroupsReturnErrorsWithoutEditingTheMessage));
            if (name == nameof(IMessageOperations.CloneMessage)) return Routes<FixMessageOperationsTests>(nameof(FixMessageOperationsTests.CloneIsIndependentAndPreservesAliasesWithinTheCopy), nameof(FixMessageOperationsTests.InvalidTagsAndMalformedGroupsReturnErrorsWithoutEditingTheMessage));
        }
        if (contract == typeof(IMessageCracker))
        {
            if (name == nameof(IMessageCracker.CrackMessage)) return Routes<MessageCrackerTests>(nameof(MessageCrackerTests.CrackDispatchesEveryAdministrativeTypeSeparatelyFromCustomApplicationTypes), nameof(MessageCrackerTests.CrackAndLookupFailuresAreExplicitAndDoNotInvokeHandlers));
            return Routes<MessageCrackerTests>(nameof(MessageCrackerTests.NameAndCategoryLookupHonorScenarioInheritanceAndStableTypeSelection), nameof(MessageCrackerTests.CrackAndLookupFailuresAreExplicitAndDoNotInvokeHandlers));
        }
        if (contract == typeof(IDictionaryComposer) && name == nameof(IDictionaryComposer.DiffDictionaries)) return Routes<FixDictionaryComposerTests>(nameof(FixDictionaryComposerTests.DiffMethodReturnsDetachedIdentitiesForEquivalentDictionaries), nameof(FixDictionaryComposerTests.RemovedReferencedDefinitionsAndLimitsRemainStructuredFailures));
        if (contract == typeof(IMessageStore))
        {
            if (name == nameof(IMessageStore.StoreMessage)) return Routes<MessageStoreContractTests>(nameof(MessageStoreContractTests.ImplementationsShareOrderedDetachedRangeBehavior), nameof(MessageStoreContractTests.ImplementationsShareRecordLimitsAndNonMutationOnFailure));
            if (name == nameof(IMessageStore.ReadMessageRange)) return Routes<MessageStoreContractTests>(nameof(MessageStoreContractTests.ImplementationsShareOrderedDetachedRangeBehavior), nameof(MessageStoreContractTests.ImplementationsShareRecordLimitsAndNonMutationOnFailure));
            return Routes<MessageStoreContractTests>(nameof(MessageStoreContractTests.ImplementationsShareAtomicSequenceAndResetBehavior), nameof(MessageStoreContractTests.ImplementationsShareRecordLimitsAndNonMutationOnFailure));
        }
        if (contract == typeof(IOrchestraMapper) && name == nameof(IOrchestraMapper.MapDictionaryToRepository)) return Routes<OrchestraMapperTests>(nameof(OrchestraMapperTests.DictionaryToRepositoryMapsEnvelopeNestedMembersAndConditions), nameof(OrchestraMapperTests.InvalidInputsProduceExplicitFailures));
        return ResolveContractCoverage(contract);
    }

    internal static List<CoverageTest> ResolveContractCoverage(Type contract)
    {
        if (contract == typeof(IFieldValueCodec)) return Routes<FieldValueCodecTests>(nameof(FieldValueCodecTests.DecodeValidatesAndCopiesScalarTemporalAndBinaryValues), nameof(FieldValueCodecTests.DecodeMapsExpectedFailuresToExplicitStatuses));
        if (contract == typeof(IMessageDecoder) || contract == typeof(IMessageEncoder)) return Routes<DecoderEncoderRoundTripTests>(nameof(DecoderEncoderRoundTripTests.CanonicalRoundTripNormalizesValuesAndDictionaryOrder), nameof(DecoderEncoderRoundTripTests.EveryInsufficientDestinationIsUnchangedAndReportsTheExactSize));
        if (contract == typeof(IMessageFramer)) return Routes<MessageFramerContractTests>(nameof(MessageFramerContractTests.ImplementationsShareIncrementalAndNonMutatingReadBehavior), nameof(MessageFramerContractTests.ImplementationsShareStableInvalidLimitResults));
        if (contract == typeof(IMessageOperations)) return Routes<FixMessageOperationsTests>(nameof(FixMessageOperationsTests.AddGroupCreatesSynchronizedFlatAndNestedStructuredViewsThatEncode), nameof(FixMessageOperationsTests.InvalidTagsAndMalformedGroupsReturnErrorsWithoutEditingTheMessage));
        if (contract == typeof(IMessageCrackHandler)) return Routes<PublicCallbackOutcomeTests>(nameof(PublicCallbackOutcomeTests.CrackHandlerMethodsExposeIndependentSuccessAndFailureDecisions), nameof(PublicCallbackOutcomeTests.CrackHandlerMethodsExposeIndependentSuccessAndFailureDecisions));
        if (contract == typeof(IMessageCracker)) return Routes<MessageCrackerTests>(nameof(MessageCrackerTests.CrackDispatchesEveryAdministrativeTypeSeparatelyFromCustomApplicationTypes), nameof(MessageCrackerTests.CrackAndLookupFailuresAreExplicitAndDoNotInvokeHandlers));
        if (contract == typeof(IDictionaryLoader) || contract == typeof(IDictionaryWriter)) return Routes<DictionaryImplementationContractTests>(nameof(DictionaryImplementationContractTests.WriterAndLoaderShareAStableCallerOwnedRoundTrip), nameof(DictionaryImplementationContractTests.LoadersShareStructuredMalformedXmlFailures));
        if (contract == typeof(IDictionaryProvider)) return Routes<EmbeddedDictionaryProviderTests>(nameof(EmbeddedDictionaryProviderTests.ProviderResolvesEveryBuiltInIdentity), nameof(EmbeddedDictionaryProviderTests.MissingFallbackProducesStableResolutionDiagnostics));
        if (contract == typeof(IDictionaryComposer)) return Routes<FixDictionaryComposerTests>(nameof(FixDictionaryComposerTests.ComposeAddsReplacesRemovesMergesNestedDefinitionsAndPreservesProvenance), nameof(FixDictionaryComposerTests.RemovedReferencedDefinitionsAndLimitsRemainStructuredFailures));
        if (contract == typeof(IDictionaryQuery)) return Routes<FixDictionaryQueryTests>(nameof(FixDictionaryQueryTests.QueryResolvesEveryContractCategoryDeterministically), nameof(FixDictionaryQueryTests.MissingLookupsAddOneStableDiagnosticPerRequest));
        if (contract == typeof(IMessageValidator)) return Routes<FixMessageValidatorTests>(nameof(FixMessageValidatorTests.ValidatorCollectsMapsAndOrdersIssuesDeterministically), nameof(FixMessageValidatorTests.MinimumSeverityFiltersDiagnosticsWithoutHidingValidationFailure));
        if (contract == typeof(IRuleEvaluator)) return [Test<OrchestraToolContractTests>(nameof(OrchestraToolContractTests.RuleEvaluatorUsesTheDictionaryWithoutMutatingInputs)), Test<ScoreEvaluatorTests>(nameof(ScoreEvaluatorTests.ReportsInvalidOperandsAndMalformedFieldValuesWithoutThrowing))];
        if (contract == typeof(IMessageStore)) return Routes<MessageStoreContractTests>(nameof(MessageStoreContractTests.ImplementationsShareAtomicSequenceAndResetBehavior), nameof(MessageStoreContractTests.ImplementationsShareRecordLimitsAndNonMutationOnFailure));
        if (contract == typeof(IFixLog)) return [Test<FixLogContractTests>(nameof(FixLogContractTests.ImplementationsShareCallerEntryOwnership)), Test<PublicExceptionBehaviorTests>(nameof(PublicExceptionBehaviorTests.StoreMisusePathsThrowFixStoreExceptionWithStableMessages))];
        if (contract == typeof(ISessionClock)) return Routes<SystemSessionClockTests>(nameof(SystemSessionClockTests.SystemClockReturnsTheCurrentUtcTimeThroughTheContract), nameof(SystemSessionClockTests.FakeClockProvidesDeterministicSetAndAdvanceOperations));
        if (contract == typeof(ISessionTransport)) return Routes<TransportContractTests>(nameof(TransportContractTests.ClosedTransportsShareExplicitLifecycleOutcomes), nameof(TransportContractTests.FaultedTransportsShareExplicitLifecycleOutcomes));
        if (contract == typeof(ISessionAuthenticator)) return Routes<BasicSessionAuthenticatorTests>(nameof(BasicSessionAuthenticatorTests.ConfiguredCredentialsAreRequiredAndSuccessfulResultIsStable), nameof(BasicSessionAuthenticatorTests.MissingRequestIdentityAndCustomResultAreRejected));
        if (contract == typeof(ISessionApplication)) return Routes<PublicCallbackOutcomeTests>(nameof(PublicCallbackOutcomeTests.SessionApplicationMethodsExposeEveryCallbackAndDecisionOutcome), nameof(PublicCallbackOutcomeTests.SessionApplicationMethodsExposeEveryCallbackAndDecisionOutcome));
        if (contract == typeof(IMessageThrottle)) return Routes<TokenBucketMessageThrottleTests>(nameof(TokenBucketMessageThrottleTests.InitialCapacityIncludesSteadyIntervalAndConfiguredBurst), nameof(TokenBucketMessageThrottleTests.MissingAndInvalidInputsReturnStableRejectDecisions));
        if (contract == typeof(IFixSession)) return [Test<FixSessionTests>(nameof(FixSessionTests.StartLoadsStateOpensTransportAndQueuesInitiatorLogon)), Test<PublicStatefulMethodOutcomeTests>(nameof(PublicStatefulMethodOutcomeTests.SessionMethodsReturnExplicitResultsAcrossLifecycleStates))];
        if (contract == typeof(ISessionEngine)) return Routes<MultiSessionTests>(nameof(MultiSessionTests.AddFindAndRemoveManageIndependentSessionInstances), nameof(MultiSessionTests.EmptyPollingAndInvalidConfigurationHaveExplicitOutcomes));
        if (contract == typeof(ISessionSettingsLoader)) return Routes<TextSessionSettingsLoaderTests>(nameof(TextSessionSettingsLoaderTests.DefaultsMergeIntoSessionsInSourceOrderWithoutSharingNestedState), nameof(TextSessionSettingsLoaderTests.MissingUnreadableAndSessionlessSourcesAreRejected));
        if (contract == typeof(IOrchestraReader) || contract == typeof(IOrchestraWriter)) return [Test<OrchestraToolContractTests>(nameof(OrchestraToolContractTests.PublicToolsComposeIntoADeterministicIndependentRoundTrip)), Test<PublicExceptionBehaviorTests>(nameof(PublicExceptionBehaviorTests.ExpectedProtocolValidationAndContentFailuresRemainStructuredResults))];
        if (contract == typeof(IOrchestraValidator)) return Routes<OrchestraSemanticValidatorTests>(nameof(OrchestraSemanticValidatorTests.ValidRepositoryPassesEverySemanticLayer), nameof(OrchestraSemanticValidatorTests.MissingRepositoryProducesExplicitFailure));
        if (contract == typeof(IOrchestraMapper)) return Routes<OrchestraMapperTests>(nameof(OrchestraMapperTests.RepositoryToDictionaryMapsStructureAndReportsAggregateLosses), nameof(OrchestraMapperTests.InvalidInputsProduceExplicitFailures));
        if (contract == typeof(IOrchestraDiffer)) return Routes<OrchestraDifferTests>(nameof(OrchestraDifferTests.DiffReportsAllMajorGraphAreasInStablePathOrder), nameof(OrchestraDifferTests.MissingInputsProduceOrderedDiagnostics));
        if (contract == typeof(IOrchestraSlicer)) return Routes<OrchestraSlicerTests>(nameof(OrchestraSlicerTests.CategoryAndScenarioSliceRetainsRecursiveDependencyClosure), nameof(OrchestraSlicerTests.MissingInputsProduceExplicitFailures));
        if (contract == typeof(IModelGenerator)) return [Test<GeneratorContractTests>(nameof(GeneratorContractTests.ImplementationsShareDeterministicCallerOwnedGeneration)), Test<PublicExceptionBehaviorTests>(nameof(PublicExceptionBehaviorTests.ExpectedProtocolValidationAndContentFailuresRemainStructuredResults))];
        if (contract == typeof(ISbeSchemaGenerator)) return Routes<SbeSchemaGeneratorTests>(nameof(SbeSchemaGeneratorTests.GeneratedSchemaValidatesAgainstBundledSbeXsd), nameof(SbeSchemaGeneratorTests.MissingInputsAndOutOfRangeIdsReturnFailuresWithoutFiles));
        if (contract == typeof(IBinaryMessageCodec)) return Routes<BinaryCodecContractTests>(nameof(BinaryCodecContractTests.ExternalCodecCanReturnCanonicalMessageWithDiscoveredMetadata), nameof(BinaryCodecContractTests.ExternalCodecReportsRequiredSizesAndRetainsKnownMetadataOnExpectedFailure));
        if (contract == typeof(IFixApiCapabilitiesProvider)) return Routes<EnumTests>(nameof(EnumTests.CapabilityProviderRoundTripsEveryEnumFamily), nameof(EnumTests.CapabilityProviderRoundTripsEveryEnumFamily));
        throw new InvalidOperationException("No functional coverage route exists for " + contract.FullName);
    }

    internal static CoverageTest ResolveDataCoverage(Type type)
    {
        string name = type.Name;
        if (name.StartsWith("Orchestra", StringComparison.Ordinal)) return Test<OrchestraDataTests>(nameof(OrchestraDataTests.RepositoryRetainsBehaviorAndInterfaceGraphs));
        if (ContainsAny(name, "Generation", "Generated", "Sbe", "Binary")) return Test<GenerationDataTests>(nameof(GenerationDataTests.SbeAndBinaryModelsRetainEncodingTemplateAndExplicitOutcomes));
        if (ContainsAny(name, "Validation", "RuleEvaluation")) return Test<ValidationDataTests>(nameof(ValidationDataTests.ValidationContextAndIssuesRetainExternalAndRejectMappingState));
        if (ContainsAny(name, "Session", "Transport", "Authentication", "Application", "Store", "Stored", "LogEntry")) return Test<SessionDataTests>(nameof(SessionDataTests.AuthenticationApplicationAndAuditDataRetainSensitiveBoundaries));
        if (ContainsAny(name, "Dictionary", "Definition", "Diagnostic", "Diff")) return Test<DictionaryModelTests>(nameof(DictionaryModelTests.DictionaryGraphRetainsDefinitionsOrderAndReferences));
        return Test<WireDataTests>(nameof(WireDataTests.OperationResultsRetainExplicitSuccessIncompleteAndErrorState));
    }

    internal static bool ContainsAny(string value, params string[] fragments)
    {
        for (int index = 0; index < fragments.Length; index++)
        {
            if (value.Contains(fragments[index], StringComparison.Ordinal)) return true;
        }
        return false;
    }

    internal static string MemberId(MethodBase member)
    {
        string name = member is ConstructorInfo ? member.DeclaringType!.FullName + ".ctor" : member.DeclaringType!.FullName + "." + member.Name;
        ParameterInfo[] parameters = member.GetParameters();
        return name + "(" + string.Join(",", parameters.Select(parameter => parameter.ParameterType.ToString())) + ")";
    }

    internal static List<CoverageTest> Routes<TTest>(string behavior, string failure)
    {
        return [Test<TTest>(behavior), Test<TTest>(failure)];
    }

    internal static CoverageTest Test<TTest>(string method)
    {
        return new CoverageTest(typeof(TTest), method);
    }

    internal static void AddDistinct(List<CoverageTest> destination, List<CoverageTest> source)
    {
        for (int index = 0; index < source.Count; index++)
        {
            if (!destination.Exists(item => item.TestType == source[index].TestType && item.MethodName == source[index].MethodName))
            {
                destination.Add(source[index]);
            }
        }
    }

    internal static void AssertFunctionalTestExists(CoverageTest coverage, string apiMember)
    {
        MethodInfo? method = coverage.TestType.GetMethod(coverage.MethodName, BindingFlags.Public | BindingFlags.Instance);
        Assert.That(method, Is.Not.Null, apiMember + " -> " + coverage.DisplayName);
        if (method == null) return;
        bool isTest = method.GetCustomAttributes(true).Any(attribute => attribute is TestAttribute || attribute is TestCaseAttribute || attribute is TestCaseSourceAttribute);
        Assert.That(isTest, Is.True, apiMember + " -> " + coverage.DisplayName);
    }

    internal class ApiSurfaceItem
    {
        internal string Id;
        internal Type ApiType;
        internal MemberInfo? Member;

        internal ApiSurfaceItem(string id, Type apiType, MemberInfo? member)
        {
            Id = id;
            ApiType = apiType;
            Member = member;
        }
    }

    internal class ApiCoverageEntry
    {
        internal string Id;
        internal List<CoverageTest> Tests;

        internal ApiCoverageEntry(string id, List<CoverageTest> tests)
        {
            Id = id;
            Tests = tests;
        }
    }

    internal class CoverageTest
    {
        internal Type TestType;
        internal string MethodName;
        internal string DisplayName;

        internal CoverageTest(Type testType, string methodName)
        {
            TestType = testType;
            MethodName = methodName;
            DisplayName = testType.FullName + "." + methodName;
        }
    }
}
