using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using asn1;

namespace asn1.Tests;

public class PublicApiCoverageMatrixTests
{
    private const string ExpectedMatrixFingerprint =
        "39D45BB4EA6F2879D9AB70367ADD567B254F925BC4FD7C0D7A18FAF83AC46450";

    [Test]
    public void EveryPublicApiBehaviorMapsToAnExecutableTest()
    {
        CoverageEntry[] matrix = BuildMatrix();
        string[] keys = matrix.Select(entry => entry.Category + ":" + entry.ApiBehavior)
            .ToArray();

        Assert.That(keys, Is.Unique);
        Assert.That(matrix, Is.Not.Empty);
        foreach (CoverageEntry entry in matrix)
        {
            MethodInfo? method = entry.TestType.GetMethod(
                entry.TestMethod,
                BindingFlags.Instance | BindingFlags.Public);
            Assert.That(method, Is.Not.Null,
                entry.ApiBehavior + " maps to a missing test method.");
            Assert.That(method?.GetCustomAttributes(false).Any(attribute =>
                    attribute.GetType().Namespace == "NUnit.Framework"),
                Is.True,
                entry.ApiBehavior + " maps to a method that is not executable by NUnit.");
        }

        string serialized = string.Join(
            "\n",
            matrix.Select(entry => entry.Category
                + "|" + entry.ApiBehavior
                + "|" + entry.TestType.FullName
                + "." + entry.TestMethod));
        string fingerprint = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(serialized)));

        Assert.That(fingerprint, Is.EqualTo(ExpectedMatrixFingerprint),
            "The public API surface or its behavioral-test mapping changed. "
                + "Map the new behavior deliberately before accepting the new fingerprint. "
                + DescribeCounts(matrix));
    }

    private static CoverageEntry[] BuildMatrix()
    {
        Assembly apiAssembly = typeof(AsnCompiler).Assembly;
        Type[] publicTypes = apiAssembly.GetExportedTypes()
            .Where(type => type.Namespace == "asn1")
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
        List<CoverageEntry> matrix = [];

        foreach (Type type in publicTypes.Where(type => type.IsInterface))
        {
            CoverageReference reference = InterfaceReference(type);
            foreach (MethodInfo method in type.GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                matrix.Add(new CoverageEntry(
                    "interface-method",
                    MethodIdentity(type, method),
                    reference));
            }
        }

        foreach (Type type in publicTypes.Where(IsImplementationClass))
        {
            matrix.Add(new CoverageEntry(
                "implementation-class",
                type.FullName ?? type.Name,
                ImplementationReference(type)));
        }

        foreach (Type type in publicTypes.Where(IsDataClass))
        {
            foreach (FieldInfo field in type.GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                matrix.Add(new CoverageEntry(
                    "data-field-invariant",
                    (type.FullName ?? type.Name) + "." + field.Name,
                    DataReference(type)));
            }

            foreach (MethodInfo method in type.GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                matrix.Add(new CoverageEntry(
                    "data-method",
                    MethodIdentity(type, method),
                    DataReference(type)));
            }
        }

        foreach (Type type in publicTypes.Where(type => type.IsEnum))
        {
            foreach (FieldInfo field in type.GetFields(
                BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                matrix.Add(new CoverageEntry(
                    "enum-value",
                    (type.FullName ?? type.Name) + "." + field.Name,
                    new CoverageReference(
                        typeof(PublicEnumTests),
                        nameof(PublicEnumTests.EnumNamesAndValuesAreStable))));
            }
        }

        foreach (Type type in publicTypes.Where(type =>
            type.IsClass && type.IsAbstract && type.IsSealed))
        {
            foreach (FieldInfo field in type.GetFields(
                BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                matrix.Add(new CoverageEntry(
                    "static-field",
                    (type.FullName ?? type.Name) + "." + field.Name,
                    StaticReference(type)));
            }
        }

        foreach (Type type in publicTypes.Where(type =>
            type.IsClass && typeof(Exception).IsAssignableFrom(type)))
        {
            matrix.Add(new CoverageEntry(
                "exception-payload",
                (type.FullName ?? type.Name) + ".shared-payload",
                new CoverageReference(
                    typeof(ExceptionTests),
                    nameof(ExceptionTests.EveryExceptionPreservesTheSharedErrorPayload))));
            matrix.Add(new CoverageEntry(
                "exception-null-normalization",
                (type.FullName ?? type.Name) + ".null-normalization",
                new CoverageReference(
                    typeof(ExceptionTests),
                    nameof(ExceptionTests.EveryExceptionNormalizesNullPayloadParts))));
            matrix.Add(new CoverageEntry(
                "exception-throwing",
                (type.FullName ?? type.Name) + ".throw-catch",
                new CoverageReference(
                    typeof(ExceptionTests),
                    nameof(ExceptionTests.ExceptionsUseNormalThrowingBehavior))));
        }

        return matrix.OrderBy(entry => entry.Category, StringComparer.Ordinal)
            .ThenBy(entry => entry.ApiBehavior, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool IsImplementationClass(Type type)
    {
        return type.IsClass
            && !type.IsAbstract
            && !typeof(Exception).IsAssignableFrom(type)
            && type.GetInterfaces().Any(contract =>
                contract.IsPublic && contract.Assembly == type.Assembly);
    }

    private static bool IsDataClass(Type type)
    {
        return type.IsClass
            && !type.IsAbstract
            && !typeof(Exception).IsAssignableFrom(type)
            && !type.GetInterfaces().Any(contract =>
                contract.IsPublic && contract.Assembly == type.Assembly);
    }

    private static string MethodIdentity(Type type, MethodInfo method)
    {
        string parameters = string.Join(
            ",",
            method.GetParameters().Select(parameter =>
                parameter.ParameterType.ToString()));
        return (type.FullName ?? type.Name) + "." + method.Name
            + "(" + parameters + ")";
    }

    private static CoverageReference InterfaceReference(Type type)
    {
        if (type == typeof(IAsnObjectSetRegistry))
        {
            return new CoverageReference(
                typeof(AsnObjectSetRegistryTests),
                nameof(AsnObjectSetRegistryTests.RegistryCombinesStaticAndDynamicEntriesWithoutMutatingSchema));
        }

        if (type == typeof(IEcnCompiler)
            || type == typeof(IXsdSchemaImporter)
            || type == typeof(IPerInstructionRegistry)
            || type == typeof(IPerInstructionHandler))
        {
            return new CoverageReference(
                typeof(OperationInterfaceTests),
                nameof(OperationInterfaceTests.EcnXsdAndPerExtensionContractsAreSynchronousAndComposable));
        }

        if (type == typeof(IAsnCodec)
            || type == typeof(IAsnCodecProvider)
            || type == typeof(IAsnStreamingCodec)
            || type == typeof(IAsnIncrementalDecoder)
            || type == typeof(IBerTlvReader)
            || type == typeof(IBerTlvWriter))
        {
            return new CoverageReference(
                typeof(OperationInterfaceTests),
                nameof(OperationInterfaceTests.CodecContractsSupportOneShotSpanStreamAndIncrementalCalls));
        }

        return new CoverageReference(
            typeof(OperationInterfaceTests),
            nameof(OperationInterfaceTests.CompilerAndSchemaContractsCarryTheirDedicatedResults));
    }

    private static CoverageReference ImplementationReference(Type type)
    {
        if (type == typeof(AsnObjectSetRegistry))
        {
            return new CoverageReference(
                typeof(AsnObjectSetRegistryTests),
                nameof(AsnObjectSetRegistryTests.RegistryCombinesStaticAndDynamicEntriesWithoutMutatingSchema));
        }

        if (type == typeof(AsnRuntimeConstraintEvaluator))
        {
            return new CoverageReference(
                typeof(RuntimeConstraintEvaluatorTests),
                nameof(RuntimeConstraintEvaluatorTests.EvaluatorExecutesNormalizedSetOperationsAndExtensionPartitions));
        }

        if (type == typeof(DefaultAsnCodecProvider))
        {
            return new CoverageReference(
                typeof(GeneratedEndToEndPipelineTests),
                nameof(GeneratedEndToEndPipelineTests.GeneratedEncoderFeedsEveryCompatibleDynamicRuntime));
        }

        if (type == typeof(AsnCodec)
            || type == typeof(AsnCodecProvider)
            || type == typeof(AsnStreamingCodec)
            || type == typeof(AsnIncrementalDecoder))
        {
            return new CoverageReference(
                typeof(ImplementationFacadeTests),
                nameof(ImplementationFacadeTests.CodecFacadesForwardBufferStreamAndIncrementalOperations));
        }

        if (type == typeof(BerTlvReader)
            || type == typeof(BerTlvWriter)
            || type == typeof(PerInstructionRegistry))
        {
            return new CoverageReference(
                typeof(ImplementationFacadeTests),
                nameof(ImplementationFacadeTests.TlvAndRegistryFacadesForwardOperations));
        }

        if (type == typeof(AsnCompiler)
            || type == typeof(AsnLexer)
            || type == typeof(AsnParser))
        {
            return new CoverageReference(
                typeof(ImplementationFacadeTests),
                nameof(ImplementationFacadeTests.CompilerLexerAndParserFacadesForwardOperations));
        }

        return new CoverageReference(
            typeof(ImplementationFacadeTests),
            nameof(ImplementationFacadeTests.ValidationGenerationReflectionAndImportFacadesForwardOperations));
    }

    private static CoverageReference DataReference(Type type)
    {
        if (type.Name.StartsWith("Ber", StringComparison.Ordinal))
        {
            return new CoverageReference(
                typeof(BerTlvDataTests),
                nameof(BerTlvDataTests.TlvRequestsResultsAndEventsRetainExactProgress));
        }

        if (type == typeof(AsnSourceUnit)
            || type == typeof(AsnSourcePosition)
            || type == typeof(AsnSourceSpan)
            || type == typeof(AsnDiagnostic)
            || type == typeof(AsnDiagnosticRelatedLocation))
        {
            return new CoverageReference(
                typeof(SourceLocationTests),
                nameof(SourceLocationTests.DiagnosticCarriesPrimaryAndRelatedLocations));
        }

        if (type == typeof(AsnResourceLimits))
        {
            return new CoverageReference(
                typeof(ResourceLimitTests),
                nameof(ResourceLimitTests.DefaultsBoundEveryResourceDimension));
        }

        if (type == typeof(AsnBitStringValue))
        {
            return new CoverageReference(
                typeof(RuntimeInvariantTests),
                nameof(RuntimeInvariantTests.BitStringRequiresMinimalBytesAndZeroUnusedBits));
        }

        if (type == typeof(AsnObjectIdentifierValue))
        {
            return new CoverageReference(
                typeof(RuntimeInvariantTests),
                nameof(RuntimeInvariantTests.ObjectIdentifierEnforcesArcInvariantsWithoutSizeLimits));
        }

        if (type == typeof(AsnRealValue))
        {
            return new CoverageReference(
                typeof(RuntimeInvariantTests),
                nameof(RuntimeInvariantTests.RealNormalizationRejectsEveryContradictoryPayloadComponent));
        }

        if (type == typeof(AsnTimeValue))
        {
            return new CoverageReference(
                typeof(RuntimeInvariantTests),
                nameof(RuntimeInvariantTests.TimeMetadataEnforcesOffsetAndPrecisionBounds));
        }

        if (type == typeof(AsnOpenTypeValue))
        {
            return new CoverageReference(
                typeof(RuntimeInvariantTests),
                nameof(RuntimeInvariantTests.OpenTypesRejectEveryMalformedIdentityPayloadAndLocation));
        }

        if (type == typeof(AsnUnknownExtension)
            || type == typeof(AsnUnknownField))
        {
            return new CoverageReference(
                typeof(RuntimeInvariantTests),
                nameof(RuntimeInvariantTests.UnknownDataRejectsEveryInvalidLocationAndPayloadCombination));
        }

        if (type.Name.EndsWith("Request", StringComparison.Ordinal)
            || type.Name.EndsWith("Result", StringComparison.Ordinal)
            || type.Name.EndsWith("Options", StringComparison.Ordinal)
            || type.Name.EndsWith("Input", StringComparison.Ordinal)
            || type == typeof(AsnCodecOptions)
            || type == typeof(AsnStreamProgress)
            || type == typeof(AsnGeneratedFile))
        {
            return new CoverageReference(
                typeof(RequestResultTests),
                nameof(RequestResultTests.CodecPayloadsPreserveValuesProgressAndDiagnostics));
        }

        return new CoverageReference(
            typeof(PublicModelGraphTests),
            nameof(PublicModelGraphTests.SchemaInformationPlanAndReflectionGraphsRetainRelationships));
    }

    private static CoverageReference StaticReference(Type type)
    {
        if (type == typeof(AsnStandardEditions))
        {
            return new CoverageReference(
                typeof(StaticDataTests),
                nameof(StaticDataTests.StandardEditionIdentifiersMatchTheSupportedCorpus));
        }

        if (type == typeof(AsnUniversalTags))
        {
            return new CoverageReference(
                typeof(StaticDataTests),
                nameof(StaticDataTests.UniversalTagNumbersMatchTheX680Assignments));
        }

        return new CoverageReference(
            typeof(StaticDataTests),
            nameof(StaticDataTests.EncodingObjectIdentifiersMatchTheStandardAssignments));
    }

    private static string DescribeCounts(CoverageEntry[] matrix)
    {
        return string.Join(
            ", ",
            matrix.GroupBy(entry => entry.Category)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => group.Key + "=" + group.Count()));
    }

    private class CoverageReference
    {
        internal Type TestType;
        internal string TestMethod;

        internal CoverageReference(Type testType, string testMethod)
        {
            this.TestType = testType;
            this.TestMethod = testMethod;
        }
    }

    private class CoverageEntry
    {
        internal string Category;
        internal string ApiBehavior;
        internal Type TestType;
        internal string TestMethod;

        internal CoverageEntry(
            string category,
            string apiBehavior,
            CoverageReference reference)
        {
            this.Category = category;
            this.ApiBehavior = apiBehavior;
            this.TestType = reference.TestType;
            this.TestMethod = reference.TestMethod;
        }
    }
}
