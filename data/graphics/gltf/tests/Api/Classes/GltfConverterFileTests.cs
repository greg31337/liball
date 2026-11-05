using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfConverterFileTests
{
    [Test]
    public void ConvertFile_AtomicallyReplacesPrimaryAndPublishesResources()
    {
        string directory = CreateTemporaryDirectory();
        string inputPath = Path.Combine(directory, "input.glb");
        string outputPath = Path.Combine(directory, "output.gltf");
        byte[] oldPrimary = [9, 9, 9];
        File.WriteAllBytes(inputPath, CreateGlbBytes());
        File.WriteAllBytes(outputPath, oldPrimary);
        GltfConversionOptions options = CreateGlbToJsonOptions();

        try
        {
            GltfConversionResult result = new GltfConverter().ConvertFile(
                inputPath,
                outputPath,
                options);
            byte[] primaryBytes = File.ReadAllBytes(outputPath);
            JsonObject json = ParseJson(primaryBytes);
            string resourcePath = Path.Combine(directory, "buffer.bin");

            Assert.Multiple(() =>
            {
                Assert.That(result.Status,
                    Is.EqualTo(GltfOperationStatus.Success));
                Assert.That(result.PrimaryPath,
                    Is.EqualTo(Path.GetFullPath(outputPath)));
                Assert.That(result.PrimaryBytes, Is.Empty);
                Assert.That(result.PrimaryContentHash,
                    Is.EqualTo(System.Convert.ToHexString(
                        SHA256.HashData(primaryBytes)).ToLowerInvariant()));
                Assert.That(result.Manifest, Has.Count.EqualTo(1));
                Assert.That(result.Manifest[0].EffectiveUri,
                    Is.EqualTo("buffer.bin"));
                Assert.That(result.Manifest[0].External, Is.True);
                Assert.That(result.Manifest[0].ContentHash,
                    Is.EqualTo(System.Convert.ToHexString(
                        SHA256.HashData([1, 2, 3, 4])).ToLowerInvariant()));
                Assert.That(primaryBytes, Is.Not.EqualTo(oldPrimary));
                Assert.That(json["buffers"]![0]!["uri"]!.GetValue<string>(),
                    Is.EqualTo("buffer.bin"));
                Assert.That(File.ReadAllBytes(resourcePath),
                    Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
                Assert.That(FindTemporaryFiles(directory), Is.Empty);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ConvertFile_RejectsResourceCollisionBeforeReplacingPrimary()
    {
        string directory = CreateTemporaryDirectory();
        string inputPath = Path.Combine(directory, "input.glb");
        string outputPath = Path.Combine(directory, "output.gltf");
        string resourcePath = Path.Combine(directory, "buffer.bin");
        byte[] oldPrimary = [8, 8];
        byte[] oldResource = [7, 7];
        File.WriteAllBytes(inputPath, CreateGlbBytes());
        File.WriteAllBytes(outputPath, oldPrimary);
        File.WriteAllBytes(resourcePath, oldResource);
        GltfConversionOptions options = CreateGlbToJsonOptions();

        try
        {
            Assert.That(
                () => new GltfConverter().ConvertFile(
                    inputPath,
                    outputPath,
                    options),
                Throws.TypeOf<GltfWriteException>().With.Message.EqualTo(
                    "Output resource 'buffer.bin' already exists."));
            Assert.Multiple(() =>
            {
                Assert.That(File.ReadAllBytes(outputPath),
                    Is.EqualTo(oldPrimary));
                Assert.That(File.ReadAllBytes(resourcePath),
                    Is.EqualTo(oldResource));
                Assert.That(FindTemporaryFiles(directory), Is.Empty);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ConvertFile_FailedTransformLeavesExistingOutputUntouched()
    {
        string directory = CreateTemporaryDirectory();
        string inputPath = Path.Combine(directory, "input.gltf");
        string outputPath = Path.Combine(directory, "output.gltf");
        byte[] oldPrimary = [8, 7, 6];
        File.WriteAllText(
            inputPath,
            "{\"asset\":{\"version\":\"2.0\"}}",
            new UTF8Encoding(false));
        File.WriteAllBytes(outputPath, oldPrimary);
        GltfConversionOptions options = new GltfConversionOptions();
        options.OutputFormat = GltfContainerFormat.Json;
        options.WriteOptions.Validate = false;
        options.TransformPlan = new GltfTransformPlan
        {
            Steps = [new GltfTransformStep { Kind = GltfTransformKind.Custom }]
        };
        options.TransformOptions.ValidateBefore = false;
        options.TransformOptions.ValidateAfter = false;

        try
        {
            GltfConversionResult result = new GltfConverter().ConvertFile(
                inputPath,
                outputPath,
                options);

            Assert.Multiple(() =>
            {
                Assert.That(result.Status,
                    Is.EqualTo(GltfOperationStatus.Unavailable));
                Assert.That(result.PrimaryBytes, Is.Empty);
                Assert.That(result.PrimaryPath, Is.Null);
                Assert.That(result.TransformReport, Is.Not.Null);
                Assert.That(File.ReadAllBytes(outputPath), Is.EqualTo(oldPrimary));
                Assert.That(FindTemporaryFiles(directory), Is.Empty);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestCase(true)]
    [TestCase(false)]
    public void ConvertFile_OverwritesResourcesUnderAtomicAndDirectPolicies(
        bool atomicOutput)
    {
        string directory = CreateTemporaryDirectory();
        string inputPath = Path.Combine(directory, "input.glb");
        string outputPath = Path.Combine(directory, "output.gltf");
        string resourcePath = Path.Combine(directory, "buffer.bin");
        File.WriteAllBytes(inputPath, CreateGlbBytes());
        File.WriteAllBytes(resourcePath, [6, 6]);
        GltfConversionOptions options = CreateGlbToJsonOptions();
        options.AtomicOutput = atomicOutput;
        options.OverwriteResources = true;

        try
        {
            GltfConversionResult result = new GltfConverter().ConvertFile(
                inputPath,
                outputPath,
                options);

            Assert.Multiple(() =>
            {
                Assert.That(result.Status,
                    Is.EqualTo(GltfOperationStatus.Success));
                Assert.That(File.Exists(outputPath), Is.True);
                Assert.That(File.ReadAllBytes(resourcePath),
                    Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
                Assert.That(FindTemporaryFiles(directory), Is.Empty);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ConvertFile_ResolvesRelativeInputResourcesFromInputDirectory()
    {
        string directory = CreateTemporaryDirectory();
        string inputPath = Path.Combine(directory, "input.gltf");
        string inputResourcePath = Path.Combine(directory, "source.bin");
        string outputPath = Path.Combine(directory, "output.glb");
        File.WriteAllText(
            inputPath,
            "{\"asset\":{\"version\":\"2.0\"},\"buffers\":[{\"uri\":\"source.bin\",\"byteLength\":4}],\"bufferViews\":[{\"buffer\":0,\"byteLength\":4}]}",
            new UTF8Encoding(false));
        File.WriteAllBytes(inputResourcePath, [4, 3, 2, 1]);
        GltfConversionOptions options = new GltfConversionOptions();
        options.InputFormat = GltfContainerFormat.Auto;
        options.OutputFormat = GltfContainerFormat.Auto;
        options.ResourceMode = GltfResourceMode.GlbBin;
        options.WriteOptions.Validate = false;

        try
        {
            GltfConversionResult result = new GltfConverter().ConvertFile(
                inputPath,
                outputPath,
                options);
            GltfReadOptions readOptions = new GltfReadOptions();
            GltfReadResult readResult = new GltfReader().ReadBytes(
                File.ReadAllBytes(outputPath),
                readOptions);

            Assert.Multiple(() =>
            {
                Assert.That(result.Status,
                    Is.EqualTo(GltfOperationStatus.Success));
                Assert.That(result.Document!.Container!.Format,
                    Is.EqualTo(GltfContainerFormat.Glb));
                Assert.That(readResult.Container!.BinBytes,
                    Is.EqualTo(new byte[] { 4, 3, 2, 1 }));
                Assert.That(File.ReadAllBytes(inputResourcePath),
                    Is.EqualTo(new byte[] { 4, 3, 2, 1 }));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ConvertFile_PreserveModeRetainsResourceAlreadyAtTarget()
    {
        string directory = CreateTemporaryDirectory();
        string inputPath = Path.Combine(directory, "input.gltf");
        string resourcePath = Path.Combine(directory, "source.bin");
        string outputPath = Path.Combine(directory, "normalized.gltf");
        File.WriteAllText(
            inputPath,
            "{ \"asset\": { \"version\": \"2.0\" }, \"buffers\": [{ \"uri\": \"source.bin\", \"byteLength\": 4 }] }",
            new UTF8Encoding(false));
        File.WriteAllBytes(resourcePath, [5, 6, 7, 8]);
        GltfConversionOptions options = new GltfConversionOptions();
        options.InputFormat = GltfContainerFormat.Auto;
        options.OutputFormat = GltfContainerFormat.Auto;
        options.ResourceMode = GltfResourceMode.Preserve;
        options.WriteOptions.Validate = false;

        try
        {
            GltfConversionResult result = new GltfConverter().ConvertFile(
                inputPath,
                outputPath,
                options);
            JsonObject json = ParseJson(File.ReadAllBytes(outputPath));

            Assert.Multiple(() =>
            {
                Assert.That(result.Status,
                    Is.EqualTo(GltfOperationStatus.Success));
                Assert.That(json["buffers"]![0]!["uri"]!.GetValue<string>(),
                    Is.EqualTo("source.bin"));
                Assert.That(File.ReadAllBytes(resourcePath),
                    Is.EqualTo(new byte[] { 5, 6, 7, 8 }));
                Assert.That(FindTemporaryFiles(directory), Is.Empty);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void AtomicTransaction_DisposeRemovesOnlyStagedTemporaryFiles()
    {
        string directory = CreateTemporaryDirectory();
        string finalPath = Path.Combine(directory, "output.gltf");
        FileConversionOutputItem item = new FileConversionOutputItem();
        item.FinalPath = finalPath;
        item.Bytes = [1, 2, 3];
        item.Primary = true;
        List<FileConversionOutputItem> items =
            new List<FileConversionOutputItem> { item };
        FileConversionOutputWriter writer =
            new FileConversionOutputWriter();
        writer.AssignTemporaryPaths(items);

        try
        {
            using (FileConversionOutputTransaction transaction =
                new FileConversionOutputTransaction())
            {
                transaction.Items = items;
                transaction.StageFiles();
                Assert.That(File.Exists(item.TemporaryPath), Is.True);
            }

            Assert.Multiple(() =>
            {
                Assert.That(Path.GetDirectoryName(item.TemporaryPath),
                    Is.EqualTo(directory));
                Assert.That(Path.GetFileName(item.TemporaryPath),
                    Does.StartWith("output.gltf.tmp-"));
                Assert.That(File.Exists(item.TemporaryPath), Is.False);
                Assert.That(File.Exists(finalPath), Is.False);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static GltfConversionOptions CreateGlbToJsonOptions()
    {
        GltfConversionOptions result = new GltfConversionOptions();
        result.InputFormat = GltfContainerFormat.Auto;
        result.OutputFormat = GltfContainerFormat.Auto;
        result.ResourceMode = GltfResourceMode.External;
        result.WriteOptions.Validate = false;
        return result;
    }

    private static byte[] CreateGlbBytes()
    {
        GltfDocument document = new GltfDocument();
        document.Container = new GltfContainerData
        {
            Format = GltfContainerFormat.Glb,
            HasBinChunk = true,
            BinBytes = [1, 2, 3, 4]
        };
        document.Buffers.Add(new GltfBuffer
        {
            ByteLength = 4
        });
        GltfWriteOptions options = new GltfWriteOptions();
        options.Format = GltfContainerFormat.Glb;
        options.ResourceMode = GltfResourceMode.Preserve;
        options.Validate = false;
        return new GltfWriter().WriteBytes(document, options).PrimaryBytes;
    }

    private static JsonObject ParseJson(byte[] bytes)
    {
        return JsonNode.Parse(Encoding.UTF8.GetString(bytes))!.AsObject();
    }

    private static string CreateTemporaryDirectory()
    {
        string result = Path.Combine(
            Path.GetTempPath(),
            "gltf-converter-file-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(result);
        return result;
    }

    private static string[] FindTemporaryFiles(string directory)
    {
        return Directory.GetFiles(
            directory,
            "*.tmp-*",
            SearchOption.AllDirectories);
    }
}
