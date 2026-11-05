using System.Reflection;
using xml;

namespace xml.Tests;

[TestFixture]
public class PublicApiCoverageMatrixTests
{
    [Test]
    public void EveryPublicApiTypeAndMemberMapsToAnExecutableTest()
    {
        Dictionary<Type, CoverageAnchor> implementations = ImplementationAnchors();
        Dictionary<Type, CoverageAnchor> interfaces = InterfaceAnchors();
        Dictionary<Type, CoverageAnchor> statics = StaticAnchors();
        Type[] apiTypes = PublicApiTypes();
        List<string> missing = [];
        List<string> rows = [];

        foreach (Type apiType in apiTypes)
        {
            CoverageAnchor? anchor = ResolveAnchor(apiType, implementations, interfaces, statics);
            if (anchor == null)
            {
                missing.Add(apiType.FullName ?? apiType.Name);
                continue;
            }

            string? anchorFailure = ValidateAnchor(anchor);
            if (anchorFailure != null)
            {
                missing.Add(apiType.Name + " -> " + anchorFailure);
                continue;
            }

            rows.Add(apiType.Name + " -> " + anchor.DisplayName());
            foreach (ConstructorInfo constructor in apiType.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                rows.Add(MemberName(apiType, constructor) + " -> " + anchor.DisplayName());
            }

            foreach (FieldInfo field in apiType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly).Where(field => !field.IsSpecialName))
            {
                rows.Add(apiType.Name + "." + field.Name + " -> " + anchor.DisplayName());
            }

            foreach (MethodInfo method in apiType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly).Where(method => !method.IsSpecialName))
            {
                rows.Add(MemberName(apiType, method) + " -> " + anchor.DisplayName());
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(apiTypes, Has.Length.GreaterThan(150));
            Assert.That(rows, Has.Count.GreaterThan(1000));
            Assert.That(rows.Distinct().Count(), Is.EqualTo(rows.Count));
            Assert.That(missing, Is.Empty, string.Join(Environment.NewLine, missing));
        });
    }

    [Test]
    public void EveryOptionBranchMapsToBehavioralCoverage()
    {
        Dictionary<Type, CoverageAnchor> anchors = OptionAnchors();
        Type[] options = PublicApiTypes().Where(IsOptionType).ToArray();
        List<string> missing = [];
        List<string> branches = [];

        foreach (Type option in options)
        {
            if (!anchors.TryGetValue(option, out CoverageAnchor? anchor))
            {
                missing.Add(option.Name);
                continue;
            }

            string? anchorFailure = ValidateAnchor(anchor);
            if (anchorFailure != null)
            {
                missing.Add(option.Name + " -> " + anchorFailure);
                continue;
            }

            foreach (FieldInfo field in option.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (field.FieldType == typeof(bool))
                {
                    branches.Add(option.Name + "." + field.Name + "=false -> " + anchor.DisplayName());
                    branches.Add(option.Name + "." + field.Name + "=true -> " + anchor.DisplayName());
                }
                else if (field.FieldType.IsEnum)
                {
                    foreach (string value in Enum.GetNames(field.FieldType))
                    {
                        branches.Add(option.Name + "." + field.Name + "=" + value + " -> " + anchor.DisplayName());
                    }
                }
                else
                {
                    branches.Add(option.Name + "." + field.Name + "=default -> " + anchor.DisplayName());
                    branches.Add(option.Name + "." + field.Name + "=configured -> " + anchor.DisplayName());
                }
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(options, Has.Length.EqualTo(12));
            Assert.That(branches, Has.Count.GreaterThan(200));
            Assert.That(missing, Is.Empty, string.Join(Environment.NewLine, missing));
        });
    }

    [Test]
    public void EveryResolverAndExtensionCallbackMapsToBehavioralCoverage()
    {
        Dictionary<Type, CoverageAnchor> callbacks = CallbackAnchors();
        List<string> missing = [];
        int methodCount = 0;

        foreach (KeyValuePair<Type, CoverageAnchor> callback in callbacks)
        {
            if (!callback.Key.IsInterface)
            {
                missing.Add(callback.Key.Name + " is not an interface");
            }

            string? anchorFailure = ValidateAnchor(callback.Value);
            if (anchorFailure != null)
            {
                missing.Add(callback.Key.Name + " -> " + anchorFailure);
            }

            methodCount = methodCount + callback.Key.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Length;
        }

        Assert.Multiple(() =>
        {
            Assert.That(callbacks, Has.Count.EqualTo(19));
            Assert.That(methodCount, Is.EqualTo(26));
            Assert.That(missing, Is.Empty, string.Join(Environment.NewLine, missing));
        });
    }

    [Test]
    public void EveryImplementationMethodMapsToSuccessfulBehavior()
    {
        Dictionary<Type, CoverageAnchor> implementations = ImplementationAnchors();
        Type[] concreteServices = PublicApiTypes()
            .Where(type => type.IsClass && !type.IsAbstract)
            .Where(type => type.GetInterfaces().Any(contract => contract.Namespace == "xml"))
            .ToArray();
        List<string> missing = [];
        int methodCount = 0;

        foreach (Type service in concreteServices)
        {
            if (!implementations.TryGetValue(service, out CoverageAnchor? anchor))
            {
                missing.Add(service.Name);
                continue;
            }

            string? anchorFailure = ValidateAnchor(anchor);
            if (anchorFailure != null)
            {
                missing.Add(service.Name + " -> " + anchorFailure);
            }

            methodCount = methodCount + service.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Length;
        }

        Assert.Multiple(() =>
        {
            Assert.That(concreteServices, Has.Length.EqualTo(35));
            Assert.That(methodCount, Is.EqualTo(111));
            Assert.That(missing, Is.Empty, string.Join(Environment.NewLine, missing));
        });
    }

    [Test]
    public void EveryPublicExceptionMapsToConstructionAndRuntimeCoverage()
    {
        Dictionary<Type, CoverageAnchor> runtime = ExceptionRuntimeAnchors();
        Type[] exceptions = PublicApiTypes().Where(type => typeof(Exception).IsAssignableFrom(type)).ToArray();
        List<string> missing = [];

        foreach (Type exception in exceptions)
        {
            ConstructorInfo[] constructors = exception.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (constructors.Length != 1)
            {
                missing.Add(exception.Name + " constructor count " + constructors.Length);
            }

            if (!runtime.TryGetValue(exception, out CoverageAnchor? anchor))
            {
                missing.Add(exception.Name + " runtime behavior");
                continue;
            }

            string? anchorFailure = ValidateAnchor(anchor);
            if (anchorFailure != null)
            {
                missing.Add(exception.Name + " -> " + anchorFailure);
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(exceptions, Has.Length.EqualTo(18));
            Assert.That(missing, Is.Empty, string.Join(Environment.NewLine, missing));
        });
    }

    private static Type[] PublicApiTypes()
    {
        return typeof(XmlParser).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == "xml")
            .OrderBy(type => type.Name)
            .ToArray();
    }

    private static bool IsOptionType(Type type)
    {
        return type.IsClass && (type.Name.EndsWith("Options", StringComparison.Ordinal)
            || type.Name.EndsWith("Policy", StringComparison.Ordinal)
            || type == typeof(XmlSecurityLimits)
            || type == typeof(XPathStaticContext)
            || type == typeof(XPathDynamicContext));
    }

    private static CoverageAnchor? ResolveAnchor(Type type, Dictionary<Type, CoverageAnchor> implementations, Dictionary<Type, CoverageAnchor> interfaces, Dictionary<Type, CoverageAnchor> statics)
    {
        if (type.IsEnum)
        {
            return Anchor(typeof(EnumTests), nameof(EnumTests.PublicEnumNamesAndIntegerValuesAreStable));
        }

        if (typeof(Exception).IsAssignableFrom(type))
        {
            return Anchor(typeof(ExceptionTests), nameof(ExceptionTests.EveryExceptionPreservesMessageDiagnosticAndLocation));
        }

        if (type.IsInterface)
        {
            interfaces.TryGetValue(type, out CoverageAnchor? interfaceAnchor);
            return interfaceAnchor;
        }

        if (type.IsAbstract && type.IsSealed)
        {
            statics.TryGetValue(type, out CoverageAnchor? staticAnchor);
            return staticAnchor;
        }

        if (type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Length > 0)
        {
            return Anchor(typeof(DataFieldCompletenessTests), nameof(DataFieldCompletenessTests.EveryPublicDataFieldRoundTripsThroughRuntimeStorage));
        }

        implementations.TryGetValue(type, out CoverageAnchor? implementationAnchor);
        return implementationAnchor;
    }

    private static string? ValidateAnchor(CoverageAnchor anchor)
    {
        MethodInfo? method = anchor.Fixture.GetMethod(anchor.Method, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        if (method == null)
        {
            return anchor.DisplayName() + " does not exist";
        }

        bool executable = method.GetCustomAttributes(false).Any(attribute =>
        {
            string? name = attribute.GetType().FullName;
            return name == "NUnit.Framework.TestAttribute" || name == "NUnit.Framework.TestCaseAttribute" || name == "NUnit.Framework.TestCaseSourceAttribute";
        });
        return executable ? null : anchor.DisplayName() + " is not an executable NUnit test";
    }

    private static string MemberName(Type type, MethodBase method)
    {
        string parameters = string.Join(",", method.GetParameters().Select(parameter => parameter.ParameterType.Name));
        return type.Name + "." + method.Name + "(" + parameters + ")";
    }

    private static CoverageAnchor Anchor(Type fixture, string method)
    {
        return new CoverageAnchor(fixture, method);
    }

    private static Dictionary<Type, CoverageAnchor> StaticAnchors()
    {
        return new Dictionary<Type, CoverageAnchor>
        {
            [typeof(XmlNamespaces)] = Anchor(typeof(StaticDataTests), nameof(StaticDataTests.XmlNamespaceUrisAreStable)),
            [typeof(XmlMediaTypes)] = Anchor(typeof(StaticDataTests), nameof(StaticDataTests.XmlMediaTypesAndEncodingNamesAreStable)),
            [typeof(XPathNamespaces)] = Anchor(typeof(StaticDataTests), nameof(StaticDataTests.XPathAndXsltNamespaceUrisAreStable)),
            [typeof(XsltNamespaces)] = Anchor(typeof(StaticDataTests), nameof(StaticDataTests.XPathAndXsltNamespaceUrisAreStable)),
            [typeof(XmlSerializationParameterNames)] = Anchor(typeof(StaticDataTests), nameof(StaticDataTests.SerializationParameterExpandedNamesAreStable)),
            [typeof(XmlErrorCodes)] = Anchor(typeof(StaticDataTests), nameof(StaticDataTests.XmlAndSerializationErrorCodesAreStable)),
            [typeof(XPathErrorCodes)] = Anchor(typeof(StaticDataTests), nameof(StaticDataTests.XPathErrorCodeInventoryIsStable)),
            [typeof(XsltErrorCodes)] = Anchor(typeof(StaticDataTests), nameof(StaticDataTests.XsltErrorCodeInventoryIsStable))
        };
    }

    private static Dictionary<Type, CoverageAnchor> InterfaceAnchors()
    {
        CoverageAnchor core = Anchor(typeof(LowLevelContractTests), nameof(LowLevelContractTests.ParserValidationEscapingAndCallbackContractsCarryValues));
        CoverageAnchor streaming = Anchor(typeof(LowLevelContractTests), nameof(LowLevelContractTests.PushPullAndHandlerContractsExposeStateTransitions));
        CoverageAnchor tree = Anchor(typeof(LowLevelContractTests), nameof(LowLevelContractTests.TreeEmissionAndCatalogContractsComposeWithoutMemberCollisions));
        CoverageAnchor output = Anchor(typeof(TreeAndOutputDataTests), nameof(TreeAndOutputDataTests.PlannedContractsCarryNewDataObjects));
        CoverageAnchor xpath = Anchor(typeof(AdvancedContractTests), nameof(AdvancedContractTests.SchemaAndXPathHooksCarryRequestsAndResults));
        CoverageAnchor xslt = Anchor(typeof(AdvancedContractTests), nameof(AdvancedContractTests.XsltHooksCarryPackageExtensionAndTransactionalResultState));
        return new Dictionary<Type, CoverageAnchor>
        {
            [typeof(IDtdValidator)] = core,
            [typeof(IXPathCollation)] = xpath,
            [typeof(IXPathCollectionResolver)] = xpath,
            [typeof(IXPathCompiler)] = Anchor(typeof(XPathPublicIntegrationTests), nameof(XPathPublicIntegrationTests.CompiledExpressionCanBeEvaluatedConcurrentlyWithIndependentContexts)),
            [typeof(IXPathEvaluator)] = Anchor(typeof(XPathPublicIntegrationTests), nameof(XPathPublicIntegrationTests.CompiledExpressionCanBeEvaluatedConcurrentlyWithIndependentContexts)),
            [typeof(IXPathFunction)] = xpath,
            [typeof(IXPathFunctionResolver)] = xpath,
            [typeof(IXPathTraceReceiver)] = xpath,
            [typeof(IXPathVariableResolver)] = xpath,
            [typeof(IXmlByteCollector)] = Anchor(typeof(XmlCanonicalizerPublicTests), nameof(XmlCanonicalizerPublicTests.CollectorExposesBoundedSnapshotCountClearAndResetOperations)),
            [typeof(IXmlByteSink)] = Anchor(typeof(CoreDataTests), nameof(CoreDataTests.ByteSinkContractReceivesMemoryWithoutChangingIt)),
            [typeof(IXmlCancellation)] = core,
            [typeof(IXmlCanonicalizer)] = output,
            [typeof(IXmlCatalogResolver)] = tree,
            [typeof(IXmlDiagnosticCollector)] = Anchor(typeof(XmlCollectorTests), nameof(XmlCollectorTests.DiagnosticCollectorRetainsOrderedSnapshotsAndSupportsClearAndReset)),
            [typeof(IXmlEnumValuesProvider)] = Anchor(typeof(EnumTests), nameof(EnumTests.EnumValueProviderContractReturnsTheSameValueBag)),
            [typeof(IXmlErrorReporter)] = core,
            [typeof(IXmlEscaper)] = core,
            [typeof(IXmlEventCollector)] = Anchor(typeof(XmlCollectorTests), nameof(XmlCollectorTests.EventCollectorRetainsOrderedSnapshotsAndClearsWithoutChangingCapacity)),
            [typeof(IXmlEventEmitter)] = tree,
            [typeof(IXmlEventHandler)] = streaming,
            [typeof(IXmlIncludeProcessor)] = output,
            [typeof(IXmlParser)] = core,
            [typeof(IXmlPullReader)] = streaming,
            [typeof(IXmlPushParser)] = streaming,
            [typeof(IXmlResourceResolver)] = Anchor(typeof(XmlDefaultResolverTests), nameof(XmlDefaultResolverTests.ResolvesAllowedFilesRelativeToABaseAndReturnsOwnedBytes)),
            [typeof(IXmlSchemaCompiler)] = Anchor(typeof(XmlSchemaCompilerTests), nameof(XmlSchemaCompilerTests.CompilesIncludesImportsAndChameleonSchemasIntoOneResolvedSet)),
            [typeof(IXmlSchemaValidator)] = xpath,
            [typeof(IXmlSerializationMethod)] = Anchor(typeof(ExtensionSafetyTests), nameof(ExtensionSafetyTests.SerializationFailuresAreWrappedByTheExtensionBoundary)),
            [typeof(IXmlSerializer)] = output,
            [typeof(IXmlTreeEditor)] = tree,
            [typeof(IXmlTreeNavigator)] = tree,
            [typeof(IXsltCompiler)] = Anchor(typeof(XsltPublicCompilerTests), nameof(XsltPublicCompilerTests.ReusesOneCompilerConcurrentlyAndPublishesThreadSafeExecutables)),
            [typeof(IXsltExtensionInstruction)] = xslt,
            [typeof(IXsltMessageReceiver)] = xslt,
            [typeof(IXsltPackageResolver)] = xslt,
            [typeof(IXsltResultDocumentResolver)] = xslt,
            [typeof(IXsltStreamAnalyzer)] = xslt,
            [typeof(IXsltTraceReceiver)] = xslt,
            [typeof(IXsltTransformer)] = Anchor(typeof(XsltPublicRuntimeTests), nameof(XsltPublicRuntimeTests.TransformerExecutesAndPublishesMessagesResultsAndTraceEvents))
        };
    }

    private static Dictionary<Type, CoverageAnchor> ImplementationAnchors()
    {
        CoverageAnchor xpath = Anchor(typeof(XPathPublicIntegrationTests), nameof(XPathPublicIntegrationTests.BaselineResolversReturnStableEmptyOrCopiedResults));
        CoverageAnchor xslt = Anchor(typeof(XsltPublicRuntimeTests), nameof(XsltPublicRuntimeTests.TransformerExecutesAndPublishesMessagesResultsAndTraceEvents));
        return new Dictionary<Type, CoverageAnchor>
        {
            [typeof(DtdValidator)] = Anchor(typeof(DtdValidatorTests), nameof(DtdValidatorTests.ValidatesACompatibleRetainedTree)),
            [typeof(XPathCodepointCollation)] = Anchor(typeof(ImplementationShellTests), nameof(ImplementationShellTests.StatefulShellsExposeSafeBaselineBehavior)),
            [typeof(XPathCollectionResolver)] = xpath,
            [typeof(XPathCompiler)] = Anchor(typeof(XPathPublicIntegrationTests), nameof(XPathPublicIntegrationTests.CompiledExpressionCanBeEvaluatedConcurrentlyWithIndependentContexts)),
            [typeof(XPathEvaluator)] = Anchor(typeof(XPathPublicIntegrationTests), nameof(XPathPublicIntegrationTests.CompiledExpressionCanBeEvaluatedConcurrentlyWithIndependentContexts)),
            [typeof(XPathFunctionAdapter)] = Anchor(typeof(XdmSequenceStorageTests), nameof(XdmSequenceStorageTests.ExistingPublicProducersReturnCanonicalImmutableEmptyAndNodeStorage)),
            [typeof(XPathFunctionResolver)] = xpath,
            [typeof(XPathTraceCollector)] = Anchor(typeof(XPathPublicIntegrationTests), nameof(XPathPublicIntegrationTests.TraceReturnsItsInputAndPublishesImmutableEvaluationEvents)),
            [typeof(XPathVariableResolver)] = xpath,
            [typeof(XmlByteCollector)] = Anchor(typeof(XmlCanonicalizerPublicTests), nameof(XmlCanonicalizerPublicTests.CollectorExposesBoundedSnapshotCountClearAndResetOperations)),
            [typeof(XmlCancellationFlag)] = Anchor(typeof(ImplementationShellTests), nameof(ImplementationShellTests.StatefulShellsExposeSafeBaselineBehavior)),
            [typeof(XmlCanonicalizer)] = Anchor(typeof(XmlCanonicalizerPublicTests), nameof(XmlCanonicalizerPublicTests.CanonicalizerWritesRawCanonicalBytesWithoutMutatingInputs)),
            [typeof(XmlCatalogResolver)] = Anchor(typeof(XmlCatalogResolverTests), nameof(XmlCatalogResolverTests.ResolvesMappedResourcesAndPropagatesNormalizedMetadata)),
            [typeof(XmlDefaultResolver)] = Anchor(typeof(XmlDefaultResolverTests), nameof(XmlDefaultResolverTests.ResolvesAllowedFilesRelativeToABaseAndReturnsOwnedBytes)),
            [typeof(XmlDiagnosticCollector)] = Anchor(typeof(XmlCollectorTests), nameof(XmlCollectorTests.DiagnosticCollectorRetainsOrderedSnapshotsAndSupportsClearAndReset)),
            [typeof(XmlEscaper)] = Anchor(typeof(XmlEscaperPublicCoverageTests), nameof(XmlEscaperPublicCoverageTests.CoversPredefinedDecimalHexadecimalAndXmlVersionSpecificReferences)),
            [typeof(XmlEventCollector)] = Anchor(typeof(XmlCollectorTests), nameof(XmlCollectorTests.EventCollectorRetainsOrderedSnapshotsAndClearsWithoutChangingCapacity)),
            [typeof(XmlEventEmitter)] = Anchor(typeof(XmlEventEmitterTests), nameof(XmlEventEmitterTests.EmitsCompleteDocumentStreamThatRebuildsTheRetainedTree)),
            [typeof(XmlIncludeProcessor)] = Anchor(typeof(XmlIncludeProcessorTests), nameof(XmlIncludeProcessorTests.IncludesXmlRecursivelyAndUsesResolvedDocumentUriAsNestedBase)),
            [typeof(XmlParser)] = Anchor(typeof(XmlParserTests), nameof(XmlParserTests.ParsesEveryXmlInputRepresentation)),
            [typeof(XmlPullReader)] = Anchor(typeof(PushPullApiCoverageTests), nameof(PushPullApiCoverageTests.PullApiExposesEveryEventKindAndReaderStateField)),
            [typeof(XmlPushParser)] = Anchor(typeof(PushPullApiCoverageTests), nameof(PushPullApiCoverageTests.PushHandlerActionsProduceTheirDocumentedTransitions)),
            [typeof(XmlSchemaCompiler)] = Anchor(typeof(XmlSchemaCompilerTests), nameof(XmlSchemaCompilerTests.CompilesIncludesImportsAndChameleonSchemasIntoOneResolvedSet)),
            [typeof(XmlSchemaValidator)] = Anchor(typeof(XmlSchemaValidatorTests), nameof(XmlSchemaValidatorTests.ValidatesAndAnnotatesTreesThroughThePublicApi)),
            [typeof(XmlSerializer)] = Anchor(typeof(XmlSerializerTests), nameof(XmlSerializerTests.SerializesEveryPublicInputFormThroughTheSelectedMethod)),
            [typeof(XmlTreeEditor)] = Anchor(typeof(XmlTreeEditorCoreTests), nameof(XmlTreeEditorCoreTests.AppendsInsertsRepositionsReplacesAndRemovesInOrder)),
            [typeof(XmlTreeNavigator)] = Anchor(typeof(XmlTreeNavigatorTests), nameof(XmlTreeNavigatorTests.NavigatesParentsChildrenAttributesAndSelectionMetadata)),
            [typeof(XsltCompiler)] = Anchor(typeof(XsltPublicCompilerTests), nameof(XsltPublicCompilerTests.ReusesOneCompilerConcurrentlyAndPublishesThreadSafeExecutables)),
            [typeof(XsltExtensionInstructionAdapter)] = Anchor(typeof(ImplementationShellTests), nameof(ImplementationShellTests.XsltShellsPreserveRequestsAndResultDocumentTransitions)),
            [typeof(XsltMessageCollector)] = xslt,
            [typeof(XsltPackageResolver)] = Anchor(typeof(XsltPublicCompilerTests), nameof(XsltPublicCompilerTests.ReusesOnePackageResolverConcurrently)),
            [typeof(XsltResultDocumentCollector)] = Anchor(typeof(ImplementationShellTests), nameof(ImplementationShellTests.XsltShellsPreserveRequestsAndResultDocumentTransitions)),
            [typeof(XsltStreamAnalyzer)] = Anchor(typeof(ImplementationShellTests), nameof(ImplementationShellTests.XsltShellsPreserveRequestsAndResultDocumentTransitions)),
            [typeof(XsltTraceCollector)] = xslt,
            [typeof(XsltTransformer)] = xslt
        };
    }

    private static Dictionary<Type, CoverageAnchor> OptionAnchors()
    {
        return new Dictionary<Type, CoverageAnchor>
        {
            [typeof(XPathDynamicContext)] = Anchor(typeof(XPathPublicIntegrationTests), nameof(XPathPublicIntegrationTests.CompiledExpressionCanBeEvaluatedConcurrentlyWithIndependentContexts)),
            [typeof(XPathStaticContext)] = Anchor(typeof(XPathPublicIntegrationTests), nameof(XPathPublicIntegrationTests.FunctionResolverBindsOnceAtCompilationAndReusesTheImplementation)),
            [typeof(XmlCanonicalizationOptions)] = Anchor(typeof(XmlCanonicalizerPublicTests), nameof(XmlCanonicalizerPublicTests.CanonicalizerWritesRawCanonicalBytesWithoutMutatingInputs)),
            [typeof(XmlExtensionPolicy)] = Anchor(typeof(ExtensionSafetyTests), nameof(ExtensionSafetyTests.TrustPolicyRejectsDeniedNamespacesAndMalformedSignatures)),
            [typeof(XmlIncludeOptions)] = Anchor(typeof(XmlIncludeProcessorTests), nameof(XmlIncludeProcessorTests.IncludesXmlRecursivelyAndUsesResolvedDocumentUriAsNestedBase)),
            [typeof(XmlParserOptions)] = Anchor(typeof(XmlParserTests), nameof(XmlParserTests.AppliesDefaultLexicalPreservationChoices)),
            [typeof(XmlResolverPolicy)] = Anchor(typeof(ResolverGuardTests), nameof(ResolverGuardTests.RestrictsSchemesCaseInsensitivelyAndResolvesRelativeUris)),
            [typeof(XmlSchemaValidationOptions)] = Anchor(typeof(XmlSchemaPublicCoverageTests), nameof(XmlSchemaPublicCoverageTests.PublicCompilerAndValidatorCoverEveryFacetAndDerivationFamily)),
            [typeof(XmlSecurityLimits)] = Anchor(typeof(XmlParserSecurityTests), nameof(XmlParserSecurityTests.StopsOwnedCharacterInputAtTheConfiguredCharacterLimit)),
            [typeof(XmlSerializationOptions)] = Anchor(typeof(SerializationPublicCoverageTests), nameof(SerializationPublicCoverageTests.DispatchesEveryOutputMethodAndEveryBuiltInExtensionName)),
            [typeof(XsltCompileOptions)] = Anchor(typeof(XsltPublicCompilerTests), nameof(XsltPublicCompilerTests.DeeplySnapshotsMutableCompilationOptions)),
            [typeof(XsltTransformOptions)] = Anchor(typeof(XsltPublicRuntimeTests), nameof(XsltPublicRuntimeTests.TransformerExecutesAndPublishesMessagesResultsAndTraceEvents))
        };
    }

    private static Dictionary<Type, CoverageAnchor> CallbackAnchors()
    {
        Dictionary<Type, CoverageAnchor> interfaces = InterfaceAnchors();
        Type[] callbackTypes =
        [
            typeof(IXPathCollation), typeof(IXPathCollectionResolver), typeof(IXPathFunction), typeof(IXPathFunctionResolver), typeof(IXPathTraceReceiver), typeof(IXPathVariableResolver),
            typeof(IXmlByteSink), typeof(IXmlCancellation), typeof(IXmlEnumValuesProvider), typeof(IXmlErrorReporter), typeof(IXmlEventHandler), typeof(IXmlResourceResolver), typeof(IXmlSerializationMethod),
            typeof(IXsltExtensionInstruction), typeof(IXsltMessageReceiver), typeof(IXsltPackageResolver), typeof(IXsltResultDocumentResolver), typeof(IXsltStreamAnalyzer), typeof(IXsltTraceReceiver)
        ];
        return callbackTypes.ToDictionary(type => type, type => interfaces[type]);
    }

    private static Dictionary<Type, CoverageAnchor> ExceptionRuntimeAnchors()
    {
        CoverageAnchor construction = Anchor(typeof(ExceptionTests), nameof(ExceptionTests.EveryExceptionPreservesMessageDiagnosticAndLocation));
        return new Dictionary<Type, CoverageAnchor>
        {
            [typeof(XmlException)] = Anchor(typeof(PublicFailureInjectionTests), nameof(PublicFailureInjectionTests.HandlerFailuresAreTypedAndDoNotLeavePendingEvents)),
            [typeof(XmlWellFormednessException)] = Anchor(typeof(XmlParserTests), nameof(XmlParserTests.MapsStructuralFailuresAndReportsTheSameFatalDiagnostic)),
            [typeof(XmlNamespaceException)] = Anchor(typeof(NamespaceIntegrationTests), nameof(NamespaceIntegrationTests.ParserRejectsUnboundPrefixesAndExpandedAttributeDuplicates)),
            [typeof(XmlValidationException)] = Anchor(typeof(DtdValidationPolicyTests), nameof(DtdValidationPolicyTests.UsesFailFastValidityReportingByDefault)),
            [typeof(XmlResourceException)] = Anchor(typeof(ResolverGuardTests), nameof(ResolverGuardTests.DeniesExternalAccessByDefaultWithAResourceDiagnostic)),
            [typeof(XmlLimitException)] = Anchor(typeof(XmlParserSecurityTests), nameof(XmlParserSecurityTests.StopsOwnedCharacterInputAtTheConfiguredCharacterLimit)),
            [typeof(XmlSerializationException)] = Anchor(typeof(XmlSerializerTests), nameof(XmlSerializerTests.ReportsStandardCodesForInvalidParameterDomains)),
            [typeof(XmlCanonicalizationException)] = Anchor(typeof(XmlCanonicalizerPublicTests), nameof(XmlCanonicalizerPublicTests.CanonicalizerRejectsUnsupportedMethodsBeforeWritingOutput)),
            [typeof(XmlSchemaException)] = Anchor(typeof(XmlSchemaPublicCoverageTests), nameof(XmlSchemaPublicCoverageTests.PublicSchemaApisDistinguishFatalInvalidAndNotKnownOutcomes)),
            [typeof(XPathException)] = construction,
            [typeof(XPathStaticException)] = Anchor(typeof(XPathMathFunctionTests), nameof(XPathMathFunctionTests.RejectsUnknownMathFunctionsAndUnsupportedArities)),
            [typeof(XPathTypeException)] = Anchor(typeof(XPathMathFunctionTests), nameof(XPathMathFunctionTests.ConvertsNumericArgumentsToDoubleAndReportsTypeErrors)),
            [typeof(XPathDynamicException)] = Anchor(typeof(XPathPublicIntegrationTests), nameof(XPathPublicIntegrationTests.TransformAndErrorDiagnosticsCrossThePublicEvaluationBoundary)),
            [typeof(XsltException)] = construction,
            [typeof(XsltStaticException)] = Anchor(typeof(XsltCompilationConformanceTests), nameof(XsltCompilationConformanceTests.ReportsRepresentativeStaticCompilationErrors)),
            [typeof(XsltTypeException)] = Anchor(typeof(XsltTemplateRuntimeTests), nameof(XsltTemplateRuntimeTests.EnforcesTemplateContextDeclarationsDuringInvocation)),
            [typeof(XsltDynamicException)] = Anchor(typeof(XsltErrorRuntimeTests), nameof(XsltErrorRuntimeTests.UnhandledErrorsCarrySourceAndTemplateStack)),
            [typeof(XsltTerminationException)] = Anchor(typeof(XsltErrorRuntimeTests), nameof(XsltErrorRuntimeTests.UnhandledTerminatingMessageThrowsTypedTermination))
        };
    }

    private sealed class CoverageAnchor
    {
        internal Type Fixture;
        internal string Method;

        internal CoverageAnchor(Type fixture, string method)
        {
            Fixture = fixture;
            Method = method;
        }

        internal string DisplayName()
        {
            return Fixture.Name + "." + Method;
        }
    }
}
