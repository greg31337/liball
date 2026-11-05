using System.Text.Json.Nodes;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfTransformerTransactionTests
{
    [Test]
    public void TransformDocument_SuccessReturnsIndependentWorkingDocument()
    {
        GltfDocument source = CreateDocument();
        MutatingHook hook = new MutatingHook(GltfOperationStatus.Success);
        GltfTransformOptions options = CreateOptions(hook);

        GltfTransformReport report = new GltfTransformer().TransformDocument(
            source,
            CreatePlan(),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(report.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(report.Document, Is.Not.Null.And.Not.SameAs(source));
            Assert.That(report.Document!.Nodes[0].Name, Is.EqualTo("changed"));
            Assert.That(report.Document.Resources.Resources[0].Bytes[0], Is.EqualTo(9));
            Assert.That(report.Resources, Is.SameAs(report.Document.Resources));
            Assert.That(report.Document.Buffers[0].ResolvedBytes, Is.SameAs(
                report.Document.Resources.Resources[0].Bytes));
            Assert.That(hook.ReceivedDocument, Is.Not.SameAs(source));
            Assert.That(hook.ReceivedBytes, Is.Not.SameAs(
                source.Resources.Resources[0].Bytes));
            AssertSourceUnchanged(source);
        });
    }

    [Test]
    public void TransformDocument_InPlaceModeCommitsOnlySuccessfulWorkingState()
    {
        GltfDocument source = CreateDocument();
        MutatingHook hook = new MutatingHook(GltfOperationStatus.Success);
        GltfTransformOptions options = CreateOptions(hook);
        options.CloneDocument = false;

        GltfTransformReport report = new GltfTransformer().TransformDocument(
            source,
            CreatePlan(),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(report.Document, Is.SameAs(source));
            Assert.That(report.Resources, Is.SameAs(source.Resources));
            Assert.That(source.Nodes[0].Name, Is.EqualTo("changed"));
            Assert.That(source.Resources.Resources[0].Bytes[0], Is.EqualTo(9));
            Assert.That(source.PropertyJson.Extras!["state"]!.GetValue<string>(),
                Is.EqualTo("changed"));
        });
    }

    [Test]
    public void TransformDocument_FailedHookRollsBackDocumentAndOutputResources()
    {
        GltfDocument source = CreateDocument();
        MutatingHook hook = new MutatingHook(GltfOperationStatus.Failed);
        GltfTransformOptions options = CreateOptions(hook);

        GltfTransformReport report = new GltfTransformer().TransformDocument(
            source,
            CreatePlan(),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(report.Status, Is.EqualTo(GltfOperationStatus.Failed));
            Assert.That(report.Document, Is.Null);
            Assert.That(report.Resources.Resources, Is.Empty);
            Assert.That(report.Steps, Has.Count.EqualTo(1));
            Assert.That(hook.ReceivedDocument!.Nodes[0].Name, Is.EqualTo("changed"));
            AssertSourceUnchanged(source);
        });
    }

    [Test]
    public void TransformDocument_MissingHookReturnsUnavailableWithoutWorkingOutput()
    {
        GltfDocument source = CreateDocument();
        GltfTransformOptions options = new GltfTransformOptions
        {
            ValidateBefore = false,
            ValidateAfter = false
        };

        GltfTransformReport report = new GltfTransformer().TransformDocument(
            source,
            CreatePlan(),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(report.Status, Is.EqualTo(GltfOperationStatus.Unavailable));
            Assert.That(report.Document, Is.Null);
            Assert.That(report.Resources.Resources, Is.Empty);
            Assert.That(report.Steps[0].Status,
                Is.EqualTo(GltfOperationStatus.Unavailable));
            Assert.That(report.Diagnostics.Select(issue => issue.Code),
                Does.Contain(GltfIssueCodes.TransformHookUnavailable));
            AssertSourceUnchanged(source);
        });
    }

    [Test]
    public void TransformDocument_InternalHookErrorCannotLeakCandidateChanges()
    {
        GltfDocument source = CreateDocument();
        MutatingHook hook = new MutatingHook(GltfOperationStatus.Success);
        hook.ThrowAfterMutation = true;
        GltfTransformOptions options = CreateOptions(hook);

        Assert.That(
            () => new GltfTransformer().TransformDocument(
                source,
                CreatePlan(),
                options),
            Throws.TypeOf<InvalidOperationException>());
        AssertSourceUnchanged(source);
    }

    [Test]
    public void TransformDocument_PostValidationFailureCannotCommitInvalidCandidate()
    {
        GltfDocument source = CreateDocument();
        MutatingHook hook = new MutatingHook(GltfOperationStatus.Success);
        hook.MakeInvalid = true;
        GltfTransformOptions options = CreateOptions(hook);
        options.CloneDocument = false;
        options.ValidateAfter = true;

        Assert.That(
            () => new GltfTransformer().TransformDocument(
                source,
                CreatePlan(),
                options),
            Throws.TypeOf<GltfValidationException>());
        AssertSourceUnchanged(source);
    }

    [Test]
    public void TransformDocument_CanReturnPartialCloneWithoutMutatingSource()
    {
        GltfDocument source = CreateDocument();
        MutatingHook hook = new MutatingHook(GltfOperationStatus.Failed);
        GltfTransformOptions options = CreateOptions(hook);
        options.RollbackOnFailure = false;

        GltfTransformReport report = new GltfTransformer().TransformDocument(
            source,
            CreatePlan(),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(report.Document, Is.Not.Null.And.Not.SameAs(source));
            Assert.That(report.Document!.Nodes[0].Name, Is.EqualTo("changed"));
            Assert.That(report.Resources, Is.SameAs(report.Document.Resources));
            AssertSourceUnchanged(source);
        });
    }

    private static GltfDocument CreateDocument()
    {
        GltfDocument result = new GltfDocument();
        result.Nodes.Add(new GltfNode { Name = "original" });
        result.PropertyJson.HasExtras = true;
        result.PropertyJson.Extras = new JsonObject { ["state"] = "original" };
        GltfResource resource = new GltfResource
        {
            Bytes = [1, 2, 3],
            CacheKey = "resource"
        };
        result.Resources.Resources.Add(resource);
        result.Resources.Cache["resource"] = resource;
        result.Buffers.Add(new GltfBuffer
        {
            Resolved = true,
            ResolvedBytes = resource.Bytes,
            ByteLength = resource.Bytes.Length
        });
        return result;
    }

    private static GltfTransformPlan CreatePlan()
    {
        GltfTransformPlan result = new GltfTransformPlan();
        result.Steps.Add(new GltfTransformStep
        {
            Kind = GltfTransformKind.Custom,
            Name = "mutate"
        });
        return result;
    }

    private static GltfTransformOptions CreateOptions(IGltfTransformHook hook)
    {
        GltfTransformOptions result = new GltfTransformOptions
        {
            ValidateBefore = false,
            ValidateAfter = false
        };
        result.Hooks.Add(hook);
        return result;
    }

    private static void AssertSourceUnchanged(GltfDocument source)
    {
        Assert.Multiple(() =>
        {
            Assert.That(source.Nodes[0].Name, Is.EqualTo("original"));
            Assert.That(source.Resources.Resources[0].Bytes,
                Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(source.Buffers[0].ResolvedBytes, Is.SameAs(
                source.Resources.Resources[0].Bytes));
            Assert.That(source.PropertyJson.Extras!["state"]!.GetValue<string>(),
                Is.EqualTo("original"));
        });
    }

    private sealed class MutatingHook : IGltfTransformHook
    {
        private readonly GltfOperationStatus _status;

        public MutatingHook(GltfOperationStatus status)
        {
            _status = status;
        }

        public bool ThrowAfterMutation;
        public bool MakeInvalid;
        public GltfDocument? ReceivedDocument;
        public byte[]? ReceivedBytes;

        public GltfTransformStepResult ApplyTransformHook(GltfTransformContext context)
        {
            ReceivedDocument = context.Document;
            ReceivedBytes = context.Resources.Resources[0].Bytes;
            context.Document!.Nodes[0].Name = "changed";
            context.Resources.Resources[0].Bytes[0] = 9;
            context.Document.PropertyJson.Extras!["state"] = "changed";
            if (MakeInvalid)
            {
                context.Document.Nodes[0].HasMesh = true;
                context.Document.Nodes[0].Mesh = 99;
            }

            if (ThrowAfterMutation)
            {
                throw new InvalidOperationException("hook failed internally");
            }

            return new GltfTransformStepResult { Status = _status };
        }
    }
}
