using System.Text;
using gltf.Tests.Support;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfExecutionIsolationTests
{
    [Test]
    public void RepeatedRunsDoNotRetainMutatedReaderValidatorOrWriterResults()
    {
        byte[] json = Encoding.UTF8.GetBytes(
            "{\"asset\":{\"version\":\"2.0\"},"
            + "\"buffers\":[{\"uri\":\"data:application/gltf-buffer;base64,AQIDBA==\","
            + "\"byteLength\":4}],\"nodes\":[{\"name\":\"original\"}]}");
        GltfReadOptions readOptions = new GltfReadOptions();
        readOptions.ResolveResources = true;
        readOptions.Validate = true;
        GltfWriteOptions writeOptions = new GltfWriteOptions();
        writeOptions.ResourceMode = GltfResourceMode.DataUri;
        writeOptions.Validate = false;
        GltfReader reader = new GltfReader();
        GltfValidator validator = new GltfValidator();
        GltfWriter writer = new GltfWriter();

        GltfReadResult baselineRead = reader.ReadBytes(json, readOptions);
        GltfValidationReport baselineValidation = validator.ValidateDocument(
            baselineRead.Document!,
            new GltfValidationOptions());
        GltfWriteResult baselineWrite = writer.WriteBytes(
            baselineRead.Document!,
            writeOptions);
        string[] expectedIssues = baselineValidation.Issues
            .Select(IssueKey)
            .ToArray();

        int run = 0;
        while (run < 20)
        {
            GltfReadResult read = reader.ReadBytes(json, readOptions);
            GltfValidationReport validation = validator.ValidateDocument(
                read.Document!,
                new GltfValidationOptions());
            GltfWriteResult write = writer.WriteBytes(
                read.Document!,
                writeOptions);

            Assert.Multiple(() =>
            {
                Assert.That(read.Status, Is.EqualTo(GltfOperationStatus.Success), run.ToString());
                Assert.That(read.Document!.Nodes[0].Name, Is.EqualTo("original"), run.ToString());
                Assert.That(
                    read.Document.Buffers[0].ResolvedBytes,
                    Is.EqualTo(new byte[] { 1, 2, 3, 4 }),
                    run.ToString());
                Assert.That(validation.Issues.Select(IssueKey), Is.EqualTo(expectedIssues), run.ToString());
                Assert.That(write.PrimaryBytes, Is.EqualTo(baselineWrite.PrimaryBytes), run.ToString());
                Assert.That(write.PrimaryBytes, Is.Not.SameAs(baselineWrite.PrimaryBytes), run.ToString());
            });

            read.Document!.Nodes[0].Name = "mutated";
            read.Document.Buffers[0].ResolvedBytes[0] = 255;
            read.Document.Resources.Resources[0].Bytes[0] = 254;
            read.Container!.JsonBytes[0] = 0;
            validation.Issues.Clear();
            write.PrimaryBytes[0] = 0;
            run++;
        }
    }

    [Test]
    public void ParallelIndependentPipelinesKeepResolversAndOutputsIsolated()
    {
        const int pipelineCount = 32;
        Task<PipelineObservation>[] tasks = new Task<PipelineObservation>[pipelineCount];
        int pipelineIndex = 0;
        while (pipelineIndex < pipelineCount)
        {
            int capturedIndex = pipelineIndex;
            tasks[pipelineIndex] = Task.Run(
                () => RunIndependentPipeline(capturedIndex));
            pipelineIndex++;
        }

        PipelineObservation[] observations = Task.WhenAll(tasks)
            .GetAwaiter()
            .GetResult();

        int observationIndex = 0;
        while (observationIndex < observations.Length)
        {
            PipelineObservation observation = observations[observationIndex];
            Assert.Multiple(() =>
            {
                Assert.That(observation.Status, Is.EqualTo(GltfOperationStatus.Success));
                Assert.That(observation.ValidationValid, Is.True);
                Assert.That(observation.ResolverRequestCount, Is.EqualTo(1));
                Assert.That(observation.ResolvedValue, Is.EqualTo(observationIndex + 1));
                Assert.That(observation.RoundTripValue, Is.EqualTo(observationIndex + 1));
                Assert.That(observation.RepeatedOutput, Is.EqualTo(observation.Output));
                Assert.That(observation.RepeatedOutput, Is.Not.SameAs(observation.Output));
            });
            observationIndex++;
        }

        string[] uniqueOutputs = observations
            .Select(observation => Convert.ToBase64String(observation.Output))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        Assert.That(uniqueOutputs, Has.Length.EqualTo(pipelineCount));
    }

    private static PipelineObservation RunIndependentPipeline(int index)
    {
        byte value = (byte)(index + 1);
        byte[] json = Encoding.UTF8.GetBytes(
            "{\"asset\":{\"version\":\"2.0\"},"
            + "\"buffers\":[{\"uri\":\"shared.bin\",\"byteLength\":1}]}");
        TestResourceResolver resolver = new TestResourceResolver();
        resolver.Add(
            "shared.bin",
            GltfResourceKind.Buffer,
            GltfMimeTypes.GltfBuffer,
            [value]);
        GltfReadOptions readOptions = new GltfReadOptions();
        readOptions.ResolveResources = true;
        readOptions.ResourceResolver = resolver;
        GltfReadResult read = new GltfReader().ReadBytes(json, readOptions);
        GltfValidationReport validation = new GltfValidator().ValidateDocument(
            read.Document!,
            new GltfValidationOptions());

        GltfWriteOptions writeOptions = new GltfWriteOptions();
        writeOptions.ResourceMode = GltfResourceMode.DataUri;
        writeOptions.Validate = false;
        GltfWriter writer = new GltfWriter();
        GltfWriteResult write = writer.WriteBytes(read.Document!, writeOptions);
        GltfWriteResult repeatedWrite = writer.WriteBytes(read.Document!, writeOptions);

        GltfReadOptions roundTripOptions = new GltfReadOptions();
        roundTripOptions.ResolveResources = true;
        GltfReadResult roundTrip = new GltfReader().ReadBytes(
            write.PrimaryBytes,
            roundTripOptions);

        PipelineObservation result = new PipelineObservation();
        result.Status = read.Status;
        result.ValidationValid = validation.IsValid;
        result.ResolverRequestCount = resolver.Requests.Count;
        result.ResolvedValue = read.Document!.Buffers[0].ResolvedBytes[0];
        result.RoundTripValue = roundTrip.Document!.Buffers[0].ResolvedBytes[0];
        result.Output = write.PrimaryBytes;
        result.RepeatedOutput = repeatedWrite.PrimaryBytes;
        return result;
    }

    private static string IssueKey(GltfValidationIssue issue)
    {
        return issue.JsonPointer
            + "|" + issue.Code
            + "|" + (int)issue.Severity
            + "|" + issue.Message;
    }
}

internal sealed class PipelineObservation
{
    internal GltfOperationStatus Status;
    internal bool ValidationValid;
    internal int ResolverRequestCount;
    internal int ResolvedValue;
    internal int RoundTripValue;
    internal byte[] Output = [];
    internal byte[] RepeatedOutput = [];
}
