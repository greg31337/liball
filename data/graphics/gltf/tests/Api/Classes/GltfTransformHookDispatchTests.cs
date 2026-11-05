namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfTransformHookDispatchTests
{
    [TestCase(GltfTransformKind.GenerateTangents)]
    [TestCase(GltfTransformKind.GenerateTextureCoordinates)]
    [TestCase(GltfTransformKind.ReorderVertices)]
    [TestCase(GltfTransformKind.Simplify)]
    [TestCase(GltfTransformKind.GenerateLod)]
    [TestCase(GltfTransformKind.EncodeDraco)]
    [TestCase(GltfTransformKind.DecodeDraco)]
    [TestCase(GltfTransformKind.EncodeMeshopt)]
    [TestCase(GltfTransformKind.DecodeMeshopt)]
    [TestCase(GltfTransformKind.ConvertImages)]
    [TestCase(GltfTransformKind.CreatePalette)]
    [TestCase(GltfTransformKind.Custom)]
    public void HookDrivenKinds_DispatchWithCompleteCapabilitiesAndIdentityRemap(
        GltfTransformKind kind)
    {
        GltfDocument source = CreateDocument();
        CapabilityProbe codec = new CapabilityProbe();
        DispatchHook hook = new DispatchHook("handled", GltfOperationStatus.Success);
        GltfTransformOptions options = CreateOptions();
        options.CompressionCodecs.Add(codec);
        options.ImageCodecs.Add(codec);
        options.Hooks.Add(hook);

        GltfTransformReport report = Transform(source, kind, options);
        GltfTransformContext context = hook.Context!;

        Assert.Multiple(() =>
        {
            Assert.That(report.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(report.Document!.Nodes[0].Name, Is.EqualTo("handled"));
            Assert.That(report.Steps[0].Step!.Kind, Is.EqualTo(kind));
            Assert.That(report.Steps[0].ChangedObjects,
                Is.EqualTo(new[] { "nodes/0" }));
            Assert.That(report.Steps[0].Remap.Nodes[0], Is.Zero);
            Assert.That(context.Step!.Kind, Is.EqualTo(kind));
            Assert.That(context.Document, Is.Not.SameAs(source));
            Assert.That(context.Resources, Is.SameAs(context.Document!.Resources));
            Assert.That(context.Remap.Nodes[0], Is.Zero);
            Assert.That(context.CompressionCodecs,
                Is.EqualTo(new IGltfCompressionCodec[] { codec }));
            Assert.That(context.ImageCodecs,
                Is.EqualTo(new IGltfImageCodec[] { codec }));
            Assert.That(context.Hooks,
                Is.EqualTo(new IGltfTransformHook[] { hook }));
            Assert.That(source.Nodes[0].Name, Is.EqualTo("original"));
        });
    }

    [Test]
    public void Dispatch_NegotiatesInRegistrationOrderWithIsolatedCandidates()
    {
        GltfDocument source = CreateDocument();
        DispatchHook declining = new DispatchHook(
            "declined mutation",
            GltfOperationStatus.Unavailable);
        declining.AddDiagnostic = true;
        DispatchHook accepting = new DispatchHook(
            "accepted mutation",
            GltfOperationStatus.Success);
        GltfTransformOptions options = CreateOptions();
        options.Hooks.Add(declining);
        options.Hooks.Add(accepting);

        GltfTransformReport report = Transform(
            source,
            GltfTransformKind.GenerateTangents,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(declining.CallCount, Is.EqualTo(1));
            Assert.That(accepting.CallCount, Is.EqualTo(1));
            Assert.That(declining.InitialNodeName, Is.EqualTo("original"));
            Assert.That(accepting.InitialNodeName, Is.EqualTo("original"));
            Assert.That(report.Document!.Nodes[0].Name,
                Is.EqualTo("accepted mutation"));
            Assert.That(report.Steps[0].Diagnostics, Is.Empty);
            Assert.That(accepting.Context!.Hooks,
                Is.EqualTo(new IGltfTransformHook[] { declining, accepting }));
            Assert.That(source.Nodes[0].Name, Is.EqualTo("original"));
        });
    }

    [Test]
    public void Dispatch_AllUnavailableAggregatesProviderDiagnosticsAndFinalReason()
    {
        GltfDocument source = CreateDocument();
        DispatchHook first = new DispatchHook(
            "first mutation",
            GltfOperationStatus.Unavailable);
        first.AddDiagnostic = true;
        DispatchHook second = new DispatchHook(
            "second mutation",
            GltfOperationStatus.Unavailable);
        second.AddDiagnostic = true;
        GltfTransformOptions options = CreateOptions();
        options.Hooks.Add(first);
        options.Hooks.Add(second);

        GltfTransformReport report = Transform(
            source,
            GltfTransformKind.EncodeDraco,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(report.Status, Is.EqualTo(GltfOperationStatus.Unavailable));
            Assert.That(report.Document, Is.Null);
            Assert.That(report.Steps[0].Status,
                Is.EqualTo(GltfOperationStatus.Unavailable));
            Assert.That(report.Steps[0].Reason,
                Is.EqualTo("No registered transform hook can encode Draco geometry."));
            Assert.That(report.Steps[0].Diagnostics.Select(issue => issue.Message),
                Is.EqualTo(new[]
                {
                    "first mutation unavailable",
                    "second mutation unavailable",
                    "No registered transform hook can encode Draco geometry."
                }));
            Assert.That(report.Steps[0].Diagnostics.Last().Code,
                Is.EqualTo(GltfIssueCodes.TransformHookUnavailable));
            Assert.That(report.Steps[0].Remap.Nodes[0], Is.Zero);
            Assert.That(source.Nodes[0].Name, Is.EqualTo("original"));
        });
    }

    [Test]
    public void Dispatch_TerminalHookMayReplaceDocumentAndResourceSet()
    {
        GltfDocument source = CreateDocument();
        DispatchHook hook = new DispatchHook("unused", GltfOperationStatus.Success);
        hook.ReplaceContext = true;
        GltfTransformOptions options = CreateOptions();
        options.Hooks.Add(hook);

        GltfTransformReport report = Transform(
            source,
            GltfTransformKind.CreatePalette,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(report.Document!.Nodes[0].Name, Is.EqualTo("replacement"));
            Assert.That(report.Document.Resources.Resources[0].Bytes,
                Is.EqualTo(new byte[] { 9 }));
            Assert.That(report.Resources, Is.SameAs(report.Document.Resources));
            Assert.That(source.Nodes[0].Name, Is.EqualTo("original"));
        });
    }

    [Test]
    public void Dispatch_RejectsIncompleteHookResultsWithoutLeakingMutations()
    {
        GltfDocument source = CreateDocument();
        DispatchHook hook = new DispatchHook("changed", GltfOperationStatus.Success);
        hook.ReturnIncompleteResult = true;
        GltfTransformOptions options = CreateOptions();
        options.Hooks.Add(hook);

        Assert.That(
            () => Transform(source, GltfTransformKind.ConvertImages, options),
            Throws.TypeOf<GltfUnsupportedFeatureException>()
                .With.Message.Contains("incomplete"));
        Assert.That(source.Nodes[0].Name, Is.EqualTo("original"));
    }

    [Test]
    public void Dispatch_RejectsNullRegisteredCodecCapabilitiesBeforeCallingHooks()
    {
        GltfDocument source = CreateDocument();
        DispatchHook hook = new DispatchHook("changed", GltfOperationStatus.Success);
        GltfTransformOptions options = CreateOptions();
        options.Hooks.Add(hook);
        options.ImageCodecs.Add(null!);

        Assert.That(
            () => Transform(source, GltfTransformKind.ConvertImages, options),
            Throws.ArgumentException.With.Message.Contains("Image codec 0"));
        Assert.Multiple(() =>
        {
            Assert.That(hook.CallCount, Is.Zero);
            Assert.That(source.Nodes[0].Name, Is.EqualTo("original"));
        });
    }

    private static GltfDocument CreateDocument()
    {
        GltfDocument result = new GltfDocument();
        result.Nodes.Add(new GltfNode { Name = "original" });
        return result;
    }

    private static GltfTransformOptions CreateOptions()
    {
        return new GltfTransformOptions
        {
            ValidateBefore = false,
            ValidateAfter = false
        };
    }

    private static GltfTransformReport Transform(
        GltfDocument document,
        GltfTransformKind kind,
        GltfTransformOptions options)
    {
        return new GltfTransformer().TransformDocument(
            document,
            new GltfTransformPlan
            {
                Steps = [new GltfTransformStep { Kind = kind }]
            },
            options);
    }

    private sealed class DispatchHook : IGltfTransformHook
    {
        private readonly string _mutation;
        private readonly GltfOperationStatus _status;

        public DispatchHook(string mutation, GltfOperationStatus status)
        {
            _mutation = mutation;
            _status = status;
        }

        public bool AddDiagnostic;
        public bool ReplaceContext;
        public bool ReturnIncompleteResult;
        public int CallCount;
        public string? InitialNodeName;
        public GltfTransformContext? Context;

        public GltfTransformStepResult ApplyTransformHook(GltfTransformContext context)
        {
            CallCount++;
            Context = context;
            InitialNodeName = context.Document!.Nodes[0].Name;
            context.Document.Nodes[0].Name = _mutation;
            if (ReplaceContext)
            {
                GltfDocument replacement = new GltfDocument();
                replacement.Nodes.Add(new GltfNode { Name = "replacement" });
                GltfResourceSet resources = new GltfResourceSet();
                resources.Resources.Add(new GltfResource { Bytes = new byte[] { 9 } });
                context.Document = replacement;
                context.Resources = resources;
            }

            GltfTransformStepResult result = new GltfTransformStepResult
            {
                Status = _status,
                ChangedObjects = new List<string> { "nodes/0" }
            };
            if (AddDiagnostic)
            {
                result.Diagnostics.Add(new GltfValidationIssue
                {
                    Code = GltfIssueCodes.OperationUnavailable,
                    Severity = GltfValidationSeverity.Warning,
                    Message = _mutation + " unavailable"
                });
            }

            if (ReturnIncompleteResult)
            {
                result.Diagnostics = null!;
            }

            return result;
        }
    }

    private sealed class CapabilityProbe : IGltfCompressionCodec, IGltfImageCodec
    {
        public GltfCompressionResult DecodeGeometry(GltfCompressionRequest request)
        {
            return new GltfCompressionResult();
        }

        public GltfCompressionResult EncodeGeometry(GltfCompressionRequest request)
        {
            return new GltfCompressionResult();
        }

        public GltfCompressionResult DecodeMeshoptData(GltfCompressionRequest request)
        {
            return new GltfCompressionResult();
        }

        public GltfCompressionResult EncodeMeshoptData(GltfCompressionRequest request)
        {
            return new GltfCompressionResult();
        }

        public GltfImageInfo InspectImage(GltfImageRequest request)
        {
            return new GltfImageInfo();
        }

        public GltfImageResult DecodeImage(GltfImageRequest request)
        {
            return new GltfImageResult();
        }

        public GltfImageResult EncodeImage(GltfImageRequest request)
        {
            return new GltfImageResult();
        }

        public GltfImageResult TranscodeImage(GltfImageRequest request)
        {
            return new GltfImageResult();
        }
    }
}
