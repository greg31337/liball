using System.Security.Cryptography;
using System.Text.Json.Nodes;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfTransformerExecutionTests
{
    [Test]
    public void TransformDocument_ExecutesPlanInOrderAndReportsDeterministicMetrics()
    {
        GltfDocument source = CreateDocument("source");
        OrderedHook hook = new OrderedHook();
        GltfTransformPlan plan = new GltfTransformPlan
        {
            Steps =
            [
                new GltfTransformStep { Kind = GltfTransformKind.Custom, Name = "first" },
                new GltfTransformStep
                {
                    Kind = GltfTransformKind.Custom,
                    Name = "disabled",
                    Enabled = false
                },
                new GltfTransformStep { Kind = GltfTransformKind.Custom, Name = "second" }
            ]
        };
        GltfTransformOptions options = CreateOptions();
        options.Hooks.Add(hook);
        byte[] expectedOriginal = WriteDeterministicJson(source);

        GltfTransformReport report = new GltfTransformer().TransformDocument(
            source,
            plan,
            options);
        byte[] expectedResult = WriteDeterministicJson(report.Document!);

        Assert.Multiple(() =>
        {
            Assert.That(hook.StepNames, Is.EqualTo(new[] { "first", "second" }));
            Assert.That(hook.ObservedNodeNames,
                Is.EqualTo(new[] { "source", "source-first" }));
            Assert.That(report.Document!.Nodes[0].Name,
                Is.EqualTo("source-first-second"));
            Assert.That(report.Steps.Select(step => step.Status),
                Is.EqualTo(new[]
                {
                    GltfOperationStatus.Success,
                    GltfOperationStatus.Skipped,
                    GltfOperationStatus.Success
                }));
            Assert.That(report.Steps[1].Reason,
                Is.EqualTo("The transform step is disabled."));
            Assert.That(report.OriginalByteLength,
                Is.EqualTo(expectedOriginal.LongLength));
            Assert.That(report.ResultByteLength,
                Is.EqualTo(expectedResult.LongLength));
            Assert.That(report.OriginalHash, Is.EqualTo(HashBytes(expectedOriginal)));
            Assert.That(report.ResultHash, Is.EqualTo(HashBytes(expectedResult)));
            Assert.That(report.ResultHash, Is.Not.EqualTo(report.OriginalHash));
            Assert.That(source.Nodes[0].Name, Is.EqualTo("source"));
        });
    }

    [Test]
    public void TransformDocument_EmptyPlanHasStableEqualInputAndResultMetrics()
    {
        GltfDocument source = CreateDocument("unchanged");

        GltfTransformReport report = new GltfTransformer().TransformDocument(
            source,
            new GltfTransformPlan(),
            CreateOptions());

        Assert.Multiple(() =>
        {
            Assert.That(report.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(report.Document, Is.Not.SameAs(source));
            Assert.That(report.Steps, Is.Empty);
            Assert.That(report.ResultByteLength,
                Is.EqualTo(report.OriginalByteLength));
            Assert.That(report.ResultHash, Is.EqualTo(report.OriginalHash));
            Assert.That(report.OriginalHash,
                Does.Match("^[0-9a-f]{64}$"));
        });
    }

    [Test]
    public void TransformDocument_RollbackMetricsDescribeTheEffectiveFinalState()
    {
        GltfDocument rolledBackSource = CreateDocument("source");
        FailingHook rolledBackHook = new FailingHook("failed-change");
        GltfTransformOptions rolledBackOptions = CreateOptions();
        rolledBackOptions.Hooks.Add(rolledBackHook);

        GltfTransformReport rolledBack = new GltfTransformer().TransformDocument(
            rolledBackSource,
            CreateCustomPlan(),
            rolledBackOptions);

        GltfDocument partialSource = CreateDocument("source");
        FailingHook partialHook = new FailingHook("failed-change");
        GltfTransformOptions partialOptions = CreateOptions();
        partialOptions.RollbackOnFailure = false;
        partialOptions.Hooks.Add(partialHook);
        GltfTransformReport partial = new GltfTransformer().TransformDocument(
            partialSource,
            CreateCustomPlan(),
            partialOptions);
        byte[] expectedPartial = WriteDeterministicJson(partial.Document!);

        Assert.Multiple(() =>
        {
            Assert.That(rolledBack.Status, Is.EqualTo(GltfOperationStatus.Failed));
            Assert.That(rolledBack.Document, Is.Null);
            Assert.That(rolledBack.ResultByteLength,
                Is.EqualTo(rolledBack.OriginalByteLength));
            Assert.That(rolledBack.ResultHash, Is.EqualTo(rolledBack.OriginalHash));
            Assert.That(rolledBackSource.Nodes[0].Name, Is.EqualTo("source"));
            Assert.That(partial.Status, Is.EqualTo(GltfOperationStatus.Failed));
            Assert.That(partial.Document!.Nodes[0].Name, Is.EqualTo("failed-change"));
            Assert.That(partial.ResultByteLength,
                Is.EqualTo(expectedPartial.LongLength));
            Assert.That(partial.ResultHash, Is.EqualTo(HashBytes(expectedPartial)));
            Assert.That(partial.ResultHash, Is.Not.EqualTo(partial.OriginalHash));
            Assert.That(partialSource.Nodes[0].Name, Is.EqualTo("source"));
        });
    }

    [Test]
    public void TransformDocument_BlocksOptionalUnknownExtensionsUnlessOptedOut()
    {
        GltfDocument source = CreateDocument("source");
        source.PropertyJson.Extensions["EXT_optional_unknown"] =
            new JsonObject { ["object"] = 0 };
        OrderedHook blockedHook = new OrderedHook();
        GltfTransformOptions blockedOptions = CreateOptions();
        blockedOptions.Hooks.Add(blockedHook);

        Assert.That(
            () => new GltfTransformer().TransformDocument(
                source,
                CreateCustomPlan(),
                blockedOptions),
            Throws.TypeOf<GltfUnsupportedFeatureException>()
                .With.Message.Contains("EXT_optional_unknown"));
        Assert.That(blockedHook.StepNames, Is.Empty);

        OrderedHook allowedHook = new OrderedHook();
        GltfTransformOptions allowedOptions = CreateOptions();
        allowedOptions.RequireSafeUnknownExtensions = false;
        allowedOptions.Hooks.Add(allowedHook);
        GltfTransformReport allowed = new GltfTransformer().TransformDocument(
            source,
            CreateCustomPlan(),
            allowedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(allowed.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(allowedHook.StepNames, Is.EqualTo(new[] { "custom" }));
            Assert.That(source.Nodes[0].Name, Is.EqualTo("source"));
        });
    }

    [Test]
    public void TransformDocument_AcceptsMutationSafeRegisteredExtensionMetadata()
    {
        GltfDocument source = CreateDocument("source");
        source.PropertyJson.Extensions["EXT_registered"] =
            new JsonObject { ["object"] = 0 };
        OrderedHook hook = new OrderedHook();
        GltfTransformOptions options = CreateOptions();
        options.ValidationOptions.ExtensionHandlers.Add(
            new SafeExtensionHandler());
        options.Hooks.Add(hook);

        GltfTransformReport report = new GltfTransformer().TransformDocument(
            source,
            CreateCustomPlan(),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(report.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(hook.StepNames, Is.EqualTo(new[] { "custom" }));
            Assert.That(report.OriginalHash, Is.Not.Null);
            Assert.That(report.ResultHash, Is.Not.Null);
        });
    }

    [Test]
    public void TransformDocument_BlocksUnknownReferencesDuringBufferRepacking()
    {
        GltfDocument source = CreateDocument("source");
        source.PropertyJson.Extensions["EXT_unknown_buffer_reference"] =
            new JsonObject { ["bufferView"] = 0 };
        source.Buffers.Add(new GltfBuffer
        {
            ByteLength = 4,
            Resolved = true,
            ResolvedBytes = new byte[] { 1, 2, 3, 4 }
        });
        source.BufferViews.Add(new GltfBufferView
        {
            Buffer = 0,
            ByteLength = 4
        });
        GltfTransformPlan plan = new GltfTransformPlan
        {
            Steps = [new GltfTransformStep
            {
                Kind = GltfTransformKind.PackBuffers
            }]
        };

        Assert.That(
            () => new GltfTransformer().TransformDocument(
                source,
                plan,
                CreateOptions()),
            Throws.TypeOf<GltfUnsupportedFeatureException>()
                .With.Message.Contains("cannot safely remap references"));

        GltfTransformOptions unsafeOptions = CreateOptions();
        unsafeOptions.RequireSafeUnknownExtensions = false;
        GltfTransformReport report = new GltfTransformer().TransformDocument(
            source,
            plan,
            unsafeOptions);

        Assert.Multiple(() =>
        {
            Assert.That(report.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(report.Document!.Buffers, Has.Count.EqualTo(1));
            Assert.That(source.Buffers[0].ResolvedBytes,
                Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
        });
    }

    [Test]
    public void TransformDocument_ValidatesBeforeInvokingAnyStep()
    {
        GltfDocument source = CreateDocument("source");
        source.Nodes[0].HasMesh = true;
        source.Nodes[0].Mesh = 4;
        OrderedHook hook = new OrderedHook();
        GltfTransformOptions options = new GltfTransformOptions();
        options.Hooks.Add(hook);

        Assert.That(
            () => new GltfTransformer().TransformDocument(
                source,
                CreateCustomPlan(),
                options),
            Throws.TypeOf<GltfValidationException>());
        Assert.That(hook.StepNames, Is.Empty);
    }

    private static GltfDocument CreateDocument(string nodeName)
    {
        GltfDocument result = new GltfDocument();
        result.Nodes.Add(new GltfNode { Name = nodeName });
        return result;
    }

    private static GltfTransformPlan CreateCustomPlan()
    {
        return new GltfTransformPlan
        {
            Steps = [new GltfTransformStep
            {
                Kind = GltfTransformKind.Custom,
                Name = "custom"
            }]
        };
    }

    private static GltfTransformOptions CreateOptions()
    {
        return new GltfTransformOptions
        {
            ValidateBefore = false,
            ValidateAfter = false
        };
    }

    private static byte[] WriteDeterministicJson(GltfDocument document)
    {
        return new GltfWriter().WriteBytes(
            document,
            new GltfWriteOptions
            {
                Format = GltfContainerFormat.Json,
                ResourceMode = GltfResourceMode.Preserve,
                Deterministic = true,
                Validate = false
            }).PrimaryBytes;
    }

    private static string HashBytes(byte[] bytes)
    {
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private sealed class OrderedHook : IGltfTransformHook
    {
        public List<string> StepNames = new List<string>();
        public List<string> ObservedNodeNames = new List<string>();

        public GltfTransformStepResult ApplyTransformHook(GltfTransformContext context)
        {
            string stepName = context.Step!.Name ?? "";
            string nodeName = context.Document!.Nodes[0].Name ?? "";
            StepNames.Add(stepName);
            ObservedNodeNames.Add(nodeName);
            context.Document.Nodes[0].Name = nodeName + "-" + stepName;
            GltfTransformStepResult result = new GltfTransformStepResult();
            result.ChangedObjects.Add("nodes/0");
            return result;
        }
    }

    private sealed class FailingHook : IGltfTransformHook
    {
        private readonly string _nodeName;

        public FailingHook(string nodeName)
        {
            _nodeName = nodeName;
        }

        public GltfTransformStepResult ApplyTransformHook(GltfTransformContext context)
        {
            context.Document!.Nodes[0].Name = _nodeName;
            GltfTransformStepResult result = new GltfTransformStepResult();
            result.Status = GltfOperationStatus.Failed;
            result.ChangedObjects.Add("nodes/0");
            return result;
        }
    }

    private sealed class SafeExtensionHandler : IGltfExtensionHandler
    {
        public GltfExtensionDescriptor DescribeExtension()
        {
            return new GltfExtensionDescriptor
            {
                Name = "EXT_registered",
                CanRemapReferences = true
            };
        }

        public GltfExtensionResult ReadExtension(GltfExtensionContext context)
        {
            return CreateResult(context);
        }

        public GltfExtensionResult WriteExtension(GltfExtensionContext context)
        {
            return CreateResult(context);
        }

        public GltfExtensionResult ValidateExtension(GltfExtensionContext context)
        {
            return CreateResult(context);
        }

        public GltfExtensionResult RemapExtensionReferences(
            GltfExtensionContext context,
            GltfIndexRemap remap)
        {
            return CreateResult(context);
        }

        private static GltfExtensionResult CreateResult(GltfExtensionContext context)
        {
            return new GltfExtensionResult
            {
                Descriptor = new GltfExtensionDescriptor
                {
                    Name = "EXT_registered",
                    CanRemapReferences = true
                },
                Json = context.Json?.DeepClone()
            };
        }
    }
}
